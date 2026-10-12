# Menü, kısayol ve modal denetimi

Tarih: 12 Ekim 2026  
Durum: Ön envanter tamamlandı; tüm menü/alt işlem ve kısayol yollarının uçtan uca denetimi sürüyor.

## Ölçüm kapsamı

- Yan menü: 10 açılır grup ve Genel Bakış/Destek dâhil 50 hedef. `tools/Verify-AppNavigation.mjs` menü hedeflerinin ve grupların kaynağa bağlı olduğunu doğruluyor; tek başına işlevlerin doğru çalıştığını kanıtlamıyor.
- Kapsam defteri: `docs/PRODUCT-COVERAGE.md` içinde 37 ürün ailesi izleniyor. 2 aile “Var” (2/37 = %5,4), 28 “Kısmi” (28/37 = %75,7), kalan 7 aile temel akış, temel yaşam döngüsü, kit kapsamı veya prototip düzeyinde. Bu oran ürün ailesi olgunluğudur; 50 menünün veya tüm kısayolların çalışma yüzdesi değildir.
- Yerel çalışma: Docker uygulama ve veritabanı sağlık kontrolü `ready`, bekleyen migration 0. Önceki oturum sonrası giriş ekranı yeniden geldi; giriş denemesinden sonra şirket oturumunun devam ettiği doğrulanamadı. Bu nedenle oturum gerektiren diğer sayfalara ait canlı tarayıcı kontrolleri tamamlanmış sayılmıyor.

## Menü hedefleri

Bu liste kaynakta bulunan 50 hedefi gösterir. “Canlı doğrulama” sütunu, her hedef oturumlu tarayıcıda açılıp alt işlemleri kontrol edilene kadar açık kalır.

| Grup | Hedefler | Canlı doğrulama |
|---|---|---|
| Genel | Genel Bakış | Kısmi: gerçek şirket verisi ve sayaçlar görüntülendi |
| Envanter | Varlıklar; Varlık satın alma siparişleri; Varlık modelleri; Markalar; Kategoriler; Durum etiketleri; QR ve barkodlar | Varlıklar kısmi; diğerleri bekliyor |
| Zimmet ve hareketler | Zimmetler; İadeler; Transferler; Rezervasyonlar; Hareket geçmişi | Bekliyor |
| Lisans ve yazılım | Yazılım lisansları; Lisans atamaları; Sözleşmeler; Garanti takibi | Bekliyor |
| Aksesuar ve stok | Aksesuarlar; Satın alma siparişleri; Sarf malzemeleri; Stok talepleri; Bileşenler; Hazır kitler; Stok hareketleri | Bekliyor |
| Kişiler ve ekipler | Çalışan kayıtları; Uygulama kullanıcıları; Departmanlar; Yetki rolleri | Bekliyor |
| Konumlar | Lokasyonlar; Binalar ve odalar; Şirket birimleri | Bekliyor |
| Bakım ve denetim | Bakım kayıtları; Bakım planları; Denetimler; Takvim; Görevler | Bekliyor |
| Raporlar | Hazır raporlar; Özel rapor oluştur; Dışa aktarımlar | Bekliyor |
| Veri ve yedekler | Tablodan içe aktar; Verileri dışa aktar; Yedekleme geçmişi; Geri yükle | Bekliyor |
| Şirket ayarları | Firma profili; Logo ve tema; Bildirimler; Entegrasyonlar; Güvenlik; Denetim kayıtları | Bekliyor |
| Genel | Destek | Bekliyor |

Kaynakta açıkça kısmi/henüz hazır değil olarak işaretlenen yüzeyler: merkezi şirket güvenlik yönetimi (Güvenlik), şube/tüzel kişilik bazında ayrım (Şirket birimleri), rol-yetki düzenleme ekranı (Yetki rolleri), görev atama ve tamamlama akışı (Görevler), harici bağlantı/webhook yönetimi (Entegrasyonlar). Bu tespitler kaynak metin ve kapsam defterine dayanır; UI davranış kabul testi değildir.

## Ekran görüntülerindeki varlık detay penceresi

Görüntülerde beş sekme var: Genel bakış, İlişkiler, Bakım kayıtları, Hareket geçmişi ve Fotoğraflar/dosyalar. Dosya alanı PNG/JPEG/WebP/PDF için en fazla 10 MB bildiriyor ve yüklemeyi isteğe bağlı bir hareket kaydına bağlayabiliyor. Alt işlem çubuğunda sekiz kontrol var: Bilgileri düzenle, Denetimi kaydet, Denetimi planla, Bakım başlat, Konum transferi, Çalışana zimmetle, Çöp kutusuna taşı ve Durumu değiştir.

Ekran görüntüleri kontrollerin mevcut olduğunu gösteriyor; kaydetme, sunucu yanıtı ve kayıt geçmişi sonucu gösterilmediğinden bunlar çalışıyor olarak puanlanmadı. Dosya seçme/yükleme, çöp kutusu ve durum değiştirme gibi veri değiştiren adımlar da denetim amaçlı canlı şirket kayıtlarında tetiklenmedi.

## Kısayolların doğrulama durumu

Ana ekran/varlık listesinde erişilebilir ağaçta arama, yeni varlık, barkod okuma, bildirim, profil/yardım/görünüm, liste filtreleri, kayıtlı arama, sütun seçimi, yazdırma/tam ekran, satır yoğunluğu ve sayfalama bulundu. Satır başına ayrıntı, zimmet/iade, denetim, etiket yazdırma, kopyalama, düzenleme ve arşive taşıma eylemleri de var. Bunların yalnızca görünmesi sonuçlarının doğrulandığı anlamına gelmez. Form açma, doğrulama mesajı, iptal, başarı/hata ve son verinin yeniden yüklenmesi sırasıyla kontrol edilmelidir.

## Yüzdeyi doğru hesaplama kuralı

Bir hedef/işlem ancak liste ve detay, ilgili alt işlemler, geçerli/geçersiz veri, iptal/hata yolu, yetki sınırı ve işlem sonrası kayıt/hareket doğrulaması tamamlanınca “tam çalışıyor” sayılacak. Her işlem için sonuç kaydedilecek: Tam, Kısmi, Çalışmıyor, Riskli/karar gerekli. Son raporda iki ayrı oran verilecek: 50 menü hedefinin tam çalışan oranı ve bulunan tüm kısayol/alt seçeneklerin tam çalışan oranı. Şu an ikincisi için doğrulanmış payda ve iki oran için de tamamlanmış canlı sonuç kümesi yok; sahte bir yüzde üretmek yerine açık bırakıldı.

## Dört doğrulama kontrolü

1. Navigasyon: 10 grup/50 hedef kaynak kontrolünden geçti; canlı sayfa ziyaretleri tamamlanmadı.
2. Veri ve servis: yerel `/api/status` ready, migration beklemiyor; oturumlu menü denetimi için oturum devamlılığı doğrulanmalı.
3. Kısayol/modal: görüntüdeki varlık modalı envanteri çıkarıldı; gönderim ve kayıt sonrası sonuçlar henüz kabul edilmedi.
4. Kapsam/oran: 37 ürün ailesi için mevcut defter sayıldı; %5,4 oranı yalnızca “Var” durumundaki aileler içindir, menü çalışma oranı diye sunulamaz.
