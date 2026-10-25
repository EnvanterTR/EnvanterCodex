# Santa normal owner — lisans, CSV, arşiv/geri yükleme

## Lisans koltuğu — 20.2.5

Normal Yazılım lisansları → Lisans ekle ile `Santa Kabul Yazılımı 20261026`, iki koltuklu kabul lisansı oluşturuldu. Koltuk ata ile mevcut Santa Kabul Çalışanı seçildi. Detayda1/2; salt okunur DB sorgusunda Seats2 / active_assignments1 / remaining1, assignment_employee_scoped=true, slug santa, exit0. LisansID `5040acc1-30bf-4f87-99c1-decf4d92d2ba`. Gerçek ticari lisans anahtarı kullanılmadı.

Kanıt: santa-license-seat-assigned-20261026.png; santa-license-scope-20261026.sql; santa-license-scope-db-20261026.png.

## CSV — 20.2.7

Normal lisans CSV düğmesiyle dosya indirildi: `envantertr-lisanslar-20261025.csv` (dosya tarih eki UTC; işlem TR26Ekim). Kopyası santa-licenses-export-20261026.csv. PowerShell Import-Csv ile başlıklar ve tek veri satırı okundu: yalnız Santa kabul lisansı;2koltuk/1atanmış. Kaynak DB'deki aynı kaydı karşılıyor. Bu dar CSV kabulüdür; Farma'da gerçek karşı kayıtla çapraz izolasyon saldırı testi yerine geçirilmez, tüm rapor türleri için genellenmez.

## Geri alınabilir çöp kutusu ve gerçek Arşiv — 20.2.9

Liste eyleminde “Varlığı arşive taşı” etiketi gerçekte Çöp kutusuna taşı onayını açtı. Bu UI adlandırma tutarsızlığı kaydedildi; çöp kutusu kabulü gerçek Arşiv yerine sayılmadı. Normal çöp kutusu → Geri al ile varlık döndü.

Ardından ayrı olarak varlık detayındaki durum seçicisinden **Arşiv** seçildi, onaylandı. DB ara durum Archived; detayda normal **Arşivden geri al** eylemi açıldı ve bu ayrı formdan geri alındı. Son DB Available.

Tüm aşamalarda aynı assetID8041958a-e66b-4b7d-a2d1-4748235a08c2; Status/UpdatedAt dışındaki varlık alanlarının JSON MD5 tutarlılık özeti `287c711fd1d80e8454a4480b62170a53` eşit. MD5 yalnız bu kabul kaydının alan karşılaştırmasıdır, güvenlik veya yedek checksum iddiası değildir. Tamamlanmış bakım1 korundu; geçmiş6→8(çöp kutusu/geri al)→9(Archived)→10(Available). Her sorgu READONLY/exit0. Kayıt ve geçmiş silinmedi.

Kanıt: santa-archive-preservation-20261026.sql; santa-archive-before-db-20261026.png; santa-trash-record-20261026.png; santa-trash-restored-db-20261026.png; santa-status-archived-20261026.png; santa-status-archived-db-20261026.png; santa-status-restored-20261026.png; santa-status-restored-db-20261026.png.

## Açık işler

Santa20.2 için7/10madde kanıtlı:1,2,3,5,6,7,9. Madde4 zimmet kayıt kısmı geçti ama PDF/imza/e-posta açık;8Editör/davet/yetki ve10logout açık. Normal owner oturumu sonraki kabul için açık bırakıldı. Farma/cross-tenant/ödeme tamam değildir.

Editör kabulünde ürünün mevcut sınırlı varlık-yazma rolü asset_operator(Envanter operatörü); özel adlı editor rolü yok. c.belge hesabının Santa'ya bu rolle eklenmesi somut formda hazırlandı fakat yeni erişim verdiğinden eylem anı kullanıcı onayı istendi, gönderilmedi. Kullanıcı onayı olmadan bu rolün Hedef'teki Editör kabulü yerine geçtiği ileri sürülmez.

TAMAM dediğim3yeni Bölüm20.2 maddesinin3'ü gerçek ürün/indirilen CSV/DB çıktısıyla doğrulanmıştır.
