# Oturum sürekliliği: kesin adayın sunucu kabulü

Kaynak: `4237b5735859cb9c2672ddc5a2f3a5ef22ba566c`. Üretim bu inceleme sırasında hâlâ `124b2d1`; yeni aday yayımlanmış sayılmaz.

- Kesin SHA arşivi sunucuda açıldı. Program.cs Git blob kimliği `35dd570434751f755f858f5c1a170ad80cfcda8a` ile doğrulandı. Önceki c22 adayından ürün/Dockerfile/deploy kaynak farkı yok; yeni SHA ayrıca derlendi.
- `envantertr-staging:4237b57` derleme çıkışı 0. `envtr-stage-web-4237b57` çalışıyor; host port eşlemesi yok. Ağ `envtr-stage-ae875b6` internal. Medya ve veri koruma anahtarları ayrı kopyalar; üretim birimleri bağlanmadı. SMTP, ödeme ve bootstrap değerleri kapalı/boş.
- Ana adres, Santa ve Farma Host başlıklarıyla `/api/status`: ready, 121 migration, 0 bekleyen. Üç hostta anonim varlık GET/PUT/PATCH: 401. CSRF istek belirteci ve Secure çerezi mevcut; belirteç kaydedilmedi.
- İlk HTTP probu Content-Type belirtmediği için PUT 415 aldı. İstek application/json olarak düzeltildi; tüm kontroller çıkış 0 ile geçti. İlk hata kanıtı saklandı, ürün değişikliği yapılmadı.
- Taze SQL: `pre-web-v1-20261026T010407Z.dump`. Rollback: `/var/backups/envantertr/rollback-20261026T010459Z`, 140 MB; dosya/imaj kurtarma provası `-drill-20261026T010559Z` başarılı. SQL ayrı `envantertr_stage_c22ce1c` veritabanına exit-on-error ile yüklendi. Son karşılaştırmada 95 public tablonun satır sayıları üretimle aynı; çıkış 0. Bu, tüm alanların birebir içerik karşılaştırması değildir.
- Güncel CI: [40253374749](https://github.com/EnvanterTR/EnvanterCodex/actions/runs/40253374749). Windows ve Linux security başarılı; PostgreSQL kabul işi son gözlemde sürüyor. Eski c22 CI başarısızlığı yeni sonuç yerine kullanılmadı.

Kanıtlar: `4237-stage-http-pass-20261026.png`, `4237-stage-restore-counts-20261026.png`; çalıştırılan gizli değer içermeyen Python probları aynı klasörde. Özel ortam dosyaları, dump, medya, anahtarlar ve ham müşteri içerikleri sunucuda kaldı.

Kullanıcı şu an Farma MFA adımını yapamıyor; yeni kod talep edilmedi. Santa imza belgesi `ZMB-20261025-a11cc2306` imzasız. İmza/PDF, normal operatör/Farma kabulü ve tam hedef açık. Bu rapor bunları tamamlanmış saymaz.
