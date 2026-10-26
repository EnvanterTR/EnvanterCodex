# Yayın sonrası 24 saatlik gözlem planı

## Güncel yayın: 26 Ekim 2026

`4237b5735859cb9c2672ddc5a2f3a5ef22ba566c`, Coolify `wbhegg8zyeiziogdpr401ycm`, Success/Finished. Gerçek web/public SHA ve DB health doğrulandı. Doğrulama kaydı 01:55:56 UTC; tam bitiş saniyesi ayrıca çıkarılmadı. İlk yayın sonrası üç-host sağlık, görsel bütünlük ve kapalı ticari sınır kontrolleri başarılıdır. Henüz 24 saat tamamlandı iddiası yoktur. Kanıt: `tests/reports/production-session-continuity-release-20261026.md`. Güncel geri dönüş imajı **124b2d1**, şema **121** korunur; aşağıdaki 124→6198 geri dönüş kaydı önceki yayın içindir.

Hedef 15.10 için uygulanacak plan. Planın bulunması, 24 saatlik gözlemin tamamlandığı anlamına gelmez. Yeni sürümün Coolify `Success / Finished` zamanı **T0** olarak kaydedilir; gerçek imaj SHA'sı ve dış HTTPS sağlığı ayrıca doğrulanır.

## Yayın kaydı ve sorumluluk

- Yayını yürüten operatör, T0, kesin SHA, CI sonucu, staging kabulü, güncel yedek/restore ve geri dönüş paketi yollarını aynı release raporuna yazar.
- 124b2d1 sürümü için önceki sürüm 6198ef6; yayın öncesi tazelenmiş geri dönüş paketi `/var/backups/envantertr/rollback-20261025T080518Z`. Şema 121; bu sürüm yeni migration içermez. Daha sonraki yayınlarda bu yollar güncel paketle değiştirilir.
- Kontrolü yapan operatör her gözlem zamanını ve sonucu kaydeder. Sürekli çalışan izleyici veya nöbetçi atanmamışsa aradaki süre gözlenmiş sayılmaz. Bu belge bir zamanlanmış görev oluşturmaz.

## Kontrol takvimi

| Zaman | Kontrol |
| --- | --- |
| T0 ve T0+5 dakika | Kesin web/public SHA, DB sağlığı, üç HTTPS health, katalog, kapalı satış/deneme sınırları, ana sayfa görselleri |
| İlk 60 dakika | Her 5 dakikada HTTPS health; her 15 dakikada konteyner yeniden başlama sayıları ve yeni hata kayıtları |
| 1–24 saat | Her 15 dakikada HTTPS health; her saat katalog ve satış/deneme durumları, konteyner/DB durumu |
| T0+24 saat | Son kontrol, gözlem boşlukları, hata/olay listesi, kalan ticari ve müşteri kabul kapıları |

## Salt okunur dış kontroller

Repo kökünden, UTC zamanlı ayrı dosyaya kaydedilir:

```powershell
$checkStamp = [DateTimeOffset]::UtcNow.ToString('yyyyMMddTHHmmssZ')
node tools/Check-CoolifyProduction.mjs https://envantertr.com/api/status https://farma.envantertr.com/api/status https://santa.envantertr.com/api/status *> "tests/reports/health-$checkStamp.log"
$healthExit = $LASTEXITCODE
```

Health için her hostta HTTP 200, `ready`, bekleyen migration 0 ve beklenen kaynak şema sayısı aranır. Santa host sağlığı şirket hesabının varlığını kanıtlamaz. Sunucu bağlantısında sertifika doğrulaması kapatılmaz.

Katalog/ticari sınır kontrolü ayrı koşulur:

```powershell
node tools/Check-CoolifyProduction.mjs *> "tests/reports/commercial-$checkStamp.log"
$commercialExit = $LASTEXITCODE
Invoke-RestMethod 'https://envantertr.com/api/trials/status' | ConvertTo-Json -Compress
```

Mevcut onaylı başlangıç durumu: **8** yayımlı yıllık Cloud planı; checkout `available:false`, deneme `available:false`. Ticari kontrolün bu nedenle exit 1 vermesi uygulama kesintisi değildir; satış kabulü henüz geçilmemiştir ve ödeme akışı kapalı bırakılmalıdır. Yapılandırma hazır görünmesi gerçek ödeme/e-posta kabulü yerine geçmez. Herhangi bir checkout veya deneme durumunun beklenmedik biçimde açılması ayrıca araştırılır.

## Sunucu kontrolleri ve kanıt gizliliği

Coolify'de gerçek çalışan konteynerler üzerinden imaj SHA, durum, yeniden başlama sayısı, DB health ve yeni uygulama/proxy hataları incelenir. Eski konteyner adları tekrar kullanılmadan önce canlı envanter doğrulanır. Kayıtlar UTC zamanlarıyla ve hata sayısı/türüyle özetlenir; cookie, token, parola, özel environment, ham müşteri satırı ve bağlantı dizeleri rapora konmaz. Yönetim hesabı sürekli canlı tutulmak veya MFA süresi uzatılmak zorunda değildir; anonim sağlık kontrolleri MFA gerektirmez.

## Müdahale eşikleri

- İlk başarısız sağlık kontrolünde 30–60 saniye içinde ikinci bağımsız kontrol yapılır; DNS/TLS/proxy/uygulama/DB ayrımı kaydedilir. Tek bir gözlem zaman aşımı, dağıtımın durduğu veya başarısız olduğu anlamına gelmez.
- İki ardışık health başarısızlığı, yeniden başlayan uygulama/sağlıksız DB, bekleyen migration veya sürekli yeni 5xx: yeni yayını durdur, hata ve zaman aralığını kaydet, mevcut release raporundaki geri dönüş planını uygula.
- Yabancı şirket verisi görünmesi, yetkisiz yazma veya yanlış firmaya e-posta/ödeme bağlanması: kritik kabul hatası; diğer kabul yazmalarını derhal durdur ve olayı P0 olarak ele al.
- Eksik sağlayıcı nedeniyle kapalı ödeme/deneme: var olan P0 ticari engeli olarak kalır; sistemi satılabilir ilan etme.

## Geri dönüş sınırı

124b2d1 kodundan 6198ef6 imajlarına geri dönüşte şema 121 korunur; yıkıcı `Down`, DropColumn veya DropTable çalıştırılmaz. Doğrulanmış özel paketteki medya/anahtarlar gerekirse ayrı dizinde tekrar prova edilir. DB restore, yayın sonrasında oluşan veriyi kaybettirebileceğinden otomatik yapılmaz; önce yeni kurtarma yedeği, etki değerlendirmesi ve tutarlılık planı gerekir. Sorunu gizlemek için yeni müşteri verisi silinmez veya MFA kapatılmaz.

## Kapanış kaydı

24 saat sonunda gerçek kontrol zamanları, eksik aralıklar, her olayın sonucu ve son sağlık durumu release raporuna eklenir. Gözlenmeyen zaman dilimi tamamlandı sayılmaz. SMTP/ödeme ve Bölüm 20 kabulü bitmemişse 24 saatlik sağlık başarısı V1 veya satış kabulünü kapatmaz.

Başlangıç kanıtı: `tests/reports/pre-put-release-health-baseline-20261025.log` üç host için 200/ready/121/0, exit 0. Bu **yayın öncesi tek anlık** kontroldür; yeni yayın sonrası 24 saatlik kayıt değildir.
