// Captures the screenshots used in the user manual from a freshly seeded demo
// school (uat/reset-and-start.sh). Run from this folder:  node capture.mjs [name ...]
import { chromium } from '../../uat/tests/node_modules/playwright/index.mjs';
import fs from 'node:fs';
import { login, makeT, BASE, today, addDays } from '../../uat/tests/lib.mjs';

const OUT = new URL('./shots/', import.meta.url).pathname;
fs.mkdirSync(OUT, { recursive: true });
const only = process.argv.slice(2);

const W = 1366, H = 860;
const browser = await chromium.launch();
const contexts = new Map();
async function ctxFor(user) {
  if (!user) return browser.newContext({ viewport: { width: W, height: H }, locale: 'en-GB', timezoneId: 'Africa/Lagos' });
  if (!contexts.has(user)) {
    const c = await login(browser, user);
    contexts.set(user, c);
  }
  return contexts.get(user);
}

async function shot(t, name, { full = false, maxHeight = 1500, selector } = {}) {
  await t.page.waitForTimeout(500);
  const path = `${OUT}${name}.jpg`;
  if (selector) {
    await t.page.locator(selector).first().screenshot({ path, type: 'jpeg', quality: 82 });
  } else if (full) {
    const h = await t.page.evaluate(() => document.documentElement.scrollHeight);
    await t.page.screenshot({ path, type: 'jpeg', quality: 82, clip: { x: 0, y: 0, width: W, height: Math.min(h, maxHeight) }, fullPage: true });
  } else {
    await t.page.screenshot({ path, type: 'jpeg', quality: 82 });
  }
  console.log('shot', name);
}
const scrollTo = async (t, text) => { await t.page.getByText(text).first().scrollIntoViewIfNeeded(); await t.page.waitForTimeout(300); };

const S = [];
const add = (name, user, run) => S.push({ name, user, run });

// ---------------------------------------------------------------- getting started
add('login', null, async t => { await t.go('/Account/Login'); await shot(t, 'login'); });
add('login-filled', null, async t => {
  await t.go('/Account/Login');
  await t.page.fill('#Input\\.Email', 'folake.adeyemi');
  await t.page.fill('#Input\\.Password', 'Uat@12345');
  await shot(t, 'login-filled');
});
add('dashboard-admin', 'superadmin', async t => { await t.go('/'); await shot(t, 'dashboard-admin'); });
add('dashboard-head', 'folake.adeyemi', async t => { await t.go('/'); await shot(t, 'dashboard-head'); });
add('dashboard-teacher', 'chinedu.okeke', async t => { await t.go('/'); await shot(t, 'dashboard-teacher'); });
add('dashboard-parent', 'ngozi.okafor', async t => { await t.go('/'); await shot(t, 'dashboard-parent'); });
add('access-denied', 'chinedu.okeke', async t => { await t.go('/users'); await shot(t, 'access-denied'); });

// ---------------------------------------------------------------- users
add('users', 'superadmin', async t => { await t.go('/users'); await shot(t, 'users'); });
add('user-new', 'superadmin', async t => {
  await t.go('/users/new');
  await t.pick('Title', 'Mrs.');
  await t.fill('First name', 'Kemi'); await t.fill('Last name', 'Adewale'); await t.pick('Gender', 'Female');
  await t.fill('Username', 'kemi.adewale'); await t.fill('Email', 'kemi.adewale@naijaprimeschool.ng');
  await t.fill('Phone', '08039998877');
  await t.fill('Password', 'Teach@2026'); await t.fill('Confirm password', 'Teach@2026');
  await t.tick('Teacher');
  await shot(t, 'user-new', { full: true });
});
add('user-edit', 'superadmin', async t => {
  await t.go('/users'); await t.fill('Search', 'grace'); await t.settle(900); await t.rowAction('grace.udoh', 'Edit'); await t.settle();
  await shot(t, 'user-edit');
  await t.click('Reset password'); await t.page.waitForTimeout(600);
  await shot(t, 'user-reset-password');
});
add('user-roles', 'superadmin', async t => { await t.go('/users'); await t.fill('Search', 'grace'); await t.settle(900); await t.rowAction('grace.udoh', 'Assign roles'); await t.settle(); await shot(t, 'user-roles'); });
add('roles', 'superadmin', async t => { await t.go('/roles'); await shot(t, 'roles'); });

