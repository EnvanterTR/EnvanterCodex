# Gerçek e-posta ve ödeme kabulü

Bu dosya uygulanacak kontrol listesidir; tamamlanmış kabul raporu değildir. Secret değerleri bu dosyaya, sohbete, Git'e veya ekran görüntüsüne yazılmaz.

## Mevcut engel — 24 Ekim 2026

Üretim Coolify kaydında `EMAIL_SMTP_PASSWORD`, `IYZIPAY_API_KEY`, `IYZIPAY_SECRET_KEY` boş olarak doğrulandı. Kanıt: `tests/reports/coolify-saved-provider-config-20261024.md`. Hesapların sağlayıcıda mevcut olup olmadığı henüz bilinmiyor. Yeniden deploy bu bilgileri üretmez.

## Ayar eşlemesi

Değerler ilgili sağlayıcının hesabından alınır. Hesap parolası yerine sağlayıcının bu uygulama için desteklediği gönderim kimliği kullanılır. Desteklenmeyen bir kimlik doğrulama yöntemi varmış gibi varsayılmaz.

| Coolify production değişkeni | Uygulama ayarı | Kaynak / beklenen içerik |
|---|---|---|
| EMAIL_SMTP_HOST | Email:Smtp:Host | Gönderici sağlayıcısının SMTP sunucusu |
| EMAIL_SMTP_PORT | Email:Smtp:Port | Sağlayıcının TLS bağlantı portu |
| EMAIL_SMTP_ENABLE_SSL | Email:Smtp:EnableSsl | TLS etkin; sertifika doğrulaması korunur |
| EMAIL_SMTP_USERNAME | Email:Smtp:Username | Yetkili gönderim hesabı |
| EMAIL_SMTP_PASSWORD | Email:Smtp:Password | Sağlayıcıya uygun gizli gönderim bilgisi |
| EMAIL_FROM_ADDRESS | Email:FromAddress | Kullanıcının seçtiği admin@envantertr.com; sağlayıcıda gönderme yetkisi doğrulanır |
| EMAIL_FROM_NAME | Email:FromName | EnvanterTR |
| EMAIL_PUBLIC_BASE_URL | Email:PublicBaseUrl | Kabul yapılan ortamın doğru HTTPS kök adresi |
| IYZIPAY_API_KEY | Iyzipay:ApiKey | Seçilen sağlayıcı ortamının API anahtarı |
| IYZIPAY_SECRET_KEY | Iyzipay:SecretKey | Aynı sağlayıcı ortamının gizli anahtarı |
| IYZIPAY_BASE_URL | Iyzipay:BaseUrl | Aynı ortamın resmi API adresi |
| IYZIPAY_CALLBACK_URL | Iyzipay:CallbackUrl | Aynı kabul ortamının HTTPS /api/checkout/callback adresi |

Eşlemelerin kaynağı: `deploy/coolify/compose.yaml`. Sandbox anahtarı, gerçek ortam API adresi ve başka ortam callback'i karıştırılmaz. Staging iç ağına bu ayarların eklenmesi tek başına dış bağlantı sağlamaz; mevcut staging dışa kapalıdır. Gerçek sağlayıcı kabulünden önce uygun ağ/HTTPS kurulumu ayrıca yapılmalıdır; ağ izolasyonu sessizce kaldırılmaz.

## Yürütme sırası ve kanıt

1. Seçilen commit ve migration sayısını kaydet. Yeni kaynak değişmişse o commit için CI, staging ve migration kanıtını doğrula. Güncel SQL/medya/anahtar/imaj yedeği ve geri dönüş provası olmadan üretimi değiştirme.
2. Sağlayıcı bilgilerini doğru ortamın Coolify secret alanlarına kullanıcı girer. Sadece alan adları ve doluluk durumları raporlanır. Kaydedilmiş değişiklik, çalışmakta olan konteyner ayarı ve gerçek teslimat üç ayrı doğrulamadır.
3. Onaylanan yıllık Cloud planlarını denetimli yönetim yoluyla yayınla; 15 gün deneme, yıllık toplam tahsilat, kullanıcı limitleri ve vergi açıklamasını doğrula. İstemciden gelen fiyatı ödeme tutarı olarak kabul etme.
4. Gerçek SMTP kabulü için kullanıcının yetkilendirdiği test alıcısını kullan. Satın alma sayfasından e-posta doğrulaması iste: `/api/checkout/email/request` CSRF ile çağrılır. HTTP 202 yalnız SMTP sunucusunun kabulünü gösterir; gerçek gelen kutusu ve doğru HTTPS bağlantısı ayrıca görülmelidir.
5. Alıcı e-postadaki bağlantıyı açıp doğrulamayı tamamlar. Token, URL fragment'i, kod veya e-posta gövdesi rapora kopyalanmaz. Uygulama `/api/checkout/email/confirm` ile e-posta/paket bağlamını doğrular. Bu adım ödeme almaz veya şirket oluşturmaz.
6. Sağlayıcının gerçek sandbox ortamında yıllık satın alma akışını tamamla. Tarayıcı dönüşü tek başına başarı değildir: sunucu tarafı sağlayıcı doğrulaması, sipariş tutarı/para birimi, şirket/owner bağlamı, ödeme kimliği ve tek abonelik birlikte denetlenir. Gerçek para tahsilatı gerektiren son adım kullanıcıya bırakılır.
7. Callback tekrarında tek şirket/abonelik/audit kalmalı; yanlış tutar/para birimi, başarısız ödeme ve başka kiracı siparişine erişim aktivasyon üretmemeli. Mevcut şirket denemeden ücretliye geçerken company ID, portal, üyeler ve envanter korunmalı.
8. Parola yenileme ve Editör davetini gerçek gelen kutusunda ayrıca doğrula. Satın alma doğrulama e-postası, diğer şablonların doğru tenant adresiyle teslim edildiğini kanıtlamaz.
9. Hedef.txt Bölüm 20 Santa/Farma karşılıklı izolasyon, yönetici destek oturumu ve sıfırdan Demo3 provisioning adımlarını tamamla. Her adımın sonucu, maskeli ekran görüntüsü ve gerekli DB/API kanıtı saklanır.

## Kabul sayılmayan durumlar

- Hazır görünen durum endpoint'i, SMTP bağlantı açılması veya yalnız 202 cevabı: gelen kutusu teslimatı değildir.
- Sentetik SMTP/iyzico veya yerel test: gerçek sağlayıcı kabulü değildir.
- DNS/sertifika çalışması: tenant oluşturulduğunu veya izolasyonu kanıtlamaz.
- HTTP 200 callback sayfası: siparişin Provisioned olduğunun kanıtı değildir; destek/hata sonucu da 200 dönebilir.
- Yeşil CI: canlıdaki sürüm ve müşteri senaryosunun kanıtı değildir.

Sonuçlar yeni tarihli `tests/reports/` raporuna yazılır; bu listeye otomatik geçti işareti konmaz.
