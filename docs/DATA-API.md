# İlk varlık veri dilimi

## Şu anki kapsam

- PostgreSQL EF Core modeli: şirket, çalışan, varlık kategorisi, marka, model, varlıklar, varlık ekleri ve varlık olayları.
- Çalışanlar, giriş yapabilen kullanıcı hesaplarından ayrı tablodadır.
- Varlık etiketi şirket içinde benzersiz ve büyük harfe normalize edilir. Etiket, durum, çalışan, konum ve şirket içindeki garanti bitiş tarihi sorguları için indeksler tanımlıdır.
- Varlık listeleme arama ve sayfalama destekler; ayrıntı cevabı kategori/marka/model/zimmetli çalışan adlarını ve olay geçmişini içerir.
- Varlık oluşturma sırasında kategori, marka ve modelin aynı şirkete ait olduğu denetlenir. Yeni kayıt ilk oluşturma olayını alır.
- Varlık tanımları yönetici tarafından düzenlenebilir; kategori/marka/model şirket kapsamı yeniden doğrulanır, etiket benzersizliği korunur ve beklenen `UpdatedAt` koşullu yazmaya katılarak eski formun yeni değişiklikleri ezmesi önlenir. Zimmet ve durum bu formdan değişmez; yapılan alan güncellemeleri varlık geçmişine tek transaction içinde eklenir.
- Kategori, marka ve model uç noktaları hızlı ekleme yanında katalog listeleme, arama, yeniden adlandırma ve yalnızca kullanılmayan tanımları silmeyi destekler. Kullanım adetleri gösterilir; kullanımda olan kategori/marka/model API düzeyinde de silinemez. Değişiklikler şirket denetim izine yazılır.
- Tüm varlık ve sözlük uç noktaları oturum açmayı ve oturumdaki `company_id` claim'ini zorunlu tutar. İstek gövdesinden şirket kimliği alınmaz.
- İlk sahip hesabı, yalnızca şirket tablosu boşken ve kurulumun ürettiği tek kullanımlık gizli token sağlandığında oluşturulabilir. Token kaynak kodda tutulmamalıdır.
- Oturum açma Identity parola hash'i, 12 karakterlik parola kuralları, başarısız girişte hesap kilitleme ve IP başına istek sınırı kullanır. Cookie `Secure`, `HttpOnly`, `SameSite=Strict`; yazma istekleri CSRF token'ı ister.
- Kimliği doğrulanmış her istekte oturumdaki şirketin etkin olduğu ve kullanıcının güncel üyeliği doğrulanır. Üyelik kaldırılmışsa veya şirket kapatılmışsa oturum reddedilir; şirket rolü değişmişse güncel rol aynı istekte uygulanır ve oturum çerezi yenilenir. Platform yöneticisi claim'i de platform yönetici kaydıyla doğrulanır.
- Tarayıcı yanıtlarında MIME koklama ve iframe içine gömme engellenir; referrer bilgisi sınırlandırılır, kamera yalnızca aynı kaynakta kullanıcı başlatınca kullanılabilir; mikrofon ve konum izinleri kapalıdır. Tüm `/api` yanıtları `Cache-Control: no-store, private` ile işaretlenir; şirket verisi ve oturum yanıtları tarayıcı önbelleğinde tutulmaz.
- Şirket logosu yükleme ucu, doğrulanmış görsel içeriğine ek olarak multipart isteğinin tamamını yaklaşık 2,06 MiB ile sınırlar; dosya seçildikten sonra boyut kontrolüne kadar büyük gövdenin diske taşınmasını engeller. Yerel Nginx geçidi aynı limiti uygular; salt okunur içe aktarma önizlemesi için 32 MiB isteğe izin verir.
- Yerel Nginx `/api/` geçidi en fazla 32 MiB gövdeye izin verir; uygulama uçları kendi sınırını ayrıca uygular. Böylece 2 MiB hesap/çalışan fotoğrafı, 10 MiB çalışan/varlık belgesi ve 32 MiB CSV/JSON önizlemeleri vekil sunucu tarafından varsayılan 1 MiB sınırında kesilmez.
- Varlık dosyaları şirket ve varlık kimliğine göre özel medya dizininde tutulur; public static dosya olarak sunulmaz. PNG/JPEG/WebP/PDF dosyaları 10 MiB ve varlık başına 30 kayıtla sınırlıdır; tür dosya imzasından belirlenir, indirme yalnızca aynı şirket kapsamındaki yetkili oturumdan yapılır, ekleme/kaldırma olay ve denetim geçmişine işlenir.
- Parola kurtarma, hesap varlığını açıklamayan yanıt kullanır; Identity tek kullanımlık reset token'ı SMTP ile e-postalanır. Token yalnızca HTTPS reset sayfasında gönderilir. SMTP veya genel site adresi yapılandırılmadığında bağlantı gönderilmez; operatör loguna yapılandırma uyarısı yazılır.
- Çalışan listesi arama/etkinlik filtreleri, ayrıntıda zimmetli varlıklar ve pasife alma/yeniden etkinleştirme uçları eklendi. Çalışan pasife alındığında geçmiş ve zimmet bağlantıları silinmez. Departmanlar şirket kapsamlı katalogda çalışan sayılarıyla birlikte listelenir; katalog girdileri çalışanlara atanmadan eklenebilir ve çalışan formlarında önerilir. Çalışan-yönetici ilişkisi ayrı kimlik alanıdır; aynı şirketten etkin çalışan seçilir, kendini yönetici seçme ve döngü oluşturma sunucuda engellenir. Çalışan profilinde yönetici, ilk 100 doğrudan raporlayan ve zimmetler gösterilir.
- Lokasyon özeti varlık, aksesuar, sarf ve bileşen stoklarının yanı sıra aktif konuma aksesuar teslim kayıtlarını şirket kapsamında sayar. Yeniden adlandırma açık teslim kayıtlarını da taşır; kapanmış teslim geçmişi korunur. Ayrıca şirket kapsamlı Bina → Kat → Oda kataloğu oluşturma, yeniden adlandırma, arşivleme/geri alma ve eski serbest metinli konum adlarını kataloğa alma uçları vardır. Katalog isimleri yeni kayıt formlarında önerilir; eski serbest metin alanları geriye uyumludur ve henüz katalog kimliğine bağlanmaz.
- Zimmet/iade ve bakım/arşiv durum geçişleri satır kilidi ve veritabanı transaction'ı içinde yapılır. Aynı varlığın iki kullanıcıya eşzamanlı zimmetlenmesi önlenir.
- Hareket geçmişi aktör/çalışan bilgisi taşır; çalışan işlemleri ayrı denetim tablosuna kaydedilir. Yönetici denetim kaydını sayfalı görebilir.
- Sayfalı varlık CSV dışa aktarımında hücre değerleri Excel formül enjeksiyonuna karşı nötrlenir; `X-Has-More` ve `X-Total-Count` başlıkları sonraki sayfa gerekip gerekmediğini gösterir. Arayüzdeki tam dışa aktarma ayrı bir akış uç noktası kullanır; tüm dosyayı sunucu belleğinde biriktirmeden kayıtları yazar.
- Varlık listesi sayfa başına 25/50/100/200 kayıt gösterir; sütun başlıklarından etiket, kategori, marka/model, çalışan, konum, durum, garanti bitişi ve güncellenme tarihine göre artan/azalan sıralanır; aynı değerlerde etiket ve kayıt kimliği sabit sıra sağlar. CSV dışa aktarımı da seçili sıralamayı korur. Arama yanında durum, kategori, marka, model, zimmetli çalışan, tam konum ve garanti durumu (süresi dolmuş, 30 gün içinde dolacak, 30 günden uzun süre aktif, tarihi girilmemiş) filtrelerini destekler. Varlık listesindeki kategori/marka/model/çalışan/konum bağlantıları ilişkili kayıt görünümünü açar; çalışan listesindeki departman bağlantısı aynı departmandaki kişileri süzer. CSV dışa aktarımı da ekrandaki filtreleri uygular. Veritabanı durum, zimmet ve negatif alış bedeli gibi tutarsızlıkları da kontrol eder.

