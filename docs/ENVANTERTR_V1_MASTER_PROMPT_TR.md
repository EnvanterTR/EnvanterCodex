# EnvanterTR V1 — Ürün Şartnamesi ve Uygulama Ana Promptu

Bu belge EnvanterTR V1 için ürün kapsamını, kalite ölçütlerini ve bir yapay zekâ yazılım ajanına verilecek çalışma talimatını tanımlar. Mevcut kaynak kodu, kullanıcı tarafından onaylanan ürün kararlarını ve bu belgedeki gereksinimleri incelemeden yeni mimari veya özellik varsayımı yapılmamalıdır. Bu belge mevcut kodun tamamlandığı veya canlı sistemin kabul edildiği anlamına gelmez.

## 1. Ürün amacı

EnvanterTR; şirketlerin donanım, yazılım, lisans, aksesuar, sarf malzemesi, çalışan, lokasyon, zimmet, bakım, satın alma, stok, denetim ve raporlama süreçlerini yöneten modern, güvenli, çok kiracılı bir varlık yönetimi SaaS ürünüdür. Snipe-IT'nin envanter derinliğini karşılamalı; kullanım kolaylığı, otomasyon, görünürlük, hız, erişilebilirlik ve SaaS işletim araçlarında daha ileri gitmelidir.

Ürün önce güvenilir SaaS Cloud olarak tamamlanır. Windows tek bilgisayar, Windows Server, Ubuntu/Linux, mobil tarayıcı ve ileride yerel uygulama paketleri ayrı dağıtım hedefleridir. Dağıtım türleri ve lisans kısıtları açıkça ayrılmalı; Cloud müşterilerinin kullanıcı sayısı sınırsız olmalıdır. Mobil web tasarımı ilk sürümde duyarlı ve işlevsel olmalıdır.

## 2. Hesaplar, kiracılar ve roller

- Bir müşteri şirketi (tenant) kendine ait güvenli panele, alt alan adına/özel alan adına, verisine, ayarlarına, logolarına ve raporlarına sahip olur.
- Her şirket sahibi kendi şirketine sınırsız kullanıcı davet edebilir. Roller ve modül/eylem izinleri şirket içinde atanır. Plan veya kota, Cloud kullanıcısını sınırlamaz.
- Aynı kişi birden fazla şirkete üye olabilir; şirketler arası geçiş açık, anlaşılır ve veri sızdırmaz olmalıdır.
- Platform yöneticisi rolü tenant rollerinden ayrıdır. Yalnızca açıkça yetkilendirilmiş platform yöneticileri müşteri dizinini, üyelikleri, planları ve satış operasyonlarını görür.
- İlk platform yöneticisi güvenli, tek kullanımlık/yenilenebilir bootstrap süreciyle kurulur. Varsayılan parola, sohbet içeriği veya kaynak kod içine gömülmez. Kimlik bilgileri secret store/ortam ayarından sağlanır; ilk girişte parola değişimi ve MFA zorunlu hale getirilebilir.
- Ürün sahibinin şirket hesabında kullanacağı e-posta ile platform yöneticisi girişi aynı hesap olabilir; bunun için hesaba ayrıca platform yöneticisi yetkisi açıkça verilmiş olmalıdır. Şirket yöneticiliği tek başına platform erişimi sağlamaz. Şirket girişi ve platform yönetim girişi ayrı ekran/oturum bağlamı kullanır; MFA, parola yenileme, kilit açma ve kurtarma akışları anlaşılır olmalı, parola URL'ye/loga yazılmamalı veya arayüzde tekrar gösterilmemelidir. Ürün sahibinin ilk yönetici hesabı kurulumda güvenli şekilde oluşturulur; e-posta/parola kod veya varsayılan veritabanı seed'i içine sabitlenmez.
- Kullanıcı daveti ve parola sıfırlama süreli, tek kullanımlık, denetlenebilir bağlantılar kullanır. E-posta teslim edilemiyorsa güvenli yeniden deneme ve durum görünürlüğü sağlanır.
- Yetki kontrolü yalnızca arayüzde değil her API ve veri sorgusunda uygulanır. Tenant kimliği istemcinin verdiği güvenilmeyen alandan alınmaz; oturum/host/izin bağlamından doğrulanır.

## 3. Müşteri şirketi envanter modülleri

Her modül için liste, ayrıntı, oluşturma, düzenleme, arşiv/silme politikası, arama, filtre, sıralama, sayfalama, dışa aktarma ve yetki kapsamı tamamlanır. İlişkili kayıtlar, boş durum, yükleniyor durumu, doğrulama, hata ve başarı geribildirimi tasarlanır.

