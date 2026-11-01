# Ana sayfa ürün galerisi kabulü — 1 Kasım 2026

## Kapsam
- Etiket görselindeki özgün rulo korunur; üç ek kullanıcı görseli ve özgün görsel otomatik sola kayan aynı ölçülü galeride gösterilir.
- Dört özgün slayt tıklama, Enter ve Space ile tam ekran açılır. Escape veya kapatma tam ekrandan çıkar ve odağı açan görsele döndürür.
- Playwright kabulü dört slaytın tamamında tam ekran kaynağını, kapatma/odak dönüşünü, 5 saniyelik otomatik geçişi, elle slayt seçimini, 390 px mobil yatay taşma olmamasını ve tarayıcı hatası oluşmamasını doğrular.

## Kanıt
- Yerel gerçek Chromium kabul testi: `node product-gallery.mjs` → PASS.
- Test ortamı: Node 24.21.0, Playwright 1.64.0; kaynak `src/EnvanterTR.PublicSite` üzerinden sunuldu.
- Masaüstü ve mobil görüntüler: `product-gallery-20261101/home-gallery-desktop.png`, `product-gallery-20261101/home-gallery-mobile.png`.
- `Dockerfile.hosted-checkout-browser` galeri JS/CSS'ini ve üç ek görseli CI imajına dahil eder. GitHub Actions galeri kabulini ayrı, görünür `Run homepage gallery browser acceptance` adımında çalıştırır; PowerShell yerel runner da aynı komutu çağırır.
- Canlıya henüz yayınlanmadı.
