# Devam notu — 2026-11-03

## Son tamamlanan çalışma paketi

WP-006–WP-019 tamamlandı. WP-012–WP-017'de `origin/main` uzlaştırması, checkout ve audited platform impersonation backend/UI slice'ları işlendi; ayrıntılar çalışma ledger'ında. Canlı dağıtım SHA'sından türetilen temiz aday `release/v1-candidate-20261102` (`1a676b2ae2dce9416fdd918ba8c26472f49ec316`) tam hosted Chromium kabulinden exit 0 ile geçti ve gerçek canlı yedeğindeki 122 migration ID'siyle birebir eşleşti. WP-018 geri alınabilir şirket silme dilimi 9 dosyada taşındı; Docker Release build 0/0 ve hedefli PostgreSQL kabulü 4/4 PASS. WP-019 handover e-posta deneme tablosu/migration/modelini geri getirdi; birleşik preview dalı canlı kaynak ID'leriyle artık 122/122 eşleşiyor. EF pending-model kontrolü temiz; yedekten restore edilmiş ağsız klonda hedefe `database update` no-op oldu, değişiklik uygulamadı. Handover gönderim/PDF endpointleri henüz taşınmadı. Birleşik preview dalı deploy edilmedi; CI/tam regresyon, canlı firma kabulü ve SMTP teslimatı hâlâ ayrı kanıt kapılarıdır.

Kanıt: `tests/reports/live-derived-candidate-hosted-chromium-20261102.md`, `tests/reports/live-derived-candidate-security-20261102.md` ve `tests/reports/summary.json`.

Canlı deploy veya production DB yazımı yapılmadı. Browser kabul koşucusu sentetik API verisi kullandı. Sonraki iş: `EmployeeHandoverEmailAttempt` migration/source parity ve birleşik dalın canlı ID setiyle tüm farkları; ardından F1 için aday build/CI ve izole gerçek-yedek migration/rollback provası. Uygulama release'i ayrıca onay gerektirir.

## Gerçek kalan kapılar

1. F1/WP-007 ve WP-008 tamamlandı. Coolify arşivi `daily-20261102T122121Z.dump` (464,439 bytes); SHA-256 `331eb93292dfb97fbb780007a6a39c86e7c25537f109df97d63344dc8911dd55` yerelde manifestle eşleşti. `pg_restore --list` ve gerçek yedeğin ağsız PostgreSQL 17 klonuna restore PASS: 96 public tablo, 122 migration, 3 şirket, 1 varlık. Yedekten okunan tam `__EFMigrationsHistory` ID listesi temiz aday `1a676b2ae2dce9416fdd918ba8c26472f49ec316` kaynak migration ID'leriyle 122/122 eşleşti; iki yönde fark yok. Adayda pending migration yok. Latest→latest EF no-op script, tekrar ve untouched baseline'dan rollback clone row-count karşılaştırması PASS. Üretim DB'sine yazılmadı. Kanıt: `tests/reports/live-backup-restore-rollback-20261102.md`.
2. Canlı-baseline temiz release candidate `1a676b2ae2dce9416fdd918ba8c26472f49ec316` yedekteki 122 migration ID'siyle birebir eşleşiyor. WP-018 sonrası birleşik preview branch'inde 121 ID var; canlıdan tek eksik `20261026083058_AddHandoverEmailAttempts`, preview-only ID yok. `SubscriptionCoupon`, `ExistingCompanyCheckoutContext`, `PlatformImpersonationSession` ve recoverable company deletion migration'ları artık kaynakta mevcut. Handover email entity/endpoint/migration/test dilimi parity'yi tamamlayacak; sonra canlı yedek klonunda migration history, schema/row count ve no-op uygulama tekrar doğrulanmalı. Bu klon provası canlı deploy veya production DB yazımı değildir. Eski full-history EF idempotent SQL üretim kusuru `20261010215527_AddAssetModelNumbersAndDefaultLocation` kaynak SQL'indeki eksik ayraçlar nedeniyle ayrı bir kısıt olarak kalır; uygulanmış migration dosyası değiştirilmemeli.
3. SMTP gerçek alıcı teslimi ve Santa/Farma/Demo3 canlı kabulü ayrıca kanıtlanmalı.
4. İyzico gerçek ödeme kapısı şirket kuruluşu ve onaylı işyeri hesabı olmadan kapalı kalır.

