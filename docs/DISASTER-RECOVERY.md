# EnvanterTR olağanüstü durum kurtarma planı

Bu runbook, EnvanterTR sunucusu veya veri diski kaybolduğunda şirket verisini yalıtılmış bir hedefe geri yüklemek için hazırlanmıştır. Yerel geliştirme önizlemesi için değil, `deploy/server` Linux sunucu profili ve `deploy/windows` Windows Server profili içindir. Geri yükleme canlı veriyi değiştirir; hedef sunucuda planlı bakım penceresi açın.

## Yedeğin içeriği ve çevrim dışı kurtarma seti

Şifreli EnvanterTR yedeği PostgreSQL dökümünü, şirket medya deposunu ve ASP.NET Data Protection anahtarlarını içerir. Medya deposunda profil fotoğrafları, envanter görselleri ve şirket dosyaları bulunur. Yedek ayrıca manifest ve dosya SHA-256 değerlerini taşır. Parola, yedek arşivinin içine yazılmaz.

Geri yükleme öncesinde dış şifreli arşiv ve iç medya/anahtar arşivleri sınırlandırılır: en fazla 250.000 girdi, toplam 64 GiB açılmış veri ve 1.024 karakter yol. Mutlak/yukarı çıkan yollar, yinelenen yollar, dosya-dizin çakışmaları ve bağlantı/özel dosya türleri reddedilir. Bu kontroller hem Linux kurtarma önizlemesinde hem Windows geri yüklemesinde yapılır; gerçek sunucuda geri yükleme tatbikatının yerini almaz.

Yedek sunucusundan bağımsız, erişimi sınırlandırılmış bir kasada şu malzemeleri saklayın:

- `.etbackup` arşivi ve ona ait `.sha256` yan dosyası. Arşivi ve yan dosyayı aynı yedekleme işiyle kopyalayın.
- Arşivi açan yedek parolası. Linux sunucudaki `backup.pass` veya Windows DPAPI dosyası tek başına çevrim dışı kurtarma için yeterli değildir.
- Sunucu kaybında uzaktaki kopyaya erişmek gerekiyorsa SFTP özel anahtarı ve sabitlenmiş `known_hosts` dosyası; SMB kullanılıyorsa hedef paylaşım/izin bilgileri.
- Kurtarma için kullanılan EnvanterTR sürüm paketi/kaynak sürümü, kurulum talimatı, alan adı/DNS bilgisi ve varsa TLS sertifikası dışa aktarımı ile ayrı sertifika parolası.
- Hedef işletim sistemi ve PostgreSQL sürümü, özel yapılandırma/entegrasyonlar ve uygulama yöneticisinin acil iletişim bilgileri.

Windows DPAPI bağlantı sırrı ve zamanlanmış yedek parolası makineye bağlıdır; eski makinenin DPAPI dosyalarını yeni makineye kopyalamak onları kurtarmaz. Yeni sunucuda yeni bağlantı sırrı üretin. `.etbackup` dosyasını açmak için eski yedek parolasını kullanın. Data Protection anahtarları yedeğin içinde olduğundan mevcut oturum/korumalı değerleri sürdürebilmek için geri yüklenir.

Yedekleri en az bir sunucu dışı konumda ve bir çevrim dışı/ayrı erişim kontrollü kopyada tutun. Saklama süresi, RPO (kabul edilen veri kaybı aralığı) ve RTO (hedef hizmete dönüş süresi) işletme tarafından belirlenip kaydedilmelidir; günlük yedek tek başına belirli bir RPO/RTO taahhüdü oluşturmaz.

## Olağan işletim kontrolleri

1. Günlük yedek ve sağlık görevlerinin sonucunu kontrol edin; yalnızca dosyanın oluşmasına güvenmeyin. Uygulama yedek görevi parola/HMAC, manifest ve izole PostgreSQL geri yükleme provasını da yapar.
2. Uzak kopyanın aynı dosya adı, SHA-256 yan dosyası ve beklenen güncellikte olduğunu sunucudan bağımsız biçimde kontrol edin.
3. Yeni bir yedek kopyası oluşturulduğunda eski kopyayı silmeden önce hedefte hash doğrulaması tamamlanmalıdır.
4. Yedek parolasını uygulama sunucusundan ayrı kasada yetkili en az iki kişi tarafından erişilebilir şekilde saklayın. Parolayı kaynak depoya, e-postaya veya yedek arşivine koymayın.
5. Üretim öncesi ve önemli sürüm yükseltmelerinden sonra ayrılmış bir kurtarma sunucusunda geri yükleme tatbikatı yapın. Başarıyı yalnız `/api/status` ile değil, kullanıcı girişi, kayıt sayıları ve görsellerin açılmasıyla doğrulayın.

