# Tanıtım sitesi önizlemesi

Statik ürün ana sayfası `src/EnvanterTR.PublicSite/index.html` içinde tutulur. Yerel Docker Compose, bunu Nginx üzerinden yalnızca bu bilgisayara bağlı `http://localhost:8081/` adresinde sunar. `http://localhost:8081/giris` müşteri uygulamasına geçiş sağlar; uygulama `http://localhost:8080/` adresinde de doğrudan açıktır.

## Platform yönetiminden gelen veriler

- `/api/plans`, etkin ve yayımlanmış paketleri döndürür. Ana sayfadaki **Paketler** bölümü listeyi yükler; aylık/yıllık/tek seferlik tutarlardan belirlenmiş olanları ve kullanıcı/varlık sınırlarını gösterir. Katalog boşsa hayali fiyat gösterilmez. Plan kartındaki eylem e-posta ile bilgi isteğidir; ödeme, sipariş, fatura ve tahsilat akışı yoktur.
- `/api/site-content`, yayımlanmış metin bloklarını döndürür. Ana sayfa şu anahtarları metin olarak uygular: `home.hero`, `home.features`, `home.pricing`, `home.deployment`, `home.demo`. Her bloğun `title` alanı başlığı, `body` alanı açıklama metnini değiştirir. İçerik HTML olarak çalıştırılmaz.
- Medya dosyası yükleme ve görsel yönetimi henüz yoktur. `mediaPath` alanı kaydedilse de dosyayı yüklemez veya tanıtım sayfasına yerleştirmez.

## Önemli sınır

Bu adres yerel önizlemedir; `envantertr.com` alan adına yayımlanmış değildir. Metin ve plan yönetimi arayüzü içerik kataloglarına bağlanmıştır fakat satın alma/tahsilat, demo rezervasyonu ve üretim alan adı/DNS dağıtımı henüz tamamlanmamıştır. Yerel adresler şirket dışından erişime açılmaz.
