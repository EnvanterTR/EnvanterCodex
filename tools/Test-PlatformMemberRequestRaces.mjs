import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { runInNewContext } from 'node:vm';

const source = await readFile('src/EnvanterTR.Web/wwwroot/platform-admin-members.js', 'utf8');
const elements = new Map();
function element(id) {
  if (!elements.has(id)) elements.set(id, {
    id, value: '', innerHTML: '', textContent: '', dataset: {}, disabled: false, hidden: false,
    listeners: new Map(), classList: { add() {}, remove() {} },
    addEventListener(type, callback) { const handlers = this.listeners.get(type) || []; handlers.push(callback); this.listeners.set(type, handlers); },
    querySelector() { return null; },
  });
  return elements.get(id);
}
const document = {
  querySelector(selector) { return selector.startsWith('#') ? element(selector.slice(1)) : element(selector); },
  querySelectorAll() { return []; },
};
const fetch = async (url) => {
  const parsed = new URL(url, 'http://localhost');
  const query = parsed.searchParams.get('q');
  const delay = query === 'old' ? 50 : 5;
  const displayName = query === 'old' ? 'Old response' : 'Current response';
  await new Promise(resolve => setTimeout(resolve, delay));
  return {
    ok: true,
    async json() { return { page: 1, pageSize: 50, total: 1, items: [{
      id: `user-${query}`, displayName, email: `${query}@example.invalid`, emailConfirmed: true,
      phoneNumber: null, twoFactorEnabled: true, isLockedOut: false, accountCreatedAt: null,
      firstMembershipAt: null, lastSeenAt: null, membershipCount: 0, memberships: []
    }] }; }
  };
};
runInNewContext(source, { document, fetch, URLSearchParams, Intl, setTimeout, clearTimeout, console });
const search = element('searchMembers').listeners.get('click')[0];
element('memberSearch').value = 'old';
const olderRequest = search();
element('memberSearch').value = 'new';
const newerRequest = search();
await Promise.all([olderRequest, newerRequest]);
const html = element('membersTable').innerHTML;
assert.match(html, /Current response/, 'the newest member search should render');
assert.doesNotMatch(html, /Old response/, 'a slower earlier response must not overwrite the latest result');
console.log('Platform member search race acceptance passed (older response resolved last).');
