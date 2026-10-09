# Güvenlik kabul kontrolleri

## Çok şirketli oturum: üyelik ve rol değişiklikleri

Tekrarlanabilir kabul senaryosu `tests/EnvanterTR.SecurityAcceptance` içindedir. Windows PowerShell'de proje kökünden çalıştırın:

```powershell
.\deploy\testing\run-security-acceptance.ps1
```

Docker gereklidir. Betik benzersiz bir Compose proje adıyla geçici PostgreSQL ve test konteynerleri açar; uygulama migration'larını boş veritabanına uygular ve sonuç ne olursa olsun yalnızca kendi konteyner, ağı ve volume'larını kaldırır. Geliştirme veritabanına, mevcut Compose projesine veya kalıcı yedeklere bağlanmaz.

Kimlik doğrulama kabulü, TOTP MFA etkin kullanıcı için ikinci adımın zorunlu olduğunu, kurtarma kodunun başarılı girişte tüketildiğini ve aynı kodla tekrar girişin reddedildiğini doğrular; ayrıca güncel Authenticator koduyla girişin başarılı olduğunu sınar.
Çok şirketli girişte şirket seçimi yalnız hesabın etkin `CompanyMembership` kayıtlarından oluşturulur: geçerli iki üyelik seçenek olarak sunulur, üçüncü/üyesiz şirket gizlenir ve kimliği doğrudan gönderilirse `403` alır; geçerli ikinci şirket seçilince oturum o şirket kimliğiyle açılır.
Hesap güvenliği kabulü başarılı giriş ve MFA yönetimi olaylarının şirket denetim günlüğüne yazıldığını, doğrulama sırrı/kurtarma kodlarının denetim ayrıntısına sızmadığını da kontrol eder.

## API rota yetki envanteri

`Every_api_endpoint_declares_an_explicit_authentication_boundary` uygulamanın çalışma zamanında oluşturduğu API `RouteEndpoint` kayıtlarını tarar. En az 300 API endpoint'i bulunduğunu; her birinin `AllowAnonymous` veya en az bir `IAuthorizeData` politikası taşıdığını doğrular. Anonim erişim kümesi yöntem/yol bazında sabit allowlist ile eşleştirilir (11 rota); anonim rotaya yeni bir ekleme veya çıkarma testin açıkça güncellenmesini gerektirir. Test, dokuz şirket/platform yetki politikasının hepsinin kullanıldığını ve başka adlandırılmış politikanın eklenmediğini de doğrular.

Bu kontrol rota metadata envanterinin eksiksizliğini kanıtlar; her endpoint için tüm rollerin HTTP davranışını ayrı ayrı kanıtlayan tam bir 318×rol matrisi değildir. Temsilî rol matrisi ve tenant davranış testleri pakette ayrıca bulunur.

`Every_api_route_policy_matches_the_company_and_platform_role_matrix` keşfedilen tüm korumalı API rotalarında birleştirilmiş çalışma zamanı politikasını her yedi şirket rolü ve platform yöneticisi için değerlendirir. Böylece rol/politika uyumsuzluğu metadata seviyesinde tüm rotalarda yakalanır. Bu test gerçek HTTP isteklerini veya endpoint'lerin kaynak düzeyindeki tenant davranışını her rol için sınamaz; bunun için aşağıdaki anonim HTTP ve temsilî tenant davranış testleriyle birlikte değerlendirilmelidir.

`Anonymous_http_requests_are_rejected_by_every_protected_api_route` rota şablonlarından örnek parametrelerle 280'den fazla korumalı HTTP yöntem/yol isteği üretir. İstek gövdesi rota metadata'sındaki içerik türüne göre seçilir; oturumsuz her yanıtın `401` olmasını doğrular. Böylece politika etiketi yanında çalışma zamanındaki challenge da kontrol edilir. Bu, anonim erişim sınırını doğrular; kimliği doğrulanmış her rolün her endpoint'teki davranış matrisi değildir.

`Company_roles_follow_the_asset_stock_employee_and_settings_policy_matrix` yedi geçerli şirket rolünün her biriyle şirket kapsamlı marka/etiket ayarlarının okunabildiğini ve hiçbirinin platform özetine erişemediğini de doğrular. `Company_member_and_platform_admin_boundaries_are_enforced_over_http` tanınmayan şirket rolünün `403` aldığını, sıradan şirket yöneticisinin platform özetine erişemediğini ve veritabanında platform yöneticisi olan hesabın platform özetine erişebildiğini doğrular.

Senaryo aynı eski `admin` kimlik bilgilerini her istekte tekrar gönderir ve veritabanındaki güncel durumu değiştirir:

