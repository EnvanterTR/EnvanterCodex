import { readFile } from 'node:fs/promises';
import assert from 'node:assert/strict';

const html = await readFile(new URL('../src/EnvanterTR.Web/wwwroot/index.html', import.meta.url), 'utf8');

function section(start, end) {
  const from = html.indexOf(start);
  assert.notEqual(from, -1, `Missing UI handler: ${start}`);
  const to = html.indexOf(end, from + start.length);
  assert.notEqual(to, -1, `Missing end marker for UI handler: ${start}`);
  return html.slice(from, to);
}

function assertCapturedForm(start, end, label) {
  const code = section(start, end);
  const capture = code.indexOf('const formElement = event.currentTarget');
  const awaitCall = code.indexOf('await ');
  assert.ok(capture >= 0 && awaitCall > capture, `${label}: capture the form before awaiting`);
  assert.match(code, /formElement\.reset\(\)/, `${label}: reset the captured form`);
  assert.doesNotMatch(code, /event\.currentTarget\.reset\(\)/, `${label}: do not access currentTarget after await`);
}

assertCapturedForm("$('#newEmployeeForm').addEventListener('submit'", "$('#employeeSearch')", 'employee form');
assertCapturedForm("$('#inviteMemberForm').addEventListener('submit'", "$('#reservationForm').addEventListener('submit'", 'member invitation form');
assertCapturedForm("$('#reservationForm').addEventListener('submit'", "$('#reservationSearch').addEventListener('input'", 'reservation form');
assertCapturedForm("$('#forgotPasswordForm').addEventListener('submit'", "$('#logoutButton').addEventListener('click'", 'forgot password form');

const assetAttachment = section("$('#assetAttachmentForm').addEventListener('submit'", "$('#copyAssetTag').addEventListener('click'",);
assert.match(assetAttachment, /const formElement = event\.currentTarget[\s\S]*?await api\([\s\S]*?formElement\.reset\(\)/, 'asset attachment form must retain its form reference across awaits');
assert.doesNotMatch(assetAttachment, /event\.currentTarget\.reset\(\)/, 'asset attachment must not access currentTarget after await');

const checkoutRenderer = section('async function loadAssetCheckouts(', 'async function loadAudit(');
assert.match(checkoutRenderer, /const formatCheckoutDate = value =>/, 'assignment and returns lists need a local date formatter');
assert.match(checkoutRenderer, /item\.expectedReturnOn \? formatCheckoutDate\(item\.expectedReturnOn\)/, 'expected return date must render with local formatter');
assert.match(checkoutRenderer, /item\.assignedAt \? formatCheckoutDate\(item\.assignedAt\)/, 'assignment date must render with local formatter');
assert.doesNotMatch(checkoutRenderer, /\bformatDate\(/, 'assignment and returns must not call out-of-scope formatDate');

console.log('Async form references and assignment/return date rendering contracts passed.');
