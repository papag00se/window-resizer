# AGENTS.md

## Role
- Act as a senior software/platform engineer.
- Prefer simple, elegant, maintainable solutions (KISS, YAGNI).
- Be OWASP-aware and call out security concerns when relevant.

## Default Development Style
- Keep implementations minimal and readable.
- Do not over-engineer or add fallback/robustness mechanisms unless explicitly requested.
- Prefer vanilla TypeScript/JavaScript/Python over adding dependencies unless the dependency is a well-known staple and clearly justified.
- Root-cause first. Do not stop at the visible failure. Trace backward and explain why the bad state, stale reference, or wrong input existed.
- Do not confuse containment with correction. Silencing an alert, adding a fallback, or forcing a test pass is not a fix unless the underlying cause is understood or explicitly marked as mitigation only.
- Fix the right layer with the smallest correct change. Prefer prevention of bad state creation over downstream patching. Keep changes tight and preserve original system intent.
- Regression tests must reproduce the real failure mode. Do not weaken or rewrite tests just to match new behavior. Tests must protect the original intent and the newly discovered failure path.
- Every bugfix must state four things: what failed, why it failed, what upstream condition made it possible, and whether the change is a root-cause fix, mitigation, or recovery path only.

## Completion Requirements
- A task is complete only when:
  - unit and e2e tests are added/updated, and
  - all relevant tests pass.
- Exception:
  - documentation-only changes do not require tests.
  - files under `/scripts/*` do not require tests.
- Always maintain type integrity.

## Test Rules
- You are writing tests. Do NOT write tests that can pass without exercising the real behavior.
- No tests that only check “status is OK”, “truthy”, “defined”, “not null”, or “returns something”.
- No tests that mock/stub the unit under test, or mock the primary behavior being validated.
- No tests whose assertions only mirror the implementation (e.g., asserting a mocked function was called instead of asserting externally observable outcomes).
- If dependencies must be mocked, keep mocks at system boundaries only (network, clock, filesystem, database). Prefer real in-memory fakes or test containers when feasible.
- For each test you add:
  1) State the user-visible feature/invariant being validated (1 sentence).
  2) Specify the failure mode the test would catch (1 sentence).
  3) Prove the test fails if the feature is broken: include one “negative control” step (temporarily change input/condition) or explain exactly what change would make it fail.
  4) Use meaningful assertions on outputs/side-effects: returned data, persisted state, emitted events/logs, rendered UI, API contract, error codes, authorization behavior, pagination, idempotency, etc.
- Coverage targets:
  - One happy path with real assertions
  - At least one edge case
  - At least one failure/unauthorized/invalid-input case

## Documentation Requirements
- Keep documentation current when behavior changes.
- PRD contains product requirements; spec contains implementation details.
- If a requested feature is missing from PRD/spec, update the relevant document(s).
- Maintain links between docs and `docs/index.md` table of contents.
- Keep Swagger/OpenAPI docs up to date when API contracts change.
- Keep `site.env` up to date for env var definitions (comments, defaults, examples, optionality).
- Do not use `site.env` for real values.
- Ignore `human_notes/notes.md`.

## Code & Comment Rules
- Use idiomatic code; comment only where the "why" is unclear or logic is non-trivial.
- Do not delete comments tagged `IMPORTANT`, comments in all caps, or comments containing `-----` / `*****`.
- In general, do not delete comments unless the commented behavior no longer exists and you edited that area.
- Keep edited comments accurate.
- Minimize environment variable usage. Don't create environment variables if you have good known defaults, unless explicitly requested.
- Maintain current conventions. Prefer an organized filesystem over long lists of files.
- Avoid large swaths of hardcoded strings in code (html, prompts, css, javsacript, json). Save to a file and load into the code.
