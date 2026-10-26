---
name: 07-review-gate2
title: "Phase 7: Review Gate 2 Verification & Audit"
phase: 7
execution_type: devin_playbook
gate: "Human Review Gate 2 Approval"
auto_proceed_on_success: false
---

## Step 1: Resolve the Tran Group

If the user supplied a Tran Group identifier as an argument, use it.

Otherwise, list the spec sets present under `Artefacts/Specs/Refactor/` — each group that has a `<Tran Group>-UI-Spec.md`, `<Tran Group>-Backend-Spec.md`, and `<Tran Group>-reconcile-Spec.md`, plus its matching `Artefacts/Architecture/<Tran Group>.Architecture.md` — show them with their short descriptions, and ask the user which Tran Group to review. If the user has already confirmed a conversion order during `/03-MoveGroup`, ask for the **next** group in that confirmed sequence rather than treating the list as unordered. Do not default to processing all of them.

If the user's selection doesn't match a set, stop and report the mismatch, listing the valid identifiers found.

## Step 2: Confirm the four artefacts exist

For the resolved Tran Group, check for all four of these:
- `Artefacts/Specs/Refactor/<Tran Group>-UI-Spec.md`
- `Artefacts/Specs/Refactor/<Tran Group>-Backend-Spec.md`
- `Artefacts/Specs/Refactor/<Tran Group>-reconcile-Spec.md`
- `Artefacts/Architecture/<Tran Group>.Architecture.md`

If any is missing, stop and report which — do not run the review-gate2.devin.md agent against a partial set. Suggest re-running `/06-TargetBuild` (Refactor path) first to produce the missing spec(s).

## Step 3: Run the review-gate2.devin.md agent

Invoke the **review-gate2.devin.md** subagent, passing the Tran Group identifier resolved in Step 1. The agent owns its own context-building (scanning `Input/`, the group's BSTS/BDD/DB artefacts, `Config/Config.md`, and `DEVIN.md`), its review logic, and its file-naming/write scope — do not re-specify that here.

The agent writes `Artefacts/Review/<Tran Group>-ReviewGate2.md` with findings grouped as **Critical Gaps**, **Good to Have**, and **Advanced Suggestions**, plus a separate **Architecture Recommendations** section.

If the agent reports the review file already exists, relay its overwrite/skip question to the user before it writes.

## Step 4: Report back and offer to implement

After the agent completes, report to the user:
- Full path to `Artefacts/Review/<Tran Group>-ReviewGate2.md`.
- The finding counts per bucket and the build-readiness verdict.
- Ask which suggestions to implement or skip (by finding ID, whole bucket, all, or none).

This command only produces the review. Applying an accepted finding means re-running the owning spec agent (`refactor-ui-spec.devin.md` / `refactor-backend-spec.devin.md` / `refactor-reconcile-spec.devin.md` via `/06-TargetBuild`) with the chosen findings as input — do not edit any spec here. If the user chooses to skip everything, acknowledge and stop.
