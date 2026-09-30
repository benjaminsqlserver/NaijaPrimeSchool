// Module: Online fee payment (Sprint 15) — uses the built-in payment simulator (Development).
import { check, login, makeT, BASE } from '../lib.mjs';

const M = 'Online payments';
const S = 'Sprint 15';
const naira = s => Number(String(s).replace(/[^\d.]/g, ''));

async function kamsiFees(t) {
  await t.go('/portal/parent');
  await t.page.locator('.nps-card, .rz-card').filter({ hasText: 'Kamsi' }).last().getByRole('button', { name: /Open/ }).first().click();
  await t.settle();
  await t.page.getByRole('tab', { name: 'Fees & invoices' }).click();
  await t.page.waitForTimeout(500);
}

async function startPayment(t, amount) {
  await kamsiFees(t);
  await t.click('Pay online');
  await t.settle();
  if (amount !== undefined) await t.fill('Amount to pay (₦)', amount);
  await t.click(/Pay ₦.* securely/);
  await t.page.waitForURL(u => u.pathname.startsWith('/payments/simulator'), { timeout: 15000 });
  await t.settle();
}

async function finish(t, button) {
  await t.click(button);
  await t.page.waitForURL(u => u.pathname.startsWith('/portal/payments/return'), { timeout: 15000 });
  await t.page.getByText(/Payment received|Payment not completed|Waiting for confirmation|Being checked/).first().waitFor({ timeout: 15000 });
  return t.text('.nps-card:has(h2)');
}

