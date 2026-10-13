# Yerel platform yöneticisi hesabını kurtarma

Bu komut yalnızca `Development` ortamında ve `envantertr` adlı yerel PostgreSQL veritabanında çalışır. E-posta ayarı bulunmayan yerel kurulumlarda, platform yöneticisinin parolasını Identity üzerinden yeniler. Parola komut satırına eklenmez, terminalde gösterilmez ve loglanmaz. Onaydan sonra açık oturumlar iptal edilir ve platform denetim kaydı yazılır.

EnvanterTR deposunun kök klasöründe PowerShell açıp çalıştırın:

```powershell
docker compose --project-directory deploy\local --file deploy\local\compose.yaml run --build --rm web --recover-local-platform-admin
```

Komut e-posta adresini ister; yeni parolayı iki kere gizli olarak yazın ve işlemi onaylamak için `RESET` yazın. Parola en az 12 karakter olmalı, büyük/küçük harf, sayı ve özel karakter kurallarını karşılamalıdır. Komut yalnızca platform yöneticisi kaydına bağlı hesapları kabul eder.

İşlem tamamlanınca yerel web servisini güncel kaynakla yeniden başlatın:

```powershell
docker compose --project-directory deploy\local --file deploy\local\compose.yaml up --build -d web
```

Komut etkileşimsiz/stdin yönlendirilmiş çalıştırmada, Production ortamında, başka veritabanı adında veya yerel olmayan veritabanı sunucusunda değişiklik yapmadan durur. Bu kurtarma aracı canlı/Production ortamda kullanılmaz.
