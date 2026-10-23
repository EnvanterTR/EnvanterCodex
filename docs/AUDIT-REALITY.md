# V1 durum iddiaları — kanıt denetimi

**Denetim tarihi:** 23 Ekim 2026
**Kaynak ağacı:** `feat/verified-cloud-trials-20261021`, HEAD `bb09ba8`
**Kapsam:** `PROJECT-STATUS.md`, `PRODUCT-COVERAGE.md`, `ROADMAP.md` içindeki yüzde iddiaları; ilişkili kod/test kayıtları; salt-okunur canlı satış ve şema durumu.

## Sonuç

Bu üç dokümandaki yüzdeler güncel V1 tamamlanma ölçüsü değildir. `PROJECT-STATUS.md` içinde yüzde bulunan 11 satırın, `PRODUCT-COVERAGE.md` içindeki 2 satırın tamamı 15–16 Ekim tarihli rapor veya geçmiş yerel ölçümleridir. `ROADMAP.md` (300 satır) yüzde iddiası içermiyor. Bu çalışma ağacında bu eski yüzde/puanları üreten bir ilerleme sayfası veya güncel hesaplayıcı bulunamadı. Dolayısıyla bu sayılar 23 Ekim’deki kod, canlı yayın veya müşteri kabulü hakkında güncel kanıt sunmaz.

`docs/PROGRESS.md` ve bu denetim dosyası başlangıçta yoktu. Canlı yönetici girişini ve maskeli ekran görüntüsünü alma adımı Windows bilgisayar-kullanım aracı açık sekmenin adresini güvenlik politikasına göre doğrulayamadığı için tamamlanamadı; kimlik bilgileri giriş formuna gönderilmedi ve ekran görüntüsü alınmadı. Bu yüzden P0-A paketi **ENGELLENDİ** olarak kaydedildi; aşağıdaki kaynak ve canlı kontrolleri bağımsız olarak tamamlandı.

## Yüzde iddiaları

| Kaynak | Tarihsel iddia | Kod/test bağı | 23 Ekim durumu |
|---|---|---|---|
| `PROJECT-STATUS.md:63` | 88 izlenen / 81 kapsam / 17 tamamlanan; 67,06% eşit ağırlıklı puan | Platform oturum verisi bugün de `PlatformEndpoints.cs:362-422` içinde; kabul testi `TenantSessionAcceptanceTests.cs:4767` | Puan ve payda eski rapor anlık görüntüsüdür. Güncel ölçüm kodu veya bu yüzdeleri yeniden üreten rapor bulunamadı. Oturum işlevi için geçmişte test kaydedilmiş; bu denetimde yeniden çalıştırılmadı. |
| `PROJECT-STATUS.md:64` | 87/80/16; 66,65% | Sistem sağlığı endpoint’i `PlatformEndpoints.cs:91` ve UI `platform-admin.html:148,249`; test `TenantSessionAcceptanceTests.cs:4734` | İşlevin kod/test izi mevcut; yüzde tarihsel rapor sayacıdır. Mevcut çalışma ağacında 100 migration varsayımı güncel değil. |
| `PROJECT-STATUS.md:572` | 84 kapsam maddesi, 13/13 tamamlanan, 65,35% | Bu dalda ilgili ilerleme raporu UI’si ve 13 satırlı güncel kabul listesi bulunamadı. | Tarihsel Chromium sonucu; güncel V1 tamamlanma göstergesi olarak doğrulanamaz. |
| `PROJECT-STATUS.md:573` | 13/13 filtre, gösterim 65% | Aynı rapor UI’si/kapsam kaynağı bu dalda yok. | Tarihsel tarayıcı gözlemi; bugünkü kod için tekrar üretilebilir kanıt yok. |
| `PROJECT-STATUS.md:586` | 85 toplam, 78 kapsam, 14 tamamlanan; 65,79% | Metin 77 kaynak doğrulayıcısı ve kısmi Chromium senaryosu kaydediyor; bugünkü hesaplayıcı/çıktı dosyası yok. | Tarihsel araç sayısı; mevcut kod/test durumunu veya canlı yayını ölçmüyor. |
| `PROJECT-STATUS.md:593` | Yerel çalışma durumu %60’tan %65’e düzeltildi | Metindeki yerel migration sayısı 93/96 ve rapor UI’si mevcut değil. | Yerel puan eski anlık görüntü; güncel tam puan hesaplanamaz. |
| `PROJECT-STATUS.md:608` | 87/80/16; 66,65% (başlık 67%) | Aynı oturum ve sağlık endpoint’leri yukarıda; bu satır tarihsel pano hesabını anlatıyor. | Rapor tekrar sayımı ve geçmiş oran; güncel ürün kapsaması değil. |
| `PROJECT-STATUS.md:702` | 110 izlenen, 103 kapsam, 39 tamamlanan (%37,86); tahmin %74,10 | Yerel rapor/snapshot kodu bu dalda yok. | Tarihsel tahmin; güncel kanıt bulunmuyor. |
| `PROJECT-STATUS.md:708` | 111 izlenen, 104 kapsam, 40 tamamlanan (%38,46); tahmin %74,35 | Yerel saatlik rapor snapshot’ı bu dalda yok. | Tarihsel tahmin; güncel kanıt bulunmuyor. |
| `PROJECT-STATUS.md:721` | 113 izlenen, 106 kapsam, 42 tamamlanan (%39,62); tahmin %74,83 | İlgili geçmiş rapor UI’si bu dalda yok. | Tarihsel tahmin; güncel kanıt bulunmuyor. |
| `PROJECT-STATUS.md:727` | 114 izlenen, 107 kapsam, 43 tamamlanan (%40,19); tahmin %75,07 | Satır hosted Chromium ve 80 verifier’ın eski çalışmasını kaydediyor; bugünkü V1 raporu değil. | Tarihsel test/puan kaydı; güncel CI veya tamamlanma kanıtı sayılamaz. |
| `PRODUCT-COVERAGE.md:26` | 87/80/16; 66,65% | Sistem sağlığı kodu ve testi `PlatformEndpoints.cs:91`, `platform-admin.html:148,249`, `TenantSessionAcceptanceTests.cs:4734` | Tarihsel, aynı eski rapor sayacı; P0 ticari kapıları açık. |
| `PRODUCT-COVERAGE.md:675` | 85 kapsam maddesi içinden 66,23% | Model ayrıntısı endpoint’i `AssetModelDetailEndpoints.cs:8-10`; tenant kabul testi `TenantSessionAcceptanceTests.cs:17855`; kaynak denetimi `Verify-AssetModelDetailsUi.mjs` | Özellik izi mevcut fakat eski verifier bu dalda başarısız (beklediği Chromium dizesi yok). Bu nedenle tarihsel 66,23% ve model özelliğinin bugünkü tarayıcı kabulü doğrulanmış sayılamaz. |
| `ROADMAP.md` | Yüzde iddiası yok | 300 satır tarandı. | Yüzde denetimi gerekmiyor. |

