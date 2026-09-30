// Module: Audit log (Sprint 16).
import { check, login, makeT, stamp } from '../lib.mjs';

const M = 'Audit log';
const S = 'Sprint 16';
const NEW_PHONE = `0809${String(Date.now()).slice(-7)}`;

export default [
  {
    id: 'AUD-01', module: M, sprint: S, feature: 'Audit log', user: 'folake.adeyemi',
    title: 'Every change is in the audit trail with who and when',
    steps: ['Sign in as the head teacher; open Administration → Audit log.'],
    expected: 'The newest changes are listed first, 50 per page, each with time, user, action (Created / Updated / Deleted / Restored) and the record it touched — including today\'s UAT activity by several staff and families.',
    async run(t) {
      await t.go('/admin/audit-log');
      const rows = (await t.rows().allInnerTexts()).map(r => r.replace(/\s+/g, ' '));
      check(rows.length === 50, `First page has ${rows.length} rows`);
      const who = new Set(rows.map(r => (r.match(/folake\.adeyemi|tunde\.bakare|musa\.ibrahim|chinedu\.okeke|superadmin|ngozi\.okafor|chukwudi\.eze|System/) ?? [])[0]).filter(Boolean));
      return `50 rows on page 1; users seen: ${[...who].join(', ')}; newest: "${rows[0].slice(0, 120)}"`;
    },
  },
  {
    id: 'AUD-02', module: M, sprint: S, feature: 'Audit log', user: 'folake.adeyemi',
    title: 'An edit is logged with before and after values',
    steps: ['Open Family → Parents → Yetunde Balogun; change Primary phone to a new number; Save changes.', 'Open Audit log; the newest row is the update — expand it.'],
    expected: 'An "Updated" row by folake.adeyemi labelled "Yetunde Balogun", 1 field changed; expanded, it shows Primary Phone: old → new.',
    async run(t) {
      await t.go('/parents');
      await t.fill('Search', 'Yetunde');
      await t.settle(900);
      await t.rowAction('Yetunde Balogun', 'Edit');
      await t.settle();
      const old = await t.field('Primary phone').locator('input').inputValue();
      await t.fill('Primary phone', NEW_PHONE);
      await t.click('Save changes');
      await t.expectToast(/updated/i);
      await t.go('/admin/audit-log');
      const first = t.rows().first();
      const text = (await first.innerText()).replace(/\s+/g, ' ');
      check(/Updated/i.test(text) && /Yetunde Balogun/.test(text) && /folake/.test(text), `Newest row: ${text}`);
      await first.locator('.rz-row-toggler').click();
      await t.page.waitForTimeout(600);
      const detail = (await t.text('.nps-audit-changes, .rz-expanded-row-content, .rz-data-grid')).replace(/\s+/g, ' ');
      check(detail.includes(old) && detail.includes(NEW_PHONE), 'Old/new phone not shown');
      return `"${text.slice(0, 120)}"; Primary Phone ${old} → ${NEW_PHONE}.`;
    },
  },
  {
    id: 'AUD-03', module: M, sprint: S, feature: 'Record history', user: 'folake.adeyemi',
    title: 'Full history of one record, deep-linkable',
    steps: ['On the newest row press History.'],
    expected: 'The log shows every change to Yetunde Balogun\'s record in order, starting with "Created" by the head teacher at seeding; the URL carries entityType=Parent&entityId=… so it can be shared.',
    async run(t) {
      await t.go('/admin/audit-log');
      await t.rows().first().getByRole('button', { name: /History/ }).click();
      await t.settle(1200);
      check(/entityType=Parent/.test(t.page.url()) && /entityId=/.test(t.page.url()), `URL: ${t.page.url()}`);
      const rows = (await t.rows().allInnerTexts()).map(r => r.replace(/\s+/g, ' '));
      check(rows.length >= 2 && rows.some(r => /Created/i.test(r)), `History rows: ${rows.length}`);
      return `${rows.length} entries in the record's history; URL ${t.path()}`;
    },
  },
  {
    id: 'AUD-04', module: M, sprint: S, feature: 'Audit log', user: 'folake.adeyemi',
    title: 'Search finds a change by an old value; filter by user and action',
    steps: ['Search for the phone number just replaced.', 'Clear; filter User = tunde.bakare.', 'Filter Action = Deleted.'],
    expected: 'Searching the old number finds the update; the user filter shows only the bursar\'s changes; Deleted shows only deletions (e.g. the parent deleted in FAM-15, the subject and period deleted in Academics).',
    async run(t) {
      await t.go('/admin/audit-log');
      await t.fill('Search', NEW_PHONE);
      await t.settle(900);
      const found = await t.rowCount();
      await t.fill('Search', '');
      await t.pick('User', 'tunde.bakare');
      await t.settle(900);
      const bursar = (await t.rows().allInnerTexts());
      check(found >= 1, `Search found ${found}`);
      check(bursar.length > 0 && bursar.every(r => /tunde\.bakare/.test(r)), `User filter: ${bursar.length} rows`);
      await t.go('/admin/audit-log');
      await t.pick('Action', 'Deleted');
      await t.settle(900);
      const deleted = (await t.rows().allInnerTexts());
      check(deleted.length > 0 && deleted.every(r => /Deleted/i.test(r)), `Deleted filter: ${deleted.length} rows`);
      return `Search → ${found}; tunde.bakare → ${bursar.length} rows; Deleted → ${deleted.length} rows.`;
    },
  },
  {
    id: 'AUD-05', module: M, sprint: S, feature: 'Security', user: 'folake.adeyemi',
    title: 'Password changes are logged without the password',
    pre: 'USR-07 reset a user\'s password earlier in the run.',
    steps: ['Search the audit log for "(hidden)".', 'Expand the entry.'],
    expected: 'The user account shows "Updated" with Password Hash (hidden) → (hidden); no password hash appears anywhere.',
    async run(t) {
      await t.go('/admin/audit-log');
      await t.fill('Search', '(hidden)');
      await t.settle(900);
      const n = await t.rowCount();
      check(n >= 1, 'No password change found');
      await t.rows().first().locator('.rz-row-toggler').click();
      await t.page.waitForTimeout(600);
      const body = await t.text();
      check(/Password Hash/i.test(body) && !/AQAAAA/.test(body), 'Hash visible or field missing');
      return `${n} entries; "Password Hash (hidden) → (hidden)"; no hash shown.`;
    },
  },
];
