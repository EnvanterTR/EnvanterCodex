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
  'Varlığı kopyala',
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
  'Seçilenleri teslim et', 'Seçilen stokları düzelt', 'aksesuar seçildi (en fazla 100)',
  'AKSESUAR KAYDI', 'Yeni aksesuar ekle', 'Aksesuar kaydını düzenle', 'Ad *', 'Kategori', 'Marka',
  'Marka seçin (isteğe bağlı)', '＋ Marka oluştur', 'Model', 'Model seçin (isteğe bağlı)', '＋ Model oluştur',
  'Model görseli (PNG/JPEG/WebP, en fazla 2 MiB)', 'Model numarası', 'Ürün numarası', 'Tedarikçi', 'Konum',
  'Minimum stok', 'İlk stok adedi', 'Birim maliyeti', 'Para birimi', 'Alış tarihi', 'Notlar',
  'Çalışan isteğine aç', 'İlk adet miktarı ilk stok girişi olarak geçmişe kaydedilir.', 'Kaydet', 'Kapat',
  'Marka adı:', 'Aksesuar model adı:', 'Önce bir marka seçin.',
  'Marka oluşturuldu. Logo ve üretici bilgilerini Markalar kataloğunda ekleyebilirsiniz.',
  'Aksesuar modeli oluşturuldu ve seçildi.', 'Aksesuar kaydı eklemek için yazma yetkiniz bulunmuyor.',
  'ŞİRKET STOK KAYDI', 'Aksesuar ayrıntısı', 'Satın alma siparişleri', 'Tüm siparişleri aç', 'Sipariş no',
  'Tedarikçi / tarih', 'Açık zimmetler', 'Stok ve zimmet geçmişi', 'Stok ekle / düzelt', 'Çalışana teslim et',
  'Yeni sipariş', 'Bu aksesuar için henüz sipariş yok.', 'Açık zimmet bulunmuyor.', 'Henüz stok hareketi yok.',
  'Ürün fotoğrafı', 'PNG, JPEG veya WebP · en fazla 2 MiB. Şirket medya yedeğine eklenir.',
  'Fotoğraf seçin', 'Fotoğrafı yükle', 'Fotoğrafı kaldır', 'Aksesuar fotoğrafı kaldırılsın mı?',
  'Aksesuar fotoğrafı güncellendi.', 'Aksesuar fotoğrafı kaldırıldı.', 'AKSESUAR ZİMMETİ',
  'TOPLU AKSESUAR ZİMMETİ', 'Teslim hedefi seçin', 'Nereye teslim ediliyor?', 'Aktif çalışan *',
  'Varlık *', 'Konum *', 'Adet *', 'Tam kit adedi *', 'Teslim notu', 'Çalışan seçin',
  'Etkin çalışan bulunmuyor', 'Varlık seçin', 'Varlık bulunmuyor', 'Teslimi kaydet',
  'Toplu teslimi kaydet', 'Seçilen aksesuarları teslim et', 'açık zimmet seçildi (en fazla 100)',
  'Aksesuar iadesi kaydedildi; stok ve hareket geçmişi güncellendi.',
  'Aksesuar teslimi kaydedildi ve zimmet geçmişine işlendi.', 'adet kullanılabilir', 'Teslim adedi',
  'Teslim hedefi bulunamadı', 'Employee', 'Asset'
];
const requiredEmployeeLabels = [
  'Çalışan kayıtları', 'Departmana ve yöneticiye göre ilişki kurun; profilden doğrudan raporlayanları ve zimmetlerini görüntüleyin.',
  'Menü, pano, varlık, çalışan listesi/profili ve zimmet formu ile aksesuar listesi ve oluşturma/düzenleme formu seçilen dile çevrildi. Diğer modüller sırayla ekleniyor.',
  '↓ CSV dışa aktar', '＋ Çalışan ekle', 'Ad, personel no, e-posta, departman, görev veya konum ara',
  'Departman filtresi', 'Departmanı temizle', 'Yönetici adı veya personel no ara', 'Tüm yöneticiler',
  'Tüm çalışanlar', 'Aktif çalışanlar', 'Pasif çalışanlar', 'İşe başlama (başlangıç)',
  'İşe başlama (bitiş)', 'Tarihleri temizle', 'Şirket çalışan kayıtları', 'Çalışan',
  'Personel no', 'Görev unvanı', 'İşe başlama', 'Durum', 'İşlem', 'Çalışanlar yükleniyor…',
  'Seçtiğiniz arama, departman, yönetici, işe başlama tarihi ve durum ölçütlerine uygun çalışan bulunamadı.',
  'Bu listede çalışan kaydı yok.', 'çalışan profilini aç', 'Pasife al', 'Etkinleştir',
  'çalışan', 'sayfa', 'ŞİRKET EKİBİ / ÇALIŞAN PROFİLİ', 'Çalışan ayrıntısı',
  'Zimmet formunu yazdır', 'Kapat', 'Yönetici seç', 'Personel numarası', 'İşe başlama tarihi', 'YÖNETİCİ',
  'Doğrudan raporlayanlar', 'Zimmetli varlıklar', 'Aksesuar zimmetleri', 'Aktif lisanslar',
  'Sarf tüketim geçmişi', 'Yayıncı / sürüm', 'Atandığı tarih', 'Süresiz',
  'ÇALIŞAN KAYDI', 'Yeni çalışan ekle', 'Çalışan bilgilerini düzenle', 'Ad soyad *',
  'Personel numarası *', 'E-posta', 'Departman', 'Görev unvanı', 'Telefon', 'Konum',
  'İşe başlama tarihi', 'Yönetici ara', 'Ad veya personel numarası', 'Yönetici seçilmedi',
  'Vazgeç', 'Çalışanı kaydet', 'Değişiklikleri kaydet', 'Çalışan kaydı şirkete eklendi.',
  'Çalışan bilgileri güncellendi.', 'Değişiklik denetim kaydına işlendi.',
  'Çalışan profil fotoğrafı', 'Fotoğraf yükle', 'Fotoğrafı kaldır', 'Çalışan belgeleri',
  'Özel İK notları', 'Belge ekle', 'Zimmetli envanterler',
  'Bu çalışana doğrudan bağlı çalışan bulunmuyor.', 'Bu çalışana zimmetli varlık yok.',
  'Açık aksesuar zimmeti yok.', 'Etkin lisans ataması yok.', 'Sarf tüketim kaydı yok.',
  'Envanter teslim ve zimmet formu', 'Belge tarihi', 'Kontrol / imza', 'Kalan adet',
  'Teslim tarihi', 'ZİMMET BELGESİ', 'Teslim eden yetkili',
  'Bu form, düzenlendiği tarihte şirket kayıtlarında çalışana bağlı görünen envanter ve açık aksesuar zimmetlerini listeler.',
  'Zimmet formu hazırlanıyor…', 'Zimmet formu hazırlanamadı: {error}',
  'Varlıklar eksik yüklendi ({loaded}/{total}).',
  'Aksesuar zimmetleri eksik yüklendi ({loaded}/{total}); eksik form basılmadı.',
  'Yönetici listesi yüklenemedi: {error}', 'varlık detayını aç', 'kayıt', 'kişi',
  'açık zimmet', 'etkin lisans', 'hareket', 'son',
  'ÇALIŞAN KAYITLARI / DOSYA AKTARIMI', 'Çalışanları içe aktar',
  'Şablon başlıkları: Personel No, Ad Soyad, E-posta, Departman, Görev Unvanı, Telefon, Konum, İşe Başlama Tarihi. Tarih biçimi yyyy-AA-gg olmalıdır. En fazla 5.000 satır ve 32 MB.',
  'Uygulamadan dışa aktarılan çalışan CSV dosyası da yeniden içe alınabilir; etkinlik durumu ve Yönetici Personel No ilişkileri korunur. Var olan personel numaraları ve yönetici döngüleri reddedilir. Önizleme kayıt oluşturmaz.',
  'Şablonu indir', 'CSV veya Excel dosyası seç', 'Henüz dosya seçilmedi.', 'Dosyayı doğrula',
  'Doğrulanmış kayıtları ekle', 'Kayıtlı sütun eşlemesi', 'Profil adı', 'Eşlemeyi kaydet',
  'Profili sil', 'Sütunları eşleyin', 'Eşlemeyi kullan ve yeniden doğrula',
  'Dosya değişti. Önce yeniden doğrulayın.',
  'Doğrulanan yeni çalışan kayıtları bu şirkete eklensin mi? Bu işlem mevcut kayıtları güncellemez.',
  'Çalışanlar ekleniyor…', 'Dosya doğrulanıyor; kayıt eklenmiyor…',
  'Dosya 32 MB sınırını aşıyor.', 'Yalnızca .csv veya .xlsx çalışan dosyaları desteklenir.',
  'Doğrulanan yeni çalışan kayıtları bu şirkete eklensin mi? Bu işlem mevcut kayıtları güncellemez.',
  '{count} satır incelendi.'
];

