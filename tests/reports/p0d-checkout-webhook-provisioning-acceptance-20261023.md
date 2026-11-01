# P0-D Checkout, webhook ve provisioning kabulü — 23 Ekim 2026

- Durum: TAMAM (izole sentetik sağlayıcı kabul testi). Canlı iyzico/SMTP ayarları ve gerçek ödeme ENGELLENDİ; bu test bunların canlıda yapılandırıldığını göstermez.
- Kapsam: Yayımlı planın sunucu fiyatı, kupon hesabı, KVKK bildirim onayı, idempotent checkout kimliği, sahte iyzico başlatma/sonuç alma, webhook imzası ve sipariş korelasyonu, tekrarlanan bildirim, tenant/owner/abonelik oluşturma ve davet e-postası.
- Komut: `docker compose -f deploy/testing/compose.security-acceptance.yaml -p p0d-checkout run --build --rm acceptance --filter "FullyQualifiedName~Public_checkout_uses_server_price_verifies_provider_and_provisions_once"`
- Çıktı: Exit 0; 1 test bulundu, 1 geçti, 0 başarısız; toplam 47.5491 sn. Sahte sağlayıcıda ilk geçersiz/mismatched webhook reddedildi; onaylı bildirim tek tenant, owner üyeliği ve aktif abonelik açtı; tekrar bildirimi ikinci kayıt oluşturmadı. Davet iletisi doğru tenant URL'sini ve Authenticator kurulum bilgisini içerdi.
- Temizlik: `docker compose ... down -v --remove-orphans` tamamlandı; `p0d-checkout` adında çalışan konteyner kalmadı.
- Uyarılar: Test konteynerinde Data Protection anahtarlarının geçici dizinde tutulması ve test sunucusunda HTTP istek gövdesi boyut özelliğinin bulunmaması uyarıları görüldü. Test sonucunu etkilemedi. Eski migrationlardaki concurrent index transaction uyarıları da başlangıçta loglandı.
- Sınır: Sentetik sağlayıcı/yerel SMTP yakalayıcı kullanıldı; gerçek iyzico sandbox hesabı, canlı webhook, teslim edilebilir e-posta, gerçek firma kullanıcısı ve Cloudflare üzerinden yeni tenant otomatik yönlendirmesi doğrulanmadı.
- Sıradaki iş: P0-D için gerçek sandbox anahtarları ve P0-E Bölüm 20 kabulü; staging ortamı, dış sağlayıcı erişimi ve güvenli sunucu sırları gerekir. Canlı plan yayımlanmadı ve üretime deploy yapılmadı.
