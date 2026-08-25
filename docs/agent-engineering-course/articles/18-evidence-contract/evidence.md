# Article 18 Evidence｜Evidence Contract

## Evidence Gate

- Gate: PASS_RECOMMENDED / MASTER_VALIDATION_PENDING
- Required Lab: NONE
- Experiment count: 0
- Observed result: ABSENT
- BuildPilot: DESIGN / NOT IMPLEMENTED / NOT RUN
- Core Claim coverage: 10 / 10
- Core `BLOCKED`: 0
- Evidence Cards: 8

The qualitative confidence labels below are uncalibrated research-review labels:

- `HIGH`: direct support from a fixed primary source or repository authority within the stated scope.
- `MEDIUM`: multiple precedents support only part of the wording, or a course design inference remains.
- `N/A_DESIGN`: implementation confidence is not asserted because the item is a Proposal and has not been run.

Acceptance is separate from confidence. `ACCEPT_FOR_SCOPED_COURSE_USE` permits only the wording recorded here; `ACCEPT_AS_DESIGN_PROPOSAL` requires Proposal language; neither is production approval.

## Claim register

| Claim ID | Kind | Auditable statement | Evidence Status | Evidence Cards | Scope and limitations | Confidence posture | Acceptance posture |
|---|---|---|---|---|---|---|---|
| 18-C01 | Inference | Fluency, parse success or schema validity alone does not establish evidentiary support or policy acceptance for a Claim. | CONFIRMED | 18-E01, E03, E04, E08 | Proves a separation of checks, not that natural-language Claims are false. | HIGH | ACCEPT_FOR_SCOPED_COURSE_USE |
| 18-C02 | Proposal | Claim, Evidence, Observation, Inference, Proposal and Unknown use the six course definitions recorded in research.md. | PROPOSAL | 18-E02, E08 | Course vocabulary; not a universal ontology. | N/A_DESIGN | ACCEPT_AS_DESIGN_PROPOSAL |
| 18-C03 | Proposal | A minimum course Evidence Record contains identity, Claim, evidence links, interpretation boundaries, status, acceptance and lifecycle groups. | PROPOSAL | 18-E02, E03, E04, E07 | Field grouping is not prescribed by one standard and is not an implemented schema. | N/A_DESIGN | ACCEPT_AS_DESIGN_PROPOSAL |
| 18-C04 | Inference | Source identity, version/time and scope are directly motivated by primary standards; limitations and falsifier are necessary course extensions for fail-closed acceptance. | PARTIAL | 18-E02, E03, E04, E07 | Standards do not uniformly mandate the exact five fields or names. | MEDIUM | ACCEPT_WITH_WORDING_CEILING |
| 18-C05 | Inference | Citation, Provenance, qualitative Confidence and policy Acceptance answer different audit questions and must not be collapsed. | PARTIAL | 18-E02, E04, E08 | Provenance and acceptance separation is sourced; the confidence scheme is a course Proposal. | MEDIUM | ACCEPT_WITH_WORDING_CEILING |
| 18-C06 | Proposal | Conflicting, stale or partial Evidence is retained, scoped and re-reviewed; unresolved conflict becomes BLOCKED/Unknown rather than silent overwrite. | PROPOSAL | 18-E02, E04, E08 | Proposed policy; no Evidence Store or organization-wide rule is implemented. | N/A_DESIGN | ACCEPT_AS_DESIGN_PROPOSAL |
| 18-C07 | Proposal | After Parse/Schema, the course applies semantic gates for identity, integrity, provenance, applicability, support mapping, counter-evidence, limitations/falsifier and policy decision. | PROPOSAL | 18-E01, E03, E04, E08 | Gate order and fail-closed behavior are course design, not a standard-mandated pipeline. | N/A_DESIGN | ACCEPT_AS_DESIGN_PROPOSAL |
| 18-C08 | Proposal | Evidence lifecycle uses append-only revisions, supersession, invalidation and policy-bound review events; current state is a projection. | PROPOSAL | 18-E02, E04, E07 | Standards provide relation/audit precedents, not this complete state machine. | N/A_DESIGN | ACCEPT_AS_DESIGN_PROPOSAL |
| 18-C09 | Proposal | BuildPilot should emit a scoped diagnostic evidence package rather than only a root-cause sentence. | PROPOSAL | 18-E08 | DESIGN / NOT IMPLEMENTED / NOT RUN; no runtime or benefit claim. | N/A_DESIGN | ACCEPT_AS_DESIGN_PROPOSAL |
| 18-C10 | Observation | The canonical course assigns Evidence Contract separately from Structured Output, Trace/Replay, Failure Taxonomy and Eval. | CONFIRMED | 18-E01, E05, E06, E08 | Confirms course ownership boundaries, not universal industry naming. | HIGH | ACCEPT_FOR_SCOPED_COURSE_USE |

