# Santa Invalid token incelemesi — 25 Ekim 2026

Kullanıcı ilk parola denemesini zayıf bulduğunu, e-postadaki bağlantıyı tekrar açınca Invalid token aldığını bildirdi. Başlangıçta eski detay görünümü doğrulanmamıştı; bunun üzerine ayrı yeni platform sekmesinden sunucu verisi tekrar alındı. Santa sahibi **Doğrulandı** ve Panele gir etkin görüldü. Bekleyen yeni davet reauth iptal edildi; ikinci e-posta gönderilmedi. E-posta gelmedi kullanıcı mesajı bu yeni davet iptaline aittir.

Normal `santa.envantertr.com` girişinde kullanıcının ilk verdiği etkinleştirme parolası kabul edildi ve **Santa Authenticator kurulumu gerekli** ekranına geçildi. İlk form doldurmada autofill üstüne eklenen e-posta tarayıcı e-posta doğrulamasında kaldı; alanlar temizlenince gerçek istek gönderildi. Başarısız parola denemesi olarak sayılmadı. Parolalar, QR/anahtar ve enrollment ticket rapora/commite alınmadı. Bu gerçek parola doğrulamasıdır; MFA tamamlanmadığından normal dashboard girişi henüz tamam değildir.

Başarılı reset token'ı tek kullanımlıdır; eski bağlantının tekrarındaki Invalid token bu sonuçla uyumludur. Kullanıcının ilk ekranda gördüğü zayıflık mesajının kaynağı doğrudan gözlenmedi; sebebi kesin olarak uydurulmadı.

## İzole kabul

`Invitation_email_token_survives_rejected_password_and_activates_once`, gerçek PostgreSQL/Identity ve SMTP yakalayıcısıyla, e-postada oluşturulan gerçek HTML-fragment linkinden token'ı ayrıştırır. Şunları sınar:

- Çok kısa parola ve Identity büyük harf kuralı reddi bağlantıyı tüketmez.
- Reddedilmiş iki denemeden sonra password hash, security stamp ve EmailConfirmed değişmez.
- Aynı e-posta token'ıyla geçerli sentetik parola kabul edilir ve hesap doğrulanır.
- Başarılı aktivasyon sonrası aynı token tekrar kullanılamaz.

İlk kısa parola kapsamı1/1 exit0; genişletilmiş iki ret dalı1/1 exit0, toplam1.1185dk. Bunlar iki bağımsız ürün testi sayılmaz; son kapsam genişletilmiş tek testtir. Kaynaklar/loglar: InvitationPasswordRetryAcceptanceTests.cs, invitation-password-retry-20261025.log, invitation-password-retry-expanded-20261025.log. Test ortamı yalnız invitation-retry-20261025 Compose projesiyle temizlendi. Ürün kodu değiştirilmedi, yeni deploy gerekmedi.

Canlı aktivasyon kanıtı: santa-owner-email-confirmed-20261025.png. QR kurulumunu kullanıcı tamamlayacak; yeni davet/OTP platform sorusu artık gereksizdir. Tam Bölüm20 ve ödeme kabulü açık.