## Linux / Ubuntu sunucu kurtarması

### Sunucu çalışıyor, eski veriye dönülecek

Önce yeni yedeğin SHA-256 değerini doğrulayın:

```bash
cd /var/backups/envantertr
sha256sum -c envantertr-server-YYYYMMDD-HHMMSS-XXXXXXXX.etbackup.sha256
```

Kurulu sunucuda envanteri geri yükleyin. Betik eski volume'ları korur, yeni volume'ları doğrular ve hata halinde eski yapılandırmayı tekrar bağlar:

```bash
sudo bash /opt/envantertr/current/deploy/server/restore.sh \
  /var/backups/envantertr/envantertr-server-YYYYMMDD-HHMMSS-XXXXXXXX.etbackup
```

Ekrandaki uyarıyı okuyup alan adını doğrulayın ve `RESTORE <alan-adı>` yazın. İşlem sırasında uygulama bakıma girer. Betik yedeği çözer, izole veritabanı provasını yapar, mevcut sistemi ayrıca şifreli yedekler, yeni veritabanı/medya/anahtar volume'larını kurar ve sağlık denetiminden sonra başarı bildirir. Hata mesajı verirse yeni volume'ları silmeyin; eski yapılandırma ile hizmet durumunu ve günlükleri inceleyin.

### Sunucu kayboldu, SFTP kopyasından yeni sunucuya kurtarma

1. Desteklenen Ubuntu sunucusunu, Docker/Compose'u, alan adını ve gerekli ağ/DNS erişimini hazırlayın. EnvanterTR'yi aynı ya da yedekle uyumlu yayınla `deploy/server/install.sh` üzerinden kurup `ready` durumuna getirin. Yeni kurulumun `server.env` ve volume bilgilerini ayrıca yedekleyin.
2. Eski yedek parolasını yeni sunucunun root-only parola dosyasına güvenli yoldan koyun; varsayılan kurulumda dosya `/etc/envantertr/backup.pass` ve izin `0600` olmalıdır. Yeni kurulumdaki rastgele yedek parolasıyla eski arşiv açılamaz. Yeni sunucunun yapılandırmasında `BACKUP_PASSPHRASE_FILE` bu dosyayı göstermelidir.
3. Yalıtılmış kurtarma sunucusuna SFTP hedefini, özel anahtarı ve sabitlenmiş host anahtarını root-only olarak sağlayın. Uzak yedeği indirme/doğrulama aracı mevcut kurulum ağacındadır:

```bash
sudo bash /opt/envantertr/current/deploy/server/fetch-sftp-backup.sh \
  envantertr-server-YYYYMMDD-HHMMSS-XXXXXXXX.etbackup
```

4. Araç arşivi indirir, SHA-256 ve parola/HMAC doğrulamasını, izole PostgreSQL/medya/anahtar provasını tamamlar. Ardından yukarıdaki `restore.sh` komutunu doğrulanmış yerel arşiv üzerinde çalıştırın.
5. DNS/TLS, SMTP, webhook ve varsa dış entegrasyonları yeni sunucuya göre kontrol edin. Eski sunucuyu kurtarma tamamlanana kadar yeniden ağa bağlamayın; eski ve yeni sunucuyu aynı veritabanı/alan adıyla eş zamanlı çalıştırmayın.

SFTP hedefi kullanılamıyorsa yedek arşivini ve yan dosyasını çevrim dışı kopyadan alın. Yedek parola dosyasının izinlerini ve sahipliğini kontrol edin; parola yoksa şifreli arşiv kurtarılamaz.

## Windows Server kurtarması

1. Windows Server, PostgreSQL 17, EnvanterTR yayın paketi, alan adı ve TLS sertifikasını hazırlayın. Sertifika özel anahtarına `NT AUTHORITY\LOCAL SERVICE` hesabının erişebildiğini doğrulayın. Yeni makinede uygulama yapılandırmasını/DB bağlantısını yeni LocalMachine DPAPI sırrıyla oluşturun; eski makinenin DPAPI dosyalarını taşımayın.
2. EnvanterTR servisini ve veritabanını çalışır hale getirin. Restore betiği uygulama servisinin çalışıyor olmasını, mevcut veritabanının configured rol tarafından yönetilebilmesini ve PostgreSQL rolünün `CREATEDB` iznini ister.
3. `.etbackup` ve `.sha256` dosyalarını güvenli konuma alın. Dosyanın hash'ini yan dosyayla doğrulayın. PowerShell örneği:

