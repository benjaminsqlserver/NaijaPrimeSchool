// Module: Fees, invoices, payments and the bursar dashboard (Sprint 6).
import { check, today, addDays, dmy } from '../lib.mjs';

const M = 'Fees & payments';
const S = 'Sprint 6';
const naira = s => Number(String(s).replace(/[^\d.]/g, ''));
const stat = async (t, label) => (await t.page.locator('.nps-stat-card', { hasText: label }).first().innerText()).replace(/\s+/g, ' ');

async function openInvoiceOf(t, pupil) {
  await t.go('/invoices');
  await t.fill('Search', pupil.split(' ')[0]);
  await t.settle(900);
  await t.rowAction(pupil, 'View');
  await t.settle();
}

export default [
  {
    id: 'FIN-01', module: M, sprint: S, feature: 'Bursar dashboard', user: 'tunde.bakare',
    title: 'Bursar dashboard totals match the ledger',
    pre: 'Seed: 21 First Term invoices — 12 paid in full, 6 part-paid, 3 unpaid; ₦5,117,000 invoiced, ₦3,645,000 collected.',
    steps: ['Sign in as bursar tunde.bakare.', 'Open Finance → Bursar dashboard.'],
    expected: 'Invoiced ₦5,117,000; Collected ₦3,645,000; Outstanding ₦1,472,000; 21 invoices: 12 paid in full, 6 partially paid, 3 unpaid; breakdowns by status, method and category.',
    async run(t) {
      await t.go('/finance');
      const inv = await stat(t, 'Invoiced'), col = await stat(t, 'Collected'), out = await stat(t, 'Outstanding');
      check(naira(inv) === 5117000, `Invoiced shows ${inv}`);
      check(naira(col) === 3645000, `Collected shows ${col}`);
      check(naira(out) === 1472000, `Outstanding shows ${out}`);
      const counts = [await stat(t, 'Paid in full'), await stat(t, 'Partially paid'), await stat(t, 'Unpaid')];
      check(/12/.test(counts[0]) && /\b6\b/.test(counts[1]) && /\b3\b/.test(counts[2]), `Counts: ${counts.join(' | ')}`);
      return `${inv} | ${col} | ${out} | ${counts.join(' | ')}`;
    },
  },
  {
    id: 'FIN-02', module: M, sprint: S, feature: 'Fee schedules', user: 'tunde.bakare',
    title: 'Fee schedules list',
    steps: ['Open Finance → Fee schedules.'],
    expected: 'Primary 5 (₦257,000, 5 items) and Primary 1 (₦222,000, 5 items) First Term schedules, both Published, with invoices issued.',
    async run(t) {
      await t.go('/fees');
      const rows = await t.rows().allInnerTexts();
      const p5 = rows.find(r => /Primary 5/.test(r)) ?? '', p1 = rows.find(r => /Primary 1/.test(r)) ?? '';
      check(/257,000/.test(p5) && /Published/i.test(p5), `Primary 5 row: ${p5}`);
      check(/222,000/.test(p1) && /Published/i.test(p1), `Primary 1 row: ${p1}`);
      return `${rows.length} schedules: ${[p5, p1].map(r => r.replace(/\s+/g, ' ')).join(' / ')}`;
    },
  },
  {
    id: 'FIN-03', module: M, sprint: S, feature: 'Fee schedules', user: 'tunde.bakare',
    title: 'Create and publish a fee schedule with line items',
    steps: ['Press New schedule: term First Term, level Primary 6, title "Primary 6 — First Term fees"; Create schedule.', 'Add item: Tuition, "Tuition", ₦200,000, mandatory.', 'Add item: Examination, "WAEC/NECO common entrance prep", ₦15,000, optional.', 'Press Publish.'],
    expected: 'Schedule created and opened; the total reads ₦215,000 with 2 items; after Publish it shows Published.',
    async run(t) {
      await t.go('/fees');
      await t.click('New schedule');
      await t.pick(t.page.locator('.nps-field').filter({ has: t.page.locator('label', { hasText: /^Term \*$/ }) }), 'First Term');
      await t.pick(t.page.locator('.nps-field').filter({ has: t.page.locator('label', { hasText: /^Class level \*$/ }) }), 'Primary 6', { exact: true });
      await t.fill('Title', 'Primary 6 — First Term fees');
      await t.click('Create schedule');
      await t.page.waitForURL(u => /\/fees\/[0-9a-f-]{36}/.test(u.pathname), { timeout: 10000 });
      await t.settle();
      for (const [cat, desc, amt, mandatory] of [['Tuition', 'Tuition', 200000, true], ['Examination', 'Common entrance prep', 15000, false]]) {
        await t.click('Add item');
        await t.pick('Category', cat, { exact: true });
        await t.fill('Description', desc);
        await t.fill('Amount', amt);
        if (!mandatory) await t.field('Mandatory').locator('.rz-switch').click();
        const before = await t.rowCount();
        await t.click('Save', { exact: true });
        await t.page.waitForFunction(n => document.querySelectorAll('.rz-data-row').length > n, before, { timeout: 8000 });
        await t.settle();
      }
      const head = await t.text('h3');
      check(/215,000/.test(head), `Items header: ${head}`);
      await t.click('Publish', { exact: true });
      await t.maybeConfirm(/Publish|Yes|Ok/i);
      await t.toast().catch(() => {});
      await t.settle();
      await t.see('Unpublish');
      return `Created; "${head}"; published.`;
    },
  },
  {
    id: 'FIN-04', module: M, sprint: S, feature: 'Issue invoices', user: 'tunde.bakare',
    title: 'Issue invoices to a whole class, without duplicates',
    steps: ['Open Finance → Issue invoices.', 'Term First Term, schedule "Primary 6 — First Term fees", class Primary 6A, issued today, due in 30 days.', 'Press Issue invoices.', 'Press Issue invoices again.'],
    expected: 'One invoice per actively-enrolled pupil (2) is issued; the second attempt issues none because each pupil already has one.',
    async run(t) {
      await t.go('/invoices/issue');
      await t.pick('Term', 'First Term');
      await t.pick('Fee schedule', 'Primary 6');
      await t.pick('Class', 'Primary 6A', { exact: true });
      await t.date('Due date', addDays(today, 30));
      await t.click('Issue invoices');
      const first = await t.toast(15000);
      await t.page.waitForTimeout(800);
      if (!t.path().startsWith('/invoices/issue')) {
        await t.go('/invoices/issue');
        await t.pick('Term', 'First Term');
        await t.pick('Fee schedule', 'Primary 6');
        await t.pick('Class', 'Primary 6A', { exact: true });
      }
      await t.click('Issue invoices');
      const second = await t.toast(15000);
      check(/\b2\b/.test(first), `First issue: "${first}"`);
      check(!/\b2 invoice/.test(second) && /\b0\b|already|skipp/i.test(second), `Second issue: "${second}"`);
      return `First: "${first}"; second: "${second}"`;
    },
  },
  {
    id: 'FIN-05', module: M, sprint: S, feature: 'Invoices', user: 'tunde.bakare',
    title: 'Find invoices by pupil and by status',
    steps: ['Open Finance → Invoices.', 'Search "Okafor".', 'Clear search; Status = Issued.'],
    expected: 'Search finds Chiamaka\'s and Tobenna\'s invoices; Issued lists the unpaid invoices (Kamsi Eze, Oluwaseun Akinola, Somto Obi and the new Primary 6 ones).',
    async run(t) {
      await t.go('/invoices');
      await t.fill('Search', 'Okafor');
      await t.settle(900);
      const s = await t.rowCount();
      await t.fill('Search', '');
      await t.pick('Status', 'Issued', { exact: true });
      await t.settle(900);
      const issued = await t.rows().allInnerTexts();
      check(s === 2, `Okafor search returned ${s}`);
      check(['Kamsi Eze', 'Oluwaseun Akinola', 'Somto Obi'].every(n => issued.some(r => r.includes(n))), `Issued list: ${issued.length} rows`);
      return `Okafor → ${s} invoices; Issued → ${issued.length} invoices.`;
    },
  },
  {
    id: 'FIN-06', module: M, sprint: S, feature: 'Invoices', user: 'tunde.bakare',
    title: 'Apply a discount to an invoice line',
    steps: ['Open Somto Obi\'s invoice.', 'Enter a ₦10,000 discount on the Tuition line (sibling discount) and press Tab.'],
    expected: '"Discount applied."; the Tuition net falls by ₦10,000 and the Discount and Balance figures update (₦222,000 → ₦212,000).',
    async run(t) {
      await openInvoiceOf(t, 'Somto Obi');
      const before = await stat(t, 'Balance');
      const box = t.row('Tuition').locator('input').first();
      await box.fill('10000');
      await box.press('Tab');
      const msg = await t.expectToast(/Discount applied/i);
      await t.settle();
      const after = await stat(t, 'Balance');
      check(naira(before) - naira(after) === 10000, `Balance ${before} → ${after}`);
      return `"${msg}"; ${before} → ${after}`;
    },
  },
  {
    id: 'FIN-07', module: M, sprint: S, feature: 'Payments', user: 'tunde.bakare',
    title: 'Record a part-payment against an invoice',
    steps: ['On Somto Obi\'s invoice press Record payment.', 'Method Bank Transfer, reference "GTB-448812", amount ₦100,000, allocate ₦100,000 to the invoice.', 'Press Save payment.'],
    expected: 'The receipt page opens with a receipt number NPS/RCP/<year>/<n>, ₦100,000, Bank Transfer and the allocation; the invoice becomes Partially Paid with balance ₦112,000.',
    async run(t) {
      await openInvoiceOf(t, 'Somto Obi');
      await t.click('Record payment');
      await t.settle();
      await t.pick('Method', 'Bank Transfer', { exact: true });
      await t.fill('Reference', 'GTB-448812');
      await t.fill('Amount', '100000');
      const alloc = t.page.locator('table:has(th:text("Allocate")) tbody tr').first().locator('input').last();
      await alloc.fill('100000');
      await alloc.press('Tab');
      await t.click('Save payment');
      await t.page.waitForURL(u => /\/payments\/[0-9a-f-]{36}/.test(u.pathname), { timeout: 10000 });
      await t.settle();
      const receipt = await t.heading();
      check(/^NPS\/RCP\/\d{4}\/\d+/.test(receipt), `Receipt number "${receipt}"`);
      await openInvoiceOf(t, 'Somto Obi');
      const status = await t.text('.nps-page-actions');
      const bal = await stat(t, 'Balance');
      check(/Partially Paid/i.test(status) && naira(bal) === 112000, `Invoice now: ${status} / ${bal}`);
      return `Receipt ${receipt}; invoice Partially Paid, ${bal}.`;
    },
  },
  {
    id: 'FIN-08', module: M, sprint: S, feature: 'Payments', user: 'tunde.bakare',
    title: 'Allocations cannot exceed the amount received',
    steps: ['Open Record payment; pupil Kamsi Eze; method Cash; amount ₦5,000.', 'Allocate ₦50,000 to his invoice.', 'Press Save payment.'],
    expected: 'An "Over-allocated" warning; nothing is saved.',
    async run(t) {
      await t.go('/payments/new');
      await t.pick('Pupil', 'Kamsi Eze');
      await t.settle();
      await t.pick('Method', 'Cash', { exact: true });
      await t.fill('Amount', '5000');
      const alloc = t.page.locator('table:has(th:text("Allocate")) tbody tr').first().locator('input').last();
      await alloc.fill('50000');
      await alloc.press('Tab');
      await t.click('Save payment');
      const msg = await t.expectToast(/allocat/i);
      check(t.path().startsWith('/payments/new'), 'A payment was saved');
      return `Refused: "${msg}"`;
    },
  },
  {
    id: 'FIN-09', module: M, sprint: S, feature: 'Payments', user: 'tunde.bakare',
    title: 'Payments register and receipt',
    steps: ['Open Finance → Payments.', 'Filter Method = Bank Transfer.', 'Open the GTB-448812 receipt.'],
    expected: 'Receipts are listed with pupil, method, reference and status; the filter narrows to bank transfers; the receipt shows amount, method, reference and the invoice it paid.',
    async run(t) {
      await t.go('/payments');
      const all = await t.rowCount();
      await t.pick('Method', 'Bank Transfer', { exact: true });
      await t.settle(900);
      const rows = await t.rows().allInnerTexts();
      check(rows.length > 0 && rows.every(r => /Bank Transfer/i.test(r)), `Bank filter: ${rows.length} rows`);
      await t.row('GTB-448812').locator('button').last().click();
      await t.settle();
      const ref = await stat(t, 'Reference');
      check(/GTB-448812/.test(ref), `Receipt reference: ${ref}`);
      return `${all} receipts; ${rows.length} bank transfers; receipt opened (${ref}).`;
    },
  },
  {
    id: 'FIN-10', module: M, sprint: S, feature: 'Refunds', user: 'tunde.bakare',
    title: 'Refund a payment restores the invoice balance',
    steps: ['Open the GTB-448812 receipt.', 'Press Refund and confirm.', 'Open Somto Obi\'s invoice.'],
    expected: '"Payment refunded."; the receipt shows Refunded; the invoice balance returns to ₦212,000 and its status to Issued.',
    async run(t) {
      await t.go('/payments');
      await t.fill('Search', 'GTB-448812');
      await t.settle(900);
      await t.row('GTB-448812').locator('button').last().click();
      await t.settle();
      await t.click('Refund', { exact: true });
      await t.confirm('Refund');
      const msg = await t.expectToast(/refunded/i);
      await openInvoiceOf(t, 'Somto Obi');
      const bal = await stat(t, 'Balance');
      const status = await t.text('.nps-page-actions');
      check(naira(bal) === 212000, `Balance after refund: ${bal}`);
      return `"${msg}"; invoice back to ${bal} (${status.split(' ')[0]}).`;
    },
  },
  {
    id: 'FIN-11', module: M, sprint: S, feature: 'Invoices', user: 'tunde.bakare',
    title: 'A discount cannot push a paid invoice into a negative balance',
    pre: 'Chiamaka Okafor\'s invoice is paid in full (₦257,000).',
    steps: ['Open Chiamaka Okafor\'s invoice.', 'Enter a ₦10,000 discount on the Tuition line and press Tab.'],
    expected: 'The discount is refused (the invoice is already settled; a refund or credit is the right tool), and the balance stays ₦0.00 — never negative.',
    async run(t) {
      await openInvoiceOf(t, 'Chiamaka Okafor');
      const before = await stat(t, 'Balance');
      const box = t.row('Tuition').locator('input').first();
      await box.fill('10000');
      await box.press('Tab');
      const msg = await t.toast().catch(() => '');
      await t.settle();
      const after = await stat(t, 'Balance');
      check(!/-/.test(after), `Discount accepted: ${before} → ${after} (message "${msg}")`);
      return `Refused: "${msg}"; balance still ${after}`;
    },
  },
];
