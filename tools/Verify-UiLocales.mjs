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
  'Teslim hedefi bulunamadı', 'Employee', 'Asset',
  'ŞİRKET ALANI / VARLIK TANIMLARI', 'Varlık tanımları', 'Şirketinizde tekrar kullanılacak kategori, marka ve modelleri yönetin.',
  'Yeni kayıt ekle', 'Varlık yöneticisi', 'Marka seçin (isteğe bağlı)', '＋ Yeni marka ekle', 'Kategori seçin',
  'Marka web sitesi', 'Teknik destek bağlantısı', 'Garanti sorgulama bağlantısı', 'Destek telefonu', 'Destek e-postası',
  'Marka logosu (PNG/JPEG/WebP, en fazla 2 MiB)', 'Model görseli (PNG/JPEG/WebP, en fazla 2 MiB)',
  'Amortisman ömrü (ay)', 'Taban değer', 'Taban değer türü', 'Amortisman yöntemi', 'Kayıt ara',
  'Marka / Kategori / Görsel', 'Üretici bilgileri / Logo', 'Model görseli', 'Görseli yükle',
  'Üretici bilgilerini kaydet', 'Logoyu yükle', 'Tanım eklendi. Varlık formunda kullanılabilir.',
  'Yeni marka adı:', 'Sil', 'Tanım listesi yenilendi.', 'Marka bilgileri güncellendi.', 'Model amortisman politikası kaydedildi.',
  '“{name}” tanımı silinsin mi?', 'Bir tanımı kullanan varlık varsa yanlışlıkla veri kaybını önlemek için bu tanım silinemez. Önce ilişkili varlıkları düzenleyin.'
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
const requiredInventoryMovementLabels = [
  'ŞİRKET ALANI / STOK', 'Stok hareketleri',
  'Aksesuar, sarf malzemesi ve bileşen girişlerini, çıkışlarını ve atamalarını birlikte inceleyin.',
  'CSV indir', 'Ürün, çalışan, varlık veya not ara', 'Stok hareketlerinde ara',
  'Tüm stok türleri', 'Aksesuar', 'Sarf', 'Bileşen', 'Stok türüne göre filtrele',
  'Hareket türüne göre filtrele', 'Tüm hareketler', 'Stok girişi', 'Sayım düzeltmesi',
  'Çıkış / atama', 'İade / çıkarma', 'Başlangıç (UTC)', 'Bitiş (UTC)',
  'Başlangıç tarihi', 'Bitiş tarihi', 'Tarih', 'Tür', 'Ürün', 'Hareket', 'Miktar',
  'Hedef / çalışan', 'İşlemi yapan', 'Not', 'Şirket stok hareketleri',
  'Hareketler yükleniyor…', 'Bu filtrelerle eşleşen hareket yok.',
  'Hareket sayısı: {count}', 'Sayfa', '← Önceki', 'Sonraki →', 'Silinmiş kayıt',
  'Stok çıkışı / atama', 'Çalışana teslim', 'İade alındı'
];
const requiredLicenseListLabels = [
  'ŞİRKET ALANI / YAZILIM', 'Yazılım lisansları',
  'Lisans türünü, koltuk kullanımını, atamaları ve yenileme tarihlerini yönetin.',
  '↓ CSV dışa aktar', '↑ CSV içe aktar', '＋ Lisans ekle',
  'Lisans, yayıncı, tedarikçi veya referans ara', 'Lisans', 'Yayıncı / sürüm', 'Tür',
  'Kullanılan koltuk', 'Bitiş tarihi', 'Yenileme', 'İşlem', 'Lisanslar yükleniyor…',
  'Referans yok', 'Sürüm belirtilmemiş', 'Süresiz', 'Otomatik', 'Manuel', 'Ayrıntı',
  'Abonelik', 'Cihaz başına', 'Şirket geneli', 'Deneme', 'Diğer', 'Lisans sayısı: {count}',
  'Sayfa', '← Önceki', 'Sonraki →', 'Lisans bulunamadı.',
  'Lisans atamaları', 'Çalışana verilmiş koltukları ve geri alınan eski atamaları inceleyin.',
  'Lisans kataloğu', 'Çalışan veya lisans ara', 'Aktif atamalar', 'Geri alınan atamalar',
  'Tüm atamalar', 'Atamalar yükleniyor…', 'Çalışan / varlık', 'Personel no',
  'Atanma tarihi', 'Durum', 'Lisans atama sayısı: {count}', 'Aktif', 'Geri alındı',
  'Atamayı kaldır', 'Henüz atama yok.',
  'Bu lisans atamasını geri almak istiyor musunuz?',
  'Atama kaldırıldı; geçmiş kaydı korundu.',
  'YAZILIM LİSANSI', 'Lisans ayrıntısı', 'Kapat', 'Lisans dosyaları ve EULA sürümleri',
  'PDF veya resim ekle', 'Bu dosya lisans koşullarını içeren EULA belgesidir',
  'EULA sürümü', 'Örn. 2026-10 veya v2.1', 'Dosya yükle',
  'Dosya başına en fazla 10 MB, lisans başına 30 dosya. Yeni EULA sürümü eskisini geçmişte korur ve ileriye dönük kabul için güncel olur.',
  'Atamalar', 'Atanma', 'EULA teyit zamanı', 'Teyit eden', 'Lisans geçmişi',
  'Yeni lisans', 'Lisans adı *', 'Yayıncı', 'Ürün sürümü', 'Lisans türü',
  'Lisans referansı', 'Tedarikçi', 'Koltuk sayısı', 'Birim maliyet', 'Para birimi',
  'Alış tarihi', 'Otomatik yenileniyor',
  'Atama sırasında kullanıcı EULA kabulünü zorunlu tut', 'Notlar',
  'Ürün anahtarı bu formda saklanmaz. Referans alanına parola veya tam anahtar yazmayın.',
  'Vazgeç', 'Kaydet', 'Lisansı düzenle', 'Düzenle', 'Koltuk ata',
  'KOLTUK ATAMASI', 'Lisans koltuğu ata', 'Atamada kabul edilecek güncel EULA',
  'Açıp kullanıcıya gösterin. Kabul kaydı bu dosyanın değiştirilemeyen sürümüne bağlanır.',
  'Bu kullanıcı(lar)ın yukarıdaki EULA sürümünü okuyup kabul ettiğini teyit ediyorum.',
  'Atama hedefi', 'Çalışan', 'Cihaz / varlık', 'Aktif çalışan *',
  'Birden çok çalışana toplu ata',
  'Ctrl veya Shift ile en fazla 100 çalışan seçin. Atama işlemi ya tüm çalışanlar için kaydedilir ya da hiçbiri için kaydedilmez.',
  'Cihaz ara', 'Etiket, ad veya seri no', 'Varlık *', 'Önce arama yapın', 'Not',
  'Atamayı kaydet', '{name} · kalan {count} koltuk', 'Güncel EULA belgesi eklenmedi',
  'Referans', 'Koltuk kullanımı', 'EULA kabulü', 'Gerekli değil',
  'Bu lisansa henüz dosya eklenmemiş.', 'Yükleniyor…', 'Güncel sürüm', 'Sil',
  'Çalışan seçin', 'Varlık {tag}', 'Otomatik yenileme', 'Birim maliyeti',
  'Güncel sürüm kabulü zorunlu', 'Lisans bilgileri güncellendi.', 'Lisans kaydı oluşturuldu.',
  'Lisans dosyası eklendi.', 'Bu lisans dosyası silinsin mi?', 'Lisans dosyası silindi.',
  'Dosya 10 MB sınırını aşıyor.',
  'Bu çalışanın lisans atamasını kaldırmak istiyor musunuz? Geçmiş kaydı korunur.',
  'Lisans ataması kaldırıldı; geçmiş kaydı korundu.',
  'Lisans koltuğu çalışanlara atandı.', 'Lisans koltuğu çalışana atandı.',
  'Lisans koltuğu cihaza atandı.'
];
const requiredLicenseImportLabels = [
  'YAZILIM LİSANSLARI / DOSYA AKTARIMI', 'Lisans kayıtlarını içe aktar',
  'Şablon alanları: Lisans, Yayıncı, Ürün sürümü, Tür, Tedarikçi, Koltuk, Birim maliyeti, Para birimi, Alış tarihi, Bitiş tarihi, Otomatik yenileme, Notlar. Tür: Subscription, Perpetual, Device, Site, Trial veya Other. UTF-8 CSV, yyyy-AA-gg tarih; en fazla 5.000 satır ve 16 MB.',
  'Lisans anahtarı ve referansı aktarılmaz. Yalnızca yeni lisanslar eklenir; mevcut atamalar değişmez. Önizleme kayıt oluşturmaz.',
  'Örn. Tedarikçi A', 'Lisans eşleme profili', 'Lisans eşleme profili kaydedildi.',
  'Seçili lisans eşleme profili silinsin mi?', 'Lisans eşleme profili silindi.',
  'Bilinmeyen sütunları lisans alanlarına bağlayın veya “Bu sütunu yoksay” seçin. Lisans adı, türü ve koltuk sayısı zorunludur.',
  'Lisans sütununu eşle', 'Bir hedef alan seçin', 'Dosya sütunu:', 'bayt',
  'Her sütun için bir hedef seçin veya yoksaymayı seçin.',
  'Önce sütunları eşleyin, ardından profil adı girin.',
  'Doğrulanmış yeni lisans kayıtları eklensin mi? Mevcut atamalar ve lisans referansları değişmez.',
  'Dosya doğrulanıyor; hiçbir kayıt eklenmiyor…', 'Lisans kayıtları ekleniyor…',
  'Dosya 16 MB sınırını aşıyor.', 'Dosya değişti. Önce yeniden doğrulayın.',
  'CSV tırnak veya ayraç biçimi bozuk ya da satır sınırı aşıldı.',
  'CSV UTF-8 biçiminde olmalıdır.', 'Excel dosyası okunamadı. Geçerli ve tek çalışma sayfalı bir XLSX dosyası seçip yeniden deneyin.',
  'Sütun eşlemesi okunamadı; dosyayı yeniden doğrulayın.', 'Bir eşleme profili 1–20 sütun eşlemesi içermelidir.',
  'Kaynak sütun adları boş, çok uzun veya yinelenen olamaz.', '“{source}” sütununun hedef alanı geçersiz.',
  'Aynı lisans alanına birden fazla sütun eşlenemez.', 'Şablon başlığı ve en fazla {count} lisans satırı kullanın.',
  '{row}. satırda {count} sütun olmalıdır.', '{row}. satırdaki lisans adı boş veya 200 karakterden uzun.',
  '{row}. satırdaki tür geçersiz; Subscription, Perpetual, Device, Site, Trial veya Other kullanın.',
  '{row}. satırdaki koltuk sayısı 1–100.000 arasında tam sayı olmalıdır.', '{row}. satırdaki maliyet geçersiz.',
  '{row}. satırdaki para birimi üç harfli olmalıdır.', '{row}. satırdaki bitiş tarihi alış tarihinden önce olamaz.',
  '{row}. satırdaki otomatik yenileme evet/hayır olmalıdır.', '{row}. satırdaki metin alanlarından biri uzunluk sınırını aşıyor.',
  '{row}. satırdaki lisans adı/yayıncı/sürüm bileşimi dosyada yineleniyor.',
  '{row}. satırdaki {dateLabel} {format} biçiminde olmalıdır.', 'alış tarihi', 'bitiş tarihi', 'yyyy-AA-gg',
  'CSV\'de düzeltilmesi gereken alanlar bulundu. Hiçbir lisans eklenmedi.',
  'Sütun eşlemesi sınırı aşıyor.',
  'Önizlemeden sonra aynı lisans eklendi. Hiçbir kayıt aktarılmadı; önizlemeyi yenileyin.',
  'CSV aktarılmadı; bir kayıt çakıştı. Önizlemeyi yenileyip tekrar deneyin.',
  'Dosyadaki sütunları eşleyin veya açıkça yoksayın: {columns}. Veri kaybını önlemek için aktarım durduruldu.',
  '{count} yeni lisans doğrulandı; {supplierCount} yeni tedarikçi yalnızca aktarımı onaylarsanız kataloğa eklenecek. Önizleme kayıt eklemedi.',
  '{count} lisans ve denetim geçmişi eklendi; {supplierCount} yeni tedarikçi kataloğa bağlandı.'
];
const requiredApiErrorLabels = [
  'İstek doğrulaması başarısız.',
  'Arama metni 120 karakteri aşamaz.',
  'Arama 120 karakteri aşamaz.',
  'Başlangıç tarihi bitiş tarihinden sonra olamaz.',
  'Profil adı 1–60 karakter olmalıdır.',
  'Aynı adlı profil eşzamanlı olarak kaydedildi. Listeyi yenileyip tekrar deneyin.',
  'Yalnızca gerçek PDF, PNG, JPEG veya WebP dosyaları yüklenebilir.',
  '1 ile 100 arasında, tekrarsız varlık seçin.',
  'Sütun eşlemesi okunamadı; dosyayı yeniden doğrulayın.',
  'Rapor tarihi 2000 ile 2100 arasında olmalıdır.',
  'Etkin çalışan bu şirkette bulunamadı.',
  'Sipariş eki 1 bayt ile 10 MiB arasında olmalıdır.',
  'Bir siparişe en fazla 30 dosya eklenebilir.',
  'Bu sipariş artık teslim alınamaz.',
  'Tamamlanmış veya iptal edilmiş sipariş değiştirilemez.',
  'Konum filtresi 160 karakteri aşamaz.',
  'Sıralama alanı veya yönü desteklenmiyor.',
  'Not en fazla 2.000 karakter olabilir.',
  'Bu ad katalog konumuna bağlı stok kayıtlarında kullanılıyor. Katalog düğümünü düzenleyerek kimlik bağlantısını koruyun.',
  'Etiket (en fazla 80 karakter), ad ve kategori zorunludur.',
  'Kategori bu şirkette bulunamadı.',
  'Marka bu şirkette bulunamadı.',
  'Departman ağacı en fazla altı seviyeye kadar açılabilir.',
  'Açıklama en fazla 1900 karakter olabilir.',
  'Seçilen aksesuarlardan biri bu şirkette bulunamadı.',
  'Yalnızca gerçek PNG, JPEG veya WebP görselleri yüklenebilir.',
  'Dışa aktarma en fazla {exportLimit:N0} sipariş satırını destekler. Sonuçları arama veya durum filtresiyle daraltın.',
  'Bu e-posta mevcut bir hesaba ait; hesabı bağlamak için mevcut parolasını girin.',
  'İki aşamalı doğrulama zaten etkin.',
  'Profil fotoğrafı 1 bayt ile 2 MB arasında olmalıdır.',
  'Yalnızca doğrulanmış PNG, JPEG veya WebP fotoğraf yüklenebilir.',
  'Bu şirket içinde özel alan anahtarı zaten kullanılıyor.',
  'Garanti filtresi expired, expiring, active veya unspecified olmalıdır.',
  'Adet 1 ile 100.000 arasında olmalı; not en fazla 2.000 karakter olabilir.',
  'Konum adı 1–120 karakter olmalı; eğik çizgi ve kontrol karakteri içeremez.',
  'Aynı üst konum altında bu ad zaten kayıtlı.',
  'Varlık başka bir kullanıcı tarafından güncellendi. Yenileyip tekrar deneyin.',
  'Bitiş tarihi bugünden başlayıp en fazla 366 gün sonrasını kapsamalıdır.',
  'Seri numarası ve konum en fazla 160 karakter olabilir.',
  'Para birimi üç harfli kod olmalıdır.',
  'Model seçilen marka ve şirketle eşleşmiyor.',
  'Kayıt çakıştı. Etiket ve ilişkili kayıtları kontrol edin.',
  'Sonraki denetim tarihi bugün ile 10 yıl sonrası arasında olmalıdır.',
  'Ad zorunludur ve en fazla 120 karakter olabilir.',
  'Sipariş durumu geçersiz.',
  'Bu sipariş numarası şirkette zaten kullanılıyor.',
  'Teslim tarihi sipariş tarihinden önce olamaz.',
  'Rezervasyon durumu geçerli değil.',
  'Bu şirketteki etkin bir çalışan seçilmelidir.',
  'Dışa aktarım en fazla 10.000 satır içerir; filtreyi daraltın.',
  'Aktif çalışan seçilmelidir.'
];

