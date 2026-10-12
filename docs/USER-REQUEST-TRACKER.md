# EnvanterTR v1 — Kullanıcı İstekleri ve İş Sırası

Güncelleme: 12 Ekim 2026

## Kapsam ve kaynak

Bu dosya, bu Codex görevi içindeki erişilebilir kullanıcı isteklerinin ve 11 Ekim tarihli ayrıntılı listenin proje iş planına dönüştürülmüş indeksidir. Birebir konuşma dökümü değildir. Ayrıntılı başlık ve durum listesi `docs/USER-REQUEST-CHECKLIST-2026-10-11.md` dosyasında özgün haliyle korunmuştur; bu listedeki ✅/🟡/❌ işaretleri kullanıcının 11 Ekim tarihli başlangıç beyanıdır, bugünkü kodun yeniden doğrulanmış sonucu değildir. Parola ve diğer kimlik bilgileri bu belgelere alınmaz.

## Kullanıcının kalıcı çalışma beklentileri

- EnvanterTR v1 tamamlanana kadar geliştirmeye devam et; süre tahmini veya tamamlandı iddiası yerine çalışan kaynak ve kanıt sun.
- Günlerdir verilen istekleri ve ekli ekran görüntülerini dikkate al. Giriş, Docker/localhost, GitHub, demo/canlı ayrımı, menü ve arka plan sorunları aynı ürün bağlamındadır.
- Geçici makyaj yerine gerçek, kalıcı, şirket/rol kapsamını koruyan iş akışları geliştir.
- Büyük iddia veya teslimattan önce isteği ve sonucu dört ayrı açıdan kontrol et: (1) kullanıcının istediği davranış, (2) kaynak/UI bağları, (3) sunucu-veri-güvenlik sonucu, (4) dağıtılan yerel/canlı hedef ve dokümantasyon.
- İlerleme raporunda ölçüm paydasını ve sınırlarını açıkça yaz. Rota açılmasını işlev başarısı sayma; kanıtsız yüzde üretme.

## İsteklerin işlem sırası ve kanıt kapıları

