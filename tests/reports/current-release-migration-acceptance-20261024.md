# 118 → 120 aday geçişi: özgün alan ve satır koruması

Kapsam: Hedef 16.6 ve P0-B için izole PostgreSQL kabulü. Gerçek üretim yedeği/restore provası veya staging kabulü değildir.

## Kaynak ve başlangıç

- Tarihsel üretim kimlik listesi: 118 migration, son kimlik `20261020174959_AddSubscriptionCoupons`, sıralı kimliklerin MD5 özeti `c8694e4a153c5f81d5cface1e0c9f366`.
- Yeni test bu sayıyı, uç kimliği ve tüm başlangıç listesinin özetini EF migration kümesiyle karşılaştırır.
- İki aday farkı yalnız `20261023190000_AddExistingCompanyCheckoutContext` ve `20261024010000_AddPlatformImpersonationSessions` olmalıdır.
- Mevcut migration ve üretim araçları değişmedi. Yeni test, rastgele isimli izole schema kullanır; search_path içinde public fallback bulunmaz. Yalnız kendi oluşturduğu schema temizlenir.

## Kabul

1. İzole schema tam 118 migration'a kurulur.
2. Sentetik şirket, kullanıcı, yıllık plan, mevcut müşteri oturumu, mevcut checkout siparişi, kategori ve varlık eklenir.
3. Migration history dışında her özgün tablonun sütun listesi dondurulur. Her satırın tüm özgün alanları kararlı sıralamayla SHA-256 projeksiyonuna katılır.
4. EF'nin ürettiği idempotent 118 → 120 SQL'i çalıştırılır. DropTable/DropColumn/Delete bulunmadığı kontrol edilir.
5. İlk uygulama ve aynı SQL'in tekrarından sonra 120 history kaydı, sıfır pending ve aynı özgün veri özeti gerekir.
6. Eski oturum/sipariş yeni alanları null kalmalı; varlığın şirket kimliği korunmalıdır.

Sonuç: **1/1 başarılı, exit 0**, 41.9429 saniye — `current-release-migration-second-20261024.log`. Test projesi 0 hata ile derlendi; önceki dört ASP.NET deprecation uyarısı devam ediyor. SQL ilk uygulama ve tekrarında özgün alan/satır SHA-256 eşitliği, 120 migration ve sıfır pending geçti. Geçici DB/ağ temizlendi.

Tek komut: `deploy/testing/run-current-release-migration-acceptance.ps1`. İlk hedefli çalıştırmalar mevcut güncel C# uygulama image'ını ve bind edilen yeni test kaynağını kullanıp test projesini yeniden derledi.

İlk koşu: sentetik sipariş tutarı 0 seçildiği için mevcut `CK_CompanyCheckoutOrders_Amount` kuralı doğru şekilde reddetti. Fixture yıllık planla uyumlu 2.400 TL tutar/ara toplamla düzeltildi; ürün kuralı veya migration değiştirilmedi. İlk hata logu saklandı.

## Canlı durum ve açık kapılar

`current-release-production-readonly-20261024.log`: yalnız GET istekleri. Apex ve firma HTTP 200/ready; dağıtılmış uygulama 118 migration, kaynak 120. Canlı plan listesi boş ve checkout kapalı. Bu görüntü canlı DB'nin yeni SQL ile güncellendiğini göstermez.

Anahtarlı SSH bağlantısı salt okunur bağlantı denemesinde reddedildi. Bu çalışma üretim migration'ı, yedek, ortam ayarı veya deploy yapmadı. Hedefteki taze SQL/medya/anahtar yedeği, gerçek geri yükleme provası, staging, SMTP/iyzico ve Santa/Farma/Demo3 kabulleri ayrıca tamamlanmalıdır. Eski `rehearse-preserved-data.sh` içindeki 92 → 117 sayıları ve sabit SQL özeti güncel adaya uygulanamaz; araç bu nedenle burada sunucuda çalıştırılmadı.

## Staging için somut SQL dosyası

EF CLI 10.0.0 ile iki açık uç kimliği arasında üretilen idempotent SQL: `current-release-118-to-120-20261024.sql`. Kanonik biçim UTF-8 (BOM yok) ve LF; SHA-256 `cddf35fd830861e61d018fb1885185e921bccfdf04934572391a162e1db77b49`.

Test artık dosya baytlarının checksum eşitliğini, dosyanın güncel EF çıktısıyla tam metin eşitliğini (yalnız izole history schema adı/son satır biçimi normalize edilerek), aynı dosyanın ilk ve tekrar uygulamasını denetler. SQL ve checksum test çıktısına kopyalanır; Docker kabul image'ı ikisini açıkça alır. Önceki 1/1, EF tarafından bellekte üretilen SQL'i sınamıştı; dosyanın paketlenmiş kabulü ayrıca yürütülüyor.

İlk SQL üretiminde startup-project test projesi olarak seçilmişti; Web açıkça seçilerek düzeltildi. İlk tam metin kontrolü EF CLI'ın yazdığı UTF-8 BOM işaretini yakaladı; artifact standart UTF-8'e çevrildi ve checksum yenilendi. Kontrol kaldırılmadı. İlgili ilk hata logları korunur.

Paketlenmiş dosya kabulü: **1/1 başarılı, exit 0**, 56.3684 saniye — `current-release-exact-sql-packaged-verified-20261024.log`. Güncel Docker image sıfır hata ile kuruldu; test build önceki dört deprecation uyarısını içeriyor. SQL checksum eşitliği, güncel EF metniyle tam eşitlik ve gerçek dosyanın ilk/tekrarında özgün veri SHA-256 eşitliği geçti. Testte EF history schema adının hem quoted hem unquoted üretimi aynı izole ada göre normalize edilir; SQL işlemleri değiştirilmez. Önceki paket, kodlama düzeltmesinden önceki build context snapshot'ını almıştı; ilk paket hash/karşılaştırma hatası saklandı. Geçici DB/ağ temizlendi.

Erişim güncellemesi: kullanıcı yönlendirmesinden sonra IAB'deki açık Coolify Terminal sekmesi yeniden incelendi; mevcut yönetici oturumu ve localhost sunucu terminali kullanılabilir bulundu. Yeni SSH/API yetkisi oluşturulmadı. Salt-okunur docker ps, mevcut 4d552708 uygulama imajları ve üretim konteynerlerini doğruladı; yedek/staging sunucuda henüz yürütülmedi. Önceki anahtarlı SSH reddi, mevcut Coolify oturumunu kapsayan bir erişimsizlik değildir.