const invalidRows = Object.entries(sandbox.localeRows)
  .filter(([key, values]) => !key.trim() || !Array.isArray(values) || values.length !== 4 || values.some(value => typeof value !== 'string' || !value.trim()))
  .map(([key]) => key || '<empty key>');
const missingAssetLabels = requiredAssetLabels.filter(key => !sandbox.localeRows[key]);
const missingReservationLabels = requiredReservationLabels.filter(key => !sandbox.localeRows[key]);
const missingAccessoryLabels = requiredAccessoryLabels.filter(key => !sandbox.localeRows[key]);
const missingEmployeeLabels = requiredEmployeeLabels.filter(key => !sandbox.localeRows[key]);
const missingInventoryMovementLabels = requiredInventoryMovementLabels.filter(key => !sandbox.localeRows[key]);
const missingLicenseListLabels = requiredLicenseListLabels.filter(key => !sandbox.localeRows[key]);
const missingLicenseImportLabels = requiredLicenseImportLabels.filter(key => !sandbox.localeRows[key]);
const missingApiErrorLabels = requiredApiErrorLabels.filter(key => !sandbox.localeRows[key]);

if (invalidRows.length || missingAssetLabels.length || missingReservationLabels.length || missingAccessoryLabels.length || missingEmployeeLabels.length || missingInventoryMovementLabels.length || missingLicenseListLabels.length || missingLicenseImportLabels.length || missingApiErrorLabels.length) {
  if (invalidRows.length) console.error(`Invalid 4-language locale entries: ${invalidRows.join(', ')}`);
  if (missingAssetLabels.length) console.error(`Missing asset-list locale entries: ${missingAssetLabels.join(', ')}`);
  if (missingReservationLabels.length) console.error(`Missing reservation-page locale entries: ${missingReservationLabels.join(', ')}`);
  if (missingAccessoryLabels.length) console.error(`Missing accessory-page locale entries: ${missingAccessoryLabels.join(', ')}`);
  if (missingEmployeeLabels.length) console.error(`Missing employee-page locale entries: ${missingEmployeeLabels.join(', ')}`);
  if (missingInventoryMovementLabels.length) console.error(`Missing inventory-movement locale entries: ${missingInventoryMovementLabels.join(', ')}`);
  if (missingLicenseListLabels.length) console.error(`Missing license-list locale entries: ${missingLicenseListLabels.join(', ')}`);
  if (missingLicenseImportLabels.length) console.error(`Missing license-import locale entries: ${missingLicenseImportLabels.join(', ')}`);
  if (missingApiErrorLabels.length) console.error(`Missing common API-error locale entries: ${missingApiErrorLabels.join(', ')}`);
  process.exitCode = 1;
} else {
  console.log(`Verified ${Object.keys(sandbox.localeRows).length} locale entries in Turkish, English, Russian, Ukrainian, and Macedonian.`);
}
