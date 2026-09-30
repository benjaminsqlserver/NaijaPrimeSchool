// Builds the Naija Prime School user manual (Word .docx) from the content
// below and the screenshots in ./shots (see capture.mjs).
//   node build-manual.js   →  NaijaPrimeSchool-User-Manual.docx
const fs = require('fs');
const path = require('path');
const {
  Document, Packer, Paragraph, TextRun, ImageRun, HeadingLevel, AlignmentType, Table, TableRow, TableCell,
  WidthType, ShadingType, BorderStyle, LevelFormat, PageBreak, Header, Footer, PageNumber, TableOfContents,
  PositionalTab, PositionalTabAlignment, PositionalTabRelativeTo, PositionalTabLeader,
} = require('docx');

const SHOTS = path.join(__dirname, 'shots');
const SIZES = JSON.parse(fs.readFileSync(path.join(SHOTS, 'sizes.json'), 'utf8'));
const GREEN = '0B6B3A', LIGHT = 'EAF4EE', AMBER = 'FFF4DC', GREY = '5B6770';
const FONT = 'Calibri';
const CONTENT_W = 9026; // A4 width 11906 − 2 × 1440 margins (DXA)
const AUTHOR = 'Benjamin Fadina', COMPANY = 'HepziBen Technologies Ltd.';

// ------------------------------------------------------------------ helpers
const body = [];
let figNo = 0, listNo = 0;

/** Text with **bold** and _italic_ markup → runs. */
function runs(text, base = {}) {
  const out = [];
  const re = /(\*\*[^*]+\*\*|_[^_]+_)/g;
  let last = 0, m;
  while ((m = re.exec(text))) {
    if (m.index > last) out.push(new TextRun({ text: text.slice(last, m.index), ...base }));
    const t = m[0];
    out.push(t.startsWith('**') ? new TextRun({ text: t.slice(2, -2), bold: true, ...base }) : new TextRun({ text: t.slice(1, -1), italics: true, ...base }));
    last = m.index + t.length;
  }
  if (last < text.length) out.push(new TextRun({ text: text.slice(last), ...base }));
  return out;
}
const h1 = t => body.push(new Paragraph({ heading: HeadingLevel.HEADING_1, children: [new TextRun(t)] }));
const h2 = t => body.push(new Paragraph({ heading: HeadingLevel.HEADING_2, children: [new TextRun(t)] }));
const h3 = t => body.push(new Paragraph({ heading: HeadingLevel.HEADING_3, children: [new TextRun(t)] }));
const p = t => body.push(new Paragraph({ children: runs(t), spacing: { after: 120 } }));

function steps(items) {
  const instance = ++listNo;
  for (const s of items) body.push(new Paragraph({ numbering: { reference: 'steps', level: 0, instance }, children: runs(s), spacing: { after: 60 } }));
  body.push(new Paragraph({ spacing: { after: 60 }, children: [] }));
}
function bullets(items) {
  for (const s of items) body.push(new Paragraph({ numbering: { reference: 'bullets', level: 0 }, children: runs(s), spacing: { after: 60 } }));
  body.push(new Paragraph({ spacing: { after: 60 }, children: [] }));
}
function callout(kind, text) {
  const colors = { Tip: [LIGHT, GREEN], Note: ['EEF2FB', '3B5BDB'], Important: [AMBER, 'B7791F'] }[kind];
  body.push(new Paragraph({
    shading: { type: ShadingType.CLEAR, color: 'auto', fill: colors[0] },
    border: { left: { style: BorderStyle.SINGLE, size: 24, color: colors[1], space: 8 } },
    spacing: { before: 80, after: 160 }, indent: { left: 160, right: 160 },
    children: [new TextRun({ text: `${kind}: `, bold: true, color: colors[1] }), ...runs(text)],
  }));
}
const tip = t => callout('Tip', t), note = t => callout('Note', t), important = t => callout('Important', t);

/** A screenshot with a numbered caption. */
function fig(name, caption, widthIn = 6.25) {
  const [w, h] = SIZES[name] ?? (() => { throw new Error(`Missing screenshot ${name}`); })();
  const width = Math.round(widthIn * 96);
  let height = Math.round(width * h / w);
  const maxH = 8.3 * 96; // keep tall pages on one page
  let finalW = width;
  if (height > maxH) { finalW = Math.round(width * maxH / height); height = Math.round(maxH); }
  body.push(new Paragraph({
    alignment: AlignmentType.CENTER, spacing: { before: 120, after: 60 }, keepNext: true,
    children: [new ImageRun({
      type: 'jpg', data: fs.readFileSync(path.join(SHOTS, `${name}.jpg`)), transformation: { width: finalW, height },
      altText: { title: caption, description: caption, name },
    })],
  }));
  body.push(new Paragraph({ style: 'Caption', alignment: AlignmentType.CENTER, children: [new TextRun(`Figure ${++figNo}: ${caption}`)] }));
}

const border = { style: BorderStyle.SINGLE, size: 4, color: 'C9D3CC' };
const borders = { top: border, bottom: border, left: border, right: border };
function table(headers, rows, widths) {
  const total = widths.reduce((a, b) => a + b, 0);
  const cell = (text, i, head) => new TableCell({
    borders, width: { size: widths[i], type: WidthType.DXA },
    shading: head ? { fill: GREEN, type: ShadingType.CLEAR, color: 'auto' } : undefined,
    margins: { top: 60, bottom: 60, left: 100, right: 100 },
    children: [new Paragraph({ children: runs(String(text), head ? { bold: true, color: 'FFFFFF' } : {}) })],
  });
  body.push(new Table({
    width: { size: total, type: WidthType.DXA }, columnWidths: widths,
    rows: [new TableRow({ tableHeader: true, children: headers.map((t, i) => cell(t, i, true)) }),
      ...rows.map(r => new TableRow({ children: r.map((t, i) => cell(t, i, false)) }))],
  }));
  body.push(new Paragraph({ spacing: { after: 120 }, children: [] }));
}
const pageBreak = () => body.push(new Paragraph({ children: [new PageBreak()] }));