## Yerel yapılandırma değişkenleri

- `ConnectionStrings__EnvanterTR`: PostgreSQL bağlantısı; parola kaynak dosyaya yazılmaz.
- `Setup__BootstrapToken`: kurulum aracı tarafından tek kullanımlık şekilde üretilen ilk yönetici kurulum anahtarı. Kurulum bitince ortamdan kaldırılmalıdır.
- `Email__PublicBaseUrl`: e-posta bağlantılarında kullanılacak uygulama kök adresi; üretimde HTTPS zorunludur.
- `Email__FromAddress`, `Email__FromName`: parola yenileme e-postasının göndereni.
- `Email__Smtp__Host`, `Email__Smtp__Port`, `Email__Smtp__EnableSsl`, `Email__Smtp__Username`, `Email__Smtp__Password`: SMTP sunucu ayarları. Parola yalnızca gizli ortam değişkeni/secret store üzerinden verilmelidir; kaynak dosyaya yazılmamalıdır.
- Üretimde HTTPS zorunludur. Geliştirme ortamında HTTP yalnızca yerel geliştirme için kullanılabilir.

Rol politikaları ayrı tutulur: varlık okuma, varlık değiştirme, çalışan görüntüleme/yönetimi ve şirket ayarları. Şirket sahibi ve yöneticiler uygulama kullanıcılarını görebilir ve e-posta daveti gönderebilir. Davet etkin deneme/abonelik gerektirir ve `MaxLoginUsers` koltuk sınırını işlem sırasında şirket satırını kilitleyerek denetler; sınır şirket sahibi ve bekleyen davetler dâhil tüm giriş hesapları üzerinden uygulanır. Şirket sahibi rolü kilitlidir; kendi rolü değiştirilemez. Şirket sahibi yönetici atayabilir/kaldırabilir; yöneticiler başka yöneticilerin rolünü değiştiremez. Rol değişiklikleri ve davetler şirket denetim kaydına yazılır. Aynı e-posta için şirketler arası hesap paylaşımı bu davet akışında yapılmaz. Şirket sahibini devretme akışı henüz tamamlanmamıştır.

## API sözleşmesi (oturum açma akışı eklendikten sonra)

Çalışan oluşturma/güncelleme uçları (`POST/PATCH /api/employees`) isteğe bağlı `jobTitle`, `phone`, `location` ve ISO `startedOn` (`YYYY-MM-DD`) alanlarını kabul eder. Profil yanıtı `locationCatalogId` ile eşleşen etkin konum kimliğini döndürür; kayıt ve çalışan CSV aktarımı tam eşleşen etkin katalog yolunu bağlar, serbest metin geriye uyumludur. Konum kataloğunda ad değişirse bağlı çalışan yolu da güncellenir. Arama ve `/api/employees/export.csv` konum metnini kapsar. Görev unvanı/konum en fazla 160, telefon 40 karakterdir.

Aksesuar, sarf ve bileşen kayıtları da etkin katalog yolu tam eşleştiğinde `locationCatalogId` ile bağlanır; kayıt/düzenleme ve CSV aktarımı bu bağı kurar. Konum kataloğunda ad değişirse bağlı stok konum metni de eş zamanlı güncellenir. Bilinmeyen veya arşivli yol eski serbest metin davranışını korur.

Konum listesi `isDefault`, `managerEmployeeId` ve `managerName` alanlarını içerir. `POST /api/locations/catalog/{id}/default` gövdesi `{ "isDefault": true }` ile şirkette tek etkin varsayılan konumu belirler veya kaldırır; şirket satır kilidi eşzamanlı güncellemeleri sıraya koyar. `POST /api/locations/catalog/{id}/manager` gövdesi `{ "employeeId": "..." }` ile aynı şirketteki etkin çalışanı sorumlu atar; `null` sorumluyu kaldırır. Her iki işlem şirket denetim kaydı üretir. Yeni varlık, çalışan, aksesuar, sarf ve bileşen oluşturma isteğinde veya bu ailelerin CSV içe aktarımında konum boşsa etkin varsayılan yol kullanılır; açıkça gönderilen konum önceliklidir. Çalışan CSV önizlemesi varsayılanın kaç boş kayda uygulanacağını döndürür.

`GET /api/employees` ve `/api/employees/export.csv`, `startedFrom` ve `startedTo` (`YYYY-MM-DD`) işe başlama tarihi aralığı filtrelerini kabul eder; başlangıç bitişten sonraysa doğrulama hatası döner.

