## 23 Ekim 2026 — P0-A durum yüzdesi denetimi
- Durum: ENGELLENDİ
- Değişen dosyalar: `docs/AUDIT-REALITY.md`, `docs/PROGRESS.md`
- Komut + çıktı: Üç durum dokümanı tarandı; 13 yüzde satırının tamamı 15–16 Ekim tarihli. Salt-okunur canlı GET: apex ve firma `ready` 118/0; plan listesi boş; checkout kullanılamıyor. Düzeltilmiş EF migration sınıf sayısı snapshot hariç 118; `origin/main` migration dosyalarıyla aynı. Public health migration kimliklerini göstermiyor.
- Test: `Verify-HostedCheckoutBrowserAcceptance.mjs` geçti. `Verify-PlatformAdminExperience.mjs` ve `Verify-AssetModelDetailsUi.mjs` başarısız; ayrıntılar denetim dosyasında. Tam hosted Chromium paketi exit 1; eski sınırsız Cloud kotası assertion’ı.
- Kanıt yolu: `docs/AUDIT-REALITY.md`; mevcut endpoint/test izleri tabloda.
- Sıradaki iş: P0-B — production-clone raporundaki yedek/geri yükleme provası var; canlı migration kimliklerinin birebir karşılaştırması ve geri dönüş kabulünün güncelliği teyit edilecek. Canlı yönetici ekran görüntüsü, bilgisayar-kullanım aracı URL güvenlik engeli nedeniyle alınamadı; parola forma gönderilmedi.

### 23 Ekim 2026 — Görsel PR #24 canlı SHA doğrulaması
- Durum: TAMAM
- Değişen dosyalar: `tests/reports/coolify-main-sha-verification-20261023.png`, `docs/PROGRESS.md`
- Komut + çıktı: `git fetch origin main` sonrası `origin/main=4d55270827be312775ebffe60c5a6d74011d7802`. Coolify dağıtım geçmişindeki en yeni başarılı kayıt `4d55270` ve açıklaması PR #24 merge commit'i; uygulama `Running`.
- Test: `https://envantertr.com` canlı ana sayfasında etiket galerisinin 4 görselli otomatik döngüsü ve duraklatma düğmesi mevcut; portal önizlemesindeki “Tam ekran görüntüle” düğmesi tam ekran katmanını açtı. Görsel PR'ın doğru SHA'sı üretimde zaten çalışıyor; bu turda yinelenen deploy tetiklenmedi.
- Kanıt yolu: `tests/reports/coolify-main-sha-verification-20261023.png`; canlı arayüz doğrulaması 23 Ekim 2026.
- Sıradaki iş: P0-B — migration kimliklerinin canlıyla karşılaştırılması, taze yedek ve geri dönüş kabulünün doğrulanması.
- Not: Coolify Git Source sayfası ayrı sekmede de “You have changes that haven't been saved yet” gösteriyor ve kaydetme denemesi bu göstergeden kurtarmadı; kaynak alanında main + doğru SHA görünmesine ve başarılı deploy kaydına rağmen bu UI uyarısının kök nedeni ENGELLENDİ. Canlı deploy geçmişi ve canlı sayfa kanıtı doğru revizyonun yayında olduğunu doğruluyor.

### 23 Ekim 2026 — P0-B canlı yedek tazeliği kontrolü
- Durum: TAMAM (başlangıçta ENGELLENDİ; aynı gün aşağıdaki kayıtta yeni yedek ve restore provasıyla kapatıldı)
- Değişen dosyalar: `docs/PROGRESS.md`
- Komut + çıktı: Coolify production projesi 1 kaynak gösteriyor (uygulama); uygulamanın Backups sayfasında `Schedules: 0`, `Enabled: 0`, `Total executions: 0` ve `No scheduled backups` yazıyor.
- Test: İlk salt-okunur Coolify UI kontrolünde 23 Ekim için taze yedek kanıtı bulunmadı. Daha sonra aynı gün taze PostgreSQL + medya/anahtar yedeği ve izole restore provası tamamlandı; eşit satır sayımları aşağıdaki P0-B kabul kaydında doğrulandı. Coolify'de otomatik yedek planı ve S3 hedefi hâlâ yok.
- Kanıt yolu: Coolify `/project/laj1kik2jvyedpoxirxvuukc/environment/qsgjulvq6cfxpxpvunw7ua8e/application/izrckbfan6xop8b6xgigduvt/backups`; nihai yedek/restore ayrıntısı `tests/reports/coolify-sha-save-triage-20261023.md`.
- Sıradaki iş: Aynı sunucudaki yedek ve geri yükleme kapısı doğrulandı. Otomatik/off-site yedek yapılandırması ayrı açık iş olarak kalır; canlıya yeni sürüm alma hedefteki staging + yedek + rollback koşullarına bağlıdır.

