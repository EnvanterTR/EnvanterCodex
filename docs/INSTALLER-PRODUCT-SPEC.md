# EnvanterTR kurulum deneyimi

Bu belge üç farklı ürün kullanımını ve müşteriye verilecek kurulum deneyimini birbirinden ayırır. “Kolay kurulum” hedefi mevcut teslimatın hazır olduğu anlamına gelmez.

## Müşterinin seçeceği üç yol

| Yol | Nerede çalışır? | Kullanıcıların erişimi | Kurulum hedefi |
| --- | --- | --- | --- |
| **Windows 11 · bireysel / tek bilgisayar** | Uygulama ve veritabanı kişinin Windows 11 x64 bilgisayarında | Aynı bilgisayardaki kullanıcı | Tek imzalı `Setup.exe`; önkoşul kontrolü, kurulum ve ilk açılış sihirbazı. Bu yol ekip sunucusu değildir. |
| **Windows Server · ofis ekibi** | Uygulama ve veritabanı kurumun Windows Server makinesinde | Ekip üyeleri kendi Windows/macOS/Linux cihazlarından tarayıcıyla; şirkete ve role göre | Bir defa sunucuya kurulum. Kurulum aracı PostgreSQL, TLS sertifikası, ağ erişimi, yedek konumu ve sağlık kontrolünü adım adım açıklamalı; çalışan bilgisayarlarına istemci kurulmaz. |
| **Ubuntu Linux · ofis ekibi** | Uygulama ve veritabanı kurumun Ubuntu sunucusunda Docker Compose ile | Ekip üyeleri tarayıcıyla; şirkete ve role göre | İndirilebilir sürümlü paket ve tek bir başlangıç komutu. Betik sunucu, Docker, DNS/HTTPS ve yedek önkoşullarını sırayla kontrol etmeli; bitişte sağlık ve yedek durumunu göstermeli. |

## Kurulum aracı için kabul ölçütleri

Kurulumu müşteri IT sorumlusu kaynak kodu, SQL veya uzun teknik kurulum kılavuzu okumadan tamamlayabilmeli. Kurulum aracı:

1. İşletim sistemi ve sürümünü, yönetici yetkisini, disk alanını, kullanılabilir portları ve gerekli servisleri önce kontrol etmeli.
2. Kişisel bilgisayar ile ekip sunucusu seçimini başlangıçta açıkça ayırmalı; Windows 11'i çok kullanıcılı ağ sunucusu gibi göstermemeli.
3. Bir önkoşul karşılanmıyorsa ne gerektiğini, nasıl sağlanacağını ve yeniden kontrol düğmesini göstermeli. Var olan PostgreSQL, uygulama, veri klasörü veya yedeği sessizce değiştirmemeli.
4. Kurum kurulumunda HTTPS sertifikası/alan adı, veritabanı parolası, ilk şirket yöneticisi ve yedek saklama hedefini sırları günlüğe yazmadan kurmalı.
5. Kurulumdan sonra API/uygulama sağlık kontrolünü, servis durumunu ve örnek şifreli yedek doğrulamasını göstermeli; hatada kullanıcıya kısa bir hata özeti ve destek için gerekli sansürlenmiş günlük yolunu vermeli.
6. Açık kaldırma ve yükseltme akışı sunmalı. Güncellemeden önce doğrulanmış yedek almalı; kaldırma sırasında kullanıcı verisini silme seçeneği varsayılan olmamalı.
7. İmzalı Windows yayınları ve doğrulanabilir Linux sürüm checksum'ları sunmalı. İmza ve temiz kurulum kabulü olmadan indirmeyi “müşteriye hazır” diye etiketlememeli.

## Mevcut teslimat durumu

- **Windows 11 kişisel:** Uygulama/veritabanını aynı bilgisayara kuran müşteri `Setup.exe` paketi yayımlanmadı. Mevcut Windows yayın paketi self-contained uygulama ve bakım betiklerini içeren ZIP'tir; imzalı sihirbaz değildir.
- **Windows Server ekip:** x64 uygulama ZIP'i ve `Setup.cmd` ile başlatılan yönlendirmeli kurulum akışı vardır. Windows Server 2019/2022, DNS ve TLS sertifikası gerekir. PostgreSQL 17 yoksa ayrı onaydan sonra sabitlenmiş resmi EDB PostgreSQL 17.11 x64 yükleyicisi HTTPS ile indirilir, EDB Authenticode imzası doğrulanır ve etkileşimli sihirbaz açılır; kullanıcı PostgreSQL yönetici parolasını yükleyicide belirler. EnvanterTR parolayı komut satırına veya günlüğe yazmaz; ardından bu parolayı gizli istemle alıp yeni, sınırlı yetkili uygulama rolü/veritabanı hazırlar. Önceden kurulu PostgreSQL 17 kullanılabilir; var olan sürüm/veritabanı üzerine kurulum yapılmaz. DNS, firewall ve TLS sertifikasının sağlanması kurum görevidir. Yeni DB bağlantı sırrı LocalMachine DPAPI ile geçici devam dosyasına kaydedilir; servis veya migration kesilirse aynı DNS/port/sertifika ile yeniden çalıştırma güvenli devam eder. Gerçek Windows Server ilk kurulum ve kurtarma kabulü tamamlanmamıştır.
- **Ubuntu ekip:** Docker Compose `install.sh`, `update.sh`, `backup.sh` ve `restore.sh` vardır. İzole Linux kurulum/geri yükleme kabulleri geçmiştir. Hedef makinede DNS/TLS ve dolu müşteri medya verisiyle kurtarma kabulü, imzalı/sürümlü müşteri teslimatı hâlâ gereklidir.

## Müşteri kitapçığı taslakları (10 Ekim 2026)

- Windows Server: [`INSTALL-WINDOWS-SERVER-GUIDE.md`](INSTALL-WINDOWS-SERVER-GUIDE.md)
- Ubuntu Server: [`INSTALL-UBUNTU-SERVER-GUIDE.md`](INSTALL-UBUNTU-SERVER-GUIDE.md)

Bu taslaklar kurulum öncesi DNS/TLS/ağ hazırlığı, mevcut ilk kurulum sihirbazı, ilk hesaplar, yedek ve hata halinde veri/sır korumasını müşteri IT'sinin takip edebileceği sıraya koyar. Müşteriye dağıtılmadan önce her ekran ve komut gerçek desteklenen Windows Server/Ubuntu hostunda baştan sona uygulanıp geri yükleme kabulinden geçirilmelidir. Windows 11 için ayrı sihirbaz/kitapçık henüz yoktur.

İlk müşteri sürümünden önce üç yol için de paket/kurulum kapsamı, yedekten kurtarma, hata halinde veri koruma, sürüm yükseltme ve hedef işletim sistemi kabulü ayrı ayrı tamamlanmalıdır. Kod imzalama sertifikası veya gerçek Windows hedefi sağlanmadıkça Windows imza/host kabulini tamamlanmış saymayın.