// ---------------------------------------------------------------- academics
add('sessions', 'folake.adeyemi', async t => {
  await t.go('/sessions'); await shot(t, 'sessions');
  await t.click('New session'); await shot(t, 'session-new', { full: true });
});
add('terms', 'folake.adeyemi', async t => { await t.go('/terms'); await shot(t, 'terms'); });
add('classes', 'folake.adeyemi', async t => {
  await t.go('/classes'); await shot(t, 'classes');
  await t.click('New class'); await shot(t, 'class-new', { full: true });
});
add('subjects', 'folake.adeyemi', async t => { await t.go('/subjects'); await shot(t, 'subjects'); });
add('periods', 'folake.adeyemi', async t => { await t.go('/timetable-periods'); await shot(t, 'periods'); });
add('timetable', 'folake.adeyemi', async t => {
  await t.go('/timetable'); await t.pick('Term', 'First Term'); await t.pick('Class', 'Primary 5A', { exact: true }); await t.settle();
  await shot(t, 'timetable', { full: true, maxHeight: 1250 });
  await t.page.locator('.nps-timetable-grid tbody tr').nth(0).locator('td').nth(0).click(); await t.page.waitForTimeout(600);
  await t.page.locator('h3', { hasText: 'Period 1' }).scrollIntoViewIfNeeded();
  await shot(t, 'timetable-edit');
});

// ---------------------------------------------------------------- family
add('students', 'folake.adeyemi', async t => { await t.go('/students'); await t.page.waitForTimeout(1500); await shot(t, 'students'); });
add('student-new', 'folake.adeyemi', async t => {
  await t.go('/students/new');
  await t.fill('Admission number', 'NPS/26/031'); await t.date('Admission date', today);
  await t.fill('First name', 'Adaeze'); await t.fill('Last name', 'Nnaji'); await t.date('Date of birth', '3 Mar 2018');
  await t.pick('Gender', 'Female'); await t.pick('Blood group', 'O+', { exact: true }); await t.fill('State of origin', 'Enugu');
  await t.pick('Class', 'Primary 3A', { exact: true });
  await t.fill('Username', 'adaeze.nnaji'); await t.fill('Email', 'adaeze.nnaji@students.naijaprimeschool.ng'); await t.fill('Initial password', 'Family@123');
  await shot(t, 'student-new', { full: true, maxHeight: 1700 });
});
add('student-detail', 'folake.adeyemi', async t => {
  await t.go('/students'); await t.fill('Search', 'Chiamaka'); await t.settle(900); await t.rowAction('Chiamaka Okafor', 'Edit'); await t.settle();
  await shot(t, 'student-profile');
  await t.page.getByRole('tab', { name: /Parents/ }).click(); await t.page.waitForTimeout(500); await shot(t, 'student-parents');
  await t.click('Link a parent'); await t.page.waitForTimeout(500); await scrollTo(t, 'Save link'); await shot(t, 'student-link-parent');
  await t.page.getByRole('tab', { name: /Enrolment history/ }).click(); await t.page.waitForTimeout(500); await shot(t, 'student-enrolments');
  await t.page.getByRole('tab', { name: 'Photo' }).click(); await t.page.waitForTimeout(500); await shot(t, 'student-photo');
});
add('parents', 'folake.adeyemi', async t => { await t.go('/parents'); await t.page.waitForTimeout(1500); await shot(t, 'parents'); });
add('parent-new', 'folake.adeyemi', async t => { await t.go('/parents/new'); await shot(t, 'parent-new', { full: true }); });
add('parent-detail', 'folake.adeyemi', async t => {
  await t.go('/parents'); await t.fill('Search', 'Ngozi'); await t.settle(900); await t.rowAction('Ngozi Okafor', 'Edit'); await t.settle();
  await shot(t, 'parent-detail');
  await t.page.getByRole('tab', { name: 'Reminders' }).click(); await t.page.waitForTimeout(600); await shot(t, 'parent-reminders');
});
add('enrolments', 'folake.adeyemi', async t => { await t.go('/enrolments'); await t.pick('Class', 'Primary 5A', { exact: true }); await t.settle(); await shot(t, 'enrolments'); });