İK notları `/api/employees/{id}/private-notes` GET/PUT uçlarıyla yönetilir; yalnızca `people.files.read` / `people.files.manage` yetkileri (şirket sahibi, yönetici, İK) erişebilir. Notlar en fazla 4.000 karakterdir; denetim kaydında metnin kendisi tutulmaz.

- `GET /api/assets?q=&status=&categoryId=&brandId=&modelId=&employeeId=&location=&warranty=expired|expiring|active|unspecified&sortBy=tag&sortDirection=asc&page=1&pageSize=50` (`warranty` isteğe bağlı; süresi dolanlar, bugünden itibaren 30 gün içinde dolacaklar, 30 günden uzun süre etkin olanlar veya bitiş tarihi girilmemiş olanlar)
- `GET /api/assets/{id}`
  - Yanıt, şirket kapsamındaki varlık alanlarını, kategori/marka/model/çalışan adlarını, `locationCatalogId` konum kataloğu bağlantısını, en yeniden eskiye hareket listesini ve ek dosya metaverisini içerir. Yeni kayıt/düzenleme/transfer ve CSV içe aktarma, girilen yol etkin katalogda tam eşleşirse kimliği bağlar; serbest metin geriye uyumludur. Katalog düğümü yeniden adlandırılınca bağlı varlıkların gösterilen konum yolu güncellenir. Hareketlerde varsa çalışan adı da bulunur; olaylar başka şirket kayıtlarıyla eşleştirilmez.
