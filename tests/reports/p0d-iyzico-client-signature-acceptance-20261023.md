# P0-D iyzico istemci ve imza regresyonu — 23 Ekim 2026

- Durum: TAMAM (yalıtılmış birim/regresyon paketi).
- Komut: `docker compose -f deploy/testing/compose.security-acceptance.yaml -p p0d-provider run --build --rm acceptance --filter "FullyQualifiedName~IyzipayCheckoutClientTests|FullyQualifiedName~IyzipayWebhookSignatureTests"`
- Çıktı: Exit 0; 5 test geçti, 0 başarısız; toplam 3.0245 sn.
- Doğrulama: Sağlayıcı imzası ve gövde bütünlüğü; bozuk imzanın reddi; eksik kimlik bilgisi ve güvenilmeyen API hostunda kapalı kalma; sağlayıcı hata yanıtının başarı gibi dönmemesi.
- Temizlik: `docker compose ... down -v --remove-orphans` tamamlandı; test DB konteyneri/ağı kaldırıldı.
- Sınır: Bu testler gerçek iyzico sandbox hesabına bağlanmadı ve üretim ayarlarını doğrulamaz.
- Sıradaki iş: Staging'de gerçek sandbox anahtarlarıyla uçtan uca ödeme, ardından P0-E gerçek müşteri kabulü. Canlıya deploy veya plan yayını yapılmadı.
