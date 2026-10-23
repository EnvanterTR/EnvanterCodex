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
- Durum: ENGELLENDİ
- Değişen dosyalar: `docs/PROGRESS.md`
- Komut + çıktı: Coolify production projesi 1 kaynak gösteriyor (uygulama); uygulamanın Backups sayfasında `Schedules: 0`, `Enabled: 0`, `Total executions: 0` ve `No scheduled backups` yazıyor.
- Test: Salt-okunur Coolify UI kontrolü. 19 Ekim tarihli eski production dump ve izole restore raporu mevcut; 23 Ekim için taze canlı yedek veya güncel geri dönüş provası kanıtı bulunmuyor. Üretim veritabanına yazılmadı, deploy tetiklenmedi.
- Kanıt yolu: Coolify `/project/laj1kik2jvyedpoxirxvuukc/environment/qsgjulvq6cfxpxpvunw7ua8e/application/izrckbfan6xop8b6xgigduvt/backups`; önceki yedek/restore kanıtı `tests/reports/production-clone-118-rehearsal-20261021.md`.
- Sıradaki iş: P0-B için taze yedek ve izole geri yükleme kabulü gerekir. Coolify'da uygulama harici yedek planı yok; üretim DB'ye dokunmadan önce bağımsız DB yedek yolunun ve geri dönüş provasının sağlanması gerekiyor.