// ------------------------------------------------------------------ content
// Part: introduction ---------------------------------------------------------
h1('1. Introduction');
p('The **Naija Prime School Management Portal** is a web application that runs the day-to-day work of the school in one place: pupil and parent records, class timetables, attendance, assessments and report cards, fees and payments (including online payment), the school store, announcements, private messages between families and the school office, and a full audit trail of every change.');
p('This manual explains, step by step and with pictures, how every type of user carries out their tasks. You do not need to read it from cover to cover — use the table below to find the chapters for your role.');
h2('1.1 Who should read what');
table(['Your role', 'What you do in the portal', 'Read chapters'], [
  ['**SuperAdmin** (system administrator)', 'Creates staff accounts, assigns roles, resets passwords; can use every feature.', '2, 3, 4 and any other'],
  ['**HeadTeacher**', 'Sets up the academic year, manages pupils and parents, publishes results and report cards, communicates with families, reviews finance, store and the audit log.', '2, 3, 5–13'],
  ['**Teacher**', 'Takes attendance, records assessment scores, views the class timetable.', '2, 3, 5.6, 7, 8.1'],
  ['**SchoolBursar**', 'Sets fees, issues invoices, records payments and refunds, monitors online payments.', '2, 3, 9'],
  ['**SchoolStoreKeeper**', 'Maintains the store catalogue, records purchases and issues, manages suppliers.', '2, 3, 10'],
  ['**Parent**', 'Follows each child\'s attendance, results and fees, pays fees online, reads announcements and messages the school.', '2, 3, 14'],
  ['**Student**', 'Sees today\'s timetable, results, attendance and fees; reads announcements; messages the school.', '2, 3, 15'],
], [2200, 4826, 2000]);
h2('1.2 Conventions used in this manual');
bullets([
  'Names of buttons, menus, fields and tabs are shown in **bold**, for example **Save changes**.',
  'A menu path such as **Family → Students** means: open the **Family** section in the menu on the left, then click **Students**.',
  'The pictures come from the demonstration school "Naija Prime School". Names, amounts and dates on your screen will be your own school\'s.',
  'Boxes labelled **Tip**, **Note** and **Important** give extra advice. Read the **Important** ones carefully — they protect records and money.',
]);
h2('1.3 What you need');
bullets([
  'A computer, tablet or phone with an up-to-date web browser (Chrome, Edge, Firefox or Safari).',
  'The portal address given to you by the school (for example https://portal.naijaprimeschool.ng).',
  'Your username (or email address) and password. Staff receive these from the system administrator; parents and pupils receive them from the school office when the pupil is admitted.',
]);

// Part: getting started ------------------------------------------------------
h1('2. Getting started');
h2('2.1 Signing in');
steps([
  'Open the portal address in your browser. The **Welcome back** sign-in page appears.',
  'In **Email or username** type your username (for example _folake.adeyemi_) or your email address.',
  'Type your **Password**. Tick **Remember me** only on a private device.',
  'Click **Sign in**. You arrive at your **Dashboard**.',
]);
fig('login-filled', 'The sign-in page');
important('Five wrong passwords in a row lock the account for 15 minutes. If you have forgotten your password, ask the system administrator (staff) or the school office (parents and pupils) to reset it — see section 4.4.');
h2('2.2 The dashboard');
p('The dashboard is the first page after signing in. It greets you by name and shows shortcut cards to the parts of the portal you use most. The shortcuts depend on your role: a teacher sees **Daily attendance**, **Assessments** and **Timetable**; a parent sees **My wards**, **Announcements** and **Messages**. The system administrator also sees account statistics (number of users, active and deactivated accounts, roles).');
fig('dashboard-head', 'Dashboard for the head teacher, with shortcut cards');
fig('dashboard-parent', 'Dashboard for a parent', 5.5);
h2('2.3 Finding your way around');
p('Every page has the same frame:');
bullets([
  '**Header (green bar):** the menu button (☰) shows or hides the menu; your username is on the right with the **Sign out** button.',
  '**Menu (left):** the sections you are allowed to use. Click a section heading to open or close it. You only ever see the parts of the portal your role permits.',
  '**Page area:** the page title and a short description at the top, action buttons on the right (for example **New class**), then filters and the page content.',
]);
h3('Lists (grids)');
bullets([
  'Most lists have **Search** and filter boxes above them. Results update as soon as you change a filter or press Tab after typing.',
  'Long lists are split into pages; use the page numbers at the bottom and choose how many rows to show per page.',
  'Click a column heading to sort by it (where sorting is available).',
  'Small icon buttons at the end of each row perform actions: ✏ edit, 🗑 delete, 👁 open/view. Hover over an icon to see what it does.',
]);
h3('Forms, messages and confirmations');
bullets([
  'Fields marked with an asterisk (*) are required. If something is missing or invalid, the field is outlined in red with a short explanation.',
  'After you save, a message appears briefly in the corner of the screen: green for success (for example "Saved"), red for a problem with the reason.',
  'Actions that cannot easily be undone — deleting, withdrawing, publishing, refunding — ask you to confirm in a pop-up first.',
]);
h2('2.4 When a page says "Access denied"');
p('If you follow a link or type an address for a page your role may not use, the portal shows **Access denied**. Click **Back to home**. If you believe you should have access, ask the system administrator to review your roles.');
fig('access-denied', 'Access denied', 5.2);
h2('2.5 Signing out');
p('Click **Sign out** in the top-right corner. Always sign out on shared computers. For your security the portal also ends sessions when an administrator deactivates your account or resets your password.');

// Part: user management ------------------------------------------------------
h1('3. Roles and permissions');
p('Access is controlled by **roles**. A person can hold more than one role (for example a teacher who is also the bursar). The seven roles are fixed by the system:');
table(['Role', 'Purpose'], [
  ['SuperAdmin', 'System administrator with access to every feature, including user accounts.'],
  ['HeadTeacher', 'Runs the school: academics, pupils and parents, results, communications, audit log; can also use finance and store.'],
  ['Teacher', 'Timetable, attendance and assessments.'],
  ['SchoolBursar', 'Fees, invoices, payments, online payments.'],
  ['SchoolStoreKeeper', 'Store catalogue, stock movements, suppliers.'],
  ['Parent', 'Parent portal for their own children only.'],
  ['Student', 'Student portal for their own records only.'],
], [2400, 6626]);
p('Parent and Student accounts are created automatically when the office registers a parent or admits a pupil (chapter 6). Staff accounts are created by the system administrator (chapter 4). Appendix A lists exactly which pages each role can open.');

h1('4. User management (SuperAdmin)');
p('Menu: **User Management**. Only the SuperAdmin role sees this section.');
h2('4.1 Viewing users');
p('**User Management → All Users** lists every account — staff, parents and pupils — with name, email, username, role badges and status (Active or Inactive).');
fig('users', 'All Users');
bullets([
  '**Search** finds users by name, email or username.',
  '**Role** shows only users with a given role, for example all Teachers.',
  '**Status** shows only Active or only Inactive accounts.',
]);
h2('4.2 Adding a staff member');
steps([
  'Click **Add new user** (or **User Management → Add New User**).',
  'Under **Personal details** enter title, first and last name (required), gender, date of birth and address.',
  'Under **Account** enter a **Username** (for example _kemi.adewale_), **Email**, phone, and a **Password** typed twice. Passwords must be at least 8 characters with upper- and lower-case letters, a number and a symbol.',
  'Under **Roles** tick at least one role.',
  'Click **Create user**. The new person can sign in immediately with the username and password you set — give these to them privately.',
]);
fig('user-new', 'Adding a new user');
note('Parents and pupils are not added here — register them from **Family** (chapter 6) so their portal account is linked to the right record.');
h2('4.3 Editing a user');
p('Click the ✏ **Edit** icon on the user\'s row. Change the details and click **Save changes**. From this page you can also **Assign roles**, **Reset password**, and **Deactivate** or **Activate** the account.');
fig('user-edit', 'Edit user');
h2('4.4 Resetting a password');
steps([
  'Open the user (✏ Edit) and click **Reset password**.',
  'Type the new password in **New password** and **Confirm password** (at least 8 characters).',
  'Click **Update password** and tell the person their new password privately. They are signed out of any open sessions.',
]);
fig('user-reset-password', 'Resetting a password');
h2('4.5 Assigning roles');
p('Click the 🛡 **Assign roles** icon on the user\'s row (or **Assign roles** on the edit page), tick or untick roles and click **Save roles**. The menu the person sees changes the next time they open a page.');
fig('user-roles', 'Assigning roles', 5.6);
h2('4.6 Deactivating and reactivating accounts');
p('When someone leaves, click the ⛔ **Deactivate** icon and confirm. The account is kept (with its history) but the person can no longer sign in. Click **Activate** to restore access. Deactivating is always preferable to deleting.');
h2('4.7 Roles page');
p('**User Management → Roles** lists the seven system roles and their descriptions for reference.');
fig('roles', 'Roles', 5.6);

