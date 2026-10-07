# Güvenlik kabul kontrolleri

## Çok şirketli oturum: üyelik ve rol değişiklikleri

Tekrarlanabilir kabul senaryosu `tests/EnvanterTR.SecurityAcceptance` içindedir. Windows PowerShell'de proje kökünden çalıştırın:

```powershell
.\deploy\testing\run-security-acceptance.ps1
```

Docker gereklidir. Betik benzersiz bir Compose proje adıyla geçici PostgreSQL ve test konteynerleri açar; uygulama migration'larını boş veritabanına uygular ve sonuç ne olursa olsun yalnızca kendi konteyner, ağı ve volume'larını kaldırır. Geliştirme veritabanına, mevcut Compose projesine veya kalıcı yedeklere bağlanmaz.

Senaryo aynı eski `admin` kimlik bilgilerini her istekte tekrar gönderir ve veritabanındaki güncel durumu değiştirir:

- Başlangıçta üye yönetimi izni çalışır.
- Üyelik rolü `viewer` olduğunda üye yönetimi isteği `403`, varlıkları okuma isteği `200` döner.
- Şirket üyeliği kaldırıldığında sonraki istek `401` döner.
- Üyelik yeniden eklenip şirket devre dışı bırakıldığında sonraki istek `401` döner.

Bu, oturumdaki şirket üyeliğinin/rolünün güncel veritabanı durumuyla eşitlenmesine yönelik dar bir regresyon kontrolüdür. Tam izin matrisi, şirketler arası veri izolasyonu, kimlik/parola kurtarma, yük dengeleme ve üretim Windows/Linux sunucu kabulünün yerine geçmez.
