// Runs the UAT catalogue in a real (headless Chromium) browser and records
// the outcome of every case, with a screenshot, in uat/results/<cycle>/.
//
//   node run.mjs --cycle cycle-1                 run everything
//   node run.mjs --cycle cycle-2 --only FIN-03,FIN-04
//   node run.mjs --cycle scratch --module Finance
//
// Cases run in catalogue order because later cases build on data earlier
// ones create. Start from a freshly seeded database (see uat/README.md).
import { chromium } from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { login, makeT, USERS, Check } from './lib.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const arg = name => { const i = process.argv.indexOf(`--${name}`); return i > 0 ? process.argv[i + 1] : undefined; };
const cycle = arg('cycle') ?? 'cycle-1';
const only = arg('only')?.split(',').map(s => s.trim());
const moduleFilter = arg('module');

const outDir = path.join(here, '..', 'results', cycle);
const shotDir = path.join(outDir, 'shots');
fs.mkdirSync(shotDir, { recursive: true });
const resultsFile = path.join(outDir, 'results.json');

const files = fs.readdirSync(path.join(here, 'cases')).filter(f => f.endsWith('.mjs')).sort();
let cases = [];
for (const f of files) cases.push(...(await import(`./cases/${f}`)).default);
const ids = new Set();
for (const c of cases) { if (ids.has(c.id)) throw new Error(`Duplicate test id ${c.id}`); ids.add(c.id); }
if (only) cases = cases.filter(c => only.includes(c.id));
if (moduleFilter) cases = cases.filter(c => c.module === moduleFilter);

// Keep earlier results for cases not re-run (so --only re-tests update in place).
const previous = fs.existsSync(resultsFile) ? JSON.parse(fs.readFileSync(resultsFile, 'utf8')) : { cases: [] };
const results = new Map(previous.cases.map(r => [r.id, r]));

const browser = await chromium.launch();
const contexts = new Map();
async function contextFor(user) {
  if (!user) {
    if (!contexts.has('')) contexts.set('', await browser.newContext({ viewport: { width: 1400, height: 950 }, locale: 'en-GB', timezoneId: 'Africa/Lagos' }));
    return contexts.get('');
  }
  if (!contexts.has(user)) contexts.set(user, await login(browser, user));
  return contexts.get(user);
}

const save = () => {
  const all = [...results.values()].sort((a, b) => a.order - b.order);
  fs.writeFileSync(resultsFile, JSON.stringify({
    cycle, runAt: new Date().toISOString(), baseUrl: process.env.UAT_BASE_URL ?? 'http://localhost:5080',
    browser: `Chromium ${browser.version()}`, cases: all,
  }, null, 1));
};

const order = new Map([...ids].map((id, i) => [id, i]));
let pass = 0, fail = 0;
for (const c of cases) {
  const started = Date.now();
  const ctx = await contextFor(c.user);
  const page = await ctx.newPage();
  const pageErrors = [];
  page.on('pageerror', e => pageErrors.push(e.message));
  const t = makeT(page, ctx);
  t.browser = browser;
  let status = 'Pass', actual = '', error;
  try {
    actual = (await c.run(t)) ?? 'As expected.';
    const blazorError = await page.locator('#blazor-error-ui').isVisible().catch(() => false);
    if (blazorError) throw new Check('The page showed "An unhandled error has occurred."');
  } catch (e) {
    status = 'Fail';
    actual = e instanceof Check ? e.message : `Step could not be completed: ${e.message.split('\n')[0]}`;
    error = e instanceof Check ? undefined : e.stack?.split('\n').slice(0, 4).join('\n');
  }
  // Icons whose name is not in the icon font render as a word instead of a glyph.
  const brokenIcons = await page.evaluate(() => [...document.querySelectorAll('.rzi, .rz-button-icon-left, i.material-icons')]
    .filter(e => { const r = e.getBoundingClientRect(); const fs = parseFloat(getComputedStyle(e).fontSize) || 16; return r.width > fs * 1.7 && e.textContent.trim().length > 3; })
    .map(e => e.textContent.trim())).catch(() => []);
  if (brokenIcons.length) t.note(`Icons shown as text: ${[...new Set(brokenIcons)].join(', ')}.`);
  const shot = `shots/${c.id}.jpg`;
  await page.screenshot({ path: path.join(outDir, shot), type: 'jpeg', quality: 60, fullPage: true }).catch(() => {});
  if (pageErrors.length) t.note(`Browser errors: ${pageErrors.join(' | ')}`);
  await page.close();

  results.set(c.id, {
    order: order.get(c.id), id: c.id, module: c.module, sprint: c.sprint, feature: c.feature, title: c.title,
    role: c.user ? USERS[c.user].role : 'Anonymous', user: c.user ?? '(not signed in)',
    preconditions: c.pre ?? '', steps: c.steps, expected: c.expected,
    status, actual: [actual, ...t.notes].join(' '), error, screenshot: shot,
    durationMs: Date.now() - started, executedAt: new Date().toISOString(),
  });
  save();
  status === 'Pass' ? pass++ : fail++;
  console.log(`${status === 'Pass' ? 'PASS' : 'FAIL'}  ${c.id.padEnd(9)} ${c.title}${status === 'Fail' ? `\n        → ${actual}` : ''}`);
}
await browser.close();
console.log(`\n${pass} passed, ${fail} failed (${cycle}) → ${path.relative(process.cwd(), resultsFile)}`);
