# Platform yönetimi (ürün sahibinin yönetim alanı)

Bu doküman, EnvanterTR platform sahibinin kullanacağı yönetim API'sinin mevcut kapsamını ve henüz yapılmamış kısımlarını tanımlar. Müşteri şirketlerinin kendi çalışma alanlarından ayrıdır.

## Eklenen API temeli

Platform yönetim ekranı `src/EnvanterTR.Web/wwwroot/platform-admin.html` dosyasında, `/platform-admin.html` yolunda yer alır. Oturum açma, özet, firma listesi ve arama, firma ayrıntısı/üyelik ve abonelik geçmişi, firma askıya alma/etkinleştirme, demo tanımlama, abonelik süresi uzatma, plan listeleme/oluşturma/düzenleme, site metni düzenleme ve denetim listesi ekranları API uçlarına bağlanmıştır. Ekran platform yöneticisi claim'i olmayan hesabı kabul etmez.

- `GET /api/platform/overview`: firma toplamı, aktif/askıdaki firmalar, aktif ücretli abonelik, demo, süresi dolmuş abonelik ve 30 gün içinde yenilenecek abonelik sayısı. Ödeme verisi olmadığı için gelir uydurmaz.
- `GET /api/platform/companies`: firma adı, alt alan adı adayı, iletişim e-postası, etkinlik, kayıt/oturum hesabı/varlık sayıları ve son abonelik. Arama, etkinlik filtresi ve sayfalama içerir.
- `GET /api/platform/companies/{id}`: firma özeti, ilk 100 üyelik, son 50 abonelik hareketi, şirkete bağlanmış son 50 satın alma/ödeme siparişi ve son 20 müşteri yaşam döngüsü olayı.
- `GET /api/platform/companies/renewals?days=30&page=1&pageSize=50`: firma adından bağımsız, bitiş tarihine göre sıralanmış yaklaşan abonelikler.
- `GET /api/platform/demo-requests?followUp=overdue`: takip zamanı geçmiş ve hâlâ açık olan demo taleplerini listeler; `q`, `status` ve sayfalama filtreleriyle birlikte kullanılabilir. Kapalı veya müşteriye dönüşmüş talepler gecikmiş takip listesine girmez.
- Firma ayrıntısındaki satın alma tablosu sipariş oluşturma tarihi, sağlayıcı tarafından kaydedilmiş ödeme tarihi, tutar, para birimi, durum ve ödeme referansını gösterir. Ödeme sayfasının gizli URL/token alanları yönetim cevabına dahil edilmez. Bu görünüm kayıt/izleme sağlar; ödeme alma, iade veya mutabakat işlemi yapmaz.
- `PATCH /api/platform/companies/{id}/active`: firmayı askıya alır veya tekrar etkinleştirir. Askıdaki firma oturum açamaz; tenant API'leri de firmayı her istekte kontrol eder.
- `GET/POST/PUT /api/platform/plans`: dağıtım türü, kullanıcı/varlık sınırı, aylık/yıllık/tek sefer fiyatı, para birimi, çevrimdışı kullanım parametreleri, özellik bayrakları ve yayımlama ayarları.
- `POST /api/platform/companies/{id}/demo`: aktif aboneliği olmayan firmaya 1-60 günlük deneme tanımlar.
- `PUT /api/platform/companies/{id}/subscription/extend`: yönetici yeni kesin bitiş tarihini ve gerekçeyi girer; tarih mevcut bitişten sonra ve en fazla 1095 gün ileride olmalıdır. Mutlak tarih kullandığı için aynı isteğin tekrar gönderilmesi süreyi ikinci kez uzatmaz ve ikinci denetim satırı oluşturmaz.
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

- Müşteri çalışma alanının tümleşik kurulumu ve platform sitesinde medya dosyası yükleme/depolama/CDN yönetimi.
- Tahsilat sağlayıcısının üretim hesabı/anahtarları, otomatik yenileme, başarısız ödeme tekrarları (dunning), fatura/vergi, iade/chargeback ve muhasebe mutabakatı. Checkout/sentetik sağlayıcı testleri canlı tahsilat veya gelir raporu kanıtı değildir.
- Firma alt alan adı için otomatik DNS/yönlendirme ve müşteri kendi kendine ilk kurulum akışının uçtan uca üretim işletimi.
- Planlarda Cloud kullanıcıları sınırsızdır ve davet akışında bu sınır uygulanır.
- Çevrimdışı lisans imzalama, doğrulama, iptal ve süre uzatma protokolü. `LocalLicenseGrant` şeması yalnızca veri modelidir; kullanılabilir lisans mekanizması sayılmaz.
- E-posta daveti, parola kurtarma, demo talebi CRM'i ve destek talepleri için temel akışlar mevcuttur. Şirket kullanıcıları doğrulanmış adreslerine günlük operasyona ek olarak abonelik bitişi e-postasını isteğe bağlı açabilir; bu özellik tahsilat veya otomatik yenileme yapmaz. SMTP teslimat izleme, demo/destek için otomatik takip, gelişmiş şablon/tercih merkezi ve operasyon/gelir raporlarının tamamı eksiktir.

