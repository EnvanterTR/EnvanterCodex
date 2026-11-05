# Devam notu — 2026-11-04

## Güncel oturum durumu

WP-082/F01, WP-083/F02, WP-084/F03, WP-086/F04, WP-087, WP-091 F5 ve WP-094 F0/A01 UI dilimleri tamamlandı. F01/F02 sentetik Chromium + izole PostgreSQL; F03 API/security + sentetik Chromium; F04 izole PostgreSQL rol politikası ve statik eşleme; F5 hosted Chromium kanıtına dayanır. F04'te `finance` yalnız `assets.read` yetkisiyle eklendi. WP-093/WP-094 ile A01'in toplu çöp kutusu API'si ve UI akışı tamamlandı; A01 `VAR`. Bu UI kabulinin API'si sentetiktir. WP-099/C01 korumalı lisans anahtarı akışı da tamamlandı (izole API 1/1 ve hosted Chromium PASS). Canlı firma kabulü yapılmadı.

Güncel salt-okunur canlı kontrol (4 Kasım 13:35 UTC): `https://envantertr.com/api/status` ve `https://firma.envantertr.com/api/status` HTTP 200/ready, 122 migration/0 pending; kaynak dal WP-099 sonrası 127 migration (`20261104190000_AddProtectedSoftwareLicenseKeys` son). 8 public plan, checkout kapalı. GET dışında canlı işlem yapılmadı. Exact migration ID eşleşmesi, 3 Kasım yedeğinin klonda restore kanıtı ve rollback provası yok; bu nedenle canlı yayın kapısı kapalı.

A–O katalog durumu: 75 `ÇALIŞTIRILMADI`, 10 `KISMEN`, 30 `VAR` (115 benzersiz hedef). Bunlar test kanıt sınıflarıdır, bitiş yüzdesi değildir. Kaynak dalda 127 migration var; önceki canlı/backup kanıtı 122 olduğundan F1 exact-ID parity ve güncel yedek/restore kanıtı olmadan yayınlanamaz. F1/F2/F3/F4/F5/F6/F7 kapıları açık; F8–F13 başlamadı; F14 Iyzico/şirket koşulu nedeniyle kilitli; F15 başlamadı. PR #37 taslak olarak kaydedilmişti; üretime yayın yapılmadı.

**WP-089 sonucu:** F1 yedek dosyası henüz bulunmadı. Worktree'de `deploy/local/backups` yok. Coolify `Backups` sekmesindeki `Schedules 0`, `Enabled 0`, `Total executions 0` Storage backups özelliğine ait. Ayrı `Scheduled Tasks` sekmesinde `EnvanterTR daily consistent PostgreSQL backup` görevi günlük ve `Success` durumunda (WP-090). Kaynak Compose, DB verisini `envantertr-v1-postgres-data` kalıcı volume'ünde tutuyor; `daily-postgres-backup.sh`, varsayılan olarak `/var/lib/postgresql/data/envantertr-backups/daily-*.dump` ve `.sha256` üretir. Henüz görev çalıştırma geçmişi, 3 Kasım dosya adı veya hash okunmadı; bu yüzden restore scripti çalıştırılmadı ve üretime yazılmadı.

**WP-091 sonucu:** F5 statik envanter kaynakla güncellendi: 2.256 statik öğe ve 65 rota işaretçisi; `node tools/Generate-UiInventory.mjs --check` PASS. Güncel hosted Chromium acceptance runner (checkout+şirket/platform arayüz testleri ve ayrı ürün galerisi testi) exit 0 ile tamamlandı; tüm çalıştırılan senaryolar PASS, FAIL 0, BLOCKED 0. Runner toplam PASS adedi üretmediği için agregat PASS sayısı raporlanmadı. Sentetik API'li Chromium kanıtı canlı şirket kabulü değildir ve 2.256 öğenin tamamını kapsamaz; F5'in kategori başına ≥95% koşulu açık.

**WP-092 sonucu: BLOCKED, 3/3 deneme.** Günlük PostgreSQL görevi `Success` ve kaynak kodundaki beklenen dump/hash yolu bulundu; ancak Coolify'de 3 Kasım dump/hash dosyasını salt okunur görme imkânı bu oturumda doğrulanmadı. Üç farklı `cua.getTab` çağrısı aynı `browser.user is unavailable` hatası verdi. Bu, yedeğin başka saklama alanında olmadığı anlamına gelmez. Restore yapılmadı; canlı veritabanına, migration'a, ortama veya yayına dokunulmadı.

