# Santa sınırlı üyelik ve aktif teslim belgesi

## Onaylanan şirket üyeliği

Kullanıcı c.belge@hotmail.com hesabına Santa Envanter operatörü erişimi sorusuna açıkça “Evet, bu sınırlı Santa erişimini ekle” dedi. Önceden hazırlanmış normal Kullanıcı davet et formunda asset_operator seçimiyle Daveti gönder kullanıldı. Ürün “Mevcut hesap şirket alanına eklendi; erişim bilgisi e-postayla gönderildi” döndürdü. Bu gönderim yanıtıdır; yeni iletinin alıcı teslimat kaydı henüz ayrıca sorgulanmadı.

Salt okunur DB: c.belge hesabında Demo3 owner1, Farma owner1, Santa asset_operator1. Diğer şirket rolleri korundu; yeni parola/MFA yaratılmadı. Kanıt: santa-operator-member-added-20261026.png; santa-operator-membership-scope-20261026.sql; santa-operator-membership-db-20261026.png. Gerçek bu kullanıcı girişi/yetki reddi yapılmadan20.2.8tamam değildir. Ürün rolünün kapsamı kullanıcıya açıklandı ve onaylandı; rol anahtarı editor olarak uydurulmadı.

## Aktif zimmet ve imza bekleyen belge

Çalışan profili → İmzalı zimmet belgeleri → Yeni belge akışında ilk taslak `ZMB-20261025-7ef2680d2` oluşturuldu. Varlık bakım öncesi iade edildiğinden bu ilk taslak aktif teslim içermiyordu; imza atılmadı, kabul kanıtı sayılmadı ve düzenleme iptal edildi. Kayıt geçmişten silinmedi.

Varlık normal formdan aynı kabul çalışanına yeniden zimmetlendi; ürün hareket kaydı başarı yanıtı verdi. İkinci belge `ZMB-20261025-a11cc2306` normal akıştan oluşturuldu. Salt okunur DB snapshot `assets` uzunluğu1, etiketiSANTA-KABUL-20261025-01, slugSanta ve employeeCompanyId eşitliği true. İki imza tarihi de null. İmza base64, QR, MFA ve password okunmadı veya kaydedilmedi.

Bu ikinci belge iki imza alanıyla kullanıcıya bırakıldı; imza/print kullanıcı adımı isteniyor. PDF üretimi ve e-posta kabulü henüz tamam değildir. Kaynak mevcut yazdırma akışında tam imza olmadan yazdırmayı reddediyor; bunu atlayarak PDF üretilmedi. Kanıt: santa-handover-signature-pending-20261026.png; santa-handover-scope-20261026.sql; santa-handover-active-asset-db-20261026.png.

## Farma normal giriş hazırlığı

Ayrı farma.envantertr.com sekmesinde Hedef'teki mevcut parola doğrulandı; c.belge hesabının güncel Authenticator kodu bekleniyor. Kurtarma kodları kullanılmadı. Santa normal owner oturumu açık tutuldu. Kullanıcının imza ve Farma kod adımlarını tamamlaması bekleniyor; yeni kayıt veya oturum tam kabul sayılmadı.

Santa kanıtlı madde7/10olarak kalır.20.2.4imza/PDF/e-posta,20.2.8gerçek sınırlı kullanıcı girişi/yetki ve20.2.10logout açık. Farma/cross-tenant/ödeme açık.