// Part: academics ------------------------------------------------------------
h1('5. Academic set-up (HeadTeacher)');
p('Menu: **Academics**. Set these up at the start of each academic year, in this order: session → terms → classes → subjects → periods → timetable.');
h2('5.1 Academic sessions');
p('A session is an academic year such as 2026/2027. Exactly one session is marked **Current**; most pages open on the current session automatically.');
fig('sessions', 'Academic sessions');
steps([
  'Click **New session**.',
  'Enter the **Name** (for example 2027/2028), **Start date** and **End date**. Turn on **Mark current** only when the new year begins.',
  'Click **Save**.',
]);
fig('session-new', 'Creating a session');
bullets([
  'To make another session current, click the ✔ **Mark current** icon on its row.',
  'A session can only be deleted while it has no terms or classes.',
  'The end date must be after the start date, and names must be unique.',
]);
h2('5.2 Terms');
p('**Academics → Terms** lists the First, Second and Third Term of each session with their dates. Click **New term**, choose the **Session** and **Term**, enter the dates and **Save**. Mark the running term as **Current** with the ✔ icon. A term that already has timetable lessons cannot be deleted.');
fig('terms', 'Terms');
h2('5.3 Classes');
p('Classes (class arms) belong to a session — for example Primary 5A in 2026/2027. Each class has a level (Creche to Primary 6) and optionally a class teacher.');
fig('classes', 'Classes');
steps([
  'Click **New class**.',
  'Enter the **Name** (for example Primary 3B), pick the **Class level** and **Session**, and optionally the **Class teacher**.',
  'Click **Save**. Class names must be unique within a session.',
]);
h2('5.4 Subjects');
p('**Academics → Subjects** holds the subjects taught, each with a short **Code** (for example MTH) that appears on timetables and result sheets. Click **New subject**, enter name and code, and **Save**. A subject on a timetable cannot be deleted.');
fig('subjects', 'Subjects');
h2('5.5 Timetable periods');
p('Periods are the time slots of the school day, including breaks. Click **New period**, enter the name, display order, start and end time, and turn on **Break period** for breaks and lunch.');
fig('periods', 'Timetable periods');
h2('5.6 Class timetable');
p('Menu: **Academics → Timetable** (HeadTeacher and Teachers). Choose a **Term** and a **Class** to see the week: days across the top, periods down the side, break rows shaded.');
fig('timetable', 'The weekly timetable for Primary 5A');
steps([
  'Click an empty cell ("+ assign") or an existing lesson.',
  'Choose the **Subject**, the **Teacher**, and optionally a **Room** and notes.',
  'Click **Save**. To clear a slot, open it and click **Remove**.',
]);
fig('timetable-edit', 'Assigning a lesson to a period');
important('A teacher cannot be timetabled in two classes in the same period. If you try, the portal refuses and names the class the teacher is already teaching.');

// Part: family ---------------------------------------------------------------
h1('6. Pupils and parents (HeadTeacher)');
p('Menu: **Family**. These pages hold every pupil and parent record, how they are linked, and which class each pupil is in.');
h2('6.1 The pupils list');
p('**Family → Students** shows every pupil with photo (or initials), admission number, current class, date of birth, primary contact and status. Use **Search** (name or admission number), **Session**, **Class** and **Status** to narrow the list.');
fig('students', 'Students');
h2('6.2 Admitting a new pupil');
steps([
  'Click **Add new student** (or **Family → Add Student**).',
  'Under **Identity** enter the **Admission number**, **Admission date**, names, **Date of birth**, gender, blood group, state of origin and address.',
  'Under **Health** record any **Allergies** and **Medical notes** staff should know about.',
  'Under **Initial enrolment (optional)** pick the pupil\'s **Class** to enrol them straight away.',
  'Under **Portal sign-in** enter a **Username**, **Email** and **Initial password** for the pupil\'s own portal account.',
  'Click **Save student**. The pupil\'s record opens.',
]);
fig('student-new', 'Admitting a pupil (lower half of the form)');
note('Admission numbers must be unique, and the date of birth must be earlier than the admission date. The pupil can sign in to the Student portal immediately with the username and password you set.');
h2('6.3 The pupil\'s record');
p('Open a pupil with the ✏ **Edit** icon. The record has tabs:');
bullets([
  '**Profile** — edit personal and health details, then **Save changes**.',
  '**Photo** — **Choose new photo** to upload a JPEG or PNG (up to 2 MB); **Remove photo** to go back to initials. The photo appears on registers, score sheets and report cards.',
  '**Parents** — the parents and guardians linked to the pupil.',
  '**Enrolment history** — every class the pupil has been in.',
  '**Reminders** — the pupil\'s email/SMS reminder preferences (see 11.4).',
]);
fig('student-profile', 'A pupil\'s record — Profile tab');
fig('student-photo', 'Photo tab', 5.6);
p('Buttons at the top: **Deactivate** / **Activate** (a deactivated pupil also cannot sign in to the portal), **Send message** (start a private conversation with the pupil, chapter 12) and **Back**.');
h2('6.4 Linking parents and guardians');
steps([
  'On the pupil\'s **Parents** tab click **Link a parent**.',
  'In **Parent** start typing the parent\'s name and pick them. (Register the parent first if they are not in the list — 6.6.)',
  'Choose the **Relationship** (Father, Mother, Guardian …).',
  'Turn on **Primary contact** for the main contact; leave **Authorised to collect** on unless this person must not collect the child. Add **Notes** if needed.',
  'Click **Save link**.',
]);
fig('student-link-parent', 'Linking a parent to a pupil');
p('Linked parents show **Primary** and, if collection is not allowed, **Cannot pick up** badges. Use ✏ to change a link and the unlink icon to remove it. Once linked, the parent sees the pupil in their portal.');
fig('student-parents', 'Parents tab');
h2('6.5 Enrolments');
p('A pupil has at most one active enrolment per session. On the pupil\'s **Enrolment history** tab click **New enrolment**, choose the **Class** and **Enrolment date**, and click **Enrol**. To move a pupil, first withdraw the current enrolment (🚪 **Withdraw**), then enrol them in the new class.');
fig('student-enrolments', 'Enrolment history');
p('**Family → Enrolments** lists all enrolments with filters for session, class and status, and the same **Withdraw** and delete actions.');
fig('enrolments', 'Enrolments register');
h2('6.6 Registering a parent');
steps([
  'Click **Add new parent** (or **Family → Add Parent**).',
  'Enter title, names, gender, marital status, phones, **Email** (required), address, occupation and employer.',
  'Under **Portal sign-in** enter a **Username** and **Initial password** for the parent.',
  'Click **Save parent**, then link the parent to their children (6.4).',
]);
fig('parent-new', 'Registering a parent');
h2('6.7 The parents list and parent record');
p('**Family → Parents** lists all parents with contact details, number of pupils and status. Open a parent with ✏ to edit their details, see their linked pupils, set their **Reminders**, **Send message**, or **Deactivate** them (which also blocks their portal sign-in).');
fig('parents', 'Parents & guardians');
fig('parent-detail', 'A parent\'s record');
important('A parent can only be deleted after they have been unlinked from all pupils. Deleting a parent also removes their portal login. Prefer **Deactivate** when a family leaves the school.');