### 23 Ekim 2026 — Coolify SHA kaydetme uyarısı ve canlı yedek geri yükleme kabulü
- Durum: TAMAM (SHA/kaydetme teşhisi ve aynı sunucuda geri yükleme provası); Coolify uyarısının kök nedeni ENGELLENDİ.
- Değişen dosyalar: `tests/reports/coolify-sha-save-triage-20261023.md`, `docs/PROGRESS.md`.
- Komut + çıktı: `git fetch origin main` → `origin/main=4d55270827be312775ebffe60c5a6d74011d7802`. Coolify Git Source `main` ve aynı SHA'yı gösteriyor. Kaydetme `Success Application source updated!` bildirimi verdi; yenileme ve Reset sonrasında kaydedilmemiş değişiklik bandı kaldı. Dağıtım geçmişindeki en yeni başarılı kayıt aynı SHA (PR #24, 2026-10-21 17:23:57 UTC); yeniden deploy edilmedi.
- Test: Canlı ve izole restore DB tabloları/satırları eşleşti: 95 tablo, 168 satır, MD5 `49d77bc6ef149bb3da96fa29d65dcfa6`; restore migration geçmişi 118. Geçici ağsız tmpfs konteyner kaldırıldı.
- Kanıt yolu: `tests/reports/coolify-sha-save-triage-20261023.md`, `tests/reports/coolify-main-sha-verification-20261023.png`.
- Sıradaki iş: Coolify uyarı bandının kök nedeni arayüzden belirlenemedi; mevcut SHA canlıda zaten başarılı olduğundan yayımlama tamam. P0 akışındaki sıradaki engel/işe geç; sonraki canlı deploy öncesi hedefteki staging + taze yedek + geri dönüş kapısını uygula.

### 23 Ekim 2026 — P0-C firma alt alanı canlı sağlık kontrolü
- Durum: TAMAM (HTTP/DNS/TLS sağlık yönlendirmesi); tenant izolasyonu ayrıca doğrulanmadı.
- Değişen dosyalar: `tests/reports/gate-p0c-tenant-host-readiness-20261023.md`, `docs/PROGRESS.md`.
- Komut + çıktı: `firma`, `santa`, `farma` DNS A kayıtları Cloudflare IP'lerine çözümlendi; üç hostta `curl` TLS doğrulaması `0`, `/api/status` HTTP `200`, `ready`, `118/0`. `firma` ana sayfası ve `/giris` HTTP `200`.
- Test: Salt-okunur canlı HTTP/DNS kontrolü; veri değiştirilmedi. Bu, giriş veya çapraz-tenant kabul testi değildir.
- Kanıt yolu: `tests/reports/gate-p0c-tenant-host-readiness-20261023.md`.
- Sıradaki iş: P0-D — canlı plan kataloğu `[]`, checkout `/api/checkout/status` `available:false`; yayımlama/iyzico/provisioning yolunun kök nedenini incele. Canlıya kod veya fiyat kaydı yazmadan önce hedefteki staging + yedek + rollback kapısı uygulanacak.

### 23 Ekim 2026 — P0-D canlı satış engelleri teşhisi
- Durum: ENGELLENDİ
- Değişen dosyalar: `tests/reports/gate-p0d-live-checkout-blockers-20261023.md`, `docs/PROGRESS.md`.
- Komut + çıktı: Canlı `GET /api/plans` HTTP 200 `[]`; `GET /api/checkout/status` HTTP 200 `available:false`. Çalışan web konteynerinde gizli değerleri göstermeden env anahtarları denetlendi: iyzico API/secret/callback ve SMTP host/user/password/from address boş.
- Test: Salt-okunur canlı API + değişken doluluk kontrolü; ödeme ya da mail gönderilmedi. Kod checkout için hem sağlayıcı ayarını hem yayımlanmış, etkin ve fiyatlı Cloud planını zorunlu tutuyor.
- Kanıt yolu: `tests/reports/gate-p0d-live-checkout-blockers-20261023.md`; ilgili kural `src/EnvanterTR.Web/Data/CheckoutEndpoints.cs`.
- Sıradaki iş: Staging ortamında checkout/provisioning kabulini yürüt; sonra iyzico üretim anahtarları ve SMTP uygulama erişimini sunucu gizli değişkenlerine ekle, planları yayımlamadan önce satış koşullarını kabul et. Bu paket canlı DB veya Coolify ayarını değiştirmedi.

