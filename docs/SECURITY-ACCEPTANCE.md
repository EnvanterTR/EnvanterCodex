# Güvenlik kabul kontrolleri

## Çok şirketli oturum: üyelik ve rol değişiklikleri

Tekrarlanabilir kabul senaryosu `tests/EnvanterTR.SecurityAcceptance` içindedir. Windows PowerShell'de proje kökünden çalıştırın:

```powershell
.\deploy\testing\run-security-acceptance.ps1
```

Docker gereklidir. Betik benzersiz bir Compose proje adıyla geçici PostgreSQL ve test konteynerleri açar; uygulama migration'larını boş veritabanına uygular ve sonuç ne olursa olsun yalnızca kendi konteyner, ağı ve volume'larını kaldırır. Geliştirme veritabanına, mevcut Compose projesine veya kalıcı yedeklere bağlanmaz.

Senaryo aynı eski `admin` kimlik bilgilerini her istekte tekrar gönderir ve veritabanındaki güncel durumu değiştirir:

- Başlangıçta üye yönetimi izni çalışır.
- İstek şirket kimliği, kullanıcının üyesi olmadığı etkin bir şirkete çevrilince `401` döner.
- Üyelik rolü `viewer` olduğunda üye yönetimi isteği `403`, varlıkları okuma isteği `200` döner.
- Varlık listesi yalnız geçerli şirkete ait kaydı içerir; diğer şirkete ait varlık kimliğiyle ayrıntı isteği `404` döner.
- Lisans bitiş raporu listesi ve CSV yalnız etkin şirkete ait lisansları içerir; kendi lisansı ayrıntıda `200`, diğer şirketin lisans kimliği `404` döner. CSV yanıtı `no-store` olarak işaretlenir.
- Aksesuar, sarf ve bileşen listesi/detay/CSV uçları diğer şirkete ait stok kayıtlarını göstermez. Birleşik stok hareketi listesi ve CSV'si de her stok türünü geçerli şirket için birleştirir.
- Talep kataloğunda talebe açık ve kullanılabilir kendi şirket varlığı görünür; diğer şirketin talep edilebilir varlığı sonuçlara sızmaz.
- Varlık talebi başka şirketin varlığında ve birden çok adet istendiğinde reddedilir; geçerli talep onaylanınca varlık çalışanla zimmetlenir, talep durumu ve hareket geçmişi aynı kabul veritabanında doğrulanır.
- Varlık talebi karar uç noktası şirket rol matrisi içinde denetlenir; yalnızca `asset.write` yetkisi olan roller erişebilir.
- Varlık ayrıntısı kendi şirketindeki açık aksesuar teslimlerini ve takılı bileşenleri gösterir; başka şirkete ait varlığa bağlı kayıtlar yanıta eklenmez.
- Varlık, aksesuar, sarf, bileşen, lisans, çalışan, etkinlik, denetim, stok hareketi, satın alma, talep ve garanti liste uçları sayfa parametreleri gönderilmeden çağrıldığında varsayılan sayfalama ile yanıt verir; varlık CSV dışa aktarımı da varsayılan kayıt sınırını kullanır.
- Şirket üyeliği kaldırıldığında sonraki istek `401` döner.
- Üyelik yeniden eklenip şirket devre dışı bırakıldığında sonraki istek `401` döner.
- Linux production profilindeki forwarded-header ayarıyla giriş hız sınırı gerçek istemci IP'sine göre bölünür: aynı IP 10 deneme sonrasında `429` alırken ayrı IP kendi 10 denemelik hakkını korur.

Bu, güncel oturum üyeliği/rolü ile seçilmiş varlık, stok, talep kataloğu ve lisans şirket sınırlarını kapsayan dar bir regresyon kontrolüdür; liste uçlarının parametresiz varsayılanlarını da kapsar. Varlık talebi açma/onaylama senaryosu kabul konteynerindeki geçici PostgreSQL üzerinde çalışır; geliştirme veritabanına yazmaz. Diğer modüllerin tüm liste/ayrıntı/dışa aktarma uçlarını kapsayan tam izin matrisi, kimlik/parola kurtarma, yük dengeleme ve üretim Windows/Linux sunucu kabulünün yerine geçmez.
