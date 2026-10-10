# Tanıtım sitesi önizlemesi ve mevcut canlı alan adı

Statik ürün ana sayfası `src/EnvanterTR.PublicSite/index.html` içinde tutulur. Yerel Docker Compose, bunu Nginx üzerinden yalnızca bu bilgisayara bağlı `http://localhost:8081/` adresinde sunar. `http://localhost:8081/giris` müşteri uygulamasına geçiş sağlar; uygulama `http://localhost:8080/` adresinde de doğrudan açıktır.

## 10 Ekim 2026 canlı alan adı kontrolü

- `https://envantertr.com/` ve `https://www.envantertr.com/` HTTP 200 döndürüyor; yanıt başlığı ve gövdesi Next.js ile üretilen ayrı bir site olduğunu gösteriyor. Bu Next.js kaynak kodu, dağıtım tanımı ve yayın kimlik bilgileri bu repoda veya çalışma alanı ortamında bulunamadı.
- Canlı ana sayfa 14 günlük ücretsiz deneme ve sabit aylık fiyatlar gibi bu repodaki ürün durumuyla doğrulanmamış taahhütler içeriyor. `https://envantertr.com/demo/` ve `https://tes.envantertr.com/` de bu kontrolde ürün uygulaması yerine aynı Next.js tanıtım sayfasını döndürdü.
- Bu repodaki yeni ürün tanıtım sayfası ve etkileşimli salt-okunur örnek demo `localhost:8081` üzerindedir; `envantertr.com` yayınına bağlı değildir. Yerel dosyayı değiştirmek canlı Next.js sitesini güncellemez. Canlı güncelleme için gerçek Next.js kaynak deposu ve o sitenin onaylı dağıtım yöntemi gerekir.

## Platform yönetiminden gelen veriler

- `/api/plans`, etkin ve yayımlanmış paketleri döndürür. Ana sayfadaki **Paketler** bölümü listeyi yükler; aylık/yıllık/tek seferlik tutarlardan belirlenmiş olanları ve kullanıcı/varlık sınırlarını gösterir. Katalog boşsa hayali fiyat gösterilmez. Plan kartındaki eylem e-posta ile bilgi isteğidir; ödeme, sipariş, fatura ve tahsilat akışı yoktur.
- `/api/site-content`, yayımlanmış metin bloklarını döndürür. Ana sayfa şu anahtarları metin olarak uygular: `home.hero`, `home.features`, `home.pricing`, `home.deployment`, `home.demo`. Her bloğun `title` alanı başlığı, `body` alanı açıklama metnini değiştirir. İçerik HTML olarak çalıştırılmaz.
- Medya dosyası yükleme ve görsel yönetimi henüz yoktur. `mediaPath` alanı kaydedilse de dosyayı yüklemez veya tanıtım sayfasına yerleştirmez.

## Yerel yayın sınırı

8081 adresi yalnızca yerel önizlemedir; genel erişime açık canlı alan adı değildir. Metin ve plan yönetim arayüzü içerik kataloglarına bağlanmıştır fakat satın alma/tahsilat ve demo rezervasyonu henüz tamamlanmamıştır. Canlı Next.js yayını ve EnvanterTR uygulama deposu ayrı teslimat kanallarıdır.
