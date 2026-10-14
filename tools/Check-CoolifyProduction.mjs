#!/usr/bin/env node
import { pathToFileURL } from 'node:url';

const DEFAULT_URLS = [
  'https://envantertr.com/api/status',
  'https://firma.envantertr.com/api/status',
];

export async function inspectEndpoint(url, fetchImpl = fetch, timeoutMs = 10000) {
  let safeUrl = null;
  let displayUrl = 'invalid HTTPS endpoint';
  try {
    const parsed = new URL(url);
    displayUrl = `${parsed.protocol}//${parsed.host}${parsed.pathname}`;
    parsed.username = '';
    parsed.password = '';
    parsed.search = '';
    parsed.hash = '';
    safeUrl = parsed.toString();
    if (parsed.protocol !== 'https:') safeUrl = null;
  } catch {
    // Never include an unparseable caller-supplied URL in diagnostics.
  }
  if (!safeUrl) {
    return {
      url: displayUrl,
      httpStatus: null,
      appStatus: null,
      migrationCount: null,
      pendingMigrationCount: null,
      diagnosis: 'Endpoint must be a valid HTTPS URL.',
      ready: false,
    };
  }

  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);
  try {
    const response = await fetchImpl(safeUrl, {
      method: 'GET',
      headers: { accept: 'application/json' },
      redirect: 'manual',
      signal: controller.signal,
    });
    const contentType = response.headers.get('content-type') || '';
    const text = await response.text();
    let payload = null;
    if (contentType.toLowerCase().includes('application/json')) {
      try { payload = JSON.parse(text); } catch { /* Report malformed status without exposing its body. */ }
    }

    let diagnosis;
    if (response.status === 503 && /no available server/i.test(text)) {
      diagnosis = 'Coolify proxy has no healthy upstream for this host.';
    } else if (!response.ok) {
      diagnosis = `HTTP ${response.status}; inspect the route and service response.`;
    } else if (!payload || typeof payload.status !== 'string') {
      diagnosis = 'Status endpoint did not return the expected JSON health payload.';
    } else if (payload.status === 'migration-required') {
      const pending = Number.isInteger(payload.pendingMigrationCount) ? payload.pendingMigrationCount : 'unknown';
      diagnosis = `Database schema is behind; ${pending} migration(s) pending. Back up and verify it before a controlled migration.`;
    } else if (payload.status === 'ready' && payload.pendingMigrationCount === 0) {
      diagnosis = 'Application and database schema report ready.';
    } else {
      diagnosis = `Application reports status "${payload.status}"; investigate before customer use.`;
    }

    return {
      url: displayUrl,
      httpStatus: response.status,
      appStatus: payload?.status ?? null,
      migrationCount: Number.isInteger(payload?.migrationCount) ? payload.migrationCount : null,
      pendingMigrationCount: Number.isInteger(payload?.pendingMigrationCount) ? payload.pendingMigrationCount : null,
      diagnosis,
      ready: response.ok && payload?.status === 'ready' && payload.pendingMigrationCount === 0,
    };
  } catch (error) {
    return {
      url: displayUrl,
      httpStatus: null,
      appStatus: null,
      migrationCount: null,
      pendingMigrationCount: null,
      diagnosis: error?.name === 'AbortError' ? `Timed out after ${timeoutMs} ms.` : 'Request failed; verify DNS, TLS, and network reachability.',
      ready: false,
    };
  } finally {
    clearTimeout(timer);
  }
}

export async function checkProduction(urls = DEFAULT_URLS, fetchImpl = fetch) {
  const results = [];
  for (const url of urls) results.push(await inspectEndpoint(url, fetchImpl));
  return { ready: results.length > 0 && results.every(result => result.ready), results };
}

async function main() {
  const urls = process.argv.slice(2);
  const report = await checkProduction(urls.length ? urls : DEFAULT_URLS);
  for (const result of report.results) {
    console.log(`${result.ready ? 'READY' : 'NOT READY'} ${result.url} HTTP ${result.httpStatus ?? 'unreachable'}${result.appStatus ? ` (${result.appStatus})` : ''}`);
    console.log(`  ${result.diagnosis}`);
  }
  if (!report.ready) process.exitCode = 1;
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) await main();