## Güvenlik ve işletim notları

- Abonelik uzatma API'si elle idari işlem yapar; ödeme kanıtı doğrulamaz. Ödeme bağlantısı kurulmadan müşteriye çevrimiçi tahsilat varmış gibi sunulmamalıdır.
- Site içeriği API cevabını ön yüzde HTML olarak doğrudan çalıştırmayın; güvenli metin olarak gösterin veya uygun biçimde sanitize edin.
- Veritabanı migration'ı oluşturulup geri dönüş planı ve yedekli uygulama süreci eklenmeden üretim veritabanına bağlamayın.
- Platform yöneticilerinde Authenticator tabanlı MFA zorunludur: MFA'sı kapalı yönetici `/api/platform/*` API'lerinden `428 platform_mfa_required` alır; güvenli mevcut oturum, parola korumalı kurulum ve etkinleştirme uçları açık kalır. Platform paneli yönetici oturumu doğrulandıktan sonra MFA kurulum ekranını açar, etkinleştirmede tek kullanımlık kurtarma kodlarını bir kez gösterir ve kaydetme onayından sonra yönetim ekranına geçer. Kritik işlem öncesi parola/TOTP ile yeniden kimlik doğrulama ayrıca uygulanmalıdır.
- Yerel geliştirme sürümü Docker içindeki .NET SDK ile derlenip `localhost:8080` üzerinde açılmıştır. Bu, üretim dağıtımının veya müşteriye sunulabilir sürümün hazır olduğu anlamına gelmez; üretim güvenliği, MFA, TLS, yedekleme/geri yükleme ve gerçek satış akışları ayrıca tamamlanmalıdır.

## 14 Ekim 2026 — firma ayrıntısında ödeme/sipariş geçmişi

Firma ayrıntısı son 50 şirkete bağlı checkout siparişini plan/dönem, sipariş durumu ve tarihi, sağlayıcıca doğrulanmış ödeme tarihi, tutar/para birimi ve ödeme referansıyla gösterir. Gizli checkout sayfası URL'si ve token hash'i API cevabına dahil edilmez. `Platform_admin_can_adjust_subscription_plan_and_dates_with_validation_and_audit` sentetik siparişte alanları ve sırların dışarıda kalmasını doğrular. İlgili test tek başına **1/1**, bu değişiklikten sonraki tüm izole PostgreSQL kabul paketi **126/126** geçti; platform paneli ve üye dizini kaynak denetimleri de geçti. Bu ekran canlı tahsilat, iade veya muhasebe mutabakatını sağlamaz.

## 14 Ekim 2026 — yönetici 2FA girişi ve müşteri yaşam döngüsü

Platform giriş ekranı Authenticator veya tek kullanımlık kurtarma kodunu ikinci adımda kabul eder; parola ve e-posta ilk adımda girilir. Yanlış/yeniden kullanılmış kod sunucunun `twoFactorRequired` cevabıyla aynı doğrulama adımında tekrar sorulur. `Authenticator_two_factor_requires_second_step_and_recovery_codes_are_single_use` sentetik hesapta MFA challenge ve platform yönetici yetkili giriş yanıtını doğrular; `Verify-PlatformAdminExperience.mjs` arayüz alanını ve challenge akışını kaynakta denetler.

Firma yönetiminde müşteri aşaması, atanmış platform sorumlusu, takip tarihi ve değişiklik notu tutulur. Firma detayında son 20 yaşam döngüsü olayı görünür; her güncelleme platform denetim kaydına yazılır. Bu CRM alanları hatırlatma bildirimi veya ödeme/fatura kayıtlarının yerini tutmaz.

## 14 Ekim 2026 — üye zaman çizelgesinde henüz açılmamış satın almalar

Üye ayrıntısının yaşam çizelgesi artık hesap e-postasıyla eşleşen, şirketi henüz oluşturulmamış checkout siparişlerini de gösterir. Böylece ödeme başlangıcı, inceleme/başarısızlık veya provisioning bekleyen kayıtlar şirket bağı oluşmasını beklemeden doğru hesapla incelenebilir. Eşleşme satın alma yetkilisinin `OwnerEmail` alanıyla yapılır; yalnızca fatura iletişim e-postasının eşleşmesi hesabı satın alma sahibi saymaz. Şirket üyeliği üzerinden erişilen şirket siparişleri önceki davranışıyla görünmeye devam eder. Gizli checkout token hash'i ve korumalı ödeme sayfası URL'si zaman çizelgesi API cevabına eklenmez. `Platform_member_directory_searches_cross_company_accounts_and_hides_identity_secrets` izole PostgreSQL kabul senaryosu bu davranışı ve sırların dışarıda kalmasını doğrular.

