# EnvanterTR

EnvanterTR, kurumların donanım ve diğer kaynaklarını kaydetmesi, sorumlulara ataması ve yaşam döngülerini izlemesi için geliştirilen bağımsız bir üründür.

## Çalışma alanı

- `src/EnvanterTR.Web`: ASP.NET Core başlangıç noktası ve müşteri paneli görsel prototipi.
- `src/EnvanterTR.Web/wwwroot/platform-admin.html`: platform sahibinin API'lere bağlı yönetim ekranı (altyapı/migration gerektirir).
- `src/EnvanterTR.PublicSite`: tanıtım sitesi ana sayfası prototipi.
- `deploy/local`: Windows geliştirici önizlemesi için uygulama + PostgreSQL Compose kurulumu; yönergeler `deploy/local/README.md` içindedir.
- `docs/PRODUCT-REQUIREMENTS.md`: ürün kapsamı ve kabul ölçütleri.
- `docs/DEMO-GUIDE.md`: yerel önizleme için kısa demo akışı ve doğru kapsam beyanları.
- `docs/PROJECT-STATUS.md`: kapsam tablosuna ve Windows/Linux teslimat durumuna dayalı ilerleme raporu.
- `docs/PRODUCT-COVERAGE.md`: 30 işlev ailesi için kapsam ve eksik iş matrisi.
- `docs/ARCHITECTURE.md`: dağıtım ve teknik mimari kararları.
- `docs/PLATFORM-ADMIN.md`: EnvanterTR ürün sahibinin platform admin kapsamı ve açık işleri.

Müşteri paneli; giriş, canlı varlık listesi ve ayrıntısı, çalışan kayıtları, zimmet/iade, durum değişikliği, hareket/denetim geçmişi, CSV ve şirket kullanıcı daveti/rol yönetimi için API'lere bağlanmıştır. İlk EF Core şeması yerel PostgreSQL geliştirme volume'una uygulandı; veritabanı konteyneri sağlıklı ve `/api/status` hazır yanıtı veriyor. İlk şirket sahibi için kurulum sayfası eklendi. Geliştirme Compose paketi localhost'a bağlıdır, Windows son kullanıcı kurucusu değildir. Platform yönetim API temeli paket/fiyat, firma ve abonelik görünümü, demo, uzatma, firma askıya alma, site metinleri ve yönetici denetimini kapsar. Şirket sahibi/yöneticileri için şifre hashleri hariç şirket verilerini JSON olarak indiren dışa aktarma eklendi; geri yükleme aracı henüz yok. Platform admin arayüzünün tamamlanması, satış/ödeme ve self-servis satın alma akışları sıradadır. Kurum içi hedef Windows 11/Windows Server ana makinede servis ve LAN tarayıcı erişimi; Linux sunucu dağıtımı da ürün kapsamındadır.

## Geliştirme durumu

Bu depo geliştirme aşamasındadır; müşteri kullanımı için hazır değildir. Windows geliştirici önizlemesi Docker içindeki .NET 10 SDK ile derlenir. Yerel geliştirme API'si şu an `ready` yanıtı veriyor; 28 migration uygulanmış durumda ve bekleyen migration yok. GitHub Actions, izole PostgreSQL tenant güvenlik kabul paketini Linux'ta ve Windows dağıtım/yedekleme güvenlik kontrollerini Windows runner'da çalıştıracak şekilde tanımlanmıştır; uzak Actions sonucu ilk workflow çalışmasından sonra teyit edilmelidir. Ayrıntılı kapsam ve yaklaşık ilerleme hesabı `docs/PROJECT-STATUS.md` içindedir. Kurum içi kurulumun gerçek Windows Server/Linux kabulü tamamlanmadı. Diğer teknik ayrıntılar için `deploy/local/README.md`, `docs/DATA-API.md`, `docs/ROADMAP.md` ve `docs/ARCHITECTURE.md` belgelerine bakın.
