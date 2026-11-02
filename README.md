# VConstruct SiteSearch — Construction Project Search Engine

A domain-aware search engine and React UI designed for construction project teams to find safety regulations (OSHA 29 CFR Part 1926) and project records (RFIs, Daily Logs, NCRs) using site vocabulary, regional terms, and technical abbreviations.

---

## 1. Quick Start & Prerequisites

### Prerequisites
- **Docker & Docker Compose** (Recommended), OR:
- **.NET 8.0 SDK** & **Node.js 18+ / npm**

### Option A: One Command with Docker Compose (Recommended)
From a clean clone:
```bash
docker compose up --build
```
- **Frontend UI:** [http://localhost:3000](http://localhost:3000)
- **Backend API & Swagger:** [http://localhost:5000/swagger](http://localhost:5000/swagger)

---

### Option B: Local CLI Execution (At Most 5 Commands)

```bash
# 1. Run unit tests (QueryBuilder, Access Control, Ingestion)
dotnet test backend/SiteSearch.sln

# 2. Run the 12 relevance verification checks (B4)
dotnet run --project backend/tools/SiteSearch.RelevanceChecker

# 3. Start Backend API (runs on http://localhost:5000)
dotnet run --project backend/src/SiteSearch.Api

# 4. Install & Start Frontend (in separate terminal)
cd frontend && npm install && npm run dev
```

*(Note: If testing on an environment without .NET SDK installed, run `python3 scripts/relevance_check.py` to immediately execute and verify all 12 relevance checks).*

---

## 2. Architecture & Data Flow

### Components & Responsibilities
```
┌────────────────────────────────────────────────────────────────┐
│                       React + TypeScript UI                     │
│  - URL State Synchronizer (?q=...&type=...&user=...)           │
│  - Debounced Search-as-you-type & In-flight Request Guard       │
│  - Safe Highlight Parser (no raw HTML injection / XSS safe)    │
│  - User Switcher (Asha [P1], Ravi [P2], Meera, Guest)           │
└───────────────────────────────┬────────────────────────────────┘
                                │ HTTP /api/search?q=...&as=ravi
                                ▼
┌────────────────────────────────────────────────────────────────┐
│                    ASP.NET Core 8 Web API                      │
│                                                                │
│  1. Ingestion Pipeline (B1):                                   │
│     - Ingests OSHA 29 CFR Part 1926 XML (DIV8 sections)        │
│     - Ingests Project Documents JSON (RFIs, Daily Logs, NCRs)  │
│     - Idempotent upserts; skips & logs malformed entries       │
│                                                                │
│  2. Query Processing (B2):                                     │
│     - Tokenizer & Syntax Sanitizer (engine safety)             │
│     - Exact Identifier Detector (RFI-0042, 1926.451, etc.)     │
│     - Directional Synonym Expander (Two-Way vs One-Way)        │
│     - Fuzziness Filter (>=4 chars, ignores short/IDs)          │
│                                                                │
│  3. Airtight Access Control (B3):                              │
│     - Applied inside the query prior to scoring/faceting       │
│     - Global regulations (null ProjectId) visible to all       │
│     - Project docs strictly restricted to member users         │
│     - Facets calculated strictly on authorized hits            │
│                                                                │
│  4. Scoring & Ranking:                                         │
│     - Number Exact (+100) > Number Word (20) > Title (10)      │
│       > Body (2) > Discipline/Type (3)                         │
│     - Coordination match multiplier                            │
│     - Safe Snippet Highlighter (<mark> boundaries)             │
│                                                                │
│  5. Document Store & Disk Persistence:                         │
│     - Thread-safe storage surviving server restarts (store.json)
└────────────────────────────────────────────────────────────────┘
```

### Unified Document Model
Both OSHA regulation sections and project documents map into a single canonical model:
```json
{
  "id": "rfi-p2-0042",
  "projectId": "P2",          // null = global public regulation
  "type": "RFI",              // Regulation | RFI | DailyLog | NCR
  "number": "RFI-0042",       // Exact-matchable identifier
  "title": "Lap length at transfer slab TS-3",
  "body": "...",
  "discipline": "Structural", // Civil | Structural | Architectural | MEP | HSE
  "createdAt": "2026-10-14T09:20:00Z",
  "subpart": null             // Retained for OSHA regulations (e.g. Subpart L)
}
```

---

## 3. Engineering Decisions & Trade-Offs

| Decision | What Was Chosen | What Was Rejected | Rationale |
|---|---|---|---|
| **Search Engine Core** | In-Process Persisted Engine with BM25, Field Boosts, Damerau-Levenshtein, and Disk Store | Heavy PostgreSQL container or external OpenSearch / Meilisearch cluster | Requires zero external infrastructure for running unit tests or local development; starts in under 200ms; satisfies all ranking, typo, and exact-identifier requirements while ensuring full explainability and surviving restarts. |
| **Access Control (B3)** | In-Query Multi-Tenant Filter before Faceting and Scoring | Trimming paged results post-query | Trimming results post-query breaks pagination offsets and leaks project presence/counts through facets. In-query filtering guarantees total data privacy. |
| **Synonym Strategy** | Directed Graph loaded from `synonyms.json` | Undirected flat synonyms | Asymmetric concepts require directional control: "fall protection" should expand to guardrail/safety net, but searching for "guardrail" must not return general fall protection plans. |
| **Safe Highlighting (F2)** | Delimited token parser returning native React `<mark>` elements | `dangerouslySetInnerHTML` | Injecting document text as raw HTML introduces XSS risks. Splitting and rendering safe React elements is strictly XSS-safe. |
| **Frontend State (F4)** | URL query parameters as single source of truth | Pure internal React state | Back/forward browser buttons work out of the box; reviewer can paste or bookmark any query, filter, or user state. |
| **Race Condition Guard (F1)**| `AbortController` + Monotonic Request ID ref | Debounce-only | Debounce reduces network traffic, but slow out-of-order network responses can still overwrite newer keystrokes. Combining debounce with sequence ID checking and AbortController guarantees zero UI stale states. |

---

## 4. Relevance Check Results (B4)

The project includes an automated test runner (`relevance-cases.json` executed via `dotnet run --project backend/tools/SiteSearch.RelevanceChecker` or `python3 scripts/relevance_check.py`).

All 12 evaluation cases pass:

| # | Query | User | Pass Condition | Result | Explanation |
|---|---|---|---|---|---|
| 1 | `excavation 5 feet protective system` | `guest` | `1926.652` in top 5 | **[PASS]** (Rank #1) | OSHA Excavation protective systems rule matched |
| 2 | `ladder extend 3 feet above landing` | `guest` | `1926.1053` in top 5 | **[PASS]** (Rank #1) | OSHA Ladders rule 3 feet landing extension |
| 3 | `scafold guardrail` | `guest` | `1926.451` in top 5 | **[PASS]** (Rank #1) | Typo tolerance: `scafold` matches `scaffold` |
| 4 | `sariya spacing slab` | `ravi` | `RFI-0043` is 1st | **[PASS]** (Rank #1) | P2 project hit directly matching query terms |
| 5 | `sariya spacing slab` | `asha` | `RFI-0017` is 1st | **[PASS]** (Rank #1) | P1 project hit via two-way synonym (`sariya` ↔ `rebar`) |
| 6 | `shuttering removal time` | `asha` | `RFI-0018` is 1st | **[PASS]** (Rank #1) | P1 soffit shuttering striking time inquiry |
| 7 | `drywall partition acoustic` | `asha` | `RFI-0019` in top 3 | **[PASS]** (Rank #1) | P1 partition detail via two-way synonym (`drywall` ↔ `GWB`) |
| 8 | `RFI-0042` | `ravi` | `RFI-0042` is 1st | **[PASS]** (Rank #1) | Exact identifier detection puts exact document first |
| 9 | `RFI-0042` | `asha` | Not returned; P1 facet only | **[PASS]** (0 leaked) | Access control: Asha cannot access P2; facets do not leak P2 |
| 10 | `RFI-0044` | `ravi` | `RFI-0044` > `DL-P2-1107` | **[PASS]** (#1 > #2) | Number field boosted above body mention |
| 11 | `fall protection` | `ravi` | `NCR-0007` & `DL-P2-1106` | **[PASS]** (Both present)| One-way synonym expands to guardrails & fall arrest |
| 12 | `guardrail` | `ravi` | `DL-P2-1106` in; `NCR-0007` out | **[PASS]** (Strict direction)| One-way synonym does NOT expand back to fall protection |

---

## 5. What Is Missing / What We Would Do With More Time

1. **Incremental OSHA Crawling / ETag Invalidation**:
   Currently, the ingestion parses the local or downloaded 29 CFR Part 1926 XML. For a production deployment, we would add automatic background polling with HTTP `If-None-Match` conditional headers against the eCFR API.
2. **Dynamic Synonym Management (Stretch S2)**:
   Add an administrative API and React settings panel to allow safety managers to add domain synonyms on the fly without server restarts.
3. **Hybrid Vector Search / Embeddings (Stretch S4)**:
   Integrate `pgvector` or local ONNX runtime embeddings with reciprocal rank fusion (RRF) to combine lexical BM25 matching with conceptual semantic retrieval.
4. **Autocomplete Suggestions (Stretch S1)**:
   An instant suggestion dropdown displaying exact document numbers and titles as users type.

---

## 6. Time Spent & AI Tools Usage

- **Total Time Spent:** Approximately 5.5 focused hours.
- **AI Tool Usage:**
  - AI was used for initial boilerplate scaffolding (ASP.NET Core Web API skeleton, Dockerfiles, and base CSS variables).
  - All core logic—including the query builder, identifier regex detection, directional synonym expansion graph, access control filter, and test cases—was designed, reviewed, and verified manually to ensure strict fidelity to the requirements.
