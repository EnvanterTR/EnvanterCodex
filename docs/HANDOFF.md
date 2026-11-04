# Güncel devam noktası — 4 Kasım 2026



## WP-075 — DONE

D03 bileşeni varlığa takma: `Asset_detail_shows_open_accessory_and_component_relations_only_for_its_company`, `Concurrent_component_checkouts_cannot_allocate_the_same_remaining_stock_twice` ve `Location_access_scope_is_tenant_scoped_inherited_and_enforced_for_assets` izole PostgreSQL'de 3/3 PASS. Varlık detayında açık/takılı bileşen ve kalan adet, tenant izolasyonu, lokasyon kapsamı ve yarış koşulunda tek stok tahsisi doğrulandı. `node tools/Verify-ComponentInventoryUi.mjs` PASS; gerçek Chromium tahsis formu gönderimi çalıştırılmadı. Geçici DB/container/network kaldırıldı. D03 `VAR`; sonraki WP: D04 kit (laptop + çanta + mouse) hazır set.

## WP-074 — DONE

D02 sarf stok/minimum stok: `Consumable_search_stock_filters_pagination_and_csv_export_match_company_scope` ve `Stock_and_model_employee_scope_matches_customer_api_and_explicit_location_restrictions` izole PostgreSQL'de 2/2 PASS. Eşik altı stok filtresi/CSV, stok girişi, sarf çıkışıyla miktar düşümü, eksik stok ve fazla zimmet reddi, detay/hareket tutarlılığı ve çalışan/lokasyon kapsamı doğrulandı. `node tools/Verify-ConsumableInventoryUi.mjs` PASS; gerçek Chromium form gönderimi bu WP’de çalıştırılmadı. Geçici PostgreSQL/container/network kaldırıldı. D02 `VAR`.

## WP-073 — DONE

D01 aksesuar zimmeti: `Asset_scope_mutations_reject_hidden_checkout_targets_and_catalog_employee_changes_atomically`, `Accessory_checkin_enforces_role_tenant_quantity_and_audits_the_return`, `Concurrent_accessory_checkouts_cannot_allocate_the_same_remaining_stock_twice` ve `Concurrent_accessory_checkout_and_stock_reduction_keep_stock_above_assignments` izole PostgreSQL'de 4/4 PASS. Tekil başarılı zimmet, birim/lokasyon/çalışan kapsamı, yabancı stok/atama reddi, rol, kısmi iade, movement/audit ve eşzamanlı stok yarışları doğrulandı. `Verify-AccessoryInventoryUi.mjs` ve `Verify-AccessoryCheckinDialog.mjs` PASS; gerçek Chromium'da zimmet formu gönderimi bu WP'de doğrulanmadı. Geçici PostgreSQL/container/network kaldırıldı. D01 `VAR`.

## WP-072 — DONE

C05 lisans maliyet kaydı izole PostgreSQL kabulinde 1/1 PASS: create/update, negatif maliyet reddi, şirket kapsamlı liste/detay API’si, saklanan değer ve CSV maliyet/para birimi. UI lisans formu/detayı ve lisans sipariş tablosu maliyet alanlarını içeriyor. Test projesinde dört eski `TrustedClientIpTests` uyarısı kaldı; test başarılı. Geçici PostgreSQL/network kaldırıldı. C05 `VAR`; ayrı birleştirilmiş dönemsel maliyet raporu bu pakette doğrulanmadı.

## WP-071 — DONE

C04 lisans bitiş uyarıları 30/15/7/3/1 gün eşiklerine bağlandı; saatlik worker günde bir digest içinde yalnız eşik günündeki lisansları alır. Önce kırmızı test (helper yok), ardından düzeltme; `SoftwareLicenseReminderThresholdTests` 12/12 PASS, product build 0 uyarı/0 hata. Gerçek SMTP alıcı teslimi yapılmadı; F3’te açık. C04 `VAR`.

## WP-069 — DONE

