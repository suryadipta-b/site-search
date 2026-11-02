import json

# 1. Project Documents
project_docs = [
  {"id": "rfi-p2-0042", "projectId": "P2", "type": "RFI", "number": "RFI-0042", "discipline": "Structural", "createdAt": "2026-10-14T09:20:00Z",
   "title": "Lap length at transfer slab TS-3",
   "body": "Drawing S-301 shows 20 mm Fe 500D bars in transfer slab TS-3 but does not give the lap length. Please confirm whether a 50d lap applies in the tension zone or whether mechanical couplers are required. Contractor proposes staggering laps at 50%. Response: use couplers at TS-3; 50d laps acceptable elsewhere."},
  {"id": "rfi-p2-0043", "projectId": "P2", "type": "RFI", "number": "RFI-0043", "discipline": "Structural", "createdAt": "2026-10-16T11:05:00Z",
   "title": "Sariya spacing at slab cut-out near lift core LC-2",
   "body": "Site team reports that sariya spacing of 150 c/c for bottom bars cannot be maintained around the 1.2 m x 1.5 m slab cut-out at lift core LC-2. Request trimmer bar details and confirmation that spacing may increase to 175 c/c locally."},
  {"id": "rfi-p2-0044", "projectId": "P2", "type": "RFI", "number": "RFI-0044", "discipline": "MEP", "createdAt": "2026-10-20T08:40:00Z",
   "title": "HVAC duct opening through PT beam PB-7",
   "body": "Supply ducts at grid C/4 need a 600 x 400 mm opening through post-tensioned beam PB-7. Request structural approval and tendon profile drawings before any core cutting."},
  {"id": "rfi-p2-0045", "projectId": "P2", "type": "RFI", "number": "RFI-0045", "discipline": "Civil", "createdAt": "2026-10-22T10:15:00Z",
   "title": "Concrete grade for PCC below raft",
   "body": "General notes call for M10 PCC below footings but the raft section on S-102 shows M15. Confirm the grade and thickness of PCC below the raft foundation."},
  {"id": "rfi-p2-0046", "projectId": "P2", "type": "RFI", "number": "RFI-0046", "discipline": "MEP", "createdAt": "2026-10-27T14:30:00Z",
   "title": "MEP coordination at level 2 corridor",
   "body": "Cable tray, sprinkler main and drainage line clash in the level 2 corridor between grids 3 and 5. Ceiling void is only 450 mm. Request a coordinated MEP section and confirmation of the false ceiling level."},
  {"id": "rfi-p1-0017", "projectId": "P1", "type": "RFI", "number": "RFI-0017", "discipline": "Structural", "createdAt": "2027-02-02T09:00:00Z",
   "title": "Rebar spacing in first floor slab at balcony cantilever B-4",
   "body": "Top reinforcement bar spacing of 125 c/c at balcony cantilever B-4 clashes with electrical conduit sleeves. Please advise whether bars may be bundled or spacing adjusted to 140 c/c with additional bars."},
  {"id": "rfi-p1-0018", "projectId": "P1", "type": "RFI", "number": "RFI-0018", "discipline": "Structural", "createdAt": "2027-02-25T16:10:00Z",
   "title": "Shuttering removal time for first floor slab",
   "body": "Please confirm the minimum striking time for soffit shuttering and props of the first floor slab (span 4.2 m, OPC 53 grade concrete). Site proposes removing props at 10 days."},
  {"id": "rfi-p1-0019", "projectId": "P1", "type": "RFI", "number": "RFI-0019", "discipline": "Architectural", "createdAt": "2027-03-04T12:00:00Z",
   "title": "GWB partition head detail at false ceiling",
   "body": "Drawing A-512 shows gypsum board partitions stopping at false ceiling level. Confirm whether partitions between apartments must run up to the slab soffit for acoustic separation, and provide the head detail."},
  {"id": "rfi-p1-0020", "projectId": "P1", "type": "RFI", "number": "RFI-0020", "discipline": "Architectural", "createdAt": "2027-03-08T10:45:00Z",
   "title": "Waterproofing membrane at podium planter boxes",
   "body": "This request for information concerns the planter boxes at podium level. The specification calls for a PVC membrane but the detail shows crystalline coating. Confirm the system and the root barrier requirement."},
  {"id": "rfi-p1-0021", "projectId": "P1", "type": "RFI", "number": "RFI-0021", "discipline": "Architectural", "createdAt": "2027-03-11T09:30:00Z",
   "title": "Cat ladder access to overhead water tank",
   "body": "The cat ladder to the overhead tank is drawn without an extension above the roof. Confirm that side rails extend 1 m above the landing and whether a safety cage is required."},
  {"id": "rfi-p1-0022", "projectId": "P1", "type": "RFI", "number": "RFI-0022", "discipline": "Structural", "createdAt": "2027-01-28T15:20:00Z",
   "title": "Reinforcing steel detailing at plinth beam junction",
   "body": "Reinforcing steel from plinth beams PB-2 and PB-5 congests the junction at column C-8. Request a revised bar bending schedule or permission to crank the bottom bars."},
  {"id": "dl-p1-0112", "projectId": "P1", "type": "DailyLog", "number": "DL-P1-0112", "discipline": "HSE", "createdAt": "2027-01-12T18:00:00Z",
   "title": "Daily log 12 Jan 2027",
   "body": "Excavation continued at grids A-D/1-4, about 180 m3 removed. Depth reached 3.2 m on the east side; sheet piling started next to the boundary wall. Two workers found without harness on scafolding near the lift pit; work stopped and a toolbox talk on PPE was held."},
  {"id": "dl-p1-0220", "projectId": "P1", "type": "DailyLog", "number": "DL-P1-0220", "discipline": "Structural", "createdAt": "2027-02-20T18:00:00Z",
   "title": "Daily log 20 Feb 2027",
   "body": "Centering for the ground floor slab is 70% complete. Rebar binding started in bays 1 to 3. Ground floor slab pour planned for 24 Feb, subject to inspection."},
  {"id": "dl-p1-0302", "projectId": "P1", "type": "DailyLog", "number": "DL-P1-0302", "discipline": "Structural", "createdAt": "2027-03-02T18:00:00Z",
   "title": "Daily log 2 Mar 2027",
   "body": "Ground floor slab concreting completed, 210 m3 of M25. Cubes cast. Curing by ponding. Props to remain until striking is approved under RFI-0018."},
  {"id": "dl-p2-1105", "projectId": "P2", "type": "DailyLog", "number": "DL-P2-1105", "discipline": "MEP", "createdAt": "2026-11-05T18:00:00Z",
   "title": "Daily log 5 Nov 2026",
   "body": "Electrical conduiting on admin block level 2: 420 rm laid. Plumbing sleeves fixed before the slab pour. Rain stopped work from 14:00 to 16:00."},
  {"id": "dl-p2-1106", "projectId": "P2", "type": "DailyLog", "number": "DL-P2-1106", "discipline": "HSE", "createdAt": "2026-11-06T18:00:00Z",
   "title": "Daily log 6 Nov 2026",
   "body": "Guardrail missing at the open edge of the level 3 slab; temporary barricade installed. Safety net along the north face pending material delivery. Personal fall arrest systems issued to 6 workers."},
  {"id": "dl-p2-1107", "projectId": "P2", "type": "DailyLog", "number": "DL-P2-1107", "discipline": "Structural", "createdAt": "2026-11-07T18:00:00Z",
   "title": "Daily log 7 Nov 2026",
   "body": "PT stressing of beams PB-5 to PB-8 completed and elongation records submitted. Duct opening in PB-7 on hold pending reply to RFI-0044."},
  {"id": "dl-p2-1108", "projectId": "P2", "type": "DailyLog", "number": "DL-P2-1108", "discipline": "Civil", "createdAt": "2026-11-08T18:00:00Z",
   "title": "Daily log 8 Nov 2026",
   "body": "Waterproofing of underground tank walls started with crystalline coating. Backfilling of the east side deferred until the coating cures."},
  {"id": "ncr-p1-0003", "projectId": "P1", "type": "NCR", "number": "NCR-0003", "discipline": "Structural", "createdAt": "2027-02-10T13:00:00Z",
   "title": "Honeycombing in column C-12, ground floor",
   "body": "Honeycombing observed after removing the shuttering at column C-12. Cover to rebar below 25 mm at two locations. Repair with polymer-modified mortar after engineer approval."},
  {"id": "ncr-p1-0004", "projectId": "P1", "type": "NCR", "number": "NCR-0004", "discipline": "Architectural", "createdAt": "2027-04-02T11:30:00Z",
   "title": "AAC blockwork out of plumb at stair core",
   "body": "Blockwork at the level 2 stair core is 18 mm out of plumb over 3 m height, beyond the 10 mm tolerance. Dismantle and rebuild the affected courses."},
  {"id": "ncr-p1-0005", "projectId": "P1", "type": "NCR", "number": "NCR-0005", "discipline": "Architectural", "createdAt": "2027-04-09T10:00:00Z",
   "title": "Drywall boards stored in the open",
   "body": "Drywall boards delivered for level 1 partitions were stored uncovered and show moisture damage. Reject the affected boards and store the remainder off the ground under cover."},
  {"id": "ncr-p2-0007", "projectId": "P2", "type": "NCR", "number": "NCR-0007", "discipline": "HSE", "createdAt": "2026-11-12T09:15:00Z",
   "title": "Fall protection plan not followed at roof level",
   "body": "Inspection found workers at the roof parapet without any fall protection. Contractor to submit a revised fall protection plan and retrain the crew before roof work resumes."},
  {"id": "ncr-p2-0008", "projectId": "P2", "type": "NCR", "number": "NCR-0008", "discipline": "HSE", "createdAt": "2026-11-14T15:40:00Z",
   "title": "Excavation without protective system at UG tank",
   "body": "Excavation for the underground water tank reached 2.1 m (about 7 feet) with vertical sides and no shoring, sloping or trench box. Work stopped; contractor to provide a protective system as per the approved method statement."},
  {"id": "ncr-p2-0009", "projectId": "P2", "type": "NCR", "number": "NCR-0009", "discipline": "Structural", "createdAt": "2026-11-18T12:25:00Z",
   "title": "Release agent on reinforcement at level 3 slab",
   "body": "Formwork release agent was sprayed after the reinforcement was placed, contaminating the bars at the level 3 slab. Clean the bars and re-inspect before concreting."}
]