// Part: attendance -----------------------------------------------------------
h1('7. Attendance (Teachers)');
p('Menu: **Attendance**. Teachers, the head teacher and the administrator can take registers.');
h2('7.1 Taking the daily register');
steps([
  'Open **Attendance → Daily attendance**. The current session is selected.',
  'Choose your **Class**; the **Date** is today.',
  'Click **Open register**. Every pupil enrolled in the class is listed as **Present**.',
  'Change the **Status** of anyone not present: Late, Excused, Sick, Absent or Suspended. For **Late**, type the **Arrival** time (for example 08:25). Add **Remarks** if useful.',
  'Click **Save changes** at any time, and **Submit register** when you are done.',
]);
fig('attendance-open', 'Choosing the class and date');
fig('attendance-register', 'Marking the register');
p('The badges above the list count **Present** (including late arrivals), **Absent** (everyone not present), **Late** and **Total**. A submitted register is locked; the head teacher can click **Reopen** to correct it and submit again.');
note('Registers cannot be opened for future dates. To view an earlier day, just change the date — submitted registers open read-only.');
h2('7.2 Subject (lesson) attendance');
steps([
  'Open **Attendance → Subject attendance**.',
  'Choose the **Term**, **Class** and **Date**. The lessons on the timetable for that day are listed.',
  'Click **Take attendance** on a lesson, mark the pupils as for the daily register, and click **Submit**.',
]);
fig('attendance-subject', 'Lessons for the selected day');
h2('7.3 Attendance summary');
p('**Attendance → Summary** shows, per pupil, the days counted, present, late, excused and absent, and the **Present rate**, for a chosen session, term and class.');
fig('attendance-summary', 'Attendance summary');

// Part: results --------------------------------------------------------------
h1('8. Assessments, results and report cards');
h2('8.1 Assessments and scores (Teachers)');
p('Menu: **Results & Reports → Assessments**. An assessment is one test or exam for a class and subject in a term — for example "First CA", "Second CA" or "Examination".');
fig('assessments', 'Assessments for Primary 5A');
h3('Creating an assessment');
steps([
  'Click **New assessment**.',
  'Choose **Term**, **Class**, **Subject** and **Type** (First CA, Second CA, Mid-Term Test, Assignment, Project, Examination).',
  'Enter a **Title**, the **Maximum score** (for example 20) and the **Weight** it carries in the final result (for example 20), and optionally the date and notes.',
  'Click **Save**. The assessment is created as a **Draft**.',
]);
fig('assessment-new', 'New assessment');
h3('Entering scores');
steps([
  'Click the 📝 **Enter scores** icon on the assessment.',
  'Type each pupil\'s **Score**. Scores cannot exceed the maximum. Tick **Absent** for pupils who missed it, and add **Remarks** if needed.',
  'Click **Save scores**. You can return and change scores until the assessment is published.',
  'When all scores are final, click the **Publish** icon on the assessment. A published score sheet is read-only ("Published — read only"); unpublish it to correct a mistake.',
]);
fig('assessment-scores', 'A published score sheet');
h2('8.2 Subject results (HeadTeacher)');
p('**Results & Reports → Subject results** turns published assessments into a result per pupil and subject: weighted total, grade (A–F) and position in class.');
steps([
  'Choose the **Term** and **Class**, and optionally one **Subject**.',
  'Click **Recompute** to calculate draft results, or **Compute & finalise** (then **Finalise**) to lock them for report cards.',
]);
fig('results', 'Mathematics results for Primary 5A');
h2('8.3 Report cards (HeadTeacher)');
p('**Results & Reports → Report cards** lists one card per pupil per term with average, position, attendance and status (Draft or Published).');
fig('report-cards', 'Report cards');
h3('Generating report cards');
p('Click **Generate / refresh**, choose the **Term** and **Class**, optionally the date the **Next term begins**, and click **Generate**. Cards are created or refreshed with the latest results and attendance; published cards are left unchanged.');
fig('report-generate', 'Generating report cards');
h3('Completing and publishing a card');
steps([
  'Open a card with the 👁 **Open** icon. The top shows subjects offered, average, position in class and days present; the **Subjects** tab lists each subject with grade and remark.',
  'On **Affective traits** and **Psychomotor skills** pick a rating for each item (Excellent to Poor). Ratings save as you choose them.',
  'On **Comments** type the **Class teacher\'s comment** and **Head teacher\'s comment**, set **Next term begins**, and click **Save comments**.',
  'Click **Publish** and confirm. Parents and the pupil can now see the card in their portals. Use **Unpublish** to make further changes.',
]);
fig('report-detail', 'A report card');
fig('report-traits', 'Rating affective traits');
fig('report-comments', 'Comments tab');

