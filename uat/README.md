# User Acceptance Testing (UAT)

Everything needed to run the Naija Prime School UAT from scratch: a
realistic demo school, a scripted test catalogue that runs every case in a
real browser as the right role, and a workbook that records the results for
sign-off.

| Path | What it is |
| --- | --- |
| `seed/` | C# console app that builds the demo school through the app's own services (so business rules and the audit trail apply). |
| `reset-and-start.sh` | Drops and re-seeds the UAT database, then starts the app against it on http://localhost:5080. |
| `tests/` | The test catalogue (`cases/*.mjs`, one file per module) and the Playwright runner (`run.mjs`). |
| `results/<cycle>/` | `results.json` plus one screenshot per test case (`shots/<ID>.jpg`). |
| `defects.json` | The defect log: what failed, why, and how it was fixed. |
| `tools/build_workbook.py` | Builds `NaijaPrimeSchool-UAT-Workbook.xlsx` from the results and the defect log. |
| `NaijaPrimeSchool-UAT-Workbook.xlsx` | Summary and sign-off sheet, test-data sheet, one sheet per module, and the defect log. |

## Running it

Prerequisites: the .NET 10 SDK, SQL Server (a container is fine), Node 20+
and Python 3 with `openpyxl` and `pillow`.

```bash
# 1. Fresh demo school + app on :5080. The database name must contain "UAT".
uat/reset-and-start.sh "Server=localhost,1433;Database=NaijaPrimeSchool_UAT;User Id=sa;Password=...;TrustServerCertificate=True"

# 2. Run every test case (about an hour; cases run in order and build on each other).
cd uat/tests && npm install && node run.mjs --cycle cycle-1

# 3. After fixing defects: reset again and re-run everything as the re-test.
uat/reset-and-start.sh "..." && (cd uat/tests && node run.mjs --cycle cycle-2)

# 4. Build the workbook.
python3 uat/tools/build_workbook.py
```

Useful runner options: `--module "Fees & payments"` runs one module and
`--only FIN-03,FIN-04` runs named cases. Both merge into the cycle's
existing `results.json`. Later cases depend on data that earlier cases create,
so run a whole module (or everything) on a fresh seed when in doubt.

Run the UAT on a school day (Monday–Friday). The seed leaves today's
register open, and the timetable checks expect today's lessons.

`reset-and-start.sh` starts the app with UAT timings:
- announcement reminders and message alerts go out 1 minute after
  publishing, instead of 120 and 10 minutes;
- the dispatcher runs every 10 seconds.

This lets the reminder and alert cases watch messages go out. Email and SMS
use the *Log* providers, so every message is written to
`uat/results/app.log`, and online payments use the built-in Paystack
simulator.

## The demo school

Dates are relative to the day of seeding: the current academic session and
term are the ones that contain today.

| Account | Password | Role |
| --- | --- | --- |
| `superadmin` | `Admin@12345` | SuperAdmin |
| `folake.adeyemi` | `Uat@12345` | HeadTeacher |
| `chinedu.okeke`, `aisha.bello`, `emeka.nwosu`, `grace.udoh` | `Uat@12345` | Teacher |
| `tunde.bakare` | `Uat@12345` | SchoolBursar |
| `musa.ibrahim` | `Uat@12345` | SchoolStoreKeeper |
| parents, e.g. `ngozi.okafor`, `chukwudi.eze` | `Family@123` | Parent |
| pupils, e.g. `chiamaka.okafor`, `oluwaseun.akinola` | `Family@123` | Student |

What the seed creates:
- **Staff and structure:** 7 staff accounts; the previous and current
  sessions with three terms; 7 classes and 9 subjects; a full weekly
  timetable for Primary 5A and part of one for Primary 1A.
- **Families:** 20 families with 30 pupils, all linked and enrolled, each
  parent and pupil with a portal login.
- **Attendance:** 10 days of submitted registers for Primary 5A and 1A.
- **Results:** Primary 5A has CA1, CA2 and Examination scores for three
  subjects, finalised results, and report cards (11 published, 2 drafts).
- **Fees:** First Term fee schedules. Of the 21 invoices, 12 are paid, 6
  half-paid and 3 unpaid.
- **Store:** 3 suppliers and 10 store items, 2 of them below reorder level.
- **Communications:**
  - 4 announcements (pinned, parents-only, class-targeted, draft);
  - 3 message threads;
  - reminder preferences for two parents.

## What the catalogue covers

136 cases across 13 modules and Sprints 1–16:

| Module | Covers |
| --- | --- |
| Sign-in & access control | Sign-in, sign-out and the dashboard; the full menu and page-access matrix for all seven roles. |
| User management | Listing, creating, editing and deactivating users; roles; password reset. |
| Academics | Sessions, terms, classes, subjects, periods and the timetable. |
| Students & parents | Admissions, photos, parent links, enrolments, portal accounts and parent deletion. |
| Attendance | Daily registers, subject attendance and summaries. |
| Results & report cards | Assessments and scores, results, report cards. |
| Fees & payments | Fee schedules, invoices, discounts, payments and refunds; the bursar dashboard. |
| Store & inventory | The catalogue, stock movements, suppliers and low stock. |
| Family portals | The parent and student portals and their privacy. |
| Announcements & reminders | Targeting, read tracking, email/SMS reminders and preferences. |
| Messaging | Two-way conversations and email/SMS reply alerts. |
| Online payments | The Paystack flow and its failure paths, the bursar view and the webhook. |
| Audit log | Before/after values, history, filters and hidden passwords. |

Every case records its steps, the expected result, the actual result, a
pass/fail and a full-page screenshot. The runner also flags any icon that
renders as a word instead of a glyph.
