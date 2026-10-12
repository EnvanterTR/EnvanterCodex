# Menü, kısayol ve modal denetimi

Tarih: 12 Ekim 2026  
Durum: Ön envanter tamamlandı; tüm menü/alt işlem ve kısayol yollarının uçtan uca denetimi sürüyor. 12 Ekim'de Şirket birimleri modülü uçtan uca API kabulünden geçti; kalan hedefler henüz tarayıcıda tek tek doğrulanmadı.

## Ölçüm kapsamı

- Yan menü: 10 açılır grup ve Genel Bakış/Destek dâhil 50 hedef. `tools/Verify-AppNavigation.mjs` menü hedeflerinin ve grupların kaynağa bağlı olduğunu doğruluyor; tek başına işlevlerin doğru çalıştığını kanıtlamıyor.
- Kapsam defteri: `docs/PRODUCT-COVERAGE.md` içinde 37 ürün ailesi izleniyor. Şirket birimleri hiyerarşik CRUD, arşiv ve geri yükleme ile temel API kabulinden geçti. Bu oran ürün ailesi olgunluğudur; 50 menünün veya tüm kısayolların çalışma yüzdesi değildir.
- Kabul kanıtı: 12 Ekim güncel kaynakta tam izole PostgreSQL güvenlik paketi 93/93 geçti; menü kaynağı denetimi 50 hedef/10 grup, locale denetimi 1.689 kayıt geçti. Bunlar oturumlu tarayıcıda her menü ve modal eyleminin görsel/işlevsel kabulü yerine geçmez.
- 12 Ekim güncel kaynak: varlık detay modalının durum değiştir, tekil düzenle, ek dosya yükle/indir/sil ve bakım başlat/tamamla API akışları izole PostgreSQL kabulinde geçti ve aynı 93/93 tam pakete dahildi. CSRF, salt-okunur rol, yabancı şirket kaydı, doğrulama/çatışma yolları, kalıcı veri, geçmiş ve denetim izi kontrol edildi.
- Yerel çalışma: Docker uygulama ve veritabanı sağlık kontrolü `ready`, bekleyen migration 0. 12 Ekim'de sağlanan şirket hesabı loopback API üzerinden kabul edildi ve yalnızca bu teşhis için açılan oturum kapatıldı. Hesap durumu kilitli değil, e-posta doğrulanmış, MFA kapalı ve tek şirket üyeliği mevcut. Açık tarayıcıya oturum aktarılmadığından rol bazlı görsel kabul tamamlanmış sayılmıyor.
- Canlı demo: `http://localhost:8081/demo/` tarayıcı erişilebilirlik ağacında 50 örnek varlık, 19 farklı ortak tablo başlığı ve satır eylem kontrolleri bulundu; güncel HTTP yanıtı 200 ve tam genişlik kuralı mevcut. `node tools/Verify-PublicDemoLayout.mjs` artık 19 tekrar etmeyen sütun başlığını, masaüstünde sağda sabit kalan Eylemler sütununu, tam genişlik, içerik sınırında yatay kaydırma, mobil üst çubuk ve HTML önbellek yenileme kurallarını doğruluyor. Bu kaynak/DOM kontrolü eylemlerin sonuç davranışını kabul etmez.

## Menü hedefleri

Bu liste kaynakta bulunan 50 hedefi gösterir. “Canlı doğrulama” sütunu, her hedef oturumlu tarayıcıda açılıp alt işlemleri kontrol edilene kadar açık kalır.

| Grup | Hedefler | Canlı doğrulama |
|---|---|---|
| Genel | Genel Bakış | Kısmi: gerçek şirket verisi ve sayaçlar görüntülendi |
| Envanter | Varlıklar; Varlık satın alma siparişleri; Varlık modelleri; Markalar; Kategoriler; Durum etiketleri; QR ve barkodlar | Varlıklar kısmi; diğerleri bekliyor |
| Zimmet ve hareketler | Zimmetler; İadeler; Transferler; Rezervasyonlar; Hareket geçmişi | Bekliyor |
| Lisans ve yazılım | Yazılım lisansları; Lisans atamaları; Sözleşmeler; Garanti takibi | Bekliyor |
| Aksesuar ve stok | Aksesuarlar; Satın alma siparişleri; Sarf malzemeleri; Stok talepleri; Bileşenler; Hazır kitler; Stok hareketleri | Bekliyor |
| Kişiler ve ekipler | Çalışan kayıtları; Uygulama kullanıcıları; Departmanlar; Yetki rolleri | Çalışanlar/departmanlar bekliyor; Yetki rolleri kısmi: yerleşik rol matrisi görüntüleniyor, yetkili yöneticilerden kullanıcı rol atamalarına doğrudan geçiş var; tarayıcıda eylem kabulü bekliyor |
| Konumlar | Lokasyonlar; Binalar ve odalar; Şirket birimleri | Lokasyonlar ve binalar bekliyor; şirket birimi hiyerarşi CRUD/tenant-yetki API kabulü geçti; tarayıcı görsel kabulü bekliyor |
| Bakım ve denetim | Bakım kayıtları; Bakım planları; Denetimler; Takvim; Görevler | Görevler kaynak ve API bağlantısı mevcut; tarayıcıda rol bazlı sonuç kabulü bekliyor. Diğerleri bekliyor |
| Raporlar | Hazır raporlar; Özel rapor oluştur; Dışa aktarımlar | Bekliyor |
| Veri ve yedekler | Tablodan içe aktar; Verileri dışa aktar; Yedekleme geçmişi; Geri yükle | Bekliyor |
| Şirket ayarları | Firma profili; Logo ve tema; Bildirimler; Entegrasyonlar; Güvenlik; Denetim kayıtları | Bekliyor |
| Genel | Destek | Bekliyor |

