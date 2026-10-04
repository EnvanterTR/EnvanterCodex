# Yerel geliştirme kurulumu (Windows + Docker Desktop)

Bu paket geliştirici önizlemesi içindir; müşteriye dağıtılacak son Windows kurulum programı değildir. Uygulama `localhost` ile sınırlıdır. PostgreSQL portu Windows ana makinesine yayımlanmaz. Şirket veritabanı Docker'ın adlandırılmış `envantertr_database` volume'unda kalır; konteyneri durdurmak volume'u silmez.

## İlk çalıştırma

PowerShell'i proje klasöründe açın:

```powershell
Set-Location .\deploy\local
if (Test-Path .env) { throw '.env zaten var; parolaları yenilemeyin. Var olan veritabanı bu parolaya bağlıdır.' }
function New-Secret { [guid]::NewGuid().ToString('N') + [guid]::NewGuid().ToString('N') }
@(
  "POSTGRES_PASSWORD=$(New-Secret)"
  "SETUP_BOOTSTRAP_TOKEN=$(New-Secret)"
  "SETUP_PLATFORM_BOOTSTRAP_TOKEN=$(New-Secret)"
  'APP_PORT=8080'
) | Set-Content -LiteralPath .env -Encoding ascii
docker compose up -d database
docker compose run --rm web --migrate
docker compose up -d web public-site
```

İlk çalıştırmada Docker, uygulama ve PostgreSQL imajlarını internetten indirir. Ardından:

- Şirket alanı: <http://localhost:8080/>
- Pazarlama ana sayfası önizlemesi: <http://localhost:8081/>
- Pazarlama sayfasındaki müşteri girişi: <http://localhost:8081/giris> (web uygulamasına aynı yerel adres üzerinden geçer)
- İlk şirket sahibi kurulumu: <http://localhost:8080/setup.html>
- İlk platform sahibi kurulumu: <http://localhost:8080/platform-setup.html>
- Sağlık durumu: <http://localhost:8080/api/status>

Kurulum formundaki ilk şirket anahtarı `SETUP_BOOTSTRAP_TOKEN` değeridir. Yerel kurulumda panoya almak için:

```powershell
(Get-Content .env | Where-Object { $_.StartsWith('SETUP_BOOTSTRAP_TOKEN=') }).Substring('SETUP_BOOTSTRAP_TOKEN='.Length) | Set-Clipboard
```

Formda şirket, yönetici adı, e-posta ve yönetici parolası belirlenir. Aynı e-posta daha önce platform hesabı olarak açılmışsa, mevcut parolayla bu şirkete sahip yetkisi bağlanır; parola veya profil değiştirilmez. Anahtar tarayıcıya kaydedilmez. Bu geliştirme önizlemesi henüz TLS sağlamaz; yalnızca bu bilgisayardan erişilir. Ana makineyi şirket ağına açmak için HTTPS, Windows güvenlik duvarı, TLS sertifikası, yedekleme ve kurulum sihirbazı ayrıca tamamlanmalıdır.

Platform sahibinin kurulum anahtarı `SETUP_PLATFORM_BOOTSTRAP_TOKEN` değeridir. Platform kurulumu önce veya şirket kurulumundan sonra yapılabilir. Aynı e-posta kullanılırsa mevcut hesap parolasıyla var olan hesaba platform yetkisi eklenir; yeni bir ikinci hesap oluşturulmaz. Kurulumdan sonra `.env` içindeki iki kurulum anahtarını kaldırıp web servisini yeniden oluşturun; uygulama bu anahtarlar olmadan da çalışır. Parolalar/anahtarlar kullanıcıya veya dış servise gönderilmez.

## Başlatma, durdurma ve veri saklama

```powershell
docker compose up -d
docker compose ps
docker compose stop
docker compose start
```

Compose projesinin adı `envantertr-product-dev` olarak ayrılmıştır; eski test kurulumlarıyla aynı Docker projesini paylaşmaz. `stop`, `start` veya `docker compose down` veritabanı volume'unu silmez. **`docker compose down --volumes` çalıştırmayın**: bu yerel geliştirme veritabanını kalıcı olarak siler. Uygulama güncellemesinde kaynak kodun değişmesi PostgreSQL volume'unu ezmez. Yeni migration uygulanmadan önce ayrıca yedek alınmalıdır; migration komutu şemayı değiştirir.

`deploy/local/.env` parolaları ve kurulum anahtarlarını içerir; paylaşmayın ve Git'e eklemeyin. `.env.example` yalnızca şablondur.

## Elle yerel yedek alma

Uygulama ve veritabanı çalışırken proje kökünden Windows'ta `pwsh -File deploy/local/backup.ps1`, Linux'ta `bash deploy/local/backup.sh` çalıştırın. İsterseniz ilk argümanla yedeklerin yazılacağı dizini belirtebilirsiniz. Betikler PostgreSQL özel biçimli dökümünü, şirket medya volume'unu ve iki içerik için SHA-256 özetlerini ayrı bir klasöre yazar; geçici konteyner dosyalarını işlem sonunda kaldırır. Yalnızca manifesto ve özetleri tamamlanan yedek klasörü başarılı sayılır; hata durumunda betik kendi oluşturduğu kısmi klasörü temizlemeyi dener. Yedek klasörü Git'e eklenmez.

Bu, yerel Docker Compose geliştirme kurulumu için **elle alınan, şifrelenmemiş bir kopyadır**. Parola hash'leri ve şirket verileri veritabanı dökümünde bulunduğundan klasörü erişimi kısıtlı ayrı bir diske taşıyın ve saklama/şifrelemeyi işletim sistemi veya güvenli yedekleme altyapısıyla sağlayın. PowerShell betiği oluşturduğu klasörde erişimi geçerli kullanıcı, Windows SYSTEM ve yöneticilerle sınırlar; Bash betiği özel dosya izinleriyle oluşturur. Veritabanı ve medya ayrı adımlarda kopyalandığından işlem sırasında yazma yapılmayan sakin bir zamanda alın. Arayüz içinden geri yükleme, otomatik zamanlama, şifreleme, geri yükleme doğrulaması ve üretim/başka kurulum türleri için destek henüz yoktur. Bu nedenle müşteri hizmetinde tek yedek olarak kullanmayın. Geri yükleme desteği gelene kadar komut satırından dökümü elle yüklemeye çalışmayın.