**WP-093 DONE:** F0/A01 company-scoped reversible bulk-trash API added and acceptance/regression passed 2/2 in isolated PostgreSQL. Verified viewer 403, CSRF 400, tenant/location 404, relation conflict 409, atomic failure behavior, invalid selection limits and per-asset history/audit.

**WP-094 DONE:** F0/A01 bulk-trash UI added in five languages. Hosted Chromium `asset-detail-actions.mjs` PASS: write-role action, reversible confirmation, Escape/cancel no-write, selection retained on cancel, CSRF request body, soft-trash list refresh, selection cleared and success message. No DB migration, live DB, deploy, payment or Coolify change. A01 is now `VAR` in the target evidence table.

**WP-095 DONE:** A02 model EOL accepted. Isolated PostgreSQL test 1/1 PASS (company scope, create/list/detail/update/clear/audit); targeted Chromium and static verifier PASS; Release build 0 warnings/0 errors; UI envanteri `Generate-UiInventory.mjs --check` 2,258 öğe/65 rota ile PASS. Source-only migration `20261104161606_AddAssetModelEndOfLife` is not applied. No production DB/deploy/Coolify/payment change.

**WP-096 DONE:** A05 annual depreciation rate backend/API accepted. Added nullable `AssetModel.DepreciationAnnualRatePercent` (0.34–100.00%, two decimals), exclusive with life-month policy and restricted to linear method; floor/salvage value remains supported. Isolated PostgreSQL acceptance `Annual_depreciation_rate_is_company_scoped_and_drives_asset_value_and_report` 1/1 PASS: model create/list/detail, asset current value, report and CSV, policy update/clear, invalid overlap/range and cross-company rejection. Exact fixed-date example: 25% annual rate, 48 months, 200 TRY floor, 1,000 TRY cost gives 16.67 TRY/month and 800 TRY book value after 12 months. Release build 0 warnings/0 errors; `dotnet ef migrations has-pending-model-changes` reports no model drift. Source-only migration `20261104171011_AddAssetModelAnnualDepreciationRate` is not applied; source migration count is 126, last measured live count 122, exact IDs not compared. No live DB/deploy/Coolify/payment change. At WP-096 closure A05 still awaited the model/report UI; WP-097 has since completed it.

**WP-097 DONE:** A05 model create/edit, model detail, depreciation report and CSV UI accepts the annual percentage. Hosted Chromium verified five-language labels, month/rate exclusivity, POST/PATCH payloads, annual-rate detail/report/CSV, and the linear-only rule: annual-rate entry forces the supported linear method and disables both half-year options. The complete `asset-detail-actions.mjs` module exited 0; static verifier, JS syntax, diff check, and updated 2,260-control/65-route inventory pass. A05 is now VAR. Source migration remains unapplied. No live DB, deployment, payment, or Coolify change.

**Sonraki:** WP-099/C01 tamamlandı. F1/WP-098 uzaktaki hash/restore komut erişimi olmadığından BLOCKED (3/3); aynı erişim yöntemi değişmeden tekrarlanmayacak. 3 Kasım yedeğinin hash ve izole-klon restore kanıtı yok. F1 açık: kaynak 127, son bilinen canlı 122; tam migration-ID parity ve güncel yedek restore kanıtı olmadan deploy/migration yok. Sıradaki ürün paketi F0 tablosundaki bir sonraki kanıtlanmamış gereksinim. A–O kanıt sınıfları 75 ölçülmedi, 10 kısmi, 30 var; bu tamamlanma yüzdesi değildir.


# Güncel devam noktası — 4 Kasım 2026

## WP-082 — DONE

