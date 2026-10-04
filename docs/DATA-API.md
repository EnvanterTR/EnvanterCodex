# İlk varlık veri dilimi

## Şu anki kapsam

- PostgreSQL EF Core modeli: şirket, çalışan, varlık kategorisi, marka, model, varlık ve varlık olayları.
- Çalışanlar, giriş yapabilen kullanıcı hesaplarından ayrı tablodadır.
- Varlık etiketi şirket içinde benzersiz ve büyük harfe normalize edilir. Etiket, durum, çalışan ve konum sorguları için indeksler tanımlıdır.
- Varlık listeleme arama ve sayfalama destekler; ayrıntı cevabı kategori/marka/model/zimmetli çalışan adlarını ve olay geçmişini içerir.
- Varlık oluşturma sırasında kategori, marka ve modelin aynı şirkete ait olduğu denetlenir. Yeni kayıt ilk oluşturma olayını alır.
- Varlık tanımları yönetici tarafından düzenlenebilir; kategori/marka/model şirket kapsamı yeniden doğrulanır, etiket benzersizliği korunur ve beklenen `UpdatedAt` koşullu yazmaya katılarak eski formun yeni değişiklikleri ezmesi önlenir. Zimmet ve durum bu formdan değişmez; yapılan alan güncellemeleri varlık geçmişine tek transaction içinde eklenir.
- Kategori, marka ve model uç noktaları hızlı ekleme yanında katalog listeleme, arama, yeniden adlandırma ve yalnızca kullanılmayan tanımları silmeyi destekler. Kullanım adetleri gösterilir; kullanımda olan kategori/marka/model API düzeyinde de silinemez. Değişiklikler şirket denetim izine yazılır.
- Tüm varlık ve sözlük uç noktaları oturum açmayı ve oturumdaki `company_id` claim'ini zorunlu tutar. İstek gövdesinden şirket kimliği alınmaz.
- İlk sahip hesabı, yalnızca şirket tablosu boşken ve kurulumun ürettiği tek kullanımlık gizli token sağlandığında oluşturulabilir. Token kaynak kodda tutulmamalıdır.
- Oturum açma Identity parola hash'i, 12 karakterlik parola kuralları, başarısız girişte hesap kilitleme ve IP başına istek sınırı kullanır. Cookie `Secure`, `HttpOnly`, `SameSite=Strict`; yazma istekleri CSRF token'ı ister.
- Tarayıcı yanıtlarında MIME koklama ve iframe içine gömme engellenir; referrer bilgisi sınırlandırılır, kullanılmayan kamera/mikrofon/konum izinleri kapatılır. Tüm `/api` yanıtları `Cache-Control: no-store, private` ile işaretlenir; şirket verisi ve oturum yanıtları tarayıcı önbelleğinde tutulmaz.
- Şirket logosu yükleme ucu, doğrulanmış görsel içeriğine ek olarak multipart isteğinin tamamını yaklaşık 2,06 MiB ile sınırlar; dosya seçildikten sonra boyut kontrolüne kadar büyük gövdenin diske taşınmasını engeller. Yerel Nginx geçidi aynı limiti uygular; salt okunur içe aktarma önizlemesi için 32 MiB isteğe izin verir.
- Parola kurtarma, hesap varlığını açıklamayan yanıt kullanır; Identity tek kullanımlık reset token'ı SMTP ile e-postalanır. Token yalnızca HTTPS reset sayfasında gönderilir. SMTP veya genel site adresi yapılandırılmadığında bağlantı gönderilmez; operatör loguna yapılandırma uyarısı yazılır.
- Çalışan listesi arama/etkinlik filtreleri, ayrıntıda zimmetli varlıklar ve pasife alma/yeniden etkinleştirme uçları eklendi. Çalışan pasife alındığında geçmiş ve zimmet bağlantıları silinmez. Departman adı şimdilik metinsel alandır; departman özet ekranı çalışan kayıtlarından sayıları hesaplar. Çalışan-yönetici ilişkisi ayrı kimlik alanıdır; aynı şirketten etkin çalışan seçilir, kendini yönetici seçme ve döngü oluşturma sunucuda engellenir. Çalışan profilinde yönetici, ilk 100 doğrudan raporlayan ve zimmetler gösterilir.
- Lokasyon özeti şirket varlıklarındaki metin değerlerini ve kullanılabilir/atanmış/bakım adetlerini gösterir; bina/oda ağacı veya ayrı lokasyon kataloğu henüz yoktur.
- Zimmet/iade ve bakım/arşiv durum geçişleri satır kilidi ve veritabanı transaction'ı içinde yapılır. Aynı varlığın iki kullanıcıya eşzamanlı zimmetlenmesi önlenir.
- Hareket geçmişi aktör/çalışan bilgisi taşır; çalışan işlemleri ayrı denetim tablosuna kaydedilir. Yönetici denetim kaydını sayfalı görebilir.
- Sayfalı varlık CSV dışa aktarımında hücre değerleri Excel formül enjeksiyonuna karşı nötrlenir; `X-Has-More` ve `X-Total-Count` başlıkları sonraki sayfa gerekip gerekmediğini gösterir. Arayüzdeki tam dışa aktarma ayrı bir akış uç noktası kullanır; tüm dosyayı sunucu belleğinde biriktirmeden kayıtları yazar.
- Varlık listesi sayfa başına 25/50/100/200 kayıt gösterir; sütun başlıklarından etiket, kategori, marka/model, çalışan, konum, durum ve güncellenme tarihine göre artan/azalan sıralanır; aynı değerlerde etiket ve kayıt kimliği sabit sıra sağlar. CSV dışa aktarımı da seçili sıralamayı korur. Arama yanında durum, kategori, marka, model, zimmetli çalışan ve tam konum filtrelerini destekler. Varlık listesindeki kategori/marka/model/çalışan/konum bağlantıları ilişkili kayıt görünümünü açar; çalışan listesindeki departman bağlantısı aynı departmandaki kişileri süzer. CSV dışa aktarımı da ekrandaki bu filtreleri uygular. Veritabanı durum, zimmet ve negatif alış bedeli gibi tutarsızlıkları da kontrol eder.

