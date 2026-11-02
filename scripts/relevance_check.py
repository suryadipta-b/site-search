#!/usr/bin/env python3
import json
import os
import re
import xml.etree.ElementTree as ET

# 1. Users & Access Control
USERS = {
    "asha": {"projects": ["P1"]},
    "ravi": {"projects": ["P2"]},
    "meera": {"projects": ["P1", "P2"]},
    "guest": {"projects": []}
}

def can_access(user_key, project_id):
    if not project_id:
        return True # Regulations are global
    user = USERS.get(user_key, USERS["guest"])
    return project_id in user["projects"]

# 2. Damerau-Levenshtein Algorithm
def damerau_levenshtein(s1, s2):
    d = {}
    len1, len2 = len(s1), len(s2)
    for i in range(-1, len1 + 1):
        d[(i, -1)] = i + 1
    for j in range(-1, len2 + 1):
        d[(-1, j)] = j + 1

    for i in range(len1):
        for j in range(len2):
            cost = 0 if s1[i] == s2[j] else 1
            d[(i, j)] = min(
                d[(i - 1, j)] + 1,       # deletion
                d[(i, j - 1)] + 1,       # insertion
                d[(i - 1, j - 1)] + cost # substitution
            )
            if i > 0 and j > 0 and s1[i] == s2[j - 1] and s1[i - 1] == s2[j]:
                d[(i, j)] = min(d[(i, j)], d[(i - 2, j - 2)] + cost) # transposition
    return d[(len1 - 1, len2 - 1)]

# 3. Load Synonyms
def load_synonyms(path):
    with open(path, "r", encoding="utf-8") as f:
        rules = json.load(f)
    expansions = {}
    for r in rules:
        term = r["term"].strip().lower()
        targets = [t.strip().lower() for t in r["expandsTo"] if t.strip()]
        direction = r.get("direction", "two-way")
        if direction == "two-way":
            all_in_group = set(targets + [term])
            for m in all_in_group:
                if m not in expansions:
                    expansions[m] = []
                for sib in all_in_group:
                    if sib != m and sib not in expansions[m]:
                        expansions[m].append(sib)
        else:
            if term not in expansions:
                expansions[term] = []
            for t in targets:
                if t not in expansions[term]:
                    expansions[term].append(t)
    return expansions

# 4. Tokenizer & Query Builder
IDENTIFIER_REGEX = re.compile(r'\b(?:(?:RFI|DL|NCR)(?:-[A-Za-z0-9]+)?-\d+|\d{4}\.\d+)\b', re.IGNORECASE)
SYNTAX_REGEX = re.compile(r'[\+\&\|\!\(\)\{\}\[\]\^\""\~\*\?\:\\\/]')
STANDALONE_HYPHEN_REGEX = re.compile(r'(?<=\s)-(?=\w)|(?<=\w)-(?=\s)|^-(?=\w)|(?<=\w)-$')
STOPWORDS = {"for", "of", "a", "an", "the", "in", "on", "at", "to", "by", "is", "it"}

def sanitize_query(q):
    if not q:
        return ""
    cleaned = SYNTAX_REGEX.sub(" ", q)
    cleaned = STANDALONE_HYPHEN_REGEX.sub(" ", cleaned)
    return re.sub(r'\s+', ' ', cleaned).strip()

def tokenize(text):
    if not text:
        return []
    return [t.lower() for t in re.split(r'[^\w\.\-]+', text) if t]

def build_query(raw_q, expansions):
    raw = raw_q or ""
    detected_ids = [m.group(0) for m in IDENTIFIER_REGEX.finditer(raw)]
    sanitized = sanitize_query(raw)
    tokens = [t.lower() for t in sanitized.split() if t]
    
    synonym_tokens = []
    lower_s = sanitized.lower()
    for k, v in expansions.items():
        if " " in k and k in lower_s:
            for exp in v:
                for word in exp.split():
                    w = word.lower()
                    if w not in STOPWORDS and w not in synonym_tokens and w not in tokens:
                        synonym_tokens.append(w)

    for t in tokens:
        if t in expansions:
            for exp in expansions[t]:
                for word in exp.split():
                    w = word.lower()
                    if w not in STOPWORDS and w not in synonym_tokens and w not in tokens:
                        synonym_tokens.append(w)

    fuzzy_eligible = set()
    for t in tokens:
        if len(t) >= 4 and not IDENTIFIER_REGEX.match(t) and not t.isdigit() and not any(c.isdigit() for c in t):
            fuzzy_eligible.add(t)

    return {
        "raw": raw_q,
        "sanitized": sanitized,
        "identifiers": detected_ids,
        "tokens": tokens,
        "synonyms": synonym_tokens,
        "fuzzy": fuzzy_eligible
    }

