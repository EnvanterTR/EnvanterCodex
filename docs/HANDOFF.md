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

## WP-022 — BLOCKED

Zimmet PDF/e-posta arşivi için ayrı Chromium retry senaryosu üç denemede tamamlanmadı: yanıt kaybı sonrası metni değişen düğme eski metinle seçildi ve POST tetiklenmedi (30 sn yanıt zaman aşımı). Test ekleri geri alındı; ürün kodu, SMTP, yedek klonu ve canlı değişmedi. Aynı testi dördüncü kez çalıştırma.

## WP-023 — BLOCKED

Güncel branch `codex/feature-main-reconcile-20261103` @ `8367c6432b20ca897536202db6da66dea942d8a9`. Release build 0 uyarı/0 hata; UI inventory `--check` 2.252 kontrol/65 rota. İki eski verifier beklentisi düzeltildi ve hedefli kontroller geçti. Tam kaynak doğrulayıcı turu 81/84: `Verify-AuthErrorFlows` fixture'ında yeni `#impersonationNotice` yok; `Verify-PlatformAdminExperience` ve `Verify-PlatformMfaBrowserAcceptance` kodda artık kullanılmayan `platformMfaChallengeStorageKey` adını arıyor. Üç deneme sınırı doldu; aynı verifier'lar tekrar denenmeyecek. Uygulama ve canlı değişmedi.

## WP-024 — DONE

İzole PostgreSQL güvenlik/tenant kabul koşusu 297/297 geçti, exit 0 (1.0484 saat). Tek kullanımlık test DB'si ve Docker ağı temizlendi; canlıya ve yedek klonuna bağlanılmadı/yazılmadı.

## WP-025 — DONE

`deploy/testing/run-hosted-checkout-browser-acceptance.ps1` exit 0. Güncel dalda checkout ve hosted portal/platform Chromium senaryoları geçti. Ayrı public-site galeri kabul testi beş slaytı, otomatik sola geçişi, tam ekran görsel açmayı, Esc/klavye odağını, mobil taşmayı ve sayfa hatası olmadığını doğruladı. Bu testler yerel/sentetik fixture kullandı; hiçbir e-posta gönderilmedi ve canlıya bağlanılmadı.

## Sonraki bağımsız adım

F1 sırasını koruyarak birleşik dalın GitHub Actions CI sonucunu doğrula (Release derleme, kaynak doğrulayıcılar, 297 PostgreSQL kabul testi ve hosted Chromium sonuçları bu worktree'de yerel olarak PASS). Sonra canlı rollout sahibi için F1 kontrol listesini sun ve yalnız yayın sonrası sağlık/tenant smoke kabuline ilerle. Üç eski kaynak doğrulayıcı WP-026'da ayrı yeni pakette güncellendi: güncel suite 84/84 PASS; WP-023'ün tarihsel blokajı aynı pakette yeniden açılmadı. Daha sonraki F2/F3 kabulinde ödemesiz yeni firma kaydı/manuel aktivasyonı ve SMTP-capture akışını gerçek alıcı tesliminden ayrı doğrula. Gerçek SMTP teslimi, müşteri kabulü ve deploy kanıtları ayrıca gerekir. İyzico, şirket/işyeri hesabı kurulana kadar kilitli kalır.

## WP-026 — DONE

Üç statik doğrulayıcı beklentisi gerçek kaynakla eşleştirildi: 401 login fixture'ına güncel `#impersonationNotice` eklendi; platform MFA challenge depolama anahtarı beklentisi `platformMfaChallengeKey` yapıldı. Hedefli 3 doğrulayıcı ve tam `tools/Verify-*.mjs` turu 84/84 PASS. Yalnız test araçları ve durum dokümanları değişti; uygulama davranışı, üretim ve yedek klonu değişmedi.

## WP-027 — DONE

Hedef 4'teki `CODEX-HEDEF-4.md` dosyası Desktop kökünde bulundu; SHA-256'sı worktree `AGENTS.md` ile birebir aynı. Windows CI job'ının yerel eşdeğer adımları `4ee470984e21ceaa3daece9a0e48b265bf310509` üzerinde geçti: AccountRecovery, deterministik üçüncü taraf envanteri, self-contained win-x64 publish, bağımlılık lisans eşlemesi, DPAPI startup, yedek şifreleme/tamper, ACL, alert, restore/update/install safety, SMTP config ve ZIP checksum. CI sırasında `PDFsharp` ile `PDFsharp-MigraDoc` için eksik lisans envanter satırları bulundu ve eklendi. Paket SHA-256 `248bcee390827b0cff3a4b95caf9d700cff60916afcfa5ae7c2709218728fbcd`. Bu bir yerel Windows kabulidir, resmi GitHub Actions sonucu değildir; üretim deploy veya DB değişikliği yoktur.

### Sonraki iş

F1/F4'ün kalan resmi kanıtı için bu dal adına main hedefli PR açılıp GitHub Actions CI'nın çalışması gerekir. Daha önce verilen PR #23 talimatı şu anki repository gerçekliğiyle örtüşmüyor: PR #23 başka `feat/annual-catalog-drafts-20261021` dalında. Mevcut feature branch için PR yaratma erişimi bu oturumda yok; GitHub Actions `pull_request` tetikleyicisi bu nedenle oluşmadı. PR compare adresi: `https://github.com/EnvanterTR/EnvanterCodex/compare/main...codex/feature-main-reconcile-20261103?expand=1`. Hedef gereği Coolify canlı rollout'u sahibi gerçekleştirir; yayın sonrası `/api/status`, apex/tenant HTTPS, giriş ve tenant yönlendirmesi kanıtlanmadan F1 kapanmaz.
