# Canlı ana sayfa galerisi — tarayıcı kabulü — 2026-11-01

Durum: **TAMAM (yalnız kullanıcı görsel galerisi gereksinimi)**

## Kimlik ve HTTP kanıtı

- Canlı sayfa: `https://envantertr.com/?verify=4789b50#varlik-etiketleri` (HTTP 200).
- Canlı HTML script referansı: `/product-gallery.js?v=cda5cf6`.
- Canlı JavaScript: HTTP 200, 5.078 karakter; `registerExpandable`, 5.000 ms timer, sola kaydırma ve dialog açma kodu mevcut.
- Live-text source comparison: canlı JS metni `cda5cf6f89a241bebb56fb8fc8392c350b8ace39:src/EnvanterTR.PublicSite/product-gallery.js` git blob metniyle tam eşleşti (`Exact text match=True`). Deploy commit `4789b509c0b719c1bf71a0b8032f7224bae9fd43` script query'yi bu kaynak commit'ine sabitliyor.

## Canlı tarayıcı sonucu

- Canlı ana sayfada galeri, dört görsel seçimi ve duraklatma kontrolü yüklendi; yeşil tema seçiliydi.
- Aktif etiket görseline tıklama tam ekran native dialog açtı. Dialog kaynağı `/assets/images/asset-label-roll-transparent.png`; doğal çözünürlük 1200×1311 idi. Ekran görüntüsünde görsel ve “Kapat” düğmesi görünür.
- “Kapat” dialog'u kapattı ve odağı açıcı görsele geri verdi.
- 5,4 saniyelik beklemede carousel son slayttan başa ilerledi. 555×1052 viewportta yatay taşma olmadı.
- Ürün kodu veya production data değiştirilmedi. Canlı kontrol salt-okunur GET ve public tarayıcı etkileşimiydi.

## Sınırlar

Bu kabul ana sayfa galerisi içindir; genel SaaS V1, checkout/iyzico, canlı ve kaynak migration kimlik paritesi, taze yedek/restore veya tenant kabul kapılarını kanıtlamaz.
