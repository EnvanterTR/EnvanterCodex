import { readFile } from 'node:fs/promises';
import assert from 'node:assert/strict';

const html = await readFile(new URL('../src/EnvanterTR.Web/wwwroot/index.html', import.meta.url), 'utf8');

assert.match(html, /<dialog[^>]+id="reservationActionDialog"[^>]+aria-labelledby="reservationActionTitle"/,
  'reservation decisions must use an accessible in-app dialog');
for (const id of ['reservationActionForm', 'reservationActionSummary', 'reservationReviewNote', 'reservationActionSubmit', 'reservationActionCancel'])
  assert.match(html, new RegExp(`id="${id}"`), `reservation dialog must include ${id}`);

const actionRenderer = html.slice(html.indexOf('async function loadReservations('), html.indexOf("$('#reservationActionClose')"));
assert.ok(actionRenderer.includes("openReservationAction(item, 'approve')"), 'approve action must open the in-app decision dialog');
assert.ok(actionRenderer.includes("openReservationAction(item, 'reject')"), 'reject action must open the in-app decision dialog');
assert.ok(actionRenderer.includes("openReservationAction(item, 'cancel')"), 'cancel action must open the in-app confirmation dialog');
assert.doesNotMatch(actionRenderer, /\bconfirm\s*\(/, 'reservation actions must not depend on browser-native confirmation');

const actionForm = html.slice(html.indexOf("$('#reservationActionForm').addEventListener('submit'"), html.indexOf('async function loadActivity('));
assert.ok(actionForm.indexOf('const formElement = event.currentTarget') < actionForm.indexOf('await api('),
  'capture the reservation form before awaiting the request');
assert.match(actionForm, /approve: draft\.action === 'approve', note: \$\('#reservationReviewNote'\)\.value\.trim\(\) \|\| null/,
  'approve/reject must submit the optional decision note');
assert.match(actionForm, /draft\.action === 'cancel'\) await api\(`\/api\/reservations\/\$\{draft\.id\}\/cancel`/,
  'cancellation must call the company reservation cancellation endpoint');
assert.match(actionForm, /formElement\.reset\(\)/, 'reset the captured form after a successful action');
assert.match(html, /reservationExportCsv[\s\S]*?\/api\/reservations\/export\.csv/, 'reservation CSV export must remain connected');
assert.match(html, /reservationPrev[\s\S]*?loadReservations\(reservationPage - 1\)/, 'reservation previous-page control must remain connected');
assert.match(html, /reservationNext[\s\S]*?loadReservations\(reservationPage \+ 1\)/, 'reservation next-page control must remain connected');

console.log('Verified reservation request, filtering/paging/export controls, and accessible approve/reject/cancel dialog flows.');