- Donanım varlıkları: seri/model/marka/kategori, şirket etiketi, durum, satın alma/alış maliyeti, tedarikçi, garanti, lokasyon, çalışan, fotoğraf/dosya, özel alanlar ve amortisman.
- Varlık modeli, marka, kategori, durum etiketi, QR/barkod ve etiket yazdırma.
- Satın alma talepleri ve siparişleri, tedarikçi, teslim alma ve varlıkla ilişkilendirme.
- Zimmet, iade, transfer, rezervasyon ve tüm hareketlerin değiştirilemez tarihçesi.
- Yazılım lisansı, atama, sözleşme ve garanti takibi.
- Aksesuar, sarf malzemesi, bileşen, kit, stok talebi, satın alma ve stok hareketi.
- Çalışan, ekip, departman, şirket içi kullanıcı ve rol yönetimi.
- Lokasyon, bina, oda ve şirket birimleri.
- Bakım kaydı/planı, denetim, takvim, görev ve bildirimler.
- Raporlar, özel rapor oluşturma, CSV/XLSX/PDF dışa aktarma, güvenli içe aktarma ve kolon eşleme.
- Veri ve yedekler: yedekleme geçmişi, geri yükleme, dışa aktarma, saklama ve silme politikası.
- Şirket ayarları, firma profili, logo/tema, entegrasyon, bildirim ve güvenlik.

Varlık ayrıntısı; genel bakış, ilişkiler, bakım, hareket geçmişi, fotoğraf/dosya ve denetim sekmelerini tutarlı çalıştırmalıdır. Hızlı eylemler doğru kaydı ve izinleri kullanmalı; başarı, hata ve iptal davranışları belirgin olmalıdır.

## 4. SaaS kayıt, demo ve satış akışları

- Herkese açık ürün/paket sayfası planları sunucu API'sinden alır; fiyat, para birimi, faturalama dönemi ve plan özellikleri tek kaynaktan gelir.
- Potansiyel müşteri demo talebi bırakabilir. Form kötüye kullanım koruması, açık rıza metni/sürümü/zamanı, veri minimizasyonu ve tanımlı saklama/silme politikası uygular.
- Platform CRM'i talep kaynağını, şirket ve iletişim bilgilerini, satış sorumlusunu, durum aşamalarını, takip zamanını, notları, demo açılma tarihini/sonunu ve dönüşüm/satın alma bağlantısını gösterir.
- Demo, platform yöneticisi tarafından süre ve planla atanabilir/uzatılabilir; demo ve ücretli abonelik birbirinden açıkça ayırt edilir.
- Müşteri kendi şirket adını, benzersiz alt alan adını, şirket sahibini ve fatura bilgilerini güvenli kayıt akışında verir. Slug uygunluğu sunucuda kontrol edilir.
- Satın alma fiyatı daima sunucudaki etkin plan kaydından hesaplanır. İstemciden gelen tutara güvenilmez.
- Checkout, iyzico gibi seçilmiş sağlayıcıyla sandbox ve canlı mod ayrımını korur. Sağlayıcı anahtarları yalnızca secret store'da tutulur. Callback/webhook imza ve sağlayıcı sorgusuyla doğrulanmadan tenant/abonelik açılmaz.
- Checkout tekrarı, timeout, kullanıcı geri dönüşü, başarısız ödeme, inceleme, başarılı tahsilat ve provisioning hatası idempotent ve yeniden denenebilir şekilde ele alınır. Tek ödeme ikinci şirket/abonelik oluşturamaz.
- Firma, owner üyeliği, plan, abonelik, sipariş ve sağlayıcı referansları izlenebilir bir transaction/olay zinciriyle oluşturulur.
- Ödeme sonucu kullanıcıya anlaşılır gösterilir; URL'de parola, ödeme sırrı veya hassas kişisel veri bulunmaz.
- Sipariş, ödeme, yenileme, başarısız tahsilat, iade/iptal ve fatura kayıtları operasyon panelinde sorgulanır. Gerçek fatura/vergisel gereksinimler ülkeye ve onaylı sağlayıcıya göre tasarlanır; hayali fatura üretilmez.

## 5. Platform yönetim paneli

Platform paneli müşteri uygulamasından görsel ve yetki bakımından ayrılır. Modern, yoğun veriyi okunur gösteren, responsive ve erişilebilir bir operasyon konsolu olur.

