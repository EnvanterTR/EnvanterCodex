# EnvanterTR statik arayüz envanteri

Oluşturulma: 2026-11-07T10:08:20.334Z

> Bu dosya mevcut HTML kaynaklarında bulunan statik arayüz öğelerinin dökümüdür. Bir öğenin listelenmesi onun çalıştığını kanıtlamaz. JavaScript ile sonradan oluşturulan kontroller, API/DB davranışı, rol varyasyonları ve erişilebilirlik ayrıca uçtan uca kabul gerektirir. Kaynak değiştikçe `node tools/Generate-UiInventory.mjs` ile yeniden üretin; `node tools/Generate-UiInventory.mjs --check` güncel olup olmadığını doğrular.

## Sayım

| Tür | Statik öğe |
|---|---:|
| Sayfa rotası | 65 |
| Menü grubu | 10 |
| Bağlantı | 54 |
| Düğme | 960 |
| Form | 113 |
| Girdi alanı | 631 |
| Seçim alanı | 204 |
| Metin alanı | 42 |
| Modal | 109 |
| Sekme | 5 |
| Tablo | 79 |
| **Toplam kayıt** | **2272** |

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
| 285 | Düğme | Kapat | id=closeIntegrationSecret; type=button |
| 286 | Düğme | Göster | id=toggleIntegrationSecret; type=button |
| 287 | Düğme | Anahtarı kopyala | id=copyIntegrationSecret; type=button |
| 288 | Düğme | Kapat | id=dismissIntegrationSecret; type=button |
| 289 | Düğme | ⇩ CSV indir | id=maintenanceExport; type=button |
| 290 | Düğme | ↻ Yenile | id=maintenanceRefresh; type=button |
| 291 | Düğme | Filtrele | id=applyMaintenanceFilters; type=button |
| 292 | Düğme | Temizle | id=clearMaintenanceFilters; type=button |
| 293 | Düğme | ← Önceki | id=maintenancePrev; type=button |
| 294 | Düğme | Sonraki → | id=maintenanceNext; type=button |
| 295 | Düğme | ⇩ CSV indir | id=calendarExport; type=button |
| 296 | Düğme | ↻ Yenile | id=calendarRefresh; type=button |
| 297 | Düğme | Takvimi göster | id=applyCalendarFilters; type=button |
| 298 | Düğme | Önceki dönem | id=calendarPeriodPrev; type=button |
| 299 | Düğme | Bugün | id=calendarPeriodToday; type=button |
| 300 | Düğme | Sonraki dönem | id=calendarPeriodNext; type=button |
| 301 | Düğme | CSV indir | id=reportExport; type=button |
| 302 | Düğme | Excel (.xlsx) | id=reportXlsxExport; type=button |
| 303 | Düğme | Yazdır / PDF | id=reportPrint; type=button |
| 304 | Düğme | Rapor takvimi | id=reportScheduleOpen; type=button |
| 305 | Düğme | ↻ Verileri yenile | id=reportRefresh; type=button |
| 306 | Düğme | Tarih aralığını uygula | id=applyReportDateFilter; type=button |
| 307 | Düğme | Temizle | id=clearReportDateFilter; type=button |
| 308 | Düğme | Alış kayıtlarını CSV indir | id=assetPurchaseReportExport; type=button |
| 309 | Düğme | Yazdır / PDF | id=assetPurchaseReportPrint; type=button |
| 310 | Düğme | ← Önceki | id=assetPurchaseReportPrev; type=button |
| 311 | Düğme | Sonraki → | id=assetPurchaseReportNext; type=button |
| 312 | Düğme | Bileşen CSV indir | id=componentReportExport; type=button |
| 313 | Düğme | Yazdır / PDF | id=componentReportPrint; type=button |
| 314 | Düğme | Denetimleri CSV indir | id=auditReportExport; type=button |
| 315 | Düğme | Yazdır / PDF | id=auditReportPrint; type=button |
| 316 | Düğme | Filtrele | id=auditReportApply; type=button |
| 317 | Düğme | ← Önceki | id=auditReportPrev; type=button |
| 318 | Düğme | Sonraki → | id=auditReportNext; type=button |
| 319 | Düğme | CSV indir | id=warrantyReportExport; type=button |
| 320 | Düğme | Yazdır / PDF | id=warrantyReportPrint; type=button |
| 321 | Düğme | ↻ Yenile | id=warrantyReportRefresh; type=button |
| 322 | Düğme | ← Önceki | id=warrantyReportPrev |
| 323 | Düğme | Sonraki → | id=warrantyReportNext |
| 324 | Düğme | ↻ Yenile | id=maintenanceReportRefresh; type=button |
| 325 | Düğme | Yazdır / PDF | id=maintenanceReportPrint; type=button |
| 326 | Düğme | Filtrele | id=applyMaintenanceReportFilter; type=button |
| 327 | Düğme | Temizle | id=clearMaintenanceReportFilter; type=button |
| 328 | Düğme | CSV indir | id=licenseReportExport; type=button |
| 329 | Düğme | Yazdır / PDF | id=licenseReportPrint; type=button |
| 330 | Düğme | ↻ Yenile | id=licenseReportRefresh; type=button |
| 331 | Düğme | ← Önceki | id=licenseReportPrev |
| 332 | Düğme | Sonraki → | id=licenseReportNext |
| 333 | Düğme | ↻ Yenile | id=pendingDeliveryRefresh; type=button |
| 334 | Düğme | CSV indir | id=pendingDeliveryExport; type=button |
| 335 | Düğme | Yazdır / PDF | id=pendingDeliveryPrint; type=button |
| 336 | Düğme | ← Önceki | id=pendingDeliveryPrev; type=button |
| 337 | Düğme | Sonraki → | id=pendingDeliveryNext; type=button |
| 338 | Düğme | ↻ Hesapla | id=depreciationReportRefresh; type=button |
| 339 | Düğme | CSV indir | id=depreciationReportExport; type=button |
| 340 | Düğme | Yazdır / PDF | id=depreciationReportPrint; type=button |
| 341 | Düğme | ← Önceki | id=depreciationReportPrev; type=button |
| 342 | Düğme | Sonraki → | id=depreciationReportNext; type=button |
| 343 | Düğme | Taslağı yükle | id=customReportLoadPreset; type=button |
| 344 | Düğme | Taslağı kaydet | id=customReportSavePreset; type=button |
| 345 | Düğme | Taslağı sil | id=customReportDeletePreset; type=button |
| 346 | Düğme | Özel raporu CSV indir | id=customReportExport; type=button |
| 347 | Düğme | Özel raporu Excel indir | id=customReportXlsxExport; type=button |
| 348 | Düğme | Yazdır / PDF | id=customReportPrint; type=button |
| 349 | Düğme | Aksesuar siparişleri | id=backAccessoryOrders; type=button |
| 350 | Düğme | CSV dışa aktar | id=exportStockOrders; type=button |
| 351 | Düğme | ↻ Yenile | id=stockOrdersRefresh; type=button |
| 352 | Düğme | ＋ Sipariş oluştur | id=newStockOrderButton; type=button |
| 353 | Düğme | Filtreleri temizle | id=clearStockOrderFilters; type=button |
| 354 | Düğme | ← Önceki | id=stockOrderPrev; type=button |
| 355 | Düğme | Sonraki → | id=stockOrderNext; type=button |
| 356 | Düğme | ↻ Yenile | id=purchaseOverviewRefresh; type=button |
| 357 | Düğme | CSV indir | id=purchaseOverviewExport; type=button |
| 358 | Düğme | Filtreleri temizle | id=purchaseOverviewClear; type=button |
| 359 | Düğme | ← Önceki | id=purchaseOverviewPrev; type=button |
| 360 | Düğme | Sonraki → | id=purchaseOverviewNext; type=button |
| 361 | Düğme | Yıllık bütçe | id=assetOrderBudgetButton; type=button |
| 362 | Düğme | ↻ Yenile | id=assetOrdersRefresh; type=button |
| 363 | Düğme | CSV indir | id=exportAssetOrdersButton; type=button |
| 364 | Düğme | Tedarikçiler | id=assetOrdersSupplierCatalog; type=button |
| 365 | Düğme | ＋ Sipariş oluştur | id=newAssetOrderButton; type=button |
| 366 | Düğme | Filtreleri temizle | id=clearAssetOrderFilters; type=button |
| 367 | Düğme | ← Önceki | id=assetOrderPrev; type=button |
| 368 | Düğme | Sonraki → | id=assetOrderNext; type=button |
| 369 | Düğme | Uygulama kullanıcıları | id=rolesManageMembersButton; type=button |
| 370 | Düğme | Özel rol oluştur | id=createCompanyRoleButton; type=button; başlangıçta gizli |
| 371 | Düğme | Kapat | id=closeCompanyRoleDialog; type=button |
| 372 | Düğme | Vazgeç | id=cancelCompanyRoleDialog; type=button |
| 373 | Düğme | Kaydet | id=saveCompanyRoleButton; type=submit |
| 374 | Düğme | Kapat | id=closeDeactivateCompanyRole; type=button |
| 375 | Düğme | Vazgeç | id=cancelDeactivateCompanyRole; type=button |
| 376 | Düğme | Devre dışı bırak | id=confirmDeactivateCompanyRole; type=button |
| 377 | Düğme | Kapat | id=licenseRevokeClose; type=button |
| 378 | Düğme | Vazgeç | id=licenseRevokeCancel; type=button |
| 379 | Düğme | Atamayı kaldır | id=licenseRevokeSubmit; type=submit |
| 380 | Düğme | Kapat | id=licenseImportProfileDeleteClose; type=button |
| 381 | Düğme | Vazgeç | id=licenseImportProfileDeleteCancel; type=button |
| 382 | Düğme | Profili sil | id=licenseImportProfileDeleteSubmit; type=submit |
| 383 | Düğme | Kapat | id=commandPaletteClose; type=button |
| 384 | Düğme | Kapat | id=keyboardShortcutsClose; type=button |
| 385 | Düğme | Kapat | type=button |
| 386 | Düğme | Vazgeç | type=button |
| 387 | Düğme | Kaydet | type=submit |
| 388 | Düğme | Kapat | id=closeContractDetail; type=button |
| 389 | Düğme | Kapat | id=closeContractDetailFooter; type=button |
| 390 | Düğme | Kapat | id=contractActionClose; type=button |
| 391 | Düğme | Vazgeç | id=contractActionCancel; type=button |
| 392 | Düğme | Onayla | id=contractActionSubmit; type=submit |
| 393 | Düğme | Kapat | id=appearanceClose; type=button |
| 394 | Düğme | Sistem ayarını izle | type=button |
| 395 | Düğme | Açık | type=button |
| 396 | Düğme | Koyu | type=button |
| 397 | Düğme | Vazgeç | id=appearanceCancel; type=button |
| 398 | Düğme | Tercihleri kaydet | id=appearanceSave; type=button |
| 399 | Düğme | Kapat | id=closeDialog |
| 400 | Düğme | Genel bakış | id=assetTabOverviewButton; type=button |
| 401 | Düğme | İlişkiler | id=assetTabRelationsButton; type=button |
| 402 | Düğme | Bakım kayıtları | id=assetTabMaintenanceButton; type=button |
| 403 | Düğme | Hareket geçmişi | id=assetTabHistoryButton; type=button |
| 404 | Düğme | Fotoğraf ve dosyalar | id=assetTabAttachmentsButton; type=button |
| 405 | Düğme | detailPurchaseOrder | id=detailPurchaseOrder; type=button |
| 406 | Düğme | Etiketi kopyala | id=copyAssetTag; type=button |
| 407 | Düğme | Etiket kartını yazdır | id=printAssetLabel; type=button |
| 408 | Düğme | Bağlantıyı kaydet | id=saveAssetParent; type=button |
| 409 | Düğme | Not ekle | type=submit |
| 410 | Düğme | Dosya ekle | id=uploadAssetAttachment; type=submit |
| 411 | Düğme | Bilgileri düzenle | id=editAssetButton; type=button |
| 412 | Düğme | Denetimi kaydet | id=auditAssetButton; type=button |
| 413 | Düğme | Denetim planla | id=scheduleAssetAuditButton; type=button |
| 414 | Düğme | Bakım başlat | id=addMaintenanceButton; type=button |
| 415 | Düğme | Konum transferi | id=transferAssetButton; type=button |
| 416 | Düğme | Çalışana zimmetle | id=assignAssetButton; type=button |
| 417 | Düğme | İade al | id=returnAssetButton; type=button |
| 418 | Düğme | Arşivden geri al | id=restoreAssetButton; type=button; başlangıçta gizli |
| 419 | Düğme | Çöp kutusuna taşı | id=trashAssetButton; type=button |
| 420 | Düğme | Kapat | type=button |
| 421 | Düğme | Vazgeç | type=button |
| 422 | Düğme | Denetimi kaydet | type=submit |
| 423 | Düğme | Kapat | type=button |
| 424 | Düğme | Vazgeç | type=button |
| 425 | Düğme | Planı kaydet | type=submit |
| 426 | Düğme | Kapat | type=button |
| 427 | Düğme | Vazgeç | type=button |
| 428 | Düğme | Bakımı başlat | id=maintenanceSubmit; type=submit |
| 429 | Düğme | Kapat | type=button |
| 430 | Düğme | Vazgeç | type=button |
| 431 | Düğme | Bakımı tamamla | type=submit |
| 432 | Düğme | Kapat | type=button |
| 433 | Düğme | Vazgeç | type=button |
| 434 | Düğme | Transferi kaydet | type=submit |
| 435 | Düğme | Kapat | type=button |
| 436 | Düğme | Vazgeç | type=button |
| 437 | Düğme | Değişiklikleri kaydet | type=submit |
| 438 | Düğme | Kapat | type=button |
| 439 | Düğme | Vazgeç | type=button |
| 440 | Düğme | Zimmeti kaydet | type=submit |
| 441 | Düğme | Kapat | type=button |
| 442 | Düğme | Vazgeç | type=button |
| 443 | Düğme | Çalışanı kaydet | type=submit |
| 444 | Düğme | Kapat | id=closeEmployeeImport; type=button |
| 445 | Düğme | Şablonu indir | id=downloadEmployeeImportTemplate; type=button |
| 446 | Düğme | Dosyayı doğrula | id=previewEmployeeImport; type=button |
| 447 | Düğme | Doğrulanmış kayıtları ekle | id=commitEmployeeImport; type=button |
| 448 | Düğme | Kapat | id=closeConsumableImport; type=button |
| 449 | Düğme | Şablonu indir | id=downloadConsumableImportTemplate; type=button |
| 450 | Düğme | Eşlemeyi kaydet | id=saveConsumableImportProfile; type=button |
| 451 | Düğme | Profili sil | id=deleteConsumableImportProfile; type=button |
| 452 | Düğme | Eşlemeyi kullan ve yeniden doğrula | id=applyConsumableImportMapping; type=button |
| 453 | Düğme | Dosyayı doğrula | id=previewConsumableImport; type=button |
| 454 | Düğme | Doğrulanmış kayıtları ekle | id=commitConsumableImport; type=button |
| 455 | Düğme | Kapat | id=closeAccessoryImport; type=button |
| 456 | Düğme | Şablonu indir | id=downloadAccessoryImportTemplate; type=button |
| 457 | Düğme | Eşlemeyi kaydet | id=saveAccessoryImportProfile; type=button |
| 458 | Düğme | Profili sil | id=deleteAccessoryImportProfile; type=button |
| 459 | Düğme | Eşlemeyi kullan ve yeniden doğrula | id=applyAccessoryImportMapping; type=button |
| 460 | Düğme | Dosyayı doğrula | id=previewAccessoryImport; type=button |
| 461 | Düğme | Doğrulanmış kayıtları ekle | id=commitAccessoryImport; type=button |
| 462 | Düğme | Kapat | type=button |
| 463 | Düğme | Vazgeç | type=button |
| 464 | Düğme | Değişiklikleri kaydet | type=submit |
| 465 | Düğme | Kapat | type=button |
| 466 | Düğme | Vazgeç | type=button |
| 467 | Düğme | Onayla | id=employeeActionSubmit; type=submit |
| 468 | Düğme | Kapat | type=button |
| 469 | Düğme | Vazgeç | type=button |
| 470 | Düğme | Kaydet | id=textEntrySubmit; type=submit |
| 471 | Düğme | İmzalı zimmet belgeleri | id=printEmployeeHandoff; type=button |
| 472 | Düğme | Kapat | id=closeEmployeeDetails; type=button |
| 473 | Düğme | Kaydet | id=saveEmployeeManager; type=button |
| 474 | Düğme | Kaldır | id=clearEmployeeManager; type=button |
| 475 | Düğme | Kapat | id=closeInviteMember; type=button |
| 476 | Düğme | Vazgeç | id=cancelInviteMember; type=button |
| 477 | Düğme | Daveti gönder | type=submit |
| 478 | Düğme | Kapat | id=closeMemberLocationAccess; type=button |
| 479 | Düğme | Vazgeç | id=cancelMemberLocationAccess; type=button |
| 480 | Düğme | Erişimi kaydet | type=submit |
| 481 | Düğme | Kapat | type=button |
| 482 | Düğme | ＋ Ekle | type=button |
| 483 | Düğme | ＋ Ekle | type=button |
| 484 | Düğme | ＋ Ekle | type=button |
| 485 | Düğme | Vazgeç | type=button |
| 486 | Düğme | Kaydı oluştur | type=submit |
| 487 | Düğme | Sunucu bağlantısını test et | id=retryApiButton; type=button |
| 488 | Düğme | Giriş yap | type=submit |
| 489 | Düğme | Parolamı unuttum | id=forgotPasswordButton; type=button |
| 490 | Düğme | Kapat | id=closeDashboardCustomize; type=button |
| 491 | Düğme | Özet kartlarını yukarı taşı | type=button |
| 492 | Düğme | Özet kartlarını aşağı taşı | type=button |
| 493 | Düğme | Varlık listesini yukarı taşı | type=button |
| 494 | Düğme | Varlık listesini aşağı taşı | type=button |
| 495 | Düğme | Son hareketleri yukarı taşı | type=button |
| 496 | Düğme | Son hareketleri aşağı taşı | type=button |
| 497 | Düğme | Yaklaşan işlemleri yukarı taşı | type=button |
| 498 | Düğme | Yaklaşan işlemleri aşağı taşı | type=button |
| 499 | Düğme | Varlık durumu grafiğini yukarı taşı | type=button |
| 500 | Düğme | Varlık durumu grafiğini aşağı taşı | type=button |
| 501 | Düğme | + Grafik ekle | id=addDashboardChart; type=button |
| 502 | Düğme | Tümünü göster | id=resetDashboardCustomize; type=button |
| 503 | Düğme | İptal | id=cancelDashboardCustomize; type=button |
| 504 | Düğme | Tercihleri kaydet | type=submit |
| 505 | Düğme | Kapat | id=closeForgotPassword; type=button |
| 506 | Düğme | İptal | type=button |
| 507 | Düğme | Bağlantı iste | type=submit |
| 508 | Düğme | Kapat | id=closeImportPreview; type=button |
| 509 | Düğme | CSV şablonunu indir | id=downloadAssetCsvTemplate; type=button |
| 510 | Düğme | Eşlemeyi kaydet | id=saveAssetImportProfile; type=button |
| 511 | Düğme | Profili sil | id=deleteAssetImportProfile; type=button |
| 512 | Düğme | Eşlemeyi kullan ve yeniden doğrula | id=applyAssetImportMapping; type=button |
| 513 | Düğme | Kapat | id=cancelImportPreview; type=button |
| 514 | Düğme | Dosyayı doğrula | id=runImportPreview; type=submit |
| 515 | Düğme | Doğrula ve içe aktar | id=applyCsvImport; type=button; başlangıçta gizli |
| 516 | Düğme | Şirkete geri yükle | id=applyCompanyImport; type=button; başlangıçta gizli |
| 517 | Düğme | Kapat | id=closeAccessoryDetail; type=button |
| 518 | Düğme | Tüm siparişleri aç | id=accessoryOrdersFromDetail; type=button |
| 519 | Düğme | Kapat | id=accessoryCheckinClose; type=button |
| 520 | Düğme | Vazgeç | id=accessoryCheckinCancel; type=button |
| 521 | Düğme | İadeyi kaydet | id=accessoryCheckinSubmit; type=submit |
| 522 | Düğme | Kapat | type=button |
| 523 | Düğme | Bütçe istisnasını reddet | id=rejectAccessoryOrderApproval; type=button |
| 524 | Düğme | Bütçe istisnasını onayla | id=approveAccessoryOrderApproval; type=button |
| 525 | Düğme | Dosya yükle | type=submit |
| 526 | Düğme | Tedarikçiye iade et | id=returnAccessoryOrderFromDetail; type=button |
| 527 | Düğme | Teslim al | id=receiveAccessoryOrderFromDetail; type=button |
| 528 | Düğme | Kapat | type=button |
| 529 | Düğme | Kapat | type=button |
| 530 | Düğme | ＋ Kalem ekle | id=addAccessoryOrderLine; type=button |
| 531 | Düğme | Vazgeç | type=button |
| 532 | Düğme | Siparişi kaydet | type=submit |
| 533 | Düğme | Kapat | type=button |
| 534 | Düğme | ＋ Kalem ekle | id=addStockOrderLine; type=button |
| 535 | Düğme | Vazgeç | type=button |
| 536 | Düğme | Siparişi kaydet | type=submit |
| 537 | Düğme | Kapat | type=button |
| 538 | Düğme | Bütçe istisnasını reddet | id=rejectStockOrderApproval; type=button |
| 539 | Düğme | Bütçe istisnasını onayla | id=approveStockOrderApproval; type=button |
| 540 | Düğme | Belge yükle | type=submit |
| 541 | Düğme | Tedarikçiye iade et | id=returnStockOrderFromDetail; type=button |
| 542 | Düğme | Bu siparişten kit reçetesi oluştur | id=createKitFromStockOrder; type=button; başlangıçta gizli |
| 543 | Düğme | Teslim al | id=receiveStockOrderFromDetail; type=button |
| 544 | Düğme | Kapat | type=button |
| 545 | Düğme | Kapat | type=button |
| 546 | Düğme | Vazgeç | type=button |
| 547 | Düğme | Teslim al ve stoğa ekle | type=submit |
| 548 | Düğme | Kapat | type=button |
| 549 | Düğme | Vazgeç | type=button |
| 550 | Düğme | Kit reçetesini kaydet | type=submit |
| 551 | Düğme | Kapat | type=button |
| 552 | Düğme | Kapat | type=button |
| 553 | Düğme | Bütçeyi kaydet | type=submit |
| 554 | Düğme | Kapat | type=button |
| 555 | Düğme | ＋ Cihaz kalemi ekle | id=addAssetOrderLine; type=button |
| 556 | Düğme | Vazgeç | type=button |
| 557 | Düğme | Siparişi kaydet | type=submit |
| 558 | Düğme | Kapat | type=button |
| 559 | Düğme | Bütçe istisnasını reddet | id=rejectAssetOrderApproval; type=button |
| 560 | Düğme | Bütçe istisnasını onayla | id=approveAssetOrderApproval; type=button |
| 561 | Düğme | Belgeleri yükle | id=uploadAssetOrderAttachments; type=button |
| 562 | Düğme | Tedarikçiye iade et | id=returnAssetOrderFromDetail; type=button |
| 563 | Düğme | Teslim al | id=receiveAssetOrderFromDetail; type=button |
| 564 | Düğme | Kapat | type=button |
| 565 | Düğme | Kapat | type=button |
| 566 | Düğme | Vazgeç | type=button |
| 567 | Düğme | Teslim al ve varlıkları oluştur | type=submit |
| 568 | Düğme | Kapat | type=button |
| 569 | Düğme | Vazgeç | type=button |
| 570 | Düğme | Stoğa al | type=submit |
| 571 | Düğme | Kapat | id=closeConsumableDetail; type=button |
| 572 | Düğme | Kapat | type=button |
| 573 | Düğme | Vazgeç | type=button |
| 574 | Düğme | Kaydet | type=submit |
| 575 | Düğme | Kapat | type=button |
| 576 | Düğme | Vazgeç | type=button |
| 577 | Düğme | Stok hareketini kaydet | type=submit |
| 578 | Düğme | Kapat | type=button |
| 579 | Düğme | Vazgeç | type=button |
| 580 | Düğme | Tüketim çıkışını kaydet | type=submit |
| 581 | Düğme | Kapat | id=closeLicenseDetail; type=button |
| 582 | Düğme | Anahtarı al ve göster | id=licenseKeyReveal; type=button |
| 583 | Düğme | Gizle | id=licenseKeyVisibility; type=button; başlangıçta gizli |
| 584 | Düğme | Anahtarı kaydet | id=licenseKeySave; type=button |
| 585 | Düğme | Anahtarı kaldır | id=licenseKeyClear; type=button |
| 586 | Düğme | Dosya yükle | type=submit |
| 587 | Düğme | Kapat | type=button |
| 588 | Düğme | Vazgeç | type=button |
| 589 | Düğme | Kaydet | type=submit |
| 590 | Düğme | Kapat | type=button |
| 591 | Düğme | Vazgeç | type=button |
| 592 | Düğme | Kaydet | type=submit |
| 593 | Düğme | Kapat | id=closeLicenseImport; type=button |
| 594 | Düğme | Şablonu indir | id=downloadLicenseImportTemplate; type=button |
| 595 | Düğme | Eşlemeyi kaydet | id=saveLicenseImportProfile; type=button |
| 596 | Düğme | Profili sil | id=deleteLicenseImportProfile; type=button |
| 597 | Düğme | Eşlemeyi kullan ve yeniden doğrula | id=applyLicenseImportMapping; type=button |
| 598 | Düğme | Dosyayı doğrula | id=previewLicenseImport; type=button |
| 599 | Düğme | Doğrulanmış kayıtları ekle | id=commitLicenseImport; type=button |
| 600 | Düğme | Kapat | id=licenseActionClose; type=button |
| 601 | Düğme | Vazgeç | id=licenseActionCancel; type=button |
| 602 | Düğme | Onayla | id=licenseActionSubmit; type=submit |
| 603 | Düğme | Kapat | type=button |
| 604 | Düğme | Vazgeç | type=button |
| 605 | Düğme | Atamayı kaydet | type=submit |
| 606 | Düğme | Kapat | type=button |
| 607 | Düğme | Vazgeç | type=button |
| 608 | Düğme | Devam et | id=componentActionSubmit; type=submit |
| 609 | Düğme | Kapat | id=closeComponentDetail; type=button |
| 610 | Düğme | Kapat | id=closeComponentImport; type=button |
| 611 | Düğme | Şablonu indir | id=downloadComponentImportTemplate; type=button |
| 612 | Düğme | Eşlemeyi kaydet | id=saveComponentImportProfile; type=button |
| 613 | Düğme | Profili sil | id=deleteComponentImportProfile; type=button |
| 614 | Düğme | Eşlemeyi kullan ve yeniden doğrula | id=applyComponentImportMapping; type=button |
| 615 | Düğme | Dosyayı doğrula | id=previewComponentImport; type=button |
| 616 | Düğme | Doğrulanmış kayıtları ekle | id=commitComponentImport; type=button |
| 617 | Düğme | Kapat | type=button |
| 618 | Düğme | Vazgeç | type=button |
| 619 | Düğme | Kaydet | type=submit |
| 620 | Düğme | ✕ | type=button |
| 621 | Düğme | Vazgeç | type=button |
| 622 | Düğme | Kaydet | type=submit |
| 623 | Düğme | ✕ | type=button |
| 624 | Düğme | Vazgeç | type=button |
| 625 | Düğme | Atamayı kaydet | type=submit |
| 626 | Düğme | Kapat | type=button |
| 627 | Düğme | Vazgeç | type=button |
| 628 | Düğme | Çıkarmayı kaydet | type=submit |
| 629 | Düğme | Kapat | type=button |
| 630 | Düğme | ＋ Stok kalemi ekle | id=addAccessoryKitLine; type=button |
| 631 | Düğme | Vazgeç | type=button |
| 632 | Düğme | Kiti kaydet | type=submit |
| 633 | Düğme | Kapat | id=closeAccessoryKitHistory; type=button |
| 634 | Düğme | Kapat | id=closeAccessoryKitHistoryFooter; type=button |
| 635 | Düğme | Kapat | type=button |
| 636 | Düğme | Vazgeç | type=button |
| 637 | Düğme | Arşivle | id=accessoryKitActionSubmit; type=submit |
| 638 | Düğme | Kapat | type=button |
| 639 | Düğme | ＋ Kategori oluştur | id=accessoryQuickCategory; type=button |
| 640 | Düğme | ＋ Marka oluştur | id=accessoryQuickBrand; type=button |
| 641 | Düğme | ＋ Model oluştur | id=accessoryQuickModel; type=button |
| 642 | Düğme | Vazgeç | type=button |
| 643 | Düğme | Kaydet | type=submit |
| 644 | Düğme | Kapat | id=closeAccessoryCategoryManager; type=button |
| 645 | Düğme | Kapat | id=cancelAccessoryCategoryManager; type=button |
| 646 | Düğme | ＋ Kategori ekle | id=addAccessoryCategory; type=button |
| 647 | Düğme | Kapat | type=button |
| 648 | Düğme | Vazgeç | type=button |
| 649 | Düğme | Stok hareketini kaydet | type=submit |
| 650 | Düğme | Kapat | type=button |
| 651 | Düğme | Vazgeç | type=button |
| 652 | Düğme | Teslimi kaydet | type=submit |
| 653 | Düğme | Kapat | id=closeAssetScanner; type=button |
| 654 | Düğme | Kamerayı başlat | id=startAssetCamera; type=button |
| 655 | Düğme | Varlığı bul | type=submit |
| 656 | Düğme | Kapat | id=closeDirectoryRename; type=button |
| 657 | Düğme | Vazgeç | id=cancelDirectoryRename; type=button |
| 658 | Düğme | Değişiklikleri kaydet | id=saveDirectoryRename; type=submit |
| 659 | Düğme | Kapat | id=closeCreateDepartment; type=button |
| 660 | Düğme | Vazgeç | id=cancelCreateDepartment; type=button |
| 661 | Düğme | Departmanı kaydet | id=saveDepartment; type=submit |
| 662 | Düğme | Kapat | id=closeDepartmentMove; type=button |
| 663 | Düğme | Kapat | id=cancelDepartmentMove; type=button |
| 664 | Düğme | Sonraki 100 çalışanı taşı | id=moveDepartmentEmployees; type=submit |
| 665 | Düğme | Kapat | id=closeCreateLocation; type=button |
| 666 | Düğme | Vazgeç | id=cancelCreateLocation; type=button |
| 667 | Düğme | Konumu kaydet | id=saveLocation; type=submit |
| 668 | Düğme | Kapat | id=closeLocationManager; type=button |
| 669 | Düğme | Vazgeç | id=cancelLocationManager; type=button |
| 670 | Düğme | Sorumluyu kaydet | id=saveLocationManager; type=submit |
| 671 | Düğme | Kapat | id=closeDepartmentManager; type=button |
| 672 | Düğme | Vazgeç | id=cancelDepartmentManager; type=button |
| 673 | Düğme | Yetkiliyi kaydet | id=saveDepartmentManager; type=submit |
| 674 | Düğme | Kapat | id=cancelQuickLookup; type=button |
| 675 | Düğme | Vazgeç | id=cancelQuickLookupAction; type=button |
| 676 | Düğme | Kaydet | type=submit |
| 677 | Düğme | Kapat | id=cancelAssetReturn; type=button |
| 678 | Düğme | Vazgeç | id=cancelAssetReturnAction; type=button |
| 679 | Düğme | İade al | id=submitAssetReturn; type=submit |
| 680 | Düğme | Kapat | id=closeAssetTrashConfirm; type=button |
| 681 | Düğme | Vazgeç | id=cancelAssetTrashConfirm; type=button |
| 682 | Düğme | Varlığı çöp kutusuna taşı | id=submitAssetTrashConfirm; type=submit |
| 683 | Düğme | Kapat | id=closeAssetStatusDialog; type=button |
| 684 | Düğme | Vazgeç | id=cancelAssetStatus; type=button |
| 685 | Düğme | Durumu güncelle | id=submitAssetStatus; type=submit |
| 686 | Düğme | Kapat | id=closeAssetAttachmentRemove; type=button |
| 687 | Düğme | Vazgeç | id=cancelAssetAttachmentRemove; type=button |
| 688 | Düğme | Dosyayı kaldır | id=submitAssetAttachmentRemove; type=submit |
| 689 | Düğme | Kapat | id=closeSavedAssetSearchDialog; type=button |
| 690 | Düğme | Vazgeç | id=cancelSavedAssetSearch; type=button |
| 691 | Düğme | Aramayı kaydet | id=submitSavedAssetSearch; type=submit |
| 692 | Düğme | Kapat | id=closeCompanyMfaEnrollment; type=button |
| 693 | Düğme | Doğrula ve devam et | id=verifyCompanyMfaEnrollment; type=submit |
| 694 | Düğme | Kodları kopyala | id=copyCompanyMfaRecoveryCodes; type=button |
| 695 | Düğme | Kodları kaydettim ve giriş yap | id=finishCompanyMfaEnrollment; type=button |
| 696 | Düğme | Kapat | id=closeSupplierCatalog; type=button |
| 697 | Düğme | Temizle | id=resetSupplierForm; type=button |
| 698 | Düğme | Tedarikçiyi kaydet | type=submit |
| 699 | Düğme | Kapat | id=closeCompanySwitch; type=button |
| 700 | Düğme | Vazgeç | id=cancelCompanySwitch; type=button |
| 701 | Düğme | Çıkış yap ve şirket seç | id=confirmCompanySwitch; type=button |
| 702 | Düğme | Kapat | id=closeAccountPhoto; type=button |
| 703 | Düğme | Fotoğraf seç | id=chooseAccountPhoto; type=button |
| 704 | Düğme | Fotoğrafı kaldır | id=removeAccountPhoto; type=button; başlangıçta gizli |
| 705 | Düğme | Parolayı değiştir | id=changeOwnPassword; type=button |
| 706 | Düğme | Güvenlik ayarları | id=openAccountTwoFactor; type=button |
| 707 | Düğme | Yenile | id=refreshAccountSessions; type=button |
| 708 | Düğme | Kapat | id=closeAccountTwoFactor; type=button |
| 709 | Düğme | Authenticator kurulumu başlat | id=startTwoFactorSetup; type=button |
| 710 | Düğme | Doğrula ve etkinleştir | id=enableTwoFactor; type=button; başlangıçta gizli |
| 711 | Düğme | Kurtarma kodlarını yenile | id=rotateTwoFactorCodes; type=button; başlangıçta gizli |
| 712 | Düğme | İki aşamalı doğrulamayı kapat | id=disableTwoFactor; type=button; başlangıçta gizli |
| 713 | Düğme | Kodları kopyala | id=copyTwoFactorRecoveryCodes; type=button |
| 714 | Düğme | Kapat | type=button |
| 715 | Düğme | Vazgeç | type=button |
| 716 | Düğme | Talebi gönder | type=submit |
| 717 | Düğme | Kapat | id=closeAssetTrash; type=button |
| 718 | Düğme | ← Önceki | id=assetTrashPrev; type=button |
| 719 | Düğme | Sonraki → | id=assetTrashNext; type=button |
| 720 | Düğme | Kapat | id=closeImagePreview; type=button |
| 721 | Düğme | Kapat | id=closeModelDetails; type=button |
| 722 | Düğme | Kapat | id=closeModelAttachments; type=button |
| 723 | Düğme | Teknik dosya ekle | id=uploadModelAttachment; type=submit |
| 724 | Düğme | Yazdır | id=assetLabelPreviewPrint; type=button |
| 725 | Düğme | Kapat | id=assetLabelPreviewClose; type=button |
| 726 | Düğme | Kapat | type=button |
| 727 | Düğme | Vazgeç | type=button |
| 728 | Düğme | İadeyi kaydet | id=submitPurchaseOrderReturn; type=submit |
| 729 | Düğme | ${escapeHtml(translateUiText('Aç'))} | type=button |
| 730 | Düğme | Kapat | type=button |
| 731 | Düğme | Vazgeç | type=button |
| 732 | Düğme | Seçilenleri güncelle | type=submit |
| 733 | Düğme | ${escapeHtml(text)} ile eşleşen kayıtları filtrele | type=button |
| 734 | Düğme | ${escapeHtml(translateUiText('Yenile'))} | id=refreshAssetSoftwareInventory; type=button |
| 735 | Düğme | ${escapeHtml(c.name)} | type=button |
| 736 | Düğme | Ayrıntı | type=button |
| 737 | Düğme | Düzenle | type=button |
| 738 | Düğme | Stok | type=button |
| 739 | Düğme | Ata | type=button |
| 740 | Düğme | Düzenle | id=editComponentButton; başlangıçta gizli |
| 741 | Düğme | Stok ekle / düzelt | id=componentStockButton; başlangıçta gizli |
| 742 | Düğme | c.allocated?'':'hidden'}>Varlığa tak | id=componentAllocateButton; başlangıçta gizli |
| 743 | Düğme | Çıkar | id=${a.id}; type=button |
| 744 | Düğme | Kapat | type=button |
| 745 | Düğme | Vazgeç | type=button |
| 746 | Düğme | Bileşenleri varlığa tak | type=submit |
| 747 | Düğme | Kapat | type=button |
| 748 | Düğme | Vazgeç | type=button |
| 749 | Düğme | Stok farklarını kaydet | type=submit |
| 750 | Düğme | ${escapeHtml(l.name)} | type=button |
| 751 | Düğme | ${escapeHtml(l.name)} ${escapeHtml(translateUiText('koltuk atamalarını aç'))} | type=button |
| 752 | Düğme | ${translateUiText('Ayrıntı')} | type=button |
| 753 | Düğme | ${translateUiText('Düzenle')} | id=${order.id}; type=button |
| 754 | Düğme | ${translateUiText(order.isArchived?'Geri yükle':'Arşivle')} | id=${order.id}; type=button; action=${order.isArchived?'restore':'archive'} |
| 755 | Düğme | ${escapeHtml(String(b\|\|translateUiText('Tedarikçi ayrıntısını aç')))} | id=licenseSupplierLink; type=button |
| 756 | Düğme | ${translateUiText('Düzenle')} | id=editLicenseButton; başlangıçta gizli |
| 757 | Düğme | ${translateUiText('Koltuk ata')} | id=assignLicenseButton; başlangıçta gizli |
| 758 | Düğme | ${translateUiText('Sipariş kaydı ekle')} | id=newLicensePurchaseOrderButton; type=button; başlangıçta gizli |
| 759 | Düğme | ${translateUiText(l.isArchived?'Arşivden geri yükle':'Arşivle')} | type=button; başlangıçta gizli |
| 760 | Düğme | ${escapeHtml(`${a.assetTag\|\|''}${a.assetName?` · ${a.assetName}`:''}`)} | type=button |
| 761 | Düğme | ${escapeHtml(a.employee\|\|'—')} | type=button |
| 762 | Düğme | ${translateUiText('Atamayı kaldır')} | id=${a.id}; type=button |
| 763 | Düğme | ${translateUiText('Sil')} | id=${a.id}; type=button |
| 764 | Düğme | ${escapeHtml(a.license)} | id=${a.licenseId}; type=button |
| 765 | Düğme | ${escapeHtml(`${a.assetTag\|\|''}${a.assetName?` · ${a.assetName}`:''}`)} | type=button |
| 766 | Düğme | ${escapeHtml(a.employee\|\|'—')} | type=button |
| 767 | Düğme | ${translateUiText('Atamayı kaldır')} | id=${a.licenseId}; type=button |
| 768 | Düğme | ${escapeHtml(item.name)} | type=button |
| 769 | Düğme | ${translateUiText('Ayrıntı')} | type=button |
| 770 | Düğme | ${translateUiText('Düzenle')} | type=button |
| 771 | Düğme | ${translateUiText(item.status === 'archived' ? 'Geri yükle' : 'Arşivle')} | type=button |
| 772 | Düğme | ${translateUiText('Sil')} | id=${file.id}; type=button |
| 773 | Düğme | ${escapeHtml(item.name)} | type=button |
| 774 | Düğme | Ayrıntı | type=button |
| 775 | Düğme | Düzenle | type=button |
| 776 | Düğme | Stok | type=button |
| 777 | Düğme | Çıkış | type=button |
| 778 | Düğme | Düzenle | id=editConsumableButton; başlangıçta gizli |
| 779 | Düğme | Stok ekle / düzelt | id=consumableStockButton; başlangıçta gizli |
| 780 | Düğme | item.issued) ? '' : 'hidden'}>Çalışana tüketim çıkışı | id=consumableCheckoutButton; başlangıçta gizli |
| 781 | Düğme | ${item.isArchived?'Arşivden çıkar':'Arşivle'} | id=${item.id} |
| 782 | Düğme | ${escapeHtml(item.name)} | type=button |
| 783 | Düğme | ${escapeHtml(item.category \|\| translateUiText('Kategori yok'))} | id=${escapeHtml(item.categoryId \|\| '')}; type=button |
| 784 | Düğme | ${translateUiText('Ayrıntı')} | type=button |
| 785 | Düğme | ${translateUiText('Düzenle')} | type=button |
| 786 | Düğme | ${translateUiText('Teslim')} | type=button |
| 787 | Düğme | ${translateUiText(accessoryOnly ? 'Teslim et' : 'Talep oluştur')} | id=${kit.id}; type=button |
| 788 | Düğme | Geçmiş | id=${kit.id}; type=button |
| 789 | Düğme | Düzenle | id=${kit.id}; type=button |
| 790 | Düğme | Arşivle | id=${kit.id}; type=button |
| 791 | Düğme | Geri al | id=${kit.id}; type=button |
| 792 | Düğme | ${translateUiText('Düzenle')} | id=editAccessoryButton; type=button; başlangıçta gizli |
| 793 | Düğme | ${translateUiText('Stok ekle / düzelt')} | id=accessoryStockButton; type=button; başlangıçta gizli |
| 794 | Düğme | currentAccessory.checkedOut ? '' : 'hidden'}>${translateUiText('Çalışana teslim et')} | id=accessoryCheckoutButton; type=button; başlangıçta gizli |
| 795 | Düğme | ${translateUiText(currentAccessory.isArchived?'Arşivden çıkar':'Arşivle')} | id=${currentAccessory.id} |
| 796 | Düğme | ${translateUiText('Yeni sipariş')} | id=createAccessoryOrderFromDetail; type=button; başlangıçta gizli |
| 797 | Düğme | ${escapeHtml(order.orderNumber)} | id=${order.purchaseOrderId}; type=button |
| 798 | Düğme | İade al | id=${assignment.id}; type=button |
| 799 | Düğme | Düzenle | type=button |
| 800 | Düğme | ${translateUiText(category.isArchived ? 'Geri yükle' : 'Arşivle')} | type=button |
| 801 | Düğme | Kapat | type=button |
| 802 | Düğme | Vazgeç | type=button |
| 803 | Düğme | Stok farklarını kaydet | type=submit |
| 804 | Düğme | ${escapeHtml(order.orderNumber)} | id=${order.id}; type=button |
| 805 | Düğme | Ayrıntı | id=${order.id}; type=button |
| 806 | Düğme | Teslim al | id=${order.id}; type=button |
| 807 | Düğme | İptal | id=${order.id}; type=button |
| 808 | Düğme | ${escapeHtml(order.orderNumber)} | id=${order.id}; type=button |
| 809 | Düğme | Ayrıntı | id=${order.id}; type=button |
| 810 | Düğme | Teslim al | id=${order.id}; type=button |
| 811 | Düğme | İptal | id=${order.id}; type=button |
| 812 | Düğme | Kalemi kaldır | type=button |
| 813 | Düğme | ${escapeHtml(order.orderNumber)} | id=${order.id}; type=button |
| 814 | Düğme | Ayrıntı | id=${order.id}; type=button |
| 815 | Düğme | Teslim al | id=${order.id}; type=button |
| 816 | Düğme | İptal | id=${order.id}; type=button |
| 817 | Düğme | Kaldır | type=button |
| 818 | Düğme | ${escapeHtml(asset.assetTag)} | id=${asset.id}; type=button |
| 819 | Düğme | Sil | id=${item.id}; type=button |
| 820 | Düğme | ${escapeHtml(order.orderNumber)} | id=${escapeHtml(order.id)}; type=button |
| 821 | Düğme | ${translateUiText('Ayrıntı')} | id=${escapeHtml(order.id)}; type=${escapeHtml(order.type)} |
| 822 | Düğme | Düzenle | id=${supplier.id}; type=button |
| 823 | Düğme | Arşivle | id=${supplier.id}; type=button |
| 824 | Düğme | Geri al | id=${supplier.id}; type=button |
| 825 | Düğme | Kalemi kaldır | type=button |
| 826 | Düğme | Kaldır | id=${item.id}; type=button |
| 827 | Düğme | ${translateUiText('Ayrıntılar')} | type=button |
| 828 | Düğme | ${translateUiText('Teknik dosyalar')} (${fmt.format(item.attachmentCount \|\| 0)}) | type=button |
| 829 | Düğme | ${translateUiText('Görseli yükle')} | type=button |
| 830 | Düğme | ${translateUiText('Amortismanı kaydet')} | type=button |
| 831 | Düğme | ${translateUiText('Üretici bilgilerini kaydet')} | type=button |
| 832 | Düğme | ${translateUiText('Logoyu yükle')} | type=button |
| 833 | Düğme | ${translateUiText('Kaydet')} |  |
| 834 | Düğme | ${translateUiText(deleteReason)} |  |
| 835 | Düğme | ${translateUiText('Sil')} | type=button |
| 836 | Düğme | ${translateUiText('Teknik dosyalar')} | type=button |
| 837 | Düğme | ${escapeHtml(asset.assetTag)} | type=button |
| 838 | Düğme | Filtrele | id=applyAssetCustomFieldFilter; type=button |
| 839 | Düğme | Temizle | id=clearAssetCustomFieldFilter; type=button |
| 840 | Düğme | Özel alan ekle | type=submit |
| 841 | Düğme | Kapat | type=button |
| 842 | Düğme | Vazgeç | type=button |
| 843 | Düğme | Kaydet | type=submit |
| 844 | Düğme | Düzenle | id=${field.id}; type=button |
| 845 | Düğme | ${field.isActive ? 'Pasifleştir' : 'Etkinleştir'} | id=${field.id}; type=button |
| 846 | Düğme | Eşlemeyi kaydet | id=saveEmployeeImportProfile; type=button |
| 847 | Düğme | Profili sil | id=deleteEmployeeImportProfile; type=button |
| 848 | Düğme | Eşlemeyi kullan ve yeniden doğrula | id=applyEmployeeImportMapping; type=button |
| 849 | Düğme | Kapat | id=closeBulkAssetAction; type=button |
| 850 | Düğme | Vazgeç | id=cancelBulkAssetAction; type=button |
| 851 | Düğme | submitBulkAssetAction | id=submitBulkAssetAction; type=submit |
| 852 | Düğme | ${escapeHtml(translateUiText('Talep et'))} | id=${item.id}; type=button |
| 853 | Girdi alanı | employeeHandoverRecipientName | id=employeeHandoverRecipientName |
| 854 | Girdi alanı | file | type=file; name=file |
| 855 | Girdi alanı | Envanterde ara | id=globalSearch |
| 856 | Girdi alanı | Varlık listesini ara | id=assetListSearch |
| 857 | Seçim alanı | Sayfa başına kayıt | id=assetPageSize; seçenekler=25: 25 kayıt, 50: 50 kayıt, 100: 100 kayıt, 200: 200 kayıt |
| 858 | Seçim alanı | Duruma göre filtrele | id=assetStatusFilter; seçenekler=(değer yok): Tüm durumlar, Available: Kullanılabilir, Deployed: Atanmış, Pending: Beklemede, Maintenance: Bakımda, Unusable: Kullanılamaz, Archived: Arşiv |
| 859 | Seçim alanı | Kişisel cihaz filtresi | id=assetByodFilter; seçenekler=(değer yok): Şirket ve kişisel cihazlar, true: Kişisel cihazlar (BYOD), false: Şirket cihazları |
| 860 | Seçim alanı | Garanti durumuna göre filtrele | id=assetWarrantyFilter; seçenekler=(değer yok): Tüm garanti durumları, expired: Süresi dolmuş, expiring: 30 gün içinde dolacak, active: 30 günden uzun süre aktif, unspecified: Bitiş tarihi girilmemiş |
| 861 | Seçim alanı | Kategoriye göre filtrele | id=assetCategoryFilter; seçenekler=(değer yok): Tüm kategoriler |
| 862 | Seçim alanı | Markaya göre filtrele | id=assetBrandFilter; seçenekler=(değer yok): Tüm markalar |
| 863 | Seçim alanı | Modele göre filtrele | id=assetModelFilter; seçenekler=(değer yok): Tüm modeller |
| 864 | Seçim alanı | Kaydedilmiş varlık aramaları | id=savedAssetSearch; seçenekler=(değer yok): Kayıtlı aramalar |
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
| 876 | Girdi alanı | (etiketsiz) | type=checkbox |
| 877 | Girdi alanı | (etiketsiz) | type=checkbox |
| 878 | Girdi alanı | (etiketsiz) | type=checkbox |
| 879 | Girdi alanı | (etiketsiz) | type=checkbox |
| 880 | Girdi alanı | (etiketsiz) | type=checkbox |
| 881 | Girdi alanı | (etiketsiz) | type=checkbox |
| 882 | Girdi alanı | (etiketsiz) | type=checkbox |
| 883 | Girdi alanı | (etiketsiz) | type=checkbox |
| 884 | Girdi alanı | (etiketsiz) | type=checkbox |
| 885 | Girdi alanı | (etiketsiz) | type=checkbox |
| 886 | Girdi alanı | (etiketsiz) | type=checkbox |
| 887 | Seçim alanı | Tablo satır yoğunluğu | id=assetRowDensity; seçenekler=comfortable: Rahat satırlar, compact: Sık satırlar |
| 888 | Girdi alanı | Bu sayfadaki seçilebilir varlıkları seç | id=selectAllAssets; type=checkbox |
| 889 | Seçim alanı | Seçili varlıklara uygulanacak durum | id=bulkAssetStatus; seçenekler=(değer yok): Yeni durumu seçin, Available: Kullanılabilir, Pending: Beklemede, Maintenance: Bakımda, Unusable: Kullanılamaz, Archived: Arşiv |
| 890 | Girdi alanı | Bu sayfadaki seçilebilir varlıkları seç | id=selectAllAssetsHeader; type=checkbox |
| 891 | Girdi alanı | Çalışan kayıtlarında ara | id=employeeSearch |
| 892 | Girdi alanı | Departmana göre filtrele | id=employeeDepartmentFilter |
| 893 | Girdi alanı | Yönetici ara | id=employeeManagerFilterSearch |
| 894 | Seçim alanı | Yöneticiye göre filtrele | id=employeeManagerFilter; seçenekler=(değer yok): Tüm yöneticiler |
| 895 | Girdi alanı | employeeIncludeArchived | id=employeeIncludeArchived; type=checkbox |
| 896 | Seçim alanı | Çalışan durumuna göre filtrele | id=employeeState; seçenekler=(değer yok): Tüm çalışanlar, true: Aktif çalışanlar, false: Pasif çalışanlar |
| 897 | Girdi alanı | İşe başlama başlangıç tarihi | id=employeeStartedFrom; type=date |
| 898 | Girdi alanı | İşe başlama bitiş tarihi | id=employeeStartedTo; type=date |
| 899 | Seçim alanı | Seçilen çalışanların yeni durumu | id=employeeBulkActive; başlangıçta gizli; seçenekler=false: Pasife al, true: Etkinleştir |
| 900 | Girdi alanı | Liste kayıtlarında ara | id=directorySearch |
| 901 | Girdi alanı | companyUnitsIncludeInactive | id=companyUnitsIncludeInactive; type=checkbox |
| 902 | Girdi alanı | Şirket birimlerinde ara | id=companyUnitSearch |
| 903 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 904 | Girdi alanı | name | name=name |
| 905 | Seçim alanı | unitType | name=unitType; seçenekler=organizational: Organizasyon birimi, legal_entity: Tüzel kişilik, branch: Şube |
| 906 | Girdi alanı | code | name=code |
| 907 | Seçim alanı | parentCompanyUnitId | name=parentCompanyUnitId; seçenekler=(değer yok): Üst birim yok |
| 908 | Girdi alanı | Aksesuarlarda ara | id=accessorySearch |
| 909 | Seçim alanı | Stok durumuna göre filtrele | id=accessoryStockFilter; seçenekler=(değer yok): Tüm stoklar, available: Kullanılabilir stok, low: Minimum seviyede veya altında |
| 910 | Seçim alanı | Aksesuarları sırala | id=accessorySort; seçenekler=name:asc: Ada göre A–Z, name:desc: Ada göre Z–A, remaining:asc: Kalan adede göre artan, remaining:desc: Kalan adede göre azalan, updated:desc: En son güncellenen |
| 911 | Seçim alanı | Sayfa başına aksesuar sayısı | id=accessoryPageSize; seçenekler=(değer yok): 12, (değer yok): 25, (değer yok): 50, (değer yok): 100 |
| 912 | Girdi alanı | (etiketsiz) | type=checkbox |
| 913 | Girdi alanı | (etiketsiz) | type=checkbox |
| 914 | Girdi alanı | (etiketsiz) | type=checkbox |
| 915 | Girdi alanı | (etiketsiz) | type=checkbox |
| 916 | Girdi alanı | (etiketsiz) | type=checkbox |
| 917 | Girdi alanı | (etiketsiz) | type=checkbox |
| 918 | Girdi alanı | (etiketsiz) | type=checkbox |
| 919 | Girdi alanı | (etiketsiz) | type=checkbox |
| 920 | Girdi alanı | (etiketsiz) | type=checkbox |
| 921 | Girdi alanı | (etiketsiz) | type=checkbox |
| 922 | Seçim alanı | Kategoriye göre filtrele | id=accessoryCategoryFilter; seçenekler=(değer yok): Tüm kategoriler |
| 923 | Seçim alanı | Konuma göre filtrele | id=accessoryLocationFilter; seçenekler=(değer yok): Tüm konumlar |
| 924 | Girdi alanı | Satın alma siparişlerinde ara | id=accessoryOrderSearch |
| 925 | Seçim alanı | Sipariş durumuna göre filtrele | id=accessoryOrderStatus; seçenekler=(değer yok): Tüm durumlar, Ordered: Sipariş edildi, PartiallyReceived: Kısmi teslim alındı, Received: Tamamlandı, Cancelled: İptal edildi |
| 926 | Seçim alanı | Teslimat takvimine göre filtrele | id=accessoryOrderDelivery; seçenekler=(değer yok): Tüm teslimatlar, overdue: Geciken teslimatlar |
| 927 | Girdi alanı | Sipariş başlangıç tarihi | id=accessoryOrderFrom; type=date |
| 928 | Girdi alanı | Sipariş bitiş tarihi | id=accessoryOrderTo; type=date |
| 929 | Seçim alanı | Sayfa başına sipariş sayısı | id=accessoryOrderPageSize; seçenekler=25: 25, 50: 50, 100: 100 |
| 930 | Girdi alanı | Hazır kitlerde ara | id=accessoryKitSearch |
| 931 | Girdi alanı | Sarf malzemelerinde ara | id=consumableSearch |
| 932 | Seçim alanı | Stok durumuna göre filtrele | id=consumableStockFilter; seçenekler=(değer yok): Tüm stoklar, available: Stokta olanlar, low: Minimum seviyede veya altında |
| 933 | Seçim alanı | name:asc: Ada göre A–Z, name:desc: Ada göre Z–A, remaining:asc: Kalan miktar artan, remaining:desc: Kalan miktar azalan, updated:desc: Son güncellenen | id=consumableSort; seçenekler=name:asc: Ada göre A–Z, name:desc: Ada göre Z–A, remaining:asc: Kalan miktar artan, remaining:desc: Kalan miktar azalan, updated:desc: Son güncellenen |
| 934 | Girdi alanı | Talep edilebilir stoklarda ara | id=requestCatalogSearch |
| 935 | Seçim alanı | Talep durumuna göre filtrele | id=requestStatusFilter; seçenekler=(değer yok): Tüm durumlar, Pending: Bekliyor, Approved: Onaylandı, Rejected: Reddedildi, Cancelled: İptal edildi |
| 936 | Girdi alanı | Taleplerde ara | id=requestSearch |
| 937 | Metin alanı | note | id=inventoryRequestActionNote; name=note |
| 938 | Girdi alanı | licenseSearch | id=licenseSearch |
| 939 | Seçim alanı | Lisans durumuna göre filtrele | id=licenseStatusFilter; seçenekler=active: Etkin lisanslar, archived: Arşivlenmiş lisanslar, all: Tüm lisanslar |
| 940 | Girdi alanı | licenseAssignmentSearch | id=licenseAssignmentSearch |
| 941 | Seçim alanı | true: Aktif atamalar, false: Geri alınan atamalar, (değer yok): Tüm atamalar | id=licenseAssignmentState; seçenekler=true: Aktif atamalar, false: Geri alınan atamalar, (değer yok): Tüm atamalar |
| 942 | Girdi alanı | kind | type=checkbox; name=kind |
| 943 | Girdi alanı | kind | type=checkbox; name=kind |
| 944 | Girdi alanı | kind | type=checkbox; name=kind |
| 945 | Girdi alanı | kind | type=checkbox; name=kind |
| 946 | Girdi alanı | kind | type=checkbox; name=kind |
| 947 | Girdi alanı | kind | type=checkbox; name=kind |
| 948 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 949 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 950 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 951 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 952 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 953 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 954 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 955 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 956 | Girdi alanı | emailKind | type=checkbox; name=emailKind |
| 957 | Girdi alanı | componentSearch | id=componentSearch |
| 958 | Seçim alanı | (değer yok): Tüm stoklar, available: Kullanılabilir miktar, low: Minimum seviyede veya altında | id=componentStockFilter; seçenekler=(değer yok): Tüm stoklar, available: Kullanılabilir miktar, low: Minimum seviyede veya altında |
| 959 | Girdi alanı | Stok hareketlerinde ara | id=inventoryMovementSearch |
| 960 | Seçim alanı | Stok türüne göre filtrele | id=inventoryMovementKind; seçenekler=(değer yok): Tüm stok türleri, accessory: Aksesuar, consumable: Sarf malzemesi, component: Bileşen |
| 961 | Seçim alanı | Hareket türüne göre filtrele | id=inventoryMovementType; seçenekler=(değer yok): Tüm hareketler, Received: Stok girişi, Adjustment: Sayım düzeltmesi, Checkout: Çıkış / atama, Checkin: İade / çıkarma |
| 962 | Girdi alanı | Başlangıç tarihi | id=inventoryMovementFrom; type=date |
| 963 | Girdi alanı | Bitiş tarihi | id=inventoryMovementTo; type=date |
| 964 | Seçim alanı | (değer yok): Şablon seçin | id=labelStockTemplate; seçenekler=(değer yok): Şablon seçin |
| 965 | Girdi alanı | labelStockTemplateName | id=labelStockTemplateName |
| 966 | Girdi alanı | labelStockTemplateManufacturer | id=labelStockTemplateManufacturer |
| 967 | Girdi alanı | labelStockTemplatePrinter | id=labelStockTemplatePrinter |
| 968 | Girdi alanı | labelStockTemplateProduct | id=labelStockTemplateProduct |
| 969 | Seçim alanı | barcode1d | name=barcode1d; seçenekler=code128: C128 · alfasayısal, code39: C39 · büyük harf ve rakam, code93: C93 · kompakt alfasayısal, code93ext: C93 Extended · geniş karakter kümesi, rationalizedCodabar: Codabar · sayısal / A–D başlangıç-bitiş, msi: MSI Plessey · sayısal stok kodu, ean5: EAN5 · 5 rakam, ean13: EAN13 · 12/13 rakam, upca: UPC-A · 11/12 rakam, upce: UPC-E · 6–8 rakam, none: Kapalı |
| 970 | Seçim alanı | barcode2d | name=barcode2d; seçenekler=qrcode: QR Code, pdf417: PDF417, datamatrix: Data Matrix, azteccode: Aztec Code, none: Kapalı |
| 971 | Seçim alanı | sizePreset | name=sizePreset; seçenekler=86x48: 86 × 48 mm · varsayılan, 50x30: 50 × 30 mm, 60x40: 60 × 40 mm, 70x35: 70 × 35 mm, 100x50: 100 × 50 mm, custom: Özel ölçü |
| 972 | Girdi alanı | widthMm | type=number; name=widthMm |
| 973 | Girdi alanı | heightMm | type=number; name=heightMm |
| 974 | Girdi alanı | offsetXmm | type=number; name=offsetXmm |
| 975 | Girdi alanı | offsetYmm | type=number; name=offsetYmm |
| 976 | Girdi alanı | paddingMm | type=number; name=paddingMm |
| 977 | Girdi alanı | codeSizeMm | type=number; name=codeSizeMm |
| 978 | Seçim alanı | qrTarget | name=qrTarget; seçenekler=tag: Varlık etiketi · aramayla bulunur, asset-url: Giriş sonrası varlık ayrıntısını aç |
| 979 | Girdi alanı | showName | type=checkbox; name=showName |
| 980 | Girdi alanı | showSerial | type=checkbox; name=showSerial |
| 981 | Girdi alanı | showModel | type=checkbox; name=showModel |
| 982 | Girdi alanı | showLocation | type=checkbox; name=showLocation |
| 983 | Girdi alanı | showStatus | type=checkbox; name=showStatus |
| 984 | Seçim alanı | sheetLayout | name=sheetLayout; seçenekler=single: Her etiketi ayrı sayfaya bas, sheet: Etiket yaprağına otomatik yerleştir |
| 985 | Seçim alanı | sheetPaper | name=sheetPaper; seçenekler=a4: A4 · 210 × 297 mm, letter: Letter · 216 × 279 mm |
| 986 | Girdi alanı | sheetMarginMm | type=number; name=sheetMarginMm |
| 987 | Girdi alanı | sheetGapMm | type=number; name=sheetGapMm |
| 988 | Girdi alanı | name | name=name |
| 989 | Seçim alanı | brandId | name=brandId; seçenekler=(değer yok): Marka seçin (isteğe bağlı) |
| 990 | Seçim alanı | categoryId | name=categoryId; seçenekler=(değer yok): Kategori seçin |
| 991 | Girdi alanı | modelNumber | name=modelNumber |
| 992 | Girdi alanı | endOfLifeOn | type=date; name=endOfLifeOn |
| 993 | Girdi alanı | website | type=url; name=website |
| 994 | Girdi alanı | supportUrl | type=url; name=supportUrl |
| 995 | Girdi alanı | warrantyUrl | type=url; name=warrantyUrl |
| 996 | Girdi alanı | supportPhone | type=tel; name=supportPhone |
| 997 | Girdi alanı | supportEmail | type=email; name=supportEmail |
| 998 | Girdi alanı | brandLogo | type=file; name=brandLogo |
| 999 | Girdi alanı | modelPhoto | type=file; name=modelPhoto |
| 1000 | Girdi alanı | depreciationMonths | type=number; name=depreciationMonths |
| 1001 | Girdi alanı | depreciationAnnualRatePercent | type=number; name=depreciationAnnualRatePercent |
| 1002 | Girdi alanı | depreciationMinimum | type=number; name=depreciationMinimum |
| 1003 | Seçim alanı | depreciationMinimumType | name=depreciationMinimumType; seçenekler=amount: Tutar, percent: Yüzde |
| 1004 | Seçim alanı | depreciationMethod | name=depreciationMethod; seçenekler=linear: Doğrusal, half_1: Yarım yıl · ilk dönem, half_2: Yarım yıl · satın alma dönemine göre |
| 1005 | Girdi alanı | catalogSearch | id=catalogSearch |
| 1006 | Girdi alanı | name | name=name |
| 1007 | Girdi alanı | slug | name=slug |
| 1008 | Girdi alanı | contactEmail | type=email; name=contactEmail |
| 1009 | Girdi alanı | brandPrimaryColor | type=color; name=brandPrimaryColor |
| 1010 | Girdi alanı | logo | type=file; name=logo |
| 1011 | Girdi alanı | Zimmetlerde ara | id=assignmentSearch |
| 1012 | Seçim alanı | İade tarihine göre filtrele | id=returnsState; seçenekler=overdue: Geciken iadeler, due30: Önümüzdeki 30 gün, undated: Tarihi belirtilmemiş, later: Daha ileri tarih, all: Tüm zimmetler |
| 1013 | Girdi alanı | İadelerde ara | id=returnsSearch |
| 1014 | Girdi alanı | Hareket kayıtlarında ara | id=activitySearch |
| 1015 | Seçim alanı | (değer yok): Tüm işlem türleri, Created: Oluşturuldu, Purchased: Satın alma teslimatı, Assigned: Zimmetlendi, Returned: İade alındı, StatusChanged: Durum değişti, Transferred: Konum transferi | id=activityType; seçenekler=(değer yok): Tüm işlem türleri, Created: Oluşturuldu, Purchased: Satın alma teslimatı, Assigned: Zimmetlendi, Returned: İade alındı, StatusChanged: Durum değişti, Transferred: Konum transferi |
| 1016 | Girdi alanı | activityFrom | id=activityFrom; type=date |
| 1017 | Girdi alanı | activityTo | id=activityTo; type=date |
| 1018 | Seçim alanı | assetId | id=reservationAsset; name=assetId; seçenekler=(değer yok): Uygun varlıklar yükleniyor… |
| 1019 | Seçim alanı | employeeId | id=reservationEmployee; name=employeeId; seçenekler=(değer yok): Çalışanlar yükleniyor… |
| 1020 | Girdi alanı | startsOn | type=date; name=startsOn |
| 1021 | Girdi alanı | endsOn | type=date; name=endsOn |
| 1022 | Metin alanı | requestNote | name=requestNote |
| 1023 | Girdi alanı | Rezervasyonlarda ara | id=reservationSearch; type=search |
| 1024 | Seçim alanı | Rezervasyon durumu | id=reservationStatus; seçenekler=(değer yok): Tüm durumlar, Pending: Onay bekliyor, Approved: Onaylandı, Rejected: Reddedildi, Cancelled: İptal edildi, Completed: Tamamlandı |
| 1025 | Girdi alanı | reservationFrom | id=reservationFrom; type=date |
| 1026 | Girdi alanı | reservationTo | id=reservationTo; type=date |
| 1027 | Metin alanı | note | id=reservationReviewNote; name=note |
| 1028 | Seçim alanı | 30: 30 gün, 90: 90 gün, 180: 180 gün, 366: 1 yıl | id=auditScheduleHorizon; seçenekler=30: 30 gün, 90: 90 gün, 180: 180 gün, 366: 1 yıl |
| 1029 | Seçim alanı | Denetim planı durumu | id=auditScheduleState; seçenekler=all: Gecikmiş ve yaklaşan, overdue: Yalnızca gecikmiş, upcoming: Yalnızca yaklaşan, unscheduled: Denetim planı olmayanlar, completed: Tamamlanan denetimler |
| 1030 | Girdi alanı | Denetim kuyruğunda ara | id=auditScheduleSearch |
| 1031 | Girdi alanı | Denetim kayıtlarında ara | id=auditSearch |
| 1032 | Seçim alanı | (değer yok): Tüm kayıt türleri, Asset: Varlık, Employee: Çalışan, Company: Şirket, categories: Kategoriler, brands: Markalar, models: Modeller | id=auditTarget; seçenekler=(değer yok): Tüm kayıt türleri, Asset: Varlık, Employee: Çalışan, Company: Şirket, categories: Kategoriler, brands: Markalar, models: Modeller |
| 1033 | Seçim alanı | İşlem türü | id=auditAction; seçenekler=(değer yok): Tüm işlemler |
| 1034 | Girdi alanı | Denetim başlangıç tarihi | id=auditFrom; type=date |
| 1035 | Girdi alanı | Denetim bitiş tarihi | id=auditTo; type=date |
| 1036 | Girdi alanı | Sözleşmelerde ara | id=contractSearch |
| 1037 | Seçim alanı | (değer yok): Etkin ve arşiv kayıtları, active: Etkin, expiring: 30 gün içinde bitecek, expired: Süresi dolmuş, archived: Arşiv | id=contractStatus; seçenekler=(değer yok): Etkin ve arşiv kayıtları, active: Etkin, expiring: 30 gün içinde bitecek, expired: Süresi dolmuş, archived: Arşiv |
| 1038 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1039 | Girdi alanı | title | name=title |
| 1040 | Metin alanı | description | name=description |
| 1041 | Seçim alanı | priority | name=priority; seçenekler=Low: Düşük, Normal: Normal, High: Yüksek, Urgent: Acil |
| 1042 | Seçim alanı | assignedEmployeeId | name=assignedEmployeeId; seçenekler=(değer yok): Atanmamış |
| 1043 | Girdi alanı | dueAt | type=date; name=dueAt |
| 1044 | Girdi alanı | Görevlerde ara | id=taskSearch |
| 1045 | Seçim alanı | Görev durumu | id=taskStatusFilter; seçenekler=(değer yok): Tüm durumlar, Open: Açık, InProgress: Devam ediyor, Completed: Tamamlandı, Cancelled: İptal edildi |
| 1046 | Seçim alanı | Görev önceliği | id=taskPriorityFilter; seçenekler=(değer yok): Tüm öncelikler, Low: Düşük, Normal: Normal, High: Yüksek, Urgent: Acil |
| 1047 | Girdi alanı | subject | name=subject |
| 1048 | Seçim alanı | category | name=category; seçenekler=Access: Hesap ve erişim, Inventory: Envanter, Data: Veri ve aktarım, Other: Diğer |
| 1049 | Metin alanı | message | name=message |
| 1050 | Girdi alanı | Destek taleplerinde ara | id=supportSearch |
| 1051 | Seçim alanı | Talep durumu | id=supportStatusFilter; seçenekler=(değer yok): Tüm durumlar, Open: Açık, InProgress: İşlemde, AwaitingCustomer: Yanıtınız bekleniyor, Resolved: Çözüldü, Closed: Kapatıldı |
| 1052 | Metin alanı | body | name=body |
| 1053 | Girdi alanı | passwordMinimumLength | type=number; name=passwordMinimumLength |
| 1054 | Girdi alanı | sessionDurationHours | type=number; name=sessionDurationHours |
| 1055 | Girdi alanı | requireMfa | type=checkbox; name=requireMfa |
| 1056 | Girdi alanı | name | name=name |
| 1057 | Girdi alanı | scope | type=checkbox; name=scope |
| 1058 | Girdi alanı | scope | type=checkbox; name=scope |
| 1059 | Girdi alanı | scope | type=checkbox; name=scope |
| 1060 | Girdi alanı | scope | type=checkbox; name=scope |
| 1061 | Seçim alanı | expiresInDays | name=expiresInDays; seçenekler=(değer yok): Süresiz, 30: 30 gün, 90: 90 gün, 365: 365 gün |
| 1062 | Girdi alanı | integrationSecretValue | id=integrationSecretValue; type=password |
| 1063 | Girdi alanı | Bakım kayıtlarında ara | id=maintenanceSearch |
| 1064 | Seçim alanı | (değer yok): Tüm durumlar, Scheduled: Planlandı, InProgress: Devam ediyor, Completed: Tamamlandı, Cancelled: İptal edildi | id=maintenanceStatus; seçenekler=(değer yok): Tüm durumlar, Scheduled: Planlandı, InProgress: Devam ediyor, Completed: Tamamlandı, Cancelled: İptal edildi |
| 1065 | Girdi alanı | maintenanceFrom | id=maintenanceFrom; type=date |
| 1066 | Girdi alanı | maintenanceTo | id=maintenanceTo; type=date |
| 1067 | Girdi alanı | calendarFrom | id=calendarFrom; type=date |
| 1068 | Girdi alanı | calendarTo | id=calendarTo; type=date |
| 1069 | Seçim alanı | Takvim etkinlik türü | id=calendarKind; seçenekler=(değer yok): Bakım, denetim, lisans, garanti, iade ve rezervasyon, maintenance: Yalnızca bakım planları, audit: Yalnızca fiziksel denetimler, license: Yalnızca lisans bitişleri, warranty: Yalnızca garanti bitişleri, return: Yalnızca beklenen varlık iadeleri, reservation: Yalnızca onaylı rezervasyonlar |
| 1070 | Seçim alanı | Takvim görünümü | id=calendarViewMode; seçenekler=list: Liste görünümü, day: Gün görünümü, week: Hafta görünümü, month: Ay görünümü |
| 1071 | Girdi alanı | reportDateFrom | id=reportDateFrom; type=date |
| 1072 | Girdi alanı | reportDateTo | id=reportDateTo; type=date |
| 1073 | Girdi alanı | Varlık alış raporunda ara | id=assetPurchaseReportSearch |
| 1074 | Girdi alanı | auditReportFrom | id=auditReportFrom; type=date |
| 1075 | Girdi alanı | auditReportTo | id=auditReportTo; type=date |
| 1076 | Girdi alanı | Fiziksel denetim sonuçlarında ara | id=auditReportSearch |
| 1077 | Seçim alanı | Garanti raporu durumu | id=warrantyReportState; seçenekler=upcoming: Yaklaşan, expired: Süresi dolan, later: Daha ileri tarih, missing: Tarihi olmayan, all: Tümü |
| 1078 | Seçim alanı | 30: 30 gün, 90: 90 gün, 180: 180 gün, 366: 1 yıl | id=warrantyReportHorizon; seçenekler=30: 30 gün, 90: 90 gün, 180: 180 gün, 366: 1 yıl |
| 1079 | Girdi alanı | Garanti raporunda ara | id=warrantyReportSearch |
| 1080 | Girdi alanı | maintenanceReportFrom | id=maintenanceReportFrom; type=date |
| 1081 | Girdi alanı | maintenanceReportTo | id=maintenanceReportTo; type=date |
| 1082 | Seçim alanı | Lisans bitiş raporu durumu | id=licenseReportState; seçenekler=upcoming: Yaklaşan, expired: Süresi dolan, later: Daha ileri tarih, missing: Tarihi olmayan, all: Tümü |
| 1083 | Seçim alanı | 30: 30 gün, 90: 90 gün, 180: 180 gün, 366: 1 yıl | id=licenseReportHorizon; seçenekler=30: 30 gün, 90: 90 gün, 180: 180 gün, 366: 1 yıl |
| 1084 | Girdi alanı | Lisans bitiş raporunda ara | id=licenseReportSearch |
| 1085 | Seçim alanı | Bekleyen teslimat tarihine göre filtrele | id=pendingDeliveryState; seçenekler=all: Tüm açık kalemler, overdue: Geciken teslimatlar, due30: Önümüzdeki 30 gün, undated: Tarihi belirtilmemiş |
| 1086 | Girdi alanı | Bekleyen teslimat raporunda ara | id=pendingDeliverySearch |
| 1087 | Girdi alanı | depreciationReportAsOf | id=depreciationReportAsOf; type=date |
| 1088 | Girdi alanı | Amortisman raporunda ara | id=depreciationReportSearch |
| 1089 | Girdi alanı | customReportName | id=customReportName |
| 1090 | Seçim alanı | (değer yok): Taslak seçin… | id=customReportPreset; seçenekler=(değer yok): Taslak seçin… |
| 1091 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1092 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1093 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1094 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1095 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1096 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1097 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1098 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1099 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1100 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1101 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1102 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1103 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1104 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1105 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1106 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1107 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1108 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1109 | Girdi alanı | customReportSearch | id=customReportSearch |
| 1110 | Seçim alanı | (değer yok): Tüm durumlar, Available: Kullanılabilir, Deployed: Atanmış, Pending: Beklemede, Maintenance: Bakımda, Unusable: Kullanılamaz, Archived: Arşivlenmiş | id=customReportStatus; seçenekler=(değer yok): Tüm durumlar, Available: Kullanılabilir, Deployed: Atanmış, Pending: Beklemede, Maintenance: Bakımda, Unusable: Kullanılamaz, Archived: Arşivlenmiş |
| 1111 | Seçim alanı | (değer yok): Tüm garanti durumları, expired: Süresi dolmuş, expiring: Yakında dolacak, active: Geçerli, unspecified: Tarih girilmemiş | id=customReportWarranty; seçenekler=(değer yok): Tüm garanti durumları, expired: Süresi dolmuş, expiring: Yakında dolacak, active: Geçerli, unspecified: Tarih girilmemiş |
| 1112 | Seçim alanı | tag:asc: Etiket · A–Z, tag:desc: Etiket · Z–A, name:asc: Ada göre · A–Z, name:desc: Ada göre · Z–A, category:asc: Kategori · A–Z, brand:asc: Marka / model · A–Z, employee:asc: Çalışan · A–Z, location:asc: Konum · A–Z | id=customReportSort; seçenekler=tag:asc: Etiket · A–Z, tag:desc: Etiket · Z–A, name:asc: Ada göre · A–Z, name:desc: Ada göre · Z–A, category:asc: Kategori · A–Z, brand:asc: Marka / model · A–Z, employee:asc: Çalışan · A–Z, location:asc: Konum · A–Z |
| 1113 | Girdi alanı | Sarf ve bileşen siparişlerinde ara | id=stockOrderSearch |
| 1114 | Seçim alanı | Sipariş durumuna göre filtrele | id=stockOrderStatus; seçenekler=(değer yok): Tüm durumlar, Ordered: Sipariş edildi, PartiallyReceived: Kısmi teslim alındı, Received: Tamamlandı, Cancelled: İptal edildi |
| 1115 | Seçim alanı | Sipariş kalem türüne göre filtrele | id=stockOrderItemType; seçenekler=(değer yok): Tüm kalem türleri, Consumable: Sarf, Component: Bileşen |
| 1116 | Seçim alanı | Teslimat takvimine göre filtrele | id=stockOrderDelivery; seçenekler=(değer yok): Tüm teslimatlar, overdue: Geciken teslimatlar |
| 1117 | Girdi alanı | Sipariş başlangıç tarihi | id=stockOrderFrom; type=date |
| 1118 | Girdi alanı | Sipariş bitiş tarihi | id=stockOrderTo; type=date |
| 1119 | Seçim alanı | Sayfa başına sipariş sayısı | id=stockOrderPageSize; seçenekler=25: 25, 50: 50, 100: 100 |
| 1120 | Girdi alanı | Tüm satın alma siparişlerinde ara | id=purchaseOverviewSearch; type=search |
| 1121 | Seçim alanı | Satın alma türü | id=purchaseOverviewType; seçenekler=(değer yok): Tüm türler, asset: Varlık, accessory: Aksesuar, stock: Sarf / bileşen, license: Yazılım lisansı |
| 1122 | Seçim alanı | Sipariş durumu | id=purchaseOverviewStatus; seçenekler=(değer yok): Tüm durumlar, Ordered: Sipariş edildi, PartiallyReceived: Kısmi teslim, Received: Tamamlandı, Cancelled: İptal edildi, Recorded: Kaydedildi |
| 1123 | Girdi alanı | Sipariş başlangıç tarihi | id=purchaseOverviewFrom; type=date |
| 1124 | Girdi alanı | Sipariş bitiş tarihi | id=purchaseOverviewTo; type=date |
| 1125 | Seçim alanı | Sayfa başına sipariş sayısı | id=purchaseOverviewPageSize; seçenekler=25: 25, 50: 50, 100: 100 |
| 1126 | Girdi alanı | Varlık satın alma siparişlerinde ara | id=assetOrderSearch |
| 1127 | Seçim alanı | Sipariş durumuna göre filtrele | id=assetOrderStatus; seçenekler=(değer yok): Tüm durumlar, Ordered: Sipariş edildi, PartiallyReceived: Kısmi teslim alındı, Received: Tamamlandı, Cancelled: İptal edildi |
| 1128 | Girdi alanı | Sipariş başlangıç tarihi | id=assetOrderFrom; type=date |
| 1129 | Girdi alanı | Sipariş bitiş tarihi | id=assetOrderTo; type=date |
| 1130 | Seçim alanı | Sayfa başına sipariş sayısı | id=assetOrderPageSize; seçenekler=25: 25, 50: 50, 100: 100 |
| 1131 | Girdi alanı | roleKey | type=hidden; name=roleKey; başlangıçta gizli |
| 1132 | Girdi alanı | name | name=name |
| 1133 | Girdi alanı | permission | type=checkbox; name=permission |
| 1134 | Girdi alanı | permission | type=checkbox; name=permission |
| 1135 | Girdi alanı | permission | type=checkbox; name=permission |
| 1136 | Girdi alanı | permission | type=checkbox; name=permission |
| 1137 | Girdi alanı | permission | type=checkbox; name=permission |
| 1138 | Girdi alanı | permission | type=checkbox; name=permission |
| 1139 | Girdi alanı | commandPaletteSearch | id=commandPaletteSearch; type=search |
| 1140 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1141 | Girdi alanı | name | name=name |
| 1142 | Girdi alanı | contractNumber | name=contractNumber |
| 1143 | Girdi alanı | contractType | name=contractType |
| 1144 | Seçim alanı | supplierId | name=supplierId; seçenekler=(değer yok): Elle girilen tedarikçi |
| 1145 | Girdi alanı | supplier | name=supplier |
| 1146 | Girdi alanı | startDate | type=date; name=startDate |
| 1147 | Girdi alanı | endDate | type=date; name=endDate |
| 1148 | Girdi alanı | cost | type=number; name=cost |
| 1149 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1150 | Girdi alanı | contractLookupSearch | id=contractLookupSearch; type=search |
| 1151 | Seçim alanı | Sözleşmeye bağlı varlıklar | id=contractAssetIds; name=assetIds |
| 1152 | Seçim alanı | Sözleşmeye bağlı yazılım lisansları | id=contractLicenseIds; name=licenseIds |
| 1153 | Metin alanı | notes | name=notes |
| 1154 | Girdi alanı | contractAttachmentFile | id=contractAttachmentFile; type=file; başlangıçta gizli |
| 1155 | Seçim alanı | Tema | id=themePreference; seçenekler=system: Sistem ayarını izle, light: Açık, dark: Koyu |
| 1156 | Seçim alanı | tr: Türkçe, en: English, ru: Русский, uk: Українська, mk: Македонски | id=languagePreference; seçenekler=tr: Türkçe, en: English, ru: Русский, uk: Українська, mk: Македонски |
| 1157 | Girdi alanı | Üst varlık ara | id=assetParentSearch; type=search |
| 1158 | Seçim alanı | Üst varlık | id=assetParentSelect; seçenekler=(değer yok): Üst varlık yok |
| 1159 | Metin alanı | note | id=assetNoteInput; name=note |
| 1160 | Girdi alanı | file | id=assetAttachmentInput; type=file; name=file |
| 1161 | Seçim alanı | assetEventId | id=assetAttachmentEvent; name=assetEventId; seçenekler=(değer yok): Bir harekete bağlama |
| 1162 | Seçim alanı | Varlık durumunu değiştir | id=assetStatusAction; seçenekler=(değer yok): Durumu değiştir…, Available: Kullanılabilir, Pending: Beklemede, Maintenance: Bakımda, Unusable: Kullanılamaz, Archived: Arşivle |
| 1163 | Girdi alanı | auditDate | type=date; name=auditDate |
| 1164 | Girdi alanı | nextAuditOn | type=date; name=nextAuditOn |
| 1165 | Seçim alanı | outcome | id=auditOutcome; name=outcome; seçenekler=Accepted: Uygun, Unacceptable: Uygun değil / bulgu var |
| 1166 | Metin alanı | issueDescription | id=auditIssueDescription; name=issueDescription |
| 1167 | Metin alanı | notes | name=notes |
| 1168 | Girdi alanı | nextAuditOn | type=date; name=nextAuditOn |
| 1169 | Girdi alanı | maintenanceType | name=maintenanceType |
| 1170 | Girdi alanı | supplier | name=supplier |
| 1171 | Seçim alanı | responsibleEmployeeId | name=responsibleEmployeeId; seçenekler=(değer yok): Çalışan atama |
| 1172 | Girdi alanı | description | name=description |
| 1173 | Seçim alanı | mode | id=maintenanceMode; name=mode; seçenekler=immediate: Hemen başlat, scheduled: İleri tarihe planla |
| 1174 | Girdi alanı | startedOn | type=date; name=startedOn |
| 1175 | Girdi alanı | plannedOn | type=date; name=plannedOn |
| 1176 | Girdi alanı | cost | type=number; name=cost |
| 1177 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY — Türk lirası, USD: USD — ABD doları, EUR: EUR — Euro |
| 1178 | Metin alanı | notes | name=notes |
| 1179 | Girdi alanı | completedOn | type=date; name=completedOn |
| 1180 | Metin alanı | notes | name=notes |
| 1181 | Girdi alanı | transferSourceLocation | id=transferSourceLocation |
| 1182 | Girdi alanı | targetLocation | name=targetLocation |
| 1183 | Metin alanı | notes | name=notes |
| 1184 | Girdi alanı | expectedUpdatedAt | type=hidden; name=expectedUpdatedAt; başlangıçta gizli |
| 1185 | Girdi alanı | assetTag | name=assetTag |
| 1186 | Girdi alanı | assetName | name=assetName |
| 1187 | Seçim alanı | categoryId | name=categoryId |
| 1188 | Seçim alanı | brandId | name=brandId; seçenekler=(değer yok): Marka seçin |
| 1189 | Seçim alanı | modelId | name=modelId; seçenekler=(değer yok): Model seçin |
| 1190 | Girdi alanı | serialNumber | name=serialNumber |
| 1191 | Girdi alanı | location | name=location |
| 1192 | Seçim alanı | defaultLocationCatalogId | name=defaultLocationCatalogId; seçenekler=(değer yok): Varsayılan konum seçin |
| 1193 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1194 | Girdi alanı | warrantyExpiresAt | type=date; name=warrantyExpiresAt |
| 1195 | Girdi alanı | insuranceProvider | name=insuranceProvider |
| 1196 | Girdi alanı | insurancePolicyNumber | name=insurancePolicyNumber |
| 1197 | Girdi alanı | insuranceExpiresAt | type=date; name=insuranceExpiresAt |
| 1198 | Girdi alanı | purchaseCost | type=number; name=purchaseCost |
| 1199 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY — Türk lirası, USD: USD — ABD doları, EUR: EUR — Euro |
| 1200 | Metin alanı | notes | name=notes |
| 1201 | Girdi alanı | isRequestable | type=checkbox; name=isRequestable |
| 1202 | Girdi alanı | isByod | type=checkbox; name=isByod |
| 1203 | Girdi alanı | assignSearch | id=assignSearch |
| 1204 | Seçim alanı | employeeId | name=employeeId |
| 1205 | Girdi alanı | expectedReturnOn | type=date; name=expectedReturnOn |
| 1206 | Girdi alanı | note | name=note |
| 1207 | Girdi alanı | displayName | name=displayName |
| 1208 | Girdi alanı | employeeNumber | name=employeeNumber |
| 1209 | Girdi alanı | email | type=email; name=email |
| 1210 | Girdi alanı | department | name=department |
| 1211 | Girdi alanı | jobTitle | name=jobTitle |
| 1212 | Girdi alanı | phone | type=tel; name=phone |
| 1213 | Girdi alanı | location | name=location |
| 1214 | Girdi alanı | startedOn | type=date; name=startedOn |
| 1215 | Seçim alanı | companyUnitId | name=companyUnitId; seçenekler=(değer yok): Birim seçilmedi |
| 1216 | Girdi alanı | newEmployeeManagerSearch | id=newEmployeeManagerSearch |
| 1217 | Seçim alanı | managerEmployeeId | id=newEmployeeManagerId; name=managerEmployeeId; seçenekler=(değer yok): Yönetici seçilmedi |
| 1218 | Girdi alanı | employeeImportFile | id=employeeImportFile; type=file; başlangıçta gizli |
| 1219 | Girdi alanı | consumableImportFile | id=consumableImportFile; type=file; başlangıçta gizli |
| 1220 | Seçim alanı | Sarf malzemesi eşleme profili | id=consumableImportProfile; seçenekler=(değer yok): Profil seçin |
| 1221 | Girdi alanı | consumableImportProfileName | id=consumableImportProfileName |
| 1222 | Girdi alanı | accessoryImportFile | id=accessoryImportFile; type=file; başlangıçta gizli |
| 1223 | Seçim alanı | Aksesuar eşleme profili | id=accessoryImportProfile; seçenekler=(değer yok): Profil seçin |
| 1224 | Girdi alanı | accessoryImportProfileName | id=accessoryImportProfileName |
| 1225 | Girdi alanı | displayName | name=displayName |
| 1226 | Girdi alanı | employeeNumber | name=employeeNumber |
| 1227 | Girdi alanı | email | type=email; name=email |
| 1228 | Girdi alanı | department | name=department |
| 1229 | Girdi alanı | jobTitle | name=jobTitle |
| 1230 | Girdi alanı | phone | type=tel; name=phone |
| 1231 | Girdi alanı | location | name=location |
| 1232 | Girdi alanı | startedOn | type=date; name=startedOn |
| 1233 | Seçim alanı | companyUnitId | name=companyUnitId; seçenekler=(değer yok): Birim seçilmedi |
| 1234 | Girdi alanı | value | id=textEntryValue; type=text; name=value |
| 1235 | Girdi alanı | Yönetici ara | id=employeeManagerSearch |
| 1236 | Seçim alanı | Yönetici seç | id=employeeManagerSelect; seçenekler=(değer yok): Yönetici yok |
| 1237 | Girdi alanı | fullName | name=fullName |
| 1238 | Girdi alanı | email | type=email; name=email |
| 1239 | Seçim alanı | roleKey | name=roleKey; seçenekler=asset_operator: Envanter operatörü, asset_manager: Envanter yöneticisi, auditor: Denetçi, viewer: Salt okunur, finance: Muhasebe (salt okunur), hr_operator: İK operatörü, admin: Yönetici |
| 1240 | Girdi alanı | userId | type=hidden; name=userId; başlangıçta gizli |
| 1241 | Seçim alanı | mode | id=memberLocationAccessMode; name=mode; seçenekler=all: Tüm konumlar, restricted: Yalnız seçilen konumlar |
| 1242 | Seçim alanı | companyUnitMode | id=memberCompanyUnitAccessMode; name=companyUnitMode; seçenekler=all: Tüm şirket birimleri, restricted: Yalnız seçilen şirket birimleri |
| 1243 | Girdi alanı | assetName | name=assetName |
| 1244 | Girdi alanı | assetTag | name=assetTag |
| 1245 | Seçim alanı | categoryId | name=categoryId; seçenekler=(değer yok): Kategori seçin |
| 1246 | Seçim alanı | brandId | name=brandId; seçenekler=(değer yok): Marka seçin |
| 1247 | Seçim alanı | modelId | name=modelId; seçenekler=(değer yok): Model seçin |
| 1248 | Girdi alanı | serialNumber | name=serialNumber |
| 1249 | Girdi alanı | location | name=location |
| 1250 | Seçim alanı | defaultLocationCatalogId | name=defaultLocationCatalogId; seçenekler=(değer yok): Varsayılan konum seçin |
| 1251 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1252 | Girdi alanı | warrantyExpiresAt | type=date; name=warrantyExpiresAt |
| 1253 | Girdi alanı | insuranceProvider | name=insuranceProvider |
| 1254 | Girdi alanı | insurancePolicyNumber | name=insurancePolicyNumber |
| 1255 | Girdi alanı | insuranceExpiresAt | type=date; name=insuranceExpiresAt |
| 1256 | Girdi alanı | purchaseCost | type=number; name=purchaseCost |
| 1257 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY — Türk lirası, USD: USD — ABD doları, EUR: EUR — Euro |
| 1258 | Metin alanı | notes | name=notes |
| 1259 | Girdi alanı | isRequestable | type=checkbox; name=isRequestable |
| 1260 | Girdi alanı | isByod | type=checkbox; name=isByod |
| 1261 | Girdi alanı | email | type=email; name=email |
| 1262 | Girdi alanı | password | type=password; name=password |
| 1263 | Girdi alanı | rememberMe | type=checkbox; name=rememberMe |
| 1264 | Girdi alanı | twoFactorCode | name=twoFactorCode |
| 1265 | Seçim alanı | companyId | id=loginCompanyId; name=companyId; seçenekler=(değer yok): Şirket seçin |
| 1266 | Girdi alanı | widget | type=checkbox; name=widget |
| 1267 | Girdi alanı | widget | type=checkbox; name=widget |
| 1268 | Girdi alanı | widget | type=checkbox; name=widget |
| 1269 | Girdi alanı | widget | type=checkbox; name=widget |
| 1270 | Girdi alanı | widget | type=checkbox; name=widget |
| 1271 | Seçim alanı | category: Kategori, brand: Marka, model: Model, location: Konum, status: Durum | id=dashboardChartDimension; seçenekler=category: Kategori, brand: Marka, model: Model, location: Konum, status: Durum |
| 1272 | Seçim alanı | bar: Yatay çubuk, donut: Halka grafik | id=dashboardChartType; seçenekler=bar: Yatay çubuk, donut: Halka grafik |
| 1273 | Girdi alanı | email | type=email; name=email |
| 1274 | Seçim alanı | Kayıtlı sütun eşlemesi | id=assetImportProfile; seçenekler=(değer yok): Profil seçin |
| 1275 | Girdi alanı | assetImportProfileName | id=assetImportProfileName |
| 1276 | Girdi alanı | importPreviewFile | id=importPreviewFile; type=file |
| 1277 | Girdi alanı | quantity | type=number; name=quantity |
| 1278 | Metin alanı | notes | name=notes |
| 1279 | Metin alanı | accessoryOrderApprovalNote | id=accessoryOrderApprovalNote |
| 1280 | Girdi alanı | file | type=file; name=file |
| 1281 | Girdi alanı | orderNumber | name=orderNumber |
| 1282 | Girdi alanı | supplier | name=supplier |
| 1283 | Girdi alanı | orderDate | type=date; name=orderDate |
| 1284 | Girdi alanı | expectedOn | type=date; name=expectedOn |
| 1285 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY, USD: USD, EUR: EUR |
| 1286 | Girdi alanı | taxRatePercent | type=number; name=taxRatePercent |
| 1287 | Girdi alanı | discountAmount | type=number; name=discountAmount |
| 1288 | Girdi alanı | freightAmount | type=number; name=freightAmount |
| 1289 | Girdi alanı | notes | name=notes |
| 1290 | Girdi alanı | orderNumber | name=orderNumber |
| 1291 | Girdi alanı | supplier | name=supplier |
| 1292 | Girdi alanı | orderDate | type=date; name=orderDate |
| 1293 | Girdi alanı | expectedOn | type=date; name=expectedOn |
| 1294 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY, USD: USD, EUR: EUR |
| 1295 | Girdi alanı | taxRatePercent | type=number; name=taxRatePercent |
| 1296 | Girdi alanı | discountAmount | type=number; name=discountAmount |
| 1297 | Girdi alanı | freightAmount | type=number; name=freightAmount |
| 1298 | Girdi alanı | notes | name=notes |
| 1299 | Metin alanı | stockOrderApprovalNote | id=stockOrderApprovalNote |
| 1300 | Girdi alanı | file | id=stockOrderAttachmentInput; type=file; name=file |
| 1301 | Girdi alanı | receivedOn | type=date; name=receivedOn |
| 1302 | Girdi alanı | notes | name=notes |
| 1303 | Girdi alanı | name | name=name |
| 1304 | Girdi alanı | description | name=description |
| 1305 | Seçim alanı | assetId | name=assetId; seçenekler=(değer yok): Varlık seçin |
| 1306 | Girdi alanı | year | type=number; name=year |
| 1307 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1308 | Girdi alanı | amount | type=number; name=amount |
| 1309 | Girdi alanı | orderNumber | name=orderNumber |
| 1310 | Girdi alanı | supplier | name=supplier |
| 1311 | Girdi alanı | orderDate | type=date; name=orderDate |
| 1312 | Girdi alanı | expectedOn | type=date; name=expectedOn |
| 1313 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1314 | Girdi alanı | taxRatePercent | type=number; name=taxRatePercent |
| 1315 | Girdi alanı | discountAmount | type=number; name=discountAmount |
| 1316 | Girdi alanı | freightAmount | type=number; name=freightAmount |
| 1317 | Girdi alanı | notes | name=notes |
| 1318 | Metin alanı | assetOrderApprovalNote | id=assetOrderApprovalNote |
| 1319 | Girdi alanı | assetOrderAttachmentFiles | id=assetOrderAttachmentFiles; type=file |
| 1320 | Girdi alanı | receivedOn | type=date; name=receivedOn |
| 1321 | Girdi alanı | notes | name=notes |
| 1322 | Girdi alanı | receivedOn | type=date; name=receivedOn |
| 1323 | Girdi alanı | notes | name=notes |
| 1324 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1325 | Girdi alanı | name | name=name |
| 1326 | Girdi alanı | category | name=category |
| 1327 | Girdi alanı | modelNumber | name=modelNumber |
| 1328 | Girdi alanı | itemNumber | name=itemNumber |
| 1329 | Girdi alanı | supplier | name=supplier |
| 1330 | Girdi alanı | purchaseOrderReference | name=purchaseOrderReference |
| 1331 | Girdi alanı | location | name=location |
| 1332 | Girdi alanı | minimumQuantity | type=number; name=minimumQuantity |
| 1333 | Girdi alanı | quantity | type=number; name=quantity |
| 1334 | Girdi alanı | unitCost | type=number; name=unitCost |
| 1335 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1336 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1337 | Girdi alanı | warrantyExpiresAt | type=date; name=warrantyExpiresAt |
| 1338 | Girdi alanı | notes | name=notes |
| 1339 | Girdi alanı | isRequestable | type=checkbox; name=isRequestable |
| 1340 | Girdi alanı | quantity | type=number; name=quantity |
| 1341 | Girdi alanı | notes | name=notes |
| 1342 | Seçim alanı | employeeId | name=employeeId; seçenekler=(değer yok): Çalışan seçin |
| 1343 | Girdi alanı | quantity | type=number; name=quantity |
| 1344 | Girdi alanı | notes | name=notes |
| 1345 | Girdi alanı | licenseKeyInput | id=licenseKeyInput; type=password |
| 1346 | Girdi alanı | file | type=file; name=file |
| 1347 | Girdi alanı | isEulaDocument | type=checkbox; name=isEulaDocument |
| 1348 | Girdi alanı | eulaVersion | name=eulaVersion |
| 1349 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1350 | Seçim alanı | supplierId | name=supplierId; seçenekler=(değer yok): Tedarikçi seçin |
| 1351 | Girdi alanı | orderNumber | name=orderNumber |
| 1352 | Girdi alanı | orderDate | type=date; name=orderDate |
| 1353 | Girdi alanı | seatsPurchased | type=number; name=seatsPurchased |
| 1354 | Girdi alanı | unitCost | type=number; name=unitCost |
| 1355 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1356 | Metin alanı | notes | name=notes |
| 1357 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1358 | Girdi alanı | name | name=name |
| 1359 | Girdi alanı | publisher | name=publisher |
| 1360 | Girdi alanı | productVersion | name=productVersion |
| 1361 | Seçim alanı | licenseType | name=licenseType; seçenekler=Subscription: Abonelik, Perpetual: Süresiz, Device: Cihaz başına, Site: Şirket geneli, Trial: Deneme, Other: Diğer |
| 1362 | Girdi alanı | licenseReference | name=licenseReference |
| 1363 | Girdi alanı | supplier | name=supplier |
| 1364 | Girdi alanı | purchaseOrderReference | name=purchaseOrderReference |
| 1365 | Girdi alanı | seats | type=number; name=seats |
| 1366 | Girdi alanı | unitCost | type=number; name=unitCost |
| 1367 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1368 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1369 | Girdi alanı | expirationDate | type=date; name=expirationDate |
| 1370 | Girdi alanı | autoRenew | type=checkbox; name=autoRenew |
| 1371 | Girdi alanı | requiresEulaAcceptance | type=checkbox; name=requiresEulaAcceptance |
| 1372 | Girdi alanı | notes | name=notes |
| 1373 | Girdi alanı | licenseImportFile | id=licenseImportFile; type=file; başlangıçta gizli |
| 1374 | Seçim alanı | Lisans eşleme profili | id=licenseImportProfile; seçenekler=(değer yok): Profil seçin |
| 1375 | Girdi alanı | licenseImportProfileName | id=licenseImportProfileName |
| 1376 | Girdi alanı | eulaAccepted | id=licenseEulaAccepted; type=checkbox; name=eulaAccepted |
| 1377 | Seçim alanı | targetType | name=targetType; seçenekler=employee: Çalışan, asset: Cihaz / varlık |
| 1378 | Seçim alanı | employeeId | name=employeeId; seçenekler=(değer yok): Çalışan seçin |
| 1379 | Girdi alanı | licenseBulkEmployeeMode | id=licenseBulkEmployeeMode; type=checkbox |
| 1380 | Girdi alanı | licenseAssetSearch | id=licenseAssetSearch; type=search |
| 1381 | Seçim alanı | assetId | name=assetId; seçenekler=(değer yok): Önce arama yapın |
| 1382 | Girdi alanı | notes | name=notes |
| 1383 | Girdi alanı | componentImportFile | id=componentImportFile; type=file; başlangıçta gizli |
| 1384 | Seçim alanı | Bileşen eşleme profili | id=componentImportProfile; seçenekler=(değer yok): Profil seçin |
| 1385 | Girdi alanı | componentImportProfileName | id=componentImportProfileName |
| 1386 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1387 | Girdi alanı | name | name=name |
| 1388 | Girdi alanı | category | name=category |
| 1389 | Girdi alanı | modelNumber | name=modelNumber |
| 1390 | Girdi alanı | itemNumber | name=itemNumber |
| 1391 | Girdi alanı | serialNumber | name=serialNumber |
| 1392 | Girdi alanı | supplier | name=supplier |
| 1393 | Girdi alanı | purchaseOrderReference | name=purchaseOrderReference |
| 1394 | Girdi alanı | location | name=location |
| 1395 | Girdi alanı | minimumQuantity | type=number; name=minimumQuantity |
| 1396 | Girdi alanı | quantity | type=number; name=quantity |
| 1397 | Girdi alanı | unitCost | type=number; name=unitCost |
| 1398 | Seçim alanı | currency | name=currency; seçenekler=(değer yok): TRY, (değer yok): USD, (değer yok): EUR |
| 1399 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1400 | Girdi alanı | warrantyExpiresAt | type=date; name=warrantyExpiresAt |
| 1401 | Girdi alanı | notes | name=notes |
| 1402 | Girdi alanı | quantity | type=number; name=quantity |
| 1403 | Girdi alanı | notes | name=notes |
| 1404 | Seçim alanı | assetId | name=assetId; seçenekler=(değer yok): Varlık seçin |
| 1405 | Girdi alanı | quantity | type=number; name=quantity |
| 1406 | Girdi alanı | notes | name=notes |
| 1407 | Girdi alanı | allocationId | type=hidden; name=allocationId; başlangıçta gizli |
| 1408 | Girdi alanı | quantity | type=number; name=quantity |
| 1409 | Girdi alanı | notes | name=notes |
| 1410 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1411 | Girdi alanı | name | name=name |
| 1412 | Girdi alanı | description | name=description |
| 1413 | Girdi alanı | accessoryKitItemSearch | id=accessoryKitItemSearch; type=search |
| 1414 | Girdi alanı | accessoryId | type=hidden; name=accessoryId; başlangıçta gizli |
| 1415 | Girdi alanı | name | name=name |
| 1416 | Seçim alanı | categoryId | name=categoryId; seçenekler=(değer yok): Kategori seçin (isteğe bağlı) |
| 1417 | Seçim alanı | brandId | name=brandId; seçenekler=(değer yok): Marka seçin (isteğe bağlı) |
| 1418 | Seçim alanı | modelId | name=modelId; seçenekler=(değer yok): Model seçin (isteğe bağlı) |
| 1419 | Girdi alanı | modelPhoto | type=file; name=modelPhoto |
| 1420 | Girdi alanı | modelNumber | name=modelNumber |
| 1421 | Girdi alanı | itemNumber | name=itemNumber |
| 1422 | Girdi alanı | supplier | name=supplier |
| 1423 | Girdi alanı | purchaseOrderReference | name=purchaseOrderReference |
| 1424 | Girdi alanı | location | name=location |
| 1425 | Girdi alanı | minimumQuantity | type=number; name=minimumQuantity |
| 1426 | Girdi alanı | quantity | type=number; name=quantity |
| 1427 | Girdi alanı | unitCost | type=number; name=unitCost |
| 1428 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY, USD: USD, EUR: EUR |
| 1429 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1430 | Girdi alanı | warrantyExpiresAt | type=date; name=warrantyExpiresAt |
| 1431 | Girdi alanı | notes | name=notes |
| 1432 | Girdi alanı | isRequestable | type=checkbox; name=isRequestable |
| 1433 | Girdi alanı | quantity | type=number; name=quantity |
| 1434 | Girdi alanı | notes | name=notes |
| 1435 | Girdi alanı | targetType | type=radio; name=targetType |
| 1436 | Girdi alanı | targetType | type=radio; name=targetType |
| 1437 | Girdi alanı | targetType | type=radio; name=targetType |
| 1438 | Seçim alanı | employeeId | name=employeeId |
| 1439 | Seçim alanı | assetId | name=assetId |
| 1440 | Girdi alanı | location | name=location |
| 1441 | Girdi alanı | quantity | type=number; name=quantity |
| 1442 | Girdi alanı | accessoryKitSets | id=accessoryKitSets; type=number |
| 1443 | Girdi alanı | notes | name=notes |
| 1444 | Seçim alanı | view: Varlık ayrıntısını aç, assign: Uygunsa zimmet formunu aç, return: Zimmetliyse iade onayını göster | id=scanAssetAction; seçenekler=view: Varlık ayrıntısını aç, assign: Uygunsa zimmet formunu aç, return: Zimmetliyse iade onayını göster |
| 1445 | Girdi alanı | code | id=scanAssetCode; name=code |
| 1446 | Girdi alanı | kind | type=hidden; name=kind; başlangıçta gizli |
| 1447 | Girdi alanı | currentName | type=hidden; name=currentName; başlangıçta gizli |
| 1448 | Girdi alanı | locationId | type=hidden; name=locationId; başlangıçta gizli |
| 1449 | Girdi alanı | departmentId | type=hidden; name=departmentId; başlangıçta gizli |
| 1450 | Girdi alanı | newName | name=newName |
| 1451 | Seçim alanı | parentDepartmentId | name=parentDepartmentId; seçenekler=(değer yok): Ana departman |
| 1452 | Seçim alanı | locationCatalogId | name=locationCatalogId; seçenekler=(değer yok): Konum seçilmedi |
| 1453 | Girdi alanı | name | name=name |
| 1454 | Seçim alanı | parentDepartmentId | name=parentDepartmentId; seçenekler=(değer yok): Ana departman |
| 1455 | Seçim alanı | locationCatalogId | name=locationCatalogId; seçenekler=(değer yok): Konum seçilmedi |
| 1456 | Seçim alanı | targetDepartmentId | name=targetDepartmentId |
| 1457 | Seçim alanı | parentLocationId | name=parentLocationId; seçenekler=(değer yok): Üst seviye · kat veya bina |
| 1458 | Girdi alanı | name | name=name |
| 1459 | Girdi alanı | locationId | type=hidden; name=locationId; başlangıçta gizli |
| 1460 | Girdi alanı | locationManagerSearch | id=locationManagerSearch; type=search |
| 1461 | Seçim alanı | employeeId | id=locationManagerEmployee; name=employeeId; seçenekler=(değer yok): Sorumlu atanmamış |
| 1462 | Girdi alanı | departmentId | type=hidden; name=departmentId; başlangıçta gizli |
| 1463 | Girdi alanı | departmentManagerSearch | id=departmentManagerSearch; type=search |
| 1464 | Seçim alanı | employeeId | id=departmentManagerEmployee; name=employeeId; seçenekler=(değer yok): Yetkili atanmamış |
| 1465 | Girdi alanı | name | name=name |
| 1466 | Girdi alanı | modelNumber | name=modelNumber |
| 1467 | Metin alanı | note | name=note |
| 1468 | Metin alanı | note | name=note |
| 1469 | Metin alanı | note | name=note |
| 1470 | Girdi alanı | name | name=name |
| 1471 | Girdi alanı | code | name=code |
| 1472 | Girdi alanı | id | type=hidden; name=id; başlangıçta gizli |
| 1473 | Girdi alanı | name | name=name |
| 1474 | Girdi alanı | contactName | name=contactName |
| 1475 | Girdi alanı | email | type=email; name=email |
| 1476 | Girdi alanı | phone | name=phone |
| 1477 | Girdi alanı | website | type=url; name=website |
| 1478 | Metin alanı | notes | name=notes |
| 1479 | Girdi alanı | logo | type=file; name=logo |
| 1480 | Girdi alanı | Tedarikçilerde ara | id=supplierCatalogSearch |
| 1481 | Girdi alanı | showArchivedSuppliers | id=showArchivedSuppliers; type=checkbox |
| 1482 | Seçim alanı | companySwitchTarget | id=companySwitchTarget |
| 1483 | Girdi alanı | accountPhotoInput | id=accountPhotoInput; type=file; başlangıçta gizli |
| 1484 | Girdi alanı | ownCurrentPassword | id=ownCurrentPassword; type=password |
| 1485 | Girdi alanı | ownNewPassword | id=ownNewPassword; type=password |
| 1486 | Girdi alanı | ownConfirmPassword | id=ownConfirmPassword; type=password |
| 1487 | Girdi alanı | ownPasswordTwoFactorCode | id=ownPasswordTwoFactorCode |
| 1488 | Girdi alanı | twoFactorCurrentPassword | id=twoFactorCurrentPassword; type=password |
| 1489 | Girdi alanı | twoFactorCode | id=twoFactorCode |
| 1490 | Girdi alanı | itemType | type=hidden; name=itemType; başlangıçta gizli |
| 1491 | Girdi alanı | itemId | type=hidden; name=itemId; başlangıçta gizli |
| 1492 | Girdi alanı | requestEmployeeSearch | id=requestEmployeeSearch |
| 1493 | Seçim alanı | employeeId | id=requestEmployee; name=employeeId; seçenekler=(değer yok): Çalışan seçin |
| 1494 | Girdi alanı | quantity | type=number; name=quantity |
| 1495 | Metin alanı | note | name=note |
| 1496 | Girdi alanı | file | id=modelAttachmentFile; type=file; name=file |
| 1497 | Girdi alanı | returnedOn | type=date; name=returnedOn |
| 1498 | Girdi alanı | reason | name=reason |
| 1499 | Girdi alanı | notes | name=notes |
| 1500 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1501 | Girdi alanı | name | name=name |
| 1502 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1503 | Girdi alanı | serialNumber | name=serialNumber |
| 1504 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1505 | Seçim alanı | categoryId | name=categoryId |
| 1506 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1507 | Seçim alanı | brandId | name=brandId |
| 1508 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1509 | Seçim alanı | modelId | name=modelId |
| 1510 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1511 | Seçim alanı | defaultLocationCatalogId | name=defaultLocationCatalogId |
| 1512 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1513 | Girdi alanı | purchaseDate | type=date; name=purchaseDate |
| 1514 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1515 | Girdi alanı | warrantyExpiresAt | type=date; name=warrantyExpiresAt |
| 1516 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1517 | Girdi alanı | insuranceProvider | name=insuranceProvider |
| 1518 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1519 | Girdi alanı | insurancePolicyNumber | name=insurancePolicyNumber |
| 1520 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1521 | Girdi alanı | insuranceExpiresAt | type=date; name=insuranceExpiresAt |
| 1522 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1523 | Girdi alanı | purchaseCost | type=number; name=purchaseCost |
| 1524 | Seçim alanı | currency | name=currency; seçenekler=TRY: TRY — Türk lirası, USD: USD — ABD doları, EUR: EUR — Euro |
| 1525 | Seçim alanı | isRequestable | name=isRequestable; seçenekler=(değer yok): Değişiklik yok, true: Evet, false: Hayır |
| 1526 | Seçim alanı | isByod | name=isByod; seçenekler=(değer yok): Değişiklik yok, true: Evet, false: Hayır |
| 1527 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1528 | Metin alanı | notes | name=notes |
| 1529 | Metin alanı | note | name=note |
| 1530 | Seçim alanı | assetId | name=assetId |
| 1531 | Girdi alanı | notes | name=notes |
| 1532 | Girdi alanı | notes | name=notes |
| 1533 | Girdi alanı | ${escapeHtml(assignment.targetName \|\| 'Açık zimmet')} iadesini seç | id=${assignment.id}; type=checkbox |
| 1534 | Girdi alanı | ${escapeHtml(assignment.targetName \|\| 'Açık zimmet')} için toplu iade adedi | type=number |
| 1535 | Girdi alanı | notes | name=notes |
| 1536 | Girdi alanı | ${escapeHtml(asset.assetTag)} varlığını iade et | type=checkbox |
| 1537 | Girdi alanı | ${escapeHtml(label \|\| 'Sipariş kalemi')} iade adedi | type=number |
| 1538 | Seçim alanı | (değer yok): Kalem seçin, ${item.itemType}\|${item.id}: ${item.typeLabel} · ${escapeHtml(item.name)} · ${escapeHtml(item.itemNumber \|\| item.modelNumber \|\| 'Stok kodu yok')} · stok ${fmt.format(item.totalQuantity)} | seçenekler=(değer yok): Kalem seçin, ${item.itemType}\|${item.id}: ${item.typeLabel} · ${escapeHtml(item.name)} · ${escapeHtml(item.itemNumber \|\| item.modelNumber \|\| 'Stok kodu yok')} · stok ${fmt.format(item.totalQuantity)} |
| 1539 | Girdi alanı | (etiketsiz) | type=number |
| 1540 | Girdi alanı | (etiketsiz) | type=number |
| 1541 | Girdi alanı | (etiketsiz) | type=checkbox |
| 1542 | Girdi alanı | ${escapeHtml(quantityLabel)} | type=number |
| 1543 | Girdi alanı | ${escapeHtml(line.itemName)} teslim adedi | type=number |
| 1544 | Girdi alanı | (etiketsiz) |  |
| 1545 | Seçim alanı | (etiketsiz) |  |
| 1546 | Seçim alanı | (etiketsiz) |  |
| 1547 | Seçim alanı | (etiketsiz) |  |
| 1548 | Seçim alanı | (etiketsiz) |  |
| 1549 | Girdi alanı | (etiketsiz) | type=number |
| 1550 | Girdi alanı | (etiketsiz) | type=number |
| 1551 | Girdi alanı | (etiketsiz) | type=date |
| 1552 | Girdi alanı | ${escapeHtml(line.name)} teslim adedi | type=number |
| 1553 | Seçim alanı | (değer yok): Aksesuar seçin, ${item.id}: ${escapeHtml(item.name)} · ${escapeHtml(item.itemNumber \|\| item.modelNumber \|\| 'Stok kodu yok')} · stok ${fmt.format(item.totalQuantity)} | seçenekler=(değer yok): Aksesuar seçin, ${item.id}: ${escapeHtml(item.name)} · ${escapeHtml(item.itemNumber \|\| item.modelNumber \|\| 'Stok kodu yok')} · stok ${fmt.format(item.totalQuantity)} |
| 1554 | Girdi alanı | (etiketsiz) | type=number |
| 1555 | Girdi alanı | (etiketsiz) | type=number |
| 1556 | Girdi alanı | ${escapeHtml(line.accessoryName)} teslim adedi | type=number |
| 1557 | Girdi alanı | ${escapeHtml(item.name)} model numarası |  |
| 1558 | Girdi alanı | (etiketsiz) | type=file |
| 1559 | Girdi alanı | (etiketsiz) | type=number |
| 1560 | Girdi alanı | (etiketsiz) | type=number |
| 1561 | Girdi alanı | (etiketsiz) | type=number |
| 1562 | Seçim alanı | amount: ${translateUiText('Tutar')}, percent: ${translateUiText('Yüzde')} | type=${item.id}; seçenekler=amount: ${translateUiText('Tutar')}, percent: ${translateUiText('Yüzde')} |
| 1563 | Seçim alanı | linear: ${translateUiText('Doğrusal')}, half_1: ${translateUiText('Yarım yıl · ilk dönem')}, half_2: ${translateUiText('Yarım yıl · satın alma dönemine göre')} | seçenekler=linear: ${translateUiText('Doğrusal')}, half_1: ${translateUiText('Yarım yıl · ilk dönem')}, half_2: ${translateUiText('Yarım yıl · satın alma dönemine göre')} |
| 1564 | Girdi alanı | (etiketsiz) | type=url |
| 1565 | Girdi alanı | (etiketsiz) | type=url |
| 1566 | Girdi alanı | (etiketsiz) | type=url |
| 1567 | Girdi alanı | (etiketsiz) | type=tel |
| 1568 | Girdi alanı | (etiketsiz) | type=email |
| 1569 | Girdi alanı | (etiketsiz) | type=file |
| 1570 | Girdi alanı | ${escapeHtml(item.name)} ${translateUiText('Ömür sonu (EOL)')} | type=date |
| 1571 | Girdi alanı | ${escapeHtml(item.name)} ${translateUiText('Kayıt adını düzenle')} | name=${item.id} |
| 1572 | Seçim alanı | (değer yok): Özel alan seçin | id=assetCustomFieldFilter; seçenekler=(değer yok): Özel alan seçin |
| 1573 | Seçim alanı | equals: Değeri eşit, contains: Metin içinde, empty: Değer girilmemiş | id=assetCustomFieldMode; seçenekler=equals: Değeri eşit, contains: Metin içinde, empty: Değer girilmemiş |
| 1574 | Girdi alanı | assetCustomFieldValue | id=assetCustomFieldValue |
| 1575 | Girdi alanı | key | name=key |
| 1576 | Girdi alanı | name | name=name |
| 1577 | Girdi alanı | groupName | name=groupName |
| 1578 | Seçim alanı | categoryId | name=categoryId; seçenekler=(değer yok): Tüm kategoriler |
| 1579 | Seçim alanı | modelId | name=modelId; seçenekler=(değer yok): Tüm modeller |
| 1580 | Seçim alanı | fieldType | name=fieldType; seçenekler=text: Metin, number: Sayı, date: Tarih, checkbox: Onay kutusu, select: Seçim listesi, sensitive: Hassas (şifreli) |
| 1581 | Metin alanı | options | name=options |
| 1582 | Girdi alanı | isRequired | type=checkbox; name=isRequired |
| 1583 | Girdi alanı | sortOrder | type=number; name=sortOrder |
| 1584 | Girdi alanı | name | name=name |
| 1585 | Girdi alanı | groupName | name=groupName |
| 1586 | Seçim alanı | categoryId | name=categoryId; seçenekler=(değer yok): Tüm kategoriler |
| 1587 | Seçim alanı | modelId | name=modelId; seçenekler=(değer yok): Tüm modeller |
| 1588 | Seçim alanı | Çalışan eşleme profili | id=employeeImportProfile; seçenekler=(değer yok): Profil seçin |
| 1589 | Girdi alanı | employeeImportProfileName | id=employeeImportProfileName |
| 1590 | Metin alanı | note | name=note |
| 1591 | Form | maintenanceAttachmentForm | id=maintenanceAttachmentForm; başlangıçta gizli |
| 1592 | Form | companyUnitForm | id=companyUnitForm |
| 1593 | Form | inventoryRequestActionForm | id=inventoryRequestActionForm |
| 1594 | Form | companyNotificationPreferencesForm | id=companyNotificationPreferencesForm |
| 1595 | Form | labelSettingsForm | id=labelSettingsForm |
| 1596 | Form | assetStatusLabelsForm | id=assetStatusLabelsForm |
| 1597 | Form | assetStatusConfigForm | id=assetStatusConfigForm |
| 1598 | Form | catalogCreateForm | id=catalogCreateForm |
| 1599 | Form | companyProfileForm | id=companyProfileForm |
| 1600 | Form | companyLogoForm | id=companyLogoForm |
| 1601 | Form | reservationForm | id=reservationForm |
| 1602 | Form | reservationActionForm | id=reservationActionForm |
| 1603 | Form | taskForm | id=taskForm |
| 1604 | Form | supportForm | id=supportForm |
| 1605 | Form | supportReplyForm | id=supportReplyForm |
| 1606 | Form | companySecurityPolicyForm | id=companySecurityPolicyForm |
| 1607 | Form | integrationTokenForm | id=integrationTokenForm |
| 1608 | Form | companyRoleForm | id=companyRoleForm |
| 1609 | Form | licenseRevokeForm | id=licenseRevokeForm |
| 1610 | Form | licenseImportProfileDeleteForm | id=licenseImportProfileDeleteForm |
| 1611 | Form | contractForm | id=contractForm |
| 1612 | Form | contractActionForm | id=contractActionForm |
| 1613 | Form | assetNoteForm | id=assetNoteForm; başlangıçta gizli |
| 1614 | Form | assetAttachmentForm | id=assetAttachmentForm; başlangıçta gizli |
| 1615 | Form | assetAuditForm | id=assetAuditForm |
| 1616 | Form | assetAuditScheduleForm | id=assetAuditScheduleForm |
| 1617 | Form | maintenanceStartForm | id=maintenanceStartForm |
| 1618 | Form | maintenanceCompleteForm | id=maintenanceCompleteForm |
| 1619 | Form | assetTransferForm | id=assetTransferForm |
| 1620 | Form | editAssetForm | id=editAssetForm |
| 1621 | Form | assignForm | id=assignForm |
| 1622 | Form | newEmployeeForm | id=newEmployeeForm |
| 1623 | Form | editEmployeeForm | id=editEmployeeForm |
| 1624 | Form | employeeActionForm | id=employeeActionForm |
| 1625 | Form | textEntryForm | id=textEntryForm |
| 1626 | Form | inviteMemberForm | id=inviteMemberForm |
| 1627 | Form | memberLocationAccessForm | id=memberLocationAccessForm |
| 1628 | Form | newAssetForm | id=newAssetForm |
| 1629 | Form | loginForm | id=loginForm |
| 1630 | Form | dashboardCustomizeForm | id=dashboardCustomizeForm |
| 1631 | Form | forgotPasswordForm | id=forgotPasswordForm |
| 1632 | Form | importPreviewForm | id=importPreviewForm |
| 1633 | Form | accessoryCheckinForm | id=accessoryCheckinForm |
| 1634 | Form | accessoryOrderAttachmentForm | id=accessoryOrderAttachmentForm |
| 1635 | Form | accessoryOrderCreateForm | id=accessoryOrderCreateForm |
| 1636 | Form | stockOrderCreateForm | id=stockOrderCreateForm |
| 1637 | Form | stockOrderAttachmentForm | id=stockOrderAttachmentForm; başlangıçta gizli |
| 1638 | Form | stockOrderReceiveForm | id=stockOrderReceiveForm |
| 1639 | Form | stockOrderKitTemplateForm | id=stockOrderKitTemplateForm |
| 1640 | Form | assetOrderBudgetForm | id=assetOrderBudgetForm |
| 1641 | Form | assetOrderCreateForm | id=assetOrderCreateForm |
| 1642 | Form | assetOrderReceiveForm | id=assetOrderReceiveForm |
| 1643 | Form | accessoryOrderReceiveForm | id=accessoryOrderReceiveForm |
| 1644 | Form | consumableForm | id=consumableForm |
| 1645 | Form | consumableStockForm | id=consumableStockForm |
| 1646 | Form | consumableCheckoutForm | id=consumableCheckoutForm |
| 1647 | Form | licenseAttachmentForm | id=licenseAttachmentForm |
| 1648 | Form | licensePurchaseOrderForm | id=licensePurchaseOrderForm |
| 1649 | Form | licenseForm | id=licenseForm |
| 1650 | Form | licenseActionForm | id=licenseActionForm |
| 1651 | Form | licenseAssignForm | id=licenseAssignForm |
| 1652 | Form | componentActionForm | id=componentActionForm |
| 1653 | Form | componentForm | id=componentForm |
| 1654 | Form | componentStockForm | id=componentStockForm |
| 1655 | Form | componentAllocateForm | id=componentAllocateForm |
| 1656 | Form | componentRemoveForm | id=componentRemoveForm |
| 1657 | Form | accessoryKitForm | id=accessoryKitForm |
| 1658 | Form | accessoryKitActionForm | id=accessoryKitActionForm |
| 1659 | Form | accessoryForm | id=accessoryForm |
| 1660 | Form | accessoryStockForm | id=accessoryStockForm |
| 1661 | Form | accessoryCheckoutForm | id=accessoryCheckoutForm |
| 1662 | Form | scanAssetForm | id=scanAssetForm |
| 1663 | Form | directoryRenameForm | id=directoryRenameForm |
| 1664 | Form | createDepartmentForm | id=createDepartmentForm |
| 1665 | Form | departmentMoveForm | id=departmentMoveForm |
| 1666 | Form | createLocationForm | id=createLocationForm |
| 1667 | Form | locationManagerForm | id=locationManagerForm |
| 1668 | Form | departmentManagerForm | id=departmentManagerForm |
| 1669 | Form | quickLookupForm | id=quickLookupForm |
| 1670 | Form | returnAssetForm | id=returnAssetForm |
| 1671 | Form | assetTrashConfirmForm | id=assetTrashConfirmForm |
| 1672 | Form | assetStatusForm | id=assetStatusForm |
| 1673 | Form | assetAttachmentRemoveForm | id=assetAttachmentRemoveForm |
| 1674 | Form | savedAssetSearchForm | id=savedAssetSearchForm |
| 1675 | Form | companyMfaEnrollmentForm | id=companyMfaEnrollmentForm |
| 1676 | Form | supplierCatalogForm | id=supplierCatalogForm |
| 1677 | Form | inventoryRequestForm | id=inventoryRequestForm |
| 1678 | Form | modelAttachmentForm | id=modelAttachmentForm; başlangıçta gizli |
| 1679 | Form | purchaseOrderReturnForm | id=purchaseOrderReturnForm |
| 1680 | Form | bulkEditAssetForm | id=bulkEditAssetForm |
| 1681 | Form | (etiketsiz) |  |
| 1682 | Form | (etiketsiz) |  |
| 1683 | Form | (etiketsiz) |  |
| 1684 | Form | assetCustomFieldForm | id=assetCustomFieldForm |
| 1685 | Form | editAssetCustomFieldForm | id=editAssetCustomFieldForm |
| 1686 | Form | bulkAssetActionForm | id=bulkAssetActionForm |
| 1687 | Modal | Kapat | id=employeeHandoverDialog; type=button; başlangıçta gizli |
| 1688 | Modal | Kapat | id=maintenanceAttachmentDialog; type=button; name=file; başlangıçta gizli |
| 1689 | Modal | Kapat | id=companyUnitDialog; type=button; name=id; başlangıçta gizli |
| 1690 | Modal | Kapat | id=inventoryRequestActionDialog; type=button; name=note |
| 1691 | Modal | Kapat | id=reservationActionDialog; type=button; name=note |
| 1692 | Modal | Kapat | id=integrationSecretDialog; type=button |
| 1693 | Modal | Kapat | id=companyRoleDialog; type=button; name=roleKey; başlangıçta gizli |
| 1694 | Modal | Kapat | id=deactivateCompanyRoleDialog; type=button |
| 1695 | Modal | Kapat | id=licenseRevokeDialog; type=button |
| 1696 | Modal | Kapat | id=licenseImportProfileDeleteDialog; type=button |
| 1697 | Modal | Kapat | id=commandPaletteDialog; type=button |
| 1698 | Modal | Kapat | id=keyboardShortcutsDialog; type=button |
| 1699 | Modal | Kapat | id=contractFormDialog; type=button; name=id; başlangıçta gizli |
| 1700 | Modal | Kapat | id=contractDetailDialog; type=button; başlangıçta gizli |
| 1701 | Modal | Kapat | id=contractActionDialog; type=button |
| 1702 | Modal | Kapat | id=appearanceDialog; type=button |
| 1703 | Modal | Kapat | id=assetDialog; type=button; name=note; başlangıçta gizli |
| 1704 | Modal | Kapat | id=assetAuditDialog; type=button; name=auditDate; başlangıçta gizli |
| 1705 | Modal | Kapat | id=assetAuditScheduleDialog; type=button; name=nextAuditOn |
| 1706 | Modal | Kapat | id=maintenanceStartDialog; type=button; name=maintenanceType; başlangıçta gizli |
| 1707 | Modal | Kapat | id=maintenanceCompleteDialog; type=button; name=completedOn |
| 1708 | Modal | Kapat | id=assetTransferDialog; type=button; name=targetLocation |
| 1709 | Modal | Kapat | id=editAssetDialog; type=button; name=expectedUpdatedAt; başlangıçta gizli |
| 1710 | Modal | Kapat | id=assignDialog; type=button; name=employeeId |
| 1711 | Modal | Kapat | id=newEmployeeDialog; type=button; name=displayName |
| 1712 | Modal | Kapat | id=employeeImportDialog; type=button; başlangıçta gizli |
| 1713 | Modal | Kapat | id=consumableImportDialog; type=button; başlangıçta gizli |
| 1714 | Modal | Kapat | id=accessoryImportDialog; type=button; başlangıçta gizli |
| 1715 | Modal | Kapat | id=editEmployeeDialog; type=button; name=displayName |
| 1716 | Modal | Kapat | id=employeeActionDialog; type=button |
| 1717 | Modal | Kapat | id=textEntryDialog; type=button; name=value |
| 1718 | Modal | Kapat | id=employeeDialog; type=button; başlangıçta gizli |
| 1719 | Modal | Kapat | id=inviteMemberDialog; type=button; name=fullName |
| 1720 | Modal | Kapat | id=memberLocationAccessDialog; type=button; name=userId; başlangıçta gizli |
| 1721 | Modal | Kapat | id=newAssetDialog; type=button; name=assetName |
| 1722 | Modal | Kapat | id=dashboardCustomizeDialog; type=button; name=widget |
| 1723 | Modal | Kapat | id=forgotPasswordDialog; type=button; name=email |
| 1724 | Modal | Kapat | id=importPreviewDialog; type=button; başlangıçta gizli |
| 1725 | Modal | Kapat | id=accessoryDialog; type=button |
| 1726 | Modal | Kapat | id=accessoryCheckinDialog; type=button; name=quantity; başlangıçta gizli |
| 1727 | Modal | Kapat | id=accessoryOrderDetailDialog; type=button; name=file; başlangıçta gizli |
| 1728 | Modal | Kapat | id=accessoryOrderCreateDialog; type=button; name=orderNumber |
| 1729 | Modal | Kapat | id=stockOrderCreateDialog; type=button; name=orderNumber |
| 1730 | Modal | Kapat | id=stockOrderDetailDialog; type=button; name=file; başlangıçta gizli |
| 1731 | Modal | Kapat | id=stockOrderReceiveDialog; type=button; name=receivedOn |
| 1732 | Modal | Kapat | id=stockOrderKitTemplateDialog; type=button; name=name |
| 1733 | Modal | Kapat | id=assetOrderBudgetDialog; type=button; name=year |
| 1734 | Modal | Kapat | id=assetOrderCreateDialog; type=button; name=orderNumber |
| 1735 | Modal | Kapat | id=assetOrderDetailDialog; type=button; başlangıçta gizli |
| 1736 | Modal | Kapat | id=assetOrderReceiveDialog; type=button; name=receivedOn |
| 1737 | Modal | Kapat | id=accessoryOrderReceiveDialog; type=button; name=receivedOn |
| 1738 | Modal | Kapat | id=consumableDialog; type=button |
| 1739 | Modal | Kapat | id=consumableFormDialog; type=button; name=id; başlangıçta gizli |
| 1740 | Modal | Kapat | id=consumableStockDialog; type=button; name=quantity |
| 1741 | Modal | Kapat | id=consumableCheckoutDialog; type=button; name=employeeId |
| 1742 | Modal | Kapat | id=licenseDialog; type=button; name=file; başlangıçta gizli |
| 1743 | Modal | Kapat | id=licensePurchaseOrderFormDialog; type=button; name=id; başlangıçta gizli |
| 1744 | Modal | Kapat | id=licenseFormDialog; type=button; name=id; başlangıçta gizli |
| 1745 | Modal | Kapat | id=licenseImportDialog; type=button; başlangıçta gizli |
| 1746 | Modal | Kapat | id=licenseActionDialog; type=button; başlangıçta gizli |
| 1747 | Modal | Kapat | id=licenseAssignDialog; type=button; name=eulaAccepted; başlangıçta gizli |
| 1748 | Modal | Kapat | id=componentActionDialog; type=button |
| 1749 | Modal | Kapat | id=componentDialog; type=button |
| 1750 | Modal | Kapat | id=componentImportDialog; type=button; başlangıçta gizli |
| 1751 | Modal | Kapat | id=componentFormDialog; type=button; name=id; başlangıçta gizli |
| 1752 | Modal | quantity | id=componentStockDialog; type=button; name=quantity |
| 1753 | Modal | assetId | id=componentAllocateDialog; type=button; name=assetId |
| 1754 | Modal | Kapat | id=componentRemoveDialog; type=button; name=allocationId; başlangıçta gizli |
| 1755 | Modal | Kapat | id=accessoryKitFormDialog; type=button; name=id; başlangıçta gizli |
| 1756 | Modal | Kapat | id=accessoryKitHistoryDialog; type=button |
| 1757 | Modal | Kapat | id=accessoryKitActionDialog; type=button |
| 1758 | Modal | Kapat | id=accessoryFormDialog; type=button; name=accessoryId; başlangıçta gizli |
| 1759 | Modal | Kapat | id=accessoryCategoryManagerDialog; type=button |
| 1760 | Modal | Kapat | id=accessoryStockDialog; type=button; name=quantity |
| 1761 | Modal | Kapat | id=accessoryCheckoutDialog; type=button; name=targetType; başlangıçta gizli |
| 1762 | Modal | Kapat | id=scanAssetDialog; type=button; name=code; başlangıçta gizli |
| 1763 | Modal | Kapat | id=directoryRenameDialog; type=button; name=kind; başlangıçta gizli |
| 1764 | Modal | Kapat | id=createDepartmentDialog; type=button; name=name |
| 1765 | Modal | Kapat | id=departmentMoveDialog; type=button; name=targetDepartmentId |
| 1766 | Modal | Kapat | id=createLocationDialog; type=button; name=parentLocationId |
| 1767 | Modal | Kapat | id=locationManagerDialog; type=button; name=locationId; başlangıçta gizli |
| 1768 | Modal | Kapat | id=departmentManagerDialog; type=button; name=departmentId; başlangıçta gizli |
| 1769 | Modal | Kapat | id=quickLookupDialog; type=button; name=name; başlangıçta gizli |
| 1770 | Modal | Kapat | id=returnAssetDialog; type=button; name=note; başlangıçta gizli |
| 1771 | Modal | Kapat | id=assetTrashConfirmDialog; type=button; name=note |
| 1772 | Modal | Kapat | id=assetStatusDialog; type=button; name=note; başlangıçta gizli |
| 1773 | Modal | Kapat | id=assetAttachmentRemoveDialog; type=button; başlangıçta gizli |
| 1774 | Modal | Kapat | id=savedAssetSearchDialog; type=button; name=name; başlangıçta gizli |
| 1775 | Modal | Kapat | id=companyMfaEnrollmentDialog; type=button; name=code; başlangıçta gizli |
| 1776 | Modal | Kapat | id=supplierCatalogDialog; type=button; name=id; başlangıçta gizli |
| 1777 | Modal | Kapat | id=companySwitchDialog; type=button |
| 1778 | Modal | Kapat | id=accountPhotoDialog; type=button; hedef=/api/session/data-export; başlangıçta gizli |
| 1779 | Modal | Kapat | id=accountTwoFactorDialog; type=button; başlangıçta gizli |
| 1780 | Modal | Kapat | id=inventoryRequestDialog; type=button; name=itemType; başlangıçta gizli |
| 1781 | Modal | Kapat | id=assetTrashDialog; type=button |
| 1782 | Modal | Görsel önizleme | id=imagePreviewDialog; type=button |
| 1783 | Modal | Kapat | id=modelDetailsDialog; type=button |
| 1784 | Modal | Kapat | id=modelAttachmentsDialog; type=button; name=file; başlangıçta gizli |
| 1785 | Modal | Varlık etiketi yazdırma önizlemesi | id=assetLabelPreviewDialog; type=button |
| 1786 | Modal | Kapat | id=purchaseOrderReturnDialog; type=button; name=returnedOn |
| 1787 | Modal | Kapat | id=editAssetCustomFieldDialog; type=button; name=name |
| 1788 | Tablo | Bu sayfadaki seçilebilir varlıkları seç | id=selectAllAssetsHeader; type=checkbox |
| 1789 | Tablo | Şirket çalışan kayıtları | id=employeeRows |
| 1790 | Tablo | directoryHead | id=directoryHead |
| 1791 | Tablo | Şirket birimleri | id=companyUnitRows |
| 1792 | Tablo | Şirket aksesuar stokları | id=accessoryRows |
| 1793 | Tablo | Aksesuar satın alma siparişleri | id=accessoryOrderRows |
| 1794 | Tablo | Şirketin aksesuar hazır kitleri | id=accessoryKitRows |
| 1795 | Tablo | Sarf malzemeleri ve kullanılabilir stok miktarları | id=consumableRows |
| 1796 | Tablo | Kalem / Tür / Konum / Kalan / | id=requestCatalogRows |
| 1797 | Tablo | Çalışan / talep eden / Kalem / Adet / Durum / İşlem | id=requestRows |
| 1798 | Tablo | Lisans / Yayıncı / sürüm / Tür / Kullanılan koltuk / Bitiş tarihi / Yenileme / İşlem | id=licenseRows |
| 1799 | Tablo | Lisans / Çalışan / varlık / Personel no / Atanma tarihi / Bitiş tarihi / Durum / İşlem | id=licenseAssignmentRows |
| 1800 | Tablo | Seç / Bileşen / Kategori / model / Seri / ürün no / Toplam / Atanmış / Kullanılabilir / Minimum / Garanti bitişi / İşlem | id=componentRows |
| 1801 | Tablo | Şirket stok hareketleri | id=inventoryMovementRows |
| 1802 | Tablo | Kullanıcı / E-posta / Çalışan profili / Rol / Üyelik tarihi / İşlem | id=membersRows |
| 1803 | Tablo | Ad / İlişki / Kullanım / İşlemler | id=catalogRelationHead |
| 1804 | Tablo | Şirketin aktif varlık zimmetleri | id=assignmentRows |
| 1805 | Tablo | Beklenen ve geciken varlık iadeleri | id=returnsRows |
| 1806 | Tablo | Tarih / Varlık / İşlem / Çalışan / İşlemi yapan / Açıklama | id=activityTableRows |
| 1807 | Tablo | Varlık / Çalışan / Kullanım dönemi / Durum / Talep eden / Not / karar / İşlemler | id=reservationRows |
| 1808 | Tablo | Plan tarihi / Etiket / Varlık / Konum / Zimmetli çalışan / Durum / | id=auditScheduleDateHeading |
| 1809 | Tablo | Tarih / İşlemi yapan / Kayıt türü / İşlem / Ayrıntı | id=auditRows |
| 1810 | Tablo | Şirket sözleşmeleri | id=contractRows |
| 1811 | Tablo | Şirket görevleri | id=taskRows |
| 1812 | Tablo | Destek talepleri | id=supportRows |
| 1813 | Tablo | Kullanıcı API anahtarları | id=integrationTokenRows |
| 1814 | Tablo | Şirket bakım kayıtları | id=maintenanceRows |
| 1815 | Tablo | Şirket bakım, denetim, lisans, garanti, iade ve rezervasyon takvimi | id=calendarRows |
| 1816 | Tablo | Durum / Varlık / İşlem | id=reportStatusRows |
| 1817 | Tablo | Para birimi / Kayıt / Toplam alış bedeli | id=reportCostRows |
| 1818 | Tablo | Kategori / Toplam / Atanmış / Kullanılabilir / Bakımda / İşlem | id=reportCategoryRows |
| 1819 | Tablo | Alış tarihi / Etiket / Varlık / Seri no / Kategori / Durum / Bedel / Konum | id=assetPurchaseReportRows |
| 1820 | Tablo | Kategori / Bileşen / Toplam adet / Atanmış adet / Kullanılabilir adet / Düşük stok kaydı | id=componentReportRows |
| 1821 | Tablo | Tarih / Sonuç / Etiket / Varlık / Konum / Denetimi yapan / Açıklama / bulgu | id=auditReportRows |
| 1822 | Tablo | Garanti bitişi / Etiket / Varlık / Durum / Konum / Zimmetli çalışan | id=warrantyReportRows |
| 1823 | Tablo | Tür / Kayıt | id=maintenanceReportTypes |
| 1824 | Tablo | Para birimi / Kayıt / Toplam | id=maintenanceReportCosts |
| 1825 | Tablo | Bitiş tarihi / Lisans / Yayıncı / sürüm / Tür / Koltuk kullanımı / Otomatik yenileme / Tedarikçi | id=licenseReportRows |
| 1826 | Tablo | Şirketin bekleyen satın alma siparişi kalemleri | id=pendingDeliveryRows |
| 1827 | Tablo | Varlıkların tahmini amortisman değerleri | id=depreciationReportRows |
| 1828 | Tablo | Sarf ve bileşen satın alma siparişleri | id=stockOrderRows |
| 1829 | Tablo | Tüm şirket satın alma kayıtları | id=purchaseOverviewRows |
| 1830 | Tablo | Varlık satın alma siparişleri | id=assetOrderRows |
| 1831 | Tablo | roleAccessCaption | id=roleAccessCaption |
| 1832 | Tablo | Aksesuar / Kalan / verilen / Teslim / Not | id=assetAccessoryAssignments |
| 1833 | Tablo | Bileşen / Kalan / verilen / Takılma / Not | id=assetComponentAllocations |
| 1834 | Tablo | Lisans / Tür / Bitiş / Atanma / Not | id=assetLicenseAssignments |
| 1835 | Tablo | İlişki / Etiket / Varlık / Durum | id=assetLinkedRows |
| 1836 | Tablo | Çalışan / Departman / Zimmet | id=employeeDirectReports |
| 1837 | Tablo | Varlık / Durum / Konum | id=employeeAssetRows |
| 1838 | Tablo | Aksesuar / Verilen / İade / Kalan / Tarih | id=employeeAccessoryRows |
| 1839 | Tablo | Lisans / Yayıncı / sürüm / Atandığı tarih / Bitiş | id=employeeLicenseRows |
| 1840 | Tablo | Malzeme / Miktar / Tarih / Not | id=employeeConsumableRows |
| 1841 | Tablo | Varlık / Bakım türü / Servis / Durum / Tarih | id=employeeMaintenanceRows |
| 1842 | Tablo | Sipariş no / Tedarikçi / tarih / Adet / Durum | id=accessoryOrderHistoryRows |
| 1843 | Tablo | Çalışan / Verilen / İade / Kalan / İşlem | id=accessoryAssignments |
| 1844 | Tablo | Aksesuar / Sipariş / Teslim alınan / Kalan / Fatura birim bedeli / Edinim birim bedeli / Toplam | id=accessoryOrderDetailLines |
| 1845 | Tablo | Kalem / Sipariş / Teslim alınan / Kalan / Fatura birim bedeli / Edinim birim bedeli / Toplam | id=stockOrderDetailLines |
| 1846 | Tablo | Cihaz / Kategori / Hedef konum / Sipariş / Teslim alındı / Kalan / Fatura birim bedeli / Edinim birim bedeli | id=assetOrderDetailLines |
| 1847 | Tablo | Etiket / cihaz / Durum / Seri numarası / Alış tarihi | id=assetOrderReceivedAssets |
| 1848 | Tablo | Tarih / İşlem / Miktar / Çalışan / İşlemi yapan / Not | id=consumableMovements |
| 1849 | Tablo | Tedarikçi / Sipariş no. / Tarih / Koltuk / Birim maliyeti / Durum / İşlemler | id=licensePurchaseOrderRows |
| 1850 | Tablo | Çalışan / varlık / Personel no / Atanma / EULA sürümü / EULA teyit zamanı / Teyit eden / Durum / İşlem | id=licenseAssignments |
| 1851 | Tablo | Varlık / Verilen / Çıkarılan / Kalan / Tarih / İşlem | id=componentAllocations |
| 1852 | Tablo | Tarih / Kalem / Teslim edilen / İade edilen / Kalan / Hedef / İşlemi yapan / Not | id=accessoryKitHistoryRows |
| 1853 | Tablo | Kategori / Aksesuar / Durum / İşlemler | id=accessoryCategoryRows |
| 1854 | Tablo | Firma / Yetkili / iletişim / Bağlı kayıt / Durum / İşlemler | id=supplierCatalogRows |
| 1855 | Tablo | Etiket / Varlık / Seri numarası / Taşınma zamanı / İşlem | id=assetTrashRows |
| 1856 | Tablo | Etiket / Varlık / Durum / Seri no / Çalışan / konum | id=modelDetailsAssets |
| 1857 | Tablo | Sipariş / Tedarikçi / Durum / Tarih / Adet / Teslim alınan | id=modelDetailsOrders |
| 1858 | Tablo | Tarih / İşlem / Açıklama | id=modelDetailsHistory |
| 1859 | Tablo | Dosya / Tür / Boyut / Yüklenme / İşlem | id=modelAttachmentRows |
| 1860 | Tablo | (etiketsiz) | başlangıçta gizli |
| 1861 | Tablo | Etiket / Varlık / Seri no / Durum / Konum |  |
| 1862 | Tablo | Aksesuar / Model / Kalan adet / Teslim tarihi |  |
| 1863 | Tablo | Lisans / EULA sürümü / Dosya / ek kimliği / Kabul eden / tarih / EULA SHA-256 / Durum |  |
| 1864 | Tablo | ${escapeHtml(translateUiText('Uygulama'))} / ${escapeHtml(translateUiText('Yayıncı'))} / ${escapeHtml(translateUiText('Sürüm'))} / ${escapeHtml(translateUiText('Mimari'))} / ${escapeHtml(translateUiText('Kurulum tarihi'))} / ${escapeHtml(translateUiText('Son görülme'))} | id=assetSoftwareInventoryRows |
| 1865 | Tablo | Alan / Anahtar / Grup / Kapsam / Tür / Zorunlu / Durum / İşlem | id=assetCustomFieldRows |
| 1866 | Sekme | assetTabOverviewButton | id=assetTabOverviewButton; type=button |
| 1867 | Sekme | assetTabRelationsButton | id=assetTabRelationsButton; type=button |
| 1868 | Sekme | assetTabMaintenanceButton | id=assetTabMaintenanceButton; type=button |
| 1869 | Sekme | assetTabHistoryButton | id=assetTabHistoryButton; type=button |
| 1870 | Sekme | assetTabAttachmentsButton | id=assetTabAttachmentsButton; type=button |
| 1871 | Sayfa rotası | Genel Bakış | hedef=Genel Bakış |
| 1872 | Sayfa rotası | Varlıklar | hedef=Varlıklar |
| 1873 | Sayfa rotası | Tüm satın almalar | hedef=Tüm satın almalar |
| 1874 | Sayfa rotası | Varlık siparişleri | hedef=Varlık siparişleri |
| 1875 | Sayfa rotası | Varlık modelleri | hedef=Varlık modelleri |
| 1876 | Sayfa rotası | Markalar | hedef=Markalar |
| 1877 | Sayfa rotası | Kategoriler | hedef=Kategoriler |
| 1878 | Sayfa rotası | Durum etiketleri | hedef=Durum etiketleri |
| 1879 | Sayfa rotası | QR ve barkodlar | hedef=QR ve barkodlar |
| 1880 | Sayfa rotası | Zimmetler | hedef=Zimmetler |
| 1881 | Sayfa rotası | İadeler | hedef=İadeler |
| 1882 | Sayfa rotası | Transferler | hedef=Transferler |
| 1883 | Sayfa rotası | Rezervasyonlar | hedef=Rezervasyonlar |
| 1884 | Sayfa rotası | Hareket geçmişi | hedef=Hareket geçmişi |
| 1885 | Sayfa rotası | Yazılım lisansları | hedef=Yazılım lisansları |
| 1886 | Sayfa rotası | Lisans atamaları | hedef=Lisans atamaları |
| 1887 | Sayfa rotası | Lisanslarım | hedef=Lisanslarım |
| 1888 | Sayfa rotası | Sözleşmeler | hedef=Sözleşmeler |
| 1889 | Sayfa rotası | Garanti takibi | hedef=Garanti takibi |
| 1890 | Sayfa rotası | Aksesuarlar | hedef=Aksesuarlar |
| 1891 | Sayfa rotası | Siparişler | hedef=Siparişler |
| 1892 | Sayfa rotası | Sarf malzemeleri | hedef=Sarf malzemeleri |
| 1893 | Sayfa rotası | Stok talepleri | hedef=Stok talepleri |
| 1894 | Sayfa rotası | Bileşenler | hedef=Bileşenler |
| 1895 | Sayfa rotası | Hazır kitler | hedef=Hazır kitler |
| 1896 | Sayfa rotası | Stok hareketleri | hedef=Stok hareketleri |
| 1897 | Sayfa rotası | Çalışan kayıtları | hedef=Çalışan kayıtları |
| 1898 | Sayfa rotası | Uygulama kullanıcıları | id=membersNav; hedef=Uygulama kullanıcıları |
| 1899 | Sayfa rotası | Departmanlar | hedef=Departmanlar |
| 1900 | Sayfa rotası | Yetki rolleri | hedef=Yetki rolleri |
| 1901 | Sayfa rotası | Lokasyonlar | hedef=Lokasyonlar |
| 1902 | Sayfa rotası | Binalar ve odalar | hedef=Binalar ve odalar |
| 1903 | Sayfa rotası | Şirket birimleri | hedef=Şirket birimleri |
| 1904 | Sayfa rotası | Bakım kayıtları | hedef=Bakım kayıtları |
| 1905 | Sayfa rotası | Bakım planları | hedef=Bakım planları |
| 1906 | Sayfa rotası | Denetimler | hedef=Denetimler |
| 1907 | Sayfa rotası | Takvim | hedef=Takvim |
| 1908 | Sayfa rotası | Görevler | hedef=Görevler |
| 1909 | Sayfa rotası | Hazır raporlar | hedef=Hazır raporlar |
| 1910 | Sayfa rotası | Özel rapor oluştur | hedef=Özel rapor oluştur |
| 1911 | Sayfa rotası | Dışa aktarımlar | hedef=Dışa aktarımlar |
| 1912 | Sayfa rotası | Tablodan içe aktar | hedef=Tablodan içe aktar |
| 1913 | Sayfa rotası | Verileri dışa aktar | hedef=Verileri dışa aktar |
| 1914 | Sayfa rotası | Yedekleme geçmişi | hedef=Yedekleme geçmişi |
| 1915 | Sayfa rotası | Geri yükle | hedef=Geri yükle |
| 1916 | Sayfa rotası | Firma profili | hedef=Firma profili |
| 1917 | Sayfa rotası | Logo ve tema | hedef=Logo ve tema |
| 1918 | Sayfa rotası | Bildirimler | hedef=Bildirimler |
| 1919 | Sayfa rotası | Entegrasyonlar | hedef=Entegrasyonlar |
| 1920 | Sayfa rotası | Güvenlik | hedef=Güvenlik |
| 1921 | Sayfa rotası | Denetim kayıtları | hedef=Denetim kayıtları |
| 1922 | Sayfa rotası | Destek | hedef=Destek |
| 1923 | Sayfa rotası | Varlıklar | hedef=Varlıklar |
| 1924 | Sayfa rotası | Hareket geçmişi | hedef=Hareket geçmişi |
| 1925 | Sayfa rotası | Takvim | hedef=Takvim |
| 1926 | Sayfa rotası | Hazır raporlar | hedef=Hazır raporlar |
| 1927 | Sayfa rotası | Yazılım lisansları | type=button; hedef=Yazılım lisansları |
| 1928 | Sayfa rotası | Varlıklar | type=button; hedef=Varlıklar |
| 1929 | Sayfa rotası | Varlıklar | type=button; hedef=Varlıklar |
| 1930 | Sayfa rotası | Çalışan kayıtları | type=button; hedef=Çalışan kayıtları |
| 1931 | Sayfa rotası | Aksesuarlar | type=button; hedef=Aksesuarlar |
| 1932 | Sayfa rotası | Sarf malzemeleri | type=button; hedef=Sarf malzemeleri |
| 1933 | Sayfa rotası | Bileşenler | type=button; hedef=Bileşenler |
| 1934 | Sayfa rotası | Yazılım lisansları | type=button; hedef=Yazılım lisansları |
| 1935 | Sayfa rotası | Genel Bakış | type=button; hedef=Genel Bakış |
| 1936 | Menü grubu | inventory | hedef=inventory |
| 1937 | Menü grubu | movement | hedef=movement |
| 1938 | Menü grubu | software | hedef=software |
| 1939 | Menü grubu | supplies | hedef=supplies |
| 1940 | Menü grubu | people | hedef=people |
| 1941 | Menü grubu | places | hedef=places |
| 1942 | Menü grubu | operations | hedef=operations |
| 1943 | Menü grubu | reports | hedef=reports |
| 1944 | Menü grubu | data | hedef=data |
| 1945 | Menü grubu | settings | hedef=settings |

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