| Sıra | İş paketi | İstenen sonuç | Şu anki kanıt / açık kapı |
|---|---|---|---|
| 1 | Güvenli yerel giriş ve kalıcılık | localhost:8080 login doğru çalışsın; Docker kaldırma/yenileme veriyi kaybettirmesin; servis, API ve GitHub durumu anlaşılır olsun | Identity cookie yenileme claim kaybı düzeltildi; izole PostgreSQL kabul paketi 93/93 geçti. Yeni regresyon denetimi security-stamp yenilemesi sonrası şirket kimliği/rol ve şirket varlık listesini doğruluyor. Gerçek localhost sekmesi >12 dk açık kaldı; varlık modalı açıkken İlişkiler sekmesine geçiş ve API verisi başarılı. Zorla süresi dolmuş cookie ile 401 sırasında modal davranışı ayrıca açık. API `ready`, migration 84/84. Docker yığınını `--volumes` ile silme. |
| 2 | V1 gereksinim eşlemesi | Yaklaşık 280 maddeyi kaynak/kanıt ve eksiklerle eşleştir | Başlangıç listesi eklendi; statüler eski. `PRODUCT-COVERAGE.md` 37 ürün ailesi, `ROADMAP.md` açık ürün/kurulum işleri içeriyor. Her satır kod veya kabul kanıtıyla güncellenmeli. |
| 3 | Yan menü hedefleri ve işlevleri | Her menü, alt menü, form ve eylem kendi doğru şirkete/role bağlı gerçek işlemi yapsın | `Verify-AppNavigation.mjs`: 50 hedef/10 grup bağlama denetimi. 12 Ekim UI turunda 50/50 rota erişimi. Bildirim/Güvenlik kısmi; özel izinler, harici webhook/entegrasyon ve bazı işlevler eksik. Rota oranı işlev yüzdesi değildir. |
| 4 | Varlık listesi ve ekran görüntüsü düzeni | Gereksiz boşlukları azalt, sütunları/eylemleri yatay kaydırmada kullanılabilir tut; modal sekmeleri ve hızlı işlemler doğru sonuç versin | Demo kaynağında 19 ortak sütun, eylem sütunu sabitleme ve tam genişlik kuralları doğrulandı; localhost demo 50 örnek kayıt. Modal API işlemlerinde 93/93 genel kabul; bu turda 7 kısayol formu yalnızca açılıp iptal edildi (gönderim 0/7). “Çöp kutusuna taşı” kaynak/API bağlantısı ve izole API kabuline sahip; tarayıcı işlem sonucu henüz doğrulanmadı. Bu turdaki filtre/arama/sayfalama sonuçları UI'da doğrulandı. |
| 5 | Kısayolların tam envanteri | Üst araç çubuğu, tablo satırı, Daha fazla, modal/sekme/durum menüsü ve sayfalama dahil tüm kısa yolları test et | Detay penceresinde 13 eylem, varlık satırında 7 kısayol için kaynak denetleyicileri var. Çöp kutusu API'si izole kabulden geçti; satır kısayollarının gerçek UI başarı/hata/kalıcılık sonuçları henüz denenmedi. Tam eylem paydası ve bütün ekranlar hâlâ açık; yüzde açıklanamaz. |
| 6 | Demo ve canlı ayrımı | Demo salt örnek veride çalışsın; ürün isteklerine göre yerel ve canlı demo aynı kaynak/başlıkları yansıtsın | Demo `localhost:8081/demo/` ve public-site hedefidir; ana uygulama `localhost:8080`. Demo 50 satır/19 sütun kontrolü geçti. Canlı üretim dağıtımını her kod değişiminde ayrıca doğrulamak gerekir; yerel değişikliği canlıya geçmiş gibi sunma. |
| 7 | İşlev kapsamı ve V1 boşlukları | Gereksinim listesindeki varlık, çalışan, zimmet, iade, lisans, aksesuar, bakım, rapor, veri/backup, rol, API, platform ve ticari akışları kapat | Güncel kapsam için `docs/PRODUCT-COVERAGE.md` ve `docs/ROADMAP.md` kullan. Öncelikle eksik/karar gerektiren P0/P1'i kaynakta doğrula; Snipe-IT kodu/marka/hukuk, ödeme sağlayıcısı, sertifika ve müşteri işletim kabulü kullanıcı/harici kararlarına bağlıdır. |
| 8 | Kullanıcıya sunulan özet | Son 10 saat/gün özeti, ilerleme ve kalan işleri kanıtla bildir | Yalnız mevcut commit, dosya değişikliği, ölçüm ve çalışma zamanı kanıtı raporlanır; geçmişe dönük olarak yapılmamış eylemler varmış gibi yazılmaz. |

## Yüzde hesaplama standardı

- **Menü erişim oranı:** doğru hedef yüzeyini açan menü rotası / toplam doğrulanan menü rotası. Mevcut kayıt: 50/50 rota.
- **Menü işlev oranı:** başarılı ve kalıcı sonucu kabul edilmiş alt eylem / envantere alınmış toplam alt eylem. Henüz toplam payda ve tüm sonuçlar kabul edilmediğinden genel oran yayımlanamaz.
- **Kısayol oranı:** aynı şekilde kısayol açılışı değil, beklenen sonuç ve hata/izin davranışı kabul edilen eylemler üzerinden hesaplanır.
- Salt okunur demo aksiyonları ile canlı şirket veri yazma işlemleri ayrı değerlendirilir. İzole API testleri gerçek UI tıklama/mesaj/yeniden yükleme kabulinin yerine geçmez.

## Sonraki uygulanabilir işler