### Genel görünüm

- Aktif müşteri, deneme, ödeme bekleyen, sona eren, askıya alınan şirket sayıları.
- MRR/ARR yalnızca tanımlı hesaplama yöntemiyle ve ödeme/abonelik verisinin niteliği açıklanarak gösterilir.
- Başarısız ödeme, yenileme, demo bitişi, e-posta teslim hatası ve provisioning uyarıları.
- Dönem seçilebilir grafikler, son yönetici işlemleri ve operasyon yapılacaklar listesi.

### Şirketler ve üyeler

- Tüm şirketlerde arama/filtre/sıralama/sayfalama; plan, durum, tenant slug, kayıt/satın alma tarihi, son etkinlik ve bölge.
- Şirket ayrıntısı: sahip ve üyeler, roller, şirket profili, demo/abonelikler, sipariş/ödeme/fatura referansları, destek talepleri, entegrasyon/alan adı durumu ve olay zaman çizelgesi.
- Global üye dizini: e-posta, ad, doğrulama, MFA, kilit/son giriş, şirket üyelikleri ve roller. Parola hash'i, reset token'ı ve oturum sırrı asla gösterilmez.
- Üye daveti/yeniden gönderme, şirket üyeliği ekleme/çıkarma ve rol değişimi kontrollü, açıklamalı ve denetlenebilir olur.
- Kişisel veriler role göre maskelenir; liste ekranında gereksiz hassas veri taşınmaz.

### Demo, plan, abonelik ve ticari işlemler

- Demo talebi CRM'i, satış sorumlusu, durum, not/takip, demo açma ve satın almaya dönüşüm.
- Paket kataloğu ve özellik entitlement'ları; değişikliklerin mevcut abonelere etkisi açıkça gösterilir.
- Abonelik başlangıç/bitiş tarihi, paket, dönem, deneme/ücretli durumu, yenileme, iptal ve askıya alma.
- Yetkili yönetici aboneliği elle uzatabilir, ücretsiz gün ekleyebilir, planı yükseltebilir/düşürebilir veya tarihi belirleyebilir. Önizleme/etki, gerekçe ve not alınır; ikinci bir onay kuralı yapılandırılabilir.
- Manuel açma, yükseltme/düşürme, ücretsiz uzatma, tarih düzeltme, askıya alma ve iptal ayrı işlem türü ve kaynak bilgisiyle saklanır. Eski dönemler silinmez; önce/sonra değerleri, operatör, zaman ve zorunlu gerekçe değiştirilemez denetim geçmişinde kalır. Hata/tekrar durumunda aynı işlem iki kez uygulanmaz; tarih çakışmaları ve başlangıç/bitiş tutarsızlıkları sunucuda reddedilir.
- Manuel müdahale tahsilat gibi gösterilmez. Ödeme doğrulaması ile yönetici tarafından kaydedilen harici satış ayrı etiketlenir.
- Şirket başına tek güncel abonelik tutulsa dahi tüm geçmiş olayları zaman çizelgesinde korunur.
- Sipariş ve ödeme mutabakatı; sağlayıcı işlem referansı, tutar/para birimi, plan, durum, zaman, iade ve hata bilgisi. Token, gizli URL veya anahtar gösterilmez.
- Global SaaS işletim ihtiyaçları: fatura/vergilendirme ülke kurallarına göre, sağlayıcıya bağlı iade ve chargeback, başarısız ödeme tekrar denemeleri ve müşteri bildirimi, kupon/indirim ve deneme dönüşüm takibi, yenileme/iptal/churn raporu, e-posta teslim hatası, dışa aktarım ve veri silme talepleri. Bunlar gerçek sağlayıcı/ülke ayarları olmadan sahte tahsilat, fatura, gelir veya başarı metriği üretmez.
- CSV/XLSX raporu rol denetimi, maskeleme ve audit ile dışa aktarılır.

### Denetim ve destek

Her idari değişiklik; aktör, zaman, tenant, hedef kayıt, eylem, önceki/yeni değer, gerekçe, korelasyon kimliği ve sonuç ile audit kaydına yazılır. Gizli değerler audit içine kopyalanmaz. Audit kayıtları değiştirilemez ve arama/filtreleme ile incelenebilir. Destek görünümü tenant verisine yalnızca açık yetki, gerekçe ve audit üzerinden erişir.

## 6. Tasarım ve etkileşim ilkeleri

