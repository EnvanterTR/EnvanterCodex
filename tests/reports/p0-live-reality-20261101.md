# P0 canlı gerçeklik kontrolü — 1 Kasım 2026

Kontrol salt-okunur HTTPS GET isteklerinden oluşur. Bu işlemde oturum açılmadı; veritabanı, hesap, Coolify ayarı, plan, ödeme veya uygulama verisi değiştirilmedi.

## Canlı çalışma alanları

| Alan adı | `/api/status` | Migration | Bekleyen |
|---|---|---:|---:|
| `envantertr.com` | HTTP 200, `ready` | 122 | 0 |
| `firma.envantertr.com` | HTTP 200, `ready` | 122 | 0 |
| `santa.envantertr.com` | HTTP 200, `ready` | 122 | 0 |
| `farma.envantertr.com` | HTTP 200, `ready` | 122 | 0 |

Kaynak adayı `feat/verified-cloud-trials-20261021` 126 EF migration içeriyor. Bu, çalışan imajdan 4 migration ileridedir. Endpoint'in `pendingMigrationCount=0` demesi yalnız çalışan imajın kendi şemasına göredir; adayla tam migration-ID eşitliğini kanıtlamaz. Gerçek production history sorgusu bu oturumda yapılmadı.

## Satış uçları

- `GET https://envantertr.com/api/plans`: HTTP 200, sekiz plan.
- `GET https://envantertr.com/api/checkout/status`: HTTP 200, `available=false`, sağlayıcı yok.
- İyzico işyeri hesabı kullanıcı tarafından henüz bulunmadığı bildirildi. Gerçek tahsilat, webhook ve ödeme sonrası provisioning kabul edilmedi.

## Sonraki işlem

P0-B için `production-migration-history-preflight.sql` ve `catalog-reference-preflight.sql` mevcut üretim DB'sinde salt-okunur çalıştırılmalı; bu bağlantı/çıktı olmadan migration uygulanmamalı. P0-D için iyzico hesabı ve gizli API bilgileri gerekir. Canlıya yazma veya migration yapılmadı.