1. Oturum sona erdiğinde `showLogin()` açık dialogları kapatan korumayı içeriyor ve kaynak doğrulayıcı bunu denetliyor. Tarihsel UI gözlemiyle çeliştiği için kontrollü oturum süresi dolma kabulini yeniden yap; varsa modalın arkada kalma yolunu düzelt. Yeniden girişte eski form sessizce gönderilmemeli.
2. Menü/kısayol denetim defterinde her alt eylem için tekil kimlik, beklenen sonuç, rol, kanıt ve sonuç alanını genişlet. Varlık detay modalının 5 sekmesi ve dar ekran/kapatma davranışı UI'da kabul edildi; 13 modal eyleminin yazma sonuçları ve 7 satır kısayolunun her biri UI'da henüz tam kabul edilmedi. Ardından diğer menülerin üst/alt eylemlerini aynı sırayla incele.
3. Güvenli giriş/kalıcılık: sayfa yenileme kabulü geçti; sonraki kapı 5 dakikayı aşan cookie yenilemesi, açık modal sırasında oturum sonlanması ve 401/403 davranışları.
4. Başlangıç gereksinimlerini `PRODUCT-COVERAGE.md` satırlarına çapraz bağla ve kanıtı bulunmayan durumları “doğrulanmadı” bırak.
5. Her tamamlanan ürün işi için yerel Compose/DB sağlığı, migration, ana ekran/demo ve GitHub senkronunu kontrol et; canlıya dağıtım yalnız gerçek dağıtım kanıtı varsa “canlı” diye işaretlenir.

## 12 Ekim doğrulama notu

12 Ekim devamı: Varlık detayındaki çöp kutusu eylemi düzeltildi; 13 modal eylemi, 5 sekme ve 7 tablo satır kısayolu için kaynak doğrulaması var. İlgili izole API kabul testi geçti ve yeni web imajı localhost:8080'e alındı. Tam UI sonucu girişli tarayıcıda ayrıca kabul edilmeli.

12 Ekim devam kontrolü (tarayıcı): `http://localhost:8080/` için kullanıcı tarafından daha önce açıkça sağlanan hesap bilgileriyle gerçek giriş yapıldı. Santa şirket alanı açıldı; 50 varlık, 10 aksesuar ve 30 çalışan gösterildi. Sayfayı yeniden yükleyince oturum korundu ve veriler tekrar geldi. Önceki “parola/oturum açma doğrulanmış sayılmaz” maddesi güncel yerel tarayıcı kanıtıyla güncellenmiştir; 5 dakika üstü cookie yenileme kabul testi hâlâ yapılmadı. Beş varlık sekmesi ve denetim, denetim planı, bakım, transfer, zimmet, düzenleme ve kopyalama formları UI'da açılıp kaydetmeden iptal edildi. Durum listesi açıldı, seçim yapılmadı; arşive taşı onaylanmadı. Şirket verisini değiştiren işlem yapılmadı. Bu giriş doğrulaması yerel Docker `localhost:8080` içindir; canlı internet sitesi dağıtım kanıtı değildir.

12 Ekim devamı: Oturumlu şirket ekranında varlık tablosunun CSS ölçümü, 20 başlıktan on veri sütununun 7 px'e düştüğünü gösterdi. Tüm 19 veri sütununa genişlik/minimum genişlik atanıp tablo içeriğine göre genişletildi; uzun metin hücre içinde satıra kırılır ve mevcut yatay kaydırma kullanılabilir. `tools/Verify-AssetTableLayout.mjs` eklendi ve geçti; satır kısayolu (7), varlık detay (13) ve gezinme (50 rota) kaynak doğrulayıcıları da tekrar geçti. Yalnız web Docker imajı yenilendi, Santa şirket verisi ve veritabanı volume'u korunuyor; servis hazır, 84 migration güncel, demo HTTP 200. Tablo sütunlarının görsel/etkileşimli tarayıcı kabulü ve canlıya dağıtım açık iş olarak kaldı.

12 Ekim devamı: Tekli varlık etiketi kısayolu gerçek oturumda kabul edildi. Uygulama içi modalda ürün adı, etiket, seri no ve barkodlar göründü; kapatma çalıştı. Yazdırma ve veri değiştirme yapılmadı. Bu kabul detay modalındaki ve toplu etiketteki yolları kapsamaz. Docker/API hazır ve demo HTTP 200; web servisi son kaynakla yeniden oluşturuldu.

