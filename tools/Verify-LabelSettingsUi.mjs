import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const sourcePath = resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html');
const source = readFileSync(sourcePath, 'utf8');
const required = [
  ['1D formats', "['code128','code39','code93','code93ext','rationalizedCodabar','msi','ean5','ean13','upca','upce','none']"],
  ['2D formats', "['qrcode','pdf417','datamatrix','azteccode','none']"],
  ['company settings load/save', "api('/api/company/label-settings'"],
  ['company printer template catalog', "api('/api/company/label-templates'"],
  ['template load', "$('#labelStockTemplateLoad').addEventListener('click'"],
  ['template save', "$('#labelStockTemplateSave').addEventListener('click'"],
  ['template delete', "$('#labelStockTemplateDelete').addEventListener('click'"],
  ['QR encoder', 'qrcodegen.QrCode.encodeText'],
  ['1D barcode renderer', 'bwip.toCanvas(canvas'],
  ['2D disabled state', "settings.barcode2d !== 'none'"],
  ['1D disabled state', "settings.barcode1d !== 'none'"],
  ['asset list shortcut', "$('#labelSettingsGoAssets').addEventListener('click'"],
  ['reset submits defaults', "$('#labelSettingsForm').requestSubmit()"],
  ['sheet layout persistence', 'saveLabelSheetSettings()'],
  ['asset print preview', 'openAssetLabelPreview(cards, settings, labelSheetSettings)'],
];
const missing = required.filter(([, text]) => !source.includes(text)).map(([name]) => name);
if (missing.length) throw new Error(`Missing QR/barcode UI contracts: ${missing.join(', ')}`);
console.log(`Verified ${required.length} QR/barcode UI contracts: format options, company settings/templates, previews, reset, sheet layout, and asset shortcut.`);
