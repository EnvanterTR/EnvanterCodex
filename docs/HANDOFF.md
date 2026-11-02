# Devam notu — 2026-11-03

## Son tamamlanan çalışma paketi

WP-006–WP-009 tamamlandı. Canlı dağıtım SHA'sından türetilen temiz aday `release/v1-candidate-20261102` (`1a676b2ae2dce9416fdd918ba8c26472f49ec316`) tam hosted Chromium kabulinden exit 0 ile geçti. Aday, gerçek canlı yedeğindeki 122 migration ID'siyle birebir eşleşti. Önceki aday kabulinde Release build (0 uyarı/0 hata), 85/85 kaynak doğrulayıcı, 2.304 kontrol / 71 rota UI envanteri ve izole PostgreSQL güvenlik kabulü (413/413) da geçti. F1 geri dönüş prosedürü de app-only rollback ile şema geri yüklemeyi ayırarak kaydedildi.

Kanıt: `tests/reports/live-derived-candidate-hosted-chromium-20261102.md`, `tests/reports/live-derived-candidate-security-20261102.md` ve `tests/reports/summary.json`.

Canlı deploy veya production DB yazımı yapılmadı. Browser kabul koşucusu sentetik API verisi kullandı.

## Gerçek kalan kapılar

1. F1/WP-007 ve WP-008 tamamlandı. Coolify arşivi `daily-20261102T122121Z.dump` (464,439 bytes); SHA-256 `331eb93292dfb97fbb780007a6a39c86e7c25537f109df97d63344dc8911dd55` yerelde manifestle eşleşti. `pg_restore --list` ve gerçek yedeğin ağsız PostgreSQL 17 klonuna restore PASS: 96 public tablo, 122 migration, 3 şirket, 1 varlık. Yedekten okunan tam `__EFMigrationsHistory` ID listesi temiz aday `1a676b2ae2dce9416fdd918ba8c26472f49ec316` kaynak migration ID'leriyle 122/122 eşleşti; iki yönde fark yok. Adayda pending migration yok. Latest→latest EF no-op script, tekrar ve untouched baseline'dan rollback clone row-count karşılaştırması PASS. Üretim DB'sine yazılmadı. Kanıt: `tests/reports/live-backup-restore-rollback-20261102.md`.
2. F1 migration ön şartı: release candidate canlıyla eşleşen 122 EF migration ID'sine sahip; kirli feature dalında 121 ID var. Feature dalında canlıdan eksik 5 ID: `20261020174959_AddSubscriptionCoupons`, `20261023190000_AddExistingCompanyCheckoutContext`, `20261024010000_AddPlatformImpersonationSessions`, `20261024212238_AddRecoverableCompanyDeletion`, `20261026083058_AddHandoverEmailAttempts`. Feature dalına özel, canlıda olmayan 4 ID: `20261101212918_AddCheckoutWebhookReceipts`, `20261101214246_AddCompanyPayments`, `20261101220912_AddCompanyInvoices`, `20261101230628_AddPlatformAnnouncements`. Ek kaynak taraması, feature dalında `SubscriptionCoupon`, `EmployeeHandoverEmailAttempt` ve `PlatformImpersonationSession` model referanslarının hiç bulunmadığını gösterdi; diğer iki eksik migration'ın ilgili alanları kısmen mevcut olabilir. Bu, farkın yalnız migration dosyası olmadığını ve eksik migration'ları tek başına kopyalamanın güvenli olmadığını doğruluyor. Candidate SHA `1a676b2ae2dce9416fdd918ba8c26472f49ec316` canlı 122'li seti korur. Kaynak kod incelemesinde dört feature migration'ının `Up` adımları yeni tablo/indeks/FK oluşturuyor; `Down` sırası announcements → invoices → payments → webhook receipts. Bu salt kaynak incelemesidir, DB provası değildir; ödeme tabloları F14 kilidine tabidir. Eksik canlı model/migration değişiklikleri uzlaştırılmadan migration çalıştırma.
3. SMTP gerçek alıcı teslimi ve Santa/Farma/Demo3 canlı kabulü ayrıca kanıtlanmalı.
4. İyzico gerçek ödeme kapısı şirket kuruluşu ve onaylı işyeri hesabı olmadan kapalı kalır.

Sonraki WP: kirli feature dalının canlı-baseline kaynak/migration sapmasını tek tek uzlaştır veya başka bir bağımsız kabul işine geç; migration farkları çözülmeden feature dalına migration uygulama. Full-history EF idempotent SQL üretimi, uygulanmış `20261010215527_AddAssetModelNumbersAndDefaultLocation` kaynak SQL'indeki eksik semikolonlar nedeniyle syntax error verdi. Uygulanmış migration düzenlenmedi. Güncel release candidate'ta pending migration olmadığı için F1 no-op provası geçti; bu full-history SQL bulgusu gelecekte kullanılmadan önce ayrı ele alınmalı. `docs/PROJECT-STATUS.md` tek durum tablosudur; `docs/LEDGER.md` çalışma paketlerini, `tests/reports/summary.json` makine kanıtını tutar. Yüzde raporlama.

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
