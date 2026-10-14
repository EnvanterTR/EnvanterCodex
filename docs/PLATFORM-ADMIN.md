# Platform yönetimi (ürün sahibinin yönetim alanı)

Bu doküman, EnvanterTR platform sahibinin kullanacağı yönetim API'sinin mevcut kapsamını ve henüz yapılmamış kısımlarını tanımlar. Müşteri şirketlerinin kendi çalışma alanlarından ayrıdır.

## Eklenen API temeli

Platform yönetim ekranı `src/EnvanterTR.Web/wwwroot/platform-admin.html` dosyasında, `/platform-admin.html` yolunda yer alır. Oturum açma, özet, firma listesi ve arama, firma ayrıntısı/üyelik ve abonelik geçmişi, firma askıya alma/etkinleştirme, demo tanımlama, abonelik süresi uzatma, plan listeleme/oluşturma/düzenleme, site metni düzenleme ve denetim listesi ekranları API uçlarına bağlanmıştır. Ekran platform yöneticisi claim'i olmayan hesabı kabul etmez.

- `GET /api/platform/overview`: firma toplamı, aktif/askıdaki firmalar, aktif ücretli abonelik, demo, süresi dolmuş abonelik ve 30 gün içinde yenilenecek abonelik sayısı. Ödeme verisi olmadığı için gelir uydurmaz.
- `GET /api/platform/companies`: firma adı, alt alan adı adayı, iletişim e-postası, etkinlik, kayıt/oturum hesabı/varlık sayıları ve son abonelik. Arama, etkinlik filtresi ve sayfalama içerir.
- `GET /api/platform/companies/{id}`: firma özeti, ilk 100 üyelik ve son 50 abonelik hareketi.
- `GET /api/platform/companies/renewals?days=30&page=1&pageSize=50`: firma adından bağımsız, bitiş tarihine göre sıralanmış yaklaşan abonelikler.
- `PATCH /api/platform/companies/{id}/active`: firmayı askıya alır veya tekrar etkinleştirir. Askıdaki firma oturum açamaz; tenant API'leri de firmayı her istekte kontrol eder.
- `GET/POST/PUT /api/platform/plans`: dağıtım türü, kullanıcı/varlık sınırı, aylık/yıllık/tek sefer fiyatı, para birimi, çevrimdışı kullanım parametreleri, özellik bayrakları ve yayımlama ayarları.
- `POST /api/platform/companies/{id}/demo`: aktif aboneliği olmayan firmaya 1-60 günlük deneme tanımlar.
- `POST /api/platform/companies/{id}/subscription/extend`: son aboneliğin bitişini 1-1095 gün uzatır ve uzatma işlemini denetim kaydına yazar.
- `GET/PUT /api/platform/site-content`: site için başlık/metin/yayımlama alanlarını düzenler. Tanıtım sayfası yayımlanmış `home.hero`, `home.features`, `home.pricing`, `home.deployment`, `home.demo` bloklarını metin olarak uygular; görsel için yalnızca `/media/...` yolunu kabul eder, dosya yükleme henüz yoktur.
- Etkin ve yayımlanmış planlar `GET /api/plans` üzerinden yerel tanıtım sitesindeki Paketler bölümünde listelenir. Ödeme ve sipariş oluşturmaz; plan için e-posta ile bilgi isteği verir.
- `GET /api/platform/audit`: platform yöneticisi işlemlerinin sayfalı denetim kaydı.
- `GET /api/plans` ve `GET /api/site-content`: web sitesinin yayımlanmış plan ve içerik verisini alması için herkese açık okuma uçları.
- Tüm yönetim uçları `platform_admin` yetkisi ister; yazma isteklerinde antiforgery token zorunludur. Platform hesabı token ile ilk kez kurulur (`Setup__PlatformBootstrapToken`) ve kurulumdan sonra bu gizli anahtar kaldırılmalıdır. Kurulum durumu anahtarın yalnızca yapılandırılmış olup olmadığını bildirir; değerini göstermez. Aynı e-posta şirket yöneticiliğinde kullanılıyorsa mevcut parola doğrulanarak hesaba platform yetkisi eklenebilir; parola ve profil değişmez. Giriş ekranları birbirine bağlantı verir.

## Bu geliştirme turunda tamamlanan kontrol listesi

