// Module: Announcements (Sprint 8), email/SMS reminders (Sprint 11) and reminder preferences (Sprint 12).
// The UAT app runs with a 1-minute unread grace period and a 10-second dispatcher (see reset-and-start.sh).
import { check, stamp } from '../lib.mjs';

const M = 'Announcements & reminders';
const SPORTS = `Primary 1A sports day (${stamp})`;
const EMERG = `School closes at 12 noon today (${stamp})`;

async function logFor(t, search) {
  await t.go('/announcements/notifications');
  await t.fill('Search', search);
  await t.settle(900);
  return (await t.rows().allInnerTexts()).map(r => r.replace(/\s+/g, ' '));
}

async function waitForDispatch(t, search, done, seconds = 150) {
  const end = Date.now() + seconds * 1000;
  let rows = [];
  while (Date.now() < end) {
    rows = await logFor(t, search);
    if (rows.length && done(rows)) return rows;
    await t.page.waitForTimeout(8000);
  }
  return rows;
}

export default [
  {
    id: 'COM-01', module: M, sprint: 'Sprint 8', feature: 'Announcements', user: 'folake.adeyemi',
    title: 'Announcement list with status, reads and reminder counters',
    steps: ['Sign in as the head teacher.', 'Open Communications → Announcements.'],
    expected: 'The four seeded notices: the pinned welcome, PTA meeting (expires in 14 days), the Primary 5A excursion, and the "Mid-term break dates" draft; each published one shows reads and email/SMS counters.',
    async run(t) {
      await t.go('/announcements');
      const rows = await t.rows().allInnerTexts();
      check(rows.length === 4, `Expected 4 announcements, got ${rows.length}`);
      const draft = rows.find(r => /Mid-term/.test(r)) ?? '';
      check(/Draft/i.test(draft), `Mid-term row: ${draft}`);
      const welcome = rows.find(r => /Welcome back/.test(r)) ?? '';
      check(/pending|sent/i.test(welcome), `Welcome row has no reminder counters: ${welcome}`);
      return rows.map(r => r.replace(/\s+/g, ' ').slice(0, 110)).join(' || ');
    },
  },
  {
    id: 'COM-02', module: M, sprint: 'Sprints 8 & 11', feature: 'Announcements', user: 'folake.adeyemi',
    title: 'Publish a class announcement',
    steps: ['Press New announcement.', `Title "${SPORTS}", body, category Events, audience Specific Class, target class Primary 1A.`, 'Press Publish now.'],
    expected: '"Published"; the notice is listed as Published and email/SMS reminders are queued (pending) for Primary 1A families.',
    async run(t) {
      await t.go('/announcements/new');
      await t.fill('Title', SPORTS);
      await t.fill('Body', 'Primary 1A sports day holds on Friday at 10am on the school field. Pupils should come in their house colours. Parents are welcome!');
      await t.pick('Category', 'Events', { exact: true });
      await t.pick('Audience', 'Specific Class', { exact: true });
      await t.pick('Target class', 'Primary 1A', { exact: true });
      await t.click('Publish now');
      await t.expectToast(/Published/i);
      await t.go('/announcements');
      const row = (await t.row(SPORTS).innerText()).replace(/\s+/g, ' ');
      check(/Published/i.test(row) && /pending/i.test(row), `Row: ${row}`);
      return `Listed: ${row}`;
    },
  },
  {
    id: 'COM-03', module: M, sprint: 'Sprint 8', feature: 'Portal announcements', user: 'yetunde.balogun',
    title: 'Class-targeted notices only reach that class\'s families',
    pre: 'Mrs. Balogun\'s only child, Damilola, is in Primary 2A.',
    steps: ['Sign in as parent yetunde.balogun.', 'Open Announcements.'],
    expected: 'She sees the welcome notice (pinned first) and the PTA meeting, but not the Primary 5A excursion or the Primary 1A sports day, and not the draft.',
    async run(t) {
      await t.go('/portal/announcements');
      const titles = (await t.page.locator('.nps-announcement h3').allInnerTexts()).map(s => s.trim());
      check(titles[0]?.startsWith('Welcome back'), `First notice is "${titles[0]}"`);
      check(titles.some(s => /PTA meeting/.test(s)), 'PTA meeting missing');
      check(!titles.some(s => /Primary 5A|Primary 1A|Mid-term/.test(s)), `Sees: ${titles.join(' | ')}`);
      return `Sees: ${titles.join(' | ')}`;
    },
  },
  {
    id: 'COM-04', module: M, sprint: 'Sprint 8', feature: 'Portal announcements', user: 'ngozi.okafor',
    title: 'Parent reads a notice; it stops being "New"',
    pre: 'Mrs. Okafor has children in Primary 5A and Primary 1A.',
    steps: ['Sign in as parent ngozi.okafor.', 'Open Announcements.', `Press Mark as read on "${SPORTS}".`],
    expected: 'Both class notices (Primary 5A and Primary 1A) are shown; after Mark as read the sports-day notice loses its "New" badge.',
    async run(t) {
      await t.go('/portal/announcements');
      const card = t.page.locator('.nps-announcement').filter({ hasText: SPORTS });
      check(await card.count(), 'Sports day notice not shown');
      await t.see('Primary 5A excursion');
      check(/New/i.test(await card.innerText()), 'Notice was not marked New');
      await card.getByRole('button', { name: /Mark as read/ }).click();
      await t.page.waitForTimeout(1200);
      const after = await card.innerText();
      check(!/\bNEW\b|\bNew\b/.test(after.split('\n').slice(0, 3).join(' ')), `Still marked New: ${after.slice(0, 100)}`);
      return 'Both class notices visible; sports day marked as read.';
    },
  },
  {
    id: 'COM-05', module: M, sprint: 'Sprint 12', feature: 'Reminder settings', user: 'ngozi.okafor',
    title: 'Parent switches SMS reminders off',
    steps: ['Open Reminder settings.', 'Turn SMS reminders off (email stays on); press Save preferences.', 'Reload the page.'],
    expected: '"Preferences saved"; after reload SMS is still off and email on; the page shows where reminders go (her email and phone).',
    async run(t) {
      await t.go('/portal/notifications');
      const sms = t.field('SMS reminders').locator('.rz-switch');
      if ((await sms.getAttribute('class')).includes('rz-switch-checked')) await sms.click();
      await t.click('Save preferences');
      const msg = await t.expectToast(/Preferences saved/i);
      await t.page.reload();
      await t.settle();
      const smsOn = (await t.field('SMS reminders').locator('.rz-switch').getAttribute('class')).includes('rz-switch-checked');
      const emailOn = (await t.field('Email reminders').locator('.rz-switch').getAttribute('class')).includes('rz-switch-checked');
      check(!smsOn && emailOn, `After reload: email ${emailOn}, SMS ${smsOn}`);
      return `"${msg}"; persisted (email on, SMS off).`;
    },
  },
  {
    id: 'COM-06', module: M, sprint: 'Sprint 11', feature: 'Email / SMS reminders', user: 'folake.adeyemi',
    title: 'Reminders go only to families who have not read the notice',
    pre: 'Runs at least a minute after COM-02 (grace period = 1 minute in UAT).',
    steps: ['Open Communications → Notification log.', `Search for "${SPORTS.slice(0, 20)}" and wait for the dispatcher (Refresh).`],
    expected: 'Mrs. Okafor\'s reminders are Skipped ("read in the portal"); every other Primary 1A family\'s email and SMS are Sent.',
    async run(t) {
      const rows = await waitForDispatch(t, 'sports day', rs => !rs.some(r => /Pending/i.test(r)));
      const ngozi = rows.filter(r => /Ngozi/.test(r));
      const others = rows.filter(r => !/Ngozi|Tobenna/.test(r));
      check(rows.length > 0, 'No reminders were queued');
      check(ngozi.length && ngozi.every(r => /Skipped/i.test(r)), `Mrs. Okafor's rows: ${ngozi.join(' / ')}`);
      check(others.length && others.every(r => /Sent/i.test(r)), `Others: ${others.filter(r => !/Sent/i.test(r)).join(' / ')}`);
      return `${rows.length} reminders: ${others.length} sent to other families; Mrs. Okafor's ${ngozi.length} skipped (${ngozi[0]?.match(/Read[^|]*/i)?.[0] ?? ''}).`;
    },
  },
  {
    id: 'COM-07', module: M, sprint: 'Sprint 11', feature: 'Notification log', user: 'folake.adeyemi',
    title: 'Notification log totals and filters',
    steps: ['Open Notification log.', 'Filter Channel = SMS, Status = Sent.'],
    expected: 'Totals for Sent / Pending / Failed / Skipped; the filters narrow the list to sent SMS only.',
    async run(t) {
      await t.go('/announcements/notifications');
      const stats = (await t.page.locator('.nps-stat-card').allInnerTexts()).map(s => s.replace(/\s+/g, ' ')).join(' | ');
      await t.pick('Channel', 'SMS');
      await t.pick('Status', 'Sent');
      await t.settle(900);
      const rows = await t.rows().allInnerTexts();
      check(rows.length > 0 && rows.every(r => /SMS/i.test(r) && /Sent/i.test(r)), `${rows.length} rows after filtering`);
      return `${stats}; SMS + Sent → ${rows.length} rows.`;
    },
  },
  {
    id: 'COM-08', module: M, sprint: 'Sprint 12', feature: 'Reminder settings', user: 'folake.adeyemi',
    title: 'Office sets a parent\'s reminder preferences',
    steps: ['Open Family → Parents → Hauwa Bello → Reminders tab.', 'Turn Email reminders off; Save preferences.'],
    expected: '"Preferences saved"; the tab shows her quiet hours (21:00–07:00, set earlier) and email now off.',
    async run(t) {
      await t.go('/parents');
      await t.fill('Search', 'Hauwa');
      await t.settle(900);
      await t.rowAction('Hauwa Bello', 'Edit');
      await t.settle();
      await t.page.getByRole('tab', { name: 'Reminders' }).click();
      await t.page.waitForTimeout(600);
      const email = t.field('Email reminders').locator('.rz-switch');
      if ((await email.getAttribute('class')).includes('rz-switch-checked')) await email.click();
      await t.click('Save preferences');
      const msg = await t.expectToast(/Preferences saved/i);
      const quiet = (await t.field('Quiet hours').locator('.rz-switch').getAttribute('class')).includes('rz-switch-checked');
      check(quiet, 'Quiet hours not shown as on');
      return `"${msg}"; quiet hours still on.`;
    },
  },
  {
    id: 'COM-09', module: M, sprint: 'Sprints 11 & 12', feature: 'Email / SMS reminders', user: 'folake.adeyemi',
    title: 'Reminders respect each family\'s opted-out channels',
    steps: ['Publish an Emergency notice to Everyone: "' + EMERG + '".', 'Open the Notification log for it.'],
    expected: 'Mrs. Okafor (SMS off) gets an email only; Mrs. Bello (email off) gets SMS only; everyone else gets both.',
    async run(t) {
      await t.go('/announcements/new');
      await t.fill('Title', EMERG);
      await t.fill('Body', 'Due to a burst water main, the school will close at 12 noon today. Please arrange early pick-up.');
      await t.pick('Category', 'Emergency', { exact: true });
      await t.pick('Audience', 'Everyone', { exact: true });
      await t.click('Publish now');
      await t.expectToast(/Published/i);
      const ngozi = await logFor(t, 'Ngozi');
      const hauwa = await logFor(t, 'Hauwa');
      const nE = ngozi.filter(r => r.includes('noon'));
      const hE = hauwa.filter(r => r.includes('noon'));
      check(nE.length === 1 && /Email/i.test(nE[0]), `Mrs. Okafor's rows: ${nE.join(' / ')}`);
      check(hE.length === 1 && /SMS/i.test(hE[0]), `Mrs. Bello's rows: ${hE.join(' / ')}`);
      return `Mrs. Okafor: ${nE.length} row (email); Mrs. Bello: ${hE.length} row (SMS).`;
    },
  },
];
