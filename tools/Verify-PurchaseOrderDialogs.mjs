import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import assert from 'node:assert/strict';

const html = readFileSync(resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html'), 'utf8');

function section(startMarker, endMarker, label) {
  const start = html.indexOf(startMarker);
  const end = html.indexOf(endMarker, start);
  assert.ok(start >= 0 && end > start, `${label} source block must exist`);
  return html.slice(start, end);
}

function assertDialogBeforeRequest(block, requestPath, label) {
  const dialog = block.indexOf('confirmEmployeeAction(');
  const request = block.indexOf(requestPath);
  assert.ok(dialog >= 0, `${label} must use the cancellable in-app confirmation`);
  assert.ok(request > dialog, `${label} must await confirmation before its API request`);
  assert.doesNotMatch(block, /\b(?:window\.)?confirm\s*\(/, `${label} must not open a browser-native confirmation`);
}

const assetCancel = section("$('#assetOrderRows').addEventListener('click'", "$('#assetOrderReceivedAssets').addEventListener", 'asset order row actions');
assertDialogBeforeRequest(assetCancel, '/api/asset-orders/${button.dataset.id}/cancel', 'asset order cancellation');
assert.ok(assetCancel.includes('loadAssetOrders()') && assetCancel.includes('Sipariş iptal edildi; alınmış varlıklar korundu.'), 'asset cancellation must refresh and report the retained received assets');

const assetAttachmentRemoval = section('function renderAssetOrderAttachments(data)', 'async function openAssetOrderReceive(', 'asset order attachment actions');
assertDialogBeforeRequest(assetAttachmentRemoval, '/api/asset-orders/${orderId}/attachments/${button.dataset.id}', 'asset order attachment removal');
assert.ok(assetAttachmentRemoval.includes('renderAssetOrderAttachments(fresh)'), 'asset attachment removal must reload the selected order before refreshing its list');

const stockCancel = section("$('#stockOrderRows').addEventListener('click'", "$('#receiveStockOrderFromDetail').addEventListener", 'stock order row actions');
assertDialogBeforeRequest(stockCancel, '/api/stock-orders/${button.dataset.id}/cancel', 'stock order cancellation');
assert.ok(stockCancel.includes('loadStockOrders()') && stockCancel.includes('teslim alınmış stok korundu'), 'stock cancellation must refresh and report retained received stock');

const stockAttachmentRemoval = section('async function removeStockOrderAttachment(orderId, attachment)', 'async function openStockOrderReceive(', 'stock order attachment removal');
assertDialogBeforeRequest(stockAttachmentRemoval, '/api/stock-orders/${orderId}/attachments/${attachment.id}', 'stock order attachment removal');
assert.ok(stockAttachmentRemoval.includes('renderStockOrderAttachments(orderId, data.attachments)'), 'stock attachment removal must reload the selected order before refreshing its list');

const accessoryCancel = section("$('#accessoryOrderRows').addEventListener('click'", "$('#receiveAccessoryOrderFromDetail').addEventListener", 'accessory order row actions');
assertDialogBeforeRequest(accessoryCancel, '/api/accessory-orders/${button.dataset.id}/cancel', 'accessory order cancellation');
assert.ok(accessoryCancel.includes('loadAccessoryOrders()') && accessoryCancel.includes('teslim alınmış stok değişmedi'), 'accessory cancellation must refresh and report retained received stock');

const accessoryAttachmentRemoval = section("$('#accessoryOrderAttachments').addEventListener('click'", 'async function openAccessoryOrderReceive(', 'accessory order attachment actions');
assertDialogBeforeRequest(accessoryAttachmentRemoval, '/api/accessory-orders/${currentAccessoryOrder.order.id}/attachments/${button.dataset.id}', 'accessory order attachment removal');
assert.ok(accessoryAttachmentRemoval.includes('loadAccessoryOrderAttachments()'), 'accessory attachment removal must reload the selected order attachments');

console.log('Verified cancellable in-app confirmations precede all three purchase-order cancellation and attachment-removal APIs; success refreshes the selected list/detail.');
