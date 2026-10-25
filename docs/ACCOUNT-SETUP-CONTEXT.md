# Hesap kurulumu ve doğrulama bağlamı — 25 Ekim 2026

Bu dosya kullanıcının konuşmada düzelttiği bağlamı korur; canlı hesap ayarlarının yerine geçmez. Yeni işlemden önce ilgili gerçek ekran yeniden kontrol edilir.

## Kullanıcının kararı ve tarihsel neden

Kullanıcı, Google'daki 2 Adımlı Doğrulama'nın testler sırasında sürekli giriş istenmesi nedeniyle kapatıldığını bildirdi. Bunu yeni keşfedilmiş veya açıklanmamış bir hesap sorunu gibi tekrar anlatma. Son doğrudan Google güvenlik ekranı gözleminde `admin@envantertr.com` hesabı için “2 Adımlı Doğrulama kapalı” ve uygulama şifreleri ekranında “Aradığınız ayar, hesabınızda kullanılamıyor” görüldü. Kullanıcının sonraki “girdim” mesajları platform yönetimi ekranı da içerebilir; bunu Google 2 Adımlı Doğrulama'nın yeniden açıldığına kanıt sayma.

Google hesap doğrulaması ile EnvanterTR platform Authenticator doğrulaması iki ayrı sistemdir. Birindeki oturum veya ayar diğerindeki kritik işlem yetkisini açmaz. Google güvenlik ayarlarını kullanıcının yerine kendiliğinden açıp kapatma.

## SMTP durumu

- Kullanıcının seçtiği gönderici `admin@envantertr.com`.
- Coolify production ayarlarında `smtp.gmail.com`, bu SMTP kullanıcı/gönderici adresi ve `https://envantertr.com` bağlantı kökü kaydedildi. Port 587/TLS açık Compose varsayılanı korunuyor. Bu hazırlık için redeploy yapılmadı.
- Son doğrulamada `EMAIL_SMTP_PASSWORD` alanı boştu. Normal Google veya platform parolası bu alana yazılmadı; parola/OTP/recovery code bu dosyada tutulmaz.
- Uygulama anahtarı eksikliği gerçek davet/parola kurtarma teslimatını engelliyor. Google uygulama anahtarı üretilemiyorsa aynı talimatı sürekli tekrarlama; gerçek hesap koşulunu kontrol et. Kullanıcının telefon/hesap kurulumu gerektiren adımı onun tamamlaması gerekir.
- Google uygulama şifresi için 2 Adımlı Doğrulama önkoşulu: https://support.google.com/accounts/answer/185833?hl=tr.

## Platform ve şirket kabulü

### Google relay alternatifi — kaydedilmemiş taslak

Google Workspace yönetici konsoluna erişim ve Gmail hizmetinin açık olduğu doğrulandı. SMTP relay henüz yapılandırılmamıştı. Sunucu genel IPv4 adresi 169.58.53.189; Google relay 465 TLS ve IPv4 587 STARTTLS bağlantıları TLS 1.3, sertifika/host doğrulaması başarılı, çıkış 0. İlk varsayılan 587 denemesi zaman aşımına uğradı; sonraki başarı bu geçmiş sonucu silmez.

Kayıtlı Apps kullanıcıları, yalnız tek sunucu IP ve zorunlu TLS seçili taslak hazırlandı; SMTP AUTH kapalı. IP ekleme alt penceresi dolduruldu, hiçbir KAYDET basılmadı. Yeni gönderim yetkisi açacağı için eylem anı kullanıcı onayı istendi. Onay gelmeden Google veya Coolify relay ayarı uygulanmaz. Mevcut Coolify Gmail hazırlığı ve çalışan uygulama değişmedi; gerçek teslimat hâlâ kanıtlanmadı. Taslak görseli: tests/reports/google-relay-ip-draft-20261025.png.

- Canlı uygulama 124b2d1, şema 121; gerçek rollout kanıtı release raporunda.
- Santa normal formdan oluşturuldu ve aktif; sahibinin e-postası son DB kontrolünde doğrulanmamıştı. Davet gönderilemedi; giriş tamamlandı sayılmadı.
- Demo3 normal formdan oluşturuldu, silme kabulünde geri alınabilir çöp kutusuna taşındı. Santa/Farma ve ortak kimlik alanları için 82 özel projeksiyon aynı kaldı. Sonraki kullanıcı doğrulamasından sonra Demo3 geri alındı ve normal formdan yeniden etkinleştirildi; üç firma aktif.
- Root oturumu açık olsa bile kritik işlem için ayrı 10 dakikalık güncel kanıt gerekebilir. Parola+MFA ile yeni normal giriş bu kanıtı açar; eski oturumlara kendiliğinden süre verilmez. Süreyi uzatma, MFA'yı atlama veya eski kurtarma kodlarını kullanma.
- Kullanıcı son bekleyen “Yönetici doğrulaması”nı tamamladı; Demo3 geri alma ve erişim açma başarıyla yapıldı. Sonra Demo3'e normal 15 dakikalık destek oturumu açıldı, sarı bant doğrulandı, platforma dönüldü ve başlangıç/bitiş audit kayıtları görüldü. Bu destek erişimi normal owner girişi yerine geçirilmez. Artık eski geri alma kod sorusunu tekrar açma. Destekten dönüş kritik işlem tazeliğini temizler; yeni yazma gerekirse gerçek sunucu durumunu kontrol et.

İlgili kanıtlar: `tests/reports/santa-e2e-20261025/README.md`, `tests/reports/google-smtp-preparation-20261025.md`, `tests/reports/production-asset-put-release-20261025.md`. Geri alma sonradan tamamlanırsa bu durum notu ve release/kabul raporu birlikte güncellenir.
