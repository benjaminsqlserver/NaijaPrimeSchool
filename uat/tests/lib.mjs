// Shared helpers for the UAT scripts. Each test case receives a `t` object
// (see makeT) wrapping a Playwright page signed in as the case's user, with
// small helpers that match how the app's Radzen pages are built:
//   <div class="nps-field"><label>Name *</label><RadzenTextBox .../></div>
import { expect } from 'playwright/test';

export const BASE = process.env.UAT_BASE_URL ?? 'http://localhost:5080';

// Accounts created by uat/seed (see uat/README.md).
export const USERS = {
  superadmin: { password: 'Admin@12345', role: 'SuperAdmin', name: 'Super Admin' },
  'folake.adeyemi': { password: 'Uat@12345', role: 'HeadTeacher', name: 'Mrs. Folake Adeyemi' },
  'chinedu.okeke': { password: 'Uat@12345', role: 'Teacher', name: 'Mr. Chinedu Okeke (Primary 5A class teacher)' },
  'aisha.bello': { password: 'Uat@12345', role: 'Teacher', name: 'Miss Aisha Bello (Primary 1A class teacher)' },
  'tunde.bakare': { password: 'Uat@12345', role: 'SchoolBursar', name: 'Mr. Tunde Bakare' },
  'musa.ibrahim': { password: 'Uat@12345', role: 'SchoolStoreKeeper', name: 'Mr. Musa Ibrahim' },
  'ngozi.okafor': { password: 'Family@123', role: 'Parent', name: 'Mrs. Ngozi Okafor (mother of Chiamaka P5A and Tobenna P1A)' },
  'yetunde.balogun': { password: 'Family@123', role: 'Parent', name: 'Mrs. Yetunde Balogun (mother of Damilola)' },
  'chukwudi.eze': { password: 'Family@123', role: 'Parent', name: 'Mr. Chukwudi Eze (father of Kamsi P5A — fees unpaid — and Obinna P1A)' },
  'chiamaka.okafor': { password: 'Family@123', role: 'Student', name: 'Chiamaka Okafor (Primary 5A)' },
  'tobenna.okafor': { password: 'Family@123', role: 'Student', name: 'Tobenna Okafor (Primary 1A)' },
  'oluwaseun.akinola': { password: 'Family@123', role: 'Student', name: 'Oluwaseun Akinola (Primary 5A, fees unpaid)' },
};

// Grid rows; the inbox's RowRender swaps Radzen's row class for its own.
const ROW = '.rz-data-row, .rz-data-grid tbody > tr.nps-thread-row';

export const today = new Date();
const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
/** Formats a date the way the app's date pickers expect ("d MMM yyyy"). */
export const dmy = d => `${d.getDate()} ${MONTHS[d.getMonth()]} ${d.getFullYear()}`;
export const addDays = (d, n) => { const x = new Date(d); x.setDate(x.getDate() + n); return x; };
/** Unique suffix so re-runs never collide with earlier data. */
export const stamp = Date.now().toString(36).slice(-5).toUpperCase();

export class Check extends Error {}
export function check(condition, message) {
  if (!condition) throw new Check(message);
}

