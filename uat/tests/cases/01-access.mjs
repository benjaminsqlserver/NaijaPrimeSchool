// Module: Sign-in, dashboard and role-based access (Sprint 1, applied to every later sprint's pages).
import { check, login, BASE } from '../lib.mjs';

const M = 'Sign-in & access control';

// What each role should and should not be able to open. The menu sections
// are what the sidebar must show; every link in them must actually open.
const MATRIX = {
  superadmin: {
    menu: ['User Management', 'Administration', 'Academics', 'Family', 'Attendance', 'Results & Reports', 'Finance', 'Store & Inventory', 'Communications'],
    allowed: ['/users', '/roles', '/admin/audit-log', '/sessions', '/students', '/attendance/daily', '/reports', '/finance', '/store', '/announcements', '/messages'],
    denied: ['/portal/messages', '/portal/notifications'],
  },
  'folake.adeyemi': {
    menu: ['Administration', 'Academics', 'Family', 'Attendance', 'Results & Reports', 'Finance', 'Store & Inventory', 'Communications'],
    allowed: ['/admin/audit-log', '/sessions', '/timetable', '/students', '/parents', '/attendance/summary', '/results', '/reports', '/finance', '/store', '/announcements', '/messages'],
    denied: ['/users', '/users/new', '/roles'],
  },
  'chinedu.okeke': {
    menu: ['Academics', 'Attendance', 'Results & Reports'],
    allowed: ['/timetable', '/attendance/daily', '/attendance/subject', '/attendance/summary', '/assessments'],
    denied: ['/users', '/admin/audit-log', '/students', '/parents', '/finance', '/invoices', '/store', '/announcements', '/messages', '/portal/parent'],
  },
  'tunde.bakare': {
    menu: ['Finance'],
    allowed: ['/finance', '/fees', '/invoices', '/invoices/issue', '/payments', '/payments/new', '/finance/online-payments'],
    denied: ['/users', '/students', '/attendance/daily', '/assessments', '/store', '/announcements', '/admin/audit-log'],
  },
  'musa.ibrahim': {
    menu: ['Store & Inventory'],
    allowed: ['/store', '/store/items', '/store/items/new', '/store/movements', '/store/movements/new', '/store/suppliers'],
    denied: ['/users', '/students', '/finance', '/payments', '/announcements', '/admin/audit-log'],
  },
  'ngozi.okafor': {
    menu: ['Parent portal'],
    allowed: ['/portal/parent', '/portal/announcements', '/portal/messages', '/portal/notifications'],
    denied: ['/users', '/students', '/finance', '/invoices', '/store', '/messages', '/portal/student', '/admin/audit-log'],
  },
  'chiamaka.okafor': {
    menu: ['Student portal'],
    allowed: ['/portal/student', '/portal/student/profile', '/portal/student/results', '/portal/student/attendance', '/portal/student/fees', '/portal/announcements', '/portal/messages'],
    denied: ['/users', '/students', '/finance', '/store', '/messages', '/portal/parent', '/admin/audit-log'],
  },
};
const SECTIONS = ['User Management', 'Administration', 'Academics', 'Family', 'Attendance', 'Results & Reports', 'Finance', 'Store & Inventory', 'Communications', 'Parent portal', 'Student portal'];

async function checkRole(t, user) {
  const m = MATRIX[user];
  await t.go('/');
  const menu = await t.menu();
  const sections = SECTIONS.filter(s => menu.includes(s));
  const missing = m.menu.filter(s => !sections.includes(s));
  const extra = sections.filter(s => !m.menu.includes(s));
  check(!missing.length && !extra.length, `Menu sections wrong. Missing: [${missing.join(', ')}], unexpected: [${extra.join(', ')}]`);

  // Every link the menu offers must open (no dead ends).
  const links = await t.page.locator('.nps-panel-menu a[href]').evaluateAll(as => as.map(a => a.getAttribute('href')));
  const deadEnds = [];
  for (const href of [...new Set(links)]) {
    const p = href.startsWith('/') ? href : '/' + href;
    await t.go(p);
    if (t.path().startsWith('/Account/AccessDenied')) deadEnds.push(p);
  }
  check(!deadEnds.length, `Menu links that end in "Access denied": ${deadEnds.join(', ')}`);

  for (const p of m.allowed) await t.expectAllowed(p);
  const leaks = [];
  for (const p of m.denied) { await t.go(p); if (!t.path().startsWith('/Account/AccessDenied')) leaks.push(p); }
  check(!leaks.length, `Pages that should be refused but opened: ${leaks.join(', ')}`);
  await t.go('/');
  return `Menu shows ${m.menu.join(', ')}; all ${links.length} menu links open; ${m.allowed.length} permitted pages open; ${m.denied.length} restricted pages are refused with "Access denied".`;
}

