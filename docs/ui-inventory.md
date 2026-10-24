# EnvanterTR statik arayüz envanteri

Oluşturulma: 2026-10-24T21:28:41.355Z

> Bu dosya mevcut HTML kaynaklarında bulunan statik arayüz öğelerinin dökümüdür. Bir öğenin listelenmesi onun çalıştığını kanıtlamaz. JavaScript ile sonradan oluşturulan kontroller, API/DB davranışı, rol varyasyonları ve erişilebilirlik ayrıca uçtan uca kabul gerektirir. Kaynak değiştikçe `node tools/Generate-UiInventory.mjs` ile yeniden üretin; `node tools/Generate-UiInventory.mjs --check` güncel olup olmadığını doğrular.

## Sayım

| Tür | Statik öğe |
|---|---:|
| Sayfa rotası | 65 |
| Menü grubu | 10 |
| Bağlantı | 58 |
| Düğme | 968 |
| Form | 117 |
| Girdi alanı | 627 |
| Seçim alanı | 206 |
| Metin alanı | 45 |
| Modal | 110 |
| Sekme | 5 |
| Tablo | 79 |
| **Toplam kayıt** | **2290** |

## Tam döküm

### Şirket uygulaması — `src/EnvanterTR.Web/wwwroot/index.html`

| # | Tür | Etiket / görünen metin | Kaynak ipucu |
|---:|---|---|---|
| 1 | Bağlantı | E Envanter TR | hedef=#overview |
| 2 | Bağlantı | Şirket sahibi için yıllık abonelik ve ödeme → | hedef=/subscription/checkout |
| 3 | Bağlantı | tenantUrlLink | id=tenantUrlLink; başlangıçta gizli |
| 4 | Bağlantı | OpenAPI şemasını görüntüle ↗ | hedef=/openapi/customer-api-v1.json |
| 5 | Bağlantı | E Envanter TR | hedef=# |
| 6 | Bağlantı | İlk şirket kurulumunu başlat | hedef=/setup.html |
| 7 | Bağlantı | İlk şirket kurulumunu başlat | hedef=/setup.html |
| 8 | Bağlantı | Platform yönetici girişi | hedef=/platform-admin.html |
| 9 | Bağlantı | licenseEulaDocumentLink | id=licenseEulaDocumentLink |
| 10 | Bağlantı | Verilerimi indir (JSON) | id=downloadAccountData; hedef=/api/session/data-export |
| 11 | Bağlantı | ${escapeHtml(a.eulaVersion\|\|'EULA')} | hedef=/api/software-licenses/${id}/attachments/${a.eulaAttachmentId}/file |
| 12 | Bağlantı | ${escapeHtml(a.originalFileName)} | hedef=/api/software-licenses/${id}/attachments/${a.id}/file |
| 13 | Bağlantı | ${escapeHtml(file.originalFileName)} | hedef=${escapeHtml(file.fileUrl)} |
| 14 | Bağlantı | ${escapeHtml(item.originalFileName)} | hedef=/api/asset-orders/${orderId}/attachments/${item.id}/file |
| 15 | Bağlantı | Aç / indir | hedef=/api/accessory-orders/${orderId}/attachments/${item.id}/file |
| 16 | Bağlantı | ${escapeHtml(file.originalFileName)} | hedef=/api/models/${modelId}/attachments/${file.id}/file?download=true |
| 17 | Düğme | Kapat | id=closeEmployeeHandover; type=button |
| 18 | Düğme | Yeni zimmet belgesi oluştur | id=createEmployeeHandover; type=button |
| 19 | Düğme | İmzayı temizle | id=clearEmployeeHandoverRecipient; type=button |
| 20 | Düğme | İmzayı temizle | id=clearEmployeeHandoverIssuer; type=button |
| 21 | Düğme | Vazgeç | id=cancelEmployeeHandoverSigning; type=button |
| 22 | Düğme | İmzaları kaydet ve belgeyi yazdır | id=saveEmployeeHandoverSignatures; type=button |
| 23 | Düğme | Kapat | id=maintenanceAttachmentClose; type=button |
| 24 | Düğme | Dosyayı yükle | type=submit |
| 25 | Düğme | Şirket değiştir | id=companySwitchButton; type=button; başlangıçta gizli |
| 26 | Düğme | Genel Bakış | hedef=Genel Bakış |
| 27 | Düğme | inventory | hedef=inventory |
| 28 | Düğme | Varlıklar | hedef=Varlıklar |
| 29 | Düğme | Tüm satın almalar | hedef=Tüm satın almalar |
| 30 | Düğme | Varlık siparişleri | hedef=Varlık siparişleri |
| 31 | Düğme | Varlık modelleri | hedef=Varlık modelleri |
| 32 | Düğme | Markalar | hedef=Markalar |
| 33 | Düğme | Kategoriler | hedef=Kategoriler |
| 34 | Düğme | Durum etiketleri | hedef=Durum etiketleri |
| 35 | Düğme | QR ve barkodlar | hedef=QR ve barkodlar |
| 36 | Düğme | movement | hedef=movement |
| 37 | Düğme | Zimmetler | hedef=Zimmetler |
| 38 | Düğme | İadeler | hedef=İadeler |
| 39 | Düğme | Transferler | hedef=Transferler |
| 40 | Düğme | Rezervasyonlar | hedef=Rezervasyonlar |
| 41 | Düğme | Hareket geçmişi | hedef=Hareket geçmişi |
| 42 | Düğme | software | hedef=software |
| 43 | Düğme | Yazılım lisansları | hedef=Yazılım lisansları |
| 44 | Düğme | Lisans atamaları | hedef=Lisans atamaları |
| 45 | Düğme | Lisanslarım | hedef=Lisanslarım |
| 46 | Düğme | Sözleşmeler | hedef=Sözleşmeler |
| 47 | Düğme | Garanti takibi | hedef=Garanti takibi |
| 48 | Düğme | supplies | hedef=supplies |
| 49 | Düğme | Aksesuarlar | hedef=Aksesuarlar |
| 50 | Düğme | Siparişler | hedef=Siparişler |
| 51 | Düğme | Sarf malzemeleri | hedef=Sarf malzemeleri |
| 52 | Düğme | Stok talepleri | hedef=Stok talepleri |
| 53 | Düğme | Bileşenler | hedef=Bileşenler |
| 54 | Düğme | Hazır kitler | hedef=Hazır kitler |
| 55 | Düğme | Stok hareketleri | hedef=Stok hareketleri |
| 56 | Düğme | people | hedef=people |
| 57 | Düğme | Çalışan kayıtları | hedef=Çalışan kayıtları |
| 58 | Düğme | Uygulama kullanıcıları | id=membersNav; hedef=Uygulama kullanıcıları |
| 59 | Düğme | Departmanlar | hedef=Departmanlar |
| 60 | Düğme | Yetki rolleri | hedef=Yetki rolleri |
| 61 | Düğme | places | hedef=places |
| 62 | Düğme | Lokasyonlar | hedef=Lokasyonlar |
| 63 | Düğme | Binalar ve odalar | hedef=Binalar ve odalar |
| 64 | Düğme | Şirket birimleri | hedef=Şirket birimleri |
| 65 | Düğme | operations | hedef=operations |
| 66 | Düğme | Bakım kayıtları | hedef=Bakım kayıtları |
| 67 | Düğme | Bakım planları | hedef=Bakım planları |
| 68 | Düğme | Denetimler | hedef=Denetimler |
| 69 | Düğme | Takvim | hedef=Takvim |
| 70 | Düğme | Görevler | hedef=Görevler |
| 71 | Düğme | reports | hedef=reports |
| 72 | Düğme | Hazır raporlar | hedef=Hazır raporlar |
| 73 | Düğme | Özel rapor oluştur | hedef=Özel rapor oluştur |
| 74 | Düğme | Dışa aktarımlar | hedef=Dışa aktarımlar |
| 75 | Düğme | data | hedef=data |
| 76 | Düğme | Tablodan içe aktar | hedef=Tablodan içe aktar |
| 77 | Düğme | Verileri dışa aktar | hedef=Verileri dışa aktar |
| 78 | Düğme | Yedekleme geçmişi | hedef=Yedekleme geçmişi |
| 79 | Düğme | Geri yükle | hedef=Geri yükle |
| 80 | Düğme | settings | hedef=settings |
| 81 | Düğme | Firma profili | hedef=Firma profili |
| 82 | Düğme | Logo ve tema | hedef=Logo ve tema |
| 83 | Düğme | Bildirimler | hedef=Bildirimler |
| 84 | Düğme | Entegrasyonlar | hedef=Entegrasyonlar |
| 85 | Düğme | Güvenlik | hedef=Güvenlik |
| 86 | Düğme | Denetim kayıtları | hedef=Denetim kayıtları |
| 87 | Düğme | Destek | hedef=Destek |
| 88 | Düğme | Menüyü aç | id=mobileMenu |
| 89 | Düğme | Görünüm ayarları | id=appearanceButton; type=button |
| 90 | Düğme | Yardım | id=helpButton |
| 91 | Düğme | Yaklaşan işler | id=notificationButton; type=button; başlangıçta gizli |
| 92 | Düğme | Şirket takvimini aç → | id=openNotificationCalendar; type=button |
| 93 | Düğme | Profil fotoğrafını yönet | id=userAvatar; type=button |
| 94 | Düğme | Çıkış | id=logoutButton; type=button |
| 95 | Düğme | Platform yönetimine dön | id=impersonationReturn; type=button |
| 96 | Düğme | ▥ Barkod okut | id=scanAssetButton; type=button |
| 97 | Düğme | ＋ Yeni varlık | id=newAssetButton |
| 98 | Düğme | Panoyu düzenle | id=dashboardCustomizeButton; type=button |
| 99 | Düğme | Varlık CSV indir | id=exportButton; type=button |
| 100 | Düğme | Varlık Excel indir | id=exportXlsxButton; type=button |
| 101 | Düğme | Çöp kutusu | id=openAssetTrash; type=button |
| 102 | Düğme | Şirket verisini indir | id=exportBackupButton; type=button; başlangıçta gizli |
| 103 | Düğme | Aktarma dosyasını kontrol et | id=importPreviewButton; type=button; başlangıçta gizli |
| 104 | Düğme | Varlıklar | hedef=Varlıklar |
| 105 | Düğme | Aramayı kaydet | id=saveAssetSearch; type=button |
| 106 | Düğme | Kaydı sil | id=deleteAssetSearch; type=button |
| 107 | Düğme | İlişki ve garanti filtrelerini temizle | id=clearAssetRelationFilter; type=button; başlangıçta gizli |
| 108 | Düğme | Varsayılan sütunlar | id=resetAssetColumns; type=button |
| 109 | Düğme | Filtreli varlık listesini yazdır | id=assetPrintButton; type=button |
| 110 | Düğme | Varlık listesini tam ekranda aç | id=assetFullscreenButton; type=button |
| 111 | Düğme | Durumu uygula | id=applyBulkAssetStatus; type=button |
| 112 | Düğme | Seçileni CSV indir | id=exportSelectedAssets; type=button |
| 113 | Düğme | Seçimi temizle | id=clearSelectedAssets; type=button |
| 114 | Düğme | Demirbaş Etiketi | type=button |
| 115 | Düğme | Model | type=button |
| 116 | Düğme | Kategori | type=button |
| 117 | Düğme | Durum | type=button |
| 118 | Düğme | Çıkış Yapılmış Olan Kişi | type=button |
| 119 | Düğme | Konum | type=button |
| 120 | Düğme | Üretici | type=button |
| 121 | Düğme | ← Önceki | id=assetPrev |
| 122 | Düğme | Sonraki → | id=assetNext |
| 123 | Düğme | Hareket geçmişi | hedef=Hareket geçmişi |
| 124 | Düğme | Takvim | hedef=Takvim |
| 125 | Düğme | Hazır raporlar | hedef=Hazır raporlar |
| 126 | Düğme | ↓ CSV dışa aktar | id=exportEmployeesButton; type=button |
| 127 | Düğme | Excel (.xlsx) | id=employeeXlsxExport; type=button |
| 128 | Düğme | ↑ CSV içe aktar | id=employeeImportOpen; type=button; başlangıçta gizli |
| 129 | Düğme | ＋ Çalışan ekle | id=newEmployeeButton |
| 130 | Düğme | Departmanı temizle | id=clearEmployeeDepartmentFilter; type=button; başlangıçta gizli |
| 131 | Düğme | Yöneticiyi temizle | id=clearEmployeeManagerFilter; type=button; başlangıçta gizli |
| 132 | Düğme | Tarihleri temizle | id=clearEmployeeStartedDateFilter; type=button; başlangıçta gizli |
| 133 | Düğme | Sayfadakileri seç | id=selectEmployeesOnPage; type=button; başlangıçta gizli |
| 134 | Düğme | Seçilenleri güncelle | id=applyEmployeeBulkActive; type=button; başlangıçta gizli |
| 135 | Düğme | ← Önceki | id=employeePrev |
| 136 | Düğme | Sonraki → | id=employeeNext |
| 137 | Düğme | + Departman ekle | id=addDepartmentButton; type=button; başlangıçta gizli |
| 138 | Düğme | + Konum ekle | id=addLocationButton; type=button; başlangıçta gizli |
| 139 | Düğme | ↻ Yenile | id=directoryRefresh; type=button |
| 140 | Düğme | ＋ Birim ekle | id=addCompanyUnitButton; type=button; başlangıçta gizli |
| 141 | Düğme | ↻ Yenile | id=companyUnitsRefresh; type=button |
| 142 | Düğme | Kapat | id=companyUnitDialogClose; type=button |
| 143 | Düğme | İptal | id=companyUnitCancel; type=button |
| 144 | Düğme | Kaydet | type=submit |
| 145 | Düğme | Varlıkları aç | id=moduleGoAssets; type=button |
| 146 | Düğme | Çalışanları aç | id=moduleGoPeople; type=button |
| 147 | Düğme | CSV indir | id=accessoryExport; type=button |
| 148 | Düğme | Excel (.xlsx) | id=accessoryXlsxExport; type=button |
| 149 | Düğme | ↑ CSV içe aktar | id=accessoryImportOpen; type=button; başlangıçta gizli |
| 150 | Düğme | ↻ Yenile | id=accessoryRefresh; type=button |
| 151 | Düğme | Kategorileri yönet | id=manageAccessoryCategories; type=button; başlangıçta gizli |
| 152 | Düğme | ＋ Aksesuar ekle | id=newAccessoryButton; type=button |
| 153 | Düğme | Varsayılan sütunlar | id=resetAccessoryColumns; type=button |
| 154 | Düğme | Yazdır | id=accessoryPrint; type=button |
| 155 | Düğme | Tam ekran | id=accessoryFullscreen; type=button |
| 156 | Düğme | Filtreleri temizle | id=clearAccessoryAdvancedFilters; type=button |
| 157 | Düğme | ← Önceki | id=accessoryPrev; type=button |
| 158 | Düğme | Sonraki → | id=accessoryNext; type=button |
| 159 | Düğme | Sarf / bileşen siparişleri | id=openStockOrders; type=button |
| 160 | Düğme | CSV indir | id=accessoryOrderExport; type=button |
| 161 | Düğme | ↻ Yenile | id=accessoryOrdersRefresh; type=button |
| 162 | Düğme | Tedarikçiler | id=supplierCatalogButton; type=button |
| 163 | Düğme | ＋ Sipariş oluştur | id=newAccessoryOrderButton; type=button |
| 164 | Düğme | Filtreleri temizle | id=clearAccessoryOrderFilters; type=button |
| 165 | Düğme | ← Önceki | id=accessoryOrderPrev; type=button |
| 166 | Düğme | Sonraki → | id=accessoryOrderNext; type=button |
| 167 | Düğme | CSV indir | id=exportAccessoryKitsButton; type=button |
| 168 | Düğme | ↻ Yenile | id=accessoryKitsRefresh; type=button |
| 169 | Düğme | ＋ Kit oluştur | id=newAccessoryKitButton; type=button |
| 170 | Düğme | CSV indir | id=exportConsumablesButton; type=button |
| 171 | Düğme | Excel (.xlsx) | id=consumableXlsxExport; type=button |
| 172 | Düğme | ↑ CSV içe aktar | id=importConsumablesButton; type=button; başlangıçta gizli |
| 173 | Düğme | ＋ Sarf malzemesi ekle | id=newConsumableButton; type=button |
| 174 | Düğme | ← Önceki | id=consumablePrev; type=button |
| 175 | Düğme | Sonraki → | id=consumableNext; type=button |
| 176 | Düğme | ↓ Filtrelenmiş CSV | id=requestExport; type=button |
| 177 | Düğme | ↻ Yenile | id=requestsRefresh; type=button |
| 178 | Düğme | ← Önceki | id=requestPrev; type=button |
| 179 | Düğme | Sonraki → | id=requestNext; type=button |
| 180 | Düğme | Kapat | type=button |
| 181 | Düğme | Vazgeç | type=button |
| 182 | Düğme | Kaydet | id=inventoryRequestActionSubmit; type=submit |
| 183 | Düğme | ↓ CSV dışa aktar | id=licenseExportButton; type=button |
| 184 | Düğme | Excel (.xlsx) | id=licenseXlsxExport; type=button |
| 185 | Düğme | ↑ CSV içe aktar | id=licenseImportOpen; type=button; başlangıçta gizli |
| 186 | Düğme | ＋ Lisans ekle | id=newLicenseButton; type=button |
| 187 | Düğme | ← Önceki | id=licensePrev |
| 188 | Düğme | Sonraki → | id=licenseNext |
| 189 | Düğme | Yazılım lisansları | type=button; hedef=Yazılım lisansları |
| 190 | Düğme | ← Önceki | id=licenseAssignmentPrev |
| 191 | Düğme | Sonraki → | id=licenseAssignmentNext |
| 192 | Düğme | Yenile | id=myLicensesRefresh; type=button |
| 193 | Düğme | Yenile | id=companyNotificationPreferencesRefresh; type=button |
| 194 | Düğme | Değişiklikleri geri al | id=companyNotificationPreferencesReset; type=button |
| 195 | Düğme | Tercihleri kaydet | id=companyNotificationPreferencesSave; type=submit |
| 196 | Düğme | CSV indir | id=exportComponentsButton; type=button |
| 197 | Düğme | Excel (.xlsx) | id=componentXlsxExport; type=button |
| 198 | Düğme | ↑ CSV içe aktar | id=importComponentsButton; type=button; başlangıçta gizli |
| 199 | Düğme | ＋ Bileşen ekle | id=newComponentButton; type=button |
| 200 | Düğme | ← Önceki | id=componentPrev |
| 201 | Düğme | Sonraki → | id=componentNext |
| 202 | Düğme | CSV indir | id=exportInventoryMovementsButton; type=button |
| 203 | Düğme | ← Önceki | id=inventoryMovementPrev; type=button |
| 204 | Düğme | Sonraki → | id=inventoryMovementNext; type=button |
| 205 | Düğme | Varsayılanlara dön | id=labelSettingsReset; type=button |
| 206 | Düğme | Şablon ayarlarını yükle | id=labelStockTemplateLoad; type=button |
| 207 | Düğme | Şablonu kaydet | id=labelStockTemplateSave; type=button |
| 208 | Düğme | Şablonu sil | id=labelStockTemplateDelete; type=button |
| 209 | Düğme | Ayarları kaydet | id=labelSettingsSave; type=submit |
| 210 | Düğme | Varlık listesini aç | id=labelSettingsGoAssets; type=button |
| 211 | Düğme | Değişiklikleri kaydet | id=assetStatusLabelsSave; type=submit |
| 212 | Düğme | ＋ Özel durum ekle | id=addAssetCustomStatus; type=button |
| 213 | Düğme | Durum ve geçiş kurallarını kaydet | id=assetStatusConfigSave; type=submit |
| 214 | Düğme | ↻ Yenile | id=membersRefresh |
| 215 | Düğme | ＋ Kullanıcı davet et | id=inviteMemberButton |
| 216 | Düğme | ↻ Yenile | id=catalogRefresh; type=button |
| 217 | Düğme | ＋ Yeni marka ekle | id=catalogQuickBrand; type=button |
| 218 | Düğme | Kaydı ekle | type=submit |
| 219 | Düğme | Değişiklikleri kaydet | type=submit |
| 220 | Düğme | Logoyu güvenli şekilde yükle | type=submit |
| 221 | Düğme | Varlıklar | type=button; hedef=Varlıklar |
| 222 | Düğme | ← Önceki | id=assignmentPrev; type=button |
| 223 | Düğme | Sonraki → | id=assignmentNext; type=button |
| 224 | Düğme | ↻ Yenile | id=returnsRefresh; type=button |
| 225 | Düğme | ← Önceki | id=returnsPrev; type=button |
| 226 | Düğme | Sonraki → | id=returnsNext; type=button |
| 227 | Düğme | ↻ Yenile | id=activityRefresh |
| 228 | Düğme | ← Önceki | id=activityPrev |
| 229 | Düğme | Sonraki → | id=activityNext |
| 230 | Düğme | ↓ CSV dışa aktar | id=reservationExportCsv; type=button |
| 231 | Düğme | ↻ Yenile | id=reservationRefresh; type=button |
| 232 | Düğme | Rezervasyon iste | id=reservationSubmit; type=submit |
| 233 | Düğme | ← Önceki | id=reservationPrev; type=button |
| 234 | Düğme | Sonraki → | id=reservationNext; type=button |
| 235 | Düğme | Kapat | id=reservationActionClose; type=button |
| 236 | Düğme | Vazgeç | id=reservationActionCancel; type=button |
| 237 | Düğme | Kaydet | id=reservationActionSubmit; type=submit |
| 238 | Düğme | ↻ Yenile | id=auditRefresh |
| 239 | Düğme | ↻ Yenile | id=auditScheduleRefresh; type=button |
| 240 | Düğme | ← Önceki | id=auditSchedulePrev; type=button |
| 241 | Düğme | Sonraki → | id=auditScheduleNext; type=button |
| 242 | Düğme | CSV dışa aktar | id=auditExportCsv; type=button |
| 243 | Düğme | ← Önceki | id=auditPrev |
| 244 | Düğme | Sonraki → | id=auditNext |
| 245 | Düğme | Şirket verisini indir (JSON) | id=dataDownloadCompany; type=button |
| 246 | Düğme | Şirket verisi ve dosyaları (ZIP) | id=dataDownloadCompanyArchive; type=button |
| 247 | Düğme | Varlık listesini indir (CSV) | id=dataDownloadAssets; type=button |
| 248 | Düğme | Varlıklar | type=button; hedef=Varlıklar |
| 249 | Düğme | Çalışan kayıtları | type=button; hedef=Çalışan kayıtları |
| 250 | Düğme | Aksesuarlar | type=button; hedef=Aksesuarlar |
| 251 | Düğme | Sarf malzemeleri | type=button; hedef=Sarf malzemeleri |
| 252 | Düğme | Bileşenler | type=button; hedef=Bileşenler |
| 253 | Düğme | Yazılım lisansları | type=button; hedef=Yazılım lisansları |
| 254 | Düğme | JSON dosyasını kontrol et | id=dataValidateFile; type=button |
| 255 | Düğme | ↓ CSV dışa aktar | id=exportContractsButton; type=button |
| 256 | Düğme | ↻ Yenile | id=refreshContractsButton; type=button |
| 257 | Düğme | ＋ Sözleşme ekle | id=newContractButton; type=button |
| 258 | Düğme | ← Önceki | id=contractPrev; type=button |
| 259 | Düğme | Sonraki → | id=contractNext; type=button |
| 260 | Düğme | ↻ Yenile | id=tasksRefresh; type=button |
| 261 | Düğme | ＋ Görev oluştur | id=taskNewButton; type=button |
| 262 | Düğme | Kapat | id=taskEditorClose; type=button |
| 263 | Düğme | Görevi kaydet | id=taskSaveButton; type=submit |
| 264 | Düğme | Vazgeç | id=taskEditorCancel; type=button |
| 265 | Düğme | ← Önceki | id=taskPrev; type=button |
| 266 | Düğme | Sonraki → | id=taskNext; type=button |
| 267 | Düğme | ↻ Yenile | id=supportRefresh; type=button |
| 268 | Düğme | ＋ Yeni destek talebi | id=supportNewButton; type=button |
| 269 | Düğme | Kapat | id=supportEditorClose; type=button |
| 270 | Düğme | Talebi gönder | id=supportSaveButton; type=submit |
| 271 | Düğme | ← Önceki | id=supportPrev; type=button |
| 272 | Düğme | Sonraki → | id=supportNext; type=button |
| 273 | Düğme | Talebi kapat | id=supportCloseTicket; type=button |
| 274 | Düğme | Kapat | id=supportDetailClose; type=button |
| 275 | Düğme | Yanıt gönder | id=supportReplyButton; type=submit |
| 276 | Düğme | ↻ Güvenlik durumunu yenile | id=securityOverviewRefresh; type=button; başlangıçta gizli |
| 277 | Düğme | Kendi hesap güvenliğini aç | id=featureStatusAccountSecurityButton; type=button; başlangıçta gizli |
| 278 | Düğme | Departmanları aç | id=featureStatusDepartmentsButton; type=button; başlangıçta gizli |
| 279 | Düğme | Genel Bakış | type=button; hedef=Genel Bakış |
| 280 | Düğme | ↻ Oturumları yenile | id=securitySessionsRefresh; type=button |
| 281 | Düğme | Güvenlik politikasını kaydet | id=companySecurityPolicySave; type=submit |
| 282 | Düğme | ↻ Yenile | id=integrationsRefresh; type=button |
| 283 | Düğme | API anahtarı oluştur | id=createIntegrationToken; type=submit |
| 284 | Düğme | Kapat | id=closeIntegrationSecret; type=button |
| 285 | Düğme | Göster | id=toggleIntegrationSecret; type=button |
| 286 | Düğme | Anahtarı kopyala | id=copyIntegrationSecret; type=button |
| 287 | Düğme | Kapat | id=dismissIntegrationSecret; type=button |
| 288 | Düğme | ⇩ CSV indir | id=maintenanceExport; type=button |
| 289 | Düğme | ↻ Yenile | id=maintenanceRefresh; type=button |
| 290 | Düğme | Filtrele | id=applyMaintenanceFilters; type=button |
| 291 | Düğme | Temizle | id=clearMaintenanceFilters; type=button |
| 292 | Düğme | ← Önceki | id=maintenancePrev; type=button |
| 293 | Düğme | Sonraki → | id=maintenanceNext; type=button |
| 294 | Düğme | ⇩ CSV indir | id=calendarExport; type=button |
| 295 | Düğme | ↻ Yenile | id=calendarRefresh; type=button |
| 296 | Düğme | Takvimi göster | id=applyCalendarFilters; type=button |
| 297 | Düğme | Önceki dönem | id=calendarPeriodPrev; type=button |
| 298 | Düğme | Bugün | id=calendarPeriodToday; type=button |
| 299 | Düğme | Sonraki dönem | id=calendarPeriodNext; type=button |
| 300 | Düğme | CSV indir | id=reportExport; type=button |
| 301 | Düğme | Excel (.xlsx) | id=reportXlsxExport; type=button |
| 302 | Düğme | Yazdır / PDF | id=reportPrint; type=button |
| 303 | Düğme | Rapor takvimi | id=reportScheduleOpen; type=button |
| 304 | Düğme | ↻ Verileri yenile | id=reportRefresh; type=button |
| 305 | Düğme | Tarih aralığını uygula | id=applyReportDateFilter; type=button |
| 306 | Düğme | Temizle | id=clearReportDateFilter; type=button |
| 307 | Düğme | Alış kayıtlarını CSV indir | id=assetPurchaseReportExport; type=button |
| 308 | Düğme | Yazdır / PDF | id=assetPurchaseReportPrint; type=button |
| 309 | Düğme | ← Önceki | id=assetPurchaseReportPrev; type=button |
| 310 | Düğme | Sonraki → | id=assetPurchaseReportNext; type=button |
| 311 | Düğme | Bileşen CSV indir | id=componentReportExport; type=button |
| 312 | Düğme | Yazdır / PDF | id=componentReportPrint; type=button |
| 313 | Düğme | Denetimleri CSV indir | id=auditReportExport; type=button |
| 314 | Düğme | Yazdır / PDF | id=auditReportPrint; type=button |
| 315 | Düğme | Filtrele | id=auditReportApply; type=button |
| 316 | Düğme | ← Önceki | id=auditReportPrev; type=button |
| 317 | Düğme | Sonraki → | id=auditReportNext; type=button |
| 318 | Düğme | CSV indir | id=warrantyReportExport; type=button |
| 319 | Düğme | Yazdır / PDF | id=warrantyReportPrint; type=button |
| 320 | Düğme | ↻ Yenile | id=warrantyReportRefresh; type=button |
| 321 | Düğme | ← Önceki | id=warrantyReportPrev |
| 322 | Düğme | Sonraki → | id=warrantyReportNext |
| 323 | Düğme | ↻ Yenile | id=maintenanceReportRefresh; type=button |
| 324 | Düğme | Yazdır / PDF | id=maintenanceReportPrint; type=button |
| 325 | Düğme | Filtrele | id=applyMaintenanceReportFilter; type=button |
| 326 | Düğme | Temizle | id=clearMaintenanceReportFilter; type=button |
| 327 | Düğme | CSV indir | id=licenseReportExport; type=button |
| 328 | Düğme | Yazdır / PDF | id=licenseReportPrint; type=button |
| 329 | Düğme | ↻ Yenile | id=licenseReportRefresh; type=button |
| 330 | Düğme | ← Önceki | id=licenseReportPrev |
| 331 | Düğme | Sonraki → | id=licenseReportNext |
| 332 | Düğme | ↻ Yenile | id=pendingDeliveryRefresh; type=button |
| 333 | Düğme | CSV indir | id=pendingDeliveryExport; type=button |
| 334 | Düğme | Yazdır / PDF | id=pendingDeliveryPrint; type=button |
| 335 | Düğme | ← Önceki | id=pendingDeliveryPrev; type=button |
| 336 | Düğme | Sonraki → | id=pendingDeliveryNext; type=button |
| 337 | Düğme | ↻ Hesapla | id=depreciationReportRefresh; type=button |
| 338 | Düğme | CSV indir | id=depreciationReportExport; type=button |
| 339 | Düğme | Yazdır / PDF | id=depreciationReportPrint; type=button |
| 340 | Düğme | ← Önceki | id=depreciationReportPrev; type=button |
| 341 | Düğme | Sonraki → | id=depreciationReportNext; type=button |
| 342 | Düğme | Taslağı yükle | id=customReportLoadPreset; type=button |
| 343 | Düğme | Taslağı kaydet | id=customReportSavePreset; type=button |
| 344 | Düğme | Taslağı sil | id=customReportDeletePreset; type=button |
| 345 | Düğme | Özel raporu CSV indir | id=customReportExport; type=button |
| 346 | Düğme | Özel raporu Excel indir | id=customReportXlsxExport; type=button |
| 347 | Düğme | Yazdır / PDF | id=customReportPrint; type=button |
| 348 | Düğme | Aksesuar siparişleri | id=backAccessoryOrders; type=button |
| 349 | Düğme | CSV dışa aktar | id=exportStockOrders; type=button |
| 350 | Düğme | ↻ Yenile | id=stockOrdersRefresh; type=button |
| 351 | Düğme | ＋ Sipariş oluştur | id=newStockOrderButton; type=button |
| 352 | Düğme | Filtreleri temizle | id=clearStockOrderFilters; type=button |
| 353 | Düğme | ← Önceki | id=stockOrderPrev; type=button |
| 354 | Düğme | Sonraki → | id=stockOrderNext; type=button |
| 355 | Düğme | ↻ Yenile | id=purchaseOverviewRefresh; type=button |
| 356 | Düğme | CSV indir | id=purchaseOverviewExport; type=button |
| 357 | Düğme | Filtreleri temizle | id=purchaseOverviewClear; type=button |
| 358 | Düğme | ← Önceki | id=purchaseOverviewPrev; type=button |
| 359 | Düğme | Sonraki → | id=purchaseOverviewNext; type=button |
| 360 | Düğme | Yıllık bütçe | id=assetOrderBudgetButton; type=button |
| 361 | Düğme | ↻ Yenile | id=assetOrdersRefresh; type=button |
| 362 | Düğme | CSV indir | id=exportAssetOrdersButton; type=button |
| 363 | Düğme | Tedarikçiler | id=assetOrdersSupplierCatalog; type=button |
| 364 | Düğme | ＋ Sipariş oluştur | id=newAssetOrderButton; type=button |
| 365 | Düğme | Filtreleri temizle | id=clearAssetOrderFilters; type=button |
| 366 | Düğme | ← Önceki | id=assetOrderPrev; type=button |
| 367 | Düğme | Sonraki → | id=assetOrderNext; type=button |
| 368 | Düğme | Uygulama kullanıcıları | id=rolesManageMembersButton; type=button |
| 369 | Düğme | Özel rol oluştur | id=createCompanyRoleButton; type=button; başlangıçta gizli |
| 370 | Düğme | Kapat | id=closeCompanyRoleDialog; type=button |
| 371 | Düğme | Vazgeç | id=cancelCompanyRoleDialog; type=button |
| 372 | Düğme | Kaydet | id=saveCompanyRoleButton; type=submit |
| 373 | Düğme | Kapat | id=closeDeactivateCompanyRole; type=button |
| 374 | Düğme | Vazgeç | id=cancelDeactivateCompanyRole; type=button |
| 375 | Düğme | Devre dışı bırak | id=confirmDeactivateCompanyRole; type=button |
| 376 | Düğme | Kapat | id=licenseRevokeClose; type=button |
| 377 | Düğme | Vazgeç | id=licenseRevokeCancel; type=button |
| 378 | Düğme | Atamayı kaldır | id=licenseRevokeSubmit; type=submit |
| 379 | Düğme | Kapat | id=licenseImportProfileDeleteClose; type=button |
| 380 | Düğme | Vazgeç | id=licenseImportProfileDeleteCancel; type=button |
| 381 | Düğme | Profili sil | id=licenseImportProfileDeleteSubmit; type=submit |
| 382 | Düğme | Kapat | id=commandPaletteClose; type=button |
| 383 | Düğme | Kapat | id=keyboardShortcutsClose; type=button |
| 384 | Düğme | Kapat | type=button |
| 385 | Düğme | Vazgeç | type=button |
| 386 | Düğme | Kaydet | type=submit |
| 387 | Düğme | Kapat | id=closeContractDetail; type=button |
| 388 | Düğme | Kapat | id=closeContractDetailFooter; type=button |
| 389 | Düğme | Kapat | id=contractActionClose; type=button |
| 390 | Düğme | Vazgeç | id=contractActionCancel; type=button |
| 391 | Düğme | Onayla | id=contractActionSubmit; type=submit |
| 392 | Düğme | Kapat | id=appearanceClose; type=button |
| 393 | Düğme | Sistem ayarını izle | type=button |
| 394 | Düğme | Açık | type=button |
| 395 | Düğme | Koyu | type=button |
| 396 | Düğme | Vazgeç | id=appearanceCancel; type=button |
| 397 | Düğme | Tercihleri kaydet | id=appearanceSave; type=button |
| 398 | Düğme | Kapat | id=closeDialog |
| 399 | Düğme | Genel bakış | id=assetTabOverviewButton; type=button |
| 400 | Düğme | İlişkiler | id=assetTabRelationsButton; type=button |
| 401 | Düğme | Bakım kayıtları | id=assetTabMaintenanceButton; type=button |
| 402 | Düğme | Hareket geçmişi | id=assetTabHistoryButton; type=button |
| 403 | Düğme | Fotoğraf ve dosyalar | id=assetTabAttachmentsButton; type=button |
| 404 | Düğme | detailPurchaseOrder | id=detailPurchaseOrder; type=button |
| 405 | Düğme | Etiketi kopyala | id=copyAssetTag; type=button |
| 406 | Düğme | Etiket kartını yazdır | id=printAssetLabel; type=button |
| 407 | Düğme | Bağlantıyı kaydet | id=saveAssetParent; type=button |
| 408 | Düğme | Not ekle | type=submit |
| 409 | Düğme | Dosya ekle | id=uploadAssetAttachment; type=submit |
| 410 | Düğme | Bilgileri düzenle | id=editAssetButton; type=button |
| 411 | Düğme | Denetimi kaydet | id=auditAssetButton; type=button |
| 412 | Düğme | Denetim planla | id=scheduleAssetAuditButton; type=button |
| 413 | Düğme | Bakım başlat | id=addMaintenanceButton; type=button |
| 414 | Düğme | Konum transferi | id=transferAssetButton; type=button |
| 415 | Düğme | Çalışana zimmetle | id=assignAssetButton; type=button |
| 416 | Düğme | İade al | id=returnAssetButton; type=button |
| 417 | Düğme | Arşivden geri al | id=restoreAssetButton; type=button; başlangıçta gizli |
| 418 | Düğme | Çöp kutusuna taşı | id=trashAssetButton; type=button |
| 419 | Düğme | Kapat | type=button |
| 420 | Düğme | Vazgeç | type=button |
| 421 | Düğme | Denetimi kaydet | type=submit |
| 422 | Düğme | Kapat | type=button |
| 423 | Düğme | Vazgeç | type=button |
| 424 | Düğme | Planı kaydet | type=submit |
| 425 | Düğme | Kapat | type=button |
| 426 | Düğme | Vazgeç | type=button |
| 427 | Düğme | Bakımı başlat | id=maintenanceSubmit; type=submit |
| 428 | Düğme | Kapat | type=button |
| 429 | Düğme | Vazgeç | type=button |
| 430 | Düğme | Bakımı tamamla | type=submit |
| 431 | Düğme | Kapat | type=button |
| 432 | Düğme | Vazgeç | type=button |
| 433 | Düğme | Transferi kaydet | type=submit |
| 434 | Düğme | Kapat | type=button |
| 435 | Düğme | Vazgeç | type=button |
| 436 | Düğme | Değişiklikleri kaydet | type=submit |
| 437 | Düğme | Kapat | type=button |
| 438 | Düğme | Vazgeç | type=button |
| 439 | Düğme | Zimmeti kaydet | type=submit |
| 440 | Düğme | Kapat | type=button |
| 441 | Düğme | Vazgeç | type=button |
| 442 | Düğme | Çalışanı kaydet | type=submit |
| 443 | Düğme | Kapat | id=closeEmployeeImport; type=button |
| 444 | Düğme | Şablonu indir | id=downloadEmployeeImportTemplate; type=button |
| 445 | Düğme | Dosyayı doğrula | id=previewEmployeeImport; type=button |
| 446 | Düğme | Doğrulanmış kayıtları ekle | id=commitEmployeeImport; type=button |
| 447 | Düğme | Kapat | id=closeConsumableImport; type=button |
| 448 | Düğme | Şablonu indir | id=downloadConsumableImportTemplate; type=button |
| 449 | Düğme | Eşlemeyi kaydet | id=saveConsumableImportProfile; type=button |
| 450 | Düğme | Profili sil | id=deleteConsumableImportProfile; type=button |
| 451 | Düğme | Eşlemeyi kullan ve yeniden doğrula | id=applyConsumableImportMapping; type=button |
| 452 | Düğme | Dosyayı doğrula | id=previewConsumableImport; type=button |
| 453 | Düğme | Doğrulanmış kayıtları ekle | id=commitConsumableImport; type=button |
| 454 | Düğme | Kapat | id=closeAccessoryImport; type=button |
| 455 | Düğme | Şablonu indir | id=downloadAccessoryImportTemplate; type=button |
| 456 | Düğme | Eşlemeyi kaydet | id=saveAccessoryImportProfile; type=button |
| 457 | Düğme | Profili sil | id=deleteAccessoryImportProfile; type=button |
| 458 | Düğme | Eşlemeyi kullan ve yeniden doğrula | id=applyAccessoryImportMapping; type=button |
| 459 | Düğme | Dosyayı doğrula | id=previewAccessoryImport; type=button |
| 460 | Düğme | Doğrulanmış kayıtları ekle | id=commitAccessoryImport; type=button |
| 461 | Düğme | Kapat | type=button |
| 462 | Düğme | Vazgeç | type=button |
| 463 | Düğme | Değişiklikleri kaydet | type=submit |
| 464 | Düğme | Kapat | type=button |
| 465 | Düğme | Vazgeç | type=button |
| 466 | Düğme | Onayla | id=employeeActionSubmit; type=submit |
| 467 | Düğme | Kapat | type=button |
| 468 | Düğme | Vazgeç | type=button |
| 469 | Düğme | Kaydet | id=textEntrySubmit; type=submit |
| 470 | Düğme | İmzalı zimmet belgeleri | id=printEmployeeHandoff; type=button |
| 471 | Düğme | Kapat | id=closeEmployeeDetails; type=button |
| 472 | Düğme | Kaydet | id=saveEmployeeManager; type=button |
| 473 | Düğme | Kaldır | id=clearEmployeeManager; type=button |
| 474 | Düğme | Kapat | id=closeInviteMember; type=button |
| 475 | Düğme | Vazgeç | id=cancelInviteMember; type=button |
| 476 | Düğme | Daveti gönder | type=submit |
| 477 | Düğme | Kapat | id=closeMemberLocationAccess; type=button |
| 478 | Düğme | Vazgeç | id=cancelMemberLocationAccess; type=button |
| 479 | Düğme | Erişimi kaydet | type=submit |
| 480 | Düğme | Kapat | type=button |
| 481 | Düğme | ＋ Ekle | type=button |
| 482 | Düğme | ＋ Ekle | type=button |
| 483 | Düğme | ＋ Ekle | type=button |
| 484 | Düğme | Vazgeç | type=button |
| 485 | Düğme | Kaydı oluştur | type=submit |
| 486 | Düğme | Sunucu bağlantısını test et | id=retryApiButton; type=button |
| 487 | Düğme | Giriş yap | type=submit |
| 488 | Düğme | Parolamı unuttum | id=forgotPasswordButton; type=button |
| 489 | Düğme | Kapat | id=closeDashboardCustomize; type=button |
| 490 | Düğme | Özet kartlarını yukarı taşı | type=button |
| 491 | Düğme | Özet kartlarını aşağı taşı | type=button |
| 492 | Düğme | Varlık listesini yukarı taşı | type=button |
| 493 | Düğme | Varlık listesini aşağı taşı | type=button |
| 494 | Düğme | Son hareketleri yukarı taşı | type=button |
| 495 | Düğme | Son hareketleri aşağı taşı | type=button |
| 496 | Düğme | Yaklaşan işlemleri yukarı taşı | type=button |
| 497 | Düğme | Yaklaşan işlemleri aşağı taşı | type=button |
| 498 | Düğme | Varlık durumu grafiğini yukarı taşı | type=button |
| 499 | Düğme | Varlık durumu grafiğini aşağı taşı | type=button |
| 500 | Düğme | + Grafik ekle | id=addDashboardChart; type=button |
| 501 | Düğme | Tümünü göster | id=resetDashboardCustomize; type=button |
| 502 | Düğme | İptal | id=cancelDashboardCustomize; type=button |
| 503 | Düğme | Tercihleri kaydet | type=submit |
| 504 | Düğme | Kapat | id=closeForgotPassword; type=button |
| 505 | Düğme | İptal | type=button |
| 506 | Düğme | Bağlantı iste | type=submit |
| 507 | Düğme | Kapat | id=closeImportPreview; type=button |
| 508 | Düğme | CSV şablonunu indir | id=downloadAssetCsvTemplate; type=button |
| 509 | Düğme | Eşlemeyi kaydet | id=saveAssetImportProfile; type=button |
| 510 | Düğme | Profili sil | id=deleteAssetImportProfile; type=button |
| 511 | Düğme | Eşlemeyi kullan ve yeniden doğrula | id=applyAssetImportMapping; type=button |
| 512 | Düğme | Kapat | id=cancelImportPreview; type=button |
| 513 | Düğme | Dosyayı doğrula | id=runImportPreview; type=submit |
| 514 | Düğme | Doğrula ve içe aktar | id=applyCsvImport; type=button; başlangıçta gizli |
| 515 | Düğme | Şirkete geri yükle | id=applyCompanyImport; type=button; başlangıçta gizli |
| 516 | Düğme | Kapat | id=closeAccessoryDetail; type=button |
| 517 | Düğme | Tüm siparişleri aç | id=accessoryOrdersFromDetail; type=button |
| 518 | Düğme | Kapat | id=accessoryCheckinClose; type=button |
| 519 | Düğme | Vazgeç | id=accessoryCheckinCancel; type=button |
| 520 | Düğme | İadeyi kaydet | id=accessoryCheckinSubmit; type=submit |
| 521 | Düğme | Kapat | type=button |
| 522 | Düğme | Bütçe istisnasını reddet | id=rejectAccessoryOrderApproval; type=button |
| 523 | Düğme | Bütçe istisnasını onayla | id=approveAccessoryOrderApproval; type=button |
| 524 | Düğme | Dosya yükle | type=submit |
| 525 | Düğme | Tedarikçiye iade et | id=returnAccessoryOrderFromDetail; type=button |
| 526 | Düğme | Teslim al | id=receiveAccessoryOrderFromDetail; type=button |
| 527 | Düğme | Kapat | type=button |
| 528 | Düğme | Kapat | type=button |
| 529 | Düğme | ＋ Kalem ekle | id=addAccessoryOrderLine; type=button |
| 530 | Düğme | Vazgeç | type=button |
| 531 | Düğme | Siparişi kaydet | type=submit |
| 532 | Düğme | Kapat | type=button |
| 533 | Düğme | ＋ Kalem ekle | id=addStockOrderLine; type=button |
| 534 | Düğme | Vazgeç | type=button |
| 535 | Düğme | Siparişi kaydet | type=submit |
| 536 | Düğme | Kapat | type=button |
| 537 | Düğme | Bütçe istisnasını reddet | id=rejectStockOrderApproval; type=button |
| 538 | Düğme | Bütçe istisnasını onayla | id=approveStockOrderApproval; type=button |
| 539 | Düğme | Belge yükle | type=submit |
| 540 | Düğme | Tedarikçiye iade et | id=returnStockOrderFromDetail; type=button |
| 541 | Düğme | Bu siparişten kit reçetesi oluştur | id=createKitFromStockOrder; type=button; başlangıçta gizli |
| 542 | Düğme | Teslim al | id=receiveStockOrderFromDetail; type=button |
| 543 | Düğme | Kapat | type=button |
| 544 | Düğme | Kapat | type=button |
| 545 | Düğme | Vazgeç | type=button |
| 546 | Düğme | Teslim al ve stoğa ekle | type=submit |
| 547 | Düğme | Kapat | type=button |
| 548 | Düğme | Vazgeç | type=button |
| 549 | Düğme | Kit reçetesini kaydet | type=submit |
| 550 | Düğme | Kapat | type=button |
| 551 | Düğme | Kapat | type=button |
| 552 | Düğme | Bütçeyi kaydet | type=submit |
| 553 | Düğme | Kapat | type=button |
| 554 | Düğme | ＋ Cihaz kalemi ekle | id=addAssetOrderLine; type=button |
| 555 | Düğme | Vazgeç | type=button |
| 556 | Düğme | Siparişi kaydet | type=submit |
| 557 | Düğme | Kapat | type=button |
| 558 | Düğme | Bütçe istisnasını reddet | id=rejectAssetOrderApproval; type=button |
| 559 | Düğme | Bütçe istisnasını onayla | id=approveAssetOrderApproval; type=button |
| 560 | Düğme | Belgeleri yükle | id=uploadAssetOrderAttachments; type=button |
| 561 | Düğme | Tedarikçiye iade et | id=returnAssetOrderFromDetail; type=button |
| 562 | Düğme | Teslim al | id=receiveAssetOrderFromDetail; type=button |
| 563 | Düğme | Kapat | type=button |
| 564 | Düğme | Kapat | type=button |
| 565 | Düğme | Vazgeç | type=button |
| 566 | Düğme | Teslim al ve varlıkları oluştur | type=submit |
| 567 | Düğme | Kapat | type=button |
| 568 | Düğme | Vazgeç | type=button |
| 569 | Düğme | Stoğa al | type=submit |
| 570 | Düğme | Kapat | id=closeConsumableDetail; type=button |
| 571 | Düğme | Kapat | type=button |
| 572 | Düğme | Vazgeç | type=button |
| 573 | Düğme | Kaydet | type=submit |
| 574 | Düğme | Kapat | type=button |
| 575 | Düğme | Vazgeç | type=button |
| 576 | Düğme | Stok hareketini kaydet | type=submit |
| 577 | Düğme | Kapat | type=button |
| 578 | Düğme | Vazgeç | type=button |
| 579 | Düğme | Tüketim çıkışını kaydet | type=submit |
| 580 | Düğme | Kapat | id=closeLicenseDetail; type=button |
| 581 | Düğme | Dosya yükle | type=submit |
| 582 | Düğme | Kapat | type=button |
| 583 | Düğme | Vazgeç | type=button |
| 584 | Düğme | Kaydet | type=submit |
| 585 | Düğme | Kapat | type=button |
| 586 | Düğme | Vazgeç | type=button |
| 587 | Düğme | Kaydet | type=submit |
| 588 | Düğme | Kapat | id=closeLicenseImport; type=button |
| 589 | Düğme | Şablonu indir | id=downloadLicenseImportTemplate; type=button |
| 590 | Düğme | Eşlemeyi kaydet | id=saveLicenseImportProfile; type=button |
| 591 | Düğme | Profili sil | id=deleteLicenseImportProfile; type=button |
| 592 | Düğme | Eşlemeyi kullan ve yeniden doğrula | id=applyLicenseImportMapping; type=button |
| 593 | Düğme | Dosyayı doğrula | id=previewLicenseImport; type=button |
| 594 | Düğme | Doğrulanmış kayıtları ekle | id=commitLicenseImport; type=button |
| 595 | Düğme | Kapat | id=licenseActionClose; type=button |
| 596 | Düğme | Vazgeç | id=licenseActionCancel; type=button |
| 597 | Düğme | Onayla | id=licenseActionSubmit; type=submit |
| 598 | Düğme | Kapat | type=button |
| 599 | Düğme | Vazgeç | type=button |
| 600 | Düğme | Atamayı kaydet | type=submit |
| 601 | Düğme | Kapat | type=button |
| 602 | Düğme | Vazgeç | type=button |
| 603 | Düğme | Devam et | id=componentActionSubmit; type=submit |
| 604 | Düğme | Kapat | id=closeComponentDetail; type=button |
| 605 | Düğme | Kapat | id=closeComponentImport; type=button |
| 606 | Düğme | Şablonu indir | id=downloadComponentImportTemplate; type=button |
| 607 | Düğme | Eşlemeyi kaydet | id=saveComponentImportProfile; type=button |
| 608 | Düğme | Profili sil | id=deleteComponentImportProfile; type=button |
| 609 | Düğme | Eşlemeyi kullan ve yeniden doğrula | id=applyComponentImportMapping; type=button |
| 610 | Düğme | Dosyayı doğrula | id=previewComponentImport; type=button |
| 611 | Düğme | Doğrulanmış kayıtları ekle | id=commitComponentImport; type=button |
| 612 | Düğme | Kapat | type=button |
| 613 | Düğme | Vazgeç | type=button |
| 614 | Düğme | Kaydet | type=submit |
| 615 | Düğme | ✕ | type=button |
| 616 | Düğme | Vazgeç | type=button |
| 617 | Düğme | Kaydet | type=submit |
| 618 | Düğme | ✕ | type=button |
| 619 | Düğme | Vazgeç | type=button |
| 620 | Düğme | Atamayı kaydet | type=submit |
| 621 | Düğme | Kapat | type=button |
| 622 | Düğme | Vazgeç | type=button |
| 623 | Düğme | Çıkarmayı kaydet | type=submit |
| 624 | Düğme | Kapat | type=button |
| 625 | Düğme | ＋ Stok kalemi ekle | id=addAccessoryKitLine; type=button |
| 626 | Düğme | Vazgeç | type=button |
| 627 | Düğme | Kiti kaydet | type=submit |
| 628 | Düğme | Kapat | id=closeAccessoryKitHistory; type=button |
| 629 | Düğme | Kapat | id=closeAccessoryKitHistoryFooter; type=button |
| 630 | Düğme | Kapat | type=button |
| 631 | Düğme | Vazgeç | type=button |
| 632 | Düğme | Arşivle | id=accessoryKitActionSubmit; type=submit |
| 633 | Düğme | Kapat | type=button |
| 634 | Düğme | ＋ Kategori oluştur | id=accessoryQuickCategory; type=button |
| 635 | Düğme | ＋ Marka oluştur | id=accessoryQuickBrand; type=button |
| 636 | Düğme | ＋ Model oluştur | id=accessoryQuickModel; type=button |
| 637 | Düğme | Vazgeç | type=button |
| 638 | Düğme | Kaydet | type=submit |
| 639 | Düğme | Kapat | id=closeAccessoryCategoryManager; type=button |
| 640 | Düğme | Kapat | id=cancelAccessoryCategoryManager; type=button |
| 641 | Düğme | ＋ Kategori ekle | id=addAccessoryCategory; type=button |
| 642 | Düğme | Kapat | type=button |
| 643 | Düğme | Vazgeç | type=button |
| 644 | Düğme | Stok hareketini kaydet | type=submit |
| 645 | Düğme | Kapat | type=button |
| 646 | Düğme | Vazgeç | type=button |
| 647 | Düğme | Teslimi kaydet | type=submit |
| 648 | Düğme | Kapat | id=closeAssetScanner; type=button |
| 649 | Düğme | Kamerayı başlat | id=startAssetCamera; type=button |
| 650 | Düğme | Varlığı bul | type=submit |
| 651 | Düğme | Kapat | id=closeDirectoryRename; type=button |
| 652 | Düğme | Vazgeç | id=cancelDirectoryRename; type=button |
| 653 | Düğme | Değişiklikleri kaydet | id=saveDirectoryRename; type=submit |
| 654 | Düğme | Kapat | id=closeCreateDepartment; type=button |
| 655 | Düğme | Vazgeç | id=cancelCreateDepartment; type=button |
| 656 | Düğme | Departmanı kaydet | id=saveDepartment; type=submit |
| 657 | Düğme | Kapat | id=closeDepartmentMove; type=button |
| 658 | Düğme | Kapat | id=cancelDepartmentMove; type=button |
| 659 | Düğme | Sonraki 100 çalışanı taşı | id=moveDepartmentEmployees; type=submit |
| 660 | Düğme | Kapat | id=closeCreateLocation; type=button |
| 661 | Düğme | Vazgeç | id=cancelCreateLocation; type=button |
| 662 | Düğme | Konumu kaydet | id=saveLocation; type=submit |
| 663 | Düğme | Kapat | id=closeLocationManager; type=button |
| 664 | Düğme | Vazgeç | id=cancelLocationManager; type=button |
| 665 | Düğme | Sorumluyu kaydet | id=saveLocationManager; type=submit |
| 666 | Düğme | Kapat | id=closeDepartmentManager; type=button |
| 667 | Düğme | Vazgeç | id=cancelDepartmentManager; type=button |
| 668 | Düğme | Yetkiliyi kaydet | id=saveDepartmentManager; type=submit |
| 669 | Düğme | Kapat | id=cancelQuickLookup; type=button |
| 670 | Düğme | Vazgeç | id=cancelQuickLookupAction; type=button |
| 671 | Düğme | Kaydet | type=submit |
| 672 | Düğme | Kapat | id=cancelAssetReturn; type=button |
| 673 | Düğme | Vazgeç | id=cancelAssetReturnAction; type=button |
| 674 | Düğme | İade al | id=submitAssetReturn; type=submit |
| 675 | Düğme | Kapat | id=closeAssetTrashConfirm; type=button |
| 676 | Düğme | Vazgeç | id=cancelAssetTrashConfirm; type=button |
| 677 | Düğme | Varlığı çöp kutusuna taşı | id=submitAssetTrashConfirm; type=submit |
| 678 | Düğme | Kapat | id=closeAssetStatusDialog; type=button |
| 679 | Düğme | Vazgeç | id=cancelAssetStatus; type=button |
| 680 | Düğme | Durumu güncelle | id=submitAssetStatus; type=submit |
| 681 | Düğme | Kapat | id=closeAssetAttachmentRemove; type=button |
| 682 | Düğme | Vazgeç | id=cancelAssetAttachmentRemove; type=button |
| 683 | Düğme | Dosyayı kaldır | id=submitAssetAttachmentRemove; type=submit |
| 684 | Düğme | Kapat | id=closeSavedAssetSearchDialog; type=button |
| 685 | Düğme | Vazgeç | id=cancelSavedAssetSearch; type=button |
| 686 | Düğme | Aramayı kaydet | id=submitSavedAssetSearch; type=submit |
| 687 | Düğme | Kapat | id=closeCompanyMfaEnrollment; type=button |
| 688 | Düğme | Doğrula ve devam et | id=verifyCompanyMfaEnrollment; type=submit |
| 689 | Düğme | Kodları kopyala | id=copyCompanyMfaRecoveryCodes; type=button |
| 690 | Düğme | Kodları kaydettim ve giriş yap | id=finishCompanyMfaEnrollment; type=button |
| 691 | Düğme | Kapat | id=closeSupplierCatalog; type=button |
| 692 | Düğme | Temizle | id=resetSupplierForm; type=button |
| 693 | Düğme | Tedarikçiyi kaydet | type=submit |
| 694 | Düğme | Kapat | id=closeCompanySwitch; type=button |
| 695 | Düğme | Vazgeç | id=cancelCompanySwitch; type=button |
| 696 | Düğme | Çıkış yap ve şirket seç | id=confirmCompanySwitch; type=button |
| 697 | Düğme | Kapat | id=closeAccountPhoto; type=button |
| 698 | Düğme | Fotoğraf seç | id=chooseAccountPhoto; type=button |
| 699 | Düğme | Fotoğrafı kaldır | id=removeAccountPhoto; type=button; başlangıçta gizli |
| 700 | Düğme | Parolayı değiştir | id=changeOwnPassword; type=button |
| 701 | Düğme | Güvenlik ayarları | id=openAccountTwoFactor; type=button |
| 702 | Düğme | Yenile | id=refreshAccountSessions; type=button |
| 703 | Düğme | Kapat | id=closeAccountTwoFactor; type=button |
| 704 | Düğme | Authenticator kurulumu başlat | id=startTwoFactorSetup; type=button |
| 705 | Düğme | Doğrula ve etkinleştir | id=enableTwoFactor; type=button; başlangıçta gizli |
| 706 | Düğme | Kurtarma kodlarını yenile | id=rotateTwoFactorCodes; type=button; başlangıçta gizli |
| 707 | Düğme | İki aşamalı doğrulamayı kapat | id=disableTwoFactor; type=button; başlangıçta gizli |
| 708 | Düğme | Kodları kopyala | id=copyTwoFactorRecoveryCodes; type=button |
| 709 | Düğme | Kapat | type=button |
| 710 | Düğme | Vazgeç | type=button |
| 711 | Düğme | Talebi gönder | type=submit |
| 712 | Düğme | Kapat | id=closeAssetTrash; type=button |
| 713 | Düğme | ← Önceki | id=assetTrashPrev; type=button |
| 714 | Düğme | Sonraki → | id=assetTrashNext; type=button |
| 715 | Düğme | Kapat | id=closeImagePreview; type=button |
| 716 | Düğme | Kapat | id=closeModelDetails; type=button |
| 717 | Düğme | Kapat | id=closeModelAttachments; type=button |
| 718 | Düğme | Teknik dosya ekle | id=uploadModelAttachment; type=submit |
| 719 | Düğme | Yazdır | id=assetLabelPreviewPrint; type=button |
| 720 | Düğme | Kapat | id=assetLabelPreviewClose; type=button |
| 721 | Düğme | Kapat | type=button |
| 722 | Düğme | Vazgeç | type=button |
| 723 | Düğme | İadeyi kaydet | id=submitPurchaseOrderReturn; type=submit |
| 724 | Düğme | ${escapeHtml(translateUiText('Aç'))} | type=button |
| 725 | Düğme | Kapat | type=button |
| 726 | Düğme | Vazgeç | type=button |
| 727 | Düğme | Seçilenleri güncelle | type=submit |
| 728 | Düğme | ${escapeHtml(text)} ile eşleşen kayıtları filtrele | type=button |
| 729 | Düğme | ${translateUiText(item.isFullySigned ? 'Görüntüle / yazdır' : 'Devam et')} | id=${escapeHtml(item.id)}; type=button |
| 730 | Düğme | ${escapeHtml(translateUiText('Yenile'))} | id=refreshAssetSoftwareInventory; type=button |
| 731 | Düğme | ${escapeHtml(c.name)} | type=button |
| 732 | Düğme | Ayrıntı | type=button |
| 733 | Düğme | Düzenle | type=button |
| 734 | Düğme | Stok | type=button |
| 735 | Düğme | Ata | type=button |
| 736 | Düğme | Düzenle | id=editComponentButton; başlangıçta gizli |
| 737 | Düğme | Stok ekle / düzelt | id=componentStockButton; başlangıçta gizli |
| 738 | Düğme | c.allocated?'':'hidden'}>Varlığa tak | id=componentAllocateButton; başlangıçta gizli |
| 739 | Düğme | Çıkar | id=${a.id}; type=button |
| 740 | Düğme | Kapat | type=button |
| 741 | Düğme | Vazgeç | type=button |
| 742 | Düğme | Bileşenleri varlığa tak | type=submit |
| 743 | Düğme | Kapat | type=button |
| 744 | Düğme | Vazgeç | type=button |
| 745 | Düğme | Stok farklarını kaydet | type=submit |
| 746 | Düğme | ${escapeHtml(l.name)} | type=button |
| 747 | Düğme | ${translateUiText('Ayrıntı')} | type=button |
| 748 | Düğme | ${translateUiText('Düzenle')} | id=${order.id}; type=button |
| 749 | Düğme | ${translateUiText(order.isArchived?'Geri yükle':'Arşivle')} | id=${order.id}; type=button; action=${order.isArchived?'restore':'archive'} |
| 750 | Düğme | ${escapeHtml(String(b\|\|translateUiText('Tedarikçi ayrıntısını aç')))} | id=licenseSupplierLink; type=button |
| 751 | Düğme | ${translateUiText('Düzenle')} | id=editLicenseButton; başlangıçta gizli |
| 752 | Düğme | ${translateUiText('Koltuk ata')} | id=assignLicenseButton; başlangıçta gizli |
| 753 | Düğme | ${translateUiText('Sipariş kaydı ekle')} | id=newLicensePurchaseOrderButton; type=button; başlangıçta gizli |
| 754 | Düğme | ${translateUiText(l.isArchived?'Arşivden geri yükle':'Arşivle')} | type=button; başlangıçta gizli |
| 755 | Düğme | ${escapeHtml(`${a.assetTag\|\|''}${a.assetName?` · ${a.assetName}`:''}`)} | type=button |
| 756 | Düğme | ${escapeHtml(a.employee\|\|'—')} | type=button |
| 757 | Düğme | ${translateUiText('Atamayı kaldır')} | id=${a.id}; type=button |
| 758 | Düğme | ${translateUiText('Sil')} | id=${a.id}; type=button |
| 759 | Düğme | ${escapeHtml(a.license)} | id=${a.licenseId}; type=button |
| 760 | Düğme | ${escapeHtml(`${a.assetTag\|\|''}${a.assetName?` · ${a.assetName}`:''}`)} | type=button |
| 761 | Düğme | ${escapeHtml(a.employee\|\|'—')} | type=button |
| 762 | Düğme | ${translateUiText('Atamayı kaldır')} | id=${a.licenseId}; type=button |
| 763 | Düğme | ${escapeHtml(item.name)} | type=button |
| 764 | Düğme | ${translateUiText('Ayrıntı')} | type=button |
| 765 | Düğme | ${translateUiText('Düzenle')} | type=button |
| 766 | Düğme | ${translateUiText(item.status === 'archived' ? 'Geri yükle' : 'Arşivle')} | type=button |
| 767 | Düğme | ${translateUiText('Sil')} | id=${file.id}; type=button |
| 768 | Düğme | ${escapeHtml(item.name)} | type=button |
| 769 | Düğme | Ayrıntı | type=button |
| 770 | Düğme | Düzenle | type=button |
| 771 | Düğme | Stok | type=button |
| 772 | Düğme | Çıkış | type=button |
| 773 | Düğme | Düzenle | id=editConsumableButton; başlangıçta gizli |
| 774 | Düğme | Stok ekle / düzelt | id=consumableStockButton; başlangıçta gizli |
| 775 | Düğme | item.issued) ? '' : 'hidden'}>Çalışana tüketim çıkışı | id=consumableCheckoutButton; başlangıçta gizli |
| 776 | Düğme | ${escapeHtml(item.name)} | type=button |
| 777 | Düğme | ${escapeHtml(item.category \|\| translateUiText('Kategori yok'))} | id=${escapeHtml(item.categoryId \|\| '')}; type=button |
| 778 | Düğme | ${translateUiText('Ayrıntı')} | type=button |
| 779 | Düğme | ${translateUiText('Düzenle')} | type=button |
| 780 | Düğme | ${translateUiText('Teslim')} | type=button |
| 781 | Düğme | ${translateUiText(accessoryOnly ? 'Teslim et' : 'Talep oluştur')} | id=${kit.id}; type=button |
| 782 | Düğme | Geçmiş | id=${kit.id}; type=button |
| 783 | Düğme | Düzenle | id=${kit.id}; type=button |
| 784 | Düğme | Arşivle | id=${kit.id}; type=button |
| 785 | Düğme | Geri al | id=${kit.id}; type=button |
| 786 | Düğme | ${translateUiText('Düzenle')} | id=editAccessoryButton; type=button; başlangıçta gizli |
| 787 | Düğme | ${translateUiText('Stok ekle / düzelt')} | id=accessoryStockButton; type=button; başlangıçta gizli |
| 788 | Düğme | currentAccessory.checkedOut ? '' : 'hidden'}>${translateUiText('Çalışana teslim et')} | id=accessoryCheckoutButton; type=button; başlangıçta gizli |
| 789 | Düğme | ${translateUiText('Yeni sipariş')} | id=createAccessoryOrderFromDetail; type=button; başlangıçta gizli |
| 790 | Düğme | ${escapeHtml(order.orderNumber)} | id=${order.purchaseOrderId}; type=button |
| 791 | Düğme | İade al | id=${assignment.id}; type=button |
| 792 | Düğme | Düzenle | type=button |
| 793 | Düğme | ${translateUiText(category.isArchived ? 'Geri yükle' : 'Arşivle')} | type=button |
| 794 | Düğme | Kapat | type=button |
| 795 | Düğme | Vazgeç | type=button |
| 796 | Düğme | Stok farklarını kaydet | type=submit |
| 797 | Düğme | ${escapeHtml(order.orderNumber)} | id=${order.id}; type=button |
| 798 | Düğme | Ayrıntı | id=${order.id}; type=button |
| 799 | Düğme | Teslim al | id=${order.id}; type=button |
| 800 | Düğme | İptal | id=${order.id}; type=button |
| 801 | Düğme | ${escapeHtml(order.orderNumber)} | id=${order.id}; type=button |
| 802 | Düğme | Ayrıntı | id=${order.id}; type=button |
| 803 | Düğme | Teslim al | id=${order.id}; type=button |
| 804 | Düğme | İptal | id=${order.id}; type=button |
| 805 | Düğme | Kalemi kaldır | type=button |
| 806 | Düğme | ${escapeHtml(order.orderNumber)} | id=${order.id}; type=button |
| 807 | Düğme | Ayrıntı | id=${order.id}; type=button |
| 808 | Düğme | Teslim al | id=${order.id}; type=button |
| 809 | Düğme | İptal | id=${order.id}; type=button |
| 810 | Düğme | Kaldır | type=button |
| 811 | Düğme | ${escapeHtml(asset.assetTag)} | id=${asset.id}; type=button |
| 812 | Düğme | Sil | id=${item.id}; type=button |
| 813 | Düğme | ${escapeHtml(order.orderNumber)} | id=${escapeHtml(order.id)}; type=button |
| 814 | Düğme | ${translateUiText('Ayrıntı')} | id=${escapeHtml(order.id)}; type=${escapeHtml(order.type)} |
| 815 | Düğme | Düzenle | id=${supplier.id}; type=button |
| 816 | Düğme | Arşivle | id=${supplier.id}; type=button |
| 817 | Düğme | Geri al | id=${supplier.id}; type=button |
| 818 | Düğme | Kalemi kaldır | type=button |
| 819 | Düğme | Kaldır | id=${item.id}; type=button |
| 820 | Düğme | ${translateUiText('Ayrıntılar')} | type=button |
| 821 | Düğme | ${translateUiText('Teknik dosyalar')} (${fmt.format(item.attachmentCount \|\| 0)}) | type=button |
| 822 | Düğme | ${translateUiText('Görseli yükle')} | type=button |
| 823 | Düğme | ${translateUiText('Amortismanı kaydet')} | type=button |
| 824 | Düğme | ${translateUiText('Üretici bilgilerini kaydet')} | type=button |
| 825 | Düğme | ${translateUiText('Logoyu yükle')} | type=button |
| 826 | Düğme | ${translateUiText('Kaydet')} |  |
| 827 | Düğme | ${translateUiText(deleteReason)} |  |
| 828 | Düğme | ${translateUiText('Sil')} | type=button |
| 829 | Düğme | ${translateUiText('Teknik dosyalar')} | type=button |
| 830 | Düğme | ${escapeHtml(asset.assetTag)} | type=button |
| 831 | Düğme | Filtrele | id=applyAssetCustomFieldFilter; type=button |
| 832 | Düğme | Temizle | id=clearAssetCustomFieldFilter; type=button |
| 833 | Düğme | Özel alan ekle | type=submit |
| 834 | Düğme | Kapat | type=button |
| 835 | Düğme | Vazgeç | type=button |
| 836 | Düğme | Kaydet | type=submit |
| 837 | Düğme | Düzenle | id=${field.id}; type=button |
| 838 | Düğme | ${field.isActive ? 'Pasifleştir' : 'Etkinleştir'} | id=${field.id}; type=button |
| 839 | Düğme | Eşlemeyi kaydet | id=saveEmployeeImportProfile; type=button |
| 840 | Düğme | Profili sil | id=deleteEmployeeImportProfile; type=button |
| 841 | Düğme | Eşlemeyi kullan ve yeniden doğrula | id=applyEmployeeImportMapping; type=button |
| 842 | Düğme | Kapat | id=closeBulkAssetAction; type=button |
| 843 | Düğme | Vazgeç | id=cancelBulkAssetAction; type=button |
| 844 | Düğme | submitBulkAssetAction | id=submitBulkAssetAction; type=submit |
| 845 | Düğme | ${escapeHtml(translateUiText('Talep et'))} | id=${item.id}; type=button |
| 846 | Girdi alanı | employeeHandoverRecipientName | id=employeeHandoverRecipientName |
| 847 | Girdi alanı | file | type=file; name=file |
| 848 | Girdi alanı | Envanterde ara | id=globalSearch |
| 849 | Girdi alanı | Varlık listesini ara | id=assetListSearch |
| 850 | Seçim alanı | Sayfa başına kayıt | id=assetPageSize; seçenekler=25: 25 kayıt, 50: 50 kayıt, 100: 100 kayıt, 200: 200 kayıt |
| 851 | Seçim alanı | Duruma göre filtrele | id=assetStatusFilter; seçenekler=(değer yok): Tüm durumlar, Available: Kullanılabilir, Deployed: Atanmış, Pending: Beklemede, Maintenance: Bakımda, Unusable: Kullanılamaz, Archived: Arşiv |
| 852 | Seçim alanı | Kişisel cihaz filtresi | id=assetByodFilter; seçenekler=(değer yok): Şirket ve kişisel cihazlar, true: Kişisel cihazlar (BYOD), false: Şirket cihazları |
| 853 | Seçim alanı | Garanti durumuna göre filtrele | id=assetWarrantyFilter; seçenekler=(değer yok): Tüm garanti durumları, expired: Süresi dolmuş, expiring: 30 gün içinde dolacak, active: 30 günden uzun süre aktif, unspecified: Bitiş tarihi girilmemiş |
| 854 | Seçim alanı | Kategoriye göre filtrele | id=assetCategoryFilter; seçenekler=(değer yok): Tüm kategoriler |
| 855 | Seçim alanı | Markaya göre filtrele | id=assetBrandFilter; seçenekler=(değer yok): Tüm markalar |
| 856 | Seçim alanı | Modele göre filtrele | id=assetModelFilter; seçenekler=(değer yok): Tüm modeller |
| 857 | Seçim alanı | Kaydedilmiş varlık aramaları | id=savedAssetSearch; seçenekler=(değer yok): Kayıtlı aramalar |
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
| 874 | Girdi alanı | (etiketsiz) | type=checkbox |
| 875 | Girdi alanı | (etiketsiz) | type=checkbox |
| 876 | Seçim alanı | Tablo satır yoğunluğu | id=assetRowDensity; seçenekler=comfortable: Rahat satırlar, compact: Sık satırlar |
| 877 | Girdi alanı | Bu sayfadaki seçilebilir varlıkları seç | id=selectAllAssets; type=checkbox |
| 878 | Seçim alanı | Seçili varlıklara uygulanacak durum | id=bulkAssetStatus; seçenekler=(değer yok): Yeni durumu seçin, Available: Kullanılabilir, Pending: Beklemede, Maintenance: Bakımda, Unusable: Kullanılamaz, Archived: Arşiv |
| 879 | Girdi alanı | Bu sayfadaki seçilebilir varlıkları seç | id=selectAllAssetsHeader; type=checkbox |
| 880 | Girdi alanı | Çalışan kayıtlarında ara | id=employeeSearch |
| 881 | Girdi alanı | Departmana göre filtrele | id=employeeDepartmentFilter |
| 882 | Girdi alanı | Yönetici ara | id=employeeManagerFilterSearch |
| 883 | Seçim alanı | Yöneticiye göre filtrele | id=employeeManagerFilter; seçenekler=(değer yok): Tüm yöneticiler |
| 884 | Seçim alanı | Çalışan durumuna göre filtrele | id=employeeState; seçenekler=(değer yok): Tüm çalışanlar, true: Aktif çalışanlar, false: Pasif çalışanlar |
| 885 | Girdi alanı | İşe başlama başlangıç tarihi | id=employeeStartedFrom; type=date |
| 886 | Girdi alanı | İşe başlama bitiş tarihi | id=employeeStartedTo; type=date |
| 887 | Seçim alanı | Seçilen çalışanların yeni durumu | id=employeeBulkActive; başlangıçta gizli; seçenekler=false: Pasife al, true: Etkinleştir |
| 888 | Girdi alanı | Liste kayıtlarında ara | id=directorySearch |
| 889 | Girdi alanı | companyUnitsIncludeInactive | id=companyUnitsIncludeInactive; type=checkbox |
| 890 | Girdi alanı | Şirket birimlerinde ara | id=companyUnitSearch |
| 891 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 892 | Girdi alanı | name | name=name |
| 893 | Seçim alanı | unitType | name=unitType; seçenekler=organizational: Organizasyon birimi, legal_entity: Tüzel kişilik, branch: Şube |
| 894 | Girdi alanı | code | name=code |
| 895 | Seçim alanı | parentCompanyUnitId | name=parentCompanyUnitId; seçenekler=(değer yok): Üst birim yok |
| 896 | Girdi alanı | Aksesuarlarda ara | id=accessorySearch |
| 897 | Seçim alanı | Stok durumuna göre filtrele | id=accessoryStockFilter; seçenekler=(değer yok): Tüm stoklar, available: Kullanılabilir stok, low: Minimum seviyede veya altında |
| 898 | Seçim alanı | Aksesuarları sırala | id=accessorySort; seçenekler=name:asc: Ada göre A–Z, name:desc: Ada göre Z–A, remaining:asc: Kalan adede göre artan, remaining:desc: Kalan adede göre azalan, updated:desc: En son güncellenen |
| 899 | Seçim alanı | Sayfa başına aksesuar sayısı | id=accessoryPageSize; seçenekler=(değer yok): 12, (değer yok): 25, (değer yok): 50, (değer yok): 100 |
| 900 | Girdi alanı | (etiketsiz) | type=checkbox |
| 901 | Girdi alanı | (etiketsiz) | type=checkbox |
| 902 | Girdi alanı | (etiketsiz) | type=checkbox |
| 903 | Girdi alanı | (etiketsiz) | type=checkbox |
| 904 | Girdi alanı | (etiketsiz) | type=checkbox |
| 905 | Girdi alanı | (etiketsiz) | type=checkbox |
| 906 | Girdi alanı | (etiketsiz) | type=checkbox |
| 907 | Girdi alanı | (etiketsiz) | type=checkbox |
| 908 | Girdi alanı | (etiketsiz) | type=checkbox |
| 909 | Girdi alanı | (etiketsiz) | type=checkbox |
| 910 | Seçim alanı | Kategoriye göre filtrele | id=accessoryCategoryFilter; seçenekler=(değer yok): Tüm kategoriler |
| 911 | Seçim alanı | Konuma göre filtrele | id=accessoryLocationFilter; seçenekler=(değer yok): Tüm konumlar |
| 912 | Girdi alanı | Satın alma siparişlerinde ara | id=accessoryOrderSearch |
| 913 | Seçim alanı | Sipariş durumuna göre filtrele | id=accessoryOrderStatus; seçenekler=(değer yok): Tüm durumlar, Ordered: Sipariş edildi, PartiallyReceived: Kısmi teslim alındı, Received: Tamamlandı, Cancelled: İptal edildi |
| 914 | Seçim alanı | Teslimat takvimine göre filtrele | id=accessoryOrderDelivery; seçenekler=(değer yok): Tüm teslimatlar, overdue: Geciken teslimatlar |
| 915 | Girdi alanı | Sipariş başlangıç tarihi | id=accessoryOrderFrom; type=date |
| 916 | Girdi alanı | Sipariş bitiş tarihi | id=accessoryOrderTo; type=date |
| 917 | Seçim alanı | Sayfa başına sipariş sayısı | id=accessoryOrderPageSize; seçenekler=25: 25, 50: 50, 100: 100 |
| 918 | Girdi alanı | Hazır kitlerde ara | id=accessoryKitSearch |
| 919 | Girdi alanı | Sarf malzemelerinde ara | id=consumableSearch |
| 920 | Seçim alanı | Stok durumuna göre filtrele | id=consumableStockFilter; seçenekler=(değer yok): Tüm stoklar, available: Stokta olanlar, low: Minimum seviyede veya altında |
| 921 | Seçim alanı | name:asc: Ada göre A–Z, name:desc: Ada göre Z–A, remaining:asc: Kalan miktar artan, remaining:desc: Kalan miktar azalan, updated:desc: Son güncellenen | id=consumableSort; seçenekler=name:asc: Ada göre A–Z, name:desc: Ada göre Z–A, remaining:asc: Kalan miktar artan, remaining:desc: Kalan miktar azalan, updated:desc: Son güncellenen |
| 922 | Girdi alanı | Talep edilebilir stoklarda ara | id=requestCatalogSearch |
| 923 | Seçim alanı | Talep durumuna göre filtrele | id=requestStatusFilter; seçenekler=(değer yok): Tüm durumlar, Pending: Bekliyor, Approved: Onaylandı, Rejected: Reddedildi, Cancelled: İptal edildi |
| 924 | Girdi alanı | Taleplerde ara | id=requestSearch |
| 925 | Metin alanı | note | id=inventoryRequestActionNote; name=note |
| 926 | Girdi alanı | licenseSearch | id=licenseSearch |
| 927 | Seçim alanı | Lisans durumuna göre filtrele | id=licenseStatusFilter; seçenekler=active: Etkin lisanslar, archived: Arşivlenmiş lisanslar, all: Tüm lisanslar |
| 928 | Girdi alanı | licenseAssignmentSearch | id=licenseAssignmentSearch |
| 929 | Seçim alanı | true: Aktif atamalar, false: Geri alınan atamalar, (değer yok): Tüm atamalar | id=licenseAssignmentState; seçenekler=true: Aktif atamalar, false: Geri alınan atamalar, (değer yok): Tüm atamalar |
| 930 | Girdi alanı | kind | type=checkbox; name=kind |
| 931 | Girdi alanı | kind | type=checkbox; name=kind |
| 932 | Girdi alanı | kind | type=checkbox; name=kind |
| 933 | Girdi alanı | kind | type=checkbox; name=kind |
| 934 | Girdi alanı | kind | type=checkbox; name=kind |
| 935 | Girdi alanı | kind | type=checkbox; name=kind |
| 936 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 937 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 938 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 939 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 940 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 941 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 942 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 943 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 944 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 945 | Girdi alanı | componentSearch | id=componentSearch |
| 946 | Seçim alanı | (değer yok): Tüm stoklar, available: Kullanılabilir miktar, low: Minimum seviyede veya altında | id=componentStockFilter; seçenekler=(değer yok): Tüm stoklar, available: Kullanılabilir miktar, low: Minimum seviyede veya altında |
| 947 | Girdi alanı | Stok hareketlerinde ara | id=inventoryMovementSearch |
| 948 | Seçim alanı | Stok türüne göre filtrele | id=inventoryMovementKind; seçenekler=(değer yok): Tüm stok türleri, accessory: Aksesuar, consumable: Sarf malzemesi, component: Bileşen |
| 949 | Seçim alanı | Hareket türüne göre filtrele | id=inventoryMovementType; seçenekler=(değer yok): Tüm hareketler, Received: Stok girişi, Adjustment: Sayım düzeltmesi, Checkout: Çıkış / atama, Checkin: İade / çıkarma |
| 950 | Girdi alanı | Başlangıç tarihi | id=inventoryMovementFrom; type=date |
| 951 | Girdi alanı | Bitiş tarihi | id=inventoryMovementTo; type=date |
| 952 | Seçim alanı | (değer yok): Şablon seçin | id=labelStockTemplate; seçenekler=(değer yok): Şablon seçin |
| 953 | Girdi alanı | labelStockTemplateName | id=labelStockTemplateName |
| 954 | Girdi alanı | labelStockTemplateManufacturer | id=labelStockTemplateManufacturer |
| 955 | Girdi alanı | labelStockTemplatePrinter | id=labelStockTemplatePrinter |
| 956 | Girdi alanı | labelStockTemplateProduct | id=labelStockTemplateProduct |
| 957 | Seçim alanı | barcode1d | name=barcode1d; seçenekler=code128: C128 · alfasayısal, code39: C39 · büyük harf ve rakam, code93: C93 · kompakt alfasayısal, code93ext: C93 Extended · geniş karakter kümesi, rationalizedCodabar: Codabar · sayısal / A–D başlangıç-bitiş, msi: MSI Plessey · sayısal stok kodu, ean5: EAN5 · 5 rakam, ean13: EAN13 · 12/13 rakam, upca: UPC-A · 11/12 rakam, upce: UPC-E · 6–8 rakam, none: Kapalı |
| 958 | Seçim alanı | barcode2d | name=barcode2d; seçenekler=qrcode: QR Code, pdf417: PDF417, datamatrix: Data Matrix, azteccode: Aztec Code, none: Kapalı |
| 959 | Seçim alanı | sizePreset | name=sizePreset; seçenekler=86x48: 86 × 48 mm · varsayılan, 50x30: 50 × 30 mm, 60x40: 60 × 40 mm, 70x35: 70 × 35 mm, 100x50: 100 × 50 mm, custom: Özel ölçü |
| 960 | Girdi alanı | widthMm | type=number; name=widthMm |
| 961 | Girdi alanı | heightMm | type=number; name=heightMm |
| 962 | Girdi alanı | offsetXmm | type=number; name=offsetXmm |
| 963 | Girdi alanı | offsetYmm | type=number; name=offsetYmm |
| 964 | Girdi alanı | paddingMm | type=number; name=paddingMm |
| 965 | Girdi alanı | codeSizeMm | type=number; name=codeSizeMm |
| 966 | Seçim alanı | qrTarget | name=qrTarget; seçenekler=tag: Varlık etiketi · aramayla bulunur, asset-url: Giriş sonrası varlık ayrıntısını aç |
| 967 | Girdi alanı | showName | type=checkbox; name=showName |
| 968 | Girdi alanı | showSerial | type=checkbox; name=showSerial |
| 969 | Girdi alanı | showModel | type=checkbox; name=showModel |
| 970 | Girdi alanı | showLocation | type=checkbox; name=showLocation |
| 971 | Girdi alanı | showStatus | type=checkbox; name=showStatus |
| 972 | Seçim alanı | sheetLayout | name=sheetLayout; seçenekler=single: Her etiketi ayrı sayfaya bas, sheet: Etiket yaprağına otomatik yerleştir |
| 973 | Seçim alanı | sheetPaper | name=sheetPaper; seçenekler=a4: A4 · 210 × 297 mm, letter: Letter · 216 × 279 mm |
| 974 | Girdi alanı | sheetMarginMm | type=number; name=sheetMarginMm |
| 975 | Girdi alanı | sheetGapMm | type=number; name=sheetGapMm |
| 976 | Girdi alanı | name | name=name |
| 977 | Seçim alanı | brandId | name=brandId; seçenekler=(değer yok): Marka seçin (isteğe bağlı) |
| 978 | Seçim alanı | categoryId | name=categoryId; seçenekler=(değer yok): Kategori seçin |
| 979 | Girdi alanı | modelNumber | name=modelNumber |
| 980 | Girdi alanı | website | type=url; name=website |
| 981 | Girdi alanı | supportUrl | type=url; name=supportUrl |
| 982 | Girdi alanı | warrantyUrl | type=url; name=warrantyUrl |
| 983 | Girdi alanı | supportPhone | type=tel; name=supportPhone |
| 984 | Girdi alanı | supportEmail | type=email; name=supportEmail |
| 985 | Girdi alanı | brandLogo | type=file; name=brandLogo |
| 986 | Girdi alanı | modelPhoto | type=file; name=modelPhoto |
| 987 | Girdi alanı | depreciationMonths | type=number; name=depreciationMonths |
| 988 | Girdi alanı | depreciationMinimum | type=number; name=depreciationMinimum |
| 989 | Seçim alanı | depreciationMinimumType | name=depreciationMinimumType; seçenekler=amount: Tutar, percent: Yüzde |
| 990 | Seçim alanı | depreciationMethod | name=depreciationMethod; seçenekler=linear: Doğrusal, half_1: Yarım yıl · ilk dönem, half_2: Yarım yıl · satın alma dönemine göre |
| 991 | Girdi alanı | catalogSearch | id=catalogSearch |
| 992 | Girdi alanı | name | name=name |
| 993 | Girdi alanı | slug | name=slug |
| 994 | Girdi alanı | contactEmail | type=email; name=contactEmail |
| 995 | Girdi alanı | brandPrimaryColor | type=color; name=brandPrimaryColor |
| 996 | Girdi alanı | logo | type=file; name=logo |
| 997 | Girdi alanı | Zimmetlerde ara | id=assignmentSearch |
| 998 | Seçim alanı | İade tarihine göre filtrele | id=returnsState; seçenekler=overdue: Geciken iadeler, due30: Önümüzdeki 30 gün, undated: Tarihi belirtilmemiş, later: Daha ileri tarih, all: Tüm zimmetler |
| 999 | Girdi alanı | İadelerde ara | id=returnsSearch |
| 1000 | Girdi alanı | Hareket kayıtlarında ara | id=activitySearch |
| 1001 | Seçim alanı | (değer yok): Tüm işlem türleri, Created: Oluşturuldu, Purchased: Satın alma teslimatı, Assigned: Zimmetlendi, Returned: İade alındı, StatusChanged: Durum değişti, Transferred: Konum transferi | id=activityType; seçenekler=(değer yok): Tüm işlem türleri, Created: Oluşturuldu, Purchased: Satın alma teslimatı, Assigned: Zimmetlendi, Returned: İade alındı, StatusChanged: Durum değişti, Transferred: Konum transferi |
| 1002 | Girdi alanı | activityFrom | id=activityFrom; type=date |
| 1003 | Girdi alanı | activityTo | id=activityTo; type=date |
| 1004 | Seçim alanı | assetId | id=reservationAsset; name=assetId; seçenekler=(değer yok): Uygun varlıklar yükleniyor… |
| 1005 | Seçim alanı | employeeId | id=reservationEmployee; name=employeeId; seçenekler=(değer yok): Çalışanlar yükleniyor… |
| 1006 | Girdi alanı | startsOn | type=date; name=startsOn |
| 1007 | Girdi alanı | endsOn | type=date; name=endsOn |
| 1008 | Metin alanı | requestNote | name=requestNote |
| 1009 | Girdi alanı | Rezervasyonlarda ara | id=reservationSearch; type=search |
| 1010 | Seçim alanı | Rezervasyon durumu | id=reservationStatus; seçenekler=(değer yok): Tüm durumlar, Pending: Onay bekliyor, Approved: Onaylandı, Rejected: Reddedildi, Cancelled: İptal edildi, Completed: Tamamlandı |
| 1011 | Girdi alanı | reservationFrom | id=reservationFrom; type=date |
| 1012 | Girdi alanı | reservationTo | id=reservationTo; type=date |
| 1013 | Metin alanı | note | id=reservationReviewNote; name=note |
| 1014 | Seçim alanı | 30: 30 gün, 90: 90 gün, 180: 180 gün, 366: 1 yıl | id=auditScheduleHorizon; seçenekler=30: 30 gün, 90: 90 gün, 180: 180 gün, 366: 1 yıl |
| 1015 | Seçim alanı | Denetim planı durumu | id=auditScheduleState; seçenekler=all: Gecikmiş ve yaklaşan, overdue: Yalnızca gecikmiş, upcoming: Yalnızca yaklaşan, unscheduled: Denetim planı olmayanlar, completed: Tamamlanan denetimler |
| 1016 | Girdi alanı | Denetim kuyruğunda ara | id=auditScheduleSearch |
| 1017 | Girdi alanı | Denetim kayıtlarında ara | id=auditSearch |
| 1018 | Seçim alanı | (değer yok): Tüm kayıt türleri, Asset: Varlık, Employee: Çalışan, Company: Şirket, categories: Kategoriler, brands: Markalar, models: Modeller | id=auditTarget; seçenekler=(değer yok): Tüm kayıt türleri, Asset: Varlık, Employee: Çalışan, Company: Şirket, categories: Kategoriler, brands: Markalar, models: Modeller |
| 1019 | Seçim alanı | İşlem türü | id=auditAction; seçenekler=(değer yok): Tüm işlemler |
| 1020 | Girdi alanı | Denetim başlangıç tarihi | id=auditFrom; type=date |
| 1021 | Girdi alanı | Denetim bitiş tarihi | id=auditTo; type=date |
| 1022 | Girdi alanı | Sözleşmelerde ara | id=contractSearch |
| 1023 | Seçim alanı | (değer yok): Etkin ve arşiv kayıtları, active: Etkin, expiring: 30 gün içinde bitecek, expired: Süresi dolmuş, archived: Arşiv | id=contractStatus; seçenekler=(değer yok): Etkin ve arşiv kayıtları, active: Etkin, expiring: 30 gün içinde bitecek, expired: Süresi dolmuş, archived: Arşiv |
| 1024 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1025 | Girdi alanı | title | name=title |
| 1026 | Metin alanı | description | name=description |
| 1027 | Seçim alanı | priority | name=priority; seçenekler=Low: Düşük, Normal: Normal, High: Yüksek, Urgent: Acil |
| 1028 | Seçim alanı | assignedEmployeeId | name=assignedEmployeeId; seçenekler=(değer yok): Atanmamış |
| 1029 | Girdi alanı | dueAt | type=date; name=dueAt |
| 1030 | Girdi alanı | Görevlerde ara | id=taskSearch |
| 1031 | Seçim alanı | Görev durumu | id=taskStatusFilter; seçenekler=(değer yok): Tüm durumlar, Open: Açık, InProgress: Devam ediyor, Completed: Tamamlandı, Cancelled: İptal edildi |
| 1032 | Seçim alanı | Görev önceliği | id=taskPriorityFilter; seçenekler=(değer yok): Tüm öncelikler, Low: Düşük, Normal: Normal, High: Yüksek, Urgent: Acil |
| 1033 | Girdi alanı | subject | name=subject |
| 1034 | Seçim alanı | category | name=category; seçenekler=Access: Hesap ve erişim, Inventory: Envanter, Data: Veri ve aktarım, Other: Diğer |
| 1035 | Metin alanı | message | name=message |
| 1036 | Girdi alanı | Destek taleplerinde ara | id=supportSearch |
| 1037 | Seçim alanı | Talep durumu | id=supportStatusFilter; seçenekler=(değer yok): Tüm durumlar, Open: Açık, InProgress: İşlemde, AwaitingCustomer: Yanıtınız bekleniyor, Resolved: Çözüldü, Closed: Kapatıldı |
| 1038 | Metin alanı | body | name=body |
| 1039 | Girdi alanı | passwordMinimumLength | type=number; name=passwordMinimumLength |
| 1040 | Girdi alanı | sessionDurationHours | type=number; name=sessionDurationHours |
| 1041 | Girdi alanı | requireMfa | type=checkbox; name=requireMfa |
| 1042 | Girdi alanı | name | name=name |
| 1043 | Girdi alanı | scope | type=checkbox; name=scope |
| 1044 | Girdi alanı | scope | type=checkbox; name=scope |
| 1045 | Girdi alanı | scope | type=checkbox; name=scope |
| 1046 | Girdi alanı | scope | type=checkbox; name=scope |
| 1047 | Seçim alanı | expiresInDays | name=expiresInDays; seçenekler=(değer yok): Süresiz, 30: 30 gün, 90: 90 gün, 365: 365 gün |
| 1048 | Girdi alanı | integrationSecretValue | id=integrationSecretValue; type=password |
| 1049 | Girdi alanı | Bakım kayıtlarında ara | id=maintenanceSearch |
| 1050 | Seçim alanı | (değer yok): Tüm durumlar, Scheduled: Planlandı, InProgress: Devam ediyor, Completed: Tamamlandı, Cancelled: İptal edildi | id=maintenanceStatus; seçenekler=(değer yok): Tüm durumlar, Scheduled: Planlandı, InProgress: Devam ediyor, Completed: Tamamlandı, Cancelled: İptal edildi |
| 1051 | Girdi alanı | maintenanceFrom | id=maintenanceFrom; type=date |
| 1052 | Girdi alanı | maintenanceTo | id=maintenanceTo; type=date |
| 1053 | Girdi alanı | calendarFrom | id=calendarFrom; type=date |
| 1054 | Girdi alanı | calendarTo | id=calendarTo; type=date |
| 1055 | Seçim alanı | Takvim etkinlik türü | id=calendarKind; seçenekler=(değer yok): Bakım, denetim, lisans, garanti, iade ve rezervasyon, maintenance: Yalnızca bakım planları, audit: Yalnızca fiziksel denetimler, license: Yalnızca lisans bitişleri, warranty: Yalnızca garanti bitişleri, return: Yalnızca beklenen varlık iadeleri, reservation: Yalnızca onaylı rezervasyonlar |
| 1056 | Seçim alanı | Takvim görünümü | id=calendarViewMode; seçenekler=list: Liste görünümü, day: Gün görünümü, week: Hafta görünümü, month: Ay görünümü |
| 1057 | Girdi alanı | reportDateFrom | id=reportDateFrom; type=date |
| 1058 | Girdi alanı | reportDateTo | id=reportDateTo; type=date |
| 1059 | Girdi alanı | Varlık alış raporunda ara | id=assetPurchaseReportSearch |
| 1060 | Girdi alanı | auditReportFrom | id=auditReportFrom; type=date |
| 1061 | Girdi alanı | auditReportTo | id=auditReportTo; type=date |
| 1062 | Girdi alanı | Fiziksel denetim sonuçlarında ara | id=auditReportSearch |
| 1063 | Seçim alanı | Garanti raporu durumu | id=warrantyReportState; seçenekler=upcoming: Yaklaşan, expired: Süresi dolan, later: Daha ileri tarih, missing: Tarihi olmayan, all: Tümü |
| 1064 | Seçim alanı | 30: 30 gün, 90: 90 gün, 180: 180 gün, 366: 1 yıl | id=warrantyReportHorizon; seçenekler=30: 30 gün, 90: 90 gün, 180: 180 gün, 366: 1 yıl |
| 1065 | Girdi alanı | Garanti raporunda ara | id=warrantyReportSearch |
| 1066 | Girdi alanı | maintenanceReportFrom | id=maintenanceReportFrom; type=date |
| 1067 | Girdi alanı | maintenanceReportTo | id=maintenanceReportTo; type=date |
| 1068 | Seçim alanı | Lisans bitiş raporu durumu | id=licenseReportState; seçenekler=upcoming: Yaklaşan, expired: Süresi dolan, later: Daha ileri tarih, missing: Tarihi olmayan, all: Tümü |
| 1069 | Seçim alanı | 30: 30 gün, 90: 90 gün, 180: 180 gün, 366: 1 yıl | id=licenseReportHorizon; seçenekler=30: 30 gün, 90: 90 gün, 180: 180 gün, 366: 1 yıl |
| 1070 | Girdi alanı | Lisans bitiş raporunda ara | id=licenseReportSearch |
| 1071 | Seçim alanı | Bekleyen teslimat tarihine göre filtrele | id=pendingDeliveryState; seçenekler=all: Tüm açık kalemler, overdue: Geciken teslimatlar, due30: Önümüzdeki 30 gün, undated: Tarihi belirtilmemiş |
| 1072 | Girdi alanı | Bekleyen teslimat raporunda ara | id=pendingDeliverySearch |
| 1073 | Girdi alanı | depreciationReportAsOf | id=depreciationReportAsOf; type=date |
| 1074 | Girdi alanı | Amortisman raporunda ara | id=depreciationReportSearch |
| 1075 | Girdi alanı | customReportName | id=customReportName |
| 1076 | Seçim alanı | (değer yok): Taslak seçin… | id=customReportPreset; seçenekler=(değer yok): Taslak seçin… |
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
| 1093 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1094 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1095 | Girdi alanı | customReportSearch | id=customReportSearch |
| 1096 | Seçim alanı | (değer yok): Tüm durumlar, Available: Kullanılabilir, Deployed: Atanmış, Pending: Beklemede, Maintenance: Bakımda, Unusable: Kullanılamaz, Archived: Arşivlenmiş | id=customReportStatus; seçenekler=(değer yok): Tüm durumlar, Available: Kullanılabilir, Deployed: Atanmış, Pending: Beklemede, Maintenance: Bakımda, Unusable: Kullanılamaz, Archived: Arşivlenmiş |
| 1097 | Seçim alanı | (değer yok): Tüm garanti durumları, expired: Süresi dolmuş, expiring: Yakında dolacak, active: Geçerli, unspecified: Tarih girilmemiş | id=customReportWarranty; seçenekler=(değer yok): Tüm garanti durumları, expired: Süresi dolmuş, expiring: Yakında dolacak, active: Geçerli, unspecified: Tarih girilmemiş |
| 1098 | Seçim alanı | tag:asc: Etiket · A–Z, tag:desc: Etiket · Z–A, name:asc: Ada göre · A–Z, name:desc: Ada göre · Z–A, category:asc: Kategori · A–Z, brand:asc: Marka / model · A–Z, employee:asc: Çalışan · A–Z, location:asc: Konum · A–Z | id=customReportSort; seçenekler=tag:asc: Etiket · A–Z, tag:desc: Etiket · Z–A, name:asc: Ada göre · A–Z, name:desc: Ada göre · Z–A, category:asc: Kategori · A–Z, brand:asc: Marka / model · A–Z, employee:asc: Çalışan · A–Z, location:asc: Konum · A–Z |
| 1099 | Girdi alanı | Sarf ve bileşen siparişlerinde ara | id=stockOrderSearch |
| 1100 | Seçim alanı | Sipariş durumuna göre filtrele | id=stockOrderStatus; seçenekler=(değer yok): Tüm durumlar, Ordered: Sipariş edildi, PartiallyReceived: Kısmi teslim alındı, Received: Tamamlandı, Cancelled: İptal edildi |
| 1101 | Seçim alanı | Sipariş kalem türüne göre filtrele | id=stockOrderItemType; seçenekler=(değer yok): Tüm kalem türleri, Consumable: Sarf, Component: Bileşen |
| 1102 | Seçim alanı | Teslimat takvimine göre filtrele | id=stockOrderDelivery; seçenekler=(değer yok): Tüm teslimatlar, overdue: Geciken teslimatlar |
| 1103 | Girdi alanı | Sipariş başlangıç tarihi | id=stockOrderFrom; type=date |
| 1104 | Girdi alanı | Sipariş bitiş tarihi | id=stockOrderTo; type=date |
| 1105 | Seçim alanı | Sayfa başına sipariş sayısı | id=stockOrderPageSize; seçenekler=25: 25, 50: 50, 100: 100 |
| 1106 | Girdi alanı | Tüm satın alma siparişlerinde ara | id=purchaseOverviewSearch; type=search |
| 1107 | Seçim alanı | Satın alma türü | id=purchaseOverviewType; seçenekler=(değer yok): Tüm türler, asset: Varlık, accessory: Aksesuar, stock: Sarf / bileşen, license: Yazılım lisansı |
| 1108 | Seçim alanı | Sipariş durumu | id=purchaseOverviewStatus; seçenekler=(değer yok): Tüm durumlar, Ordered: Sipariş edildi, PartiallyReceived: Kısmi teslim, Received: Tamamlandı, Cancelled: İptal edildi, Recorded: Kaydedildi |
| 1109 | Girdi alanı | Sipariş başlangıç tarihi | id=purchaseOverviewFrom; type=date |
| 1110 | Girdi alanı | Sipariş bitiş tarihi | id=purchaseOverviewTo; type=date |
| 1111 | Seçim alanı | Sayfa başına sipariş sayısı | id=purchaseOverviewPageSize; seçenekler=25: 25, 50: 50, 100: 100 |
| 1112 | Girdi alanı | Varlık satın alma siparişlerinde ara | id=assetOrderSearch |
| 1113 | Seçim alanı | Sipariş durumuna göre filtrele | id=assetOrderStatus; seçenekler=(değer yok): Tüm durumlar, Ordered: Sipariş edildi, PartiallyReceived: Kısmi teslim alındı, Received: Tamamlandı, Cancelled: İptal edildi |
| 1114 | Girdi alanı | Sipariş başlangıç tarihi | id=assetOrderFrom; type=date |
| 1115 | Girdi alanı | Sipariş bitiş tarihi | id=assetOrderTo; type=date |
| 1116 | Seçim alanı | Sayfa başına sipariş sayısı | id=assetOrderPageSize; seçenekler=25: 25, 50: 50, 100: 100 |
| 1117 | Girdi alanı | roleKey | type=hidden; name=roleKey; başlangıçta gizli |
| 1118 | Girdi alanı | name | name=name |
| 1119 | Girdi alanı | permission | type=checkbox; name=permission |
| 1120 | Girdi alanı | permission | type=checkbox; name=permission |
| 1121 | Girdi alanı | permission | type=checkbox; name=permission |
| 1122 | Girdi alanı | permission | type=checkbox; name=permission |
| 1123 | Girdi alanı | permission | type=checkbox; name=permission |
| 1124 | Girdi alanı | permission | type=checkbox; name=permission |
| 1125 | Girdi alanı | commandPaletteSearch | id=commandPaletteSearch; type=search |
| 1126 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1127 | Girdi alanı | name | name=name |
| 1128 | Girdi alanı | contractNumber | name=contractNumber |
| 1129 | Girdi alanı | contractType | name=contractType |
| 1130 | Seçim alanı | supplierId | name=supplierId; seçenekler=(değer yok): Elle girilen tedarikçi |
| 1131 | Girdi alanı | supplier | name=supplier |
| 1132 | Girdi alanı | startDate | type=date; name=startDate |
| 1133 | Girdi alanı | endDate | type=date; name=endDate |
| 1134 | Girdi alanı | cost | type=number; name=cost |
| 1135 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1136 | Girdi alanı | contractLookupSearch | id=contractLookupSearch; type=search |
| 1137 | Seçim alanı | Sözleşmeye bağlı varlıklar | id=contractAssetIds; name=assetIds |
| 1138 | Seçim alanı | Sözleşmeye bağlı yazılım lisansları | id=contractLicenseIds; name=licenseIds |
| 1139 | Metin alanı | notes | name=notes |
| 1140 | Girdi alanı | contractAttachmentFile | id=contractAttachmentFile; type=file; başlangıçta gizli |
| 1141 | Seçim alanı | Tema | id=themePreference; seçenekler=system: Sistem ayarını izle, light: Açık, dark: Koyu |
| 1142 | Seçim alanı | tr: Türkçe, en: English, ru: Русский, uk: Українська, mk: Македонски | id=languagePreference; seçenekler=tr: Türkçe, en: English, ru: Русский, uk: Українська, mk: Македонски |
| 1143 | Girdi alanı | Üst varlık ara | id=assetParentSearch; type=search |
| 1144 | Seçim alanı | Üst varlık | id=assetParentSelect; seçenekler=(değer yok): Üst varlık yok |
| 1145 | Metin alanı | note | id=assetNoteInput; name=note |
| 1146 | Girdi alanı | file | id=assetAttachmentInput; type=file; name=file |
| 1147 | Seçim alanı | assetEventId | id=assetAttachmentEvent; name=assetEventId; seçenekler=(değer yok): Bir harekete bağlama |
| 1148 | Seçim alanı | Varlık durumunu değiştir | id=assetStatusAction; seçenekler=(değer yok): Durumu değiştir…, Available: Kullanılabilir, Pending: Beklemede, Maintenance: Bakımda, Unusable: Kullanılamaz, Archived: Arşivle |
| 1149 | Girdi alanı | auditDate | type=date; name=auditDate |
| 1150 | Girdi alanı | nextAuditOn | type=date; name=nextAuditOn |
| 1151 | Seçim alanı | outcome | id=auditOutcome; name=outcome; seçenekler=Accepted: Uygun, Unacceptable: Uygun değil / bulgu var |
| 1152 | Metin alanı | issueDescription | id=auditIssueDescription; name=issueDescription |
| 1153 | Metin alanı | notes | name=notes |
| 1154 | Girdi alanı | nextAuditOn | type=date; name=nextAuditOn |
| 1155 | Girdi alanı | maintenanceType | name=maintenanceType |
| 1156 | Girdi alanı | supplier | name=supplier |
| 1157 | Seçim alanı | responsibleEmployeeId | name=responsibleEmployeeId; seçenekler=(değer yok): Çalışan atama |
| 1158 | Girdi alanı | description | name=description |
| 1159 | Seçim alanı | mode | id=maintenanceMode; name=mode; seçenekler=immediate: Hemen başlat, scheduled: İleri tarihe planla |
| 1160 | Girdi alanı | startedOn | type=date; name=startedOn |
| 1161 | Girdi alanı | plannedOn | type=date; name=plannedOn |
| 1162 | Girdi alanı | cost | type=number; name=cost |
| 1163 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY — Türk lirası, USD: USD — ABD doları, EUR: EUR — Euro |
| 1164 | Metin alanı | notes | name=notes |
| 1165 | Girdi alanı | completedOn | type=date; name=completedOn |
| 1166 | Metin alanı | notes | name=notes |
| 1167 | Girdi alanı | transferSourceLocation | id=transferSourceLocation |
| 1168 | Girdi alanı | targetLocation | name=targetLocation |
| 1169 | Metin alanı | notes | name=notes |
| 1170 | Girdi alanı | expectedUpdatedAt | type=hidden; name=expectedUpdatedAt; başlangıçta gizli |
| 1171 | Girdi alanı | assetTag | name=assetTag |
| 1172 | Girdi alanı | assetName | name=assetName |
| 1173 | Seçim alanı | categoryId | name=categoryId |
| 1174 | Seçim alanı | brandId | name=brandId; seçenekler=(değer yok): Marka seçin |
| 1175 | Seçim alanı | modelId | name=modelId; seçenekler=(değer yok): Model seçin |
| 1176 | Girdi alanı | serialNumber | name=serialNumber |
| 1177 | Girdi alanı | location | name=location |
| 1178 | Seçim alanı | defaultLocationCatalogId | name=defaultLocationCatalogId; seçenekler=(değer yok): Varsayılan konum seçin |
| 1179 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1180 | Girdi alanı | warrantyExpiresAt | type=date; name=warrantyExpiresAt |
| 1181 | Girdi alanı | insuranceProvider | name=insuranceProvider |
| 1182 | Girdi alanı | insurancePolicyNumber | name=insurancePolicyNumber |
| 1183 | Girdi alanı | insuranceExpiresAt | type=date; name=insuranceExpiresAt |
| 1184 | Girdi alanı | purchaseCost | type=number; name=purchaseCost |
| 1185 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY — Türk lirası, USD: USD — ABD doları, EUR: EUR — Euro |
| 1186 | Metin alanı | notes | name=notes |
| 1187 | Girdi alanı | isRequestable | type=checkbox; name=isRequestable |
| 1188 | Girdi alanı | isByod | type=checkbox; name=isByod |
| 1189 | Girdi alanı | assignSearch | id=assignSearch |
| 1190 | Seçim alanı | employeeId | name=employeeId |
| 1191 | Girdi alanı | expectedReturnOn | type=date; name=expectedReturnOn |
| 1192 | Girdi alanı | note | name=note |
| 1193 | Girdi alanı | displayName | name=displayName |
| 1194 | Girdi alanı | employeeNumber | name=employeeNumber |
| 1195 | Girdi alanı | email | type=email; name=email |
| 1196 | Girdi alanı | department | name=department |
| 1197 | Girdi alanı | jobTitle | name=jobTitle |
| 1198 | Girdi alanı | phone | type=tel; name=phone |
| 1199 | Girdi alanı | location | name=location |
| 1200 | Girdi alanı | startedOn | type=date; name=startedOn |
| 1201 | Seçim alanı | companyUnitId | name=companyUnitId; seçenekler=(değer yok): Birim seçilmedi |
| 1202 | Girdi alanı | newEmployeeManagerSearch | id=newEmployeeManagerSearch |
| 1203 | Seçim alanı | managerEmployeeId | id=newEmployeeManagerId; name=managerEmployeeId; seçenekler=(değer yok): Yönetici seçilmedi |
| 1204 | Girdi alanı | employeeImportFile | id=employeeImportFile; type=file; başlangıçta gizli |
| 1205 | Girdi alanı | consumableImportFile | id=consumableImportFile; type=file; başlangıçta gizli |
| 1206 | Seçim alanı | Sarf malzemesi eşleme profili | id=consumableImportProfile; seçenekler=(değer yok): Profil seçin |
| 1207 | Girdi alanı | consumableImportProfileName | id=consumableImportProfileName |
| 1208 | Girdi alanı | accessoryImportFile | id=accessoryImportFile; type=file; başlangıçta gizli |
| 1209 | Seçim alanı | Aksesuar eşleme profili | id=accessoryImportProfile; seçenekler=(değer yok): Profil seçin |
| 1210 | Girdi alanı | accessoryImportProfileName | id=accessoryImportProfileName |
| 1211 | Girdi alanı | displayName | name=displayName |
| 1212 | Girdi alanı | employeeNumber | name=employeeNumber |
| 1213 | Girdi alanı | email | type=email; name=email |
| 1214 | Girdi alanı | department | name=department |
| 1215 | Girdi alanı | jobTitle | name=jobTitle |
| 1216 | Girdi alanı | phone | type=tel; name=phone |
| 1217 | Girdi alanı | location | name=location |
| 1218 | Girdi alanı | startedOn | type=date; name=startedOn |
| 1219 | Seçim alanı | companyUnitId | name=companyUnitId; seçenekler=(değer yok): Birim seçilmedi |
| 1220 | Girdi alanı | value | id=textEntryValue; type=text; name=value |
| 1221 | Girdi alanı | Yönetici ara | id=employeeManagerSearch |
| 1222 | Seçim alanı | Yönetici seç | id=employeeManagerSelect; seçenekler=(değer yok): Yönetici yok |
| 1223 | Girdi alanı | fullName | name=fullName |
| 1224 | Girdi alanı | email | type=email; name=email |
| 1225 | Seçim alanı | roleKey | name=roleKey; seçenekler=asset_operator: Envanter operatörü, asset_manager: Envanter yöneticisi, auditor: Denetçi, viewer: Salt okunur, hr_operator: İK operatörü, admin: Yönetici |
| 1226 | Girdi alanı | userId | type=hidden; name=userId; başlangıçta gizli |
| 1227 | Seçim alanı | mode | id=memberLocationAccessMode; name=mode; seçenekler=all: Tüm konumlar, restricted: Yalnız seçilen konumlar |
| 1228 | Seçim alanı | companyUnitMode | id=memberCompanyUnitAccessMode; name=companyUnitMode; seçenekler=all: Tüm şirket birimleri, restricted: Yalnız seçilen şirket birimleri |
| 1229 | Girdi alanı | assetName | name=assetName |
| 1230 | Girdi alanı | assetTag | name=assetTag |
| 1231 | Seçim alanı | categoryId | name=categoryId; seçenekler=(değer yok): Kategori seçin |
| 1232 | Seçim alanı | brandId | name=brandId; seçenekler=(değer yok): Marka seçin |
| 1233 | Seçim alanı | modelId | name=modelId; seçenekler=(değer yok): Model seçin |
| 1234 | Girdi alanı | serialNumber | name=serialNumber |
| 1235 | Girdi alanı | location | name=location |
| 1236 | Seçim alanı | defaultLocationCatalogId | name=defaultLocationCatalogId; seçenekler=(değer yok): Varsayılan konum seçin |
| 1237 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1238 | Girdi alanı | warrantyExpiresAt | type=date; name=warrantyExpiresAt |
| 1239 | Girdi alanı | insuranceProvider | name=insuranceProvider |
| 1240 | Girdi alanı | insurancePolicyNumber | name=insurancePolicyNumber |
| 1241 | Girdi alanı | insuranceExpiresAt | type=date; name=insuranceExpiresAt |
| 1242 | Girdi alanı | purchaseCost | type=number; name=purchaseCost |
| 1243 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY — Türk lirası, USD: USD — ABD doları, EUR: EUR — Euro |
| 1244 | Metin alanı | notes | name=notes |
| 1245 | Girdi alanı | isRequestable | type=checkbox; name=isRequestable |
| 1246 | Girdi alanı | isByod | type=checkbox; name=isByod |
| 1247 | Girdi alanı | email | type=email; name=email |
| 1248 | Girdi alanı | password | type=password; name=password |
| 1249 | Girdi alanı | rememberMe | type=checkbox; name=rememberMe |
| 1250 | Girdi alanı | twoFactorCode | name=twoFactorCode |
| 1251 | Seçim alanı | companyId | id=loginCompanyId; name=companyId; seçenekler=(değer yok): Şirket seçin |
| 1252 | Girdi alanı | widget | type=checkbox; name=widget |
| 1253 | Girdi alanı | widget | type=checkbox; name=widget |
| 1254 | Girdi alanı | widget | type=checkbox; name=widget |
| 1255 | Girdi alanı | widget | type=checkbox; name=widget |
| 1256 | Girdi alanı | widget | type=checkbox; name=widget |
| 1257 | Seçim alanı | category: Kategori, brand: Marka, model: Model, location: Konum, status: Durum | id=dashboardChartDimension; seçenekler=category: Kategori, brand: Marka, model: Model, location: Konum, status: Durum |
| 1258 | Seçim alanı | bar: Yatay çubuk, donut: Halka grafik | id=dashboardChartType; seçenekler=bar: Yatay çubuk, donut: Halka grafik |
| 1259 | Girdi alanı | email | type=email; name=email |
| 1260 | Seçim alanı | Kayıtlı sütun eşlemesi | id=assetImportProfile; seçenekler=(değer yok): Profil seçin |
| 1261 | Girdi alanı | assetImportProfileName | id=assetImportProfileName |
| 1262 | Girdi alanı | importPreviewFile | id=importPreviewFile; type=file |
| 1263 | Girdi alanı | quantity | type=number; name=quantity |
| 1264 | Metin alanı | notes | name=notes |
| 1265 | Metin alanı | accessoryOrderApprovalNote | id=accessoryOrderApprovalNote |
| 1266 | Girdi alanı | file | type=file; name=file |
| 1267 | Girdi alanı | orderNumber | name=orderNumber |
| 1268 | Girdi alanı | supplier | name=supplier |
| 1269 | Girdi alanı | orderDate | type=date; name=orderDate |
| 1270 | Girdi alanı | expectedOn | type=date; name=expectedOn |
| 1271 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY, USD: USD, EUR: EUR |
| 1272 | Girdi alanı | taxRatePercent | type=number; name=taxRatePercent |
| 1273 | Girdi alanı | discountAmount | type=number; name=discountAmount |
| 1274 | Girdi alanı | freightAmount | type=number; name=freightAmount |
| 1275 | Girdi alanı | notes | name=notes |
| 1276 | Girdi alanı | orderNumber | name=orderNumber |
| 1277 | Girdi alanı | supplier | name=supplier |
| 1278 | Girdi alanı | orderDate | type=date; name=orderDate |
| 1279 | Girdi alanı | expectedOn | type=date; name=expectedOn |
| 1280 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY, USD: USD, EUR: EUR |
| 1281 | Girdi alanı | taxRatePercent | type=number; name=taxRatePercent |
| 1282 | Girdi alanı | discountAmount | type=number; name=discountAmount |
| 1283 | Girdi alanı | freightAmount | type=number; name=freightAmount |
| 1284 | Girdi alanı | notes | name=notes |
| 1285 | Metin alanı | stockOrderApprovalNote | id=stockOrderApprovalNote |
| 1286 | Girdi alanı | file | id=stockOrderAttachmentInput; type=file; name=file |
| 1287 | Girdi alanı | receivedOn | type=date; name=receivedOn |
| 1288 | Girdi alanı | notes | name=notes |
| 1289 | Girdi alanı | name | name=name |
| 1290 | Girdi alanı | description | name=description |
| 1291 | Seçim alanı | assetId | name=assetId; seçenekler=(değer yok): Varlık seçin |
| 1292 | Girdi alanı | year | type=number; name=year |
| 1293 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1294 | Girdi alanı | amount | type=number; name=amount |
| 1295 | Girdi alanı | orderNumber | name=orderNumber |
| 1296 | Girdi alanı | supplier | name=supplier |
| 1297 | Girdi alanı | orderDate | type=date; name=orderDate |
| 1298 | Girdi alanı | expectedOn | type=date; name=expectedOn |
| 1299 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1300 | Girdi alanı | taxRatePercent | type=number; name=taxRatePercent |
| 1301 | Girdi alanı | discountAmount | type=number; name=discountAmount |
| 1302 | Girdi alanı | freightAmount | type=number; name=freightAmount |
| 1303 | Girdi alanı | notes | name=notes |
| 1304 | Metin alanı | assetOrderApprovalNote | id=assetOrderApprovalNote |
| 1305 | Girdi alanı | assetOrderAttachmentFiles | id=assetOrderAttachmentFiles; type=file |
| 1306 | Girdi alanı | receivedOn | type=date; name=receivedOn |
| 1307 | Girdi alanı | notes | name=notes |
| 1308 | Girdi alanı | receivedOn | type=date; name=receivedOn |
| 1309 | Girdi alanı | notes | name=notes |
| 1310 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1311 | Girdi alanı | name | name=name |
| 1312 | Girdi alanı | category | name=category |
| 1313 | Girdi alanı | modelNumber | name=modelNumber |
| 1314 | Girdi alanı | itemNumber | name=itemNumber |
| 1315 | Girdi alanı | supplier | name=supplier |
| 1316 | Girdi alanı | purchaseOrderReference | name=purchaseOrderReference |
| 1317 | Girdi alanı | location | name=location |
| 1318 | Girdi alanı | minimumQuantity | type=number; name=minimumQuantity |
| 1319 | Girdi alanı | quantity | type=number; name=quantity |
| 1320 | Girdi alanı | unitCost | type=number; name=unitCost |
| 1321 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1322 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1323 | Girdi alanı | warrantyExpiresAt | type=date; name=warrantyExpiresAt |
| 1324 | Girdi alanı | notes | name=notes |
| 1325 | Girdi alanı | isRequestable | type=checkbox; name=isRequestable |
| 1326 | Girdi alanı | quantity | type=number; name=quantity |
| 1327 | Girdi alanı | notes | name=notes |
| 1328 | Seçim alanı | employeeId | name=employeeId; seçenekler=(değer yok): Çalışan seçin |
| 1329 | Girdi alanı | quantity | type=number; name=quantity |
| 1330 | Girdi alanı | notes | name=notes |
| 1331 | Girdi alanı | file | type=file; name=file |
| 1332 | Girdi alanı | isEulaDocument | type=checkbox; name=isEulaDocument |
| 1333 | Girdi alanı | eulaVersion | name=eulaVersion |
| 1334 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1335 | Seçim alanı | supplierId | name=supplierId; seçenekler=(değer yok): Tedarikçi seçin |
| 1336 | Girdi alanı | orderNumber | name=orderNumber |
| 1337 | Girdi alanı | orderDate | type=date; name=orderDate |
| 1338 | Girdi alanı | seatsPurchased | type=number; name=seatsPurchased |
| 1339 | Girdi alanı | unitCost | type=number; name=unitCost |
| 1340 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1341 | Metin alanı | notes | name=notes |
| 1342 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1343 | Girdi alanı | name | name=name |
| 1344 | Girdi alanı | publisher | name=publisher |
| 1345 | Girdi alanı | productVersion | name=productVersion |
| 1346 | Seçim alanı | licenseType | name=licenseType; seçenekler=Subscription: Abonelik, Perpetual: Süresiz, Device: Cihaz başına, Site: Şirket geneli, Trial: Deneme, Other: Diğer |
| 1347 | Girdi alanı | licenseReference | name=licenseReference |
| 1348 | Girdi alanı | supplier | name=supplier |
| 1349 | Girdi alanı | purchaseOrderReference | name=purchaseOrderReference |
| 1350 | Girdi alanı | seats | type=number; name=seats |
| 1351 | Girdi alanı | unitCost | type=number; name=unitCost |
| 1352 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1353 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1354 | Girdi alanı | expirationDate | type=date; name=expirationDate |
| 1355 | Girdi alanı | autoRenew | type=checkbox; name=autoRenew |
| 1356 | Girdi alanı | requiresEulaAcceptance | type=checkbox; name=requiresEulaAcceptance |
| 1357 | Girdi alanı | notes | name=notes |
| 1358 | Girdi alanı | licenseImportFile | id=licenseImportFile; type=file; başlangıçta gizli |
| 1359 | Seçim alanı | Lisans eşleme profili | id=licenseImportProfile; seçenekler=(değer yok): Profil seçin |
| 1360 | Girdi alanı | licenseImportProfileName | id=licenseImportProfileName |
| 1361 | Girdi alanı | eulaAccepted | id=licenseEulaAccepted; type=checkbox; name=eulaAccepted |
| 1362 | Seçim alanı | targetType | name=targetType; seçenekler=employee: Çalışan, asset: Cihaz / varlık |
| 1363 | Seçim alanı | employeeId | name=employeeId; seçenekler=(değer yok): Çalışan seçin |
| 1364 | Girdi alanı | licenseBulkEmployeeMode | id=licenseBulkEmployeeMode; type=checkbox |
| 1365 | Girdi alanı | licenseAssetSearch | id=licenseAssetSearch; type=search |
| 1366 | Seçim alanı | assetId | name=assetId; seçenekler=(değer yok): Önce arama yapın |
| 1367 | Girdi alanı | notes | name=notes |
| 1368 | Girdi alanı | componentImportFile | id=componentImportFile; type=file; başlangıçta gizli |
| 1369 | Seçim alanı | Bileşen eşleme profili | id=componentImportProfile; seçenekler=(değer yok): Profil seçin |
| 1370 | Girdi alanı | componentImportProfileName | id=componentImportProfileName |
| 1371 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1372 | Girdi alanı | name | name=name |
| 1373 | Girdi alanı | category | name=category |
| 1374 | Girdi alanı | modelNumber | name=modelNumber |
| 1375 | Girdi alanı | itemNumber | name=itemNumber |
| 1376 | Girdi alanı | serialNumber | name=serialNumber |
| 1377 | Girdi alanı | supplier | name=supplier |
| 1378 | Girdi alanı | purchaseOrderReference | name=purchaseOrderReference |
| 1379 | Girdi alanı | location | name=location |
| 1380 | Girdi alanı | minimumQuantity | type=number; name=minimumQuantity |
| 1381 | Girdi alanı | quantity | type=number; name=quantity |
| 1382 | Girdi alanı | unitCost | type=number; name=unitCost |
| 1383 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1384 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1385 | Girdi alanı | warrantyExpiresAt | type=date; name=warrantyExpiresAt |
| 1386 | Girdi alanı | notes | name=notes |
| 1387 | Girdi alanı | quantity | type=number; name=quantity |
| 1388 | Girdi alanı | notes | name=notes |
| 1389 | Seçim alanı | assetId | name=assetId; seçenekler=(değer yok): Varlık seçin |
| 1390 | Girdi alanı | quantity | type=number; name=quantity |
| 1391 | Girdi alanı | notes | name=notes |
| 1392 | Girdi alanı | allocationId | type=hidden; name=allocationId; başlangıçta gizli |
| 1393 | Girdi alanı | quantity | type=number; name=quantity |
| 1394 | Girdi alanı | notes | name=notes |
| 1395 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1396 | Girdi alanı | name | name=name |
| 1397 | Girdi alanı | description | name=description |
| 1398 | Girdi alanı | accessoryKitItemSearch | id=accessoryKitItemSearch; type=search |
| 1399 | Girdi alanı | accessoryId | type=hidden; name=accessoryId; başlangıçta gizli |
| 1400 | Girdi alanı | name | name=name |
| 1401 | Seçim alanı | categoryId | name=categoryId; seçenekler=(değer yok): Kategori seçin (isteğe bağlı) |
| 1402 | Seçim alanı | brandId | name=brandId; seçenekler=(değer yok): Marka seçin (isteğe bağlı) |
| 1403 | Seçim alanı | modelId | name=modelId; seçenekler=(değer yok): Model seçin (isteğe bağlı) |
| 1404 | Girdi alanı | modelPhoto | type=file; name=modelPhoto |
| 1405 | Girdi alanı | modelNumber | name=modelNumber |
| 1406 | Girdi alanı | itemNumber | name=itemNumber |
| 1407 | Girdi alanı | supplier | name=supplier |
| 1408 | Girdi alanı | purchaseOrderReference | name=purchaseOrderReference |
| 1409 | Girdi alanı | location | name=location |
| 1410 | Girdi alanı | minimumQuantity | type=number; name=minimumQuantity |
| 1411 | Girdi alanı | quantity | type=number; name=quantity |
| 1412 | Girdi alanı | unitCost | type=number; name=unitCost |
| 1413 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY, USD: USD, EUR: EUR |
| 1414 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1415 | Girdi alanı | warrantyExpiresAt | type=date; name=warrantyExpiresAt |
| 1416 | Girdi alanı | notes | name=notes |
| 1417 | Girdi alanı | isRequestable | type=checkbox; name=isRequestable |
| 1418 | Girdi alanı | quantity | type=number; name=quantity |
| 1419 | Girdi alanı | notes | name=notes |
| 1420 | Girdi alanı | targetType | type=radio; name=targetType |
| 1421 | Girdi alanı | targetType | type=radio; name=targetType |
| 1422 | Girdi alanı | targetType | type=radio; name=targetType |
| 1423 | Seçim alanı | employeeId | name=employeeId |
| 1424 | Seçim alanı | assetId | name=assetId |
| 1425 | Girdi alanı | location | name=location |
| 1426 | Girdi alanı | quantity | type=number; name=quantity |
| 1427 | Girdi alanı | accessoryKitSets | id=accessoryKitSets; type=number |
| 1428 | Girdi alanı | notes | name=notes |
| 1429 | Seçim alanı | view: Varlık ayrıntısını aç, assign: Uygunsa zimmet formunu aç, return: Zimmetliyse iade onayını göster | id=scanAssetAction; seçenekler=view: Varlık ayrıntısını aç, assign: Uygunsa zimmet formunu aç, return: Zimmetliyse iade onayını göster |
| 1430 | Girdi alanı | code | id=scanAssetCode; name=code |
| 1431 | Girdi alanı | kind | type=hidden; name=kind; başlangıçta gizli |
| 1432 | Girdi alanı | currentName | type=hidden; name=currentName; başlangıçta gizli |
| 1433 | Girdi alanı | locationId | type=hidden; name=locationId; başlangıçta gizli |
| 1434 | Girdi alanı | departmentId | type=hidden; name=departmentId; başlangıçta gizli |
| 1435 | Girdi alanı | newName | name=newName |
| 1436 | Seçim alanı | parentDepartmentId | name=parentDepartmentId; seçenekler=(değer yok): Ana departman |
| 1437 | Seçim alanı | locationCatalogId | name=locationCatalogId; seçenekler=(değer yok): Konum seçilmedi |
| 1438 | Girdi alanı | name | name=name |
| 1439 | Seçim alanı | parentDepartmentId | name=parentDepartmentId; seçenekler=(değer yok): Ana departman |
| 1440 | Seçim alanı | locationCatalogId | name=locationCatalogId; seçenekler=(değer yok): Konum seçilmedi |
| 1441 | Seçim alanı | targetDepartmentId | name=targetDepartmentId |
| 1442 | Seçim alanı | parentLocationId | name=parentLocationId; seçenekler=(değer yok): Üst seviye · kat veya bina |
| 1443 | Girdi alanı | name | name=name |
| 1444 | Girdi alanı | locationId | type=hidden; name=locationId; başlangıçta gizli |
| 1445 | Girdi alanı | locationManagerSearch | id=locationManagerSearch; type=search |
| 1446 | Seçim alanı | employeeId | id=locationManagerEmployee; name=employeeId; seçenekler=(değer yok): Sorumlu atanmamış |
| 1447 | Girdi alanı | departmentId | type=hidden; name=departmentId; başlangıçta gizli |
| 1448 | Girdi alanı | departmentManagerSearch | id=departmentManagerSearch; type=search |
| 1449 | Seçim alanı | employeeId | id=departmentManagerEmployee; name=employeeId; seçenekler=(değer yok): Yetkili atanmamış |
| 1450 | Girdi alanı | name | name=name |
| 1451 | Girdi alanı | modelNumber | name=modelNumber |
| 1452 | Metin alanı | note | name=note |
| 1453 | Metin alanı | note | name=note |
| 1454 | Metin alanı | note | name=note |
| 1455 | Girdi alanı | name | name=name |
| 1456 | Girdi alanı | code | name=code |
| 1457 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1458 | Girdi alanı | name | name=name |
| 1459 | Girdi alanı | contactName | name=contactName |
| 1460 | Girdi alanı | email | type=email; name=email |
| 1461 | Girdi alanı | phone | name=phone |
| 1462 | Girdi alanı | website | type=url; name=website |
| 1463 | Metin alanı | notes | name=notes |
| 1464 | Girdi alanı | logo | type=file; name=logo |
| 1465 | Girdi alanı | Tedarikçilerde ara | id=supplierCatalogSearch |
| 1466 | Girdi alanı | showArchivedSuppliers | id=showArchivedSuppliers; type=checkbox |
| 1467 | Seçim alanı | companySwitchTarget | id=companySwitchTarget |
| 1468 | Girdi alanı | accountPhotoInput | id=accountPhotoInput; type=file; başlangıçta gizli |
| 1469 | Girdi alanı | ownCurrentPassword | id=ownCurrentPassword; type=password |
| 1470 | Girdi alanı | ownNewPassword | id=ownNewPassword; type=password |
| 1471 | Girdi alanı | ownConfirmPassword | id=ownConfirmPassword; type=password |
| 1472 | Girdi alanı | ownPasswordTwoFactorCode | id=ownPasswordTwoFactorCode |
| 1473 | Girdi alanı | twoFactorCurrentPassword | id=twoFactorCurrentPassword; type=password |
| 1474 | Girdi alanı | twoFactorCode | id=twoFactorCode |
| 1475 | Girdi alanı | itemType | type=hidden; name=itemType; başlangıçta gizli |
| 1476 | Girdi alanı | itemId | type=hidden; name=itemId; başlangıçta gizli |
| 1477 | Girdi alanı | requestEmployeeSearch | id=requestEmployeeSearch |
| 1478 | Seçim alanı | employeeId | id=requestEmployee; name=employeeId; seçenekler=(değer yok): Çalışan seçin |
| 1479 | Girdi alanı | quantity | type=number; name=quantity |
| 1480 | Metin alanı | note | name=note |
| 1481 | Girdi alanı | file | id=modelAttachmentFile; type=file; name=file |
| 1482 | Girdi alanı | returnedOn | type=date; name=returnedOn |
| 1483 | Girdi alanı | reason | name=reason |
| 1484 | Girdi alanı | notes | name=notes |
| 1485 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1486 | Girdi alanı | name | name=name |
| 1487 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1488 | Girdi alanı | serialNumber | name=serialNumber |
| 1489 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1490 | Seçim alanı | categoryId | name=categoryId |
| 1491 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1492 | Seçim alanı | brandId | name=brandId |
| 1493 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1494 | Seçim alanı | modelId | name=modelId |
| 1495 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1496 | Seçim alanı | defaultLocationCatalogId | name=defaultLocationCatalogId |
| 1497 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1498 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1499 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1500 | Girdi alanı | warrantyExpiresAt | type=date; name=warrantyExpiresAt |
| 1501 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1502 | Girdi alanı | insuranceProvider | name=insuranceProvider |
| 1503 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1504 | Girdi alanı | insurancePolicyNumber | name=insurancePolicyNumber |
| 1505 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1506 | Girdi alanı | insuranceExpiresAt | type=date; name=insuranceExpiresAt |
| 1507 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1508 | Girdi alanı | purchaseCost | type=number; name=purchaseCost |
| 1509 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY — Türk lirası, USD: USD — ABD doları, EUR: EUR — Euro |
| 1510 | Seçim alanı | isRequestable | name=isRequestable; seçenekler=(değer yok): Değişiklik yok, true: Evet, false: Hayır |
| 1511 | Seçim alanı | isByod | name=isByod; seçenekler=(değer yok): Değişiklik yok, true: Evet, false: Hayır |
| 1512 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1513 | Metin alanı | notes | name=notes |
| 1514 | Metin alanı | note | name=note |
| 1515 | Seçim alanı | assetId | name=assetId |
| 1516 | Girdi alanı | notes | name=notes |
| 1517 | Girdi alanı | notes | name=notes |
| 1518 | Girdi alanı | ${escapeHtml(assignment.targetName \|\| 'Açık zimmet')} iadesini seç | id=${assignment.id}; type=checkbox |
| 1519 | Girdi alanı | ${escapeHtml(assignment.targetName \|\| 'Açık zimmet')} için toplu iade adedi | type=number |
| 1520 | Girdi alanı | notes | name=notes |
| 1521 | Girdi alanı | ${escapeHtml(asset.assetTag)} varlığını iade et | type=checkbox |
| 1522 | Girdi alanı | ${escapeHtml(label \|\| 'Sipariş kalemi')} iade adedi | type=number |
| 1523 | Seçim alanı | (değer yok): Kalem seçin, ${item.itemType}\|${item.id}: ${item.typeLabel} · ${escapeHtml(item.name)} · ${escapeHtml(item.itemNumber \|\| item.modelNumber \|\| 'Stok kodu yok')} · stok ${fmt.format(item.totalQuantity)} | seçenekler=(değer yok): Kalem seçin, ${item.itemType}\|${item.id}: ${item.typeLabel} · ${escapeHtml(item.name)} · ${escapeHtml(item.itemNumber \|\| item.modelNumber \|\| 'Stok kodu yok')} · stok ${fmt.format(item.totalQuantity)} |
| 1524 | Girdi alanı | (etiketsiz) | type=number |
| 1525 | Girdi alanı | (etiketsiz) | type=number |
| 1526 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1527 | Girdi alanı | ${escapeHtml(quantityLabel)} | type=number |
| 1528 | Girdi alanı | ${escapeHtml(line.itemName)} teslim adedi | type=number |
| 1529 | Girdi alanı | (etiketsiz) |  |
| 1530 | Seçim alanı | (etiketsiz) |  |
| 1531 | Seçim alanı | (etiketsiz) |  |
| 1532 | Seçim alanı | (etiketsiz) |  |
| 1533 | Seçim alanı | (etiketsiz) |  |
| 1534 | Girdi alanı | (etiketsiz) | type=number |
| 1535 | Girdi alanı | (etiketsiz) | type=number |
| 1536 | Girdi alanı | (etiketsiz) | type=date |
| 1537 | Girdi alanı | ${escapeHtml(line.name)} teslim adedi | type=number |
| 1538 | Seçim alanı | (değer yok): Aksesuar seçin, ${item.id}: ${escapeHtml(item.name)} · ${escapeHtml(item.itemNumber \|\| item.modelNumber \|\| 'Stok kodu yok')} · stok ${fmt.format(item.totalQuantity)} | seçenekler=(değer yok): Aksesuar seçin, ${item.id}: ${escapeHtml(item.name)} · ${escapeHtml(item.itemNumber \|\| item.modelNumber \|\| 'Stok kodu yok')} · stok ${fmt.format(item.totalQuantity)} |
| 1539 | Girdi alanı | (etiketsiz) | type=number |
| 1540 | Girdi alanı | (etiketsiz) | type=number |
| 1541 | Girdi alanı | ${escapeHtml(line.accessoryName)} teslim adedi | type=number |
| 1542 | Girdi alanı | ${escapeHtml(item.name)} model numarası |  |
| 1543 | Girdi alanı | (etiketsiz) | type=file |
| 1544 | Girdi alanı | (etiketsiz) | type=number |
| 1545 | Girdi alanı | (etiketsiz) | type=number |
| 1546 | Seçim alanı | amount: ${translateUiText('Tutar')}, percent: ${translateUiText('Yüzde')} | type=${item.id}; seçenekler=amount: ${translateUiText('Tutar')}, percent: ${translateUiText('Yüzde')} |
| 1547 | Seçim alanı | linear: ${translateUiText('Doğrusal')}, half_1: ${translateUiText('Yarım yıl · ilk dönem')}, half_2: ${translateUiText('Yarım yıl · satın alma dönemine göre')} | seçenekler=linear: ${translateUiText('Doğrusal')}, half_1: ${translateUiText('Yarım yıl · ilk dönem')}, half_2: ${translateUiText('Yarım yıl · satın alma dönemine göre')} |
| 1548 | Girdi alanı | (etiketsiz) | type=url |
| 1549 | Girdi alanı | (etiketsiz) | type=url |
| 1550 | Girdi alanı | (etiketsiz) | type=url |
| 1551 | Girdi alanı | (etiketsiz) | type=tel |
| 1552 | Girdi alanı | (etiketsiz) | type=email |
| 1553 | Girdi alanı | (etiketsiz) | type=file |
| 1554 | Girdi alanı | ${escapeHtml(item.name)} ${translateUiText('Kayıt adını düzenle')} | name=${item.id} |
| 1555 | Seçim alanı | (değer yok): Özel alan seçin | id=assetCustomFieldFilter; seçenekler=(değer yok): Özel alan seçin |
| 1556 | Seçim alanı | equals: Değeri eşit, contains: Metin içinde, empty: Değer girilmemiş | id=assetCustomFieldMode; seçenekler=equals: Değeri eşit, contains: Metin içinde, empty: Değer girilmemiş |
| 1557 | Girdi alanı | assetCustomFieldValue | id=assetCustomFieldValue |
| 1558 | Girdi alanı | key | name=key |
| 1559 | Girdi alanı | name | name=name |
| 1560 | Girdi alanı | groupName | name=groupName |
| 1561 | Seçim alanı | categoryId | name=categoryId; seçenekler=(değer yok): Tüm kategoriler |
| 1562 | Seçim alanı | modelId | name=modelId; seçenekler=(değer yok): Tüm modeller |
| 1563 | Seçim alanı | fieldType | name=fieldType; seçenekler=text: Metin, number: Sayı, date: Tarih, checkbox: Onay kutusu, select: Seçim listesi, sensitive: Hassas (şifreli) |
| 1564 | Metin alanı | options | name=options |
| 1565 | Girdi alanı | isRequired | type=checkbox; name=isRequired |
| 1566 | Girdi alanı | sortOrder | type=number; name=sortOrder |
| 1567 | Girdi alanı | name | name=name |
| 1568 | Girdi alanı | groupName | name=groupName |
| 1569 | Seçim alanı | categoryId | name=categoryId; seçenekler=(değer yok): Tüm kategoriler |
| 1570 | Seçim alanı | modelId | name=modelId; seçenekler=(değer yok): Tüm modeller |
| 1571 | Seçim alanı | Çalışan eşleme profili | id=employeeImportProfile; seçenekler=(değer yok): Profil seçin |
| 1572 | Girdi alanı | employeeImportProfileName | id=employeeImportProfileName |
| 1573 | Metin alanı | note | name=note |
| 1574 | Form | maintenanceAttachmentForm | id=maintenanceAttachmentForm; başlangıçta gizli |
| 1575 | Form | companyUnitForm | id=companyUnitForm |
| 1576 | Form | inventoryRequestActionForm | id=inventoryRequestActionForm |
| 1577 | Form | companyNotificationPreferencesForm | id=companyNotificationPreferencesForm |
| 1578 | Form | labelSettingsForm | id=labelSettingsForm |
| 1579 | Form | assetStatusLabelsForm | id=assetStatusLabelsForm |
| 1580 | Form | assetStatusConfigForm | id=assetStatusConfigForm |
| 1581 | Form | catalogCreateForm | id=catalogCreateForm |
| 1582 | Form | companyProfileForm | id=companyProfileForm |
| 1583 | Form | companyLogoForm | id=companyLogoForm |
| 1584 | Form | reservationForm | id=reservationForm |
| 1585 | Form | reservationActionForm | id=reservationActionForm |
| 1586 | Form | taskForm | id=taskForm |
| 1587 | Form | supportForm | id=supportForm |
| 1588 | Form | supportReplyForm | id=supportReplyForm |
| 1589 | Form | companySecurityPolicyForm | id=companySecurityPolicyForm |
| 1590 | Form | integrationTokenForm | id=integrationTokenForm |
| 1591 | Form | companyRoleForm | id=companyRoleForm |
| 1592 | Form | licenseRevokeForm | id=licenseRevokeForm |
| 1593 | Form | licenseImportProfileDeleteForm | id=licenseImportProfileDeleteForm |
| 1594 | Form | contractForm | id=contractForm |
| 1595 | Form | contractActionForm | id=contractActionForm |
| 1596 | Form | assetNoteForm | id=assetNoteForm; başlangıçta gizli |
| 1597 | Form | assetAttachmentForm | id=assetAttachmentForm; başlangıçta gizli |
| 1598 | Form | assetAuditForm | id=assetAuditForm |
| 1599 | Form | assetAuditScheduleForm | id=assetAuditScheduleForm |
| 1600 | Form | maintenanceStartForm | id=maintenanceStartForm |
| 1601 | Form | maintenanceCompleteForm | id=maintenanceCompleteForm |
| 1602 | Form | assetTransferForm | id=assetTransferForm |
| 1603 | Form | editAssetForm | id=editAssetForm |
| 1604 | Form | assignForm | id=assignForm |
| 1605 | Form | newEmployeeForm | id=newEmployeeForm |
| 1606 | Form | editEmployeeForm | id=editEmployeeForm |
| 1607 | Form | employeeActionForm | id=employeeActionForm |
| 1608 | Form | textEntryForm | id=textEntryForm |
| 1609 | Form | inviteMemberForm | id=inviteMemberForm |
| 1610 | Form | memberLocationAccessForm | id=memberLocationAccessForm |
| 1611 | Form | newAssetForm | id=newAssetForm |
| 1612 | Form | loginForm | id=loginForm |
| 1613 | Form | dashboardCustomizeForm | id=dashboardCustomizeForm |
| 1614 | Form | forgotPasswordForm | id=forgotPasswordForm |
| 1615 | Form | importPreviewForm | id=importPreviewForm |
| 1616 | Form | accessoryCheckinForm | id=accessoryCheckinForm |
| 1617 | Form | accessoryOrderAttachmentForm | id=accessoryOrderAttachmentForm |
| 1618 | Form | accessoryOrderCreateForm | id=accessoryOrderCreateForm |
| 1619 | Form | stockOrderCreateForm | id=stockOrderCreateForm |
| 1620 | Form | stockOrderAttachmentForm | id=stockOrderAttachmentForm; başlangıçta gizli |
| 1621 | Form | stockOrderReceiveForm | id=stockOrderReceiveForm |
| 1622 | Form | stockOrderKitTemplateForm | id=stockOrderKitTemplateForm |
| 1623 | Form | assetOrderBudgetForm | id=assetOrderBudgetForm |
| 1624 | Form | assetOrderCreateForm | id=assetOrderCreateForm |
| 1625 | Form | assetOrderReceiveForm | id=assetOrderReceiveForm |
| 1626 | Form | accessoryOrderReceiveForm | id=accessoryOrderReceiveForm |
| 1627 | Form | consumableForm | id=consumableForm |
| 1628 | Form | consumableStockForm | id=consumableStockForm |
| 1629 | Form | consumableCheckoutForm | id=consumableCheckoutForm |
| 1630 | Form | licenseAttachmentForm | id=licenseAttachmentForm |
| 1631 | Form | licensePurchaseOrderForm | id=licensePurchaseOrderForm |
| 1632 | Form | licenseForm | id=licenseForm |
| 1633 | Form | licenseActionForm | id=licenseActionForm |
| 1634 | Form | licenseAssignForm | id=licenseAssignForm |
| 1635 | Form | componentActionForm | id=componentActionForm |
| 1636 | Form | componentForm | id=componentForm |
| 1637 | Form | componentStockForm | id=componentStockForm |
| 1638 | Form | componentAllocateForm | id=componentAllocateForm |
| 1639 | Form | componentRemoveForm | id=componentRemoveForm |
| 1640 | Form | accessoryKitForm | id=accessoryKitForm |
| 1641 | Form | accessoryKitActionForm | id=accessoryKitActionForm |
| 1642 | Form | accessoryForm | id=accessoryForm |
| 1643 | Form | accessoryStockForm | id=accessoryStockForm |
| 1644 | Form | accessoryCheckoutForm | id=accessoryCheckoutForm |
| 1645 | Form | scanAssetForm | id=scanAssetForm |
| 1646 | Form | directoryRenameForm | id=directoryRenameForm |
| 1647 | Form | createDepartmentForm | id=createDepartmentForm |
| 1648 | Form | departmentMoveForm | id=departmentMoveForm |
| 1649 | Form | createLocationForm | id=createLocationForm |
| 1650 | Form | locationManagerForm | id=locationManagerForm |
| 1651 | Form | departmentManagerForm | id=departmentManagerForm |
| 1652 | Form | quickLookupForm | id=quickLookupForm |
| 1653 | Form | returnAssetForm | id=returnAssetForm |
| 1654 | Form | assetTrashConfirmForm | id=assetTrashConfirmForm |
| 1655 | Form | assetStatusForm | id=assetStatusForm |
| 1656 | Form | assetAttachmentRemoveForm | id=assetAttachmentRemoveForm |
| 1657 | Form | savedAssetSearchForm | id=savedAssetSearchForm |
| 1658 | Form | companyMfaEnrollmentForm | id=companyMfaEnrollmentForm |
| 1659 | Form | supplierCatalogForm | id=supplierCatalogForm |
| 1660 | Form | inventoryRequestForm | id=inventoryRequestForm |
| 1661 | Form | modelAttachmentForm | id=modelAttachmentForm; başlangıçta gizli |
| 1662 | Form | purchaseOrderReturnForm | id=purchaseOrderReturnForm |
| 1663 | Form | bulkEditAssetForm | id=bulkEditAssetForm |
| 1664 | Form | (etiketsiz) |  |
| 1665 | Form | (etiketsiz) |  |
| 1666 | Form | (etiketsiz) |  |
| 1667 | Form | assetCustomFieldForm | id=assetCustomFieldForm |
| 1668 | Form | editAssetCustomFieldForm | id=editAssetCustomFieldForm |
| 1669 | Form | bulkAssetActionForm | id=bulkAssetActionForm |
| 1670 | Modal | Kapat | id=employeeHandoverDialog; type=button; başlangıçta gizli |
| 1671 | Modal | Kapat | id=maintenanceAttachmentDialog; type=button; name=file; başlangıçta gizli |
| 1672 | Modal | Kapat | id=companyUnitDialog; type=button; name=id; başlangıçta gizli |
| 1673 | Modal | Kapat | id=inventoryRequestActionDialog; type=button; name=note |
| 1674 | Modal | Kapat | id=reservationActionDialog; type=button; name=note |
| 1675 | Modal | Kapat | id=integrationSecretDialog; type=button |
| 1676 | Modal | Kapat | id=companyRoleDialog; type=button; name=roleKey; başlangıçta gizli |
| 1677 | Modal | Kapat | id=deactivateCompanyRoleDialog; type=button |
| 1678 | Modal | Kapat | id=licenseRevokeDialog; type=button |
| 1679 | Modal | Kapat | id=licenseImportProfileDeleteDialog; type=button |
| 1680 | Modal | Kapat | id=commandPaletteDialog; type=button |
| 1681 | Modal | Kapat | id=keyboardShortcutsDialog; type=button |
| 1682 | Modal | Kapat | id=contractFormDialog; type=button; name=id; başlangıçta gizli |
| 1683 | Modal | Kapat | id=contractDetailDialog; type=button; başlangıçta gizli |
| 1684 | Modal | Kapat | id=contractActionDialog; type=button |
| 1685 | Modal | Kapat | id=appearanceDialog; type=button |
| 1686 | Modal | Kapat | id=assetDialog; type=button; name=note; başlangıçta gizli |
| 1687 | Modal | Kapat | id=assetAuditDialog; type=button; name=auditDate; başlangıçta gizli |
| 1688 | Modal | Kapat | id=assetAuditScheduleDialog; type=button; name=nextAuditOn |
| 1689 | Modal | Kapat | id=maintenanceStartDialog; type=button; name=maintenanceType; başlangıçta gizli |
| 1690 | Modal | Kapat | id=maintenanceCompleteDialog; type=button; name=completedOn |
| 1691 | Modal | Kapat | id=assetTransferDialog; type=button; name=targetLocation |
| 1692 | Modal | Kapat | id=editAssetDialog; type=button; name=expectedUpdatedAt; başlangıçta gizli |
| 1693 | Modal | Kapat | id=assignDialog; type=button; name=employeeId |
| 1694 | Modal | Kapat | id=newEmployeeDialog; type=button; name=displayName |
| 1695 | Modal | Kapat | id=employeeImportDialog; type=button; başlangıçta gizli |
| 1696 | Modal | Kapat | id=consumableImportDialog; type=button; başlangıçta gizli |
| 1697 | Modal | Kapat | id=accessoryImportDialog; type=button; başlangıçta gizli |
| 1698 | Modal | Kapat | id=editEmployeeDialog; type=button; name=displayName |
| 1699 | Modal | Kapat | id=employeeActionDialog; type=button |
| 1700 | Modal | Kapat | id=textEntryDialog; type=button; name=value |
| 1701 | Modal | Kapat | id=employeeDialog; type=button; başlangıçta gizli |
| 1702 | Modal | Kapat | id=inviteMemberDialog; type=button; name=fullName |
| 1703 | Modal | Kapat | id=memberLocationAccessDialog; type=button; name=userId; başlangıçta gizli |
| 1704 | Modal | Kapat | id=newAssetDialog; type=button; name=assetName |
| 1705 | Modal | Kapat | id=dashboardCustomizeDialog; type=button; name=widget |
| 1706 | Modal | Kapat | id=forgotPasswordDialog; type=button; name=email |
| 1707 | Modal | Kapat | id=importPreviewDialog; type=button; başlangıçta gizli |
| 1708 | Modal | Kapat | id=accessoryDialog; type=button |
| 1709 | Modal | Kapat | id=accessoryCheckinDialog; type=button; name=quantity; başlangıçta gizli |
| 1710 | Modal | Kapat | id=accessoryOrderDetailDialog; type=button; name=file; başlangıçta gizli |
| 1711 | Modal | Kapat | id=accessoryOrderCreateDialog; type=button; name=orderNumber |
| 1712 | Modal | Kapat | id=stockOrderCreateDialog; type=button; name=orderNumber |
| 1713 | Modal | Kapat | id=stockOrderDetailDialog; type=button; name=file; başlangıçta gizli |
| 1714 | Modal | Kapat | id=stockOrderReceiveDialog; type=button; name=receivedOn |
| 1715 | Modal | Kapat | id=stockOrderKitTemplateDialog; type=button; name=name |
| 1716 | Modal | Kapat | id=assetOrderBudgetDialog; type=button; name=year |
| 1717 | Modal | Kapat | id=assetOrderCreateDialog; type=button; name=orderNumber |
| 1718 | Modal | Kapat | id=assetOrderDetailDialog; type=button; başlangıçta gizli |
| 1719 | Modal | Kapat | id=assetOrderReceiveDialog; type=button; name=receivedOn |
| 1720 | Modal | Kapat | id=accessoryOrderReceiveDialog; type=button; name=receivedOn |
| 1721 | Modal | Kapat | id=consumableDialog; type=button |
| 1722 | Modal | Kapat | id=consumableFormDialog; type=button; name=id; başlangıçta gizli |
| 1723 | Modal | Kapat | id=consumableStockDialog; type=button; name=quantity |
| 1724 | Modal | Kapat | id=consumableCheckoutDialog; type=button; name=employeeId |
| 1725 | Modal | Kapat | id=licenseDialog; type=button; name=file; başlangıçta gizli |
| 1726 | Modal | Kapat | id=licensePurchaseOrderFormDialog; type=button; name=id; başlangıçta gizli |
| 1727 | Modal | Kapat | id=licenseFormDialog; type=button; name=id; başlangıçta gizli |
| 1728 | Modal | Kapat | id=licenseImportDialog; type=button; başlangıçta gizli |
| 1729 | Modal | Kapat | id=licenseActionDialog; type=button; başlangıçta gizli |
| 1730 | Modal | Kapat | id=licenseAssignDialog; type=button; name=eulaAccepted; başlangıçta gizli |
| 1731 | Modal | Kapat | id=componentActionDialog; type=button |
| 1732 | Modal | Kapat | id=componentDialog; type=button |
| 1733 | Modal | Kapat | id=componentImportDialog; type=button; başlangıçta gizli |
| 1734 | Modal | Kapat | id=componentFormDialog; type=button; name=id; başlangıçta gizli |
| 1735 | Modal | quantity | id=componentStockDialog; type=button; name=quantity |
| 1736 | Modal | assetId | id=componentAllocateDialog; type=button; name=assetId |
| 1737 | Modal | Kapat | id=componentRemoveDialog; type=button; name=allocationId; başlangıçta gizli |
| 1738 | Modal | Kapat | id=accessoryKitFormDialog; type=button; name=id; başlangıçta gizli |
| 1739 | Modal | Kapat | id=accessoryKitHistoryDialog; type=button |
| 1740 | Modal | Kapat | id=accessoryKitActionDialog; type=button |
| 1741 | Modal | Kapat | id=accessoryFormDialog; type=button; name=accessoryId; başlangıçta gizli |
| 1742 | Modal | Kapat | id=accessoryCategoryManagerDialog; type=button |
| 1743 | Modal | Kapat | id=accessoryStockDialog; type=button; name=quantity |
| 1744 | Modal | Kapat | id=accessoryCheckoutDialog; type=button; name=targetType; başlangıçta gizli |
| 1745 | Modal | Kapat | id=scanAssetDialog; type=button; name=code; başlangıçta gizli |
| 1746 | Modal | Kapat | id=directoryRenameDialog; type=button; name=kind; başlangıçta gizli |
| 1747 | Modal | Kapat | id=createDepartmentDialog; type=button; name=name |
| 1748 | Modal | Kapat | id=departmentMoveDialog; type=button; name=targetDepartmentId |
| 1749 | Modal | Kapat | id=createLocationDialog; type=button; name=parentLocationId |
| 1750 | Modal | Kapat | id=locationManagerDialog; type=button; name=locationId; başlangıçta gizli |
| 1751 | Modal | Kapat | id=departmentManagerDialog; type=button; name=departmentId; başlangıçta gizli |
| 1752 | Modal | Kapat | id=quickLookupDialog; type=button; name=name; başlangıçta gizli |
| 1753 | Modal | Kapat | id=returnAssetDialog; type=button; name=note; başlangıçta gizli |
| 1754 | Modal | Kapat | id=assetTrashConfirmDialog; type=button; name=note |
| 1755 | Modal | Kapat | id=assetStatusDialog; type=button; name=note; başlangıçta gizli |
| 1756 | Modal | Kapat | id=assetAttachmentRemoveDialog; type=button; başlangıçta gizli |
| 1757 | Modal | Kapat | id=savedAssetSearchDialog; type=button; name=name; başlangıçta gizli |
| 1758 | Modal | Kapat | id=companyMfaEnrollmentDialog; type=button; name=code; başlangıçta gizli |
| 1759 | Modal | Kapat | id=supplierCatalogDialog; type=button; name=id; başlangıçta gizli |
| 1760 | Modal | Kapat | id=companySwitchDialog; type=button |
| 1761 | Modal | Kapat | id=accountPhotoDialog; type=button; hedef=/api/session/data-export; başlangıçta gizli |
| 1762 | Modal | Kapat | id=accountTwoFactorDialog; type=button; başlangıçta gizli |
| 1763 | Modal | Kapat | id=inventoryRequestDialog; type=button; name=itemType; başlangıçta gizli |
| 1764 | Modal | Kapat | id=assetTrashDialog; type=button |
| 1765 | Modal | Görsel önizleme | id=imagePreviewDialog; type=button |
| 1766 | Modal | Kapat | id=modelDetailsDialog; type=button |
| 1767 | Modal | Kapat | id=modelAttachmentsDialog; type=button; name=file; başlangıçta gizli |
| 1768 | Modal | Varlık etiketi yazdırma önizlemesi | id=assetLabelPreviewDialog; type=button |
| 1769 | Modal | Kapat | id=purchaseOrderReturnDialog; type=button; name=returnedOn |
| 1770 | Modal | Kapat | id=editAssetCustomFieldDialog; type=button; name=name |
| 1771 | Tablo | Bu sayfadaki seçilebilir varlıkları seç | id=selectAllAssetsHeader; type=checkbox |
| 1772 | Tablo | Şirket çalışan kayıtları | id=employeeRows |
| 1773 | Tablo | directoryHead | id=directoryHead |
| 1774 | Tablo | Şirket birimleri | id=companyUnitRows |
| 1775 | Tablo | Şirket aksesuar stokları | id=accessoryRows |
| 1776 | Tablo | Aksesuar satın alma siparişleri | id=accessoryOrderRows |
| 1777 | Tablo | Şirketin aksesuar hazır kitleri | id=accessoryKitRows |
| 1778 | Tablo | Sarf malzemeleri ve kullanılabilir stok miktarları | id=consumableRows |
| 1779 | Tablo | Kalem / Tür / Konum / Kalan / | id=requestCatalogRows |
| 1780 | Tablo | Çalışan / talep eden / Kalem / Adet / Durum / İşlem | id=requestRows |
| 1781 | Tablo | Lisans / Yayıncı / sürüm / Tür / Kullanılan koltuk / Bitiş tarihi / Yenileme / İşlem | id=licenseRows |
| 1782 | Tablo | Lisans / Çalışan / varlık / Personel no / Atanma tarihi / Bitiş tarihi / Durum / İşlem | id=licenseAssignmentRows |
| 1783 | Tablo | Seç / Bileşen / Kategori / model / Seri / ürün no / Toplam / Atanmış / Kullanılabilir / Minimum / Garanti bitişi / İşlem | id=componentRows |
| 1784 | Tablo | Şirket stok hareketleri | id=inventoryMovementRows |
| 1785 | Tablo | Kullanıcı / E-posta / Çalışan profili / Rol / Üyelik tarihi / İşlem | id=membersRows |
| 1786 | Tablo | Ad / İlişki / Kullanım / İşlemler | id=catalogRelationHead |
| 1787 | Tablo | Şirketin aktif varlık zimmetleri | id=assignmentRows |
| 1788 | Tablo | Beklenen ve geciken varlık iadeleri | id=returnsRows |
| 1789 | Tablo | Tarih / Varlık / İşlem / Çalışan / İşlemi yapan / Açıklama | id=activityTableRows |
| 1790 | Tablo | Varlık / Çalışan / Kullanım dönemi / Durum / Talep eden / Not / karar / İşlemler | id=reservationRows |
| 1791 | Tablo | Plan tarihi / Etiket / Varlık / Konum / Zimmetli çalışan / Durum / | id=auditScheduleDateHeading |
| 1792 | Tablo | Tarih / İşlemi yapan / Kayıt türü / İşlem / Ayrıntı | id=auditRows |
| 1793 | Tablo | Şirket sözleşmeleri | id=contractRows |
| 1794 | Tablo | Şirket görevleri | id=taskRows |
| 1795 | Tablo | Destek talepleri | id=supportRows |
| 1796 | Tablo | Kullanıcı API anahtarları | id=integrationTokenRows |
| 1797 | Tablo | Şirket bakım kayıtları | id=maintenanceRows |
| 1798 | Tablo | Şirket bakım, denetim, lisans, garanti, iade ve rezervasyon takvimi | id=calendarRows |
| 1799 | Tablo | Durum / Varlık / İşlem | id=reportStatusRows |
| 1800 | Tablo | Para birimi / Kayıt / Toplam alış bedeli | id=reportCostRows |
| 1801 | Tablo | Kategori / Toplam / Atanmış / Kullanılabilir / Bakımda / İşlem | id=reportCategoryRows |
| 1802 | Tablo | Alış tarihi / Etiket / Varlık / Seri no / Kategori / Durum / Bedel / Konum | id=assetPurchaseReportRows |
| 1803 | Tablo | Kategori / Bileşen / Toplam adet / Atanmış adet / Kullanılabilir adet / Düşük stok kaydı | id=componentReportRows |
| 1804 | Tablo | Tarih / Sonuç / Etiket / Varlık / Konum / Denetimi yapan / Açıklama / bulgu | id=auditReportRows |
| 1805 | Tablo | Garanti bitişi / Etiket / Varlık / Durum / Konum / Zimmetli çalışan | id=warrantyReportRows |
| 1806 | Tablo | Tür / Kayıt | id=maintenanceReportTypes |
| 1807 | Tablo | Para birimi / Kayıt / Toplam | id=maintenanceReportCosts |
| 1808 | Tablo | Bitiş tarihi / Lisans / Yayıncı / sürüm / Tür / Koltuk kullanımı / Otomatik yenileme / Tedarikçi | id=licenseReportRows |
| 1809 | Tablo | Şirketin bekleyen satın alma siparişi kalemleri | id=pendingDeliveryRows |
| 1810 | Tablo | Varlıkların tahmini amortisman değerleri | id=depreciationReportRows |
| 1811 | Tablo | Sarf ve bileşen satın alma siparişleri | id=stockOrderRows |
| 1812 | Tablo | Tüm şirket satın alma kayıtları | id=purchaseOverviewRows |
| 1813 | Tablo | Varlık satın alma siparişleri | id=assetOrderRows |
| 1814 | Tablo | roleAccessCaption | id=roleAccessCaption |
| 1815 | Tablo | Aksesuar / Kalan / verilen / Teslim / Not | id=assetAccessoryAssignments |
| 1816 | Tablo | Bileşen / Kalan / verilen / Takılma / Not | id=assetComponentAllocations |
| 1817 | Tablo | Lisans / Tür / Bitiş / Atanma / Not | id=assetLicenseAssignments |
| 1818 | Tablo | İlişki / Etiket / Varlık / Durum | id=assetLinkedRows |
| 1819 | Tablo | Çalışan / Departman / Zimmet | id=employeeDirectReports |
| 1820 | Tablo | Varlık / Durum / Konum | id=employeeAssetRows |
| 1821 | Tablo | Aksesuar / Verilen / İade / Kalan / Tarih | id=employeeAccessoryRows |
| 1822 | Tablo | Lisans / Yayıncı / sürüm / Atandığı tarih / Bitiş | id=employeeLicenseRows |
| 1823 | Tablo | Malzeme / Miktar / Tarih / Not | id=employeeConsumableRows |
| 1824 | Tablo | Varlık / Bakım türü / Servis / Durum / Tarih | id=employeeMaintenanceRows |
| 1825 | Tablo | Sipariş no / Tedarikçi / tarih / Adet / Durum | id=accessoryOrderHistoryRows |
| 1826 | Tablo | Çalışan / Verilen / İade / Kalan / İşlem | id=accessoryAssignments |
| 1827 | Tablo | Aksesuar / Sipariş / Teslim alınan / Kalan / Fatura birim bedeli / Edinim birim bedeli / Toplam | id=accessoryOrderDetailLines |
| 1828 | Tablo | Kalem / Sipariş / Teslim alınan / Kalan / Fatura birim bedeli / Edinim birim bedeli / Toplam | id=stockOrderDetailLines |
| 1829 | Tablo | Cihaz / Kategori / Hedef konum / Sipariş / Teslim alındı / Kalan / Fatura birim bedeli / Edinim birim bedeli | id=assetOrderDetailLines |
| 1830 | Tablo | Etiket / cihaz / Durum / Seri numarası / Alış tarihi | id=assetOrderReceivedAssets |
| 1831 | Tablo | Tarih / İşlem / Miktar / Çalışan / İşlemi yapan / Not | id=consumableMovements |
| 1832 | Tablo | Tedarikçi / Sipariş no. / Tarih / Koltuk / Birim maliyeti / Durum / İşlemler | id=licensePurchaseOrderRows |
| 1833 | Tablo | Çalışan / varlık / Personel no / Atanma / EULA sürümü / EULA teyit zamanı / Teyit eden / Durum / İşlem | id=licenseAssignments |
| 1834 | Tablo | Varlık / Verilen / Çıkarılan / Kalan / Tarih / İşlem | id=componentAllocations |
| 1835 | Tablo | Tarih / Kalem / Teslim edilen / İade edilen / Kalan / Hedef / İşlemi yapan / Not | id=accessoryKitHistoryRows |
| 1836 | Tablo | Kategori / Aksesuar / Durum / İşlemler | id=accessoryCategoryRows |
| 1837 | Tablo | Firma / Yetkili / iletişim / Bağlı kayıt / Durum / İşlemler | id=supplierCatalogRows |
| 1838 | Tablo | Etiket / Varlık / Seri numarası / Taşınma zamanı / İşlem | id=assetTrashRows |
| 1839 | Tablo | Etiket / Varlık / Durum / Seri no / Çalışan / konum | id=modelDetailsAssets |
| 1840 | Tablo | Sipariş / Tedarikçi / Durum / Tarih / Adet / Teslim alınan | id=modelDetailsOrders |
| 1841 | Tablo | Tarih / İşlem / Açıklama | id=modelDetailsHistory |
| 1842 | Tablo | Dosya / Tür / Boyut / Yüklenme / İşlem | id=modelAttachmentRows |
| 1843 | Tablo | (etiketsiz) | başlangıçta gizli |
| 1844 | Tablo | Etiket / Varlık / Seri no / Durum / Konum |  |
| 1845 | Tablo | Aksesuar / Model / Kalan adet / Teslim tarihi |  |
| 1846 | Tablo | Lisans / EULA sürümü / Dosya / ek kimliği / Kabul eden / tarih / EULA SHA-256 / Durum |  |
| 1847 | Tablo | ${escapeHtml(translateUiText('Uygulama'))} / ${escapeHtml(translateUiText('Yayıncı'))} / ${escapeHtml(translateUiText('Sürüm'))} / ${escapeHtml(translateUiText('Mimari'))} / ${escapeHtml(translateUiText('Kurulum tarihi'))} / ${escapeHtml(translateUiText('Son görülme'))} | id=assetSoftwareInventoryRows |
| 1848 | Tablo | Alan / Anahtar / Grup / Kapsam / Tür / Zorunlu / Durum / İşlem | id=assetCustomFieldRows |
| 1849 | Sekme | assetTabOverviewButton | id=assetTabOverviewButton; type=button |
| 1850 | Sekme | assetTabRelationsButton | id=assetTabRelationsButton; type=button |
| 1851 | Sekme | assetTabMaintenanceButton | id=assetTabMaintenanceButton; type=button |
| 1852 | Sekme | assetTabHistoryButton | id=assetTabHistoryButton; type=button |
| 1853 | Sekme | assetTabAttachmentsButton | id=assetTabAttachmentsButton; type=button |
| 1854 | Sayfa rotası | Genel Bakış | hedef=Genel Bakış |
| 1855 | Sayfa rotası | Varlıklar | hedef=Varlıklar |
| 1856 | Sayfa rotası | Tüm satın almalar | hedef=Tüm satın almalar |
| 1857 | Sayfa rotası | Varlık siparişleri | hedef=Varlık siparişleri |
| 1858 | Sayfa rotası | Varlık modelleri | hedef=Varlık modelleri |
| 1859 | Sayfa rotası | Markalar | hedef=Markalar |
| 1860 | Sayfa rotası | Kategoriler | hedef=Kategoriler |
| 1861 | Sayfa rotası | Durum etiketleri | hedef=Durum etiketleri |
| 1862 | Sayfa rotası | QR ve barkodlar | hedef=QR ve barkodlar |
| 1863 | Sayfa rotası | Zimmetler | hedef=Zimmetler |
| 1864 | Sayfa rotası | İadeler | hedef=İadeler |
| 1865 | Sayfa rotası | Transferler | hedef=Transferler |
| 1866 | Sayfa rotası | Rezervasyonlar | hedef=Rezervasyonlar |
| 1867 | Sayfa rotası | Hareket geçmişi | hedef=Hareket geçmişi |
| 1868 | Sayfa rotası | Yazılım lisansları | hedef=Yazılım lisansları |
| 1869 | Sayfa rotası | Lisans atamaları | hedef=Lisans atamaları |
| 1870 | Sayfa rotası | Lisanslarım | hedef=Lisanslarım |
| 1871 | Sayfa rotası | Sözleşmeler | hedef=Sözleşmeler |
| 1872 | Sayfa rotası | Garanti takibi | hedef=Garanti takibi |
| 1873 | Sayfa rotası | Aksesuarlar | hedef=Aksesuarlar |
| 1874 | Sayfa rotası | Siparişler | hedef=Siparişler |
| 1875 | Sayfa rotası | Sarf malzemeleri | hedef=Sarf malzemeleri |
| 1876 | Sayfa rotası | Stok talepleri | hedef=Stok talepleri |
| 1877 | Sayfa rotası | Bileşenler | hedef=Bileşenler |
| 1878 | Sayfa rotası | Hazır kitler | hedef=Hazır kitler |
| 1879 | Sayfa rotası | Stok hareketleri | hedef=Stok hareketleri |
| 1880 | Sayfa rotası | Çalışan kayıtları | hedef=Çalışan kayıtları |
| 1881 | Sayfa rotası | Uygulama kullanıcıları | id=membersNav; hedef=Uygulama kullanıcıları |
| 1882 | Sayfa rotası | Departmanlar | hedef=Departmanlar |
| 1883 | Sayfa rotası | Yetki rolleri | hedef=Yetki rolleri |
| 1884 | Sayfa rotası | Lokasyonlar | hedef=Lokasyonlar |
| 1885 | Sayfa rotası | Binalar ve odalar | hedef=Binalar ve odalar |
| 1886 | Sayfa rotası | Şirket birimleri | hedef=Şirket birimleri |
| 1887 | Sayfa rotası | Bakım kayıtları | hedef=Bakım kayıtları |
| 1888 | Sayfa rotası | Bakım planları | hedef=Bakım planları |
| 1889 | Sayfa rotası | Denetimler | hedef=Denetimler |
| 1890 | Sayfa rotası | Takvim | hedef=Takvim |
| 1891 | Sayfa rotası | Görevler | hedef=Görevler |
| 1892 | Sayfa rotası | Hazır raporlar | hedef=Hazır raporlar |
| 1893 | Sayfa rotası | Özel rapor oluştur | hedef=Özel rapor oluştur |
| 1894 | Sayfa rotası | Dışa aktarımlar | hedef=Dışa aktarımlar |
| 1895 | Sayfa rotası | Tablodan içe aktar | hedef=Tablodan içe aktar |
| 1896 | Sayfa rotası | Verileri dışa aktar | hedef=Verileri dışa aktar |
| 1897 | Sayfa rotası | Yedekleme geçmişi | hedef=Yedekleme geçmişi |
| 1898 | Sayfa rotası | Geri yükle | hedef=Geri yükle |
| 1899 | Sayfa rotası | Firma profili | hedef=Firma profili |
| 1900 | Sayfa rotası | Logo ve tema | hedef=Logo ve tema |
| 1901 | Sayfa rotası | Bildirimler | hedef=Bildirimler |
| 1902 | Sayfa rotası | Entegrasyonlar | hedef=Entegrasyonlar |
| 1903 | Sayfa rotası | Güvenlik | hedef=Güvenlik |
| 1904 | Sayfa rotası | Denetim kayıtları | hedef=Denetim kayıtları |
| 1905 | Sayfa rotası | Destek | hedef=Destek |
| 1906 | Sayfa rotası | Varlıklar | hedef=Varlıklar |
| 1907 | Sayfa rotası | Hareket geçmişi | hedef=Hareket geçmişi |
| 1908 | Sayfa rotası | Takvim | hedef=Takvim |
| 1909 | Sayfa rotası | Hazır raporlar | hedef=Hazır raporlar |
| 1910 | Sayfa rotası | Yazılım lisansları | type=button; hedef=Yazılım lisansları |
| 1911 | Sayfa rotası | Varlıklar | type=button; hedef=Varlıklar |
| 1912 | Sayfa rotası | Varlıklar | type=button; hedef=Varlıklar |
| 1913 | Sayfa rotası | Çalışan kayıtları | type=button; hedef=Çalışan kayıtları |
| 1914 | Sayfa rotası | Aksesuarlar | type=button; hedef=Aksesuarlar |
| 1915 | Sayfa rotası | Sarf malzemeleri | type=button; hedef=Sarf malzemeleri |
| 1916 | Sayfa rotası | Bileşenler | type=button; hedef=Bileşenler |
| 1917 | Sayfa rotası | Yazılım lisansları | type=button; hedef=Yazılım lisansları |
| 1918 | Sayfa rotası | Genel Bakış | type=button; hedef=Genel Bakış |
| 1919 | Menü grubu | inventory | hedef=inventory |
| 1920 | Menü grubu | movement | hedef=movement |
| 1921 | Menü grubu | software | hedef=software |
| 1922 | Menü grubu | supplies | hedef=supplies |
| 1923 | Menü grubu | people | hedef=people |
| 1924 | Menü grubu | places | hedef=places |
| 1925 | Menü grubu | operations | hedef=operations |
| 1926 | Menü grubu | reports | hedef=reports |
| 1927 | Menü grubu | data | hedef=data |
| 1928 | Menü grubu | settings | hedef=settings |

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
| 30 | Düğme | ＋ Demo firma oluştur | id=provisionDemoCompany |
| 31 | Düğme | ＋ Ücretli firma oluştur | id=provisionCompany |
| 32 | Düğme | Uygula | id=searchCompanies |
| 33 | Düğme | Seçilenleri askıya al | id=bulkSuspendCompanies; type=button; başlangıçta gizli |
| 34 | Düğme | Seçilenleri etkinleştir | id=bulkReactivateCompanies; type=button; başlangıçta gizli |
| 35 | Düğme | Seçilenlerin planını değiştir | id=bulkChangeCompanyPlan; type=button; başlangıçta gizli |
| 36 | Düğme | Filtrelenmiş şirket listesini CSV olarak indir | id=exportCompanies; type=button |
| 37 | Düğme | ← Önceki | id=prevCompanies |
| 38 | Düğme | Sonraki → | id=nextCompanies |
| 39 | Düğme | Yenile | id=refreshOrders |
| 40 | Düğme | Ara | id=searchOrders |
| 41 | Düğme | ← Önceki | id=prevOrders |
| 42 | Düğme | Sonraki → | id=nextOrders |
| 43 | Düğme | Yıllık paketleri hazırla | id=openAnnualCatalog; type=button |
| 44 | Düğme | ＋ Yeni Plan | id=newPlan |
| 45 | Düğme | Kuponu kaydet | type=submit |
| 46 | Düğme | Temizle | id=clearCoupon; type=button |
| 47 | Düğme | Formu Temizle | id=clearPlan; type=button |
| 48 | Düğme | Planı Kaydet | type=submit |
| 49 | Düğme | Yeni içerik | id=clearContent; type=button |
| 50 | Düğme | İçeriği Kaydet | type=submit |
| 51 | Düğme | Yenile | id=refreshAudit |
| 52 | Düğme | Süreyi kaydet | id=saveAuditRetention |
| 53 | Düğme | Silinecek kayıtları önizle | id=previewAuditRetention |
| 54 | Düğme | Önizlenen eski kayıtları sil | id=applyAuditRetention |
| 55 | Düğme | Filtrele | id=searchAudit |
| 56 | Düğme | Temizle | id=clearAudit |
| 57 | Düğme | ← Önceki | id=prevAudit |
| 58 | Düğme | Sonraki → | id=nextAudit |
| 59 | Düğme | + Üye davet et | id=openMemberInvite |
| 60 | Düğme | Yenile | id=refreshMembers |
| 61 | Düğme | Ara | id=searchMembers |
| 62 | Düğme | ← Önceki | id=prevMembers |
| 63 | Düğme | Sonraki → | id=nextMembers |
| 64 | Düğme | Kapat | id=closeMemberDetail |
| 65 | Düğme | Yenile | id=refreshDemoLeads |
| 66 | Düğme | Ara | id=searchDemoLeads |
| 67 | Düğme | ← Önceki | id=prevDemoLeads |
| 68 | Düğme | Sonraki → | id=nextDemoLeads |
| 69 | Düğme | Kapat | id=closeDemoLeadDetail |
| 70 | Düğme | Yenile | id=refreshSupport |
| 71 | Düğme | ← Önceki | id=prevSupport |
| 72 | Düğme | Sonraki → | id=nextSupport |
| 73 | Düğme | Kapat | id=hideSupportDetail |
| 74 | Düğme | Yanıt gönder | id=supportAdminReplyButton; type=submit |
| 75 | Düğme | Durumu kaydet | type=submit |
| 76 | Düğme | ✕ | type=button |
| 77 | Düğme | Vazgeç | type=button |
| 78 | Düğme | İşlemi Uygula | type=submit |
| 79 | Düğme | Kapat | id=platformRecoveryClose; type=button |
| 80 | Düğme | İptal | id=platformRecoveryCancel; type=button |
| 81 | Düğme | Bağlantı iste | id=platformRecoverySubmit; type=submit |
| 82 | Düğme | Kapat | id=closeAnnualCatalog; type=button |
| 83 | Düğme | İptal | id=cancelAnnualCatalog; type=button |
| 84 | Düğme | Taslakları oluştur | id=applyAnnualCatalog; type=submit |
| 85 | Düğme | Kapat | id=platformReauthClose; type=button |
| 86 | Düğme | İptal | id=platformReauthCancel; type=button |
| 87 | Düğme | Doğrula ve devam et | id=platformReauthSubmit; type=submit |
| 88 | Düğme | Vazgeç | id=impersonationCancel; type=button |
| 89 | Düğme | Panele gir | id=impersonationSubmit; type=submit |
| 90 | Düğme | ✕ | type=button |
| 91 | Düğme | Kapat | type=button |
| 92 | Düğme | Kapat | id=closeMemberInvite; type=button |
| 93 | Düğme | İptal | id=cancelMemberInvite; type=button |
| 94 | Düğme | Davet gönder | id=submitMemberInvite; type=submit |
| 95 | Düğme | Kapat | id=closePaymentFollowUp; type=button |
| 96 | Düğme | Vazgeç | id=cancelPaymentFollowUp; type=button |
| 97 | Düğme | Takip kaydını ekle | type=submit |
| 98 | Düğme | Hesabı aç | type=button |
| 99 | Düğme | Aç | id=${esc(x.id)} |
| 100 | Düğme | Takip kaydı |  |
| 101 | Düğme | Detay | id=${c.id}; action=details |
| 102 | Düğme | Geri al |  |
| 103 | Düğme | ${c.isActive?'Askıya al':'Etkinleştir'} | id=${c.id}; action=toggle |
| 104 | Düğme | Demo | id=${c.id}; action=demo |
| 105 | Düğme | Uzat | id=${c.id}; action=extend |
| 106 | Düğme | Sil |  |
| 107 | Düğme | Yaşam döngüsünü kaydet | type=submit |
| 108 | Düğme | ${esc(m.displayName)} | name=${esc(m.displayName)} |
| 109 | Düğme | Önceki sayfa |  |
| 110 | Düğme | =d.membersTotalPages?'disabled':''}>Sonraki sayfa |  |
| 111 | Düğme | Yetkili erişim e-postasını yeniden gönder |  |
| 112 | Düğme | Düzenle |  |
| 113 | Düğme | Düzenle |  |
| 114 | Düğme | Düzenle |  |
| 115 | Düğme | Aç |  |
| 116 | Düğme | Kapat | type=button |
| 117 | Düğme | Vazgeç | type=button |
| 118 | Düğme | ${restore?'Geri al':'Çöp kutusuna taşı'} | type=submit |
| 119 | Girdi alanı | email | type=email; name=email |
| 120 | Girdi alanı | password | type=password; name=password |
| 121 | Girdi alanı | twoFactorCode | type=text; name=twoFactorCode |
| 122 | Girdi alanı | rememberMe | type=checkbox; name=rememberMe |
| 123 | Girdi alanı | currentPassword | type=password; name=currentPassword |
| 124 | Girdi alanı | platformMfaSecret | id=platformMfaSecret |
| 125 | Girdi alanı | platformMfaCode | id=platformMfaCode |
| 126 | Girdi alanı | platformMfaSavedRecovery | id=platformMfaSavedRecovery; type=checkbox |
| 127 | Girdi alanı | Firma veya e-posta ara (Ctrl+K) | id=globalSearch |
| 128 | Girdi alanı | companySearch | id=companySearch |
| 129 | Seçim alanı | (değer yok): Tüm durumlar, true: Aktif, false: Askıdaki | id=companyActive; seçenekler=(değer yok): Tüm durumlar, true: Aktif, false: Askıdaki |
| 130 | Seçim alanı | Firma kayıtları | id=companyDeleted; seçenekler=false: Güncel firmalar, true: Çöp kutusu |
| 131 | Seçim alanı | Paket filtresi | id=companyPlan; seçenekler=(değer yok): Tüm paketler |
| 132 | Girdi alanı | Kayıt tarihi başlangıcı | id=companyCreatedFrom; type=date |
| 133 | Girdi alanı | Kayıt tarihi bitişi | id=companyCreatedTo; type=date |
| 134 | Seçim alanı | Sıralama | id=companySort; seçenekler=name:asc: Firma adı A–Z, name:desc: Firma adı Z–A, createdAt:desc: En yeni kayıt, createdAt:asc: En eski kayıt, status:asc: Durum: askıdaki önce, status:desc: Durum: aktif önce, followUp:asc: Takip tarihi artan, followUp:desc: Takip tarihi azalan |
| 135 | Seçim alanı | Sayfa başına firma | id=companyPageSize; seçenekler=50: 50 / sayfa, 100: 100 / sayfa, 250: 250 / sayfa |
| 136 | Girdi alanı | orderSearch | id=orderSearch |
| 137 | Seçim alanı | (değer yok): Tüm durumlar, Provisioned: Şirket açıldı, PaymentStarted: Ödeme başladı, PaymentReview: İnceleme bekliyor, PaymentFailed: Ödeme başarısız, ProvisioningFailed: Şirket açılamadı, Expired: Süresi doldu, Pending: Bekliyor | id=orderStatus; seçenekler=(değer yok): Tüm durumlar, Provisioned: Şirket açıldı, PaymentStarted: Ödeme başladı, PaymentReview: İnceleme bekliyor, PaymentFailed: Ödeme başarısız, ProvisioningFailed: Şirket açılamadı, Expired: Süresi doldu, Pending: Bekliyor |
| 138 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 139 | Girdi alanı | code | name=code |
| 140 | Seçim alanı | discountType | name=discountType; seçenekler=Percent: Yüzde, Fixed: Sabit tutar |
| 141 | Girdi alanı | discountValue | type=number; name=discountValue |
| 142 | Girdi alanı | currency | name=currency |
| 143 | Seçim alanı | planId | name=planId; seçenekler=(değer yok): Tüm Cloud planları |
| 144 | Girdi alanı | startsAt | type=datetime-local; name=startsAt |
| 145 | Girdi alanı | endsAt | type=datetime-local; name=endsAt |
| 146 | Girdi alanı | maxRedemptions | type=number; name=maxRedemptions |
| 147 | Girdi alanı | maxRedemptionsPerCustomer | type=number; name=maxRedemptionsPerCustomer |
| 148 | Seçim alanı | isActive | name=isActive; seçenekler=true: Etkin, false: Pasif |
| 149 | Metin alanı | changeReason | name=changeReason |
| 150 | Metin alanı | changeReason | name=changeReason |
| 151 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 152 | Girdi alanı | code | name=code |
| 153 | Girdi alanı | name | name=name |
| 154 | Seçim alanı | deploymentKind | name=deploymentKind; seçenekler=cloud: Bulut, local-windows: Windows 11 Yerel, windows-server: Windows Server, linux: Linux |
| 155 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): EUR, (değer yok): USD, (değer yok): GBP, (değer yok): RSD, (değer yok): MKD, (değer yok): UAH, (değer yok): RUB, (değer yok): CHF |
| 156 | Girdi alanı | maxLoginUsers | id=planMaxLoginUsers; type=number; name=maxLoginUsers |
| 157 | Girdi alanı | maxAssets | type=number; name=maxAssets |
| 158 | Girdi alanı | monthlyPrice | type=number; name=monthlyPrice |
| 159 | Girdi alanı | annualPrice | type=number; name=annualPrice |
| 160 | Girdi alanı | oneTimePrice | type=number; name=oneTimePrice |
| 161 | Girdi alanı | offlineGraceDays | type=number; name=offlineGraceDays |
| 162 | Metin alanı | description | name=description |
| 163 | Metin alanı | featureFlagsJson | name=featureFlagsJson |
| 164 | Girdi alanı | planReportsEnabled | id=planReportsEnabled; type=checkbox |
| 165 | Girdi alanı | planApiEnabled | id=planApiEnabled; type=checkbox |
| 166 | Girdi alanı | planMaintenanceEnabled | id=planMaintenanceEnabled; type=checkbox |
| 167 | Girdi alanı | supportsOfflineUse | type=checkbox; name=supportsOfflineUse |
| 168 | Girdi alanı | isPublic | type=checkbox; name=isPublic |
| 169 | Girdi alanı | isActive | type=checkbox; name=isActive |
| 170 | Girdi alanı | sortOrder | type=number; name=sortOrder |
| 171 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 172 | Girdi alanı | key | name=key |
| 173 | Girdi alanı | title | name=title |
| 174 | Metin alanı | body | name=body |
| 175 | Girdi alanı | mediaPath | name=mediaPath |
| 176 | Girdi alanı | isPublished | type=checkbox; name=isPublished |
| 177 | Girdi alanı | auditRetentionDays | id=auditRetentionDays; type=number |
| 178 | Girdi alanı | auditRetentionConfirmation | id=auditRetentionConfirmation; type=text |
| 179 | Girdi alanı | auditSearch | id=auditSearch; type=search |
| 180 | Seçim alanı | (değer yok): Tüm kaynaklar, platform: Platform, company: Şirket | id=auditSource; seçenekler=(değer yok): Tüm kaynaklar, platform: Platform, company: Şirket |
| 181 | Girdi alanı | auditAction | id=auditAction |
| 182 | Girdi alanı | auditTargetType | id=auditTargetType |
| 183 | Girdi alanı | auditFrom | id=auditFrom; type=date |
| 184 | Girdi alanı | auditTo | id=auditTo; type=date |
| 185 | Girdi alanı | memberSearch | id=memberSearch |
| 186 | Girdi alanı | demoLeadSearch | id=demoLeadSearch |
| 187 | Seçim alanı | (değer yok): Tüm durumlar, New: Yeni, Contacted: İletişime geçildi, Qualified: Nitelikli, DemoScheduled: Demo planlandı, DemoGranted: Demo açıldı, Converted: Müşteri oldu, Closed: Kapalı | id=demoLeadStatus; seçenekler=(değer yok): Tüm durumlar, New: Yeni, Contacted: İletişime geçildi, Qualified: Nitelikli, DemoScheduled: Demo planlandı, DemoGranted: Demo açıldı, Converted: Müşteri oldu, Closed: Kapalı |
| 188 | Seçim alanı | Takip tarihi filtresi | id=demoLeadFollowUp; seçenekler=(değer yok): Tüm takipler, overdue: Takibi gecikenler |
| 189 | Girdi alanı | supportSearch | id=supportSearch |
| 190 | Seçim alanı | (değer yok): Tüm durumlar, Open: Açık, InProgress: İşlemde, AwaitingCustomer: Müşteri yanıtı bekleniyor, Resolved: Çözüldü, Closed: Kapatıldı | id=supportStatus; seçenekler=(değer yok): Tüm durumlar, Open: Açık, InProgress: İşlemde, AwaitingCustomer: Müşteri yanıtı bekleniyor, Resolved: Çözüldü, Closed: Kapatıldı |
| 191 | Metin alanı | body | name=body |
| 192 | Seçim alanı | status | name=status; seçenekler=Open: Açık, InProgress: İşlemde, AwaitingCustomer: Müşteri yanıtı bekleniyor, Resolved: Çözüldü, Closed: Kapatıldı |
| 193 | Girdi alanı | email | type=email; name=email |
| 194 | Metin alanı | changeReason | name=changeReason |
| 195 | Girdi alanı | password | type=password; name=password |
| 196 | Girdi alanı | code | type=text; name=code |
| 197 | Metin alanı | reason | name=reason |
| 198 | Girdi alanı | memberInviteCompanySearch | id=memberInviteCompanySearch; type=search |
| 199 | Seçim alanı | companyId | id=memberInviteCompany; name=companyId; seçenekler=(değer yok): Önce şirketleri yükleyin |
| 200 | Girdi alanı | fullName | name=fullName |
| 201 | Girdi alanı | email | type=email; name=email |
| 202 | Seçim alanı | roleKey | name=roleKey; seçenekler=viewer: Salt okunur, asset_operator: Varlık sorumlusu, asset_manager: Varlık yöneticisi, auditor: Denetçi, hr_operator: Ekip yöneticisi, admin: Şirket yöneticisi |
| 203 | Seçim alanı | outcome | name=outcome; seçenekler=Contacted: Müşteriyle iletişim kuruldu, NoResponse: Yanıt alınamadı, ReminderScheduled: Hatırlatma planlandı, CustomerRequestedCallback: Müşteri geri arama istedi, Other: Diğer |
| 204 | Girdi alanı | nextFollowUpAt | type=datetime-local; name=nextFollowUpAt |
| 205 | Metin alanı | note | name=note |
| 206 | Girdi alanı | Bu sayfadaki tüm firmaları seç | type=checkbox |
| 207 | Girdi alanı | ${esc(c.name)} firmasını seç | type=checkbox |
| 208 | Seçim alanı | stage | name=stage |
| 209 | Seçim alanı | assignedAdminUserId | name=assignedAdminUserId; seçenekler=(değer yok): Atanmamış |
| 210 | Girdi alanı | nextFollowUpAt | type=datetime-local; name=nextFollowUpAt |
| 211 | Metin alanı | note | name=note |
| 212 | Metin alanı | note | name=note |
| 213 | Seçim alanı | planId | name=planId; seçenekler=${esc(plan.id)}: ${esc(plan.name)} · ${esc(plan.code)} |
| 214 | Metin alanı | note | name=note |
| 215 | Metin alanı | note | name=note |
| 216 | Seçim alanı | planId | name=planId; seçenekler=${esc(p.id)}: ${esc(p.name)} — ${esc(p.deploymentKind)} |
| 217 | Girdi alanı | days | type=number; name=days |
| 218 | Girdi alanı | note | name=note |
| 219 | Girdi alanı | companyName | name=companyName |
| 220 | Girdi alanı | slug | name=slug |
| 221 | Girdi alanı | contactEmail | type=email; name=contactEmail |
| 222 | Girdi alanı | ownerName | name=ownerName |
| 223 | Girdi alanı | ownerEmail | type=email; name=ownerEmail |
| 224 | Seçim alanı | planId | name=planId; seçenekler=${esc(p.id)}: ${esc(p.name)} · ${esc(p.code)} |
| 225 | Girdi alanı | termDays | type=number; name=termDays |
| 226 | Metin alanı | note | name=note |
| 227 | Girdi alanı | paymentReference | name=paymentReference |
| 228 | Girdi alanı | paymentVerified | type=checkbox; name=paymentVerified |
| 229 | Girdi alanı | endsAt | type=datetime-local; name=endsAt |
| 230 | Girdi alanı | note | name=note |
| 231 | Girdi alanı | confirmSlug | name=confirmSlug |
| 232 | Metin alanı | note | name=note |
| 233 | Form | loginForm | id=loginForm |
| 234 | Form | platformMfaSetupForm | id=platformMfaSetupForm |
| 235 | Form | couponForm | id=couponForm |
| 236 | Form | planForm | id=planForm |
| 237 | Form | contentForm | id=contentForm |
| 238 | Form | supportAdminReply | id=supportAdminReply |
| 239 | Form | supportAdminStatus | id=supportAdminStatus |
| 240 | Form | actionForm | id=actionForm |
| 241 | Form | platformRecoveryForm | id=platformRecoveryForm |
| 242 | Form | annualCatalogForm | id=annualCatalogForm |
| 243 | Form | platformReauthForm | id=platformReauthForm |
| 244 | Form | impersonationForm | id=impersonationForm |
| 245 | Form | memberInviteForm | id=memberInviteForm |
| 246 | Form | paymentFollowUpForm | id=paymentFollowUpForm |
| 247 | Form | companyLifecycleForm | id=companyLifecycleForm |
| 248 | Form | companyDeletionForm | id=companyDeletionForm |
| 249 | Modal | İşlem | id=actionDialog; type=button |
| 250 | Modal | Kapat | id=platformRecoveryDialog; type=button; name=email |
| 251 | Modal | Kapat | id=annualCatalogDialog; type=button; name=changeReason |
| 252 | Modal | Kapat | id=platformReauthDialog; type=button; name=password |
| 253 | Modal | reason | id=impersonationDialog; type=button; name=reason |
| 254 | Modal | Firma ayrıntısı | id=companyDetailDialog; type=button |
| 255 | Modal | companyId | id=memberInviteDialog; type=button; name=companyId |
| 256 | Modal | Kapat | id=paymentFollowUpDialog; type=button; name=outcome |
| 257 | Tablo | ${x} |  |

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
| 7 | Düğme | Doğrulama bağlantısı gönder | id=sendEmailVerification; type=button |
| 8 | Düğme | Bu e-postayı doğrula | id=confirmEmailVerification; type=button; başlangıçta gizli |
| 9 | Düğme | E-postayı değiştir | id=changeVerifiedEmail; type=button; başlangıçta gizli |
| 10 | Düğme | Yeni ödeme denemesi oluştur | id=newAttempt; type=button |
| 11 | Düğme | Güvenli ödemeye geç | id=checkoutSubmit; type=submit |
| 12 | Girdi alanı | couponCode | id=couponCode; type=text |
| 13 | Girdi alanı | companyName | name=companyName |
| 14 | Girdi alanı | slug | name=slug |
| 15 | Girdi alanı | ownerFirstName | name=ownerFirstName |
| 16 | Girdi alanı | ownerLastName | name=ownerLastName |
| 17 | Girdi alanı | ownerEmail | type=email; name=ownerEmail |
| 18 | Girdi alanı | phone | type=tel; name=phone |
| 19 | Girdi alanı | identityNumber | name=identityNumber |
| 20 | Girdi alanı | zipCode | name=zipCode |
| 21 | Girdi alanı | billingAddress | name=billingAddress |
| 22 | Girdi alanı | city | name=city |
| 23 | Girdi alanı | country | name=country |
| 24 | Girdi alanı | dataProcessingAcknowledged | type=checkbox; name=dataProcessingAcknowledged |
| 25 | Form | checkoutForm | id=checkoutForm |

