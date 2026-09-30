// Module: Two-way messaging (Sprint 13) and email/SMS alerts for office replies (Sprint 14).
// UAT runs with a 1-minute alert grace period and a 10-second dispatcher (see reset-and-start.sh).
import { check, stamp, login, makeT } from '../lib.mjs';

const M = 'Messaging';
const SUBJ = `Kamsi's reading glasses (${stamp})`;

async function openPortalThread(t, subject) {
  await t.go('/portal/messages');
  await t.page.locator('.nps-thread-row').filter({ hasText: subject }).first().click();
  await t.settle();
}
async function openInboxThread(t, subject) {
  await t.go('/messages');
  await t.row(subject).click();
  await t.settle();
}
async function reply(t, text) {
  await t.page.getByPlaceholder('Write a reply...').fill(text);
  await t.click('Send reply');
  await t.page.waitForTimeout(800);
}
async function alertRows(t) {
  // Alert log for the conversation (opens the notification log filtered to it).
  await openInboxThread(t, SUBJ);
  await t.click('Alert log');
  await t.settle(900);
  return (await t.rows().allInnerTexts()).map(r => r.replace(/\s+/g, ' '));
}
async function waitAlerts(t, done, seconds = 150) {
  const end = Date.now() + seconds * 1000;
  let rows = [];
  while (Date.now() < end) {
    rows = await alertRows(t);
    if (done(rows)) return rows;
    await t.page.waitForTimeout(8000);
  }
  return rows;
}

