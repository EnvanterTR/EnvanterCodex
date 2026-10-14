import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import vm from 'node:vm';

const sourcePath = resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html');
const source = readFileSync(sourcePath, 'utf8');
const failures = [];
const requireSource = (condition, message) => { if (!condition) failures.push(message); };
const count = (text, value) => text.split(value).length - 1;

for (const id of ['commandPaletteDialog', 'commandPaletteSearch', 'commandPaletteResults', 'keyboardShortcutsDialog']) {
  requireSource(count(source, `id="${id}"`) === 1, `Expected exactly one ${id} element.`);
}

const setupIndex = source.indexOf("const commandPaletteDialog = $('#commandPaletteDialog')");
const handlerIndex = source.indexOf("document.addEventListener('keydown'", setupIndex);
const handlerEnd = source.indexOf("let searchTimer;", handlerIndex);
const handler = setupIndex < 0 || handlerIndex < 0 || handlerEnd < 0 ? '' : source.slice(handlerIndex, handlerEnd);
const palette = setupIndex < 0 ? '' : source.slice(setupIndex, handlerIndex);

requireSource(handler.includes("modified && key === 'k'") && handler.includes('openCommandPalette()'), 'Ctrl/Command+K must open the command palette.');
requireSource(handler.includes("const key = (event.key || '').toLocaleLowerCase()"), 'Keyboard shortcut handling must tolerate synthetic or incomplete key events without throwing.');
requireSource(handler.includes("modified && key === 'n' && !typing") && handler.includes('canWriteAssets()') && handler.includes("$('#newAssetButton').click()"), 'Ctrl/Command+N must be ignored while typing and open asset creation only for authorized users.');
requireSource(handler.includes("event.key === '/' && !typing") && handler.includes("$('#globalSearch').focus()") && handler.includes("$('#globalSearch').select()"), 'Slash must focus and select global search when the user is not editing text.');
requireSource(handler.includes("event.key === '?' && !typing") && handler.includes("$('#keyboardShortcutsDialog').showModal()"), 'Question mark must open the keyboard-shortcut help dialog outside text fields.');
requireSource(handler.includes("if (event.key !== 'Escape') return") && handler.includes('closeOperationalAlerts()') && handler.includes('setMobileSidebarOpen(false)'), 'Escape must preserve notification and mobile-sidebar dismissal behavior.');
requireSource(palette.includes("document.querySelectorAll('#sidebar [data-page]')") && palette.includes("button.dataset.page === entry.value") && palette.includes("?.click()"), 'Command palette destinations must be derived from and activate the actual sidebar routes.');
requireSource(palette.includes("commandPaletteSearch.addEventListener('input', renderCommandPalette)") && palette.includes('toLocaleLowerCase(locale)') && palette.includes('commandPaletteResults.replaceChildren()'), 'Command palette search must filter and refresh its result list.');
requireSource(source.includes('id="commandPaletteResults" class="command-palette-results" role="menu"') && palette.includes("setAttribute('role', 'menuitem')"), 'Command palette results must expose accessible menu and menuitem semantics.');
requireSource(palette.includes("event.key === 'Enter' && event.target === commandPaletteSearch") && palette.includes("['ArrowDown', 'ArrowUp']"), 'Command palette must support Enter and arrow-key navigation.');
requireSource(palette.includes("event.target === commandPaletteDialog") && palette.includes("event.currentTarget.close()"), 'New dialogs must close from their close button, Escape, and outside click.');
requireSource(source.includes('aria-keyshortcuts="Control+K"') && source.includes("setAttribute('aria-keyshortcuts', '/')") && source.includes("setAttribute('aria-keyshortcuts', 'Control+N')") && source.includes("setAttribute('aria-keyshortcuts', '?')") && source.includes('id="keyboardShortcutsDialog"'), 'Keyboard shortcut entry points must be discoverable to assistive technology and documented in the help dialog.');
requireSource(palette.includes("label: translateUiText('Yeni varlık'), group: translateUiText('Hızlı işlem')") && palette.includes("label: translateUiText('Barkod okut'), group: translateUiText('Hızlı işlem')") && palette.includes("group: translateUiText('Sayfa')"), 'Command palette labels and groups must be localized before rendering and filtering.');

const entriesFunction = palette.match(/function commandPaletteEntries\(\) \{[\s\S]*?\n    \}/)?.[0];
requireSource(Boolean(entriesFunction), 'Could not locate the command palette entry builder.');
if (entriesFunction) {
  const languageCases = [
    ['tr', 'tr-TR', 'Yeni varlık', 'Barkod okut', 'Hızlı işlem', 'Sayfa'],
    ['en', 'en-US', 'New asset', 'Scan barcode', 'Quick action', 'Page'],
    ['ru', 'ru-RU', 'Новый актив', 'Сканировать штрихкод', 'Быстрое действие', 'Страница'],
    ['uk', 'uk-UA', 'Новий актив', 'Сканувати штрихкод', 'Швидка дія', 'Сторінка'],
    ['mk', 'mk-MK', 'Ново средство', 'Скенирај баркод', 'Брзо дејство', 'Страница']
  ];
  const labels = new Map(languageCases.map(([language, , newAsset, barcode, quickAction, page]) =>
    [language, new Map([['Yeni varlık', newAsset], ['Barkod okut', barcode], ['Hızlı işlem', quickAction], ['Sayfa', page]])]));
  for (const [language, locale, expectedAsset, expectedBarcode] of languageCases) {
    const buttons = [{ dataset: { page: 'Varlıklar' }, textContent: language === 'tr' ? 'Varlıklar' : labels.get(language).get('Sayfa') }];
    const context = {
      document: { querySelectorAll: () => buttons },
      $: selector => ({ hidden: false }),
      canWriteAssets: () => true,
      translateUiText: value => labels.get(language).get(value) || value
    };
    try {
      const entries = vm.runInNewContext(`(${entriesFunction})`, context, { timeout: 1000 })();
      const matches = query => entries.filter(entry => `${entry.label} ${entry.group}`.toLocaleLowerCase(locale).includes(query.toLocaleLowerCase(locale)));
      const assetHits = matches(expectedAsset);
      const barcodeHits = matches(expectedBarcode);
      requireSource(assetHits.length === 1 && assetHits[0].value === 'newAssetButton', `${language}: localized asset command search must return the authorized quick action.`);
      requireSource(barcodeHits.length === 1 && barcodeHits[0].value === 'scanAssetButton', `${language}: localized barcode search must return its quick action.`);
    } catch (error) {
      failures.push(`Could not execute localized command palette entries for ${language}: ${error.message}`);
    }
  }
}

if (failures.length) {
  console.error(failures.map(message => `FAIL ${message}`).join('\n'));
  process.exitCode = 1;
} else {
  console.log('Verified Ctrl/Command+K, Ctrl/Command+N, /, ?, and Escape; accessible command search/navigation; sidebar route activation; and authorized quick actions.');
}