F01 çalışan kabulini tamamla. `deploy/testing/hosted-checkout-browser/employee-records.mjs` içinde sentetik API'li Chromium akışına arşivleme/geri yükleme eklendi: iptal yazma üretmiyor, CSRF ve açıklama gidiyor, arşivli kayıt varsayılan listeden çıkıyor, filtre ile bulunuyor ve geri yüklenince pasif kalıyor. Hedefli Chromium koşusu PASS. `./deploy/testing/run-security-acceptance.ps1 -Filter 'FullyQualifiedName~Employee_'` izole PostgreSQL'de 31/31 PASS; çalışan listesi, detay/dışa aktarma, create/edit, fotoğraf/dosya, CSV, departman/lokasyon/yönetici bağlantıları, rol/tenant kapsamı ve arşiv yaşam döngüsü dahil. Test DB/container/network temizlendi. `node tools/Verify-EmployeeRecordsUi.mjs`, JS syntax ve `git diff --check` PASS. `docs/PROJECT-STATUS.md` F01 `VAR` olarak güncellendi. Bu kanıt canlı firma kabulü değildir; canlı DB/deploy/migration/Coolify/ödeme değişmedi. Ürün kodu `92b7e7ba8aeb593f79f56e19d4cee65ab2210aa5`; test ve kanıt checkpoint'i `9f505cc`.

**Sıradaki:** F03 çalışan devre dışı bırakılınca zimmetli varlıkları otomatik iade etme akışının mevcut kod/test kapsamını ölç; eksikse kırmızı kabul testiyle tamamla. Ardından F04 rol adlarının ve izin sınırlarının hedef matrisiyle eşleşmesini ölç. Üretim yayını, PR oluşturma/birleştirme ve ödeme kilitleri yürürlükte.

## WP-081 — DONE

F01 çalışan arşivleme/geri yükleme dilimi `92b7e7ba8aeb593f79f56e19d4cee65ab2210aa5` commit'inde. Önce kabul testi kırmızıydı: `Employee` entity'sinde `ArchivedAt` bulunmadı. `20261104112743_ArchiveEmployeeRecords` migration'ı eklendi; `POST /api/employees/{id}/archive` ve `/restore` şirket/lokasyon kapsamı, `people.manage`, CSRF, zorunlu neden, pasif çalışan şartı ve audit kaydıyla çalışıyor. Varsayılan liste arşivlileri gizliyor; UI filtresi arşivlileri gösteriyor ve geri yükleme pasif kaydı geri getiriyor. `Employee_archive_and_restore_are_authorized_company_scoped_reversible_and_audited` izole PostgreSQL'de 1/1 PASS; `node tools/Verify-EmployeeRecordsUi.mjs` ve `git diff --check` PASS. Test DB/container/internal network kaldırıldı. Migration canlıya uygulanmadı; canlı DB/deploy/ödeme/Coolify değişmedi. F01 KISMEN: create/edit/fotoğraf/departman/lokasyon/yönetici ve arşiv UI etkileşiminin gerçek Chromium matrisi ayrıca tamamlanmalı.

**Sıradaki:** F01 gerçek Chromium kabulini tamamla; ardından F02–F04 kullanıcı yaşam döngüsü ve rol kapsamı. Canlıya alma, PR açma/birleştirme ve ödeme ayrı onay/kapılara bağlı.

## WP-080 — DONE

E03 bakım geçmişi ve ekleri: `Asset_maintenance_start_and_completion_preserve_authorization_state_and_history` + `Maintenance_attachments_are_validated_audited_and_included_in_company_archive` izole PostgreSQL'de 2/2 PASS. Geçmiş olayları, yükleme/liste/indirme/silme, içerik ve dosya adı doğrulaması, tenant/rol/CSRF, audit ve şirket arşivine dahil edilme doğrulandı. `Verify-MaintenanceAttachmentsUi.mjs`, `Verify-MaintenancePlanUi.mjs`, `git diff --check` PASS; test DB/network kaldırıldı. Gerçek Chromium etkileşimi F5'te. E03 `VAR`; canlı/ödeme/Coolify değişmedi. Sonraki WP: F01 kullanıcı yaşam döngüsü, profil fotoğrafı ve departman/lokasyon/yönetici bağlantıları.

## WP-079 — DONE

E02 fiziksel denetim: üç izole PostgreSQL kabul testi 3/3 PASS: denetim kaydı/bulgu/sonraki tarih/geçmiş/rapor-CSV; gecikmiş-yaklaşan-planlanmamış kuyruk, arama ve sayfalama; plan oluşturma/temizleme. Şirket/rol/CSRF ve tarih sınırları PASS. `Verify-AuditScheduleUi.mjs`, `git diff --check` PASS. Geçici DB/network kaldırıldı; canlı DB/deploy/migration/ödeme/Coolify değişmedi. E02 `VAR`; gerçek Chromium form gönderimi F5'te ayrıca doğrulanacak. Sonraki WP: E03 bakım geçmişi ve dosya ekleri.

