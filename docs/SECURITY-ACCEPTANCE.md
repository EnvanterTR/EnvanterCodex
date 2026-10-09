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
