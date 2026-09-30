// Module: Academic structure (Sprint 2) — sessions, terms, classes, subjects, periods, timetable.
import { check, today } from '../lib.mjs';

const M = 'Academics';
const S = 'Sprint 2';
const y = today.getMonth() >= 8 ? today.getFullYear() : today.getFullYear() - 1;
const CUR = `${y}/${y + 1}`;
const NEXT = `${y + 1}/${y + 2}`;

export default [
  {
    id: 'ACA-01', module: M, sprint: S, feature: 'Sessions', user: 'folake.adeyemi',
    title: 'Sessions list shows the current session with its terms and classes',
    steps: ['Sign in as the head teacher.', 'Open Academics → Sessions.'],
    expected: `${y - 1}/${y} and ${CUR} are listed; ${CUR} carries the "Current" badge with 3 terms and 7 classes.`,
    async run(t) {
      await t.go('/sessions');
      const row = await t.row(CUR).innerText();
      check(/Current/i.test(row), `${CUR} row: ${row}`);
      check(/\b3\b/.test(row) && /\b7\b/.test(row), `${CUR} row does not show 3 terms / 7 classes: ${row.replace(/\s+/g, ' ')}`);
      check(await t.row(`${y - 1}/${y}`).count(), 'Previous session missing');
      return `Current session row: "${row.replace(/\s+/g, ' ')}"`;
    },
  },
  {
    id: 'ACA-02', module: M, sprint: S, feature: 'Sessions', user: 'folake.adeyemi',
    title: 'Session dates are validated',
    steps: ['On Sessions press New session.', `Name "${NEXT}", start 1 Sep ${y + 2}, end 31 Jul ${y + 1} (before the start).`, 'Press Save.'],
    expected: '"End date must be after start date." and nothing is saved.',
    async run(t) {
      await t.go('/sessions');
      await t.click('New session');
      await t.fill('Name', NEXT);
      await t.date('Start date', `1 Sep ${y + 2}`);
      await t.date('End date', `31 Jul ${y + 1}`);
      await t.click('Save', { exact: true });
      const msg = await t.expectToast(/after start/i);
      return `Refused: "${msg}"`;
    },
  },
  {
    id: 'ACA-03', module: M, sprint: S, feature: 'Sessions', user: 'folake.adeyemi',
    title: 'Create next year\'s session',
    steps: ['On Sessions press New session.', `Name "${NEXT}", start 1 Sep ${y + 1}, end 31 Jul ${y + 2}; leave "Mark current" off.`, 'Press Save.'],
    expected: `"Session '${NEXT}' saved." and it appears in the list without the Current badge; ${CUR} stays current.`,
    async run(t) {
      await t.go('/sessions');
      await t.click('New session');
      await t.fill('Name', NEXT);
      await t.date('Start date', `1 Sep ${y + 1}`);
      await t.date('End date', `31 Jul ${y + 2}`);
      await t.click('Save', { exact: true });
      const msg = await t.expectToast(/saved/i);
      await t.settle();
      const row = await t.row(NEXT).innerText();
      check(!/Current/i.test(row.replace(/Mark current/i, '')), `New session marked current: ${row}`);
      check(/Current/i.test(await t.row(CUR).innerText()), 'Current session changed');
      return `"${msg}"; ${NEXT} listed, ${CUR} still current.`;
    },
  },
  {
    id: 'ACA-04', module: M, sprint: S, feature: 'Sessions', user: 'folake.adeyemi',
    title: 'Duplicate session name is rejected',
    steps: ['Press New session.', `Enter name "${CUR}" (already exists) with valid dates.`, 'Press Save.'],
    expected: `"A session named '${CUR}' already exists."`,
    async run(t) {
      await t.go('/sessions');
      await t.click('New session');
      await t.fill('Name', CUR);
      await t.date('Start date', `1 Sep ${y}`);
      await t.date('End date', `31 Jul ${y + 1}`);
      await t.click('Save', { exact: true });
      return `Refused: "${await t.expectToast(/already exists/i)}"`;
    },
  },
  {
    id: 'ACA-05', module: M, sprint: S, feature: 'Sessions', user: 'folake.adeyemi',
    title: 'Switch the current session and back',
    steps: [`Press "Mark current" on ${y - 1}/${y}.`, 'Check the badges.', `Press "Mark current" on ${CUR}.`],
    expected: 'Only one session is ever marked Current; the switch is confirmed each time and the list updates immediately.',
    async run(t) {
      await t.go('/sessions');
      await t.rowAction(`${y - 1}/${y}`, 'Mark current');
      const m1 = await t.expectToast(/current session/i);
      await t.settle();
      const rows = await t.rows().allInnerTexts();
      const current = rows.filter(r => /current/i.test(r));
      check(current.length === 1 && current[0].includes(`${y - 1}/${y}`), `Current badges after switch: ${current.length}`);
      await t.rowAction(CUR, 'Mark current');
      await t.expectToast(/current session/i);
      await t.settle();
      check(/Current/i.test(await t.row(CUR).innerText()), `${CUR} not current again`);
      return `"${m1}" then switched back; exactly one Current badge each time.`;
    },
  },
  {
    id: 'ACA-06', module: M, sprint: S, feature: 'Terms', user: 'folake.adeyemi',
    title: 'Terms list and filter by session',
    steps: ['Open Academics → Terms.', `Filter by session ${CUR}.`],
    expected: 'First, Second and Third Term are listed with dates; the term containing today is marked Current.',
    async run(t) {
      await t.go('/terms');
      await t.pick('Filter by session', CUR, { exact: true });
      await t.settle();
      const rows = await t.rows().allInnerTexts();
      check(rows.length === 3, `Expected 3 terms, got ${rows.length}`);
      const cur = rows.filter(r => /current/i.test(r));
      check(cur.length === 1 && /First Term/.test(cur[0]), `Current term: ${cur.join(' / ')}`);
      return `3 terms; current: "${cur[0].replace(/\s+/g, ' ')}"`;
    },
  },
  {
    id: 'ACA-07', module: M, sprint: S, feature: 'Terms', user: 'folake.adeyemi',
    title: 'Add a term to next year\'s session',
    steps: ['On Terms press New term.', `Session ${NEXT}, Term "First Term", 8 Sep ${y + 1} – 18 Dec ${y + 1}.`, 'Press Save.', 'Repeat with the same values.'],
    expected: '"Term saved." the first time; the repeat is refused with "That term already exists for this session."',
    async run(t) {
      await t.go('/terms');
      for (const attempt of [1, 2]) {
        await t.click('New term');
        await t.pick('Session', NEXT, { exact: true });
        await t.pick('Term', 'First Term');
        await t.date('Start date', `8 Sep ${y + 1}`);
        await t.date('End date', `18 Dec ${y + 1}`);
        await t.click('Save', { exact: true });
        if (attempt === 1) await t.expectToast(/Term saved/i);
        else return `Saved once; duplicate refused: "${await t.expectToast(/already exists/i)}"`;
        await t.settle();
      }
    },
  },
  {
    id: 'ACA-08', module: M, sprint: S, feature: 'Sessions', user: 'folake.adeyemi',
    title: 'A session with terms cannot be deleted',
    steps: [`On Sessions press Delete on ${NEXT} and confirm.`, `On Terms delete ${NEXT} First Term and confirm.`, `Delete ${NEXT} again.`],
    expected: 'First delete is refused ("Cannot delete a session that still has terms or classes attached."); after its term is removed the session deletes.',
    async run(t) {
      await t.go('/sessions');
      await t.rowAction(NEXT, 'Delete');
      await t.confirm('Delete');
      const refused = await t.expectToast(/Cannot delete/i);
      await t.go('/terms');
      await t.pick('Filter by session', NEXT, { exact: true });
      await t.settle();
      await t.rowAction('First Term', 'Delete');
      await t.confirm('Delete');
      await t.expectToast(/deleted/i);
      await t.go('/sessions');
      await t.rowAction(NEXT, 'Delete');
      await t.confirm('Delete');
      const ok = await t.expectToast(/deleted/i);
      await t.settle();
      check(!(await t.row(NEXT).count()), 'Session still listed');
      return `Refused: "${refused}"; after removing the term: "${ok}"`;
    },
  },
  {
    id: 'ACA-09', module: M, sprint: S, feature: 'Classes', user: 'folake.adeyemi',
    title: 'Create a class with a class teacher',
    steps: ['Open Academics → Classes and press New class.', `Name "Primary 3B", level Primary 3, session ${CUR}, class teacher Grace Udoh.`, 'Press Save.'],
    expected: '"Class \'Primary 3B\' saved." and the grid shows its level, session and class teacher.',
    async run(t) {
      await t.go('/classes');
      await t.click('New class');
      await t.fill('Name', 'Primary 3B');
      await t.pick('Class level', 'Primary 3', { exact: true });
      await t.pick('Session', CUR, { exact: true });
      await t.pick('Class teacher', 'Grace');
      await t.click('Save', { exact: true });
      const msg = await t.expectToast(/saved/i);
      await t.settle();
      const row = await t.row('Primary 3B').innerText();
      check(/Primary 3/.test(row) && /Grace/.test(row), `Row: ${row}`);
      return `"${msg}"; row: ${row.replace(/\s+/g, ' ')}`;
    },
  },
  {
    id: 'ACA-10', module: M, sprint: S, feature: 'Classes', user: 'folake.adeyemi',
    title: 'Class names are unique within a session; classes on a timetable cannot be deleted',
    steps: ['Press New class; name "Primary 5A", level Primary 5, current session; Save.', 'Press Delete on Primary 5A and confirm.'],
    expected: 'Duplicate refused ("A class named \'Primary 5A\' already exists in this session."); delete refused ("Cannot delete a class that has timetable entries.").',
    async run(t) {
      await t.go('/classes');
      await t.click('New class');
      await t.fill('Name', 'Primary 5A');
      await t.pick('Class level', 'Primary 5', { exact: true });
      await t.pick('Session', CUR, { exact: true });
      await t.click('Save', { exact: true });
      const dup = await t.expectToast(/already exists/i);
      await t.click('Cancel');
      await t.rowAction('Primary 5A', 'Delete');
      await t.confirm('Delete');
      const del = await t.expectToast(/Cannot delete/i);
      return `Duplicate: "${dup}"; delete: "${del}"`;
    },
  },
  {
    id: 'ACA-11', module: M, sprint: S, feature: 'Subjects', user: 'folake.adeyemi',
    title: 'Create, validate and delete a subject',
    steps: ['Open Academics → Subjects; press New subject.', 'Name "French", code "MTH"; Save (code already used).', 'Change code to "FRN"; Save.', 'Press Delete on Mathematics and confirm.', 'Press Delete on French and confirm.'],
    expected: 'Code clash refused; French saved; Mathematics cannot be deleted because it is on a timetable; French is deleted.',
    async run(t) {
      await t.go('/subjects');
      await t.click('New subject');
      await t.fill('Name', 'French');
      await t.fill('Code', 'MTH');
      await t.click('Save', { exact: true });
      const clash = await t.expectToast(/already in use/i);
      await t.fill('Code', 'FRN');
      await t.click('Save', { exact: true });
      await t.expectToast(/saved/i);
      await t.settle();
      check(await t.row('French').count(), 'French not listed');
      await t.rowAction('Mathematics', 'Delete');
      await t.confirm('Delete');
      const inUse = await t.expectToast(/Cannot delete/i);
      await t.rowAction('French', 'Delete');
      await t.confirm('Delete');
      await t.expectToast(/deleted/i);
      return `Clash: "${clash}"; in use: "${inUse}"; French created then deleted.`;
    },
  },
  {
    id: 'ACA-12', module: M, sprint: S, feature: 'Timetable periods', user: 'folake.adeyemi',
    title: 'Timetable periods: list, add, validate, delete',
    steps: ['Open Academics → Periods.', 'Press New period: "Clubs", order 10, 15:00–14:00; Save.', 'Correct to 14:00–15:00; Save.', 'Delete "Clubs".'],
    expected: 'Seven teaching periods plus Short Break and Lunch are listed; an end time before the start is refused; "Clubs" is saved and then deleted.',
    async run(t) {
      await t.go('/timetable-periods');
      const rows = await t.rows().allInnerTexts();
      check(rows.length >= 9 && rows.some(r => /Lunch/.test(r)) && rows.some(r => /Short Break/.test(r)), `Periods listed: ${rows.length}`);
      await t.click('New period');
      await t.fill('Name', 'Clubs');
      await t.fill('Display order', '10');
      await t.date('Start time', '15:00');
      await t.date('End time', '14:00');
      await t.click('Save', { exact: true });
      const bad = await t.expectToast(/after start/i);
      await t.date('Start time', '14:00');
      await t.date('End time', '15:00');
      await t.click('Save', { exact: true });
      await t.expectToast(/saved/i);
      await t.settle();
      check(await t.row('Clubs').count(), 'Clubs not listed');
      await t.rowAction('Clubs', 'Delete');
      await t.confirm('Delete');
      await t.expectToast(/deleted/i);
      return `${rows.length} periods listed; bad times refused ("${bad}"); Clubs added and deleted.`;
    },
  },
  {
    id: 'ACA-13', module: M, sprint: S, feature: 'Timetable', user: 'folake.adeyemi',
    title: 'View a class timetable',
    steps: ['Open Academics → Timetable.', 'Choose the current First Term and class Primary 5A.'],
    expected: 'A Monday–Friday grid with every period; all 35 teaching slots show subject code, name and teacher; break rows are shaded.',
    async run(t) {
      await t.go('/timetable');
      await t.pick('Term', 'First Term');
      await t.pick('Class', 'Primary 5A', { exact: true });
      await t.settle();
      const filled = await t.page.locator('.nps-tt-filled').count();
      const days = await t.page.locator('.nps-timetable-grid thead th').allInnerTexts();
      check(filled === 35, `Expected 35 lessons, found ${filled}`);
      return `${filled} lessons across ${days.slice(1).join(', ')}.`;
    },
  },
  {
    id: 'ACA-14', module: M, sprint: S, feature: 'Timetable', user: 'folake.adeyemi',
    title: 'Assign a lesson by clicking an empty cell',
    steps: ['Choose First Term and Primary 3A.', 'Click Monday / Period 1 ("+ assign").', 'Subject English Language, teacher Grace Udoh, room "Block A, Room 3"; Save.'],
    expected: '"Timetable updated." and the cell shows ENG / English Language / the teacher.',
    async run(t) {
      await t.go('/timetable');
      await t.pick('Term', 'First Term');
      await t.pick('Class', 'Primary 3A', { exact: true });
      await t.settle();
      await t.page.locator('.nps-timetable-grid tbody tr').nth(0).locator('td').nth(0).click();
      await t.page.waitForTimeout(500);
      await t.pick('Subject', 'English Language');
      await t.pick('Teacher', 'Grace');
      await t.fill('Room', 'Block A, Room 3');
      await t.click('Save', { exact: true });
      const msg = await t.expectToast(/Timetable updated/i);
      await t.settle();
      const cell = await t.page.locator('.nps-timetable-grid tbody tr').nth(0).locator('td').nth(0).innerText();
      check(/ENG/.test(cell) && /Grace/.test(cell), `Cell reads: ${cell}`);
      return `"${msg}"; cell shows "${cell.replace(/\s+/g, ' ')}"`;
    },
  },
  {
    id: 'ACA-15', module: M, sprint: S, feature: 'Timetable', user: 'folake.adeyemi',
    title: 'A teacher cannot be double-booked in the same period',
    pre: 'Seed data: Mr. Chinedu Okeke teaches Primary 5A Mathematics on Monday, Period 2.',
    steps: ['Choose First Term and Primary 4A.', 'Click Monday / Period 2.', 'Subject Mathematics, teacher Chinedu Okeke; Save.'],
    expected: 'The lesson is refused with a clear message that Mr. Okeke is already teaching Primary 5A at that time.',
    async run(t) {
      await t.go('/timetable');
      await t.pick('Term', 'First Term');
      await t.pick('Class', 'Primary 4A', { exact: true });
      await t.settle();
      await t.page.locator('.nps-timetable-grid tbody tr').nth(1).locator('td').nth(0).click();
      await t.page.waitForTimeout(500);
      await t.pick('Subject', 'Mathematics');
      await t.pick('Teacher', 'Chinedu');
      await t.click('Save', { exact: true });
      const msg = await t.toast();
      check(!/Timetable updated/i.test(msg), 'The clash was accepted: Mr. Okeke is now timetabled in Primary 5A and Primary 4A on Monday, Period 2.');
      return `Refused: "${msg}"`;
    },
  },
  {
    id: 'ACA-16', module: M, sprint: S, feature: 'Timetable', user: 'chinedu.okeke',
    title: 'Teacher can view the class timetable',
    steps: ['Sign in as teacher chinedu.okeke.', 'Open Academics → Timetable; choose First Term and Primary 5A.'],
    expected: 'The full Primary 5A week is shown.',
    async run(t) {
      await t.go('/timetable');
      await t.pick('Term', 'First Term');
      await t.pick('Class', 'Primary 5A', { exact: true });
      await t.settle();
      const filled = await t.page.locator('.nps-tt-filled').count();
      check(filled === 35, `Teacher sees ${filled} lessons`);
      return `Teacher sees all ${filled} lessons.`;
    },
  },
];