Kaynakta açıkça kısmi/henüz hazır değil olarak işaretlenen yüzeyler: merkezi şirket güvenlik yönetimi (Güvenlik), şube/tüzel kişilik bazında varlık ayrımı (Şirket birimleri kapsamının dışında), özel rol ve izin düzenleme (Yetki rolleri), harici bağlantı/webhook yönetimi (Entegrasyonlar). Yetki rolleri sayfası yerleşik rollerin erişim matrisini gösterir; yetkili şirket yöneticileri buradan Uygulama kullanıcıları ekranına geçerek üyeye rol atayabilir. Yeni rol veya özel izin tanımı yapılamaz. Şirket birimleri artık şirket içi hiyerarşik katalog CRUD akışıdır; tüzel kişilik veya varlık erişim sınırı oluşturmaz. Görevler için şirket kapsamlı liste, arama/filtre/sayfalama, oluşturma/düzenleme, çalışan atama, durum geçişleri ve denetim izi kapsam defterinde mevcut; kaynak/API bağlantısı kontrol edildi ancak rol bazlı tarayıcı sonucu bekliyor. Bu tespitler kaynak metin ve kapsam defterine dayanır; UI davranış kabul testi değildir.

## Ekran görüntülerindeki varlık detay penceresi

Görüntülerde beş sekme var: Genel bakış, İlişkiler, Bakım kayıtları, Hareket geçmişi ve Fotoğraflar/dosyalar. Dosya alanı PNG/JPEG/WebP/PDF için en fazla 10 MB bildiriyor ve yüklemeyi isteğe bağlı bir hareket kaydına bağlayabiliyor. Alt işlem çubuğunda sekiz kontrol var: Bilgileri düzenle, Denetimi kaydet, Denetimi planla, Bakım başlat, Konum transferi, Çalışana zimmetle, Çöp kutusuna taşı ve Durumu değiştir.

Ekran görüntüleri kontrollerin mevcut olduğunu gösteriyor; kaydetme, sunucu yanıtı ve kayıt geçmişi sonucu gösterilmediğinden bunlar çalışıyor olarak puanlanmadı. Dosya seçme/yükleme, çöp kutusu ve durum değiştirme gibi veri değiştiren adımlar da denetim amaçlı canlı şirket kayıtlarında tetiklenmedi.

12 Ekim 2026: Gönderilen koyu tema görüntüsündeki beyaz sekme/alt işlem çubukları, dosya seçici ve boş dosya alanı kaynak CSS'inde doğrulandı. Bunlara koyu tema renkleri, dosya kartı/boş durum renkleri ve küçük ekran için dikey form düzeni eklendi; `Verify-AppNavigation.mjs` bu seçicileri ve sarılmayı denetliyor. Release Docker web servisi yeniden oluşturuldu; API `ready`, bekleyen migration 0, demo `200`. Mevcut tarayıcı oturumu giriş ekranında olduğundan bu modal bu turda oturumlu tarayıcıda görüntülenmedi. Sekme gezinmesi, sekiz ana eylem ve dosya ekle/indir/sil sonuçları için canlı kabul hâlâ açık; bu düzeltme çalışma yüzdesine tamamlanmış eylem olarak eklenmedi.