// ---------------------------------------------------------------- attendance
add('attendance-daily', 'chinedu.okeke', async t => {
  await t.go('/attendance/daily'); await t.pick('Class', 'Primary 5A', { exact: true }); await t.date('Date', today); await t.settle();
  await shot(t, 'attendance-open');
  await t.click('Open register'); await t.toast(); await t.settle();
  await t.pick(t.row('Kamsi Eze'), 'Absent', { exact: true });
  await t.pick(t.row('Aminu Ibrahim'), 'Late', { exact: true });
  const a = t.row('Aminu Ibrahim').locator('input[placeholder="HH:mm"]'); await a.fill('08:25'); await a.press('Tab');
  await shot(t, 'attendance-register', { full: true, maxHeight: 1300 });
});
add('attendance-subject', 'chinedu.okeke', async t => {
  await t.go('/attendance/subject'); await t.pick('Term', 'First Term'); await t.pick('Class', 'Primary 5A', { exact: true }); await t.date('Date', addDays(today, -1)); await t.settle();
  await shot(t, 'attendance-subject');
});
add('attendance-summary', 'folake.adeyemi', async t => {
  await t.go('/attendance/summary'); await t.pick('Class', 'Primary 5A', { exact: true }); await t.pick('Term', 'First Term').catch(() => {}); await t.settle();
  await shot(t, 'attendance-summary', { full: true, maxHeight: 1200 });
});

// ---------------------------------------------------------------- results
add('assessments', 'chinedu.okeke', async t => {
  await t.go('/assessments'); await t.pick('Class', 'Primary 5A', { exact: true }); await t.settle(); await shot(t, 'assessments');
  await t.click('New assessment'); await t.page.waitForTimeout(500); await scrollTo(t, 'Maximum score'); await shot(t, 'assessment-new');
});
add('scores', 'chinedu.okeke', async t => {
  await t.go('/assessments'); await t.pick('Class', 'Primary 5A', { exact: true }); await t.settle();
  await t.rows().first().locator('button[title="Enter scores"]').click(); await t.settle();
  await shot(t, 'assessment-scores');
});
add('results', 'folake.adeyemi', async t => {
  await t.go('/results'); await t.pick('Term', 'First Term'); await t.pick('Class', 'Primary 5A', { exact: true }); await t.pick('Subject (optional)', 'Mathematics'); await t.settle();
  await shot(t, 'results');
});
add('reports', 'folake.adeyemi', async t => {
  await t.go('/reports'); await t.pick('Class', 'Primary 5A', { exact: true }); await t.settle(); await shot(t, 'report-cards');
  await t.click('Generate / refresh'); await t.page.waitForTimeout(500); await scrollTo(t, 'Generate / refresh report cards'); await shot(t, 'report-generate');
});
add('report-detail', 'folake.adeyemi', async t => {
  await t.go('/reports'); await t.pick('Class', 'Primary 5A', { exact: true }); await t.settle();
  await t.rowAction('Ekaette Okon', 'Open'); await t.settle(); await shot(t, 'report-detail');
  await t.page.getByRole('tab', { name: 'Affective traits' }).click(); await t.page.waitForTimeout(500); await scrollTo(t, 'Pick a rating'); await shot(t, 'report-traits');
  await t.page.getByRole('tab', { name: 'Comments' }).click(); await t.page.waitForTimeout(500); await scrollTo(t, 'Save comments'); await shot(t, 'report-comments');
});

