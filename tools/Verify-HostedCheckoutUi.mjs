import { readFileSync } from 'node:fs';

const html = readFileSync(new URL('../src/EnvanterTR.PublicSite/checkout.html', import.meta.url), 'utf8');
const script = readFileSync(new URL('../src/EnvanterTR.PublicSite/checkout.js', import.meta.url), 'utf8');
const site = readFileSync(new URL('../src/EnvanterTR.PublicSite/index.html', import.meta.url), 'utf8');
const failures = [];
const check = (label, condition) => { if (!condition) failures.push(label); };

check('checkout page defines every JS-required element', ['checkoutNotice', 'checkoutLayout', 'checkoutForm', 'checkoutFields', 'checkoutSubmit', 'checkoutFeedback', 'newAttempt', 'demoLink'].every(id => html.includes(`id="${id}"`) && script.includes(`#${id}`)));
check('company data stays disabled until API reports payment ready', /fields\.disabled\s*=\s*false/.test(script) && /if\s*\(!paymentAvailable\)/.test(script) && /checkoutFields'\)\.disabled\s*=\s*true/.test(script));
check('public pricing opens checkout only for an API-published Cloud plan with monthly or annual pricing', /cloudPlans\.forEach\(plan=>[\s\S]*?if\(plan\.id&&published&&\(Number\(plan\.monthlyPrice\)>0\|\|Number\(plan\.annualPrice\)>0\)\)/.test(site) && site.includes('new URLSearchParams({planId:plan.id})') && /Number\(plan\.monthlyPrice\)\s*>\s*0/.test(script) && /Number\(plan\.annualPrice\)\s*>\s*0/.test(script));
check('public pricing renders only API-published Cloud plans and uses server-defined names and descriptions', /cloudPlans\.forEach\(plan=>/.test(site) && /addText\(card,'h3','',plan\.name\)/.test(site) && /addText\(card,'p','',plan\.description\|\|''\)/.test(site) && !/const tiers=cloudPlans\.length\?cloudPlans:/.test(site));
check('public pricing distinguishes loading, empty catalog, and API failure in all five languages', /let pricingLoadState = 'loading'/.test(site) && /pricingAvailabilityCopy\[language\]/.test(site) && ['tr', 'en', 'ru', 'uk', 'mk'].every(language => new RegExp(`^\\s*${language}: \\{ loading:`, 'm').test(site)) && /pricingLoadState='error'/.test(site) && /pricingLoadState='empty'/.test(site));
check('submitted amount is absent and plan ID plus term are sent for server-side pricing', /planId:\s*plan\.id/.test(script) && /term,/.test(script) && !/amount\s*:/.test(script));
check('CSRF token is fetched and attached to checkout initialization', /fetch\('\/api\/session\/csrf'/.test(script) && /X-CSRF-TOKEN/.test(script));
check('consent version accompanies acknowledgement and PII is not persisted in browser storage or URL', /dataProcessingAcknowledged:\s*data\.get/.test(script) && /dataProcessingNoticeVersion:\s*noticeVersion/.test(script) && /sessionStorage\.setItem\(attemptKey, id\)/.test(script) && !/(localStorage|sessionStorage)\.setItem\([^\n]*(identityNumber|billingAddress|ownerEmail|phone)/i.test(script) && !/location\.(?:search|href)\s*=/.test(script));
check('provider redirect permits only HTTPS iyzico production or sandbox hosts', /checkoutUrl\.protocol\s*!==\s*'https:'/.test(script) && /\['api\.iyzipay\.com',\s*'sandbox-api\.iyzipay\.com'\]/.test(script));
check('checkout is responsive and keyboard accessible', /@media\(max-width:760px\)/.test(html) && /:focus-visible/.test(html) && /aria-live="assertive"/.test(html));
check('all five public-site languages have purchase button labels', ['tr', 'en', 'ru', 'uk', 'mk'].every(language => new RegExp(`${language}:'[^']+'`).test(site.slice(site.indexOf('const cloudPurchaseLabels'), site.indexOf('const contentTargets')))));

if (failures.length) {
  for (const failure of failures) console.error(`FAIL: ${failure}`);
  process.exit(1);
}
console.log('PASS: hosted checkout UI is price-driven, consent-aware, CSRF-protected, fail-closed, and responsive.');
