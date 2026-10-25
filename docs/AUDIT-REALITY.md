## 25 Ekim 2026 — güncel canlı PUT ve izolasyon sürümü

Canlı web/ana sayfa **124b2d1e93af2a13c393d68353c271671c856de8**, şema **121/0**. Tam CI 40158737966 üç işte başarılı; gerçek izole staging, taze 080433Z SQL yedeği, 140 MB 080518Z rollback, 080618Z dosya/imaj provası ve ayrı DB'de 95 tablo sayımı eşitliği sonrası Coolify **8eyikaefpne2tcfuetcloj3n Success/Finished**. Gerçek imaj etiketleri doğrulandı. İlk Farma 10 saniyelik gözlem zaman aşımı bağımsız HTTPS ve iki-host probe ile yeniden kontrol edildi; 200 ready/121/0. 8 plan ve 8 görsel/galeri kaynağı korundu. Checkout eksik sağlayıcı nedeniyle kapalı. Santa/Demo3 şirketleri henüz yok; normal provisioning ve gerçek Bölüm 20/SMTP/ödeme kabulü açık. 24 saatlik gözlem planı var, 24 saat tamamlandı iddiası yok. Kanıt: tests/reports/production-asset-put-release-20261025.md. Önceki başlıklar tarihsel kayıttır.

## 25 Ekim 2026 — güncel canlı firma silme/geri alma sürümü

Canlı web ve ana sayfa **6198ef6bc424ac44e9e74c9ab39357b2d6c162b9**, SQL şeması **121**, apex/Farma HTTPS ready ve 0 bekleyen. CI 40135536734 üç işte başarılı; gerçek izole staging, iki SQL uygulamasında 94 veri tablosunun özgün alan içeriklerinin korunması, güncel 023647Z SQL/medya/anahtar/imaj yedeği ve ayrı geri dönüş provası sonrası Coolify q7up7i4mglpe1w1ismqpnmzn yayını Success/Finished. Firma çöp kutusu/geri alma ve önceki demo/MFA özellikleri sunuluyor. 8 plan ve istenen görseller kaynak hash'leriyle korundu. Checkout sağlayıcı eksikliği nedeniyle kapalı; gerçek SMTP/ödeme ve Bölüm 20 müşteri kabulü tamamlanmadı. Ana dala merge veya tam Hedef tamamlanması iddia edilmiyor. Kanıt: tests/reports/production-company-deletion-release-20261025.md. Aşağıdaki sürümler tarihsel kayıttır.

## 25 Ekim 2026 — önceki canlı demo ve MFA sürümü

Canlı web/ana sayfa 5790980f8f8a298b720287260a37b7f972bd1e57, şema 120/0, apex ve Farma HTTPS health 200/ready. Tam CI 40129797684 3/3 success; gerçek staging ve hemen önceki bf3ed6d imajlarını içeren güncel SQL/medya/anahtar/imaj yedeği, ayrı 95 tablo restore sayımı ve dosya/imaj provası sonrası Coolify esw998lnesfol9swvgxwq3li yayını başarılı. MFA giriş kanıtı ve gerekçeli süreli demo oluşturma canlıda; yeni demo kontrolü sunulan platform HTML’de doğrulandı. 8 yıllık Cloud planı yayımlı, checkout sağlayıcı eksikliği nedeniyle kapalı. Canlı yönetici oturumu giriş ekranında; yeni OTP istenmedi. Gerçek şirket oluşturma/müşteri işlemleri ve SMTP/iyzico/Bölüm 20 kabulü tamamlanmadı; ana dala merge iddia edilmiyor. Kanıt: tests/reports/production-demo-release-20261024.md ve 25 Ekim canlı probe logları. Aşağıdaki önceki canlı sürüm notları tarihsel kayıttır.
# V1 durum iddiaları — kanıt denetimi

**Denetim tarihi:** 23 Ekim 2026
**Kaynak ağacı:** `feat/verified-cloud-trials-20261021`, HEAD `036b52a654da6cda87f7dec15285373303f4f9e5`
**Kapsam:** `PROJECT-STATUS.md`, `PRODUCT-COVERAGE.md`, `ROADMAP.md` içindeki yüzde iddiaları; ilişkili kod/test kayıtları; salt-okunur canlı satış ve şema durumu.

## Sonuç

Bu üç dokümandaki yüzdeler güncel V1 tamamlanma ölçüsü değildir. `PROJECT-STATUS.md` içinde yüzde bulunan 11 satırın, `PRODUCT-COVERAGE.md` içindeki 2 satırın tamamı 15–16 Ekim tarihli rapor veya geçmiş yerel ölçümleridir. `ROADMAP.md` (300 satır) yüzde iddiası içermiyor. Bu çalışma ağacında bu eski yüzde/puanları üreten bir ilerleme sayfası veya güncel hesaplayıcı bulunamadı. Dolayısıyla bu sayılar 23 Ekim’deki kod, canlı yayın veya müşteri kabulü hakkında güncel kanıt sunmaz.

Canlı yönetici girişi ve maskeli ekran görüntüsü bu denetimde de alınmadı. Bilgisayar-kullanım güvenlik yönergesi kullanıcı kimlik doğrulama pencerelerinin otomasyonunu yasakladığından, giriş bilgileri forma gönderilmedi. P0-A'nın yüzde iddialarına ilişkin kaynak denetimi ve salt-okunur canlı sağlık kontrolü tamamlandı; yönetici ekranı adımı **ENGELLENDİ** kaldı.

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

