# EnvanterTR statik arayüz envanteri

Oluşturulma: 2026-11-03T21:31:31.306Z

> Bu dosya mevcut HTML kaynaklarında bulunan statik arayüz öğelerinin dökümüdür. Bir öğenin listelenmesi onun çalıştığını kanıtlamaz. JavaScript ile sonradan oluşturulan kontroller, API/DB davranışı, rol varyasyonları ve erişilebilirlik ayrıca uçtan uca kabul gerektirir. Kaynak değiştikçe `node tools/Generate-UiInventory.mjs` ile yeniden üretin; `node tools/Generate-UiInventory.mjs --check` güncel olup olmadığını doğrular.

## Sayım

| Tür | Statik öğe |
|---|---:|
| Sayfa rotası | 65 |
| Menü grubu | 10 |
| Bağlantı | 53 |
| Düğme | 952 |
| Form | 113 |
| Girdi alanı | 621 |
| Seçim alanı | 204 |
| Metin alanı | 42 |
| Modal | 109 |
| Sekme | 5 |
| Tablo | 79 |
| **Toplam kayıt** | **2253** |

## Tam döküm

### Şirket uygulaması — `src/EnvanterTR.Web/wwwroot/index.html`

| # | Tür | Etiket / görünen metin | Kaynak ipucu |
|---:|---|---|---|
| 1 | Bağlantı | E Envanter TR | hedef=#overview |
| 2 | Bağlantı | tenantUrlLink | id=tenantUrlLink; başlangıçta gizli |
| 3 | Bağlantı | OpenAPI şemasını görüntüle ↗ | hedef=/openapi/customer-api-v1.json |
| 4 | Bağlantı | E Envanter TR | hedef=# |
| 5 | Bağlantı | İlk şirket kurulumunu başlat | hedef=/setup.html |
| 6 | Bağlantı | İlk şirket kurulumunu başlat | hedef=/setup.html |
| 7 | Bağlantı | Platform yönetici girişi | hedef=/platform-admin.html |
| 8 | Bağlantı | licenseEulaDocumentLink | id=licenseEulaDocumentLink |
| 9 | Bağlantı | Verilerimi indir (JSON) | id=downloadAccountData; hedef=/api/session/data-export |
| 10 | Bağlantı | ${escapeHtml(a.eulaVersion\|\|'EULA')} | hedef=/api/software-licenses/${id}/attachments/${a.eulaAttachmentId}/file |
| 11 | Bağlantı | ${escapeHtml(a.originalFileName)} | hedef=/api/software-licenses/${id}/attachments/${a.id}/file |
| 12 | Bağlantı | ${escapeHtml(file.originalFileName)} | hedef=${escapeHtml(file.fileUrl)} |
| 13 | Bağlantı | ${escapeHtml(item.originalFileName)} | hedef=/api/asset-orders/${orderId}/attachments/${item.id}/file |
| 14 | Bağlantı | Aç / indir | hedef=/api/accessory-orders/${orderId}/attachments/${item.id}/file |
| 15 | Bağlantı | ${escapeHtml(file.originalFileName)} | hedef=/api/models/${modelId}/attachments/${file.id}/file?download=true |
| 16 | Düğme | Kapat | id=closeEmployeeHandover; type=button |
| 17 | Düğme | Yeni zimmet belgesi oluştur | id=createEmployeeHandover; type=button |
| 18 | Düğme | İmzayı temizle | id=clearEmployeeHandoverRecipient; type=button |
| 19 | Düğme | İmzayı temizle | id=clearEmployeeHandoverIssuer; type=button |
| 20 | Düğme | Vazgeç | id=cancelEmployeeHandoverSigning; type=button |
| 21 | Düğme | İmzaları kaydet ve belgeyi yazdır | id=saveEmployeeHandoverSignatures; type=button |
| 22 | Düğme | Kapat | id=maintenanceAttachmentClose; type=button |
| 23 | Düğme | Dosyayı yükle | type=submit |
| 24 | Düğme | Şirket değiştir | id=companySwitchButton; type=button; başlangıçta gizli |
| 25 | Düğme | Genel Bakış | hedef=Genel Bakış |
| 26 | Düğme | inventory | hedef=inventory |
| 27 | Düğme | Varlıklar | hedef=Varlıklar |
| 28 | Düğme | Tüm satın almalar | hedef=Tüm satın almalar |
| 29 | Düğme | Varlık siparişleri | hedef=Varlık siparişleri |
| 30 | Düğme | Varlık modelleri | hedef=Varlık modelleri |
| 31 | Düğme | Markalar | hedef=Markalar |
| 32 | Düğme | Kategoriler | hedef=Kategoriler |
| 33 | Düğme | Durum etiketleri | hedef=Durum etiketleri |
| 34 | Düğme | QR ve barkodlar | hedef=QR ve barkodlar |
| 35 | Düğme | movement | hedef=movement |
| 36 | Düğme | Zimmetler | hedef=Zimmetler |
| 37 | Düğme | İadeler | hedef=İadeler |
| 38 | Düğme | Transferler | hedef=Transferler |
| 39 | Düğme | Rezervasyonlar | hedef=Rezervasyonlar |
| 40 | Düğme | Hareket geçmişi | hedef=Hareket geçmişi |
| 41 | Düğme | software | hedef=software |
| 42 | Düğme | Yazılım lisansları | hedef=Yazılım lisansları |
| 43 | Düğme | Lisans atamaları | hedef=Lisans atamaları |
| 44 | Düğme | Lisanslarım | hedef=Lisanslarım |
| 45 | Düğme | Sözleşmeler | hedef=Sözleşmeler |
| 46 | Düğme | Garanti takibi | hedef=Garanti takibi |
| 47 | Düğme | supplies | hedef=supplies |
| 48 | Düğme | Aksesuarlar | hedef=Aksesuarlar |
| 49 | Düğme | Siparişler | hedef=Siparişler |
| 50 | Düğme | Sarf malzemeleri | hedef=Sarf malzemeleri |
| 51 | Düğme | Stok talepleri | hedef=Stok talepleri |
| 52 | Düğme | Bileşenler | hedef=Bileşenler |
| 53 | Düğme | Hazır kitler | hedef=Hazır kitler |
| 54 | Düğme | Stok hareketleri | hedef=Stok hareketleri |
| 55 | Düğme | people | hedef=people |
| 56 | Düğme | Çalışan kayıtları | hedef=Çalışan kayıtları |
| 57 | Düğme | Uygulama kullanıcıları | id=membersNav; hedef=Uygulama kullanıcıları |
| 58 | Düğme | Departmanlar | hedef=Departmanlar |
| 59 | Düğme | Yetki rolleri | hedef=Yetki rolleri |
| 60 | Düğme | places | hedef=places |
| 61 | Düğme | Lokasyonlar | hedef=Lokasyonlar |
| 62 | Düğme | Binalar ve odalar | hedef=Binalar ve odalar |
| 63 | Düğme | Şirket birimleri | hedef=Şirket birimleri |
| 64 | Düğme | operations | hedef=operations |
| 65 | Düğme | Bakım kayıtları | hedef=Bakım kayıtları |
| 66 | Düğme | Bakım planları | hedef=Bakım planları |
| 67 | Düğme | Denetimler | hedef=Denetimler |
| 68 | Düğme | Takvim | hedef=Takvim |
| 69 | Düğme | Görevler | hedef=Görevler |
| 70 | Düğme | reports | hedef=reports |
| 71 | Düğme | Hazır raporlar | hedef=Hazır raporlar |
| 72 | Düğme | Özel rapor oluştur | hedef=Özel rapor oluştur |
| 73 | Düğme | Dışa aktarımlar | hedef=Dışa aktarımlar |
| 74 | Düğme | data | hedef=data |
| 75 | Düğme | Tablodan içe aktar | hedef=Tablodan içe aktar |
| 76 | Düğme | Verileri dışa aktar | hedef=Verileri dışa aktar |
| 77 | Düğme | Yedekleme geçmişi | hedef=Yedekleme geçmişi |
| 78 | Düğme | Geri yükle | hedef=Geri yükle |
| 79 | Düğme | settings | hedef=settings |
| 80 | Düğme | Firma profili | hedef=Firma profili |
| 81 | Düğme | Logo ve tema | hedef=Logo ve tema |
| 82 | Düğme | Bildirimler | hedef=Bildirimler |
| 83 | Düğme | Entegrasyonlar | hedef=Entegrasyonlar |
| 84 | Düğme | Güvenlik | hedef=Güvenlik |
| 85 | Düğme | Denetim kayıtları | hedef=Denetim kayıtları |
| 86 | Düğme | Destek | hedef=Destek |
| 87 | Düğme | Menüyü aç | id=mobileMenu |
| 88 | Düğme | Görünüm ayarları | id=appearanceButton; type=button |
| 89 | Düğme | Yardım | id=helpButton |
| 90 | Düğme | Yaklaşan işler | id=notificationButton; type=button; başlangıçta gizli |
| 91 | Düğme | Şirket takvimini aç → | id=openNotificationCalendar; type=button |
| 92 | Düğme | Profil fotoğrafını yönet | id=userAvatar; type=button |
| 93 | Düğme | Çıkış | id=logoutButton; type=button |
| 94 | Düğme | Platform yönetimine dön | id=impersonationReturn; type=button |
| 95 | Düğme | ▥ Barkod okut | id=scanAssetButton; type=button |
| 96 | Düğme | ＋ Yeni varlık | id=newAssetButton |
| 97 | Düğme | Panoyu düzenle | id=dashboardCustomizeButton; type=button |
| 98 | Düğme | Varlık CSV indir | id=exportButton; type=button |
| 99 | Düğme | Varlık Excel indir | id=exportXlsxButton; type=button |
| 100 | Düğme | Çöp kutusu | id=openAssetTrash; type=button |
| 101 | Düğme | Şirket verisini indir | id=exportBackupButton; type=button; başlangıçta gizli |
| 102 | Düğme | Aktarma dosyasını kontrol et | id=importPreviewButton; type=button; başlangıçta gizli |
| 103 | Düğme | Varlıklar | hedef=Varlıklar |
| 104 | Düğme | Aramayı kaydet | id=saveAssetSearch; type=button |
| 105 | Düğme | Kaydı sil | id=deleteAssetSearch; type=button |
| 106 | Düğme | İlişki ve garanti filtrelerini temizle | id=clearAssetRelationFilter; type=button; başlangıçta gizli |
| 107 | Düğme | Varsayılan sütunlar | id=resetAssetColumns; type=button |
| 108 | Düğme | Filtreli varlık listesini yazdır | id=assetPrintButton; type=button |
| 109 | Düğme | Varlık listesini tam ekranda aç | id=assetFullscreenButton; type=button |
| 110 | Düğme | Durumu uygula | id=applyBulkAssetStatus; type=button |
| 111 | Düğme | Seçileni CSV indir | id=exportSelectedAssets; type=button |
| 112 | Düğme | Seçimi temizle | id=clearSelectedAssets; type=button |
| 113 | Düğme | Demirbaş Etiketi | type=button |
| 114 | Düğme | Model | type=button |
| 115 | Düğme | Kategori | type=button |
| 116 | Düğme | Durum | type=button |
| 117 | Düğme | Çıkış Yapılmış Olan Kişi | type=button |
| 118 | Düğme | Konum | type=button |
| 119 | Düğme | Üretici | type=button |
| 120 | Düğme | ← Önceki | id=assetPrev |
| 121 | Düğme | Sonraki → | id=assetNext |
| 122 | Düğme | Hareket geçmişi | hedef=Hareket geçmişi |
| 123 | Düğme | Takvim | hedef=Takvim |
| 124 | Düğme | Hazır raporlar | hedef=Hazır raporlar |
| 125 | Düğme | ↓ CSV dışa aktar | id=exportEmployeesButton; type=button |
| 126 | Düğme | Excel (.xlsx) | id=employeeXlsxExport; type=button |
| 127 | Düğme | ↑ CSV içe aktar | id=employeeImportOpen; type=button; başlangıçta gizli |
| 128 | Düğme | ＋ Çalışan ekle | id=newEmployeeButton |
| 129 | Düğme | Departmanı temizle | id=clearEmployeeDepartmentFilter; type=button; başlangıçta gizli |
| 130 | Düğme | Yöneticiyi temizle | id=clearEmployeeManagerFilter; type=button; başlangıçta gizli |
| 131 | Düğme | Tarihleri temizle | id=clearEmployeeStartedDateFilter; type=button; başlangıçta gizli |
| 132 | Düğme | Sayfadakileri seç | id=selectEmployeesOnPage; type=button; başlangıçta gizli |
| 133 | Düğme | Seçilenleri güncelle | id=applyEmployeeBulkActive; type=button; başlangıçta gizli |
| 134 | Düğme | ← Önceki | id=employeePrev |
| 135 | Düğme | Sonraki → | id=employeeNext |
| 136 | Düğme | + Departman ekle | id=addDepartmentButton; type=button; başlangıçta gizli |
| 137 | Düğme | + Konum ekle | id=addLocationButton; type=button; başlangıçta gizli |
| 138 | Düğme | ↻ Yenile | id=directoryRefresh; type=button |
| 139 | Düğme | ＋ Birim ekle | id=addCompanyUnitButton; type=button; başlangıçta gizli |
| 140 | Düğme | ↻ Yenile | id=companyUnitsRefresh; type=button |
| 141 | Düğme | Kapat | id=companyUnitDialogClose; type=button |
| 142 | Düğme | İptal | id=companyUnitCancel; type=button |
| 143 | Düğme | Kaydet | type=submit |
| 144 | Düğme | Varlıkları aç | id=moduleGoAssets; type=button |
| 145 | Düğme | Çalışanları aç | id=moduleGoPeople; type=button |
| 146 | Düğme | CSV indir | id=accessoryExport; type=button |
| 147 | Düğme | Excel (.xlsx) | id=accessoryXlsxExport; type=button |
| 148 | Düğme | ↑ CSV içe aktar | id=accessoryImportOpen; type=button; başlangıçta gizli |
| 149 | Düğme | ↻ Yenile | id=accessoryRefresh; type=button |
| 150 | Düğme | Kategorileri yönet | id=manageAccessoryCategories; type=button; başlangıçta gizli |
| 151 | Düğme | ＋ Aksesuar ekle | id=newAccessoryButton; type=button |
| 152 | Düğme | Varsayılan sütunlar | id=resetAccessoryColumns; type=button |
| 153 | Düğme | Yazdır | id=accessoryPrint; type=button |
| 154 | Düğme | Tam ekran | id=accessoryFullscreen; type=button |
| 155 | Düğme | Filtreleri temizle | id=clearAccessoryAdvancedFilters; type=button |
| 156 | Düğme | ← Önceki | id=accessoryPrev; type=button |
| 157 | Düğme | Sonraki → | id=accessoryNext; type=button |
| 158 | Düğme | Sarf / bileşen siparişleri | id=openStockOrders; type=button |
| 159 | Düğme | CSV indir | id=accessoryOrderExport; type=button |
| 160 | Düğme | ↻ Yenile | id=accessoryOrdersRefresh; type=button |
| 161 | Düğme | Tedarikçiler | id=supplierCatalogButton; type=button |
| 162 | Düğme | ＋ Sipariş oluştur | id=newAccessoryOrderButton; type=button |
| 163 | Düğme | Filtreleri temizle | id=clearAccessoryOrderFilters; type=button |
| 164 | Düğme | ← Önceki | id=accessoryOrderPrev; type=button |
| 165 | Düğme | Sonraki → | id=accessoryOrderNext; type=button |
| 166 | Düğme | CSV indir | id=exportAccessoryKitsButton; type=button |
| 167 | Düğme | ↻ Yenile | id=accessoryKitsRefresh; type=button |
| 168 | Düğme | ＋ Kit oluştur | id=newAccessoryKitButton; type=button |
| 169 | Düğme | CSV indir | id=exportConsumablesButton; type=button |
| 170 | Düğme | Excel (.xlsx) | id=consumableXlsxExport; type=button |
| 171 | Düğme | ↑ CSV içe aktar | id=importConsumablesButton; type=button; başlangıçta gizli |
| 172 | Düğme | ＋ Sarf malzemesi ekle | id=newConsumableButton; type=button |
| 173 | Düğme | ← Önceki | id=consumablePrev; type=button |
| 174 | Düğme | Sonraki → | id=consumableNext; type=button |
| 175 | Düğme | ↓ Filtrelenmiş CSV | id=requestExport; type=button |
| 176 | Düğme | ↻ Yenile | id=requestsRefresh; type=button |
| 177 | Düğme | ← Önceki | id=requestPrev; type=button |
| 178 | Düğme | Sonraki → | id=requestNext; type=button |
| 179 | Düğme | Kapat | type=button |
| 180 | Düğme | Vazgeç | type=button |
| 181 | Düğme | Kaydet | id=inventoryRequestActionSubmit; type=submit |
| 182 | Düğme | ↓ CSV dışa aktar | id=licenseExportButton; type=button |
| 183 | Düğme | Excel (.xlsx) | id=licenseXlsxExport; type=button |
| 184 | Düğme | ↑ CSV içe aktar | id=licenseImportOpen; type=button; başlangıçta gizli |
| 185 | Düğme | ＋ Lisans ekle | id=newLicenseButton; type=button |
| 186 | Düğme | ← Önceki | id=licensePrev |
| 187 | Düğme | Sonraki → | id=licenseNext |
| 188 | Düğme | Yazılım lisansları | type=button; hedef=Yazılım lisansları |
| 189 | Düğme | ← Önceki | id=licenseAssignmentPrev |
| 190 | Düğme | Sonraki → | id=licenseAssignmentNext |
| 191 | Düğme | Yenile | id=myLicensesRefresh; type=button |
| 192 | Düğme | Yenile | id=companyNotificationPreferencesRefresh; type=button |
| 193 | Düğme | Değişiklikleri geri al | id=companyNotificationPreferencesReset; type=button |
| 194 | Düğme | Tercihleri kaydet | id=companyNotificationPreferencesSave; type=submit |
| 195 | Düğme | CSV indir | id=exportComponentsButton; type=button |
| 196 | Düğme | Excel (.xlsx) | id=componentXlsxExport; type=button |
| 197 | Düğme | ↑ CSV içe aktar | id=importComponentsButton; type=button; başlangıçta gizli |
| 198 | Düğme | ＋ Bileşen ekle | id=newComponentButton; type=button |
| 199 | Düğme | ← Önceki | id=componentPrev |
| 200 | Düğme | Sonraki → | id=componentNext |
| 201 | Düğme | CSV indir | id=exportInventoryMovementsButton; type=button |
| 202 | Düğme | ← Önceki | id=inventoryMovementPrev; type=button |
| 203 | Düğme | Sonraki → | id=inventoryMovementNext; type=button |
| 204 | Düğme | Varsayılanlara dön | id=labelSettingsReset; type=button |
| 205 | Düğme | Şablon ayarlarını yükle | id=labelStockTemplateLoad; type=button |
| 206 | Düğme | Şablonu kaydet | id=labelStockTemplateSave; type=button |
| 207 | Düğme | Şablonu sil | id=labelStockTemplateDelete; type=button |
| 208 | Düğme | Ayarları kaydet | id=labelSettingsSave; type=submit |
| 209 | Düğme | Varlık listesini aç | id=labelSettingsGoAssets; type=button |
| 210 | Düğme | Değişiklikleri kaydet | id=assetStatusLabelsSave; type=submit |
| 211 | Düğme | ＋ Özel durum ekle | id=addAssetCustomStatus; type=button |
| 212 | Düğme | Durum ve geçiş kurallarını kaydet | id=assetStatusConfigSave; type=submit |
| 213 | Düğme | ↻ Yenile | id=membersRefresh |
| 214 | Düğme | ＋ Kullanıcı davet et | id=inviteMemberButton |
| 215 | Düğme | ↻ Yenile | id=catalogRefresh; type=button |
| 216 | Düğme | ＋ Yeni marka ekle | id=catalogQuickBrand; type=button |
| 217 | Düğme | Kaydı ekle | type=submit |
| 218 | Düğme | Değişiklikleri kaydet | type=submit |
| 219 | Düğme | Logoyu güvenli şekilde yükle | type=submit |
| 220 | Düğme | Varlıklar | type=button; hedef=Varlıklar |
| 221 | Düğme | ← Önceki | id=assignmentPrev; type=button |
| 222 | Düğme | Sonraki → | id=assignmentNext; type=button |
| 223 | Düğme | ↻ Yenile | id=returnsRefresh; type=button |
| 224 | Düğme | ← Önceki | id=returnsPrev; type=button |
| 225 | Düğme | Sonraki → | id=returnsNext; type=button |
| 226 | Düğme | ↻ Yenile | id=activityRefresh |
| 227 | Düğme | ← Önceki | id=activityPrev |
| 228 | Düğme | Sonraki → | id=activityNext |
| 229 | Düğme | ↓ CSV dışa aktar | id=reservationExportCsv; type=button |
| 230 | Düğme | ↻ Yenile | id=reservationRefresh; type=button |
| 231 | Düğme | Rezervasyon iste | id=reservationSubmit; type=submit |
| 232 | Düğme | ← Önceki | id=reservationPrev; type=button |
| 233 | Düğme | Sonraki → | id=reservationNext; type=button |
| 234 | Düğme | Kapat | id=reservationActionClose; type=button |
| 235 | Düğme | Vazgeç | id=reservationActionCancel; type=button |
| 236 | Düğme | Kaydet | id=reservationActionSubmit; type=submit |
| 237 | Düğme | ↻ Yenile | id=auditRefresh |
| 238 | Düğme | ↻ Yenile | id=auditScheduleRefresh; type=button |
| 239 | Düğme | ← Önceki | id=auditSchedulePrev; type=button |
| 240 | Düğme | Sonraki → | id=auditScheduleNext; type=button |
| 241 | Düğme | CSV dışa aktar | id=auditExportCsv; type=button |
| 242 | Düğme | ← Önceki | id=auditPrev |
| 243 | Düğme | Sonraki → | id=auditNext |
| 244 | Düğme | Şirket verisini indir (JSON) | id=dataDownloadCompany; type=button |
| 245 | Düğme | Şirket verisi ve dosyaları (ZIP) | id=dataDownloadCompanyArchive; type=button |
| 246 | Düğme | Varlık listesini indir (CSV) | id=dataDownloadAssets; type=button |
| 247 | Düğme | Varlıklar | type=button; hedef=Varlıklar |
| 248 | Düğme | Çalışan kayıtları | type=button; hedef=Çalışan kayıtları |
| 249 | Düğme | Aksesuarlar | type=button; hedef=Aksesuarlar |
| 250 | Düğme | Sarf malzemeleri | type=button; hedef=Sarf malzemeleri |
| 251 | Düğme | Bileşenler | type=button; hedef=Bileşenler |
| 252 | Düğme | Yazılım lisansları | type=button; hedef=Yazılım lisansları |
| 253 | Düğme | JSON dosyasını kontrol et | id=dataValidateFile; type=button |
| 254 | Düğme | ↓ CSV dışa aktar | id=exportContractsButton; type=button |
| 255 | Düğme | ↻ Yenile | id=refreshContractsButton; type=button |
| 256 | Düğme | ＋ Sözleşme ekle | id=newContractButton; type=button |
| 257 | Düğme | ← Önceki | id=contractPrev; type=button |
| 258 | Düğme | Sonraki → | id=contractNext; type=button |
| 259 | Düğme | ↻ Yenile | id=tasksRefresh; type=button |
| 260 | Düğme | ＋ Görev oluştur | id=taskNewButton; type=button |
| 261 | Düğme | Kapat | id=taskEditorClose; type=button |
| 262 | Düğme | Görevi kaydet | id=taskSaveButton; type=submit |
| 263 | Düğme | Vazgeç | id=taskEditorCancel; type=button |
| 264 | Düğme | ← Önceki | id=taskPrev; type=button |
| 265 | Düğme | Sonraki → | id=taskNext; type=button |
| 266 | Düğme | ↻ Yenile | id=supportRefresh; type=button |
| 267 | Düğme | ＋ Yeni destek talebi | id=supportNewButton; type=button |
| 268 | Düğme | Kapat | id=supportEditorClose; type=button |
| 269 | Düğme | Talebi gönder | id=supportSaveButton; type=submit |
| 270 | Düğme | ← Önceki | id=supportPrev; type=button |
| 271 | Düğme | Sonraki → | id=supportNext; type=button |
| 272 | Düğme | Talebi kapat | id=supportCloseTicket; type=button |
| 273 | Düğme | Kapat | id=supportDetailClose; type=button |
| 274 | Düğme | Yanıt gönder | id=supportReplyButton; type=submit |
| 275 | Düğme | ↻ Güvenlik durumunu yenile | id=securityOverviewRefresh; type=button; başlangıçta gizli |
| 276 | Düğme | Kendi hesap güvenliğini aç | id=featureStatusAccountSecurityButton; type=button; başlangıçta gizli |
| 277 | Düğme | Departmanları aç | id=featureStatusDepartmentsButton; type=button; başlangıçta gizli |
| 278 | Düğme | Genel Bakış | type=button; hedef=Genel Bakış |
| 279 | Düğme | ↻ Oturumları yenile | id=securitySessionsRefresh; type=button |
| 280 | Düğme | Güvenlik politikasını kaydet | id=companySecurityPolicySave; type=submit |
| 281 | Düğme | ↻ Yenile | id=integrationsRefresh; type=button |
| 282 | Düğme | API anahtarı oluştur | id=createIntegrationToken; type=submit |
| 283 | Düğme | Kapat | id=closeIntegrationSecret; type=button |
| 284 | Düğme | Göster | id=toggleIntegrationSecret; type=button |
| 285 | Düğme | Anahtarı kopyala | id=copyIntegrationSecret; type=button |
| 286 | Düğme | Kapat | id=dismissIntegrationSecret; type=button |
| 287 | Düğme | ⇩ CSV indir | id=maintenanceExport; type=button |
| 288 | Düğme | ↻ Yenile | id=maintenanceRefresh; type=button |
| 289 | Düğme | Filtrele | id=applyMaintenanceFilters; type=button |
| 290 | Düğme | Temizle | id=clearMaintenanceFilters; type=button |
| 291 | Düğme | ← Önceki | id=maintenancePrev; type=button |
| 292 | Düğme | Sonraki → | id=maintenanceNext; type=button |
| 293 | Düğme | ⇩ CSV indir | id=calendarExport; type=button |
| 294 | Düğme | ↻ Yenile | id=calendarRefresh; type=button |
| 295 | Düğme | Takvimi göster | id=applyCalendarFilters; type=button |
| 296 | Düğme | Önceki dönem | id=calendarPeriodPrev; type=button |
| 297 | Düğme | Bugün | id=calendarPeriodToday; type=button |
| 298 | Düğme | Sonraki dönem | id=calendarPeriodNext; type=button |
| 299 | Düğme | CSV indir | id=reportExport; type=button |
| 300 | Düğme | Excel (.xlsx) | id=reportXlsxExport; type=button |
| 301 | Düğme | Yazdır / PDF | id=reportPrint; type=button |
| 302 | Düğme | Rapor takvimi | id=reportScheduleOpen; type=button |
| 303 | Düğme | ↻ Verileri yenile | id=reportRefresh; type=button |
| 304 | Düğme | Tarih aralığını uygula | id=applyReportDateFilter; type=button |
| 305 | Düğme | Temizle | id=clearReportDateFilter; type=button |
| 306 | Düğme | Alış kayıtlarını CSV indir | id=assetPurchaseReportExport; type=button |
| 307 | Düğme | Yazdır / PDF | id=assetPurchaseReportPrint; type=button |
| 308 | Düğme | ← Önceki | id=assetPurchaseReportPrev; type=button |
| 309 | Düğme | Sonraki → | id=assetPurchaseReportNext; type=button |
| 310 | Düğme | Bileşen CSV indir | id=componentReportExport; type=button |
| 311 | Düğme | Yazdır / PDF | id=componentReportPrint; type=button |
| 312 | Düğme | Denetimleri CSV indir | id=auditReportExport; type=button |
| 313 | Düğme | Yazdır / PDF | id=auditReportPrint; type=button |
| 314 | Düğme | Filtrele | id=auditReportApply; type=button |
| 315 | Düğme | ← Önceki | id=auditReportPrev; type=button |
| 316 | Düğme | Sonraki → | id=auditReportNext; type=button |
| 317 | Düğme | CSV indir | id=warrantyReportExport; type=button |
| 318 | Düğme | Yazdır / PDF | id=warrantyReportPrint; type=button |
| 319 | Düğme | ↻ Yenile | id=warrantyReportRefresh; type=button |
| 320 | Düğme | ← Önceki | id=warrantyReportPrev |
| 321 | Düğme | Sonraki → | id=warrantyReportNext |
| 322 | Düğme | ↻ Yenile | id=maintenanceReportRefresh; type=button |
| 323 | Düğme | Yazdır / PDF | id=maintenanceReportPrint; type=button |
| 324 | Düğme | Filtrele | id=applyMaintenanceReportFilter; type=button |
| 325 | Düğme | Temizle | id=clearMaintenanceReportFilter; type=button |
| 326 | Düğme | CSV indir | id=licenseReportExport; type=button |
| 327 | Düğme | Yazdır / PDF | id=licenseReportPrint; type=button |
| 328 | Düğme | ↻ Yenile | id=licenseReportRefresh; type=button |
| 329 | Düğme | ← Önceki | id=licenseReportPrev |
| 330 | Düğme | Sonraki → | id=licenseReportNext |
| 331 | Düğme | ↻ Yenile | id=pendingDeliveryRefresh; type=button |
| 332 | Düğme | CSV indir | id=pendingDeliveryExport; type=button |
| 333 | Düğme | Yazdır / PDF | id=pendingDeliveryPrint; type=button |
| 334 | Düğme | ← Önceki | id=pendingDeliveryPrev; type=button |
| 335 | Düğme | Sonraki → | id=pendingDeliveryNext; type=button |
| 336 | Düğme | ↻ Hesapla | id=depreciationReportRefresh; type=button |
| 337 | Düğme | CSV indir | id=depreciationReportExport; type=button |
| 338 | Düğme | Yazdır / PDF | id=depreciationReportPrint; type=button |
| 339 | Düğme | ← Önceki | id=depreciationReportPrev; type=button |
| 340 | Düğme | Sonraki → | id=depreciationReportNext; type=button |
| 341 | Düğme | Taslağı yükle | id=customReportLoadPreset; type=button |
| 342 | Düğme | Taslağı kaydet | id=customReportSavePreset; type=button |
| 343 | Düğme | Taslağı sil | id=customReportDeletePreset; type=button |
| 344 | Düğme | Özel raporu CSV indir | id=customReportExport; type=button |
| 345 | Düğme | Özel raporu Excel indir | id=customReportXlsxExport; type=button |
| 346 | Düğme | Yazdır / PDF | id=customReportPrint; type=button |
| 347 | Düğme | Aksesuar siparişleri | id=backAccessoryOrders; type=button |
| 348 | Düğme | CSV dışa aktar | id=exportStockOrders; type=button |
| 349 | Düğme | ↻ Yenile | id=stockOrdersRefresh; type=button |
| 350 | Düğme | ＋ Sipariş oluştur | id=newStockOrderButton; type=button |
| 351 | Düğme | Filtreleri temizle | id=clearStockOrderFilters; type=button |
| 352 | Düğme | ← Önceki | id=stockOrderPrev; type=button |
| 353 | Düğme | Sonraki → | id=stockOrderNext; type=button |
| 354 | Düğme | ↻ Yenile | id=purchaseOverviewRefresh; type=button |
| 355 | Düğme | CSV indir | id=purchaseOverviewExport; type=button |
| 356 | Düğme | Filtreleri temizle | id=purchaseOverviewClear; type=button |
| 357 | Düğme | ← Önceki | id=purchaseOverviewPrev; type=button |
| 358 | Düğme | Sonraki → | id=purchaseOverviewNext; type=button |
| 359 | Düğme | Yıllık bütçe | id=assetOrderBudgetButton; type=button |
| 360 | Düğme | ↻ Yenile | id=assetOrdersRefresh; type=button |
| 361 | Düğme | CSV indir | id=exportAssetOrdersButton; type=button |
| 362 | Düğme | Tedarikçiler | id=assetOrdersSupplierCatalog; type=button |
| 363 | Düğme | ＋ Sipariş oluştur | id=newAssetOrderButton; type=button |
| 364 | Düğme | Filtreleri temizle | id=clearAssetOrderFilters; type=button |
| 365 | Düğme | ← Önceki | id=assetOrderPrev; type=button |
| 366 | Düğme | Sonraki → | id=assetOrderNext; type=button |
| 367 | Düğme | Uygulama kullanıcıları | id=rolesManageMembersButton; type=button |
| 368 | Düğme | Özel rol oluştur | id=createCompanyRoleButton; type=button; başlangıçta gizli |
| 369 | Düğme | Kapat | id=closeCompanyRoleDialog; type=button |
| 370 | Düğme | Vazgeç | id=cancelCompanyRoleDialog; type=button |
| 371 | Düğme | Kaydet | id=saveCompanyRoleButton; type=submit |
| 372 | Düğme | Kapat | id=closeDeactivateCompanyRole; type=button |
| 373 | Düğme | Vazgeç | id=cancelDeactivateCompanyRole; type=button |
| 374 | Düğme | Devre dışı bırak | id=confirmDeactivateCompanyRole; type=button |
| 375 | Düğme | Kapat | id=licenseRevokeClose; type=button |
| 376 | Düğme | Vazgeç | id=licenseRevokeCancel; type=button |
| 377 | Düğme | Atamayı kaldır | id=licenseRevokeSubmit; type=submit |
| 378 | Düğme | Kapat | id=licenseImportProfileDeleteClose; type=button |
| 379 | Düğme | Vazgeç | id=licenseImportProfileDeleteCancel; type=button |
| 380 | Düğme | Profili sil | id=licenseImportProfileDeleteSubmit; type=submit |
| 381 | Düğme | Kapat | id=commandPaletteClose; type=button |
| 382 | Düğme | Kapat | id=keyboardShortcutsClose; type=button |
| 383 | Düğme | Kapat | type=button |
| 384 | Düğme | Vazgeç | type=button |
| 385 | Düğme | Kaydet | type=submit |
| 386 | Düğme | Kapat | id=closeContractDetail; type=button |
| 387 | Düğme | Kapat | id=closeContractDetailFooter; type=button |
| 388 | Düğme | Kapat | id=contractActionClose; type=button |
| 389 | Düğme | Vazgeç | id=contractActionCancel; type=button |
| 390 | Düğme | Onayla | id=contractActionSubmit; type=submit |
| 391 | Düğme | Kapat | id=appearanceClose; type=button |
| 392 | Düğme | Sistem ayarını izle | type=button |
| 393 | Düğme | Açık | type=button |
| 394 | Düğme | Koyu | type=button |
| 395 | Düğme | Vazgeç | id=appearanceCancel; type=button |
| 396 | Düğme | Tercihleri kaydet | id=appearanceSave; type=button |
| 397 | Düğme | Kapat | id=closeDialog |
| 398 | Düğme | Genel bakış | id=assetTabOverviewButton; type=button |
| 399 | Düğme | İlişkiler | id=assetTabRelationsButton; type=button |
| 400 | Düğme | Bakım kayıtları | id=assetTabMaintenanceButton; type=button |
| 401 | Düğme | Hareket geçmişi | id=assetTabHistoryButton; type=button |
| 402 | Düğme | Fotoğraf ve dosyalar | id=assetTabAttachmentsButton; type=button |
| 403 | Düğme | detailPurchaseOrder | id=detailPurchaseOrder; type=button |
| 404 | Düğme | Etiketi kopyala | id=copyAssetTag; type=button |
| 405 | Düğme | Etiket kartını yazdır | id=printAssetLabel; type=button |
| 406 | Düğme | Bağlantıyı kaydet | id=saveAssetParent; type=button |
| 407 | Düğme | Not ekle | type=submit |
| 408 | Düğme | Dosya ekle | id=uploadAssetAttachment; type=submit |
| 409 | Düğme | Bilgileri düzenle | id=editAssetButton; type=button |
| 410 | Düğme | Denetimi kaydet | id=auditAssetButton; type=button |
| 411 | Düğme | Denetim planla | id=scheduleAssetAuditButton; type=button |
| 412 | Düğme | Bakım başlat | id=addMaintenanceButton; type=button |
| 413 | Düğme | Konum transferi | id=transferAssetButton; type=button |
| 414 | Düğme | Çalışana zimmetle | id=assignAssetButton; type=button |
| 415 | Düğme | İade al | id=returnAssetButton; type=button |
| 416 | Düğme | Arşivden geri al | id=restoreAssetButton; type=button; başlangıçta gizli |
| 417 | Düğme | Çöp kutusuna taşı | id=trashAssetButton; type=button |
| 418 | Düğme | Kapat | type=button |
| 419 | Düğme | Vazgeç | type=button |
| 420 | Düğme | Denetimi kaydet | type=submit |
| 421 | Düğme | Kapat | type=button |
| 422 | Düğme | Vazgeç | type=button |
| 423 | Düğme | Planı kaydet | type=submit |
| 424 | Düğme | Kapat | type=button |
| 425 | Düğme | Vazgeç | type=button |
| 426 | Düğme | Bakımı başlat | id=maintenanceSubmit; type=submit |
| 427 | Düğme | Kapat | type=button |
| 428 | Düğme | Vazgeç | type=button |
| 429 | Düğme | Bakımı tamamla | type=submit |
| 430 | Düğme | Kapat | type=button |
| 431 | Düğme | Vazgeç | type=button |
| 432 | Düğme | Transferi kaydet | type=submit |
| 433 | Düğme | Kapat | type=button |
| 434 | Düğme | Vazgeç | type=button |
| 435 | Düğme | Değişiklikleri kaydet | type=submit |
| 436 | Düğme | Kapat | type=button |
| 437 | Düğme | Vazgeç | type=button |
| 438 | Düğme | Zimmeti kaydet | type=submit |
| 439 | Düğme | Kapat | type=button |
| 440 | Düğme | Vazgeç | type=button |
| 441 | Düğme | Çalışanı kaydet | type=submit |
| 442 | Düğme | Kapat | id=closeEmployeeImport; type=button |
| 443 | Düğme | Şablonu indir | id=downloadEmployeeImportTemplate; type=button |
| 444 | Düğme | Dosyayı doğrula | id=previewEmployeeImport; type=button |
| 445 | Düğme | Doğrulanmış kayıtları ekle | id=commitEmployeeImport; type=button |
| 446 | Düğme | Kapat | id=closeConsumableImport; type=button |
| 447 | Düğme | Şablonu indir | id=downloadConsumableImportTemplate; type=button |
| 448 | Düğme | Eşlemeyi kaydet | id=saveConsumableImportProfile; type=button |
| 449 | Düğme | Profili sil | id=deleteConsumableImportProfile; type=button |
| 450 | Düğme | Eşlemeyi kullan ve yeniden doğrula | id=applyConsumableImportMapping; type=button |
| 451 | Düğme | Dosyayı doğrula | id=previewConsumableImport; type=button |
| 452 | Düğme | Doğrulanmış kayıtları ekle | id=commitConsumableImport; type=button |
| 453 | Düğme | Kapat | id=closeAccessoryImport; type=button |
| 454 | Düğme | Şablonu indir | id=downloadAccessoryImportTemplate; type=button |
| 455 | Düğme | Eşlemeyi kaydet | id=saveAccessoryImportProfile; type=button |
| 456 | Düğme | Profili sil | id=deleteAccessoryImportProfile; type=button |
| 457 | Düğme | Eşlemeyi kullan ve yeniden doğrula | id=applyAccessoryImportMapping; type=button |
| 458 | Düğme | Dosyayı doğrula | id=previewAccessoryImport; type=button |
| 459 | Düğme | Doğrulanmış kayıtları ekle | id=commitAccessoryImport; type=button |
| 460 | Düğme | Kapat | type=button |
| 461 | Düğme | Vazgeç | type=button |
| 462 | Düğme | Değişiklikleri kaydet | type=submit |
| 463 | Düğme | Kapat | type=button |
| 464 | Düğme | Vazgeç | type=button |
| 465 | Düğme | Onayla | id=employeeActionSubmit; type=submit |
| 466 | Düğme | Kapat | type=button |
| 467 | Düğme | Vazgeç | type=button |
| 468 | Düğme | Kaydet | id=textEntrySubmit; type=submit |
| 469 | Düğme | İmzalı zimmet belgeleri | id=printEmployeeHandoff; type=button |
| 470 | Düğme | Kapat | id=closeEmployeeDetails; type=button |
| 471 | Düğme | Kaydet | id=saveEmployeeManager; type=button |
| 472 | Düğme | Kaldır | id=clearEmployeeManager; type=button |
| 473 | Düğme | Kapat | id=closeInviteMember; type=button |
| 474 | Düğme | Vazgeç | id=cancelInviteMember; type=button |
| 475 | Düğme | Daveti gönder | type=submit |
| 476 | Düğme | Kapat | id=closeMemberLocationAccess; type=button |
| 477 | Düğme | Vazgeç | id=cancelMemberLocationAccess; type=button |
| 478 | Düğme | Erişimi kaydet | type=submit |
| 479 | Düğme | Kapat | type=button |
| 480 | Düğme | ＋ Ekle | type=button |
| 481 | Düğme | ＋ Ekle | type=button |
| 482 | Düğme | ＋ Ekle | type=button |
| 483 | Düğme | Vazgeç | type=button |
| 484 | Düğme | Kaydı oluştur | type=submit |
| 485 | Düğme | Sunucu bağlantısını test et | id=retryApiButton; type=button |
| 486 | Düğme | Giriş yap | type=submit |
| 487 | Düğme | Parolamı unuttum | id=forgotPasswordButton; type=button |
| 488 | Düğme | Kapat | id=closeDashboardCustomize; type=button |
| 489 | Düğme | Özet kartlarını yukarı taşı | type=button |
| 490 | Düğme | Özet kartlarını aşağı taşı | type=button |
| 491 | Düğme | Varlık listesini yukarı taşı | type=button |
| 492 | Düğme | Varlık listesini aşağı taşı | type=button |
| 493 | Düğme | Son hareketleri yukarı taşı | type=button |
| 494 | Düğme | Son hareketleri aşağı taşı | type=button |
| 495 | Düğme | Yaklaşan işlemleri yukarı taşı | type=button |
| 496 | Düğme | Yaklaşan işlemleri aşağı taşı | type=button |
| 497 | Düğme | Varlık durumu grafiğini yukarı taşı | type=button |
| 498 | Düğme | Varlık durumu grafiğini aşağı taşı | type=button |
| 499 | Düğme | + Grafik ekle | id=addDashboardChart; type=button |
| 500 | Düğme | Tümünü göster | id=resetDashboardCustomize; type=button |
| 501 | Düğme | İptal | id=cancelDashboardCustomize; type=button |
| 502 | Düğme | Tercihleri kaydet | type=submit |
| 503 | Düğme | Kapat | id=closeForgotPassword; type=button |
| 504 | Düğme | İptal | type=button |
| 505 | Düğme | Bağlantı iste | type=submit |
| 506 | Düğme | Kapat | id=closeImportPreview; type=button |
| 507 | Düğme | CSV şablonunu indir | id=downloadAssetCsvTemplate; type=button |
| 508 | Düğme | Eşlemeyi kaydet | id=saveAssetImportProfile; type=button |
| 509 | Düğme | Profili sil | id=deleteAssetImportProfile; type=button |
| 510 | Düğme | Eşlemeyi kullan ve yeniden doğrula | id=applyAssetImportMapping; type=button |
| 511 | Düğme | Kapat | id=cancelImportPreview; type=button |
| 512 | Düğme | Dosyayı doğrula | id=runImportPreview; type=submit |
| 513 | Düğme | Doğrula ve içe aktar | id=applyCsvImport; type=button; başlangıçta gizli |
| 514 | Düğme | Şirkete geri yükle | id=applyCompanyImport; type=button; başlangıçta gizli |
| 515 | Düğme | Kapat | id=closeAccessoryDetail; type=button |
| 516 | Düğme | Tüm siparişleri aç | id=accessoryOrdersFromDetail; type=button |
| 517 | Düğme | Kapat | id=accessoryCheckinClose; type=button |
| 518 | Düğme | Vazgeç | id=accessoryCheckinCancel; type=button |
| 519 | Düğme | İadeyi kaydet | id=accessoryCheckinSubmit; type=submit |
| 520 | Düğme | Kapat | type=button |
| 521 | Düğme | Bütçe istisnasını reddet | id=rejectAccessoryOrderApproval; type=button |
| 522 | Düğme | Bütçe istisnasını onayla | id=approveAccessoryOrderApproval; type=button |
| 523 | Düğme | Dosya yükle | type=submit |
| 524 | Düğme | Tedarikçiye iade et | id=returnAccessoryOrderFromDetail; type=button |
| 525 | Düğme | Teslim al | id=receiveAccessoryOrderFromDetail; type=button |
| 526 | Düğme | Kapat | type=button |
| 527 | Düğme | Kapat | type=button |
| 528 | Düğme | ＋ Kalem ekle | id=addAccessoryOrderLine; type=button |
| 529 | Düğme | Vazgeç | type=button |
| 530 | Düğme | Siparişi kaydet | type=submit |
| 531 | Düğme | Kapat | type=button |
| 532 | Düğme | ＋ Kalem ekle | id=addStockOrderLine; type=button |
| 533 | Düğme | Vazgeç | type=button |
| 534 | Düğme | Siparişi kaydet | type=submit |
| 535 | Düğme | Kapat | type=button |
| 536 | Düğme | Bütçe istisnasını reddet | id=rejectStockOrderApproval; type=button |
| 537 | Düğme | Bütçe istisnasını onayla | id=approveStockOrderApproval; type=button |
| 538 | Düğme | Belge yükle | type=submit |
| 539 | Düğme | Tedarikçiye iade et | id=returnStockOrderFromDetail; type=button |
| 540 | Düğme | Bu siparişten kit reçetesi oluştur | id=createKitFromStockOrder; type=button; başlangıçta gizli |
| 541 | Düğme | Teslim al | id=receiveStockOrderFromDetail; type=button |
| 542 | Düğme | Kapat | type=button |
| 543 | Düğme | Kapat | type=button |
| 544 | Düğme | Vazgeç | type=button |
| 545 | Düğme | Teslim al ve stoğa ekle | type=submit |
| 546 | Düğme | Kapat | type=button |
| 547 | Düğme | Vazgeç | type=button |
| 548 | Düğme | Kit reçetesini kaydet | type=submit |
| 549 | Düğme | Kapat | type=button |
| 550 | Düğme | Kapat | type=button |
| 551 | Düğme | Bütçeyi kaydet | type=submit |
| 552 | Düğme | Kapat | type=button |
| 553 | Düğme | ＋ Cihaz kalemi ekle | id=addAssetOrderLine; type=button |
| 554 | Düğme | Vazgeç | type=button |
| 555 | Düğme | Siparişi kaydet | type=submit |
| 556 | Düğme | Kapat | type=button |
| 557 | Düğme | Bütçe istisnasını reddet | id=rejectAssetOrderApproval; type=button |
| 558 | Düğme | Bütçe istisnasını onayla | id=approveAssetOrderApproval; type=button |
| 559 | Düğme | Belgeleri yükle | id=uploadAssetOrderAttachments; type=button |
| 560 | Düğme | Tedarikçiye iade et | id=returnAssetOrderFromDetail; type=button |
| 561 | Düğme | Teslim al | id=receiveAssetOrderFromDetail; type=button |
| 562 | Düğme | Kapat | type=button |
| 563 | Düğme | Kapat | type=button |
| 564 | Düğme | Vazgeç | type=button |
| 565 | Düğme | Teslim al ve varlıkları oluştur | type=submit |
| 566 | Düğme | Kapat | type=button |
| 567 | Düğme | Vazgeç | type=button |
| 568 | Düğme | Stoğa al | type=submit |
| 569 | Düğme | Kapat | id=closeConsumableDetail; type=button |
| 570 | Düğme | Kapat | type=button |
| 571 | Düğme | Vazgeç | type=button |
| 572 | Düğme | Kaydet | type=submit |
| 573 | Düğme | Kapat | type=button |
| 574 | Düğme | Vazgeç | type=button |
| 575 | Düğme | Stok hareketini kaydet | type=submit |
| 576 | Düğme | Kapat | type=button |
| 577 | Düğme | Vazgeç | type=button |
| 578 | Düğme | Tüketim çıkışını kaydet | type=submit |
| 579 | Düğme | Kapat | id=closeLicenseDetail; type=button |
| 580 | Düğme | Dosya yükle | type=submit |
| 581 | Düğme | Kapat | type=button |
| 582 | Düğme | Vazgeç | type=button |
| 583 | Düğme | Kaydet | type=submit |
| 584 | Düğme | Kapat | type=button |
| 585 | Düğme | Vazgeç | type=button |
| 586 | Düğme | Kaydet | type=submit |
| 587 | Düğme | Kapat | id=closeLicenseImport; type=button |
| 588 | Düğme | Şablonu indir | id=downloadLicenseImportTemplate; type=button |
| 589 | Düğme | Eşlemeyi kaydet | id=saveLicenseImportProfile; type=button |
| 590 | Düğme | Profili sil | id=deleteLicenseImportProfile; type=button |
| 591 | Düğme | Eşlemeyi kullan ve yeniden doğrula | id=applyLicenseImportMapping; type=button |
| 592 | Düğme | Dosyayı doğrula | id=previewLicenseImport; type=button |
| 593 | Düğme | Doğrulanmış kayıtları ekle | id=commitLicenseImport; type=button |
| 594 | Düğme | Kapat | id=licenseActionClose; type=button |
| 595 | Düğme | Vazgeç | id=licenseActionCancel; type=button |
| 596 | Düğme | Onayla | id=licenseActionSubmit; type=submit |
| 597 | Düğme | Kapat | type=button |
| 598 | Düğme | Vazgeç | type=button |
| 599 | Düğme | Atamayı kaydet | type=submit |
| 600 | Düğme | Kapat | type=button |
| 601 | Düğme | Vazgeç | type=button |
| 602 | Düğme | Devam et | id=componentActionSubmit; type=submit |
| 603 | Düğme | Kapat | id=closeComponentDetail; type=button |
| 604 | Düğme | Kapat | id=closeComponentImport; type=button |
| 605 | Düğme | Şablonu indir | id=downloadComponentImportTemplate; type=button |
| 606 | Düğme | Eşlemeyi kaydet | id=saveComponentImportProfile; type=button |
| 607 | Düğme | Profili sil | id=deleteComponentImportProfile; type=button |
| 608 | Düğme | Eşlemeyi kullan ve yeniden doğrula | id=applyComponentImportMapping; type=button |
| 609 | Düğme | Dosyayı doğrula | id=previewComponentImport; type=button |
| 610 | Düğme | Doğrulanmış kayıtları ekle | id=commitComponentImport; type=button |
| 611 | Düğme | Kapat | type=button |
| 612 | Düğme | Vazgeç | type=button |
| 613 | Düğme | Kaydet | type=submit |
| 614 | Düğme | ✕ | type=button |
| 615 | Düğme | Vazgeç | type=button |
| 616 | Düğme | Kaydet | type=submit |
| 617 | Düğme | ✕ | type=button |
| 618 | Düğme | Vazgeç | type=button |
| 619 | Düğme | Atamayı kaydet | type=submit |
| 620 | Düğme | Kapat | type=button |
| 621 | Düğme | Vazgeç | type=button |
| 622 | Düğme | Çıkarmayı kaydet | type=submit |
| 623 | Düğme | Kapat | type=button |
| 624 | Düğme | ＋ Stok kalemi ekle | id=addAccessoryKitLine; type=button |
| 625 | Düğme | Vazgeç | type=button |
| 626 | Düğme | Kiti kaydet | type=submit |
| 627 | Düğme | Kapat | id=closeAccessoryKitHistory; type=button |
| 628 | Düğme | Kapat | id=closeAccessoryKitHistoryFooter; type=button |
| 629 | Düğme | Kapat | type=button |
| 630 | Düğme | Vazgeç | type=button |
| 631 | Düğme | Arşivle | id=accessoryKitActionSubmit; type=submit |
| 632 | Düğme | Kapat | type=button |
| 633 | Düğme | ＋ Kategori oluştur | id=accessoryQuickCategory; type=button |
| 634 | Düğme | ＋ Marka oluştur | id=accessoryQuickBrand; type=button |
| 635 | Düğme | ＋ Model oluştur | id=accessoryQuickModel; type=button |
| 636 | Düğme | Vazgeç | type=button |
| 637 | Düğme | Kaydet | type=submit |
| 638 | Düğme | Kapat | id=closeAccessoryCategoryManager; type=button |
| 639 | Düğme | Kapat | id=cancelAccessoryCategoryManager; type=button |
| 640 | Düğme | ＋ Kategori ekle | id=addAccessoryCategory; type=button |
| 641 | Düğme | Kapat | type=button |
| 642 | Düğme | Vazgeç | type=button |
| 643 | Düğme | Stok hareketini kaydet | type=submit |
| 644 | Düğme | Kapat | type=button |
| 645 | Düğme | Vazgeç | type=button |
| 646 | Düğme | Teslimi kaydet | type=submit |
| 647 | Düğme | Kapat | id=closeAssetScanner; type=button |
| 648 | Düğme | Kamerayı başlat | id=startAssetCamera; type=button |
| 649 | Düğme | Varlığı bul | type=submit |
| 650 | Düğme | Kapat | id=closeDirectoryRename; type=button |
| 651 | Düğme | Vazgeç | id=cancelDirectoryRename; type=button |
| 652 | Düğme | Değişiklikleri kaydet | id=saveDirectoryRename; type=submit |
| 653 | Düğme | Kapat | id=closeCreateDepartment; type=button |
| 654 | Düğme | Vazgeç | id=cancelCreateDepartment; type=button |
| 655 | Düğme | Departmanı kaydet | id=saveDepartment; type=submit |
| 656 | Düğme | Kapat | id=closeDepartmentMove; type=button |
| 657 | Düğme | Kapat | id=cancelDepartmentMove; type=button |
| 658 | Düğme | Sonraki 100 çalışanı taşı | id=moveDepartmentEmployees; type=submit |
| 659 | Düğme | Kapat | id=closeCreateLocation; type=button |
| 660 | Düğme | Vazgeç | id=cancelCreateLocation; type=button |
| 661 | Düğme | Konumu kaydet | id=saveLocation; type=submit |
| 662 | Düğme | Kapat | id=closeLocationManager; type=button |
| 663 | Düğme | Vazgeç | id=cancelLocationManager; type=button |
| 664 | Düğme | Sorumluyu kaydet | id=saveLocationManager; type=submit |
| 665 | Düğme | Kapat | id=closeDepartmentManager; type=button |
| 666 | Düğme | Vazgeç | id=cancelDepartmentManager; type=button |
| 667 | Düğme | Yetkiliyi kaydet | id=saveDepartmentManager; type=submit |
| 668 | Düğme | Kapat | id=cancelQuickLookup; type=button |
| 669 | Düğme | Vazgeç | id=cancelQuickLookupAction; type=button |
| 670 | Düğme | Kaydet | type=submit |
| 671 | Düğme | Kapat | id=cancelAssetReturn; type=button |
| 672 | Düğme | Vazgeç | id=cancelAssetReturnAction; type=button |
| 673 | Düğme | İade al | id=submitAssetReturn; type=submit |
| 674 | Düğme | Kapat | id=closeAssetTrashConfirm; type=button |
| 675 | Düğme | Vazgeç | id=cancelAssetTrashConfirm; type=button |
| 676 | Düğme | Varlığı çöp kutusuna taşı | id=submitAssetTrashConfirm; type=submit |
| 677 | Düğme | Kapat | id=closeAssetStatusDialog; type=button |
| 678 | Düğme | Vazgeç | id=cancelAssetStatus; type=button |
| 679 | Düğme | Durumu güncelle | id=submitAssetStatus; type=submit |
| 680 | Düğme | Kapat | id=closeAssetAttachmentRemove; type=button |
| 681 | Düğme | Vazgeç | id=cancelAssetAttachmentRemove; type=button |
| 682 | Düğme | Dosyayı kaldır | id=submitAssetAttachmentRemove; type=submit |
| 683 | Düğme | Kapat | id=closeSavedAssetSearchDialog; type=button |
| 684 | Düğme | Vazgeç | id=cancelSavedAssetSearch; type=button |
| 685 | Düğme | Aramayı kaydet | id=submitSavedAssetSearch; type=submit |
| 686 | Düğme | Kapat | id=closeCompanyMfaEnrollment; type=button |
| 687 | Düğme | Doğrula ve devam et | id=verifyCompanyMfaEnrollment; type=submit |
| 688 | Düğme | Kodları kopyala | id=copyCompanyMfaRecoveryCodes; type=button |
| 689 | Düğme | Kodları kaydettim ve giriş yap | id=finishCompanyMfaEnrollment; type=button |
| 690 | Düğme | Kapat | id=closeSupplierCatalog; type=button |
| 691 | Düğme | Temizle | id=resetSupplierForm; type=button |
| 692 | Düğme | Tedarikçiyi kaydet | type=submit |
| 693 | Düğme | Kapat | id=closeCompanySwitch; type=button |
| 694 | Düğme | Vazgeç | id=cancelCompanySwitch; type=button |
| 695 | Düğme | Çıkış yap ve şirket seç | id=confirmCompanySwitch; type=button |
| 696 | Düğme | Kapat | id=closeAccountPhoto; type=button |
| 697 | Düğme | Fotoğraf seç | id=chooseAccountPhoto; type=button |
| 698 | Düğme | Fotoğrafı kaldır | id=removeAccountPhoto; type=button; başlangıçta gizli |
| 699 | Düğme | Parolayı değiştir | id=changeOwnPassword; type=button |
| 700 | Düğme | Güvenlik ayarları | id=openAccountTwoFactor; type=button |
| 701 | Düğme | Yenile | id=refreshAccountSessions; type=button |
| 702 | Düğme | Kapat | id=closeAccountTwoFactor; type=button |
| 703 | Düğme | Authenticator kurulumu başlat | id=startTwoFactorSetup; type=button |
| 704 | Düğme | Doğrula ve etkinleştir | id=enableTwoFactor; type=button; başlangıçta gizli |
| 705 | Düğme | Kurtarma kodlarını yenile | id=rotateTwoFactorCodes; type=button; başlangıçta gizli |
| 706 | Düğme | İki aşamalı doğrulamayı kapat | id=disableTwoFactor; type=button; başlangıçta gizli |
| 707 | Düğme | Kodları kopyala | id=copyTwoFactorRecoveryCodes; type=button |
| 708 | Düğme | Kapat | type=button |
| 709 | Düğme | Vazgeç | type=button |
| 710 | Düğme | Talebi gönder | type=submit |
| 711 | Düğme | Kapat | id=closeAssetTrash; type=button |
| 712 | Düğme | ← Önceki | id=assetTrashPrev; type=button |
| 713 | Düğme | Sonraki → | id=assetTrashNext; type=button |
| 714 | Düğme | Kapat | id=closeImagePreview; type=button |
| 715 | Düğme | Kapat | id=closeModelDetails; type=button |
| 716 | Düğme | Kapat | id=closeModelAttachments; type=button |
| 717 | Düğme | Teknik dosya ekle | id=uploadModelAttachment; type=submit |
| 718 | Düğme | Yazdır | id=assetLabelPreviewPrint; type=button |
| 719 | Düğme | Kapat | id=assetLabelPreviewClose; type=button |
| 720 | Düğme | Kapat | type=button |
| 721 | Düğme | Vazgeç | type=button |
| 722 | Düğme | İadeyi kaydet | id=submitPurchaseOrderReturn; type=submit |
| 723 | Düğme | ${escapeHtml(translateUiText('Aç'))} | type=button |
| 724 | Düğme | Kapat | type=button |
| 725 | Düğme | Vazgeç | type=button |
| 726 | Düğme | Seçilenleri güncelle | type=submit |
| 727 | Düğme | ${escapeHtml(text)} ile eşleşen kayıtları filtrele | type=button |
| 728 | Düğme | ${escapeHtml(translateUiText('Yenile'))} | id=refreshAssetSoftwareInventory; type=button |
| 729 | Düğme | ${escapeHtml(c.name)} | type=button |
| 730 | Düğme | Ayrıntı | type=button |
| 731 | Düğme | Düzenle | type=button |
| 732 | Düğme | Stok | type=button |
| 733 | Düğme | Ata | type=button |
| 734 | Düğme | Düzenle | id=editComponentButton; başlangıçta gizli |
| 735 | Düğme | Stok ekle / düzelt | id=componentStockButton; başlangıçta gizli |
| 736 | Düğme | c.allocated?'':'hidden'}>Varlığa tak | id=componentAllocateButton; başlangıçta gizli |
| 737 | Düğme | Çıkar | id=${a.id}; type=button |
| 738 | Düğme | Kapat | type=button |
| 739 | Düğme | Vazgeç | type=button |
| 740 | Düğme | Bileşenleri varlığa tak | type=submit |
| 741 | Düğme | Kapat | type=button |
| 742 | Düğme | Vazgeç | type=button |
| 743 | Düğme | Stok farklarını kaydet | type=submit |
| 744 | Düğme | ${escapeHtml(l.name)} | type=button |
| 745 | Düğme | ${translateUiText('Ayrıntı')} | type=button |
| 746 | Düğme | ${translateUiText('Düzenle')} | id=${order.id}; type=button |
| 747 | Düğme | ${translateUiText(order.isArchived?'Geri yükle':'Arşivle')} | id=${order.id}; type=button; action=${order.isArchived?'restore':'archive'} |
| 748 | Düğme | ${escapeHtml(String(b\|\|translateUiText('Tedarikçi ayrıntısını aç')))} | id=licenseSupplierLink; type=button |
| 749 | Düğme | ${translateUiText('Düzenle')} | id=editLicenseButton; başlangıçta gizli |
| 750 | Düğme | ${translateUiText('Koltuk ata')} | id=assignLicenseButton; başlangıçta gizli |
| 751 | Düğme | ${translateUiText('Sipariş kaydı ekle')} | id=newLicensePurchaseOrderButton; type=button; başlangıçta gizli |
| 752 | Düğme | ${translateUiText(l.isArchived?'Arşivden geri yükle':'Arşivle')} | type=button; başlangıçta gizli |
| 753 | Düğme | ${escapeHtml(`${a.assetTag\|\|''}${a.assetName?` · ${a.assetName}`:''}`)} | type=button |
| 754 | Düğme | ${escapeHtml(a.employee\|\|'—')} | type=button |
| 755 | Düğme | ${translateUiText('Atamayı kaldır')} | id=${a.id}; type=button |
| 756 | Düğme | ${translateUiText('Sil')} | id=${a.id}; type=button |
| 757 | Düğme | ${escapeHtml(a.license)} | id=${a.licenseId}; type=button |
| 758 | Düğme | ${escapeHtml(`${a.assetTag\|\|''}${a.assetName?` · ${a.assetName}`:''}`)} | type=button |
| 759 | Düğme | ${escapeHtml(a.employee\|\|'—')} | type=button |
| 760 | Düğme | ${translateUiText('Atamayı kaldır')} | id=${a.licenseId}; type=button |
| 761 | Düğme | ${escapeHtml(item.name)} | type=button |
| 762 | Düğme | ${translateUiText('Ayrıntı')} | type=button |
| 763 | Düğme | ${translateUiText('Düzenle')} | type=button |
| 764 | Düğme | ${translateUiText(item.status === 'archived' ? 'Geri yükle' : 'Arşivle')} | type=button |
| 765 | Düğme | ${translateUiText('Sil')} | id=${file.id}; type=button |
| 766 | Düğme | ${escapeHtml(item.name)} | type=button |
| 767 | Düğme | Ayrıntı | type=button |
| 768 | Düğme | Düzenle | type=button |
| 769 | Düğme | Stok | type=button |
| 770 | Düğme | Çıkış | type=button |
| 771 | Düğme | Düzenle | id=editConsumableButton; başlangıçta gizli |
| 772 | Düğme | Stok ekle / düzelt | id=consumableStockButton; başlangıçta gizli |
| 773 | Düğme | item.issued) ? '' : 'hidden'}>Çalışana tüketim çıkışı | id=consumableCheckoutButton; başlangıçta gizli |
| 774 | Düğme | ${escapeHtml(item.name)} | type=button |
| 775 | Düğme | ${escapeHtml(item.category \|\| translateUiText('Kategori yok'))} | id=${escapeHtml(item.categoryId \|\| '')}; type=button |
| 776 | Düğme | ${translateUiText('Ayrıntı')} | type=button |
| 777 | Düğme | ${translateUiText('Düzenle')} | type=button |
| 778 | Düğme | ${translateUiText('Teslim')} | type=button |
| 779 | Düğme | ${translateUiText(accessoryOnly ? 'Teslim et' : 'Talep oluştur')} | id=${kit.id}; type=button |
| 780 | Düğme | Geçmiş | id=${kit.id}; type=button |
| 781 | Düğme | Düzenle | id=${kit.id}; type=button |
| 782 | Düğme | Arşivle | id=${kit.id}; type=button |
| 783 | Düğme | Geri al | id=${kit.id}; type=button |
| 784 | Düğme | ${translateUiText('Düzenle')} | id=editAccessoryButton; type=button; başlangıçta gizli |
| 785 | Düğme | ${translateUiText('Stok ekle / düzelt')} | id=accessoryStockButton; type=button; başlangıçta gizli |
| 786 | Düğme | currentAccessory.checkedOut ? '' : 'hidden'}>${translateUiText('Çalışana teslim et')} | id=accessoryCheckoutButton; type=button; başlangıçta gizli |
| 787 | Düğme | ${translateUiText('Yeni sipariş')} | id=createAccessoryOrderFromDetail; type=button; başlangıçta gizli |
| 788 | Düğme | ${escapeHtml(order.orderNumber)} | id=${order.purchaseOrderId}; type=button |
| 789 | Düğme | İade al | id=${assignment.id}; type=button |
| 790 | Düğme | Düzenle | type=button |
| 791 | Düğme | ${translateUiText(category.isArchived ? 'Geri yükle' : 'Arşivle')} | type=button |
| 792 | Düğme | Kapat | type=button |
| 793 | Düğme | Vazgeç | type=button |
| 794 | Düğme | Stok farklarını kaydet | type=submit |
| 795 | Düğme | ${escapeHtml(order.orderNumber)} | id=${order.id}; type=button |
| 796 | Düğme | Ayrıntı | id=${order.id}; type=button |
| 797 | Düğme | Teslim al | id=${order.id}; type=button |
| 798 | Düğme | İptal | id=${order.id}; type=button |
| 799 | Düğme | ${escapeHtml(order.orderNumber)} | id=${order.id}; type=button |
| 800 | Düğme | Ayrıntı | id=${order.id}; type=button |
| 801 | Düğme | Teslim al | id=${order.id}; type=button |
| 802 | Düğme | İptal | id=${order.id}; type=button |
| 803 | Düğme | Kalemi kaldır | type=button |
| 804 | Düğme | ${escapeHtml(order.orderNumber)} | id=${order.id}; type=button |
| 805 | Düğme | Ayrıntı | id=${order.id}; type=button |
| 806 | Düğme | Teslim al | id=${order.id}; type=button |
| 807 | Düğme | İptal | id=${order.id}; type=button |
| 808 | Düğme | Kaldır | type=button |
| 809 | Düğme | ${escapeHtml(asset.assetTag)} | id=${asset.id}; type=button |
| 810 | Düğme | Sil | id=${item.id}; type=button |
| 811 | Düğme | ${escapeHtml(order.orderNumber)} | id=${escapeHtml(order.id)}; type=button |
| 812 | Düğme | ${translateUiText('Ayrıntı')} | id=${escapeHtml(order.id)}; type=${escapeHtml(order.type)} |
| 813 | Düğme | Düzenle | id=${supplier.id}; type=button |
| 814 | Düğme | Arşivle | id=${supplier.id}; type=button |
| 815 | Düğme | Geri al | id=${supplier.id}; type=button |
| 816 | Düğme | Kalemi kaldır | type=button |
| 817 | Düğme | Kaldır | id=${item.id}; type=button |
| 818 | Düğme | ${translateUiText('Ayrıntılar')} | type=button |
| 819 | Düğme | ${translateUiText('Teknik dosyalar')} (${fmt.format(item.attachmentCount \|\| 0)}) | type=button |
| 820 | Düğme | ${translateUiText('Görseli yükle')} | type=button |
| 821 | Düğme | ${translateUiText('Amortismanı kaydet')} | type=button |
| 822 | Düğme | ${translateUiText('Üretici bilgilerini kaydet')} | type=button |
| 823 | Düğme | ${translateUiText('Logoyu yükle')} | type=button |
| 824 | Düğme | ${translateUiText('Kaydet')} |  |
| 825 | Düğme | ${translateUiText(deleteReason)} |  |
| 826 | Düğme | ${translateUiText('Sil')} | type=button |
| 827 | Düğme | ${translateUiText('Teknik dosyalar')} | type=button |
| 828 | Düğme | ${escapeHtml(asset.assetTag)} | type=button |
| 829 | Düğme | Filtrele | id=applyAssetCustomFieldFilter; type=button |
| 830 | Düğme | Temizle | id=clearAssetCustomFieldFilter; type=button |
| 831 | Düğme | Özel alan ekle | type=submit |
| 832 | Düğme | Kapat | type=button |
| 833 | Düğme | Vazgeç | type=button |
| 834 | Düğme | Kaydet | type=submit |
| 835 | Düğme | Düzenle | id=${field.id}; type=button |
| 836 | Düğme | ${field.isActive ? 'Pasifleştir' : 'Etkinleştir'} | id=${field.id}; type=button |
| 837 | Düğme | Eşlemeyi kaydet | id=saveEmployeeImportProfile; type=button |
| 838 | Düğme | Profili sil | id=deleteEmployeeImportProfile; type=button |
| 839 | Düğme | Eşlemeyi kullan ve yeniden doğrula | id=applyEmployeeImportMapping; type=button |
| 840 | Düğme | Kapat | id=closeBulkAssetAction; type=button |
| 841 | Düğme | Vazgeç | id=cancelBulkAssetAction; type=button |
| 842 | Düğme | submitBulkAssetAction | id=submitBulkAssetAction; type=submit |
| 843 | Düğme | ${escapeHtml(translateUiText('Talep et'))} | id=${item.id}; type=button |
| 844 | Girdi alanı | employeeHandoverRecipientName | id=employeeHandoverRecipientName |
| 845 | Girdi alanı | file | type=file; name=file |
| 846 | Girdi alanı | Envanterde ara | id=globalSearch |
| 847 | Girdi alanı | Varlık listesini ara | id=assetListSearch |
| 848 | Seçim alanı | Sayfa başına kayıt | id=assetPageSize; seçenekler=25: 25 kayıt, 50: 50 kayıt, 100: 100 kayıt, 200: 200 kayıt |
| 849 | Seçim alanı | Duruma göre filtrele | id=assetStatusFilter; seçenekler=(değer yok): Tüm durumlar, Available: Kullanılabilir, Deployed: Atanmış, Pending: Beklemede, Maintenance: Bakımda, Unusable: Kullanılamaz, Archived: Arşiv |
| 850 | Seçim alanı | Kişisel cihaz filtresi | id=assetByodFilter; seçenekler=(değer yok): Şirket ve kişisel cihazlar, true: Kişisel cihazlar (BYOD), false: Şirket cihazları |
| 851 | Seçim alanı | Garanti durumuna göre filtrele | id=assetWarrantyFilter; seçenekler=(değer yok): Tüm garanti durumları, expired: Süresi dolmuş, expiring: 30 gün içinde dolacak, active: 30 günden uzun süre aktif, unspecified: Bitiş tarihi girilmemiş |
| 852 | Seçim alanı | Kategoriye göre filtrele | id=assetCategoryFilter; seçenekler=(değer yok): Tüm kategoriler |
| 853 | Seçim alanı | Markaya göre filtrele | id=assetBrandFilter; seçenekler=(değer yok): Tüm markalar |
| 854 | Seçim alanı | Modele göre filtrele | id=assetModelFilter; seçenekler=(değer yok): Tüm modeller |
| 855 | Seçim alanı | Kaydedilmiş varlık aramaları | id=savedAssetSearch; seçenekler=(değer yok): Kayıtlı aramalar |
| 856 | Girdi alanı | (etiketsiz) | type=checkbox |
| 857 | Girdi alanı | (etiketsiz) | type=checkbox |
| 858 | Girdi alanı | (etiketsiz) | type=checkbox |
| 859 | Girdi alanı | (etiketsiz) | type=checkbox |
| 860 | Girdi alanı | (etiketsiz) | type=checkbox |
| 861 | Girdi alanı | (etiketsiz) | type=checkbox |
| 862 | Girdi alanı | (etiketsiz) | type=checkbox |
| 863 | Girdi alanı | (etiketsiz) | type=checkbox |
| 864 | Girdi alanı | (etiketsiz) | type=checkbox |
| 865 | Girdi alanı | (etiketsiz) | type=checkbox |
| 866 | Girdi alanı | (etiketsiz) | type=checkbox |
| 867 | Girdi alanı | (etiketsiz) | type=checkbox |
| 868 | Girdi alanı | (etiketsiz) | type=checkbox |
| 869 | Girdi alanı | (etiketsiz) | type=checkbox |
| 870 | Girdi alanı | (etiketsiz) | type=checkbox |
| 871 | Girdi alanı | (etiketsiz) | type=checkbox |
| 872 | Girdi alanı | (etiketsiz) | type=checkbox |
| 873 | Girdi alanı | (etiketsiz) | type=checkbox |
| 874 | Seçim alanı | Tablo satır yoğunluğu | id=assetRowDensity; seçenekler=comfortable: Rahat satırlar, compact: Sık satırlar |
| 875 | Girdi alanı | Bu sayfadaki seçilebilir varlıkları seç | id=selectAllAssets; type=checkbox |
| 876 | Seçim alanı | Seçili varlıklara uygulanacak durum | id=bulkAssetStatus; seçenekler=(değer yok): Yeni durumu seçin, Available: Kullanılabilir, Pending: Beklemede, Maintenance: Bakımda, Unusable: Kullanılamaz, Archived: Arşiv |
| 877 | Girdi alanı | Bu sayfadaki seçilebilir varlıkları seç | id=selectAllAssetsHeader; type=checkbox |
| 878 | Girdi alanı | Çalışan kayıtlarında ara | id=employeeSearch |
| 879 | Girdi alanı | Departmana göre filtrele | id=employeeDepartmentFilter |
| 880 | Girdi alanı | Yönetici ara | id=employeeManagerFilterSearch |
| 881 | Seçim alanı | Yöneticiye göre filtrele | id=employeeManagerFilter; seçenekler=(değer yok): Tüm yöneticiler |
| 882 | Seçim alanı | Çalışan durumuna göre filtrele | id=employeeState; seçenekler=(değer yok): Tüm çalışanlar, true: Aktif çalışanlar, false: Pasif çalışanlar |
| 883 | Girdi alanı | İşe başlama başlangıç tarihi | id=employeeStartedFrom; type=date |
| 884 | Girdi alanı | İşe başlama bitiş tarihi | id=employeeStartedTo; type=date |
| 885 | Seçim alanı | Seçilen çalışanların yeni durumu | id=employeeBulkActive; başlangıçta gizli; seçenekler=false: Pasife al, true: Etkinleştir |
| 886 | Girdi alanı | Liste kayıtlarında ara | id=directorySearch |
| 887 | Girdi alanı | companyUnitsIncludeInactive | id=companyUnitsIncludeInactive; type=checkbox |
| 888 | Girdi alanı | Şirket birimlerinde ara | id=companyUnitSearch |
| 889 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 890 | Girdi alanı | name | name=name |
| 891 | Seçim alanı | unitType | name=unitType; seçenekler=organizational: Organizasyon birimi, legal_entity: Tüzel kişilik, branch: Şube |
| 892 | Girdi alanı | code | name=code |
| 893 | Seçim alanı | parentCompanyUnitId | name=parentCompanyUnitId; seçenekler=(değer yok): Üst birim yok |
| 894 | Girdi alanı | Aksesuarlarda ara | id=accessorySearch |
| 895 | Seçim alanı | Stok durumuna göre filtrele | id=accessoryStockFilter; seçenekler=(değer yok): Tüm stoklar, available: Kullanılabilir stok, low: Minimum seviyede veya altında |
| 896 | Seçim alanı | Aksesuarları sırala | id=accessorySort; seçenekler=name:asc: Ada göre A–Z, name:desc: Ada göre Z–A, remaining:asc: Kalan adede göre artan, remaining:desc: Kalan adede göre azalan, updated:desc: En son güncellenen |
| 897 | Seçim alanı | Sayfa başına aksesuar sayısı | id=accessoryPageSize; seçenekler=(değer yok): 12, (değer yok): 25, (değer yok): 50, (değer yok): 100 |
| 898 | Girdi alanı | (etiketsiz) | type=checkbox |
| 899 | Girdi alanı | (etiketsiz) | type=checkbox |
| 900 | Girdi alanı | (etiketsiz) | type=checkbox |
| 901 | Girdi alanı | (etiketsiz) | type=checkbox |
| 902 | Girdi alanı | (etiketsiz) | type=checkbox |
| 903 | Girdi alanı | (etiketsiz) | type=checkbox |
| 904 | Girdi alanı | (etiketsiz) | type=checkbox |
| 905 | Girdi alanı | (etiketsiz) | type=checkbox |
| 906 | Girdi alanı | (etiketsiz) | type=checkbox |
| 907 | Girdi alanı | (etiketsiz) | type=checkbox |
| 908 | Seçim alanı | Kategoriye göre filtrele | id=accessoryCategoryFilter; seçenekler=(değer yok): Tüm kategoriler |
| 909 | Seçim alanı | Konuma göre filtrele | id=accessoryLocationFilter; seçenekler=(değer yok): Tüm konumlar |
| 910 | Girdi alanı | Satın alma siparişlerinde ara | id=accessoryOrderSearch |
| 911 | Seçim alanı | Sipariş durumuna göre filtrele | id=accessoryOrderStatus; seçenekler=(değer yok): Tüm durumlar, Ordered: Sipariş edildi, PartiallyReceived: Kısmi teslim alındı, Received: Tamamlandı, Cancelled: İptal edildi |
| 912 | Seçim alanı | Teslimat takvimine göre filtrele | id=accessoryOrderDelivery; seçenekler=(değer yok): Tüm teslimatlar, overdue: Geciken teslimatlar |
| 913 | Girdi alanı | Sipariş başlangıç tarihi | id=accessoryOrderFrom; type=date |
| 914 | Girdi alanı | Sipariş bitiş tarihi | id=accessoryOrderTo; type=date |
| 915 | Seçim alanı | Sayfa başına sipariş sayısı | id=accessoryOrderPageSize; seçenekler=25: 25, 50: 50, 100: 100 |
| 916 | Girdi alanı | Hazır kitlerde ara | id=accessoryKitSearch |
| 917 | Girdi alanı | Sarf malzemelerinde ara | id=consumableSearch |
| 918 | Seçim alanı | Stok durumuna göre filtrele | id=consumableStockFilter; seçenekler=(değer yok): Tüm stoklar, available: Stokta olanlar, low: Minimum seviyede veya altında |
| 919 | Seçim alanı | name:asc: Ada göre A–Z, name:desc: Ada göre Z–A, remaining:asc: Kalan miktar artan, remaining:desc: Kalan miktar azalan, updated:desc: Son güncellenen | id=consumableSort; seçenekler=name:asc: Ada göre A–Z, name:desc: Ada göre Z–A, remaining:asc: Kalan miktar artan, remaining:desc: Kalan miktar azalan, updated:desc: Son güncellenen |
| 920 | Girdi alanı | Talep edilebilir stoklarda ara | id=requestCatalogSearch |
| 921 | Seçim alanı | Talep durumuna göre filtrele | id=requestStatusFilter; seçenekler=(değer yok): Tüm durumlar, Pending: Bekliyor, Approved: Onaylandı, Rejected: Reddedildi, Cancelled: İptal edildi |
| 922 | Girdi alanı | Taleplerde ara | id=requestSearch |
| 923 | Metin alanı | note | id=inventoryRequestActionNote; name=note |
| 924 | Girdi alanı | licenseSearch | id=licenseSearch |
| 925 | Seçim alanı | Lisans durumuna göre filtrele | id=licenseStatusFilter; seçenekler=active: Etkin lisanslar, archived: Arşivlenmiş lisanslar, all: Tüm lisanslar |
| 926 | Girdi alanı | licenseAssignmentSearch | id=licenseAssignmentSearch |
| 927 | Seçim alanı | true: Aktif atamalar, false: Geri alınan atamalar, (değer yok): Tüm atamalar | id=licenseAssignmentState; seçenekler=true: Aktif atamalar, false: Geri alınan atamalar, (değer yok): Tüm atamalar |
| 928 | Girdi alanı | kind | type=checkbox; name=kind |
| 929 | Girdi alanı | kind | type=checkbox; name=kind |
| 930 | Girdi alanı | kind | type=checkbox; name=kind |
| 931 | Girdi alanı | kind | type=checkbox; name=kind |
| 932 | Girdi alanı | kind | type=checkbox; name=kind |
| 933 | Girdi alanı | kind | type=checkbox; name=kind |
| 934 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 935 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 936 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 937 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 938 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 939 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 940 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 941 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 942 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 943 | Girdi alanı | componentSearch | id=componentSearch |
| 944 | Seçim alanı | (değer yok): Tüm stoklar, available: Kullanılabilir miktar, low: Minimum seviyede veya altında | id=componentStockFilter; seçenekler=(değer yok): Tüm stoklar, available: Kullanılabilir miktar, low: Minimum seviyede veya altında |
| 945 | Girdi alanı | Stok hareketlerinde ara | id=inventoryMovementSearch |
| 946 | Seçim alanı | Stok türüne göre filtrele | id=inventoryMovementKind; seçenekler=(değer yok): Tüm stok türleri, accessory: Aksesuar, consumable: Sarf malzemesi, component: Bileşen |
| 947 | Seçim alanı | Hareket türüne göre filtrele | id=inventoryMovementType; seçenekler=(değer yok): Tüm hareketler, Received: Stok girişi, Adjustment: Sayım düzeltmesi, Checkout: Çıkış / atama, Checkin: İade / çıkarma |
| 948 | Girdi alanı | Başlangıç tarihi | id=inventoryMovementFrom; type=date |
| 949 | Girdi alanı | Bitiş tarihi | id=inventoryMovementTo; type=date |
| 950 | Seçim alanı | (değer yok): Şablon seçin | id=labelStockTemplate; seçenekler=(değer yok): Şablon seçin |
| 951 | Girdi alanı | labelStockTemplateName | id=labelStockTemplateName |
| 952 | Girdi alanı | labelStockTemplateManufacturer | id=labelStockTemplateManufacturer |
| 953 | Girdi alanı | labelStockTemplatePrinter | id=labelStockTemplatePrinter |
| 954 | Girdi alanı | labelStockTemplateProduct | id=labelStockTemplateProduct |
| 955 | Seçim alanı | barcode1d | name=barcode1d; seçenekler=code128: C128 · alfasayısal, code39: C39 · büyük harf ve rakam, code93: C93 · kompakt alfasayısal, code93ext: C93 Extended · geniş karakter kümesi, rationalizedCodabar: Codabar · sayısal / A–D başlangıç-bitiş, msi: MSI Plessey · sayısal stok kodu, ean5: EAN5 · 5 rakam, ean13: EAN13 · 12/13 rakam, upca: UPC-A · 11/12 rakam, upce: UPC-E · 6–8 rakam, none: Kapalı |
| 956 | Seçim alanı | barcode2d | name=barcode2d; seçenekler=qrcode: QR Code, pdf417: PDF417, datamatrix: Data Matrix, azteccode: Aztec Code, none: Kapalı |
| 957 | Seçim alanı | sizePreset | name=sizePreset; seçenekler=86x48: 86 × 48 mm · varsayılan, 50x30: 50 × 30 mm, 60x40: 60 × 40 mm, 70x35: 70 × 35 mm, 100x50: 100 × 50 mm, custom: Özel ölçü |
| 958 | Girdi alanı | widthMm | type=number; name=widthMm |
| 959 | Girdi alanı | heightMm | type=number; name=heightMm |
| 960 | Girdi alanı | offsetXmm | type=number; name=offsetXmm |
| 961 | Girdi alanı | offsetYmm | type=number; name=offsetYmm |
| 962 | Girdi alanı | paddingMm | type=number; name=paddingMm |
| 963 | Girdi alanı | codeSizeMm | type=number; name=codeSizeMm |
| 964 | Seçim alanı | qrTarget | name=qrTarget; seçenekler=tag: Varlık etiketi · aramayla bulunur, asset-url: Giriş sonrası varlık ayrıntısını aç |
| 965 | Girdi alanı | showName | type=checkbox; name=showName |
| 966 | Girdi alanı | showSerial | type=checkbox; name=showSerial |
| 967 | Girdi alanı | showModel | type=checkbox; name=showModel |
| 968 | Girdi alanı | showLocation | type=checkbox; name=showLocation |
| 969 | Girdi alanı | showStatus | type=checkbox; name=showStatus |
| 970 | Seçim alanı | sheetLayout | name=sheetLayout; seçenekler=single: Her etiketi ayrı sayfaya bas, sheet: Etiket yaprağına otomatik yerleştir |
| 971 | Seçim alanı | sheetPaper | name=sheetPaper; seçenekler=a4: A4 · 210 × 297 mm, letter: Letter · 216 × 279 mm |
| 972 | Girdi alanı | sheetMarginMm | type=number; name=sheetMarginMm |
| 973 | Girdi alanı | sheetGapMm | type=number; name=sheetGapMm |
| 974 | Girdi alanı | name | name=name |
| 975 | Seçim alanı | brandId | name=brandId; seçenekler=(değer yok): Marka seçin (isteğe bağlı) |
| 976 | Seçim alanı | categoryId | name=categoryId; seçenekler=(değer yok): Kategori seçin |
| 977 | Girdi alanı | modelNumber | name=modelNumber |
| 978 | Girdi alanı | website | type=url; name=website |
| 979 | Girdi alanı | supportUrl | type=url; name=supportUrl |
| 980 | Girdi alanı | warrantyUrl | type=url; name=warrantyUrl |
| 981 | Girdi alanı | supportPhone | type=tel; name=supportPhone |
| 982 | Girdi alanı | supportEmail | type=email; name=supportEmail |
| 983 | Girdi alanı | brandLogo | type=file; name=brandLogo |
| 984 | Girdi alanı | modelPhoto | type=file; name=modelPhoto |
| 985 | Girdi alanı | depreciationMonths | type=number; name=depreciationMonths |
| 986 | Girdi alanı | depreciationMinimum | type=number; name=depreciationMinimum |
| 987 | Seçim alanı | depreciationMinimumType | name=depreciationMinimumType; seçenekler=amount: Tutar, percent: Yüzde |
| 988 | Seçim alanı | depreciationMethod | name=depreciationMethod; seçenekler=linear: Doğrusal, half_1: Yarım yıl · ilk dönem, half_2: Yarım yıl · satın alma dönemine göre |
| 989 | Girdi alanı | catalogSearch | id=catalogSearch |
| 990 | Girdi alanı | name | name=name |
| 991 | Girdi alanı | slug | name=slug |
| 992 | Girdi alanı | contactEmail | type=email; name=contactEmail |
| 993 | Girdi alanı | brandPrimaryColor | type=color; name=brandPrimaryColor |
| 994 | Girdi alanı | logo | type=file; name=logo |
| 995 | Girdi alanı | Zimmetlerde ara | id=assignmentSearch |
| 996 | Seçim alanı | İade tarihine göre filtrele | id=returnsState; seçenekler=overdue: Geciken iadeler, due30: Önümüzdeki 30 gün, undated: Tarihi belirtilmemiş, later: Daha ileri tarih, all: Tüm zimmetler |
| 997 | Girdi alanı | İadelerde ara | id=returnsSearch |
| 998 | Girdi alanı | Hareket kayıtlarında ara | id=activitySearch |
| 999 | Seçim alanı | (değer yok): Tüm işlem türleri, Created: Oluşturuldu, Purchased: Satın alma teslimatı, Assigned: Zimmetlendi, Returned: İade alındı, StatusChanged: Durum değişti, Transferred: Konum transferi | id=activityType; seçenekler=(değer yok): Tüm işlem türleri, Created: Oluşturuldu, Purchased: Satın alma teslimatı, Assigned: Zimmetlendi, Returned: İade alındı, StatusChanged: Durum değişti, Transferred: Konum transferi |
| 1000 | Girdi alanı | activityFrom | id=activityFrom; type=date |
| 1001 | Girdi alanı | activityTo | id=activityTo; type=date |
| 1002 | Seçim alanı | assetId | id=reservationAsset; name=assetId; seçenekler=(değer yok): Uygun varlıklar yükleniyor… |
| 1003 | Seçim alanı | employeeId | id=reservationEmployee; name=employeeId; seçenekler=(değer yok): Çalışanlar yükleniyor… |
| 1004 | Girdi alanı | startsOn | type=date; name=startsOn |
| 1005 | Girdi alanı | endsOn | type=date; name=endsOn |
| 1006 | Metin alanı | requestNote | name=requestNote |
| 1007 | Girdi alanı | Rezervasyonlarda ara | id=reservationSearch; type=search |
| 1008 | Seçim alanı | Rezervasyon durumu | id=reservationStatus; seçenekler=(değer yok): Tüm durumlar, Pending: Onay bekliyor, Approved: Onaylandı, Rejected: Reddedildi, Cancelled: İptal edildi, Completed: Tamamlandı |
| 1009 | Girdi alanı | reservationFrom | id=reservationFrom; type=date |
| 1010 | Girdi alanı | reservationTo | id=reservationTo; type=date |
| 1011 | Metin alanı | note | id=reservationReviewNote; name=note |
| 1012 | Seçim alanı | 30: 30 gün, 90: 90 gün, 180: 180 gün, 366: 1 yıl | id=auditScheduleHorizon; seçenekler=30: 30 gün, 90: 90 gün, 180: 180 gün, 366: 1 yıl |
| 1013 | Seçim alanı | Denetim planı durumu | id=auditScheduleState; seçenekler=all: Gecikmiş ve yaklaşan, overdue: Yalnızca gecikmiş, upcoming: Yalnızca yaklaşan, unscheduled: Denetim planı olmayanlar, completed: Tamamlanan denetimler |
| 1014 | Girdi alanı | Denetim kuyruğunda ara | id=auditScheduleSearch |
| 1015 | Girdi alanı | Denetim kayıtlarında ara | id=auditSearch |
| 1016 | Seçim alanı | (değer yok): Tüm kayıt türleri, Asset: Varlık, Employee: Çalışan, Company: Şirket, categories: Kategoriler, brands: Markalar, models: Modeller | id=auditTarget; seçenekler=(değer yok): Tüm kayıt türleri, Asset: Varlık, Employee: Çalışan, Company: Şirket, categories: Kategoriler, brands: Markalar, models: Modeller |
| 1017 | Seçim alanı | İşlem türü | id=auditAction; seçenekler=(değer yok): Tüm işlemler |
| 1018 | Girdi alanı | Denetim başlangıç tarihi | id=auditFrom; type=date |
| 1019 | Girdi alanı | Denetim bitiş tarihi | id=auditTo; type=date |
| 1020 | Girdi alanı | Sözleşmelerde ara | id=contractSearch |
| 1021 | Seçim alanı | (değer yok): Etkin ve arşiv kayıtları, active: Etkin, expiring: 30 gün içinde bitecek, expired: Süresi dolmuş, archived: Arşiv | id=contractStatus; seçenekler=(değer yok): Etkin ve arşiv kayıtları, active: Etkin, expiring: 30 gün içinde bitecek, expired: Süresi dolmuş, archived: Arşiv |
| 1022 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1023 | Girdi alanı | title | name=title |
| 1024 | Metin alanı | description | name=description |
| 1025 | Seçim alanı | priority | name=priority; seçenekler=Low: Düşük, Normal: Normal, High: Yüksek, Urgent: Acil |
| 1026 | Seçim alanı | assignedEmployeeId | name=assignedEmployeeId; seçenekler=(değer yok): Atanmamış |
| 1027 | Girdi alanı | dueAt | type=date; name=dueAt |
| 1028 | Girdi alanı | Görevlerde ara | id=taskSearch |
| 1029 | Seçim alanı | Görev durumu | id=taskStatusFilter; seçenekler=(değer yok): Tüm durumlar, Open: Açık, InProgress: Devam ediyor, Completed: Tamamlandı, Cancelled: İptal edildi |
| 1030 | Seçim alanı | Görev önceliği | id=taskPriorityFilter; seçenekler=(değer yok): Tüm öncelikler, Low: Düşük, Normal: Normal, High: Yüksek, Urgent: Acil |
| 1031 | Girdi alanı | subject | name=subject |
| 1032 | Seçim alanı | category | name=category; seçenekler=Access: Hesap ve erişim, Inventory: Envanter, Data: Veri ve aktarım, Other: Diğer |
| 1033 | Metin alanı | message | name=message |
| 1034 | Girdi alanı | Destek taleplerinde ara | id=supportSearch |
| 1035 | Seçim alanı | Talep durumu | id=supportStatusFilter; seçenekler=(değer yok): Tüm durumlar, Open: Açık, InProgress: İşlemde, AwaitingCustomer: Yanıtınız bekleniyor, Resolved: Çözüldü, Closed: Kapatıldı |
| 1036 | Metin alanı | body | name=body |
| 1037 | Girdi alanı | passwordMinimumLength | type=number; name=passwordMinimumLength |
| 1038 | Girdi alanı | sessionDurationHours | type=number; name=sessionDurationHours |
| 1039 | Girdi alanı | requireMfa | type=checkbox; name=requireMfa |
| 1040 | Girdi alanı | name | name=name |
| 1041 | Girdi alanı | scope | type=checkbox; name=scope |
| 1042 | Girdi alanı | scope | type=checkbox; name=scope |
| 1043 | Girdi alanı | scope | type=checkbox; name=scope |
| 1044 | Girdi alanı | scope | type=checkbox; name=scope |
| 1045 | Seçim alanı | expiresInDays | name=expiresInDays; seçenekler=(değer yok): Süresiz, 30: 30 gün, 90: 90 gün, 365: 365 gün |
| 1046 | Girdi alanı | integrationSecretValue | id=integrationSecretValue; type=password |
| 1047 | Girdi alanı | Bakım kayıtlarında ara | id=maintenanceSearch |
| 1048 | Seçim alanı | (değer yok): Tüm durumlar, Scheduled: Planlandı, InProgress: Devam ediyor, Completed: Tamamlandı, Cancelled: İptal edildi | id=maintenanceStatus; seçenekler=(değer yok): Tüm durumlar, Scheduled: Planlandı, InProgress: Devam ediyor, Completed: Tamamlandı, Cancelled: İptal edildi |
| 1049 | Girdi alanı | maintenanceFrom | id=maintenanceFrom; type=date |
| 1050 | Girdi alanı | maintenanceTo | id=maintenanceTo; type=date |
| 1051 | Girdi alanı | calendarFrom | id=calendarFrom; type=date |
| 1052 | Girdi alanı | calendarTo | id=calendarTo; type=date |
| 1053 | Seçim alanı | Takvim etkinlik türü | id=calendarKind; seçenekler=(değer yok): Bakım, denetim, lisans, garanti, iade ve rezervasyon, maintenance: Yalnızca bakım planları, audit: Yalnızca fiziksel denetimler, license: Yalnızca lisans bitişleri, warranty: Yalnızca garanti bitişleri, return: Yalnızca beklenen varlık iadeleri, reservation: Yalnızca onaylı rezervasyonlar |
| 1054 | Seçim alanı | Takvim görünümü | id=calendarViewMode; seçenekler=list: Liste görünümü, day: Gün görünümü, week: Hafta görünümü, month: Ay görünümü |
| 1055 | Girdi alanı | reportDateFrom | id=reportDateFrom; type=date |
| 1056 | Girdi alanı | reportDateTo | id=reportDateTo; type=date |
| 1057 | Girdi alanı | Varlık alış raporunda ara | id=assetPurchaseReportSearch |
| 1058 | Girdi alanı | auditReportFrom | id=auditReportFrom; type=date |
| 1059 | Girdi alanı | auditReportTo | id=auditReportTo; type=date |
| 1060 | Girdi alanı | Fiziksel denetim sonuçlarında ara | id=auditReportSearch |
| 1061 | Seçim alanı | Garanti raporu durumu | id=warrantyReportState; seçenekler=upcoming: Yaklaşan, expired: Süresi dolan, later: Daha ileri tarih, missing: Tarihi olmayan, all: Tümü |
| 1062 | Seçim alanı | 30: 30 gün, 90: 90 gün, 180: 180 gün, 366: 1 yıl | id=warrantyReportHorizon; seçenekler=30: 30 gün, 90: 90 gün, 180: 180 gün, 366: 1 yıl |
| 1063 | Girdi alanı | Garanti raporunda ara | id=warrantyReportSearch |
| 1064 | Girdi alanı | maintenanceReportFrom | id=maintenanceReportFrom; type=date |
| 1065 | Girdi alanı | maintenanceReportTo | id=maintenanceReportTo; type=date |
| 1066 | Seçim alanı | Lisans bitiş raporu durumu | id=licenseReportState; seçenekler=upcoming: Yaklaşan, expired: Süresi dolan, later: Daha ileri tarih, missing: Tarihi olmayan, all: Tümü |
| 1067 | Seçim alanı | 30: 30 gün, 90: 90 gün, 180: 180 gün, 366: 1 yıl | id=licenseReportHorizon; seçenekler=30: 30 gün, 90: 90 gün, 180: 180 gün, 366: 1 yıl |
| 1068 | Girdi alanı | Lisans bitiş raporunda ara | id=licenseReportSearch |
| 1069 | Seçim alanı | Bekleyen teslimat tarihine göre filtrele | id=pendingDeliveryState; seçenekler=all: Tüm açık kalemler, overdue: Geciken teslimatlar, due30: Önümüzdeki 30 gün, undated: Tarihi belirtilmemiş |
| 1070 | Girdi alanı | Bekleyen teslimat raporunda ara | id=pendingDeliverySearch |
| 1071 | Girdi alanı | depreciationReportAsOf | id=depreciationReportAsOf; type=date |
| 1072 | Girdi alanı | Amortisman raporunda ara | id=depreciationReportSearch |
| 1073 | Girdi alanı | customReportName | id=customReportName |
| 1074 | Seçim alanı | (değer yok): Taslak seçin… | id=customReportPreset; seçenekler=(değer yok): Taslak seçin… |
| 1075 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1076 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1077 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1078 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1079 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1080 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1081 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1082 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1083 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1084 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1085 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1086 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1087 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1088 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1089 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1090 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1091 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1092 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1093 | Girdi alanı | customReportSearch | id=customReportSearch |
| 1094 | Seçim alanı | (değer yok): Tüm durumlar, Available: Kullanılabilir, Deployed: Atanmış, Pending: Beklemede, Maintenance: Bakımda, Unusable: Kullanılamaz, Archived: Arşivlenmiş | id=customReportStatus; seçenekler=(değer yok): Tüm durumlar, Available: Kullanılabilir, Deployed: Atanmış, Pending: Beklemede, Maintenance: Bakımda, Unusable: Kullanılamaz, Archived: Arşivlenmiş |
| 1095 | Seçim alanı | (değer yok): Tüm garanti durumları, expired: Süresi dolmuş, expiring: Yakında dolacak, active: Geçerli, unspecified: Tarih girilmemiş | id=customReportWarranty; seçenekler=(değer yok): Tüm garanti durumları, expired: Süresi dolmuş, expiring: Yakında dolacak, active: Geçerli, unspecified: Tarih girilmemiş |
| 1096 | Seçim alanı | tag:asc: Etiket · A–Z, tag:desc: Etiket · Z–A, name:asc: Ada göre · A–Z, name:desc: Ada göre · Z–A, category:asc: Kategori · A–Z, brand:asc: Marka / model · A–Z, employee:asc: Çalışan · A–Z, location:asc: Konum · A–Z | id=customReportSort; seçenekler=tag:asc: Etiket · A–Z, tag:desc: Etiket · Z–A, name:asc: Ada göre · A–Z, name:desc: Ada göre · Z–A, category:asc: Kategori · A–Z, brand:asc: Marka / model · A–Z, employee:asc: Çalışan · A–Z, location:asc: Konum · A–Z |
| 1097 | Girdi alanı | Sarf ve bileşen siparişlerinde ara | id=stockOrderSearch |
| 1098 | Seçim alanı | Sipariş durumuna göre filtrele | id=stockOrderStatus; seçenekler=(değer yok): Tüm durumlar, Ordered: Sipariş edildi, PartiallyReceived: Kısmi teslim alındı, Received: Tamamlandı, Cancelled: İptal edildi |
| 1099 | Seçim alanı | Sipariş kalem türüne göre filtrele | id=stockOrderItemType; seçenekler=(değer yok): Tüm kalem türleri, Consumable: Sarf, Component: Bileşen |
| 1100 | Seçim alanı | Teslimat takvimine göre filtrele | id=stockOrderDelivery; seçenekler=(değer yok): Tüm teslimatlar, overdue: Geciken teslimatlar |
| 1101 | Girdi alanı | Sipariş başlangıç tarihi | id=stockOrderFrom; type=date |
| 1102 | Girdi alanı | Sipariş bitiş tarihi | id=stockOrderTo; type=date |
| 1103 | Seçim alanı | Sayfa başına sipariş sayısı | id=stockOrderPageSize; seçenekler=25: 25, 50: 50, 100: 100 |
| 1104 | Girdi alanı | Tüm satın alma siparişlerinde ara | id=purchaseOverviewSearch; type=search |
| 1105 | Seçim alanı | Satın alma türü | id=purchaseOverviewType; seçenekler=(değer yok): Tüm türler, asset: Varlık, accessory: Aksesuar, stock: Sarf / bileşen, license: Yazılım lisansı |
| 1106 | Seçim alanı | Sipariş durumu | id=purchaseOverviewStatus; seçenekler=(değer yok): Tüm durumlar, Ordered: Sipariş edildi, PartiallyReceived: Kısmi teslim, Received: Tamamlandı, Cancelled: İptal edildi, Recorded: Kaydedildi |
| 1107 | Girdi alanı | Sipariş başlangıç tarihi | id=purchaseOverviewFrom; type=date |
| 1108 | Girdi alanı | Sipariş bitiş tarihi | id=purchaseOverviewTo; type=date |
| 1109 | Seçim alanı | Sayfa başına sipariş sayısı | id=purchaseOverviewPageSize; seçenekler=25: 25, 50: 50, 100: 100 |
| 1110 | Girdi alanı | Varlık satın alma siparişlerinde ara | id=assetOrderSearch |
| 1111 | Seçim alanı | Sipariş durumuna göre filtrele | id=assetOrderStatus; seçenekler=(değer yok): Tüm durumlar, Ordered: Sipariş edildi, PartiallyReceived: Kısmi teslim alındı, Received: Tamamlandı, Cancelled: İptal edildi |
| 1112 | Girdi alanı | Sipariş başlangıç tarihi | id=assetOrderFrom; type=date |
| 1113 | Girdi alanı | Sipariş bitiş tarihi | id=assetOrderTo; type=date |
| 1114 | Seçim alanı | Sayfa başına sipariş sayısı | id=assetOrderPageSize; seçenekler=25: 25, 50: 50, 100: 100 |
| 1115 | Girdi alanı | roleKey | type=hidden; name=roleKey; başlangıçta gizli |
| 1116 | Girdi alanı | name | name=name |
| 1117 | Girdi alanı | permission | type=checkbox; name=permission |
| 1118 | Girdi alanı | permission | type=checkbox; name=permission |
| 1119 | Girdi alanı | permission | type=checkbox; name=permission |
| 1120 | Girdi alanı | permission | type=checkbox; name=permission |
| 1121 | Girdi alanı | permission | type=checkbox; name=permission |
| 1122 | Girdi alanı | permission | type=checkbox; name=permission |
| 1123 | Girdi alanı | commandPaletteSearch | id=commandPaletteSearch; type=search |
| 1124 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1125 | Girdi alanı | name | name=name |
| 1126 | Girdi alanı | contractNumber | name=contractNumber |
| 1127 | Girdi alanı | contractType | name=contractType |
| 1128 | Seçim alanı | supplierId | name=supplierId; seçenekler=(değer yok): Elle girilen tedarikçi |
| 1129 | Girdi alanı | supplier | name=supplier |
| 1130 | Girdi alanı | startDate | type=date; name=startDate |
| 1131 | Girdi alanı | endDate | type=date; name=endDate |
| 1132 | Girdi alanı | cost | type=number; name=cost |
| 1133 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1134 | Girdi alanı | contractLookupSearch | id=contractLookupSearch; type=search |
| 1135 | Seçim alanı | Sözleşmeye bağlı varlıklar | id=contractAssetIds; name=assetIds |
| 1136 | Seçim alanı | Sözleşmeye bağlı yazılım lisansları | id=contractLicenseIds; name=licenseIds |
| 1137 | Metin alanı | notes | name=notes |
| 1138 | Girdi alanı | contractAttachmentFile | id=contractAttachmentFile; type=file; başlangıçta gizli |
| 1139 | Seçim alanı | Tema | id=themePreference; seçenekler=system: Sistem ayarını izle, light: Açık, dark: Koyu |
| 1140 | Seçim alanı | tr: Türkçe, en: English, ru: Русский, uk: Українська, mk: Македонски | id=languagePreference; seçenekler=tr: Türkçe, en: English, ru: Русский, uk: Українська, mk: Македонски |
| 1141 | Girdi alanı | Üst varlık ara | id=assetParentSearch; type=search |
| 1142 | Seçim alanı | Üst varlık | id=assetParentSelect; seçenekler=(değer yok): Üst varlık yok |
| 1143 | Metin alanı | note | id=assetNoteInput; name=note |
| 1144 | Girdi alanı | file | id=assetAttachmentInput; type=file; name=file |
| 1145 | Seçim alanı | assetEventId | id=assetAttachmentEvent; name=assetEventId; seçenekler=(değer yok): Bir harekete bağlama |
| 1146 | Seçim alanı | Varlık durumunu değiştir | id=assetStatusAction; seçenekler=(değer yok): Durumu değiştir…, Available: Kullanılabilir, Pending: Beklemede, Maintenance: Bakımda, Unusable: Kullanılamaz, Archived: Arşivle |
| 1147 | Girdi alanı | auditDate | type=date; name=auditDate |
| 1148 | Girdi alanı | nextAuditOn | type=date; name=nextAuditOn |
| 1149 | Seçim alanı | outcome | id=auditOutcome; name=outcome; seçenekler=Accepted: Uygun, Unacceptable: Uygun değil / bulgu var |
| 1150 | Metin alanı | issueDescription | id=auditIssueDescription; name=issueDescription |
| 1151 | Metin alanı | notes | name=notes |
| 1152 | Girdi alanı | nextAuditOn | type=date; name=nextAuditOn |
| 1153 | Girdi alanı | maintenanceType | name=maintenanceType |
| 1154 | Girdi alanı | supplier | name=supplier |
| 1155 | Seçim alanı | responsibleEmployeeId | name=responsibleEmployeeId; seçenekler=(değer yok): Çalışan atama |
| 1156 | Girdi alanı | description | name=description |
| 1157 | Seçim alanı | mode | id=maintenanceMode; name=mode; seçenekler=immediate: Hemen başlat, scheduled: İleri tarihe planla |
| 1158 | Girdi alanı | startedOn | type=date; name=startedOn |
| 1159 | Girdi alanı | plannedOn | type=date; name=plannedOn |
| 1160 | Girdi alanı | cost | type=number; name=cost |
| 1161 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY — Türk lirası, USD: USD — ABD doları, EUR: EUR — Euro |
| 1162 | Metin alanı | notes | name=notes |
| 1163 | Girdi alanı | completedOn | type=date; name=completedOn |
| 1164 | Metin alanı | notes | name=notes |
| 1165 | Girdi alanı | transferSourceLocation | id=transferSourceLocation |
| 1166 | Girdi alanı | targetLocation | name=targetLocation |
| 1167 | Metin alanı | notes | name=notes |
| 1168 | Girdi alanı | expectedUpdatedAt | type=hidden; name=expectedUpdatedAt; başlangıçta gizli |
| 1169 | Girdi alanı | assetTag | name=assetTag |
| 1170 | Girdi alanı | assetName | name=assetName |
| 1171 | Seçim alanı | categoryId | name=categoryId |
| 1172 | Seçim alanı | brandId | name=brandId; seçenekler=(değer yok): Marka seçin |
| 1173 | Seçim alanı | modelId | name=modelId; seçenekler=(değer yok): Model seçin |
| 1174 | Girdi alanı | serialNumber | name=serialNumber |
| 1175 | Girdi alanı | location | name=location |
| 1176 | Seçim alanı | defaultLocationCatalogId | name=defaultLocationCatalogId; seçenekler=(değer yok): Varsayılan konum seçin |
| 1177 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1178 | Girdi alanı | warrantyExpiresAt | type=date; name=warrantyExpiresAt |
| 1179 | Girdi alanı | insuranceProvider | name=insuranceProvider |
| 1180 | Girdi alanı | insurancePolicyNumber | name=insurancePolicyNumber |
| 1181 | Girdi alanı | insuranceExpiresAt | type=date; name=insuranceExpiresAt |
| 1182 | Girdi alanı | purchaseCost | type=number; name=purchaseCost |
| 1183 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY — Türk lirası, USD: USD — ABD doları, EUR: EUR — Euro |
| 1184 | Metin alanı | notes | name=notes |
| 1185 | Girdi alanı | isRequestable | type=checkbox; name=isRequestable |
| 1186 | Girdi alanı | isByod | type=checkbox; name=isByod |
| 1187 | Girdi alanı | assignSearch | id=assignSearch |
| 1188 | Seçim alanı | employeeId | name=employeeId |
| 1189 | Girdi alanı | expectedReturnOn | type=date; name=expectedReturnOn |
| 1190 | Girdi alanı | note | name=note |
| 1191 | Girdi alanı | displayName | name=displayName |
| 1192 | Girdi alanı | employeeNumber | name=employeeNumber |
| 1193 | Girdi alanı | email | type=email; name=email |
| 1194 | Girdi alanı | department | name=department |
| 1195 | Girdi alanı | jobTitle | name=jobTitle |
| 1196 | Girdi alanı | phone | type=tel; name=phone |
| 1197 | Girdi alanı | location | name=location |
| 1198 | Girdi alanı | startedOn | type=date; name=startedOn |
| 1199 | Seçim alanı | companyUnitId | name=companyUnitId; seçenekler=(değer yok): Birim seçilmedi |
| 1200 | Girdi alanı | newEmployeeManagerSearch | id=newEmployeeManagerSearch |
| 1201 | Seçim alanı | managerEmployeeId | id=newEmployeeManagerId; name=managerEmployeeId; seçenekler=(değer yok): Yönetici seçilmedi |
| 1202 | Girdi alanı | employeeImportFile | id=employeeImportFile; type=file; başlangıçta gizli |
| 1203 | Girdi alanı | consumableImportFile | id=consumableImportFile; type=file; başlangıçta gizli |
| 1204 | Seçim alanı | Sarf malzemesi eşleme profili | id=consumableImportProfile; seçenekler=(değer yok): Profil seçin |
| 1205 | Girdi alanı | consumableImportProfileName | id=consumableImportProfileName |
| 1206 | Girdi alanı | accessoryImportFile | id=accessoryImportFile; type=file; başlangıçta gizli |
| 1207 | Seçim alanı | Aksesuar eşleme profili | id=accessoryImportProfile; seçenekler=(değer yok): Profil seçin |
| 1208 | Girdi alanı | accessoryImportProfileName | id=accessoryImportProfileName |
| 1209 | Girdi alanı | displayName | name=displayName |
| 1210 | Girdi alanı | employeeNumber | name=employeeNumber |
| 1211 | Girdi alanı | email | type=email; name=email |
| 1212 | Girdi alanı | department | name=department |
| 1213 | Girdi alanı | jobTitle | name=jobTitle |
| 1214 | Girdi alanı | phone | type=tel; name=phone |
| 1215 | Girdi alanı | location | name=location |
| 1216 | Girdi alanı | startedOn | type=date; name=startedOn |
| 1217 | Seçim alanı | companyUnitId | name=companyUnitId; seçenekler=(değer yok): Birim seçilmedi |
| 1218 | Girdi alanı | value | id=textEntryValue; type=text; name=value |
| 1219 | Girdi alanı | Yönetici ara | id=employeeManagerSearch |
| 1220 | Seçim alanı | Yönetici seç | id=employeeManagerSelect; seçenekler=(değer yok): Yönetici yok |
| 1221 | Girdi alanı | fullName | name=fullName |
| 1222 | Girdi alanı | email | type=email; name=email |
| 1223 | Seçim alanı | roleKey | name=roleKey; seçenekler=asset_operator: Envanter operatörü, asset_manager: Envanter yöneticisi, auditor: Denetçi, viewer: Salt okunur, hr_operator: İK operatörü, admin: Yönetici |
| 1224 | Girdi alanı | userId | type=hidden; name=userId; başlangıçta gizli |
| 1225 | Seçim alanı | mode | id=memberLocationAccessMode; name=mode; seçenekler=all: Tüm konumlar, restricted: Yalnız seçilen konumlar |
| 1226 | Seçim alanı | companyUnitMode | id=memberCompanyUnitAccessMode; name=companyUnitMode; seçenekler=all: Tüm şirket birimleri, restricted: Yalnız seçilen şirket birimleri |
| 1227 | Girdi alanı | assetName | name=assetName |
| 1228 | Girdi alanı | assetTag | name=assetTag |
| 1229 | Seçim alanı | categoryId | name=categoryId; seçenekler=(değer yok): Kategori seçin |
| 1230 | Seçim alanı | brandId | name=brandId; seçenekler=(değer yok): Marka seçin |
| 1231 | Seçim alanı | modelId | name=modelId; seçenekler=(değer yok): Model seçin |
| 1232 | Girdi alanı | serialNumber | name=serialNumber |
| 1233 | Girdi alanı | location | name=location |
| 1234 | Seçim alanı | defaultLocationCatalogId | name=defaultLocationCatalogId; seçenekler=(değer yok): Varsayılan konum seçin |
| 1235 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1236 | Girdi alanı | warrantyExpiresAt | type=date; name=warrantyExpiresAt |
| 1237 | Girdi alanı | insuranceProvider | name=insuranceProvider |
| 1238 | Girdi alanı | insurancePolicyNumber | name=insurancePolicyNumber |
| 1239 | Girdi alanı | insuranceExpiresAt | type=date; name=insuranceExpiresAt |
| 1240 | Girdi alanı | purchaseCost | type=number; name=purchaseCost |
| 1241 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY — Türk lirası, USD: USD — ABD doları, EUR: EUR — Euro |
| 1242 | Metin alanı | notes | name=notes |
| 1243 | Girdi alanı | isRequestable | type=checkbox; name=isRequestable |
| 1244 | Girdi alanı | isByod | type=checkbox; name=isByod |
| 1245 | Girdi alanı | email | type=email; name=email |
| 1246 | Girdi alanı | password | type=password; name=password |
| 1247 | Girdi alanı | rememberMe | type=checkbox; name=rememberMe |
| 1248 | Girdi alanı | twoFactorCode | name=twoFactorCode |
| 1249 | Seçim alanı | companyId | id=loginCompanyId; name=companyId; seçenekler=(değer yok): Şirket seçin |
| 1250 | Girdi alanı | widget | type=checkbox; name=widget |
| 1251 | Girdi alanı | widget | type=checkbox; name=widget |
| 1252 | Girdi alanı | widget | type=checkbox; name=widget |
| 1253 | Girdi alanı | widget | type=checkbox; name=widget |
| 1254 | Girdi alanı | widget | type=checkbox; name=widget |
| 1255 | Seçim alanı | category: Kategori, brand: Marka, model: Model, location: Konum, status: Durum | id=dashboardChartDimension; seçenekler=category: Kategori, brand: Marka, model: Model, location: Konum, status: Durum |
| 1256 | Seçim alanı | bar: Yatay çubuk, donut: Halka grafik | id=dashboardChartType; seçenekler=bar: Yatay çubuk, donut: Halka grafik |
| 1257 | Girdi alanı | email | type=email; name=email |
| 1258 | Seçim alanı | Kayıtlı sütun eşlemesi | id=assetImportProfile; seçenekler=(değer yok): Profil seçin |
| 1259 | Girdi alanı | assetImportProfileName | id=assetImportProfileName |
| 1260 | Girdi alanı | importPreviewFile | id=importPreviewFile; type=file |
| 1261 | Girdi alanı | quantity | type=number; name=quantity |
| 1262 | Metin alanı | notes | name=notes |
| 1263 | Metin alanı | accessoryOrderApprovalNote | id=accessoryOrderApprovalNote |
| 1264 | Girdi alanı | file | type=file; name=file |
| 1265 | Girdi alanı | orderNumber | name=orderNumber |
| 1266 | Girdi alanı | supplier | name=supplier |
| 1267 | Girdi alanı | orderDate | type=date; name=orderDate |
| 1268 | Girdi alanı | expectedOn | type=date; name=expectedOn |
| 1269 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY, USD: USD, EUR: EUR |
| 1270 | Girdi alanı | taxRatePercent | type=number; name=taxRatePercent |
| 1271 | Girdi alanı | discountAmount | type=number; name=discountAmount |
| 1272 | Girdi alanı | freightAmount | type=number; name=freightAmount |
| 1273 | Girdi alanı | notes | name=notes |
| 1274 | Girdi alanı | orderNumber | name=orderNumber |
| 1275 | Girdi alanı | supplier | name=supplier |
| 1276 | Girdi alanı | orderDate | type=date; name=orderDate |
| 1277 | Girdi alanı | expectedOn | type=date; name=expectedOn |
| 1278 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY, USD: USD, EUR: EUR |
| 1279 | Girdi alanı | taxRatePercent | type=number; name=taxRatePercent |
| 1280 | Girdi alanı | discountAmount | type=number; name=discountAmount |
| 1281 | Girdi alanı | freightAmount | type=number; name=freightAmount |
| 1282 | Girdi alanı | notes | name=notes |
| 1283 | Metin alanı | stockOrderApprovalNote | id=stockOrderApprovalNote |
| 1284 | Girdi alanı | file | id=stockOrderAttachmentInput; type=file; name=file |
| 1285 | Girdi alanı | receivedOn | type=date; name=receivedOn |
| 1286 | Girdi alanı | notes | name=notes |
| 1287 | Girdi alanı | name | name=name |
| 1288 | Girdi alanı | description | name=description |
| 1289 | Seçim alanı | assetId | name=assetId; seçenekler=(değer yok): Varlık seçin |
| 1290 | Girdi alanı | year | type=number; name=year |
| 1291 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1292 | Girdi alanı | amount | type=number; name=amount |
| 1293 | Girdi alanı | orderNumber | name=orderNumber |
| 1294 | Girdi alanı | supplier | name=supplier |
| 1295 | Girdi alanı | orderDate | type=date; name=orderDate |
| 1296 | Girdi alanı | expectedOn | type=date; name=expectedOn |
| 1297 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1298 | Girdi alanı | taxRatePercent | type=number; name=taxRatePercent |
| 1299 | Girdi alanı | discountAmount | type=number; name=discountAmount |
| 1300 | Girdi alanı | freightAmount | type=number; name=freightAmount |
| 1301 | Girdi alanı | notes | name=notes |
| 1302 | Metin alanı | assetOrderApprovalNote | id=assetOrderApprovalNote |
| 1303 | Girdi alanı | assetOrderAttachmentFiles | id=assetOrderAttachmentFiles; type=file |
| 1304 | Girdi alanı | receivedOn | type=date; name=receivedOn |
| 1305 | Girdi alanı | notes | name=notes |
| 1306 | Girdi alanı | receivedOn | type=date; name=receivedOn |
| 1307 | Girdi alanı | notes | name=notes |
| 1308 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1309 | Girdi alanı | name | name=name |
| 1310 | Girdi alanı | category | name=category |
| 1311 | Girdi alanı | modelNumber | name=modelNumber |
| 1312 | Girdi alanı | itemNumber | name=itemNumber |
| 1313 | Girdi alanı | supplier | name=supplier |
| 1314 | Girdi alanı | purchaseOrderReference | name=purchaseOrderReference |
| 1315 | Girdi alanı | location | name=location |
| 1316 | Girdi alanı | minimumQuantity | type=number; name=minimumQuantity |
| 1317 | Girdi alanı | quantity | type=number; name=quantity |
| 1318 | Girdi alanı | unitCost | type=number; name=unitCost |
| 1319 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1320 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1321 | Girdi alanı | warrantyExpiresAt | type=date; name=warrantyExpiresAt |
| 1322 | Girdi alanı | notes | name=notes |
| 1323 | Girdi alanı | isRequestable | type=checkbox; name=isRequestable |
| 1324 | Girdi alanı | quantity | type=number; name=quantity |
| 1325 | Girdi alanı | notes | name=notes |
| 1326 | Seçim alanı | employeeId | name=employeeId; seçenekler=(değer yok): Çalışan seçin |
| 1327 | Girdi alanı | quantity | type=number; name=quantity |
| 1328 | Girdi alanı | notes | name=notes |
| 1329 | Girdi alanı | file | type=file; name=file |
| 1330 | Girdi alanı | isEulaDocument | type=checkbox; name=isEulaDocument |
| 1331 | Girdi alanı | eulaVersion | name=eulaVersion |
| 1332 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1333 | Seçim alanı | supplierId | name=supplierId; seçenekler=(değer yok): Tedarikçi seçin |
| 1334 | Girdi alanı | orderNumber | name=orderNumber |
| 1335 | Girdi alanı | orderDate | type=date; name=orderDate |
| 1336 | Girdi alanı | seatsPurchased | type=number; name=seatsPurchased |
| 1337 | Girdi alanı | unitCost | type=number; name=unitCost |
| 1338 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1339 | Metin alanı | notes | name=notes |
| 1340 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1341 | Girdi alanı | name | name=name |
| 1342 | Girdi alanı | publisher | name=publisher |
| 1343 | Girdi alanı | productVersion | name=productVersion |
| 1344 | Seçim alanı | licenseType | name=licenseType; seçenekler=Subscription: Abonelik, Perpetual: Süresiz, Device: Cihaz başına, Site: Şirket geneli, Trial: Deneme, Other: Diğer |
| 1345 | Girdi alanı | licenseReference | name=licenseReference |
| 1346 | Girdi alanı | supplier | name=supplier |
| 1347 | Girdi alanı | purchaseOrderReference | name=purchaseOrderReference |
| 1348 | Girdi alanı | seats | type=number; name=seats |
| 1349 | Girdi alanı | unitCost | type=number; name=unitCost |
| 1350 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1351 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1352 | Girdi alanı | expirationDate | type=date; name=expirationDate |
| 1353 | Girdi alanı | autoRenew | type=checkbox; name=autoRenew |
| 1354 | Girdi alanı | requiresEulaAcceptance | type=checkbox; name=requiresEulaAcceptance |
| 1355 | Girdi alanı | notes | name=notes |
| 1356 | Girdi alanı | licenseImportFile | id=licenseImportFile; type=file; başlangıçta gizli |
| 1357 | Seçim alanı | Lisans eşleme profili | id=licenseImportProfile; seçenekler=(değer yok): Profil seçin |
| 1358 | Girdi alanı | licenseImportProfileName | id=licenseImportProfileName |
| 1359 | Girdi alanı | eulaAccepted | id=licenseEulaAccepted; type=checkbox; name=eulaAccepted |
| 1360 | Seçim alanı | targetType | name=targetType; seçenekler=employee: Çalışan, asset: Cihaz / varlık |
| 1361 | Seçim alanı | employeeId | name=employeeId; seçenekler=(değer yok): Çalışan seçin |
| 1362 | Girdi alanı | licenseBulkEmployeeMode | id=licenseBulkEmployeeMode; type=checkbox |
| 1363 | Girdi alanı | licenseAssetSearch | id=licenseAssetSearch; type=search |
| 1364 | Seçim alanı | assetId | name=assetId; seçenekler=(değer yok): Önce arama yapın |
| 1365 | Girdi alanı | notes | name=notes |
| 1366 | Girdi alanı | componentImportFile | id=componentImportFile; type=file; başlangıçta gizli |
| 1367 | Seçim alanı | Bileşen eşleme profili | id=componentImportProfile; seçenekler=(değer yok): Profil seçin |
| 1368 | Girdi alanı | componentImportProfileName | id=componentImportProfileName |
| 1369 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1370 | Girdi alanı | name | name=name |
| 1371 | Girdi alanı | category | name=category |
| 1372 | Girdi alanı | modelNumber | name=modelNumber |
| 1373 | Girdi alanı | itemNumber | name=itemNumber |
| 1374 | Girdi alanı | serialNumber | name=serialNumber |
| 1375 | Girdi alanı | supplier | name=supplier |
| 1376 | Girdi alanı | purchaseOrderReference | name=purchaseOrderReference |
| 1377 | Girdi alanı | location | name=location |
| 1378 | Girdi alanı | minimumQuantity | type=number; name=minimumQuantity |
| 1379 | Girdi alanı | quantity | type=number; name=quantity |
| 1380 | Girdi alanı | unitCost | type=number; name=unitCost |
| 1381 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1382 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1383 | Girdi alanı | warrantyExpiresAt | type=date; name=warrantyExpiresAt |
| 1384 | Girdi alanı | notes | name=notes |
| 1385 | Girdi alanı | quantity | type=number; name=quantity |
| 1386 | Girdi alanı | notes | name=notes |
| 1387 | Seçim alanı | assetId | name=assetId; seçenekler=(değer yok): Varlık seçin |
| 1388 | Girdi alanı | quantity | type=number; name=quantity |
| 1389 | Girdi alanı | notes | name=notes |
| 1390 | Girdi alanı | allocationId | type=hidden; name=allocationId; başlangıçta gizli |
| 1391 | Girdi alanı | quantity | type=number; name=quantity |
| 1392 | Girdi alanı | notes | name=notes |
| 1393 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1394 | Girdi alanı | name | name=name |
| 1395 | Girdi alanı | description | name=description |
| 1396 | Girdi alanı | accessoryKitItemSearch | id=accessoryKitItemSearch; type=search |
| 1397 | Girdi alanı | accessoryId | type=hidden; name=accessoryId; başlangıçta gizli |
| 1398 | Girdi alanı | name | name=name |
| 1399 | Seçim alanı | categoryId | name=categoryId; seçenekler=(değer yok): Kategori seçin (isteğe bağlı) |
| 1400 | Seçim alanı | brandId | name=brandId; seçenekler=(değer yok): Marka seçin (isteğe bağlı) |
| 1401 | Seçim alanı | modelId | name=modelId; seçenekler=(değer yok): Model seçin (isteğe bağlı) |
| 1402 | Girdi alanı | modelPhoto | type=file; name=modelPhoto |
| 1403 | Girdi alanı | modelNumber | name=modelNumber |
| 1404 | Girdi alanı | itemNumber | name=itemNumber |
| 1405 | Girdi alanı | supplier | name=supplier |
| 1406 | Girdi alanı | purchaseOrderReference | name=purchaseOrderReference |
| 1407 | Girdi alanı | location | name=location |
| 1408 | Girdi alanı | minimumQuantity | type=number; name=minimumQuantity |
| 1409 | Girdi alanı | quantity | type=number; name=quantity |
| 1410 | Girdi alanı | unitCost | type=number; name=unitCost |
| 1411 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY, USD: USD, EUR: EUR |
| 1412 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1413 | Girdi alanı | warrantyExpiresAt | type=date; name=warrantyExpiresAt |
| 1414 | Girdi alanı | notes | name=notes |
| 1415 | Girdi alanı | isRequestable | type=checkbox; name=isRequestable |
| 1416 | Girdi alanı | quantity | type=number; name=quantity |
| 1417 | Girdi alanı | notes | name=notes |
| 1418 | Girdi alanı | targetType | type=radio; name=targetType |
| 1419 | Girdi alanı | targetType | type=radio; name=targetType |
| 1420 | Girdi alanı | targetType | type=radio; name=targetType |
| 1421 | Seçim alanı | employeeId | name=employeeId |
| 1422 | Seçim alanı | assetId | name=assetId |
| 1423 | Girdi alanı | location | name=location |
| 1424 | Girdi alanı | quantity | type=number; name=quantity |
| 1425 | Girdi alanı | accessoryKitSets | id=accessoryKitSets; type=number |
| 1426 | Girdi alanı | notes | name=notes |
| 1427 | Seçim alanı | view: Varlık ayrıntısını aç, assign: Uygunsa zimmet formunu aç, return: Zimmetliyse iade onayını göster | id=scanAssetAction; seçenekler=view: Varlık ayrıntısını aç, assign: Uygunsa zimmet formunu aç, return: Zimmetliyse iade onayını göster |
| 1428 | Girdi alanı | code | id=scanAssetCode; name=code |
| 1429 | Girdi alanı | kind | type=hidden; name=kind; başlangıçta gizli |
| 1430 | Girdi alanı | currentName | type=hidden; name=currentName; başlangıçta gizli |
| 1431 | Girdi alanı | locationId | type=hidden; name=locationId; başlangıçta gizli |
| 1432 | Girdi alanı | departmentId | type=hidden; name=departmentId; başlangıçta gizli |
| 1433 | Girdi alanı | newName | name=newName |
| 1434 | Seçim alanı | parentDepartmentId | name=parentDepartmentId; seçenekler=(değer yok): Ana departman |
| 1435 | Seçim alanı | locationCatalogId | name=locationCatalogId; seçenekler=(değer yok): Konum seçilmedi |
| 1436 | Girdi alanı | name | name=name |
| 1437 | Seçim alanı | parentDepartmentId | name=parentDepartmentId; seçenekler=(değer yok): Ana departman |
| 1438 | Seçim alanı | locationCatalogId | name=locationCatalogId; seçenekler=(değer yok): Konum seçilmedi |
| 1439 | Seçim alanı | targetDepartmentId | name=targetDepartmentId |
| 1440 | Seçim alanı | parentLocationId | name=parentLocationId; seçenekler=(değer yok): Üst seviye · kat veya bina |
| 1441 | Girdi alanı | name | name=name |
| 1442 | Girdi alanı | locationId | type=hidden; name=locationId; başlangıçta gizli |
| 1443 | Girdi alanı | locationManagerSearch | id=locationManagerSearch; type=search |
| 1444 | Seçim alanı | employeeId | id=locationManagerEmployee; name=employeeId; seçenekler=(değer yok): Sorumlu atanmamış |
| 1445 | Girdi alanı | departmentId | type=hidden; name=departmentId; başlangıçta gizli |
| 1446 | Girdi alanı | departmentManagerSearch | id=departmentManagerSearch; type=search |
| 1447 | Seçim alanı | employeeId | id=departmentManagerEmployee; name=employeeId; seçenekler=(değer yok): Yetkili atanmamış |
| 1448 | Girdi alanı | name | name=name |
| 1449 | Girdi alanı | modelNumber | name=modelNumber |
| 1450 | Metin alanı | note | name=note |
| 1451 | Metin alanı | note | name=note |
| 1452 | Metin alanı | note | name=note |
| 1453 | Girdi alanı | name | name=name |
| 1454 | Girdi alanı | code | name=code |
| 1455 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1456 | Girdi alanı | name | name=name |
| 1457 | Girdi alanı | contactName | name=contactName |
| 1458 | Girdi alanı | email | type=email; name=email |
| 1459 | Girdi alanı | phone | name=phone |
| 1460 | Girdi alanı | website | type=url; name=website |
| 1461 | Metin alanı | notes | name=notes |
| 1462 | Girdi alanı | logo | type=file; name=logo |
| 1463 | Girdi alanı | Tedarikçilerde ara | id=supplierCatalogSearch |
| 1464 | Girdi alanı | showArchivedSuppliers | id=showArchivedSuppliers; type=checkbox |
| 1465 | Seçim alanı | companySwitchTarget | id=companySwitchTarget |
| 1466 | Girdi alanı | accountPhotoInput | id=accountPhotoInput; type=file; başlangıçta gizli |
| 1467 | Girdi alanı | ownCurrentPassword | id=ownCurrentPassword; type=password |
| 1468 | Girdi alanı | ownNewPassword | id=ownNewPassword; type=password |
| 1469 | Girdi alanı | ownConfirmPassword | id=ownConfirmPassword; type=password |
| 1470 | Girdi alanı | ownPasswordTwoFactorCode | id=ownPasswordTwoFactorCode |
| 1471 | Girdi alanı | twoFactorCurrentPassword | id=twoFactorCurrentPassword; type=password |
| 1472 | Girdi alanı | twoFactorCode | id=twoFactorCode |
| 1473 | Girdi alanı | itemType | type=hidden; name=itemType; başlangıçta gizli |
| 1474 | Girdi alanı | itemId | type=hidden; name=itemId; başlangıçta gizli |
| 1475 | Girdi alanı | requestEmployeeSearch | id=requestEmployeeSearch |
| 1476 | Seçim alanı | employeeId | id=requestEmployee; name=employeeId; seçenekler=(değer yok): Çalışan seçin |
| 1477 | Girdi alanı | quantity | type=number; name=quantity |
| 1478 | Metin alanı | note | name=note |
| 1479 | Girdi alanı | file | id=modelAttachmentFile; type=file; name=file |
| 1480 | Girdi alanı | returnedOn | type=date; name=returnedOn |
| 1481 | Girdi alanı | reason | name=reason |
| 1482 | Girdi alanı | notes | name=notes |
| 1483 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1484 | Girdi alanı | name | name=name |
| 1485 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1486 | Girdi alanı | serialNumber | name=serialNumber |
| 1487 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1488 | Seçim alanı | categoryId | name=categoryId |
| 1489 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1490 | Seçim alanı | brandId | name=brandId |
| 1491 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1492 | Seçim alanı | modelId | name=modelId |
| 1493 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1494 | Seçim alanı | defaultLocationCatalogId | name=defaultLocationCatalogId |
| 1495 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1496 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1497 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1498 | Girdi alanı | warrantyExpiresAt | type=date; name=warrantyExpiresAt |
| 1499 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1500 | Girdi alanı | insuranceProvider | name=insuranceProvider |
| 1501 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1502 | Girdi alanı | insurancePolicyNumber | name=insurancePolicyNumber |
| 1503 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1504 | Girdi alanı | insuranceExpiresAt | type=date; name=insuranceExpiresAt |
| 1505 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1506 | Girdi alanı | purchaseCost | type=number; name=purchaseCost |
| 1507 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY — Türk lirası, USD: USD — ABD doları, EUR: EUR — Euro |
| 1508 | Seçim alanı | isRequestable | name=isRequestable; seçenekler=(değer yok): Değişiklik yok, true: Evet, false: Hayır |
| 1509 | Seçim alanı | isByod | name=isByod; seçenekler=(değer yok): Değişiklik yok, true: Evet, false: Hayır |
| 1510 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1511 | Metin alanı | notes | name=notes |
| 1512 | Metin alanı | note | name=note |
| 1513 | Seçim alanı | assetId | name=assetId |
| 1514 | Girdi alanı | notes | name=notes |
| 1515 | Girdi alanı | notes | name=notes |
| 1516 | Girdi alanı | ${escapeHtml(assignment.targetName \|\| 'Açık zimmet')} iadesini seç | id=${assignment.id}; type=checkbox |
| 1517 | Girdi alanı | ${escapeHtml(assignment.targetName \|\| 'Açık zimmet')} için toplu iade adedi | type=number |
| 1518 | Girdi alanı | notes | name=notes |
| 1519 | Girdi alanı | ${escapeHtml(asset.assetTag)} varlığını iade et | type=checkbox |
| 1520 | Girdi alanı | ${escapeHtml(label \|\| 'Sipariş kalemi')} iade adedi | type=number |
| 1521 | Seçim alanı | (değer yok): Kalem seçin, ${item.itemType}\|${item.id}: ${item.typeLabel} · ${escapeHtml(item.name)} · ${escapeHtml(item.itemNumber \|\| item.modelNumber \|\| 'Stok kodu yok')} · stok ${fmt.format(item.totalQuantity)} | seçenekler=(değer yok): Kalem seçin, ${item.itemType}\|${item.id}: ${item.typeLabel} · ${escapeHtml(item.name)} · ${escapeHtml(item.itemNumber \|\| item.modelNumber \|\| 'Stok kodu yok')} · stok ${fmt.format(item.totalQuantity)} |
| 1522 | Girdi alanı | (etiketsiz) | type=number |
| 1523 | Girdi alanı | (etiketsiz) | type=number |
| 1524 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1525 | Girdi alanı | ${escapeHtml(quantityLabel)} | type=number |
| 1526 | Girdi alanı | ${escapeHtml(line.itemName)} teslim adedi | type=number |
| 1527 | Girdi alanı | (etiketsiz) |  |
| 1528 | Seçim alanı | (etiketsiz) |  |
| 1529 | Seçim alanı | (etiketsiz) |  |
| 1530 | Seçim alanı | (etiketsiz) |  |
| 1531 | Seçim alanı | (etiketsiz) |  |
| 1532 | Girdi alanı | (etiketsiz) | type=number |
| 1533 | Girdi alanı | (etiketsiz) | type=number |
| 1534 | Girdi alanı | (etiketsiz) | type=date |
| 1535 | Girdi alanı | ${escapeHtml(line.name)} teslim adedi | type=number |
| 1536 | Seçim alanı | (değer yok): Aksesuar seçin, ${item.id}: ${escapeHtml(item.name)} · ${escapeHtml(item.itemNumber \|\| item.modelNumber \|\| 'Stok kodu yok')} · stok ${fmt.format(item.totalQuantity)} | seçenekler=(değer yok): Aksesuar seçin, ${item.id}: ${escapeHtml(item.name)} · ${escapeHtml(item.itemNumber \|\| item.modelNumber \|\| 'Stok kodu yok')} · stok ${fmt.format(item.totalQuantity)} |
| 1537 | Girdi alanı | (etiketsiz) | type=number |
| 1538 | Girdi alanı | (etiketsiz) | type=number |
| 1539 | Girdi alanı | ${escapeHtml(line.accessoryName)} teslim adedi | type=number |
| 1540 | Girdi alanı | ${escapeHtml(item.name)} model numarası |  |
| 1541 | Girdi alanı | (etiketsiz) | type=file |
| 1542 | Girdi alanı | (etiketsiz) | type=number |
| 1543 | Girdi alanı | (etiketsiz) | type=number |
| 1544 | Seçim alanı | amount: ${translateUiText('Tutar')}, percent: ${translateUiText('Yüzde')} | type=${item.id}; seçenekler=amount: ${translateUiText('Tutar')}, percent: ${translateUiText('Yüzde')} |
| 1545 | Seçim alanı | linear: ${translateUiText('Doğrusal')}, half_1: ${translateUiText('Yarım yıl · ilk dönem')}, half_2: ${translateUiText('Yarım yıl · satın alma dönemine göre')} | seçenekler=linear: ${translateUiText('Doğrusal')}, half_1: ${translateUiText('Yarım yıl · ilk dönem')}, half_2: ${translateUiText('Yarım yıl · satın alma dönemine göre')} |
| 1546 | Girdi alanı | (etiketsiz) | type=url |
| 1547 | Girdi alanı | (etiketsiz) | type=url |
| 1548 | Girdi alanı | (etiketsiz) | type=url |
| 1549 | Girdi alanı | (etiketsiz) | type=tel |
| 1550 | Girdi alanı | (etiketsiz) | type=email |
| 1551 | Girdi alanı | (etiketsiz) | type=file |
| 1552 | Girdi alanı | ${escapeHtml(item.name)} ${translateUiText('Kayıt adını düzenle')} | name=${item.id} |
| 1553 | Seçim alanı | (değer yok): Özel alan seçin | id=assetCustomFieldFilter; seçenekler=(değer yok): Özel alan seçin |
| 1554 | Seçim alanı | equals: Değeri eşit, contains: Metin içinde, empty: Değer girilmemiş | id=assetCustomFieldMode; seçenekler=equals: Değeri eşit, contains: Metin içinde, empty: Değer girilmemiş |
| 1555 | Girdi alanı | assetCustomFieldValue | id=assetCustomFieldValue |
| 1556 | Girdi alanı | key | name=key |
| 1557 | Girdi alanı | name | name=name |
| 1558 | Girdi alanı | groupName | name=groupName |
| 1559 | Seçim alanı | categoryId | name=categoryId; seçenekler=(değer yok): Tüm kategoriler |
| 1560 | Seçim alanı | modelId | name=modelId; seçenekler=(değer yok): Tüm modeller |
| 1561 | Seçim alanı | fieldType | name=fieldType; seçenekler=text: Metin, number: Sayı, date: Tarih, checkbox: Onay kutusu, select: Seçim listesi, sensitive: Hassas (şifreli) |
| 1562 | Metin alanı | options | name=options |
| 1563 | Girdi alanı | isRequired | type=checkbox; name=isRequired |
| 1564 | Girdi alanı | sortOrder | type=number; name=sortOrder |
| 1565 | Girdi alanı | name | name=name |
| 1566 | Girdi alanı | groupName | name=groupName |
| 1567 | Seçim alanı | categoryId | name=categoryId; seçenekler=(değer yok): Tüm kategoriler |
| 1568 | Seçim alanı | modelId | name=modelId; seçenekler=(değer yok): Tüm modeller |
| 1569 | Seçim alanı | Çalışan eşleme profili | id=employeeImportProfile; seçenekler=(değer yok): Profil seçin |
| 1570 | Girdi alanı | employeeImportProfileName | id=employeeImportProfileName |
| 1571 | Metin alanı | note | name=note |
| 1572 | Form | maintenanceAttachmentForm | id=maintenanceAttachmentForm; başlangıçta gizli |
| 1573 | Form | companyUnitForm | id=companyUnitForm |
| 1574 | Form | inventoryRequestActionForm | id=inventoryRequestActionForm |
| 1575 | Form | companyNotificationPreferencesForm | id=companyNotificationPreferencesForm |
| 1576 | Form | labelSettingsForm | id=labelSettingsForm |
| 1577 | Form | assetStatusLabelsForm | id=assetStatusLabelsForm |
| 1578 | Form | assetStatusConfigForm | id=assetStatusConfigForm |
| 1579 | Form | catalogCreateForm | id=catalogCreateForm |
| 1580 | Form | companyProfileForm | id=companyProfileForm |
| 1581 | Form | companyLogoForm | id=companyLogoForm |
| 1582 | Form | reservationForm | id=reservationForm |
| 1583 | Form | reservationActionForm | id=reservationActionForm |
| 1584 | Form | taskForm | id=taskForm |
| 1585 | Form | supportForm | id=supportForm |
| 1586 | Form | supportReplyForm | id=supportReplyForm |
| 1587 | Form | companySecurityPolicyForm | id=companySecurityPolicyForm |
| 1588 | Form | integrationTokenForm | id=integrationTokenForm |
| 1589 | Form | companyRoleForm | id=companyRoleForm |
| 1590 | Form | licenseRevokeForm | id=licenseRevokeForm |
| 1591 | Form | licenseImportProfileDeleteForm | id=licenseImportProfileDeleteForm |
| 1592 | Form | contractForm | id=contractForm |
| 1593 | Form | contractActionForm | id=contractActionForm |
| 1594 | Form | assetNoteForm | id=assetNoteForm; başlangıçta gizli |
| 1595 | Form | assetAttachmentForm | id=assetAttachmentForm; başlangıçta gizli |
| 1596 | Form | assetAuditForm | id=assetAuditForm |
| 1597 | Form | assetAuditScheduleForm | id=assetAuditScheduleForm |
| 1598 | Form | maintenanceStartForm | id=maintenanceStartForm |
| 1599 | Form | maintenanceCompleteForm | id=maintenanceCompleteForm |
| 1600 | Form | assetTransferForm | id=assetTransferForm |
| 1601 | Form | editAssetForm | id=editAssetForm |
| 1602 | Form | assignForm | id=assignForm |
| 1603 | Form | newEmployeeForm | id=newEmployeeForm |
| 1604 | Form | editEmployeeForm | id=editEmployeeForm |
| 1605 | Form | employeeActionForm | id=employeeActionForm |
| 1606 | Form | textEntryForm | id=textEntryForm |
| 1607 | Form | inviteMemberForm | id=inviteMemberForm |
| 1608 | Form | memberLocationAccessForm | id=memberLocationAccessForm |
| 1609 | Form | newAssetForm | id=newAssetForm |
| 1610 | Form | loginForm | id=loginForm |
| 1611 | Form | dashboardCustomizeForm | id=dashboardCustomizeForm |
| 1612 | Form | forgotPasswordForm | id=forgotPasswordForm |
| 1613 | Form | importPreviewForm | id=importPreviewForm |
| 1614 | Form | accessoryCheckinForm | id=accessoryCheckinForm |
| 1615 | Form | accessoryOrderAttachmentForm | id=accessoryOrderAttachmentForm |
| 1616 | Form | accessoryOrderCreateForm | id=accessoryOrderCreateForm |
| 1617 | Form | stockOrderCreateForm | id=stockOrderCreateForm |
| 1618 | Form | stockOrderAttachmentForm | id=stockOrderAttachmentForm; başlangıçta gizli |
| 1619 | Form | stockOrderReceiveForm | id=stockOrderReceiveForm |
| 1620 | Form | stockOrderKitTemplateForm | id=stockOrderKitTemplateForm |
| 1621 | Form | assetOrderBudgetForm | id=assetOrderBudgetForm |
| 1622 | Form | assetOrderCreateForm | id=assetOrderCreateForm |
| 1623 | Form | assetOrderReceiveForm | id=assetOrderReceiveForm |
| 1624 | Form | accessoryOrderReceiveForm | id=accessoryOrderReceiveForm |
| 1625 | Form | consumableForm | id=consumableForm |
| 1626 | Form | consumableStockForm | id=consumableStockForm |
| 1627 | Form | consumableCheckoutForm | id=consumableCheckoutForm |
| 1628 | Form | licenseAttachmentForm | id=licenseAttachmentForm |
| 1629 | Form | licensePurchaseOrderForm | id=licensePurchaseOrderForm |
| 1630 | Form | licenseForm | id=licenseForm |
| 1631 | Form | licenseActionForm | id=licenseActionForm |
| 1632 | Form | licenseAssignForm | id=licenseAssignForm |
| 1633 | Form | componentActionForm | id=componentActionForm |
| 1634 | Form | componentForm | id=componentForm |
| 1635 | Form | componentStockForm | id=componentStockForm |
| 1636 | Form | componentAllocateForm | id=componentAllocateForm |
| 1637 | Form | componentRemoveForm | id=componentRemoveForm |
| 1638 | Form | accessoryKitForm | id=accessoryKitForm |
| 1639 | Form | accessoryKitActionForm | id=accessoryKitActionForm |
| 1640 | Form | accessoryForm | id=accessoryForm |
| 1641 | Form | accessoryStockForm | id=accessoryStockForm |
| 1642 | Form | accessoryCheckoutForm | id=accessoryCheckoutForm |
| 1643 | Form | scanAssetForm | id=scanAssetForm |
| 1644 | Form | directoryRenameForm | id=directoryRenameForm |
| 1645 | Form | createDepartmentForm | id=createDepartmentForm |
| 1646 | Form | departmentMoveForm | id=departmentMoveForm |
| 1647 | Form | createLocationForm | id=createLocationForm |
| 1648 | Form | locationManagerForm | id=locationManagerForm |
| 1649 | Form | departmentManagerForm | id=departmentManagerForm |
| 1650 | Form | quickLookupForm | id=quickLookupForm |
| 1651 | Form | returnAssetForm | id=returnAssetForm |
| 1652 | Form | assetTrashConfirmForm | id=assetTrashConfirmForm |
| 1653 | Form | assetStatusForm | id=assetStatusForm |
| 1654 | Form | assetAttachmentRemoveForm | id=assetAttachmentRemoveForm |
| 1655 | Form | savedAssetSearchForm | id=savedAssetSearchForm |
| 1656 | Form | companyMfaEnrollmentForm | id=companyMfaEnrollmentForm |
| 1657 | Form | supplierCatalogForm | id=supplierCatalogForm |
| 1658 | Form | inventoryRequestForm | id=inventoryRequestForm |
| 1659 | Form | modelAttachmentForm | id=modelAttachmentForm; başlangıçta gizli |
| 1660 | Form | purchaseOrderReturnForm | id=purchaseOrderReturnForm |
| 1661 | Form | bulkEditAssetForm | id=bulkEditAssetForm |
| 1662 | Form | (etiketsiz) |  |
| 1663 | Form | (etiketsiz) |  |
| 1664 | Form | (etiketsiz) |  |
| 1665 | Form | assetCustomFieldForm | id=assetCustomFieldForm |
| 1666 | Form | editAssetCustomFieldForm | id=editAssetCustomFieldForm |
| 1667 | Form | bulkAssetActionForm | id=bulkAssetActionForm |
| 1668 | Modal | Kapat | id=employeeHandoverDialog; type=button; başlangıçta gizli |
| 1669 | Modal | Kapat | id=maintenanceAttachmentDialog; type=button; name=file; başlangıçta gizli |
| 1670 | Modal | Kapat | id=companyUnitDialog; type=button; name=id; başlangıçta gizli |
| 1671 | Modal | Kapat | id=inventoryRequestActionDialog; type=button; name=note |
| 1672 | Modal | Kapat | id=reservationActionDialog; type=button; name=note |
| 1673 | Modal | Kapat | id=integrationSecretDialog; type=button |
| 1674 | Modal | Kapat | id=companyRoleDialog; type=button; name=roleKey; başlangıçta gizli |
| 1675 | Modal | Kapat | id=deactivateCompanyRoleDialog; type=button |
| 1676 | Modal | Kapat | id=licenseRevokeDialog; type=button |
| 1677 | Modal | Kapat | id=licenseImportProfileDeleteDialog; type=button |
| 1678 | Modal | Kapat | id=commandPaletteDialog; type=button |
| 1679 | Modal | Kapat | id=keyboardShortcutsDialog; type=button |
| 1680 | Modal | Kapat | id=contractFormDialog; type=button; name=id; başlangıçta gizli |
| 1681 | Modal | Kapat | id=contractDetailDialog; type=button; başlangıçta gizli |
| 1682 | Modal | Kapat | id=contractActionDialog; type=button |
| 1683 | Modal | Kapat | id=appearanceDialog; type=button |
| 1684 | Modal | Kapat | id=assetDialog; type=button; name=note; başlangıçta gizli |
| 1685 | Modal | Kapat | id=assetAuditDialog; type=button; name=auditDate; başlangıçta gizli |
| 1686 | Modal | Kapat | id=assetAuditScheduleDialog; type=button; name=nextAuditOn |
| 1687 | Modal | Kapat | id=maintenanceStartDialog; type=button; name=maintenanceType; başlangıçta gizli |
| 1688 | Modal | Kapat | id=maintenanceCompleteDialog; type=button; name=completedOn |
| 1689 | Modal | Kapat | id=assetTransferDialog; type=button; name=targetLocation |
| 1690 | Modal | Kapat | id=editAssetDialog; type=button; name=expectedUpdatedAt; başlangıçta gizli |
| 1691 | Modal | Kapat | id=assignDialog; type=button; name=employeeId |
| 1692 | Modal | Kapat | id=newEmployeeDialog; type=button; name=displayName |
| 1693 | Modal | Kapat | id=employeeImportDialog; type=button; başlangıçta gizli |
| 1694 | Modal | Kapat | id=consumableImportDialog; type=button; başlangıçta gizli |
| 1695 | Modal | Kapat | id=accessoryImportDialog; type=button; başlangıçta gizli |
| 1696 | Modal | Kapat | id=editEmployeeDialog; type=button; name=displayName |
| 1697 | Modal | Kapat | id=employeeActionDialog; type=button |
| 1698 | Modal | Kapat | id=textEntryDialog; type=button; name=value |
| 1699 | Modal | Kapat | id=employeeDialog; type=button; başlangıçta gizli |
| 1700 | Modal | Kapat | id=inviteMemberDialog; type=button; name=fullName |
| 1701 | Modal | Kapat | id=memberLocationAccessDialog; type=button; name=userId; başlangıçta gizli |
| 1702 | Modal | Kapat | id=newAssetDialog; type=button; name=assetName |
| 1703 | Modal | Kapat | id=dashboardCustomizeDialog; type=button; name=widget |
| 1704 | Modal | Kapat | id=forgotPasswordDialog; type=button; name=email |
| 1705 | Modal | Kapat | id=importPreviewDialog; type=button; başlangıçta gizli |
| 1706 | Modal | Kapat | id=accessoryDialog; type=button |
| 1707 | Modal | Kapat | id=accessoryCheckinDialog; type=button; name=quantity; başlangıçta gizli |
| 1708 | Modal | Kapat | id=accessoryOrderDetailDialog; type=button; name=file; başlangıçta gizli |
| 1709 | Modal | Kapat | id=accessoryOrderCreateDialog; type=button; name=orderNumber |
| 1710 | Modal | Kapat | id=stockOrderCreateDialog; type=button; name=orderNumber |
| 1711 | Modal | Kapat | id=stockOrderDetailDialog; type=button; name=file; başlangıçta gizli |
| 1712 | Modal | Kapat | id=stockOrderReceiveDialog; type=button; name=receivedOn |
| 1713 | Modal | Kapat | id=stockOrderKitTemplateDialog; type=button; name=name |
| 1714 | Modal | Kapat | id=assetOrderBudgetDialog; type=button; name=year |
| 1715 | Modal | Kapat | id=assetOrderCreateDialog; type=button; name=orderNumber |
| 1716 | Modal | Kapat | id=assetOrderDetailDialog; type=button; başlangıçta gizli |
| 1717 | Modal | Kapat | id=assetOrderReceiveDialog; type=button; name=receivedOn |
| 1718 | Modal | Kapat | id=accessoryOrderReceiveDialog; type=button; name=receivedOn |
| 1719 | Modal | Kapat | id=consumableDialog; type=button |
| 1720 | Modal | Kapat | id=consumableFormDialog; type=button; name=id; başlangıçta gizli |
| 1721 | Modal | Kapat | id=consumableStockDialog; type=button; name=quantity |
| 1722 | Modal | Kapat | id=consumableCheckoutDialog; type=button; name=employeeId |
| 1723 | Modal | Kapat | id=licenseDialog; type=button; name=file; başlangıçta gizli |
| 1724 | Modal | Kapat | id=licensePurchaseOrderFormDialog; type=button; name=id; başlangıçta gizli |
| 1725 | Modal | Kapat | id=licenseFormDialog; type=button; name=id; başlangıçta gizli |
| 1726 | Modal | Kapat | id=licenseImportDialog; type=button; başlangıçta gizli |
| 1727 | Modal | Kapat | id=licenseActionDialog; type=button; başlangıçta gizli |
| 1728 | Modal | Kapat | id=licenseAssignDialog; type=button; name=eulaAccepted; başlangıçta gizli |
| 1729 | Modal | Kapat | id=componentActionDialog; type=button |
| 1730 | Modal | Kapat | id=componentDialog; type=button |
| 1731 | Modal | Kapat | id=componentImportDialog; type=button; başlangıçta gizli |
| 1732 | Modal | Kapat | id=componentFormDialog; type=button; name=id; başlangıçta gizli |
| 1733 | Modal | quantity | id=componentStockDialog; type=button; name=quantity |
| 1734 | Modal | assetId | id=componentAllocateDialog; type=button; name=assetId |
| 1735 | Modal | Kapat | id=componentRemoveDialog; type=button; name=allocationId; başlangıçta gizli |
| 1736 | Modal | Kapat | id=accessoryKitFormDialog; type=button; name=id; başlangıçta gizli |
| 1737 | Modal | Kapat | id=accessoryKitHistoryDialog; type=button |
| 1738 | Modal | Kapat | id=accessoryKitActionDialog; type=button |
| 1739 | Modal | Kapat | id=accessoryFormDialog; type=button; name=accessoryId; başlangıçta gizli |
| 1740 | Modal | Kapat | id=accessoryCategoryManagerDialog; type=button |
| 1741 | Modal | Kapat | id=accessoryStockDialog; type=button; name=quantity |
| 1742 | Modal | Kapat | id=accessoryCheckoutDialog; type=button; name=targetType; başlangıçta gizli |
| 1743 | Modal | Kapat | id=scanAssetDialog; type=button; name=code; başlangıçta gizli |
| 1744 | Modal | Kapat | id=directoryRenameDialog; type=button; name=kind; başlangıçta gizli |
| 1745 | Modal | Kapat | id=createDepartmentDialog; type=button; name=name |
| 1746 | Modal | Kapat | id=departmentMoveDialog; type=button; name=targetDepartmentId |
| 1747 | Modal | Kapat | id=createLocationDialog; type=button; name=parentLocationId |
| 1748 | Modal | Kapat | id=locationManagerDialog; type=button; name=locationId; başlangıçta gizli |
| 1749 | Modal | Kapat | id=departmentManagerDialog; type=button; name=departmentId; başlangıçta gizli |
| 1750 | Modal | Kapat | id=quickLookupDialog; type=button; name=name; başlangıçta gizli |
| 1751 | Modal | Kapat | id=returnAssetDialog; type=button; name=note; başlangıçta gizli |
| 1752 | Modal | Kapat | id=assetTrashConfirmDialog; type=button; name=note |
| 1753 | Modal | Kapat | id=assetStatusDialog; type=button; name=note; başlangıçta gizli |
| 1754 | Modal | Kapat | id=assetAttachmentRemoveDialog; type=button; başlangıçta gizli |
| 1755 | Modal | Kapat | id=savedAssetSearchDialog; type=button; name=name; başlangıçta gizli |
| 1756 | Modal | Kapat | id=companyMfaEnrollmentDialog; type=button; name=code; başlangıçta gizli |
| 1757 | Modal | Kapat | id=supplierCatalogDialog; type=button; name=id; başlangıçta gizli |
| 1758 | Modal | Kapat | id=companySwitchDialog; type=button |
| 1759 | Modal | Kapat | id=accountPhotoDialog; type=button; hedef=/api/session/data-export; başlangıçta gizli |
| 1760 | Modal | Kapat | id=accountTwoFactorDialog; type=button; başlangıçta gizli |
| 1761 | Modal | Kapat | id=inventoryRequestDialog; type=button; name=itemType; başlangıçta gizli |
| 1762 | Modal | Kapat | id=assetTrashDialog; type=button |
| 1763 | Modal | Görsel önizleme | id=imagePreviewDialog; type=button |
| 1764 | Modal | Kapat | id=modelDetailsDialog; type=button |
| 1765 | Modal | Kapat | id=modelAttachmentsDialog; type=button; name=file; başlangıçta gizli |
| 1766 | Modal | Varlık etiketi yazdırma önizlemesi | id=assetLabelPreviewDialog; type=button |
| 1767 | Modal | Kapat | id=purchaseOrderReturnDialog; type=button; name=returnedOn |
| 1768 | Modal | Kapat | id=editAssetCustomFieldDialog; type=button; name=name |
| 1769 | Tablo | Bu sayfadaki seçilebilir varlıkları seç | id=selectAllAssetsHeader; type=checkbox |
| 1770 | Tablo | Şirket çalışan kayıtları | id=employeeRows |
| 1771 | Tablo | directoryHead | id=directoryHead |
| 1772 | Tablo | Şirket birimleri | id=companyUnitRows |
| 1773 | Tablo | Şirket aksesuar stokları | id=accessoryRows |
| 1774 | Tablo | Aksesuar satın alma siparişleri | id=accessoryOrderRows |
| 1775 | Tablo | Şirketin aksesuar hazır kitleri | id=accessoryKitRows |
| 1776 | Tablo | Sarf malzemeleri ve kullanılabilir stok miktarları | id=consumableRows |
| 1777 | Tablo | Kalem / Tür / Konum / Kalan / | id=requestCatalogRows |
| 1778 | Tablo | Çalışan / talep eden / Kalem / Adet / Durum / İşlem | id=requestRows |
| 1779 | Tablo | Lisans / Yayıncı / sürüm / Tür / Kullanılan koltuk / Bitiş tarihi / Yenileme / İşlem | id=licenseRows |
| 1780 | Tablo | Lisans / Çalışan / varlık / Personel no / Atanma tarihi / Bitiş tarihi / Durum / İşlem | id=licenseAssignmentRows |
| 1781 | Tablo | Seç / Bileşen / Kategori / model / Seri / ürün no / Toplam / Atanmış / Kullanılabilir / Minimum / Garanti bitişi / İşlem | id=componentRows |
| 1782 | Tablo | Şirket stok hareketleri | id=inventoryMovementRows |
| 1783 | Tablo | Kullanıcı / E-posta / Çalışan profili / Rol / Üyelik tarihi / İşlem | id=membersRows |
| 1784 | Tablo | Ad / İlişki / Kullanım / İşlemler | id=catalogRelationHead |
| 1785 | Tablo | Şirketin aktif varlık zimmetleri | id=assignmentRows |
| 1786 | Tablo | Beklenen ve geciken varlık iadeleri | id=returnsRows |
| 1787 | Tablo | Tarih / Varlık / İşlem / Çalışan / İşlemi yapan / Açıklama | id=activityTableRows |
| 1788 | Tablo | Varlık / Çalışan / Kullanım dönemi / Durum / Talep eden / Not / karar / İşlemler | id=reservationRows |
| 1789 | Tablo | Plan tarihi / Etiket / Varlık / Konum / Zimmetli çalışan / Durum / | id=auditScheduleDateHeading |
| 1790 | Tablo | Tarih / İşlemi yapan / Kayıt türü / İşlem / Ayrıntı | id=auditRows |
| 1791 | Tablo | Şirket sözleşmeleri | id=contractRows |
| 1792 | Tablo | Şirket görevleri | id=taskRows |
| 1793 | Tablo | Destek talepleri | id=supportRows |
| 1794 | Tablo | Kullanıcı API anahtarları | id=integrationTokenRows |
| 1795 | Tablo | Şirket bakım kayıtları | id=maintenanceRows |
| 1796 | Tablo | Şirket bakım, denetim, lisans, garanti, iade ve rezervasyon takvimi | id=calendarRows |
| 1797 | Tablo | Durum / Varlık / İşlem | id=reportStatusRows |
| 1798 | Tablo | Para birimi / Kayıt / Toplam alış bedeli | id=reportCostRows |
| 1799 | Tablo | Kategori / Toplam / Atanmış / Kullanılabilir / Bakımda / İşlem | id=reportCategoryRows |
| 1800 | Tablo | Alış tarihi / Etiket / Varlık / Seri no / Kategori / Durum / Bedel / Konum | id=assetPurchaseReportRows |
| 1801 | Tablo | Kategori / Bileşen / Toplam adet / Atanmış adet / Kullanılabilir adet / Düşük stok kaydı | id=componentReportRows |
| 1802 | Tablo | Tarih / Sonuç / Etiket / Varlık / Konum / Denetimi yapan / Açıklama / bulgu | id=auditReportRows |
| 1803 | Tablo | Garanti bitişi / Etiket / Varlık / Durum / Konum / Zimmetli çalışan | id=warrantyReportRows |
| 1804 | Tablo | Tür / Kayıt | id=maintenanceReportTypes |
| 1805 | Tablo | Para birimi / Kayıt / Toplam | id=maintenanceReportCosts |
| 1806 | Tablo | Bitiş tarihi / Lisans / Yayıncı / sürüm / Tür / Koltuk kullanımı / Otomatik yenileme / Tedarikçi | id=licenseReportRows |
| 1807 | Tablo | Şirketin bekleyen satın alma siparişi kalemleri | id=pendingDeliveryRows |
| 1808 | Tablo | Varlıkların tahmini amortisman değerleri | id=depreciationReportRows |
| 1809 | Tablo | Sarf ve bileşen satın alma siparişleri | id=stockOrderRows |
| 1810 | Tablo | Tüm şirket satın alma kayıtları | id=purchaseOverviewRows |
| 1811 | Tablo | Varlık satın alma siparişleri | id=assetOrderRows |
| 1812 | Tablo | roleAccessCaption | id=roleAccessCaption |
| 1813 | Tablo | Aksesuar / Kalan / verilen / Teslim / Not | id=assetAccessoryAssignments |
| 1814 | Tablo | Bileşen / Kalan / verilen / Takılma / Not | id=assetComponentAllocations |
| 1815 | Tablo | Lisans / Tür / Bitiş / Atanma / Not | id=assetLicenseAssignments |
| 1816 | Tablo | İlişki / Etiket / Varlık / Durum | id=assetLinkedRows |
| 1817 | Tablo | Çalışan / Departman / Zimmet | id=employeeDirectReports |
| 1818 | Tablo | Varlık / Durum / Konum | id=employeeAssetRows |
| 1819 | Tablo | Aksesuar / Verilen / İade / Kalan / Tarih | id=employeeAccessoryRows |
| 1820 | Tablo | Lisans / Yayıncı / sürüm / Atandığı tarih / Bitiş | id=employeeLicenseRows |
| 1821 | Tablo | Malzeme / Miktar / Tarih / Not | id=employeeConsumableRows |
| 1822 | Tablo | Varlık / Bakım türü / Servis / Durum / Tarih | id=employeeMaintenanceRows |
| 1823 | Tablo | Sipariş no / Tedarikçi / tarih / Adet / Durum | id=accessoryOrderHistoryRows |
| 1824 | Tablo | Çalışan / Verilen / İade / Kalan / İşlem | id=accessoryAssignments |
| 1825 | Tablo | Aksesuar / Sipariş / Teslim alınan / Kalan / Fatura birim bedeli / Edinim birim bedeli / Toplam | id=accessoryOrderDetailLines |
| 1826 | Tablo | Kalem / Sipariş / Teslim alınan / Kalan / Fatura birim bedeli / Edinim birim bedeli / Toplam | id=stockOrderDetailLines |
| 1827 | Tablo | Cihaz / Kategori / Hedef konum / Sipariş / Teslim alındı / Kalan / Fatura birim bedeli / Edinim birim bedeli | id=assetOrderDetailLines |
| 1828 | Tablo | Etiket / cihaz / Durum / Seri numarası / Alış tarihi | id=assetOrderReceivedAssets |
| 1829 | Tablo | Tarih / İşlem / Miktar / Çalışan / İşlemi yapan / Not | id=consumableMovements |
| 1830 | Tablo | Tedarikçi / Sipariş no. / Tarih / Koltuk / Birim maliyeti / Durum / İşlemler | id=licensePurchaseOrderRows |
| 1831 | Tablo | Çalışan / varlık / Personel no / Atanma / EULA sürümü / EULA teyit zamanı / Teyit eden / Durum / İşlem | id=licenseAssignments |
| 1832 | Tablo | Varlık / Verilen / Çıkarılan / Kalan / Tarih / İşlem | id=componentAllocations |
| 1833 | Tablo | Tarih / Kalem / Teslim edilen / İade edilen / Kalan / Hedef / İşlemi yapan / Not | id=accessoryKitHistoryRows |
| 1834 | Tablo | Kategori / Aksesuar / Durum / İşlemler | id=accessoryCategoryRows |
| 1835 | Tablo | Firma / Yetkili / iletişim / Bağlı kayıt / Durum / İşlemler | id=supplierCatalogRows |
| 1836 | Tablo | Etiket / Varlık / Seri numarası / Taşınma zamanı / İşlem | id=assetTrashRows |
| 1837 | Tablo | Etiket / Varlık / Durum / Seri no / Çalışan / konum | id=modelDetailsAssets |
| 1838 | Tablo | Sipariş / Tedarikçi / Durum / Tarih / Adet / Teslim alınan | id=modelDetailsOrders |
| 1839 | Tablo | Tarih / İşlem / Açıklama | id=modelDetailsHistory |
| 1840 | Tablo | Dosya / Tür / Boyut / Yüklenme / İşlem | id=modelAttachmentRows |
| 1841 | Tablo | (etiketsiz) | başlangıçta gizli |
| 1842 | Tablo | Etiket / Varlık / Seri no / Durum / Konum |  |
| 1843 | Tablo | Aksesuar / Model / Kalan adet / Teslim tarihi |  |
| 1844 | Tablo | Lisans / EULA sürümü / Dosya / ek kimliği / Kabul eden / tarih / EULA SHA-256 / Durum |  |
| 1845 | Tablo | ${escapeHtml(translateUiText('Uygulama'))} / ${escapeHtml(translateUiText('Yayıncı'))} / ${escapeHtml(translateUiText('Sürüm'))} / ${escapeHtml(translateUiText('Mimari'))} / ${escapeHtml(translateUiText('Kurulum tarihi'))} / ${escapeHtml(translateUiText('Son görülme'))} | id=assetSoftwareInventoryRows |
| 1846 | Tablo | Alan / Anahtar / Grup / Kapsam / Tür / Zorunlu / Durum / İşlem | id=assetCustomFieldRows |
| 1847 | Sekme | assetTabOverviewButton | id=assetTabOverviewButton; type=button |
| 1848 | Sekme | assetTabRelationsButton | id=assetTabRelationsButton; type=button |
| 1849 | Sekme | assetTabMaintenanceButton | id=assetTabMaintenanceButton; type=button |
| 1850 | Sekme | assetTabHistoryButton | id=assetTabHistoryButton; type=button |
| 1851 | Sekme | assetTabAttachmentsButton | id=assetTabAttachmentsButton; type=button |
| 1852 | Sayfa rotası | Genel Bakış | hedef=Genel Bakış |
| 1853 | Sayfa rotası | Varlıklar | hedef=Varlıklar |
| 1854 | Sayfa rotası | Tüm satın almalar | hedef=Tüm satın almalar |
| 1855 | Sayfa rotası | Varlık siparişleri | hedef=Varlık siparişleri |
| 1856 | Sayfa rotası | Varlık modelleri | hedef=Varlık modelleri |
| 1857 | Sayfa rotası | Markalar | hedef=Markalar |
| 1858 | Sayfa rotası | Kategoriler | hedef=Kategoriler |
| 1859 | Sayfa rotası | Durum etiketleri | hedef=Durum etiketleri |
| 1860 | Sayfa rotası | QR ve barkodlar | hedef=QR ve barkodlar |
| 1861 | Sayfa rotası | Zimmetler | hedef=Zimmetler |
| 1862 | Sayfa rotası | İadeler | hedef=İadeler |
| 1863 | Sayfa rotası | Transferler | hedef=Transferler |
| 1864 | Sayfa rotası | Rezervasyonlar | hedef=Rezervasyonlar |
| 1865 | Sayfa rotası | Hareket geçmişi | hedef=Hareket geçmişi |
| 1866 | Sayfa rotası | Yazılım lisansları | hedef=Yazılım lisansları |
| 1867 | Sayfa rotası | Lisans atamaları | hedef=Lisans atamaları |
| 1868 | Sayfa rotası | Lisanslarım | hedef=Lisanslarım |
| 1869 | Sayfa rotası | Sözleşmeler | hedef=Sözleşmeler |
| 1870 | Sayfa rotası | Garanti takibi | hedef=Garanti takibi |
| 1871 | Sayfa rotası | Aksesuarlar | hedef=Aksesuarlar |
| 1872 | Sayfa rotası | Siparişler | hedef=Siparişler |
| 1873 | Sayfa rotası | Sarf malzemeleri | hedef=Sarf malzemeleri |
| 1874 | Sayfa rotası | Stok talepleri | hedef=Stok talepleri |
| 1875 | Sayfa rotası | Bileşenler | hedef=Bileşenler |
| 1876 | Sayfa rotası | Hazır kitler | hedef=Hazır kitler |
| 1877 | Sayfa rotası | Stok hareketleri | hedef=Stok hareketleri |
| 1878 | Sayfa rotası | Çalışan kayıtları | hedef=Çalışan kayıtları |
| 1879 | Sayfa rotası | Uygulama kullanıcıları | id=membersNav; hedef=Uygulama kullanıcıları |
| 1880 | Sayfa rotası | Departmanlar | hedef=Departmanlar |
| 1881 | Sayfa rotası | Yetki rolleri | hedef=Yetki rolleri |
| 1882 | Sayfa rotası | Lokasyonlar | hedef=Lokasyonlar |
| 1883 | Sayfa rotası | Binalar ve odalar | hedef=Binalar ve odalar |
| 1884 | Sayfa rotası | Şirket birimleri | hedef=Şirket birimleri |
| 1885 | Sayfa rotası | Bakım kayıtları | hedef=Bakım kayıtları |
| 1886 | Sayfa rotası | Bakım planları | hedef=Bakım planları |
| 1887 | Sayfa rotası | Denetimler | hedef=Denetimler |
| 1888 | Sayfa rotası | Takvim | hedef=Takvim |
| 1889 | Sayfa rotası | Görevler | hedef=Görevler |
| 1890 | Sayfa rotası | Hazır raporlar | hedef=Hazır raporlar |
| 1891 | Sayfa rotası | Özel rapor oluştur | hedef=Özel rapor oluştur |
| 1892 | Sayfa rotası | Dışa aktarımlar | hedef=Dışa aktarımlar |
| 1893 | Sayfa rotası | Tablodan içe aktar | hedef=Tablodan içe aktar |
| 1894 | Sayfa rotası | Verileri dışa aktar | hedef=Verileri dışa aktar |
| 1895 | Sayfa rotası | Yedekleme geçmişi | hedef=Yedekleme geçmişi |
| 1896 | Sayfa rotası | Geri yükle | hedef=Geri yükle |
| 1897 | Sayfa rotası | Firma profili | hedef=Firma profili |
| 1898 | Sayfa rotası | Logo ve tema | hedef=Logo ve tema |
| 1899 | Sayfa rotası | Bildirimler | hedef=Bildirimler |
| 1900 | Sayfa rotası | Entegrasyonlar | hedef=Entegrasyonlar |
| 1901 | Sayfa rotası | Güvenlik | hedef=Güvenlik |
| 1902 | Sayfa rotası | Denetim kayıtları | hedef=Denetim kayıtları |
| 1903 | Sayfa rotası | Destek | hedef=Destek |
| 1904 | Sayfa rotası | Varlıklar | hedef=Varlıklar |
| 1905 | Sayfa rotası | Hareket geçmişi | hedef=Hareket geçmişi |
| 1906 | Sayfa rotası | Takvim | hedef=Takvim |
| 1907 | Sayfa rotası | Hazır raporlar | hedef=Hazır raporlar |
| 1908 | Sayfa rotası | Yazılım lisansları | type=button; hedef=Yazılım lisansları |
| 1909 | Sayfa rotası | Varlıklar | type=button; hedef=Varlıklar |
| 1910 | Sayfa rotası | Varlıklar | type=button; hedef=Varlıklar |
| 1911 | Sayfa rotası | Çalışan kayıtları | type=button; hedef=Çalışan kayıtları |
| 1912 | Sayfa rotası | Aksesuarlar | type=button; hedef=Aksesuarlar |
| 1913 | Sayfa rotası | Sarf malzemeleri | type=button; hedef=Sarf malzemeleri |
| 1914 | Sayfa rotası | Bileşenler | type=button; hedef=Bileşenler |
| 1915 | Sayfa rotası | Yazılım lisansları | type=button; hedef=Yazılım lisansları |
| 1916 | Sayfa rotası | Genel Bakış | type=button; hedef=Genel Bakış |
| 1917 | Menü grubu | inventory | hedef=inventory |
| 1918 | Menü grubu | movement | hedef=movement |
| 1919 | Menü grubu | software | hedef=software |
| 1920 | Menü grubu | supplies | hedef=supplies |
| 1921 | Menü grubu | people | hedef=people |
| 1922 | Menü grubu | places | hedef=places |
| 1923 | Menü grubu | operations | hedef=operations |
| 1924 | Menü grubu | reports | hedef=reports |
| 1925 | Menü grubu | data | hedef=data |
| 1926 | Menü grubu | settings | hedef=settings |