### 23 Ekim 2026 — P0-D checkout/webhook/provisioning sentetik kabulü
- Durum: TAMAM (izole sentetik uçtan uca kabul); gerçek iyzico/SMTP ve canlı müşteri kabulü ENGELLENDİ.
- Değişen dosyalar: `tests/reports/p0d-checkout-webhook-provisioning-acceptance-20261023.md`, `docs/PROGRESS.md`.
- Komut + çıktı: `docker compose -f deploy/testing/compose.security-acceptance.yaml -p p0d-checkout run --build --rm acceptance --filter "FullyQualifiedName~Public_checkout_uses_server_price_verifies_provider_and_provisions_once"` → exit 0; 1/1 geçti, 47.5491 sn. Yalıtılmış DB ve sentetik sağlayıcı kullanıldı; compose DB/ağı sonrasında kaldırıldı.
- Test: Sunucu fiyatı/kupon, KVKK onayı, imza ve sipariş eşleştirme, hatalı webhook reddi, tekrar tesliminde tek provisioning, owner/abonelik ve Authenticator içerikli davet e-postası doğrulandı.
- Kanıt yolu: `tests/reports/p0d-checkout-webhook-provisioning-acceptance-20261023.md`.
- Sıradaki iş: P0-D sandbox sağlayıcı/staging; ardından P0-E Bölüm 20 müşteri kabulü. Canlı iyzico ve SMTP sırları boş olduğundan canlı satış açılmadı; canlıya deploy yapılmadı.

### 23 Ekim 2026 — P0-D iyzico istemci ve imza regresyonu
- Durum: TAMAM (5/5 yalıtılmış test); gerçek sandbox bağlantısı ENGELLENDİ.
- Değişen dosyalar: `tests/reports/p0d-iyzico-client-signature-acceptance-20261023.md`, `docs/PROGRESS.md`.
- Komut + çıktı: `docker compose -f deploy/testing/compose.security-acceptance.yaml -p p0d-provider run --build --rm acceptance --filter "FullyQualifiedName~IyzipayCheckoutClientTests|FullyQualifiedName~IyzipayWebhookSignatureTests"` → exit 0; 5/5 geçti, 3.0245 sn. İzole DB/ağ temizlendi.
- Test: Sunucu imzası/gövde bütünlüğü, webhook imzası ve bozulma reddi, eksik credential/güvenilmeyen hostta fail-closed, sağlayıcı hata yanıtı doğrulandı.
- Kanıt yolu: `tests/reports/p0d-iyzico-client-signature-acceptance-20261023.md`.
- Sıradaki iş: Staging'de gerçek sandbox anahtarlarıyla ödeme kabulü ve P0-E Bölüm 20; canlı sırlar hâlâ boş, satış planı yayımlanmadı.
### 23 Ekim 2026 — P0-D bekleyen checkout siparişi sona erme kabulü
- Durum: TAMAM (izole PostgreSQL 17 kabul testi); canlı davranış yayımlanmadı.
- Değişen dosyalar: `src/EnvanterTR.Web/Data/CheckoutEndpoints.cs`, `src/EnvanterTR.Web/Program.cs`, `src/EnvanterTR.Web/Data/CompanyCheckoutOrderExpirationService.cs`, `tests/EnvanterTR.SecurityAcceptance/CheckoutOrderExpirationAcceptanceTests.cs`, `tests/reports/p0d-checkout-order-expiration-acceptance-20261023.md`, `docs/PROGRESS.md`.
- Komut + çıktı: `docker compose -f deploy/testing/compose.security-acceptance.yaml -p p0d-expiration run --build --rm acceptance --filter "FullyQualifiedName~Checkout_expiration_marks_only_overdue_unpaid_orders_and_releases_initialization_leases"` → exit 0; 1/1 geçti iki çalıştırmada. Web derlemesi 0 uyarı / 0 hata; kabul testi derlemesi 4 mevcut ASP.NET kullanımdan kaldırma uyarısı / 0 hata.
- Test: 24 saatten eski `Pending` ve `PaymentStarted` siparişleri `Expired` oldu ve başlatma lease'leri temizlendi; süresi dolmamış, `PaymentReview` ve `ProvisioningFailed` siparişleri korundu; ikinci çalıştırma idempotent. İzole Compose DB/ağı testten sonra kaldırıldı. İlk testte saptanan göç/işçi başlangıç yarışı giderildi; işçi ilk temizleme sorgusunu 15 dakika sonraki ilk periyodik tikte çalıştırır. Rerun bu hatayı üretmedi.
- Kanıt yolu: `tests/reports/p0d-checkout-order-expiration-acceptance-20261023.md`.
- Sıradaki iş: P0-D staging üzerinde gerçek sağlayıcı sandbox kabulü; canlı iyzico/SMTP sırları boş ve plan kataloğu yayımlanmamış durumda olduğundan canlı satış/deploy kapalı kalır.

