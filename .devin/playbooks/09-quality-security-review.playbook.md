---
name: 09-quality-security-review
title: "Phase 9: Code Quality, Security Review & Fix"
phase: 9
execution_type: devin_playbook
gate: "Human Review Gate 3 Sign-Off"
auto_proceed_on_success: false
---

## Step 1: Build the Trans code list and let the user select one

Check for `Artefacts/Discovery/MoveGroup.md`. If it is missing, stop and report that — do not run either subagent without it. Suggest re-running `/03-MoveGroup` first.

Read the distinct values of the `Trans Group` column from `Artefacts/Discovery/MoveGroup.md` and present them to the user as the Trans code list, each with the `Short Description` recorded against it in that file and the transactions it covers. If the user has already confirmed a conversion order during `/03-MoveGroup`, present and process the groups in that confirmed order and ask for the **next** one in sequence rather than treating the list as unordered.

- If a Trans code was supplied as an argument to this command and it matches a value in the list, use it and skip the prompt.
- Otherwise **ask the user which single Trans code to review**. Do not default to one, do not pick for them, and never process more than one per run.
- If the supplied or selected value doesn't match any `Trans Group` value in the file, stop and report the mismatch, listing the valid Trans codes found.

## Step 2: Run security-review.devin.md and code-quality-review.devin.md in parallel

Invoke the **security-review.devin.md** subagent and the **code-quality-review.devin.md** subagent **together, as parallel calls in the same response** — do not run one, wait for it, then run the other. Pass both the same Trans code resolved in Step 1, and do not ask either one to review any other Trans code.

Each agent owns its own target-code validation, context building, review domains, severity scheme, scorecard, report path and file-naming, and previous-report handling — do not re-check, re-derive, or re-specify any of that here.

Both agents run without a direct user channel, so relay their questions and the user's answers verbatim in both directions:
- **code-quality-review.devin.md** asks up front whether a quality document or coding guideline is being supplied, its path if so, and how it should be applied. Put those questions to the user and pass the answers back before that agent proceeds.
- **security-review.devin.md** and **code-quality-review.devin.md** each ask, after their report is written, whether the findings should be fixed. Relay each question with its own finding IDs, and relay the user's selection back to the agent that asked.

Keep the two conversations distinct — never answer one agent's question with the other's context.

## Step 3: Report back

After both subagent runs complete, report to the user:
- Full paths to both files written under `Artefacts/Code Quality & Security/` for this Trans code.
- The Trans code reviewed (identifier and short description).
- Each report's headline scorecard grades, finding counts per severity, and verdict — kept separate per report.
- Any gaps either agent flagged (missing target code, missing specs, unreadable guideline).
- Which findings, if any, were accepted for fixing and what changed as a result.
- Which other Trans codes from `MoveGroup.md` have no report yet under `Artefacts/Code Quality & Security/`, so the user knows to run `/09-Quality-security-review.devin.md <Trans code>` again for those.
- Request the user to review both generated reports.

## Step 4: Offer to run the quality-security-fix.devin.md agent

Ask the user whether they want to go ahead with fixing the findings now. Keep this question scope-neutral — do not phrase it as "fix security and quality" or otherwise imply the scope is already Both; scope is not decided here.

- If the user says **yes**, invoke the **quality-security-fix.devin.md** subagent for the same Trans code resolved in Step 1. That agent owns its own scope question (Security / Code quality / Both), its own report reading, its own fix sequencing, and its own success summary — do not re-ask, re-derive, duplicate, or pre-answer any of that here. When the subagent asks its Step 2 scope question, relay it to the user verbatim and relay their answer back; do not answer it yourself based on this step's yes/no.
- If the user says **no**, abort here and do not invoke the agent.