## Yerel yapılandırma değişkenleri

- `ConnectionStrings__EnvanterTR`: PostgreSQL bağlantısı; parola kaynak dosyaya yazılmaz.
- `Setup__BootstrapToken`: kurulum aracı tarafından tek kullanımlık şekilde üretilen ilk yönetici kurulum anahtarı. Kurulum bitince ortamdan kaldırılmalıdır.
- `Email__PublicBaseUrl`: e-posta bağlantılarında kullanılacak uygulama kök adresi; üretimde HTTPS zorunludur.
- `Email__FromAddress`, `Email__FromName`: parola yenileme e-postasının göndereni.
- `Email__Smtp__Host`, `Email__Smtp__Port`, `Email__Smtp__EnableSsl`, `Email__Smtp__Username`, `Email__Smtp__Password`: SMTP sunucu ayarları. Parola yalnızca gizli ortam değişkeni/secret store üzerinden verilmelidir; kaynak dosyaya yazılmamalıdır.
- Üretimde HTTPS zorunludur. Geliştirme ortamında HTTP yalnızca yerel geliştirme için kullanılabilir.

Rol politikaları ayrı tutulur: varlık okuma, varlık değiştirme, çalışan görüntüleme/yönetimi ve şirket ayarları. Şirket sahibi ve yöneticiler uygulama kullanıcılarını görebilir ve e-posta daveti gönderebilir. Davet etkin deneme/abonelik gerektirir ve `MaxLoginUsers` koltuk sınırını işlem sırasında şirket satırını kilitleyerek denetler; sınır şirket sahibi ve bekleyen davetler dâhil tüm giriş hesapları üzerinden uygulanır. Şirket sahibi rolü kilitlidir; kendi rolü değiştirilemez. Şirket sahibi yönetici atayabilir/kaldırabilir; yöneticiler başka yöneticilerin rolünü değiştiremez. Rol değişiklikleri ve davetler şirket denetim kaydına yazılır. Aynı e-posta için şirketler arası hesap paylaşımı bu davet akışında yapılmaz. Şirket sahibini devretme akışı henüz tamamlanmamıştır.

