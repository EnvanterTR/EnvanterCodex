# Windows Server kurulum kitapçığı

> **Sürüm durumu:** Müşteriye yayımlanmış son kurulum kitapçığı değildir. Windows Server 2019/2022 x64 için mevcut `Setup.cmd` akışını sade dille açıklar. Gerçek müşteri sunucusunda ilk kurulum, LAN erişimi, yedek ve geri yükleme kabulü ile imzalı yayın tamamlanmadan indirme bağlantısı olarak sunulmamalıdır.

## Hangi seçeneği kuruyorsunuz?

Bu paket **Windows Server üzerinde ekip kullanımı** içindir. EnvanterTR sunucuya bir kez kurulur ve Windows hizmeti olarak arka planda çalışır. Ekip üyeleri Windows, macOS veya Linux bilgisayarlarından; telefon ve tabletlerden aynı ağdaki HTTPS adresine tarayıcıyla bağlanır. Çalışanların bilgisayarına EnvanterTR uygulaması kurulmaz.

Windows 11 · Tek Bilgisayar seçeneği ayrı bir üründür: yalnızca kurulduğu bilgisayarda kişisel kullanım içindir ve bu sunucu paketinin parçası değildir.

## Kurulumdan önce hazır olması gerekenler

Bu adımlar kurulum sihirbazı başlamadan kurumun DNS, ağ ve sertifika yöneticisi tarafından tamamlanmalıdır:

- Windows Server 2019 veya 2022, 64 bit.
- Sunucuda yönetici yetkisi ve internet erişimi.
- Tarayıcı kullanıcılarının çözdüğü bir DNS adı; örneğin `envanter.sirketiniz.com`. Ad, sunucuya hem sunucudan hem istemci bilgisayarlardan erişilebilmelidir.
- Bu DNS adı için güvenilen, özel anahtarı bulunan TLS sunucu sertifikası. Sertifikayı `Yerel Bilgisayar > Kişisel` deposuna yükleyin; adı sertifikanın SAN alanında bulunmalıdır.
- Windows hizmeti `LocalService` hesabının sertifikanın özel anahtarını okuyabilmesi. Sertifika yönetim konsolunda ilgili sertifikanın **Özel anahtarları yönet** ekranından bu izni verin.
- PostgreSQL 17 için yeterli boş disk. Kurulumda yoksa yardımcı, imzasını doğruladıktan sonra resmi EDB yükleyicisini açabilir.
- Ağ güvenlik duvarında yalnızca gereken HTTPS TCP portunun (varsayılan `8443`) istemci alt ağlarına açılması. Kurulum sihirbazı güvenlik duvarı kuralı eklemez.
- İlk kurulum anahtarlarını ve şifreli yedek parolasını sunucudan ayrı, erişim denetimli bir parola kasasında saklama yöntemi.

> İstemciler sertifika zincirine güvenmiyorsa tarayıcı güvenli bağlantı uyarısı verir. Kurulum sihirbazı DNS, güvenlik duvarı veya sertifika dağıtımını kendiliğinden yapmaz.

## Kurulum