export default [
  {
    id: 'MSG-01', module: M, sprint: 'Sprint 13', feature: 'Portal messages', user: 'chukwudi.eze',
    title: 'Parent writes to the school about a child',
    steps: ['Sign in as parent chukwudi.eze.', 'Open Messages → Message the school.', `Subject "${SUBJ}", about Kamsi Eze, message text.`, 'Press Send.'],
    expected: '"Message sent"; the conversation opens and is listed under Messages with "You: …" as the last message.',
    async run(t) {
      await t.go('/portal/messages/new');
      await t.fill('Subject', SUBJ);
      await t.pick('About (optional)', 'Kamsi');
      const body = t.field('Message').locator('textarea');
      await body.fill('Good morning. Kamsi left his reading glasses in class on Monday. Could someone check the lost property box? Thank you.');
      await body.press('Tab');
      await t.click('Send', { exact: true });
      await t.expectToast(/Message sent/i);
      await t.go('/portal/messages');
      const card = (await t.page.locator('.nps-thread-row').filter({ hasText: SUBJ }).innerText()).replace(/\s+/g, ' ');
      check(/You:/.test(card), `Card: ${card}`);
      return `Listed: ${card}`;
    },
  },
  {
    id: 'MSG-02', module: M, sprint: 'Sprint 13', feature: 'Office inbox', user: 'folake.adeyemi',
    title: 'Office inbox shows new and awaiting conversations',
    steps: ['Sign in as the head teacher; open Communications → Inbox.'],
    expected: 'Mr. Eze\'s new conversation is at the top, bold/unread, marked "Awaiting reply" with the pupil name; the earlier "School bus route" question is also awaiting reply; answered threads are not.',
    async run(t) {
      await t.go('/messages');
      const rows = (await t.rows().allInnerTexts()).map(r => r.replace(/\s+/g, ' '));
      check(rows[0]?.includes(SUBJ.slice(0, 20)), `Top row: ${rows[0]}`);
      check(/Awaiting reply/i.test(rows[0]), `Top row not awaiting reply: ${rows[0]}`);
      const bus = rows.find(r => /School bus route/.test(r)) ?? '';
      check(/Awaiting reply/i.test(bus), `Bus route row: ${bus}`);
      const answered = rows.find(r => /Replacement reading book/.test(r)) ?? '';
      check(!/Awaiting reply/i.test(answered), 'Answered thread is marked awaiting reply');
      return `${rows.length} conversations; top: ${rows[0]}`;
    },
  },
  {
    id: 'MSG-03', module: M, sprint: 'Sprints 13 & 14', feature: 'Office reply', user: 'folake.adeyemi',
    title: 'Office replies; an email and SMS alert is queued',
    steps: [`Open "${SUBJ}".`, 'Type a reply and press Send reply.', 'Press Alert log.'],
    expected: 'The reply appears in the conversation; the Alert log shows one Pending email and one Pending SMS to Mr. Eze (sent after the grace period unless he reads it first).',
    async run(t) {
      await openInboxThread(t, SUBJ);
      await reply(t, 'Good morning Mr. Eze. We found the glasses in the lost property box; Kamsi can collect them from the office at break.');
      await t.see('lost property box; Kamsi can collect');
      const rows = await alertRows(t);
      check(rows.length === 2 && rows.some(r => /Email/i.test(r)) && rows.some(r => /SMS/i.test(r)), `Alert rows: ${rows.join(' / ')}`);
      check(rows.every(r => /Pending|Sent/i.test(r)), `Statuses: ${rows.join(' / ')}`);
      return `Alerts: ${rows.map(r => r.match(/(Email|SMS).*?(Pending|Sent)/i)?.[0] ?? r).join('; ')}`;
    },
  },
  {
    id: 'MSG-04', module: M, sprint: 'Sprint 14', feature: 'Reply alerts', user: 'folake.adeyemi',
    title: 'Alerts are sent when the family has not read the reply',
    pre: 'Mr. Eze has not opened the conversation since the office replied.',
    steps: ['Wait for the alert grace period (1 minute in UAT) and the dispatcher.', 'Refresh the Alert log.'],
    expected: 'Both alerts become Sent; the application log shows the email quoting the reply and an SMS naming only the subject.',
    async run(t) {
      const rows = await waitAlerts(t, rs => rs.length >= 2 && rs.every(r => /Sent|Skipped|Failed/i.test(r)));
      check(rows.length >= 2 && rows.every(r => /Sent/i.test(r)), `Alert rows: ${rows.join(' / ')}`);
      return `Both alerts Sent: ${rows.join(' / ')}`;
    },
  },
  {
    id: 'MSG-05', module: M, sprint: 'Sprints 13 & 14', feature: 'Portal messages', user: 'chukwudi.eze',
    title: 'Parent sees the reply; a reply read in time skips the alert',
    steps: ['As Mr. Eze open Messages: the conversation shows "New reply".', '(Office sends a second reply.)', 'Mr. Eze opens the conversation straight away and replies "Thank you!".'],
    expected: 'The new reply is shown; the alerts for the second office reply become Skipped — "Read in the portal before the alert went out."',
    async run(t) {
      await t.go('/portal/messages');
      const card = await t.page.locator('.nps-thread-row').filter({ hasText: SUBJ }).innerText();
      check(/New reply/i.test(card), `Card: ${card.replace(/\s+/g, ' ')}`);
      // Office sends a second reply.
      const staff = await login(t.browser, 'folake.adeyemi');
      const sp = makeT(await staff.newPage(), staff);
      await openInboxThread(sp, SUBJ);
      await reply(sp, 'PS: please label the case with his name.');
      await openPortalThread(t, SUBJ);
      await t.see('please label the case');
      await reply(t, 'Thank you!');
      const rows = await waitAlerts(sp, rs => rs.length >= 4 && !rs.some(r => /Pending/i.test(r)));
      await staff.close();
      const latest = rows.filter(r => !/Sent/i.test(r));
      check(latest.length === 2 && latest.every(r => /Skipped/i.test(r)), `Alert rows: ${rows.join(' / ')}`);
      return `Second-round alerts skipped: ${latest[0]}`;
    },
  },
  {
    id: 'MSG-06', module: M, sprint: 'Sprint 13', feature: 'Office inbox', user: 'folake.adeyemi',
    title: 'Resolve and reopen a conversation',
    steps: [`Open "${SUBJ}"; press Mark resolved.`, 'Inbox → Status = Resolved: the conversation is listed as Resolved.', 'Open it and press Reopen.'],
    expected: '"Marked resolved"; the inbox row shows Resolved; after Reopen it is open again.',
    async run(t) {
      await openInboxThread(t, SUBJ);
      await t.click('Mark resolved');
      const msg = await t.expectToast(/resolved/i);
      await t.go('/messages');
      await t.pick('Status', 'Resolved').catch(() => {});
      await t.settle(900);
      const row = (await t.row(SUBJ).innerText()).replace(/\s+/g, ' ');
      check(/Resolved/i.test(row), `Row: ${row}`);
      await t.row(SUBJ).click();
      await t.settle();
      await t.click('Reopen');
      await t.expectToast(/Reopen/i);
      return `"${msg}"; row showed Resolved; reopened.`;
    },
  },
  {
    id: 'MSG-07', module: M, sprint: 'Sprint 13', feature: 'Office-started conversation', user: 'folake.adeyemi',
    title: 'Office starts a conversation with a family',
    steps: ['Press New message.', 'To: Ngozi Okafor; about Tobenna; subject "Tobenna\'s eye test"; message.', 'Press Send; then sign in as Mrs. Okafor.'],
    expected: '"Message sent"; Mrs. Okafor sees the conversation under Messages with "New reply" and "School office: …".',
    async run(t) {
      await t.go('/messages/new');
      await t.pick('To', 'Ngozi');
      await t.pick('About (optional)', 'Tobenna').catch(() => t.note('("About" list did not offer Tobenna.)'));
      await t.fill('Subject', `Tobenna's eye test (${stamp})`);
      const body = t.field('Message').locator('textarea');
      await body.fill('Dear Mrs. Okafor, the school nurse recommends an eye test for Tobenna. Please let us know if you need a referral.');
      await body.press('Tab');
      await t.click('Send', { exact: true });
      await t.expectToast(/Message sent/i);
      const parent = await login(t.browser, 'ngozi.okafor');
      const pp = makeT(await parent.newPage(), parent);
      await pp.go('/portal/messages');
      const card = (await pp.page.locator('.nps-thread-row').filter({ hasText: 'eye test' }).innerText()).replace(/\s+/g, ' ');
      await parent.close();
      check(/New reply/i.test(card) && /School office/.test(card), `Parent sees: ${card}`);
      return `Parent sees: ${card}`;
    },
  },
  {
    id: 'MSG-08', module: M, sprint: 'Sprint 13', feature: 'Portal messages', user: 'chiamaka.okafor',
    title: 'A student can message the school',
    steps: ['Sign in as pupil chiamaka.okafor; Messages → Message the school.', 'Subject "Library card", message; Send.'],
    expected: '"Message sent"; the office inbox shows it from Chiamaka Okafor.',
    async run(t) {
      await t.go('/portal/messages/new');
      await t.fill('Subject', `Library card (${stamp})`);
      const body = t.field('Message').locator('textarea');
      await body.fill('Please can I get a new library card? I lost mine.');
      await body.press('Tab');
      await t.click('Send', { exact: true });
      await t.expectToast(/Message sent/i);
      const staff = await login(t.browser, 'folake.adeyemi');
      const sp = makeT(await staff.newPage(), staff);
      await sp.go('/messages');
      const row = (await sp.row(`Library card (${stamp})`).innerText()).replace(/\s+/g, ' ');
      await staff.close();
      check(/Chiamaka/.test(row), `Inbox row: ${row}`);
      return `Inbox row: ${row}`;
    },
  },
  {
    id: 'MSG-09', module: M, sprint: 'Sprint 13', feature: 'Privacy', user: 'ngozi.okafor',
    title: 'A family cannot open another family\'s conversation',
    steps: ['Signed in as Mrs. Okafor, type the address of Mr. Eze\'s conversation (/portal/messages/<id>).'],
    expected: '"Conversation not found." — none of the messages are shown.',
    async run(t) {
      const staff = await login(t.browser, 'folake.adeyemi');
      const sp = makeT(await staff.newPage(), staff);
      await openInboxThread(sp, SUBJ);
      const id = sp.path().split('/')[2];
      await staff.close();
      await t.go(`/portal/messages/${id}`);
      const body = await t.text('.nps-page-container');
      check(!/reading glasses|lost property/i.test(body), 'Another family\'s messages are visible');
      return `Shown: "${body.slice(0, 80)}"`;
    },
  },
];
