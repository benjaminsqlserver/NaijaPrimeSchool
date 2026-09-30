// Module: Attendance (Sprint 4) — daily register, subject attendance, summary.
// Run on a school day (Mon–Fri): the seed leaves today's register unopened.
import { check, today, addDays, dmy } from '../lib.mjs';

const M = 'Attendance';
const S = 'Sprint 4';
let lastSchoolDay = addDays(today, -1);
while ([0, 6].includes(lastSchoolDay.getDay())) lastSchoolDay = addDays(lastSchoolDay, -1);

async function openDaily(t, cls, date) {
  await t.go('/attendance/daily');
  await t.pick('Class', cls, { exact: true });
  await t.date('Date', date);
  await t.settle();
}
const badges = async t => (await t.page.locator('.rz-badge').allInnerTexts()).join(' ').replace(/\s+/g, ' ');

export default [
  {
    id: 'ATT-01', module: M, sprint: S, feature: 'Daily register', user: 'chinedu.okeke',
    title: 'Open today\'s class register',
    steps: ['Sign in as teacher chinedu.okeke.', 'Open Attendance → Daily attendance.', `Class Primary 5A, date today (${dmy(today)}).`, 'Press Open register.'],
    expected: '"Register opened."; all 13 enrolled pupils are pre-loaded as Present (Present: 13, Total: 13), with the term and session shown.',
    async run(t) {
      await openDaily(t, 'Primary 5A', today);
      await t.see('No register found');
      await t.click('Open register');
      await t.expectToast(/Register opened/i);
      await t.settle();
      const b = await badges(t);
      check(/Present: 13/i.test(b) && /Total: 13/i.test(b), `Badges: ${b}`);
      return `Register opened; ${b}`;
    },
  },
  {
    id: 'ATT-02', module: M, sprint: S, feature: 'Daily register', user: 'chinedu.okeke',
    title: 'Mark absences and late arrivals, then save',
    steps: ['On today\'s Primary 5A register set Kamsi Eze = Absent with remark "No word from parent".', 'Set Aminu Ibrahim = Late, arrival 08:25.', 'Press Save changes.'],
    expected: '"Attendance saved."; counts become Present 12 (late arrivals count as present), Absent 1, Late 1; the arrival time and remark are kept.',
    async run(t) {
      await openDaily(t, 'Primary 5A', today);
      await t.pick(t.row('Kamsi Eze'), 'Absent', { exact: true });
      await t.row('Kamsi Eze').locator('input').last().fill('No word from parent');
      await t.row('Kamsi Eze').locator('input').last().press('Tab');
      await t.pick(t.row('Aminu Ibrahim'), 'Late', { exact: true });
      const arrival = t.row('Aminu Ibrahim').locator('input[placeholder="HH:mm"]');
      await arrival.fill('08:25');
      await arrival.press('Tab');
      await t.click('Save changes');
      const msg = await t.expectToast(/Attendance saved/i);
      await t.settle();
      const b = await badges(t);
      check(/Present: 12/i.test(b) && /Absent: 1\b/i.test(b) && /Late: 1\b/i.test(b), `Badges: ${b}`);
      const late = await t.row('Aminu Ibrahim').locator('input[placeholder="HH:mm"]').inputValue();
      check(late === '08:25', `Arrival time shows "${late}"`);
      return `"${msg}"; ${b}; arrival 08:25 kept.`;
    },
  },
  {
    id: 'ATT-03', module: M, sprint: S, feature: 'Daily register', user: 'chinedu.okeke',
    title: 'Submit the register; it locks',
    steps: ['On today\'s Primary 5A register press Submit register.'],
    expected: '"Register submitted."; a "Submitted <date time>" badge and a Reopen button replace Save/Submit; the status drop-downs are disabled.',
    async run(t) {
      await openDaily(t, 'Primary 5A', today);
      await t.click('Submit register');
      await t.expectToast(/Register submitted/i, 12000);
      await t.settle();
      const b = await badges(t);
      const disabled = await t.page.locator('.rz-data-row .rz-dropdown.rz-state-disabled, .rz-data-row .rz-dropdown[disabled], .rz-data-row .rz-disabled').count();
      check(/Submitted/i.test(b), `No Submitted badge: ${b}`);
      check(disabled >= 13, `Only ${disabled} status drop-downs are disabled`);
      return `${b}; ${disabled} drop-downs disabled.`;
    },
  },
  {
    id: 'ATT-04', module: M, sprint: S, feature: 'Daily register', user: 'folake.adeyemi',
    title: 'Head teacher reopens a submitted register',
    steps: ['Sign in as the head teacher; open today\'s Primary 5A register.', 'Press Reopen.', 'Kamsi Eze\'s mother phones in: change him to Present; Submit register again.'],
    expected: '"Register reopened for editing."; after the change and re-submission the counts show Present 13, Absent 0.',
    async run(t) {
      await openDaily(t, 'Primary 5A', today);
      await t.click('Reopen');
      const msg = await t.expectToast(/reopened/i);
      await t.settle();
      await t.pick(t.row('Kamsi Eze'), 'Present', { exact: true });
      await t.click('Submit register');
      await t.expectToast(/submitted|saved/i, 12000);
      await t.settle(1200);
      const b = await badges(t);
      check(/Absent: 0/i.test(b) && /Submitted/i.test(b), `Badges: ${b}`);
      return `"${msg}"; after correction: ${b}`;
    },
  },
  {
    id: 'ATT-05', module: M, sprint: S, feature: 'Daily register', user: 'chinedu.okeke',
    title: 'View an earlier day\'s submitted register',
    steps: [`Open the Primary 5A register for ${dmy(lastSchoolDay)}.`],
    expected: 'The submitted register shows each pupil\'s recorded status (read-only) with counts.',
    async run(t) {
      await openDaily(t, 'Primary 5A', lastSchoolDay);
      const b = await badges(t);
      check(/Submitted/i.test(b) && /Total: 13/i.test(b), `Badges: ${b}`);
      return b;
    },
  },
  {
    id: 'ATT-06', module: M, sprint: S, feature: 'Daily register', user: 'chinedu.okeke',
    title: 'A register cannot be opened for a future date',
    steps: ['Choose Primary 5A and a date one week ahead.', 'Press Open register.'],
    expected: 'The register is refused with a message that attendance cannot be taken in advance.',
    async run(t) {
      await openDaily(t, 'Primary 5A', addDays(today, 7));
      await t.click('Open register');
      const msg = await t.toast();
      check(!/Register opened/i.test(msg), `A register was opened for ${dmy(addDays(today, 7))}, a week in the future.`);
      return `Refused: "${msg}"`;
    },
  },
  {
    id: 'ATT-07', module: M, sprint: S, feature: 'Subject attendance', user: 'chinedu.okeke',
    title: 'Take attendance for a timetabled lesson',
    steps: ['Open Attendance → Subject attendance.', 'Term First Term, class Primary 5A, date today.', 'Press Take attendance on the first lesson.', 'Mark Zainab Bello Absent; press Submit.'],
    expected: 'Today\'s 7 lessons are listed from the timetable; the lesson register opens pre-filled; after Submit it shows Present 12, Absent 1 and "Submitted".',
    async run(t) {
      await t.go('/attendance/subject');
      await t.pick('Term', 'First Term');
      await t.pick('Class', 'Primary 5A', { exact: true });
      await t.date('Date', today);
      await t.settle();
      const lessons = await t.rowCount();
      check(lessons === 7, `Expected 7 lessons today, found ${lessons}`);
      await t.rows().first().getByRole('button').click();
      await t.settle(1000);
      await t.pick(t.row('Zainab Bello'), 'Absent', { exact: true });
      await t.click('Submit', { exact: true });
      await t.expectToast(/submitted|saved/i, 12000);
      await t.settle(1200);
      const b = await badges(t);
      check(/Absent: 1\b/i.test(b) && /Submitted/i.test(b), `Badges: ${b}`);
      return `${lessons} lessons listed; after submit: ${b}`;
    },
  },
  {
    id: 'ATT-08', module: M, sprint: S, feature: 'Attendance summary', user: 'folake.adeyemi',
    title: 'Attendance summary per pupil for the term',
    steps: ['Open Attendance → Summary.', 'Session current, Term First Term, Class Primary 5A.'],
    expected: 'One row per pupil with days counted, present, late, excused, absent and a present rate; today\'s register is included.',
    async run(t) {
      await t.go('/attendance/summary');
      await t.pick('Class', 'Primary 5A', { exact: true });
      await t.pick('Term', 'First Term').catch(() => {});
      await t.settle();
      const rows = await t.rows().allInnerTexts();
      check(rows.length === 13, `Expected 13 pupils, got ${rows.length}`);
      const kamsi = rows.find(r => /Kamsi/.test(r))?.replace(/\s+/g, ' ');
      return `13 pupils summarised; e.g. "${kamsi}"`;
    },
  },
];
