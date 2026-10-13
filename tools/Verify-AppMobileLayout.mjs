import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const sourcePath = resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html');
const source = readFileSync(sourcePath, 'utf8');
const failures = [];
const bodyRule = source.match(/(?:^|})body\{([^}]*)\}/)?.[1] || '';
const pageHeadEnd = source.indexOf('</head>');
const mobileStylePosition = source.indexOf('<style id="mobile-topbar-layout">');
const mobileStyle = mobileStylePosition >= 0 && mobileStylePosition < pageHeadEnd
  ? source.slice(mobileStylePosition, source.indexOf('</style>', mobileStylePosition) + '</style>'.length)
  : '';

if (!source.includes('<meta name="viewport" content="width=device-width, initial-scale=1">')) {
  failures.push('The company app must use the device viewport on mobile.');
}
if (/\bmin-width\s*:\s*\d+px/i.test(bodyRule)) {
  failures.push('The document body must not force a minimum width that creates page-level horizontal scrolling.');
}
if (!source.includes('@media(max-width:720px){.app{display:block}') || !source.includes('.main{min-width:0}')) {
  failures.push('The mobile shell must allow its main content to shrink to the viewport.');
}
if (!mobileStyle.includes('@media(max-width:720px){html,body{overflow-x:hidden}.topbar{display:grid;grid-template-columns:38px minmax(0,1fr) 32px 32px 32px auto;') ||
    !mobileStyle.includes('grid-template-areas:"menu spacer appearance help notifications profile" "search search search search search search"') ||
    !mobileStyle.includes('.search{grid-area:search;max-width:none;min-width:0;width:100%}')) {
  failures.push('The mobile top bar must wrap search onto its own row, and the document must clip page-level horizontal overflow.');
}
if (!source.includes('.auth-layer{position:fixed;inset:0;') || !source.includes('.auth-card{width:min(440px,100%)')) {
  failures.push('The sign-in card must fit inside narrow viewports.');
}
if (!source.includes('<style id="mobile-dashboard-welcome-fix">@media(max-width:430px){.welcome{align-items:flex-start;flex-direction:column}.welcome>div{min-width:0;width:100%}.period{max-width:100%;white-space:normal;overflow-wrap:anywhere}}</style>')) {
  failures.push('The dashboard status badge must wrap below the welcome text on narrow screens without clipping.');
}

if (failures.length) {
  console.error(failures.map(failure => `FAIL ${failure}`).join('\n'));
  process.exitCode = 1;
} else {
  console.log('Verified the mobile app shell, sign-in card, two-row top bar, and dashboard welcome status fit narrow viewports.');
}
