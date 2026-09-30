// Module: Students, parents, links and enrolments (Sprints 3, 5b, 9 and 10).
import { check, login, BASE, stamp, today, addDays, dmy } from '../lib.mjs';

const M = 'Students & parents';
const PUPIL = { first: 'Adaeze', last: `Nnaji${stamp}`, user: `adaeze.nnaji${stamp.toLowerCase()}`, adm: `NPS/UAT/${stamp}` };
const PARENT = { first: 'Obioma', last: `Nnaji${stamp}`, user: `obioma.nnaji${stamp.toLowerCase()}`, phone: '08061234567' };
const y = today.getMonth() >= 8 ? today.getFullYear() : today.getFullYear() - 1;

async function canSignIn(t, user, password) {
  const ctx = await t.browser.newContext();
  const p = await ctx.newPage();
  await p.goto(BASE + '/Account/Login');
  await p.fill('#Input\\.Email', user);
  await p.fill('#Input\\.Password', password);
  await p.click('button[type=submit]');
  await p.waitForTimeout(2000);
  const ok = !p.url().includes('/Account/Login');
  await ctx.close();
  return ok;
}

async function openStudent(t, name) {
  await t.go('/students');
  await t.fill('Search', name);
  await t.settle(900);
  await t.rowAction(name, 'Edit');
  await t.settle();
}

async function openParent(t, name) {
  await t.go('/parents');
  await t.fill('Search', name);
  await t.settle(900);
  await t.rowAction(name, 'Edit');
  await t.settle();
}