### 23 Ekim 2026 — P0-E parola kurtarma ve SMTP yakalayıcı kabulü
- Durum: TAMAM (izole SMTP kabul testi); canlı SMTP gönderimi ve teslimatı ENGELLENDİ.
- Değişen dosyalar: `tests/reports/p0e-password-recovery-capture-acceptance-20261023.md`, `docs/PROGRESS.md`.
- Komut + çıktı: `docker compose -f deploy/testing/compose.security-acceptance.yaml -p p0d-expiration run --rm acceptance --filter "FullyQualifiedName~Password_recovery_does_not_disclose_account_existence_and_reset_tokens_are_single_use"` → exit 0; 1/1 geçti, 25.6045 sn.
- Test: Var olan/olmayan hesap yanıtları eşit; izole SMTP yakalayıcısına parola sıfırlama bağlantısı teslim edildi; token tek kullanımlı ve yeni parola doğrulandı. Geçici PostgreSQL/ağ temizlendi. Bu test üretim SMTP'si veya gerçek e-posta teslimatı kanıtı değildir.
- Kanıt yolu: `tests/reports/p0e-password-recovery-capture-acceptance-20261023.md`.
- Sıradaki iş: P0-D gerçek sandbox anahtarı ve P0-E canlı SMTP yapılandırması gerekiyor; production checkout boş plan kataloğu ve sağlayıcı/SMTP sırları nedeniyle kapalı. Farma test kullanıcısının canlı portalda MFA kurup dashboard'a ulaştığı, bu konuşmadaki kullanıcı ekran görüntüsüyle doğrulandı; üçüncü şirket Bölüm 20 kabul senaryosu tamamlanmadı.

### 23 Ekim 2026 — P0-D yıllık katalog ve checkout readiness kabulü
- Durum: TAMAM (izole PostgreSQL kabul testleri); canlı checkout hâlâ kullanılamıyor.
- Değişen dosyalar: `tests/reports/p0d-annual-catalog-checkout-readiness-acceptance-20261023.md`, `docs/PROGRESS.md`.
- Komut + çıktı: `docker compose -f deploy/testing/compose.security-acceptance.yaml -p p0d-expiration run --rm acceptance --filter "FullyQualifiedName~Annual_catalog_drafts_are_authorized_csrf_protected_idempotent_and_preserve_existing_plans|FullyQualifiedName~Platform_checkout_readiness_matches_public_gate_without_exposing_provider_secrets"` → exit 0; 2/2 geçti, 31.6012 sn.
- Test: 15 günlük deneme/KDV hariç yıllık katalog taslağı; yetki, CSRF, idempotency ve mevcut planı koruma; checkout readiness için yayımlı fiyatlı Cloud planı + geçerli provider/callback/API ayarı ve sırların gizlenmesi doğrulandı. İzole DB/ağ temizlendi. Canlıya plan yazılmadı.
- Kanıt yolu: `tests/reports/p0d-annual-catalog-checkout-readiness-acceptance-20261023.md`.
- Sıradaki iş: Canlı salt-okunur kontrolünde `/api/plans=[]`, `/api/checkout/status.available=false`, `/api/status.ready` ve `farma` 118/0 doğrulandı. Gerçek sandbox anahtarları, SMTP ve staging kabul ortamı olmadan P0-D/E canlı ödeme/e-posta/üçüncü firma kabuline geçilmez; sonraki işlem bu dış kapılar için mevcut güvenli yapılandırmayı doğrulamak veya açık engeli belgelemektir.

### 23 Ekim 2026 — P0 bulut denemesi kabulü
- Durum: TAMAM (izole sentetik e-posta kabul testi); canlı e-posta teslimatı ve gerçek müşteri kurulumu doğrulanmadı.
- Değişen dosyalar: `tests/reports/p0-cloud-trial-acceptance-20261023.md`, `tests/reports/cloud-trial-acceptance-20261023.log`, `docs/PROGRESS.md`.
- Komut + çıktı: `docker compose -f deploy/testing/compose.security-acceptance.yaml -p p0d-expiration run --rm acceptance --filter "FullyQualifiedName~Verified_cloud_trial|FullyQualifiedName~Cloud_trial|FullyQualifiedName~Concurrent_cloud_trial|FullyQualifiedName~Concurrent_distinct_cloud_trials"` → exit 0; 6 geçti, 0 başarısız, 1.1533 dakika.
- Test: 15 günlük bulut denemesi, doğrulanmış e-posta, zorunlu MFA, onay tekrarında idempotency/süre uzatmama, mevcut sahip kimlik bilgilerinin korunması, bozuk/süresi dolmuş kanıt ve kapalı planın reddi, aynı e-posta için eşzamanlı denemelerde tek hak doğrulandı. İzole PostgreSQL ve ağ testten sonra kaldırıldı; canlıya yazma yapılmadı.
- Kanıt yolu: `tests/reports/p0-cloud-trial-acceptance-20261023.md`, `tests/reports/cloud-trial-acceptance-20261023.log`.
- Sıradaki iş: P0-D gerçek Iyzico sandbox; P0-E canlı SMTP ve Bölüm 20 üçüncü şirket tenant izolasyonu kabulü. Canlı plan kataloğu boş ve checkout kapalı; bu sentetik kabul satışa açma veya canlıya deploy kanıtı değildir.

