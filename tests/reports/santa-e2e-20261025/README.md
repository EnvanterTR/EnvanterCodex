# Gerçek şirket kabulü — 25 Ekim 2026, kısmi

## Güncel ek kabul — 17:18–17:23 TRT

Kullanıcı bekleyen normal yönetici doğrulamasını tamamladı. Demo3 geri alma işlemi sonuçlandı; ilk aşamada erişim askıda kaldı. Ardından ayrı, gerekçeli “Erişimi yeniden aç” işlemi başarıyla uygulandı. Güncel firma listesi **Demo3, Farma ve Santa'yı aktif** gösterdi. `CompanyRestored` (17:18) ve `CompanyReactivated` (17:21) gerçek denetim kayıtları doğrulandı.

Demo3 ayrıntısından, mevcut sahibin rolüyle ve kayıtlı gerekçeyle **15 dakikalık yönetici destek oturumu** açıldı. Tarayıcı gerçek `https://demo3.envantertr.com/` adresine geçti; Demo3 bağlamındaki boş envanter ve sarı “Yönetici destek oturumu” bandı görüldü. “Platform yönetimine dön” ile gerçek yönetici Genel Bakış ekranına dönüldü; üç aktif firma görünür kaldı. İlk yüklemedeki geçici giriş formu, sonraki nihai oturum kontrolünde yönetim paneline dönüştü; oturum kaybı veya başarısız dönüş diye sayılmadı.

`ImpersonationStarted` ve `ImpersonationEnded` (17:23) kayıtları aynı destek oturumu hedefiyle gerçek Yönetici İşlem Kaydı ekranında ayrı ayrı doğrulandı. Destek sırasında otomatik API okumalarının `ImpersonationRequest` kayıtları da listelendi; bunlar ayrı kullanıcı eylemleri veya ayrı kabul testleri sayılmaz.

Bu, 20.5'in gerçek şirket görünürlüğü/destek oturumu/sarı bandı/audit/dönüş alt adımları için kanıttır. **Normal owner parola+MFA girişi değildir.** Boş bir şirketin boş pano göstermesi, Santa/Farma kayıtlarının çapraz erişim saldırı testini kanıtlamaz. Canlı HTTP 428 negatif yazma ve 20.4 GET/PUT kabulü hâlâ açık. SMTP teslimatında bir iyileşme veya Google ayarı değişikliği yapılmadı.

Yeni kanıtlar: `live-demo3-restored-suspended-20261025.png`, `live-demo3-reactivated-20261025.png`, `live-demo3-support-banner-20261025.png`, `live-platform-returned-20261025.png`, `live-support-start-audit-20261025.png`, `live-support-end-audit-20261025.png`, `live-company-restored-audit-20261025.png`, `live-company-reactivated-audit-20261025.png`.

Aşağıdaki oluşturma/silme kanıtları ve DB tablosu önceki kabul anını kaydeder; Demo3'ün güncel UI durumu artık aktiftir.

Canlı sürüm `124b2d1e93af2a13c393d68353c271671c856de8`. Kullanıcı normal platform MFA girişini tamamladı; uygulama Genel Bakış ve firma yönetimini açtı. Yeni kişisel kod alınmadı, eski kurtarma kodları kullanılmadı. Aşağıdaki işlemler doğrudan ürünün normal formundan yapıldı; DB yazması, özel token veya test-auth başlığı kullanılmadı.

## Gerçekleşen işlemler

1. **Santa oluşturuldu** — alt alan adı `santa`, Hedef 5.3.2'deki yetkili, İşletme 4 kullanıcı, 15 gün demo. Form sonucu aktif Trial, 9 Kasım 2026 bitişi ve bir giriş hesabı gösterdi. Ödeme kaydı veya doğrulanmış gelir oluşturulmadı.
2. **Demo3 oluşturuldu** — alt alan adı `demo3`, kullanıcının mevcut Farma yetkilisi, aynı 15 günlük/4 kullanıcılı demo. Liste üç firmayı gösterdi. Mevcut ortak hesap kullanıldı; parolası değiştirilmedi.
3. **Her iki e-posta gönderimi BAŞARISIZ** — form sonucu açıkça “Şirket ve abonelik oluşturuldu; e-posta gönderilemedi” dedi. Bu, SMTP teslimat kabulü değildir.
4. **Demo3 çöp kutusuna taşındı** — gerekçe ve tam `demo3` slug onayı ile normal Sil akışı. Güncel listede yalnız Santa/Farma kaldı; çöp kutusunda Demo3 göründü.
5. **Diğer şirketlerin korunması doğrulandı** — işlemden önce/sonra gerçek üretim DB'sinde 80 CompanyId içeren tablonun Santa/Farma satırları için sayı+tam satır içerik parmak izleri, ayrıca şirket satırları ve ilişkili kimliklerin parola/stamp/MFA alan parmak izleri karşılaştırıldı. **82 projeksiyonun tamamı eşit**. Bu bir test sayısı değildir. Ham değerler ve parmak izleri yalnız sunucudaki özel dosyalarda kaldı; rapora taşınmadı. MD5 yalnız tutarlılık karşılaştırmasıdır, güvenlik özeti değildir.
6. **Denetim kayıtları doğrulandı** — Yönetici İşlem Kaydı ekranında iki `DemoCompanyProvisioned` (Santa 11:44, Demo3 11:45 TRT) ve bir `CompanySoftDeleted` (Demo3 11:51 TRT) kaydı görüldü.

