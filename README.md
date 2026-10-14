# EnvanterTR

EnvanterTR, kurumların donanım ve diğer kaynaklarını kaydetmesi, sorumlulara ataması ve yaşam döngülerini izlemesi için geliştirilen bağımsız bir üründür.

## Çalışma alanı

- `src/EnvanterTR.Web`: ASP.NET Core uygulaması, şirket paneli ve API.
- `src/EnvanterTR.Web/wwwroot/platform-admin.html`: platform sahibinin API'lere bağlı yönetim ekranı (altyapı/migration gerektirir).
- `src/EnvanterTR.PublicSite`: tanıtım sitesi ana sayfası prototipi.
- `deploy/local`: Windows geliştirici önizlemesi için uygulama + PostgreSQL Compose kurulumu; yönergeler `deploy/local/README.md` içindedir.
- `docs/PRODUCT-REQUIREMENTS.md`: ürün kapsamı ve kabul ölçütleri.
- `docs/DEMO-GUIDE.md`: yerel önizleme için kısa demo akışı ve doğru kapsam beyanları.
- `docs/PROJECT-STATUS.md`: kapsam tablosuna ve Windows/Linux teslimat durumuna dayalı ilerleme raporu.
- `docs/PRODUCT-COVERAGE.md`: 30 işlev ailesi için kapsam ve eksik iş matrisi.
- `docs/ARCHITECTURE.md`: dağıtım ve teknik mimari kararları.
- `docs/ACCOUNT-RECOVERY.md`: SMTP e-postası çalışmadığında sunucu konsolundan yerel hesap parolası kurtarma.
- `docs/PLATFORM-ADMIN.md`: EnvanterTR ürün sahibinin platform admin kapsamı ve açık işleri.
- `docs/ROADMAP.md` ve `docs/PRODUCT-COVERAGE.md`: tamamlanan alanlar, açık v1 işleri ve kabul sınırları.

Ürün; şirket/rol kapsamlı varlık ve çalışan yaşam döngülerini, aksesuar/sarf/bileşen/lisans stoklarını, siparişleri, denetim ve bakım kayıtlarını, raporları, CSV/XLSX aktarımını, yazdırılabilir etiketleri, çalışan zimmet formunu ve görsel eklerini kapsayan geliştirme aşamasındaki bir uygulamadır. Kurulum, yedekleme ve geri yükleme için Windows/Linux dağıtım betikleri ile izole doğrulama akışları vardır. Bunlar gerçek Windows Server ve Ubuntu hedeflerinde kurulum, güncelleme ve kurtarma tatbikatlarının yerini tutmaz.

## Geliştirme durumu

Bu depo müşteri kullanımı için hazır v1 olarak ilan edilmemiştir. 14 Ekim 2026 kontrolde yerel Docker önizlemesi `localhost:8080/api/status` üzerinden `ready` (91 migration, 0 bekleyen), şirket uygulaması `localhost:8080` ve demo `localhost:8081/demo/` üzerinden HTTP 200 döndü. İzole PostgreSQL 17 kabul regresyonu **142/142**, beş dilde kaynak UI kontrolleri **65/65**, locale denetimi **1.826 girdi** geçti. Bunlar yerel kaynak kabulidir; üretim kabulü değildir. Aynı gün salt okunur üretim kontrollerinde `https://envantertr.com/api/status` HTTP 200 ile `migration-required` (91 toplam, 21 bekleyen), `https://firma.envantertr.com/api/status` ise HTTP 503 döndü. Üretim veritabanı migration/yedek ve geri dönüş hazırlığı, tenant wildcard proxy/DNS/TLS sağlığı ve onaylı canlı geçiş ayrıca çözülmelidir; canlıya dağıtım yapılmadı. GitHub Actions iş akışı CI ve paket kontrollerini çalıştırır; bu depoda `envantertr.com` için otomatik yayın adımı bulunmuyor. Güncel kapsam, açık işler ve kanıtların sınırları `docs/PROJECT-STATUS.md`, `docs/PRODUCT-COVERAGE.md` ve `docs/ROADMAP.md` dosyalarındadır. Gerçek Windows Server/Ubuntu hedef sistem kabulü, düzenli kurtarma tatbikatları ve müşteri teslimatları tamamlanmadan ürün müşteri kullanımına hazır sayılmaz. Diğer teknik ayrıntılar için `deploy/local/README.md`, `docs/DATA-API.md`, `docs/ROADMAP.md` ve `docs/ARCHITECTURE.md` dosyalarına bakın.