### 23 Ekim 2026 — P0-E tenant host ve slug kabulü
- Durum: TAMAM (izole tenant host/session/slug testleri ve canlı wildcard DNS/TLS salt-okunur kontrolü); Bölüm 20 müşteri iş akışı tamamlanmadı.
- Değişen dosyalar: `tests/reports/p0e-tenant-host-slug-acceptance-20261023.md`, `tests/reports/p0e-tenant-host-slug-acceptance-20261023.log`, `docs/PROGRESS.md`.
- Komut + çıktı: İlgili compose kabul testi exit 0; 3/3 geçti, 1.1070 dakika. `demo3.envantertr.com` Cloudflare A kayıtlarına çözüldü; HTTPS `/api/status` HTTP 200, TLS doğrulaması 0.
- Test: Firma hostu/oturumu eşleşmesi, başka/bilinmeyen tenant hostunda mevcut oturumun reddi, geçerli/ayrılmış/çakışan slug ve kullanıcıya açık şirket seçimi doğrulandı. Canlı DNS/TLS sorgusu salt-okunurdu; canlıda Demo3 oluşturulmadı. Wildcard DNS/TLS ile genel health yanıtı Demo3 tenantının varlığını veya verisini kanıtlamaz. Compose DB/ağı kaldırıldı.
- Kanıt yolu: `tests/reports/p0e-tenant-host-slug-acceptance-20261023.md`, `tests/reports/p0e-tenant-host-slug-acceptance-20261023.log`.
- Sıradaki iş: Gerçek Iyzico sandbox ve canlı SMTP dış kapıları; ardından gerçek üçüncü firma oluşturma/giriş/izolasyon/silme kabulü. Santa/Farma Bölüm 20.2–20.5 işlemleri de tamamlanmadı; canlı checkout boş plan kataloğu nedeniyle kapalı.

### 23 Ekim 2026 — canlı bulut deneme rotası
- Durum: ENGELLENDİ — canlıda cloud trial endpoint'i yayımlı değil; satış ve deneme kaydı canlıda doğrulanmadı.
- Değişen dosyalar: `tests/reports/p0-live-cloud-trial-route-20261023.md`, `docs/PROGRESS.md`.
- Komut + çıktı: Salt-okunur canlı GET: `/api/status` 200 (118/0), `/api/plans` 200 (`[]`), `/api/checkout/status` 200 (`available:false`), `/api/trials/status` 404. Aday kaynakta `MapCloudTrialEndpoints()` var; doğrulanmış canlı `origin/main` SHA `4d55270827be312775ebffe60c5a6d74011d7802` içinde endpoint sınıfı ve eşleme yok. `demo3.envantertr.com` wildcard DNS/TLS ile genel `/api/status` 200 verdi; tenant oluşturulmuş olduğunu göstermez.
- Test: Salt-okunur HTTP ve Git kaynak karşılaştırması; üretim DB/Coolify ayarı değişmedi, deploy yapılmadı.
- Kanıt yolu: `tests/reports/p0-live-cloud-trial-route-20261023.md`, mevcut Coolify SHA kanıtı `tests/reports/coolify-sha-save-triage-20261023.md`.
- Sıradaki iş: Cloud trial rotası için aday PR/release incelemesi; canlıya almadan önce staging kabulü, taze yedek ve rollback provası. Iyzico/SMTP sırları ve yayımlanmış plan olmadığı için checkout kapalıdır.

### 23 Ekim 2026 — P0-E yeni Cloud şirket provisioning kabulü
- Durum: TAMAM (izole PostgreSQL + sentetik SMTP); canlı müşteri kabulü ve e-posta teslimi doğrulanmadı.
- Değişen dosyalar: `tests/reports/p0e-new-company-provisioning-acceptance-20261023.md`, `tests/reports/p0e-new-company-provisioning-acceptance-20261023.log`, `docs/PROGRESS.md`.
- Komut + çıktı: `docker compose -f deploy/testing/compose.security-acceptance.yaml -p p0e-provision run --build --rm acceptance --filter "FullyQualifiedName~Platform_can_provision_a_paid_cloud_company_owner_and_retryable_invitation"` → exit 0; 1/1 geçti, 48.9926 sn.
- Test: Platform admin şirketi ve tenant slug'ını oluşturdu; 365 günlük aktif Cloud aboneliği, owner üyeliği, zorunlu MFA, davet e-postası ve daveti tekrar gönderme doğrulandı; çakışan slug reddedildi. İzole Compose DB/ağı kaldırıldı, canlıya yazma yapılmadı.
- Kanıt yolu: `tests/reports/p0e-new-company-provisioning-acceptance-20261023.md`, `tests/reports/p0e-new-company-provisioning-acceptance-20261023.log`.
- Sıradaki iş: Gerçek üçüncü firma için staging ve canlı müşteri akışı, gerçek SMTP teslimi, kiracı verisi ve silme kapsamı. Canlı rota doğrulanmış `origin/main` sürümünde yok; canlı checkout boş plan kataloğu ve Iyzico/SMTP ayarları nedeniyle kullanılamıyor.

