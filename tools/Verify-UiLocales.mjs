import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import vm from 'node:vm';

const sourcePath = resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html');
const source = readFileSync(sourcePath, 'utf8');
const baseMatch = source.match(/const localeRows\s*=\s*(\{[\s\S]*?\n\s{4}\});/);
const additionsMatch = source.match(/Object\.assign\(localeRows,\s*(\{[\s\S]*?\n\s{4}\})\);/);

if (!baseMatch || !additionsMatch) {
  throw new Error(`Could not locate the locale dictionaries in ${sourcePath}`);
}

const sandbox = {};
vm.runInNewContext(
  `globalThis.localeRows = ${baseMatch[1]}; Object.assign(localeRows, ${additionsMatch[1]});`,
  sandbox,
  { timeout: 1000 }
);

const requiredAssetLabels = [
  'Ayrıntıyı aç',
  'Zimmeti iade al',
  'Çalışana zimmetle',
  'Fiziksel denetim kaydı aç',
  'Envanter etiketini yazdır',
  'Varlığı düzenle',
  'Varlığı arşive taşı',
  'Görsel yok',
  'Görsel açılamadı',
  'Kişisel cihaz'
];
const requiredReservationLabels = [
  'Varlık rezervasyonları', 'Rezervasyon isteği oluştur', 'Şirket kapsamlı', 'Talep notu',
  'Etiket, varlık, çalışan veya not ara', 'Tüm durumlar', 'Onay bekliyor', 'Onaylandı',
  'Reddedildi', 'İptal edildi', 'Tamamlandı', 'Kullanım dönemi', 'Talep eden',
  'Not / karar', 'İşlemler', 'Bu ölçütlerde rezervasyon bulunamadı.', 'Rezervasyonlar yükleniyor…',
  'Onayla', 'Reddet', 'İptal et', 'Rezervasyon iste', 'CSV dışa aktar', 'Yenile',
  'Bu rezervasyon isteği iptal edilsin mi?', 'Rezervasyon isteği gönderildi; varlık yöneticisinin onayı bekleniyor.',
  'Rezervasyonlar yenilendi.', 'Başlangıç', 'Bitiş', 'Sayfa', 'Kayıt yok'
];

const invalidRows = Object.entries(sandbox.localeRows)
  .filter(([key, values]) => !key.trim() || !Array.isArray(values) || values.length !== 4 || values.some(value => typeof value !== 'string' || !value.trim()))
  .map(([key]) => key || '<empty key>');
const missingAssetLabels = requiredAssetLabels.filter(key => !sandbox.localeRows[key]);
const missingReservationLabels = requiredReservationLabels.filter(key => !sandbox.localeRows[key]);

if (invalidRows.length || missingAssetLabels.length || missingReservationLabels.length) {
  if (invalidRows.length) console.error(`Invalid 4-language locale entries: ${invalidRows.join(', ')}`);
  if (missingAssetLabels.length) console.error(`Missing asset-list locale entries: ${missingAssetLabels.join(', ')}`);
  if (missingReservationLabels.length) console.error(`Missing reservation-page locale entries: ${missingReservationLabels.join(', ')}`);
  process.exitCode = 1;
} else {
  console.log(`Verified ${Object.keys(sandbox.localeRows).length} locale entries in Turkish, English, Russian, Ukrainian, and Macedonian.`);
}
