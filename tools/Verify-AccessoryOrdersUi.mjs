import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import assert from 'node:assert/strict';

const html = readFileSync(resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');
for (const id of [
  'accessoryOrdersPage', 'accessoryOrderSearch', 'accessoryOrderStatus', 'accessoryOrderDelivery',
  'accessoryOrderFrom', 'accessoryOrderTo', 'accessoryOrderPageSize', 'clearAccessoryOrderFilters',
  'accessoryOrderRows', 'accessoryOrderPrev', 'accessoryOrderNext', 'accessoryOrderExport',
  'accessoryOrdersRefresh', 'newAccessoryOrderButton', 'accessoryOrderCreateDialog', 'accessoryOrderDetailDialog',
  'accessoryOrderReceiveDialog', 'accessoryOrderAttachmentForm', 'receiveAccessoryOrderFromDetail'
]) assert.match(html, new RegExp(`id="${id}"`), `accessory orders must expose ${id}`);

const listStart = html.indexOf('async function loadAccessoryOrders(');
const listEnd = html.indexOf('async function loadStockOrders(', listStart);
assert.ok(listStart >= 0 && listEnd > listStart, 'accessory order list loader must be present');
const list = html.slice(listStart, listEnd);
for (const token of [
  'const requestId = ++accessoryOrderRequestId', "params.set('q', q)", "params.set('status', status)",
  "params.set('from', from)", "params.set('to', to)", "params.set('overdueOnly', 'true')", "params.set('asOf', today)",
  '/api/accessory-orders?${params}', 'requestId !== accessoryOrderRequestId', 'escapeHtml(order.orderNumber)',
  'accessory-order-open', 'accessory-order-receive', 'accessory-order-cancel', "$('#accessoryOrderPrev').disabled",
  "$('#accessoryOrderNext').disabled"
]) assert.ok(list.includes(token), `accessory order list must implement ${token}`);

const detailStart = html.indexOf('async function openAccessoryOrder(');
const detailEnd = html.indexOf('async function loadAccessoryOrderAttachments(', detailStart);
const detail = html.slice(detailStart, detailEnd);
assert.ok(detail.includes('accessoryOrderDetailRequestId') && detail.includes('escapeHtml('), 'order details must reject stale responses and escape rendered values');
assert.ok(detail.includes("if (!$('#accessoryOrderDetailDialog').open) $('#accessoryOrderDetailDialog').showModal()"), 'refreshing an open order detail must not reopen the native dialog');
assert.ok(detail.includes("$('#receiveAccessoryOrderFromDetail').hidden = !canWriteAssets()"), 'receipt shortcut must honor write permissions and order state');

const receiveStart = html.indexOf("$('#accessoryOrderReceiveForm').addEventListener('submit'");
const receiveEnd = html.indexOf("$('#accessoryOrdersFromDetail').addEventListener", receiveStart);
const receive = html.slice(receiveStart, receiveEnd);
assert.ok(receive.includes('/api/accessory-orders/${orderId}/receive') && receive.includes('Promise.all([loadAccessoryOrders(), openAccessoryOrder(orderId)])') && receive.includes('En az bir kalem için teslim alınan miktarı girin.'), 'receipt must validate quantities and refresh both the list and still-open details');

const exportStart = html.indexOf("$('#accessoryOrderExport').addEventListener('click'");
const exportEnd = html.indexOf("$('#accessoryOrderStatus').addEventListener('change'", exportStart);
const exportHandler = html.slice(exportStart, exportEnd);
for (const token of [
  '/api/accessory-orders/export.csv?', "params.set('q', q)", "params.set('status', status)",
  "params.set('from', from)", "params.set('to', to)", "params.set('overdueOnly', 'true')", 'window.location.assign'
]) assert.ok(exportHandler.includes(token), `CSV export must keep ${token}`);
assert.match(exportHandler, /if \(from && to && from > to\)/, 'invalid date range must stop export');

const writeHandlersStart = html.indexOf("$('#newAccessoryOrderButton').addEventListener('click'");
const writeHandlersEnd = html.indexOf("$('#receiveAccessoryOrderFromDetail').addEventListener", writeHandlersStart);
const writeHandlers = html.slice(writeHandlersStart, writeHandlersEnd);
for (const token of [
  'if (!canWriteAssets()) return', '/api/accessory-orders/suppliers', "api('/api/accessory-orders'",
  '/api/accessory-orders/${button.dataset.id}/cancel', 'Her aksesuarı siparişte bir kez seçin'
]) assert.ok(writeHandlers.includes(token), `purchase order action must remain wired: ${token}`);
assert.ok(html.includes('/api/accessory-orders/${orderId}/attachments') && html.includes('/api/accessory-orders/${currentAccessoryOrder.order.id}/attachments/${button.dataset.id}'), 'order attachment upload, delete, and refresh paths must remain connected');

console.log('Verified purchase order filters, overdue date handling, paging race guard, safe detail rendering, create/receive/cancel actions, attachment workflow, and CSV filter parity.');