### 23 Ekim 2026 — P0-E bulut denemesi Chromium kabulü
- Durum: TAMAM (yerel sentetik API + Chromium); gerçek SMTP ve canlı deneme rotası doğrulanmadı.
- Değişen dosyalar: `tests/reports/p0e-cloud-trial-browser-acceptance-20261023.md`, `docs/PROGRESS.md`.
- Komut + çıktı: Hosted-checkout browser imajı oluşturuldu; `docker run --rm --init --entrypoint node envantertr-hosted-checkout-browser-acceptance:local cloud-trial.mjs` → PASS. Kapalı form, slug önerisi/özel slug, CSRF/onay tek POST, beş dil, 390px taşmasız görünüm, açık kullanıcı onayı, URL'den kanıt temizliği ve dış portal adresi reddi geçti.
- Test: Gerçek ağ sağlayıcısı ve e-posta kullanılmadı; HTTP istekleri sentetik API'ye yönlendirildi. Üretim verisi/ayarına yazılmadı.
- Kanıt yolu: `tests/reports/p0e-cloud-trial-browser-acceptance-20261023.md`.
- Sıradaki iş: Staging ve gerçek SMTP/Iyzico kabul kapıları; canlı trial route 404 ve published plan kataloğu boş.
### 23 Ekim 2026 — P0-D iyzico ortamının açık seçimi
- Durum: TAMAM (izole ödeme readiness kabulü); canlı ödeme yapılandırması/deploy yapılmadı.
- Değişen dosyalar: `deploy/coolify/compose.yaml`, `deploy/server/compose.yaml`, `deploy/server/.env.example`, `src/EnvanterTR.Web/Data/IyzipayCheckoutClient.cs`, `tests/EnvanterTR.SecurityAcceptance/CheckoutReadinessAcceptanceTests.cs`, `tests/reports/p0d-explicit-iyzipay-environment-acceptance-20261023.md`, `docs/PROGRESS.md`.
- Komut + çıktı: Docker içindeki web derlemesi 0 uyarı/0 hata; kabul derlemesi 4 mevcut ASP.NET deprecation uyarısı/0 hata. Her iki üretim Compose dosyası test değerleriyle `config --quiet` doğrulamasından geçti.
- Test: `Platform_checkout_readiness_matches_public_gate_without_exposing_provider_secrets` → 1/1 geçti. Boş BaseUrl, sandbox ve production durumları; secretsız readiness yanıtı ve kapalı checkout doğrulandı. Test PostgreSQL'i ve ağı izoleydi.
- Kanıt yolu: `tests/reports/p0d-explicit-iyzipay-environment-acceptance-20261023.md`.
- Sıradaki iş: P0-D staging üzerinde gerçek iyzico sandbox kabulü ve P0-E SMTP/üçüncü firma kabulü. Gerekli sağlayıcı sırları, canlı SMTP ve staging dış koşulları olmadan satış açılmaz; üretim deploy kapısı staging kabulü + taze yedek + geri dönüş provasıdır.

### 23 Ekim 2026 — P0 katalog ve tarayıcı sözleşmesi düzeltmeleri
- Durum: TAMAM (iki sentetik API/Chromium akışı ve üç kaynak doğrulayıcısı); tam uzak CI ve canlı müşteri kabulü açık.
- Değişen dosyalar: platform browser fixture, `Verify-AssetModelDetailsUi.mjs`, `Verify-HostedCheckoutBrowserAcceptance.mjs`, `Verify-PlatformAdminExperience.mjs`, denetim/progress ve kabul raporu.
- Komut + çıktı: Üç Node doğrulayıcı geçti; Docker Chromium `platform-admin-members.mjs` ve `asset-detail-actions.mjs` exit 0.
- Test: Cloud dört kullanıcı kotası kaydı ve `4 / 4` gösterimi; model ayrıntısı özet/varlık/sipariş/geçmiş/ekler ve bağlantılı varlığa geçiş doğrulandı. Kaynak doğrulayıcıdaki kırılgan metin mesafesi ayrı davranış assertion'larıyla değiştirildi.
- Kanıt yolu: `tests/reports/p0-catalog-browser-contract-acceptance-20261023.md`.
- Sıradaki iş: P0-D'de şirket oluşturmanın panel/deneme/ödeme yollarında ortak servis şartını uygulamak. PR #25 CI proxy fixture imaj indirmesinde Docker Hub timeout nedeniyle başarısız; yerel proxy ilk `ResponseEnded` sonrası tek yeniden denemede geçti. Staging/gerçek iyzico/SMTP kapıları devam ediyor.