## WP-078 — DONE

E01 arıza/bakım/onarım: `Asset_maintenance_start_and_completion_preserve_authorization_state_and_history` izole PostgreSQL'de 1/1 PASS; `maintenanceType`, `startedOn`, `completedOn`, `cost=1200`, `currency=TRY` API'den okundu. CSRF/rol/tenant, tarih sınırları, varlık durumu ve geçmiş doğrulandı. `Verify-MaintenancePlanUi.mjs`, `Verify-MaintenanceAttachmentsUi.mjs`, `git diff --check` PASS. Test DB/network kaldırıldı; üretim DB/deploy/migration/ödeme/Coolify değişmedi. Test commit `ddc47fed1ec0c2c62dc40c23c79f1d05be009288`. Gerçek Chromium bakım formu gönderimi F5'te ayrıca doğrulanmalı. Sonraki WP: E02 fiziksel sayım/audit kabul ölçümü.

## WP-077 — DONE

D05 ortak stok CRUD/zimmet/stok/minimum uyarısı: D01–D04 önceki kabul kanıtlarına yeni, geri alınabilir arşiv yaşam döngüsü eklendi. `Stock_catalogs_archive_and_restore_without_cross_tenant_access_or_history_loss` izole PostgreSQL'de 1/1 PASS: aksesuar/sarf/bileşen arşivleme, başka şirket kaydında 404, varsayılan listeden saklama, `includeArchived` ile erişim, geri yükleme ve üç hareket geçmişini koruma. `node tools/Verify-StockArchiveUi.mjs` ile `Verify-AccessoryInventoryUi.mjs`, `Verify-ConsumableInventoryUi.mjs`, `Verify-ComponentInventoryUi.mjs` PASS. Yeni `IsArchived` alanları için migration test DB'de uygulandı. Gerçek Chromium arşiv/geri yükleme tıklaması denenmedi; F5'te doğrula. Commit `99245393f4ba74437e3019f363575d1945a72a1b`. Canlı DB/deploy/Coolify/ödeme değişmedi. Sonraki WP: E01 arıza/bakım/onarım kabul ölçümü.



## WP-076 — DONE

D04 kit hazır set: `Accessory_kits_support_company_scoped_crud_safe_export_and_archive_restore`, `Accessory_kit_requests_are_company_scoped_and_approve_all_stock_lines_atomically`, `Mixed_kits_validate_tenant_items_and_issue_all_four_types_as_one_request` izole PostgreSQL'de 3/3 PASS. Kit reçetesi CRUD/arşiv, tüm kalemlerde yeterli stok/atomik teslim ve mixed kit'in çalışan talebi üzerinden aksesuar/sarf/varlık/bileşen tahsisi doğrulandı. Hosted Chromium `accessory-kits.mjs` PASS: kit CRUD/history/export ve satın alma satırlarından varlığa bağlı kit reçetesi, yalnız alınmış/iade edilmemiş satırlar, adet/CSRF ve stok değişmedi bildirimi. `node tools/Verify-AccessoryKitsUi.mjs` PASS. Geçici DB/container/network kaldırıldı. D04 `VAR`; sonraki WP: D05 ortak CRUD/zimmet/stok/minimum stok kabul matrisi.

## WP-075 — DONE

D03 bileşeni varlığa takma: `Asset_detail_shows_open_accessory_and_component_relations_only_for_its_company`, `Concurrent_component_checkouts_cannot_allocate_the_same_remaining_stock_twice` ve `Location_access_scope_is_tenant_scoped_inherited_and_enforced_for_assets` izole PostgreSQL'de 3/3 PASS. Varlık detayında açık/takılı bileşen ve kalan adet, tenant izolasyonu, lokasyon kapsamı ve yarış koşulunda tek stok tahsisi doğrulandı. `node tools/Verify-ComponentInventoryUi.mjs` PASS; gerçek Chromium tahsis formu gönderimi çalıştırılmadı. Geçici DB/container/network kaldırıldı. D03 `VAR`.

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

## WP-086 — DONE — F04 rol politikası

