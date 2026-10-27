# Hedef v30 özellik matrisi — 27 Ekim 2026

Hedef7.8 için ilk kapsam dökümü. Bu dosya genel tamamlanma yüzdesi değildir. Bir satırın kod/test izi olması; CRUD, tüm roller, tenant, boş/hata ve tarayıcı kabulünün tamamının geçtiği anlamına gelmez. `DOĞRULANMADI`, özellik yok demek değil; satırın **tam kapsamının** kanıtlanmadığı demektir. Tarihsel testler bu çalışma sırasında yeniden koşulmuş sayılmaz. Yeni satırlar eklenerek payda değiştirilip ilerleme üretilmez.

Kod yolları `src/EnvanterTR.Web/Data/` altındadır. T = `tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs`. Belirtilen test yöntemleri kaynakta aranabilir; sonuç kanıtı ayrıca ilgili rapordan incelenmelidir. Güncel teknik yayın `27e9120`, CI40390600764, PostgreSQL356/356; bu toplam her satır için ayrı kabul değildir.

## 7.1 Çekirdek varlıklar

| Madde | Kod izi | Test izi / kanıt | Tam kapsam durumu ve kalan doğrulama |
|---|---|---|---|
| CRUD | AssetEndpoints.cs | T: Asset_single_edit_is_tenant_scoped_version_checked_and_written_to_history | DOĞRULANMADI — bütün CRUD/rol/boş-hata/UI eşlemesi açık |
| Klonlama | AssetEndpoints.cs | Özel klon kabulü eşlenecek | DOĞRULANMADI |
| Toplu güncelleme | AssetEndpoints.cs | Satır bazlı test eşlemesi açık | DOĞRULANMADI |
| Toplu silme | AssetEndpoints.cs | Tek kayıt trash testi toplu silme yerine geçmez | DOĞRULANMADI |
| Arşivleme | AssetEndpoints.cs | T: Asset_archive_restore_is_authorized_tenant_scoped_and_audited; santa-e2e raporları | DOĞRULANMADI — tam parite matrisi açık; Santa gerçek alt akışı kanıtlı |
| İade | InventoryMovementEndpoints.cs | T: Asset_assignment_accepts_a_valid_expected_return_and_return_clears_it_with_history | DOĞRULANMADI — tüm hedef türleri ve UI kapsamı eşlenecek |
| Zimmet | InventoryMovementEndpoints.cs | Aynı test; Concurrent_asset_checkouts_assign_one_asset_to_only_one_employee | DOĞRULANMADI — gerçek PDF/imza/e-posta kabulü açık |
| Marka | OperationalEndpoints.cs | Katalog testleri ayrı eşlenecek | DOĞRULANMADI |
| Model | AssetModelDetailEndpoints.cs | p0-catalog-browser-contract-acceptance-20261023.md | DOĞRULANMADI — tam CRUD paritesi açık |
| Kategori | OperationalEndpoints.cs | Katalog testleri ayrı eşlenecek | DOĞRULANMADI |
| Üretici | OperationalEndpoints.cs | T: Asset_table_model_and_manufacturer_sort_use_their_own_catalog_fields | DOĞRULANMADI — sıralama CRUD kabulünün tamamı değildir |
| Model numarası | AssetModelDetailEndpoints.cs | Alan kalıcılığı/rol/UI ayrıca eşlenecek | DOĞRULANMADI |
| EOL | AssetModelDetailEndpoints.cs | Ayrı alan kabulü eşlenecek | DOĞRULANMADI |
| Durum etiketleri ve renk | AssetStatusConfiguration.cs | T: Asset_status_display_labels_are_company_scoped_audited_and_manager_only | DOĞRULANMADI — tüm UI durumları eşlenecek |
| Tedarikçi | SupplierCatalogEndpoints.cs | Ayrı CRUD testleri eşlenecek | DOĞRULANMADI |
| Departman | OperationalEndpoints.cs | T: Department_can_be_archived_and_employees_moved_in_audited_batches | DOĞRULANMADI — bütün CRUD/rol/UI eşlemesi açık |
| Lokasyon | CompanyLocationAccess.cs; OperationalEndpoints.cs | Tenant ve lokasyon kapsam testleri mevcut; tam satır eşlemesi açık | DOĞRULANMADI |
| Grup | OperationalEndpoints.cs | Hedefteki grup kavramıyla model eşlemesi açık | DOĞRULANMADI |
| Değer | AssetEndpoints.cs | T: Asset_detail_returns_company_scoped_book_value_and_null_when_required_inputs_are_missing | DOĞRULANMADI — muhasebe/tam UI kabulü açık |
| Satın alma | AssetPurchaseOrderEndpoints.cs | T: Asset_purchase_orders_are_tenant_scoped_export_formula_safe_and_receive_assets_atomically | DOĞRULANMADI — tüm satın alma alt akışları tek teste indirgenmez |
| Garanti | AssetEndpoints.cs | Ayrı sınır/tarih/UI kanıtı eşlenecek | DOĞRULANMADI |
| Sigorta | AssetEndpoints.cs | Gereksinim/model/işlem kabulü eşlenecek | DOĞRULANMADI |
| Amortisman | DepreciationReportEndpoints.cs | T: Asset_detail_returns_company_scoped_book_value_and_null_when_required_inputs_are_missing | DOĞRULANMADI — rapor/tür/maliyet kapsamı ayrı |
| Özel alan: text | AssetCustomFieldEndpoints.cs | T: Asset_custom_field_name_update_preserves_key_and_writes_audit | DOĞRULANMADI — alan türü bazlı tam kabul açık |
| Özel alan: dropdown | AssetCustomFieldEndpoints.cs | Tür bazlı test eşlemesi açık | DOĞRULANMADI |
| Özel alan: checkbox | AssetCustomFieldEndpoints.cs | Tür bazlı test eşlemesi açık | DOĞRULANMADI |
| Özel alan: tarih | AssetCustomFieldEndpoints.cs | Tür bazlı test eşlemesi açık | DOĞRULANMADI |
| Fotoğraf/dosya | AssetEndpoints.cs | T: Asset_attachment_upload_download_and_delete_are_authorized_tenant_scoped_and_audited | DOĞRULANMADI — tam UI/limit matrisi eşlenecek |
| Timeline | InventoryMovementEndpoints.cs | T: Asset_notes_require_write_access_and_are_audited_in_the_tenant_history | DOĞRULANMADI — bütün işlem türlerinin geçmişi ayrı doğrulanmalı |