### 23 Ekim 2026 — P0-D tek kiracı oluşturma servisi
- Durum: TAMAM (ortak servis ve izole PostgreSQL kabulü); üretimde yayımlanmadı.
- Değişen dosyalar: `TenantProvisioningService.cs`, Account/Platform/Checkout/CloudTrial endpoint'leri, DI kaydı, provisioning kabul testleri, ilgili iki kaynak doğrulayıcısı ve kanıt raporu.
- Komut + çıktı: Docker web build 0 uyarı/0 hata; kabul build 4 mevcut deprecation uyarısı/0 hata. Hedefli PostgreSQL kabulü exit 0, 11/11 geçti, 2.5126 dakika.
- Test: Şirket ekleme tek ortak metotta; başlangıç kurulumu, panel, deneme ve ödeme yolları servisi çağırıyor. Transaction yoksa işlem reddi; Identity hesabı/şirket/üyelik/aboneliğin birlikte rollback'i; ilk sahip kimlik bilgilerinin korunması; deneme geçerlilik/tekrar/eşzamanlılık, ücretli panel kurulumu, imzalı checkout ve Cloud kullanıcı kotası doğrulandı. Üç kaynak kontrolü geçti.
- Kanıt yolu: `tests/reports/p0d-shared-tenant-provisioning-acceptance-20261023.md`, `tests/reports/shared-tenant-provisioning-20261023.log`.
- Sıradaki iş: PR #25 uzak PostgreSQL koşusu 278/279: tek hata yeni trial rotalarının açık API envanterinde bulunmaması; açık rota listesi ve trial rate-limit sözleşmesi güncellenip doğrulanacak. Staging + gerçek iyzico/SMTP ve Bölüm 20 müşteri kabulü açık.

### 23 Ekim 2026 — P0 trial API envanteri CI hatası
- Durum: TAMAM (yerel hedefli doğrulama); güncel tam uzak CI bekleniyor.
- Değişen dosyalar: `TenantSessionAcceptanceTests.cs`, `docs/DATA-API.md`, `tests/reports/p0-trial-api-auth-boundary-acceptance-20261023.md`, `tests/reports/trial-api-auth-boundary-20261023.log`, `docs/PROGRESS.md`.
- Komut + çıktı: `p0-api-boundary` Compose kabulü exit 0, 2/2 geçti, son koşu 41.6320 saniye. Web build 0 uyarı/0 hata; test build dört mevcut ASP.NET deprecation uyarısı/0 hata.
- Test: Açık API rotalarının tam küme eşitliği ve yetki politikaları korundu; anonim trial rotaları açıkça listelendi, iki yazma ucunda checkout rate-limit metadata'sı zorunlu kılındı. Kayıt özelliği kapalı/CSRF eksik/kanıt geçersiz durumlarında şirket açılmadığı doğrulandı.
- Kanıt yolu: `tests/reports/p0-trial-api-auth-boundary-acceptance-20261023.md`, `tests/reports/trial-api-auth-boundary-20261023.log`.
- Sıradaki iş: Üç iş paketini mevcut taslak PR #25'e gönderip yeni CI'ı izlemek; başarılı tam koşu sonrası staging kabul kapısını hazırlamak. Gerçek iyzico/SMTP, Bölüm 20 müşteri kabulü ve canlı yayın koşulları açık. Önceki hedef turu PR açılması ve testli kod gönderimiyle ilerleme olarak sınıflandırıldı.

### 23 Ekim 2026 — P0 platform davetinde Cloud kullanıcı kotası
- Durum: TAMAM (hedefli PostgreSQL kabulü ve kaynak kontrolü).
- Değişen dosyalar: `Verify-PlatformMemberInvites.mjs`, `CloudSeatQuotaAcceptanceTests.cs`, ilgili log/rapor ve kaynak kontrol JSON'u.
- Komut + çıktı: İzole `p0-invite-quota` Compose kabulü exit 0; 3/3 geçti, 1.1135 dakika. CI'daki 78 ayrı Node kaynak kontrolü, sonraki SMTP paketinden önce 78/78 geçti.
- Test: Tenant ve platform yönetici davetinde dolu Cloud kotası reddi, hesap oluşturmama, kota artışıyla kabul, yeniden dolduğunda ret ve mevcut üyeliklerin korunması; davet yeniden gönderme/e-posta senaryosu geçti. CI'ın eski Cloud muafiyeti bekleyen assertion'ı doğru sınır şartıyla değiştirildi.
- Kanıt yolu: `tests/reports/p0-platform-invite-quota-acceptance-20261023.md`, `tests/reports/platform-invite-seat-quota-20261023.log`, `tests/reports/p0-ci-source-checks-20261023.json`.
- Sıradaki iş: SMTP yapılandırması hatalıyken trial/public readiness ile platform sağlık bilgisinin tutarlı kapanmasını sağlamak ve gerçek e-posta kabulüne hazırlamak. Önceki hedef turu üç testli commit/push ile ilerleme olarak sınıflandırıldı.