### Platform yönetimi — `src/EnvanterTR.Web/wwwroot/platform-admin.html`

| # | Tür | Etiket / görünen metin | Kaynak ipucu |
|---:|---|---|---|
| 1 | Bağlantı | İlk platform yöneticisini oluştur | id=platformBootstrapLink; hedef=/platform-setup.html; başlangıçta gizli |
| 2 | Bağlantı | Şirket kullanıcı girişine dön | id=companyLoginLink; hedef=/ |
| 3 | Düğme | Sunucu bağlantısını test et | id=platformCheckConnection; type=button |
| 4 | Düğme | Güvenli giriş yap | id=platformLoginSubmit; type=submit |
| 5 | Düğme | Parolamı unuttum | id=platformForgotPassword; type=button |
| 6 | Düğme | Kurulumu başlat | id=platformMfaStart; type=submit |
| 7 | Düğme | Anahtarı kopyala | id=platformMfaCopySecret; type=button |
| 8 | Düğme | Doğrula ve etkinleştir | id=platformMfaEnable; type=button |
| 9 | Düğme | Kurtarma kodlarını kopyala | id=platformMfaCopyRecovery; type=button |
| 10 | Düğme | Güvenli yönetim paneline geç | id=platformMfaContinue; type=button |
| 11 | Düğme | Çıkış yap | id=platformMfaLogout; type=button |
| 12 | Düğme | ▦ Genel Bakış |  |
| 13 | Düğme | ♙ Müşteriler / Firmalar |  |
| 14 | Düğme | ♧ Üyeler / Hesaplar |  |
| 15 | Düğme | ◷ Demo talepleri |  |
| 16 | Düğme | ↗ Satın almalar |  |
| 17 | Düğme | ◇ Planlar & Fiyatlar |  |
| 18 | Düğme | ◉ Web Sitesi İçeriği |  |
| 19 | Düğme | ✉ Destek talepleri |  |
| 20 | Düğme | ≡ Yönetici İşlem Kaydı |  |
| 21 | Düğme | Çıkış | id=logout |
| 22 | Düğme | Firmaları Gör |  |
| 23 | Düğme | Planları Yönet |  |
| 24 | Düğme | Yenile | id=refreshSystemHealth; type=button |
| 25 | Düğme | Yenile | id=refreshRecentLogins; type=button |
| 26 | Düğme | Yenile | id=refreshRecentPayments; type=button |
| 27 | Düğme | Tüm siparişleri aç | type=button |
| 28 | Düğme | Firma listesini aç |  |
| 29 | Düğme | Yenile | id=refreshFollowUps |
| 30 | Düğme | ＋ Firma oluştur | id=provisionCompany |
| 31 | Düğme | Uygula | id=searchCompanies |
| 32 | Düğme | Seçilenleri askıya al | id=bulkSuspendCompanies; type=button; başlangıçta gizli |
| 33 | Düğme | Seçilenleri etkinleştir | id=bulkReactivateCompanies; type=button; başlangıçta gizli |
| 34 | Düğme | Seçilenlerin planını değiştir | id=bulkChangeCompanyPlan; type=button; başlangıçta gizli |
| 35 | Düğme | Filtrelenmiş şirket listesini CSV olarak indir | id=exportCompanies; type=button |
| 36 | Düğme | ← Önceki | id=prevCompanies |
| 37 | Düğme | Sonraki → | id=nextCompanies |
| 38 | Düğme | Yenile | id=refreshOrders |
| 39 | Düğme | Ara | id=searchOrders |
| 40 | Düğme | ← Önceki | id=prevOrders |
| 41 | Düğme | Sonraki → | id=nextOrders |
| 42 | Düğme | ＋ Yeni Plan | id=newPlan |
| 43 | Düğme | Kuponu kaydet | type=submit |
| 44 | Düğme | Temizle | id=clearCoupon; type=button |
| 45 | Düğme | Formu Temizle | id=clearPlan; type=button |
| 46 | Düğme | Planı Kaydet | type=submit |
| 47 | Düğme | Yeni içerik | id=clearContent; type=button |
| 48 | Düğme | İçeriği Kaydet | type=submit |
| 49 | Düğme | Yenile | id=refreshAudit |
| 50 | Düğme | Süreyi kaydet | id=saveAuditRetention |
| 51 | Düğme | Silinecek kayıtları önizle | id=previewAuditRetention |
| 52 | Düğme | Önizlenen eski kayıtları sil | id=applyAuditRetention |
| 53 | Düğme | Filtrele | id=searchAudit |
| 54 | Düğme | Temizle | id=clearAudit |
| 55 | Düğme | ← Önceki | id=prevAudit |
| 56 | Düğme | Sonraki → | id=nextAudit |
| 57 | Düğme | + Üye davet et | id=openMemberInvite |
| 58 | Düğme | Yenile | id=refreshMembers |
| 59 | Düğme | Ara | id=searchMembers |
| 60 | Düğme | ← Önceki | id=prevMembers |
| 61 | Düğme | Sonraki → | id=nextMembers |
| 62 | Düğme | Kapat | id=closeMemberDetail |
| 63 | Düğme | Yenile | id=refreshDemoLeads |
| 64 | Düğme | Ara | id=searchDemoLeads |
| 65 | Düğme | ← Önceki | id=prevDemoLeads |
| 66 | Düğme | Sonraki → | id=nextDemoLeads |
| 67 | Düğme | Kapat | id=closeDemoLeadDetail |
| 68 | Düğme | Yenile | id=refreshSupport |
| 69 | Düğme | ← Önceki | id=prevSupport |
| 70 | Düğme | Sonraki → | id=nextSupport |
| 71 | Düğme | Kapat | id=hideSupportDetail |
| 72 | Düğme | Yanıt gönder | id=supportAdminReplyButton; type=submit |
| 73 | Düğme | Durumu kaydet | type=submit |
| 74 | Düğme | ✕ | type=button |
| 75 | Düğme | Vazgeç | type=button |
| 76 | Düğme | İşlemi Uygula | type=submit |
| 77 | Düğme | Kapat | id=platformRecoveryClose; type=button |
| 78 | Düğme | İptal | id=platformRecoveryCancel; type=button |
| 79 | Düğme | Bağlantı iste | id=platformRecoverySubmit; type=submit |
| 80 | Düğme | Kapat | id=platformReauthClose; type=button |
| 81 | Düğme | İptal | id=platformReauthCancel; type=button |
| 82 | Düğme | Doğrula ve devam et | id=platformReauthSubmit; type=submit |
| 83 | Düğme | Vazgeç | id=impersonationCancel; type=button |
| 84 | Düğme | Panele gir | id=impersonationSubmit; type=submit |
| 85 | Düğme | ✕ | type=button |
| 86 | Düğme | Kapat | type=button |
| 87 | Düğme | Kapat | id=closeMemberInvite; type=button |
| 88 | Düğme | İptal | id=cancelMemberInvite; type=button |
| 89 | Düğme | Davet gönder | id=submitMemberInvite; type=submit |
| 90 | Düğme | Kapat | id=closePaymentFollowUp; type=button |
| 91 | Düğme | Vazgeç | id=cancelPaymentFollowUp; type=button |
| 92 | Düğme | Takip kaydını ekle | type=submit |
| 93 | Düğme | Hesabı aç | type=button |
| 94 | Düğme | Aç | id=${esc(x.id)} |
| 95 | Düğme | Takip kaydı |  |
| 96 | Düğme | Detay | id=${c.id}; action=details |
| 97 | Düğme | ${c.isActive?'Askıya al':'Etkinleştir'} | id=${c.id}; action=toggle |
| 98 | Düğme | Demo | id=${c.id}; action=demo |
| 99 | Düğme | Uzat | id=${c.id}; action=extend |
| 100 | Düğme | Yaşam döngüsünü kaydet | type=submit |
| 101 | Düğme | ${esc(m.displayName)} | name=${esc(m.displayName)} |
| 102 | Düğme | Önceki sayfa |  |
| 103 | Düğme | =d.membersTotalPages?'disabled':''}>Sonraki sayfa |  |
| 104 | Düğme | Yetkili erişim e-postasını yeniden gönder |  |
| 105 | Düğme | Düzenle |  |
| 106 | Düğme | Düzenle |  |
| 107 | Düğme | Düzenle |  |
| 108 | Düğme | Aç |  |
| 109 | Girdi alanı | email | type=email; name=email |
| 110 | Girdi alanı | password | type=password; name=password |
| 111 | Girdi alanı | twoFactorCode | type=text; name=twoFactorCode |
| 112 | Girdi alanı | rememberMe | type=checkbox; name=rememberMe |
| 113 | Girdi alanı | currentPassword | type=password; name=currentPassword |
| 114 | Girdi alanı | platformMfaSecret | id=platformMfaSecret |
| 115 | Girdi alanı | platformMfaCode | id=platformMfaCode |
| 116 | Girdi alanı | platformMfaSavedRecovery | id=platformMfaSavedRecovery; type=checkbox |
| 117 | Girdi alanı | Firma veya e-posta ara (Ctrl+K) | id=globalSearch |
| 118 | Girdi alanı | companySearch | id=companySearch |
| 119 | Seçim alanı | (değer yok): Tüm durumlar, true: Aktif, false: Askıdaki | id=companyActive; seçenekler=(değer yok): Tüm durumlar, true: Aktif, false: Askıdaki |
| 120 | Seçim alanı | Paket filtresi | id=companyPlan; seçenekler=(değer yok): Tüm paketler |
| 121 | Girdi alanı | Kayıt tarihi başlangıcı | id=companyCreatedFrom; type=date |
| 122 | Girdi alanı | Kayıt tarihi bitişi | id=companyCreatedTo; type=date |
| 123 | Seçim alanı | Sıralama | id=companySort; seçenekler=name:asc: Firma adı A–Z, name:desc: Firma adı Z–A, createdAt:desc: En yeni kayıt, createdAt:asc: En eski kayıt, status:asc: Durum: askıdaki önce, status:desc: Durum: aktif önce, followUp:asc: Takip tarihi artan, followUp:desc: Takip tarihi azalan |
| 124 | Seçim alanı | Sayfa başına firma | id=companyPageSize; seçenekler=50: 50 / sayfa, 100: 100 / sayfa, 250: 250 / sayfa |
| 125 | Girdi alanı | orderSearch | id=orderSearch |
| 126 | Seçim alanı | (değer yok): Tüm durumlar, Provisioned: Şirket açıldı, PaymentStarted: Ödeme başladı, PaymentReview: İnceleme bekliyor, PaymentFailed: Ödeme başarısız, ProvisioningFailed: Şirket açılamadı, Expired: Süresi doldu, Pending: Bekliyor | id=orderStatus; seçenekler=(değer yok): Tüm durumlar, Provisioned: Şirket açıldı, PaymentStarted: Ödeme başladı, PaymentReview: İnceleme bekliyor, PaymentFailed: Ödeme başarısız, ProvisioningFailed: Şirket açılamadı, Expired: Süresi doldu, Pending: Bekliyor |
| 127 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 128 | Girdi alanı | code | name=code |
| 129 | Seçim alanı | discountType | name=discountType; seçenekler=Percent: Yüzde, Fixed: Sabit tutar |
| 130 | Girdi alanı | discountValue | type=number; name=discountValue |
| 131 | Girdi alanı | currency | name=currency |
| 132 | Seçim alanı | planId | name=planId; seçenekler=(değer yok): Tüm Cloud planları |
| 133 | Girdi alanı | startsAt | type=datetime-local; name=startsAt |
| 134 | Girdi alanı | endsAt | type=datetime-local; name=endsAt |
| 135 | Girdi alanı | maxRedemptions | type=number; name=maxRedemptions |
| 136 | Girdi alanı | maxRedemptionsPerCustomer | type=number; name=maxRedemptionsPerCustomer |
| 137 | Seçim alanı | isActive | name=isActive; seçenekler=true: Etkin, false: Pasif |
| 138 | Metin alanı | changeReason | name=changeReason |
| 139 | Metin alanı | changeReason | name=changeReason |
| 140 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 141 | Girdi alanı | code | name=code |
| 142 | Girdi alanı | name | name=name |
| 143 | Seçim alanı | deploymentKind | name=deploymentKind; seçenekler=cloud: Bulut, local-windows: Windows 11 Yerel, windows-server: Windows Server, linux: Linux |
| 144 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): EUR, (değer yok): USD, (değer yok): GBP, (değer yok): RSD, (değer yok): MKD, (değer yok): UAH, (değer yok): RUB, (değer yok): CHF |
| 145 | Girdi alanı | maxLoginUsers | id=planMaxLoginUsers; type=number; name=maxLoginUsers |
| 146 | Girdi alanı | maxAssets | type=number; name=maxAssets |
| 147 | Girdi alanı | monthlyPrice | type=number; name=monthlyPrice |
| 148 | Girdi alanı | annualPrice | type=number; name=annualPrice |
| 149 | Girdi alanı | oneTimePrice | type=number; name=oneTimePrice |
| 150 | Girdi alanı | offlineGraceDays | type=number; name=offlineGraceDays |
| 151 | Metin alanı | description | name=description |
| 152 | Metin alanı | featureFlagsJson | name=featureFlagsJson |
| 153 | Girdi alanı | planReportsEnabled | id=planReportsEnabled; type=checkbox |
| 154 | Girdi alanı | planApiEnabled | id=planApiEnabled; type=checkbox |
| 155 | Girdi alanı | planMaintenanceEnabled | id=planMaintenanceEnabled; type=checkbox |
| 156 | Girdi alanı | supportsOfflineUse | type=checkbox; name=supportsOfflineUse |
| 157 | Girdi alanı | isPublic | type=checkbox; name=isPublic |
| 158 | Girdi alanı | isActive | type=checkbox; name=isActive |
| 159 | Girdi alanı | sortOrder | type=number; name=sortOrder |
| 160 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 161 | Girdi alanı | key | name=key |
| 162 | Girdi alanı | title | name=title |
| 163 | Metin alanı | body | name=body |
| 164 | Girdi alanı | mediaPath | name=mediaPath |
| 165 | Girdi alanı | isPublished | type=checkbox; name=isPublished |
| 166 | Girdi alanı | auditRetentionDays | id=auditRetentionDays; type=number |
| 167 | Girdi alanı | auditRetentionConfirmation | id=auditRetentionConfirmation; type=text |
| 168 | Girdi alanı | auditSearch | id=auditSearch; type=search |
| 169 | Seçim alanı | (değer yok): Tüm kaynaklar, platform: Platform, company: Şirket | id=auditSource; seçenekler=(değer yok): Tüm kaynaklar, platform: Platform, company: Şirket |
| 170 | Girdi alanı | auditAction | id=auditAction |
| 171 | Girdi alanı | auditTargetType | id=auditTargetType |
| 172 | Girdi alanı | auditFrom | id=auditFrom; type=date |
| 173 | Girdi alanı | auditTo | id=auditTo; type=date |
| 174 | Girdi alanı | memberSearch | id=memberSearch |
| 175 | Girdi alanı | demoLeadSearch | id=demoLeadSearch |
| 176 | Seçim alanı | (değer yok): Tüm durumlar, New: Yeni, Contacted: İletişime geçildi, Qualified: Nitelikli, DemoScheduled: Demo planlandı, DemoGranted: Demo açıldı, Converted: Müşteri oldu, Closed: Kapalı | id=demoLeadStatus; seçenekler=(değer yok): Tüm durumlar, New: Yeni, Contacted: İletişime geçildi, Qualified: Nitelikli, DemoScheduled: Demo planlandı, DemoGranted: Demo açıldı, Converted: Müşteri oldu, Closed: Kapalı |
| 177 | Seçim alanı | Takip tarihi filtresi | id=demoLeadFollowUp; seçenekler=(değer yok): Tüm takipler, overdue: Takibi gecikenler |
| 178 | Girdi alanı | supportSearch | id=supportSearch |
| 179 | Seçim alanı | (değer yok): Tüm durumlar, Open: Açık, InProgress: İşlemde, AwaitingCustomer: Müşteri yanıtı bekleniyor, Resolved: Çözüldü, Closed: Kapatıldı | id=supportStatus; seçenekler=(değer yok): Tüm durumlar, Open: Açık, InProgress: İşlemde, AwaitingCustomer: Müşteri yanıtı bekleniyor, Resolved: Çözüldü, Closed: Kapatıldı |
| 180 | Metin alanı | body | name=body |
| 181 | Seçim alanı | status | name=status; seçenekler=Open: Açık, InProgress: İşlemde, AwaitingCustomer: Müşteri yanıtı bekleniyor, Resolved: Çözüldü, Closed: Kapatıldı |
| 182 | Girdi alanı | email | type=email; name=email |
| 183 | Girdi alanı | password | type=password; name=password |
| 184 | Girdi alanı | code | type=text; name=code |
| 185 | Metin alanı | reason | name=reason |
| 186 | Girdi alanı | memberInviteCompanySearch | id=memberInviteCompanySearch; type=search |
| 187 | Seçim alanı | companyId | id=memberInviteCompany; name=companyId; seçenekler=(değer yok): Önce şirketleri yükleyin |
| 188 | Girdi alanı | fullName | name=fullName |
| 189 | Girdi alanı | email | type=email; name=email |
| 190 | Seçim alanı | roleKey | name=roleKey; seçenekler=viewer: Salt okunur, asset_operator: Varlık sorumlusu, asset_manager: Varlık yöneticisi, auditor: Denetçi, hr_operator: Ekip yöneticisi, admin: Şirket yöneticisi |
| 191 | Seçim alanı | outcome | name=outcome; seçenekler=Contacted: Müşteriyle iletişim kuruldu, NoResponse: Yanıt alınamadı, ReminderScheduled: Hatırlatma planlandı, CustomerRequestedCallback: Müşteri geri arama istedi, Other: Diğer |
| 192 | Girdi alanı | nextFollowUpAt | type=datetime-local; name=nextFollowUpAt |
| 193 | Metin alanı | note | name=note |
| 194 | Girdi alanı | Bu sayfadaki tüm firmaları seç | type=checkbox |
| 195 | Girdi alanı | ${esc(c.name)} firmasını seç | type=checkbox |
| 196 | Seçim alanı | stage | name=stage |
| 197 | Seçim alanı | assignedAdminUserId | name=assignedAdminUserId; seçenekler=(değer yok): Atanmamış |
| 198 | Girdi alanı | nextFollowUpAt | type=datetime-local; name=nextFollowUpAt |
| 199 | Metin alanı | note | name=note |
| 200 | Metin alanı | note | name=note |
| 201 | Seçim alanı | planId | name=planId; seçenekler=${esc(plan.id)}: ${esc(plan.name)} · ${esc(plan.code)} |
| 202 | Metin alanı | note | name=note |
| 203 | Metin alanı | note | name=note |
| 204 | Seçim alanı | planId | name=planId; seçenekler=${esc(p.id)}: ${esc(p.name)} — ${esc(p.deploymentKind)} |
| 205 | Girdi alanı | days | type=number; name=days |
| 206 | Girdi alanı | note | name=note |
| 207 | Seçim alanı | provisioningMode | name=provisioningMode; seçenekler=trial: 15 günlük ücretsiz deneme, paid: Harici ödemesi doğrulanmış abonelik |
| 208 | Girdi alanı | companyName | name=companyName |
| 209 | Girdi alanı | slug | name=slug |
| 210 | Girdi alanı | contactEmail | type=email; name=contactEmail |
| 211 | Girdi alanı | ownerName | name=ownerName |
| 212 | Girdi alanı | ownerEmail | type=email; name=ownerEmail |
| 213 | Seçim alanı | planId | name=planId; seçenekler=${esc(p.id)}: ${esc(p.name)} · ${esc(p.code)} |
| 214 | Girdi alanı | termDays | type=number; name=termDays |
| 215 | Girdi alanı | paymentReference | name=paymentReference |
| 216 | Girdi alanı | paymentVerified | type=checkbox; name=paymentVerified |
| 217 | Girdi alanı | endsAt | type=datetime-local; name=endsAt |
| 218 | Girdi alanı | note | name=note |
| 219 | Form | loginForm | id=loginForm |
| 220 | Form | platformMfaSetupForm | id=platformMfaSetupForm |
| 221 | Form | couponForm | id=couponForm |
| 222 | Form | planForm | id=planForm |
| 223 | Form | contentForm | id=contentForm |
| 224 | Form | supportAdminReply | id=supportAdminReply |
| 225 | Form | supportAdminStatus | id=supportAdminStatus |
| 226 | Form | actionForm | id=actionForm |
| 227 | Form | platformRecoveryForm | id=platformRecoveryForm |
| 228 | Form | platformReauthForm | id=platformReauthForm |
| 229 | Form | impersonationForm | id=impersonationForm |
| 230 | Form | memberInviteForm | id=memberInviteForm |
| 231 | Form | paymentFollowUpForm | id=paymentFollowUpForm |
| 232 | Form | companyLifecycleForm | id=companyLifecycleForm |
| 233 | Modal | İşlem | id=actionDialog; type=button |
| 234 | Modal | Kapat | id=platformRecoveryDialog; type=button; name=email |
| 235 | Modal | Kapat | id=platformReauthDialog; type=button; name=password |
| 236 | Modal | reason | id=impersonationDialog; type=button; name=reason |
| 237 | Modal | Firma ayrıntısı | id=companyDetailDialog; type=button |
| 238 | Modal | companyId | id=memberInviteDialog; type=button; name=companyId |
| 239 | Modal | Kapat | id=paymentFollowUpDialog; type=button; name=outcome |
| 240 | Tablo | ${x} |  |