- Başlangıçta üye yönetimi izni çalışır.
- İstek şirket kimliği, kullanıcının üyesi olmadığı etkin bir şirkete çevrilince `401` döner.
- Kayıtlı varlık aramaları yalnız oluşturan kullanıcıya görünür; aynı şirketteki başka kullanıcı boş liste alır, başka şirkete geçiş `401` olur. Büyük/küçük harf farklılığı mevcut aramayı yinelenen kayıt yerine günceller, yalnız sahibinin silebildiği doğrulanır ve CSRF token'ı olmayan yazma isteği `400` döner.
- Pano ve görünüm tercihleri kullanıcı/şirket kapsamında ayrıdır. Tema/dil seçenekleri allowlist ile doğrulanır, yazma CSRF koruması ister, pano düzeni güncellemesi kayıtlı tema/dili korur; başka kullanıcı ve şirket aynı tercihi okuyamaz.
- Üyelik rolü `viewer` olduğunda üye yönetimi isteği `403`, varlıkları okuma isteği `200` döner.
- Varlık listesi yalnız geçerli şirkete ait kaydı içerir; diğer şirkete ait varlık kimliğiyle ayrıntı isteği `404` döner.
- Lisans bitiş raporu listesi ve CSV yalnız etkin şirkete ait lisansları içerir; kendi lisansı ayrıntıda `200`, diğer şirketin lisans kimliği `404` döner. CSV yanıtı `no-store` olarak işaretlenir.
- Aksesuar, sarf ve bileşen listesi/detay/CSV uçları diğer şirkete ait stok kayıtlarını göstermez. Birleşik stok hareketi listesi ve CSV'si de her stok türünü geçerli şirket için birleştirir.
- Bakım eki kabulü, yabancı şirket bakımına dosya yükleme, yabancı bakım ekini indirme ve silme isteklerinin `404` döndürdüğünü; reddedilen işlemlerden sonra yabancı ekin veritabanı metaverisi ve disk dosyasının yerinde kaldığını doğrular.
- Varlık dosya kabulü, yabancı şirkete ait varlık ekini indirme, yabancı varlığa dosya yükleme ve silme girişimlerinin `404` döndürdüğünü; yabancı ekin veritabanı metaverisiyle disk dosyasının korunduğunu doğrular.
- Çalışan belge kabulü, yabancı şirkete ait çalışan profiline belge yükleme ve belge silme girişimlerinin `404` döndürdüğünü; yabancı belge metaverisiyle disk dosyasının korunduğunu doğrular.
- Bileşen stok raporu yalnız etkin şirketin kayıtlarından toplam, zimmetli ve kullanılabilir adetleri hesaplar; kabul senaryosu diğer şirketteki bileşen miktarlarının rapor toplamlarına karışmadığını denetler.
- Tamamlanmış fiziksel denetim raporu ile CSV dışa aktarımı yalnız etkin şirkete ait denetim ve varlık bilgilerini döndürür.
- Talep kataloğunda talebe açık ve kullanılabilir kendi şirket varlığı görünür; diğer şirketin talep edilebilir varlığı sonuçlara sızmaz.
- Varlık talebi başka şirketin varlığında ve birden çok adet istendiğinde reddedilir; geçerli talep onaylanınca varlık çalışanla zimmetlenir, talep durumu ve hareket geçmişi aynı kabul veritabanında doğrulanır.
- Varlık talebi karar uç noktası şirket rol matrisi içinde denetlenir; yalnızca `asset.write` yetkisi olan roller erişebilir.
- Çalışan hesabı eşleştirme akışında bağlı `viewer` sadece kendi çalışanını görebilir ve talep edebilir; çalışan profili bağlantısı olmayan yazma yetkisiz hesap çalışan listesini göremez ve başkası adına talep gönderemez. Talep listesi ve CSV'si yalnız kendi taleplerini döndürür.
- Varlık ayrıntısı kendi şirketindeki açık aksesuar teslimlerini ve takılı bileşenleri gösterir; başka şirkete ait varlığa bağlı kayıtlar yanıta eklenmez.
- Varlık, aksesuar, sarf, bileşen, lisans, çalışan, etkinlik, denetim, stok hareketi, satın alma, talep ve garanti liste uçları sayfa parametreleri gönderilmeden çağrıldığında varsayılan sayfalama ile yanıt verir; varlık CSV dışa aktarımı da varsayılan kayıt sınırını kullanır.
- Şirket üyeliği kaldırıldığında sonraki istek `401` döner.
- Üyelik yeniden eklenip şirket devre dışı bırakıldığında sonraki istek `401` döner.
- Linux production profilindeki forwarded-header ayarıyla giriş hız sınırı gerçek istemci IP'sine göre bölünür: aynı IP 10 deneme sonrasında `429` alırken ayrı IP kendi 10 denemelik hakkını korur.

Bu, güncel oturum üyeliği/rolü ile seçilmiş varlık, stok, talep kataloğu ve lisans şirket sınırlarını kapsayan dar bir regresyon kontrolüdür; liste uçlarının parametresiz varsayılanlarını da kapsar. Varlık talebi açma/onaylama senaryosu kabul konteynerindeki geçici PostgreSQL üzerinde çalışır; geliştirme veritabanına yazmaz. Diğer modüllerin tüm liste/ayrıntı/dışa aktarma uçlarını kapsayan tam izin matrisi, kimlik/parola kurtarma, yük dengeleme ve üretim Windows/Linux sunucu kabulünün yerine geçmez.

- Herkese açık web kabuğu EnvanterTR markasını gösterir ve karşılaştırma ürününün adını içermez.