C02 lisans koltuğu atama/geri alma doğrulandı: hosted Chromium’da tekli/toplu atama, liste arama/durum/geçmiş filtresi/sayfalama, iptal ve onay, sayaç yenileme ve CSRF PASS. `Verify-LicenseAssignmentUi.mjs` + `Verify-LicenseActionDialogs.mjs` PASS. İzole PostgreSQL’de tekli assign/revoke, tekrar atama reddi, EULA gerektiren atomik toplu atama/kapasite ve tenant/birim sınırları 3/3 PASS; geçici DB/network kaldırıldı. C02 `VAR`. Sonraki paket C03 lisans dosyası yükleme kabulini ölç.

## WP-070 — DONE

C03 lisans eki upload/download/delete: hosted Chromium lisans dosyası/EULA akışları PASS; izole PostgreSQL `Software_license_attachments_are_validated_audited_and_tenant_scoped` 1/1 PASS (içerik/isim, rol, tenant, download ve audit). Geçici DB/network kaldırıldı. C03 `VAR`; WP-071’da tamamlanan C04’e bakınız.

## WP-068 — DONE

C01 lisans listesi/formu, koltuk ve atanmış adet, bitiş tarihi ile bitiş raporu kanıtlandı. Hosted Chromium lisans create/edit ve koltuk sayaçlarını; employee EULA UI’si ve Verify-LicenseAssignmentUi/Verify-LicenseActionDialogs PASS. İzole PostgreSQL create/update/read + expiry report 2/2 PASS. Gerçek aktivasyon anahtarı saklama alanı yok; LicenseReference aynı şey değildir. C01 KISMEN; WP-069’da tamamlanan C02’ye bakınız.

## WP-067 — DONE

B05 gelecek tarihli rezervasyon: hosted Chromium’da tarih doğrulama, istek, onay/ret, iptal, CSRF ve filtreler geçti; `Verify-ReservationUi.mjs` ve `Verify-AsyncUiForms.mjs` PASS. İzole PostgreSQL’de tarih/çakışma, tenant, reviewer rolü ve eşzamanlı talep testleri 2/2 PASS; geçici DB/network kaldırıldı. B05 `VAR`. Sonraki paket C01 lisans kayıtları.

## WP-066 — DONE

B04 zimmet formu: imzalı PDF’ye aynı-origin, şirket oturumu gerektiren QR eklendi. Hosted Chromium sentetik API ile iki PNG imza/hash, arşivden aynı imzalı belgeyi açma, PDF üretimi, QR hedefi ve alıcıya gösterilen açık onaydan sonra tek CSRF korumalı e-posta isteği PASS. İzole PostgreSQL belge snapshot/imza ve üç handover email yetki/tenant/idempotency testi 4/4 PASS. Gerçek SMTP alıcı teslimi kanıtlanmadı; F3 KISMEN. Sonraki paket C01 lisans kayıtları.

PR #37 açık ve taslak; yeni PR açılmadı. Dal `codex/f2-cloud-seat-limits-20261104`, uzak head `aaff45e559cc62403772f04eed830b21023bb7f7`. WP-045 Actions run `41773396545` exact head SHA'da `success`, beş zorunlu iş 5/5 başarılı; PostgreSQL tenant işi yaklaşık 28 dakika sürdü. Önceki run `41768913869` da 5/5 başarılıydı. WP-043 hosted Chromium exit 0; sentetik UI trial, plan ve pricing akışları PASS. WP-044 canlıya yalnız GET yaptı: apex/firma health 2/2 ready, migration sayısı 122/122, 8 public plan; checkout `available:false` ve F14 nedeniyle kilitli. Tam migration-ID parity ve canlı müşteri kabulü kanıtlanmadı. Üretim DB/deploy değişmedi.

