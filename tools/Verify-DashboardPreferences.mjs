import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const app = fs.readFileSync(path.join(root, 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');
const failures = [];

function requireSource(condition, message) {
  if (!condition) failures.push(message);
}

requireSource(app.includes('let dashboardWidgetOrderDraft = [...dashboardWidgetIds];'), 'Dashboard order needs a separate draft from the applied order.');
requireSource(/function arrangeDashboardWidgets\(order = dashboardWidgetOrder\)\s*\{\s*dashboardWidgetOrder = \[\.\.\.new Set\(Array\.isArray\(order\) \? order\.filter\(id => dashboardWidgetIds\.includes\(id\)\) : \[\]\)\];/.test(app), 'Applying server preferences must preserve the requested widget order while discarding unknown identifiers.');
requireSource(app.includes('dashboardWidgetOrderDraft = [...dashboardWidgetOrder];'), 'Opening dashboard preferences must start from the currently applied order.');
requireSource(app.includes('dashboardWidgetOrderDraft = [...dashboardWidgetIds]') && app.includes("$('#resetDashboardCustomize').addEventListener('click'"), 'Reset must update only the dialog draft.');
requireSource(/function renderDashboardWidgetOrder\(\)\s*\{[\s\S]*?for \(const id of dashboardWidgetOrderDraft\)/.test(app), 'The order editor must render the draft order.');
requireSource(/\.dashboard-order-up,\.dashboard-order-down[\s\S]*?dashboardWidgetOrderDraft\.indexOf/.test(app), 'Arrow controls must edit only the order draft.');
requireSource(/addEventListener\('drop'[\s\S]*?dashboardWidgetOrderDraft\.indexOf[\s\S]*?dashboardWidgetOrderDraft\.splice/.test(app), 'Drag and drop must edit only the order draft.');
requireSource(app.includes('widgetOrder: dashboardWidgetOrderDraft'), 'Saving must submit the edited draft order.');
requireSource(app.includes('applyDashboardPreferences(result.visibleWidgets, result.widgetOrder, result.customCharts)'), 'The saved server response must become the applied dashboard state.');
requireSource(app.includes('let dashboardChartDraft = [];') && app.includes('dashboardChartDraft=dashboardCustomCharts.map(chart=>({...chart}))'), 'Opening dashboard preferences must copy saved charts into a separate editable draft.');
requireSource(/dashboardChartDraft\.splice\(index,1\);renderDashboardChartDrafts\(\)/.test(app), 'Removing a custom chart must edit only the chart draft.');
requireSource(app.includes("dashboardChartDraft.length >= 6") && app.includes('dashboardChartDraft.some(item=>item.dimension===chart.dimension&&item.type===chart.type)'), 'The chart editor must enforce its six-chart cap and reject duplicate dimension/view pairs.');
requireSource(app.includes('customCharts: dashboardChartDraft'), 'Saving must submit chart drafts with dashboard preferences.');
requireSource(app.includes("$('#cancelDashboardCustomize').addEventListener('click', () => $('#dashboardCustomizeDialog').close())"), 'Cancel must close the editor without applying or submitting the drafts.');

if (failures.length) {
  console.error(failures.map(message => `FAIL ${message}`).join('\n'));
  process.exitCode = 1;
} else {
  console.log('Verified dashboard order edits, reset, and drag-and-drop stay in a dialog draft until the server confirms save.');
}