12 Ekim 2026: Varlık detayındaki “Denetimi kaydet” ve “Denetim planla” akışları izole PostgreSQL HTTP kabulinde doğrulandı: CSRF ve yazma rolü zorunlu, yabancı şirket varlığı 404, geçersiz tarihler/not sınırı reddediliyor; başarılı denetim/plan ve planı kaldırma varlık geçmişi ile denetim kaydına yansıyor. İki yeni senaryoyla tam kabul paketi 88/88 geçti. Ekran görüntüsünde parolanın URL sorgusunda görünmesi üzerine hassas sorgu anahtarlarını servisten önce temizleyen 303 yönlendirmesi eklendi; güncel kaynaklı 3 odaklı test geçti. Yerel `localhost:8080` uygulaması yeni web imajıyla yenilendi: `/api/status` `ready`, ana sayfa `200`, giriş formu POST, “Sunucu bağlantısını test et” mevcut, demo `8081/demo/` `200`. Sahte test değerleriyle URL isteği `/?view=assets` konumuna `no-store`/`no-referrer` başlıklarıyla yönlendirildi. Bu iş canlı tarayıcı oturum açma veya tüm menülerin uçtan uca kabulü değildir; önceki adres geçmişi/screenshot içindeki açığa çıkmış parola ayrıca kullanıcı tarafından değiştirilmelidir.

12 Ekim 2026: Varlık modalı için dört kapsamlı izole PostgreSQL HTTP kabul testi eklendi. Tekil düzenlemede geçersiz kategori, eski ExpectedUpdatedAt, tekrar oynatma, salt-okunur/yabancı tenant/CSRF ve başarılı geçmiş kaydı; ek dosyada bozuk PDF, yabancı tenant, salt-okunur rol, CSRF, yükleme/indirme/silme ve denetim izi; durum değiştirmede geçersiz değer, not sınırı, aynı durum ve geçmiş; bakımda gelecek tarih, yinelenen açık kayıt, erken/tekrarlı tamamlama, rol/CSRF/tenant sınırı ve başlangıç/tamamlanma geçmişi kontrol edildi. Güncel tam paket 93/93 geçti ve bu dört test pakete dahildir. Bu API kanıtı arayüz düğmelerinin DOM olay bağını ve sonuç mesajını kabul etmez.

## Kısayolların doğrulama durumu

Ana ekran/varlık listesinde erişilebilir ağaçta arama, yeni varlık, barkod okuma, bildirim, profil/yardım/görünüm, liste filtreleri, kayıtlı arama, sütun seçimi, yazdırma/tam ekran, satır yoğunluğu ve sayfalama bulundu. Satır başına ayrıntı, zimmet/iade, denetim, etiket yazdırma, kopyalama, düzenleme ve arşive taşıma eylemleri de var. Bunların yalnızca görünmesi sonuçlarının doğrulandığı anlamına gelmez. Form açma, doğrulama mesajı, iptal, başarı/hata ve son verinin yeniden yüklenmesi sırasıyla kontrol edilmelidir.

## Yüzdeyi doğru hesaplama kuralı

Bir hedef/işlem ancak liste ve detay, ilgili alt işlemler, geçerli/geçersiz veri, iptal/hata yolu, yetki sınırı ve işlem sonrası kayıt/hareket doğrulaması tamamlanınca “tam çalışıyor” sayılacak. Her işlem için sonuç kaydedilecek: Tam, Kısmi, Çalışmıyor, Riskli/karar gerekli. Son raporda iki ayrı oran verilecek: 50 menü hedefinin tam çalışan oranı ve bulunan tüm kısayol/alt seçeneklerin tam çalışan oranı. Şu an ikincisi için doğrulanmış payda ve iki oran için de tamamlanmış canlı sonuç kümesi yok; sahte bir yüzde üretmek yerine açık bırakıldı.

## Dört doğrulama kontrolü

1. Navigasyon: 10 grup/50 hedef kaynak kontrolünden geçti; canlı şirket menüsü ziyaretleri tamamlanmadı. Yerel demo 50 varlık/19 sütun DOM'u gösterdi; düzen doğrulayıcı geçti.
2. Veri ve servis: önceki release kontrolünde yerel /api/status ready, migration beklemiyor; geçerli şirket hesabıyla yerel API girişi başarılı ve teşhis oturumu kapalı. Bu devam turunda tam izole PostgreSQL paketi 93/93 geçti. Tarayıcıda açık oturum ve rol bazlı görsel kabul hâlâ yapılmadı.
3. Kısayol/modal: varlık modalı durum, tekil düzenleme, ek dosya ve bakım eylemlerinin API kabulü tam pakette 4/4; denetim eylemleri de pakette geçti. UI etkileşim ve başarı/hata mesajı kabulü bekliyor.
4. Kapsam/oran: 37 ürün ailesi için mevcut defter sayıldı; %5,4 oranı yalnızca “Var” durumundaki aileler içindir, menü çalışma oranı diye sunulamaz.
