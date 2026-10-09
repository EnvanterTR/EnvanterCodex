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

Bu depo müşteri kullanımı için hazır v1 olarak ilan edilmemiştir. Windows geliştirici önizlemesi Docker içindeki .NET 10 SDK ile derlenir ve yalnızca `localhost:8080` üzerinde dinler. Bu çalışma alanında `/api/status` `ready`, veritabanı 64 migration ile güncel ve bekleyen migration sayısı 0'dır; bu yerel sağlık sinyalidir, üretim kabulü değildir. 9 Ekim 2026'da izole PostgreSQL güvenlik/kabul paketi 58/58 geçti ve `tools/Verify-UiLocales.mjs` 1.044 sözlük kaydını beş dil için doğruladı. GitHub Actions, PostgreSQL tenant güvenlik kabulini, Linux dağıtım/yedekleme denetimlerini ve Windows dağıtım/yedekleme denetimlerini çalıştıracak iş akışlarına sahiptir. Güncel kapsam, açık işler ve test kanıtlarının sınırları `docs/PROJECT-STATUS.md`, `docs/PRODUCT-COVERAGE.md` ve `docs/ROADMAP.md` dosyalarındadır. Windows Server/Linux hedef sistem kabulü, düzenli gerçek kurtarma tatbikatları ve bazı müşteri teslimatları tamamlanmadan ürün müşteri kullanımına hazır sayılmaz. Diğer teknik ayrıntılar için `deploy/local/README.md`, `docs/DATA-API.md`, `docs/ROADMAP.md` ve `docs/ARCHITECTURE.md` belgelerine bakın.
