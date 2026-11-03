# WP-020 — İmzalı zimmet PDF ve e-posta backend kabulü

- Tarih: 2026-11-03
- Branch: `codex/feature-main-reconcile-20261103`
- Önceki commit: `50f4dc6` (handover email schema/model parity, 122/122)
- Canlıya yazım/deploy: yapılmadı

## Değişiklik

- Çalışan zimmet belgesi PDF indirme rotası: `GET /api/employees/{employeeId}/handover-documents/{documentId}/pdf`.
- Gönderim durumu: `GET /api/employees/{employeeId}/handover-documents/{documentId}/email`.
- Açık kullanıcı gönderme eylemi: `POST /api/employees/{employeeId}/handover-documents/{documentId}/email`.
- PDF yalnız snapshot hash'i ve iki tarafın PNG imza hash'leri doğrulanırsa üretilir; indirilen yanıt `no-store` ve `nosniff` başlıkları taşır.
- E-posta şirket/çalışan/lokasyon yetkisi, kayıtlı aktif çalışan e-postası, güncel snapshot hash'i, HTTPS dışı production SMTP/TLS engeli, antiforgery, audit kaydı, istek idempotency anahtarı ve kullanıcı başına 10/10dk rate-limit ile korunur.
- SMTP sunucusu kabul edince durum `Accepted` olur; bu posta kutusuna teslim edildiği anlamına gelmez. Belirsiz SMTP sonucu `Uncertain` kaydedilir ve yeniden gönderim için önceki denemenin açıkça belirtilmesi gerekir; arka planda otomatik retry yoktur.
- Geçmiş kaynak commit'ten yalnız gerekli API/PDF/SMTP dilimleri, font/lisans dosyaları ve testler alındı. `wwwroot` arayüz değişiklikleri bu WP'ye taşınmadı.

## Doğrulama

Komut:

```text
docker compose -p envantertr-handover-wp020 -f deploy/testing/compose.security-acceptance.yaml run --build --rm acceptance --filter FullyQualifiedName~Handover_
```

Sonuç: **5/5 PASS**; test ağı `internal: true` idi ve yalnız sentetik SMTP capture + izole PostgreSQL acceptance veritabanı kullanıldı.

Kapsananlar: eşzamanlı/tekrarlı aynı idempotency anahtarının tek gönderime düşmesi; değiştirilmiş alıcı ve onaysız retry'nin reddi; farklı şirket/rol, eski alıcı, imzasız/tahrif edilmiş belge ve production'da TLS kapalı SMTP'nin reddi; SMTP belirsiz durumunda otomatik retry olmaması ve yalnız önceki deneme kimliği ile yeni gönderim; PDF imza/tenant sınırı ve çok sayfalı Türkçe içerik.

Web Release container build geçti. Kabul test image içindeki web ve test build'leri 0 hata ile geçti; web build 0 uyarı, test build'inde yalnız mevcut `TrustedClientIpTests` WebHostBuilder/TestServer obsolescence uyarıları vardı. `git diff --check` rapor hazırlanırken tekrar çalıştırılacak.

## Sınırlar

- Testte e-posta dış alıcıya gönderilmedi; gerçek Gmail/Workspace teslimat kabulü yapılmadı.
- Bu çalışma yalnız API backend'idir. Çalışan detay ekranına PDF indir/gönderme/tekrar gönderme arayüzü bu pakette yoktur.
- Production database, restored backup clone, canlı web uygulaması, Coolify ve GitHub değiştirilmedi.