## Evidence Cards

### Evidence 18-E01｜Schema validity is a structural check

- Article: `18｜Evidence Contract`
- Claim ID: `18-C01`, `18-C07`, `18-C10`
- Claim: JSON Schema validation constrains instance data; it does not by itself establish source provenance, factual support or acceptance policy.
- Evidence Status: `CONFIRMED`
- Evidence Class: `OFFICIAL_DOC`
- Source Type: specification
- Source: [JSON Schema Core, Draft 2020-12](https://json-schema.org/draft/2020-12/json-schema-core); [JSON Schema Validation, Draft 2020-12](https://json-schema.org/draft/2020-12/json-schema-validation)
- Repository: N/A
- Commit: N/A; fixed draft version `2020-12`
- File: N/A
- Symbol: Validation §3, Overview
- Call Path: N/A
- Experiment: N/A
- Fixture: N/A
- Trace: N/A
- Retrieved / Run At: 2026-08-25 Asia/Shanghai
- Version Scope: JSON Schema Draft 2020-12
- Reproduction: Open Validation §3 and inspect what validation asserts about instance data; search the specification for provenance, counter-evidence and policy acceptance requirements.
- Observation: The Validation specification defines assertions as constraints on instance structure/content and validity as satisfaction of asserted constraints.
- Counter-evidence Searched: Looked for language making schema validity sufficient to establish a proposition's real-world truth, source authority or policy approval; none is part of the validation semantics.
- Interpretation: Parse/schema checks are necessary for machine contracts but address a different question from semantic evidence acceptance.
- Proves: Structured validity and evidentiary acceptance are separable checks.
- Does Not Prove: That every valid object is false; that the proposed Article 18 semantic-gate order is standardized.
- Limitations: JSON Schema can express application annotations or custom vocabularies, so an application may encode evidence fields; their presence still does not independently verify their contents.
- Falsifier: A normative Draft 2020-12 provision stating that schema validity alone establishes provenance, real-world truth and policy acceptance would defeat the scoped Claim.
- Confidence Posture: HIGH, fixed primary specification and narrow wording.
- Acceptance Posture: ACCEPT_FOR_SCOPED_COURSE_USE.
- Course Usage: Establish the Structured Output/Evidence Contract boundary and semantic Gate 0.
- BuildPilot Implication: `SIMPLIFY` — validate diagnostic package shape, then run separate semantic gates.
- Owner: RESEARCHER
- Verified At: 2026-08-25

### Evidence 18-E02｜Provenance is an input to assessment

- Article: `18｜Evidence Contract`
- Claim ID: `18-C02`, `18-C03`, `18-C04`, `18-C05`, `18-C06`, `18-C08`
- Claim: Provenance can represent entities, activities, agents, generation, derivation, revision, invalidation and bundles, while remaining distinct from the downstream trust/acceptance judgment.
- Evidence Status: `CONFIRMED`
- Evidence Class: `OFFICIAL_DOC`
- Source Type: W3C Recommendation
- Source: [W3C PROV-DM, 30 April 2013](https://www.w3.org/TR/2013/REC-prov-dm-20130430/)
- Repository: N/A
- Commit: N/A; fixed Recommendation date
- File: N/A
- Symbol: §§2–3; §5.1.8 Invalidation; §5.2.2 Revision; §5.2.4 Primary Source; §5.4.1 Bundle
- Call Path: N/A
- Experiment: N/A
- Fixture: N/A
- Trace: N/A
- Retrieved / Run At: 2026-08-25 Asia/Shanghai
- Version Scope: W3C Recommendation 2013-04-30
- Reproduction: Open the fixed Recommendation; inspect the conceptual model and the named relation sections.
- Observation: PROV-DM models origin and derivation through entity/activity/agent relations; it represents invalidation and revision; provenance bundles can themselves have provenance. Its introduction describes provenance as information usable for assessments rather than as an automatic trust decision.
- Counter-evidence Searched: Checked whether PROV-DM specifies a universal evidence schema, confidence scale or acceptance policy, and whether primary-source status is absolute. It does not; primary source is relational and domain-dependent.
- Interpretation: Article 18 can reuse explicit provenance and lifecycle relations, but must separately declare Claim semantics and acceptance policy.
- Proves: Provenance, lifecycle relations and acceptance are distinct concerns; preserving source identity and derivation aids audit.
- Does Not Prove: That PROV-DM mandates Article 18's exact fields, states, falsifier or conflict policy.
- Limitations: Domain-agnostic provenance model from 2013; it is not an agent Evidence Contract and does not assess source truth.
- Falsifier: A normative PROV-DM rule that a provenance record automatically approves all represented Claims would defeat the separation claim.
- Confidence Posture: HIGH for provenance concepts; MEDIUM where used as precedent for the course state model.
- Acceptance Posture: ACCEPT_FOR_SCOPED_COURSE_USE; course extensions remain Proposal.
- Course Usage: Define Provenance, identity/derivation and lifecycle precedent; impose a wording ceiling on C03–C06/C08.
- BuildPilot Implication: `ADOPT` — record source/derivation and revision relations, without claiming provenance equals truth.
- Owner: RESEARCHER
- Verified At: 2026-08-25

### Evidence 18-E03｜Attestation binds predicate to identified subject

- Article: `18｜Evidence Contract`
- Claim ID: `18-C01`, `18-C03`, `18-C04`, `18-C07`
- Claim: A typed statement and resource descriptor identify what an assertion is about; content identity and locators are explicit and do not replace semantic verification.
- Evidence Status: `CONFIRMED`
- Evidence Class: `PINNED_SOURCE`
- Source Type: pinned specification
- Source: [in-toto Attestation Framework v1.0 Statement](https://github.com/in-toto/attestation/blob/v1.0/spec/v1.0/statement.md); [v1.0 Resource Descriptor](https://github.com/in-toto/attestation/blob/v1.0/spec/v1.0/resource_descriptor.md)
- Repository: `in-toto/attestation`
- Commit: tag `v1.0`
- File: `spec/v1.0/statement.md`; `spec/v1.0/resource_descriptor.md`
- Symbol: Statement layer schema; Resource Descriptor Schema, Fields and Parsing rules
- Call Path: N/A
- Experiment: N/A
- Fixture: N/A
- Trace: N/A
- Retrieved / Run At: 2026-08-25 Asia/Shanghai
- Version Scope: Attestation Framework v1.0
- Reproduction: Open the tag-pinned files; inspect `_type`, `subject`, `predicateType`, `predicate` and resource `digest/uri/content` fields.
- Observation: The Statement model binds a predicate type and predicate to identified subjects. Resource descriptors distinguish name/URI from digest/content and permit digest-based immutable resource identity.
- Counter-evidence Searched: Checked whether a syntactically valid Statement or URI alone is defined as sufficient proof of predicate truth; the framework separates statement shape and resource identity from verification policy.
- Interpretation: Evidence records need an unambiguous Claim/subject and resolvable or content-addressed evidence references before semantic support can be evaluated.
- Proves: Subject identity, predicate typing, digest and locator are separate audit dimensions.
- Does Not Prove: That every Evidence item needs every Resource Descriptor field; that an attestation is truthful merely because it is well-formed.
- Limitations: Supply-chain attestation specification; mapping to general agent Claims is an analogy and course design choice.
- Falsifier: A v1.0 normative rule defining URI presence or schema validity alone as proof of predicate truth would defeat the scoped interpretation.
- Confidence Posture: HIGH for field semantics; MEDIUM for generalization to the course schema.
- Acceptance Posture: ACCEPT_FOR_SCOPED_COURSE_USE; mapping remains Proposal.
- Course Usage: Support subject/source identity, locator/digest separation and pre-acceptance checks.
- BuildPilot Implication: `SIMPLIFY` — use a small source descriptor with locator plus digest when content can be fixed.
- Owner: RESEARCHER
- Verified At: 2026-08-25

### Evidence 18-E04｜Verification is policy-bound and recorded separately

- Article: `18｜Evidence Contract`
- Claim ID: `18-C01`, `18-C03`, `18-C04`, `18-C05`, `18-C06`, `18-C07`, `18-C08`
- Claim: Artifact verification checks subject identity, trust and expectations; a VSA separately records verifier, policy, time and decision.
- Evidence Status: `CONFIRMED`
- Evidence Class: `OFFICIAL_DOC`
- Source Type: versioned specification
- Source: [SLSA v1.2 Provenance](https://slsa.dev/spec/v1.2/provenance); [Verifying Artifacts](https://slsa.dev/spec/v1.2/verifying-artifacts); [Verification Summary Attestation](https://slsa.dev/spec/v1.2/verification_summary)
- Repository: N/A
- Commit: N/A; published version `v1.2`
- File: N/A
- Symbol: Verifying Artifacts — subject digest, roots of trust and expectations; VSA — `subject`, `verifier`, `timeVerified`, `resourceUri`, `policy`, `inputAttestations`, `verificationResult`
- Call Path: N/A
- Experiment: N/A
- Fixture: N/A
- Trace: N/A
- Retrieved / Run At: 2026-08-25 Asia/Shanghai
- Version Scope: SLSA Specification v1.2
- Reproduction: Open the v1.2 pages; follow artifact verification steps and compare Provenance inputs with the VSA result fields.
- Observation: Verification includes matching an artifact to the attestation subject and applying trusted roots/expectations. VSA records the verifier identity/version, verification time, policy URI/digest, input attestations and PASSED/FAILED result.
- Counter-evidence Searched: Checked whether provenance presence or a high SLSA build level eliminates reliance on platform trust or policy inspection; the specification retains these limits.
- Interpretation: Citation/provenance inputs and policy acceptance outputs must remain separate records with explicit version/time.
- Proves: Policy, verifier, inputs, subject and decision are independent audit fields in a mature attestation workflow.
- Does Not Prove: That SLSA's binary result or field names should be copied unchanged into agent reasoning; that signed provenance proves arbitrary factual Claims.
- Limitations: Software supply-chain context; confidence semantics, conflict resolution and Evidence lifecycle remain course proposals.
- Falsifier: A v1.2 rule making all attestations accepted without subject matching, trust or policy expectations would defeat the scoped Claim.
- Confidence Posture: HIGH for verification/acceptance separation; MEDIUM for agent-domain transfer.
- Acceptance Posture: ACCEPT_FOR_SCOPED_COURSE_USE with explicit analogy boundary.
- Course Usage: Anchor Acceptance and semantic gate inputs; motivate review-event records.
- BuildPilot Implication: `ADOPT` — attach policy identity/version and reviewer decision to each diagnostic Claim.
- Owner: RESEARCHER
- Verified At: 2026-08-25

### Evidence 18-E05｜Trace records execution context, not Claim acceptance

- Article: `18｜Evidence Contract`
- Claim ID: `18-C10`
- Claim: OpenTelemetry spans record operations and correlated execution data; the Trace API does not define Evidence support, falsifiers or Claim acceptance policy.
- Evidence Status: `CONFIRMED`
- Evidence Class: `OFFICIAL_DOC`
- Source Type: moving official specification
- Source: [OpenTelemetry Specification 1.60.0](https://opentelemetry.io/docs/specs/otel/); [Trace API](https://opentelemetry.io/docs/specs/otel/trace/api/)
- Repository: N/A
- Commit: N/A; rendered specification version `1.60.0` at retrieval
- File: N/A
- Symbol: Trace API — Span, SpanContext, Links, Events, Status
- Call Path: N/A
- Experiment: N/A
- Fixture: N/A
- Trace: N/A
- Retrieved / Run At: 2026-08-25 Asia/Shanghai
- Version Scope: OpenTelemetry Specification 1.60.0 snapshot
- Reproduction: Inspect the Trace API data model and search for Claim, falsifier, counter-evidence and acceptance policy semantics.
- Observation: A Span represents an operation and can carry parent context, timing, attributes, links, timestamped events and status.
- Counter-evidence Searched: Looked for normative Trace API fields that decide whether an external engineering Claim is supported or accepted; none are part of the Trace API contract.
- Interpretation: A trace/span can be cited as an Evidence source, while Article 18 still needs an Evidence Record and policy decision.
- Proves: Execution correlation and Evidence acceptance are separate course concerns.
- Does Not Prove: That traces are complete, truthful, replayable or sufficient to reconstruct every failure; those claims belong to Article 21 evidence.
- Limitations: Moving source; exact version may drift after access date. Only the stable conceptual boundary is used.
- Falsifier: A later/source-specific Evidence semantic layer does not falsify this Trace API claim; a Trace API rule making Span status a universal external-Claim acceptance decision would.
- Confidence Posture: HIGH for the 1.60.0 API scope.
- Acceptance Posture: ACCEPT_FOR_SCOPED_COURSE_USE.
- Course Usage: Boundary box for Trace/Replay and warning that trace presence is not truth.
- BuildPilot Implication: `DEFER` — future runtime traces may be Evidence inputs, but no BuildPilot Trace exists here.
- Owner: RESEARCHER
- Verified At: 2026-08-25

### Evidence 18-E06｜Eval is a repeatable measurement process

- Article: `18｜Evidence Contract`
- Claim ID: `18-C10`
- Claim: Evaluation/TEVV concerns documented and repeatable measurement over defined contexts; it is broader than accepting one scoped Claim.
- Evidence Status: `PARTIAL`
- Evidence Class: `OFFICIAL_DOC`
- Source Type: government framework
- Source: [NIST AI Risk Management Framework 1.0](https://www.nist.gov/publications/artificial-intelligence-risk-management-framework-ai-rmf-10); publication NIST AI 100-1
- Repository: N/A
- Commit: N/A; version `1.0`, published 2023-01-26
- File: `NIST.AI.100-1.pdf`
- Symbol: §5.3 MEASURE; locate terms `test, evaluation, verification, and validation (TEVV)`, `repeatable`, `documented`
- Call Path: N/A
- Experiment: N/A
- Fixture: N/A
- Trace: N/A
- Retrieved / Run At: 2026-08-25 Asia/Shanghai
- Version Scope: NIST AI RMF 1.0
- Reproduction: Open the official publication and inspect the MEASURE function's TEVV and documentation/repeatability expectations.
- Observation: AI RMF treats measurement and TEVV as contextual, documented processes used to assess risks and trustworthy characteristics.
- Counter-evidence Searched: Checked whether AI RMF defines the exact Article 22 dataset/grader/regression architecture or Article 18 Evidence Record; it does not.
- Interpretation: The source supports keeping systematic evaluation separate from a single Claim's evidence decision, but course-specific Eval components come from the canonical plan.
- Proves: Evaluation has process/context scope beyond a lone assertion.
- Does Not Prove: The detailed future Article 22 design, any BuildPilot metric, accuracy, regression or production quality.
- Limitations: High-level voluntary framework; it supplies framing, not an implementation schema.
- Falsifier: A framework definition equating all TEVV with acceptance of one unmeasured Claim would defeat the scoped distinction.
- Confidence Posture: MEDIUM due to high-level source and course-specific boundary.
- Acceptance Posture: ACCEPT_WITH_WORDING_CEILING.
- Course Usage: One bounded paragraph distinguishing Evidence acceptance from Eval.
- BuildPilot Implication: `DEFER` — BuildPilot evaluation belongs to Article 22; no measurement is run here.
- Owner: RESEARCHER
- Verified At: 2026-08-25

### Evidence 18-E07｜Audit content, time, protection and retention are separate controls

- Article: `18｜Evidence Contract`
- Claim ID: `18-C03`, `18-C04`, `18-C08`
- Claim: Established audit controls separately address event content, timestamps, protection and retention, supporting explicit metadata and preserved history as design concerns.
- Evidence Status: `PARTIAL`
- Evidence Class: `OFFICIAL_DOC`
- Source Type: government control catalog
- Source: [NIST SP 800-53 Rev. 5.1 OSCAL-derived PDF](https://csrc.nist.gov/CSRC/media/Projects/risk-management/800-53%20Downloads/800-53r5/SP_800-53_v5_1-derived-OSCAL.pdf)
- Repository: N/A
- Commit: N/A; Revision 5.1
- File: `SP_800-53_v5_1-derived-OSCAL.pdf`
- Symbol: AU-3 Content of Audit Records; AU-8 Time Stamps; AU-9 Protection of Audit Information; AU-11 Audit Record Retention
- Call Path: N/A
- Experiment: N/A
- Fixture: N/A
- Trace: N/A
- Retrieved / Run At: 2026-08-25 Asia/Shanghai
- Version Scope: NIST SP 800-53 Revision 5.1
- Reproduction: Open the official derived control catalog and locate AU-3, AU-8, AU-9 and AU-11.
- Observation: The catalog treats record content, time, protection and retention as distinct audit controls.
- Counter-evidence Searched: Checked whether the AU family prescribes Claim/Evidence/Inference fields or an append-only agent Evidence state machine; it does not.
- Interpretation: Article 18 should make identity/time/history explicit, but its exact record and lifecycle remain a course Proposal.
- Proves: Auditability is not satisfied by keeping an undifferentiated conclusion string alone.
- Does Not Prove: That every system must implement NIST controls, immutable storage, cryptographic protection or this course schema.
- Limitations: Security/privacy control catalog, not an agent-evidence ontology; selection and tailoring are organization-specific.
- Falsifier: Evidence that the cited controls collapse content, timestamp and retention into no-record operation would defeat this narrow precedent.
- Confidence Posture: MEDIUM; direct control facts, analogical course use.
- Acceptance Posture: ACCEPT_WITH_WORDING_CEILING.
- Course Usage: Motivate explicit event metadata and durable review history without presenting a compliance claim.
- BuildPilot Implication: `SIMPLIFY` — retain review metadata in the design; no compliance assertion.
- Owner: RESEARCHER
- Verified At: 2026-08-25

### Evidence 18-E08｜Canonical course ownership and BuildPilot boundary

- Article: `18｜Evidence Contract`
- Claim ID: `18-C01` through `18-C10`
- Claim: The repository's canonical course assigns syntax to Article 03, execution receipts/trace foundations to Article 06, Evidence Contract to Article 18, Trace/Replay/Failure Taxonomy to Article 21 and Eval to Article 22; Article 18's BuildPilot case is design-only.
- Evidence Status: `CONFIRMED`
- Evidence Class: `PINNED_SOURCE`
- Source Type: repository canonical plan, glossary, published lessons and approved Article card
- Source: `docs/agent-engineering-series-plan.md`; `docs/agent-engineering-course/glossary.md`; published Articles 03, 06, 12–17; `docs/agent-engineering-course/articles/18-evidence-contract/article-card.md`
- Repository: `TechStackShow`
- Commit: `272ff0e24450ead78ff959dd019da202593a518d` for committed corpus; Article 18 card is current master-owned workspace input
- File: canonical and published paths listed above
- Symbol: Article rows/ownership tables; Article 03 semantic-boundary section; Article 06 Result-vs-Evidence/Trace boundary; Articles 12–17 Evidence Boundary sections; Article 18 Human-approved questions/Non-goals
- Call Path: N/A
- Experiment: N/A
- Fixture: Article 18 active workspace
- Trace: `docs/agent-engineering-course/articles/18-evidence-contract/subagent-trace.md`
- Retrieved / Run At: 2026-08-25 Asia/Shanghai
- Version Scope: Active Article 18 transaction on baseline `272ff0e...`
- Reproduction: Read the canonical ownership rows, glossary terms, the full published upstream lessons and the Article 18 card; compare stated ownership and non-goals.
- Observation: The corpus repeatedly separates structure from evidence truth, execution trace from Evidence, information provenance from current truth, and future Article 21/22 ownership. The Article 18 card sets Required Lab NONE and forbids BuildPilot runtime/benefit claims.
- Counter-evidence Searched: Read full Articles 03, 06 and 12–17 for overlapping ownership or contradictory BuildPilot implementation evidence; none transfers Article 21/22 ownership or supplies Article 18 runtime evidence.
- Interpretation: C10 is a repository fact; C02/C03/C06–C09 remain Article 18 design choices bounded by the approved card.
- Proves: Course-local topic boundaries, upstream terminology and strict BuildPilot posture.
- Does Not Prove: That the course design is an industry standard; that BuildPilot, Evidence Store, Replay or Eval exists or works.
- Limitations: Course-local authority; current Article 18 workspace is uncommitted during the Research gate and requires Master validation.
- Falsifier: A canonical/current course artifact assigning these exact responsibilities differently, or approved runtime evidence for Article 18, would require revision.
- Confidence Posture: HIGH for course ownership; N/A_DESIGN for proposed record/state semantics.
- Acceptance Posture: ACCEPT_FOR_SCOPED_COURSE_USE; proposals retain Proposal wording.
- Course Usage: Boundary matrix, terminology continuity, BuildPilot design case and all wording ceilings.
- BuildPilot Implication: `ADOPT` — keep the package as DESIGN / NOT IMPLEMENTED / NOT RUN; experiment count 0; observed evidence ABSENT.
- Owner: RESEARCHER
- Verified At: 2026-08-25

## Proposed semantic acceptance gates

This is the minimal course design associated with `18-C07`; it is not a runtime report:

| Gate | Question | Fail-closed result |
|---|---|---|
| 0 Parse / Schema | Is the record readable and structurally valid? | Reject malformed record; do not infer semantics |
| 1 Claim / Subject Identity | Is the exact proposition and subject unambiguous? | BLOCKED |
| 2 Source Integrity / Resolution | Can each Evidence reference be resolved and, where possible, content-identified? | PARTIAL or BLOCKED |
| 3 Provenance | Are source origin and relevant derivations attributable? | PARTIAL or BLOCKED |
| 4 Applicability | Do version, time and scope cover the Claim? | Narrow Claim or PARTIAL |
| 5 Support Mapping | Does each Observation support/refute a specific Claim portion without hidden inference? | PARTIAL or BLOCKED |
| 6 Alternatives | Are conflicting Evidence and plausible alternatives recorded? | Unknown/BLOCKED if unresolved |
| 7 Boundaries | Are limitations, does-not-prove and falsifier explicit? | Do not accept causal/absolute wording |
| 8 Confidence | Is any confidence label tied to a declared scheme and rationale? | Remove or mark uncalibrated |
| 9 Acceptance | Did an identified reviewer/verifier apply a fixed policy/version to fixed Evidence inputs? | No accepted decision |

## Proposed lifecycle and conflict policy

`APPEND -> REVIEW -> ACCEPT | REJECT | NEEDS_REVIEW`

- `APPEND`: create a new immutable record/revision; never rewrite the captured Observation.
- `SUPERSEDE`: a replacement points to the prior revision and states why.
- `INVALIDATE`: an event records target, reason, actor and time; the historical record remains addressable.
- `REVIEW`: bind policy/version, Evidence IDs, reviewer/verifier, time and decision.
- `CONFLICT`: retain both sides. Resolve only by explicit scope/version/authority rules; otherwise keep `BLOCKED`/Unknown.
- `PARTIAL`: narrow Claim/scope; never promote partial support to an unrestricted causal statement.

## BuildPilot diagnostic evidence package

Status: **DESIGN / NOT IMPLEMENTED / NOT RUN**.

A proposed package contains `case_id`, Claim set, source manifest, Observations, inference graph, alternatives/counter-evidence, limitations/falsifiers/Unknowns, Evidence statuses, acceptance policy/version/decision and lifecycle/review references. No package was generated in this Article; there is no runtime trace, experiment, accuracy, cost, latency, production or benefit evidence.

## Gate recommendation

**PASS_RECOMMENDED / MASTER_VALIDATION_PENDING.** The Claim Register covers all ten approved questions with eight complete Evidence Cards and zero core `BLOCKED` Claims. Authoring must preserve:

- `18-C04` and `18-C05` wording ceilings;
- Proposal language for `18-C02`, `18-C03`, `18-C06`, `18-C07`, `18-C08`, `18-C09`;
- the Article 03/21/22 boundaries;
- Required Lab `NONE`, experiment count `0`, observed evidence `ABSENT`;
- BuildPilot `DESIGN / NOT IMPLEMENTED / NOT RUN`.

Outline and Draft remain forbidden until Master validation completes the Evidence Gate.
