## 23 Ekim 2026 — P0-A gerçeklik denetimi güncellemesi
- Durum: ENGELLENDİ (yüzde ve canlı sağlık denetimi tamamlandı; yönetici oturum ekranı kanıtı alınamadı).
- Değişen dosyalar: `docs/AUDIT-REALITY.md`, `docs/PROGRESS.md`.
- Komut + çıktı: Yüzde iddiaları 11 `PROJECT-STATUS.md` + 2 `PRODUCT-COVERAGE.md` + 0 `ROADMAP.md` olarak doğrulandı. `node tools/Check-CoolifyProduction.mjs` tekrarında apex ve firma HTTP 200 `ready` 118/0; canlı plan sayısı 0; checkout `available:false`. PR #25 HEAD `036b52a654da6cda87f7dec15285373303f4f9e5`; Linux güvenlik, Linux PostgreSQL ve Windows deployment/backup CI işleri 3/3 success (run `40008494630`). Aday migration sayısı 119; `origin/main` 118; fark `20261023190000_AddExistingCompanyCheckoutContext` ve model snapshot.
- Test: Bu denetim salt-okunur. Yüzde kod/test izleri `docs/AUDIT-REALITY.md` tablosunda; CI sonucu aynı commit için tam üç işin başarılı olduğunu doğruluyor. Canlı admin girişi yapılmadı; mevcut Windows bilgisayar-kullanım yönergesi auth pencerelerini otomatikleştirmeyi yasakladığından ekran görüntüsü adımı ENGELLENDİ.
- Kanıt yolu: `docs/AUDIT-REALITY.md`; `node tools/Check-CoolifyProduction.mjs` çıktısı; https://github.com/EnvanterTR/EnvanterCodex/actions/runs/40008494630.
- Sıradaki iş: P0-B'de canlı migration kimliklerini doğrulanmış kopya DB ile karşılaştır; tam eşitlik public health endpoint'iyle kanıtlanamıyor. Canlı admin görüntüsü için kullanıcı oturum açmalı veya izinli başka doğrulama yolu sağlanmalı.