export default [
  {
    id: 'AUTH-01', module: M, sprint: 'Sprint 1', feature: 'Sign-in', user: null,
    title: 'Anonymous visitor is sent to the sign-in page',
    steps: ['Open the site without signing in.', 'Try to open /students directly.'],
    expected: 'Both requests land on the sign-in page; no school data is shown.',
    async run(t) {
      await t.go('/');
      check(t.path().startsWith('/Account/Login'), `Home page did not redirect to sign-in (${t.path()})`);
      await t.go('/students');
      check(t.path().startsWith('/Account/Login'), `/students did not redirect to sign-in (${t.path()})`);
      check(t.path().includes('ReturnUrl=%2Fstudents'), 'The return URL was not kept');
      return 'Redirected to /Account/Login with ReturnUrl preserved.';
    },
  },
  {
    id: 'AUTH-02', module: M, sprint: 'Sprint 1', feature: 'Sign-in', user: null,
    title: 'Sign in with username and password',
    steps: ['Open /Account/Login.', 'Enter username "folake.adeyemi" and password "Uat@12345".', 'Press Sign in.'],
    expected: 'The dashboard opens and the header shows the signed-in user and a Sign out button.',
    async run(t) {
      await t.go('/Account/Login');
      await t.page.fill('#Input\\.Email', 'folake.adeyemi');
      await t.page.fill('#Input\\.Password', 'Uat@12345');
      await Promise.all([t.page.waitForURL(u => !u.pathname.startsWith('/Account/Login')), t.page.click('button[type=submit]')]);
      await t.settle();
      await t.see('Sign out');
      const chip = await t.text('.nps-user-chip');
      check(/folake/i.test(chip), `Header shows "${chip}"`);
      await t.ctx.clearCookies();
      return `Signed in; dashboard shows "${await t.heading().catch(() => '')}" and header chip "${chip}".`;
    },
  },
  {
    id: 'AUTH-03', module: M, sprint: 'Sprint 1', feature: 'Sign-in', user: null,
    title: 'Sign in with email address',
    steps: ['Open /Account/Login.', 'Enter email "tunde.bakare@naijaprimeschool.ng" and the password.', 'Press Sign in.'],
    expected: 'Sign-in succeeds with the email address as well as the username.',
    async run(t) {
      await t.go('/Account/Login');
      await t.page.fill('#Input\\.Email', 'tunde.bakare@naijaprimeschool.ng');
      await t.page.fill('#Input\\.Password', 'Uat@12345');
      await Promise.all([t.page.waitForURL(u => !u.pathname.startsWith('/Account/Login')), t.page.click('button[type=submit]')]);
      await t.settle();
      await t.see('Sign out');
      await t.ctx.clearCookies();
      return 'Signed in with the email address.';
    },
  },
  {
    id: 'AUTH-04', module: M, sprint: 'Sprint 1', feature: 'Sign-in', user: null,
    title: 'Wrong password is rejected without revealing which field was wrong',
    steps: ['Open /Account/Login.', 'Enter username "aisha.bello" and password "WrongPass1".', 'Press Sign in.'],
    expected: 'User stays on the sign-in page with a generic "invalid" error message.',
    async run(t) {
      await t.go('/Account/Login');
      await t.page.fill('#Input\\.Email', 'aisha.bello');
      await t.page.fill('#Input\\.Password', 'WrongPass1');
      await t.page.click('button[type=submit]');
      await t.settle(1200);
      check(t.path().startsWith('/Account/Login'), 'Was signed in with a wrong password!');
      const body = await t.text();
      const m = body.match(/(Invalid[^.]*\.|incorrect[^.]*\.)/i);
      check(m, 'No error message shown');
      return `Stayed on sign-in page; message: "${m[0]}"`;
    },
  },
  {
    id: 'AUTH-05', module: M, sprint: 'Sprint 1', feature: 'Sign-out', user: null,
    title: 'Sign out ends the session',
    steps: ['Sign in as grace.udoh.', 'Press Sign out in the header.', 'Press the browser Back button / open / again.'],
    expected: 'User is returned to the sign-in page and cannot reach the dashboard without signing in again.',
    async run(t) {
      const ctx = await login(t.browser, 'grace.udoh', { password: 'Uat@12345' });
      const page = await ctx.newPage();
      await page.goto(BASE + '/');
      await page.waitForTimeout(800);
      await page.getByRole('button', { name: 'Sign out' }).click();
      await page.waitForURL(u => u.pathname.startsWith('/Account/Login'));
      await page.goto(BASE + '/');
      const url = page.url();
      await ctx.close();
      check(url.includes('/Account/Login'), `After sign-out "/" opened ${url}`);
      return 'Signed out; revisiting / goes back to the sign-in page.';
    },
  },
  {
    id: 'AUTH-06', module: M, sprint: 'Sprint 1', feature: 'Dashboard', user: 'superadmin',
    title: 'SuperAdmin dashboard shows user statistics and shortcuts',
    steps: ['Sign in as superadmin.', 'Open the Dashboard.'],
    expected: 'Welcome banner, "Manage users" and "Add user" shortcuts, and counts of users, roles, active and deactivated accounts, each with its icon; header shows the user chip and Sign out icons.',
    async run(t) {
      await t.go('/');
      await t.see('Manage users');
      await t.see('Add user');
      const stats = await t.page.locator('.nps-stat-card').allInnerTexts();
      check(stats.length === 4, `Expected 4 stat cards, found ${stats.length}`);
      const total = Number((stats[0].match(/\d+/) ?? [0])[0]);
      check(total >= 58, `Staff & students count looks wrong: ${stats[0]}`);
      const blankIcons = await t.page.locator('.nps-stat-icon, .nps-user-chip .rzi, .nps-logout-button .rzi').evaluateAll(els => els.filter(e => !e.textContent.trim()).length);
      check(!blankIcons, `${blankIcons} dashboard/header icons render as empty boxes (icon name missing)`);
      return `Stat cards: ${stats.map(s => s.replace(/\s+/g, ' ')).join(' | ')}`;
    },
  },
  {
    id: 'AUTH-07', module: M, sprint: 'Sprint 1', feature: 'Dashboard', user: 'ngozi.okafor',
    title: 'Parent dashboard greets the parent and shows nothing about other users',
    steps: ['Sign in as parent ngozi.okafor.', 'Open the Dashboard.'],
    expected: 'A personal welcome (by name) and portal-relevant content. School-wide account statistics (total users, deactivated accounts, roles) are not shown to families.',
    async run(t) {
      await t.go('/');
      const h = await t.text('.nps-hero h2');
      const stats = await t.page.locator('.nps-stat-card').allInnerTexts();
      const leaks = stats.filter(s => /Staff|Roles configured|Deactivated|Active users/i.test(s));
      check(!leaks.length, `Parent sees school-wide account statistics: ${leaks.map(s => s.replace(/\s+/g, ' ')).join(' | ')}`);
      check(/Ngozi/.test(h), `Greeting is "${h}" (shows the username, not the parent's name)`);
      return `Greeting "${h}"; no school-wide statistics shown.`;
    },
  },
  ...Object.keys(MATRIX).map((user, i) => ({
    id: `AUTH-${String(8 + i).padStart(2, '0')}`, module: M, sprint: 'Sprints 1–16', feature: 'Role-based access', user,
    title: `Menu and page access for the ${{ superadmin: 'SuperAdmin', 'folake.adeyemi': 'HeadTeacher', 'chinedu.okeke': 'Teacher', 'tunde.bakare': 'SchoolBursar', 'musa.ibrahim': 'SchoolStoreKeeper', 'ngozi.okafor': 'Parent', 'chiamaka.okafor': 'Student' }[user]} role`,
    steps: [`Sign in as ${user}.`, 'Note the sidebar sections.', 'Open every link in the sidebar.', `Type these addresses directly: ${MATRIX[user].allowed.join(', ')}.`, `Type these restricted addresses directly: ${MATRIX[user].denied.join(', ')}.`],
    expected: `Sidebar shows exactly: ${MATRIX[user].menu.join(', ')}. Every sidebar link opens (none ends in "Access denied"). Permitted pages open; restricted pages show "Access denied".`,
    run: t => checkRole(t, user),
  })),
];