F04 hedef rolleri kaynak API ve mevcut erişim modeliyle eşlendi. `finance` rolü şirket daveti, platform daveti, platform üye listesi/rol seçicileri ve veri içe aktarma allowlistine eklendi. En az ayrıcalık: yalnız `assets.read`; `people.read`, `people.manage`, `people.documents`, `assets.write` ve `company.settings` izinleri yok. F04 eşlemesi: Süper Admin = ayrı platform admin hesabı; Şirket Yöneticisi = owner/admin; Editör = asset_manager/asset_operator; Okuyucu = viewer; Sınırlı = şirket kapsamlı özel izin seti ve ayrı konum/birim kısıtları; Muhasebe = finance.

Kanıt: `Company_roles_follow_the_asset_stock_employee_and_settings_policy_matrix` 1/1 PASS; `Company_custom_roles_are_tenant_scoped_owner_controlled_and_recalculated_per_request` ve `Company_member_and_platform_admin_boundaries_are_enforced_over_http` 2/2 PASS. Testler izole PostgreSQL'de koştu, geçici DB/container/network kaldırıldı. `node tools/Verify-RoleMatrix.mjs`, `node --check src/EnvanterTR.Web/wwwroot/platform-admin-members.js` ve `git diff --check` PASS. İlk hedefli koşunun kırmızı sonucu role resolver eksikliğini gösterdi; düzeltme sonrası yeşil. F04 kod/API için VAR; canlı firma kabulü yok. Üretime/ödeme/Coolify'ye dokunulmadı.

## Sıradaki iş

WP-094 toplu çöp kutusu UI kabulini; WP-095 model EOL, WP-096/097 yıllık amortisman backend/UI kabulini ve WP-099/C01 gerçek lisans anahtarı korumasını tamamladı. WP-099 izole PostgreSQL 1/1, hosted Chromium, Release build 0/0 ve EF snapshot parity PASS; migration `20261104190000_AddProtectedSoftwareLicenseKeys` source-only. WP-090 yedek dosyası ve manifestini buldu; WP-098 hash/restore uzaktaki komut erişimi olmadığı için 3/3 denemede BLOCKED. F1 yayın kapısı açık; canlı ölçüm 122, kaynak 127, exact-ID karşılaştırması ve güncel yedeğin klona restore kanıtı olmadan migration/yayın yok. Sonraki paket: F0 durum tablosundaki sıradaki kanıtlanmamış P0 gereksinimine geç; WP-098 erişim engeli değişmeden aynı yöntemleri tekrarlama.

## WP-099 — DONE — F0/C01 lisans anahtarı

Lisans anahtarı API/model/UI eklendi. Anahtar CompanyId ve LicenseId ile bağlanmış ASP.NET Data Protection koruyucusuyla saklanır; liste/detay/export/audit yanıtında düz metin veya şifreli metin yer almaz. Yetkili reveal yanıtsız cache başlıklarıyla gelir ve görüntüleme audit edilir; detay penceresi kapanırken tarayıcı alanı temizlenir. İzole PostgreSQL 1/1, hosted Chromium PASS, Release build 0 warning/0 error ve EF snapshot parity PASS. Kaynak migration sayısı 127; son migration WP-099. Canlı migration/DB/deploy/ödeme/Coolify değişikliği yok. Kod commit `5b4e495` (`codex/f2-cloud-seat-limits-20261104`), yerel ve push edilmedi.
## 2026-11-05 resumed checkpoint

