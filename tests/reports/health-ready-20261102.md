# Container readiness probe — 2 Kasım 2026

## Değişiklik

- `GET /health` liveness probe olarak korunur: uygulama HTTP isteği yanıtlıyor mu?
- `GET /health/ready` yeni readiness probe'udur: PostgreSQL erişimi ve bekleyen EF migration olmaması gerekir. DB ayarsız, erişilemez, hata veriyor veya migration bekliyorsa `503` ve yalnız `{"status":"not-ready"}` döner; hazırsa `200` ve `{"status":"ready"}` döner.
- Runtime imajına `curl` eklendi. Docker `HEALTHCHECK` her 30 saniyede readiness endpoint'ini deniyor; başlangıç toleransı 60 saniye, timeout 5 saniye, 3 başarısız denemede unhealthy.

## Kanıt

- `HealthReadinessAcceptanceTests`: 2/2 geçti. Liveness 200 kalırken DB yapılandırması yok ve DB portuna erişilemiyor durumlarının ikisinde de readiness `503` döndü.
- Yalıtılmış PostgreSQL 17 üzerinde `Database_readiness_is_healthy_after_current_migrations_are_applied`: 1/1 geçti; güncel migration'larla readiness `200` ve `{"status":"ready"}` döndü.
- `Dockerfile` ile Release uygulama imajı `envantertr-health-ready:20261102` başarıyla üretildi.
- İmajdan ağsız, portsuz, volumesuz tek kullanımlık konteyner başlatıldı. Docker health status `unhealthy` oldu ve her health probe `/health/ready` kaynaklı HTTP 503 kaydetti. Bu, DB'siz imajın yanlışlıkla sağlıklı görünmediğini doğrular.
- Test konteyneri kaldırıldı. Yerel geliştirme konteynerlerine, staging'e ve production'a dokunulmadı.
- `git diff --check`: geçti.

## Sınır

DB'siz Docker imajının `unhealthy` durumu ve güncel PostgreSQL şemasının HTTP `200` readiness yanıtı ayrı ayrı doğrulandı. Kod değişikliği henüz CI, PR, staging veya production'dan geçmedi; canlıya konmuş değildir.
