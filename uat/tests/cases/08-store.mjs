// Module: Store & inventory (Sprint 7).
import { check, stamp } from '../lib.mjs';

const M = 'Store & inventory';
const S = 'Sprint 7';
const num = s => Number(String(s).replace(/[^\d.]/g, ''));
const stat = async (t, label) => (await t.page.locator('.nps-stat-card', { hasText: label }).first().innerText()).replace(/\s+/g, ' ');
const SKU = `ST-CRY-${stamp}`;

async function movement(t, { item, type, qty, cost, supplier, pupil, cls, ref }) {
  await t.go('/store/movements/new');
  await t.pick('Item', item);
  await t.pick('Movement type', type, { exact: true });
  await t.fill('Quantity', qty);
  if (cost !== undefined) await t.fill('Unit cost', cost);
  if (ref) await t.fill('Reference', ref);
  if (supplier) await t.pick('Supplier', supplier);
  if (pupil) await t.pick('Pupil', pupil);
  if (cls) await t.pick('Class', cls, { exact: true });
  await t.click('Save movement');
}

async function onHand(t, item) {
  await t.go('/store/items');
  await t.fill('Search', item);
  await t.settle(800);
  await t.rowAction(item, 'Open');
  await t.settle();
  return stat(t, 'On hand');
}