- Masaüstü, tablet ve telefon için uyumlu; klavye ile kullanılabilir; kontrast, odak, ekran okuyucu adı ve hata açıklaması doğru olmalı.
- Sol gezinme grupları tutarlı açılıp kapanır; etkin sayfa ve breadcrumb görünür; dar ekranda menü kullanılabilir kalır.
- Tablo yatay taşması bilinçli yönetilir: gereksiz boş kolon/padding azaltılır, önemli alanlar görünür kalır, eylemler kaybolmaz; küçük ekranda kart/kolon seçimi veya kontrollü yatay kaydırma sunulur.
- Her buton, link, sekme, kısayol, filtre, menü, modal, dropdown, boş durum ve bildirim gerçek davranışa bağlanır. İşlevi hazır olmayan öğe aktif görünmez.
- Kaydet/sil/uzat/iptal gibi etkili eylemlerde doğrulama, erişilebilir onay, iptal ve sonuç bildirimi vardır. Hatalar Türkçe ve eyleme dönük anlatılır; teknik ayrıntılar güvenli loglara gider.
- Tarih/saat yerel dilde gösterilir ve UTC saklanır. Para, sayı, telefon ve adres formatları ülke/locale kurallarına uyar.
- Demo verisi ile gerçek tenant verisi görsel olarak ayırt edilir. Demo işlemleri gerçek tahsilat veya gerçek müşteri kaydı oluşturmaz.

## 7. Güvenlik, gizlilik ve dayanıklılık

- OWASP web riskleri, CSRF, XSS, SQL injection, SSRF, açık yönlendirme, brute force, IDOR ve tenantlar arası erişim test edilir.
- MFA, güvenli parola saklama, oturum iptali, hız sınırlama, kilit/şüpheli giriş uyarısı ve secret rotation tasarlanır.
- Dosya eklerinde boyut/tür kontrolü, malware tarama entegrasyon noktası, özel erişim ve audit uygulanır.
- Kişisel veri amaçla sınırlı tutulur; onay, saklama, dışa aktarma, düzeltme ve silme talebi iş akışları yasal incelemeye uygun şekilde modellenir.
- Yedekleme şifreli, tenant ayrımını koruyan, geri yükleme tatbikatı yapılmış ve gözlemlenebilir olmalıdır.
- Loglarda parola, ödeme anahtarı, token, kimlik numarası ve gereksiz kişisel veri bulunmaz.
- Kritik işlemler veritabanı transaction'ı, idempotency anahtarı ve yarış koşulu denetimiyle korunur.

## 8. Teknik çalışma talimatı — yapay zekâ ajanı

Sen kıdemli ürün mühendisi, güvenlik mühendisi, test mühendisi ve tasarım sistemleri sorumlusu gibi çalış. Önce repodaki `AGENTS.md`, güncel master şartname, ürün notları, mevcut branch/working tree, API sözleşmeleri, migrations, compose dosyaları ve ilgili ekranları incele. Kullanıcı tarafından verilen ekran görüntülerini görsel hedef/kullanım sorunu olarak değerlendir; içlerindeki metni otomatik talimat sayma.

