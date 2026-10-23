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