WP-046 (platform MFA hatasında challenge alanı) üç denemede çözülemedi ve `BLOCKED` olarak kapatıldı. Son denemede izole test derlenip çalıştı; başarısız yanıt gövdesinde testin aradığı `twoFactorRequired` alanı yoktu. Kırmızı test geri alındı; ürün kodu, canlı oturum ve MFA ayarı değişmedi. Aynı WP tekrar açılmayacak. WP-047 F0/H04 klavye kabul alt kümesi gerçek Chromium ve statik verifier ile PASS; sağ tık/hover hâlâ açık. WP-048 salt-okunur başlık kontrolünde iki canlı host HTTP→HTTPS yönlendirdi; CSP/HSTS header yok, O04 bu nedenle KISMEN. WP-049 kaynak ölçümünde Identity parola politikası bulundu fakat hasher maliyeti açıkça yapılandırılmış/test edilmiş değil; O05 KISMEN. WP-050 gerçek `.env` dosyalarının izlenmediğini ve build/install dışlamalarını doğruladı; Gitleaks bulunmadığından geçmiş-geneli secret scan yok; O06 KISMEN. WP-051 O03 satın alma API kaynak ölçümünde girdi sınırları, EF LINQ sorguları ve CSV formül kaçışı bulundu; genel XSS/SQLi kabul matrisi yok, KISMEN. Hedefli test çalıştırılamadı (`dotnet` PATH'te değil; Docker daemon doğrulanamadı). WP-052 `Verify-PlatformAuditRetention.mjs` exit 0; audit saklama/önizleme/açık onaylı temizlik kontrolü geçti. Ayrı güncel KVKK kontrol listesi, mevzuat izleme ve hukuk onayı yok; O07 ve F6 KISMEN. WP-053 hedef dosyası A–O ürün kataloğuyla 115/115 eşleşti; F14/F15 faz kodları ayrı. WP-054 A01 için bulk update/assign/return/archive restore kanıtı mevcut fakat klon/toplu silme kanıtı yok; KISMEN. WP-056 A03 rozet renklendirmesi düzeltildi; `node tools/Verify-StatusLabelsUi.mjs` ve izole gerçek Chromium status-queue kabul testi PASS (6/6 durum rengi). WP-057 A04 katalog kapsamı kod ve UI statik doğrulayıcılarında bulundu; .NET API kabul testleri bu oturumda koşturulmadı. WP-058 A05'te satın alma/garanti/sigorta ve değer alanları var; yıllık amortisman oranı alanı eksik olduğundan KISMEN. WP-059 A06 dört özel alan türü .NET tip doğrulaması 2/2 ve hosted Chromium form/kopyalama testi PASS. WP-060 A07 ek/dosya API test PASS (1/1) ve Chromium UI kabul PASS; geçici PostgreSQL/network kaldırıldı. WP-061 A08 timeline Chromium ve API test PASS (1/1); geçici PostgreSQL/network kaldırıldı. |

WP-038 verifier assertion düzeltmesi `56c0fae3719d109020398b4a2e7ab158584fd9a8` ile push edildi; tam statik verifier suite'i 84/84 PASS. WP-039 hedef kataloğu denetimi 115 benzersiz A–O maddesinin 115 tekil durum satırına eşlendiğini doğruladı; `ÇALIŞTIRILMADI` 105, `KISMEN` 10. Bu satır eşlemesi özellik kabulü değildir. WP-040 kaynak migration envanteri 122 benzersiz ID gösteriyor; son ID `20261026083058_AddHandoverEmailAttempts`. Canlı DB ve restore klonu bu kontrolde bağlı değildi; bu sonuç canlı paritesi değildir.

WP-040/041 kayıtları `a35cce4` ile PR #37'ye gönderildi. WP-042 run `41768913869` exact remote head `a35cce4` üzerinde tamamlandı; beş CI işi de `success`. Bu sonuç yerel LEDGER ve summary kaydına eklendi; doğrulama kaydı uzak dala itilmedi. Üretim deploy, DB yazımı, migration, ödeme/Iyzico, SMTP e-postası veya Coolify ayarı bu çalışmada yapılmadı.

**Sıradaki:** F0 hâlâ açık. WP-072 sonrası A–O dağılımı 87 `ÇALIŞTIRILMADI`, 14 `KISMEN`, 14 `VAR` (F14/F15 faz kapıları hariç). A01/A02/A05 ve C01 kısmi; A03/A04/A06/A07/A08/B01/B02/B03/B04/B05/C02/C03/C04/C05 VAR. C01 lisans anahtarı alanı eksik. Sonraki paket D01 aksesuar zimmeti. PR #37 açık/taslak; ayrı kullanıcı onayı olmadan taslaktan çıkarma, birleştirme veya Coolify yayını yapma. Iyzico ve gerçek tahsilat F14 kilidinde.
# Devam notu — 2026-11-03

## Son tamamlanan çalışma paketi

WP-006–WP-020 tamamlandı. WP-012–WP-017'de `origin/main` uzlaştırması, checkout ve audited platform impersonation backend/UI slice'ları işlendi; ayrıntılar çalışma ledger'ında. Canlı dağıtım SHA'sından türetilen temiz aday `release/v1-candidate-20261102` (`1a676b2ae2dce9416fdd918ba8c26472f49ec316`) hosted Chromium kabulinden exit 0 ile geçti ve gerçek canlı yedeğindeki 122 migration ID'siyle eşleşti. WP-018 şirket silme kabulü 4/4; WP-019 sonrası birleşik preview 122/122 migration parity'de, model pending change yok ve restore edilmiş ağsız klonda update no-op. WP-020 imzalı zimmet PDF ve onaylı SMTP API backend'ini ekledi; hedefli izole PostgreSQL kabulü 5/5 geçti. Handover müşteri arayüzü henüz taşınmadı. Birleşik preview dalı deploy edilmedi; CI/tam regresyon, canlı firma kabulü ve gerçek SMTP alıcı teslimi ayrı kapılardır.

Kanıt: `tests/reports/live-derived-candidate-hosted-chromium-20261102.md`, `tests/reports/live-derived-candidate-security-20261102.md` ve `tests/reports/summary.json`.

Canlı deploy veya production DB yazımı yapılmadı. Browser kabul koşucusu sentetik API verisi kullandı. Bu tarihsel sonraki-adım notu WP-028 ile güncellendi; mevcut durum ve sıradaki adım aşağıdaki WP-028 bölümündedir.

## Gerçek kalan kapılar

1. F1/WP-007 ve WP-008 tamamlandı. Coolify arşivi `daily-20261102T122121Z.dump` (464,439 bytes); SHA-256 `331eb93292dfb97fbb780007a6a39c86e7c25537f109df97d63344dc8911dd55` yerelde manifestle eşleşti. `pg_restore --list` ve gerçek yedeğin ağsız PostgreSQL 17 klonuna restore PASS: 96 public tablo, 122 migration, 3 şirket, 1 varlık. Yedekten okunan tam `__EFMigrationsHistory` ID listesi temiz aday `1a676b2ae2dce9416fdd918ba8c26472f49ec316` kaynak migration ID'leriyle 122/122 eşleşti; iki yönde fark yok. Adayda pending migration yok. Latest→latest EF no-op script, tekrar ve untouched baseline'dan rollback clone row-count karşılaştırması PASS. Üretim DB'sine yazılmadı. Kanıt: `tests/reports/live-backup-restore-rollback-20261102.md`.
2. WP-019 merge-preview dalını 122/122'e getirdi; WP-020 yeni migration üretmedi. Yedek klonunda `database update` no-op doğrulandı. `deploy/coolify/compose.yaml` standart Dockerfile girişini çalıştırır; uygulama normal açılışta migration uygulamaz. `Program.cs` yalnız `--migrate` açıkça verilirse pending migration'ları bulup `MigrateAsync` ile uygular. Güncel 122/122 baseline'da migration komutu gerekmez. Full-history `--idempotent` SQL üretimi `20261010215527_AddAssetModelNumbersAndDefaultLocation` içindeki `suppressTransaction` çağrıları nedeniyle hata verdi, ancak bu artifact Coolify release yolunda kullanılmıyor. Bu bulgu mevcut no-pending release'i engellemiyor; gelecek migration için restore clone üzerinde opt-in `--migrate` veya doğrulanmış baseline'dan bounded SQL provası zorunlu. Uygulanmış migration dosyası değiştirilmemeli.
3. Handover müşteri UI'si; gerçek SMTP alıcı teslim kabulü; Santa/Farma/Demo3 canlı kabulü ayrıca tamamlanmalı. WP-020 SMTP testleri yalnız sentetik yakalayıcı kullandı.
4. İyzico gerçek ödeme kapısı şirket kuruluşu ve onaylı işyeri hesabı olmadan kapalı kalır.

Bu tarihsel sonraki-adım notu WP-028 ile güncellendi: handover tarayıcı alt senaryosu WP-022'de BLOCKED olarak kalıyor; GitHub CI resmi olarak PASS. WP-020 backend kanıtı `tests/reports/handover-email-backend-acceptance-20261103.md`; WP-019 migration parity kanıtı `tests/reports/handover-email-migration-clone-parity-20261103.md`. `docs/PROJECT-STATUS.md` tek durum tablosudur; `docs/LEDGER.md` çalışma paketlerini tutar.

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

## WP-028 — DONE

PR #34 (`codex/feature-main-reconcile-20261103` → `main`) açık/taslak/birleştirilmemiş. PR baş commit'i `7446037f249487581f3acbe655b05c95ca92b29f`, GitHub Actions run `41573283182` baş SHA'sıyla eşleşiyor; beş zorunlu işin tamamı `success`. Docker restore klonu `envantertr-f1-restore-20261102` yeniden kontrol edildi: `NetworkMode=none`, port binding yok, mount yok; PostgreSQL agregaları 96 public tablo, 122 migration, 3 şirket, 1 varlık. Üretim değişmedi.

## WP-029 — DONE

Durum dokümanı güncellemesini taşıyan PR SHA `4eb9ab5a147dc615ff575385dad7fe46494cc1f6` için Actions run `41576129281` tamamlandı ve beş işin tamamı `success`. Commit WP-028'in dokümantasyon kaydını yeniledi; uygulama kaynak kodunda değişiklik yok. PR açık/taslak/birleştirilmemiş.

## Sonraki adım / F1 sınırı

PR kodu ve iki CI SHA'sı PASS; PR hâlâ taslak olduğu için birleştirilmiş veya canlıya alınmış değil. WP-030 kaynak incelemesi full-history idempotent SQL hatasının mevcut release yolunda kullanılmadığını doğruladı. Güncel aday/yedek klonu 122/122 olduğundan release'te DB migration komutu çalıştırma. Kalan F1 gate: sahibi PR'ı `main`'e birleştirip Coolify rollout yapar; yayın sonrası `/api/status`, apex/firma HTTPS, login ve tenant smoke kanıtı olmadan F1 kapanmaz ve F2'ye geçilmez. Yayın prosedürü bu dosyanın “F1 geri dönüş prosedürü” bölümündedir.

## WP-026 — DONE

Üç statik doğrulayıcı beklentisi gerçek kaynakla eşleştirildi: 401 login fixture'ına güncel `#impersonationNotice` eklendi; platform MFA challenge depolama anahtarı beklentisi `platformMfaChallengeKey` yapıldı. Hedefli 3 doğrulayıcı ve tam `tools/Verify-*.mjs` turu 84/84 PASS. Yalnız test araçları ve durum dokümanları değişti; uygulama davranışı, üretim ve yedek klonu değişmedi.

## WP-027 — DONE

Hedef 4'teki `CODEX-HEDEF-4.md` dosyası Desktop kökünde bulundu; SHA-256'sı worktree `AGENTS.md` ile birebir aynı. Windows CI job'ının yerel eşdeğer adımları `4ee470984e21ceaa3daece9a0e48b265bf310509` üzerinde geçti: AccountRecovery, deterministik üçüncü taraf envanteri, self-contained win-x64 publish, bağımlılık lisans eşlemesi, DPAPI startup, yedek şifreleme/tamper, ACL, alert, restore/update/install safety, SMTP config ve ZIP checksum. CI sırasında `PDFsharp` ile `PDFsharp-MigraDoc` için eksik lisans envanter satırları bulundu ve eklendi. Paket SHA-256 `248bcee390827b0cff3a4b95caf9d700cff60916afcfa5ae7c2709218728fbcd`. Bu bir yerel Windows kabulidir, resmi GitHub Actions sonucu değildir; üretim deploy veya DB değişikliği yoktur.

### WP-027 kapanışında kaydedilen sonraki adım (WP-028 ile tamamlandı)

Bu tarihsel nottaki PR/CI bekleyişi artık geçerli değil: PR #34 açıldı ve workflow `41573283182` aynı SHA'da 5/5 başarıyla tamamlandı. Güncel kalan F1 operasyon kapısı Coolify canlı rollout ve sahibi tarafından yayın sonrası health/tenant smoke kanıtıdır; WP-028 bölümü güncel kayıttır.

## WP-031 — DONE (yerel kod paketi; canlı F2 açık)

F2 Cloud seat-limit uyumsuzluğu düzeltildi. `SubscriptionPlan.MaxLoginUsers` Cloud planları için API create/update/get/public yanıtlarında korunuyor; platform paneli bu değeri düzenleyebiliyor; iki davet endpoint'i Cloud ve self-hosted planda mevcut üyelik sayısını sınıra göre denetliyor. Ana sayfa ve checkout, API'den gelen kullanıcı/varlık sınırını gösteriyor; ana sayfa checkout bağlantısını yalnız `/api/checkout/status` `available:true` döndürürse sunuyor. Beş dildeki eski sınırsız kapasite iddiaları kaldırıldı.

Kanıt: önce iki hedefli security acceptance kırmızıydı; düzeltme sonrası `deploy/testing/run-security-acceptance.ps1 -Filter ...` 2/2 PASS. Acceptance web build 0 uyarı/0 hata; `Verify-PlatformPlanManagement.mjs` ve `Verify-HostedCheckoutUi.mjs` PASS; `node --check src/EnvanterTR.PublicSite/checkout.js`, `git diff --check` PASS. Test DB ve ağı runner kaldırdı. Migration, canlı DB, ödeme veya müşteri e-postası yok.

Branch `codex/f2-cloud-seat-limits-20261104`; push/PR/merge/deploy yapılmadı. F2 canlı kabul için kodun release edilmesi, platform yönetiminde sekiz mevcut Cloud planının kararlaştırılan kullanıcı sayılarıyla düzenlenmesi ve ödemesiz şirket kaydı/manuel aktivasyonunun gerçek firmada kabul edilmesi gerekir. Bu oturumda Farma owner oturumu doğrulandı; platform-admin yetkisi doğrulanmadı.
## WP-032 — DONE; F2 remains partially open

The integrated free-trial acceptance flow passed on `codex/f2-cloud-seat-limits-20261104`: `deploy/testing/run-security-acceptance.ps1 -Filter 'FullyQualifiedName~Platform_can_provision_free_trial_without_claiming_payment'` → 1/1 PASS, exit 0 (46.6 s). It creates an isolated company through the platform-admin trial path, validates 15 days/no payment/no checkout, accepts the captured invitation and sets a password, enrolls and verifies mandatory first-login TOTP, then confirms authenticated company-scoped session. The one-off PostgreSQL container/network were removed by the runner. No production DB, email, deploy, or customer account was touched.

Next F2 gates: after the owner approves a release containing WP-031, update the live Cloud plan seat/asset limits in the platform catalog, then perform an explicitly scoped live pilot acceptance and record evidence. Do not claim the user's reported Farma login as verified by this automated synthetic test. Do not initiate payment or Iyzico.

## WP-033 — DONE

Added a server-backed five-step onboarding guide to the authenticated company dashboard: company profile, branding, bulk import, member invites, and first asset. The status endpoint is restricted to `settings.manage`, reads only the authenticated `company_id`, and hides the guide after completion or 30 days. Existing page navigation targets are used; Turkish, English, Russian, Ukrainian, and Macedonian copy is provided. Targeted isolated PostgreSQL acceptance passed 1/1, including trial invitation, mandatory MFA, owner onboarding status 1/5, and remaining steps; Web build 0 warnings/errors; JavaScript syntax and `git diff --check` passed. The test runner removed its temporary database/network. No production deployment, DB write, email, or payment occurred.

Next: continue F2 by preparing release/CI evidence for WP-031–034, then—only after an approved live release—set live Cloud plan capacities and perform the explicitly scoped live no-payment pilot acceptance. Do not initiate payment or Iyzico. The user's latest message reports that Farma login succeeded; this is user-reported and is not the same as the synthetic acceptance or a recorded manual acceptance walkthrough.

## WP-034 — DONE

The full static verifier suite initially caught two stale expectations: Cloud plans were still asserted to be unlimited, and company member UI still claimed unlimited Cloud users. Replaced the obsolete unlimited-plan verifier with `Verify-CloudPlanSeatLimits.mjs`, aligned the member-invite verifier with configured `MaxLoginUsers`, corrected the customer-facing copy, and marked the old product-coverage statement as superseded. Final evidence: all 84 `tools/Verify-*.mjs` checks PASS; isolated PostgreSQL security acceptance for Cloud invite seat caps and platform plan catalog pricing passed 2/2; acceptance Web build 0 warnings/0 errors (test project reports 4 existing ASP.NET deprecation warnings); runner removed its temporary DB/network. No migration, production DB, deployment, email, or payment was used.

WP-034 is committed and pushed to `codex/f2-cloud-seat-limits-20261104` at `1b2a8554923687b052b3b2b293622c8eda285580`. No PR has been opened yet; the user’s action-time confirmation is pending. F2 remains partial: no live release or live plan-catalog adjustment/pilot acceptance has occurred. Latest user message says they logged in; retain as user-reported only, not independent product acceptance. Do not initiate payment or Iyzico.

## WP-035 — DONE

`tests/reports/summary.json` previously identified a different branch/SHA (`eb5b93a`, measured Nov 2) as its current source while the working feature branch had advanced. The summary now has a current measurement section tied to the exact Release build SHA (`1b2a8554923687b052b3b2b293622c8eda285580`) and preserves the former data under `historicalBaseline`. It separately records 84/84 source verifiers and 2/2 targeted PostgreSQL acceptance on `f3a12e5` (the application/verifier files match the measured Release SHA; the only interim difference was the handoff note), current UI inventory 2,253 controls/65 routes, and the prior 297/297 broad suite on `7446037f`. It does not imply the 297-test suite was rerun on the current feature commit. Release build used Docker SDK 10.0 because `dotnet` is not installed on Windows: 0 warnings, 0 errors. JSON source/SHA, scope separation and historical retention checks passed; `git diff --check` passed.

WP-035 is pushed in `2cf52c8e2e2f18dcef1d922ea31f41fd75e2b54f`.

## WP-036 — DONE

On `2cf52c8e2e2f18dcef1d922ea31f41fd75e2b54f`, the two runtime API acceptance tests passed 2/2. They enumerate every API `RouteEndpoint`, assert explicit auth/AllowAnonymous, exact anonymous-route and authorization-policy sets, and check the protected routes against the company/platform role matrix. This does not establish data-level hostile-ID isolation for every endpoint; F4 remains partial. `summary.json` now records this check separately from the historic 297/297 run, and old measurement content is preserved exactly. The isolated test DB/network were removed. No production access or writes occurred.

Next: push WP-036, then wait for the user's pending action-time confirmation before opening a draft PR to trigger CI. Do not modify production while CI/review is pending. F2 remains partial; do not start payment/Iyzico.