with open("data/project-documents.json", "w", encoding="utf-8") as f:
    json.dump(project_docs, f, indent=2)

# 2. Synonyms
synonyms = [
  {
    "term": "rebar",
    "expandsTo": ["reinforcement bar", "reinforcing steel", "sariya"],
    "direction": "two-way"
  },
  {
    "term": "formwork",
    "expandsTo": ["shuttering", "centering"],
    "direction": "two-way"
  },
  {
    "term": "GWB",
    "expandsTo": ["gypsum wallboard", "gypsum board", "drywall"],
    "direction": "two-way"
  },
  {
    "term": "RFI",
    "expandsTo": ["request for information"],
    "direction": "two-way"
  },
  {
    "term": "PPE",
    "expandsTo": ["personal protective equipment"],
    "direction": "two-way"
  },
  {
    "term": "fall protection",
    "expandsTo": ["guardrail", "safety net", "personal fall arrest system"],
    "direction": "one-way"
  }
]

with open("data/synonyms.json", "w", encoding="utf-8") as f:
    json.dump(synonyms, f, indent=2)

# 3. OSHA Title 29 XML
# Structure: eCFR Title 29 Part 1926 XML with DIV6 SUBPART and DIV8 SECTION N="..."
osha_xml = """<?xml version="1.0" encoding="utf-8"?>
<ECFR>
  <DIV5 TYPE="PART" N="1926">
    <HEAD>PART 1926—SAFETY AND HEALTH REGULATIONS FOR CONSTRUCTION</HEAD>
    
    <!-- SUBPART L: Scaffolds -->
    <DIV6 TYPE="SUBPART" N="L">
      <HEAD>Subpart L—Scaffolds</HEAD>
      
      <DIV8 TYPE="SECTION" N="1926.450">
        <HEAD>§ 1926.450 Scope, application and definitions applicable to this subpart.</HEAD>
        <P>(a) Scope and application. This subpart applies to all scaffolds used in workplaces covered by this part. It does not apply to crane or derrick suspended personnel platforms.</P>
        <P>(b) Definitions. Body belt, body harness, cleat, competent person, guardrail system, personal fall arrest system, rated load, scaffold.</P>
      </DIV8>
      
      <DIV8 TYPE="SECTION" N="1926.451">
        <HEAD>§ 1926.451 General requirements.</HEAD>
        <P>(a) Capacity. (1) Except as provided in paragraphs (a)(2), (a)(3), (a)(4), (a)(5) and (g) of this section, each scaffold and scaffold component shall be capable of supporting, without failure, its own weight and at least 4 times the maximum intended load applied or transmitted to it.</P>
        <P>(b) Scaffold platform construction. (1) Each platform on all working levels of scaffolds shall be fully planked or decked between the front uprights and the guardrail supports.</P>
        <P>(e) Access. (1) When scaffold platforms are more than 2 feet (0.6 m) above or below a point of access, portable ladders, hook-on ladders, attachable ladders, stair towers, stairway-type ladders, ramps, walkways, or built-in ladders shall be used to provide access.</P>
        <P>(g) Fall protection. (1) Each employee on a scaffold more than 10 feet (3.1 m) above a lower level shall be protected from falling to that lower level. Paragraphs (g)(1)(i) through (vii) of this section establish the types of fall protection to be provided to the employees on each type of scaffold.</P>
        <P>(g)(1)(i) Each employee on a boatswains' chair, catenary scaffold, float scaffold, needle beam scaffold, or ladder jack scaffold shall be protected by a personal fall arrest system;</P>
        <P>(g)(1)(iv) Each employee on a self-contained adjustable scaffold shall be protected by a guardrail system (with minimum 200 pound toprail capacity) when the platform is supported by the frame structure, and by both a personal fall arrest system and a guardrail system when the platform is supported by ropes.</P>
        <P>(g)(4) Guardrail systems installed to meet the requirements of this section shall comply with the following provisions: (i) Guardrail systems shall be installed along all open sides and ends of platforms. Guardrail systems shall be installed before the scaffold is released for use by employees other than erection/dismantling crews.</P>
      </DIV8>
      
      <DIV8 TYPE="SECTION" N="1926.452">
        <HEAD>§ 1926.452 Additional requirements applicable to specific types of scaffolds.</HEAD>
        <P>(a) Pole scaffolds. (b) Tube and coupler scaffolds. (c) Fabricated frame scaffolds. (w) Mobile scaffolds. Scaffolds shall be braced by cross, horizontal, or diagonal braces.</P>
      </DIV8>

      <DIV8 TYPE="SECTION" N="1926.454">
        <HEAD>§ 1926.454 Training requirements.</HEAD>
        <P>This section supplements and clarifies the requirements of § 1926.21 regarding the hazards of working on scaffolds. The employer shall have each employee who performs work while on a scaffold trained by a person qualified in the subject matter to recognize the hazards associated with the type of scaffold being used.</P>
      </DIV8>
    </DIV6>

    <!-- SUBPART M: Fall Protection -->
    <DIV6 TYPE="SUBPART" N="M">
      <HEAD>Subpart M—Fall Protection</HEAD>
      
      <DIV8 TYPE="SECTION" N="1926.500">
        <HEAD>§ 1926.500 Scope, application, and definitions applicable to this subpart.</HEAD>
        <P>(a) Scope and application. (1) This subpart sets forth requirements and specifications for fall protection systems and equipment in construction workplaces.</P>
        <P>(b) Definitions. Body harness, deceleration device, guardrail system, hole, leading edge, personal fall arrest system, positioning device system, safety net system, warning line system.</P>
      </DIV8>

      <DIV8 TYPE="SECTION" N="1926.501">
        <HEAD>§ 1926.501 Duty to have fall protection.</HEAD>
        <P>(a) General. (1) This section sets forth requirements for employers to provide fall protection systems. All fall protection required by this part shall conform to the criteria set forth in § 1926.502 of this subpart.</P>
        <P>(b)(1) Unprotected sides and edges. Each employee on a walking/working surface (horizontal and vertical surface) with an unprotected side or edge which is 6 feet (1.8 m) or more above a lower level shall be protected from falling by the use of guardrail systems, safety net systems, or personal fall arrest systems.</P>
        <P>(b)(2) Leading edges. Each employee who is constructing a leading edge 6 feet (1.8 m) or more above lower levels shall be protected by guardrail systems, safety net systems, or personal fall arrest systems.</P>
      </DIV8>

      <DIV8 TYPE="SECTION" N="1926.502">
        <HEAD>§ 1926.502 Fall protection systems criteria and practices.</HEAD>
        <P>(a) General. (1) Fall protection systems required by this part shall comply with the applicable provisions of this section.</P>
        <P>(b) Guardrail systems. Guardrail systems and their use shall comply with the following provisions: (1) Top edge height of top rails, or equivalent guardrail system members, shall be 42 inches (1.1 m) plus or minus 3 inches (8 cm) above the walking/working level.</P>
        <P>(c) Safety net systems. Safety net systems and their use shall comply with the following provisions: (1) Safety nets shall be installed as close under the walking/working surface on which employees are working as being practical, but in no case more than 30 feet (9.1 m) below such level.</P>
        <P>(d) Personal fall arrest systems. Personal fall arrest systems and their use shall comply with the provisions set forth below: deep anchorages, lifelines, body harness, and deceleration devices.</P>
      </DIV8>
    </DIV6>

    <!-- SUBPART P: Excavations -->
    <DIV6 TYPE="SUBPART" N="P">
      <HEAD>Subpart P—Excavations</HEAD>
      
      <DIV8 TYPE="SECTION" N="1926.650">
        <HEAD>§ 1926.650 Scope, application, and definitions applicable to this subpart.</HEAD>
        <P>(a) Scope and application. This subpart applies to all open excavations made in the earth's surface. Excavations are defined to include trenches.</P>
        <P>(b) Definitions. Accepted engineering practices, benching, cave-in, competent person, excavation, protective system, shoring, sloping, trench.</P>
      </DIV8>

      <DIV8 TYPE="SECTION" N="1926.651">
        <HEAD>§ 1926.651 Specific excavation requirements.</HEAD>
        <P>(a) Surface encumbrances. All surface encumbrances that are located so as to create a hazard to employees shall be removed or supported, as necessary, to safeguard employees.</P>
        <P>(b) Underground installations. (1) The estimated location of utility installations, such as sewer, telephone, fuel, electric, water lines, shall be determined prior to opening an excavation.</P>
        <P>(c) Access and egress. (2) Means of egress from trench excavations. A stairway, ladder, ramp or other safe means of egress shall be located in trench excavations that are 4 feet (1.22 m) or more in depth so as to require no more than 25 feet (7.62 m) of lateral travel for employees.</P>
      </DIV8>

      <DIV8 TYPE="SECTION" N="1926.652">
        <HEAD>§ 1926.652 Requirements for protective systems.</HEAD>
        <P>(a) Protection of employees in excavations. (1) Each employee in an excavation shall be protected from cave-ins by an adequate protective system designed in accordance with paragraph (b) or (c) of this section except when: (i) Excavations are made entirely in stable rock; or (ii) Excavations are less than 5 feet (1.52 m) in depth and examination of the ground by a competent person provides no indication of a potential cave-in.</P>
        <P>(2) Protective systems shall have the capacity to resist without failure all loads that are intended or could reasonably be expected to be applied or transmitted to the system.</P>
        <P>(b) Design of sloping and benching systems. The slopes and configurations of sloping and benching systems shall be selected and constructed by the employer in accordance with the requirements of paragraph (b)(1); or paragraph (b)(2); or paragraph (b)(3); or paragraph (b)(4).</P>
        <P>(c) Design of support systems, shield systems, and other protective systems. Designs shall be selected and constructed by the employer in accordance with the requirements of paragraph (c)(1); or (c)(2); or (c)(3); or (c)(4).</P>
      </DIV8>
    </DIV6>

    <!-- SUBPART X: Stairways and Ladders -->
    <DIV6 TYPE="SUBPART" N="X">
      <HEAD>Subpart X—Stairways and Ladders</HEAD>
      
      <DIV8 TYPE="SECTION" N="1926.1050">
        <HEAD>§ 1926.1050 Scope, application, and definitions applicable to this subpart.</HEAD>
        <P>(a) Scope and application. This subpart applies to all stairways and ladders used in construction, alteration, repair, painting and decorating, and demolition workplaces.</P>
        <P>(b) Definitions. Cleat, double-cleat ladder, failure, fixed ladder, individual-rung ladder, ladder stand, lower levels, point of access, portable ladder, single-cleat ladder, stairway, step stool.</P>
      </DIV8>

      <DIV8 TYPE="SECTION" N="1926.1051">
        <HEAD>§ 1926.1051 General requirements.</HEAD>
        <P>(a) A stairway or ladder shall be provided at all personnel points of access where there is a break in elevation of 19 inches (48 cm) or more, and no ramp, runway, sloped embankment, or personnel hoist is provided.</P>
      </DIV8>

      <DIV8 TYPE="SECTION" N="1926.1052">
        <HEAD>§ 1926.1052 Stairways.</HEAD>
        <P>(a) General. (1) Stairways that will not be a permanent part of the structure on which construction work is being performed shall have landings of not less than 30 inches (76 cm) in the direction of travel and extend at least 22 inches (56 cm) in width at every 12 feet (3.7 m) or less of vertical rise.</P>
        <P>(b) Temporary stairs. (c) Stairrails and handrails. (1) Stairways having four or more risers or rising more than 30 inches (76 cm), whichever is less, shall be equipped with at least one handrail and one stairrail system along each unprotected side or edge.</P>
      </DIV8>

      <DIV8 TYPE="SECTION" N="1926.1053">
        <HEAD>§ 1926.1053 Ladders.</HEAD>
        <P>(a) General requirements. (1) Ladders shall be capable of supporting the following loads without failure: each self-supporting portable ladder at least four times the maximum intended load.</P>
        <P>(b) Use. The following requirements apply to the use of all ladders, including job-made ladders, except as otherwise indicated:</P>
        <P>(b)(1) When portable ladders are used for access to an upper landing surface, the ladder side rails shall extend at least 3 feet (.9 m) above the upper landing surface to which the ladder is used to gain access; or, when such an extension is not possible because of the ladder's length, then the ladder shall be secured at its top to a rigid support that will not deflect, and a grasping device, such as a grabrail, shall be provided to assist employees in mounting and dismounting the ladder. In no case shall the extension be such that ladder deflection under load would, by itself, cause the ladder to slip off its support.</P>
        <P>(b)(2) Ladders shall be maintained free of oil, grease, and other slipping hazards.</P>
        <P>(b)(4) Ladders shall be used only for the purpose for which they were designed.</P>
      </DIV8>
    </DIV6>
  </DIV5>
</ECFR>
"""