1. Platform yöneticisi oturumu müşteri çalışma alanı seçimine bağımlı olmayacak şekilde ayrıldı.
2. Yönetici ekranı e-posta yerine oturumdaki görünen adı kullanır.
3. Özet kartları firma ve abonelik sayılarını ayrı gösterir; ödeme verisi olmadan gelir üretmez.
4. Yenileme listesi özel uç noktadan gelir; ilk alfabetik firmalara takılıp kalmaz.
5. Yenilemeler bitiş tarihine göre sıralanır ve sayfalanır.
6. Yenileme listesinde faturalama aralığı bilinmediği için yanlış yıllık ücret gösterilmez.
7. Firma araması ad, alt alan adı ve iletişim e-postasını kapsar.
8. Firma liste sayfalaması üst sınırla taşma/çok büyük sorgu riskini sınırlar.
9. Firma ayrıntısı ayrı ekranda açılır.
10. Firma ayrıntısı kullanıcı adlarını ve şirket rollerini listeler.
11. Üyelik listesi 100 kayıtla sınırlanır ve kesildiği belirtilir.
12. Son 50 abonelik dönemi ve ödeme referansı firma geçmişinde gösterilir.
13. Askıya alma/etkinleştirme durum değişmiyorsa gereksiz denetim satırı oluşturmaz.
14. Demo yalnızca erişimi açık firmaya ve etkin plana verilebilir.
15. Demo süresi 1-60 gün aralığında doğrulanır.
16. Demo oluşturma seri transaction içinde yapılır; eşzamanlılık çakışması kullanıcıya 409 ile bildirilir.
17. Veri modeline firma başına tek aktif/deneme aboneliği kısıtı eklendi; veritabanı migration'ı uygulanana kadar bu kısıt canlı veritabanında etkin değildir.
18. Demo yenilenirken bitmiş önceki demo/abonelik kayıtları `Expired` durumuna alınır.
19. İptal edilmiş abonelik doğrudan uzatılamaz.
20. Demo süresi uzatılınca ücretli aboneliğe çevrilmez; satış/tahsilat kanıtı olmadan gelir sayımına girmez.
21. Plan kodu basit URL uyumlu biçime zorlanır.
22. Plan ücretleri negatif ve veritabanı sayısal sınırını aşan değerleri reddeder.
23. Para birimi kodu, sıra numarası ve çevrimdışı tolerans doğrulanır.
24. Özellik bayrakları yalnızca adlandırılmış JSON boolean alanları kabul eder.
25. Plan güncellemesi ve eşzamanlı site metni oluşturma çakışmaları kontrollü cevap verir.
26. Site medya yolu yalnızca yerel `/media/` alt yol biçiminde kabul edilir.
27. Denetim geçmişi arayüzde sayfalanır.
28. Formlar antiforgery token ile gönderilir; ekrandaki müşteri metinleri HTML olarak çalıştırılmaz.
29. Plan yönetimi ekranda çevrimdışı lisans/paket sınırlarının henüz zorunlu tutulmadığı konusunda uyarır.
30. Ekran dar pencerelerde tek sütun/taşabilir menü düzenine uyarlanır.

## Henüz yapılmayanlar

- Müşteri çalışma alanı arayüzü ve platform sitesindeki ayrı profesyonel içerik/medya editörü.
- Site görsel dosyası yükleme/depolama ve CDN/medya yönetimi.
- Sipariş/ödeme sağlayıcısı, fatura, vergi, iade ve tahsilat mutabakatı. Dolayısıyla satış/gelir metrikleri henüz gerçek finans verisi değildir.
- Self-servis satın alma, firma oluşturma, alt alan adı DNS/yönlendirmesi, e-posta daveti ve ilk kurulum akışı.
- Kullanıcı koltuğu/varlık paket sınırlarını işlem anında uygulama; mevcut plan alanları yalnızca tanımlama düzeyindedir.
- Çevrimdışı lisans imzalama, doğrulama, iptal ve süre uzatma protokolü. `LocalLicenseGrant` şeması yalnızca veri modelidir; kullanılabilir lisans mekanizması sayılmaz.
- E-posta/WhatsApp bildirimleri, parola kurtarma, destek talebi, satış demo rezervasyonu ve operasyon raporları.

## Güvenlik ve işletim notları

- Abonelik uzatma API'si elle idari işlem yapar; ödeme kanıtı doğrulamaz. Ödeme bağlantısı kurulmadan müşteriye çevrimiçi tahsilat varmış gibi sunulmamalıdır.
- Site içeriği API cevabını ön yüzde HTML olarak doğrudan çalıştırmayın; güvenli metin olarak gösterin veya uygun biçimde sanitize edin.
- Veritabanı migration'ı oluşturulup geri dönüş planı ve yedekli uygulama süreci eklenmeden üretim veritabanına bağlamayın.
- Platform yöneticileri ortak hesap altyapısındaki Authenticator tabanlı MFA ve tek kullanımlık kurtarma kodlarını kullanabilir; platform giriş ekranı 2FA kodu/kurtarma kodu adımını gösterir. Platform yöneticilerine özgü MFA zorunluluğu ve kritik işlem öncesi yeniden kimlik doğrulama ayrıca uygulanmalıdır.
- Yerel geliştirme sürümü Docker içindeki .NET SDK ile derlenip `localhost:8080` üzerinde açılmıştır. Bu, üretim dağıtımının veya müşteriye sunulabilir sürümün hazır olduğu anlamına gelmez; üretim güvenliği, MFA, TLS, yedekleme/geri yükleme ve gerçek satış akışları ayrıca tamamlanmalıdır.

## 14 Ekim 2026 — yönetici 2FA girişi ve müşteri yaşam döngüsü

Platform giriş ekranı Authenticator veya tek kullanımlık kurtarma kodunu ikinci adımda kabul eder; parola ve e-posta ilk adımda girilir. Yanlış/yeniden kullanılmış kod sunucunun `twoFactorRequired` cevabıyla aynı doğrulama adımında tekrar sorulur. `Authenticator_two_factor_requires_second_step_and_recovery_codes_are_single_use` sentetik hesapta MFA challenge ve platform yönetici yetkili giriş yanıtını doğrular; `Verify-PlatformAdminExperience.mjs` arayüz alanını ve challenge akışını kaynakta denetler.

Firma yönetiminde müşteri aşaması, atanmış platform sorumlusu, takip tarihi ve değişiklik notu tutulur. Firma detayında son 20 yaşam döngüsü olayı görünür; her güncelleme platform denetim kaydına yazılır. Bu CRM alanları hatırlatma bildirimi veya ödeme/fatura kayıtlarının yerini tutmaz.