export function makeT(page, ctx) {
  const t = {
    page,
    ctx,
    notes: [],
    note(s) { t.notes.push(s); },

    async go(path) {
      const resp = await page.goto(BASE + path);
      await t.settle();
      return resp;
    },

    /** Waits for the Blazor circuit to render and for Radzen grids to finish loading. */
    async settle(ms = 700) {
      await page.waitForLoadState('load');
      await page.waitForTimeout(ms);
      await page.locator('.rz-datatable-loading, .rz-grid-loading').first().waitFor({ state: 'detached', timeout: 10000 }).catch(() => {});
      // Grids that load their data on the server render after the first paint.
      if (await page.locator('.rz-data-grid').count()) {
        await page.locator(`${ROW}, .rz-datatable-emptymessage`).first().waitFor({ timeout: 6000 }).catch(() => {});
        // An empty grid may just be one that has not received its data yet.
        if (!(await page.locator(ROW).count())) await page.locator(ROW).first().waitFor({ timeout: 1500 }).catch(() => {});
        await page.waitForTimeout(200);
      }
    },

    path() { return page.url().replace(BASE, ''); },

    field(label) {
      return page.locator('.nps-field').filter({ has: page.locator('label', { hasText: new RegExp(`^\\s*${esc(label)}\\s*\\*?\\s*$`) }) }).first();
    },

    async fill(label, value) {
      const input = t.field(label).locator('input:not([type=hidden]):not([type=checkbox]), textarea').first();
      await input.fill(String(value));
      await input.press('Tab');
    },

    async date(label, d) {
      const input = t.field(label).locator('input').first();
      await input.fill(typeof d === 'string' ? d : dmy(d));
      await input.press('Tab');
      await page.waitForTimeout(250);
    },

    /** Opens the Radzen drop-down in the labelled field and picks the option with this text. */
    async pick(label, option, { exact = false } = {}) {
      const field = typeof label === 'string' ? t.field(label) : label;
      await field.locator('.rz-dropdown').first().click();
      const panel = page.locator('.rz-dropdown-panel:visible, .rz-popup:visible').last();
      const filter = panel.locator('input:visible').first();
      if (await filter.count()) { await filter.fill(option); await page.waitForTimeout(400); }
      const items = panel.locator('.rz-dropdown-item');
      const item = exact ? items.filter({ hasText: new RegExp(`^\\s*${esc(option)}(\\s+[—·(-].*)?\\s*$`) }) : items.filter({ hasText: option });
      await item.first().click();
      await page.waitForTimeout(400);
    },

    async toggle(label) {
      await t.field(label).locator('.rz-switch, .rz-chkbox-box').first().click();
      await page.waitForTimeout(200);
    },

    /** Ticks the checkbox (RadzenCheckBoxList / RadzenCheckBox) whose label is exactly `text`. */
    async tick(text) {
      await page.locator('div:has(> .rz-chkbox)').filter({ hasText: new RegExp(`^\\s*${esc(text)}\\s*$`) }).locator('.rz-chkbox-box').first().click();
      await page.waitForTimeout(250);
    },

    /** Clicks the button whose visible label is `name` (Radzen puts the icon ligature in the accessible name, so match the label span). */
    async click(name, { exact = false } = {}) {
      const re = name instanceof RegExp ? name : exact ? new RegExp(`^\\s*${esc(name)}\\s*$`, 'i') : new RegExp(esc(name), 'i');
      const byLabel = page.locator('button:visible').filter({ has: page.locator('.rz-button-text', { hasText: re }) });
      const target = (await byLabel.count()) ? byLabel.last() : page.locator('button:visible').filter({ hasText: re }).last();
      await target.click();
      await page.waitForTimeout(500);
    },

    /** Clicks an icon button (identified by its title) in the grid row containing `rowText`. */
    async rowAction(rowText, title) {
      const row = t.row(rowText);
      await row.locator(`button[title="${title}"]`).first().click();
      await page.waitForTimeout(600);
    },

    row(text) { return page.locator(ROW).filter({ hasText: text }).first(); },
    rows() { return page.locator(ROW); },
    async rowCount() { return page.locator(ROW).count(); },

    /** Waits for a Radzen toast and returns its text. */
    async toast(timeout = 10000) {
      const msg = page.locator('.rz-notification-item').last();
      await msg.waitFor({ state: 'visible', timeout });
      const text = (await msg.innerText()).replace(/\s+/g, ' ').trim();
      await page.locator('.rz-notification-item').evaluateAll(els => els.forEach(e => e.remove())).catch(() => {});
      return text;
    },

    async expectToast(re, timeout) {
      const text = await t.toast(timeout);
      check(re.test(text), `Expected a message matching ${re} but got "${text}"`);
      return text;
    },

    /** Presses the given button in the open confirmation dialog. */
    /** Waits for any on-screen feedback (toast, inline validation, alert) matching `re`; returns the matching line. */
    async expectMessage(re, timeout = 8000) {
      const end = Date.now() + timeout;
      while (Date.now() < end) {
        const body = await page.locator('body').innerText();
        const line = body.split('\n').find(l => re.test(l));
        if (line) return line.trim();
        await page.waitForTimeout(250);
      }
      throw new Check(`No message matching ${re} appeared`);
    },

    /** Confirms a dialog only if one opens (some actions ask first, some don't). */
    async maybeConfirm(button) {
      const dialog = page.locator('.rz-dialog:visible').last();
      if (await dialog.waitFor({ timeout: 1200 }).then(() => true, () => false)) {
        await dialog.getByRole('button', { name: button }).first().click();
        await page.waitForTimeout(500);
      }
    },

    async confirm(button = /^(Ok|Yes|Delete|Confirm|Withdraw|Remove|Deactivate|Publish|Cancel invoice|Refund|Send)/i) {
      const dialog = page.locator('.rz-dialog:visible').last();
      await dialog.waitFor({ timeout: 5000 });
      await dialog.getByRole('button', { name: button }).first().click();
      await page.waitForTimeout(700);
    },

    async text(selector = 'body') { return (await page.locator(selector).first().innerText()).replace(/\s+/g, ' ').trim(); },

    async see(text, timeout = 8000) {
      await page.getByText(text).first().waitFor({ state: 'visible', timeout });
    },

    async seeNot(text) {
      const n = await page.getByText(text).count();
      check(n === 0 || !(await page.getByText(text).first().isVisible()), `Did not expect to see "${text}"`);
    },

    async heading() { return (await page.locator('h1').first().innerText()).trim(); },

    async menu() {
      return (await page.locator('.nps-panel-menu .rz-navigation-item-text').allInnerTexts()).map(s => s.trim());
    },

    /** Asserts the page is refused (redirected to Access denied). */
    async expectDenied(path) {
      await t.go(path);
      check(t.path().startsWith('/Account/AccessDenied'), `${path} should be refused but loaded ${t.path()}`);
    },

    async expectAllowed(path, heading) {
      await t.go(path);
      check(!t.path().startsWith('/Account/'), `${path} was refused (${t.path()})`);
      if (heading) {
        const h = await t.heading();
        check(heading instanceof RegExp ? heading.test(h) : h.includes(heading), `Expected heading "${heading}" but got "${h}"`);
      }
    },

    expect,
  };
  return t;
}

export async function login(browser, user, { password } = {}) {
  const ctx = await browser.newContext({ viewport: { width: 1400, height: 950 }, locale: 'en-GB', timezoneId: 'Africa/Lagos' });
  const page = await ctx.newPage();
  await page.goto(BASE + '/Account/Login');
  await page.fill('#Input\\.Email', user);
  await page.fill('#Input\\.Password', password ?? USERS[user].password);
  await Promise.all([page.waitForURL(u => !u.pathname.startsWith('/Account/Login'), { timeout: 15000 }), page.click('button[type=submit]')]);
  await page.close();
  return ctx;
}

function esc(s) { return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'); }
