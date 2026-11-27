# Phase 5 — Build

## Preconditions

- The user approved the plan. Note in `decisions.md` whether they chose a commit per step.
- Prerequisites are done (recorded in `decisions.md`; secrets are never written into the record).
- Set `state.json` to `phase: "build", waiting: null, step: "1/<n>"`.
- The Build skills from the table in [SKILL.md](SKILL.md) are loaded. Check each step against them,
  not only against CLAUDE.md.
- In a git repo: check `git status`. If there are uncommitted changes you didn't make, ask before
  touching those files. For anything beyond a small fix, work on a branch
  (`feature/<slug>`); creating a local branch is fine, pushing is not without a yes.

## Each step

1. **Write the code to CLAUDE.md** and the `review-ready-code` skill: the reference
   implementation's shape, precise names, guard clauses, no magic values, errors handled with
   context, types precise, comments only for why.
2. **Tests with the code**, not after. Where the plan names a test, write it in the same step. Bug
   fixes start with a failing test that reproduces the bug when practical.
3. **Contracts and generated code.** Change the source schema / OpenAPI / migration, then
   regenerate clients and types with the project's tool. Never hand-edit generated output.
4. **Config.** Every new env var or config key goes into `.env.example` (placeholder value), Docker
   / compose files, CI and the README or CONTEXT.md in the same step.
5. **Verify the step**, actually, and log the commands and results in `log.md`:
   - re-read every changed file end to end;
   - trace the imports and callers of the touched symbols;
   - run the project's typecheck, lint and tests (the commands from CLAUDE.md "Check it" or the
     manifests); fix every new warning; report pre-existing failures as pre-existing;
   - for API changes, call the endpoint (curl / `postman:api-testing`) and check the response
     shape and status codes, including one failure case;
   - for UI changes, load the page and look (`browser-automation`): console errors, failed
     requests, layout at mobile and desktop width, and the loading / empty / error states;
   - test every other feature the change interacts with, not only the new code.
6. **Update** the requirements matrix status in `plan.md`, append to `log.md`, and bump `step`.
   If the user chose a commit per step, commit now with a message that says why.

## After the last step

- Run `/code-review` (or the `pr-review-toolkit` agents: `code-reviewer`, `silent-failure-hunter`,
  `pr-test-analyzer`, `type-design-analyzer`) on the changed files. Fix what it confirms.
- For anything touching auth, input handling, payments or personal data, run `/security-review`.
- Re-run the full check suite once more after the fixes.

## Stop mid-build only for

- **A deviation from the approved plan**: a file the plan didn't list, a changed contract, a
  replaced feature, a changed decision. Explain it in one line and ask.
- **A new unknown that changes the build.** Ask with `AskUserQuestion`, as in
  [questions.md](questions.md).
- **An outward-facing action**: push, PR, deploy, a migration against a shared database, emails,
  webhooks, paid API calls. Ask every time.
- **A destructive change the plan didn't name**: deleting files, data or columns, dropping a
  dependency others use.

Everything else proceeds without asking.

## Document the feature

Set `phase: "qa"`. Load the `feature-docs` skill and write `docs/features/<feature>.md` (or update
the existing doc) **from the final code**, following its template: what it does, the walkthrough,
the Mermaid diagram, the code walkthrough layer by layer with file links, services and
dependencies, data, API and contracts, configuration, errors and edge cases, testing, extending
it, limitations, changelog. Update `docs/features/README.md` and the CONTEXT.md features index,
and link the doc from `log.md`. Re-read the doc as a newcomer would before moving on.

## QA handoff

1. Post the handoff:
   - how the feature works, in two or three plain sentences, with a link to its feature doc;
   - what was built: files by path, contracts changed, migrations added;
   - requirements status: N/N rows implemented, and any deviations by Dn;
   - what was verified, with the commands and results, and what wasn't;
   - the QA script from `plan.md`, as the user's checklist;
   - the config keys that tune it and where each lives;
   - rollback;
   - a suggested commit message / PR description if nothing was committed yet.
2. Set `phase: "done"`.
3. Save one `project` memory pointing to `docs/pipeline/<slug>/` and the feature doc, holding what
   is non-obvious (gotchas, pending items, follow-ups), and add its line to `MEMORY.md`.

Bug reports after the handoff are normal iteration. Fix them directly; the write gate is off once
the phase is `done`. When a fix changes behaviour, update the feature doc and its Changelog in the
same change. For anything larger than a fix, start a new pipeline or `resume` this one.
