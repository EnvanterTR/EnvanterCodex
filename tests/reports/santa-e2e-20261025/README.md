# Gerçek şirket kabulü — 25 Ekim 2026, kısmi

Canlı sürüm `124b2d1e93af2a13c393d68353c271671c856de8`. Kullanıcı normal platform MFA girişini tamamladı; uygulama Genel Bakış ve firma yönetimini açtı. Yeni kişisel kod alınmadı, eski kurtarma kodları kullanılmadı. Aşağıdaki işlemler doğrudan ürünün normal formundan yapıldı; DB yazması, özel token veya test-auth başlığı kullanılmadı.

## Gerçekleşen işlemler

1. **Santa oluşturuldu** — alt alan adı `santa`, Hedef 5.3.2'deki yetkili, İşletme 4 kullanıcı, 15 gün demo. Form sonucu aktif Trial, 9 Kasım 2026 bitişi ve bir giriş hesabı gösterdi. Ödeme kaydı veya doğrulanmış gelir oluşturulmadı.
2. **Demo3 oluşturuldu** — alt alan adı `demo3`, kullanıcının mevcut Farma yetkilisi, aynı 15 günlük/4 kullanıcılı demo. Liste üç firmayı gösterdi. Mevcut ortak hesap kullanıldı; parolası değiştirilmedi.
3. **Her iki e-posta gönderimi BAŞARISIZ** — form sonucu açıkça “Şirket ve abonelik oluşturuldu; e-posta gönderilemedi” dedi. Bu, SMTP teslimat kabulü değildir.
4. **Demo3 çöp kutusuna taşındı** — gerekçe ve tam `demo3` slug onayı ile normal Sil akışı. Güncel listede yalnız Santa/Farma kaldı; çöp kutusunda Demo3 göründü.
5. **Diğer şirketlerin korunması doğrulandı** — işlemden önce/sonra gerçek üretim DB'sinde 80 CompanyId içeren tablonun Santa/Farma satırları için sayı+tam satır içerik parmak izleri, ayrıca şirket satırları ve ilişkili kimliklerin parola/stamp/MFA alan parmak izleri karşılaştırıldı. **82 projeksiyonun tamamı eşit**. Bu bir test sayısı değildir. Ham değerler ve parmak izleri yalnız sunucudaki özel dosyalarda kaldı; rapora taşınmadı. MD5 yalnız tutarlılık karşılaştırmasıdır, güvenlik özeti değildir.
6. **Denetim kayıtları doğrulandı** — Yönetici İşlem Kaydı ekranında iki `DemoCompanyProvisioned` (Santa 11:44, Demo3 11:45 TRT) ve bir `CompanySoftDeleted` (Demo3 11:51 TRT) kaydı görüldü.

## Geri alma tamamlanmadı

Demo3'ü kalan kabul adımları için geri alma formu gönderildiğinde sunucu 10 dakikalık kritik işlem yetkisinin yeniden doğrulanmasını istedi. Doğrulama iptal edildi; ürün “Kritik işlem iptal edildi; hiçbir değişiklik uygulanmadı” gösterdi. Geri alma veya etkinleştirme gerçekleşmiş gibi raporlanmaz. Demo3 **çöp kutusunda/pasif** bırakıldı; kayıtlar geri alınabilir. MFA süresi uzatılmadı veya kapatılmadı.

## Salt okunur DB son durumu

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
| 20.5 platform iki şirketi görür / audit | Gerçek listede üç şirket ve gerçek oluşturma/silme audit kayıtları görüldü; support impersonation/return ve HTTP 428 API kanıtı tamamlanmadı |
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