1. Kurumun verdiği Windows x64 ZIP dosyasını indirin. ZIP ile birlikte yayımlanmış SHA-256 değerini yalnızca dosya aktarımını doğrulamak için kullanın; SHA-256 yayıncı kimliğini kanıtlamaz.
2. ZIP'i EnvanterTR için ayrılmış boş bir klasöre çıkarın. `Setup.cmd` dosyasını başlatın ve Windows yönetici iznini onaylayın.
3. Tarayıcı adresi olarak kullanılacak DNS adını, PostgreSQL portunu (genellikle `5432`), HTTPS portunu (genellikle `8443`) ve sertifika listesinden doğru DNS adını taşıyan sertifikayı seçin.
4. Ön kontrol ekranındaki her sonucu okuyun. Hata varsa sihirbazı kapatıp DNS, TLS, servis izni, disk veya port sorununu giderin; ardından aynı `Setup.cmd` dosyasını yeniden açın.
5. PostgreSQL 17 kurulu değilse `INSTALL POSTGRESQL` yazarak resmi EDB kurulumunu başlatın. Açılan EDB penceresinde PostgreSQL yönetici parolasını kendiniz belirleyin ve güvenli kasanıza kaydedin. Bu parola EnvanterTR'ye veya komut satırına aktarılmaz.
6. Ön kontroller tekrar geçince EnvanterTR kurulumunu başlatmak için `INSTALL` yazın. Sihirbaz yalnız yeni `envantertr` veritabanı ve sınırlı yetkili `envantertr_app` rolü oluşturur; var olan veritabanı veya servisin üzerine yazmaz.
7. Migration onayında `MIGRATE` yazın. Kurulum, uygulama servisini açar ve `/api/status` yanıtının `ready` olmasını bekler. Kesinti olursa aynı DNS, port ve sertifikayla `Setup.cmd`'yi tekrar çalıştırın; ilk kurulum güvenli biçimde devam eder.
8. Sihirbazın gösterdiği şirket sahibi ve platform yöneticisi ilk kurulum anahtarlarını güvenli kasaya alın. Tarayıcıda sırasıyla `https://envanter.sirketiniz.com:8443/setup.html` ve `/platform-setup.html` sayfalarını açıp hesapları oluşturun.
9. Her iki hesap da oluşturulduktan sonra ZIP'teki `disable-bootstrap.ps1` dosyasını yönetici PowerShell'den bir kez çalıştırın. Bu adım ilk kurulum anahtarlarını etkin yapılandırmadan kaldırır.
10. Aynı ağdaki başka bir bilgisayar, tablet ve telefondan HTTPS adresini açın; giriş, rol erişimi, varlık listesi ve güvenilir sertifikayı doğrulayın.

## E-posta ve yedek

- Parola kurtarma ve davet e-postaları için SMTP sağlayıcınızın verdiği bilgileri paketteki `configure-email.ps1` akışında girin. Önce kuruma ait bir test alıcısına gönderimi doğrulayın.
- `configure-backup-schedule.ps1` betiği günlük şifreli yedek ve yedek sağlık görevlerini kurar. İlk yedek görevini Görev Zamanlayıcı'dan başlatın; sonucun `PASS` olduğunu ve SHA-256 yan dosyasının üretildiğini doğrulayın.
- Yedek parolasını ayrıca çevrim dışı parola kasasına kaydedin. DPAPI korumalı parola dosyası bu Windows makinesine bağlıdır; sunucu kaybında yedeği tek başına açamaz.
- Şirket dosyaları, PostgreSQL verisi ve Data Protection anahtarları birlikte yedeklenir. Üretim verisine geri yükleme yapmadan önce ayrı ve boş bir kurtarma ortamında geri yükleme provası tamamlanmalıdır.

## Güncelleme ve hata halinde

Güncellemeden önce uygulamanın `/api/status` adresinin `ready` olduğunu doğrulayın. Yeni paketin `update.ps1` akışını kullanın; şema değişikliği onayı istenirse yalnızca doğrulanmış yeni yedek oluşturulduktan sonra devam edin. Migration hatasında veritabanı otomatik geri alınmaz; yedeği ve rollback klasörünü silmeyin.

Kurulum `ready` durumuna ulaşmazsa ve aynı ayarlarla tekrar başlatma da çözmezse şu bilgileri kurum IT sorumlusuna iletin: ekrandaki hata metni, UTC zamanı, Windows sürümü, `EnvanterTR` hizmet durumu, TLS sertifika son kullanma tarihi ve sansürlenmiş olay günlüğü. **Parola, ilk kurulum anahtarı, bağlantı dizesi, yedek parolası veya özel anahtar paylaşmayın.**

## Müşteri sürümüne açılmadan önce gereken kabul

Bu kitapçık hedef sunucuda temiz kurulum, aynı Wi-Fi/LAN'dan HTTPS erişimi, kullanıcı/rol oluşturma, zamanlanmış yedek, medya içeren yedeğin ayrı sunucuda geri yüklenmesi ve güncelleme/kurtarma adımları gerçek Windows Server 2019/2022 ortamında doğrulanınca yayımlanabilir. Bu doğrulama ve kod imzalı müşteri paketi şu an tamamlanmış sayılmaz.
