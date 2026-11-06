# EnvanterTR — Claude çalışma kuralları

Sahibi: Celal Belge. Bu dosya kısa tutulur; ayrıntılı tarihçe `AGENTS.md` ve `docs/` altındadır.

## Ürün
Snipe-IT'in işlev olarak eşdeğeri, çok kiracılı, Türkçe öncelikli envanter yönetimi.
Dört dağıtım tek kod tabanından çıkar: Bulut SaaS (`firma.envantertr.com`), Windows masaüstü (yerel kurulum), şirket içi sunucu (Windows/Windows Server), Ubuntu sunucu.

## Teknoloji (değişmez)
- `src/EnvanterTR.Web`: ASP.NET Core (.NET 10), EF Core 10, PostgreSQL 17, ASP.NET Identity. Uç noktalar `Data/*Endpoints.cs`.
- Arayüz: `src/EnvanterTR.Web/wwwroot/index.html` (tek dosya SPA, ~13 bin satır; parçalanması planlı iş).
- `src/EnvanterTR.PublicSite`: tanıtım sitesi. `deploy/`: coolify, local, server (Ubuntu), windows, testing.
- Canlı: Coolify (Traefik) → `main` dalından manuel deploy.

## Sınırlar
1. Canlıya deploy, canlı veritabanına yazma, GitHub'a push, dal silme: yalnız sahibin açık onayıyla.
2. F1 kapanana kadar yeni EF migration yok; uygulanmış migration değiştirilmez.
3. Iyzico / gerçek ödeme kilitli (şirket kurulana kadar).
4. Snipe-IT AGPL lisanslı: davranış referansıdır, kodu kopyalanmaz.
5. Parola, API anahtarı, token hiçbir dosyaya, loga, commit'e yazılmaz. DeepSeek inceleme anahtarı depo dışında: `C:\Users\Lenovo\Desktop\EnvanterTR\.secrets\`.
6. Yüzdeyle ilerleme raporu verilmez; kanıt = çalıştırılan komut ve sonucu.

## Çalışma şekli
- Tek iş paketi, tek dal. Önce kırmızı test, sonra en küçük düzeltme.
- Her değişiklikte: `Verify-*.mjs` statik doğrulayıcılar + ilgili tarayıcı testi + gerekirse `node tools/Generate-UiInventory.mjs`.
- Tarayıcı kabul testleri: `tests/browser/*.acceptance.mjs` (yerel Santa demo verisiyle; canlıda çalıştırılmaz).
- Kod incelemesi: değişiklik farkı DeepSeek'e (yalnız kod) gönderilir, bulgular doğrulanarak değerlendirilir.

## Yerel doğrulama
```bash
dotnet build src/EnvanterTR.Web -c Release
dotnet src/EnvanterTR.Web/bin/Release/net10.0/EnvanterTR.Web.dll --migrate   # ConnectionStrings__EnvanterTR gerekli
# Setup__BootstrapToken ile ilk şirket: /setup.html; demo veri: tools/seed-santa-*.sql (psql -v owner_user_id=...)
for f in tools/Verify-*.mjs; do node "$f" || echo "FAIL $f"; done
BASE_URL=http://127.0.0.1:5099 TEST_EMAIL=... TEST_PASSWORD=... node tests/browser/drill-down.acceptance.mjs
```