- Follow `AGENTS.md` R12: F1 migration freeze remains active. No EF migration was added or modified.
- `docs/PROJECT-STATUS.md` has separate phase-gate and product-requirement tables; the A–O product catalog is 115 unique IDs. F14/F15 are gates, not catalog entries. Status columns are `Kodda / Canlıda / Kabul edildi`.
- The canonical prompt copy at `C:\Users\Lenovo\Desktop\EnvanterTR\CODEX-HEDEF-4.md` matches `AGENTS.md` SHA-256 `661FC84C86F9E29F5828E2A6D3462E53750BFFD95E3129D01AAB28E86657A6CE`.
- 3 Nov backup hash/restore, exact 122 migration-ID parity, five-migration clone rehearsal, and before/after table counts remain BLOCKED because no authorized server-side Coolify shell/DB command channel is available. Do not copy customer data off-host. No production DB operation occurred.
- Earlier WP-105 full run: 319 total, 318 PASS, 1 FAIL (license reminder test used an unsupported five-day fixture); WP-106 corrected the test fixture and targeted regression passed 13/13. This was superseded by final WP-111 run on `95293ffe7a5745ca49bad5e09ac9682e7eadf9c7`: 320 total, 320 PASS, 0 FAIL, runner exit 0. Output: `%TEMP%\envantertr-security-acceptance-20261105-wp111.log`; disposable PostgreSQL/acceptance containers and network removed.
- Static verifiers: current 85/85 PASS. The original two failures were stale `Synthetic Invited Viewer` in `Verify-HostedCheckoutBrowserAcceptance.mjs` and native `confirm()` at `index.html:8614` for `Verify-NoNativeBrowserDialogs.mjs`; WP-101/WP-103 corrected both. `Generate-UiInventory.mjs --check`: 2,265 controls / 65 routes. This is an inventory count, not proof that all controls were exercised.
- WP-100 route-specific log search remains BLOCKED because no route log event was observable at the configured level. WP-110 superseded the secret-log acceptance requirement with a host-capturing `ILoggerProvider` and verified active warning canary plus absence of synthetic plaintext during key set/reveal: isolated PostgreSQL 1/1 PASS. Do not repeat the route-specific log search.
- No push, PR, deploy, live DB write, migration, payment, domain, environment variable, or wildcard change. No test was run after the latest full-suite result; only summary, project-status, ledger, and handoff documentation changed.


## 2026-11-05 final closeout

- WP-111: one full disposable PostgreSQL security/tenant acceptance run on `95293ffe7a5745ca49bad5e09ac9682e7eadf9c7`; 320 total / 320 PASS / 0 FAIL; runner exit 0. No repeat was run.
- F4: six tenant-owned read families reject foreign detail IDs, exclude foreign collection rows, and ignore forged `companyId` selectors (WP-108 1/1); broad hostile-ID coverage for all write, file, report and export routes remains open.
- F5: hosted Chromium runner exit 0; homepage product gallery fullscreen, keyboard/Escape/focus, slide navigation, auto/manual leftward transitions and mobile overflow verified. Inventory is 2,265 controls / 65 routes, not per-control acceptance.
- F6: protected license key encryption/role/CSRF test 1/1 (WP-104), plus host log capture and no-plaintext test 1/1 (WP-110). Broader security and legal acceptance remains open.
- Current static validators: 85/85 PASS. The two earlier failures were `Verify-HostedCheckoutBrowserAcceptance.mjs` (stale `Synthetic Invited Viewer` fixture expectation) and `Verify-NoNativeBrowserDialogs.mjs` (native `confirm()` at `index.html:8614`); both were corrected. UI inventory check remains 2,265 controls / 65 routes.
- F1: 3 Nov dump hash matches its manifest (WP-101). Restore into isolated clone, compare 122 migration IDs, run five pending migrations in clone, and compare pre/post table counts remain BLOCKED because no authorized server-side restore/SQL command path is available. No customer dump was copied off-host.
- No live DB write, migration, deploy, payment, environment/domain/wildcard edit, or push occurred. After WP-111, only status/ledger/handoff documentation changed; no tests were rerun. Local documentation closeout only.

## 2026-11-05 continuation closeout

- Completed the requested independent packages WP-112 through WP-116 (five packages total). R12 migration freeze remains in force; no new EF migration was created.
- WP-113 F4: hostile foreign-tenant archive mutations for accessories, consumables, and components returned 404 and preserved rows/audit state; isolated PostgreSQL 1/1 PASS. F4 remains partial; this is not coverage of every write/file/report/export endpoint.
- WP-117 F4: foreign stock/accessory/asset purchase-order attachments were not listable, downloadable or deletable under company A's session; all requests returned 404, company A's ZIP excluded B's attachment IDs/files, and B's media files/DB rows remained intact. Final isolated PostgreSQL 1/1 PASS; log `%TEMP%\\envantertr-security-acceptance-20261105-wp117-final.log`. F4 remains partial.
- WP-114 F5: 85/85 static verifiers PASS, inventory 2,265 controls / 65 routes, hosted Chromium 75 PASS summaries / 0 FAIL / 0 BLOCKED, exit 0. Coverage is curated, not per-control. No current UI FAIL needed a code fix.
- WP-115 F6: protected license-key encryption/role/CSRF and captured-log plaintext absence 2/2 PASS; role/audit verifiers PASS. Broad legal/KVKK and live acceptance remain open.
- WP-116 full PostgreSQL run: `deploy/testing/run-security-acceptance.ps1`, exact source/test commit `58339bb8089d3b9d4132c50ae859b1dd787f53d7`, clean tree at run; 321 total / 321 passed / 0 failed, 50.5365 minutes, runner exit 0. Log: `%TEMP%\envantertr-security-acceptance-20261105-wp116.log`. Disposable test containers and network were removed. `summary.json` is updated; no tests were run after this suite, only documentation and `git diff --check`.
- F1 restore, exact live/source migration-ID parity, five-migration clone rehearsal, and before/after table counts remain BLOCKED due lack of an authorized safe server-side restore/SQL path. Do not repeat old backup evidence as fresh proof.
- No production DB write, migration, deployment, payment, Coolify config, environment/domain/wildcard change, or push occurred.

