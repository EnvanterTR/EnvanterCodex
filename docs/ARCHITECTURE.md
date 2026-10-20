# Teknik mimari başlangıç kararı

## İlk teknoloji seçimi

### Geçmişin kanıt sınırı (20 Ekim 2026)

- `C:\Users\Lenovo\Desktop\EnvanterTR\snipe-it-8.7.2\snipe-it-8.7.2` deposunun 25 Eylül Git geçmişi, PHP 8.2 / Laravel 12 tabanlı Snipe-IT 8.7.2 kopyasına EnvanterTR SaaS özelliklerinin eklendiğini gösteriyor.
- Bugünkü EnvanterTR web deposu ayrıdır; ilk commit'i `38744c6` / 4 Ekim 2026 ve web projesi ASP.NET Core / .NET 10, EF Core, Npgsql ve PostgreSQL kullanır.
- İncelenen Git ve erişilebilir proje notlarında Snipe/Laravel tabanından ayrı ASP.NET Core deposuna ayrılma kararının sahibi veya gerekçesi kayıtlı bulunmadı. Bu nedenle bu kararı kullanıcıya atfetme.
- Güncel hedef v24.2'ye göre var olan C# uygulamasını kullan, mevcut veriyi koru ve yeni bir teknoloji geçişi yapma. Snipe-IT işlevsel karşılaştırma kaynağıdır; mevcut C# deposunun eski PHP kodundan otomatik dönüştürüldüğü veya lisans incelemesinin tamamlandığı anlamına gelmez.

- Sunucu uygulaması: ASP.NET Core, .NET 10 LTS.
- Arayüz: sunucu tarafından sunulan erişilebilir web arayüzü; ilk ince dilimden sonra modül bazında genişletilecek.
- Veritabanı: PostgreSQL; Windows ve Linux kurulumlarında yerel servis olarak çalışabilir.
- Kurum içi Windows: uygulama Windows Service olarak otomatik başlar ve LAN tarayıcı erişimi verir. Docker Desktop veya IIS zorunlu olmayacak.
- Linux: systemd servisi ve PostgreSQL.
- Barındırılan sürüm: aynı uygulama çekirdeği, kontrollü tenant ayrımı ve otomatik yedekleme.

.NET 10, Microsoft'un yayımladığı destek takviminde LTS olarak listeleniyor; PostgreSQL'in güncel destek belgeleri Windows ve Linux'u kapsıyor. Bu seçim kolay kurulum ve iki işletim sistemi hedefini ortak kod tabanında tutmayı amaçlıyor. Hedef makinelerdeki sürüm ve servis uyumluluğu ayrıca doğrulanmalıdır.

## İlk teslim dilimi

1. Proje kimliği, sağlık uç noktası ve tarayıcıdan açılan EnvanterTR başlangıç sayfası. **Başlangıç iskeleti eklendi ve .NET 10 SDK konteynerinde derlendi.**
2. Tanıtım sitesi ana sayfası taslak arayüzü. **Statik prototip eklendi; form ve satış akışları bağlı değil.**
3. Şirket paneli, varlık/çalışan yönetimi, lisans ve stok katalogları, denetim, bakım, konum transferi, CSV, filtrelenebilir hareket ve yönetim ekranları geliştiriliyor. **Docker yerel ortamı derlenip çalışıyor; kimlik doğrulamalı iş akışlarının uçtan uca kabulü hâlâ sürüyor ve tüm menü iş akışları tamamlanmış değil.**
4. PostgreSQL/EF Core veri katmanı ile şirket, çalışan, kategori, marka, model, varlık, bakım ve olay kayıtları tanımlandı. **İlk migration üretildi ve yerel geliştirme veritabanına uygulandı; uygulama konteynerinde `/api/status` hazır yanıtı doğrulandı.**
5. İlk yönetici kurulumu, parola politikası, kilitleme, CSRF koruması, oturum açma/kapama ve şirket üyeliği claim'i için uç noktalar eklendi. Şirket ve platform kurulum ekranları sunucu anahtarının yapılandırılmış olup olmadığını anahtarı göstermeden bildirir. Aynı e-posta iki rol alanında kullanılacaksa mevcut parola ile hesaba yetki bağlanır; parola değiştirilmez.
6. Varlık/çalışan filtreleme ve ayrıntı, zimmet/iade, durum geçişleri, denetim kaydı, bakımı başlatma/tamamlama, notlu konum transferi ve CSV dışa aktarım uçları eklendi.
7. İlk migration, açıkça çağrılan `--migrate` komutu, çok aşamalı Docker imajı, PostgreSQL kalıcı volume'u ve localhost'a bağlı geliştirme Compose akışı eklendi. Şirket sahibi ilk kurulum sayfası eklendi. **Bu son kullanıcı kurucusu veya üretim dağıtımı değildir.**
8. Çalışanlara isteğe bağlı yönetici ilişkisi için geriye dönük uyumlu, nullable migration eklendi. Yönetici ataması şirket satırında kilitlenerek eşzamanlı döngü oluşturma riski azaltılır; kendine atama ve mevcut hiyerarşi döngüleri reddedilir. Çalışan profilleri doğrudan raporlayanları gösterebilir.