Doğrulama: 14 Ekim'deki bu değişiklikle tam izole PostgreSQL güvenlik kabul paketi 126/126 geçti. Testler ayrı ve rastgele adlandırılmış Compose projesinde çalıştı; sonrasında yalnız bu test ağı/konteyneri/geçici volume'u kaldırıldı. Yerel ürün DB'si ve volume'ları bu kabul için kullanılmadı.

## 14 Ekim 2026 — ürün sahibinin SaaS yönetim paneli kapsamı

Ürün sahibinin talebi; şirket kullanıcısı panelinden ayrı, aynı doğrulanmış hesapla yetki verildiğinde açılabilen bir platform yönetim alanıdır. İstenen premium arayüz, hesap/şirket arama ve filtreleme, yeni kullanıcı daveti, hesap ve tüm şirket üyeliklerinin ayrıntısı; kayıt ve ilk üyelik zamanları, şirket rolleri, demo talebi/kararı, sipariş ve doğrulanmış ödeme tarihi, paket/dönem/tutar/referans, yenileme ve hesap güvenliği bilgisini içermelidir. Üyeler birden çok şirkette farklı rollerle bulunabilir; bulut kullanıcı kotası sınırsız plan kuralına uymalıdır.

Yönetici; mevcut şirket aboneliğinin planını ve başlangıç/bitiş tarihlerini elle değiştirebilmeli, ücretsiz süre uzatabilmeli ve değişikliği gerekçe ile kaydedebilmelidir. Var olan plan/tarih düzenleme ve gün bazlı uzatma uçları bu işlemi destekler. Elle uzatma ödeme tahsilatı veya ödeme onayı sayılmaz; ödeme durumu ancak sağlayıcı kanıtıyla değişebilir. Üyelik rolü değişimi, davet, şirketten çıkarma, demo verme, askıya alma ve elle plan/tarih müdahaleleri platform yetkisi, CSRF, doğrulama ve denetim iziyle korunmalıdır. Parola yalnızca güvenli giriş formunda kullanılmalı; kaynak kod, URL, log veya rapora yazılmamalıdır. Yönetici hesabında MFA ve tek kullanımlık kurtarma kodları kullanılabilir; MFA'nın zorunlu kılınması ve kritik işlemlerde yeniden doğrulama ayrıca tamamlanmalıdır.

Mevcut arayüzde hesap/üye dizini ve ayrıntısı, çok şirketli üyelikler, davet/rol düzenleme/üyelik kaldırma, satın alma zaman çizelgesi, şirket yaşam döngüsü/CRM, demo talepleri, plan ve tarih düzenleme, şirket sipariş/ödeme geçmişi, yenilemeler ve denetim ekranı vardır. Açık global SaaS işleri arasında canlı sağlayıcı tahsilatı ve webhook doğrulaması, fatura/vergi, iade/chargeback ve mutabakat, başarısız tahsilat/dunning, MFA zorunluluğu, kritik işlem için yeniden doğrulama, medya yükleme/CDN, bildirim/hatırlatma ve üretim yedekli migration/yayın prosedürüdür. Bunlar bitmiş veya canlıda doğrulanmış gibi sunulmayacaktır.

14 Ekim canlı/yerel erişim kontrolü: `http://localhost:8080/api/status` `ready` (89 migration, 0 bekleyen); `/api/platform/bootstrap/status` `setupRequired=false` ve `/api/setup/status` `setupRequired=false` döndürdü. Salt-okunur yerel SQL kontrolü kullanıcının istediği mevcut hesapta hem platform yönetici rolü hem şirket üyeliği olduğunu doğruladı; bu kontrolde parola ile giriş tekrar denenmedi. `https://envantertr.com/api/status` `migration-required` (89 migration, 19 bekleyen), platform bootstrap ve ilk kurulum durum uçları da HTTP 503 döndürdü. Bu nedenle canlı giriş bilgileri şu an doğrulanabilir durumda değildir; üretim DB'sine parola/rol müdahalesi yapılmadı. Canlı açılış için yedek alınmalı, migration'lar kontrollü ve geri dönüş planıyla uygulanmalı, ardından yönetici rolü ve MFA ayrı doğrulanmalıdır.