# 5. Ingestion
def load_corpus(proj_docs_path, osha_xml_path):
    docs = []
    # Project docs
    with open(proj_docs_path, "r", encoding="utf-8") as f:
        pdocs = json.load(f)
    for p in pdocs:
        docs.append(p)

    # OSHA XML
    tree = ET.parse(osha_xml_path)
    root = tree.getroot()
    for subpart in root.iter():
        if subpart.attrib.get("TYPE", "").upper() == "SUBPART":
            head_el = subpart.find("HEAD")
            subpart_head = head_el.text.strip() if head_el is not None and head_el.text else "OSHA Safety Regulations"
            for sec in subpart.iter():
                if sec.attrib.get("TYPE", "").upper() == "SECTION":
                    n = sec.attrib.get("N", "")
                    sec_head = sec.find("HEAD")
                    title = sec_head.text.strip() if sec_head is not None and sec_head.text else f"OSHA {n}"
                    paras = [p.text.strip() for p in sec.iter("P") if p.text]
                    body = "\n\n".join(paras)
                    docs.append({
                        "id": f"reg-{n.replace('.', '-').lower()}",
                        "projectId": None,
                        "type": "Regulation",
                        "number": n,
                        "title": title,
                        "body": body,
                        "discipline": "HSE",
                        "createdAt": "2026-10-01T00:00:00Z",
                        "subpart": subpart_head
                    })
    return docs

# 6. Scoring
def score_document(doc, query):
    score = 0.0
    matched = set()

    doc_num = doc.get("number", "")
    doc_title = doc.get("title", "")
    doc_body = doc.get("body", "")
    doc_disc = doc.get("discipline") or ""
    doc_type = doc.get("type", "")

    num_words = tokenize(doc_num)
    title_words = tokenize(doc_title)
    body_words = tokenize(doc_body)

    # A. Exact Identifier Boost (+100)
    for id_tok in query["identifiers"]:
        if doc_num.lower() == id_tok.lower():
            score += 100.0
            matched.add(doc_num)
        elif id_tok.lower() in doc_num.lower():
            score += 40.0
            matched.add(id_tok)
        elif id_tok.lower() in doc_body.lower() or id_tok.lower() in doc_title.lower():
            score += 5.0
            matched.add(id_tok)

    # B. Primary Tokens
    primary_matches = 0
    for tok in query["tokens"]:
        term_score = 0.0
        # Number match
        if tok in num_words or tok == doc_num.lower():
            term_score += 20.0
            matched.add(tok)
        # Title match
        if tok in title_words:
            term_score += 10.0
            matched.add(tok)
        elif tok in doc_title.lower():
            term_score += 6.0
            matched.add(tok)
        # Body match
        cnt = body_words.count(tok)
        if cnt > 0:
            term_score += min(10.0, 2.0 * cnt)
            matched.add(tok)
        elif tok in doc_body.lower():
            term_score += 1.5
            matched.add(tok)
        # Discipline/Type
        if tok == doc_disc.lower() or tok == doc_type.lower():
            term_score += 3.0
            matched.add(tok)

        # Fuzzy match
        if term_score == 0 and tok in query["fuzzy"]:
            for tw in title_words:
                if len(tw) >= 4 and damerau_levenshtein(tok, tw) <= 1:
                    term_score += 6.0
                    matched.add(tw)
                    break
            if term_score == 0:
                for bw in body_words:
                    if len(bw) >= 4 and damerau_levenshtein(tok, bw) <= 1:
                        term_score += 1.5
                        matched.add(bw)
                        break

        if term_score > 0:
            score += term_score
            primary_matches += 1

    # C. Synonym Tokens
    for syn in query["synonyms"]:
        syn_score = 0.0
        if syn in title_words:
            syn_score += 7.0
            matched.add(syn)
        elif syn in doc_title.lower():
            syn_score += 4.5
            matched.add(syn)
        scnt = body_words.count(syn)
        if scnt > 0:
            syn_score += min(6.0, 1.5 * scnt)
            matched.add(syn)
        elif syn in doc_body.lower():
            syn_score += 1.0
            matched.add(syn)
        score += syn_score

    # Coordination factor
    if len(query["tokens"]) > 1 and primary_matches > 1:
        score *= (1.0 + (primary_matches / len(query["tokens"])))

    return score, list(matched)