Sonraki WP: signed zimmet PDF indirme ve açık onaylı SMTP gönderim endpointlerini, idempotency/çift gönderim korumasını, rate-limit ve hedefli güvenlik kabul testlerini seçerek uyarlamak; sonra müşteri UI akışını eklemek. Model/migration parity kanıtı `tests/reports/handover-email-migration-clone-parity-20261103.md` içinde. `docs/PROJECT-STATUS.md` tek durum tablosudur; `docs/LEDGER.md` çalışma paketlerini, `tests/reports/summary.json` makine kanıtını tutar. Yüzde raporlama.

## F1 geri dönüş prosedürü

### Mevcut canlı-baseline aday (122/122 migration, pending yok)

1. Yayın öncesi Coolify'de mevcut çalışan uygulama SHA/image bilgisini ve son başarılı deployment kaydını not et. Yayımlanacak SHA için Release build, CI ve canlı-baseline migration eşitliği PASS değilse devam etme.
2. Yayın öncesi taze PostgreSQL yedeği al; manifest SHA-256'sını doğrula ve `pg_restore --list` ile arşiv okunabilirliğini kontrol et. Geri dönüş için önceki app SHA'sını sakla.
3. Bu adaydaki 122 migration ID'sini yedekteki `__EFMigrationsHistory` ile birebir karşılaştır. Eşitlik ve 0 pending doğrulanırsa DB migration komutu çalıştırma; yalnız uygulama image/SHA'sını yayımla.
4. Yayın sonrası `/api/status`, apex ve firma portalı HTTPS yanıtı, giriş sayfası ve temel tenant yönlendirmesini kontrol et. Beklenmeyen hata veya sağlık düşüşünde Coolify'de uygulamayı not edilen önceki değişmez SHA/image'a geri al; DB'ye dokunma. Önce sağlık kontrolünü tekrarla, ardından login/tenant temel akışını doğrula.

### Yeni migration içeren gelecek sürüm

1. Uygulanmış migration dosyalarını değiştirme. Adaya yeni migration ekle; yedekten alınmış izole klonda boş şema ve gerçek yedek şeması üzerinde yükseltme, tekrar uygulama, veri koruma ve rollback/restore provası PASS olmadan yayına geçme.
2. Yayın öncesi yeni taze yedek al; hash, `pg_restore --list`, migration kaynağı ve beklenen başlangıç/bitiş ID'lerini kaydet. Migration SQL'ini yalnız doğrulanmış başlangıç migration ID'sinden hedef ID'ye üret; full-history script mevcut `20261010215527_AddAssetModelNumbersAndDefaultLocation` sözdizimi bulgusu çözülmeden kullanılmamalı.
3. Migration sonrası uygulama uyumsuzsa önce uygulama SHA'sını geri al. Geri alınan uygulama yeni şemayla güvenli çalışmıyorsa yazmaları durdur ve sahibi bilgilendir. DB'yi yedekten geri yükleme veri kaybı doğurabileceğinden yalnız sahibin açık onayıyla, planlı kesinti penceresinde yapılır; önce aynı yedeğe izole restore prova et.
4. Geri dönüşten sonra migration geçmişi, temel satır sayıları, `/api/status`, portal girişi ve tenant sınırlarını doğrula; sonuçları aynı release kaydına ekle.

Mevcut F1 provası uygulama rollback klonunu ve geri yüklenmiş baz satır sayılarını doğruladı; bu, henüz canlı rollout/rollback yapıldığı anlamına gelmez.