```powershell
$backup = 'C:\ProgramData\EnvanterTR\backups\envantertr-YYYYMMDD-HHMMSS-XXXXXXXX.etbackup'
$expectedParts = ((Get-Content -LiteralPath "$backup.sha256" -Raw).Trim()) -split '\s+', 2
$actualHash = (Get-FileHash -LiteralPath $backup -Algorithm SHA256).Hash
if ($expectedParts.Count -ne 2 -or $expectedParts[0] -notmatch '^[0-9a-fA-F]{64}$' -or
    $expectedParts[1].TrimStart('*') -cne [IO.Path]::GetFileName($backup) -or
    $expectedParts[0] -ine $actualHash) { throw 'Yedek SHA-256 yan dosyası doğrulanamadı.' }
Write-Output 'Yedek SHA-256 doğrulandı.'
```

Hash doğrulanmazsa geri yüklemeyi başlatmayın. Yönetici PowerShell'de yayın paketindeki aracı çalıştırın:

```powershell
Set-Location 'C:\recovery\envantertr-windows-publish'
./restore.ps1 -BackupPath $backup `
  -InstallRoot "$env:ProgramFiles\EnvanterTR" `
  -DataRoot "$env:ProgramData\EnvanterTR"
```

Yedek parolasını gizli isteme girin; eski DPAPI parolasını yeni sunucuya taşımayın. Betik arşiv/HMAC/manifest doğrulamasını ve izole PostgreSQL provasını canlı veriye dokunmadan yapar. Ardından mevcut sistemi ayrıca yedekler, açık `RESTORE <veritabanı-adı>` onayı ister, veritabanı/medya/Data Protection anahtarlarını değiştirir ve sağlık kontrolü başarısız olursa eski veriye geri döner. Eski veritabanı ve klasörleri doğrulamadan silmeyin.

Yeni makineye eski TLS sertifikası taşınamıyorsa alan adı için yeni sertifika hazırlayıp Production yapılandırmasını güncelleyin; sertifika yenilenmeden hizmeti internetten açmayın. DNS, firewall, servis olay günlüğü ve SMTP gibi dış hizmetleri ayrıca kontrol edin.

## Geri yükleme sonrası kabul listesi

- `https://<alan-adı>/api/status` `ready` ve `pendingMigrationCount: 0` döndürür.
- En az bir şirket sahibi/yönetici giriş yapabilir; şirket adı ve beklenen varlık/çalışan sayıları görünür.
- Önceki yedekte bulunan bir profil fotoğrafı, bir envanter resmi ve bir belge açılır. Fotoğraf yoksa medya volume/klasörünü ve ilgili kaydın dosya ilişkisini inceleyin.
- Şirket üyeliği, rol/yetkiler, oturum ve şirket sınırları beklenen şekilde çalışır.
- Yedek/sağlık zamanlayıcısı ve server dışı hedef tekrar yapılandırılır; yeni yedek alınır ve hedefte yeniden hash doğrulanır.
- Eski veritabanı/volume/klasör, kaynak arşiv ve kurtarma günlükleri saklama politikasına göre gözden geçirilmeden silinmez.

Yerel Compose geliştirme verisinde şifreli arşiv, HMAC/ZIP doğrulaması, güvenli açma ve izole PostgreSQL + medya + Data Protection anahtar geri yükleme provasını tekrarlamak için depo kökünden `./tools/Verify-LocalRecovery.ps1` çalıştırılabilir. Betik yedek öncesi dokuz temel tablonun kayıt adetlerini alır; medya ve anahtar volume'larına benzersiz geçici dosyalar koyar; izole geri yüklemeden sonra kayıt adetlerini ve iki dosyanın SHA-256 değerini karşılaştırır. Yalnızca geçici parola/klasör ve kalıcı volume bağlamayan prova konteyneri kullanır; `finally` temizliği geçici volume dosyalarını, parolayı, açılmış arşivi ve konteyneri kaldırır. 11 Ekim 2026'da bu bilgisayarda 74 migration, medya ve anahtar dosyası, veritabanı adetleri ve şifreli arşiv doğrulandı. Bu geliştirme kabulü gerçek Windows Server/Ubuntu veya uzak hedef kurtarma tatbikatı değildir.

## Açık doğrulama

Bu belge gerçek hedef sunucu tatbikatının yerine geçmez. Windows Server ve Ubuntu üzerinde ayrı, yalıtılmış hedeflere tam kurtarma; uzak kopya erişimi; fotoğraf kontrolleri; DNS/TLS; geri dönüş davranışı; süre ölçümü ve işletme imzası henüz tamamlanmış kabul edilmemelidir. Tatbikat kaydına tarih, sürüm, yedek adı/hash'i, ölçülen RTO, gözlenen veri kaybı/RPO ve bulunan aksaklıklar eklenmelidir.
