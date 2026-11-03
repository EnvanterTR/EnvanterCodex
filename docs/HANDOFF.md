# Devam notu — 2026-11-03

## Son tamamlanan çalışma paketi

WP-006–WP-020 tamamlandı. WP-012–WP-017'de `origin/main` uzlaştırması, checkout ve audited platform impersonation backend/UI slice'ları işlendi; ayrıntılar çalışma ledger'ında. Canlı dağıtım SHA'sından türetilen temiz aday `release/v1-candidate-20261102` (`1a676b2ae2dce9416fdd918ba8c26472f49ec316`) hosted Chromium kabulinden exit 0 ile geçti ve gerçek canlı yedeğindeki 122 migration ID'siyle eşleşti. WP-018 şirket silme kabulü 4/4; WP-019 sonrası birleşik preview 122/122 migration parity'de, model pending change yok ve restore edilmiş ağsız klonda update no-op. WP-020 imzalı zimmet PDF ve onaylı SMTP API backend'ini ekledi; hedefli izole PostgreSQL kabulü 5/5 geçti. Handover müşteri arayüzü henüz taşınmadı. Birleşik preview dalı deploy edilmedi; CI/tam regresyon, canlı firma kabulü ve gerçek SMTP alıcı teslimi ayrı kapılardır.

Kanıt: `tests/reports/live-derived-candidate-hosted-chromium-20261102.md`, `tests/reports/live-derived-candidate-security-20261102.md` ve `tests/reports/summary.json`.

Canlı deploy veya production DB yazımı yapılmadı. Browser kabul koşucusu sentetik API verisi kullandı. Sonraki iş: handover müşteri UI'sini eklemek; sonra birleşik dalın tam CI/regresyon ve hosted browser kabulini koşmak. Uygulama release'i ayrıca onay gerektirir.

## Gerçek kalan kapılar

1. F1/WP-007 ve WP-008 tamamlandı. Coolify arşivi `daily-20261102T122121Z.dump` (464,439 bytes); SHA-256 `331eb93292dfb97fbb780007a6a39c86e7c25537f109df97d63344dc8911dd55` yerelde manifestle eşleşti. `pg_restore --list` ve gerçek yedeğin ağsız PostgreSQL 17 klonuna restore PASS: 96 public tablo, 122 migration, 3 şirket, 1 varlık. Yedekten okunan tam `__EFMigrationsHistory` ID listesi temiz aday `1a676b2ae2dce9416fdd918ba8c26472f49ec316` kaynak migration ID'leriyle 122/122 eşleşti; iki yönde fark yok. Adayda pending migration yok. Latest→latest EF no-op script, tekrar ve untouched baseline'dan rollback clone row-count karşılaştırması PASS. Üretim DB'sine yazılmadı. Kanıt: `tests/reports/live-backup-restore-rollback-20261102.md`.
2. Canlı-baseline temiz release candidate `1a676b2ae2dce9416fdd918ba8c26472f49ec316` yedekteki 122 migration ID'siyle birebir eşleşiyor. WP-019 merge-preview dalını 122/122'e getirdi; WP-020 yeni migration üretmedi. Yedek klonunda `database update` önceki WP'de no-op doğrulandı; WP-020 klona bağlanmadı ve DB değiştirmedi. Eski full-history EF idempotent SQL üretim kusuru `20261010215527_AddAssetModelNumbersAndDefaultLocation` kaynak SQL ayıracı nedeniyle ayrı kısıt olarak kalır; uygulanmış migration dosyası değiştirilmemeli.
3. Handover müşteri UI'si; gerçek SMTP alıcı teslim kabulü; Santa/Farma/Demo3 canlı kabulü ayrıca tamamlanmalı. WP-020 SMTP testleri yalnız sentetik yakalayıcı kullandı.
4. İyzico gerçek ödeme kapısı şirket kuruluşu ve onaylı işyeri hesabı olmadan kapalı kalır.

Sonraki WP: WP-021 arayüzünü hosted Chromium'da doğrulamak (alıcı onayı iptali, yanıt kaybında aynı idempotency anahtarı, yalnız explicit resend), sonra birleşik preview dalında tam CI/regresyon ve hosted browser kabulini koşmak. SMTP gerçek alıcı teslimi ve Iyzico onayı ayrı operasyonel/ticari kapılardır. WP-020 backend kanıtı `tests/reports/handover-email-backend-acceptance-20261103.md`; WP-019 migration parity kanıtı `tests/reports/handover-email-migration-clone-parity-20261103.md`. `docs/PROJECT-STATUS.md` tek durum tablosudur; `docs/LEDGER.md` çalışma paketlerini tutar.

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
