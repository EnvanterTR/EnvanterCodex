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
  const defaultRun = urls === DEFAULT_URLS;
  if (defaultRun && urls.length > 0) {
    const base = new URL(urls[0]).origin;
    results.push(...await inspectCommercialReadiness(base, fetchImpl));
  }
  return { ready: results.length > 0 && results.every(result => result.ready), results };
}

function safeHttpsOrigin(value) {
  try {
    const parsed = new URL(value);
    if (parsed.protocol !== 'https:') return null;
    return parsed.origin;
  } catch {
    return null;
  }
}

async function inspectJsonEndpoint(url, fetchImpl, timeoutMs = 10000) {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);
  try {
    const response = await fetchImpl(url, { method: 'GET', headers: { accept: 'application/json' }, redirect: 'manual', signal: controller.signal });
    const contentType = response.headers.get('content-type') || '';
    if (!response.ok || !contentType.toLowerCase().includes('application/json')) return { response, payload: null, error: null };
    try { return { response, payload: JSON.parse(await response.text()), error: null }; }
    catch { return { response, payload: null, error: null }; }
  } catch (error) {
    return { response: null, payload: null, error: error?.name === 'AbortError' ? 'Request timed out.' : 'Request failed; verify DNS, TLS, and network reachability.' };
  } finally {
    clearTimeout(timer);
  }
}

export async function inspectCommercialReadiness(baseUrl, fetchImpl = fetch) {
  const origin = safeHttpsOrigin(baseUrl);
  if (!origin) {
    return [
      { url: 'invalid HTTPS public API origin', httpStatus: null, publicPlanCount: null, diagnosis: 'Public API origin must use HTTPS.', ready: false },
      { url: 'invalid HTTPS public API origin', httpStatus: null, checkoutAvailable: null, diagnosis: 'Public API origin must use HTTPS.', ready: false },
    ];
  }

  const plansUrl = `${origin}/api/plans`;
  const checkoutUrl = `${origin}/api/checkout/status`;
  const [plansResult, checkoutResult] = await Promise.all([
    inspectJsonEndpoint(plansUrl, fetchImpl),
    inspectJsonEndpoint(checkoutUrl, fetchImpl),
  ]);
  const plans = Array.isArray(plansResult.payload) ? plansResult.payload : null;
  const publicPlanCount = plans?.length ?? null;
  const plansReady = plansResult.response?.ok === true && plans !== null && publicPlanCount > 0;
  const planDiagnosis = !plansResult.response
    ? plansResult.error
    : !plansResult.response.ok
    ? `HTTP ${plansResult.response.status}; public plans could not be checked.`
    : plans === null
      ? 'Public plans endpoint did not return the expected JSON array.'
      : publicPlanCount === 0
        ? 'No published Cloud plans; publish an active public plan before opening online sales.'
        : `${publicPlanCount} public plan(s) are available.`;

  const checkoutAvailable = typeof checkoutResult.payload?.available === 'boolean'
    ? checkoutResult.payload.available
    : null;
  const checkoutReady = checkoutResult.response?.ok === true && checkoutAvailable === true;
  const checkoutDiagnosis = !checkoutResult.response
    ? checkoutResult.error
    : !checkoutResult.response.ok
    ? `HTTP ${checkoutResult.response.status}; checkout configuration could not be checked.`
    : checkoutAvailable === null
      ? 'Checkout status endpoint did not return the expected availability flag.'
      : checkoutAvailable
        ? 'Payment provider and trusted callback configuration report available.'
        : 'Online checkout is unavailable; payment provider keys, API base, and trusted callback must be configured.';

  return [
    { url: plansUrl, httpStatus: plansResult.response?.status ?? null, publicPlanCount, diagnosis: planDiagnosis, ready: plansReady },
    { url: checkoutUrl, httpStatus: checkoutResult.response?.status ?? null, checkoutAvailable, diagnosis: checkoutDiagnosis, ready: checkoutReady },
  ];
}

async function main() {
  const urls = process.argv.slice(2);
  const report = await checkProduction(urls.length ? urls : DEFAULT_URLS);
  for (const result of report.results) {
    const detail = result.appStatus
      ? ` (${result.appStatus})`
      : Number.isInteger(result.publicPlanCount)
        ? ` (${result.publicPlanCount} public plan(s))`
        : typeof result.checkoutAvailable === 'boolean'
          ? ` (checkout ${result.checkoutAvailable ? 'available' : 'unavailable'})`
          : '';
    console.log(`${result.ready ? 'READY' : 'NOT READY'} ${result.url} HTTP ${result.httpStatus ?? 'unreachable'}${detail}`);
    console.log(`  ${result.diagnosis}`);
  }
  if (!report.ready) process.exitCode = 1;
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) await main();