## 7.2 Zimmet ve yaşam döngüsü

| Madde | Kod izi | Test izi / kanıt | Tam kapsam durumu ve kalan doğrulama |
|---|---|---|---|
| Kullanıcıya checkin/checkout | InventoryMovementEndpoints.cs | T: Asset_assignment_accepts_a_valid_expected_return_and_return_clears_it_with_history | DOĞRULANMADI — gerçek müşteri tam kabulü açık |
| Lokasyona checkin/checkout | InventoryMovementEndpoints.cs | Hedef türüne özel test eşlenecek | DOĞRULANMADI |
| Varlığa checkin/checkout | InventoryMovementEndpoints.cs | T: Asset_hierarchy_is_tenant_scoped_and_rejects_cycles; hiyerarşi checkout yerine geçmez | DOĞRULANMADI |
| Zimmet timeline | InventoryMovementEndpoints.cs | T: Asset_transfers_are_tenant_scoped_and_appear_in_the_transfer_history_filter | DOĞRULANMADI |
| Toplu zimmet/iade | InventoryMovementEndpoints.cs | Ayrı atomiklik/rol/UI kabulü eşlenecek | DOĞRULANMADI |
| PDF | EmployeeHandoverPdf.cs | TenantSessionAcceptanceTests.HandoverPdf.cs; handover-pdf-20261026.md | DOĞRULANMADI — yerel API/render kanıtlı; gerçek Santa imzalı belge açık |
| Dijital imza | EmployeeHandoverEndpoints.cs | T: Employee_handover_documents_snapshot_company_inventory_and_keep_signatures_immutable | ENGELLENDİ — gerçek Santa imzasını kullanıcı atmalı |
| E-posta | EmployeeHandoverEmail.cs | TenantSessionAcceptanceTests.HandoverEmail.cs; production-handover-pdf-email-release-20261026.md | DOĞRULANMADI — gerçek imzalı belge alıcı teslimi açık |
| QR | EmployeeHandoverPdf.cs | Belge QR'sı/etiket QR'sı ayrı kapsam; uçtan uca eşleme açık | DOĞRULANMADI |
| Bekleyen talepler | InventoryRequestEndpoints.cs | Ayrı talep/rol/UI testleri eşlenecek | DOĞRULANMADI |
| Rezervasyon | AssetReservationEndpoints.cs | T: Asset_reservations_reject_date_overlaps_require_review_permissions_and_stay_in_tenant | DOĞRULANMADI — bütün UI durumları eşlenecek |