- Kaynak çalışma ağacı: **119 EF migration kimliği**; `origin/main` **118**. Tek fark PR #25'teki `20261023190000_AddExistingCompanyCheckoutContext` migration'ı ve model snapshot güncellemesidir.
- Salt-okunur canlı `GET https://envantertr.com/api/status` ve `GET https://firma.envantertr.com/api/status`: ikisi de HTTP 200 `ready`, **118 migration / 0 bekleyen**. Coolify terminalindeki production PostgreSQL konteynerinde `BEGIN READ ONLY` sorgusuyla `__EFMigrationsHistory` listesi ayrıca karşılaştırıldı: canlı ve `origin/main` aynı **118** benzersiz migration kimliğine, aynı ilk/son kimliğe ve sıralı liste MD5 `c8694e4a153c5f81d5cface1e0c9f366` değerine sahip. PR'daki 119. migration canlıya uygulanmadı. Ayrıntı: `tests/reports/p0b-migration-identity-acceptance-20261023.md`.
- Salt-okunur canlı `GET /api/plans`: HTTP 200, **0 yayımlı plan**. `GET /api/checkout/status`: HTTP 200, `available:false`. Satış/ödeme hazır değil.
- `036b52a654da6cda87f7dec15285373303f4f9e5` PR #25 taslak dalında. Tam GitHub Actions run `40008494630` bu SHA için üç işi **3/3 success** tamamladı. Gerçeklik denetimi commit'i `217261746e62a5f6debbe6010b71e40a033c7b2d` için run `40019065157` de üç işi **3/3 success** tamamladı. Yerel hosted Chromium acceptance exit 0'dır; bu gerçek iyzico veya yönetici oturum kabulü değildir.
- Yönetici canlı oturum ve ekran görüntüsü: **ENGELLENDİ**; bilgisayar-kullanım aracı kimlik doğrulama pencerelerini otomatikleştiremez. Parola araca gönderilmedi ve ekran görüntüsü alınmadı.

## 23 Ekim — sonraki düzeltme ve kabul

İlk denetimde başarısız bulunan iki kaynak doğrulayıcısı ve platform Chromium kotasız-Cloud varsayımı düzeltildi. Onaylı paketlerde kullanıcı kotası korunuyor; tarayıcı testi dört kullanıcı limitinin POST gövdesinde ve `4 / 4` kullanımında gösterildiğini doğruluyor. Model ayrıntısı denetimi görünür pencere, doğru özet ve satın alma siparişini ayrı ayrı denetliyor. Üç kaynak doğrulayıcısı ve `platform-admin-members.mjs` / `asset-detail-actions.mjs` Chromium senaryoları exit 0 verdi. Kanıt: `tests/reports/p0-catalog-browser-contract-acceptance-20261023.md`. Bu kayıt yalnız söz konusu eski test uyumsuzluklarını kapatır; canlı müşteri kabulü, satış kapıları ve genel V1 tamamlanması hâlâ açık.

## Denetim komutları

- `rg` ile üç dokümandaki yüzde ifadeleri sayıldı: 11 + 2 + 0 satır.
- Kaynak/test taraması: platform yönetici oturumları, sistem sağlığı ve varlık modeli ayrıntısı için yukarıdaki C# endpoint, UI ve xUnit izleri bulundu.
- Canlı API kontrolleri salt-okunur HTTP GET’tir; hesap, veri veya yapılandırma değiştirilmedi.

**P0-A sonucu:** Yüzde iddialarının tamamı tarihsel; güncel V1 tamamlanmasını kanıtlamıyor. Dağıtılmış canlı migration kümesinin 118 kimliği `origin/main` ile eşleşiyor; release adayı bir ek migration içeriyor. CI yeşil olsa da yayımlı plan yok, checkout kullanılamıyor ve gerçek iyzico/SMTP kabulü tamamlanmadı. Yönetici ekranı/giriş adımı, Windows bilgisayar-kullanım aracının kimlik doğrulamasını otomatikleştirmemesi nedeniyle **ENGELLENDİ**. Yeşil CI'dan aday kodun veya checkout migration'ının canlıya alındığı sonucu çıkarılmamalı.

## 24 Ekim — yeni destek oturumu migration adayı

Kaynak migration sayısı artık 120: tarihsel canlı 118 kimlik listesine ExistingCompanyCheckoutContext ve PlatformImpersonationSessions eklenir. Her iki Up yalnız nullable alanlar/constraint/index ekler. İzole 118 → 120 idempotent SQL kabulü özgün satır/alan SHA-256 eşitliğiyle 1/1 geçti; üretim yedeği/staging değildir. Salt-okunur canlı GET kontrolü apex/firma 118/0 ready, public plan 0 ve checkout kapalı gösteriyor. Anahtarlı SSH reddedildi; canlı DB/ayar/deploy değişmedi. Güncel kanıt: tests/reports/current-release-migration-acceptance-20261024.md.

## 24 Ekim 2026 — gerçek yedek, geri dönüş ve staging

Mevcut Coolify oturumuyla erişim sağlandı; SSH/API erişimi engeli bu çalışma için çözüldü. Taze 142 MB geri dönüş paketi doğrulandı; gerçek dump ayrı DB'ye geri yüklendi (95 tablo sayımı eşit). 118→120 exact SQL ve tekrarında 94 veri tablosunun özgün alan/satırları korundu. Aday staging imajları derlendi; yalnız internal ağda, dış port olmadan 11 temel HTTP/güvenlik kontrolü geçti. ae875b6 CI 3/3 success (40073522201). Canlı 118/0 olarak kaldı; dağıtım yapılmadı. Tam kimlik doğrulamalı müşteri/provider/Bölüm 20 kabulü açık. Kanıt: tests/reports/real-server-staging-acceptance-20261024.md ve altı Coolify ekran görüntüsü.
