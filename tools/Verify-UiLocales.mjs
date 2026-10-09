import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import vm from 'node:vm';

const sourcePath = resolve(process.argv[2] || 'src/EnvanterTR.Web/wwwroot/index.html');
const source = readFileSync(sourcePath, 'utf8');
const baseMatch = source.match(/const localeRows\s*=\s*(\{[\s\S]*?\n\s{4}\});/);
const additionsMatches = [...source.matchAll(/Object\.assign\(localeRows,\s*(\{[\s\S]*?\n\s{4}\})\);/g)];

if (!baseMatch || additionsMatches.length === 0) {
  throw new Error(`Could not locate the locale dictionaries in ${sourcePath}`);
}

const sandbox = {};
const localeAssignments = additionsMatches.map(match => `Object.assign(localeRows, ${match[1]});`).join('\n');
vm.runInNewContext(
  `globalThis.localeRows = ${baseMatch[1]}; ${localeAssignments}`,
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
const requiredAccessoryLabels = [
  'Aksesuarlar', 'Aksesuar stoklarını, teslimleri, iadeleri ve satın alma geçmişini yönetin.',
  'Aksesuar ekle', 'CSV indir', 'Aksesuarlarda ara', 'Stok durumuna göre filtrele',
  'Tüm stoklar', 'Kullanılabilir stok', 'Minimum seviyede veya altında', 'Aksesuarları sırala',
  'Sütunlar', 'Görüntülenecek sütunlar', 'Kategori / model', 'Tedarikçi / konum',
  'Zimmetli', 'Kalan', 'Minimum', 'Birim / toplam maliyet', 'İşlemler',
  'Kategori ve konum filtreleri', 'Filtreleri temizle', 'Aksesuarlar yükleniyor…',
  'Stok kodu tanımlanmamış', 'Az stok', 'Stokta', 'Ayrıntı', 'Düzenle', 'Teslim',
  'Bu filtrelerle eşleşen aksesuar bulunamadı.', 'Henüz aksesuar kaydı yok. İlk stok kaydını ekleyebilirsiniz.',
  'aksesuarını toplu işlem için seç', 'Sayfadakileri seç', 'Sayfa seçildi', 'Seçimi temizle',
  'Seçilenleri teslim et', 'Seçilen stokları düzelt', 'aksesuar seçildi (en fazla 100)'
];

const invalidRows = Object.entries(sandbox.localeRows)
  .filter(([key, values]) => !key.trim() || !Array.isArray(values) || values.length !== 4 || values.some(value => typeof value !== 'string' || !value.trim()))
  .map(([key]) => key || '<empty key>');
const missingAssetLabels = requiredAssetLabels.filter(key => !sandbox.localeRows[key]);
const missingReservationLabels = requiredReservationLabels.filter(key => !sandbox.localeRows[key]);
const missingAccessoryLabels = requiredAccessoryLabels.filter(key => !sandbox.localeRows[key]);

if (invalidRows.length || missingAssetLabels.length || missingReservationLabels.length || missingAccessoryLabels.length) {
  if (invalidRows.length) console.error(`Invalid 4-language locale entries: ${invalidRows.join(', ')}`);
  if (missingAssetLabels.length) console.error(`Missing asset-list locale entries: ${missingAssetLabels.join(', ')}`);
  if (missingReservationLabels.length) console.error(`Missing reservation-page locale entries: ${missingReservationLabels.join(', ')}`);
  if (missingAccessoryLabels.length) console.error(`Missing accessory-page locale entries: ${missingAccessoryLabels.join(', ')}`);
  process.exitCode = 1;
} else {
  console.log(`Verified ${Object.keys(sandbox.localeRows).length} locale entries in Turkish, English, Russian, Ukrainian, and Macedonian.`);
}