14 Ekim platform kabul tekrarı: sekiz `Verify-Platform*.mjs` doğrulayıcısı geçti. İzole PostgreSQL üzerinde `FullyQualifiedName~Platform_` filtresi **12/12** geçti; üyelik rolü, davet, demo talebi/kararı, müşteri yaşam döngüsü, sipariş sırlarının gizlenmesi, abonelik plan/tarih düzenleme ve şirket provisioning kontrolleri kapsandı. Bu test gerçek canlı müşteri verisine yazmadı.

## 14 Ekim 2026 — ücretsiz üyelik uzatmasının kesin tarih ve tekrar güvenliği

Platform şirket listesindeki “Uzat” akışı artık gün eklemek yerine yönetici tarafından seçilen kesin bitiş tarihini ve zorunlu işlem gerekçesini kullanır. API `PUT` ile aynı mutlak tarihi uygular; aynı isteğin tekrarı abonelik süresini ikinci kez uzatmaz ve yinelenen denetim olayı yazmaz. Bitiş tarihi kısaltılamaz, iptal edilmiş abonelik açılamaz; yeni tarih mevcut bitişten sonra ve en fazla 1095 gün ileride olmalıdır. Uzatma `IsDemo` değerini korur; deneme aboneliği ücretliye dönüşmez. Önce/sonra dönem bilgisi ve gerekçe audit kaydına girer, ödeme referansı değiştirilmez.

Kabul: `Platform_subscription_extension_uses_exact_end_date_and_safe_retries` ücretsiz ücretli-plan uzatmasını, aynı isteği ikinci kez gönderme durumunu, kısaltma reddini, tek audit kaydını, mevcut ödeme referansının korunmasını ve demo aboneliğinin `Trial` kalmasını gerçek izole PostgreSQL'de doğrular. Düzeltme sonrası platform kabul filtresi **13/13**, sekiz platform kaynak doğrulayıcısı ve panel inline-JavaScript sözdizim kontrolü geçti. İlk geniş koşu tekrar eşitliğinde PostgreSQL mikro-saniye hassasiyeti ile .NET'in 100 nanosaniye tick farkını yakaladı; API karşılaştırma öncesi tarihi DB hassasiyetine normalize edecek şekilde düzeltildi ve ardından tam platform filtresi geçti.

## Abonelik sona erme e-posta hatırlatmaları

Şirket kullanıcısı, Bildirim Tercihleri'nden abonelik e-postasını ayrıca açabilir; varsayılanı kapalıdır. Yalnız etkin ve demo olmayan şirket aboneliği için bitiş tarihinden 30, 7 ve 1 gün önce günlük özete eklenir. Ara günlerde yeni hatırlatma üretmez; gönderim aynı kullanıcı/şirket için günde bir kez idempotent olarak kilitlenir. Alıcı, o şirkette üyeliği bulunan ve e-postası doğrulanmış kullanıcıdır. Bu bildirim otomatik tahsilat veya yenileme yapmaz.

## 14 Ekim 2026 — abonelik varlık kotasının işlem anında uygulanması

`MaxAssets` limiti PostgreSQL trigger'ı ve `Companies.AssetCount` sayacıyla veritabanı seviyesinde korunur. Sayaç mevcut kayıtlardan migration sırasında doldurulur; yeni kayıt, CSV/Excel içe aktarma, şirket arşivini geri yükleme, satın alma siparişi teslim alma ve tenant değişikliği aynı atomik sınırdan geçer. Eşzamanlı yazmalar aynı şirket satırındaki atomik artışla sıralanır; limit aşımında işlem transaction'ıyla geri alınır ve API `402 asset_quota_exceeded` döndürür. Varlık silinmesi sayacı azaltır. Sona ermiş/iptal edilmiş abonelik aktif plan kotası uygulamaz; geçerli Active/Trial döneminde plan kotası geçerlidir. `MaxAssets = null` sınırsız varlık demektir. Bulut kullanıcı sınırı bağımsızdır ve sınırsız kalır.

İçe aktarma önizlemesi mevcut sayaç ile geçerli plan sınırını karşılaştırıp aşımı kaydetmeden önce bildirir; veritabanı kuralı eşzamanlı yazmalara ve önizleme-kayıt aralığındaki değişikliklere karşı son güvenceyi sağlar. Kota yükseltildikten sonra yazma yeniden denenebilir. `Asset_plan_quota_covers_manual_import_and_purchase_receipt_with_atomic_upgrade_behavior` izole PostgreSQL'de manuel ekleme, limit aşımı, CSV önizleme/kaydetme, limit aşımında satın alma siparişinin değişmeden kalması, kota yükseltme ve sınırsız plana geçişi doğruladı. `Verify-AssetPlanQuota.mjs` migration ve uç nokta bağlantılarını denetler.
