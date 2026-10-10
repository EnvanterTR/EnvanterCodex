# Tanıtım sitesi önizlemesi ve mevcut canlı alan adı

Statik ürün ana sayfası `src/EnvanterTR.PublicSite/index.html` içinde tutulur. Yerel Docker Compose, bunu Nginx üzerinden yalnızca bu bilgisayara bağlı `http://localhost:8081/` adresinde sunar. `http://localhost:8081/giris` müşteri uygulamasına geçiş sağlar; uygulama `http://localhost:8080/` adresinde de doğrudan açıktır.

## Yerel ana sayfa tasarımı (10 Ekim 2026)

Ana sayfanın açılış alanı, kullanıcının paylaştığı incident-management ve engineering-platform örneklerindeki koyu teknoloji görsel yönüne göre yenilendi. Nane yeşili/vurgulu palet, HTML/CSS ile oluşturulmuş etkileşimli ürün arayüzü, yüzen garanti ve zimmet bilgi kartları, kaydırma ilerleme çizgisi, bölümlerde görünür olma hareketleri ve responsive düzen bulunur. Ürün arayüzündeki Genel Bakış, Varlıklar, Kişiler, Aksesuarlar, Konumlar ve Raporlar menüleri tıklanarak sentetik kayıtlarla farklı ekranlar gösterir; hareket akışı animasyonludur ve kullanıcı etkileşiminde durur. Ekrandaki bağlantılar salt okunur yerel demo rotasına gider. Hareket azaltma tercihi desteklenir. SSS'de çalışan erişimi, demo verisi ve kurulum hakkındaki yanıtlar geçici ürün açıklamalarıdır; paket/fiyat taahhüdü değildir. Ana içerikte modül kapsamı, demo sınırı, iletişim ve fiyat açıklamaları korunur. Bu değişiklik yalnızca repodaki yerel önizlemeyi etkiler; canlı Next.js sitesine aktarım değildir.

10 Ekim'de kurulum bölümü genişletildi: Windows x64 kartı ASP.NET Core/.NET 10, PostgreSQL ve Windows Service'i; Ubuntu Linux kartı Docker Engine/Compose v2, Caddy, ASP.NET Core ve PostgreSQL'i açıklar. Ayrı kartlar şirket kullanıcı davetleri ve mevcut yedi rolü, firma/plan/içerik/işlem günlüğü yöneten platform panelini, yedek kapsamı ve AES-256-CBC + HMAC-SHA256/SHA-256 doğrulamasını anlatır. Cloud barındırma için şu an otomatik kayıt/satın alma/abonelik/ödeme olmadığını ve teklifin e-postayla alındığını belirtir. Gerçek müşteri Windows Server/Ubuntu kurulumu, izin matrisinin tamamlanması ve Cloud ticari akışları hâlâ açık iştir.

## Etkileşimli demo menüsü (10 Ekim 2026)

`/demo/` ekranında ürün uygulamasının 10 ana menü grubu ve 48 alt menüsü gösterilir: Envanter, Zimmet ve hareketler, Lisans ve yazılım, Aksesuar ve stok, Kişiler ve ekipler, Konumlar, Bakım ve denetim, Raporlar, Veri ve yedekler, Şirket ayarları; Genel Bakış ve Destek de bulunur. Her alt menü ilişkili örnek ekranı açar. Varlık, aksesuar ve çalışan tablolarında örnek ürün/kişi görselleri, detay pencereleri ve sağda satır eylem düğmeleri bulunur. Düğmeler üzerine gelindiğinde açıklama gösterilir; gerçek veriye yazma, silme ve yedek geri yükleme yapılmaz, eylemler yalnızca demo sonucu verir. Arama, uygun filtreler, CSV indirme ve görsel büyütme sentetik örneklerle çalışır. Sol menü sonundaki Güvenli demo alanı gerçek şirket kayıtlarına erişilmediğini ve demo menüleri/işlemlerinin sınırlı olduğunu açıkça belirtir. Menü masaüstünde kaydırılır; mobilde açılır çekmece olarak kullanılabilir. Şirket alanı kartı yalnızca örnek depolama göstergesidir, gerçek kota değildir. TR/EN/RU/UK/MK arayüz dili, beş renk paleti (mavi/yeşil/mor/kehribar/turkuaz) ve açık-koyu görünüm seçimi tarayıcıda saklanır.

10 Ekim'de canlı `https://envantertr.com/#deploy` sayfası tarayıcıdan okundu. Canlı sayfadaki Windows 11 tek tık kurulum, Active Directory, Hetzner bulut IP'si, Debian paketi, nginx ve lisans/fiyat taahhütleri güncel kaynakta kanıtlanmadığından yerel ürün açıklamalarına kopyalanmadı. Yerel Kurulum bölümü doğrulanabilir kapsamı kullanır: Windows x64 self-contained paket, Ubuntu Docker Compose/Caddy dağıtımı, şirket verisi ayrımı ve doğrulanmış şifreli yedek/geri yükleme araçları; hedef müşteri sunucusu kabulinin ayrıca gerektiğini açıkça belirtir.

## 10 Ekim 2026 canlı yayın durumu

- `https://envantertr.com/` ve `https://www.envantertr.com/` bu repo dışındaki tanıtım sitesini sunar. Bu Next.js kaynak kodu ve dağıtım tanımı bu repoda bulunmuyor.
- Canlı ana sayfa 14 günlük ücretsiz deneme ve sabit aylık fiyatlar gibi bu repodaki ürün durumuyla doğrulanmamış taahhütler içeriyor.
- `https://envantertr.com/demo/` bu repodaki salt okunur demo dosyasını Coolify üzerinden sunar. 10 Ekim 2026'da `bd60bba` (`Clarify demo limitations in sidebar`) dağıtımı HTTPS üzerinden HTTP 200 ile doğrulandı. Demo ve ana sayfa ayrı teslimatlardır: demo bu GitHub deposundaki koddan Coolify webhook dağıtımıyla güncellenir; ana sayfa ayrı Next.js kaynağında kalır.

## Platform yönetiminden gelen veriler

- `/api/plans`, etkin ve yayımlanmış paketleri döndürür. Ana sayfadaki **Paketler** bölümü listeyi yükler; aylık/yıllık/tek seferlik tutarlardan belirlenmiş olanları ve kullanıcı/varlık sınırlarını gösterir. Katalog boşsa hayali fiyat gösterilmez. Plan kartındaki eylem e-posta ile bilgi isteğidir; ödeme, sipariş, fatura ve tahsilat akışı yoktur.
- `/api/site-content`, yayımlanmış metin bloklarını döndürür. Ana sayfa şu anahtarları metin olarak uygular: `home.hero`, `home.features`, `home.pricing`, `home.deployment`, `home.demo`. Her bloğun `title` alanı başlığı, `body` alanı açıklama metnini değiştirir. İçerik HTML olarak çalıştırılmaz.
- Medya dosyası yükleme ve görsel yönetimi henüz yoktur. `mediaPath` alanı kaydedilse de dosyayı yüklemez veya tanıtım sayfasına yerleştirmez.

## Yerel önizleme ve canlı yayın sınırı

8081 adresi yalnızca yerel ana sayfa önizlemesidir. `/demo/` Coolify üzerinden genel erişime açık canlı yayındır. Metin ve plan yönetim arayüzü içerik kataloglarına bağlanmıştır fakat satın alma/tahsilat ve demo rezervasyonu henüz tamamlanmamıştır. Canlı Next.js ana sayfası ve EnvanterTR ürün deposu ayrı teslimat kanallarıdır.
