# EnvanterTR statik arayüz envanteri

Oluşturulma: 2026-11-07T21:49:50.022Z

> Bu dosya mevcut HTML kaynaklarında bulunan statik arayüz öğelerinin dökümüdür. Bir öğenin listelenmesi onun çalıştığını kanıtlamaz. JavaScript ile sonradan oluşturulan kontroller, API/DB davranışı, rol varyasyonları ve erişilebilirlik ayrıca uçtan uca kabul gerektirir. Kaynak değiştikçe `node tools/Generate-UiInventory.mjs` ile yeniden üretin; `node tools/Generate-UiInventory.mjs --check` güncel olup olmadığını doğrular.

## Sayım

| Tür | Statik öğe |
|---|---:|
| Sayfa rotası | 65 |
| Menü grubu | 10 |
| Bağlantı | 54 |
| Düğme | 976 |
| Form | 114 |
| Girdi alanı | 634 |
| Seçim alanı | 207 |
| Metin alanı | 42 |
| Modal | 109 |
| Sekme | 5 |
| Tablo | 80 |
| **Toplam kayıt** | **2296** |

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
| 10 | Bağlantı | ${escapeHtml(text)} | hedef=${escapeHtml(url.pathname + url.search)} |
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
| 132 | Düğme | Konum filtresini temizle | id=clearEmployeeLocationFilter; type=button |
| 133 | Düğme | Tarihleri temizle | id=clearEmployeeStartedDateFilter; type=button; başlangıçta gizli |
| 134 | Düğme | Sayfadakileri seç | id=selectEmployeesOnPage; type=button; başlangıçta gizli |
| 135 | Düğme | Seçilenleri güncelle | id=applyEmployeeBulkActive; type=button; başlangıçta gizli |
| 136 | Düğme | ← Önceki | id=employeePrev |
| 137 | Düğme | Sonraki → | id=employeeNext |
| 138 | Düğme | + Departman ekle | id=addDepartmentButton; type=button; başlangıçta gizli |
| 139 | Düğme | + Konum ekle | id=addLocationButton; type=button; başlangıçta gizli |
| 140 | Düğme | ↻ Yenile | id=directoryRefresh; type=button |
| 141 | Düğme | ＋ Birim ekle | id=addCompanyUnitButton; type=button; başlangıçta gizli |
| 142 | Düğme | ↻ Yenile | id=companyUnitsRefresh; type=button |
| 143 | Düğme | Kapat | id=companyUnitDialogClose; type=button |
| 144 | Düğme | İptal | id=companyUnitCancel; type=button |
| 145 | Düğme | Kaydet | type=submit |
| 146 | Düğme | Varlıkları aç | id=moduleGoAssets; type=button |
| 147 | Düğme | Çalışanları aç | id=moduleGoPeople; type=button |
| 148 | Düğme | CSV indir | id=accessoryExport; type=button |
| 149 | Düğme | Excel (.xlsx) | id=accessoryXlsxExport; type=button |
| 150 | Düğme | ↑ CSV içe aktar | id=accessoryImportOpen; type=button; başlangıçta gizli |
| 151 | Düğme | ↻ Yenile | id=accessoryRefresh; type=button |
| 152 | Düğme | Kategorileri yönet | id=manageAccessoryCategories; type=button; başlangıçta gizli |
| 153 | Düğme | ＋ Aksesuar ekle | id=newAccessoryButton; type=button |
| 154 | Düğme | Varsayılan sütunlar | id=resetAccessoryColumns; type=button |
| 155 | Düğme | Yazdır | id=accessoryPrint; type=button |
| 156 | Düğme | Tam ekran | id=accessoryFullscreen; type=button |
| 157 | Düğme | Filtreleri temizle | id=clearAccessoryAdvancedFilters; type=button |
| 158 | Düğme | ← Önceki | id=accessoryPrev; type=button |
| 159 | Düğme | Sonraki → | id=accessoryNext; type=button |
| 160 | Düğme | Sarf / bileşen siparişleri | id=openStockOrders; type=button |
| 161 | Düğme | CSV indir | id=accessoryOrderExport; type=button |
| 162 | Düğme | ↻ Yenile | id=accessoryOrdersRefresh; type=button |
| 163 | Düğme | Tedarikçiler | id=supplierCatalogButton; type=button |
| 164 | Düğme | ＋ Sipariş oluştur | id=newAccessoryOrderButton; type=button |
| 165 | Düğme | Filtreleri temizle | id=clearAccessoryOrderFilters; type=button |
| 166 | Düğme | ← Önceki | id=accessoryOrderPrev; type=button |
| 167 | Düğme | Sonraki → | id=accessoryOrderNext; type=button |
| 168 | Düğme | CSV indir | id=exportAccessoryKitsButton; type=button |
| 169 | Düğme | ↻ Yenile | id=accessoryKitsRefresh; type=button |
| 170 | Düğme | ＋ Kit oluştur | id=newAccessoryKitButton; type=button |
| 171 | Düğme | CSV indir | id=exportConsumablesButton; type=button |
| 172 | Düğme | Excel (.xlsx) | id=consumableXlsxExport; type=button |
| 173 | Düğme | ↑ CSV içe aktar | id=importConsumablesButton; type=button; başlangıçta gizli |
| 174 | Düğme | ＋ Sarf malzemesi ekle | id=newConsumableButton; type=button |
| 175 | Düğme | ← Önceki | id=consumablePrev; type=button |
| 176 | Düğme | Sonraki → | id=consumableNext; type=button |
| 177 | Düğme | ↓ Filtrelenmiş CSV | id=requestExport; type=button |
| 178 | Düğme | ↻ Yenile | id=requestsRefresh; type=button |
| 179 | Düğme | ← Önceki | id=requestPrev; type=button |
| 180 | Düğme | Sonraki → | id=requestNext; type=button |
| 181 | Düğme | Kapat | type=button |
| 182 | Düğme | Vazgeç | type=button |
| 183 | Düğme | Kaydet | id=inventoryRequestActionSubmit; type=submit |
| 184 | Düğme | ↓ CSV dışa aktar | id=licenseExportButton; type=button |
| 185 | Düğme | Excel (.xlsx) | id=licenseXlsxExport; type=button |
| 186 | Düğme | ↑ CSV içe aktar | id=licenseImportOpen; type=button; başlangıçta gizli |
| 187 | Düğme | ＋ Lisans ekle | id=newLicenseButton; type=button |
| 188 | Düğme | ← Önceki | id=licensePrev |
| 189 | Düğme | Sonraki → | id=licenseNext |
| 190 | Düğme | Yazılım lisansları | type=button; hedef=Yazılım lisansları |
| 191 | Düğme | ← Önceki | id=licenseAssignmentPrev |
| 192 | Düğme | Sonraki → | id=licenseAssignmentNext |
| 193 | Düğme | Yenile | id=myLicensesRefresh; type=button |
| 194 | Düğme | Yenile | id=companyNotificationPreferencesRefresh; type=button |
| 195 | Düğme | Değişiklikleri geri al | id=companyNotificationPreferencesReset; type=button |
| 196 | Düğme | Tercihleri kaydet | id=companyNotificationPreferencesSave; type=submit |
| 197 | Düğme | CSV indir | id=exportComponentsButton; type=button |
| 198 | Düğme | Excel (.xlsx) | id=componentXlsxExport; type=button |
| 199 | Düğme | ↑ CSV içe aktar | id=importComponentsButton; type=button; başlangıçta gizli |
| 200 | Düğme | ＋ Bileşen ekle | id=newComponentButton; type=button |
| 201 | Düğme | ← Önceki | id=componentPrev |
| 202 | Düğme | Sonraki → | id=componentNext |
| 203 | Düğme | CSV indir | id=exportInventoryMovementsButton; type=button |
| 204 | Düğme | ← Önceki | id=inventoryMovementPrev; type=button |
| 205 | Düğme | Sonraki → | id=inventoryMovementNext; type=button |
| 206 | Düğme | Varsayılanlara dön | id=labelSettingsReset; type=button |
| 207 | Düğme | Şablon ayarlarını yükle | id=labelStockTemplateLoad; type=button |
| 208 | Düğme | Şablonu kaydet | id=labelStockTemplateSave; type=button |
| 209 | Düğme | Şablonu sil | id=labelStockTemplateDelete; type=button |
| 210 | Düğme | Ayarları kaydet | id=labelSettingsSave; type=submit |
| 211 | Düğme | Varlık listesini aç | id=labelSettingsGoAssets; type=button |
| 212 | Düğme | Değişiklikleri kaydet | id=assetStatusLabelsSave; type=submit |
| 213 | Düğme | ＋ Özel durum ekle | id=addAssetCustomStatus; type=button |
| 214 | Düğme | Durum ve geçiş kurallarını kaydet | id=assetStatusConfigSave; type=submit |
| 215 | Düğme | ↻ Yenile | id=membersRefresh |
| 216 | Düğme | ＋ Kullanıcı davet et | id=inviteMemberButton |
| 217 | Düğme | ↻ Yenile | id=catalogRefresh; type=button |
| 218 | Düğme | ＋ Yeni marka ekle | id=catalogQuickBrand; type=button |
| 219 | Düğme | Kaydı ekle | type=submit |
| 220 | Düğme | Değişiklikleri kaydet | type=submit |
| 221 | Düğme | Logoyu güvenli şekilde yükle | type=submit |
| 222 | Düğme | Varlıklar | type=button; hedef=Varlıklar |
| 223 | Düğme | ← Önceki | id=assignmentPrev; type=button |
| 224 | Düğme | Sonraki → | id=assignmentNext; type=button |
| 225 | Düğme | ↻ Yenile | id=returnsRefresh; type=button |
| 226 | Düğme | ← Önceki | id=returnsPrev; type=button |
| 227 | Düğme | Sonraki → | id=returnsNext; type=button |
| 228 | Düğme | ↻ Yenile | id=activityRefresh |
| 229 | Düğme | ← Önceki | id=activityPrev |
| 230 | Düğme | Sonraki → | id=activityNext |
| 231 | Düğme | ↓ CSV dışa aktar | id=reservationExportCsv; type=button |
| 232 | Düğme | ↻ Yenile | id=reservationRefresh; type=button |
| 233 | Düğme | Rezervasyon iste | id=reservationSubmit; type=submit |
| 234 | Düğme | ← Önceki | id=reservationPrev; type=button |
| 235 | Düğme | Sonraki → | id=reservationNext; type=button |
| 236 | Düğme | Kapat | id=reservationActionClose; type=button |
| 237 | Düğme | Vazgeç | id=reservationActionCancel; type=button |
| 238 | Düğme | Kaydet | id=reservationActionSubmit; type=submit |
| 239 | Düğme | ↻ Yenile | id=auditRefresh |
| 240 | Düğme | ↻ Yenile | id=auditScheduleRefresh; type=button |
| 241 | Düğme | ← Önceki | id=auditSchedulePrev; type=button |
| 242 | Düğme | Sonraki → | id=auditScheduleNext; type=button |
| 243 | Düğme | CSV dışa aktar | id=auditExportCsv; type=button |
| 244 | Düğme | ← Önceki | id=auditPrev |
| 245 | Düğme | Sonraki → | id=auditNext |
| 246 | Düğme | Şirket verisini indir (JSON) | id=dataDownloadCompany; type=button |
| 247 | Düğme | Şirket verisi ve dosyaları (ZIP) | id=dataDownloadCompanyArchive; type=button |
| 248 | Düğme | Varlık listesini indir (CSV) | id=dataDownloadAssets; type=button |
| 249 | Düğme | Varlıklar | type=button; hedef=Varlıklar |
| 250 | Düğme | Çalışan kayıtları | type=button; hedef=Çalışan kayıtları |
| 251 | Düğme | Aksesuarlar | type=button; hedef=Aksesuarlar |
| 252 | Düğme | Sarf malzemeleri | type=button; hedef=Sarf malzemeleri |
| 253 | Düğme | Bileşenler | type=button; hedef=Bileşenler |
| 254 | Düğme | Yazılım lisansları | type=button; hedef=Yazılım lisansları |
| 255 | Düğme | JSON dosyasını kontrol et | id=dataValidateFile; type=button |
| 256 | Düğme | ↓ CSV dışa aktar | id=exportContractsButton; type=button |
| 257 | Düğme | ↻ Yenile | id=refreshContractsButton; type=button |
| 258 | Düğme | ＋ Sözleşme ekle | id=newContractButton; type=button |
| 259 | Düğme | ← Önceki | id=contractPrev; type=button |
| 260 | Düğme | Sonraki → | id=contractNext; type=button |
| 261 | Düğme | ↻ Yenile | id=tasksRefresh; type=button |
| 262 | Düğme | ＋ Görev oluştur | id=taskNewButton; type=button |
| 263 | Düğme | Kapat | id=taskEditorClose; type=button |
| 264 | Düğme | Görevi kaydet | id=taskSaveButton; type=submit |
| 265 | Düğme | Vazgeç | id=taskEditorCancel; type=button |
| 266 | Düğme | ← Önceki | id=taskPrev; type=button |
| 267 | Düğme | Sonraki → | id=taskNext; type=button |
| 268 | Düğme | ↻ Yenile | id=supportRefresh; type=button |
| 269 | Düğme | ＋ Yeni destek talebi | id=supportNewButton; type=button |
| 270 | Düğme | Kapat | id=supportEditorClose; type=button |
| 271 | Düğme | Talebi gönder | id=supportSaveButton; type=submit |
| 272 | Düğme | ← Önceki | id=supportPrev; type=button |
| 273 | Düğme | Sonraki → | id=supportNext; type=button |
| 274 | Düğme | Talebi kapat | id=supportCloseTicket; type=button |
| 275 | Düğme | Kapat | id=supportDetailClose; type=button |
| 276 | Düğme | Yanıt gönder | id=supportReplyButton; type=submit |
| 277 | Düğme | ↻ Güvenlik durumunu yenile | id=securityOverviewRefresh; type=button; başlangıçta gizli |
| 278 | Düğme | Kendi hesap güvenliğini aç | id=featureStatusAccountSecurityButton; type=button; başlangıçta gizli |
| 279 | Düğme | Departmanları aç | id=featureStatusDepartmentsButton; type=button; başlangıçta gizli |
| 280 | Düğme | Genel Bakış | type=button; hedef=Genel Bakış |
| 281 | Düğme | ↻ Oturumları yenile | id=securitySessionsRefresh; type=button |
| 282 | Düğme | Güvenlik politikasını kaydet | id=companySecurityPolicySave; type=submit |
| 283 | Düğme | ↻ Yenile | id=integrationsRefresh; type=button |
| 284 | Düğme | API anahtarı oluştur | id=createIntegrationToken; type=submit |
| 285 | Düğme | ＋ Webhook ekle | id=createWebhook; type=submit |
| 286 | Düğme | Kapat | id=closeIntegrationSecret; type=button |
| 287 | Düğme | Göster | id=toggleIntegrationSecret; type=button |
| 288 | Düğme | Anahtarı kopyala | id=copyIntegrationSecret; type=button |
| 289 | Düğme | Kapat | id=dismissIntegrationSecret; type=button |
| 290 | Düğme | ⇩ CSV indir | id=maintenanceExport; type=button |
| 291 | Düğme | ↻ Yenile | id=maintenanceRefresh; type=button |
| 292 | Düğme | Filtrele | id=applyMaintenanceFilters; type=button |
| 293 | Düğme | Temizle | id=clearMaintenanceFilters; type=button |
| 294 | Düğme | ← Önceki | id=maintenancePrev; type=button |
| 295 | Düğme | Sonraki → | id=maintenanceNext; type=button |
| 296 | Düğme | ⇩ CSV indir | id=calendarExport; type=button |
| 297 | Düğme | ↻ Yenile | id=calendarRefresh; type=button |
| 298 | Düğme | Takvimi göster | id=applyCalendarFilters; type=button |
| 299 | Düğme | Önceki dönem | id=calendarPeriodPrev; type=button |
| 300 | Düğme | Bugün | id=calendarPeriodToday; type=button |
| 301 | Düğme | Sonraki dönem | id=calendarPeriodNext; type=button |
| 302 | Düğme | CSV indir | id=reportExport; type=button |
| 303 | Düğme | Excel (.xlsx) | id=reportXlsxExport; type=button |
| 304 | Düğme | Yazdır / PDF | id=reportPrint; type=button |
| 305 | Düğme | Rapor takvimi | id=reportScheduleOpen; type=button |
| 306 | Düğme | ↻ Verileri yenile | id=reportRefresh; type=button |
| 307 | Düğme | Tarih aralığını uygula | id=applyReportDateFilter; type=button |
| 308 | Düğme | Temizle | id=clearReportDateFilter; type=button |
| 309 | Düğme | Alış kayıtlarını CSV indir | id=assetPurchaseReportExport; type=button |
| 310 | Düğme | Yazdır / PDF | id=assetPurchaseReportPrint; type=button |
| 311 | Düğme | ← Önceki | id=assetPurchaseReportPrev; type=button |
| 312 | Düğme | Sonraki → | id=assetPurchaseReportNext; type=button |
| 313 | Düğme | Bileşen CSV indir | id=componentReportExport; type=button |
| 314 | Düğme | Yazdır / PDF | id=componentReportPrint; type=button |
| 315 | Düğme | Denetimleri CSV indir | id=auditReportExport; type=button |
| 316 | Düğme | Yazdır / PDF | id=auditReportPrint; type=button |
| 317 | Düğme | Filtrele | id=auditReportApply; type=button |
| 318 | Düğme | ← Önceki | id=auditReportPrev; type=button |
| 319 | Düğme | Sonraki → | id=auditReportNext; type=button |
| 320 | Düğme | CSV indir | id=warrantyReportExport; type=button |
| 321 | Düğme | Yazdır / PDF | id=warrantyReportPrint; type=button |
| 322 | Düğme | ↻ Yenile | id=warrantyReportRefresh; type=button |
| 323 | Düğme | ← Önceki | id=warrantyReportPrev |
| 324 | Düğme | Sonraki → | id=warrantyReportNext |
| 325 | Düğme | ↻ Yenile | id=maintenanceReportRefresh; type=button |
| 326 | Düğme | Yazdır / PDF | id=maintenanceReportPrint; type=button |
| 327 | Düğme | Filtrele | id=applyMaintenanceReportFilter; type=button |
| 328 | Düğme | Temizle | id=clearMaintenanceReportFilter; type=button |
| 329 | Düğme | CSV indir | id=licenseReportExport; type=button |
| 330 | Düğme | Yazdır / PDF | id=licenseReportPrint; type=button |
| 331 | Düğme | ↻ Yenile | id=licenseReportRefresh; type=button |
| 332 | Düğme | ← Önceki | id=licenseReportPrev |
| 333 | Düğme | Sonraki → | id=licenseReportNext |
| 334 | Düğme | ↻ Yenile | id=pendingDeliveryRefresh; type=button |
| 335 | Düğme | CSV indir | id=pendingDeliveryExport; type=button |
| 336 | Düğme | Yazdır / PDF | id=pendingDeliveryPrint; type=button |
| 337 | Düğme | ← Önceki | id=pendingDeliveryPrev; type=button |
| 338 | Düğme | Sonraki → | id=pendingDeliveryNext; type=button |
| 339 | Düğme | ↻ Hesapla | id=depreciationReportRefresh; type=button |
| 340 | Düğme | CSV indir | id=depreciationReportExport; type=button |
| 341 | Düğme | Yazdır / PDF | id=depreciationReportPrint; type=button |
| 342 | Düğme | ← Önceki | id=depreciationReportPrev; type=button |
| 343 | Düğme | Sonraki → | id=depreciationReportNext; type=button |
| 344 | Düğme | Taslağı yükle | id=customReportLoadPreset; type=button |
| 345 | Düğme | Taslağı kaydet | id=customReportSavePreset; type=button |
| 346 | Düğme | Taslağı sil | id=customReportDeletePreset; type=button |
| 347 | Düğme | Özel raporu CSV indir | id=customReportExport; type=button |
| 348 | Düğme | Özel raporu Excel indir | id=customReportXlsxExport; type=button |
| 349 | Düğme | Yazdır / PDF | id=customReportPrint; type=button |
| 350 | Düğme | Aksesuar siparişleri | id=backAccessoryOrders; type=button |
| 351 | Düğme | CSV dışa aktar | id=exportStockOrders; type=button |
| 352 | Düğme | ↻ Yenile | id=stockOrdersRefresh; type=button |
| 353 | Düğme | ＋ Sipariş oluştur | id=newStockOrderButton; type=button |
| 354 | Düğme | Filtreleri temizle | id=clearStockOrderFilters; type=button |
| 355 | Düğme | ← Önceki | id=stockOrderPrev; type=button |
| 356 | Düğme | Sonraki → | id=stockOrderNext; type=button |
| 357 | Düğme | ↻ Yenile | id=purchaseOverviewRefresh; type=button |
| 358 | Düğme | CSV indir | id=purchaseOverviewExport; type=button |
| 359 | Düğme | Filtreleri temizle | id=purchaseOverviewClear; type=button |
| 360 | Düğme | ← Önceki | id=purchaseOverviewPrev; type=button |
| 361 | Düğme | Sonraki → | id=purchaseOverviewNext; type=button |
| 362 | Düğme | Yıllık bütçe | id=assetOrderBudgetButton; type=button |
| 363 | Düğme | ↻ Yenile | id=assetOrdersRefresh; type=button |
| 364 | Düğme | CSV indir | id=exportAssetOrdersButton; type=button |
| 365 | Düğme | Tedarikçiler | id=assetOrdersSupplierCatalog; type=button |
| 366 | Düğme | ＋ Sipariş oluştur | id=newAssetOrderButton; type=button |
| 367 | Düğme | Filtreleri temizle | id=clearAssetOrderFilters; type=button |
| 368 | Düğme | ← Önceki | id=assetOrderPrev; type=button |
| 369 | Düğme | Sonraki → | id=assetOrderNext; type=button |
| 370 | Düğme | Uygulama kullanıcıları | id=rolesManageMembersButton; type=button |
| 371 | Düğme | Özel rol oluştur | id=createCompanyRoleButton; type=button; başlangıçta gizli |
| 372 | Düğme | Kapat | id=closeCompanyRoleDialog; type=button |
| 373 | Düğme | Vazgeç | id=cancelCompanyRoleDialog; type=button |
| 374 | Düğme | Kaydet | id=saveCompanyRoleButton; type=submit |
| 375 | Düğme | Kapat | id=closeDeactivateCompanyRole; type=button |
| 376 | Düğme | Vazgeç | id=cancelDeactivateCompanyRole; type=button |
| 377 | Düğme | Devre dışı bırak | id=confirmDeactivateCompanyRole; type=button |
| 378 | Düğme | Kapat | id=licenseRevokeClose; type=button |
| 379 | Düğme | Vazgeç | id=licenseRevokeCancel; type=button |
| 380 | Düğme | Atamayı kaldır | id=licenseRevokeSubmit; type=submit |
| 381 | Düğme | Kapat | id=licenseImportProfileDeleteClose; type=button |
| 382 | Düğme | Vazgeç | id=licenseImportProfileDeleteCancel; type=button |
| 383 | Düğme | Profili sil | id=licenseImportProfileDeleteSubmit; type=submit |
| 384 | Düğme | Kapat | id=commandPaletteClose; type=button |
| 385 | Düğme | Kapat | id=keyboardShortcutsClose; type=button |
| 386 | Düğme | Kapat | type=button |
| 387 | Düğme | Vazgeç | type=button |
| 388 | Düğme | Kaydet | type=submit |
| 389 | Düğme | Kapat | id=closeContractDetail; type=button |
| 390 | Düğme | Kapat | id=closeContractDetailFooter; type=button |
| 391 | Düğme | Kapat | id=contractActionClose; type=button |
| 392 | Düğme | Vazgeç | id=contractActionCancel; type=button |
| 393 | Düğme | Onayla | id=contractActionSubmit; type=submit |
| 394 | Düğme | Kapat | id=appearanceClose; type=button |
| 395 | Düğme | Sistem ayarını izle | type=button |
| 396 | Düğme | Açık | type=button |
| 397 | Düğme | Koyu | type=button |
| 398 | Düğme | Klasik | type=button; başlangıçta gizli |
| 399 | Düğme | Stüdyo | type=button; başlangıçta gizli |
| 400 | Düğme | Klasik | type=button; başlangıçta gizli |
| 401 | Düğme | Okyanus | type=button; başlangıçta gizli |
| 402 | Düğme | Aurora | type=button; başlangıçta gizli |
| 403 | Düğme | Zümrüt | type=button; başlangıçta gizli |
| 404 | Düğme | Gün batımı | type=button; başlangıçta gizli |
| 405 | Düğme | Grafit | type=button; başlangıçta gizli |
| 406 | Düğme | Gül | type=button; başlangıçta gizli |
| 407 | Düğme | Buzul | type=button; başlangıçta gizli |
| 408 | Düğme | Bordo | type=button; başlangıçta gizli |
| 409 | Düğme | Gece mavisi | type=button; başlangıçta gizli |
| 410 | Düğme | Orman | type=button; başlangıçta gizli |
| 411 | Düğme | Mor | type=button; başlangıçta gizli |
| 412 | Düğme | Obsidyen | type=button; başlangıçta gizli |
| 413 | Düğme | Vazgeç | id=appearanceCancel; type=button |
| 414 | Düğme | Tercihleri kaydet | id=appearanceSave; type=button |
| 415 | Düğme | Kapat | id=closeDialog |
| 416 | Düğme | Genel bakış | id=assetTabOverviewButton; type=button |
| 417 | Düğme | İlişkiler | id=assetTabRelationsButton; type=button |
| 418 | Düğme | Bakım kayıtları | id=assetTabMaintenanceButton; type=button |
| 419 | Düğme | Hareket geçmişi | id=assetTabHistoryButton; type=button |
| 420 | Düğme | Fotoğraf ve dosyalar | id=assetTabAttachmentsButton; type=button |
| 421 | Düğme | detailPurchaseOrder | id=detailPurchaseOrder; type=button |
| 422 | Düğme | Etiketi kopyala | id=copyAssetTag; type=button |
| 423 | Düğme | Etiket kartını yazdır | id=printAssetLabel; type=button |
| 424 | Düğme | Bağlantıyı kaydet | id=saveAssetParent; type=button |
| 425 | Düğme | Not ekle | type=submit |
| 426 | Düğme | Dosya ekle | id=uploadAssetAttachment; type=submit |
| 427 | Düğme | Bilgileri düzenle | id=editAssetButton; type=button |
| 428 | Düğme | Denetimi kaydet | id=auditAssetButton; type=button |
| 429 | Düğme | Denetim planla | id=scheduleAssetAuditButton; type=button |
| 430 | Düğme | Bakım başlat | id=addMaintenanceButton; type=button |
| 431 | Düğme | Konum transferi | id=transferAssetButton; type=button |
| 432 | Düğme | Çalışana zimmetle | id=assignAssetButton; type=button |
| 433 | Düğme | İade al | id=returnAssetButton; type=button |
| 434 | Düğme | Arşivden geri al | id=restoreAssetButton; type=button; başlangıçta gizli |
| 435 | Düğme | Çöp kutusuna taşı | id=trashAssetButton; type=button |
| 436 | Düğme | Kapat | type=button |
| 437 | Düğme | Vazgeç | type=button |
| 438 | Düğme | Denetimi kaydet | type=submit |
| 439 | Düğme | Kapat | type=button |
| 440 | Düğme | Vazgeç | type=button |
| 441 | Düğme | Planı kaydet | type=submit |
| 442 | Düğme | Kapat | type=button |
| 443 | Düğme | Vazgeç | type=button |
| 444 | Düğme | Bakımı başlat | id=maintenanceSubmit; type=submit |
| 445 | Düğme | Kapat | type=button |
| 446 | Düğme | Vazgeç | type=button |
| 447 | Düğme | Bakımı tamamla | type=submit |
| 448 | Düğme | Kapat | type=button |
| 449 | Düğme | Vazgeç | type=button |
| 450 | Düğme | Transferi kaydet | type=submit |
| 451 | Düğme | Kapat | type=button |
| 452 | Düğme | Vazgeç | type=button |
| 453 | Düğme | Değişiklikleri kaydet | type=submit |
| 454 | Düğme | Kapat | type=button |
| 455 | Düğme | Vazgeç | type=button |
| 456 | Düğme | Zimmeti kaydet | type=submit |
| 457 | Düğme | Kapat | type=button |
| 458 | Düğme | Vazgeç | type=button |
| 459 | Düğme | Çalışanı kaydet | type=submit |
| 460 | Düğme | Kapat | id=closeEmployeeImport; type=button |
| 461 | Düğme | Şablonu indir | id=downloadEmployeeImportTemplate; type=button |
| 462 | Düğme | Dosyayı doğrula | id=previewEmployeeImport; type=button |
| 463 | Düğme | Doğrulanmış kayıtları ekle | id=commitEmployeeImport; type=button |
| 464 | Düğme | Kapat | id=closeConsumableImport; type=button |
| 465 | Düğme | Şablonu indir | id=downloadConsumableImportTemplate; type=button |
| 466 | Düğme | Eşlemeyi kaydet | id=saveConsumableImportProfile; type=button |
| 467 | Düğme | Profili sil | id=deleteConsumableImportProfile; type=button |
| 468 | Düğme | Eşlemeyi kullan ve yeniden doğrula | id=applyConsumableImportMapping; type=button |
| 469 | Düğme | Dosyayı doğrula | id=previewConsumableImport; type=button |
| 470 | Düğme | Doğrulanmış kayıtları ekle | id=commitConsumableImport; type=button |
| 471 | Düğme | Kapat | id=closeAccessoryImport; type=button |
| 472 | Düğme | Şablonu indir | id=downloadAccessoryImportTemplate; type=button |
| 473 | Düğme | Eşlemeyi kaydet | id=saveAccessoryImportProfile; type=button |
| 474 | Düğme | Profili sil | id=deleteAccessoryImportProfile; type=button |
| 475 | Düğme | Eşlemeyi kullan ve yeniden doğrula | id=applyAccessoryImportMapping; type=button |
| 476 | Düğme | Dosyayı doğrula | id=previewAccessoryImport; type=button |
| 477 | Düğme | Doğrulanmış kayıtları ekle | id=commitAccessoryImport; type=button |
| 478 | Düğme | Kapat | type=button |
| 479 | Düğme | Vazgeç | type=button |
| 480 | Düğme | Değişiklikleri kaydet | type=submit |
| 481 | Düğme | Kapat | type=button |
| 482 | Düğme | Vazgeç | type=button |
| 483 | Düğme | Onayla | id=employeeActionSubmit; type=submit |
| 484 | Düğme | Kapat | type=button |
| 485 | Düğme | Vazgeç | type=button |
| 486 | Düğme | Kaydet | id=textEntrySubmit; type=submit |
| 487 | Düğme | İmzalı zimmet belgeleri | id=printEmployeeHandoff; type=button |
| 488 | Düğme | Kapat | id=closeEmployeeDetails; type=button |
| 489 | Düğme | Kaydet | id=saveEmployeeManager; type=button |
| 490 | Düğme | Kaldır | id=clearEmployeeManager; type=button |
| 491 | Düğme | Kapat | id=closeInviteMember; type=button |
| 492 | Düğme | Vazgeç | id=cancelInviteMember; type=button |
| 493 | Düğme | Daveti gönder | type=submit |
| 494 | Düğme | Kapat | id=closeMemberLocationAccess; type=button |
| 495 | Düğme | Vazgeç | id=cancelMemberLocationAccess; type=button |
| 496 | Düğme | Erişimi kaydet | type=submit |
| 497 | Düğme | Kapat | type=button |
| 498 | Düğme | ＋ Ekle | type=button |
| 499 | Düğme | ＋ Ekle | type=button |
| 500 | Düğme | ＋ Ekle | type=button |
| 501 | Düğme | Vazgeç | type=button |
| 502 | Düğme | Kaydı oluştur | type=submit |
| 503 | Düğme | Sunucu bağlantısını test et | id=retryApiButton; type=button |
| 504 | Düğme | Giriş yap | type=submit |
| 505 | Düğme | Parolamı unuttum | id=forgotPasswordButton; type=button |
| 506 | Düğme | Kapat | id=closeDashboardCustomize; type=button |
| 507 | Düğme | Özet kartlarını yukarı taşı | type=button |
| 508 | Düğme | Özet kartlarını aşağı taşı | type=button |
| 509 | Düğme | Varlık listesini yukarı taşı | type=button |
| 510 | Düğme | Varlık listesini aşağı taşı | type=button |
| 511 | Düğme | Son hareketleri yukarı taşı | type=button |
| 512 | Düğme | Son hareketleri aşağı taşı | type=button |
| 513 | Düğme | Yaklaşan işlemleri yukarı taşı | type=button |
| 514 | Düğme | Yaklaşan işlemleri aşağı taşı | type=button |
| 515 | Düğme | Varlık durumu grafiğini yukarı taşı | type=button |
| 516 | Düğme | Varlık durumu grafiğini aşağı taşı | type=button |
| 517 | Düğme | + Grafik ekle | id=addDashboardChart; type=button |
| 518 | Düğme | Tümünü göster | id=resetDashboardCustomize; type=button |
| 519 | Düğme | İptal | id=cancelDashboardCustomize; type=button |
| 520 | Düğme | Tercihleri kaydet | type=submit |
| 521 | Düğme | Kapat | id=closeForgotPassword; type=button |
| 522 | Düğme | İptal | type=button |
| 523 | Düğme | Bağlantı iste | type=submit |
| 524 | Düğme | Kapat | id=closeImportPreview; type=button |
| 525 | Düğme | CSV şablonunu indir | id=downloadAssetCsvTemplate; type=button |
| 526 | Düğme | Eşlemeyi kaydet | id=saveAssetImportProfile; type=button |
| 527 | Düğme | Profili sil | id=deleteAssetImportProfile; type=button |
| 528 | Düğme | Eşlemeyi kullan ve yeniden doğrula | id=applyAssetImportMapping; type=button |
| 529 | Düğme | Kapat | id=cancelImportPreview; type=button |
| 530 | Düğme | Dosyayı doğrula | id=runImportPreview; type=submit |
| 531 | Düğme | Doğrula ve içe aktar | id=applyCsvImport; type=button; başlangıçta gizli |
| 532 | Düğme | Şirkete geri yükle | id=applyCompanyImport; type=button; başlangıçta gizli |
| 533 | Düğme | Kapat | id=closeAccessoryDetail; type=button |
| 534 | Düğme | Tüm siparişleri aç | id=accessoryOrdersFromDetail; type=button |
| 535 | Düğme | Kapat | id=accessoryCheckinClose; type=button |
| 536 | Düğme | Vazgeç | id=accessoryCheckinCancel; type=button |
| 537 | Düğme | İadeyi kaydet | id=accessoryCheckinSubmit; type=submit |
| 538 | Düğme | Kapat | type=button |
| 539 | Düğme | Bütçe istisnasını reddet | id=rejectAccessoryOrderApproval; type=button |
| 540 | Düğme | Bütçe istisnasını onayla | id=approveAccessoryOrderApproval; type=button |
| 541 | Düğme | Dosya yükle | type=submit |
| 542 | Düğme | Tedarikçiye iade et | id=returnAccessoryOrderFromDetail; type=button |
| 543 | Düğme | Teslim al | id=receiveAccessoryOrderFromDetail; type=button |
| 544 | Düğme | Kapat | type=button |
| 545 | Düğme | Kapat | type=button |
| 546 | Düğme | ＋ Kalem ekle | id=addAccessoryOrderLine; type=button |
| 547 | Düğme | Vazgeç | type=button |
| 548 | Düğme | Siparişi kaydet | type=submit |
| 549 | Düğme | Kapat | type=button |
| 550 | Düğme | ＋ Kalem ekle | id=addStockOrderLine; type=button |
| 551 | Düğme | Vazgeç | type=button |
| 552 | Düğme | Siparişi kaydet | type=submit |
| 553 | Düğme | Kapat | type=button |
| 554 | Düğme | Bütçe istisnasını reddet | id=rejectStockOrderApproval; type=button |
| 555 | Düğme | Bütçe istisnasını onayla | id=approveStockOrderApproval; type=button |
| 556 | Düğme | Belge yükle | type=submit |
| 557 | Düğme | Tedarikçiye iade et | id=returnStockOrderFromDetail; type=button |
| 558 | Düğme | Bu siparişten kit reçetesi oluştur | id=createKitFromStockOrder; type=button; başlangıçta gizli |
| 559 | Düğme | Teslim al | id=receiveStockOrderFromDetail; type=button |
| 560 | Düğme | Kapat | type=button |
| 561 | Düğme | Kapat | type=button |
| 562 | Düğme | Vazgeç | type=button |
| 563 | Düğme | Teslim al ve stoğa ekle | type=submit |
| 564 | Düğme | Kapat | type=button |
| 565 | Düğme | Vazgeç | type=button |
| 566 | Düğme | Kit reçetesini kaydet | type=submit |
| 567 | Düğme | Kapat | type=button |
| 568 | Düğme | Kapat | type=button |
| 569 | Düğme | Bütçeyi kaydet | type=submit |
| 570 | Düğme | Kapat | type=button |
| 571 | Düğme | ＋ Cihaz kalemi ekle | id=addAssetOrderLine; type=button |
| 572 | Düğme | Vazgeç | type=button |
| 573 | Düğme | Siparişi kaydet | type=submit |
| 574 | Düğme | Kapat | type=button |
| 575 | Düğme | Bütçe istisnasını reddet | id=rejectAssetOrderApproval; type=button |
| 576 | Düğme | Bütçe istisnasını onayla | id=approveAssetOrderApproval; type=button |
| 577 | Düğme | Belgeleri yükle | id=uploadAssetOrderAttachments; type=button |
| 578 | Düğme | Tedarikçiye iade et | id=returnAssetOrderFromDetail; type=button |
| 579 | Düğme | Teslim al | id=receiveAssetOrderFromDetail; type=button |
| 580 | Düğme | Kapat | type=button |
| 581 | Düğme | Kapat | type=button |
| 582 | Düğme | Vazgeç | type=button |
| 583 | Düğme | Teslim al ve varlıkları oluştur | type=submit |
| 584 | Düğme | Kapat | type=button |
| 585 | Düğme | Vazgeç | type=button |
| 586 | Düğme | Stoğa al | type=submit |
| 587 | Düğme | Kapat | id=closeConsumableDetail; type=button |
| 588 | Düğme | Kapat | type=button |
| 589 | Düğme | Vazgeç | type=button |
| 590 | Düğme | Kaydet | type=submit |
| 591 | Düğme | Kapat | type=button |
| 592 | Düğme | Vazgeç | type=button |
| 593 | Düğme | Stok hareketini kaydet | type=submit |
| 594 | Düğme | Kapat | type=button |
| 595 | Düğme | Vazgeç | type=button |
| 596 | Düğme | Tüketim çıkışını kaydet | type=submit |
| 597 | Düğme | Kapat | id=closeLicenseDetail; type=button |
| 598 | Düğme | Anahtarı al ve göster | id=licenseKeyReveal; type=button |
| 599 | Düğme | Gizle | id=licenseKeyVisibility; type=button; başlangıçta gizli |
| 600 | Düğme | Anahtarı kaydet | id=licenseKeySave; type=button |
| 601 | Düğme | Anahtarı kaldır | id=licenseKeyClear; type=button |
| 602 | Düğme | Dosya yükle | type=submit |
| 603 | Düğme | Kapat | type=button |
| 604 | Düğme | Vazgeç | type=button |
| 605 | Düğme | Kaydet | type=submit |
| 606 | Düğme | Kapat | type=button |
| 607 | Düğme | Vazgeç | type=button |
| 608 | Düğme | Kaydet | type=submit |
| 609 | Düğme | Kapat | id=closeLicenseImport; type=button |
| 610 | Düğme | Şablonu indir | id=downloadLicenseImportTemplate; type=button |
| 611 | Düğme | Eşlemeyi kaydet | id=saveLicenseImportProfile; type=button |
| 612 | Düğme | Profili sil | id=deleteLicenseImportProfile; type=button |
| 613 | Düğme | Eşlemeyi kullan ve yeniden doğrula | id=applyLicenseImportMapping; type=button |
| 614 | Düğme | Dosyayı doğrula | id=previewLicenseImport; type=button |
| 615 | Düğme | Doğrulanmış kayıtları ekle | id=commitLicenseImport; type=button |
| 616 | Düğme | Kapat | id=licenseActionClose; type=button |
| 617 | Düğme | Vazgeç | id=licenseActionCancel; type=button |
| 618 | Düğme | Onayla | id=licenseActionSubmit; type=submit |
| 619 | Düğme | Kapat | type=button |
| 620 | Düğme | Vazgeç | type=button |
| 621 | Düğme | Atamayı kaydet | type=submit |
| 622 | Düğme | Kapat | type=button |
| 623 | Düğme | Vazgeç | type=button |
| 624 | Düğme | Devam et | id=componentActionSubmit; type=submit |
| 625 | Düğme | Kapat | id=closeComponentDetail; type=button |
| 626 | Düğme | Kapat | id=closeComponentImport; type=button |
| 627 | Düğme | Şablonu indir | id=downloadComponentImportTemplate; type=button |
| 628 | Düğme | Eşlemeyi kaydet | id=saveComponentImportProfile; type=button |
| 629 | Düğme | Profili sil | id=deleteComponentImportProfile; type=button |
| 630 | Düğme | Eşlemeyi kullan ve yeniden doğrula | id=applyComponentImportMapping; type=button |
| 631 | Düğme | Dosyayı doğrula | id=previewComponentImport; type=button |
| 632 | Düğme | Doğrulanmış kayıtları ekle | id=commitComponentImport; type=button |
| 633 | Düğme | Kapat | type=button |
| 634 | Düğme | Vazgeç | type=button |
| 635 | Düğme | Kaydet | type=submit |
| 636 | Düğme | ✕ | type=button |
| 637 | Düğme | Vazgeç | type=button |
| 638 | Düğme | Kaydet | type=submit |
| 639 | Düğme | ✕ | type=button |
| 640 | Düğme | Vazgeç | type=button |
| 641 | Düğme | Atamayı kaydet | type=submit |
| 642 | Düğme | Kapat | type=button |
| 643 | Düğme | Vazgeç | type=button |
| 644 | Düğme | Çıkarmayı kaydet | type=submit |
| 645 | Düğme | Kapat | type=button |
| 646 | Düğme | ＋ Stok kalemi ekle | id=addAccessoryKitLine; type=button |
| 647 | Düğme | Vazgeç | type=button |
| 648 | Düğme | Kiti kaydet | type=submit |
| 649 | Düğme | Kapat | id=closeAccessoryKitHistory; type=button |
| 650 | Düğme | Kapat | id=closeAccessoryKitHistoryFooter; type=button |
| 651 | Düğme | Kapat | type=button |
| 652 | Düğme | Vazgeç | type=button |
| 653 | Düğme | Arşivle | id=accessoryKitActionSubmit; type=submit |
| 654 | Düğme | Kapat | type=button |
| 655 | Düğme | ＋ Kategori oluştur | id=accessoryQuickCategory; type=button |
| 656 | Düğme | ＋ Marka oluştur | id=accessoryQuickBrand; type=button |
| 657 | Düğme | ＋ Model oluştur | id=accessoryQuickModel; type=button |
| 658 | Düğme | Vazgeç | type=button |
| 659 | Düğme | Kaydet | type=submit |
| 660 | Düğme | Kapat | id=closeAccessoryCategoryManager; type=button |
| 661 | Düğme | Kapat | id=cancelAccessoryCategoryManager; type=button |
| 662 | Düğme | ＋ Kategori ekle | id=addAccessoryCategory; type=button |
| 663 | Düğme | Kapat | type=button |
| 664 | Düğme | Vazgeç | type=button |
| 665 | Düğme | Stok hareketini kaydet | type=submit |
| 666 | Düğme | Kapat | type=button |
| 667 | Düğme | Vazgeç | type=button |
| 668 | Düğme | Teslimi kaydet | type=submit |
| 669 | Düğme | Kapat | id=closeAssetScanner; type=button |
| 670 | Düğme | Kamerayı başlat | id=startAssetCamera; type=button |
| 671 | Düğme | Varlığı bul | type=submit |
| 672 | Düğme | Kapat | id=closeDirectoryRename; type=button |
| 673 | Düğme | Vazgeç | id=cancelDirectoryRename; type=button |
| 674 | Düğme | Değişiklikleri kaydet | id=saveDirectoryRename; type=submit |
| 675 | Düğme | Kapat | id=closeCreateDepartment; type=button |
| 676 | Düğme | Vazgeç | id=cancelCreateDepartment; type=button |
| 677 | Düğme | Departmanı kaydet | id=saveDepartment; type=submit |
| 678 | Düğme | Kapat | id=closeDepartmentMove; type=button |
| 679 | Düğme | Kapat | id=cancelDepartmentMove; type=button |
| 680 | Düğme | Sonraki 100 çalışanı taşı | id=moveDepartmentEmployees; type=submit |
| 681 | Düğme | Kapat | id=closeCreateLocation; type=button |
| 682 | Düğme | Vazgeç | id=cancelCreateLocation; type=button |
| 683 | Düğme | Konumu kaydet | id=saveLocation; type=submit |
| 684 | Düğme | Kapat | id=closeLocationManager; type=button |
| 685 | Düğme | Vazgeç | id=cancelLocationManager; type=button |
| 686 | Düğme | Sorumluyu kaydet | id=saveLocationManager; type=submit |
| 687 | Düğme | Kapat | id=closeDepartmentManager; type=button |
| 688 | Düğme | Vazgeç | id=cancelDepartmentManager; type=button |
| 689 | Düğme | Yetkiliyi kaydet | id=saveDepartmentManager; type=submit |
| 690 | Düğme | Kapat | id=cancelQuickLookup; type=button |
| 691 | Düğme | Vazgeç | id=cancelQuickLookupAction; type=button |
| 692 | Düğme | Kaydet | type=submit |
| 693 | Düğme | Kapat | id=cancelAssetReturn; type=button |
| 694 | Düğme | Vazgeç | id=cancelAssetReturnAction; type=button |
| 695 | Düğme | İade al | id=submitAssetReturn; type=submit |
| 696 | Düğme | Kapat | id=closeAssetTrashConfirm; type=button |
| 697 | Düğme | Vazgeç | id=cancelAssetTrashConfirm; type=button |
| 698 | Düğme | Varlığı çöp kutusuna taşı | id=submitAssetTrashConfirm; type=submit |
| 699 | Düğme | Kapat | id=closeAssetStatusDialog; type=button |
| 700 | Düğme | Vazgeç | id=cancelAssetStatus; type=button |
| 701 | Düğme | Durumu güncelle | id=submitAssetStatus; type=submit |
| 702 | Düğme | Kapat | id=closeAssetAttachmentRemove; type=button |
| 703 | Düğme | Vazgeç | id=cancelAssetAttachmentRemove; type=button |
| 704 | Düğme | Dosyayı kaldır | id=submitAssetAttachmentRemove; type=submit |
| 705 | Düğme | Kapat | id=closeSavedAssetSearchDialog; type=button |
| 706 | Düğme | Vazgeç | id=cancelSavedAssetSearch; type=button |
| 707 | Düğme | Aramayı kaydet | id=submitSavedAssetSearch; type=submit |
| 708 | Düğme | Kapat | id=closeCompanyMfaEnrollment; type=button |
| 709 | Düğme | Doğrula ve devam et | id=verifyCompanyMfaEnrollment; type=submit |
| 710 | Düğme | Kodları kopyala | id=copyCompanyMfaRecoveryCodes; type=button |
| 711 | Düğme | Kodları kaydettim ve giriş yap | id=finishCompanyMfaEnrollment; type=button |
| 712 | Düğme | Kapat | id=closeSupplierCatalog; type=button |
| 713 | Düğme | Temizle | id=resetSupplierForm; type=button |
| 714 | Düğme | Tedarikçiyi kaydet | type=submit |
| 715 | Düğme | Kapat | id=closeCompanySwitch; type=button |
| 716 | Düğme | Vazgeç | id=cancelCompanySwitch; type=button |
| 717 | Düğme | Çıkış yap ve şirket seç | id=confirmCompanySwitch; type=button |
| 718 | Düğme | Kapat | id=closeAccountPhoto; type=button |
| 719 | Düğme | Fotoğraf seç | id=chooseAccountPhoto; type=button |
| 720 | Düğme | Fotoğrafı kaldır | id=removeAccountPhoto; type=button; başlangıçta gizli |
| 721 | Düğme | Parolayı değiştir | id=changeOwnPassword; type=button |
| 722 | Düğme | Güvenlik ayarları | id=openAccountTwoFactor; type=button |
| 723 | Düğme | Yenile | id=refreshAccountSessions; type=button |
| 724 | Düğme | Kapat | id=closeAccountTwoFactor; type=button |
| 725 | Düğme | Authenticator kurulumu başlat | id=startTwoFactorSetup; type=button |
| 726 | Düğme | Doğrula ve etkinleştir | id=enableTwoFactor; type=button; başlangıçta gizli |
| 727 | Düğme | Kurtarma kodlarını yenile | id=rotateTwoFactorCodes; type=button; başlangıçta gizli |
| 728 | Düğme | İki aşamalı doğrulamayı kapat | id=disableTwoFactor; type=button; başlangıçta gizli |
| 729 | Düğme | Kodları kopyala | id=copyTwoFactorRecoveryCodes; type=button |
| 730 | Düğme | Kapat | type=button |
| 731 | Düğme | Vazgeç | type=button |
| 732 | Düğme | Talebi gönder | type=submit |
| 733 | Düğme | Kapat | id=closeAssetTrash; type=button |
| 734 | Düğme | ← Önceki | id=assetTrashPrev; type=button |
| 735 | Düğme | Sonraki → | id=assetTrashNext; type=button |
| 736 | Düğme | Kapat | id=closeImagePreview; type=button |
| 737 | Düğme | Kapat | id=closeModelDetails; type=button |
| 738 | Düğme | Kapat | id=closeModelAttachments; type=button |
| 739 | Düğme | Teknik dosya ekle | id=uploadModelAttachment; type=submit |
| 740 | Düğme | Yazdır | id=assetLabelPreviewPrint; type=button |
| 741 | Düğme | Kapat | id=assetLabelPreviewClose; type=button |
| 742 | Düğme | Kapat | type=button |
| 743 | Düğme | Vazgeç | type=button |
| 744 | Düğme | İadeyi kaydet | id=submitPurchaseOrderReturn; type=submit |
| 745 | Düğme | ${escapeHtml(translateUiText('Aç'))} | type=button |
| 746 | Düğme | Kapat | type=button |
| 747 | Düğme | Vazgeç | type=button |
| 748 | Düğme | Seçilenleri güncelle | type=submit |
| 749 | Düğme | ${escapeHtml(text)} ile eşleşen kayıtları filtrele | type=button |
| 750 | Düğme | ${escapeHtml(translateUiText('Yenile'))} | id=refreshAssetSoftwareInventory; type=button |
| 751 | Düğme | ${escapeHtml(c.name)} | type=button |
| 752 | Düğme | Ayrıntı | type=button |
| 753 | Düğme | Düzenle | type=button |
| 754 | Düğme | Stok | type=button |
| 755 | Düğme | Ata | type=button |
| 756 | Düğme | Düzenle | id=editComponentButton; başlangıçta gizli |
| 757 | Düğme | Stok ekle / düzelt | id=componentStockButton; başlangıçta gizli |
| 758 | Düğme | c.allocated?'':'hidden'}>Varlığa tak | id=componentAllocateButton; başlangıçta gizli |
| 759 | Düğme | Çıkar | id=${a.id}; type=button |
| 760 | Düğme | Kapat | type=button |
| 761 | Düğme | Vazgeç | type=button |
| 762 | Düğme | Bileşenleri varlığa tak | type=submit |
| 763 | Düğme | Kapat | type=button |
| 764 | Düğme | Vazgeç | type=button |
| 765 | Düğme | Stok farklarını kaydet | type=submit |
| 766 | Düğme | ${escapeHtml(l.name)} | type=button |
| 767 | Düğme | ${escapeHtml(l.name)} ${escapeHtml(translateUiText('koltuk atamalarını aç'))} | type=button |
| 768 | Düğme | ${translateUiText('Ayrıntı')} | type=button |
| 769 | Düğme | ${translateUiText('Düzenle')} | id=${order.id}; type=button |
| 770 | Düğme | ${translateUiText(order.isArchived?'Geri yükle':'Arşivle')} | id=${order.id}; type=button; action=${order.isArchived?'restore':'archive'} |
| 771 | Düğme | ${escapeHtml(String(b\|\|translateUiText('Tedarikçi ayrıntısını aç')))} | id=licenseSupplierLink; type=button |
| 772 | Düğme | ${translateUiText('Düzenle')} | id=editLicenseButton; başlangıçta gizli |
| 773 | Düğme | ${translateUiText('Koltuk ata')} | id=assignLicenseButton; başlangıçta gizli |
| 774 | Düğme | ${translateUiText('Sipariş kaydı ekle')} | id=newLicensePurchaseOrderButton; type=button; başlangıçta gizli |
| 775 | Düğme | ${translateUiText(l.isArchived?'Arşivden geri yükle':'Arşivle')} | type=button; başlangıçta gizli |
| 776 | Düğme | ${escapeHtml(`${a.assetTag\|\|''}${a.assetName?` · ${a.assetName}`:''}`)} | type=button |
| 777 | Düğme | ${escapeHtml(a.employee\|\|'—')} | type=button |
| 778 | Düğme | ${translateUiText('Atamayı kaldır')} | id=${a.id}; type=button |
| 779 | Düğme | ${translateUiText('Sil')} | id=${a.id}; type=button |
| 780 | Düğme | ${escapeHtml(a.license)} | id=${a.licenseId}; type=button |
| 781 | Düğme | ${escapeHtml(`${a.assetTag\|\|''}${a.assetName?` · ${a.assetName}`:''}`)} | type=button |
| 782 | Düğme | ${escapeHtml(a.employee\|\|'—')} | type=button |
| 783 | Düğme | ${translateUiText('Atamayı kaldır')} | id=${a.licenseId}; type=button |
| 784 | Düğme | ${escapeHtml(item.name)} | type=button |
| 785 | Düğme | ${translateUiText('Ayrıntı')} | type=button |
| 786 | Düğme | ${translateUiText('Düzenle')} | type=button |
| 787 | Düğme | ${translateUiText(item.status === 'archived' ? 'Geri yükle' : 'Arşivle')} | type=button |
| 788 | Düğme | ${translateUiText('Sil')} | id=${file.id}; type=button |
| 789 | Düğme | ${escapeHtml(item.name)} | type=button |
| 790 | Düğme | Ayrıntı | type=button |
| 791 | Düğme | Düzenle | type=button |
| 792 | Düğme | Stok | type=button |
| 793 | Düğme | ${translateUiText('Teslim et')} | type=button |
| 794 | Düğme | Düzenle | id=editConsumableButton; başlangıçta gizli |
| 795 | Düğme | Stok ekle / düzelt | id=consumableStockButton; başlangıçta gizli |
| 796 | Düğme | item.issued) ? '' : 'hidden'}>Çalışana tüketim çıkışı | id=consumableCheckoutButton; başlangıçta gizli |
| 797 | Düğme | ${item.isArchived?'Arşivden çıkar':'Arşivle'} | id=${item.id} |
| 798 | Düğme | ${escapeHtml(item.name)} | type=button |
| 799 | Düğme | ${escapeHtml(item.category \|\| translateUiText('Kategori yok'))} | id=${escapeHtml(item.categoryId \|\| '')}; type=button |
| 800 | Düğme | ${translateUiText('Ayrıntı')} | type=button |
| 801 | Düğme | ${translateUiText('Düzenle')} | type=button |
| 802 | Düğme | ${translateUiText('Teslim')} | type=button |
| 803 | Düğme | ${translateUiText(accessoryOnly ? 'Teslim et' : 'Talep oluştur')} | id=${kit.id}; type=button |
| 804 | Düğme | Geçmiş | id=${kit.id}; type=button |
| 805 | Düğme | Düzenle | id=${kit.id}; type=button |
| 806 | Düğme | Arşivle | id=${kit.id}; type=button |
| 807 | Düğme | Geri al | id=${kit.id}; type=button |
| 808 | Düğme | ${translateUiText('Düzenle')} | id=editAccessoryButton; type=button; başlangıçta gizli |
| 809 | Düğme | ${translateUiText('Stok ekle / düzelt')} | id=accessoryStockButton; type=button; başlangıçta gizli |
| 810 | Düğme | currentAccessory.checkedOut ? '' : 'hidden'}>${translateUiText('Çalışana teslim et')} | id=accessoryCheckoutButton; type=button; başlangıçta gizli |
| 811 | Düğme | ${translateUiText(currentAccessory.isArchived?'Arşivden çıkar':'Arşivle')} | id=${currentAccessory.id} |
| 812 | Düğme | ${translateUiText('Yeni sipariş')} | id=createAccessoryOrderFromDetail; type=button; başlangıçta gizli |
| 813 | Düğme | ${escapeHtml(order.orderNumber)} | id=${order.purchaseOrderId}; type=button |
| 814 | Düğme | İade al | id=${assignment.id}; type=button |
| 815 | Düğme | Düzenle | type=button |
| 816 | Düğme | ${translateUiText(category.isArchived ? 'Geri yükle' : 'Arşivle')} | type=button |
| 817 | Düğme | Kapat | type=button |
| 818 | Düğme | Vazgeç | type=button |
| 819 | Düğme | Stok farklarını kaydet | type=submit |
| 820 | Düğme | ${escapeHtml(order.orderNumber)} | id=${order.id}; type=button |
| 821 | Düğme | Ayrıntı | id=${order.id}; type=button |
| 822 | Düğme | Teslim al | id=${order.id}; type=button |
| 823 | Düğme | İptal | id=${order.id}; type=button |
| 824 | Düğme | ${escapeHtml(order.orderNumber)} | id=${order.id}; type=button |
| 825 | Düğme | Ayrıntı | id=${order.id}; type=button |
| 826 | Düğme | Teslim al | id=${order.id}; type=button |
| 827 | Düğme | İptal | id=${order.id}; type=button |
| 828 | Düğme | Kalemi kaldır | type=button |
| 829 | Düğme | ${escapeHtml(order.orderNumber)} | id=${order.id}; type=button |
| 830 | Düğme | Ayrıntı | id=${order.id}; type=button |
| 831 | Düğme | Teslim al | id=${order.id}; type=button |
| 832 | Düğme | İptal | id=${order.id}; type=button |
| 833 | Düğme | Kaldır | type=button |
| 834 | Düğme | ${escapeHtml(asset.assetTag)} | id=${asset.id}; type=button |
| 835 | Düğme | Sil | id=${item.id}; type=button |
| 836 | Düğme | ${escapeHtml(order.orderNumber)} | id=${escapeHtml(order.id)}; type=button |
| 837 | Düğme | ${translateUiText('Ayrıntı')} | id=${escapeHtml(order.id)}; type=${escapeHtml(order.type)} |
| 838 | Düğme | Düzenle | id=${supplier.id}; type=button |
| 839 | Düğme | Arşivle | id=${supplier.id}; type=button |
| 840 | Düğme | Geri al | id=${supplier.id}; type=button |
| 841 | Düğme | Kalemi kaldır | type=button |
| 842 | Düğme | Kaldır | id=${item.id}; type=button |
| 843 | Düğme | ${translateUiText('Ayrıntılar')} | type=button |
| 844 | Düğme | ${translateUiText('Teknik dosyalar')} (${fmt.format(item.attachmentCount \|\| 0)}) | type=button |
| 845 | Düğme | ${translateUiText('Görseli yükle')} | type=button |
| 846 | Düğme | ${translateUiText('Amortismanı kaydet')} | type=button |
| 847 | Düğme | ${translateUiText('Üretici bilgilerini kaydet')} | type=button |
| 848 | Düğme | ${translateUiText('Logoyu yükle')} | type=button |
| 849 | Düğme | ${translateUiText('Kaydet')} |  |
| 850 | Düğme | ${translateUiText(deleteReason)} |  |
| 851 | Düğme | ${translateUiText('Sil')} | type=button |
| 852 | Düğme | ${translateUiText('Teknik dosyalar')} | type=button |
| 853 | Düğme | ${escapeHtml(asset.assetTag)} | type=button |
| 854 | Düğme | Filtrele | id=applyAssetCustomFieldFilter; type=button |
| 855 | Düğme | Temizle | id=clearAssetCustomFieldFilter; type=button |
| 856 | Düğme | Özel alan ekle | type=submit |
| 857 | Düğme | Kapat | type=button |
| 858 | Düğme | Vazgeç | type=button |
| 859 | Düğme | Kaydet | type=submit |
| 860 | Düğme | Düzenle | id=${field.id}; type=button |
| 861 | Düğme | ${field.isActive ? 'Pasifleştir' : 'Etkinleştir'} | id=${field.id}; type=button |
| 862 | Düğme | Eşlemeyi kaydet | id=saveEmployeeImportProfile; type=button |
| 863 | Düğme | Profili sil | id=deleteEmployeeImportProfile; type=button |
| 864 | Düğme | Eşlemeyi kullan ve yeniden doğrula | id=applyEmployeeImportMapping; type=button |
| 865 | Düğme | Kapat | id=closeBulkAssetAction; type=button |
| 866 | Düğme | Vazgeç | id=cancelBulkAssetAction; type=button |
| 867 | Düğme | submitBulkAssetAction | id=submitBulkAssetAction; type=submit |
| 868 | Düğme | ${escapeHtml(translateUiText('Talep et'))} | id=${item.id}; type=button |
| 869 | Girdi alanı | employeeHandoverRecipientName | id=employeeHandoverRecipientName |
| 870 | Girdi alanı | file | type=file; name=file |
| 871 | Girdi alanı | Envanterde ara | id=globalSearch |
| 872 | Girdi alanı | Varlık listesini ara | id=assetListSearch |
| 873 | Seçim alanı | Sayfa başına kayıt | id=assetPageSize; seçenekler=25: 25 kayıt, 50: 50 kayıt, 100: 100 kayıt, 200: 200 kayıt |
| 874 | Seçim alanı | Duruma göre filtrele | id=assetStatusFilter; seçenekler=(değer yok): Tüm durumlar, Available: Kullanılabilir, Deployed: Atanmış, Pending: Beklemede, Maintenance: Bakımda, Unusable: Kullanılamaz, Archived: Arşiv |
| 875 | Seçim alanı | Kişisel cihaz filtresi | id=assetByodFilter; seçenekler=(değer yok): Şirket ve kişisel cihazlar, true: Kişisel cihazlar (BYOD), false: Şirket cihazları |
| 876 | Seçim alanı | Garanti durumuna göre filtrele | id=assetWarrantyFilter; seçenekler=(değer yok): Tüm garanti durumları, expired: Süresi dolmuş, expiring: 30 gün içinde dolacak, active: 30 günden uzun süre aktif, unspecified: Bitiş tarihi girilmemiş |
| 877 | Seçim alanı | Kategoriye göre filtrele | id=assetCategoryFilter; seçenekler=(değer yok): Tüm kategoriler |
| 878 | Seçim alanı | Markaya göre filtrele | id=assetBrandFilter; seçenekler=(değer yok): Tüm markalar |
| 879 | Seçim alanı | Modele göre filtrele | id=assetModelFilter; seçenekler=(değer yok): Tüm modeller |
| 880 | Seçim alanı | Kaydedilmiş varlık aramaları | id=savedAssetSearch; seçenekler=(değer yok): Kayıtlı aramalar |
| 881 | Girdi alanı | (etiketsiz) | type=checkbox |
| 882 | Girdi alanı | (etiketsiz) | type=checkbox |
| 883 | Girdi alanı | (etiketsiz) | type=checkbox |
| 884 | Girdi alanı | (etiketsiz) | type=checkbox |
| 885 | Girdi alanı | (etiketsiz) | type=checkbox |
| 886 | Girdi alanı | (etiketsiz) | type=checkbox |
| 887 | Girdi alanı | (etiketsiz) | type=checkbox |
| 888 | Girdi alanı | (etiketsiz) | type=checkbox |
| 889 | Girdi alanı | (etiketsiz) | type=checkbox |
| 890 | Girdi alanı | (etiketsiz) | type=checkbox |
| 891 | Girdi alanı | (etiketsiz) | type=checkbox |
| 892 | Girdi alanı | (etiketsiz) | type=checkbox |
| 893 | Girdi alanı | (etiketsiz) | type=checkbox |
| 894 | Girdi alanı | (etiketsiz) | type=checkbox |
| 895 | Girdi alanı | (etiketsiz) | type=checkbox |
| 896 | Girdi alanı | (etiketsiz) | type=checkbox |
| 897 | Girdi alanı | (etiketsiz) | type=checkbox |
| 898 | Girdi alanı | (etiketsiz) | type=checkbox |
| 899 | Girdi alanı | (etiketsiz) | type=checkbox |
| 900 | Girdi alanı | (etiketsiz) | type=checkbox |
| 901 | Girdi alanı | (etiketsiz) | type=checkbox |
| 902 | Girdi alanı | (etiketsiz) | type=checkbox |
| 903 | Seçim alanı | Tablo satır yoğunluğu | id=assetRowDensity; seçenekler=comfortable: Rahat satırlar, compact: Sık satırlar |
| 904 | Girdi alanı | Bu sayfadaki seçilebilir varlıkları seç | id=selectAllAssets; type=checkbox |
| 905 | Seçim alanı | Seçili varlıklara uygulanacak durum | id=bulkAssetStatus; seçenekler=(değer yok): Yeni durumu seçin, Available: Kullanılabilir, Pending: Beklemede, Maintenance: Bakımda, Unusable: Kullanılamaz, Archived: Arşiv |
| 906 | Girdi alanı | Bu sayfadaki seçilebilir varlıkları seç | id=selectAllAssetsHeader; type=checkbox |
| 907 | Girdi alanı | Çalışan kayıtlarında ara | id=employeeSearch |
| 908 | Girdi alanı | Departmana göre filtrele | id=employeeDepartmentFilter |
| 909 | Girdi alanı | Yönetici ara | id=employeeManagerFilterSearch |
| 910 | Seçim alanı | Yöneticiye göre filtrele | id=employeeManagerFilter; seçenekler=(değer yok): Tüm yöneticiler |
| 911 | Girdi alanı | employeeIncludeArchived | id=employeeIncludeArchived; type=checkbox |
| 912 | Seçim alanı | Çalışan durumuna göre filtrele | id=employeeState; seçenekler=(değer yok): Tüm çalışanlar, true: Aktif çalışanlar, false: Pasif çalışanlar |
| 913 | Girdi alanı | İşe başlama başlangıç tarihi | id=employeeStartedFrom; type=date |
| 914 | Girdi alanı | İşe başlama bitiş tarihi | id=employeeStartedTo; type=date |
| 915 | Seçim alanı | Seçilen çalışanların yeni durumu | id=employeeBulkActive; başlangıçta gizli; seçenekler=false: Pasife al, true: Etkinleştir |
| 916 | Girdi alanı | Liste kayıtlarında ara | id=directorySearch |
| 917 | Girdi alanı | companyUnitsIncludeInactive | id=companyUnitsIncludeInactive; type=checkbox |
| 918 | Girdi alanı | Şirket birimlerinde ara | id=companyUnitSearch |
| 919 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 920 | Girdi alanı | name | name=name |
| 921 | Seçim alanı | unitType | name=unitType; seçenekler=organizational: Organizasyon birimi, legal_entity: Tüzel kişilik, branch: Şube |
| 922 | Girdi alanı | code | name=code |
| 923 | Seçim alanı | parentCompanyUnitId | name=parentCompanyUnitId; seçenekler=(değer yok): Üst birim yok |
| 924 | Girdi alanı | Aksesuarlarda ara | id=accessorySearch |
| 925 | Seçim alanı | Stok durumuna göre filtrele | id=accessoryStockFilter; seçenekler=(değer yok): Tüm stoklar, available: Kullanılabilir stok, low: Minimum seviyede veya altında |
| 926 | Seçim alanı | Aksesuarları sırala | id=accessorySort; seçenekler=name:asc: Ada göre A–Z, name:desc: Ada göre Z–A, remaining:asc: Kalan adede göre artan, remaining:desc: Kalan adede göre azalan, updated:desc: En son güncellenen |
| 927 | Seçim alanı | Sayfa başına aksesuar sayısı | id=accessoryPageSize; seçenekler=(değer yok): 12, (değer yok): 25, (değer yok): 50, (değer yok): 100 |
| 928 | Girdi alanı | (etiketsiz) | type=checkbox |
| 929 | Girdi alanı | (etiketsiz) | type=checkbox |
| 930 | Girdi alanı | (etiketsiz) | type=checkbox |
| 931 | Girdi alanı | (etiketsiz) | type=checkbox |
| 932 | Girdi alanı | (etiketsiz) | type=checkbox |
| 933 | Girdi alanı | (etiketsiz) | type=checkbox |
| 934 | Girdi alanı | (etiketsiz) | type=checkbox |
| 935 | Girdi alanı | (etiketsiz) | type=checkbox |
| 936 | Girdi alanı | (etiketsiz) | type=checkbox |
| 937 | Girdi alanı | (etiketsiz) | type=checkbox |
| 938 | Seçim alanı | Kategoriye göre filtrele | id=accessoryCategoryFilter; seçenekler=(değer yok): Tüm kategoriler |
| 939 | Seçim alanı | Konuma göre filtrele | id=accessoryLocationFilter; seçenekler=(değer yok): Tüm konumlar |
| 940 | Girdi alanı | Satın alma siparişlerinde ara | id=accessoryOrderSearch |
| 941 | Seçim alanı | Sipariş durumuna göre filtrele | id=accessoryOrderStatus; seçenekler=(değer yok): Tüm durumlar, Ordered: Sipariş edildi, PartiallyReceived: Kısmi teslim alındı, Received: Tamamlandı, Cancelled: İptal edildi |
| 942 | Seçim alanı | Teslimat takvimine göre filtrele | id=accessoryOrderDelivery; seçenekler=(değer yok): Tüm teslimatlar, overdue: Geciken teslimatlar |
| 943 | Girdi alanı | Sipariş başlangıç tarihi | id=accessoryOrderFrom; type=date |
| 944 | Girdi alanı | Sipariş bitiş tarihi | id=accessoryOrderTo; type=date |
| 945 | Seçim alanı | Sayfa başına sipariş sayısı | id=accessoryOrderPageSize; seçenekler=25: 25, 50: 50, 100: 100 |
| 946 | Girdi alanı | Hazır kitlerde ara | id=accessoryKitSearch |
| 947 | Girdi alanı | Sarf malzemelerinde ara | id=consumableSearch |
| 948 | Seçim alanı | Stok durumuna göre filtrele | id=consumableStockFilter; seçenekler=(değer yok): Tüm stoklar, available: Stokta olanlar, low: Minimum seviyede veya altında |
| 949 | Seçim alanı | Sarf malzemesi sıralaması | id=consumableSort; seçenekler=name:asc: Ada göre A–Z, name:desc: Ada göre Z–A, remaining:asc: Kalan miktar artan, remaining:desc: Kalan miktar azalan, updated:desc: Son güncellenen |
| 950 | Girdi alanı | Talep edilebilir stoklarda ara | id=requestCatalogSearch |
| 951 | Seçim alanı | Talep durumuna göre filtrele | id=requestStatusFilter; seçenekler=(değer yok): Tüm durumlar, Pending: Bekliyor, Approved: Onaylandı, Rejected: Reddedildi, Cancelled: İptal edildi |
| 952 | Girdi alanı | Taleplerde ara | id=requestSearch |
| 953 | Metin alanı | note | id=inventoryRequestActionNote; name=note |
| 954 | Girdi alanı | licenseSearch | id=licenseSearch |
| 955 | Seçim alanı | Lisans durumuna göre filtrele | id=licenseStatusFilter; seçenekler=active: Etkin lisanslar, archived: Arşivlenmiş lisanslar, all: Tüm lisanslar |
| 956 | Girdi alanı | licenseAssignmentSearch | id=licenseAssignmentSearch |
| 957 | Seçim alanı | Lisans ataması durumu | id=licenseAssignmentState; seçenekler=true: Aktif atamalar, false: Geri alınan atamalar, (değer yok): Tüm atamalar |
| 958 | Girdi alanı | kind | type=checkbox; name=kind |
| 959 | Girdi alanı | kind | type=checkbox; name=kind |
| 960 | Girdi alanı | kind | type=checkbox; name=kind |
| 961 | Girdi alanı | kind | type=checkbox; name=kind |
| 962 | Girdi alanı | kind | type=checkbox; name=kind |
| 963 | Girdi alanı | kind | type=checkbox; name=kind |
| 964 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 965 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 966 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 967 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 968 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 969 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 970 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 971 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 972 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 973 | Girdi alanı | componentSearch | id=componentSearch |
| 974 | Seçim alanı | Bileşen stok filtresi | id=componentStockFilter; seçenekler=(değer yok): Tüm stoklar, available: Kullanılabilir miktar, low: Minimum seviyede veya altında |
| 975 | Girdi alanı | Stok hareketlerinde ara | id=inventoryMovementSearch |
| 976 | Seçim alanı | Stok türüne göre filtrele | id=inventoryMovementKind; seçenekler=(değer yok): Tüm stok türleri, accessory: Aksesuar, consumable: Sarf malzemesi, component: Bileşen |
| 977 | Seçim alanı | Hareket türüne göre filtrele | id=inventoryMovementType; seçenekler=(değer yok): Tüm hareketler, Received: Stok girişi, Adjustment: Sayım düzeltmesi, Checkout: Çıkış / atama, Checkin: İade / çıkarma |
| 978 | Girdi alanı | Başlangıç tarihi | id=inventoryMovementFrom; type=date |
| 979 | Girdi alanı | Bitiş tarihi | id=inventoryMovementTo; type=date |
| 980 | Seçim alanı | (değer yok): Şablon seçin | id=labelStockTemplate; seçenekler=(değer yok): Şablon seçin |
| 981 | Girdi alanı | labelStockTemplateName | id=labelStockTemplateName |
| 982 | Girdi alanı | labelStockTemplateManufacturer | id=labelStockTemplateManufacturer |
| 983 | Girdi alanı | labelStockTemplatePrinter | id=labelStockTemplatePrinter |
| 984 | Girdi alanı | labelStockTemplateProduct | id=labelStockTemplateProduct |
| 985 | Seçim alanı | barcode1d | name=barcode1d; seçenekler=code128: C128 · alfasayısal, code39: C39 · büyük harf ve rakam, code93: C93 · kompakt alfasayısal, code93ext: C93 Extended · geniş karakter kümesi, rationalizedCodabar: Codabar · sayısal / A–D başlangıç-bitiş, msi: MSI Plessey · sayısal stok kodu, ean5: EAN5 · 5 rakam, ean13: EAN13 · 12/13 rakam, upca: UPC-A · 11/12 rakam, upce: UPC-E · 6–8 rakam, none: Kapalı |
| 986 | Seçim alanı | barcode2d | name=barcode2d; seçenekler=qrcode: QR Code, pdf417: PDF417, datamatrix: Data Matrix, azteccode: Aztec Code, none: Kapalı |
| 987 | Seçim alanı | sizePreset | name=sizePreset; seçenekler=86x48: 86 × 48 mm · varsayılan, 50x30: 50 × 30 mm, 60x40: 60 × 40 mm, 70x35: 70 × 35 mm, 100x50: 100 × 50 mm, custom: Özel ölçü |
| 988 | Girdi alanı | widthMm | type=number; name=widthMm |
| 989 | Girdi alanı | heightMm | type=number; name=heightMm |
| 990 | Girdi alanı | offsetXmm | type=number; name=offsetXmm |
| 991 | Girdi alanı | offsetYmm | type=number; name=offsetYmm |
| 992 | Girdi alanı | paddingMm | type=number; name=paddingMm |
| 993 | Girdi alanı | codeSizeMm | type=number; name=codeSizeMm |
| 994 | Seçim alanı | qrTarget | name=qrTarget; seçenekler=tag: Varlık etiketi · aramayla bulunur, asset-url: Giriş sonrası varlık ayrıntısını aç |
| 995 | Girdi alanı | showName | type=checkbox; name=showName |
| 996 | Girdi alanı | showSerial | type=checkbox; name=showSerial |
| 997 | Girdi alanı | showModel | type=checkbox; name=showModel |
| 998 | Girdi alanı | showLocation | type=checkbox; name=showLocation |
| 999 | Girdi alanı | showStatus | type=checkbox; name=showStatus |
| 1000 | Seçim alanı | sheetLayout | name=sheetLayout; seçenekler=single: Her etiketi ayrı sayfaya bas, sheet: Etiket yaprağına otomatik yerleştir |
| 1001 | Seçim alanı | sheetPaper | name=sheetPaper; seçenekler=a4: A4 · 210 × 297 mm, letter: Letter · 216 × 279 mm |
| 1002 | Girdi alanı | sheetMarginMm | type=number; name=sheetMarginMm |
| 1003 | Girdi alanı | sheetGapMm | type=number; name=sheetGapMm |
| 1004 | Girdi alanı | name | name=name |
| 1005 | Seçim alanı | brandId | name=brandId; seçenekler=(değer yok): Marka seçin (isteğe bağlı) |
| 1006 | Seçim alanı | categoryId | name=categoryId; seçenekler=(değer yok): Kategori seçin |
| 1007 | Girdi alanı | modelNumber | name=modelNumber |
| 1008 | Girdi alanı | endOfLifeOn | type=date; name=endOfLifeOn |
| 1009 | Girdi alanı | website | type=url; name=website |
| 1010 | Girdi alanı | supportUrl | type=url; name=supportUrl |
| 1011 | Girdi alanı | warrantyUrl | type=url; name=warrantyUrl |
| 1012 | Girdi alanı | supportPhone | type=tel; name=supportPhone |
| 1013 | Girdi alanı | supportEmail | type=email; name=supportEmail |
| 1014 | Girdi alanı | brandLogo | type=file; name=brandLogo |
| 1015 | Girdi alanı | modelPhoto | type=file; name=modelPhoto |
| 1016 | Girdi alanı | depreciationMonths | type=number; name=depreciationMonths |
| 1017 | Girdi alanı | depreciationAnnualRatePercent | type=number; name=depreciationAnnualRatePercent |
| 1018 | Girdi alanı | depreciationMinimum | type=number; name=depreciationMinimum |
| 1019 | Seçim alanı | depreciationMinimumType | name=depreciationMinimumType; seçenekler=amount: Tutar, percent: Yüzde |
| 1020 | Seçim alanı | depreciationMethod | name=depreciationMethod; seçenekler=linear: Doğrusal, half_1: Yarım yıl · ilk dönem, half_2: Yarım yıl · satın alma dönemine göre |
| 1021 | Girdi alanı | catalogSearch | id=catalogSearch |
| 1022 | Girdi alanı | name | name=name |
| 1023 | Girdi alanı | slug | name=slug |
| 1024 | Girdi alanı | contactEmail | type=email; name=contactEmail |
| 1025 | Girdi alanı | brandPrimaryColor | type=color; name=brandPrimaryColor |
| 1026 | Girdi alanı | logo | type=file; name=logo |
| 1027 | Girdi alanı | Zimmetlerde ara | id=assignmentSearch |
| 1028 | Seçim alanı | İade tarihine göre filtrele | id=returnsState; seçenekler=overdue: Geciken iadeler, due30: Önümüzdeki 30 gün, undated: Tarihi belirtilmemiş, later: Daha ileri tarih, all: Tüm zimmetler |
| 1029 | Girdi alanı | İadelerde ara | id=returnsSearch |
| 1030 | Girdi alanı | Hareket kayıtlarında ara | id=activitySearch |
| 1031 | Seçim alanı | Hareket türü | id=activityType; seçenekler=(değer yok): Tüm işlem türleri, Created: Oluşturuldu, Purchased: Satın alma teslimatı, Assigned: Zimmetlendi, Returned: İade alındı, StatusChanged: Durum değişti, Transferred: Konum transferi |
| 1032 | Girdi alanı | activityFrom | id=activityFrom; type=date |
| 1033 | Girdi alanı | activityTo | id=activityTo; type=date |
| 1034 | Seçim alanı | assetId | id=reservationAsset; name=assetId; seçenekler=(değer yok): Uygun varlıklar yükleniyor… |
| 1035 | Seçim alanı | employeeId | id=reservationEmployee; name=employeeId; seçenekler=(değer yok): Çalışanlar yükleniyor… |
| 1036 | Girdi alanı | startsOn | type=date; name=startsOn |
| 1037 | Girdi alanı | endsOn | type=date; name=endsOn |
| 1038 | Metin alanı | requestNote | name=requestNote |
| 1039 | Girdi alanı | Rezervasyonlarda ara | id=reservationSearch; type=search |
| 1040 | Seçim alanı | Rezervasyon durumu | id=reservationStatus; seçenekler=(değer yok): Tüm durumlar, Pending: Onay bekliyor, Approved: Onaylandı, Rejected: Reddedildi, Cancelled: İptal edildi, Completed: Tamamlandı |
| 1041 | Girdi alanı | reservationFrom | id=reservationFrom; type=date |
| 1042 | Girdi alanı | reservationTo | id=reservationTo; type=date |
| 1043 | Metin alanı | note | id=reservationReviewNote; name=note |
| 1044 | Seçim alanı | 30: 30 gün, 90: 90 gün, 180: 180 gün, 366: 1 yıl | id=auditScheduleHorizon; seçenekler=30: 30 gün, 90: 90 gün, 180: 180 gün, 366: 1 yıl |
| 1045 | Seçim alanı | Denetim planı durumu | id=auditScheduleState; seçenekler=all: Gecikmiş ve yaklaşan, overdue: Yalnızca gecikmiş, upcoming: Yalnızca yaklaşan, unscheduled: Denetim planı olmayanlar, completed: Tamamlanan denetimler |
| 1046 | Girdi alanı | Denetim kuyruğunda ara | id=auditScheduleSearch |
| 1047 | Girdi alanı | Denetim kayıtlarında ara | id=auditSearch |
| 1048 | Seçim alanı | Denetim hedefi | id=auditTarget; seçenekler=(değer yok): Tüm kayıt türleri, Asset: Varlık, Employee: Çalışan, Company: Şirket, categories: Kategoriler, brands: Markalar, models: Modeller |
| 1049 | Seçim alanı | İşlem türü | id=auditAction; seçenekler=(değer yok): Tüm işlemler |
| 1050 | Girdi alanı | Denetim başlangıç tarihi | id=auditFrom; type=date |
| 1051 | Girdi alanı | Denetim bitiş tarihi | id=auditTo; type=date |
| 1052 | Girdi alanı | Sözleşmelerde ara | id=contractSearch |
| 1053 | Seçim alanı | Sözleşme durumu | id=contractStatus; seçenekler=(değer yok): Etkin ve arşiv kayıtları, active: Etkin, expiring: 30 gün içinde bitecek, expired: Süresi dolmuş, archived: Arşiv |
| 1054 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1055 | Girdi alanı | title | name=title |
| 1056 | Metin alanı | description | name=description |
| 1057 | Seçim alanı | priority | name=priority; seçenekler=Low: Düşük, Normal: Normal, High: Yüksek, Urgent: Acil |
| 1058 | Seçim alanı | assignedEmployeeId | name=assignedEmployeeId; seçenekler=(değer yok): Atanmamış |
| 1059 | Girdi alanı | dueAt | type=date; name=dueAt |
| 1060 | Girdi alanı | Görevlerde ara | id=taskSearch |
| 1061 | Seçim alanı | Görev durumu | id=taskStatusFilter; seçenekler=(değer yok): Tüm durumlar, Open: Açık, InProgress: Devam ediyor, Completed: Tamamlandı, Cancelled: İptal edildi |
| 1062 | Seçim alanı | Görev önceliği | id=taskPriorityFilter; seçenekler=(değer yok): Tüm öncelikler, Low: Düşük, Normal: Normal, High: Yüksek, Urgent: Acil |
| 1063 | Girdi alanı | subject | name=subject |
| 1064 | Seçim alanı | category | name=category; seçenekler=Access: Hesap ve erişim, Inventory: Envanter, Data: Veri ve aktarım, Other: Diğer |
| 1065 | Metin alanı | message | name=message |
| 1066 | Girdi alanı | Destek taleplerinde ara | id=supportSearch |
| 1067 | Seçim alanı | Talep durumu | id=supportStatusFilter; seçenekler=(değer yok): Tüm durumlar, Open: Açık, InProgress: İşlemde, AwaitingCustomer: Yanıtınız bekleniyor, Resolved: Çözüldü, Closed: Kapatıldı |
| 1068 | Metin alanı | body | name=body |
| 1069 | Girdi alanı | passwordMinimumLength | type=number; name=passwordMinimumLength |
| 1070 | Girdi alanı | sessionDurationHours | type=number; name=sessionDurationHours |
| 1071 | Girdi alanı | requireMfa | type=checkbox; name=requireMfa |
| 1072 | Girdi alanı | name | name=name |
| 1073 | Girdi alanı | scope | type=checkbox; name=scope |
| 1074 | Girdi alanı | scope | type=checkbox; name=scope |
| 1075 | Girdi alanı | scope | type=checkbox; name=scope |
| 1076 | Girdi alanı | scope | type=checkbox; name=scope |
| 1077 | Seçim alanı | expiresInDays | name=expiresInDays; seçenekler=(değer yok): Süresiz, 30: 30 gün, 90: 90 gün, 365: 365 gün |
| 1078 | Girdi alanı | name | name=name |
| 1079 | Seçim alanı | kind | id=webhookKind; name=kind; seçenekler=slack: Slack, teams: Microsoft Teams, generic: Genel webhook (imzalı JSON) |
| 1080 | Girdi alanı | url | id=webhookUrl; type=url; name=url |
| 1081 | Girdi alanı | webhookSecretValue | id=webhookSecretValue |
| 1082 | Girdi alanı | integrationSecretValue | id=integrationSecretValue; type=password |
| 1083 | Girdi alanı | Bakım kayıtlarında ara | id=maintenanceSearch |
| 1084 | Seçim alanı | Bakım durumu | id=maintenanceStatus; seçenekler=(değer yok): Tüm durumlar, Scheduled: Planlandı, InProgress: Devam ediyor, Completed: Tamamlandı, Cancelled: İptal edildi |
| 1085 | Girdi alanı | maintenanceFrom | id=maintenanceFrom; type=date |
| 1086 | Girdi alanı | maintenanceTo | id=maintenanceTo; type=date |
| 1087 | Girdi alanı | calendarFrom | id=calendarFrom; type=date |
| 1088 | Girdi alanı | calendarTo | id=calendarTo; type=date |
| 1089 | Seçim alanı | Takvim etkinlik türü | id=calendarKind; seçenekler=(değer yok): Bakım, denetim, lisans, garanti, iade ve rezervasyon, maintenance: Yalnızca bakım planları, audit: Yalnızca fiziksel denetimler, license: Yalnızca lisans bitişleri, warranty: Yalnızca garanti bitişleri, return: Yalnızca beklenen varlık iadeleri, reservation: Yalnızca onaylı rezervasyonlar |
| 1090 | Seçim alanı | Takvim görünümü | id=calendarViewMode; seçenekler=list: Liste görünümü, day: Gün görünümü, week: Hafta görünümü, month: Ay görünümü |
| 1091 | Girdi alanı | reportDateFrom | id=reportDateFrom; type=date |
| 1092 | Girdi alanı | reportDateTo | id=reportDateTo; type=date |
| 1093 | Girdi alanı | Varlık alış raporunda ara | id=assetPurchaseReportSearch |
| 1094 | Girdi alanı | auditReportFrom | id=auditReportFrom; type=date |
| 1095 | Girdi alanı | auditReportTo | id=auditReportTo; type=date |
| 1096 | Girdi alanı | Fiziksel denetim sonuçlarında ara | id=auditReportSearch |
| 1097 | Seçim alanı | Garanti raporu durumu | id=warrantyReportState; seçenekler=upcoming: Yaklaşan, expired: Süresi dolan, later: Daha ileri tarih, missing: Tarihi olmayan, all: Tümü |
| 1098 | Seçim alanı | 30: 30 gün, 90: 90 gün, 180: 180 gün, 366: 1 yıl | id=warrantyReportHorizon; seçenekler=30: 30 gün, 90: 90 gün, 180: 180 gün, 366: 1 yıl |
| 1099 | Girdi alanı | Garanti raporunda ara | id=warrantyReportSearch |
| 1100 | Girdi alanı | maintenanceReportFrom | id=maintenanceReportFrom; type=date |
| 1101 | Girdi alanı | maintenanceReportTo | id=maintenanceReportTo; type=date |
| 1102 | Seçim alanı | Lisans bitiş raporu durumu | id=licenseReportState; seçenekler=upcoming: Yaklaşan, expired: Süresi dolan, later: Daha ileri tarih, missing: Tarihi olmayan, all: Tümü |
| 1103 | Seçim alanı | 30: 30 gün, 90: 90 gün, 180: 180 gün, 366: 1 yıl | id=licenseReportHorizon; seçenekler=30: 30 gün, 90: 90 gün, 180: 180 gün, 366: 1 yıl |
| 1104 | Girdi alanı | Lisans bitiş raporunda ara | id=licenseReportSearch |
| 1105 | Seçim alanı | Bekleyen teslimat tarihine göre filtrele | id=pendingDeliveryState; seçenekler=all: Tüm açık kalemler, overdue: Geciken teslimatlar, due30: Önümüzdeki 30 gün, undated: Tarihi belirtilmemiş |
| 1106 | Girdi alanı | Bekleyen teslimat raporunda ara | id=pendingDeliverySearch |
| 1107 | Girdi alanı | depreciationReportAsOf | id=depreciationReportAsOf; type=date |
| 1108 | Girdi alanı | Amortisman raporunda ara | id=depreciationReportSearch |
| 1109 | Girdi alanı | customReportName | id=customReportName |
| 1110 | Seçim alanı | (değer yok): Taslak seçin… | id=customReportPreset; seçenekler=(değer yok): Taslak seçin… |
| 1111 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1112 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1113 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1114 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1115 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1116 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1117 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1118 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1119 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1120 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1121 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1122 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1123 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1124 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1125 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1126 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1127 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1128 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1129 | Girdi alanı | customReportSearch | id=customReportSearch |
| 1130 | Seçim alanı | (değer yok): Tüm durumlar, Available: Kullanılabilir, Deployed: Atanmış, Pending: Beklemede, Maintenance: Bakımda, Unusable: Kullanılamaz, Archived: Arşivlenmiş | id=customReportStatus; seçenekler=(değer yok): Tüm durumlar, Available: Kullanılabilir, Deployed: Atanmış, Pending: Beklemede, Maintenance: Bakımda, Unusable: Kullanılamaz, Archived: Arşivlenmiş |
| 1131 | Seçim alanı | (değer yok): Tüm garanti durumları, expired: Süresi dolmuş, expiring: Yakında dolacak, active: Geçerli, unspecified: Tarih girilmemiş | id=customReportWarranty; seçenekler=(değer yok): Tüm garanti durumları, expired: Süresi dolmuş, expiring: Yakında dolacak, active: Geçerli, unspecified: Tarih girilmemiş |
| 1132 | Seçim alanı | tag:asc: Etiket · A–Z, tag:desc: Etiket · Z–A, name:asc: Ada göre · A–Z, name:desc: Ada göre · Z–A, category:asc: Kategori · A–Z, brand:asc: Marka / model · A–Z, employee:asc: Çalışan · A–Z, location:asc: Konum · A–Z | id=customReportSort; seçenekler=tag:asc: Etiket · A–Z, tag:desc: Etiket · Z–A, name:asc: Ada göre · A–Z, name:desc: Ada göre · Z–A, category:asc: Kategori · A–Z, brand:asc: Marka / model · A–Z, employee:asc: Çalışan · A–Z, location:asc: Konum · A–Z |
| 1133 | Girdi alanı | Sarf ve bileşen siparişlerinde ara | id=stockOrderSearch |
| 1134 | Seçim alanı | Sipariş durumuna göre filtrele | id=stockOrderStatus; seçenekler=(değer yok): Tüm durumlar, Ordered: Sipariş edildi, PartiallyReceived: Kısmi teslim alındı, Received: Tamamlandı, Cancelled: İptal edildi |
| 1135 | Seçim alanı | Sipariş kalem türüne göre filtrele | id=stockOrderItemType; seçenekler=(değer yok): Tüm kalem türleri, Consumable: Sarf, Component: Bileşen |
| 1136 | Seçim alanı | Teslimat takvimine göre filtrele | id=stockOrderDelivery; seçenekler=(değer yok): Tüm teslimatlar, overdue: Geciken teslimatlar |
| 1137 | Girdi alanı | Sipariş başlangıç tarihi | id=stockOrderFrom; type=date |
| 1138 | Girdi alanı | Sipariş bitiş tarihi | id=stockOrderTo; type=date |
| 1139 | Seçim alanı | Sayfa başına sipariş sayısı | id=stockOrderPageSize; seçenekler=25: 25, 50: 50, 100: 100 |
| 1140 | Girdi alanı | Tüm satın alma siparişlerinde ara | id=purchaseOverviewSearch; type=search |
| 1141 | Seçim alanı | Satın alma türü | id=purchaseOverviewType; seçenekler=(değer yok): Tüm türler, asset: Varlık, accessory: Aksesuar, stock: Sarf / bileşen, license: Yazılım lisansı |
| 1142 | Seçim alanı | Sipariş durumu | id=purchaseOverviewStatus; seçenekler=(değer yok): Tüm durumlar, Ordered: Sipariş edildi, PartiallyReceived: Kısmi teslim, Received: Tamamlandı, Cancelled: İptal edildi, Recorded: Kaydedildi |
| 1143 | Girdi alanı | Sipariş başlangıç tarihi | id=purchaseOverviewFrom; type=date |
| 1144 | Girdi alanı | Sipariş bitiş tarihi | id=purchaseOverviewTo; type=date |
| 1145 | Seçim alanı | Sayfa başına sipariş sayısı | id=purchaseOverviewPageSize; seçenekler=25: 25, 50: 50, 100: 100 |
| 1146 | Girdi alanı | Varlık satın alma siparişlerinde ara | id=assetOrderSearch |
| 1147 | Seçim alanı | Sipariş durumuna göre filtrele | id=assetOrderStatus; seçenekler=(değer yok): Tüm durumlar, Ordered: Sipariş edildi, PartiallyReceived: Kısmi teslim alındı, Received: Tamamlandı, Cancelled: İptal edildi |
| 1148 | Girdi alanı | Sipariş başlangıç tarihi | id=assetOrderFrom; type=date |
| 1149 | Girdi alanı | Sipariş bitiş tarihi | id=assetOrderTo; type=date |
| 1150 | Seçim alanı | Sayfa başına sipariş sayısı | id=assetOrderPageSize; seçenekler=25: 25, 50: 50, 100: 100 |
| 1151 | Girdi alanı | roleKey | type=hidden; name=roleKey; başlangıçta gizli |
| 1152 | Girdi alanı | name | name=name |
| 1153 | Girdi alanı | permission | type=checkbox; name=permission |
| 1154 | Girdi alanı | permission | type=checkbox; name=permission |
| 1155 | Girdi alanı | permission | type=checkbox; name=permission |
| 1156 | Girdi alanı | permission | type=checkbox; name=permission |
| 1157 | Girdi alanı | permission | type=checkbox; name=permission |
| 1158 | Girdi alanı | permission | type=checkbox; name=permission |
| 1159 | Girdi alanı | commandPaletteSearch | id=commandPaletteSearch; type=search |
| 1160 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1161 | Girdi alanı | name | name=name |
| 1162 | Girdi alanı | contractNumber | name=contractNumber |
| 1163 | Girdi alanı | contractType | name=contractType |
| 1164 | Seçim alanı | supplierId | name=supplierId; seçenekler=(değer yok): Elle girilen tedarikçi |
| 1165 | Girdi alanı | supplier | name=supplier |
| 1166 | Girdi alanı | startDate | type=date; name=startDate |
| 1167 | Girdi alanı | endDate | type=date; name=endDate |
| 1168 | Girdi alanı | cost | type=number; name=cost |
| 1169 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1170 | Girdi alanı | contractLookupSearch | id=contractLookupSearch; type=search |
| 1171 | Seçim alanı | Sözleşmeye bağlı varlıklar | id=contractAssetIds; name=assetIds |
| 1172 | Seçim alanı | Sözleşmeye bağlı yazılım lisansları | id=contractLicenseIds; name=licenseIds |
| 1173 | Metin alanı | notes | name=notes |
| 1174 | Girdi alanı | contractAttachmentFile | id=contractAttachmentFile; type=file; başlangıçta gizli |
| 1175 | Seçim alanı | Tema | id=themePreference; seçenekler=system: Sistem ayarını izle, light: Açık, dark: Koyu |
| 1176 | Seçim alanı | Tasarım | id=layoutPreference; seçenekler=classic: Klasik, studio: Stüdyo |
| 1177 | Seçim alanı | Renk paleti | id=palettePreference; seçenekler=classic: Klasik, ocean: Okyanus, aurora: Aurora, emerald: Zümrüt, sunset: Gün batımı, graphite: Grafit, rose: Gül, arctic: Buzul, maroon: Bordo, midnight: Gece mavisi, forest: Orman, violet: Mor, obsidian: Obsidyen |
| 1178 | Seçim alanı | tr: Türkçe, en: English, ru: Русский, uk: Українська, mk: Македонски | id=languagePreference; seçenekler=tr: Türkçe, en: English, ru: Русский, uk: Українська, mk: Македонски |
| 1179 | Girdi alanı | Üst varlık ara | id=assetParentSearch; type=search |
| 1180 | Seçim alanı | Üst varlık | id=assetParentSelect; seçenekler=(değer yok): Üst varlık yok |
| 1181 | Metin alanı | note | id=assetNoteInput; name=note |
| 1182 | Girdi alanı | file | id=assetAttachmentInput; type=file; name=file |
| 1183 | Seçim alanı | assetEventId | id=assetAttachmentEvent; name=assetEventId; seçenekler=(değer yok): Bir harekete bağlama |
| 1184 | Seçim alanı | Varlık durumunu değiştir | id=assetStatusAction; seçenekler=(değer yok): Durumu değiştir…, Available: Kullanılabilir, Pending: Beklemede, Maintenance: Bakımda, Unusable: Kullanılamaz, Archived: Arşivle |
| 1185 | Girdi alanı | auditDate | type=date; name=auditDate |
| 1186 | Girdi alanı | nextAuditOn | type=date; name=nextAuditOn |
| 1187 | Seçim alanı | outcome | id=auditOutcome; name=outcome; seçenekler=Accepted: Uygun, Unacceptable: Uygun değil / bulgu var |
| 1188 | Metin alanı | issueDescription | id=auditIssueDescription; name=issueDescription |
| 1189 | Metin alanı | notes | name=notes |
| 1190 | Girdi alanı | nextAuditOn | type=date; name=nextAuditOn |
| 1191 | Girdi alanı | maintenanceType | name=maintenanceType |
| 1192 | Girdi alanı | supplier | name=supplier |
| 1193 | Seçim alanı | responsibleEmployeeId | name=responsibleEmployeeId; seçenekler=(değer yok): Çalışan atama |
| 1194 | Girdi alanı | description | name=description |
| 1195 | Seçim alanı | mode | id=maintenanceMode; name=mode; seçenekler=immediate: Hemen başlat, scheduled: İleri tarihe planla |
| 1196 | Girdi alanı | startedOn | type=date; name=startedOn |
| 1197 | Girdi alanı | plannedOn | type=date; name=plannedOn |
| 1198 | Girdi alanı | cost | type=number; name=cost |
| 1199 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY — Türk lirası, USD: USD — ABD doları, EUR: EUR — Euro |
| 1200 | Metin alanı | notes | name=notes |
| 1201 | Girdi alanı | completedOn | type=date; name=completedOn |
| 1202 | Metin alanı | notes | name=notes |
| 1203 | Girdi alanı | transferSourceLocation | id=transferSourceLocation |
| 1204 | Girdi alanı | targetLocation | name=targetLocation |
| 1205 | Metin alanı | notes | name=notes |
| 1206 | Girdi alanı | expectedUpdatedAt | type=hidden; name=expectedUpdatedAt; başlangıçta gizli |
| 1207 | Girdi alanı | assetTag | name=assetTag |
| 1208 | Girdi alanı | assetName | name=assetName |
| 1209 | Seçim alanı | categoryId | name=categoryId |
| 1210 | Seçim alanı | brandId | name=brandId; seçenekler=(değer yok): Marka seçin |
| 1211 | Seçim alanı | modelId | name=modelId; seçenekler=(değer yok): Model seçin |
| 1212 | Girdi alanı | serialNumber | name=serialNumber |
| 1213 | Girdi alanı | location | name=location |
| 1214 | Seçim alanı | defaultLocationCatalogId | name=defaultLocationCatalogId; seçenekler=(değer yok): Varsayılan konum seçin |
| 1215 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1216 | Girdi alanı | warrantyExpiresAt | type=date; name=warrantyExpiresAt |
| 1217 | Girdi alanı | insuranceProvider | name=insuranceProvider |
| 1218 | Girdi alanı | insurancePolicyNumber | name=insurancePolicyNumber |
| 1219 | Girdi alanı | insuranceExpiresAt | type=date; name=insuranceExpiresAt |
| 1220 | Girdi alanı | purchaseCost | type=number; name=purchaseCost |
| 1221 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY — Türk lirası, USD: USD — ABD doları, EUR: EUR — Euro |
| 1222 | Metin alanı | notes | name=notes |
| 1223 | Girdi alanı | isRequestable | type=checkbox; name=isRequestable |
| 1224 | Girdi alanı | isByod | type=checkbox; name=isByod |
| 1225 | Girdi alanı | assignSearch | id=assignSearch |
| 1226 | Seçim alanı | employeeId | name=employeeId |
| 1227 | Girdi alanı | expectedReturnOn | type=date; name=expectedReturnOn |
| 1228 | Girdi alanı | note | name=note |
| 1229 | Girdi alanı | displayName | name=displayName |
| 1230 | Girdi alanı | employeeNumber | name=employeeNumber |
| 1231 | Girdi alanı | email | type=email; name=email |
| 1232 | Girdi alanı | department | name=department |
| 1233 | Girdi alanı | jobTitle | name=jobTitle |
| 1234 | Girdi alanı | phone | type=tel; name=phone |
| 1235 | Girdi alanı | location | name=location |
| 1236 | Girdi alanı | startedOn | type=date; name=startedOn |
| 1237 | Seçim alanı | companyUnitId | name=companyUnitId; seçenekler=(değer yok): Birim seçilmedi |
| 1238 | Girdi alanı | newEmployeeManagerSearch | id=newEmployeeManagerSearch |
| 1239 | Seçim alanı | managerEmployeeId | id=newEmployeeManagerId; name=managerEmployeeId; seçenekler=(değer yok): Yönetici seçilmedi |
| 1240 | Girdi alanı | employeeImportFile | id=employeeImportFile; type=file; başlangıçta gizli |
| 1241 | Girdi alanı | consumableImportFile | id=consumableImportFile; type=file; başlangıçta gizli |
| 1242 | Seçim alanı | Sarf malzemesi eşleme profili | id=consumableImportProfile; seçenekler=(değer yok): Profil seçin |
| 1243 | Girdi alanı | consumableImportProfileName | id=consumableImportProfileName |
| 1244 | Girdi alanı | accessoryImportFile | id=accessoryImportFile; type=file; başlangıçta gizli |
| 1245 | Seçim alanı | Aksesuar eşleme profili | id=accessoryImportProfile; seçenekler=(değer yok): Profil seçin |
| 1246 | Girdi alanı | accessoryImportProfileName | id=accessoryImportProfileName |
| 1247 | Girdi alanı | displayName | name=displayName |
| 1248 | Girdi alanı | employeeNumber | name=employeeNumber |
| 1249 | Girdi alanı | email | type=email; name=email |
| 1250 | Girdi alanı | department | name=department |
| 1251 | Girdi alanı | jobTitle | name=jobTitle |
| 1252 | Girdi alanı | phone | type=tel; name=phone |
| 1253 | Girdi alanı | location | name=location |
| 1254 | Girdi alanı | startedOn | type=date; name=startedOn |
| 1255 | Seçim alanı | companyUnitId | name=companyUnitId; seçenekler=(değer yok): Birim seçilmedi |
| 1256 | Girdi alanı | value | id=textEntryValue; type=text; name=value |
| 1257 | Girdi alanı | Yönetici ara | id=employeeManagerSearch |
| 1258 | Seçim alanı | Yönetici seç | id=employeeManagerSelect; seçenekler=(değer yok): Yönetici yok |
| 1259 | Girdi alanı | fullName | name=fullName |
| 1260 | Girdi alanı | email | type=email; name=email |
| 1261 | Seçim alanı | roleKey | name=roleKey; seçenekler=asset_operator: Envanter operatörü, asset_manager: Envanter yöneticisi, auditor: Denetçi, viewer: Salt okunur, finance: Muhasebe (salt okunur), hr_operator: İK operatörü, admin: Yönetici |
| 1262 | Girdi alanı | userId | type=hidden; name=userId; başlangıçta gizli |
| 1263 | Seçim alanı | mode | id=memberLocationAccessMode; name=mode; seçenekler=all: Tüm konumlar, restricted: Yalnız seçilen konumlar |
| 1264 | Seçim alanı | companyUnitMode | id=memberCompanyUnitAccessMode; name=companyUnitMode; seçenekler=all: Tüm şirket birimleri, restricted: Yalnız seçilen şirket birimleri |
| 1265 | Girdi alanı | assetName | name=assetName |
| 1266 | Girdi alanı | assetTag | name=assetTag |
| 1267 | Seçim alanı | categoryId | name=categoryId; seçenekler=(değer yok): Kategori seçin |
| 1268 | Seçim alanı | brandId | name=brandId; seçenekler=(değer yok): Marka seçin |
| 1269 | Seçim alanı | modelId | name=modelId; seçenekler=(değer yok): Model seçin |
| 1270 | Girdi alanı | serialNumber | name=serialNumber |
| 1271 | Girdi alanı | location | name=location |
| 1272 | Seçim alanı | defaultLocationCatalogId | name=defaultLocationCatalogId; seçenekler=(değer yok): Varsayılan konum seçin |
| 1273 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1274 | Girdi alanı | warrantyExpiresAt | type=date; name=warrantyExpiresAt |
| 1275 | Girdi alanı | insuranceProvider | name=insuranceProvider |
| 1276 | Girdi alanı | insurancePolicyNumber | name=insurancePolicyNumber |
| 1277 | Girdi alanı | insuranceExpiresAt | type=date; name=insuranceExpiresAt |
| 1278 | Girdi alanı | purchaseCost | type=number; name=purchaseCost |
| 1279 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY — Türk lirası, USD: USD — ABD doları, EUR: EUR — Euro |
| 1280 | Metin alanı | notes | name=notes |
| 1281 | Girdi alanı | isRequestable | type=checkbox; name=isRequestable |
| 1282 | Girdi alanı | isByod | type=checkbox; name=isByod |
| 1283 | Girdi alanı | email | type=email; name=email |
| 1284 | Girdi alanı | password | type=password; name=password |
| 1285 | Girdi alanı | rememberMe | type=checkbox; name=rememberMe |
| 1286 | Girdi alanı | twoFactorCode | name=twoFactorCode |
| 1287 | Seçim alanı | companyId | id=loginCompanyId; name=companyId; seçenekler=(değer yok): Şirket seçin |
| 1288 | Girdi alanı | widget | type=checkbox; name=widget |
| 1289 | Girdi alanı | widget | type=checkbox; name=widget |
| 1290 | Girdi alanı | widget | type=checkbox; name=widget |
| 1291 | Girdi alanı | widget | type=checkbox; name=widget |
| 1292 | Girdi alanı | widget | type=checkbox; name=widget |
| 1293 | Seçim alanı | category: Kategori, brand: Marka, model: Model, location: Konum, status: Durum | id=dashboardChartDimension; seçenekler=category: Kategori, brand: Marka, model: Model, location: Konum, status: Durum |
| 1294 | Seçim alanı | bar: Yatay çubuk, donut: Halka grafik | id=dashboardChartType; seçenekler=bar: Yatay çubuk, donut: Halka grafik |
| 1295 | Girdi alanı | email | type=email; name=email |
| 1296 | Seçim alanı | Kayıtlı sütun eşlemesi | id=assetImportProfile; seçenekler=(değer yok): Profil seçin |
| 1297 | Girdi alanı | assetImportProfileName | id=assetImportProfileName |
| 1298 | Girdi alanı | importPreviewFile | id=importPreviewFile; type=file |
| 1299 | Girdi alanı | quantity | type=number; name=quantity |
| 1300 | Metin alanı | notes | name=notes |
| 1301 | Metin alanı | accessoryOrderApprovalNote | id=accessoryOrderApprovalNote |
| 1302 | Girdi alanı | file | type=file; name=file |
| 1303 | Girdi alanı | orderNumber | name=orderNumber |
| 1304 | Girdi alanı | supplier | name=supplier |
| 1305 | Girdi alanı | orderDate | type=date; name=orderDate |
| 1306 | Girdi alanı | expectedOn | type=date; name=expectedOn |
| 1307 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY, USD: USD, EUR: EUR |
| 1308 | Girdi alanı | taxRatePercent | type=number; name=taxRatePercent |
| 1309 | Girdi alanı | discountAmount | type=number; name=discountAmount |
| 1310 | Girdi alanı | freightAmount | type=number; name=freightAmount |
| 1311 | Girdi alanı | notes | name=notes |
| 1312 | Girdi alanı | orderNumber | name=orderNumber |
| 1313 | Girdi alanı | supplier | name=supplier |
| 1314 | Girdi alanı | orderDate | type=date; name=orderDate |
| 1315 | Girdi alanı | expectedOn | type=date; name=expectedOn |
| 1316 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY, USD: USD, EUR: EUR |
| 1317 | Girdi alanı | taxRatePercent | type=number; name=taxRatePercent |
| 1318 | Girdi alanı | discountAmount | type=number; name=discountAmount |
| 1319 | Girdi alanı | freightAmount | type=number; name=freightAmount |
| 1320 | Girdi alanı | notes | name=notes |
| 1321 | Metin alanı | stockOrderApprovalNote | id=stockOrderApprovalNote |
| 1322 | Girdi alanı | file | id=stockOrderAttachmentInput; type=file; name=file |
| 1323 | Girdi alanı | receivedOn | type=date; name=receivedOn |
| 1324 | Girdi alanı | notes | name=notes |
| 1325 | Girdi alanı | name | name=name |
| 1326 | Girdi alanı | description | name=description |
| 1327 | Seçim alanı | assetId | name=assetId; seçenekler=(değer yok): Varlık seçin |
| 1328 | Girdi alanı | year | type=number; name=year |
| 1329 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1330 | Girdi alanı | amount | type=number; name=amount |
| 1331 | Girdi alanı | orderNumber | name=orderNumber |
| 1332 | Girdi alanı | supplier | name=supplier |
| 1333 | Girdi alanı | orderDate | type=date; name=orderDate |
| 1334 | Girdi alanı | expectedOn | type=date; name=expectedOn |
| 1335 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1336 | Girdi alanı | taxRatePercent | type=number; name=taxRatePercent |
| 1337 | Girdi alanı | discountAmount | type=number; name=discountAmount |
| 1338 | Girdi alanı | freightAmount | type=number; name=freightAmount |
| 1339 | Girdi alanı | notes | name=notes |
| 1340 | Metin alanı | assetOrderApprovalNote | id=assetOrderApprovalNote |
| 1341 | Girdi alanı | assetOrderAttachmentFiles | id=assetOrderAttachmentFiles; type=file |
| 1342 | Girdi alanı | receivedOn | type=date; name=receivedOn |
| 1343 | Girdi alanı | notes | name=notes |
| 1344 | Girdi alanı | receivedOn | type=date; name=receivedOn |
| 1345 | Girdi alanı | notes | name=notes |
| 1346 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1347 | Girdi alanı | name | name=name |
| 1348 | Girdi alanı | category | name=category |
| 1349 | Girdi alanı | modelNumber | name=modelNumber |
| 1350 | Girdi alanı | itemNumber | name=itemNumber |
| 1351 | Girdi alanı | supplier | name=supplier |
| 1352 | Girdi alanı | purchaseOrderReference | name=purchaseOrderReference |
| 1353 | Girdi alanı | location | name=location |
| 1354 | Girdi alanı | minimumQuantity | type=number; name=minimumQuantity |
| 1355 | Girdi alanı | quantity | type=number; name=quantity |
| 1356 | Girdi alanı | unitCost | type=number; name=unitCost |
| 1357 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1358 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1359 | Girdi alanı | warrantyExpiresAt | type=date; name=warrantyExpiresAt |
| 1360 | Girdi alanı | notes | name=notes |
| 1361 | Girdi alanı | isRequestable | type=checkbox; name=isRequestable |
| 1362 | Girdi alanı | quantity | type=number; name=quantity |
| 1363 | Girdi alanı | notes | name=notes |
| 1364 | Seçim alanı | employeeId | name=employeeId; seçenekler=(değer yok): Çalışan seçin |
| 1365 | Girdi alanı | quantity | type=number; name=quantity |
| 1366 | Girdi alanı | notes | name=notes |
| 1367 | Girdi alanı | licenseKeyInput | id=licenseKeyInput; type=password |
| 1368 | Girdi alanı | file | type=file; name=file |
| 1369 | Girdi alanı | isEulaDocument | type=checkbox; name=isEulaDocument |
| 1370 | Girdi alanı | eulaVersion | name=eulaVersion |
| 1371 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1372 | Seçim alanı | supplierId | name=supplierId; seçenekler=(değer yok): Tedarikçi seçin |
| 1373 | Girdi alanı | orderNumber | name=orderNumber |
| 1374 | Girdi alanı | orderDate | type=date; name=orderDate |
| 1375 | Girdi alanı | seatsPurchased | type=number; name=seatsPurchased |
| 1376 | Girdi alanı | unitCost | type=number; name=unitCost |
| 1377 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1378 | Metin alanı | notes | name=notes |
| 1379 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1380 | Girdi alanı | name | name=name |
| 1381 | Girdi alanı | publisher | name=publisher |
| 1382 | Girdi alanı | productVersion | name=productVersion |
| 1383 | Seçim alanı | licenseType | name=licenseType; seçenekler=Subscription: Abonelik, Perpetual: Süresiz, Device: Cihaz başına, Site: Şirket geneli, Trial: Deneme, Other: Diğer |
| 1384 | Girdi alanı | licenseReference | name=licenseReference |
| 1385 | Girdi alanı | supplier | name=supplier |
| 1386 | Girdi alanı | purchaseOrderReference | name=purchaseOrderReference |
| 1387 | Girdi alanı | seats | type=number; name=seats |
| 1388 | Girdi alanı | unitCost | type=number; name=unitCost |
| 1389 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1390 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1391 | Girdi alanı | expirationDate | type=date; name=expirationDate |
| 1392 | Girdi alanı | autoRenew | type=checkbox; name=autoRenew |
| 1393 | Girdi alanı | requiresEulaAcceptance | type=checkbox; name=requiresEulaAcceptance |
| 1394 | Girdi alanı | notes | name=notes |
| 1395 | Girdi alanı | licenseImportFile | id=licenseImportFile; type=file; başlangıçta gizli |
| 1396 | Seçim alanı | Lisans eşleme profili | id=licenseImportProfile; seçenekler=(değer yok): Profil seçin |
| 1397 | Girdi alanı | licenseImportProfileName | id=licenseImportProfileName |
| 1398 | Girdi alanı | eulaAccepted | id=licenseEulaAccepted; type=checkbox; name=eulaAccepted |
| 1399 | Seçim alanı | targetType | name=targetType; seçenekler=employee: Çalışan, asset: Cihaz / varlık |
| 1400 | Seçim alanı | employeeId | name=employeeId; seçenekler=(değer yok): Çalışan seçin |
| 1401 | Girdi alanı | licenseBulkEmployeeMode | id=licenseBulkEmployeeMode; type=checkbox |
| 1402 | Girdi alanı | licenseAssetSearch | id=licenseAssetSearch; type=search |
| 1403 | Seçim alanı | assetId | name=assetId; seçenekler=(değer yok): Önce arama yapın |
| 1404 | Girdi alanı | notes | name=notes |
| 1405 | Girdi alanı | componentImportFile | id=componentImportFile; type=file; başlangıçta gizli |
| 1406 | Seçim alanı | Bileşen eşleme profili | id=componentImportProfile; seçenekler=(değer yok): Profil seçin |
| 1407 | Girdi alanı | componentImportProfileName | id=componentImportProfileName |
| 1408 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1409 | Girdi alanı | name | name=name |
| 1410 | Girdi alanı | category | name=category |
| 1411 | Girdi alanı | modelNumber | name=modelNumber |
| 1412 | Girdi alanı | itemNumber | name=itemNumber |
| 1413 | Girdi alanı | serialNumber | name=serialNumber |
| 1414 | Girdi alanı | supplier | name=supplier |
| 1415 | Girdi alanı | purchaseOrderReference | name=purchaseOrderReference |
| 1416 | Girdi alanı | location | name=location |
| 1417 | Girdi alanı | minimumQuantity | type=number; name=minimumQuantity |
| 1418 | Girdi alanı | quantity | type=number; name=quantity |
| 1419 | Girdi alanı | unitCost | type=number; name=unitCost |
| 1420 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1421 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1422 | Girdi alanı | warrantyExpiresAt | type=date; name=warrantyExpiresAt |
| 1423 | Girdi alanı | notes | name=notes |
| 1424 | Girdi alanı | quantity | type=number; name=quantity |
| 1425 | Girdi alanı | notes | name=notes |
| 1426 | Seçim alanı | assetId | name=assetId; seçenekler=(değer yok): Varlık seçin |
| 1427 | Girdi alanı | quantity | type=number; name=quantity |
| 1428 | Girdi alanı | notes | name=notes |
| 1429 | Girdi alanı | allocationId | type=hidden; name=allocationId; başlangıçta gizli |
| 1430 | Girdi alanı | quantity | type=number; name=quantity |
| 1431 | Girdi alanı | notes | name=notes |
| 1432 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1433 | Girdi alanı | name | name=name |
| 1434 | Girdi alanı | description | name=description |
| 1435 | Girdi alanı | accessoryKitItemSearch | id=accessoryKitItemSearch; type=search |
| 1436 | Girdi alanı | accessoryId | type=hidden; name=accessoryId; başlangıçta gizli |
| 1437 | Girdi alanı | name | name=name |
| 1438 | Seçim alanı | categoryId | name=categoryId; seçenekler=(değer yok): Kategori seçin (isteğe bağlı) |
| 1439 | Seçim alanı | brandId | name=brandId; seçenekler=(değer yok): Marka seçin (isteğe bağlı) |
| 1440 | Seçim alanı | modelId | name=modelId; seçenekler=(değer yok): Model seçin (isteğe bağlı) |
| 1441 | Girdi alanı | modelPhoto | type=file; name=modelPhoto |
| 1442 | Girdi alanı | modelNumber | name=modelNumber |
| 1443 | Girdi alanı | itemNumber | name=itemNumber |
| 1444 | Girdi alanı | supplier | name=supplier |
| 1445 | Girdi alanı | purchaseOrderReference | name=purchaseOrderReference |
| 1446 | Girdi alanı | location | name=location |
| 1447 | Girdi alanı | minimumQuantity | type=number; name=minimumQuantity |
| 1448 | Girdi alanı | quantity | type=number; name=quantity |
| 1449 | Girdi alanı | unitCost | type=number; name=unitCost |
| 1450 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY, USD: USD, EUR: EUR |
| 1451 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1452 | Girdi alanı | warrantyExpiresAt | type=date; name=warrantyExpiresAt |
| 1453 | Girdi alanı | notes | name=notes |
| 1454 | Girdi alanı | isRequestable | type=checkbox; name=isRequestable |
| 1455 | Girdi alanı | quantity | type=number; name=quantity |
| 1456 | Girdi alanı | notes | name=notes |
| 1457 | Girdi alanı | targetType | type=radio; name=targetType |
| 1458 | Girdi alanı | targetType | type=radio; name=targetType |
| 1459 | Girdi alanı | targetType | type=radio; name=targetType |
| 1460 | Seçim alanı | employeeId | name=employeeId |
| 1461 | Seçim alanı | assetId | name=assetId |
| 1462 | Girdi alanı | location | name=location |
| 1463 | Girdi alanı | quantity | type=number; name=quantity |
| 1464 | Girdi alanı | accessoryKitSets | id=accessoryKitSets; type=number |
| 1465 | Girdi alanı | notes | name=notes |
| 1466 | Seçim alanı | view: Varlık ayrıntısını aç, assign: Uygunsa zimmet formunu aç, return: Zimmetliyse iade onayını göster | id=scanAssetAction; seçenekler=view: Varlık ayrıntısını aç, assign: Uygunsa zimmet formunu aç, return: Zimmetliyse iade onayını göster |
| 1467 | Girdi alanı | code | id=scanAssetCode; name=code |
| 1468 | Girdi alanı | kind | type=hidden; name=kind; başlangıçta gizli |
| 1469 | Girdi alanı | currentName | type=hidden; name=currentName; başlangıçta gizli |
| 1470 | Girdi alanı | locationId | type=hidden; name=locationId; başlangıçta gizli |
| 1471 | Girdi alanı | departmentId | type=hidden; name=departmentId; başlangıçta gizli |
| 1472 | Girdi alanı | newName | name=newName |
| 1473 | Seçim alanı | parentDepartmentId | name=parentDepartmentId; seçenekler=(değer yok): Ana departman |
| 1474 | Seçim alanı | locationCatalogId | name=locationCatalogId; seçenekler=(değer yok): Konum seçilmedi |
| 1475 | Girdi alanı | name | name=name |
| 1476 | Seçim alanı | parentDepartmentId | name=parentDepartmentId; seçenekler=(değer yok): Ana departman |
| 1477 | Seçim alanı | locationCatalogId | name=locationCatalogId; seçenekler=(değer yok): Konum seçilmedi |
| 1478 | Seçim alanı | targetDepartmentId | name=targetDepartmentId |
| 1479 | Seçim alanı | parentLocationId | name=parentLocationId; seçenekler=(değer yok): Üst seviye · kat veya bina |
| 1480 | Girdi alanı | name | name=name |
| 1481 | Girdi alanı | locationId | type=hidden; name=locationId; başlangıçta gizli |
| 1482 | Girdi alanı | locationManagerSearch | id=locationManagerSearch; type=search |
| 1483 | Seçim alanı | employeeId | id=locationManagerEmployee; name=employeeId; seçenekler=(değer yok): Sorumlu atanmamış |
| 1484 | Girdi alanı | departmentId | type=hidden; name=departmentId; başlangıçta gizli |
| 1485 | Girdi alanı | departmentManagerSearch | id=departmentManagerSearch; type=search |
| 1486 | Seçim alanı | employeeId | id=departmentManagerEmployee; name=employeeId; seçenekler=(değer yok): Yetkili atanmamış |
| 1487 | Girdi alanı | name | name=name |
| 1488 | Girdi alanı | modelNumber | name=modelNumber |
| 1489 | Metin alanı | note | name=note |
| 1490 | Metin alanı | note | name=note |
| 1491 | Metin alanı | note | name=note |
| 1492 | Girdi alanı | name | name=name |
| 1493 | Girdi alanı | code | name=code |
| 1494 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1495 | Girdi alanı | name | name=name |
| 1496 | Girdi alanı | contactName | name=contactName |
| 1497 | Girdi alanı | email | type=email; name=email |
| 1498 | Girdi alanı | phone | name=phone |
| 1499 | Girdi alanı | website | type=url; name=website |
| 1500 | Metin alanı | notes | name=notes |
| 1501 | Girdi alanı | logo | type=file; name=logo |
| 1502 | Girdi alanı | Tedarikçilerde ara | id=supplierCatalogSearch |
| 1503 | Girdi alanı | showArchivedSuppliers | id=showArchivedSuppliers; type=checkbox |
| 1504 | Seçim alanı | companySwitchTarget | id=companySwitchTarget |
| 1505 | Girdi alanı | accountPhotoInput | id=accountPhotoInput; type=file; başlangıçta gizli |
| 1506 | Girdi alanı | ownCurrentPassword | id=ownCurrentPassword; type=password |
| 1507 | Girdi alanı | ownNewPassword | id=ownNewPassword; type=password |
| 1508 | Girdi alanı | ownConfirmPassword | id=ownConfirmPassword; type=password |
| 1509 | Girdi alanı | ownPasswordTwoFactorCode | id=ownPasswordTwoFactorCode |
| 1510 | Girdi alanı | twoFactorCurrentPassword | id=twoFactorCurrentPassword; type=password |
| 1511 | Girdi alanı | twoFactorCode | id=twoFactorCode |
| 1512 | Girdi alanı | itemType | type=hidden; name=itemType; başlangıçta gizli |
| 1513 | Girdi alanı | itemId | type=hidden; name=itemId; başlangıçta gizli |
| 1514 | Girdi alanı | requestEmployeeSearch | id=requestEmployeeSearch |
| 1515 | Seçim alanı | employeeId | id=requestEmployee; name=employeeId; seçenekler=(değer yok): Çalışan seçin |
| 1516 | Girdi alanı | quantity | type=number; name=quantity |
| 1517 | Metin alanı | note | name=note |
| 1518 | Girdi alanı | file | id=modelAttachmentFile; type=file; name=file |
| 1519 | Girdi alanı | returnedOn | type=date; name=returnedOn |
| 1520 | Girdi alanı | reason | name=reason |
| 1521 | Girdi alanı | notes | name=notes |
| 1522 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1523 | Girdi alanı | name | name=name |
| 1524 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1525 | Girdi alanı | serialNumber | name=serialNumber |
| 1526 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1527 | Seçim alanı | categoryId | name=categoryId |
| 1528 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1529 | Seçim alanı | brandId | name=brandId |
| 1530 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1531 | Seçim alanı | modelId | name=modelId |
| 1532 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1533 | Seçim alanı | defaultLocationCatalogId | name=defaultLocationCatalogId |
| 1534 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1535 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1536 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1537 | Girdi alanı | warrantyExpiresAt | type=date; name=warrantyExpiresAt |
| 1538 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1539 | Girdi alanı | insuranceProvider | name=insuranceProvider |
| 1540 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1541 | Girdi alanı | insurancePolicyNumber | name=insurancePolicyNumber |
| 1542 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1543 | Girdi alanı | insuranceExpiresAt | type=date; name=insuranceExpiresAt |
| 1544 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1545 | Girdi alanı | purchaseCost | type=number; name=purchaseCost |
| 1546 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY — Türk lirası, USD: USD — ABD doları, EUR: EUR — Euro |
| 1547 | Seçim alanı | isRequestable | name=isRequestable; seçenekler=(değer yok): Değişiklik yok, true: Evet, false: Hayır |
| 1548 | Seçim alanı | isByod | name=isByod; seçenekler=(değer yok): Değişiklik yok, true: Evet, false: Hayır |
| 1549 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1550 | Metin alanı | notes | name=notes |
| 1551 | Metin alanı | note | name=note |
| 1552 | Seçim alanı | assetId | name=assetId |
| 1553 | Girdi alanı | notes | name=notes |
| 1554 | Girdi alanı | notes | name=notes |
| 1555 | Girdi alanı | ${escapeHtml(assignment.targetName \|\| 'Açık zimmet')} iadesini seç | id=${assignment.id}; type=checkbox |
| 1556 | Girdi alanı | ${escapeHtml(assignment.targetName \|\| 'Açık zimmet')} için toplu iade adedi | type=number |
| 1557 | Girdi alanı | notes | name=notes |
| 1558 | Girdi alanı | ${escapeHtml(asset.assetTag)} varlığını iade et | type=checkbox |
| 1559 | Girdi alanı | ${escapeHtml(label \|\| 'Sipariş kalemi')} iade adedi | type=number |
| 1560 | Seçim alanı | (değer yok): Kalem seçin, ${item.itemType}\|${item.id}: ${item.typeLabel} · ${escapeHtml(item.name)} · ${escapeHtml(item.itemNumber \|\| item.modelNumber \|\| 'Stok kodu yok')} · stok ${fmt.format(item.totalQuantity)} | seçenekler=(değer yok): Kalem seçin, ${item.itemType}\|${item.id}: ${item.typeLabel} · ${escapeHtml(item.name)} · ${escapeHtml(item.itemNumber \|\| item.modelNumber \|\| 'Stok kodu yok')} · stok ${fmt.format(item.totalQuantity)} |
| 1561 | Girdi alanı | (etiketsiz) | type=number |
| 1562 | Girdi alanı | (etiketsiz) | type=number |
| 1563 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1564 | Girdi alanı | ${escapeHtml(quantityLabel)} | type=number |
| 1565 | Girdi alanı | ${escapeHtml(line.itemName)} teslim adedi | type=number |
| 1566 | Girdi alanı | (etiketsiz) |  |
| 1567 | Seçim alanı | (etiketsiz) |  |
| 1568 | Seçim alanı | (etiketsiz) |  |
| 1569 | Seçim alanı | (etiketsiz) |  |
| 1570 | Seçim alanı | (etiketsiz) |  |
| 1571 | Girdi alanı | (etiketsiz) | type=number |
| 1572 | Girdi alanı | (etiketsiz) | type=number |
| 1573 | Girdi alanı | (etiketsiz) | type=date |
| 1574 | Girdi alanı | ${escapeHtml(line.name)} teslim adedi | type=number |
| 1575 | Seçim alanı | (değer yok): Aksesuar seçin, ${item.id}: ${escapeHtml(item.name)} · ${escapeHtml(item.itemNumber \|\| item.modelNumber \|\| 'Stok kodu yok')} · stok ${fmt.format(item.totalQuantity)} | seçenekler=(değer yok): Aksesuar seçin, ${item.id}: ${escapeHtml(item.name)} · ${escapeHtml(item.itemNumber \|\| item.modelNumber \|\| 'Stok kodu yok')} · stok ${fmt.format(item.totalQuantity)} |
| 1576 | Girdi alanı | (etiketsiz) | type=number |
| 1577 | Girdi alanı | (etiketsiz) | type=number |
| 1578 | Girdi alanı | ${escapeHtml(line.accessoryName)} teslim adedi | type=number |
| 1579 | Girdi alanı | ${escapeHtml(item.name)} model numarası |  |
| 1580 | Girdi alanı | (etiketsiz) | type=file |
| 1581 | Girdi alanı | (etiketsiz) | type=number |
| 1582 | Girdi alanı | (etiketsiz) | type=number |
| 1583 | Girdi alanı | (etiketsiz) | type=number |
| 1584 | Seçim alanı | amount: ${translateUiText('Tutar')}, percent: ${translateUiText('Yüzde')} | type=${item.id}; seçenekler=amount: ${translateUiText('Tutar')}, percent: ${translateUiText('Yüzde')} |
| 1585 | Seçim alanı | linear: ${translateUiText('Doğrusal')}, half_1: ${translateUiText('Yarım yıl · ilk dönem')}, half_2: ${translateUiText('Yarım yıl · satın alma dönemine göre')} | seçenekler=linear: ${translateUiText('Doğrusal')}, half_1: ${translateUiText('Yarım yıl · ilk dönem')}, half_2: ${translateUiText('Yarım yıl · satın alma dönemine göre')} |
| 1586 | Girdi alanı | (etiketsiz) | type=url |
| 1587 | Girdi alanı | (etiketsiz) | type=url |
| 1588 | Girdi alanı | (etiketsiz) | type=url |
| 1589 | Girdi alanı | (etiketsiz) | type=tel |
| 1590 | Girdi alanı | (etiketsiz) | type=email |
| 1591 | Girdi alanı | (etiketsiz) | type=file |
| 1592 | Girdi alanı | ${escapeHtml(item.name)} ${translateUiText('Ömür sonu (EOL)')} | type=date |
| 1593 | Girdi alanı | ${escapeHtml(item.name)} ${translateUiText('Kayıt adını düzenle')} | name=${item.id} |
| 1594 | Seçim alanı | (değer yok): Özel alan seçin | id=assetCustomFieldFilter; seçenekler=(değer yok): Özel alan seçin |
| 1595 | Seçim alanı | equals: Değeri eşit, contains: Metin içinde, empty: Değer girilmemiş | id=assetCustomFieldMode; seçenekler=equals: Değeri eşit, contains: Metin içinde, empty: Değer girilmemiş |
| 1596 | Girdi alanı | assetCustomFieldValue | id=assetCustomFieldValue |
| 1597 | Girdi alanı | key | name=key |
| 1598 | Girdi alanı | name | name=name |
| 1599 | Girdi alanı | groupName | name=groupName |
| 1600 | Seçim alanı | categoryId | name=categoryId; seçenekler=(değer yok): Tüm kategoriler |
| 1601 | Seçim alanı | modelId | name=modelId; seçenekler=(değer yok): Tüm modeller |
| 1602 | Seçim alanı | fieldType | name=fieldType; seçenekler=text: Metin, number: Sayı, date: Tarih, checkbox: Onay kutusu, select: Seçim listesi, sensitive: Hassas (şifreli) |
| 1603 | Metin alanı | options | name=options |
| 1604 | Girdi alanı | isRequired | type=checkbox; name=isRequired |
| 1605 | Girdi alanı | sortOrder | type=number; name=sortOrder |
| 1606 | Girdi alanı | name | name=name |
| 1607 | Girdi alanı | groupName | name=groupName |
| 1608 | Seçim alanı | categoryId | name=categoryId; seçenekler=(değer yok): Tüm kategoriler |
| 1609 | Seçim alanı | modelId | name=modelId; seçenekler=(değer yok): Tüm modeller |
| 1610 | Seçim alanı | Çalışan eşleme profili | id=employeeImportProfile; seçenekler=(değer yok): Profil seçin |
| 1611 | Girdi alanı | employeeImportProfileName | id=employeeImportProfileName |
| 1612 | Metin alanı | note | name=note |
| 1613 | Form | maintenanceAttachmentForm | id=maintenanceAttachmentForm; başlangıçta gizli |
| 1614 | Form | companyUnitForm | id=companyUnitForm |
| 1615 | Form | inventoryRequestActionForm | id=inventoryRequestActionForm |
| 1616 | Form | companyNotificationPreferencesForm | id=companyNotificationPreferencesForm |
| 1617 | Form | labelSettingsForm | id=labelSettingsForm |
| 1618 | Form | assetStatusLabelsForm | id=assetStatusLabelsForm |
| 1619 | Form | assetStatusConfigForm | id=assetStatusConfigForm |
| 1620 | Form | catalogCreateForm | id=catalogCreateForm |
| 1621 | Form | companyProfileForm | id=companyProfileForm |
| 1622 | Form | companyLogoForm | id=companyLogoForm |
| 1623 | Form | reservationForm | id=reservationForm |
| 1624 | Form | reservationActionForm | id=reservationActionForm |
| 1625 | Form | taskForm | id=taskForm |
| 1626 | Form | supportForm | id=supportForm |
| 1627 | Form | supportReplyForm | id=supportReplyForm |
| 1628 | Form | companySecurityPolicyForm | id=companySecurityPolicyForm |
| 1629 | Form | integrationTokenForm | id=integrationTokenForm |
| 1630 | Form | webhookForm | id=webhookForm |
| 1631 | Form | companyRoleForm | id=companyRoleForm |
| 1632 | Form | licenseRevokeForm | id=licenseRevokeForm |
| 1633 | Form | licenseImportProfileDeleteForm | id=licenseImportProfileDeleteForm |
| 1634 | Form | contractForm | id=contractForm |
| 1635 | Form | contractActionForm | id=contractActionForm |
| 1636 | Form | assetNoteForm | id=assetNoteForm; başlangıçta gizli |
| 1637 | Form | assetAttachmentForm | id=assetAttachmentForm; başlangıçta gizli |
| 1638 | Form | assetAuditForm | id=assetAuditForm |
| 1639 | Form | assetAuditScheduleForm | id=assetAuditScheduleForm |
| 1640 | Form | maintenanceStartForm | id=maintenanceStartForm |
| 1641 | Form | maintenanceCompleteForm | id=maintenanceCompleteForm |
| 1642 | Form | assetTransferForm | id=assetTransferForm |
| 1643 | Form | editAssetForm | id=editAssetForm |
| 1644 | Form | assignForm | id=assignForm |
| 1645 | Form | newEmployeeForm | id=newEmployeeForm |
| 1646 | Form | editEmployeeForm | id=editEmployeeForm |
| 1647 | Form | employeeActionForm | id=employeeActionForm |
| 1648 | Form | textEntryForm | id=textEntryForm |
| 1649 | Form | inviteMemberForm | id=inviteMemberForm |
| 1650 | Form | memberLocationAccessForm | id=memberLocationAccessForm |
| 1651 | Form | newAssetForm | id=newAssetForm |
| 1652 | Form | loginForm | id=loginForm |
| 1653 | Form | dashboardCustomizeForm | id=dashboardCustomizeForm |
| 1654 | Form | forgotPasswordForm | id=forgotPasswordForm |
| 1655 | Form | importPreviewForm | id=importPreviewForm |
| 1656 | Form | accessoryCheckinForm | id=accessoryCheckinForm |
| 1657 | Form | accessoryOrderAttachmentForm | id=accessoryOrderAttachmentForm |
| 1658 | Form | accessoryOrderCreateForm | id=accessoryOrderCreateForm |
| 1659 | Form | stockOrderCreateForm | id=stockOrderCreateForm |
| 1660 | Form | stockOrderAttachmentForm | id=stockOrderAttachmentForm; başlangıçta gizli |
| 1661 | Form | stockOrderReceiveForm | id=stockOrderReceiveForm |
| 1662 | Form | stockOrderKitTemplateForm | id=stockOrderKitTemplateForm |
| 1663 | Form | assetOrderBudgetForm | id=assetOrderBudgetForm |
| 1664 | Form | assetOrderCreateForm | id=assetOrderCreateForm |
| 1665 | Form | assetOrderReceiveForm | id=assetOrderReceiveForm |
| 1666 | Form | accessoryOrderReceiveForm | id=accessoryOrderReceiveForm |
| 1667 | Form | consumableForm | id=consumableForm |
| 1668 | Form | consumableStockForm | id=consumableStockForm |
| 1669 | Form | consumableCheckoutForm | id=consumableCheckoutForm |
| 1670 | Form | licenseAttachmentForm | id=licenseAttachmentForm |
| 1671 | Form | licensePurchaseOrderForm | id=licensePurchaseOrderForm |
| 1672 | Form | licenseForm | id=licenseForm |
| 1673 | Form | licenseActionForm | id=licenseActionForm |
| 1674 | Form | licenseAssignForm | id=licenseAssignForm |
| 1675 | Form | componentActionForm | id=componentActionForm |
| 1676 | Form | componentForm | id=componentForm |
| 1677 | Form | componentStockForm | id=componentStockForm |
| 1678 | Form | componentAllocateForm | id=componentAllocateForm |
| 1679 | Form | componentRemoveForm | id=componentRemoveForm |
| 1680 | Form | accessoryKitForm | id=accessoryKitForm |
| 1681 | Form | accessoryKitActionForm | id=accessoryKitActionForm |
| 1682 | Form | accessoryForm | id=accessoryForm |
| 1683 | Form | accessoryStockForm | id=accessoryStockForm |
| 1684 | Form | accessoryCheckoutForm | id=accessoryCheckoutForm |
| 1685 | Form | scanAssetForm | id=scanAssetForm |
| 1686 | Form | directoryRenameForm | id=directoryRenameForm |
| 1687 | Form | createDepartmentForm | id=createDepartmentForm |
| 1688 | Form | departmentMoveForm | id=departmentMoveForm |
| 1689 | Form | createLocationForm | id=createLocationForm |
| 1690 | Form | locationManagerForm | id=locationManagerForm |
| 1691 | Form | departmentManagerForm | id=departmentManagerForm |
| 1692 | Form | quickLookupForm | id=quickLookupForm |
| 1693 | Form | returnAssetForm | id=returnAssetForm |
| 1694 | Form | assetTrashConfirmForm | id=assetTrashConfirmForm |
| 1695 | Form | assetStatusForm | id=assetStatusForm |
| 1696 | Form | assetAttachmentRemoveForm | id=assetAttachmentRemoveForm |
| 1697 | Form | savedAssetSearchForm | id=savedAssetSearchForm |
| 1698 | Form | companyMfaEnrollmentForm | id=companyMfaEnrollmentForm |
| 1699 | Form | supplierCatalogForm | id=supplierCatalogForm |
| 1700 | Form | inventoryRequestForm | id=inventoryRequestForm |
| 1701 | Form | modelAttachmentForm | id=modelAttachmentForm; başlangıçta gizli |
| 1702 | Form | purchaseOrderReturnForm | id=purchaseOrderReturnForm |
| 1703 | Form | bulkEditAssetForm | id=bulkEditAssetForm |
| 1704 | Form | (etiketsiz) |  |
| 1705 | Form | (etiketsiz) |  |
| 1706 | Form | (etiketsiz) |  |
| 1707 | Form | assetCustomFieldForm | id=assetCustomFieldForm |
| 1708 | Form | editAssetCustomFieldForm | id=editAssetCustomFieldForm |
| 1709 | Form | bulkAssetActionForm | id=bulkAssetActionForm |
| 1710 | Modal | Kapat | id=employeeHandoverDialog; type=button; başlangıçta gizli |
| 1711 | Modal | Kapat | id=maintenanceAttachmentDialog; type=button; name=file; başlangıçta gizli |
| 1712 | Modal | Kapat | id=companyUnitDialog; type=button; name=id; başlangıçta gizli |
| 1713 | Modal | Kapat | id=inventoryRequestActionDialog; type=button; name=note |
| 1714 | Modal | Kapat | id=reservationActionDialog; type=button; name=note |
| 1715 | Modal | Kapat | id=integrationSecretDialog; type=button |
| 1716 | Modal | Kapat | id=companyRoleDialog; type=button; name=roleKey; başlangıçta gizli |
| 1717 | Modal | Kapat | id=deactivateCompanyRoleDialog; type=button |
| 1718 | Modal | Kapat | id=licenseRevokeDialog; type=button |
| 1719 | Modal | Kapat | id=licenseImportProfileDeleteDialog; type=button |
| 1720 | Modal | Kapat | id=commandPaletteDialog; type=button |
| 1721 | Modal | Kapat | id=keyboardShortcutsDialog; type=button |
| 1722 | Modal | Kapat | id=contractFormDialog; type=button; name=id; başlangıçta gizli |
| 1723 | Modal | Kapat | id=contractDetailDialog; type=button; başlangıçta gizli |
| 1724 | Modal | Kapat | id=contractActionDialog; type=button |
| 1725 | Modal | Kapat | id=appearanceDialog; type=button; başlangıçta gizli |
| 1726 | Modal | Kapat | id=assetDialog; type=button; name=note; başlangıçta gizli |
| 1727 | Modal | Kapat | id=assetAuditDialog; type=button; name=auditDate; başlangıçta gizli |
| 1728 | Modal | Kapat | id=assetAuditScheduleDialog; type=button; name=nextAuditOn |
| 1729 | Modal | Kapat | id=maintenanceStartDialog; type=button; name=maintenanceType; başlangıçta gizli |
| 1730 | Modal | Kapat | id=maintenanceCompleteDialog; type=button; name=completedOn |
| 1731 | Modal | Kapat | id=assetTransferDialog; type=button; name=targetLocation |
| 1732 | Modal | Kapat | id=editAssetDialog; type=button; name=expectedUpdatedAt; başlangıçta gizli |
| 1733 | Modal | Kapat | id=assignDialog; type=button; name=employeeId |
| 1734 | Modal | Kapat | id=newEmployeeDialog; type=button; name=displayName |
| 1735 | Modal | Kapat | id=employeeImportDialog; type=button; başlangıçta gizli |
| 1736 | Modal | Kapat | id=consumableImportDialog; type=button; başlangıçta gizli |
| 1737 | Modal | Kapat | id=accessoryImportDialog; type=button; başlangıçta gizli |
| 1738 | Modal | Kapat | id=editEmployeeDialog; type=button; name=displayName |
| 1739 | Modal | Kapat | id=employeeActionDialog; type=button |
| 1740 | Modal | Kapat | id=textEntryDialog; type=button; name=value |
| 1741 | Modal | Kapat | id=employeeDialog; type=button; başlangıçta gizli |
| 1742 | Modal | Kapat | id=inviteMemberDialog; type=button; name=fullName |
| 1743 | Modal | Kapat | id=memberLocationAccessDialog; type=button; name=userId; başlangıçta gizli |
| 1744 | Modal | Kapat | id=newAssetDialog; type=button; name=assetName |
| 1745 | Modal | Kapat | id=dashboardCustomizeDialog; type=button; name=widget |
| 1746 | Modal | Kapat | id=forgotPasswordDialog; type=button; name=email |
| 1747 | Modal | Kapat | id=importPreviewDialog; type=button; başlangıçta gizli |
| 1748 | Modal | Kapat | id=accessoryDialog; type=button |
| 1749 | Modal | Kapat | id=accessoryCheckinDialog; type=button; name=quantity; başlangıçta gizli |
| 1750 | Modal | Kapat | id=accessoryOrderDetailDialog; type=button; name=file; başlangıçta gizli |
| 1751 | Modal | Kapat | id=accessoryOrderCreateDialog; type=button; name=orderNumber |
| 1752 | Modal | Kapat | id=stockOrderCreateDialog; type=button; name=orderNumber |
| 1753 | Modal | Kapat | id=stockOrderDetailDialog; type=button; name=file; başlangıçta gizli |
| 1754 | Modal | Kapat | id=stockOrderReceiveDialog; type=button; name=receivedOn |
| 1755 | Modal | Kapat | id=stockOrderKitTemplateDialog; type=button; name=name |
| 1756 | Modal | Kapat | id=assetOrderBudgetDialog; type=button; name=year |
| 1757 | Modal | Kapat | id=assetOrderCreateDialog; type=button; name=orderNumber |
| 1758 | Modal | Kapat | id=assetOrderDetailDialog; type=button; başlangıçta gizli |
| 1759 | Modal | Kapat | id=assetOrderReceiveDialog; type=button; name=receivedOn |
| 1760 | Modal | Kapat | id=accessoryOrderReceiveDialog; type=button; name=receivedOn |
| 1761 | Modal | Kapat | id=consumableDialog; type=button |
| 1762 | Modal | Kapat | id=consumableFormDialog; type=button; name=id; başlangıçta gizli |
| 1763 | Modal | Kapat | id=consumableStockDialog; type=button; name=quantity |
| 1764 | Modal | Kapat | id=consumableCheckoutDialog; type=button; name=employeeId |
| 1765 | Modal | Kapat | id=licenseDialog; type=button; name=file; başlangıçta gizli |
| 1766 | Modal | Kapat | id=licensePurchaseOrderFormDialog; type=button; name=id; başlangıçta gizli |
| 1767 | Modal | Kapat | id=licenseFormDialog; type=button; name=id; başlangıçta gizli |
| 1768 | Modal | Kapat | id=licenseImportDialog; type=button; başlangıçta gizli |
| 1769 | Modal | Kapat | id=licenseActionDialog; type=button; başlangıçta gizli |
| 1770 | Modal | Kapat | id=licenseAssignDialog; type=button; name=eulaAccepted; başlangıçta gizli |
| 1771 | Modal | Kapat | id=componentActionDialog; type=button |
| 1772 | Modal | Kapat | id=componentDialog; type=button |
| 1773 | Modal | Kapat | id=componentImportDialog; type=button; başlangıçta gizli |
| 1774 | Modal | Kapat | id=componentFormDialog; type=button; name=id; başlangıçta gizli |
| 1775 | Modal | quantity | id=componentStockDialog; type=button; name=quantity |
| 1776 | Modal | assetId | id=componentAllocateDialog; type=button; name=assetId |
| 1777 | Modal | Kapat | id=componentRemoveDialog; type=button; name=allocationId; başlangıçta gizli |
| 1778 | Modal | Kapat | id=accessoryKitFormDialog; type=button; name=id; başlangıçta gizli |
| 1779 | Modal | Kapat | id=accessoryKitHistoryDialog; type=button |
| 1780 | Modal | Kapat | id=accessoryKitActionDialog; type=button |
| 1781 | Modal | Kapat | id=accessoryFormDialog; type=button; name=accessoryId; başlangıçta gizli |
| 1782 | Modal | Kapat | id=accessoryCategoryManagerDialog; type=button |
| 1783 | Modal | Kapat | id=accessoryStockDialog; type=button; name=quantity |
| 1784 | Modal | Kapat | id=accessoryCheckoutDialog; type=button; name=targetType; başlangıçta gizli |
| 1785 | Modal | Kapat | id=scanAssetDialog; type=button; name=code; başlangıçta gizli |
| 1786 | Modal | Kapat | id=directoryRenameDialog; type=button; name=kind; başlangıçta gizli |
| 1787 | Modal | Kapat | id=createDepartmentDialog; type=button; name=name |
| 1788 | Modal | Kapat | id=departmentMoveDialog; type=button; name=targetDepartmentId |
| 1789 | Modal | Kapat | id=createLocationDialog; type=button; name=parentLocationId |
| 1790 | Modal | Kapat | id=locationManagerDialog; type=button; name=locationId; başlangıçta gizli |
| 1791 | Modal | Kapat | id=departmentManagerDialog; type=button; name=departmentId; başlangıçta gizli |
| 1792 | Modal | Kapat | id=quickLookupDialog; type=button; name=name; başlangıçta gizli |
| 1793 | Modal | Kapat | id=returnAssetDialog; type=button; name=note; başlangıçta gizli |
| 1794 | Modal | Kapat | id=assetTrashConfirmDialog; type=button; name=note |
| 1795 | Modal | Kapat | id=assetStatusDialog; type=button; name=note; başlangıçta gizli |
| 1796 | Modal | Kapat | id=assetAttachmentRemoveDialog; type=button; başlangıçta gizli |
| 1797 | Modal | Kapat | id=savedAssetSearchDialog; type=button; name=name; başlangıçta gizli |
| 1798 | Modal | Kapat | id=companyMfaEnrollmentDialog; type=button; name=code; başlangıçta gizli |
| 1799 | Modal | Kapat | id=supplierCatalogDialog; type=button; name=id; başlangıçta gizli |
| 1800 | Modal | Kapat | id=companySwitchDialog; type=button |
| 1801 | Modal | Kapat | id=accountPhotoDialog; type=button; hedef=/api/session/data-export; başlangıçta gizli |
| 1802 | Modal | Kapat | id=accountTwoFactorDialog; type=button; başlangıçta gizli |
| 1803 | Modal | Kapat | id=inventoryRequestDialog; type=button; name=itemType; başlangıçta gizli |
| 1804 | Modal | Kapat | id=assetTrashDialog; type=button |
| 1805 | Modal | Görsel önizleme | id=imagePreviewDialog; type=button |
| 1806 | Modal | Kapat | id=modelDetailsDialog; type=button |
| 1807 | Modal | Kapat | id=modelAttachmentsDialog; type=button; name=file; başlangıçta gizli |
| 1808 | Modal | Varlık etiketi yazdırma önizlemesi | id=assetLabelPreviewDialog; type=button |
| 1809 | Modal | Kapat | id=purchaseOrderReturnDialog; type=button; name=returnedOn |
| 1810 | Modal | Kapat | id=editAssetCustomFieldDialog; type=button; name=name |
| 1811 | Tablo | Bu sayfadaki seçilebilir varlıkları seç | id=selectAllAssetsHeader; type=checkbox |
| 1812 | Tablo | Şirket çalışan kayıtları | id=employeeRows |
| 1813 | Tablo | directoryHead | id=directoryHead |
| 1814 | Tablo | Şirket birimleri | id=companyUnitRows |
| 1815 | Tablo | Şirket aksesuar stokları | id=accessoryRows |
| 1816 | Tablo | Aksesuar satın alma siparişleri | id=accessoryOrderRows |
| 1817 | Tablo | Şirketin aksesuar hazır kitleri | id=accessoryKitRows |
| 1818 | Tablo | Sarf malzemeleri ve kullanılabilir stok miktarları | id=consumableRows |
| 1819 | Tablo | Kalem / Tür / Konum / Kalan / | id=requestCatalogRows |
| 1820 | Tablo | Çalışan / talep eden / Kalem / Adet / Durum / İşlem | id=requestRows |
| 1821 | Tablo | Lisans / Yayıncı / sürüm / Tür / Kullanılan koltuk / Bitiş tarihi / Yenileme / İşlem | id=licenseRows |
| 1822 | Tablo | Lisans / Çalışan / varlık / Personel no / Atanma tarihi / Bitiş tarihi / Durum / İşlem | id=licenseAssignmentRows |
| 1823 | Tablo | Seç / Bileşen / Kategori / model / Seri / ürün no / Toplam / Atanmış / Kullanılabilir / Minimum / Garanti bitişi / İşlem | id=componentRows |
| 1824 | Tablo | Şirket stok hareketleri | id=inventoryMovementRows |
| 1825 | Tablo | Kullanıcı / E-posta / Çalışan profili / Rol / Üyelik tarihi / İşlem | id=membersRows |
| 1826 | Tablo | Ad / İlişki / Kullanım / İşlemler | id=catalogRelationHead |
| 1827 | Tablo | Şirketin aktif varlık zimmetleri | id=assignmentRows |
| 1828 | Tablo | Beklenen ve geciken varlık iadeleri | id=returnsRows |
| 1829 | Tablo | Tarih / Varlık / İşlem / Çalışan / İşlemi yapan / Açıklama | id=activityTableRows |
| 1830 | Tablo | Varlık / Çalışan / Kullanım dönemi / Durum / Talep eden / Not / karar / İşlemler | id=reservationRows |
| 1831 | Tablo | Plan tarihi / Etiket / Varlık / Konum / Zimmetli çalışan / Durum / | id=auditScheduleDateHeading |
| 1832 | Tablo | Tarih / İşlemi yapan / Kayıt türü / İşlem / Ayrıntı | id=auditRows |
| 1833 | Tablo | Şirket sözleşmeleri | id=contractRows |
| 1834 | Tablo | Şirket görevleri | id=taskRows |
| 1835 | Tablo | Destek talepleri | id=supportRows |
| 1836 | Tablo | Kullanıcı API anahtarları | id=integrationTokenRows |
| 1837 | Tablo | Ad / Tür / Adres / Olaylar / Durum / Son teslim / İşlem | id=webhookRows |
| 1838 | Tablo | Şirket bakım kayıtları | id=maintenanceRows |
| 1839 | Tablo | Şirket bakım, denetim, lisans, garanti, iade ve rezervasyon takvimi | id=calendarRows |
| 1840 | Tablo | Durum / Varlık / İşlem | id=reportStatusRows |
| 1841 | Tablo | Para birimi / Kayıt / Toplam alış bedeli | id=reportCostRows |
| 1842 | Tablo | Kategori / Toplam / Atanmış / Kullanılabilir / Bakımda / İşlem | id=reportCategoryRows |
| 1843 | Tablo | Alış tarihi / Etiket / Varlık / Seri no / Kategori / Durum / Bedel / Konum | id=assetPurchaseReportRows |
| 1844 | Tablo | Kategori / Bileşen çeşidi / Toplam adet / Atanmış adet / Kullanılabilir adet / Düşük stok kaydı | id=componentReportRows |
| 1845 | Tablo | Tarih / Sonuç / Etiket / Varlık / Konum / Denetimi yapan / Açıklama / bulgu | id=auditReportRows |
| 1846 | Tablo | Garanti bitişi / Etiket / Varlık / Durum / Konum / Zimmetli çalışan | id=warrantyReportRows |
| 1847 | Tablo | Tür / Kayıt | id=maintenanceReportTypes |
| 1848 | Tablo | Para birimi / Kayıt / Toplam | id=maintenanceReportCosts |
| 1849 | Tablo | Bitiş tarihi / Lisans / Yayıncı / sürüm / Tür / Koltuk kullanımı / Otomatik yenileme / Tedarikçi | id=licenseReportRows |
| 1850 | Tablo | Şirketin bekleyen satın alma siparişi kalemleri | id=pendingDeliveryRows |
| 1851 | Tablo | Varlıkların tahmini amortisman değerleri | id=depreciationReportRows |
| 1852 | Tablo | Sarf ve bileşen satın alma siparişleri | id=stockOrderRows |
| 1853 | Tablo | Tüm şirket satın alma kayıtları | id=purchaseOverviewRows |
| 1854 | Tablo | Varlık satın alma siparişleri | id=assetOrderRows |
| 1855 | Tablo | roleAccessCaption | id=roleAccessCaption |
| 1856 | Tablo | Aksesuar / Kalan / verilen / Teslim / Not | id=assetAccessoryAssignments |
| 1857 | Tablo | Bileşen / Kalan / verilen / Takılma / Not | id=assetComponentAllocations |
| 1858 | Tablo | Lisans / Tür / Bitiş / Atanma / Not | id=assetLicenseAssignments |
| 1859 | Tablo | İlişki / Etiket / Varlık / Durum | id=assetLinkedRows |
| 1860 | Tablo | Çalışan / Departman / Zimmet | id=employeeDirectReports |
| 1861 | Tablo | Varlık / Durum / Konum | id=employeeAssetRows |
| 1862 | Tablo | Aksesuar / Verilen / İade / Kalan / Tarih | id=employeeAccessoryRows |
| 1863 | Tablo | Lisans / Yayıncı / sürüm / Atandığı tarih / Bitiş | id=employeeLicenseRows |
| 1864 | Tablo | Malzeme / Miktar / Tarih / Not | id=employeeConsumableRows |
| 1865 | Tablo | Varlık / Bakım türü / Servis / Durum / Tarih | id=employeeMaintenanceRows |
| 1866 | Tablo | Sipariş no / Tedarikçi / tarih / Adet / Durum | id=accessoryOrderHistoryRows |
| 1867 | Tablo | Çalışan / Verilen / İade / Kalan / İşlem | id=accessoryAssignments |
| 1868 | Tablo | Aksesuar / Sipariş / Teslim alınan / Kalan / Fatura birim bedeli / Edinim birim bedeli / Toplam | id=accessoryOrderDetailLines |
| 1869 | Tablo | Kalem / Sipariş / Teslim alınan / Kalan / Fatura birim bedeli / Edinim birim bedeli / Toplam | id=stockOrderDetailLines |
| 1870 | Tablo | Cihaz / Kategori / Hedef konum / Sipariş / Teslim alındı / Kalan / Fatura birim bedeli / Edinim birim bedeli | id=assetOrderDetailLines |
| 1871 | Tablo | Etiket / cihaz / Durum / Seri numarası / Alış tarihi | id=assetOrderReceivedAssets |
| 1872 | Tablo | Tarih / İşlem / Miktar / Çalışan / İşlemi yapan / Not | id=consumableMovements |
| 1873 | Tablo | Tedarikçi / Sipariş no. / Tarih / Koltuk / Birim maliyeti / Durum / İşlemler | id=licensePurchaseOrderRows |
| 1874 | Tablo | Çalışan / varlık / Personel no / Atanma / EULA sürümü / EULA teyit zamanı / Teyit eden / Durum / İşlem | id=licenseAssignments |
| 1875 | Tablo | Varlık / Verilen / Çıkarılan / Kalan / Tarih / İşlem | id=componentAllocations |
| 1876 | Tablo | Tarih / Kalem / Teslim edilen / İade edilen / Kalan / Hedef / İşlemi yapan / Not | id=accessoryKitHistoryRows |
| 1877 | Tablo | Kategori / Aksesuar / Durum / İşlemler | id=accessoryCategoryRows |
| 1878 | Tablo | Firma / Yetkili / iletişim / Bağlı kayıt / Durum / İşlemler | id=supplierCatalogRows |
| 1879 | Tablo | Etiket / Varlık / Seri numarası / Taşınma zamanı / İşlem | id=assetTrashRows |
| 1880 | Tablo | Etiket / Varlık / Durum / Seri no / Çalışan / konum | id=modelDetailsAssets |
| 1881 | Tablo | Sipariş / Tedarikçi / Durum / Tarih / Adet / Teslim alınan | id=modelDetailsOrders |
| 1882 | Tablo | Tarih / İşlem / Açıklama | id=modelDetailsHistory |
| 1883 | Tablo | Dosya / Tür / Boyut / Yüklenme / İşlem | id=modelAttachmentRows |
| 1884 | Tablo | (etiketsiz) | başlangıçta gizli |
| 1885 | Tablo | Etiket / Varlık / Seri no / Durum / Konum |  |
| 1886 | Tablo | Aksesuar / Model / Kalan adet / Teslim tarihi |  |
| 1887 | Tablo | Lisans / EULA sürümü / Dosya / ek kimliği / Kabul eden / tarih / EULA SHA-256 / Durum |  |
| 1888 | Tablo | ${escapeHtml(translateUiText('Uygulama'))} / ${escapeHtml(translateUiText('Yayıncı'))} / ${escapeHtml(translateUiText('Sürüm'))} / ${escapeHtml(translateUiText('Mimari'))} / ${escapeHtml(translateUiText('Kurulum tarihi'))} / ${escapeHtml(translateUiText('Son görülme'))} | id=assetSoftwareInventoryRows |
| 1889 | Tablo | Alan / Anahtar / Grup / Kapsam / Tür / Zorunlu / Durum / İşlem | id=assetCustomFieldRows |
| 1890 | Sekme | assetTabOverviewButton | id=assetTabOverviewButton; type=button |
| 1891 | Sekme | assetTabRelationsButton | id=assetTabRelationsButton; type=button |
| 1892 | Sekme | assetTabMaintenanceButton | id=assetTabMaintenanceButton; type=button |
| 1893 | Sekme | assetTabHistoryButton | id=assetTabHistoryButton; type=button |
| 1894 | Sekme | assetTabAttachmentsButton | id=assetTabAttachmentsButton; type=button |
| 1895 | Sayfa rotası | Genel Bakış | hedef=Genel Bakış |
| 1896 | Sayfa rotası | Varlıklar | hedef=Varlıklar |
| 1897 | Sayfa rotası | Tüm satın almalar | hedef=Tüm satın almalar |
| 1898 | Sayfa rotası | Varlık siparişleri | hedef=Varlık siparişleri |
| 1899 | Sayfa rotası | Varlık modelleri | hedef=Varlık modelleri |
| 1900 | Sayfa rotası | Markalar | hedef=Markalar |
| 1901 | Sayfa rotası | Kategoriler | hedef=Kategoriler |
| 1902 | Sayfa rotası | Durum etiketleri | hedef=Durum etiketleri |
| 1903 | Sayfa rotası | QR ve barkodlar | hedef=QR ve barkodlar |
| 1904 | Sayfa rotası | Zimmetler | hedef=Zimmetler |
| 1905 | Sayfa rotası | İadeler | hedef=İadeler |
| 1906 | Sayfa rotası | Transferler | hedef=Transferler |
| 1907 | Sayfa rotası | Rezervasyonlar | hedef=Rezervasyonlar |
| 1908 | Sayfa rotası | Hareket geçmişi | hedef=Hareket geçmişi |
| 1909 | Sayfa rotası | Yazılım lisansları | hedef=Yazılım lisansları |
| 1910 | Sayfa rotası | Lisans atamaları | hedef=Lisans atamaları |
| 1911 | Sayfa rotası | Lisanslarım | hedef=Lisanslarım |
| 1912 | Sayfa rotası | Sözleşmeler | hedef=Sözleşmeler |
| 1913 | Sayfa rotası | Garanti takibi | hedef=Garanti takibi |
| 1914 | Sayfa rotası | Aksesuarlar | hedef=Aksesuarlar |
| 1915 | Sayfa rotası | Siparişler | hedef=Siparişler |
| 1916 | Sayfa rotası | Sarf malzemeleri | hedef=Sarf malzemeleri |
| 1917 | Sayfa rotası | Stok talepleri | hedef=Stok talepleri |
| 1918 | Sayfa rotası | Bileşenler | hedef=Bileşenler |
| 1919 | Sayfa rotası | Hazır kitler | hedef=Hazır kitler |
| 1920 | Sayfa rotası | Stok hareketleri | hedef=Stok hareketleri |
| 1921 | Sayfa rotası | Çalışan kayıtları | hedef=Çalışan kayıtları |
| 1922 | Sayfa rotası | Uygulama kullanıcıları | id=membersNav; hedef=Uygulama kullanıcıları |
| 1923 | Sayfa rotası | Departmanlar | hedef=Departmanlar |
| 1924 | Sayfa rotası | Yetki rolleri | hedef=Yetki rolleri |
| 1925 | Sayfa rotası | Lokasyonlar | hedef=Lokasyonlar |
| 1926 | Sayfa rotası | Binalar ve odalar | hedef=Binalar ve odalar |
| 1927 | Sayfa rotası | Şirket birimleri | hedef=Şirket birimleri |
| 1928 | Sayfa rotası | Bakım kayıtları | hedef=Bakım kayıtları |
| 1929 | Sayfa rotası | Bakım planları | hedef=Bakım planları |
| 1930 | Sayfa rotası | Denetimler | hedef=Denetimler |
| 1931 | Sayfa rotası | Takvim | hedef=Takvim |
| 1932 | Sayfa rotası | Görevler | hedef=Görevler |
| 1933 | Sayfa rotası | Hazır raporlar | hedef=Hazır raporlar |
| 1934 | Sayfa rotası | Özel rapor oluştur | hedef=Özel rapor oluştur |
| 1935 | Sayfa rotası | Dışa aktarımlar | hedef=Dışa aktarımlar |
| 1936 | Sayfa rotası | Tablodan içe aktar | hedef=Tablodan içe aktar |
| 1937 | Sayfa rotası | Verileri dışa aktar | hedef=Verileri dışa aktar |
| 1938 | Sayfa rotası | Yedekleme geçmişi | hedef=Yedekleme geçmişi |
| 1939 | Sayfa rotası | Geri yükle | hedef=Geri yükle |
| 1940 | Sayfa rotası | Firma profili | hedef=Firma profili |
| 1941 | Sayfa rotası | Logo ve tema | hedef=Logo ve tema |
| 1942 | Sayfa rotası | Bildirimler | hedef=Bildirimler |
| 1943 | Sayfa rotası | Entegrasyonlar | hedef=Entegrasyonlar |
| 1944 | Sayfa rotası | Güvenlik | hedef=Güvenlik |
| 1945 | Sayfa rotası | Denetim kayıtları | hedef=Denetim kayıtları |
| 1946 | Sayfa rotası | Destek | hedef=Destek |
| 1947 | Sayfa rotası | Varlıklar | hedef=Varlıklar |
| 1948 | Sayfa rotası | Hareket geçmişi | hedef=Hareket geçmişi |
| 1949 | Sayfa rotası | Takvim | hedef=Takvim |
| 1950 | Sayfa rotası | Hazır raporlar | hedef=Hazır raporlar |
| 1951 | Sayfa rotası | Yazılım lisansları | type=button; hedef=Yazılım lisansları |
| 1952 | Sayfa rotası | Varlıklar | type=button; hedef=Varlıklar |
| 1953 | Sayfa rotası | Varlıklar | type=button; hedef=Varlıklar |
| 1954 | Sayfa rotası | Çalışan kayıtları | type=button; hedef=Çalışan kayıtları |
| 1955 | Sayfa rotası | Aksesuarlar | type=button; hedef=Aksesuarlar |
| 1956 | Sayfa rotası | Sarf malzemeleri | type=button; hedef=Sarf malzemeleri |
| 1957 | Sayfa rotası | Bileşenler | type=button; hedef=Bileşenler |
| 1958 | Sayfa rotası | Yazılım lisansları | type=button; hedef=Yazılım lisansları |
| 1959 | Sayfa rotası | Genel Bakış | type=button; hedef=Genel Bakış |
| 1960 | Menü grubu | inventory | hedef=inventory |
| 1961 | Menü grubu | movement | hedef=movement |
| 1962 | Menü grubu | software | hedef=software |
| 1963 | Menü grubu | supplies | hedef=supplies |
| 1964 | Menü grubu | people | hedef=people |
| 1965 | Menü grubu | places | hedef=places |
| 1966 | Menü grubu | operations | hedef=operations |
| 1967 | Menü grubu | reports | hedef=reports |
| 1968 | Menü grubu | data | hedef=data |
| 1969 | Menü grubu | settings | hedef=settings |

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
| 190 | Seçim alanı | roleKey | name=roleKey; seçenekler=viewer: Salt okunur, finance: Muhasebe (salt okunur), asset_operator: Varlık sorumlusu, asset_manager: Varlık yöneticisi, auditor: Denetçi, hr_operator: Ekip yöneticisi, admin: Şirket yöneticisi |
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