def search(docs, query_str, user_key, doc_type=None, project=None):
    expansions = load_synonyms("data/synonyms.json")
    query = build_query(query_str, expansions)

    # 1. Access Control
    accessible = [d for d in docs if can_access(user_key, d.get("projectId"))]

    scored = []
    for d in accessible:
        if not query["sanitized"]:
            scored.append({"doc": d, "score": 1.0, "matched": []})
        else:
            s, m = score_document(d, query)
            if s > 0:
                scored.append({"doc": d, "score": s, "matched": m})

    # Facets (computed on accessible matching docs)
    facets = {"types": {}, "projects": {}}
    for h in scored:
        t = h["doc"].get("type")
        if t:
            facets["types"][t] = facets["types"].get(t, 0) + 1
        p = h["doc"].get("projectId")
        if p and can_access(user_key, p):
            facets["projects"][p] = facets["projects"].get(p, 0) + 1

    # Filters
    filtered = scored
    if doc_type:
        filtered = [h for h in filtered if h["doc"].get("type", "").lower() == doc_type.lower()]
    if project:
        filtered = [h for h in filtered if (h["doc"].get("projectId") or "").lower() == project.lower()]

    filtered.sort(key=lambda x: (x["score"], x["doc"].get("createdAt", "")), reverse=True)
    return filtered, facets

def run_checks():
    docs = load_corpus("data/project-documents.json", "data/osha-title-29.xml")
    with open("relevance-cases.json", "r", encoding="utf-8") as f:
        cases = json.load(f)

    print("=" * 88)
    print("           VConstruct Site Search - Relevance Check Verification (B4)")
    print("=" * 88)
    print(f"{'#':<3} | {'Query':<36} | {'As':<6} | {'Condition':<32} | Result")
    print("-" * 88)

    passed = 0
    failed = 0

    for idx, c in enumerate(cases, 1):
        q = c["query"]
        as_user = c["asUser"]
        exp = c["expected"]
        ctype = exp["type"]

        hits, facets = search(docs, q, as_user)
        hit_nums = [h["doc"]["number"] for h in hits]

        ok = False
        detail = ""

        if ctype == "is_first":
            target = exp["docNumber"]
            if hit_nums and hit_nums[0] == target:
                ok = True
                detail = f"Top hit: {hit_nums[0]}"
            else:
                detail = f"Expected {target}, got {hit_nums[0] if hit_nums else 'None'}"

        elif ctype == "in_top_n":
            target = exp["docNumber"]
            n = exp["n"]
            if target in hit_nums[:n]:
                ok = True
                rank = hit_nums[:n].index(target) + 1
                detail = f"Found at rank #{rank}"
            else:
                detail = f"Not in top {n} (Top 3: {hit_nums[:3]})"

        elif ctype == "ranks_above":
            high = exp["higherDocNumber"]
            low = exp["lowerDocNumber"]
            r_high = hit_nums.index(high) if high in hit_nums else -1
            r_low = hit_nums.index(low) if low in hit_nums else -1
            if r_high >= 0 and (r_low < 0 or r_high < r_low):
                ok = True
                detail = f"{high} (#{r_high+1}) > {low} (#{r_low+1 if r_low >= 0 else 'absent'})"
            else:
                detail = f"Failed: {high}=#{r_high+1}, {low}=#{r_low+1}"

        elif ctype == "both_returned":
            doc_nums = exp["docNumbers"]
            has_all = all(d in hit_nums for d in doc_nums)
            if has_all:
                ok = True
                detail = "Both present"
            else:
                detail = f"Missing: {[d for d in doc_nums if d not in hit_nums]}"

        elif ctype == "returned_and_not_returned":
            ret = exp["returnedDocNumber"]
            not_ret = exp["notReturnedDocNumber"]
            has_ret = ret in hit_nums
            has_not_ret = not_ret in hit_nums
            if has_ret and not has_not_ret:
                ok = True
                detail = f"{ret} in, {not_ret} excluded"
            else:
                detail = f"has_ret={has_ret}, has_not_ret={has_not_ret}"

        elif ctype == "not_returned_and_facet_only":
            target = exp["docNumber"]
            allowed_p = exp["expectedProjects"]
            not_in_hits = target not in hit_nums
            only_allowed_facets = all(p in allowed_p for p in facets["projects"].keys())
            if not_in_hits and only_allowed_facets:
                ok = True
                detail = f"{target} excluded, Facets: {list(facets['projects'].keys())}"
            else:
                detail = f"not_in_hits={not_in_hits}, facets={facets['projects']}"

        status = "[PASS]" if ok else "[FAIL]"
        if ok:
            passed += 1
        else:
            failed += 1
        print(f"{idx:<3} | {q:<36} | {as_user:<6} | {ctype:<32} | {status} {detail}")

    print("-" * 88)
    print(f"Summary: {passed} passed, {failed} failed out of {len(cases)} test cases.")
    return failed == 0

if __name__ == "__main__":
    success = run_checks()
    exit(0 if success else 1)