## 7.3 Lisans

| Madde | Kod izi | Test izi / kanıt | Tam kapsam durumu ve kalan doğrulama |
|---|---|---|---|
| Ad/koltuk/kalan/bitiş/anahtar | SoftwareLicenseEndpoints.cs | T: Software_license_create_update_and_read_are_tenant_scoped_audited_and_csrf_protected | DOĞRULANMADI — her alan/sınır ayrı eşlenecek |
| Koltuk atama/geri alma | SoftwareLicenseEmployeeEndpoints.cs | T: Software_license_bulk_assignment_is_atomic_and_tenant_scoped | DOĞRULANMADI — tüm hedefler/geri alma/UI eşlemesi açık |
| Dosya yükleme | SoftwareLicenseEndpoints.cs | T: Software_license_attachments_are_validated_audited_and_tenant_scoped | DOĞRULANMADI — tam parite/UI açık |
| 30/15/7/3/1 gün uyarıları | OperationalNotificationEndpoints.cs; OperationalNotificationEmailService.cs | Beş eşik ve yinelenmeme kanıtı ayrı eşlenecek | DOĞRULANMADI |
| Maliyet takibi | SoftwareLicenseEndpoints.cs; PurchaseOverviewEndpoints.cs | LicensePurchaseOrderAcceptanceTests.cs; tam maliyet raporu ayrı | DOĞRULANMADI |

## 7.4 Aksesuar/sarf/bileşen/kit

| Madde | Kod izi | Test izi / kanıt | Tam kapsam durumu ve kalan doğrulama |
|---|---|---|---|
| Aksesuar CRUD/zimmet | OperationalEndpoints.cs | T: Accessory_filter_options_and_filters_are_company_scoped yalnız filtre kanıtıdır | DOĞRULANMADI — tüm CRUD/zimmet/rol/UI eşlemesi açık |
| Sarf CRUD/min stok | OperationalEndpoints.cs | T: Consumable_search_stock_filters_pagination_and_csv_export_match_company_scope | DOĞRULANMADI — stok eşiği/uyarı ve yazma kabulü ayrı |
| Bileşen CRUD/varlığa takma | OperationalEndpoints.cs | T: Asset_detail_shows_open_accessory_and_component_relations_only_for_its_company | DOĞRULANMADI — ilişki okuma tüm CRUD/takma kabulü değildir |
| Kit CRUD/zimmet | AccessoryKitEndpoints.cs | T: Accessory_kits_support_company_scoped_crud_safe_export_and_archive_restore; Accessory_kit_requests_are_company_scoped_and_approve_all_stock_lines_atomically | DOĞRULANMADI — tüm parite/UI açık |
| Her türde stok uyarısı | OperationalNotificationEndpoints.cs | Tür bazında eşik/tekrar/tenant testleri eşlenecek | DOĞRULANMADI |

## 7.5 Bakım/onarım/denetim

