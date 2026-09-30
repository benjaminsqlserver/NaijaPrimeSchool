// Module: User management (Sprint 1).
import { check, login, BASE, stamp } from '../lib.mjs';

const M = 'User management';
const S = 'Sprint 1';
const NEW_USER = `kemi.adewale${stamp.toLowerCase()}`;

async function tryLogin(t, user, password) {
  const ctx = await t.browser.newContext();
  const p = await ctx.newPage();
  await p.goto(BASE + '/Account/Login');
  await p.fill('#Input\\.Email', user);
  await p.fill('#Input\\.Password', password);
  await p.click('button[type=submit]');
  await p.waitForTimeout(2000);
  const ok = !p.url().includes('/Account/Login');
  const msg = ok ? '' : (await p.locator('body').innerText()).match(/(Invalid[^.\n]*|deactivated[^.\n]*|not active[^.\n]*|locked[^.\n]*)/i)?.[0] ?? '';
  await ctx.close();
  return { ok, msg };
}

export default [
  {
    id: 'USR-01', module: M, sprint: S, feature: 'User list', user: 'superadmin',
    title: 'Users list shows every account with roles and status',
    steps: ['Sign in as superadmin.', 'Open User Management → All Users.'],
    expected: 'A paged grid lists users with name, username, role badges and Active/Inactive status, plus Edit / Assign roles / Deactivate actions.',
    async run(t) {
      await t.go('/users');
      const n = await t.rowCount();
      check(n > 0, 'No users listed');
      const head = await t.row('folake.adeyemi').innerText();
      check(/HeadTeacher/i.test(head) && /Active/i.test(head), `Head teacher row: ${head}`);
      const pager = await t.text('.rz-pager, .rz-paginator').catch(() => '');
      return `${n} rows on the first page; head teacher row reads "${head.replace(/\s+/g, ' ')}". ${pager ? 'Pager: ' + pager : ''}`;
    },
  },
  {
    id: 'USR-02', module: M, sprint: S, feature: 'User list', user: 'superadmin',
    title: 'Search and filter users by role and status',
    steps: ['Open All Users.', 'Type "bakare" in Search and press Tab.', 'Clear search; choose Role = Teacher.', 'Choose Status = Inactive.'],
    expected: 'Search finds only Tunde Bakare; Role = Teacher lists the 4 teachers only; Inactive shows the empty message (nobody is deactivated yet).',
    async run(t) {
      await t.go('/users');
      await t.fill('Search', 'bakare');
      await t.settle(800);
      const s = await t.rows().allInnerTexts();
      check(s.length === 1 && /tunde\.bakare/.test(s[0]), `Search "bakare" returned ${s.length} rows`);
      await t.fill('Search', '');
      await t.pick('Role', 'Teacher', { exact: true });
      await t.settle(800);
      const teachers = await t.rows().allInnerTexts();
      check(teachers.length >= 4 && teachers.every(r => /Teacher/i.test(r)) && ['chinedu.okeke', 'aisha.bello', 'emeka.nwosu', 'grace.udoh'].every(u => teachers.some(r => r.includes(u))), `Role filter returned ${teachers.length} rows`);
      await t.pick('Status', 'Inactive');
      await t.settle(800);
      const inactive = await t.rowCount();
      check(inactive === 0, `Expected no inactive teachers, got ${inactive}`);
      return `Search → 1 row (Tunde Bakare); Role=Teacher → ${teachers.length} teachers, all with the Teacher badge; + Status=Inactive → "No users match your filters."`;
    },
  },
  {
    id: 'USR-03', module: M, sprint: S, feature: 'Create user', user: 'superadmin',
    title: 'Create user requires at least one role',
    steps: ['Open Add New User.', 'Fill first name, last name, username, email and matching passwords.', 'Leave all roles unticked and press Create user.'],
    expected: 'The Roles box is highlighted with "Please assign at least one role" and no account is created.',
    async run(t) {
      await t.go('/users/new');
      await t.fill('First name', 'Kemi');
      await t.fill('Last name', 'Adewale');
      await t.fill('Username', NEW_USER);
      await t.fill('Email', `${NEW_USER}@naijaprimeschool.ng`);
      await t.fill('Password', 'Teach@2026');
      await t.fill('Confirm password', 'Teach@2026');
      await t.click('Create user');
      const msg = await t.expectMessage(/assign at least one role/i);
      check(t.path() === '/users/new', 'Left the form');
      return `Warning shown: "${msg}"`;
    },
  },
  {
    id: 'USR-04', module: M, sprint: S, feature: 'Create user', user: 'superadmin',
    title: 'Create a new teacher account',
    steps: ['On Add New User, enter Title Mrs., Kemi Adewale, gender Female, phone, username, email, password "Teach@2026" twice.', 'Tick the Teacher role.', 'Press Create user.', 'Sign in as the new user in another browser.'],
    expected: '"User created" confirmation; the user appears in All Users with the Teacher badge and can sign in with the new password.',
    async run(t) {
      await t.go('/users/new');
      await t.pick('Title', 'Mrs.');
      await t.fill('First name', 'Kemi');
      await t.fill('Last name', 'Adewale');
      await t.pick('Gender', 'Female');
      await t.fill('Phone', '08039998877');
      await t.fill('Username', NEW_USER);
      await t.fill('Email', `${NEW_USER}@naijaprimeschool.ng`);
      await t.fill('Password', 'Teach@2026');
      await t.fill('Confirm password', 'Teach@2026');
      await t.tick('Teacher');
      await t.click('Create user');
      const msg = await t.expectToast(/created/i);
      await t.go('/users');
      await t.fill('Search', NEW_USER);
      await t.settle(800);
      const row = await t.row(NEW_USER).innerText();
      check(/Teacher/i.test(row), `New user row: ${row}`);
      const r = await tryLogin(t, NEW_USER, 'Teach@2026');
      check(r.ok, `New user could not sign in: ${r.msg}`);
      return `"${msg}"; listed as "${row.replace(/\s+/g, ' ')}"; new user signed in successfully.`;
    },
  },
  {
    id: 'USR-05', module: M, sprint: S, feature: 'Create user', user: 'superadmin',
    title: 'Duplicate username is rejected',
    steps: ['Open Add New User.', 'Enter username "chinedu.okeke" (already taken), other valid details and a role.', 'Press Create user.'],
    expected: 'An error explains the username is already taken; no second account is created.',
    async run(t) {
      await t.go('/users/new');
      await t.fill('First name', 'Copy');
      await t.fill('Last name', 'Cat');
      await t.fill('Username', 'chinedu.okeke');
      await t.fill('Email', `copycat${stamp}@naijaprimeschool.ng`);
      await t.fill('Password', 'Teach@2026');
      await t.fill('Confirm password', 'Teach@2026');
      await t.tick('Teacher');
      await t.click('Create user');
      const msg = await t.expectToast(/taken|exists|already/i);
      return `Rejected: "${msg}"`;
    },
  },
  {
    id: 'USR-06', module: M, sprint: S, feature: 'Edit user', user: 'superadmin',
    title: 'Edit a user\'s profile',
    steps: ['In All Users, press Edit on Kemi Adewale.', 'Change Phone to 08030001122 and Middle name to "Oluwakemi".', 'Press Save changes.'],
    expected: '"User updated" confirmation; the changes are shown when the page is reopened.',
    async run(t) {
      await t.go('/users');
      await t.fill('Search', NEW_USER);
      await t.settle(800);
      await t.rowAction(NEW_USER, 'Edit');
      await t.settle();
      await t.fill('Middle name', 'Oluwakemi');
      await t.fill('Phone', '08030001122');
      await t.click('Save changes');
      const msg = await t.expectToast(/updated/i);
      await t.page.reload();
      await t.settle();
      const phone = await t.field('Phone').locator('input').inputValue();
      check(phone === '08030001122', `Phone after reload is "${phone}"`);
      return `"${msg}"; phone persisted as ${phone}.`;
    },
  },
  {
    id: 'USR-07', module: M, sprint: S, feature: 'Reset password', user: 'superadmin',
    title: 'Reset a user\'s password',
    steps: ['On Kemi Adewale\'s edit page press Reset password.', 'Enter "short" and press Update password.', 'Enter "NewPass@2026" in both boxes and press Update password.', 'Sign in as the user with the old and then the new password.'],
    expected: 'A too-short password is refused in the dialog; the valid one shows "Password updated."; the old password no longer works and the new one does.',
    async run(t) {
      await t.go('/users');
      await t.fill('Search', NEW_USER);
      await t.settle(800);
      await t.rowAction(NEW_USER, 'Edit');
      await t.settle();
      await t.click('Reset password');
      const dlg = t.page.locator('.rz-dialog:visible');
      const inputs = dlg.locator('input[type=password], input');
      await inputs.nth(0).fill('short');
      await inputs.nth(1).fill('short');
      await dlg.getByRole('button', { name: 'Update password' }).click();
      await t.page.waitForTimeout(400);
      const dlgText = (await dlg.innerText()).replace(/\s+/g, ' ');
      check(/at least 8/i.test(dlgText), `No length warning in dialog: ${dlgText}`);
      await inputs.nth(0).fill('NewPass@2026');
      await inputs.nth(1).fill('NewPass@2026');
      await dlg.getByRole('button', { name: 'Update password' }).click();
      const msg = await t.expectToast(/Password updated/i);
      const oldPw = await tryLogin(t, NEW_USER, 'Teach@2026');
      const newPw = await tryLogin(t, NEW_USER, 'NewPass@2026');
      check(!oldPw.ok, 'Old password still works');
      check(newPw.ok, `New password refused: ${newPw.msg}`);
      return `Short password refused ("Password must be at least 8 characters."); "${msg}"; old password refused, new password accepted.`;
    },
  },
  {
    id: 'USR-08', module: M, sprint: S, feature: 'Assign roles', user: 'superadmin',
    title: 'Assign an additional role to a user',
    steps: ['In All Users press Assign roles on Kemi Adewale.', 'Tick SchoolBursar as well as Teacher.', 'Press Save roles.'],
    expected: '"Roles updated" confirmation; All Users shows both Teacher and SchoolBursar badges; the user now sees the Finance menu.',
    async run(t) {
      await t.go('/users');
      await t.fill('Search', NEW_USER);
      await t.settle(800);
      await t.rowAction(NEW_USER, 'Assign roles');
      await t.settle();
      await t.tick('SchoolBursar');
      await t.click('Save roles');
      const msg = await t.expectToast(/Role/i);
      await t.go('/users');
      await t.fill('Search', NEW_USER);
      await t.settle(800);
      const row = await t.row(NEW_USER).innerText();
      check(/Teacher/i.test(row) && /SchoolBursar/i.test(row), `Row after save: ${row}`);
      const ctx = await login(t.browser, NEW_USER, { password: 'NewPass@2026' });
      const p = await ctx.newPage();
      await p.goto(BASE + '/');
      await p.waitForTimeout(1200);
      const menu = await p.locator('.nps-panel-menu .rz-navigation-item-text').allInnerTexts();
      await ctx.close();
      check(menu.includes('Finance'), `Finance menu not shown: ${menu.join(', ')}`);
      return `"${msg}"; badges: ${row.replace(/\s+/g, ' ')}; user now sees the Finance menu.`;
    },
  },
  {
    id: 'USR-09', module: M, sprint: S, feature: 'Deactivate / activate', user: 'superadmin',
    title: 'Deactivated user cannot sign in until reactivated',
    steps: ['In All Users press Deactivate on Kemi Adewale and confirm.', 'Try to sign in as her.', 'Press Activate on her row.', 'Try to sign in again.'],
    expected: 'After deactivation the row shows Inactive and sign-in is refused; after activation she can sign in again.',
    async run(t) {
      await t.go('/users');
      await t.fill('Search', NEW_USER);
      await t.settle(800);
      await t.rowAction(NEW_USER, 'Deactivate');
      await t.confirm('Deactivate');
      await t.toast();
      await t.settle();
      const row = await t.row(NEW_USER).innerText();
      check(/Inactive/i.test(row), `Row after deactivate: ${row}`);
      const blocked = await tryLogin(t, NEW_USER, 'NewPass@2026');
      check(!blocked.ok, 'Deactivated user could still sign in');
      await t.rowAction(NEW_USER, 'Activate');
      await t.toast();
      const back = await tryLogin(t, NEW_USER, 'NewPass@2026');
      check(back.ok, `Reactivated user could not sign in: ${back.msg}`);
      return `Deactivated → sign-in refused${blocked.msg ? ` ("${blocked.msg}")` : ''}; activated → sign-in works.`;
    },
  },
  {
    id: 'USR-10', module: M, sprint: S, feature: 'Roles', user: 'superadmin',
    title: 'Roles page lists the seven system roles',
    steps: ['Open User Management → Roles.'],
    expected: 'SuperAdmin, HeadTeacher, Teacher, SchoolBursar, SchoolStoreKeeper, Parent and Student are listed with descriptions, each marked System.',
    async run(t) {
      await t.go('/roles');
      const rows = await t.rows().allInnerTexts();
      const names = ['SuperAdmin', 'HeadTeacher', 'Teacher', 'SchoolBursar', 'SchoolStoreKeeper', 'Parent', 'Student'];
      const missing = names.filter(n => !rows.some(r => r.startsWith(n) || r.includes(n + '\t') || r.split(/\s/)[0] === n));
      check(rows.length === 7 && !missing.length, `${rows.length} roles; missing ${missing.join(', ')}`);
      return `${rows.length} roles listed: ${names.join(', ')}.`;
    },
  },
];