## 2026-11-05 resumed work

- WP-118 / F4 DONE: Company-A lists for its own stock/accessory/asset purchase orders omitted company-B attachment IDs; isolated PostgreSQL test 1/1 PASS, 59.9293 s, exit 0. Disposable DB/container/network removed. F4 still lacks complete hostile-ID coverage for all API routes, reports, imports, and exports.
- WP-116 remains the last unfiltered full run (321/321 PASS on `58339bb8089d3b9d4132c50ae859b1dd787f53d7`). WP-117/WP-118 targeted F4 tests were added after it; run the requested unfiltered PostgreSQL suite only at the end of this continuation.
- WP-114 still reports 85/85 static verifiers and 75 curated hosted-browser summaries with zero FAIL/BLOCKED; there was no active UI failure identified for a product fix. Full per-control UI acceptance remains open.
- Migration freeze is active. No production, migration, deployment, payment, Coolify, or push action has occurred.

## 2026-11-05 continued work (WP-119)

- WP-119 / F6 DONE: isolated platform MFA Chromium acceptance now builds the web image from current source, preventing stale-image results. Invalid Authenticator input returns retryable HTTP 400 (`twoFactorRequired`); the MFA field and secure-login action remain visible; a subsequent valid TOTP reaches the dashboard. `node --check` and `git diff --check` PASS; disposable browser/web/migrate/PostgreSQL stack and network/volumes were removed.
- The last full PostgreSQL acceptance remains WP-116, 321/321 PASS on `58339bb8089d3b9d4132c50ae859b1dd787f53d7`. WP-117 through WP-119 occurred afterward, so run the unfiltered suite once at the end.
- F1 steps b–d remain blocked: the Nov 3 dump cannot be safely fetched into the local isolated clone using available approved interfaces; the local clone predates the dump. No restore/parity/rehearsal/table-row proof is claimed. Step a had prior matching hash evidence (WP-101); step e's two static verifier failures are fixed (85/85, WP-107); step g is covered by WP-104/WP-110. Do not rerun blocked restore via the Coolify web terminal.
- F4 remains partial after WP-113/WP-117/WP-118. F5 has no active UI FAIL from WP-114's curated sweep; the 2,265 controls were not individually exercised. F6 remains partial despite the targeted encryption/log and MFA retry evidence; broader KVKK/legal/live-company acceptance remains open.
- No production database writes, migration, deployment, payment action, Coolify configuration change, domain/wildcard edit, or push occurred.

## 2026-11-05 continued work (WP-120)

- WP-120 / F4 DONE: `Brand_model_category_and_images_are_company_scoped_and_reusable` passed 1/1 in 58.7348 s on disposable PostgreSQL. Company-B read/upload/delete attempts against Company-A brand/model/accessory-model/supplier media returned 404; owned media workflows remained functional. Disposable DB/network were removed. This targeted slice does not close F4's all-route/report/import/export matrix.

## 2026-11-05 continued work (WP-121)

- WP-121 / F4 DONE: `Maintenance_report_aggregates_date_filtered_status_and_costs_without_cross_company_data` passed 1/1, xUnit 55.2980 s. Date/status-filtered maintenance aggregates excluded other-company data. Disposable PostgreSQL/network removed. This covers one report family, not all reports.
- WP-119 through WP-121 are the three migration-free continuation packages since the last full run. Run the requested full PostgreSQL acceptance exactly once after this update; then record totals/exit/cleanup and stop.