// Part: finance --------------------------------------------------------------
h1('9. Fees and payments (SchoolBursar)');
p('Menu: **Finance**. Available to the bursar, head teacher and administrator. The normal flow each term is: fee schedule → issue invoices → record payments.');
h2('9.1 Bursar dashboard');
p('**Finance → Bursar dashboard** summarises a term: total invoiced, collected and outstanding, the number of invoices and payments, invoices by status (paid in full, partially paid, unpaid), money collected by method and invoiced by fee category.');
fig('finance-dashboard', 'Bursar dashboard');
h2('9.2 Fee schedules');
p('A fee schedule is the list of fees for one class level in one term (for example "Primary 5 — First Term fees").');
fig('fee-schedules', 'Fee schedules');
steps([
  'Click **New schedule**, choose the **Term** and **Class level**, enter a **Title**, and click **Create schedule**.',
  'On the schedule page click **Add item** for each fee: **Category** (Tuition, Development Levy, Examination, Books, Uniforms, Transport, Meals, PTA Levy …), **Description**, **Amount**, and whether it is **Mandatory**. Click **Save**.',
  'Check the total in the **Line items** heading, then click **Publish**. Only published schedules can be used to issue invoices.',
]);
fig('fee-schedule-detail', 'A fee schedule with its line items');
h2('9.3 Issuing invoices');
steps([
  'Open **Finance → Issue invoices**.',
  'Choose the **Term**, a published **Fee schedule** and the **Class**; set **Issued on** and an optional **Due date**.',
  'Click **Issue invoices**. One invoice is created for every pupil actively enrolled in the class. Pupils who already have an invoice from this schedule are skipped, so it is safe to run again after new admissions.',
]);
fig('invoices-issue', 'Issuing invoices to a class');
h2('9.4 Invoices');
p('**Finance → Invoices** lists all invoices with total, paid, balance and status (Issued, Partially Paid, Paid, Overdue, Cancelled). Search by invoice number, pupil name or admission number, and filter by term and status.');
fig('invoices', 'Invoices');
p('Click 👁 **View** to open an invoice. It shows the subtotal, discount, amount paid and balance, the line items, and the payments applied.');
fig('invoice-detail', 'An invoice');
h3('Giving a discount')
p('Type a discount against a line (for example a sibling discount on Tuition) and press Tab. The net and balance update at once. A discount cannot exceed the line amount, and it cannot bring the invoice below what has already been paid — refund the payment first if a family has overpaid.');
h2('9.5 Recording a payment');
steps([
  'Click **Record payment** on the invoice (or the 💳 icon on its row, or **Finance → Record payment** and pick the **Pupil**).',
  'Choose the **Method** (Cash, Bank Transfer, POS, Cheque, Mobile Money), the **Payment date**, the **Amount** received and a **Reference** (bank reference, cheque number).',
  'Under **Outstanding invoices** enter how much of the payment goes to each invoice — or click **Auto-allocate oldest first**. The total allocated cannot exceed the amount received; anything not allocated is kept as credit.',
  'Click **Save payment**. The receipt opens with its receipt number (for example NPS/RCP/2026/0019).',
]);
fig('payment-record', 'Recording a payment against an invoice');
h2('9.6 Payments, receipts and refunds');
p('**Finance → Payments** lists every receipt with pupil, method, reference, amount and status, with filters for dates, method and status. Click 👁 to open a receipt, which shows the amount, method, reference and the invoices it paid.');
fig('payments', 'Payments');
fig('payment-receipt', 'A receipt');
important('To reverse a payment (for example a bounced cheque or money returned to a parent), open the receipt, click **Refund** and confirm. The receipt is kept, marked Refunded, and the invoice balance goes back up by the refunded amount.');
h2('9.7 Online payments');
p('Parents and pupils can pay invoices online by card, bank transfer or USSD through Paystack (chapter 14.5). A payment is recorded as a normal receipt with method **Online Payment** only after Paystack confirms it. **Finance → Online payments** lists every attempt:');
bullets([
  '**Succeeded** — paid; links to the receipt.',
  '**Pending** / **Abandoned** — not yet confirmed; click 🔄 to check with Paystack now.',
  '**Failed** — no money was taken.',
  '**Needs review** — money arrived but something did not match (for example the amount); reconcile it and record the payment by hand with the Paystack reference.',
]);
fig('online-payments', 'Online payments');

// Part: store ----------------------------------------------------------------
h1('10. Store and inventory (SchoolStoreKeeper)');
p('Menu: **Store & Inventory**. Available to the storekeeper, head teacher and administrator.');
h2('10.1 Store dashboard');
p('The dashboard shows how many items are in the catalogue, how many are **below their reorder level**, the total stock value, this month\'s movements in and out, a **Low stock — needs replenishing** list, stock value by category and recent movements.');
fig('store-dashboard', 'Store dashboard');
h2('10.2 The catalogue');
p('**Store & Inventory → Catalog** lists every item with category, quantity on hand, reorder level, last cost and status. Items at or below their reorder level show a **Low** badge; turn on **Low stock only** to see just those.');
fig('store-items', 'Store catalogue');
h3('Adding an item');
steps([
  'Click **New item**.',
  'Enter the **Name**, optional **SKU** (must be unique), **Category**, **Unit of measure** and **Reorder level**.',
  'If you already hold stock, enter the **Opening quantity** and **Opening unit cost**.',
  'Click **Save item**.',
]);
fig('store-item-new', 'New store item');
p('Open an item (👁) to see its quantity, reorder level, last unit cost, stock value and full movement history, and to edit its details.');
fig('store-item-detail', 'An item with its movement history');
h2('10.3 Recording stock movements');
steps([
  'Click **Record movement** (from the dashboard, the catalogue row, the item page or the menu).',
  'Choose the **Item** and **Movement type**: Purchase, Return or Adjustment In add stock; Issue, Write-off or Adjustment Out remove it.',
  'Enter the **Date**, **Quantity**, **Unit cost** and a **Reference** (for example the purchase order number).',
  'For purchases pick the **Supplier**; for issues pick **one** recipient — a **Pupil**, a **Class** or a **Staff member**.',
  'Click **Save movement**. The quantity on hand updates immediately.',
]);
fig('movement-new', 'Recording a purchase');
note('You cannot issue more than is on hand; the portal tells you how many are available.');
p('**Store & Inventory → Movements** is the complete log, filterable by type, direction and dates.');
fig('movements', 'Stock movements');
h2('10.4 Suppliers');
p('**Store & Inventory → Suppliers** lists suppliers with contact details and purchase totals. Click **New supplier** to add one. Suppliers with purchase history cannot be deleted — deactivate them instead.');
fig('suppliers', 'Suppliers');

// Part: communications -------------------------------------------------------
h1('11. Announcements and reminders (HeadTeacher)');
p('Menu: **Communications**. Announcements appear in the parent and student portals and, if not read, are followed up by email and SMS.');
h2('11.1 The announcements list');
p('**Communications → Announcements** shows each notice with its status (Published or Draft), expiry, number of reads and email/SMS counters (sent, pending, failed, skipped). Row icons edit, send reminders now, publish/unpublish and delete.');
fig('announcements', 'Announcements');
h2('11.2 Writing an announcement');
steps([
  'Click **New announcement**.',
  'Enter a short **Title** and the **Body**.',
  'Choose the **Category** (General, Academic, Finance, Events, Holiday, Health, Emergency).',
  'Choose the **Audience**: Everyone, Parents, Students, or Specific Class (then pick the **Target class**).',
  'Optionally set **Expires on** and turn on **Pin to top** for important notices.',
  'Click **Publish now**, or **Save as draft** to finish later.',
]);
fig('announcement-new', 'Writing a class announcement');
h2('11.3 Email and SMS reminders');
p('When a notice is published, the portal waits a short grace period. Anyone in the audience who has **not** read it in the portal by then receives an email and/or SMS reminder. **Communications → Notification log** shows each reminder (and each message alert, chapter 12) with recipient, channel, status (Sent, Pending, Failed, Skipped) and a note explaining any skip.');
fig('notification-log', 'Notification log');
bullets([
  'Filter by **Type**, **Announcement**, **Channel**, **Status** or search by recipient.',
  'Use **Retry all failed** (or the retry icon on a row) after fixing a problem such as a wrong phone number.',
  'Emergency announcements are treated as urgent and are not held back by quiet hours.',
]);
h2('11.4 Reminder preferences');
p('Each parent and pupil can choose whether to receive email and/or SMS reminders and set **quiet hours** (for example 21:00–07:00) during which reminders are held. They do this themselves under **Reminder settings** in their portal (14.7); the office can also set them on the **Reminders** tab of the parent or pupil record.');
fig('parent-reminders', 'Reminder preferences on a parent\'s record');

