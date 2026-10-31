# Normal Santa owner kabulü — varlık, zimmet, bakım

25 Ekim2026. Kullanıcı Santa QR kurulumunu tamamladı. Normal `https://santa.envantertr.com/` oturumu Santa dashboard'una geçti; şirket adı ve boş başlangıç envanteri görüldü. Bu yönetici destek oturumu değildir. Kurtarma kodları/QR/anahtar kullanılmadı, rapora veya repo dosyasına alınmadı.

## Gerçek ürün akışları

1. Yeni varlık formundan kategori `Kabul Dizüstü` inline eklendi; varlık adı `Santa Kabul Dizüstü 20261025`, etiket `SANTA-KABUL-20261025-01`, seri `SANTA-SN-20261025-01` ile oluşturuldu ve listede görüldü.
2. Çalışan kayıtları ekranından `Santa Kabul Çalışanı`, personel `SANTA-PER-20261025-01` oluşturuldu. E-posta yalnız kullanıcının kendi Santa test adresi; dış üçüncü kişi daveti yapılmadı.
3. Normal Çalışana zimmetle formuyla bu varlık çalışana atandı. Başarı mesajı ve DB `Assigned` olayı doğrulandı. PDF/imza/e-posta alt adımları henüz tamam değil; 20.2.4 tam kabul sayılmaz.
4. Varlık detayından `Santa kabul kontrolü` bakım planı oluşturuldu; normal İade al formuyla çalışan iadesi tamamlandı; Bakımı başlat ve Bakımı tamamla kullanıldı. Sonuç notu bakım geçmişinde Tamamlandı olarak göründü.

## Salt okunur gerçek DB kanıtı

`santa-asset-scope.sql` → exit0: tek varlık `8041958a-e66b-4b7d-a2d1-4748235a08c2`; şirket Santa `3526d59e-d0bb-4755-a90e-6f3e7eccc341`. Kategori ve atanmış çalışan CompanyId eşitlikleri true; Created/Assigned olayları Santa kapsamında.

`santa-maintenance-scope.sql` → exit0: aynı varlık Available, bakım Completed, CompletedOn2026-10-25, bakım CompanyId eşitliği true; sonuç notu korundu. Altı gerçek olay: Created, Assigned, MaintenanceScheduled, Returned, MaintenanceStarted, MaintenanceCompleted; her biri varlığın CompanyId'sine eşit. Satırlar yalnız açık kabul işaretine daraltıldı; SQL yazma yapılmadı.

## Kanıt ve açık kapsam

Ekranlar: santa-normal-owner-dashboard.png, santa-asset-created.png, santa-asset-assigned.png, santa-asset-scope-db.png, santa-maintenance-completed.png, santa-maintenance-scope-db.png.

20.2.1 giriş, .2 şirket panosu, .3 varlık/listesi/DBCompanyId ve .6 bakım/sonuç/geçmiş kanıtlı. .4 zimmetin yalnız kayıt ve DB alt adımı kanıtlı; PDF/imza/e-posta açık. Lisans/koltuk, CSV, Editör daveti+giriş+yetersiz yetki, arşiv/restore, logout, Farma eşdeğeri, çift yönlü canlı GET/PUT ve ödeme hâlâ açık. Test kayıtları sonraki kabul için canlıda korundu; silinmedi. Açık normal Santa oturumundan devam edilmeli.

TAMAM dediğim 4 Bölüm20.2 maddesinin4'ü gerçek ürün ekranı veya SQL çıktısıyla doğrulanmıştır; diğer maddeler tamam ilan edilmedi.