12 Ekim devamı: Kullanıcının gerçek localhost sekmesindeki 304 px görünümünde detay modalı iki çok dar sütuna sıkıştığı için tek sütun responsive kuralı ve yatay taşma koruması eklendi. Modal hâlâ U6 Pro · 01 gerçek kaydını ve özet/alan değerlerini gösteriyor; sekmeler kendi şeridinde kayıyor, modal gövdesi dikey kayıyor. Release web imajı üretildi ve yalnız web konteyneri yenilendi; veritabanı ve medya volume'ları korunuyor. `health` 200/Healthy, demo 200; kaynak doğrulayıcıları: modal 5/13, satır 7, tablo 19 sütun, navigasyon 50 hedef/10 grup geçti. Bu sayılar ilgili yapısal/kaynak kapılarını bildirir, tüm alt işlevlerin başarı yüzdesi değildir.

12 Ekim devamı: `Login_refreshes_antiforgery_token_for_authenticated_session_requests` izole kabuline security-stamp aralığı sıfırken yenileme sonrası `companyId`, `companyName`, `admin` rolü ve `/api/assets` şirket kapsamı eklendi; `OWN-1` görünür, `OTHER-1` görünmez. Tam izole PostgreSQL 17 paketi 93/93 geçti. Aynı localhost tarayıcı oturumu 12 dakikadan uzun süre açık kaldı; modal açıkken İlişkiler sekmesine geçildi ve kategori/marka/model/konum ile ilişkili API verileri yüklendi; modal kapanmadı ve şirket alanı login ekranına düşmedi. Bu, gerçek 5 dakikalık SecurityStamp yenileme aralığından sonra UI oturumunun korunmasını doğrular; süresi zorla bitirilmiş/iptal edilmiş cookie ile 401 sırasında açık modalın kapatılması ve tekrar giriş sonrası eski işlemin gönderilmemesi henüz ayrı kabul edilmedi. İzole test veritabanı temizlendi; geliştirme veritabanı ve volume'ları kullanılmadı.

12 Ekim devamı: 433 px yerel tarayıcı görünümünde varlık detay gövdesinin boş kalması canlı olarak tekrarlandı. Kaynakta sekme panelleri vardı ancak modal gövdesi erişilebilirlik ağacında yoktu. Dialog yüksekliği/gövde kaydırması sabitlendi, etkin panelin görünürlük ve erişilebilirlik durumları birlikte yönetiliyor. İlk düzenleme kapalı native dialog'u sayfa akışında gösterdi; kaynakta `.asset-detail-dialog[open]` ile düzeltildi ve doğrulayıcıya regresyon koşulu eklendi. Son imajın ardından gerçek Santa kaydı `U6 Pro · 01` açıldı; genel bakışta kategori, marka/model, durum, seri numarası, alış bedeli ve mevcut değer göründü. İlişkiler, Bakım, Hareket geçmişi ve Fotoğraf/dosyalar sekmelerinin her birinde içerik veya doğru boş durum mesajı görüldü. 433 px ekran görüntüsünde sekmeler yatay kaydırılabilir, panel gövdesi dikey kaydırılabilir; Kapat düğmesi modalı kapatıp 50 kayıtlı listeye döndürdü. Kapalı modalın listede görünmediği de kabul edildi. Dosya yüklenmedi, not yazılmadı, şirket verisi değiştirilmedi. Bu dar ekran/modal düzeltmesi yerel UI kabulinden geçti; diğer menü ve eylemlerin genel işlev yüzdesi değildir. Giriş ekranı kısa süre göründü; şirket oturumu yeniden kullanılabilir hale geldi, ancak login formu üzerinde parola gönderimi yapılmadı. DB ve volume'lar değiştirilmedi.

Aynı tarayıcı turunda salt-okunur varlık listesi kabulü: “Kullanılabilir” filtresi 40 kayıt, `SNT-AP-037` araması 1 kayıt verdi; arama temizlenince 50 kayda döndü. İkinci sayfa 13–24/50, geri dönüş 1–12/50 gösterdi. Bu 3 liste davranışı UI'da geçti; garanti/BYOD/özel alan filtreleri, sıralama, diğer sayfa boyutları ve satır kısayollarının işlem sonuçları açık.
