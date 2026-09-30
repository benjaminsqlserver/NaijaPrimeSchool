// Module: Parent and student portals (Sprints 8 and 9).
import { check, login, BASE, makeT } from '../lib.mjs';

const M = 'Family portals';
const S = 'Sprint 8';

/** Looks up a record id through the staff screens (as the head teacher). */
async function staffLookup(t, fn) {
  const ctx = await login(t.browser, 'folake.adeyemi');
  const page = await ctx.newPage();
  const st = makeT(page, ctx);
  try { return await fn(st); } finally { await ctx.close(); }
}
const studentId = (t, name) => staffLookup(t, async st => {
  await st.go('/students');
  await st.fill('Search', name.split(' ')[0]);
  await st.settle(900);
  await st.rowAction(name, 'Edit');
  return st.path().split('/')[2];
});
const reportCardId = (t, name) => staffLookup(t, async st => {
  await st.go('/reports');
  await st.pick('Class', 'Primary 5A', { exact: true });
  await st.settle();
  await st.rowAction(name, 'Open');
  return st.path().split('/')[2];
});

export default [
  {
    id: 'PTL-01', module: M, sprint: S, feature: 'Parent portal', user: 'ngozi.okafor',
    title: 'Parent sees all linked wards at a glance',
    steps: ['Sign in as parent ngozi.okafor.', 'Open Parent portal → My wards.'],
    expected: 'Two ward cards — Chiamaka (Primary 5A) and Tobenna (Primary 1A) — each with class, outstanding balance (₦0 — both paid), attendance percentage and report-card count; no other family\'s children.',
    async run(t) {
      await t.go('/portal/parent');
      const body = await t.text('.nps-page-container');
      check(/Chiamaka/.test(body) && /Tobenna/.test(body), 'A ward is missing');
      check(!/Kamsi|Zainab|Tolu/.test(body), 'Another family\'s child is shown');
      const cards = await t.page.locator('.nps-card, .rz-card').filter({ hasText: /Chiamaka|Tobenna/ }).allInnerTexts();
      return cards.map(c => c.replace(/\s+/g, ' ').slice(0, 160)).join(' || ');
    },
  },
  {
    id: 'PTL-02', module: M, sprint: S, feature: 'Parent portal', user: 'ngozi.okafor',
    title: 'Ward detail: overview, report cards and fees',
    steps: ['On My wards press Open on Chiamaka.', 'Look at the Overview, Report cards and Fees & invoices tabs.'],
    expected: 'Overview shows class, attendance and balance; Report cards lists the published First Term card (average, position); Fees & invoices lists her First Term invoice as Paid.',
    async run(t) {
      await t.go('/portal/parent');
      await t.page.locator('.nps-card, .rz-card').filter({ hasText: 'Chiamaka' }).last().getByRole('button', { name: /Open/ }).first().click();
      await t.settle();
      check(/\/portal\/parent\/wards\//.test(t.path()), `Did not open the ward page (${t.path()})`);
      await t.page.getByRole('tab', { name: 'Report cards' }).click();
      await t.page.waitForTimeout(400);
      const cards = await t.rows().allInnerTexts();
      check(cards.length === 1 && /First Term/.test(cards[0]), `Report cards: ${cards.length}`);
      await t.page.getByRole('tab', { name: 'Fees & invoices' }).click();
      await t.page.waitForTimeout(400);
      const fees = await t.text('.rz-tabview-panels');
      check(/NPS\/INV\//.test(fees) && /Paid/i.test(fees), `Fees tab: ${fees.slice(0, 200)}`);
      return `Report card: "${cards[0].replace(/\s+/g, ' ')}"; invoice shown as Paid.`;
    },
  },
  {
    id: 'PTL-03', module: M, sprint: S, feature: 'Parent portal', user: 'ngozi.okafor',
    title: 'Parent opens a ward\'s published report card',
    steps: ['On Chiamaka\'s page open the Report cards tab.', 'Press the view (eye) button on the First Term card.'],
    expected: 'The report card opens read-only: subjects with grades, average, position, attendance, traits and comments.',
    async run(t) {
      await t.go('/portal/parent');
      await t.page.locator('.nps-card, .rz-card').filter({ hasText: 'Chiamaka' }).last().getByRole('button', { name: /Open/ }).first().click();
      await t.settle();
      await t.page.getByRole('tab', { name: 'Report cards' }).click();
      await t.page.waitForTimeout(400);
      await t.rows().first().locator('button').last().click();
      await t.settle(1200);
      check(!t.path().startsWith('/Account/AccessDenied'), 'The view button leads to "Access denied" — the report card page is staff-only.');
      const h = await t.heading();
      check(/Chiamaka/.test(h), `Opened "${h}"`);
      return `Opened "${h}" (${t.path()}).`;
    },
  },
  {
    id: 'PTL-04', module: M, sprint: S, feature: 'Parent portal', user: 'ngozi.okafor',
    title: 'A parent cannot open another family\'s child',
    steps: ['Signed in as Mrs. Okafor, type the address of Kamsi Eze\'s ward page (/portal/parent/wards/<Kamsi\'s id>).'],
    expected: '"Not authorised — This pupil is not linked to your parent profile." No details of the child are shown.',
    async run(t) {
      const id = await studentId(t, 'Kamsi Eze');
      await t.go(`/portal/parent/wards/${id}`);
      const body = await t.text('.nps-page-container');
      check(/Not authorised/i.test(body) && !/Primary 5A/.test(body), `Page shows: ${body.slice(0, 200)}`);
      return 'Refused: "Not authorised — This pupil is not linked to your parent profile."';
    },
  },
  {
    id: 'PTL-05', module: M, sprint: S, feature: 'Student portal', user: 'chiamaka.okafor',
    title: 'Student portal shows today\'s timetable',
    steps: ['Sign in as pupil chiamaka.okafor.', 'Open Student portal → Today.'],
    expected: 'Today\'s Primary 5A lessons with period, time, subject, teacher and room, plus shortcut buttons (profile, results, attendance, fees, announcements).',
    async run(t) {
      await t.go('/portal/student');
      const lessons = await t.rowCount();
      const day = new Date().getDay();
      if (day >= 1 && day <= 5) check(lessons === 7, `Expected 7 lessons today, found ${lessons}`);
      await t.see('Shortcuts');
      return `${lessons} lessons today; shortcuts shown.`;
    },
  },
  {
    id: 'PTL-06', module: M, sprint: S, feature: 'Student portal', user: 'chiamaka.okafor',
    title: 'Student profile',
    steps: ['Open My profile.'],
    expected: 'Chiamaka\'s own details: name, admission number, class, date of birth; no edit controls.',
    async run(t) {
      await t.go('/portal/student/profile');
      const body = await t.text('.nps-page-container');
      check(/Chiamaka Okafor/.test(body) && /NPS\/\d{2}\/001/.test(body), `Profile: ${body.slice(0, 200)}`);
      const saves = await t.page.getByRole('button', { name: /Save/ }).count();
      check(saves === 0, 'Profile has a Save button');
      return body.slice(0, 220);
    },
  },
  {
    id: 'PTL-07', module: M, sprint: S, feature: 'Student portal', user: 'chiamaka.okafor',
    title: 'Student views published results and report card',
    steps: ['Open My results.', 'Press the view (eye) button on the First Term card.'],
    expected: 'Only published cards are listed; the report card opens read-only.',
    async run(t) {
      await t.go('/portal/student/results');
      const n = await t.rowCount();
      check(n === 1, `${n} report cards listed`);
      await t.rows().first().locator('button').last().click();
      await t.settle(1200);
      check(!t.path().startsWith('/Account/AccessDenied'), 'The view button leads to "Access denied" — the report card page is staff-only.');
      return `Opened ${await t.heading()}.`;
    },
  },
  {
    id: 'PTL-08', module: M, sprint: S, feature: 'Student portal', user: 'chiamaka.okafor',
    title: 'A student cannot open another pupil\'s report card',
    steps: ['Signed in as Chiamaka, type the address of Kamsi Eze\'s report card: the staff page /reports/<id>, and the family view of the same card.'],
    expected: 'Both are refused — pupils only ever see their own report cards.',
    async run(t) {
      const id = await reportCardId(t, 'Kamsi Eze');
      const leaks = [];
      for (const path of [`/reports/${id}`, `/portal/report-cards/${id}`]) {
        await t.go(path);
        await t.page.waitForTimeout(800);
        const body = await t.text('body');
        if (/Kamsi Eze/.test(body) && !t.path().startsWith('/Account/AccessDenied')) leaks.push(path);
      }
      check(!leaks.length, `Chiamaka can read Kamsi Eze's report card at ${leaks.join(', ')}`);
      return 'Refused on both addresses.';
    },
  },
  {
    id: 'PTL-09', module: M, sprint: S, feature: 'Student portal', user: 'chiamaka.okafor',
    title: 'Student attendance summary',
    steps: ['Open My attendance.'],
    expected: 'Days present / total and a percentage for the current term, with the recent register entries.',
    async run(t) {
      await t.go('/portal/student/attendance');
      const body = await t.text('.nps-page-container');
      check(/%/.test(body), `No percentage shown: ${body.slice(0, 200)}`);
      return body.slice(0, 220);
    },
  },
  {
    id: 'PTL-10', module: M, sprint: S, feature: 'Student portal', user: 'chiamaka.okafor',
    title: 'Student fees and payments',
    steps: ['Open My fees.'],
    expected: 'Her First Term invoice (Paid, balance ₦0) and the receipt that paid it; no "Pay online" button on a paid invoice.',
    async run(t) {
      await t.go('/portal/student/fees');
      const body = await t.text('.nps-page-container');
      check(/NPS\/INV\//.test(body) && /NPS\/RCP\//.test(body), 'Invoice or receipt missing');
      const pay = await t.page.getByRole('button', { name: /Pay online/ }).count();
      check(pay === 0, 'Pay online is offered on a paid invoice');
      return body.slice(0, 240);
    },
  },
];