export default [
  {
    id: 'PAY-01', module: M, sprint: S, feature: 'Pay online', user: 'chukwudi.eze',
    title: '"Pay online" is offered only on invoices with a balance',
    steps: ['Sign in as parent chukwudi.eze.', 'My wards → Kamsi → Fees & invoices.', 'My wards → Obinna → Fees & invoices.'],
    expected: 'Kamsi\'s unpaid First Term invoice (₦257,000) has a Pay online button; Obinna\'s paid invoice does not.',
    async run(t) {
      await kamsiFees(t);
      const kamsi = await t.page.getByRole('button', { name: /Pay online/ }).count();
      await t.go('/portal/parent');
      await t.page.locator('.nps-card, .rz-card').filter({ hasText: 'Obinna' }).last().getByRole('button', { name: /Open/ }).first().click();
      await t.settle();
      await t.page.getByRole('tab', { name: 'Fees & invoices' }).click();
      await t.page.waitForTimeout(500);
      const obinna = await t.page.getByRole('button', { name: /Pay online/ }).count();
      check(kamsi === 1 && obinna === 0, `Pay online buttons — Kamsi: ${kamsi}, Obinna: ${obinna}`);
      return 'Kamsi: 1 Pay online button; Obinna (paid): none.';
    },
  },
  {
    id: 'PAY-02', module: M, sprint: S, feature: 'Pay online', user: 'chukwudi.eze',
    title: 'Payment page validates the amount',
    steps: ['Press Pay online on Kamsi\'s invoice.', 'Enter ₦50 and press Tab.', 'Enter ₦900,000 and press Tab.'],
    expected: 'The page shows the invoice, pupil, term and ₦257,000 outstanding; ₦50 is raised to the ₦100 minimum and ₦900,000 is capped at the ₦257,000 balance (the Pay button label follows).',
    async run(t) {
      await kamsiFees(t);
      await t.click('Pay online');
      await t.settle();
      const body = await t.text('.nps-page-container');
      check(/Kamsi/.test(body) && /257,000/.test(body), `Pay page: ${body.slice(0, 200)}`);
      const box = t.field('Amount to pay (₦)').locator('input');
      await t.fill('Amount to pay (₦)', 50);
      await t.page.waitForTimeout(400);
      const low = await box.inputValue();
      const lowBtn = await t.text('button:has-text("securely")');
      await t.fill('Amount to pay (₦)', 900000);
      await t.page.waitForTimeout(400);
      const high = await box.inputValue();
      check(naira(low) === 100 && /100\.00/.test(lowBtn), `₦50 became "${low}" (button "${lowBtn}")`);
      check(naira(high) === 257000, `₦900,000 became "${high}"`);
      return `₦50 → ${low} ("${lowBtn}"); ₦900,000 → ${high}.`;
    },
  },
  {
    id: 'PAY-03', module: M, sprint: S, feature: 'Pay online', user: 'chukwudi.eze',
    title: 'Successful part-payment records a receipt',
    steps: ['Pay online on Kamsi\'s invoice; amount ₦100,000; press Pay ₦100,000.00 securely.', 'On the (simulated) Paystack checkout press Pay (simulate success).'],
    expected: '"Payment received" with the amount, invoice number and a receipt number (NPS/RCP/…); the invoice balance falls to ₦157,000 (Partially Paid).',
    async run(t) {
      await startPayment(t, 100000);
      const body = await finish(t, 'Pay (simulate success)');
      check(/Payment received/.test(body) && /NPS\/RCP\//.test(body), `Return page: ${body.slice(0, 200)}`);
      await kamsiFees(t);
      const fees = await t.text('.rz-tabview-panels');
      check(/157,000/.test(fees) && /Partially Paid/i.test(fees), `Fees tab: ${fees.slice(0, 200)}`);
      return `${body.match(/Thank you.*?Receipt number: \S+/)?.[0] ?? body.slice(0, 160)}; balance now ₦157,000.`;
    },
  },
  {
    id: 'PAY-04', module: M, sprint: S, feature: 'Pay online', user: 'chukwudi.eze',
    title: 'Declined card: nothing is recorded',
    steps: ['Pay online on Kamsi\'s invoice (full balance).', 'On checkout press Card declined (simulate failure).'],
    expected: '"Payment not completed — No money was taken" with a Try again button; the balance stays ₦157,000.',
    async run(t) {
      await startPayment(t);
      const body = await finish(t, 'Card declined');
      check(/Payment not completed/.test(body), `Return page: ${body.slice(0, 200)}`);
      await t.see('Try again');
      await kamsiFees(t);
      check(/157,000/.test(await t.text('.rz-tabview-panels')), 'Balance changed');
      return `"${body.slice(0, 120)}"`;
    },
  },
  {
    id: 'PAY-05', module: M, sprint: S, feature: 'Pay online', user: 'chukwudi.eze',
    title: 'Abandoned checkout: nothing is recorded',
    steps: ['Pay online on Kamsi\'s invoice.', 'On checkout press Cancel and return.'],
    expected: '"Payment not completed" (abandoned); no receipt; balance unchanged.',
    async run(t) {
      await startPayment(t, 20000);
      const body = await finish(t, 'Cancel and return');
      check(/not completed/i.test(body) && !/Receipt number/.test(body), `Return page: ${body.slice(0, 200)}`);
      return `"${body.slice(0, 140)}"`;
    },
  },
  {
    id: 'PAY-06', module: M, sprint: S, feature: 'Pay online', user: 'oluwaseun.akinola',
    title: 'A student pays their own fees in full',
    steps: ['Sign in as pupil oluwaseun.akinola.', 'My fees → Pay online; keep the full balance; pay; simulate success.', 'Return to My fees.'],
    expected: '"Payment received"; the invoice shows Paid with ₦0 balance and the Pay online button disappears.',
    async run(t) {
      await t.go('/portal/student/fees');
      await t.click('Pay online');
      await t.settle();
      await t.click(/Pay ₦.* securely/);
      await t.page.waitForURL(u => u.pathname.startsWith('/payments/simulator'), { timeout: 15000 });
      await t.settle();
      const body = await finish(t, 'Pay (simulate success)');
      check(/Payment received/.test(body), `Return page: ${body.slice(0, 200)}`);
      await t.go('/portal/student/fees');
      const pay = await t.page.getByRole('button', { name: /Pay online/ }).count();
      check(pay === 0, 'Pay online still offered');
      return `Paid in full: ${body.match(/Receipt number: \S+/)?.[0]}`;
    },
  },
  {
    id: 'PAY-07', module: M, sprint: S, feature: 'Bursar view', user: 'tunde.bakare',
    title: 'Bursar sees every online attempt and its outcome',
    steps: ['Sign in as the bursar; open Finance → Online payments.'],
    expected: 'Four attempts: two Succeeded (₦100,000 for Kamsi, ₦257,000 for Oluwaseun) with receipt links, one Failed, one Abandoned (with a verify-now button); "Received online" totals ₦357,000.',
    async run(t) {
      await t.go('/finance/online-payments');
      const rows = (await t.rows().allInnerTexts()).map(r => r.replace(/\s+/g, ' '));
      const received = (await t.page.locator('.nps-stat-card', { hasText: 'Received online' }).innerText()).replace(/\s+/g, ' ');
      check(rows.length === 4, `${rows.length} attempts listed`);
      check(rows.filter(r => /Succeeded/i.test(r)).length === 2 && rows.some(r => /Failed/i.test(r)) && rows.some(r => /Abandoned/i.test(r)), `Statuses: ${rows.map(r => r.match(/Succeeded|Failed|Abandoned|Pending|Needs review/i)?.[0]).join(', ')}`);
      check(naira(received) === 357000, `Received online: ${received}`);
      return `${rows.length} attempts; ${received}.`;
    },
  },
  {
    id: 'PAY-08', module: M, sprint: S, feature: 'Bursar view', user: 'tunde.bakare',
    title: 'Online receipts appear alongside counter payments',
    steps: ['Open Finance → Payments; filter Method = Online Payment.'],
    expected: 'The two online receipts are listed with method Online Payment and the Paystack reference (NPS-…).',
    async run(t) {
      await t.go('/payments');
      await t.pick('Method', 'Online Payment', { exact: true });
      await t.settle(900);
      const rows = (await t.rows().allInnerTexts()).map(r => r.replace(/\s+/g, ' '));
      check(rows.length === 2 && rows.every(r => /NPS-\d{8}-/.test(r)), `Rows: ${rows.join(' / ')}`);
      return rows.join(' / ');
    },
  },
  {
    id: 'PAY-09', module: M, sprint: S, feature: 'Webhook', user: null,
    title: 'Payment webhook rejects forged calls',
    steps: ['POST {} to /api/payments/paystack/webhook with header x-paystack-signature: wrong.'],
    expected: 'HTTP 401 Unauthorized; nothing is recorded.',
    async run(t) {
      const resp = await t.page.request.post(BASE + '/api/payments/paystack/webhook', { data: '{}', headers: { 'x-paystack-signature': 'wrong', 'content-type': 'application/json' } });
      check(resp.status() === 401, `Got HTTP ${resp.status()}`);
      return 'HTTP 401 Unauthorized.';
    },
  },
  {
    id: 'PAY-10', module: M, sprint: S, feature: 'Pay online', user: 'folake.adeyemi',
    title: 'Staff previewing a family page cannot pay on their behalf',
    steps: ['Sign in as the head teacher; open Kamsi Eze\'s ward page in the parent portal (preview).', 'Open Fees & invoices.'],
    expected: 'The invoices are shown but there is no Pay online button.',
    async run(t) {
      const id = await (async () => {
        await t.go('/students');
        await t.fill('Search', 'Kamsi');
        await t.settle(900);
        await t.rowAction('Kamsi Eze', 'Edit');
        return t.path().split('/')[2];
      })();
      await t.go(`/portal/parent/wards/${id}`);
      await t.page.getByRole('tab', { name: 'Fees & invoices' }).click();
      await t.page.waitForTimeout(500);
      const n = await t.page.getByRole('button', { name: /Pay online/ }).count();
      check(n === 0, `${n} Pay online buttons shown to staff`);
      return 'Invoices visible; no Pay online button for staff.';
    },
  },
];