export default [
  {
    id: 'FAM-01', module: M, sprint: 'Sprint 3', feature: 'Students', user: 'folake.adeyemi',
    title: 'Students list shows pupils as soon as it opens',
    steps: ['Sign in as the head teacher.', 'Open Family → Students (no filters).'],
    expected: 'All 30 pupils are listed (paged) with photo/initials, admission number, class, primary contact and status.',
    async run(t) {
      await t.go('/students');
      await t.page.waitForTimeout(2500);
      const n = await t.rowCount();
      check(n > 0, 'The grid is empty ("No students match your filters.") until a filter is changed');
      return `${n} pupils on the first page.`;
    },
  },
  {
    id: 'FAM-02', module: M, sprint: 'Sprint 3', feature: 'Students', user: 'folake.adeyemi',
    title: 'Search and filter pupils',
    steps: ['On Students type "Okafor" in Search.', 'Clear it; choose Class = Primary 5A.'],
    expected: 'Search finds Chiamaka and Tobenna Okafor; the class filter lists the 13 Primary 5A pupils.',
    async run(t) {
      await t.go('/students');
      await t.fill('Search', 'Okafor');
      await t.settle(900);
      const s = await t.rows().allInnerTexts();
      check(s.length === 2 && s.some(r => /Chiamaka/.test(r)) && s.some(r => /Tobenna/.test(r)), `Search returned ${s.length}`);
      await t.fill('Search', '');
      await t.pick('Class', 'Primary 5A', { exact: true });
      await t.settle(900);
      const n = await t.rowCount();
      check(n === 13, `Primary 5A filter returned ${n}`);
      return 'Okafor → Chiamaka and Tobenna; Primary 5A → 13 pupils.';
    },
  },
  {
    id: 'FAM-03', module: M, sprint: 'Sprint 3', feature: 'Create student', user: 'folake.adeyemi',
    title: 'Admission rules are enforced',
    steps: ['Open Add Student.', 'Enter admission number "NPS/26/001" (already used), date of birth after the admission date, and the other required fields.', 'Press Save student; then fix the date of birth and save again.'],
    expected: 'Date of birth after admission is refused; then the duplicate admission number is refused. No pupil is created.',
    async run(t) {
      await t.go('/students/new');
      await t.fill('Admission number', `NPS/${String(today.getFullYear() % 100).padStart(2, '0')}/001`);
      await t.date('Admission date', dmy(addDays(today, -20)));
      await t.fill('First name', 'Test');
      await t.fill('Last name', 'Duplicate');
      await t.date('Date of birth', dmy(addDays(today, -10)));
      await t.fill('Username', `dup${stamp}`);
      await t.fill('Email', `dup${stamp}@students.naijaprimeschool.ng`);
      await t.fill('Initial password', 'Family@123');
      await t.click('Save student');
      const dob = await t.expectMessage(/Date of birth must be earlier/i);
      await t.date('Date of birth', `3 Mar ${y - 9}`);
      await t.click('Save student');
      const dup = await t.expectMessage(/already exists/i);
      check(t.path() === '/students/new', 'Left the form');
      return `"${dob}" then "${dup}"`;
    },
  },
  {
    id: 'FAM-04', module: M, sprint: 'Sprints 3 & 9', feature: 'Create student', user: 'folake.adeyemi',
    title: 'Admit a new pupil with a class and a portal login',
    steps: ['Open Add Student.', `Admission number ${PUPIL.adm}, admission date today, ${PUPIL.first} ${PUPIL.last}, female, born 3 Mar ${y - 8}, blood group O+, state Enugu, allergy "Penicillin".`, 'Initial enrolment: Primary 3A.', `Portal sign-in: username ${PUPIL.user}, password Family@123.`, 'Press Save student.', 'Sign in as the new pupil.'],
    expected: 'The pupil\'s page opens; Enrolment history shows Primary 3A (Active); the pupil can sign in to the Student portal straight away.',
    async run(t) {
      await t.go('/students/new');
      await t.fill('Admission number', PUPIL.adm);
      await t.date('Admission date', today);
      await t.fill('First name', PUPIL.first);
      await t.fill('Last name', PUPIL.last);
      await t.date('Date of birth', `3 Mar ${y - 8}`);
      await t.pick('Gender', 'Female');
      await t.pick('Blood group', 'O+', { exact: true });
      await t.fill('State of origin', 'Enugu');
      await t.fill('Allergies', 'Penicillin');
      await t.pick('Class', 'Primary 3A', { exact: true });
      await t.fill('Username', PUPIL.user);
      await t.fill('Email', `${PUPIL.user}@students.naijaprimeschool.ng`);
      await t.fill('Initial password', 'Family@123');
      await t.click('Save student');
      await t.page.waitForURL(u => /\/students\/[0-9a-f-]{36}/.test(u.pathname), { timeout: 10000 });
      await t.settle();
      const h = await t.heading();
      await t.page.getByRole('tab', { name: /Enrolment history/ }).click();
      await t.page.waitForTimeout(500);
      const enrol = await t.text('.rz-tabview-panels, .rz-tabview-panel');
      check(/Primary 3A/.test(enrol) && /Active/i.test(enrol), `Enrolment tab: ${enrol.slice(0, 200)}`);
      check(await canSignIn(t, PUPIL.user, 'Family@123'), 'New pupil cannot sign in');
      return `Opened "${h}"; enrolled in Primary 3A (Active); pupil signed in to the portal.`;
    },
  },
  {
    id: 'FAM-05', module: M, sprint: 'Sprint 3', feature: 'Edit student', user: 'folake.adeyemi',
    title: 'Update a pupil\'s medical notes',
    steps: [`Open ${PUPIL.first} ${PUPIL.last} from Students (Edit).`, 'Profile tab: Medical notes "Asthmatic – inhaler kept in the sick bay"; Save changes.', 'Reload the page.'],
    expected: '"Student profile updated." and the note is still there after reloading.',
    async run(t) {
      await openStudent(t, PUPIL.last);
      await t.fill('Medical notes', 'Asthmatic – inhaler kept in the sick bay');
      await t.click('Save changes');
      const msg = await t.expectToast(/updated/i);
      await t.page.reload();
      await t.settle();
      const v = await t.field('Medical notes').locator('textarea, input').first().inputValue();
      check(/inhaler/.test(v), `After reload: "${v}"`);
      return `"${msg}"; persisted.`;
    },
  },
  {
    id: 'FAM-06', module: M, sprint: 'Sprint 5b', feature: 'Pupil photo', user: 'folake.adeyemi',
    title: 'Upload a pupil photograph',
    steps: ['On the pupil\'s page open the Photo tab.', 'Choose new photo → select a JPEG under 2 MB.', 'Open Students and search for the pupil.'],
    expected: '"Uploaded" confirmation; the photo replaces the initials tile on the pupil page and in the Students list.',
    async run(t) {
      await openStudent(t, PUPIL.last);
      await t.page.getByRole('tab', { name: 'Photo' }).click();
      await t.page.waitForTimeout(400);
      // A small real JPEG: a screenshot of the school logo badge.
      const jpeg = await t.page.locator('.nps-brand-badge').screenshot({ type: 'jpeg' });
      await t.page.setInputFiles('#nps-photo-input', { name: 'adaeze.jpg', mimeType: 'image/jpeg', buffer: jpeg });
      const msg = await t.expectToast(/Uploaded/i);
      await t.go('/students');
      await t.fill('Search', PUPIL.last);
      await t.settle(900);
      const img = await t.row(PUPIL.last).locator('img').count();
      check(img > 0, 'List still shows the initials tile');
      return `"${msg}"; photo shown in the Students list.`;
    },
  },
  {
    id: 'FAM-07', module: M, sprint: 'Sprint 3', feature: 'Parents', user: 'folake.adeyemi',
    title: 'Parents list shows parents as soon as it opens; search by phone',
    steps: ['Open Family → Parents.', 'Type "08035000000" (Mrs. Okafor\'s phone) in Search.'],
    expected: 'All 20 parents are listed on opening with contact details and pupil count; the phone search finds Ngozi Okafor with 2 pupils.',
    async run(t) {
      await t.go('/parents');
      await t.page.waitForTimeout(2500);
      const n = await t.rowCount();
      await t.fill('Search', '08035000000');
      await t.settle(900);
      const r = await t.rows().allInnerTexts();
      check(r.length === 1 && /Ngozi/.test(r[0]), `Phone search returned ${r.length}`);
      check(n > 0, 'The grid is empty ("No parents match your filters.") until a filter is changed');
      return `${n} parents on open; phone search → "${r[0].replace(/\s+/g, ' ')}"`;
    },
  },
  {
    id: 'FAM-08', module: M, sprint: 'Sprints 3 & 9', feature: 'Create parent', user: 'folake.adeyemi',
    title: 'Register a parent with a portal login',
    steps: ['Open Add Parent.', `Mr. ${PARENT.first} ${PARENT.last}, male, married, phone ${PARENT.phone}, email, occupation "Accountant".`, `Portal sign-in: username ${PARENT.user}, password Family@123.`, 'Press Save parent.', 'Sign in as the parent.'],
    expected: 'The parent\'s page opens; the parent can sign in and sees the Parent portal with no wards yet.',
    async run(t) {
      await t.go('/parents/new');
      await t.pick('Title', 'Mr.', { exact: true });
      await t.fill('First name', PARENT.first);
      await t.fill('Last name', PARENT.last);
      await t.pick('Gender', 'Male');
      await t.pick('Marital status', 'Married');
      await t.fill('Primary phone', PARENT.phone);
      await t.fill('Email', `${PARENT.user}@example.ng`);
      await t.fill('Occupation', 'Accountant');
      await t.fill('Username', PARENT.user);
      await t.fill('Initial password', 'Family@123');
      await t.click('Save parent');
      await t.page.waitForURL(u => /\/parents\/[0-9a-f-]{36}/.test(u.pathname), { timeout: 10000 });
      await t.settle();
      const h = await t.heading();
      const ctx = await login(t.browser, PARENT.user, { password: 'Family@123' });
      const p = await ctx.newPage();
      await p.goto(BASE + '/portal/parent');
      await p.waitForTimeout(1500);
      const body = (await p.locator('body').innerText()).replace(/\s+/g, ' ');
      await ctx.close();
      check(/Parent portal/.test(body), 'Parent portal did not open');
      return `Opened "${h}"; parent signed in; portal: "${body.match(/My wards.{0,80}/)?.[0] ?? ''}"`;
    },
  },
  {
    id: 'FAM-09', module: M, sprint: 'Sprint 3', feature: 'Parent links', user: 'folake.adeyemi',
    title: 'Link a parent to a pupil as primary contact',
    steps: [`Open ${PUPIL.first}'s page → Parents tab → Link a parent.`, `Parent ${PARENT.first} ${PARENT.last}, relationship Father, Primary contact on, Authorised to collect on.`, 'Press Save link.', 'Sign in as the parent and open My wards.'],
    expected: '"Parent link saved."; the tab reads Parents (1) with a Primary badge; the parent now sees the pupil under My wards.',
    async run(t) {
      await openStudent(t, PUPIL.last);
      await t.page.getByRole('tab', { name: /Parents/ }).click();
      await t.page.waitForTimeout(400);
      await t.click('Link a parent');
      await t.pick('Parent', PARENT.last);
      await t.pick('Relationship', 'Father', { exact: true });
      const primary = t.field('Primary contact').locator('.rz-switch');
      if (!(await primary.getAttribute('class')).includes('rz-switch-checked')) await primary.click();
      await t.click('Save link');
      const msg = await t.expectToast(/link saved/i);
      await t.settle();
      const tab = await t.page.getByRole('tab', { name: /Parents/ }).innerText();
      const row = await t.row(PARENT.last).innerText();
      check(/\(1\)/.test(tab) && /Primary/i.test(row), `Tab "${tab}", row "${row}"`);
      const ctx = await login(t.browser, PARENT.user, { password: 'Family@123' });
      const p = await ctx.newPage();
      await p.goto(BASE + '/portal/parent');
      await p.waitForTimeout(1500);
      const wards = await p.locator('body').innerText();
      await ctx.close();
      check(wards.includes(PUPIL.first), 'Pupil not shown under My wards');
      return `"${msg}"; ${tab.trim()}; parent now sees ${PUPIL.first} under My wards.`;
    },
  },
  {
    id: 'FAM-10', module: M, sprint: 'Sprint 3', feature: 'Parent links', user: 'folake.adeyemi',
    title: 'Mark a guardian as not authorised to collect the pupil',
    steps: ['On the pupil\'s Parents tab press Edit link on the father.', 'Turn "Authorised to collect" off; add note "Collection by mother only (court order)"; Save link.'],
    expected: 'A "Cannot pick up" badge appears next to the parent with the note.',
    async run(t) {
      await openStudent(t, PUPIL.last);
      await t.page.getByRole('tab', { name: /Parents/ }).click();
      await t.page.waitForTimeout(400);
      await t.rowAction(PARENT.last, 'Edit link');
      const sw = t.field('Authorised to collect').locator('.rz-switch');
      if ((await sw.getAttribute('class')).includes('rz-switch-checked')) await sw.click();
      await t.fill('Notes', 'Collection by mother only (court order)');
      await t.click('Save link');
      await t.expectToast(/saved/i);
      await t.settle();
      const row = await t.row(PARENT.last).innerText();
      check(/Cannot pick up/i.test(row) && /court order/.test(row), `Row: ${row}`);
      return `Row now reads "${row.replace(/\s+/g, ' ')}"`;
    },
  },
  {
    id: 'FAM-11', module: M, sprint: 'Sprint 3', feature: 'Edit parent', user: 'folake.adeyemi',
    title: 'Update a parent\'s contact details',
    steps: [`Open ${PARENT.first} ${PARENT.last} from Parents (Edit).`, 'Change Alternate phone to 07012345678 and Employer to "Access Bank"; Save changes.'],
    expected: '"Parent profile updated."; the Pupils tab lists the linked pupil.',
    async run(t) {
      await openParent(t, PARENT.last);
      await t.fill('Alternate phone', '07012345678');
      await t.fill('Employer', 'Access Bank');
      await t.click('Save changes');
      const msg = await t.expectToast(/updated/i);
      const tabs = await t.page.getByRole('tab').allInnerTexts();
      return `"${msg}"; tabs: ${tabs.join(' | ')}`;
    },
  },
  {
    id: 'FAM-12', module: M, sprint: 'Sprint 3', feature: 'Enrolments', user: 'folake.adeyemi',
    title: 'Enrolments register: filter and withdraw',
    steps: ['Open Family → Enrolments.', 'Filter Class = Primary 3A.', `Press Withdraw on ${PUPIL.first} ${PUPIL.last} and confirm.`],
    expected: 'Primary 3A shows its pupils; after withdrawal the row shows status Withdrawn with today\'s date.',
    async run(t) {
      await t.go('/enrolments');
      await t.pick('Class', 'Primary 3A', { exact: true });
      await t.settle(900);
      const before = await t.rowCount();
      await t.rowAction(PUPIL.last, 'Withdraw');
      await t.confirm('Withdraw');
      const msg = await t.expectToast(/Withdrawn/i);
      await t.settle();
      const row = await t.row(PUPIL.last).innerText();
      check(/Withdrawn/i.test(row), `Row: ${row}`);
      return `${before} Primary 3A enrolments; "${msg}"; row: ${row.replace(/\s+/g, ' ')}`;
    },
  },
  {
    id: 'FAM-13', module: M, sprint: 'Sprint 3', feature: 'Enrolments', user: 'folake.adeyemi',
    title: 'Re-enrol a pupil in another class',
    steps: [`Open ${PUPIL.first}'s page → Enrolment history → New enrolment.`, 'Class Primary 4A, enrolment date today; press Enrol.', 'Try to enrol again in Primary 2A.'],
    expected: 'The Primary 4A enrolment is Active; a second active enrolment in the same session is refused.',
    async run(t) {
      await openStudent(t, PUPIL.last);
      await t.page.getByRole('tab', { name: /Enrolment history/ }).click();
      await t.page.waitForTimeout(400);
      await t.click('New enrolment');
      await t.pick('Class', 'Primary 4A', { exact: true });
      await t.date('Enrolment date', today);
      await t.click('Enrol', { exact: true });
      const ok = await t.expectToast(/Enrolled/i);
      await t.settle();
      await t.click('New enrolment');
      await t.pick('Class', 'Primary 2A', { exact: true });
      await t.date('Enrolment date', today);
      await t.click('Enrol', { exact: true });
      const refused = await t.expectToast(/already/i);
      return `"${ok}"; second enrolment refused: "${refused}"`;
    },
  },
  {
    id: 'FAM-14', module: M, sprint: 'Sprint 3', feature: 'Deactivate', user: 'folake.adeyemi',
    title: 'A deactivated parent can no longer sign in to the portal',
    steps: [`Open ${PARENT.first} ${PARENT.last}'s page and press Deactivate.`, 'Try to sign in as the parent.', 'Press Activate.'],
    expected: 'The parent shows Inactive and the portal login is refused while inactive; after Activate the parent can sign in again.',
    async run(t) {
      await openParent(t, PARENT.last);
      await t.click('Deactivate');
      await t.toast();
      const blocked = !(await canSignIn(t, PARENT.user, 'Family@123'));
      await t.page.reload();
      await t.settle();
      await t.click('Activate');
      await t.toast();
      check(blocked, 'The deactivated parent could still sign in to the portal (only the parent record was deactivated, not the login).');
      check(await canSignIn(t, PARENT.user, 'Family@123'), 'Could not sign in after reactivation');
      return 'Sign-in refused while inactive; works again after Activate.';
    },
  },
  {
    id: 'FAM-15', module: M, sprint: 'Sprint 10', feature: 'Delete parent', user: 'folake.adeyemi',
    title: 'Deleting a parent: blocked while linked, then retires the login',
    steps: [`In Parents press Delete on ${PARENT.first} ${PARENT.last} and confirm.`, `Unlink the parent from ${PUPIL.first}'s Parents tab.`, 'Delete the parent again (same screen session).', 'Try to sign in as the parent.'],
    expected: 'First delete is refused (active link); after unlinking, the delete succeeds and the parent\'s portal login stops working.',
    async run(t) {
      await t.go('/parents');
      await t.fill('Search', PARENT.last);
      await t.settle(900);
      await t.rowAction(PARENT.last, 'Delete');
      await t.confirm(/Delete|Yes|Ok/i);
      const refused = await t.expectToast(/Cannot delete/i);
      await openStudent(t, PUPIL.last);
      await t.page.getByRole('tab', { name: /Parents/ }).click();
      await t.page.waitForTimeout(400);
      await t.rowAction(PARENT.last, 'Unlink');
      await t.confirm('Unlink');
      await t.expectToast(/Unlinked/i);
      await t.go('/parents');
      await t.fill('Search', PARENT.last);
      await t.settle(900);
      await t.rowAction(PARENT.last, 'Delete');
      await t.confirm(/Delete|Yes|Ok/i);
      const ok = await t.expectToast(/removed|deleted/i);
      check(!(await canSignIn(t, PARENT.user, 'Family@123')), 'Deleted parent can still sign in');
      return `Refused: "${refused}"; after unlinking: "${ok}"; login retired.`;
    },
  },
];
