# Müşteri satın alma sonrası portal ve MFA

Tarih: 20 Ekim 2026

## İstenen müşteri akışı

Örnek müşteri şirketi, yetkili e-posta `müşteri yetkilisinin e-posta adresi`, portal slug `farma`.

1. Müşteri satış/kayıt sayfasında şirket ve yetkili bilgilerini girer, uygun planı seçer ve iyzico üzerinden ödemeyi tamamlar.
2. Ödeme sağlayıcıdan doğrulanınca sistem firma tenant'ını, owner üyeliğini ve aktif aboneliği idempotent biçimde açar.
3. Yeni kullanıcı parola belirleme bağlantısı alır; e-postada `https://farma.envantertr.com/` şirket portalı ayrıca gösterilir. Var olan hesabın e-postasında da aynı portal bağlantısı yer alır.
4. İlk girişte şirket MFA politikası nedeniyle QR ile Authenticator kaydı istenir. Kullanıcı QR'ı Google/Microsoft Authenticator veya uyumlu TOTP uygulamasıyla tarar, güncel 6 haneli kodu doğrular ve tek kullanımlık kurtarma kodlarını güvenli yere kaydeder.
5. MFA etkinleştikten sonra her yeni girişte Authenticator kodu veya kullanılmamış kurtarma kodu istenir. Oturum süresi boyunca devam eden gezintide her istekte kod sorulmaz.

## Uygulanan kod

- Portal URL'si tenant slug doğrulamasından sonra `TenantHostResolver.BuildPortalUrl` ile sabit HTTPS altında üretiliyor.
- Paid checkout ile açılan yeni şirket `SecurityRequireMfa = true` alıyor.
- Checkout sonrasındaki yeni kullanıcı daveti ve mevcut kullanıcı erişim bildirimi müşteri portal linkini içeriyor.
- Ödeme onay sayfası doğrudan tenant portal linkini gösteriyor; webhook önce, tarayıcı geri dönüşü sonra geldiğinde de tekrar işleme durumu doğru anlatılıyor.
- Sayfa e-posta teslimini kesin varsaymıyor: aynı işlemde SMTP kabul edildiyse gönderildi diyor; idempotent geri dönüşte daha önce gönderilmiş olabileceğini belirtiyor ve parola yenileme yolunu gösteriyor.
- E-posta gövdesinde tenant portal adresi bulunuyor.

## Doğrulama

- İzole Docker PostgreSQL kabul testi: **6/6 geçti** (`customer-purchase-flow-finalverified-20261020.log`).
- `Public_checkout_uses_server_price_verifies_provider_and_provisions_once`: ödeme sonrası şirket/abonelik/owner, zorunlu MFA, idempotent geri dönüş sayfası ve davet e-postasındaki portal URL'si doğrulandı.
- Tenant portal URL testleri: güvenli slug/domain senaryoları geçti.
- Web ve test derlemesi: 0 warning, 0 error.

## Canlı sınır

Portal/MFA onboarding değişikliği PR #5 içinde; GitHub CI sonucu bekleniyor ve canlıya alınmadı. Salt okunur canlı kontrolünde `/api/checkout/status` `available:false`, `/api/plans` boş liste döndürüyor. Bu nedenle örnek bir müşteri şu anda satın alma yapıp giriş başlatamaz. İyzico üretim ayarı, yayımlanmış planlar ve SMTP e-posta teslimi tamamlanıp kabul edilmeden canlı satış akışı çalışıyor denemez.