- `POST /api/assets/{id}/attachments` (`multipart/form-data`, `file`; PNG/JPEG/WebP/PDF, dosya başına 10 MiB, varlık başına 30 dosya; içerik imzası denetlenir; `asset.write`)
- `GET /api/assets/{id}/attachments/{attachmentId}/file[?download=true]` (şirket ve varlık kapsamında yetki denetimli görsel önizleme veya ek indirme; `asset.read`)
- `DELETE /api/assets/{id}/attachments/{attachmentId}` (dosya kaydını ve özel medya dosyasını kaldırır; olay ve denetim izi yazar; `asset.write`)
- `GET /api/employees?q=&department=&managerId=&active=true&page=1&pageSize=50`
- `GET /api/departments` (şirket çalışan kayıtlarındaki departman metinlerini ve aktif/pasif çalışan sayılarını özetler; `people.read`)
- `PATCH /api/departments/rename` (`{ currentName, newName, merge? }`; eşleşen şirket çalışan departmanlarını tek transaction'da değiştirir; hedef departman varsa yalnızca `merge: true` ile birleştirir, aksi hâlde `409`; kayıt yoksa `404`; denetim izi yazar; `people.manage`)
- `GET /api/locations` (şirket varlık, aksesuar, sarf ve bileşen stok konumlarını, ayrıca aktif konuma aksesuar teslim kayıtlarını ve aile başına sayıları özetler; `asset.read`)
- `POST /api/locations/catalog` (`{ name, parentLocationId? }`; şirket kataloğuna bina, kat veya oda ekler; etkin üst konum, üç seviye sınırı ve aynı üstte yinelenen adlar doğrulanır; `asset.write`)
- `PATCH /api/locations/catalog/{id}` (`{ name }`; katalog düğümünü yeniden adlandırır; alt konum yolları 160 karakteri aşamaz, kardeş düğümlerde yinelenen ada izin verilmez; `asset.write`)
- `POST /api/locations/catalog/{id}/archive` (önce etkin alt konumlar arşivlenmelidir; envanter bağlantıları silinmez; `asset.write`)
- `POST /api/locations/catalog/{id}/restore` (üst düğüm etkin olmalıdır; `asset.write`)
- `PATCH /api/locations/rename` (`{ currentName, newName, merge? }`; dört stok ailesindeki tam eşleşen konumları ve aktif konuma aksesuar teslimlerini tek transaction'da değiştirir; kapanmış teslim geçmişini korur; hedef konum varsa yalnızca `merge: true` ile birleştirir, aksi hâlde `409`; kayıt yoksa `404`; denetim izi yazar; `asset.write`)
- `GET /api/accessories?q=&location=&stock=&sortBy=&sortDirection=&page=1&pageSize=25` (şirket aksesuarları; tam stok konumu filtresi, aktif konuma teslim yerini de kapsayan serbest metin araması, stok durumu, sıralama ve sayfalama; `asset.read`)
- `GET /api/accessories/{id}` (şirket kapsamlı açık teslimleri, son 50 stok hareketini ve son 25 ilişkili satın alma sipariş satırını döndürür; `asset.read`)
- `POST /api/accessories/bulk-checkout` (aynı çalışan, varlık veya konuma 1–100 farklı aksesuarı her biri için ayrı adetle tek atomik işlemde teslim eder; şirket kapsamı, etkin çalışan, stok kilitleri ve yeterli kalan miktar doğrulanır; her ürün için zimmet, hareket ve denetim kaydı yazılır; stoklardan biri yetmiyorsa hiçbir kayıt değişmez; `asset.write`)
- `POST /api/accessories/bulk-stock-adjustment` (`{ items: [{ accessoryId, quantity }], notes? }`; 1–100 farklı aksesuarın artı/eksi stok sayım farkını aynı atomik işlemde kaydeder; ürün satırlarını kilitler, toplam stoku, 100.000 üst sınırını ve açık zimmet adedini doğrular; her fark için stok hareketi ve denetim kaydı üretir; herhangi bir satır geçersizse hiçbir stok değişmez; `asset.write`)
- `POST /api/accessories/{id}/bulk-checkin` (`{ items: [{ assignmentId, quantity }], notes? }`; aynı aksesuara ait 1–100 açık zimmetten seçilen miktarları tek atomik işlemde iade alır; şirket kapsamı, açık kalan miktar ve stok/zimmet kilitleri doğrulanır; her satır hareket ve denetim kaydı üretir; herhangi bir adet geçersizse hiçbir satır değişmez; `asset.write`)
- `POST /api/accessories/{id}/checkout` (seçilen çalışan, varlık veya konuma kullanılabilir miktardan teslim yapar; stok satırını transaction boyunca kilitler, zimmet, hareket ve denetim kaydını birlikte yazar; `asset.write`)
- `POST /api/accessories/{id}/checkin` (`{ assignmentId, quantity, notes? }`; açık zimmetin bir kısmını veya tamamını iade alır; aynı stok satırı kilit sırasını izler, hareket ve denetim kaydını atomik yazar; `asset.write`)
- `GET /api/accessory-orders?q=&status=&page=1&pageSize=25` (sipariş no/tedarikçi/not/aksesuar adı araması, durum filtresi ve sayfalı şirket sipariş listesi; miktar ve maliyet özeti döndürür; `asset.read`)
- `GET /api/accessory-orders/suppliers` (etkin tedarikçi kataloğu ile sipariş, aksesuar, sarf, bileşen, lisans ve bakım kayıtlarından şirket kapsamlı öneri adlarını birleştirir; `asset.read`)
- `GET /api/suppliers?q=&includeArchived=false` (şirket tedarikçi kartları ve sipariş bağlantı sayısı; `asset.read`)
- `POST /api/suppliers`, `PUT /api/suppliers/{id}`, `POST /api/suppliers/{id}/archive|restore` (yetkili kişi/iletişim alanları doğrulanır; şirket içinde normalize edilmiş ad tekildir; ad değişikliği aksesuar/sarf/bileşen/lisans/bakım kayıtlarının metnini ve yabancı anahtarını aynı transaction içinde günceller, sipariş başlığının geçmiş metnini değiştirmez; `asset.write`)
- `GET /api/accessory-orders/{id}` (şirket kapsamındaki sipariş başlığı ve kalemleri; kalemlerde sipariş/teslim/kalan miktar ve maliyet bilgisi; `asset.read`)
- `POST /api/accessory-orders` (`{ orderNumber?, supplier, orderDate, expectedOn?, currency, notes?, items: [{ accessoryId, quantity, unitCost, notes? }] }`; 1–100 tekrarsız aksesuar kalemi, sipariş numarası şirket içinde benzersiz; sipariş `Ordered` durumuyla açılır; `asset.write`)
- `POST /api/accessory-orders/{id}/receive` (`{ receivedOn, notes?, items: [{ lineId, quantity }] }`; 1–100 sipariş satırında kısmi veya tam miktar kabulü; sipariş ve aksesuar satırları transaction içinde kilitlenir, kalan miktar/şirket/100.000 stok sınırı denetlenir, aksesuar stoku, maliyet/tedarikçi bilgisi, stok hareketi ve denetim izi birlikte yazılır; hatada hiçbir stok değişmez; `asset.write`)
- `POST /api/accessory-orders/{id}/cancel` (tamamlanmış sipariş dışında iptal; daha önce teslim alınan miktar ve stok hareketleri korunur; `asset.write`)
- `GET /api/consumables?q=&stock=&sortBy=&sortDirection=&page=1&pageSize=25` (şirket sarf kayıtları; ad/kategori/model/ürün no/tedarikçi/konum araması, stok durumu filtresi, sıralama ve sayfalama; `asset.read`)
- `GET /api/components?q=&stock=&page=1&pageSize=25` (şirket bileşenleri; ad/kategori/model/seri no/ürün no/tedarikçi/konum araması ve stok durumu filtresi; `asset.read`)
- `POST /api/components/bulk-stock-adjustment` (`{ items: [{ componentId, quantity }], notes? }`; 1–100 farklı bileşenin artı/eksi sayım farkını atomik uygular; stok satırlarını kilitler, toplamı ve atanmış miktar alt sınırını doğrular; her değişiklik hareket ve denetim kaydı üretir; geçersiz satır varsa hiçbir stok değişmez; `asset.write`)
- `POST /api/components/bulk-allocate` (`{ assetId, items: [{ componentId, quantity }], notes? }`; 1–100 farklı bileşeni aynı etkin varlığa her birinden ayrı adetle atomik takar; şirket kapsamını, stok kilitlerini ve tüm kullanılabilir miktarları doğrular; her atama için hareket ve denetim kaydı üretir; herhangi bir stok yetmezse hiçbir atama yapılmaz; `asset.write`)
- `GET /api/reports/assets?purchaseDateFrom=YYYY-MM-DD&purchaseDateTo=YYYY-MM-DD` (şirket kapsamlı varlık/durum/kategori adetleri ve para birimine göre kayıtlı alış bedeli toplamı; isteğe bağlı alış tarihi filtresi dahil; `asset.read`; tarih aralığı uçları dahildir, tarihi boş kayıtlar filtreli kapsamına girmez, değerler defter değeri değildir ve para çevrimi yapılmaz). JSON, para toplamını görüntüleme için sayı ve hassas dışa aktarım için invariant ondalık metin olarak sunar. Arayüz raporu uygulanan tarih aralığını da içeren Excel uyumlu CSV indirir; metin hücreleri formül başlatabilecek karakterlere karşı nötrlenir.
- `POST /api/employees` (isteğe bağlı `managerEmployeeId`, aynı şirketten etkin bir çalışan olmalıdır)
- `GET /api/employees/{id}` (zimmetleriyle birlikte yöneticiyi ve doğrudan raporlayan ilk 100 çalışanı döndürür)
- `PATCH /api/employees/{id}/manager` (yönetici bağlantısını kurar veya kaldırır; şirket sınırı, kendini seçme ve yönetici döngüsü kontrolleri sunucuda uygulanır)
- `POST /api/assets`
- `PATCH /api/assets/{id}` (şirket içi alan doğrulaması, benzersiz etiket, model/marka eşleşmesi, `expectedUpdatedAt` ile eski kayıt koruması; zimmet ve durum alanlarını değiştirmez)
- `GET /api/lookups/categories|brands|models`
- `POST /api/lookups/categories|brands|models`
- `PATCH /api/lookups/{categories|brands|models}/{id}` (yalnızca ad değiştirir; varlık bağlantılarını korur)
- `DELETE /api/lookups/{categories|brands|models}/{id}` (tanım herhangi bir varlıkta veya bağlı modelde kullanılıyorsa `409 Conflict`; veri silmez)
- `GET /api/setup/status` (şirket ilk kurulumu gerekli mi ve `Setup:BootstrapToken` sunucu ayarında yapılandırılmış mı bilgisini verir; anahtarın değerini döndürmez).
- `GET /api/platform/bootstrap/status` (platform ilk kurulumu gerekli mi ve `Setup:PlatformBootstrapToken` ayarlanmış mı bilgisini verir; anahtarın değerini döndürmez).
- `GET /api/company/export.zip` (`settings.manage`; özel/no-store, tekrarlanabilir şirket veri görünümünü ZIP içinde `company-data.json` ile şirket medya dosyalarını birleştirir; şirket üyelerinin hesap profil fotoğrafları dahil edilir, diğer hesapların dosyaları alınmaz; 100.000 dosya/4 GiB medya sınırı; symlink ve harici oturum dosyalarını izlemez; sistem yedeği/geri yükleme değildir).
- `GET /api/status` (veritabanı bağlantısı, migration varlığı ve bekleyen migration sayısı; bağlantı parolasını göstermez)
- `GET /api/session/csrf`
- `POST /api/setup/initial-owner` (`X-Setup-Token` ve CSRF başlığı gerekir; e-posta mevcut hesapta kayıtlıysa mevcut parola doğrulanarak hesap şirkete bağlanabilir, parola/profile dokunulmaz).
- `POST /api/session/login` (`X-CSRF-TOKEN` gerekir)
- `POST /api/session/password/forgot` (genel yanıt; SMTP yapılandırması gerekir)
- `POST /api/session/password/reset` (e-posta, token ve yeni parola; CSRF gerekir)
- `GET /api/session`
- `POST /api/session/logout` (`X-CSRF-TOKEN` gerekir)
- `GET /api/employees` departman filtresi tam eşleşir ve en fazla 160 karakterdir; yönetici kimliğiyle doğrudan raporlayanlar filtrelenebilir.
- `GET /api/departments` (`people.read`; çalışan kayıtlarındaki değerleri ve henüz çalışanı olmayan katalog birimlerini birleştirilmiş sayımla verir).
- `POST /api/departments` (`people.manage`; şirkette benzersiz 1–160 karakterli ad ekler; denetim kaydı yazar; CSV/çalışan serbest metin kayıtları değişmeden kalır).
- `PATCH /api/departments/rename` (`people.manage`; çalışan değerlerini ve katalog adını atomik olarak yeniden adlandırır veya onaylı gruba birleştirir; şirket satırını kilitler ve denetim kaydı yazar).
- `POST /api/locations/catalog`, `PATCH /api/locations/catalog/{id}`, `POST /api/locations/catalog/{id}/archive|restore` (`asset.write`; şirket kapsamında bina/kat/oda olarak en fazla üç seviyeli, üst konumu aynı şirkette doğrulanan hiyerarşik katalog oluşturur, adını günceller ve arşivler/geri alır; aynı üst konumda yinelenen adı engeller; üst konum, etkin alt konumlar arşivlenmeden arşivlenemez; katalog seçimleri eski serbest metinli stok/varlık değerlerini kendiliğinden değiştirmez).
- `GET /api/locations` (`asset.read`; varlık/stok sayımlarını ve katalog konumlarını birleştirir; katalog kayıtlarında `locationId`, `parentLocationId`, `depth`, `isCatalogEntry`, `isActive` ve gösterim yolu `name` alanları bulunur).
- `POST /api/employees/import/preview.csv` (`people.manage`; CSRF zorunlu; en fazla 32 MiB ve 5.000 satır; UTF-8, virgülle ayrılmış çalışan şablonu; personel no yinelenmesi, alan sınırları, e-posta ve yyyy-MM-dd tarihi doğrulanır; mevcut şirket numaralarıyla çakışma reddedilir; salt okunur).
- `POST /api/employees/import.csv` (`people.manage`; CSRF zorunlu; aynı doğrulama ve sınırlar; sadece yeni etkin çalışan kaydı ekler, yöneticiyi sonradan atamak üzere boş bırakır; yinelenen personel no durumunda aktarımın tamamı reddedilir; tüm çalışan ve denetim kayıtları tek transaction içinde yazılır).
- `POST /api/accessories/import/preview.csv` (`asset.write`; CSRF zorunlu; en fazla 32 MiB ve 5.000 satır; UTF-8 aksesuar şablonu, miktar/maliyet/tarih alanları ve dosya/şirket içi ad/ürün no tekrarlarını doğrular; salt okunur).
- `POST /api/accessories/import.csv` (`asset.write`; CSRF zorunlu; önizlemedeki aynı doğrulama ve sınırlar; yalnızca yeni aksesuarlar eklenir; başlangıç stoku, stok hareketi ve denetim izi tek transaction içinde yazılır; mevcut zimmetler değiştirilmez).
- `POST /api/software-licenses/import/preview.csv` (`asset.write`; CSRF zorunlu; en fazla 16 MiB ve 5.000 satır; lisans alanlarını, tür/koltuk/maliyet/tarih değerlerini ve dosya/şirket içi ad-yayıncı-sürüm tekrarlarını denetler; lisans anahtarı/referansı almaz, salt okunur).
- `POST /api/software-licenses/import.csv` (`asset.write`; aynı doğrulama ve sınırlar; yeni lisans kayıtları ve oluşturulma olayları transaction içinde eklenir; mevcut atamalar değişmez).
- `GET /api/software-licenses/export.csv?q=...` (`asset.read`; etkin lisans aramasıyla eşleşen en fazla 10.000 satır; CSV formül başlatıcıları nötrlenir; lisans anahtarı/referansı dışa aktarılmaz).
- `GET /api/employees/{id}?page=1&pageSize=20` çalışanı, yöneticiyi, doğrudan raporlayanları ve sayfalı zimmetli varlıkları döndürür.
- `GET /api/employees/{id}/documents` (`owner`, `admin`, `hr_operator`; yalnızca belge metaverisi; en fazla 30 belge)
- `POST /api/employees/{id}/documents` (`multipart/form-data`, `file`; `owner`, `admin`, `hr_operator`; PDF/PNG/JPEG/WebP, dosya başına 10 MiB; içerik imzası doğrulanır)
- `GET /api/employees/{id}/documents/{documentId}/file` (aynı şirket/çalışan kapsamında yetkili oturuma attachment indirmesi; `owner`, `admin`, `hr_operator`)
- `DELETE /api/employees/{id}/documents/{documentId}` (özel medya dosyasını ve kaydı kaldırır, denetim izi yazar; `owner`, `admin`, `hr_operator`)
- `GET /api/employees/{id}/offboarding-summary` (`people.manage`; pasife alma öncesinde varlık, açık aksesuar teslimi, aktif lisans ve aktif doğrudan raporlayan adetleri; kayıt ayrıntılarını döndürmez)
- `PATCH /api/employees/{id}/active`
- `POST /api/assets/{id}/assign`
- `POST /api/assets/{id}/return`
- `PATCH /api/assets/{id}/status`
- `POST /api/assets/{id}/restore` (`asset.write`; yalnızca arşivlenmiş varlığı satır kilidi altında kullanılabilir duruma döndürür; zimmet/açık bakım doğrular, isteğe bağlı notla hareket ve denetim kaydı yazar)
- `GET /api/calendar?from=&to=&kind=&limit=200` (şirket kapsamlı planlı bakım, lisans ve garanti bitiş tarihleri; `kind=maintenance|license|warranty`, en çok 366 takvim günü ve 500 kayıt)
- `GET /api/maintenances?q=&status=&from=&to=&page=1&pageSize=50` (şirket kapsamlı arama ve filtreleme; varlık/etiket/servis/not, planlandı/devam/tamamlandı/iptal durumu ve planlanan/başlangıç tarihine göre; sayfa boyutu en fazla 100)
- `GET /api/maintenances/export.csv?q=&status=&from=&to=` (aynı filtrelere uyan tüm şirket bakım kayıtları; akış halinde UTF-8 CSV, hücre formül enjeksiyonuna karşı kaçış uygulanır)
- `GET /api/reports/maintenances?from=&to=` (başlayan bakım kayıtlarını tarih aralığına göre toplam, devam/tamamlandı sayısı, en sık 25 bakım türü ve para birimi bazında maliyet olarak özetler; para birimlerini birleştirmez)
- `GET /api/assets/{id}/maintenances` (şirket kapsamlı bakım geçmişi, kaydı açan kullanıcıyı içerir)
- `POST /api/assets/{id}/maintenances/schedule` (kullanılabilir veya zimmetli varlığa 5 yıla kadar bakım tarihi planlar; varlığın mevcut durumunu değiştirmez)
- `POST /api/assets/{assetId}/maintenances/{maintenanceId}/start` (planlı bakımı başlatır; yalnızca kullanılabilir ve zimmet dışı varlıkta çalışır)
- `POST /api/assets/{assetId}/maintenances/{maintenanceId}/cancel` (planlı bakımı iptal eder; varlık ve hareket geçmişi korunur)
- `GET /api/company/label-settings` (şirket üyesi; barkod/etiket ayar profilini döndürür)
- `PUT /api/company/label-settings` (`settings.manage`; şirket ortak etiket profilini ölçü, hizalama, kod türü ve gösterilecek alanlarla günceller; değişiklik denetime eklenir)
- `POST /api/assets/{id}/maintenances` (kullanılabilir ve zimmet dışı varlıkta tek açık bakım; tür, servis, tarih, maliyet, notlar; varlığı bakım durumuna alır)
- `POST /api/assets/{assetId}/maintenances/{maintenanceId}/complete` (tamamlanma tarihi/notu; bakım kaydını kapatır ve varlığı kullanılabilir duruma getirir)
- `GET /api/activity?page=1&pageSize=50`
- `GET /api/activity?assetId=&employeeId=&from=&to=&eventType=&q=` (şirket içinde varlık/çalışan/tarih/tür/metin filtreli)
- `GET /api/audit?page=1&pageSize=50&q=&targetType=` (sahip/yönetici; filtreler ve sayfalama)
- `GET /api/members` (sahip/yönetici; şirket üyelikleri, kullanılabilir roller ve varsa çalışan profili bağlantısı)
- `POST /api/members/invite` (sahip/yönetici; ad, e-posta ve rol; CSRF + istek sınırı + etkin plan/koltuk kontrolü)
- `PATCH /api/members/{userId}/role` (sahip/yönetici; denetim kaydı üretir)
- `PATCH /api/members/{userId}/employee` (`settings.manage`; `{ "employeeId": "..." }` veya bağlantıyı kaldırmak için `{ "employeeId": null }`; yalnızca aynı şirketin etkin çalışanı, bir çalışan profiline tek üyelik ve denetim kaydı)
- `GET /api/assets/export.csv?page=1&pageSize=5000&q=`
- `GET /api/assets/export-all.csv?q=&status=&categoryId=&brandId=&modelId=&employeeId=&location=&warranty=expired|expiring|active|unspecified&sortBy=tag&sortDirection=asc` (bellekte tüm dosyayı oluşturmadan akış halinde tam dışa aktarım; etiket/ad/durum, kategori/marka/model, seri no/konum, çalışan numarası/adı, alış tarihi, garanti bitiş tarihi/tutarı ve para birimi; arayüzdeki garanti filtresi ve diğer süzgeçler uygulanır; CSV formül enjeksiyonu için hücre kaçışı uygulanır)
- `POST /api/assets/export-selected.csv` (`asset.read`; CSRF zorunlu; 1–100 şirket varlık kimliği; seçili kayıtları garanti bitiş tarihi dahil tam dışa aktarımla aynı sütunlarda ve formül güvenli CSV biçiminde indirir)
- `POST /api/assets/labels/selected` (`asset.read`; CSRF zorunlu; 1–100 tekrarsız ve şirkete ait varlık kimliği; seçili kimlik sırasını ve etiket alanlarını döndürür)
- `POST /api/assets/bulk-status` (`asset.write`; CSRF zorunlu; 1–100 tekrarsız şirket varlık kimliği; durum güncellemesini tek işlemde uygular, zimmetli/arşiv kısıtlarını doğrular ve her değişen varlık için hareket/denetim kaydı oluşturur)
- `POST /api/assets/bulk-assign` (`asset.write`; CSRF zorunlu; 1–100 tekrarsız şirket varlık kimliği, etkin aynı şirket çalışanı ve isteğe bağlı not; seçimin tamamı boşta/kullanılabilir olmalı; tek transaction, kilitli kayıtlar, varlık başına hareket ve denetim kaydı)
- `POST /api/assets/bulk-return` (`asset.write`; CSRF zorunlu; 1–100 tekrarsız şirket varlık kimliği ve isteğe bağlı not; seçimin tamamı zimmetli olmalı; tek transaction, önceki çalışan bilgisiyle varlık başına hareket ve denetim kaydı)
- `POST /api/assets/bulk-transfer` (`asset.write`; CSRF zorunlu; 1–100 tekrarsız şirket varlık kimliği, hedef konum ve 1.600 karaktere kadar not; seçimin tamamı boşta/kullanılabilir olmalı ve hedef konum var olan konumla çakışmamalı; tek transaction ve varlık başına hareket/denetim kaydı)
- `POST /api/assets/bulk-audit` (`asset.write`; CSRF zorunlu; 1–100 tekrarsız şirket varlık kimliği, denetim tarihi ve 1.900 karaktere kadar not; tek transaction, varlık başına hareket ve denetim kaydı)
- `GET /api/company/export.json` (şirket sahibi/yöneticisi; JSON biçim sürüm 1, şirket ayarları, çalışanlar, envanter sözlükleri, varlıklar, bakım kayıtları, olaylar, denetim kayıtları ve üyelik rolleri; parola hashleri/kimlik bilgileri içermez; indirilen dosya henüz geri yüklenemez).
- `POST /api/company/import/preview` (şirket sahibi/yöneticisi; CSRF zorunlu; en fazla 32 MiB JSON; biçim/sürüm/şirket kimliği/tekrar eden GUID, alan kuralları ve kayıt bağlantılarını doğrular; dosya adetlerini ve büyük-küçük harf farkını yok sayarak mevcut şirkette aynı çalışan numarası/varlık etiketi/kategori/marka adına sahip kayıt sayılarını döndürür; bu yalnızca olası eşleşme sayısıdır; salt okunur, otomatik eşleştirme veya yazma yapmaz).
- `POST /api/assets/import/preview.csv` (`asset.write`; CSRF zorunlu; en fazla 32 MiB ve 5.000 varlık satırı; noktalı virgülle ayrılmış UTF-8 EnvanterTR CSV'si; garanti bitiş tarihli yeni 14 sütunlu şemayı ve eski 13 sütunlu dosyaları kabul eder; başlık/sütun sayısı, alan sınırları, durum/tarih/tutar biçimi, tekil kategori/marka/model/çalışan eşleşmeleri, yinelenen etiket ve mevcut şirket etiket çakışmalarını doğrular; salt okunur).
- `POST /api/assets/import.csv` (`asset.write`; CSRF zorunlu; aynı CSV biçimi ve 5.000 satır sınırı; yeni 14 sütunlu dosyada garanti bitiş tarihini içe alır, eski 13 sütunlu dosyaları da kabul eder; yalnızca aynı şirkette tekil olarak bulunan kategori/marka/model ve personel numarası+adı eşleşmeleriyle yeni varlık ekler; yinelenen etiket/bozuk ilişki halinde tüm istek reddedilir; her varlık için hareket ve aktarım için denetim kaydı ekler; tüm yazmalar tek veritabanı işlemi içindedir).
- `POST /api/consumables/import/preview.csv`, `POST /api/consumables/import.csv` (`asset.write`; CSRF zorunlu; en fazla 32 MiB ve 5.000 satır; UTF-8 virgülle ayrılmış CSV, miktar/tarih/maliyet/para birimi doğrulaması; aynı ad/ürün no veya dosya içi tekrarları reddeder; önizleme yazma yapmaz; içe aktarma yalnızca yeni kayıtları ekler, başlangıç miktarı için stok girişi hareketi ve denetim izi oluşturur; tüm yazmalar tek transaction içindedir).
- `POST /api/components/import/preview.csv`, `POST /api/components/import.csv` (`asset.write`; CSRF zorunlu; en fazla 32 MiB ve 5.000 satır; UTF-8 CSV, ürün no/seri no/miktar/tarih/maliyet doğrulaması ve yinelenen kayıt kontrolü; salt okunur önizleme; yalnızca yeni bileşen ekler, başlangıç stok hareketi ve denetim izi yazar; var olan atamaları değiştirmez; tüm yazmalar tek transaction içindedir).
- Yerel Compose yedek betikleri (`deploy/local/backup.ps1`, `deploy/local/backup.sh`) uygulama API'si değildir; çalışan geliştirme konteynerinden tüm PostgreSQL örneği ve şirket medya volume'u için elle dosya üretir. Bu klasör çıktısı başlangıçta şifrelenmemiştir. `tools/EnvanterTR.Backup` onu ayrıca parola ile şifreli arşive çevirebilir ve dosya bütünlüğünü doğrulayabilir. `restore-preview.ps1` ve `restore-preview.sh`, doğrulanmış paketi kalıcı volume veya ağ kullanmayan geçici PostgreSQL konteynerine geri yükleyip medya arşivini açarak kurtarılabilirliği gösterir; çalışan uygulama veritabanına geri yükleme yapmaz.
- Şirket panelindeki **Veri ve yedekler** menüsü JSON/CSV dışa aktarımını ve JSON doğrulamasını sunar. Varlık ve çalışan listelerindeki CSV araçları, indirilebilir şablonla yeni kayıtları doğrulayıp toplu ekler. CSV alan eşleme, JSON geri yükleme ve yedekleme geçmişi henüz etkin değildir.
- `GET /api/dashboard/summary`

`pageSize` en fazla 200'dür. Hatalı alanlar doğrulama cevabı, yinelenen varlık etiketi 409 cevabı verir.

## Platform sahibi yönetimi

Platform sahibinin yönetim API'si müşteri şirket alanından ayrı bir `platform_admin` claim'i ile korunur. İlk platform sahibi `Setup__PlatformBootstrapToken` ile bir kere oluşturulur; secret kurulumdan sonra kaldırılmalıdır.

- `POST /api/platform/bootstrap` (ilk platform hesabı; antiforgery + `X-Platform-Setup-Token` gerekir; e-posta mevcut hesapta kayıtlıysa mevcut parola doğrulanarak hesaba platform yetkisi bağlanabilir, parola/profile dokunulmaz).
- `GET /api/plans`, `GET /api/site-content` (yayımlanan web sitesi verisi)
- `GET /api/platform/overview`
- `GET /api/platform/companies?q=&active=&page=1&pageSize=50`
- `GET /api/platform/companies/{id}` (firma özeti, kullanıcı/rol listesi ve son abonelik geçmişi)
- `GET /api/platform/companies/renewals?days=30&page=1&pageSize=50`
- `PATCH /api/platform/companies/{id}/active`
- `GET/POST/PUT /api/platform/plans`
- `POST /api/platform/companies/{id}/demo`
- `POST /api/platform/companies/{id}/subscription/extend`
- `GET/PUT /api/platform/site-content` ve `/api/platform/site-content/{key}`
- `GET /api/platform/audit?page=1&pageSize=50`

Yazma uçları antiforgery doğrular ve yönetici işlemlerini platform denetim tablosuna kaydeder. Plan ve içerik API'leri temel veri işlemleridir; henüz gerçek ödeme, müşteri self-servis siparişi, dosya yükleme, imzalı çevrimdışı lisans ya da platform admin arayüzü olarak tamamlanmış değildir. Ayrıntılı kapsam ve açık işler `docs/PLATFORM-ADMIN.md` içindedir.

## Kurulum ve veri güvenliği

Bağlantı sırrını kaynak dosyaya yazmayın. `ConnectionStrings__EnvanterTR` ortam değişkeniyle yapılandırın. Normal uygulama başlangıcı `EnsureCreated` veya migration çalıştırmaz; şema değişikliği yalnızca açık `--migrate` komutuyla uygulanır. İlk boş yerel veritabanı Compose içindeki ayrı kalıcı volume'a kurulur. `/api/status` bağlantıyı ve şema sürümünü ayrı raporlar. `/api/setup/status`, migration yoksa veya bekleyen migration varsa ilk yönetici kurulumunu başlatmaz. İlk şirket sahibi için `/setup.html` kurulum sayfası vardır ve tek kullanımlık kurulum anahtarı ister. Yerel Docker geliştirme akışı `deploy/local/README.md` içindedir. Yedekleme/geri yükleme aracı ve gerçek müşteri verisi geçişi henüz tamamlanmadı.

## Henüz çalıştırılabilir kabul edilmemeli

Müşteri panelinin giriş, varlık/çalışan listeleri, kayıt, zimmet/iade, durum, CSV, hareket/denetim geçmişi, parola kurtarma, şirket üyesi davet/rol yönetimi, departman/lokasyon özetleri ve yönetici hiyerarşisi API'ye bağlandı; diğer menü iş akışları hâlâ sırada ve açıkça bu şekilde gösterilir. İlk migration'lar yalnızca yerel geliştirme veritabanına uygulandı. SMTP parola sıfırlama, yedekleme/geri yükleme, güncelleme geri alma ve üretim için token rotasyonu tamamlanmadı. Kimlik doğrulamalı uç noktalar fail-closed biçimde yetkilidir. Şu anki kod gerçek müşteri verisini yönetmeye hazır değildir; kurulum ya da üretim sunucusunda kullanmayın. Yerel önizleme loopback adresiyle sınırlıdır; LAN istemcileri için TLS ve güvenlik duvarı kurulumu ürün kurulumuna eklenecek.

Birden fazla uygulama örneği kullanılacağı zaman ASP.NET Core Data Protection anahtar halkası kalıcı ve ortak saklanmalıdır; aksi durumda oturum/parola sıfırlama token'ları dağıtım sonrası geçersiz olabilir. Bu dağıtım ayarı henüz uygulanmadı.

## Stok talepleri

- `GET /api/requests/catalog?q=` talebe açık ve kullanılabilir aksesuar/sarf kalemleriyle, talebe açık boşta/kullanılabilir varlıkları döndürür. `GET /api/requests/employees?q=` yalnızca akışta gerekli etkin çalışan kimliği, adı ve çalışan numarasını döndürür. İki uç da `asset.read` ister ve oturumdaki şirketle sınırlıdır.
- `GET /api/requests/employees?q=` yanıtı, şirket üyeliği bir çalışan kaydına bağlıysa `selfEmployee` alanında o kişinin kimliğini ve görünen bilgilerini de içerir. Bağlı kullanıcı formda çalışan seçemez; `POST /api/requests` gövdesindeki çalışan kimliği oturum üyeliğiyle aynı değilse 403 döner. Üyeliğe bağlı çalışan pasifse talep oluşturulmaz.
- `GET /api/requests?status=&q=&page=1&pageSize=25` talepleri sayfalar; `asset.write` yetkili kullanıcılar şirket taleplerini, diğer kullanıcılar yalnızca kendi açtıkları talepleri görür. `GET /api/requests/export.csv` aynı erişim sınırını ve filtreleri uygular. Durum `Pending`, `Approved`, `Rejected` veya `Cancelled` olabilir. Yanıt kalem, çalışan, talep eden ve karar veren görünen adlarını içerir.
- `POST /api/requests` gövdesi `{ "employeeId": "...", "itemType": "Accessory|Consumable|Asset", "itemId": "...", "quantity": 1, "note": "..." }` biçimindedir. Etkin çalışan ve talep edilebilir kalem şirket kapsamında doğrulanır. Varlık talebinde adet tam olarak 1 olmalıdır.
- `POST /api/requests/{id}/decision` gövdesi `{ "approve": true, "note": "..." }` biçimindedir ve `asset.write` ister. Onay sırasında talep, çalışan ve hedef satır kilitlenip durum yeniden doğrulanır; onay, aksesuar zimmeti, sarf tüketim hareketi veya varlık zimmeti ile denetim kaydı tek transaction içinde yazılır. Stok azaldıysa veya varlık artık boşta değilse istek açık kalır ve 409 döner. Red stok/zimmet hareketi üretmez.
- `POST /api/requests/{id}/cancel` yalnızca talebi açan kullanıcı tarafından beklemedeyken çağrılabilir; diğer durumlar 409 döndürür. İptal ve karar işlemleri aynı talep satırını kilitleyerek çakışan değişiklikleri engeller.
- Talep ekranı oturum kullanıcısının yetkili olduğu şirkette, envanter okuma rolüyle açılır. Yönetici `PATCH /api/members/{userId}/employee` ile üyeliği aynı şirketteki etkin bir çalışan profiline bağlayabilir veya `employeeId: null` ile kaldırabilir (`settings.manage`). Bir çalışan profili aynı şirkette en fazla bir üyeliğe bağlanabilir. Bağlı hesap sadece kendi adına talep açar; henüz eşleştirilmemiş hesaplar seçtikleri etkin çalışan adına talep açmaya devam eder.
