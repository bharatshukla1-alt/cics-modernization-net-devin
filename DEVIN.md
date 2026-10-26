# CICS to .NET Modernization Workbench — Devin IDE

You orchestrate a nine-phase autonomous modernization pipeline that re-architects mainframe CICS/COBOL screens (BMS 3270 maps, EXEC CICS programs, DB2 SQL, and VSAM files) into a modern cloud-ready target stack:
- **Backend**: ASP.NET Core (.NET 9) REST Web API with Entity Framework Core
- **Frontend**: Modern Web UI (React / Vite / Tailwind) replacing 3270 green screens
- **Database**: Target Relational Database (PostgreSQL, SQL Server, MySQL, SQLite, Oracle) provisioned automatically via `tools/db-apply.mjs`
- **Testing**: Automated end-to-end integration & UI test suite with Playwright & xUnit

---

## Architecture & Directory Structure

```
cics-modernization-net-devin/
├── DEVIN.md                   # Primary orchestration guide for Devin IDE
├── setup_devin_env.sh         # Linux environment initialization script
├── run_pipeline.sh            # Automation shell script for running pipeline phases
├── run_pipeline.py            # Python CLI pipeline runner with interactive gate prompts
├── .gitignore
├── .devin/
│   ├── config.json            # Devin project and pipeline configuration
│   ├── agents/                # 18 specialized Devin subagents
│   │   ├── discovery-agent.devin.md
│   │   ├── review-gate1.devin.md
│   │   ├── create-tran-group.devin.md
│   │   ├── tran-group-bdd.devin.md
│   │   ├── tran-group-bsts.devin.md
│   │   ├── setup-db.devin.md
│   │   ├── setup-vsam.devin.md
│   │   ├── db-status-report.devin.md
│   │   ├── refactor-ui-spec.devin.md
│   │   ├── refactor-backend-spec.devin.md
│   │   ├── refactor-reconcile-spec.devin.md
│   │   ├── review-gate2.devin.md
│   │   ├── test-writer-refactor.devin.md
│   │   ├── test-runner-refactor.devin.md
│   │   ├── test-bug-fix.devin.md
│   │   ├── security-review.devin.md
│   │   ├── code-quality-review.devin.md
│   │   └── quality-security-fix.devin.md
│   ├── playbooks/             # 9 phase playbooks executable by Devin
│   │   ├── 01-discovery.playbook.md
│   │   ├── 02-review-stage1.playbook.md
│   │   ├── 03-move-group.playbook.md
│   │   ├── 04-create-bdd-bsts.playbook.md
│   │   ├── 05-create-db.playbook.md
│   │   ├── 06-target-build.playbook.md
│   │   ├── 07-review-gate2.playbook.md
│   │   ├── 08-test-target.playbook.md
│   │   └── 09-quality-security-review.playbook.md
│   └── scripts/               # Setup and runner utilities
├── Config/
│   ├── Config.md              # Source-to-target tech stack mapping
│   └── Style-Sheet.md         # UI tokens and design system guide
├── Input/                     # READ-ONLY mainframe source artifacts
│   ├── bms/                   # BMS 3270 map definitions
│   ├── cbl/                   # CICS COBOL application programs
│   ├── cpy/                   # COBOL copybooks
│   ├── cpy-bms/               # Symbolic map copybooks
│   ├── csd/                   # CICS System Definition exports
│   ├── ctl/                   # DB2 DDL / IDCAMS control cards
│   ├── dcl/                   # DB2 DCLGEN declarations
│   └── ddl/                   # Mainframe DB2 DDL scripts
├── Artefacts/                 # Generated deliverables & documentation
│   ├── Discovery/             # TransVsPgm.md, CicsXref.md, Screen-metadata.md, MoveGroup.md
│   ├── Review/                # ReviewGate1.md, <Group>-ReviewGate2.md
│   ├── TranGroupData/         # BDD, BSTS, DB-Details, VSAM-Details specifications
│   ├── Architecture/          # <Group>.Architecture.md
│   ├── Database/              # <Group>-DB-Status.md
│   ├── DDL/                   # Target DDL scripts
│   ├── Specs/Refactor/        # UI-Spec, Backend-Spec, Reconcile-Spec
│   ├── QA/                    # Test cases and test results
│   └── Code Quality & Security/ # Security and code quality audit reports
├── Target/                    # Generated Modern .NET and React Source Code
│   └── MG-01/
│       ├── Backend/           # ASP.NET Core Web API microservice
│       │   └── TransactionTypeService/
│       └── Frontend/          # Modern React / Vite web interface
├── tools/
│   └── db-apply.mjs           # Database provisioning CLI wrapper
└── db-adapters/               # Database adapter engines (PostgreSQL, MySQL, SQL Server, SQLite, Oracle)
```

---

## Golden Rules for Devin IDE