export default [
  {
    id: 'STO-01', module: M, sprint: S, feature: 'Store dashboard', user: 'musa.ibrahim',
    title: 'Store dashboard highlights low stock',
    steps: ['Sign in as storekeeper musa.ibrahim.', 'Open Store & Inventory → Store dashboard.'],
    expected: '10 items in the catalogue, 2 below reorder level (HB pencils: 3 on hand vs 5; Whiteboard markers: 4 vs 6) listed under "Low stock — needs replenishing", stock value by category and recent movements.',
    async run(t) {
      await t.go('/store');
      const items = await stat(t, 'Items in catalog'), low = await stat(t, 'Below reorder level'), value = await stat(t, 'Stock value');
      check(/\b10\b/.test(items) && /\b2\b/.test(low), `Cards: ${items} | ${low}`);
      const lowTable = await t.page.locator('.nps-card', { hasText: 'Low stock' }).innerText();
      check(/HB pencils/.test(lowTable) && /Whiteboard markers/.test(lowTable), 'Low-stock table is missing an item');
      return `${items} | ${low} | ${value}; low-stock list: HB pencils, Whiteboard markers.`;
    },
  },
  {
    id: 'STO-02', module: M, sprint: S, feature: 'Catalogue', user: 'musa.ibrahim',
    title: 'Catalogue search and low-stock filter',
    steps: ['Open Store & Inventory → Catalog.', 'Turn on "Low stock only".', 'Turn it off; search "textbook".'],
    expected: 'Low stock only shows the 2 low items with a "Low" badge; the search finds the two Primary 5 textbooks.',
    async run(t) {
      await t.go('/store/items');
      await t.field('Low stock only').locator('.rz-switch').click();
      await t.settle(800);
      const low = await t.rows().allInnerTexts();
      check(low.length === 2 && low.every(r => /Low/i.test(r)), `Low stock only: ${low.length}`);
      await t.field('Low stock only').locator('.rz-switch').click();
      await t.fill('Search', 'textbook');
      await t.settle(800);
      const found = await t.rowCount();
      check(found === 2, `Search found ${found}`);
      return `Low stock only → ${low.length}; "textbook" → ${found}.`;
    },
  },
  {
    id: 'STO-03', module: M, sprint: S, feature: 'Catalogue', user: 'musa.ibrahim',
    title: 'Add a new item with an opening balance',
    steps: ['Press New item.', `Name "Crayons (12 colours)", SKU ${SKU}, category Stationery, unit Box, reorder level 5.`, 'Opening quantity 20 at ₦1,500.', 'Press Save item.'],
    expected: '"\'Crayons (12 colours)\' added."; the item page shows On hand 20, last unit cost ₦1,500, stock value ₦30,000 and an Opening Balance movement.',
    async run(t) {
      await t.go('/store/items/new');
      await t.fill('Name', 'Crayons (12 colours)');
      await t.fill('SKU', SKU);
      await t.pick('Category', 'Stationery');
      await t.pick('Unit of measure', 'Box', { exact: true });
      await t.fill('Reorder level', '5');
      await t.fill('Opening quantity', '20');
      await t.fill('Opening unit cost', '1500');
      await t.click('Save item');
      const msg = await t.expectToast(/added/i);
      await t.page.waitForURL(u => /\/store\/items\/[0-9a-f-]{36}/.test(u.pathname), { timeout: 10000 });
      await t.settle();
      const oh = await stat(t, 'On hand'), val = await stat(t, 'Stock value');
      check(num(oh) === 20 && num(val) === 30000, `On hand ${oh}; value ${val}`);
      await t.see('Opening Balance');
      return `"${msg}"; ${oh}; ${val}.`;
    },
  },
  {
    id: 'STO-04', module: M, sprint: S, feature: 'Catalogue', user: 'musa.ibrahim',
    title: 'SKUs are unique',
    steps: ['Press New item; name "Pencils (duplicate)", SKU "ST-PEN-HB" (already used), category Stationery, unit Box.', 'Press Save item.'],
    expected: '"An item with SKU \'ST-PEN-HB\' already exists."',
    async run(t) {
      await t.go('/store/items/new');
      await t.fill('Name', 'Pencils (duplicate)');
      await t.fill('SKU', 'ST-PEN-HB');
      await t.pick('Category', 'Stationery');
      await t.pick('Unit of measure', 'Box', { exact: true });
      await t.click('Save item');
      return `Refused: "${await t.expectToast(/already exists/i)}"`;
    },
  },
  {
    id: 'STO-05', module: M, sprint: S, feature: 'Stock movements', user: 'musa.ibrahim',
    title: 'Receive a purchase from a supplier',
    steps: ['Open Record movement.', 'Item HB pencils, type Purchase, quantity 20, unit cost ₦950, reference "LPO-2701", supplier Lagos Book Hub Ltd.', 'Press Save movement.'],
    expected: 'Saved; HB pencils on hand rises from 3 to 23 and the item is no longer low; the dashboard shows 1 item below reorder level.',
    async run(t) {
      await movement(t, { item: 'HB pencils', type: 'Purchase', qty: 20, cost: 950, ref: 'LPO-2701', supplier: 'Lagos Book Hub' });
      const msg = await t.expectToast(/Saved/i);
      const oh = await onHand(t, 'HB pencils');
      check(num(oh) === 23, `On hand now ${oh}`);
      await t.go('/store');
      const low = await stat(t, 'Below reorder level');
      check(/\b1\b/.test(low), `Dashboard: ${low}`);
      return `"${msg}"; ${oh}; dashboard ${low}.`;
    },
  },
  {
    id: 'STO-06', module: M, sprint: S, feature: 'Stock movements', user: 'musa.ibrahim',
    title: 'Issue a textbook to a pupil',
    steps: ['Open Record movement.', 'Item Primary 5 English textbook, type Issue, quantity 1, pupil Kamsi Eze.', 'Press Save movement.'],
    expected: 'Saved; on hand falls from 59 to 58; the movement history shows the issue to Kamsi Eze.',
    async run(t) {
      await movement(t, { item: 'Primary 5 English textbook', type: 'Issue', qty: 1, pupil: 'Kamsi Eze' });
      await t.expectToast(/Saved/i);
      const oh = await onHand(t, 'Primary 5 English textbook');
      check(num(oh) === 58, `On hand now ${oh}`);
      const hist = await t.row('Kamsi').innerText();
      return `${oh}; history: ${hist.replace(/\s+/g, ' ')}`;
    },
  },
  {
    id: 'STO-07', module: M, sprint: S, feature: 'Stock movements', user: 'musa.ibrahim',
    title: 'Cannot issue more than is on hand',
    steps: ['Open Record movement.', 'Item Whiteboard markers (4 on hand), type Issue, quantity 50, class Primary 5A.', 'Press Save movement.'],
    expected: 'Refused with a clear message such as "Cannot remove 50 — only 4 are on hand."; stock is unchanged.',
    async run(t) {
      await movement(t, { item: 'Whiteboard markers', type: 'Issue', qty: 50, cls: 'Primary 5A' });
      const msg = await t.expectToast(/Cannot remove/i);
      check(/only|but/i.test(msg), `The message is garbled: "${msg}"`);
      return `Refused: "${msg}"`;
    },
  },
  {
    id: 'STO-08', module: M, sprint: S, feature: 'Stock movements', user: 'musa.ibrahim',
    title: 'Movement log with filters',
    steps: ['Open Store & Inventory → Movements.', 'Filter Type = Issue.'],
    expected: 'Every movement is listed with number, item, type, quantity, counter-party and cost; the filter shows only issues (to pupils and classes).',
    async run(t) {
      await t.go('/store/movements');
      const all = await t.rowCount();
      await t.pick('Type', 'Issue', { exact: true });
      await t.settle(800);
      const issues = await t.rows().allInnerTexts();
      check(issues.length >= 4 && issues.every(r => /Issue/i.test(r)), `Issue filter: ${issues.length}`);
      return `${all} movements; ${issues.length} issues.`;
    },
  },
  {
    id: 'STO-09', module: M, sprint: S, feature: 'Suppliers', user: 'musa.ibrahim',
    title: 'Manage suppliers',
    steps: ['Open Suppliers; press New supplier: "Ikeja Stationers", contact "Mrs Bola Ajayi", phone 08091112233; Save.', 'Deactivate Ikeja Stationers.', 'Try to delete Lagos Book Hub Ltd (has purchases).'],
    expected: 'The supplier is saved and listed Active, then Inactive after deactivation; deleting a supplier with purchase history is refused ("Deactivate instead").',
    async run(t) {
      await t.go('/store/suppliers');
      await t.click('New supplier');
      await t.fill('Name', 'Ikeja Stationers');
      await t.fill('Contact name', 'Mrs Bola Ajayi');
      await t.fill('Phone', '08091112233');
      await t.click('Save', { exact: true });
      await t.expectToast(/Saved/i);
      await t.settle();
      await t.rowAction('Ikeja Stationers', 'Deactivate');
      await t.maybeConfirm(/Deactivate|Yes|Ok/i);
      await t.toast().catch(() => {});
      await t.settle();
      const row = await t.row('Ikeja Stationers').innerText();
      check(/Inactive/i.test(row), `Row: ${row}`);
      await t.rowAction('Lagos Book Hub', 'Delete');
      await t.confirm('Delete');
      const refused = await t.expectToast(/Cannot delete/i);
      return `Created and deactivated Ikeja Stationers; delete refused: "${refused}"`;
    },
  },
  {
    id: 'STO-10', module: M, sprint: S, feature: 'Catalogue', user: 'musa.ibrahim',
    title: 'Items with history cannot be deleted',
    steps: ['In Catalog press Delete on "Football" and confirm.'],
    expected: '"Cannot delete an item with movement history. Deactivate instead."',
    async run(t) {
      await t.go('/store/items');
      await t.rowAction('Football', 'Delete');
      await t.confirm('Delete');
      return `Refused: "${await t.expectToast(/Cannot delete/i)}"`;
    },
  },
];