## API sözleşmesi (oturum açma akışı eklendikten sonra)

- `GET /api/assets?q=&status=&categoryId=&brandId=&modelId=&employeeId=&location=&sortBy=tag&sortDirection=asc&page=1&pageSize=50`
- `GET /api/assets/{id}`
  - Yanıt, şirket kapsamındaki varlık alanlarını, kategori/marka/model/çalışan adlarını ve en yeniden eskiye hareket listesini içerir. Hareketlerde varsa çalışan adı da bulunur; olaylar başka şirket kayıtlarıyla eşleştirilmez.
- `GET /api/employees?q=&department=&managerId=&active=true&page=1&pageSize=50`
- `GET /api/departments` (şirket çalışan kayıtlarındaki departman metinlerini ve aktif/pasif çalışan sayılarını özetler; `people.read`)
- `GET /api/locations` (şirket varlıklarındaki konum metinlerini ve durum sayılarını özetler; `asset.read`)
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
- `GET /api/status` (veritabanı bağlantısı, migration varlığı ve bekleyen migration sayısı; bağlantı parolasını göstermez)
- `GET /api/session/csrf`
- `POST /api/setup/initial-owner` (`X-Setup-Token` ve CSRF başlığı gerekir; e-posta mevcut hesapta kayıtlıysa mevcut parola doğrulanarak hesap şirkete bağlanabilir, parola/profile dokunulmaz).
- `POST /api/session/login` (`X-CSRF-TOKEN` gerekir)
- `POST /api/session/password/forgot` (genel yanıt; SMTP yapılandırması gerekir)
- `POST /api/session/password/reset` (e-posta, token ve yeni parola; CSRF gerekir)
- `GET /api/session`
- `POST /api/session/logout` (`X-CSRF-TOKEN` gerekir)
- `GET /api/employees` departman filtresi tam eşleşir ve en fazla 160 karakterdir; yönetici kimliğiyle doğrudan raporlayanlar filtrelenebilir.
- `GET /api/employees/{id}?page=1&pageSize=20` çalışanı, yöneticiyi, doğrudan raporlayanları ve sayfalı zimmetli varlıkları döndürür.
- `PATCH /api/employees/{id}/active`
- `POST /api/assets/{id}/assign`
- `POST /api/assets/{id}/return`
- `PATCH /api/assets/{id}/status`
- `GET /api/maintenances?q=&status=&from=&to=&page=1&pageSize=50` (şirket kapsamlı arama ve filtreleme; varlık/etiket/servis/not, durum ve başlangıç tarihine göre; sayfa boyutu en fazla 100)
- `GET /api/assets/{id}/maintenances` (şirket kapsamlı bakım geçmişi, kaydı açan kullanıcıyı içerir)
- `POST /api/assets/{id}/maintenances` (kullanılabilir ve zimmet dışı varlıkta tek açık bakım; tür, servis, tarih, maliyet, notlar; varlığı bakım durumuna alır)
- `POST /api/assets/{assetId}/maintenances/{maintenanceId}/complete` (tamamlanma tarihi/notu; bakım kaydını kapatır ve varlığı kullanılabilir duruma getirir)
- `GET /api/activity?page=1&pageSize=50`
- `GET /api/activity?assetId=&employeeId=&from=&to=&eventType=&q=` (şirket içinde varlık/çalışan/tarih/tür/metin filtreli)
- `GET /api/audit?page=1&pageSize=50&q=&targetType=` (sahip/yönetici; filtreler ve sayfalama)
- `GET /api/members` (sahip/yönetici; şirket üyelikleri ve kullanılabilir roller)
- `POST /api/members/invite` (sahip/yönetici; ad, e-posta ve rol; CSRF + istek sınırı + etkin plan/koltuk kontrolü)
- `PATCH /api/members/{userId}/role` (sahip/yönetici; denetim kaydı üretir)
- `GET /api/assets/export.csv?page=1&pageSize=5000&q=`
- `GET /api/assets/export-all.csv?q=&status=&categoryId=&brandId=&modelId=&employeeId=&location=&sortBy=tag&sortDirection=asc` (bellekte tüm dosyayı oluşturmadan akış halinde tam dışa aktarım; etiket/ad/durum, kategori/marka/model, seri no/konum, çalışan numarası/adı, alış tarihi/tutarı ve para birimi; CSV formül enjeksiyonu için hücre kaçışı uygulanır)
- `POST /api/assets/export-selected.csv` (`asset.read`; CSRF zorunlu; 1–100 şirket varlık kimliği; seçili kayıtları aynı sütun ve formül güvenli CSV biçiminde indirir)
- `POST /api/assets/bulk-status` (`asset.write`; CSRF zorunlu; 1–100 tekrarsız şirket varlık kimliği; durum güncellemesini tek işlemde uygular, zimmetli/arşiv kısıtlarını doğrular ve her değişen varlık için hareket/denetim kaydı oluşturur)
- `GET /api/company/export.json` (şirket sahibi/yöneticisi; JSON biçim sürüm 1, şirket ayarları, çalışanlar, envanter sözlükleri, varlıklar, bakım kayıtları, olaylar, denetim kayıtları ve üyelik rolleri; parola hashleri/kimlik bilgileri içermez; indirilen dosya henüz geri yüklenemez).
- `POST /api/company/import/preview` (şirket sahibi/yöneticisi; CSRF zorunlu; en fazla 32 MiB JSON; biçim/sürüm/şirket kimliği/tekrar eden GUID, alan kuralları ve kayıt bağlantılarını doğrular; dosya adetlerini ve büyük-küçük harf farkını yok sayarak mevcut şirkette aynı çalışan numarası/varlık etiketi/kategori/marka adına sahip kayıt sayılarını döndürür; bu yalnızca olası eşleşme sayısıdır; salt okunur, otomatik eşleştirme veya yazma yapmaz).
- `POST /api/assets/import/preview.csv` (`asset.write`; CSRF zorunlu; en fazla 32 MiB ve 5.000 varlık satırı; noktalı virgülle ayrılmış UTF-8 EnvanterTR CSV'si; başlık/sütun sayısı, alan sınırları, durum/tarih/tutar biçimi, tekil kategori/marka/model/çalışan eşleşmeleri, yinelenen etiket ve mevcut şirket etiket çakışmalarını doğrular; salt okunur).
- `POST /api/assets/import.csv` (`asset.write`; CSRF zorunlu; aynı CSV biçimi ve 5.000 satır sınırı; yalnızca aynı şirkette tekil olarak bulunan kategori/marka/model ve personel numarası+adı eşleşmeleriyle yeni varlık ekler; yinelenen etiket/bozuk ilişki halinde tüm istek reddedilir; her varlık için hareket ve aktarım için denetim kaydı ekler; tüm yazmalar tek veritabanı işlemi içindedir).
- Yerel Compose yedek betikleri (`deploy/local/backup.ps1`, `deploy/local/backup.sh`) uygulama API'si değildir; çalışan geliştirme konteynerinden tüm PostgreSQL örneği ve şirket medya volume'u için elle dosya üretir. Çıktı şifrelenmez ve uygulama içinden geri yüklenemez.
- Şirket panelindeki **Veri ve yedekler** menüsü JSON/CSV dışa aktarımını ve JSON/CSV doğrulamasını sunar. Varlık listesindeki içe aktarma aracı, mevcut sözlüklerle sınırlı CSV varlık eklemesini ve indirilebilir şablonu sunar. CSV alan eşleme, JSON geri yükleme ve yedekleme geçmişi henüz etkin değildir.
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
