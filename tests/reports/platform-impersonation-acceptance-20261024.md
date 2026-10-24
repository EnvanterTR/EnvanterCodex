# Süreli platform destek oturumu — 24 Ekim 2026

Kapsam: Hedef 9.5 ve 20.5 için yerel uygulama. Bu rapor gerçek Santa/Farma/Demo3 veya canlı kabulün tamamlandığını söylemez.

## Davranış

- Platform yöneticisi doğrulanmış şirket üyesini ve 8–500 karakterlik gerekçeyi seçer; müşteri parolasını kullanmaz.
- Platform MFA ve son 10 dakika içindeki parola/MFA doğrulaması gerekir. Süre, 15 dakika ve asıl yönetici oturumunun kalan süresiyle sınırlıdır.
- Müşterinin gerçek rolü ve seçilen şirket geçerlidir. Platform claim'leri taşınmaz; diğer şirketler oturum menüsünden çıkarılır.
- Sarı bant gerçek yönetici, müşteri ve bitiş zamanını gösterir. Normal Çıkış ve banttaki dönüş asıl oturumu geri yükler; yeni kritik işlem yeniden doğrulama ister.
- Dönüş anahtarı HttpOnly/SameSite Strict çerezde; yalnız SHA-256 özeti DB'dedir. Bitiş satır kilidiyle tek kullanımlıktır. Orijinal yönetici oturumu ve yetkisi her API isteğinde kontrol edilir.
- Başlangıç/istek/sonuç/bitiş platform audit'inde gerçek yöneticiye bağlanır. Gövde ve sorgu dizesi kaydedilmez.

## Derleme ve API

`platform-impersonation-second-20261024.log`: güncel C# Web build 0 hata/0 uyarı; test build 0 hata ve önceki dört ASP.NET deprecation uyarısı. İzole PostgreSQL gerçek Identity çerezleri kullanıldı. Runtime model/snapshot eşitliği her hazırlıkta doğrulanır.

Son API sonucu: **13/13 başarılı, exit 0**, test süresi 2.2914 dakika — `platform-impersonation-third-20261024.log`. Owner yazması ve DB şirketi/audit aktörü; viewer yazma ve iç içe platform erişimi reddi; yabancı şirket 404; MFA ve güncel doğrulama 428; yönetici yetkisi/MFA/damga/oturum iptali sonrası 401; müşteri üyeliği/şirketi/oturumu iptalinden doğrudan geri dönüş; eksik/yanlış/tüketilmiş dönüş çerezi reddi; asıl oturum süresinin korunması ve yeni adım doğrulaması kontrol edildi. Geçici DB ve ağ temizlendi.

Yeniden üretim: `deploy/testing/run-platform-impersonation-acceptance.ps1`. Son hedefli tekrar, ikinci image'ın güncel C# uygulama derlemesini ve bind edilen son test kaynağını kullanıp test projesini yeniden derler. Son UI kaynakları ayrıca Chromium'da çalıştırılır; tam güncel image CI'ı ayrı kapıdır.

## Chromium ve kaynak kontrolleri

- `platform-impersonation-browser-final-20261024.log`: exit 0. Sarı renk, yönetici/müşteri, CSRF ve her iki çıkış düğmesinin tek dönüş isteği kontrol edildi. Browser JS hatası yok. Gerçek HTML + sentetik HTTP yanıtları kullanıldı.
- `platform-impersonation-admin-browser-second-20261024.log`: exit 0. Gerçek yönetici HTML'inde doğru kullanıcı kimliği, gerekçe, CSRF, iptalde yazma olmaması ve 503 sonrasında gerekçenin korunması; mevcut yönetici senaryoları geçti.
- `platform-impersonation-20261024/support-banner.png`: sentetik ekran görüntüsü görsel olarak incelendi. Üretim verisi içermez.
- `platform-impersonation-source-20261024.log`: 2.279 kontrol envanteri, 2.249 locale girdisi, platform deneyimi/üyelik/oturum kaynak doğrulayıcıları ve diff kontrolü geçti.

## İlk başarısız koşular

İlk API koşusu 3/12: yeni test doğrulama sözleşmesini yanlış alanlarla ve yanlış 204 beklentisiyle çağırıyordu. İkinci koşu 10/12: yeni test var olmayan `/api/categories` yolunu kullanıyordu; mevcut `/api/accessory-categories` ve gerçek şirket tablosuna bağlandı. Davranış kontrolleri kaldırılmadı. İlk admin browser fixture'ında API'nin gerçek üye `id` alanı eksikti; gerçek DTO ile eşlendi. İlk bağımsız browser komutunda modül yanlış dizine bağlanmıştı. İlk hata logları saklandı.

## Yayın sınırı

Yeni migration yalnız eklemeli Up işlemleri içerir; canlıya uygulanmadı. Üretimde ortak kök cookie domain zorunludur. Staging, güncel doğrulanmış yedek/geri dönüş provası, gerçek müşteri ve dış servis kabulü açıktır. Önceki `4e4f255` CI run 40040576315 üç işte başarılıdır (`pr25-ci-4e4f255-20261024.json`); bu yeni değişikliklerin CI sonucu değildir.