with open("data/osha-title-29.xml", "w", encoding="utf-8") as f:
    f.write(osha_xml.strip())

# 4. Relevance test cases JSON file (B4)
relevance_cases = [
  {"query": "excavation 5 feet protective system", "asUser": "guest", "expected": {"type": "in_top_n", "docNumber": "1926.652", "n": 5}},
  {"query": "ladder extend 3 feet above landing", "asUser": "guest", "expected": {"type": "in_top_n", "docNumber": "1926.1053", "n": 5}},
  {"query": "scafold guardrail", "asUser": "guest", "expected": {"type": "in_top_n", "docNumber": "1926.451", "n": 5, "note": "typo tolerance"}},
  {"query": "sariya spacing slab", "asUser": "ravi", "expected": {"type": "is_first", "docNumber": "RFI-0043"}},
  {"query": "sariya spacing slab", "asUser": "asha", "expected": {"type": "is_first", "docNumber": "RFI-0017", "note": "synonym: rebar"}},
  {"query": "shuttering removal time", "asUser": "asha", "expected": {"type": "is_first", "docNumber": "RFI-0018"}},
  {"query": "drywall partition acoustic", "asUser": "asha", "expected": {"type": "in_top_n", "docNumber": "RFI-0019", "n": 3, "note": "synonym: GWB"}},
  {"query": "RFI-0042", "asUser": "ravi", "expected": {"type": "is_first", "docNumber": "RFI-0042"}},
  {"query": "RFI-0042", "asUser": "asha", "expected": {"type": "not_returned_and_facet_only", "docNumber": "RFI-0042", "expectedProjects": ["P1"]}},
  {"query": "RFI-0044", "asUser": "ravi", "expected": {"type": "ranks_above", "higherDocNumber": "RFI-0044", "lowerDocNumber": "DL-P2-1107"}},
  {"query": "fall protection", "asUser": "ravi", "expected": {"type": "both_returned", "docNumbers": ["NCR-0007", "DL-P2-1106"]}},
  {"query": "guardrail", "asUser": "ravi", "expected": {"type": "returned_and_not_returned", "returnedDocNumber": "DL-P2-1106", "notReturnedDocNumber": "NCR-0007"}}
]

with open("relevance-cases.json", "w", encoding="utf-8") as f:
    json.dump(relevance_cases, f, indent=2)

print("Data files generated successfully!")
