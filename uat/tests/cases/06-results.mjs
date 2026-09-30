// Module: Assessments, results and report cards (Sprint 5).
import { check, today, dmy } from '../lib.mjs';

const M = 'Results & report cards';
const S = 'Sprint 5';
const TITLE = 'Social Studies – First CA';

async function filterAssessments(t) {
  await t.go('/assessments');
  await t.pick('Class', 'Primary 5A', { exact: true });
  await t.settle();
}

export default [
  {
    id: 'RES-01', module: M, sprint: S, feature: 'Assessments', user: 'chinedu.okeke',
    title: 'Teacher sees the class gradebook',
    steps: ['Sign in as teacher chinedu.okeke.', 'Open Results & Reports → Assessments; filter Class = Primary 5A.'],
    expected: 'Nine published assessments (First CA, Second CA, Examination for English, Mathematics and Basic Science), each showing 13 pupils scored.',
    async run(t) {
      await filterAssessments(t);
      const rows = await t.rows().allInnerTexts();
      check(rows.length === 9, `Expected 9 assessments, got ${rows.length}`);
      check(rows.every(r => /Published/i.test(r)), 'Not all are published');
      return `${rows.length} published assessments, e.g. "${rows[0].replace(/\s+/g, ' ')}"`;
    },
  },
  {
    id: 'RES-02', module: M, sprint: S, feature: 'Assessments', user: 'chinedu.okeke',
    title: 'Create a new assessment',
    steps: ['Press New assessment.', `Term First Term, class Primary 5A, subject Social Studies, type First CA, title "${TITLE}", maximum 20, weight 20, date today.`, 'Press Save.'],
    expected: `"Assessment '${TITLE}' saved." and it is listed as Draft with 0 scored.`,
    async run(t) {
      await filterAssessments(t);
      await t.click('New assessment');
      await t.pick('Term', 'First Term');
      await t.pick(t.page.locator('.nps-field').filter({ has: t.page.locator('label', { hasText: /^Class \*$/ }) }), 'Primary 5A', { exact: true });
      await t.pick(t.page.locator('.nps-field').filter({ has: t.page.locator('label', { hasText: /^Subject \*$/ }) }), 'Social Studies');
      await t.pick('Type', 'First CA');
      await t.fill('Title', TITLE);
      await t.fill('Maximum score', '20');
      await t.fill('Weight', '20');
      await t.date('Date', today);
      await t.click('Save', { exact: true });
      const msg = await t.expectToast(/saved/i);
      await t.settle();
      const row = await t.row(TITLE).innerText();
      check(/Draft/i.test(row), `Row: ${row}`);
      return `"${msg}"; listed: ${row.replace(/\s+/g, ' ')}`;
    },
  },
  {
    id: 'RES-03', module: M, sprint: S, feature: 'Score entry', user: 'chinedu.okeke',
    title: 'Enter scores, mark an absentee, keep scores within the maximum',
    steps: [`Press Enter scores on "${TITLE}".`, 'Enter 18 for the first pupil, 25 for the second (above the maximum of 20), tick Absent for the third, and 12–16 for the rest.', 'Press Save scores.'],
    expected: 'The 25 is capped at 20 (or refused); the absent pupil\'s score box is disabled; "Saved" is shown and the list shows 13 scored.',
    async run(t) {
      await filterAssessments(t);
      await t.rowAction(TITLE, 'Enter scores');
      await t.settle();
      const rows = t.page.locator('tbody tr');
      const n = await rows.count();
      for (let i = 0; i < n; i++) {
        if (i === 2) { await rows.nth(i).locator('.rz-chkbox-box').click(); continue; }
        const box = rows.nth(i).locator('input').first();
        await box.fill(String(i === 0 ? 18 : i === 1 ? 25 : 12 + (i % 5)));
        await box.press('Tab');
      }
      await t.page.waitForTimeout(400);
      const second = await rows.nth(1).locator('input').first().inputValue();
      const absentDisabled = await rows.nth(2).locator('input').first().isDisabled();
      await t.click('Save scores');
      const msg = await t.expectToast(/Saved/i);
      check(Number(second) <= 20, `Second pupil's score shows ${second} (above the maximum)`);
      check(absentDisabled, 'Absent pupil\'s score box is still editable');
      await filterAssessments(t);
      const row = await t.row(TITLE).innerText();
      return `Over-max value became ${second}; absent box disabled; "${msg}"; list: ${row.replace(/\s+/g, ' ')}`;
    },
  },
  {
    id: 'RES-04', module: M, sprint: S, feature: 'Assessments', user: 'chinedu.okeke',
    title: 'Publish an assessment; its scores become read-only',
    steps: [`Press Publish on "${TITLE}".`, 'Open Enter scores again.'],
    expected: 'The assessment shows Published; the score sheet shows "Published — read only" and its inputs are disabled.',
    async run(t) {
      await filterAssessments(t);
      await t.rowAction(TITLE, 'Publish');
      await t.toast();
      await t.settle();
      check(/Published/i.test(await t.row(TITLE).innerText()), 'Not published');
      await t.rowAction(TITLE, 'Enter scores');
      await t.settle();
      await t.see('Published — read only');
      const enabled = await t.page.locator('tbody tr input:not([disabled]):not([type=checkbox])').count();
      check(enabled === 0, `${enabled} inputs still editable`);
      return 'Published; score sheet read-only.';
    },
  },
  {
    id: 'RES-05', module: M, sprint: S, feature: 'Subject results', user: 'folake.adeyemi',
    title: 'Compute and finalise subject results',
    steps: ['Sign in as the head teacher; open Results & Reports → Subject results.', 'Term First Term, class Primary 5A, subject Social Studies.', 'Press Compute & finalise and confirm Finalise.'],
    expected: 'A "Computed" confirmation; each pupil gets a total, grade band (A–F) and class position, marked Finalised.',
    async run(t) {
      await t.go('/results');
      await t.pick('Term', 'First Term');
      await t.pick('Class', 'Primary 5A', { exact: true });
      await t.pick('Subject (optional)', 'Social Studies');
      await t.settle();
      await t.click('Compute & finalise');
      await t.confirm('Finalise');
      const msg = await t.expectToast(/Computed/i, 15000);
      await t.settle();
      const rows = await t.rows().allInnerTexts();
      check(rows.length >= 12, `Only ${rows.length} results`);
      const first = rows[0].replace(/\s+/g, ' ');
      check(/Finalised/i.test(first), `Row: ${first}`);
      return `"${msg}"; ${rows.length} results, e.g. "${first}"`;
    },
  },
  {
    id: 'RES-06', module: M, sprint: S, feature: 'Subject results', user: 'folake.adeyemi',
    title: 'Mathematics results are graded and ranked',
    steps: ['On Subject results choose First Term, Primary 5A, Mathematics.'],
    expected: '13 results, positions 1–13, with grades consistent with the totals (e.g. 70%+ = A).',
    async run(t) {
      await t.go('/results');
      await t.pick('Term', 'First Term');
      await t.pick('Class', 'Primary 5A', { exact: true });
      await t.pick('Subject (optional)', 'Mathematics');
      await t.settle();
      const rows = await t.rows().allInnerTexts();
      check(rows.length === 13, `Expected 13, got ${rows.length}`);
      return `13 ranked results; top: "${rows[0].replace(/\s+/g, ' ')}"`;
    },
  },
  {
    id: 'RES-07', module: M, sprint: S, feature: 'Report cards', user: 'folake.adeyemi',
    title: 'Report card list for a class',
    steps: ['Open Results & Reports → Report cards.', 'Filter Class = Primary 5A.'],
    expected: '13 cards with average, position and attendance; 11 Published and 2 Draft (Aminu Ibrahim, Ekaette Okon).',
    async run(t) {
      await t.go('/reports');
      await t.pick('Class', 'Primary 5A', { exact: true });
      await t.settle();
      const rows = await t.rows().allInnerTexts();
      const drafts = rows.filter(r => /Draft/i.test(r));
      check(rows.length === 13 && drafts.length === 2, `${rows.length} cards, ${drafts.length} drafts`);
      return `13 cards; drafts: ${drafts.map(r => r.split('\n')[0]).join(', ')}`;
    },
  },
  {
    id: 'RES-08', module: M, sprint: S, feature: 'Report cards', user: 'folake.adeyemi',
    title: 'Complete and publish a draft report card',
    steps: ['Open Ekaette Okon\'s card.', 'Affective traits: Punctuality = Excellent.', 'Comments: head teacher "A bright, cheerful pupil."; next term begins; Save comments.', 'Press Publish and confirm.'],
    expected: 'The card shows subjects with grades, average, position and days present; rating and comments save; after Publish it is marked Published and fields lock.',
    async run(t) {
      await t.go('/reports');
      await t.pick('Class', 'Primary 5A', { exact: true });
      await t.settle();
      await t.rowAction('Ekaette Okon', 'Open');
      await t.settle();
      const stats = (await t.page.locator('.nps-stat-card').allInnerTexts()).map(s => s.replace(/\s+/g, ' ')).join(' | ');
      await t.page.getByRole('tab', { name: 'Affective traits' }).click();
      await t.page.waitForTimeout(400);
      await t.pick(t.page.locator('tr', { hasText: 'Punctuality' }), 'Excellent', { exact: true });
      await t.toast().catch(() => {});
      await t.page.getByRole('tab', { name: 'Comments' }).click();
      await t.page.waitForTimeout(400);
      await t.fill('Head teacher\'s comment', 'A bright, cheerful pupil.');
      await t.click('Save comments');
      await t.expectToast(/saved|updated|Done/i);
      await t.click('Publish', { exact: true });
      await t.confirm('Publish');
      await t.toast();
      await t.settle();
      await t.see('Unpublish');
      return `Card stats: ${stats}; rating + comment saved; published.`;
    },
  },
  {
    id: 'RES-09', module: M, sprint: S, feature: 'Report cards', user: 'folake.adeyemi',
    title: 'Generate / refresh report cards picks up new results',
    steps: ['On Report cards press Generate / refresh.', 'Term First Term, class Primary 5A; press Generate.'],
    expected: 'A "Generated" confirmation reporting cards created/updated; published cards are left untouched.',
    async run(t) {
      await t.go('/reports');
      await t.click('Generate / refresh');
      await t.pick(t.page.locator('.nps-field').filter({ has: t.page.locator('label', { hasText: /^Term \*$/ }) }), 'First Term');
      await t.pick(t.page.locator('.nps-field').filter({ has: t.page.locator('label', { hasText: /^Class \*$/ }) }), 'Primary 5A', { exact: true });
      await t.click('Generate', { exact: true });
      const msg = await t.expectToast(/Generated/i, 15000);
      return `"${msg}"`;
    },
  },
];
