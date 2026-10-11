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
- `docs/PLATFORM-ADMIN.md`: EnvanterTR ürün sahibinin platform admin kapsamı ve açık işleri.
- `docs/ROADMAP.md` ve `docs/PRODUCT-COVERAGE.md`: tamamlanan alanlar, açık v1 işleri ve kabul sınırları.

Ürün; şirket/rol kapsamlı varlık ve çalışan yaşam döngülerini, aksesuar/sarf/bileşen/lisans stoklarını, siparişleri, denetim ve bakım kayıtlarını, raporları, CSV/XLSX aktarımını, yazdırılabilir etiketleri, çalışan zimmet formunu ve görsel eklerini kapsayan geliştirme aşamasındaki bir uygulamadır. Kurulum, yedekleme ve geri yükleme için Windows/Linux dağıtım betikleri ile izole doğrulama akışları vardır. Bunlar gerçek Windows Server ve Ubuntu hedeflerinde kurulum, güncelleme ve kurtarma tatbikatlarının yerini tutmaz.

## Geliştirme durumu

Bu depo müşteri kullanımı için hazır v1 olarak ilan edilmemiştir. Windows geliştirici önizlemesi Docker içindeki .NET 10 SDK ile derlenir ve yalnızca `localhost:8080` üzerinde dinler. 11 Ekim 2026 yerel kontrolde `/api/status` `ready`, veritabanı 74 migration ile güncel ve bekleyen migration sayısı 0'dır; bu yerel sağlık sinyalidir, üretim kabulü değildir. İzole PostgreSQL güvenlik/kabul paketi 72/72 geçti; `tools/Verify-UiLocales.mjs` beş dilde 1.361 arayüz girdisini doğruladı. Bunlar, gerçek Windows Server/Ubuntu hedef kabulü veya üretim yayını yerine geçmez. GitHub Actions iş akışı CI ve paket kontrollerini çalıştırır; bu depoda `envantertr.com` için otomatik yayın adımı bulunmuyor. Güncel kapsam, açık işler ve kanıtların sınırları `docs/PROJECT-STATUS.md`, `docs/PRODUCT-COVERAGE.md` ve `docs/ROADMAP.md` dosyalarındadır. Windows Server/Linux hedef sistem kabulü, düzenli gerçek kurtarma tatbikatları ve müşteri teslimatları tamamlanmadan ürün müşteri kullanımına hazır sayılmaz. Diğer teknik ayrıntılar için `deploy/local/README.md`, `docs/DATA-API.md`, `docs/ROADMAP.md` ve `docs/ARCHITECTURE.md` dosyalarına bakın.
