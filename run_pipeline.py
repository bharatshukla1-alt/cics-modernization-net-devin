#!/usr/bin/env python3
"""
CICS Modernization Pipeline Runner for Devin IDE
Orchestrates the 9-phase modernization pipeline with automated human review gate checks.
"""

import glob
import os
import sys
import argparse
import subprocess
import json

PHASES = [
    {"num": 1, "name": "Discovery", "playbook": "01-discovery.playbook.md", "gate": None},
    {"num": 2, "name": "ReviewStage1", "playbook": "02-review-stage1.playbook.md", "gate": "GATE 1"},
    {"num": 3, "name": "MoveGroup", "playbook": "03-move-group.playbook.md", "gate": None},
    {"num": 4, "name": "CreateBDDBSTS", "playbook": "04-create-bdd-bsts.playbook.md", "gate": None},
    {"num": 5, "name": "CreateDB", "playbook": "05-create-db.playbook.md", "gate": None},
    {"num": 6, "name": "TargetBuild", "playbook": "06-target-build.playbook.md", "gate": None},
    {"num": 7, "name": "ReviewGate2", "playbook": "07-review-gate2.playbook.md", "gate": "GATE 2"},
    {"num": 8, "name": "TestTarget", "playbook": "08-test-target.playbook.md", "gate": None},
    {"num": 9, "name": "QualitySecurityReview", "playbook": "09-quality-security-review.playbook.md", "gate": "GATE 3"}
]

def print_banner(text):
    print("\n" + "=" * 70)
    print(f"  {text}")
    print("=" * 70 + "\n")

def check_gate(gate_name, auto_approve=False):
    print_banner(f"HUMAN REVIEW REQUIRED: {gate_name}")
    if auto_approve:
        print(f"[!] Auto-approving {gate_name} (flag --yes provided). Proceeding...")
        return True
    
    response = input(f"Review the artifacts for {gate_name}. Type 'approved' or 'yes' to proceed (or 'exit' to pause): ").strip().lower()
    if response in ["approved", "yes", "y"]:
        print(f"[+] {gate_name} confirmed. Advancing to next phase.\n")
        return True
    else:
        print(f"[-] {gate_name} not approved. Pausing pipeline. Re-run after addressing feedback.")
        return False

EXPECTED_OUTPUTS = {
    1: ["Artefacts/Discovery/TransVsPgm.md", "Artefacts/Discovery/CicsXref.md", "Artefacts/Discovery/Screen-metadata.md"],
    2: ["Artefacts/Review/ReviewGate1.md"],
    3: ["Artefacts/Discovery/MoveGroup.md"],
    4: ["Artefacts/TranGroupData/{group}-*-BDD.md", "Artefacts/TranGroupData/{group}-*-BSTS.md"],
    5: ["Artefacts/TranGroupData/{group}-*-DB-Details.md", "Artefacts/Database/{group}-*-DB-Status.md"],
    6: ["Artefacts/Specs/Refactor/{group}-UI-Spec.md", "Artefacts/Specs/Refactor/{group}-Backend-Spec.md",
        "Artefacts/Specs/Refactor/{group}-reconcile-Spec.md", "Target/{group}"],
    7: ["Artefacts/Review/{group}-ReviewGate2.md"],
    8: ["Artefacts/QA/Test Case/{group}-Refactor-TestCase.md", "Artefacts/QA/Test Result/{group}-Refactor-TestResult.md"],
    9: ["Artefacts/Code Quality & Security/{group}-Security-Review.md",
        "Artefacts/Code Quality & Security/{group}-CodeQuality-Review.md"],
}


def check_outputs(num, group):
    missing = []
    for pattern in EXPECTED_OUTPUTS.get(num, []):
        if "{group}" in pattern and not group:
            missing.append(pattern + " (pass --group)")
            continue
        path = pattern.format(group=group)
        if not glob.glob(path):
            missing.append(path)
    return missing


def run_phase(phase_info, group=None, auto_approve=False):
    num = phase_info["num"]
    name = phase_info["name"]
    pb = phase_info["playbook"]
    gate = phase_info["gate"]

    print_banner(f"Phase {num}: {name} (Playbook: .devin/playbooks/{pb})")
    
    if group:
        print(f"[*] Targeting Transaction Group: {group}")
    
    pb_path = os.path.join(".devin", "playbooks", pb)
    if not os.path.exists(pb_path):
        print(f"[-] Playbook file not found: {pb_path}")
        return False
    
    print(f"[*] Agents run by Devin from {pb_path}; this runner checks their outputs.")
    missing = check_outputs(num, group)
    if missing:
        for path in missing:
            print(f"[-] Missing expected output: {path}")
        print(f"[-] Phase {num} incomplete - run the playbook in Devin, then re-run this phase.")
        return False
    print(f"[+] Phase {num} outputs present.")

    if gate:
        approved = check_gate(gate, auto_approve=auto_approve)
        if not approved:
            return False
            
    return True

def main():
    parser = argparse.ArgumentParser(description="CICS to .NET Modernization Pipeline Runner for Devin IDE")
    parser.add_argument("--phase", type=int, choices=range(1, 10), help="Run a specific phase (1-9)")
    parser.add_argument("--group", type=str, default="MG-01", help="Target Trans Group identifier (e.g. MG-01)")
    parser.add_argument("--all", action="store_true", help="Execute all pipeline phases sequentially")
    parser.add_argument("--yes", action="store_true", help="Auto-approve human review gates (non-interactive mode)")

    args = parser.parse_args()

    print_banner("CICS Modernization Workbench - Devin IDE Pipeline")
    print(f"Target Architecture: .NET 9 Web API + React UI")
    print(f"Active Transaction Group: {args.group}")

    if args.phase:
        target_phase = next(p for p in PHASES if p["num"] == args.phase)
        success = run_phase(target_phase, group=args.group, auto_approve=args.yes)
        sys.exit(0 if success else 1)

    if args.all:
        for p in PHASES:
            success = run_phase(p, group=args.group, auto_approve=args.yes)
            if not success:
                print(f"[-] Pipeline stopped at Phase {p['num']}.")
                sys.exit(1)
        print_banner("ALL PHASES COMPLETED SUCCESSFULLY")
        sys.exit(0)

    parser.print_help()

if __name__ == "__main__":
    main()