| Madde | Kod izi | Test izi / kanıt | Tam kapsam durumu ve kalan doğrulama |
|---|---|---|---|
| Arıza/bakım/onarım tip/tarih/maliyet | OperationalEndpoints.cs | T: Asset_maintenance_start_and_completion_preserve_authorization_state_and_history; Maintenance_report_aggregates_date_filtered_status_and_costs_without_cross_company_data | DOĞRULANMADI — her tip/alan/UI ayrı |
| Fiziksel sayım | OperationalEndpoints.cs | T: Asset_physical_audit_submission_is_authorized_validated_tenant_scoped_and_written_to_history | DOĞRULANMADI — tam saha/arayüz matrisi açık |
| Bakım geçmişi | OperationalEndpoints.cs | T: Scheduled_maintenance_cancellation_enforces_csrf_company_and_role_scope_and_preserves_history | DOĞRULANMADI |
| Bakım dosya eki | MaintenanceAttachmentEndpoints.cs | T: Maintenance_attachments_are_validated_audited_and_included_in_company_archive | DOĞRULANMADI — tam UI kapsamı açık |

## 7.6 Kullanıcı/yetki

| Madde | Kod izi | Test izi / kanıt | Tam kapsam durumu ve kalan doğrulama |
|---|---|---|---|
| Kullanıcı CRUD | AccountEndpoints.cs; OperationalEndpoints.cs | Employee_create_and_edit_records_do_not_create_login_accounts_and_are_tenant_scoped; çalışan ve giriş hesabı ayrıdır | DOĞRULANMADI — hesap ve çalışan CRUD ayrı eşlenecek |
| Profil fotoğrafı | AccountEndpoints.cs | T: Employee_profile_photo_upload_replace_remove_enforces_company_role_csrf_and_two_mib_limit yalnız çalışan fotoğrafıdır | DOĞRULANMADI — hesap profil fotoğrafı ayrı kanıtla eşlenecek |
| Departman/lokasyon/yönetici | OperationalEndpoints.cs | T: Employee_edit_and_manager_hierarchy_are_company_scoped_validated_and_audited | DOĞRULANMADI |
| CSV içe aktarma | EmployeeImportMappingProfileEndpoints.cs; OperationalEndpoints.cs | T: Employee_csv_import_preview_is_read_only_and_commit_is_atomic_round_trips_managers_and_audits | DOĞRULANMADI — tüm hata/UI matrisi açık |
| Devre dışı bırakınca otomatik iade | OperationalEndpoints.cs | Çalışan pasife alma/stock scope testleri ayrı eşlenecek | DOĞRULANMADI |
| Süper admin/Tenant admin/Editör/Okuyucu/Sınırlı/Muhasebe | CompanyRoleEndpoints.cs; CompanyRoleAuthorization.cs | T: Company_roles_follow_the_asset_stock_employee_and_settings_policy_matrix | DOĞRULANMADI — adlandırılmış altı rolün tüm matris ve canlı kabulü ayrı |

## 7.7 Diğer