1. **`Input/` is strictly READ-ONLY**: Never edit, rewrite, or delete files in `Input/`.
2. **Deterministic Outputs**: Write all outputs to the exact paths specified in the artifact contract table below.
3. **Respect Human Review Gates**:
   - **GATE 1** after Phase 2 (ReviewStage1): Stop and wait for user approval before clustering move groups.
   - **GATE 2** after Phase 7 (ReviewGate2): Stop and wait for user approval of target architecture & specs before test execution.
   - **GATE 3** after Phase 9 (Quality & Security Review): Final sign-off.
4. **Target Modernization Standards**:
   - Target backend must follow clean architecture: Controllers -> Services -> Repositories -> EF Core DbContext.
   - Target frontend must follow modern responsive component principles with proper state management and proxy routing to `/api/v1`.

---

## The 9-Phase Modernization Lifecycle

| Phase | Playbook | Agent(s) | Inputs | Outputs | Gate Check |
|---|---|---|---|---|---|
| **1. Discovery** | `01-discovery.playbook.md` | `discovery-agent` | `Input/csd/`, `Input/cbl/`, `Input/cpy-bms/` | `Artefacts/Discovery/TransVsPgm.md`<br>`Artefacts/Discovery/CicsXref.md`<br>`Artefacts/Discovery/Screen-metadata.md` | Auto |
| **2. Review Stage 1** | `02-review-stage1.playbook.md` | `review-gate1` | `Artefacts/Discovery/` | `Artefacts/Review/ReviewGate1.md` | **GATE 1 (Human)** |
| **3. Move Group** | `03-move-group.playbook.md` | `create-tran-group` | `Artefacts/Discovery/CicsXref.md` | `Artefacts/Discovery/MoveGroup.md` | Auto |
| **4. BDD & BSTS** | `04-create-bdd-bsts.playbook.md` | `tran-group-bdd`<br>`tran-group-bsts` | `Artefacts/Discovery/MoveGroup.md`<br>`Input/cbl/`, `Input/cpy/` | `Artefacts/TranGroupData/<Group>-BDD.md`<br>`Artefacts/TranGroupData/<Group>-BSTS.md` | Auto |
| **5. Database Provisioning** | `05-create-db.playbook.md` | `setup-db`<br>`setup-vsam`<br>`db-status-report` | `Input/ctl/`, `Input/dcl/`, `Input/ddl/`<br>`Config/Config.md` | `Artefacts/TranGroupData/<Group>-DB-Details.md`<br>`Artefacts/DDL/<Group>-schema.sql`<br>`Artefacts/Database/<Group>-DB-Status.md` | Auto |
| **6. Target Build Specs** | `06-target-build.playbook.md` | `refactor-ui-spec`<br>`refactor-backend-spec`<br>`refactor-reconcile-spec` | `Artefacts/TranGroupData/`<br>`Config/Style-Sheet.md` | `Artefacts/Specs/Refactor/<Group>-UI-Spec.md`<br>`Artefacts/Specs/Refactor/<Group>-Backend-Spec.md`<br>`Artefacts/Architecture/<Group>.Architecture.md`<br>`Artefacts/<Group>-Run-Guide.md` | Auto |
| **7. Review Gate 2** | `07-review-gate2.playbook.md` | `review-gate2` | `Artefacts/Specs/Refactor/`<br>`Artefacts/Architecture/` | `Artefacts/Review/<Group>-ReviewGate2.md` | **GATE 2 (Human)** |
| **8. Testing & Bug Fix** | `08-test-target.playbook.md` | `test-writer-refactor`<br>`test-runner-refactor`<br>`test-bug-fix` | `Target/<Group>/`<br>`Artefacts/Specs/` | `Artefacts/QA/Test Case/<Group>-Refactor-TestCase.md`<br>`Artefacts/QA/Test Result/<Group>-Refactor-TestResult.md` | Auto / Remediation |
| **9. Quality & Security** | `09-quality-security-review.playbook.md` | `security-review`<br>`code-quality-review`<br>`quality-security-fix` | `Target/<Group>/` | `Artefacts/Code Quality & Security/<Tran>-Security-Review.md`<br>`Artefacts/Code Quality & Security/<Tran>-CodeQuality-Review.md` | **GATE 3 (Human)** |

---

## Running the Target .NET 9 Service

To run and verify the generated .NET 9 Web API backend service:

```bash
# Navigate to the service API project
cd Target/MG-01/Backend/TransactionTypeService/src/TransactionTypeService.Api

# Restore dependencies
dotnet restore

# Build project
dotnet build

# Run service on designated port
dotnet run --urls=http://0.0.0.0:5000
```

To run and verify the modernized React frontend:

```bash
# Navigate to frontend project
cd Target/MG-01/Frontend

# Install dependencies
npm install

# Start development server
npm run dev
```

---

## How to Trigger in Devin IDE

- **Run Full Modernization**:
  ```bash
  python3 run_pipeline.py --all --group MG-01
  ```

- **Run Single Phase**:
  ```bash
  python3 run_pipeline.py --phase 1
  ```

- **Run With Auto-Gate Approval (Batch/CI Mode)**:
  ```bash
  python3 run_pipeline.py --all --group MG-01 --yes
  ```