### 23 Ekim 2026 — P0-B canlı migration kimlik eşleşmesi
- Durum: TAMAM (dağıtılmış canlı baseline ile `origin/main` birebir eşleşti; aday release migration'ı ayrı ve uygulanmamış).
- Değişen dosyalar: `docs/AUDIT-REALITY.md`, `tests/reports/p0b-migration-identity-acceptance-20261023.md`, `docs/PROGRESS.md`.
- Komut + çıktı: Coolify PostgreSQL konteynerinde yalnız `BEGIN READ ONLY`/SELECT/ROLLBACK sorgusu; canlı 118 ID, `origin/main` 118 benzersiz EF migration ID; uç kimlikler ve sıralı ID-listesi MD5 `c8694e4a153c5f81d5cface1e0c9f366` aynı. `git diff --check` doğrulandı.
- Test: Ek P0-B raporunda canlı ve kaynak kümesi count/range/hash eşitliği; aynı günkü taze dump + medya/anahtar restore eşitliği `coolify-sha-save-triage-20261023.md` ile birlikte. Run `40019065157`, head `217261746e62a5f6debbe6010b71e40a033c7b2d`, Linux tenant security + Linux PostgreSQL + Windows deployment/backup **3/3 success**.
- Kanıt yolu: `tests/reports/p0b-migration-identity-acceptance-20261023.md`, `tests/reports/coolify-sha-save-triage-20261023.md`, GitHub run `https://github.com/EnvanterTR/EnvanterCodex/actions/runs/40019065157`.
- Sınır: PR #25 adayı canlı baseline'dan yalnız `20261023190000_AddExistingCompanyCheckoutContext` ile ileride; `Up()` additive/nullable, canlıya uygulanmadı. Staging uygulaması, yeni release için taze yedek/rollback kapısı ve iyzico/SMTP gerçek kabulü hâlâ gerekir.
- Sıradaki iş: P0-D mevcut şirket checkout'un browser kabulinde provider failure/mismatch/target deletion ve mobil sonucu kapsa; sonra P0-E gerçek sandbox ve Bölüm 20 firma senaryolarına geç. Gerçek staging/provider erişimi olmadan canlı migration/deploy/satış açma.

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

### 23 Ekim 2026 — PR #25 önceki adayın tam uzak CI sonucu
- Durum: TAMAM (yalnız `3c63980536e11d1712706f882696e24de86b03c8` CI kabulü).
- Komut + çıktı: GitHub Actions API run `39987675342`: completed/success; Linux kaynak/güvenlik, Linux PostgreSQL ve Windows dağıtım/yedek güvenliği işleri 3/3 success.
- Kanıt yolu: `tests/reports/p0-pr25-ci-3c63980-20261023.json`, https://github.com/EnvanterTR/EnvanterCodex/actions/runs/39987675342.
- Sınır: Bu sonuç sonradan yazılan mevcut şirket ödeme dönüşümü kodunu, gerçek iyzico/SMTP'yi veya üretim kabulünü kapsamıyor. Taslak PR satışa hazır veya merge edilebilir sayılmadı.
- Sıradaki iş: Mevcut şirket yıllık checkout API/transaction/migration kabulünü tamamlamak; ardından müşteri arayüzü ve tarayıcı kabulü.

### 23 Ekim 2026 — P0-D mevcut şirket için yıllık ödeme API'si
- Durum: TAMAM (hedefli backend kabulü); müşteri ödeme arayüzü/tarayıcı kabulü ve gerçek sağlayıcı açık.
- Değişen dosyalar: `ExistingCompanyCheckoutEndpoints.cs`, checkout/ortak provisioning servisi, sipariş modeli/EF snapshot/additive migration `20261023190000`, write-lock middleware, yeni PostgreSQL kabulü, API/açık işler belgeleri ve raporlar.
- Komut + çıktı: `p0-existing-checkout` Compose kabulü exit 0; 8/8 geçti, 1.7565 dakika. Web build 0 uyarı/0 hata; test build dört mevcut deprecation uyarısı/0 hata. CI'ın 78 kaynak kontrolü 78/78 geçti; model snapshot ile runtime model arasında bekleyen fark yok.
- Test: Altı yeni senaryo aktif/süresi dolmuş deneme dönüşümü, ücretli süreyi koruyan aynı paket yenilemesi, sunucudan türetilen tenant/sahip bağlamı, CSRF/parola/MFA/kota/istek sınırı ve ayrı owner yetkisi reddi, trailing-slash/canonical retry, anonim sipariş kimliği kullanma reddi, ödeme sonrası sahiplik/kota değişimi, sıralı callback replay, eski tarihler ve kendi/diğer şirketin varlıklarının korunmasını kapsar. İki mevcut senaryo subdomain oturum izolasyonu ve tüm korumalı API'larda anonim reddidir.
- İlk başarısızlıklar: 41 karakter test plan kodu DB'deki 40 karakter sınırını aştı; kod kısaltıldı. İkinci koşu 6/7: altıncı istekte gerçek limiter 429 döndürdü, test 403 bekliyordu. Son koşu hem 429'u hem yeni hostta bağımsız 403 owner reddini doğrular; korumalar kaldırılmadı.
- Kanıt yolu: `tests/reports/p0-existing-company-checkout-acceptance-20261023.md`, `existing-company-checkout-20261023.log`, önceki iki hata logu, `p0-existing-checkout-source-checks-20261023.json`.
- Sınır: Paralel callback, hedef şirketin gerçek silinmesi, bu bağlamdaki başarısız/uyuşmayan ödeme, yanlış MFA birikimi/lockout ve tam plan değiştirme yaşam döngüsü ayrıca doğrulanacak. Şirket arayüzü, gerçek iyzico/SMTP, staging/yedek/geri dönüş ve Bölüm 20 tamamlanmadan satışa hazır/canlı kabulü verilmedi.
- Regresyon: Aynı aday imajında mevcut iki public checkout senaryosu ve üretim-baseline schema yükseltmesinde şirket/varlık koruma testi 3/3 geçti; exit 0, 37.7785 saniye. Kanıt `tests/reports/existing-company-checkout-regression-20261023.log`. Yeni migration yalnız izole kabul DB/schema'larında uygulandı.
- Sıradaki iş: Süresi dolan denemeden yıllık satın alma sayfasına geçiş ve form gönderimi uygulandı; sonraki adım gerçek tarayıcı kabulü. Toplam önceki API/DB paketi 11/11 kabul ve 78/78 kaynak kontrolüyle doğrulandı.

### 23 Ekim 2026 — P0-D mevcut şirket yıllık satın alma sayfası
- Durum: TAMAM (sunucu tarafı form, sentetik sağlayıcı ve hedefli PostgreSQL kabulü); browser/gerçek sağlayıcı/staging açık.
- Değişen dosyalar: `ExistingCompanyCheckoutPage.cs`, `ExistingCompanyCheckoutEndpoints.cs`, `CheckoutEndpoints.cs`, `Program.cs`, portal `index.html`, mevcut-şirket checkout kabul testleri; checkout gap, ilerleme, durum ve test raporları.
- Komut + çıktı: `docker compose -p p0-checkout-page -f deploy/testing/compose.security-acceptance.yaml build acceptance` başarılı; web 0 uyarı/0 hata, test derlemesi 4 mevcut ASP.NET deprecation uyarısı/0 hata. `node tools/Verify-UiLocales.mjs` 5 dilde 2.246 kayıt doğruladı; `git diff --check` geçti.
- Test: İlk UI adayı `FullyQualifiedName~Existing_company` 9/9 geçti (2.1029 dk). Callback izolasyon düzeltmesinden sonra aynı izole PostgreSQL kapsamı 10/10 geçti (2.1253 dk); ayrı paralel callback hedefi 1/1 geçti (1.0272 dk). Sayfa/form kabulü: süresi geçmiş denemede sahibi erişimi, İngilizce/koyu tema, paket ve aylık eşdeğer, hassas parola/TOTP'nin yanıtta olmaması, başka rol için 403; CSRF + MFA/parola sonrası 4.200 TRY yıllık sipariş, değişmez şirket/istek sahibi, `PaymentStarted` ve sentetik iyzico. İki eşzamanlı callback de başarı yanıtı döndürüp tek aktif abonelik ve tek audit kaydı bıraktı. Trial/expired/paid dönüşüm, sıralı replay, kapasite/sahip yeniden kontrolü ve tenant session izolasyonu da geçti.
- Kanıt yolu: `tests/reports/company-checkout-page-build-20261023.log`, `company-checkout-page-regression-20261023.log`, `company-checkout-page-concurrent-callback-20261023.log`, `company-checkout-concurrent-callback-20261023.log`, `company-checkout-page-first-build-20261023.log`, `company-checkout-page-acceptance-20261023.md`.
- Sınır: Gerçek iyzico kart/merchant callback'i, interaktif tarayıcı akışı, SMTP/inbox, staging, yeni doğrulanmış yedek ve geri dönüş provası ile Santa/Farma/Demo3 uçtan uca kabulü yapılmadı. Canlı deploy yapılmadı.
- Sıradaki iş: Tarayıcıda bu formun tenant sahibi, hata/başarılı ödeme dönüşü ve mobil görünüm kabulini eklemek; provider failure/mismatch ve target deletion senaryolarını tamamlamak. Mevcut PR #25 taslak adayına yeni değişikliği ekleyip tam uzak CI'ı çalıştırmak.

### 23 Ekim 2026 — checkout sayfası envanter CI düzeltmesi
- Durum: TAMAM (statik envanter yenilendi); önceki head'in CI sonucu başarısız, düzeltmelerden sonraki head CI bekleniyor.
- Değişen dosyalar: `docs/ui-inventory.md`, `docs/PROGRESS.md`.
- Komut + çıktı: İlk yeni PR CI'da `Verify the static UI inventory matches the source` adımı başarısızdı; kök neden checkout bağlantısı eklendikten sonra üretilmiş statik envanterin eski kalması. `node tools/Generate-UiInventory.mjs` 2.269 kontrol/65 rota/58 bağlantı/955 düğme yazdı; `node tools/Generate-UiInventory.mjs --check` geçti.
- CI: Run `40003911832` iptal edildi. Sonraki head `64b863c0671458b5ffe62af64dc9bcbdaff9b511` için run `40004284465`: Windows deployment/backup ve Linux PostgreSQL işleri başarılı; Linux tenant güvenlik işi `Run hosted Cloud checkout in Chromium` adımında başarısız. İlk Chromium hatası İngilizce süre dolumu bildiriminin beklenen açılış cümlesinin değişmesiydi; beş dildeki cümle başlangıçları geri yüklendi ve yerel aynı hosted Chromium imajı exit 0 ile tamamlandı. Yeni commit'te tam GitHub CI yeniden çalışmalı.
- Kanıt: `docs/ui-inventory.md`; GitHub run `https://github.com/EnvanterTR/EnvanterCodex/actions/runs/40003911832`.
- Sıradaki iş: Chromium yerel kanıtını ve callback yarış düzeltmesini PR #25 taslak dalına itip yeni head revision için 3/3 CI sonucunu doğrulamak.

### 23 Ekim 2026 — P0-D eşzamanlı ödeme callback'i
- Durum: TAMAM (izole PostgreSQL kabulü); yeni remote CI henüz çalıştırılmadı.
- Değişen dosyalar: `CheckoutEndpoints.cs`, `ExistingCompanyCheckoutAcceptanceTests.cs`, checkout kabul raporları ve ilerleme/durum/gap belgeleri.
- Hata ve düzeltme: `SERIALIZABLE` transaction altında aynı mevcut şirket siparişine iki paralel callback PostgreSQL `40001 could not serialize access due to concurrent update` üretti. Mevcut şirket yolu aynı sipariş için `FOR UPDATE` ve ortak provisioning servisindeki şirket satır kilidiyle `READ COMMITTED` kullanır; kilidi bekleyen callback tamamlanmış sipariş durumunu görüp başarı verir. Yeni şirket checkout yolu `SERIALIZABLE` kalır.
- Komut + çıktı: web build 0 uyarı/0 hata, test derlemesi 4 mevcut deprecation uyarısı/0 hata. İzole `p0-checkout-page` PostgreSQL'de paralel callback testi 1/1; bütün existing-company kapsamı 10/10 geçti. Hosted Chromium acceptance imajı exit 0 ile tamamlandı; aynı koşunun tam çıktısı `tests/reports/company-checkout-hosted-chromium-20261023.log` dosyasına alınıyor. 2.246 locale ve 2.269 UI kontrolü doğrulandı; `git diff --check` log sonu whitespace temizliği sonrası tekrarlanacak.
- Kanıt yolu: `tests/reports/company-checkout-page-concurrent-callback-20261023.log` (ilk race), `company-checkout-concurrent-callback-20261023.log` (düzeltme 1/1), `company-checkout-page-regression-20261023.log` (10/10).
- Sıradaki iş: Kanıt loglarının satır sonlarını normalize edip paketi PR #25 taslak dalına eklemek; yeni head'de 3/3 CI doğrulamak.

### 24 Ekim 2026 — P0-D mevcut şirket sağlayıcı güvenlik kapıları
- Durum: TAMAM (hedefli izole API/veritabanı kabulü); tarayıcı/gerçek sağlayıcı/staging hâlâ açık.
- Değişen dosyalar: `ExistingCompanyCheckoutAcceptanceTests.cs`, `docs/P0-CHECKOUT-GAPS.md`, bu ilerleme kaydı ve `tests/reports/p0d-existing-company-provider-guards-acceptance-20261024.md`.
- Komut + çıktı: Mevcut şirket checkout hedefli Compose kabulü 7/7 geçti; exit 0, 1.683 dakika. `git diff --check` geçti.
- Kapsam: Yanlış para birimi sağlayıcı sonucu, başarısız/erişilemeyen sağlayıcı sonucu, callback öncesi şirket devre dışı bırakma ve hedef şirketi callback öncesi gerçekten silme. Hiçbir senaryoda abonelik etkinleşmiyor; silinen kiracı yeniden oluşturulmuyor. Başarısız sağlayıcı sonucu güvenli biçimde 503 veriyor.
- Kanıt: `tests/reports/p0d-existing-company-provider-guards-acceptance-20261024.md`.
- Sınır: Sentetik sağlayıcı ve izole API/DB; interaktif browser, gerçek iyzico/SMTP, staging/geri dönüş ve tam plan değişimi yaşam döngüsü kanıtı değildir.
- Sıradaki iş: Mevcut şirket checkout için gerçek sunucu-rendered sayfa/form tarayıcı kabulünü tamamlamak; değişiklikleri PR #25'e gönderip son head CI'ını doğrulamak.

### 24 Ekim 2026 — P0 canlı satış yolu güncel gözlemi
- Durum: TAMAM (salt okunur canlı kontrol); satış yolu kullanılamıyor.
- Kanıt: `tests/reports/p0-live-sales-readiness-probe-20261024.md`; `GET /api/status` 200 ve 118/0 migration, `GET /api/plans` boş dizi, `GET /api/checkout/status` `available=false`, `GET /api/trials/status` 404.
- Sonuç: Migration hazır cevabı aday checkout/trial kodunun canlıya dağıtıldığını kanıtlamıyor. Canlıya mutasyon yapılmadı.
- Sınır: Bu public probe SMTP gerçek teslimatını doğrulamaz. Plan yayınlama, provider/SMTP doğrulaması, staging, yeni yedek ve rollback provası açık.
- Sıradaki iş: Checkout tarayıcı kabulünü ve Hedef Bölüm 20 müşteri senaryolarını ilerlet; bu kapılar geçmeden üretime dağıtma.

### 24 Ekim 2026 — P0 Bölüm 20.4 tenant sınırı kabulü
- Durum: TAMAM (sentetik iki şirket/izole PostgreSQL); gerçek Santa/Farma kabulü açık.
- Test: `Role_downgrade_revocation_and_company_suspension_take_effect_on_the_next_request`, `Asset_single_edit_is_tenant_scoped_version_checked_and_written_to_history`, `Existing_company_sessions_cannot_be_reused_on_another_tenant_subdomain` — **3/3**, exit 0, 2.1753 dakika.
- Kanıt: `tests/reports/p0-section20-tenant-isolation-acceptance-20261024.md` ve `p0-section20-tenant-isolation-20261024.log`.
- Sınır: Cross-tenant varlık okuma/yazma 404; başka tenant subdomain'inde oturum 401. Sentetik kabul canlı Santa/Farma, üçüncü şirket veya Bölüm 20'nin kalan iş akışlarını kanıtlamaz. Canlı veriye dokunulmadı.
- Sıradaki iş: Gerçek/interactive checkout browser akışı ve müşteri E2E; mevcut Windows tarayıcı bilgisayar-kullanım aracı URL politikası hatası verdiği için browser incelemesi yapılamadı. Kullanılabilir başka güvenli kabul yüzeyiyle devam et.

### 24 Ekim 2026 — P0 Bölüm 20.5 platform admin güvenlik alt kabulü
- Durum: KISMİ (izole kabul); tam Bölüm 20.5 açık.
- Test: `Company_member_and_platform_admin_boundaries_are_enforced_over_http`, `Authenticator_two_factor_requires_second_step_and_recovery_codes_are_single_use`, `Platform_admin_first_login_accepts_password_then_requires_mfa_setup_before_panel` — **3/3**, exit 0, 2.9147 dakika.
- Kanıt: `tests/reports/p0-section20-platform-admin-acceptance-20261024.md` ve `p0-section20-platform-admin-20261024.log`.
- Doğrulanan: Platform rolüyle en az iki şirket görülebiliyor; müşteri tenant yöneticisi platform API'sinden 403 alıyor; ilk platform girişi MFA kurulumu istiyor; MFA olmadan platform overview 428 veriyor; TOTP ve kurtarma kodları testleri geçiyor.
- Açık: Hedef Bölüm 9.5 / 20.5'teki “Panele gir” impersonation, sarı banner, impersonation audit izi ve platforma geri dönüş mevcut aday kaynakta bulunmadı. Canlı platform/sirket oturumu da kabul edilmedi.
- Sıradaki iş: Güvenli impersonation tasarımını ve acceptance kapsamını tamamla; ardından gerçek hesap akışı ve P0-E SMTP/Bölüm 20 kalan adımları.

### 24 Ekim 2026 — P0 hosted Chromium kabul paketi
- Durum: TAMAM (sentetik hosted Chromium suite); mevcut şirket checkout gerçek kullanıcı akışı, gerçek sağlayıcı ve canlı kabul açık.
- Değişen dosyalar: `tests/reports/p0-hosted-browser-acceptance-20261024.md`, `tests/reports/p0-hosted-browser-acceptance-20261024.log`, `docs/PROGRESS.md`.
- Komut + çıktı: `deploy/testing/run-hosted-checkout-browser-acceptance.ps1` → exit 0; **74/74 PASS**. Chromium imajı `mcr.microsoft.com/playwright:v1.64.0-noble`.
- Test: Checkout ödeme kapalıyken kişisel/fatura verisi göndermeme; fiyatı sunucu kotasıyla gösterme; Farma/Santa/Celal slug önerileri ve elle düzenlenmiş slug'ı koruma; CSRF, veri işleme onayı, hassas tutar/kart verisini istemci gövdesine koymama ve yalnız güvenilir sağlayıcıya yönlendirme. Aynı hosted paket diğer şirket portalı, platform UI, varlık, rapor, lisans, satın alma, destek ve zamanlanmış rapor akışlarını da kapsar.
- Kanıt yolu: `tests/reports/p0-hosted-browser-acceptance-20261024.md` ve tam çıktı `.log` dosyası.
- Sınır: Sentetik API/iyzico; gerçek e-posta veya tahsilat yok. Genel yeni şirket checkout testi, `/subscription/checkout` mevcut firma satın alma/dönüş akışını kanıtlamaz. Santa/Farma gerçek hesapları, staging, taze yedek/rollback ve canlı ayar kabulü değildir.
- Sıradaki iş: Checkout P0-D için mevcut şirket browser dönüş akışını ayrı kabul et; ardından P0-E SMTP/Bölüm 20 müşteri senaryolarına ve kalan platform impersonation kabuline ilerle. Üretime yayın kapıları geçmeden canlıya deploy etme.

### 24 Ekim 2026 — P0-D mevcut şirket gerçek uygulama tarayıcı kabulü
- Durum: TAMAM (gerçek Kestrel + PostgreSQL + Chromium; sentetik iyzico); gerçek sağlayıcı/canlı kabul açık.
- Değişen dosyalar: `tests/EnvanterTR.CheckoutBrowser`, browser senaryosu, Dockerfile/Compose/PowerShell runner, CI adımı/kanıt upload'u, checkout gap ve ilerleme belgeleri.
- Komut + çıktı: `deploy/testing/run-existing-checkout-browser-acceptance.ps1` → exit 0; **1/1 uçtan uca senaryo geçti**. Derleme 0 hata/0 uyarı. `git diff --check` doğrulandı.
- Test: Gerçek parola/MFA/cookie girişi, süresi dolmuş denemede 4.200 TRY yıllık / 350 TRY aylık eşdeğer, İngilizce/koyu/390px; yanlış parolada 400, fatura koruma ve gizli alanları temizleme; gerçek CSRF formu, güvenilir 302 Location; callback/replay, aktif ödeme geçmişi; ayrı viewer oturumunda 403. DB: tek sağlayıcı initialize ve tek aktif abonelik; şirket UUID'si, iki üyelik, deneme geçmişi ve iki firmanın varlıkları korunuyor.
- İlk koşular: Onaysız test callback URL'si formu doğru olarak kapattı; test yapılandırması düzeltildi. Sonraki koşu izole ağdan sağlayıcıya yönlendirmede durdu; gerçek Location doğrulanıp yalnız testte yerel sağlayıcı sayfasıyla devam edilecek şekilde browser interception düzeltildi. İki hata logu saklandı; ürün güvenlik kontrolü kaldırılmadı.
- Kanıt: `tests/reports/existing-company-browser-acceptance-20261024.md`, son/ilk/redirect logları ve `existing-company-browser-20261024/checkout-390.png`. Görsel incelendi. Geçici Compose DB/ağ temizlendi.
- Sınır: Gerçek iyzico tahsilatı/e-posta yok; staging, taze yedek/rollback, tam ücretli plan değişimi ve canlı Santa/Farma/Demo3 kabulü açık. Canlı deploy yapılmadı.
- Sıradaki iş: PR #25'te uzak CI doğrulaması; P0-D e-posta doğrulama açığı ve gerçek sağlayıcı, ardından P0-E SMTP/Bölüm 20 kalan adımları.

### 24 Ekim 2026 — P0-D checkout kullanıcı kotası gösterimi
- Durum: TAMAM (yerel ürün düzeltmesi + Chromium kabulü); yeni PR CI/canlı kabulü açık.
- Değişen dosyalar: public `checkout.html/js`, hedefli Chromium senaryosu ve mevcut hosted suite/Docker bağlantısı; kabul raporu ve loglar.
- Hata: 4 kullanıcılı yayımlı paket sipariş özetinde `Sınırsız` görünüyordu; düzeltme öncesi Chromium exit 1 ile yeniden üretildi. Açıklaması olmayan paket de aynı yanlış ifadeyi gösteriyordu.
- Düzeltme: Pozitif tamsayı API kotası gösterilir; yalnız açık `null` sınırsızdır. Eksik/geçersiz kota `—`, boş açıklama boş kalır.
- Test: 1/4/10/2000/null/eksik × 5 dil = **30/30**, exit 0; sıfır ödeme POST'u ve browser JS hatası. Ara testte Node/Chromium ICU ayırıcı farkı yakalandı; sayı değeri bağımsız doğrulandı ve hata logu saklandı. Hosted checkout kaynak doğrulayıcıları ve UI inventory check (2.269 kontrol) geçti. Docker .NET acceptance build exit 0 (değişmeyen C# katmanları cache); `git diff --check` geçti.
- Kanıt: `tests/reports/checkout-user-quota-acceptance-20261024.md`, before/after/ICU/build logları.
- CI: Önceki `dfdb36e` run `40035243704` Windows başarılı; iki Linux işi son kontrolde çalışıyor. Bu paket önce yerel commit olur; devam eden CI'ı iptal etmemek için sonraki toplu PR güncellemesine bırakılır.
- Sıradaki iş: P0-D doğrudan satın almada ödeme öncesi e-posta doğrulaması; aktif CI sonucu ve gerçek iyzico/SMTP kabulü. Canlıya yayın yapılmadı.

### 24 Ekim 2026 — P0-D ödeme öncesi e-posta doğrulaması
- Durum: TAMAM (yerel kod + API/PostgreSQL/SMTP capture/Chromium kabulü); gerçek servis/canlı kabulü açık.
- Değişen dosyalar: `CheckoutEmailEndpoints.cs`, checkout sözleşmesi/başlatma/hazırlık, ayrı e-posta istek sınırı, checkout HTML/JS, API ve browser kabulleri, tek komutlu runner, API/envanter/checkout gap ve kanıt belgeleri.
- Davranış: 30 dakikalık korumalı e-posta/paket kanıtı yalnız e-postaya gider; CSRF ve açık onay gerekir. Fragment kaldırılır, anahtar yalnız sayfa belleğinde kalır. Kanıtsız/bozuk/yanlış e-posta-paket/amaç/süresi dolmuş istek ödeme veya sipariş oluşturmaz (428). SMTP ayarı kapalıysa eski geçerli kanıtla dahi yeni şirket ödemesi başlamaz (503). Mevcut doğrulanmış owner parola/MFA akışı korunur.
- Komut + çıktı: `deploy/testing/run-checkout-email-acceptance.ps1` → **15/15**, exit 0, 5.9858 dakika. Web build 0 hata/0 uyarı; test build 0 hata ve önceki dört ASP.NET deprecation uyarısı. Hedefli gerçek Chromium checkout → exit 0; kota regresyonu 30/30. Kaynak doğrulayıcıları, 2.272 kontrol envanteri ve 2.246 locale kontrolü geçti; `git diff --check` geçti.
- Test: Gerçek yerel SMTP capture; geçerli kanıt/replay; eksik/yanlış/bozuk/expired/purpose reddi; CSRF; SMTP eksikliği/bağlantı hatası/ayar kaldırılması; ayrı rate-limit; API yetki envanteri; mevcut ödeme/provisioning ve owner formu. Browser: çift e-posta gönderimini engelleme, başarısız gönderimden tekrar, yanlış bağlantı, aynı sekme fragment'i, geç gelen eski onayı reddetme, e-posta değiştirme, ödeme sırasında expiry ve bilgi kaybetmeden tekrar doğrulama.
- İlk hatalar: İlk API koşusu 6/12; yeni testlerin global boş sipariş varsayımı diğer fixture siparişlerini gördü. Kontroller ilgili checkout kimliğine bağlandı; diğer veriler silinmedi. İlk browser koşusu aynı sekme hash değişimini yakaladı; hashchange/stale yanıt koruması eklendi. Hata logları saklandı.
- Kanıt: `tests/reports/checkout-email-verification-acceptance-20261024.md`, final API/browser/build/kota ve ilk hata logları. Geçici DB/ağlar temizlendi. Script URL'si yeni doğrulama protokolü için versiyonlandı.
- CI: Önceki `dfdb36e` run `40035243704` **3/3 success**; kanıt `pr25-ci-dfdb36e-20261024.json`. Yeni kota/e-posta commit'leri için ayrıca uzak CI gerekir.
- Sınır/sıradaki iş: Gerçek inbox/iyzico, staging, taze yedek/rollback, Santa/Farma/Demo3 ve platform impersonation kabulü açık. Yeni paketi PR #25'e gönderip tam CI'ı takip et; sonra P0-E ve kalan dış servis kabulünü ilerlet. Canlı deploy yapılmadı.

### 24 Ekim 2026 — P0-E / 20.5 platform destek oturumu
- Durum: TAMAM (yerel uygulama ve hedefli kabul); tam yeni CI, staging ve canlı kabul açık.
- Değişen dosyalar: süreli UserSession metadata/migration/snapshot; PlatformImpersonation API/middleware; session/logout; yönetici üye eylemi ve gerekçe penceresi; müşteri sarı bandı/dönüş; API/Chromium testleri, runner, envanter ve belgeler.
- Davranış: MFA + 10 dakikalık step-up; gerçek müşteri rolü ve tek şirket; en fazla 15 dakika; HttpOnly tek kullanımlı dönüş ve asıl oturum yetkisi kontrolü; gerçek yöneticiye bağlı başlangıç/istek/sonuç/bitiş audit'i. Çıkış yöneticiye döner ve sonraki kritik yazma yeniden doğrulama ister.
- Komut/çıktı: Docker .NET Web build 0 hata/0 uyarı; test build 0 hata, önceki dört deprecation uyarısı. Gerçek Identity/PostgreSQL kabulü 13/13, exit 0 (2.2914 dakika). Chromium müşteri ve yönetici kabulü exit 0; sarı bant ekran görüntüsü incelendi. 2.279 kontrol envanteri, 2.249 locale ve ilgili kaynak doğrulayıcıları geçti.
- İlk hatalar: yeni test reauth alanları/yanıt beklentisi ve olmayan kategori yolu düzeltildi; browser fixture eksik üye id alanı gerçek DTO'ya eşlendi. İlk hata logları saklandı; kontroller kaldırılmadı.
- Kanıt: tests/reports/platform-impersonation-acceptance-20261024.md ve bağlı log/görsel. Geçici DB/ağ temizlendi. Önceki 4e4f255 CI run 40040576315 3/3 success; yeni commit CI'ı ayrıca gereklidir.
- Sınır/sıradaki iş: Hedef 20'nin gerçek Santa/Farma/Demo3 kabulü, SMTP/iyzico, staging, güncel doğrulanmış yedek/rollback açık. Canlı migration/deploy yapılmadı. PR #25'e gönder; tam CI ve dış ortam kapılarını ilerlet.

24 Ekim CI takibi: 54a0600 run 40045415095 Linux kaynak kontrolü Verify-AuthErrorFlows üzerinde başarısız oldu. Yerel tekrar, yeni destek bandını tanımayan VM fixture seçicisini gösterdi. Fixture güncellendi; 401 sonrası destek kimliğinin sıfırlanması ve bandın gizlenmesi ayrıca doğrulanıyor. CI içindeki 79 Node kaynak komutunun tamamı yerelde geçti (platform-impersonation-all-source-20261024.log); hedefli auth-flow logu da başarılı. Ürün davranışı değiştirilmedi. Yeni commit için tam CI gerekir; eski run sırasında iki diğer iş çalışıyordu.

### 24 Ekim 2026 — P0-B güncel 118 → 120 veri koruma kabulü
- Durum: TAMAM (izole kabul); gerçek yedek/restore, staging ve canlı kabul açık.
- Değişen dosyalar: CurrentReleaseMigrationAcceptanceTests.cs, tek komutlu migration kabul runner ve kanıt belgeleri. Ürün/migration kodu değişmedi.
- Test: Tarihsel 118 kimlik listesi MD5 eşitliği ve iki yeni kimlik; public fallback olmadan rastgele schema; sentetik şirket/kullanıcı/plan/oturum/checkout/varlık; tüm özgün tablo alanı/satır projeksiyonları; idempotent SQL ilk/tekrar eşitliği; eski oturum ve checkout yeni alanları null. 1/1 exit 0, 41.9429 saniye; test build 0 hata ve önceki dört uyarı.
- İlk hata: sentetik sipariş 0 tutarla mevcut Amount check constraint kuralına takıldı; planla uyumlu 2400 tutar/ara toplam kullanıldı. Ürün kuralı değişmedi. İlk log saklandı, satır sonu boşlukları temizlendi.
- Kanıt: tests/reports/current-release-migration-acceptance-20261024.md; first/second logları ve salt-okunur production kontrolü. Apex/firma 118/0 ready; kaynak 120; canlı plan boş, checkout kapalı.
- Sınır: Bu sentetik izole schema, güncel üretim yedeğinin staging provasının yerine geçmez. SSH anahtarlı erişimi reddedildi; kullanıcıdan yönetim bağlantısı bilgisi istendi. Canlı değişmedi.
- CI: ba39391 run 40045829382 Windows ve Linux güvenlik işleri başarılı, PostgreSQL paketi sürüyor. Yeni migration kabulü henüz bu CI head içinde değil; mevcut canlı koşuyu sırf zaman doldu diye yeniden başlatmadan yerel commit hazırlanıyor.
- Sıradaki: devam eden tam CI sonucu; bu kabulü PR dalına gönder; erişim sağlanınca taze SQL/medya/anahtar yedeği, gerçek restore/staging ve SMTP/iyzico/Bölüm 20 kabullerini yürüt.

### 24 Ekim 2026 — P0-B somut release SQL dosyası
- Durum: TAMAM (dosya/paket/izole kabul); gerçek üretim restore/staging açık.
- EF CLI 10.0.0 ile 118 → 120 idempotent SQL ve SHA-256 dosyası hazırlandı. Test artık dosya hash'ini ve güncel EF çıktısıyla metin eşitliğini doğrulayıp dosyanın kendisini ilk/tekrar uygular. Test çıktı kopyası ve Docker COPY bağlantısı eklendi.
- Kabul: run-current-release-migration-acceptance.ps1 güncel image ile 1/1, exit 0; 56.3684 saniye. Web build 0 hata/0 uyarı; test build önceki dört uyarı. İlk startup-project, BOM ve izole schema normalizasyon hataları düzeltilip saklandı; kontroller kaldırılmadı.
- Kanıt: current-release-migration-acceptance-20261024.md; SQL/checksum ve exact-sql-packaged-verified logu. Geçici DB/ağ temizlendi.
- CI: ba39391 run 40045829382 üç işte 3/3 success; pr25-ci-ba39391-20261024.json. Bu yeni dosya/paket testinin tam CI sonucu ayrıca gerekir.
- Erişim yeniden doğrulandı: IAB Coolify Terminal sekmesinde açık yönetici oturumu var; localhost sunucu terminaliyle docker ps salt-okunur çalıştı. Yeni credential oluşturulmadı. Kullanıcıdan tekrar bağlantı istemeden mevcut oturumla ilerle.
- Sıradaki: PR dalını güncelle; mevcut Coolify oturumunda taze SQL/medya/anahtar/imaj yedeği, ayrı restore ve staging; ardından tam gerçek müşteri/SMTP/iyzico kabulü. Canlı DB/ayar/deploy henüz değişmedi.

## 24 Ekim 2026 — gerçek yedek, geri dönüş ve staging

Mevcut Coolify oturumuyla erişim sağlandı; SSH/API erişimi engeli bu çalışma için çözüldü. Taze 142 MB geri dönüş paketi doğrulandı; gerçek dump ayrı DB'ye geri yüklendi (95 tablo sayımı eşit). 118→120 exact SQL ve tekrarında 94 veri tablosunun özgün alan/satırları korundu. Aday staging imajları derlendi; yalnız internal ağda, dış port olmadan 11 temel HTTP/güvenlik kontrolü geçti. ae875b6 CI 3/3 success (40073522201). Canlı 118/0 olarak kaldı; dağıtım yapılmadı. Tam kimlik doğrulamalı müşteri/provider/Bölüm 20 kabulü açık. Kanıt: tests/reports/real-server-staging-acceptance-20261024.md ve altı Coolify ekran görüntüsü.

### 24 Ekim 2026 — gerçek satış kapıları ve canlı oturum
- Durum: KISMİ; gerçek SMTP/iyzico ve kimlik doğrulamalı Bölüm 20 açık.
- Gerçek staging checkout available:false, trial available:false (15 gün/yıllık), plan listesi boş. Canlı runtime SMTP host/kullanıcı/parola/gönderici ve Iyzipay anahtarları boş/yok; gizli değer çıktısı alınmadı. Kaydedilmiş tüm Coolify ayarları denetlenmiş sayılmaz.
- Medya/anahtar arşivleri mevcut canlı volume dosyalarıyla tekrar eşleşti; özel migration kanıt arşivi geçerli. Staging üç süreç çalışıyor. ae875b6 tam CI 3/3 success; JSON kanıtı kaydedildi.
- Farma parolası doğrulandı, MFA istendi; kullanıcı giriş yaptığını bildirse de kontrol edilen sekme MFA ekranında. Giriş/kapsam testi geçti denmedi; kurtarma kodu kullanılmadı, MFA değiştirilmedi.
- Kanıt: tests/reports/live-auth-sales-followup-20261024.md. Canlı deploy/migration ve satış açılmadı.