Şirket profilinde ad, iletişim e-postası ve ana renk ayarları `/api/company/profile` ile saklanır. Her şirket üyesi yalnızca kendi şirketine ait ana renk ve logoyu görür (`/api/company/branding` ve `/api/company/branding/logo`); ayar değişikliği ve logo yükleme yalnızca owner/admin rolündedir. Şirket logosu PNG/JPEG/WebP imza kontrolünden geçirilir, rastgele adla şirket kimliğine ayrılmış kalıcı Docker volume'unda tutulur ve 2 MB ile sınırlıdır. Uygulama konteyneri yeniden oluşturulsa bile medya volume'u korunur; veritabanı ve medya için yedekleme/geri yükleme hâlâ ayrıca tamamlanmalıdır.
8. Parola yenileme için SMTP çevre değişkenleri ve token akışı eklendi; posta sağlayıcısında teslim/teslim edilebilirlik doğrulanmadı.
9. Zimmet, iade, çalışan yönetimi, denetim izi ve yetki matrisi uçtan uca çalıştırılıp doğrulanmalı.
10. Statik tanıtım sitesi Compose ortamında ayrı bir Nginx hizmetiyle `http://localhost:8081/` üzerinde sunulur; `/giris` şirket uygulamasına yönlendirilir. Bu yerel önizleme üretim alan adı/DNS yapılandırması değildir.

## Veri güvenliği kuralları

- Kurulum ve güncelleme mevcut kullanıcı verisini silmez veya üzerine boş veritabanı yazmaz.
- Her müşteri kaydı tenant kimliğiyle sınırlandırılır; tenant bağlamı istemciden doğrulanmadan kabul edilmez.
- Veritabanı dış ağa açılmaz; yedekler şifreli ve ana sunucudan ayrı tutulur.
- Geri yükleme öncesi mevcut durumun yedeği alınır ve bütünlük kontrolü yapılır.
- Denetim izi, kullanıcı ve zaman bilgisiyle kritik işlemleri kaydeder.

## Kurulum paketi hedefi

Tek bir imzalı Windows kurulum dosyası bağımlılıkları kurup Windows servisini yapılandırmalıdır. Veritabanı bileşeni kurulumda hazırlanmalı, servis hesabı en az yetkiyle çalışmalı, LAN güvenlik duvarı kuralı açık ve anlaşılır onayla eklenmeli, son ekranda URL ve yönetici oluşturma adımı gösterilmelidir. Önce Windows 11, sonra Windows Server sürümleri için doğrulanmış matris hazırlanır. Linux için ayrı paket/kurulum betiği sağlanır.

## Açık işler

- Geliştirme ana makinesine .NET SDK kurulmadı; uygulama imajı Docker içindeki .NET 10 SDK ile derlenir. Yerel web, tanıtım ve kurulum sayfaları HTTP 200; sağlık uç noktası `ready` yanıtı verdi. Bu kontroller kullanıcı kabulü, iş akışı veya güvenlik testlerinin yerine geçmez.
- EF Core ilk migration'ı yerel, yeni geliştirme veritabanına uygulandı. Tenant kapsamı API sorgularında doğrulanmış oturumdaki `company_id` kimliği üzerinden uygulanıyor; tenant izolasyonu için güvenlik incelemesi sürüyor.
- İlk şirket yöneticisi kurulum sayfası ve açık migration komutu mevcut. Son kullanıcı/IT kurulum sihirbazı, kurulum anahtarı rotasyonu ve üretim ortamında ilk kurulum tamamlanmadı.
- İş akışı uç noktaları ve denetim kayıtları kodlandı; tenant güvenliği, uçtan uca, yük ve yedekten dönüş doğrulaması henüz yapılmadı.
- Kurulum sihirbazı, imzalama, otomatik güncelleme ve geri alma henüz uygulanmadı.
- SaaS ödeme sağlayıcısı, faturalama ve abonelik kuralları henüz uygulanmadı.