### Platform kurulumu — `src/EnvanterTR.Web/wwwroot/platform-setup.html`

| # | Tür | Etiket / görünen metin | Kaynak ipucu |
|---:|---|---|---|
| 1 | Bağlantı | ← Platform girişine dön | hedef=/platform-admin.html |
| 2 | Düğme | Platform yöneticisini oluştur | type=submit |
| 3 | Girdi alanı | fullName | name=fullName |
| 4 | Girdi alanı | email | type=email; name=email |
| 5 | Girdi alanı | password | type=password; name=password |
| 6 | Girdi alanı | setupToken | type=password; name=setupToken |
| 7 | Form | setupForm | id=setupForm |

### Satış sitesi — `src/EnvanterTR.PublicSite/index.html`

| # | Tür | Etiket / görünen metin | Kaynak ipucu |
|---:|---|---|---|
| 1 | Bağlantı | Ana içeriğe geç | hedef=#top |
| 2 | Bağlantı | E Envanter TR | hedef=#top; başlangıçta gizli |
| 3 | Bağlantı | Modüller | hedef=#ozellikler |
| 4 | Bağlantı | Nasıl çalışır? | hedef=#nasil-calisir |
| 5 | Bağlantı | Kurulum | hedef=#kurulum |
| 6 | Bağlantı | Paketler | hedef=#paketler |
| 7 | Bağlantı | Canlı demo | hedef=/demo/ |
| 8 | Bağlantı | Müşteri girişi | hedef=/giris |
| 9 | Bağlantı | Demoyu deneyin | hedef=/demo/ |
| 10 | Bağlantı | Canlı demoyu aç → | hedef=/demo/; başlangıçta gizli |
| 11 | Bağlantı | Örnek kayıtları inceleyin → | hedef=/demo/ |
| 12 | Bağlantı | Envanter ekranını inceleyin → | id=assetLabelDemo; hedef=/demo/ |
| 13 | Bağlantı | Bilgi iste | hedef=mailto:admin@envantertr.com?subject=EnvanterTR%20Windows%2011%20kurulum%20paketi |
| 14 | Bağlantı | Teklif iste | hedef=mailto:admin@envantertr.com?subject=EnvanterTR%20Windows%20Server%20kurulum%20teklifi |
| 15 | Bağlantı | Teklif iste | hedef=mailto:admin@envantertr.com?subject=EnvanterTR%20Ubuntu%20Linux%20kurulum%20teklifi |
| 16 | Bağlantı | Teklif isteyin: admin@envantertr.com | hedef=mailto:admin@envantertr.com?subject=EnvanterTR%20plan%20teklifi |
| 17 | Bağlantı | Etkileşimli ürün demosunu açın | hedef=/demo/ |
| 18 | Bağlantı | Kurulum seçeneklerini görüşün | hedef=mailto:admin@envantertr.com?subject=EnvanterTR%20kurulum |
| 19 | Bağlantı | admin@envantertr.com | hedef=mailto:admin@envantertr.com?subject=EnvanterTR%20paket%20ihtiyac%C4%B1 |
| 20 | Bağlantı | Tanıtım görüşmesi isteyin | hedef=#demo |
| 21 | Bağlantı | Canlı demoyu aç | hedef=/demo/ |
| 22 | Bağlantı | E Envanter TR | hedef=#top; başlangıçta gizli |
| 23 | Bağlantı | ${cell} | hedef=/demo/ |
| 24 | Bağlantı | Filtreler | hedef=/demo/ |
| 25 | Bağlantı | Tüm varlıkları aç → | hedef=/demo/ |
| 26 | Bağlantı | Ekipleri gör | hedef=/demo/ |
| 27 | Bağlantı | Profilleri incele → | hedef=/demo/ |
| 28 | Bağlantı | Stok görünümü | hedef=/demo/ |
| 29 | Bağlantı | Aksesuarları aç → | hedef=/demo/ |
| 30 | Bağlantı | Konumları gör | hedef=/demo/ |
| 31 | Bağlantı | Konum envanterini aç → | hedef=/demo/ |
| 32 | Bağlantı | Rapor ekranını aç → | hedef=/demo/ |
| 33 | Düğme | Menüyü aç | id=menuToggle; type=button |
| 34 | Düğme | Tanıtım görüşmesi iste | type=button |
| 35 | Düğme | ⌂ Genel Bakış | type=button |
| 36 | Düğme | ▣ Varlıklar | type=button |
| 37 | Düğme | ♙ Kişiler | type=button |
| 38 | Düğme | ⌑ Aksesuarlar | type=button |
| 39 | Düğme | ⌖ Konumlar | type=button |
| 40 | Düğme | ▤ Raporlar | type=button |
| 41 | Düğme | Tanıtım iste | type=button |
| 42 | Düğme | Kapat | id=demoRequestClose; type=button |
| 43 | Düğme | Vazgeç | id=demoRequestCancel; type=button |
| 44 | Düğme | Tanıtım isteği gönder | id=demoRequestSubmit; type=submit |
| 45 | Seçim alanı | Sayfa dili | id=homeLanguage; seçenekler=tr: TR, en: EN, ru: RU, uk: UK, mk: MK |
| 46 | Seçim alanı | Renk teması | id=homeTheme; seçenekler=forest: Yeşil, navy: Lacivert, gold: Sarı, violet: Mor |
| 47 | Girdi alanı | companyName | name=companyName |
| 48 | Girdi alanı | contactName | name=contactName |
| 49 | Girdi alanı | jobTitle | name=jobTitle |
| 50 | Girdi alanı | phone | type=tel; name=phone |
| 51 | Girdi alanı | email | type=email; name=email |
| 52 | Seçim alanı | teamSize | name=teamSize; seçenekler=(değer yok): Seçin, (değer yok): 1-10, (değer yok): 11-50, (değer yok): 51-200, (değer yok): 201-1000, (değer yok): 1000+ |
| 53 | Seçim alanı | assetCount | name=assetCount; seçenekler=(değer yok): Belirtmek istemiyorum, (değer yok): 1-50, (değer yok): 51-250, (değer yok): 251-1000, (değer yok): 1001-5000, (değer yok): 5000+ |
| 54 | Metin alanı | message | name=message |
| 55 | Girdi alanı | website | name=website |
| 56 | Girdi alanı | consent | type=checkbox; name=consent |
| 57 | Form | demoRequestForm | id=demoRequestForm |
| 58 | Modal | Kapat | id=demoRequestDialog; type=button; name=companyName; başlangıçta gizli |