### 23 Ekim 2026 — P0-E ortak SMTP yapılandırma doğrulaması
- Durum: TAMAM (yapılandırma ve yakalayıcı regresyon kabulü); gerçek SMTP/inbox teslimi açık.
- Değişen dosyalar: `EmailConfigurationReadiness.cs`, CloudTrial/Platform endpoint'leri, platform sağlık açıklaması, ilgili verifier ve kabul testi/doküman/raporlar.
- Komut + çıktı: Web build 0 uyarı/0 hata; test build dört mevcut deprecation uyarısı/0 hata. İlk koşu 3/3, mevcut admin-only sağlık regresyonu ayrıca 1/1 geçti. SMTP adayında 78 kaynak kontrolü 78/78 geçti.
- Test: On yapılandırmada host, port, gönderici, HTTPS bağlantı, eksik credential çifti ve TLS doğrulaması; açık trial durumu ile sağlık tutarlılığı; bozuk ayarda mail gönderilmeden 503; sırların yanıtta bulunmaması; parola kurtarma/gerçek yakalayıcı trial e-postası ve sağlık yetkisi doğrulandı.
- Kanıt yolu: `tests/reports/p0e-smtp-readiness-acceptance-20261023.md`, SMTP iki logu ve kaynak kontrol JSON'u.
- Sıradaki iş: Ücretli şirket oluşturulduktan sonra ilk davet e-postası gönderilemezse görünür başarısızlık, güvenli teşhis logu ve yeniden gönderimle aynı şirket/üyelik/aboneliğin korunması kabulünü tamamlamak. Staging/gerçek sağlayıcı/SMTP ve Bölüm 20 kapıları açık.

### 23 Ekim 2026 — P0-E ücretli şirket davetinde hata ve yeniden gönderim
- Durum: TAMAM (yakalanmış SMTP ve izole PostgreSQL kabulü); gerçek SMTP teslimi açık.
- Değişen dosyalar: `PlatformEndpoints.cs`, mevcut provisioning kabul testi, log/rapor ve progress.
- Komut + çıktı: Web build 0 uyarı/0 hata; test build dört mevcut deprecation uyarısı/0 hata. `p0-owner-mail` Compose kabulü exit 0, 2/2 geçti, 55.0757 saniye. İlgili iki kaynak kontrolü geçti.
- Test: İlk mail hatasında `201/invitationSent=false` ve görünür mesaj; mail yeniden gönderimi hata durumunda `503`; yapılandırma düzeldikten sonra aynı sahibin daveti gönderiliyor. Tek şirket/owner üyelik/abonelik korunuyor. Güvenli warning yalnız şirket ID ve exception türü içeriyor.
- Kanıt yolu: `tests/reports/p0e-paid-owner-invitation-retry-20261023.md`, `tests/reports/paid-owner-invitation-failure-20261023.log`.
- Uzak kontrol: Önceki `06cbd5d` adayında tam PostgreSQL CI 281/281 geçti. Aynı koşunun Linux kaynak işi eski Cloud kotasızlık assertion'ında başarısızdı; bu assertion `17b54f1` içinde düzeltildi. Yeni SMTP adayı için tam CI ayrıca gerekir.
- Sıradaki iş: Bu turdaki üç testli paketi PR #25'e göndermek; mevcut deneme şirketini/verilerini koruyarak yıllık ücretli aboneliğe geçiren self-servis ödeme açığını kaynak denetimiyle belgelemek ve P0-D kapsamında gidermek.

### 23 Ekim 2026 — P0-D denemeden ücretli aboneliğe geçiş açığı
- Durum: EKSİK — mevcut kaynakta müşteri ödeme dönüşümü bulunmuyor; uygulanmış veya çalışma zamanında geçmiş sayılmadı.
- Değişen dosyalar: `docs/P0-CHECKOUT-GAPS.md`, `docs/PROGRESS.md`.
- Komut + çıktı: Checkout şirket-slug kontrolleri ve endpoint envanteri tarandı. `CheckoutEndpoints.cs:247` mevcut şirket slug'ını başlangıçta reddediyor; `:522` doğrulanmış ödeme sonucunda da mevcut şirketi provisioning hatası sayıyor. Yalnız platform yöneticisi için manuel subscription extend/edit rotaları var.
- Kanıt: `docs/P0-CHECKOUT-GAPS.md` bu iki kod yolunu, eksik mevcut-şirket bağlamını ve doğrudan satın almada ödeme öncesi e-posta doğrulaması açığını listeler. Bu bir kaynak denetimidir; yeni şirket checkout testleri deneme dönüşümünün kanıtı değildir.
- Sıradaki iş: P0-D'de mevcut şirket sahibiyle bağlı, süresi dolmuş denemede de erişilebilir yıllık ödeme; doğrulanmış sağlayıcı sonucuyla aynı şirket/veri/üyelikleri koruyan atomik abonelik geçişi; replay/eşzamanlılık/tenant güvenliği ve tarayıcı kabulü. Gerçek sağlayıcı/SMTP, staging ve Bölüm 20 kapıları ayrıca açık.