// ---------------------------------------------------------------- finance
add('finance', 'tunde.bakare', async t => { await t.go('/finance'); await shot(t, 'finance-dashboard', { full: true, maxHeight: 1400 }); });
add('fees', 'tunde.bakare', async t => {
  await t.go('/fees'); await shot(t, 'fee-schedules');
  await t.rows().first().locator('button[title="Edit items"]').click(); await t.settle(); await shot(t, 'fee-schedule-detail', { full: true });
});
add('fee-new', 'tunde.bakare', async t => { await t.go('/fees'); await t.click('New schedule'); await t.page.waitForTimeout(500); await scrollTo(t, 'New fee schedule'); await shot(t, 'fee-schedule-new'); });
add('issue', 'tunde.bakare', async t => {
  await t.go('/invoices/issue'); await t.pick('Term', 'First Term'); await t.pick('Fee schedule', 'Primary 5'); await t.pick('Class', 'Primary 5A', { exact: true });
  await shot(t, 'invoices-issue');
});
add('invoices', 'tunde.bakare', async t => { await t.go('/invoices'); await shot(t, 'invoices'); });
add('invoice-detail', 'tunde.bakare', async t => {
  await t.go('/invoices'); await t.fill('Search', 'Ekaette'); await t.settle(900); await t.rowAction('Ekaette Okon', 'View'); await t.settle();
  await shot(t, 'invoice-detail', { full: true });
  await t.click('Record payment'); await t.settle();
  await t.pick('Method', 'Bank Transfer', { exact: true }); await t.fill('Reference', 'GTB-448812');
  await shot(t, 'payment-record', { full: true });
});
add('payments', 'tunde.bakare', async t => {
  await t.go('/payments'); await shot(t, 'payments');
  await t.rows().first().locator('button').last().click(); await t.settle(); await shot(t, 'payment-receipt', { full: true });
});

// ---------------------------------------------------------------- online payments (parent)
add('pay-online', 'chukwudi.eze', async t => {
  await t.go('/portal/parent');
  await t.page.locator('.nps-card, .rz-card').filter({ hasText: 'Kamsi' }).last().getByRole('button', { name: /Open/ }).first().click(); await t.settle();
  await t.page.getByRole('tab', { name: 'Fees & invoices' }).click(); await t.page.waitForTimeout(600);
  await shot(t, 'ward-fees');
  await t.click('Pay online'); await t.settle(); await t.fill('Amount to pay (₦)', 100000); await t.page.waitForTimeout(400);
  await shot(t, 'pay-invoice');
  await t.click(/Pay ₦.* securely/); await t.page.waitForURL(u => u.pathname.startsWith('/payments/simulator')); await t.settle();
  await shot(t, 'pay-checkout');
  await t.click('Pay (simulate success)'); await t.page.waitForURL(u => u.pathname.startsWith('/portal/payments/return'));
  await t.page.getByText('Payment received').waitFor(); await shot(t, 'pay-success');
});
add('online-payments', 'tunde.bakare', async t => { await t.go('/finance/online-payments'); await shot(t, 'online-payments'); });

// ---------------------------------------------------------------- store
add('store', 'musa.ibrahim', async t => { await t.go('/store'); await shot(t, 'store-dashboard', { full: true, maxHeight: 1400 }); });
add('store-items', 'musa.ibrahim', async t => {
  await t.go('/store/items'); await shot(t, 'store-items');
  await t.rowAction('HB pencils', 'Open'); await t.settle(); await shot(t, 'store-item-detail', { full: true });
});
add('store-new', 'musa.ibrahim', async t => { await t.go('/store/items/new'); await shot(t, 'store-item-new', { full: true }); });
add('movement-new', 'musa.ibrahim', async t => {
  await t.go('/store/movements/new');
  await t.pick('Item', 'HB pencils'); await t.pick('Movement type', 'Purchase', { exact: true }); await t.fill('Quantity', 20); await t.fill('Unit cost', 950);
  await t.fill('Reference', 'LPO-2701'); await t.pick('Supplier', 'Lagos Book Hub');
  await shot(t, 'movement-new', { full: true });
});
add('movements', 'musa.ibrahim', async t => { await t.go('/store/movements'); await shot(t, 'movements'); });
add('suppliers', 'musa.ibrahim', async t => { await t.go('/store/suppliers'); await shot(t, 'suppliers'); });