### Ödeme sayfası — `src/EnvanterTR.PublicSite/checkout.html`

| # | Tür | Etiket / görünen metin | Kaynak ipucu |
|---:|---|---|---|
| 1 | Bağlantı | E Envanter TR | hedef=/; başlangıçta gizli |
| 2 | Bağlantı | ← Paketlere dön | hedef=/#paketler |
| 3 | Bağlantı | Önce demoyu incele | id=demoLink; hedef=/demo/ |
| 4 | Düğme | Aylık | id=monthlyTerm; type=button |
| 5 | Düğme | Yıllık | id=annualTerm; type=button |
| 6 | Düğme | Uygula | id=applyCoupon; type=button |
| 7 | Düğme | Yeni ödeme denemesi oluştur | id=newAttempt; type=button |
| 8 | Düğme | Güvenli ödemeye geç | id=checkoutSubmit; type=submit |
| 9 | Girdi alanı | couponCode | id=couponCode; type=text |
| 10 | Girdi alanı | companyName | name=companyName |
| 11 | Girdi alanı | slug | name=slug |
| 12 | Girdi alanı | ownerFirstName | name=ownerFirstName |
| 13 | Girdi alanı | ownerLastName | name=ownerLastName |
| 14 | Girdi alanı | ownerEmail | type=email; name=ownerEmail |
| 15 | Girdi alanı | phone | type=tel; name=phone |
| 16 | Girdi alanı | identityNumber | name=identityNumber |
| 17 | Girdi alanı | zipCode | name=zipCode |
| 18 | Girdi alanı | billingAddress | name=billingAddress |
| 19 | Girdi alanı | city | name=city |
| 20 | Girdi alanı | country | name=country |
| 21 | Girdi alanı | dataProcessingAcknowledged | type=checkbox; name=dataProcessingAcknowledged |
| 22 | Form | checkoutForm | id=checkoutForm |

## Kapsam notları

- `data-page` ve `data-group` kayıtları yönlendirme hedefi/menü grubu envanteridir; aynı HTML elemanı bağlantı veya düğme olarak ayrıca listelenebilir.
- Form alanları için `id`, `name`, tür ve varsa select seçenekleri kaynak ipucu olarak kaydedilir. Etiket ilişkisi tek başına bu statik dökümle doğrulanmış sayılmaz.
- Açılır menüler, koşullu alanlar ve JavaScript tarafından üretilen tablo satırı/eylemleri kaynakta statik değillerse bu sayımda yer almayabilir.
- Fonksiyon kabul sonuçları `docs/UX-MENU-ACTION-AUDIT.md` ve `docs/USER-REQUEST-CHECKLIST-2026-10-11.md` içinde kanıt ve açık kalan kapsamla izlenir.
