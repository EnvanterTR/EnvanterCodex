# P0-D — canlı plan ve checkout engelleri — 23 Ekim 2026

## Canlı kanıt

- `GET https://envantertr.com/api/plans` → HTTP 200, `[]`; satışa açık plan yok.
- `GET https://envantertr.com/api/checkout/status` → HTTP 200, `available:false`, provider `null`.
- `CheckoutEndpoints.GetConfigurationReadiness` checkout'u sağlayıcı ayarları ve en az bir etkin, herkese açık, fiyatlı Cloud planı birlikte hazırsa kullanılabilir sayıyor (`src/EnvanterTR.Web/Data/CheckoutEndpoints.cs`). Plan endpoint'i boş olduğu için plan koşulu kesin olarak başarısız.
- Canlı çalışan `web` konteyneri ortamından yalnızca anahtar adı ve doluluk bilgisi okundu; gizli değerler görüntülenmedi. `Iyzipay__ApiKey`, `Iyzipay__SecretKey`, `Iyzipay__CallbackUrl`, `Email__Smtp__Host`, `Email__Smtp__Username`, `Email__Smtp__Password` ve `Email__FromAddress` boş; `Iyzipay__BaseUrl`, SMTP port/SSL ve FromName mevcut.
- Çalışma dalında 15 günlük deneme ve onaylı yıllık Cloud fiyatlarını taslak olarak üreten `AnnualCloudCatalog` bulunuyor (`194f45e`); `origin/main` içinde bu dosya yok. Kod taslakları yayımlanmamış (`IsPublic=false`) kaydeder. Bu canlı GET sonucu bir plan taslağının var olduğunu kanıtlamaz.

## Durum ve sınır

P0-D: **ENGELLENDİ** — canlı satın alma için yayımlanmış Cloud planı, iyzico kimlik bilgileri ve SMTP teslim ayarları eksik. Checkout kapalı kalırken müşteriden fatura/kimlik bilgisi istenmediği doğrulandı. Üretim DB'sine veya Coolify ayarlarına yazılmadı; ödeme başlatılmadı.

Sonraki güvenli adım: plan taslakları ve ödeme akışı için staging ortamı açıp hedefli kabul testi yapmak; ardından iyzico üretim bilgilerini ve SMTP uygulama erişimini Coolify'da gizli değişken olarak tanımlamak. Canlı plan yayımlama ve yeni kod deploy'u hedefteki staging + yedek + geri dönüş koşulları geçmeden yapılmayacak.