h1('12. Messages (school office)');
p('Menu: **Communications → Inbox**. Parents and pupils can write privately to the school office; the head teacher and administrator answer from a shared inbox.');
h2('12.1 The inbox');
p('Conversations are listed newest first with the family member, the pupil it is about, the last message and the number of messages. A blue dot marks unread conversations and **Awaiting reply** marks those whose last message came from the family. Use **Search**, **Status** (Open or Resolved) and **Unread only**.');
fig('inbox', 'Inbox');
h2('12.2 Replying');
steps([
  'Click a conversation to open it. New messages appear automatically while it is open.',
  'Type in **Write a reply…** and click **Send reply**.',
  'When the matter is dealt with, click **Mark resolved**. Replying to a resolved conversation reopens it.',
  'Click **Alert log** to see the email/SMS alerts sent to the family about your replies.',
]);
fig('inbox-thread', 'Replying to a parent');
note('After you reply, the family receives an email and SMS alert unless they read your reply in the portal within a few minutes. The SMS never contains the message text.');
h2('12.3 Starting a conversation');
p('Click **New message** (or **Send message** on a parent or pupil record). Pick the recipient in **To**, optionally the pupil it is **About**, enter a **Subject** and **Message**, and click **Send**.');
fig('message-new', 'New message', 5.6);

h1('13. Audit log (HeadTeacher and SuperAdmin)');
p('Menu: **Administration → Audit log**. Every record created, updated, deleted or restored is logged with the date and time, the user, the record and — for updates — each changed field\'s **before** and **after** value. Passwords and security values are never shown, only "(hidden)".');
steps([
  'Filter by date range (**From**, **To**), **User**, **Record type** and **Action**, or **Search** for a name, number or any changed value (for example an old phone number).',
  'Click the ▸ arrow on a row to see the fields that changed.',
  'Click **History** to see the full timeline of that one record. The page address can be shared with a colleague.',
]);
fig('audit-log', 'Audit log with one entry expanded');
fig('audit-history', 'History of a single record');

// Part: portals --------------------------------------------------------------
h1('14. Parent portal');
p('Parents sign in with the username and password given by the school office (2.1). The menu shows **Parent portal** with **My wards**, **Announcements**, **Messages** and **Reminder settings**.');
h2('14.1 My wards');
p('**My wards** shows a card for each of your children with class, outstanding fees, attendance percentage and number of report cards, and totals across all your children. Click **Open** on a child for details.');
fig('parent-wards', 'My wards');
h2('14.2 A child\'s overview');
p('The **Overview** tab summarises the child\'s outstanding balance, attendance, report cards and days present this term.');
fig('ward-overview', 'Overview tab');
h2('14.3 Report cards');
p('The **Report cards** tab lists every published report card. Click the 👁 view button to open it: subjects with percentages, grades, positions and remarks; affective and psychomotor ratings; teachers\' comments and the date the next term begins. Click **Print** for a paper copy.');
fig('ward-report-cards', 'Report cards tab');
fig('portal-report-card', 'A report card as parents and pupils see it');
h2('14.4 Fees and invoices');
p('The **Fees & invoices** tab lists your child\'s invoices with total, balance and status, and the payments made.');
fig('ward-fees', 'Fees & invoices tab with the Pay online button');
h2('14.5 Paying fees online');
steps([
  'On **Fees & invoices**, click **Pay online** next to an invoice with a balance.',
  'Check the invoice, pupil and outstanding balance. Keep the full balance in **Amount to pay (₦)** or enter a smaller part-payment (at least ₦100).',
  'Click **Pay ₦… securely**. You are taken to Paystack\'s secure checkout, where you pay by card, bank transfer or USSD. The school never sees your card details.',
  'When you finish, you return to the **Payment status** page. **Payment received** shows your receipt number, and the invoice balance is updated.',
]);
fig('pay-invoice', 'Choosing how much to pay');
fig('pay-success', 'Payment received', 5.6);
bullets([
  '**Waiting for confirmation** — Paystack has not confirmed yet (common for transfers and USSD). Click **Check again** a little later.',
  '**Payment not completed** — no money was taken. Click **Try again**.',
  '**Being checked by the bursary** — the money arrived but needs a quick check. Do not pay again; the bursary will contact you.',
]);
note('The picture of the checkout page in this manual comes from the demonstration system, where a simulator stands in for Paystack.');
fig('pay-checkout', 'Checkout (demonstration simulator)', 5.2);
h2('14.6 Announcements');
p('**Announcements** shows notices for everyone, for parents, and for your children\'s classes. Pinned notices come first; new ones carry a **New** badge. Click **Mark as read** once you have read a notice — you will then not be sent an email or SMS reminder about it.');
fig('portal-announcements', 'Announcements in the parent portal');
h2('14.7 Messages to the school');
steps([
  'Open **Messages** and click **Message the school**.',
  'Optionally choose which child it is **About**, then enter a **Subject** and your **Message**.',
  'Click **Send**. The conversation appears in your list.',
]);
fig('portal-message-new', 'Writing to the school');
p('Conversations with a **New reply** badge have an answer from the school. Open one to read and reply. You are also alerted by email/SMS when the school replies, unless you read it in the portal first.');
fig('portal-messages', 'Your conversations');
fig('portal-thread', 'A conversation with the school office');
h2('14.8 Reminder settings');
p('Choose whether you want **Email reminders** and **SMS reminders** for unread announcements and replies from the school, and set **Quiet hours** when you do not want to be disturbed. The page shows the email address and phone number reminders go to. Click **Save preferences**.');
fig('portal-reminders', 'Reminder settings');

h1('15. Student portal');
p('Pupils sign in with their own username and password. The menu shows **Student portal** with **Today**, **My profile**, **My results**, **My attendance**, **My fees**, **Announcements**, **Messages** and **Reminder settings**.');
h2('15.1 Today');
p('The **Today** page shows your class, outstanding fees, attendance and report cards, today\'s timetable (period, time, subject, teacher and room) and shortcut buttons.');
fig('student-today', 'Student portal — Today');
h2('15.2 My profile');
p('Your personal and admission details. If anything is wrong, tell the school office — pupils cannot edit their own records.');
fig('student-my-profile', 'My profile');
h2('15.3 My results');
p('Your published report cards with term, class, average and position. Click the view button to open a card (as in 14.3). Only your own cards can be opened.');
fig('student-results', 'My results');
h2('15.4 My attendance');
p('Days counted, present, late and absent this term, and your attendance percentage.');
fig('student-attendance', 'My attendance');
h2('15.5 My fees');
p('Your invoices and payments. Where an invoice has a balance, **Pay online** works exactly as for parents (14.5).');
fig('student-fees', 'My fees');
h2('15.6 Announcements, messages and reminders');
p('These work the same way as in the parent portal: see 14.6, 14.7 and 14.8.');

