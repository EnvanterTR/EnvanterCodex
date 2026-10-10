# EnvanterTR müşteri API v1

API anahtarları kullanıcı ve etkin şirket üyeliğine bağlıdır. Şirket sahibi veya yöneticisi olmayan kullanıcı da kendi hesabı için anahtar oluşturabilir; yalnızca rolünün okuyabildiği kaynaklara ait salt okunur yetkileri seçebilir. Anahtar 32 rastgele bayttan üretilir, oluşturma yanıtında bir kez gösterilir ve veritabanına yalnız SHA-256 özeti yazılır. Kullanıcı kendi anahtarlarını listeleyebilir ve iptal edebilir. İptal, üyeliğin kaldırılması, şirketin kapatılması veya kullanıcının kilitlenmesi sonraki API çağrısını reddeder.

## Kimlik doğrulama

```http
Authorization: Bearer etr_live_<secret>
```

Anahtarı URL, sorgu parametresi, log veya kaynak kodunda saklamayın. En fazla 10 etkin anahtar/kullanıcı/şirket bulunabilir. Anahtarlar isteğe bağlı olarak 365 gün içinde sona erebilir. İstek limiti IP başına dakikada 120'dir; üretimde yalnız güvenilir proxy başlıkları yapılandırılmışsa istemci IP'si proxy zincirinden alınır.

## Yetkiler

- `assets:read`: Şirket varlıklarının sayfalı listesi. Maliyet ve özel alanlar dönmez.
- `accessories:read`: Şirket aksesuar kataloğu ve stok adetleri. Notlar ve maliyetler dönmez.
- `people:read`: Çalışan numarası, ad, e-posta, departman, iş unvanı, konum, işe başlama ve etkinlik alanları. Özel İK notları/belgeleri dönmez.

Her istekte kullanıcının güncel şirket rolü tekrar denetlenir. Erişim yalnız `/api/v1` sürümlü, salt okunur uçlarına açıktır; anahtar, tarayıcı oturumu veya diğer uygulama API'lerinin yetkisini devralmaz. Her liste `q`, `page` ve `pageSize` kabul eder; sayfa boyutu 1–100 ile sınırlıdır ve en çok 120 karakterlik arama yapılır.

## Uçlar

- `GET /api/v1` — sürüm/kimlik doğrulama/endpoint bilgisi.
- `GET /api/v1/assets`
- `GET /api/v1/accessories`
- `GET /api/v1/employees`
- Oturum içi anahtar yönetimi: `GET/POST /api/integrations/tokens`, `POST /api/integrations/tokens/{id}/revoke` (değişikliklerde CSRF doğrulaması).

`401` geçersiz, sona ermiş veya iptal edilmiş anahtar; `403` geçerli anahtarın kapsamı dışındaki endpoint; `429` hız sınırı yanıtıdır. Tüm şirket verileri sunucuda etkin şirket kimliğiyle filtrelenir.