const invalidRows = Object.entries(sandbox.localeRows)
  .filter(([key, values]) => !key.trim() || !Array.isArray(values) || values.length !== 4 || values.some(value => typeof value !== 'string' || !value.trim()))
  .map(([key]) => key || '<empty key>');
const missingAssetLabels = requiredAssetLabels.filter(key => !sandbox.localeRows[key]);
const missingReservationLabels = requiredReservationLabels.filter(key => !sandbox.localeRows[key]);
const missingAccessoryLabels = requiredAccessoryLabels.filter(key => !sandbox.localeRows[key]);
const missingEmployeeLabels = requiredEmployeeLabels.filter(key => !sandbox.localeRows[key]);

if (invalidRows.length || missingAssetLabels.length || missingReservationLabels.length || missingAccessoryLabels.length || missingEmployeeLabels.length) {
  if (invalidRows.length) console.error(`Invalid 4-language locale entries: ${invalidRows.join(', ')}`);
  if (missingAssetLabels.length) console.error(`Missing asset-list locale entries: ${missingAssetLabels.join(', ')}`);
  if (missingReservationLabels.length) console.error(`Missing reservation-page locale entries: ${missingReservationLabels.join(', ')}`);
  if (missingAccessoryLabels.length) console.error(`Missing accessory-page locale entries: ${missingAccessoryLabels.join(', ')}`);
  if (missingEmployeeLabels.length) console.error(`Missing employee-page locale entries: ${missingEmployeeLabels.join(', ')}`);
  process.exitCode = 1;
} else {
  console.log(`Verified ${Object.keys(sandbox.localeRows).length} locale entries in Turkish, English, Russian, Ukrainian, and Macedonian.`);
}