// ---------------------------------------------------------------- communications
add('announcements', 'folake.adeyemi', async t => { await t.go('/announcements'); await shot(t, 'announcements'); });
add('announcement-new', 'folake.adeyemi', async t => {
  await t.go('/announcements/new');
  await t.fill('Title', 'Primary 1A sports day');
  await t.fill('Body', 'Primary 1A sports day holds on Friday at 10am on the school field. Pupils should come in their house colours. Parents are welcome!');
  await t.pick('Category', 'Events', { exact: true }); await t.pick('Audience', 'Specific Class', { exact: true }); await t.pick('Target class', 'Primary 1A', { exact: true });
  await shot(t, 'announcement-new', { full: true });
});
add('notification-log', 'folake.adeyemi', async t => { await t.go('/announcements/notifications'); await shot(t, 'notification-log'); });
add('inbox', 'folake.adeyemi', async t => {
  await t.go('/messages'); await shot(t, 'inbox');
  await t.row('School bus route').click(); await t.settle(); await t.page.getByPlaceholder('Write a reply...').fill('Yes — Bus 2 passes Ogui Road at 7:05am. We will add your address to the route.');
  await shot(t, 'inbox-thread');
});
add('message-new', 'folake.adeyemi', async t => { await t.go('/messages/new'); await shot(t, 'message-new'); });
add('audit', 'folake.adeyemi', async t => {
  await t.go('/admin/audit-log'); await t.rows().first().locator('.rz-row-toggler').click(); await t.page.waitForTimeout(600); await shot(t, 'audit-log');
  await t.rows().first().getByRole('button', { name: /History/ }).click(); await t.settle(1200); await shot(t, 'audit-history');
});

// ---------------------------------------------------------------- parent portal
add('parent-portal', 'ngozi.okafor', async t => {
  await t.go('/portal/parent'); await shot(t, 'parent-wards');
  await t.page.locator('.nps-card, .rz-card').filter({ hasText: 'Chiamaka' }).last().getByRole('button', { name: /Open/ }).first().click(); await t.settle();
  await shot(t, 'ward-overview');
  await t.page.getByRole('tab', { name: 'Report cards' }).click(); await t.page.waitForTimeout(500); await shot(t, 'ward-report-cards');
  await t.rows().first().locator('button').last().click(); await t.settle(1200); await shot(t, 'portal-report-card', { full: true, maxHeight: 1700 });
});
add('portal-announcements', 'ngozi.okafor', async t => { await t.go('/portal/announcements'); await shot(t, 'portal-announcements'); });
add('portal-messages', 'ngozi.okafor', async t => {
  await t.go('/portal/messages'); await shot(t, 'portal-messages');
  await t.page.locator('.nps-thread-row').first().click(); await t.settle(); await shot(t, 'portal-thread');
  await t.go('/portal/messages/new'); await shot(t, 'portal-message-new');
});
add('portal-reminders', 'ngozi.okafor', async t => { await t.go('/portal/notifications'); await shot(t, 'portal-reminders'); });

// ---------------------------------------------------------------- student portal
add('student-portal', 'chiamaka.okafor', async t => {
  await t.go('/portal/student'); await shot(t, 'student-today');
  await t.go('/portal/student/profile'); await shot(t, 'student-my-profile');
  await t.go('/portal/student/results'); await shot(t, 'student-results');
  await t.go('/portal/student/attendance'); await shot(t, 'student-attendance');
  await t.go('/portal/student/fees'); await shot(t, 'student-fees');
});

for (const s of S) {
  if (only.length && !only.includes(s.name)) continue;
  const ctx = await ctxFor(s.user);
  const page = await ctx.newPage();
  const t = makeT(page, ctx);
  try { await s.run(t); } catch (e) { console.error('FAILED', s.name, e.message.split('\n')[0]); }
  await page.close();
  if (!s.user) await ctx.close();
}
await browser.close();
