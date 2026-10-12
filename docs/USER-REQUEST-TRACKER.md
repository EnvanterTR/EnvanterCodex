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
| 1 | Güvenli yerel giriş ve kalıcılık | localhost:8080 login doğru çalışsın; Docker kaldırma/yenileme veriyi kaybettirmesin; servis, API ve GitHub durumu anlaşılır olsun | Identity cookie yenileme claim kaybı düzeltildi; 93/93 izole PG kabul paketi geçti; 12 Ekim tekrarında API `ready`, migration 84/84, kullanıcı hesabı onaylı/kilitsiz ve tek etkin üyelikli çıktı. Parola ve gerçek oturum açma bu kontrolle doğrulanmış sayılmaz; 5 dk üstü UI kabuli açık. Docker yığınını `--volumes` ile silme. |
| 2 | V1 gereksinim eşlemesi | Yaklaşık 280 maddeyi kaynak/kanıt ve eksiklerle eşleştir | Başlangıç listesi eklendi; statüler eski. `PRODUCT-COVERAGE.md` 37 ürün ailesi, `ROADMAP.md` açık ürün/kurulum işleri içeriyor. Her satır kod veya kabul kanıtıyla güncellenmeli. |
| 3 | Yan menü hedefleri ve işlevleri | Her menü, alt menü, form ve eylem kendi doğru şirkete/role bağlı gerçek işlemi yapsın | `Verify-AppNavigation.mjs`: 50 hedef/10 grup bağlama denetimi. 12 Ekim UI turunda 50/50 rota erişimi. Bildirim/Güvenlik kısmi; özel izinler, harici webhook/entegrasyon ve bazı işlevler eksik. Rota oranı işlev yüzdesi değildir. |
| 4 | Varlık listesi ve ekran görüntüsü düzeni | Gereksiz boşlukları azalt, sütunları/eylemleri yatay kaydırmada kullanılabilir tut; modal sekmeleri ve hızlı işlemler doğru sonuç versin | Demo kaynağında 19 ortak sütun, eylem sütunu sabitleme ve tam genişlik kuralları doğrulandı; localhost demo 50 örnek kayıt. Modal API işlemlerinde 93/93 genel kabul; altı modal formu yalnızca aç/iptal edildi (gönderim 0/6). Bu turda görünürde etkisiz “Çöp kutusuna taşı” kontrolü gerçek arşiv/soft-delete API akışına bağlandı; UI tıklama kabuli açık. |
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
2. Menü/kısayol denetim defterine her alt eylem için tekil kimlik, beklenen sonuç, role, kanıt ve sonuç alanı ekle; varlık detayındaki 13 eylem ve tablodaki 7 satır kısayolu kaynak eşlemesi yapıldı. UI kabuli ve kalan üst/alt kısayollar sürüyor.
3. Giriş kabulini kullanıcı kendi açık tarayıcısında yapabildiğinde 5 dakikadan uzun kullanım, sayfa yenileme, modallar ve 401/403 davranışlarıyla tamamla.
4. Başlangıç gereksinimlerini `PRODUCT-COVERAGE.md` satırlarına çapraz bağla ve kanıtı bulunmayan durumları “doğrulanmadı” bırak.
5. Her tamamlanan ürün işi için yerel Compose/DB sağlığı, migration, ana ekran/demo ve GitHub senkronunu kontrol et; canlıya dağıtım yalnız gerçek dağıtım kanıtı varsa “canlı” diye işaretlenir.

## 12 Ekim doğrulama notu

12 Ekim devamı: Varlık detayındaki çöp kutusu eylemi düzeltildi; 13 modal eylemi, 5 sekme ve 7 tablo satır kısayolu için kaynak doğrulaması var. İlgili izole API kabul testi geçti ve yeni web imajı localhost:8080'e alındı. Tam UI sonucu girişli tarayıcıda ayrıca kabul edilmeli.