| Madde | Kod izi | Test izi / kanıt | Tam kapsam durumu ve kalan doğrulama |
|---|---|---|---|
| Dosya yükleme | Medya/ek endpointleri | Modül başına testler yukarıda | DOĞRULANMADI — tek ortak test tüm modülleri kapsamaz |
| Activity log | PlatformEndpoints.cs; InventoryMovementEndpoints.cs | T: Platform_audit_search_filters_actor_action_target_and_date_with_stable_paging | DOĞRULANMADI — müşteri/global audit ayrı |
| Varlık/lisans/aksesuar/bakım/denetim/maliyet/zimmet raporları | DataExportEndpoints.cs; OperationalEndpoints.cs; DepreciationReportEndpoints.cs | T: Asset_summary_report_filters_purchase_dates_and_scopes_category_status_and_currency_totals ve modül testleri | DOĞRULANMADI — yedi raporun ayrı kabulü açık |
| Excel/CSV içe-dışa | XlsxExportWriter.cs; modül import/export endpointleri | T: Employee_xlsx_import_preview_and_commit_preserve_values_and_audit | DOĞRULANMADI — bütün modüller ve iki biçim ayrı |
| LDAP | Bağlayıcı doğrulanmadı | C# kayıt taramasında LdapConnection bulunmadı; negatif arama tek başına yokluk kanıtı değildir | DOĞRULANMADI — entegrasyon kapsam incelemesi gerekli |
| Active Directory | Bağlayıcı doğrulanmadı | PRODUCT-COVERAGE kısmi/eksik kaydı; güncel uygulama kabulü yok | DOĞRULANMADI |
| SAML/SSO | Bağlayıcı doğrulanmadı | AddSaml/AddOpenIdConnect/Saml2 C# kayıt izi bulunmadı | DOĞRULANMADI |
| 2FA | AccountEndpoints.cs | TenantSessionAcceptanceTests.cs; ayrı cihaz MFA/kurulum testleri | DOĞRULANMADI — tüm UI/rol/giriş kapsamı ayrı |
| Google OAuth | Bağlayıcı doğrulanmadı | AddGoogle C# kayıt izi bulunmadı | DOĞRULANMADI |
| REST API/API anahtarı | CustomerApiEndpoints.cs | T: Customer_api_tokens_are_hashed_scoped_tenant_bound_and_revocable | DOĞRULANMADI — tam endpoint/rol/tenant/UI matrisi açık |
| Logo/para birimi/tarih/barkod ayarları | OperationalEndpoints.cs; wwwroot/index.html | Alan bazlı kabul eşlenecek | DOĞRULANMADI |
| E-posta bildirimleri | OperationalNotificationEmailService.cs | google-relay-production-acceptance-20261025.md yalnız belirtilen gerçek iletileri kanıtlar | DOĞRULANMADI — her bildirim ve alıcı sınırı ayrı |
| SQL yedeği | deploy/coolify yedek/kurtarma betikleri | activation-atomicity-server-staging-20261026.md: gerçek dump/restore/kurtarma | DOĞRULANMADI — ürünün tek tık yetkili yedek UI kabulü ayrı |
| Excel/CSV/JSON yedeği | DataExportEndpoints.cs | Modül export/restore testleri ayrı eşlenecek | DOĞRULANMADI — export tam kurtarılabilir yedek yerine geçmez |

## 12 — P0 kayıt/ödeme/kurulum kanıt ayrımı

Bu beş satır, üstteki74parite satırına yeni tamamlanma puanı eklemez. API düzeyindeki test kapsamı ile gerçek sağlayıcı/müşteri kabulü ayrı gösterilir. Tarihsel raporların “henüz deploy edilmedi” notları kendi tarihlerine aittir; sonraki üretim raporu ayrıca verilir.