## İlk geri alma denemesi — tarihsel engel

Demo3'ü kalan kabul adımları için geri alma formu gönderildiğinde sunucu 10 dakikalık kritik işlem yetkisinin yeniden doğrulanmasını istedi. Doğrulama iptal edildi; ürün “Kritik işlem iptal edildi; hiçbir değişiklik uygulanmadı” gösterdi. Geri alma veya etkinleştirme gerçekleşmiş gibi raporlanmaz. Demo3 **çöp kutusunda/pasif** bırakıldı; kayıtlar geri alınabilir. MFA süresi uzatılmadı veya kapatılmadı.

## Silme sonrasındaki salt okunur DB ara durumu

| Slug | Aktif | Çöp kutusunda | Sahip üyeliği | Sahibin e-postası doğrulanmış |
| --- | --- | --- | ---: | --- |
| demo3 | hayır | evet | 1 | evet |
| farma | evet | hayır | 1 | evet |
| santa | evet | hayır | 1 | hayır |

Sorgu `live-company-state-after-provision-20261025.sql`, ekran çıktısı aynı adlı PNG'dir. Santa sahibi doğrulanmış sayılmadı. Gerçek davet bağlantısı/hesap kurulumu ve kullanıcı girişi hâlâ gereklidir.

## Hedef kapsamı

| Madde | Bu adımın kanıtı |
| --- | --- |
| 20.2 Santa'nın 10 kullanıcı adımı | Tamamlanmadı; yalnız normal şirket oluşturma kanıtlandı |
| 20.3 Farma'nın 10 kullanıcı adımı | Bu adımda yapılmadı |
| 20.4 canlı karşılıklı GET/PUT saldırı testi | Yapılmadı; izole kabulün yerine geçirilmedi |
| 20.5 platform iki şirketi görür / audit | Yukarıdaki ek kabulde gerçek destek oturumu, sarı bant, audit ve dönüş doğrulandı; canlı HTTP 428 negatif API kanıtı açık |
| 20.9.1 normal Demo3 oluşturma | Gerçek yönetim formuyla geçti |
| 20.9.2 DNS/TLS | Önceki dış DNS/TLS kanıtı var; bu adımda sahibiyle portal girişi yapılmadı |
| 20.9.3 yönetici hesabı/giriş | Sahip üyeliği var, gerçek owner girişi yapılmadı |
| 20.9.4 veri izolasyonu | Owner oturumuyla görünürlük kabulü yapılmadı |
| 20.9.5 silme diğerlerini etkilemez | Gerçek soft-delete, çöp kutusu, audit ve 82 korunmuş projeksiyonla belirtilen kapsamda geçti |

Bu klasörde UI ekranları ve salt-okunur SQL kanıtı vardır. Bu adımda ham HTTP yanıtları kaydedilmedi; tam 20.6 API+DB+UI kabulü iddia edilmez. Tam Bölüm 20, SMTP ve ödeme kabulü açık.

## Kanıt dosyaları

- `live-santa-provisioned-20261025.png`, `live-demo3-provisioned-20261025.png`
- `live-demo3-deleted-20261025.png`, `live-demo3-trash-final-20261025.png`
- `live-demo3-delete-preservation-20261025.png`, `live-demo3-preservation-query-20261025.sql`
- `live-company-provision-audit-20261025.png`, `live-demo3-delete-audit-20261025.png`
- `live-company-state-after-provision-20261025.png`, aynı adlı SQL

Özel sunucu dosyaları: `/var/backups/envantertr/rollback-20261025T080518Z/{tenant-preservation.sql,before-demo3-delete.private,after-demo3-delete.private}`. Bunlar raw kimlik veya parmak izi verileriyle birlikte public repoya eklenmedi.

Tarayıcı terminalinde ilk uzun metin girişi gözlem zaman aşımıyla yarıda kaldı; komut çalışmış sayılmadı. Yeni işlem başlatılmadan terminal görüntüsü incelendi, aynı giriş temizlenip tek parça paste ile tamamlandı; gerçek `BASELINE_CAPTURED 82` ve `SANTA_FARMA_AND_SHARED_IDENTITY_UNCHANGED` çıktıları doğrulandı.
# Santa daveti gerçek gönderim — 25 Ekim 2026 devamı

Kullanıcının “girdi” mesajı sonrası yeni platform sekmesinin oturum kontrolü gerçek yönetici Genel Bakışına geçti. Normal Müşteriler/Firmalar → Santa Detay → Yetkili erişim e-postasını yeniden gönder kullanıldı. Yeni MFA sorusu açılmadı. Google ileti günlüğü **EnvanterTR şirket alanı daveti**, alıcı cloudmailhome@gmail.com, 22:39:30 TRT alınış / 22:39:31 TRT **Google dahili sunucusuna teslim**, Teslim Edilen1/1, kaynak169.58.53.189/TLS gösterdi. Bu posta kutusu/spam konumu veya hesabın etkinleştiği kanıtı değildir.

Kanıt: ../santa-owner-invitation-google-delivered-20261025.png. Davet normal parola belirleme/etkinleştirme akışıdır; yeni credential giriş ve gönderim adımı kullanıcıya bırakıldı. Davet token'ı veya yeni parola okunmadı, DB EmailConfirmed doğrudan değiştirilmedi. Owner girişi ve kalan10adım/cross-tenant kabulü açık. Önceki davet gönderilemedi ve MFA bekliyor kayıtları tarihseldir.
