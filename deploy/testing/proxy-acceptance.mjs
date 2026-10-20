import assert from 'node:assert/strict';

for (const proxy of ['production', 'local']) {
  const request = path => fetch(`http://${proxy}${path}`, { headers: { host: 'envantertr.com' } });
  const marketing = await request('/');
  assert.equal(marketing.status, 200);
  assert.match(marketing.headers.get('permissions-policy'), /camera=\(\)/);
  const app = await request('/giris');
  assert.equal(app.status, 200);
  assert.equal(app.headers.get('permissions-policy'), 'camera=(self), microphone=(), geolocation=()');
  assert.equal(app.headers.get('x-content-type-options'), 'nosniff');
  assert.equal(app.headers.get('x-frame-options'), 'DENY');
  assert.equal(app.headers.get('referrer-policy'), 'strict-origin-when-cross-origin');
  assert.match(await app.text(), /id="startAssetCamera"/);
  for (const path of ['/vendor/qrcodegen.js', '/vendor/zxing-browser-0.2.1.min.js', '/platform-admin-members.js',
    '/platform-admin-subscriptions.js', '/platform-admin-demo-requests.js', '/platform-admin-payment-follow-ups.js', '/report-print.js', '/scheduled-reports.js']) {
    const response = await request(path);
    assert.equal(response.status, 200, `${proxy} ${path}`);
    assert.match(response.headers.get('content-type'), /javascript/, `${proxy} ${path} must return script MIME, not marketing HTML.`);
    assert.doesNotMatch((await response.text()).slice(0, 200), /<!doctype html>/i);
  }
  console.log(`PASS: ${proxy} nginx serves the app, QR/decoder and all six application modules; camera is self-only on /giris, disabled on marketing; security headers retained.`);
}