// Appendices -----------------------------------------------------------------
h1('Appendix A. Who can use what');
p('✔ = can open and use the section. "Own" = only the person\'s own records or their own children\'s.');
table(['Section', 'Super-Admin', 'Head-Teacher', 'Teacher', 'Bursar', 'Store-Keeper', 'Parent', 'Student'], [
  ['User management', '✔', '', '', '', '', '', ''],
  ['Audit log', '✔', '✔', '', '', '', '', ''],
  ['Sessions, terms, classes, subjects, periods', '✔', '✔', '', '', '', '', ''],
  ['Timetable', '✔', '✔', '✔', '', '', '', ''],
  ['Students, parents, enrolments', '✔', '✔', '', '', '', '', ''],
  ['Attendance', '✔', '✔', '✔', '', '', '', ''],
  ['Assessments and scores', '✔', '✔', '✔', '', '', '', ''],
  ['Subject results, report cards', '✔', '✔', '', '', '', '', ''],
  ['Finance (fees, invoices, payments, online payments)', '✔', '✔', '', '✔', '', '', ''],
  ['Store & inventory', '✔', '✔', '', '', '✔', '', ''],
  ['Announcements, notification log, inbox', '✔', '✔', '', '', '', '', ''],
  ['Parent portal', '', '', '', '', '', 'Own', ''],
  ['Student portal', '', '', '', '', '', '', 'Own'],
  ['Portal announcements', '✔', '✔', '', '', '', '✔', '✔'],
  ['Portal messages and reminder settings', '', '', '', '', '', 'Own', 'Own'],
], [2826, 880, 880, 880, 880, 880, 900, 1000]);

h1('Appendix B. Status reference');
h3('Attendance statuses');
table(['Status', 'Counts as present?', 'Use for'], [
  ['Present (P)', 'Yes', 'Pupil in school on time.'],
  ['Late (L)', 'Yes', 'Pupil arrived late — record the arrival time.'],
  ['Excused (E)', 'No', 'Absence approved in advance.'],
  ['Sick (S)', 'No', 'Absent through illness.'],
  ['Absent (A)', 'No', 'Absent without explanation.'],
  ['Suspended (SP)', 'No', 'Pupil on suspension.'],
], [2200, 2000, 4826]);
h3('Invoice statuses');
table(['Status', 'Meaning'], [
  ['Draft', 'Not yet issued to the family.'],
  ['Issued', 'Issued, nothing paid yet.'],
  ['Partially Paid', 'Some money received; a balance remains.'],
  ['Paid', 'Fully paid.'],
  ['Overdue', 'Past its due date with a balance.'],
  ['Cancelled', 'Withdrawn; no longer payable.'],
], [2200, 6826]);
h3('Payment and online payment statuses');
table(['Status', 'Meaning'], [
  ['Confirmed', 'Payment received and applied.'],
  ['Pending', 'Awaiting confirmation (e.g. cheque not yet cleared; online payment not yet confirmed).'],
  ['Bounced', 'Cheque or transfer failed.'],
  ['Refunded', 'Payment reversed; the invoice balance was restored.'],
  ['Succeeded / Failed / Abandoned', 'Online payment outcomes (see 9.7).'],
  ['Needs review', 'Online payment arrived but needs the bursar to reconcile it.'],
], [2800, 6226]);
h3('Stock movement types');
table(['Adds stock', 'Removes stock'], [
  ['Opening Balance, Purchase, Return, Adjustment In', 'Issue, Write-off, Adjustment Out'],
], [4513, 4513]);

h1('Appendix C. Troubleshooting and frequently asked questions');
const faq = [
  ['I cannot sign in.', 'Check the username (or use your email address) and password — passwords are case-sensitive. After five wrong attempts the account is locked for 15 minutes. If your account has been deactivated, contact the school office or system administrator.'],
  ['A menu item I need is missing.', 'Your role does not include it. Ask the system administrator to review your roles (4.5).'],
  ['A list is empty.', 'Check the filters above the list — for example the session, term or class — and clear the search box.'],
  ['My change was not saved.', 'Look for a red message in the corner of the screen or a red outline on a field; it explains what needs correcting.'],
  ['A parent cannot see their child in the portal.', 'The parent must be linked to the pupil on the pupil\'s Parents tab (6.4), and both records must be active.'],
  ['A parent cannot see a report card.', 'Only published report cards are shown. Publish the card (8.3).'],
  ['I cannot delete a record.', 'Records with history are protected — for example a subject on a timetable, a supplier with purchases, or an item with movements. Deactivate them instead.'],
  ['A family was charged the wrong amount.', 'Give a discount on the invoice line (9.4) before payment, or refund the payment (9.6) and record the correct one.'],
  ['An online payment shows "Waiting for confirmation".', 'Wait a few minutes and click Check again. The bursar can also verify it from Finance → Online payments. Do not pay twice.'],
  ['Families are not receiving reminders.', 'Check their reminder settings (11.4) and contact details, then the Notification log for the reason (for example opted out, missing phone number, or read in the portal).'],
];
table(['Question', 'Answer'], faq, [3000, 6026]);

h1('Appendix D. Glossary');
table(['Term', 'Meaning'], [
  ['Session', 'An academic year, e.g. 2026/2027.'],
  ['Term', 'First, Second or Third Term within a session.'],
  ['Class (class arm)', 'A group of pupils at one level in one session, e.g. Primary 5A.'],
  ['Enrolment', 'A pupil\'s membership of a class for a session.'],
  ['Ward', 'A child linked to a parent or guardian.'],
  ['Register', 'The attendance record for one class on one day (or one lesson).'],
  ['Assessment', 'A test, assignment or exam with a maximum score and a weight.'],
  ['Fee schedule', 'The list of fees for one class level in one term.'],
  ['Invoice', 'The bill for one pupil generated from a fee schedule.'],
  ['Receipt', 'The record of a payment received, with its receipt number.'],
  ['Allocation', 'The part of a payment applied to a particular invoice.'],
  ['Reorder level', 'The stock quantity at or below which an item should be re-ordered.'],
  ['Quiet hours', 'Times when a family does not want email/SMS reminders.'],
  ['Audit trail', 'The permanent log of every change made in the portal.'],
], [2400, 6626]);

