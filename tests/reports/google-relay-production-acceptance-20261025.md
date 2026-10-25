# Google SMTP relay — gerçek üretim teslimat kabulü

25 Ekim 2026. Kullanıcı, tek IPv4 adresi ve TLS ile sınırlı relay kaydı sorusuna “Sana bırakıyorum” yanıtını verdi. Bu eylem anı onayından sonra kural kaydedildi. Google normal hesap doğrulaması istedi; mevcut yetkili hesapla doğrulandı. Yeni credential oluşturulmadı, Google veya platform MFA ayarı değiştirilmedi.

## Gerçek Google ayarı

- EnvanterTR kuruluşunda tek etkin kural: kayıtlı Apps kullanıcıları; yalnız `169.58.53.189`; TLS zorunlu; SMTP AUTH kapalı.
- Google ekranı “SMTP geçiş hizmeti ayarları güncellendi”, kural Etkin/Yerel olarak uygulandı.
- İlk host testinde IPv6 çıkışı Google 550/5.7.1 ile reddedildi; kanıt korundu. IPv6 izni eklenmedi. Mevcut uygulama konteynerinin ağları IPv4; yetkili IPv4 kaynak adresiyle ikinci SMTP testinde sıfır alıcı reddi ve exit 0.
- Google ileti günlüğü test iletisini 19:03:02 TRT'de Gmail posta kutusuna teslim, 1/1 alıcı, kaynak IPv4 ve TLS olarak doğruladı. Bu tek başına uygulama gönderiminin kanıtı sayılmadı.

## Uygulamaya uygulama ve gerçek teslimat

- Coolify production: `smtp-relay.gmail.com`, 587, TLS true, SMTP kullanıcı/parola boş Compose varsayılanı. Gönderici `admin@envantertr.com`, HTTPS public URL korunuyor. Preview değiştirilmedi.
- Aynı onaylı SHA `124b2d1e93af2a13c393d68353c271671c856de8`, CI/staging zaten bu sürümde başarılı. Güncel 145446Z SQL + 145517Z rollback ve ayrı 145609Z dosya/imaj provası + 95 tablo restore sayımı gate'i kullanıldı; bkz. relay-preparation-recovery-20261025.md.
- Coolify dağıtımı `p0flhitgplemaxbk2548ll8a`, 16:03:51 UTC başlangıç, 3m56s, Success/Finished. Yeni web/public/DB son eki `20261025T160415`. Gerçek iki imaj SHA etiketi aynı124; DB healthy.
- Çalışan web içinde sırları göstermeyen exact testler host/port/TLS/boş username/boş password/from/public URL için geçti: `RUNTIME_RELAY_CONFIGURATION_OK`.
- Yayın sonrası apex/Farma/Santa HTTPS health 200 ready, kaynak/canlı121, bekleyen0; `relay-postdeploy-health-20261025.log`.
- Normal `/giris` → Parolamı unuttum → admin adresi → Bağlantı iste akışı kullanıldı. Genel güvenli yanıt döndü; bu tek başına teslimat sayılmadı.
- Google günlüğünde uygulamanın **EnvanterTR hesap parolası yenileme** iletisi 19:10:57 TRT'de alındı ve **19:10:59 TRT Gmail posta kutusuna teslim edildi**, 1/1 alıcı; kaynak `169.58.53.189`, TLS etkin.
- Yenileme bağlantısı açılmadı; parola değiştirilmedi veya reset token'ı okunmadı/kaydedilmedi.

## Kapsam sınırı

### Farma gerçek dış alıcı teslimatı — sonraki kabul

Normal `https://farma.envantertr.com/` → Parolamı unuttum akışında Hedef test hesabı c.belge@hotmail.com adresine tek kurtarma isteği gönderildi. Uygulama genel güvenli yanıtı döndü. Google ileti günlüğünde 25 Ekim19:20:48 TRT, **Teslim Edilen1/1**, Microsoft/Hotmail SMTP52.101.60.70 sunucusuna TLS etkin teslim kaydı görüldü; kaynak169.58.53.189/TLS. Bu dış SMTP sunucusunun kabul kanıtıdır; Hotmail gelen kutusu veya spam klasörü konumu doğrulanmadı. Reset bağlantısı/token okunmadı, parola değişmedi. Kanıt: google-relay-farma-hotmail-delivered-20261025.png.

Santa detayındaki normal Yetkili erişim e-postasını yeniden gönder eylemi mevcut600s kritik yetki dolduğu için Yönetici doğrulaması açtı; henüz davet gönderilmedi. Parola hazırlandı, mevcut kullanıcı sorusunda yalnız güncel kodun sayfada girilmesi isteniyor. MFA veya süre değişmedi. SMTP çalışıyor olsa da Santa aktivasyon/normal owner kabulü tamam sayılmaz.

Bu gerçek SMTP ve uygulama parola yenileme e-postası teslimatını kanıtlar. Santa davet teslimatı/aktivasyonu, normal owner girişleri, tüm Bölüm20, gerçek iyzico ve satış kabulü açık. Deneme özelliği veya ödeme ayrıca açılmadı. Yedek yerel hosttadır; off-host felaket kurtarma iddiası değildir.

Kanıtlar: google-relay-enabled-20261025.png; google-relay-first-ipv6-rejection-20261025.png; google-relay-ipv4-send-accepted-20261025.png; google-relay-mailbox-delivered-20261025.png; google-relay-coolify-deploy-finished-20261025.png; google-relay-runtime-config-20261025.png; google-relay-app-reset-delivered-20261025.png.

TAMAM dediğim 3 kapının (Google relay kaydı, çalışan uygulama ayarı/yayın, gerçek uygulama e-postası teslimi) 3'ü gerçek ekran/komut çıktısıyla doğrulanmıştır.