### Deneme kaydı — `src/EnvanterTR.PublicSite/trial.html`

| # | Tür | Etiket / görünen metin | Kaynak ipucu |
|---:|---|---|---|
| 1 | Bağlantı | EnvanterTR | hedef=/ |
| 2 | Bağlantı | Parolanızı belirleyin | id=setupLink; başlangıçta gizli |
| 3 | Bağlantı | Şirket portalına girin | id=portalLink |
| 4 | Bağlantı | Ana sayfaya dön | hedef=/ |
| 5 | Düğme | Doğrulama bağlantısı gönder | id=requestTrial; type=submit |
| 6 | Düğme | E-postamı doğrula ve denemeyi başlat | id=confirmTrial; type=submit |
| 7 | Seçim alanı | Language | id=language; seçenekler=tr: Türkçe, en: English, ru: Русский, uk: Українська, mk: Македонски |
| 8 | Girdi alanı | companyName | name=companyName |
| 9 | Girdi alanı | slug | name=slug |
| 10 | Girdi alanı | ownerName | name=ownerName |
| 11 | Girdi alanı | email | type=email; name=email |
| 12 | Seçim alanı | planId | name=planId |
| 13 | Girdi alanı | consent | type=checkbox; name=consent |
| 14 | Form | registration | id=registration |
| 15 | Form | confirmation | id=confirmation; başlangıçta gizli |

## Kapsam notları

- `data-page` ve `data-group` kayıtları yönlendirme hedefi/menü grubu envanteridir; aynı HTML elemanı bağlantı veya düğme olarak ayrıca listelenebilir.
- Form alanları için `id`, `name`, tür ve varsa select seçenekleri kaynak ipucu olarak kaydedilir. Etiket ilişkisi tek başına bu statik dökümle doğrulanmış sayılmaz.
- Açılır menüler, koşullu alanlar ve JavaScript tarafından üretilen tablo satırı/eylemleri kaynakta statik değillerse bu sayımda yer almayabilir.
- Fonksiyon kabul sonuçları `docs/UX-MENU-ACTION-AUDIT.md` ve `docs/USER-REQUEST-CHECKLIST-2026-10-11.md` içinde kanıt ve açık kalan kapsamla izlenir.