## Güncel doğrulama

- Kaynak çalışma ağacı: **118 EF migration sınıfı** (`AppDbContextModelSnapshot.cs` hariç); `origin/main` ile migration dosyaları aynı. Son commit `bb09ba8`. 2 test dosyası değiştirilmiş ve 1 test logu izlenmiyor; henüz commit/push/deploy edilmedi.
- Salt-okunur canlı `GET https://envantertr.com/api/status`: `ready`, **118 migration / 0 bekleyen**. Canlı `firma.envantertr.com/api/status`: `ready`, **118/0**. Görünen migration sayısı eşit; public status uygulanan migration kimliklerini döndürmediği için ad/adli migration geçmişi eşitliği burada doğrulanamadı.
- Salt-okunur canlı `GET /api/plans`: HTTP 200, boş liste. `GET /api/checkout/status`: `available:false`. Gerçek satış/ödeme hazır değil.
- Mevcut dalda `node tools/Verify-HostedCheckoutBrowserAcceptance.mjs` geçti. `Verify-PlatformAdminExperience.mjs` başarısız: eski doğrulayıcı Cloud planlarında kullanıcı kotasının sınırsız kalmasını bekliyor; güncel onaylı paket kotaları bu beklentiyle çelişiyor. `Verify-AssetModelDetailsUi.mjs` başarısız: beklediği model ayrıntısı/sipariş metni Chromium betiğinde bulunmuyor. İki başarısızlık da kodun kabul edildiği iddiasını desteklemiyor; düzeltme ve gerçek tarayıcı yeniden kabulü açık.
- Tam hosted Chromium paketi bu dalda exit 1 verdi: `platform-admin-members.mjs:760` eski “Cloud plan kotasız” assertion’ı. Yerel düzeltme henüz yeniden çalıştırılmadı. Bu hata rapor yüzdeleriyle ilgisizdir, ancak test uyumsuzluğunun güncel ve gerçek olduğunu gösterir.
- Yönetici canlı oturum ve ekran görüntüsü: **ENGELLENDİ**; bilgisayar-kullanım URL güvenlik denetimi açık sekmenin adresini tanıyamadı. Kullanıcı parolası araca girilmedi.

## Denetim komutları

- `rg` ile üç dokümandaki yüzde ifadeleri sayıldı: 11 + 2 + 0 satır.
- Kaynak/test taraması: platform yönetici oturumları, sistem sağlığı ve varlık modeli ayrıntısı için yukarıdaki C# endpoint, UI ve xUnit izleri bulundu.
- Canlı API kontrolleri salt-okunur HTTP GET’tir; hesap, veri veya yapılandırma değiştirilmedi.

**P0-A sonucu:** yüzde iddialarının hepsi tarihsel ve güncel V1 tamamlanmasını ispatlamıyor. Yeni özellik işine geçmeden önce tarihsel oranlar güncel rapor olarak kullanılmamalı; canlı giriş kabulü ve Windows araç engeli ayrıca çözülmeli. P0-B sayım kontrolünde ilk ham `.cs` dosya sayımı EF model snapshot’ını yanlışlıkla migration olarak saymıştı; düzeltilmiş sayım 118 sınıf, canlı 118/0. Önceki üretim-yedeği kopya provası (`tests/reports/production-clone-118-rehearsal-20261021.md`) 93 özgün tablonun alan/satır projeksiyonlarını koruyup idempotent migrationı 118’de doğruladı; canlı uygulanan migration kimliklerinin birebir karşılaştırması henüz kanıtlanmadı.