// ------------------------------------------------------------------ cover + document
const cover = [
  new Paragraph({ spacing: { before: 1800 }, alignment: AlignmentType.LEFT, children: [new TextRun({ text: 'NAIJA PRIME SCHOOL', bold: true, size: 28, color: GREEN, characterSpacing: 40 })] }),
  new Paragraph({ spacing: { before: 200 }, children: [new TextRun({ text: 'School Management Portal', size: 56, bold: true, color: '1F2A24' })] }),
  new Paragraph({ spacing: { before: 120, after: 200 }, border: { bottom: { style: BorderStyle.SINGLE, size: 18, color: GREEN, space: 8 } }, children: [new TextRun({ text: 'User Manual', size: 44, color: GREEN })] }),
  new Paragraph({ spacing: { before: 240 }, children: runs('For system administrators, head teachers, teachers, bursars, storekeepers, parents and pupils', { size: 26, color: GREY }) }),
  new Paragraph({ alignment: AlignmentType.CENTER, spacing: { before: 500, after: 500 }, children: [new ImageRun({ type: 'jpg', data: fs.readFileSync(path.join(SHOTS, 'dashboard-head.jpg')), transformation: { width: 560, height: Math.round(560 * SIZES['dashboard-head'][1] / SIZES['dashboard-head'][0]) }, altText: { title: 'Portal dashboard', description: 'Portal dashboard', name: 'cover' } })] }),
  new Paragraph({ children: [new TextRun({ text: 'Version 1.0  ·  September 2026', size: 22, color: GREY })] }),
  new Paragraph({ spacing: { before: 200 }, children: [new TextRun({ text: 'Produced by ', size: 24 }), new TextRun({ text: AUTHOR, size: 24, bold: true }), new TextRun({ text: ' for ', size: 24 }), new TextRun({ text: COMPANY, size: 24, bold: true })] }),
  new Paragraph({ children: [new PageBreak()] }),
  new Paragraph({ heading: HeadingLevel.HEADING_2, children: [new TextRun('Document information')] }),
];
const infoRows = [['Title', 'Naija Prime School Management Portal — User Manual'], ['Version', '1.0'], ['Date', 'September 2026'], ['Author', AUTHOR], ['Produced for', COMPANY], ['Covers', 'Portal release with Sprints 1–16 (user accounts to audit log)'], ['Audience', 'All users: SuperAdmin, HeadTeacher, Teacher, SchoolBursar, SchoolStoreKeeper, Parent, Student']];
const infoTable = new Table({
  width: { size: CONTENT_W, type: WidthType.DXA }, columnWidths: [2400, 6626],
  rows: infoRows.map(([k, v]) => new TableRow({ children: [
    new TableCell({ borders, width: { size: 2400, type: WidthType.DXA }, shading: { fill: LIGHT, type: ShadingType.CLEAR, color: 'auto' }, margins: { top: 60, bottom: 60, left: 100, right: 100 }, children: [new Paragraph({ children: [new TextRun({ text: k, bold: true })] })] }),
    new TableCell({ borders, width: { size: 6626, type: WidthType.DXA }, margins: { top: 60, bottom: 60, left: 100, right: 100 }, children: [new Paragraph(v)] }),
  ] })),
});
const copyright = new Paragraph({ spacing: { before: 300 }, children: runs(`© 2026 ${COMPANY} All rights reserved. This manual was produced by ${AUTHOR} for ${COMPANY} It describes the Naija Prime School Management Portal; screenshots show demonstration data only.`, { color: GREY, size: 20 }) });
const tocPage = [
  new Paragraph({ children: [new PageBreak()] }),
  new Paragraph({ heading: HeadingLevel.HEADING_2, children: [new TextRun('Contents')] }),
  new TableOfContents('Contents', { hyperlink: true, headingStyleRange: '1-2' }),
  new Paragraph({ children: runs('_If the contents list is empty, right-click it in Word and choose Update Field._', { color: GREY, size: 18 }) }),
];

const doc = new Document({
  creator: AUTHOR, company: COMPANY, title: 'Naija Prime School Management Portal — User Manual',
  description: `User manual produced by ${AUTHOR} for ${COMPANY}`,
  features: { updateFields: true },
  styles: {
    default: { document: { run: { font: FONT, size: 22 } } },
    paragraphStyles: [
      { id: 'Heading1', name: 'Heading 1', basedOn: 'Normal', next: 'Normal', quickFormat: true, run: { size: 36, bold: true, color: GREEN, font: FONT }, paragraph: { spacing: { before: 0, after: 240 }, outlineLevel: 0, pageBreakBefore: true } },
      { id: 'Heading2', name: 'Heading 2', basedOn: 'Normal', next: 'Normal', quickFormat: true, run: { size: 28, bold: true, color: '1F2A24', font: FONT }, paragraph: { spacing: { before: 280, after: 120 }, outlineLevel: 1, keepNext: true } },
      { id: 'Heading3', name: 'Heading 3', basedOn: 'Normal', next: 'Normal', quickFormat: true, run: { size: 24, bold: true, color: GREEN, font: FONT }, paragraph: { spacing: { before: 200, after: 80 }, outlineLevel: 2, keepNext: true } },
      { id: 'Caption', name: 'Caption', basedOn: 'Normal', next: 'Normal', run: { size: 18, italics: true, color: GREY }, paragraph: { spacing: { after: 200 } } },
    ],
  },
  numbering: { config: [
    { reference: 'steps', levels: [{ level: 0, format: LevelFormat.DECIMAL, text: '%1.', alignment: AlignmentType.LEFT, style: { paragraph: { indent: { left: 540, hanging: 360 } } } }] },
    { reference: 'bullets', levels: [{ level: 0, format: LevelFormat.BULLET, text: '•', alignment: AlignmentType.LEFT, style: { paragraph: { indent: { left: 540, hanging: 360 } } } }] },
  ] },
  sections: [
    { properties: { page: { size: { width: 11906, height: 16838 }, margin: { top: 1440, right: 1440, bottom: 1440, left: 1440 } } }, children: [...cover, infoTable, copyright, ...tocPage] },
    {
      properties: { page: { size: { width: 11906, height: 16838 }, margin: { top: 1440, right: 1440, bottom: 1440, left: 1440 }, pageNumbers: { start: 1 } } },
      headers: { default: new Header({ children: [new Paragraph({ border: { bottom: { style: BorderStyle.SINGLE, size: 4, color: 'C9D3CC', space: 4 } }, children: [new TextRun({ text: 'Naija Prime School Management Portal — User Manual', size: 18, color: GREY })] })] }) },
      footers: { default: new Footer({ children: [new Paragraph({ children: [
        new TextRun({ text: `Produced by ${AUTHOR} for ${COMPANY}`, size: 18, color: GREY }),
        new TextRun({ children: [new PositionalTab({ alignment: PositionalTabAlignment.RIGHT, relativeTo: PositionalTabRelativeTo.MARGIN, leader: PositionalTabLeader.NONE }), 'Page ', PageNumber.CURRENT, ' of ', PageNumber.TOTAL_PAGES], size: 18, color: GREY }),
      ] })] }) },
      children: body,
    },
  ],
});

Packer.toBuffer(doc).then(buf => {
  const out = path.join(__dirname, 'NaijaPrimeSchool-User-Manual.docx');
  fs.writeFileSync(out, buf);
  console.log(`Wrote ${path.relative(process.cwd(), out)} — ${figNo} figures, ${(buf.length / 1048576).toFixed(1)} MB`);
});