| Madde | Kod | İncelenen test/sonuç kanıtı | Gerçek kapsam durumu |
|---|---|---|---|
| 12.1 Kayıt → subdomain → e-posta → plan → ödeme → webhook → provisioning → 5adım | CloudTrialEndpoints.cs; CheckoutEndpoints.cs; TenantProvisioningService.cs; CompanyOnboardingEndpoints.cs | p0-cloud-trial-acceptance-20261023.md; checkout-email-verification-acceptance-20261024.md; company-onboarding-20261026.md; onboarding-production-release-20261026.md | ENGELLENDİ — teknik alt akışlar ve5adımrehberi mevcut; gerçek merchant ve müşterinin baştan sona tamamlama kabulü yok |
| 12.2 AynıpaymentIdikinci tenant açmaz | CheckoutEndpoints.cs; TenantProvisioningService.cs | T: Public_checkout_uses_server_price_verifies_provider_and_provisions_once; p0d-checkout-webhook-provisioning-acceptance-20261023.md:1/1 sentetik sağlayıcı/PostgreSQL; annual-lifecycle-acceptance-20261024.md:callbackreplay | DOĞRULANMADI — belirtilen sentetik replay kabulü geçti; gerçek iyzico webhook kabulü açık |
| 12.3 Başarısız ödeme tenant açmaz; unpaidpending24saattensonratemizlenir | CheckoutEndpoints.cs; CompanyCheckoutOrderExpirationService.cs | CheckoutOrderExpirationAcceptanceTests.cs:Checkout_expiration_marks_only_overdue_unpaid_orders_and_releases_initialization_leases; p0d-checkout-order-expiration-acceptance-20261023.md:1/1; ücretli bildirim regresyonu32/32 | DOĞRULANMADI — süre dolumu kayıt silmek yerineExpiredyapar ve leasebırakır; gerçek başarısız ödeme kabulü açık. Ödenmiş/inceleme/provisioninghatası kayıtları süpürülmez |
| 12.4 Tek provisioning; slug/ownerrolü/hoşgeldin; hata geri alması/log | TenantProvisioningService.cs; CheckoutEndpoints.cs; CloudTrialEndpoints.cs | TenantProvisioningAcceptanceTests.cs:Shared_tenant_provisioning_requires_transaction_and_rolls_back_all_new_records; paid-provisioning-email-20261026.md:32/32; trial-welcome-20261026.md:11/11örtüşen grup; paid-trial-notification-release-20261026.md:yayın | DOĞRULANMADI — DBgeri alması veSMTPkabulzorunluluğu kanıtlı. Kabul edilmiş dışe-posta geri alınamaz; dağıtıkexactly-onceiddiasıyok; gerçekmerchant akışı açık |
| 12.5 Deneme/kupon bitince erişim kısıtlanır, veri korunur | TenantSubscriptionAccess.cs; TenantPlanFeatures.cs | T:Expired_cloud_subscription_preserves_read_access_and_blocks_company_writes; Current_cloud_subscription_is_selected_when_company_keeps_expired_subscription_history; CloudTrialAcceptanceTests.cs | DOĞRULANMADI — süresi dolanabonelikte okuma korunur/yazma kapanır testi var; kuponun tam kapsamı ve gerçek müşteri bitiş davranışı ayrı kabul ister |

## Diğer bölümler için zorunlu üst kabul bağlantıları

Bu tablo7.1–7.7'nin kapsamıdır; tüm Hedefin yerine geçmez. Bölüm6tenant/globalfilter/cache/queue/dosya, Bölüm8modern özellik/flag, Bölüm9platformun tüm kart/sekme/aksiyonları, Bölüm10portal, Bölüm11satış sitesi, Bölüm12ödeme/provisioning/onboarding, Bölüm13UI kategori matrisi ve Bölüm20gerçekmüşteri kabulü ayrıca tam satırlandırılmalıdır. Bu üst kapsamlar eksikken bu tablodan genel yüzde hesaplanamaz.

- P0 gerçek merchant: hesap yok, checkout/deneme kapalı. Sentetik sağlayıcı gerçek ödeme değildir.
- P0 müşteri: Santa7/10tarihsel kanıtlı; gerçek imza/PDF alıcı, operatör/çıkış, Farma ve iki yönlü canlıGET/PUT, Demo3normalowner açık. Mevcut müşteri/yönetici oturumları giriş ekranında.
- Rehber: company-onboarding-20261026.md ve onboarding-production-release-20261026.md; teknik5adım var, gerçek müşteri tamamlama açık.
- Genel UI: docs/ui-inventory.md öğe sayısı test edilmiş öğe sayısı değildir. Her kategori için yüzde95kabul kanıtı yok.
- Sekiz kapı: yalnız gate-8raporunun varlığı diğer yedi kapıyı kapatmaz; gate-p0c/gate-p0d adları gate1–7yerine geçmez.

İlk inceleme sırasında doğrulanmış şey bu kapsam/iz dökümüdür. Hiçbir ürün satırı sırf dosya veya yöntem adı bulundu diye TAMAM'a yükseltilmedi. Sonraki inceleme önce P0 açıklarını ve mevcut somut hata kanıtlarını ele alır; entegrasyon/parite P2sırası korunur.