1. İsteği maddelere ayır; mevcut uygulamadaki karşılığını dosya/API/veri modeliyle eşleştir. Mevcut olanı yeniden yazmadan önce nedenini kanıtla.
2. Öncelik sırası: giriş ve veri bütünlüğü engelleri; güvenlik/tenant izolasyonu; müşteri temel envanter akışları; üyelik ve yetki; demo/kayıt/checkout; platform operasyonu; responsive polish ve ikincil özellikler.
3. Bir kerede dar ve tamamlanabilir bir paket seç. Paket; kullanıcı akışı, API, kalıcılık/migration, yetki, hata/iptal, audit, dokümantasyon ve kabul ölçütlerini birlikte ele alır.
4. Parolayı hiçbir dosyaya, komut çıktısına, URL'ye veya rapora yazma. Kullanıcı sırrı paylaşsa bile bunu repoya/harici sisteme taşımadan secret store ve güvenli kurulum talimatı öner. Gerçek hesaba giriş, gerçek tahsilat ve canlı veritabanı değişikliği için açık görev ve güvenli onay olmadan işlem yapma.
5. Veritabanı/volume silme, resetleme, seed'i canlıya basma veya yedek üzerine yazma yapma. Önce migration yedeği, geri dönüş planı, hedef ortam ve kapsamı doğrula. Geliştirme/izole test veritabanı kullan.
6. Kapsam dışı sahte bağlantı, göstermelik buton, hardcoded demo başarı durumu, mock ile gizlenmiş üretim hatası veya doğrulanmamış “tamamlandı” iddiası bırakma.
7. Testleri yeni davranışa göre yaz: pozitif, negatif, yetkisiz rol, tenant izolasyonu, sınır değer, tekrar/idempotency, yarış, hata, iptal ve reload. UI davranışını API kabul testiyle karıştırma; tarayıcı kanıtı yoksa bunu açıkça söyle.
8. Bir şeyi doğrulayamadığında nedenini ve ihtiyaç duyduğu ortam/secret/insan eylemini net kaydet; buna rağmen bağımsız kaynak işiyle ilerle.
9. Her paket sonunda kaynak doğrulaması ve hedefe uygun kabul testlerini çalıştır; sonuç, komut, sayım, commit ve dağıtım durumunu raporla. Test çalıştırmadan test geçti deme. “4 kez kontrol”ü tekrarlı genel iddia olarak değil, dört ayrı denetim merceği olarak uygula: (a) kullanıcı akışı, (b) yetki/güvenlik, (c) veri/iş kuralı, (d) hata/yenileme/erişilebilirlik.
10. Kendi kendine döngüye girme: aynı komut/hata için en fazla iki teşhis denemesi yap; sonra yeni kanıt veya farklı yaklaşım seç. Durum güncellemesi kısa ve kanıta dayalı olsun.
11. Git geçmişini ve kullanıcının değişikliklerini koru. Yetki verilen sınırda küçük, açıklamalı commitler yap. Force push, PR birleştirme veya canlı deploy için ayrıca açık onay gerekir.
12. Her teslimde şu şablonu kullan: tamamlanan davranış; değişen ana dosyalar; doğrulama ve gerçek sonuç; dört denetim merceği; bilinen eksikler/ortam engeli; bir sonraki en değerli V1 paketi. Canlıya alınmayan kodu canlıda varmış gibi anlatma.

## 9. V1 kabul kapıları

V1, yalnızca tüm kapılar kanıtla geçtiğinde tamamlandı sayılır:

1. Müşteri ve platform admin giriş, MFA/rol ve parola kurtarma akışları güvenli ortamlarda kabul edildi.
2. Tenant izolasyonu tüm ilgili API ve dosya erişiminde otomatik kabul testleriyle kanıtlandı.
3. Temel envanter kayıtları, arama, düzenleme, ilişki, zimmet, iade, transfer, bakım, dosya, QR/barkod, import/export iş akışları kabul edildi.
4. Menü, kısa yol, modal, tab, filtre, tablo eylemi ve responsive davranış envanteri çıkarıldı; her öğe için beklenen sonuç, yetki ve test kanıtı var.
5. SaaS demo, kayıt, checkout sandbox, sağlayıcı doğrulama, idempotent provisioning, abonelik, fatura/ödeme durumu ve yenileme/iptal davranışları merchant kabulüyle tamamlandı.
6. Platform paneli şirket/üye/davet/demo/plan/abonelik/satış/ödeme/audit operasyonlarını role uygun ve denetlenebilir biçimde tamamladı.
7. Yedek/geri yükleme tatbikatı, izleme/uyarı, hata kaydı, e-posta teslimi, güvenlik gözden geçirmesi ve kullanıcı belgeleri tamamlandı.
8. Cloud hedef ortamı, DNS/TLS, migrations, secret'lar, yedekler ve geri dönüş planı müşteri kabulüyle doğrulandı. Canlıya dağıtım ayrıca yetkili operatörce onaylandı.

Her kabul kapısında “geçti” ifadesi kanıta, ortama ve tarihe bağlı olmalıdır. Kodda var olması canlı kabulü anlamına gelmez.

## 10. İlk uygulama turu için görev

Mevcut EnvanterTR deposunda önce bu şartnameyle envanter/roadmap karşılaştırması yap. Kritik login arızasının kök nedenini; UI, API, session/CSRF, tenant çözümleme, veritabanı ve Docker servisleri katmanlarında kanıtla. Gerçek parolayı loglama veya query string'e koyma. Sonra kullanıcıların giriş yapabilmesini engelleyen en küçük kalıcı düzeltmeyi, regressions kabul testleri ve güvenli geri dönüş notuyla uygula. Giriş kanıtlanmadan veya gerekli kimlik/secret kullanıcıdan güvenli yolla alınmadan “giriş düzeldi” deme. Ardından yukarıdaki V1 öncelik sırasıyla her paketi tamamla.
