# EnvanterTR müşteri API v1

API anahtarları kullanıcı ve etkin şirket üyeliğine bağlıdır. Kullanıcı kendi rolünün izin verdiği kapsamları seçer. `assets:write`, yerleşik veya özel şirket rolünün güncel `assets.write` iznini gerektirir. Okuma kapsamları da ilgili `assets.read` / `people.read` izinleriyle doğrulanır. Anahtar 32 rastgele bayttan üretilir, oluşturma yanıtında bir kez gösterilir ve veritabanına yalnız SHA-256 özeti yazılır. Kullanıcı kendi anahtarlarını listeleyebilir ve iptal edebilir. İptal, üyeliğin kaldırılması, şirketin kapatılması veya kullanıcının kilitlenmesi sonraki API çağrısını reddeder.

Makinece okunabilir OpenAPI 3.1.2 tanımı: [`/openapi/customer-api-v1.json`](../src/EnvanterTR.Web/wwwroot/openapi/customer-api-v1.json). `/api/v1` keşif yanıtı da bu tanıma bağlantı verir.

## Kimlik doğrulama

```http
Authorization: Bearer etr_live_<secret>
```

Anahtarı URL, sorgu parametresi, log veya kaynak kodunda saklamayın. En fazla 10 etkin anahtar/kullanıcı/şirket bulunabilir. Anahtarlar isteğe bağlı olarak 365 gün içinde sona erebilir. İstek limiti IP başına dakikada 120'dir; üretimde yalnız güvenilir proxy başlıkları yapılandırılmışsa istemci IP'si proxy zincirinden alınır.

## Yetkiler

- `assets:read`: Şirket varlıklarının sayfalı listesi. Hassas özel alanlar yalnız şirket sahibi/yöneticisine döner; maliyet alanları dönmez.
- `assets:write`: `POST /api/v1/assets` ile oluşturma ve `PUT /api/v1/assets/{id}` ile düzenleme. Etiket, ad ve aynı şirkete ait kategori zorunludur; marka/model/konum katalog bağları, kota, audit kaydı ve tenant sınırı kontrol edilir. `GET /api/v1/asset-custom-fields` etkin alan tanımlarını ve seçeneklerini verir. `customFields` değerleri şirket tanımına göre doğrulanır; güncellemede alanı göndermemek mevcut değeri korur. Hassas özel alanları yalnız şirket sahibi/yöneticisi yazıp okuyabilir; diğer rollerin yanıtlarında bu değerler gizlenir. Güncelleme, en son GET yanıtındaki `updatedAt` değerini `expectedUpdatedAt` olarak ister ve eski sürümlü yazmayı 409 ile reddeder. Bu anahtarı yalnız güvenilir sunucu tarafında saklayın.
- `assets:write`: `PUT /api/v1/assets/{id}/installed-software` ile Windows ajanının **tam tarama** anlık görüntüsünü eşitler. Varlık şirket ve anahtar kullanıcısının konum kapsamıyla doğrulanır; eksik taramalar reddedilir, en fazla 3.000 uygulama kabul edilir, kaldırılmış uygulamalar silinmeden `IsPresent=false` geçmişinde korunur. API anahtarı sadece güvenilir cihaz/otomasyon hesabında saklanmalıdır.
- `accessories:read`: Şirket aksesuar kataloğu ve stok adetleri. Notlar ve maliyetler dönmez.
- `people:read`: Çalışan numarası, ad, e-posta, departman, iş unvanı, konum, işe başlama ve etkinlik alanları. Özel İK notları/belgeleri dönmez.

Her istekte kullanıcının güncel şirket üyeliği ve rol izinleri tekrar denetlenir. Özel rolün devre dışı bırakılması veya gereken iznin kaldırılması mevcut anahtarın ilgili erişimini de durdurur. `canWriteValue`, anahtar kapsamıyla birlikte güncel yazma iznini gerektirir; hassas alanlar için şirket sahibi/yönetici sınırı korunur. Anahtar başka şirketteki aynı adlı rolün izinlerini kullanamaz; anahtar, tarayıcı oturumu veya diğer uygulama API'lerinin yetkisini devralmaz. Her liste `q`, `page` ve `pageSize` kabul eder; sayfa boyutu 1–100 ile sınırlıdır ve en çok 120 karakterlik arama yapılır.

Varlık oluşturma ve düzenleme gövdeleri isteğe bağlı `insuranceProvider` (en fazla 160 karakter), `insurancePolicyNumber` (en fazla 120 karakter) ve `insuranceExpiresAt` (yyyy-AA-gg) alanlarını kabul eder; bu alanlar varlık listesi yanıtında da bulunur. Şirket CSV dışa aktarımında aynı alanlar seçilebilir; JSON şirket arşivinde varlık alanlarıyla birlikte taşınır.

## Uçlar

- `GET /api/v1` — sürüm/kimlik doğrulama/endpoint bilgisi.
- `GET /api/v1/assets`
- `GET /api/v1/asset-custom-fields` — etkin şirket özel alanlarının tanımları ve seçim seçenekleri; `canWriteValue` alanı belirli API anahtarının kapsamı ve kullanıcının rolüne göre hesaplanır.
- `POST /api/v1/assets` — `assets:write` gerektirir; `CreateAssetRequest` gövdesini ve 201 yanıtını OpenAPI tanımında inceleyin.
- `PUT /api/v1/assets/{id}` — `assets:write` gerektirir; en son `updatedAt` değerini `expectedUpdatedAt` olarak gönderir.
- `PUT /api/v1/assets/{id}/installed-software` — `assets:write` ile tam yazılım taraması yükler; `isComplete: true`, `source` ve `items` gerekir. Windows başlangıç ajanı: [`tools/Collect-InstalledSoftware.ps1`](../tools/Collect-InstalledSoftware.ps1). Tek seferlik tarama için `pwsh -File tools/Collect-InstalledSoftware.ps1 -BaseUrl https://firma.envantertr.com -AssetId <varlık-guid>` çalıştırın; anahtar güvenli istemde sorulur ve komut satırına eklenmez.

Windows Görev Zamanlayıcısı'na günlük ve kullanıcı oturumu açılışında çalışan görev kurmak için yönetici olmayan PowerShell penceresinde [`tools/Install-InstalledSoftwareTask.ps1`](../tools/Install-InstalledSoftwareTask.ps1) betiğini çalıştırın: `pwsh -File tools/Install-InstalledSoftwareTask.ps1 -BaseUrl https://firma.envantertr.com -AssetId <varlık-guid> -DailyAt 03:00`. `assets:write` API anahtarı güvenli istemde bir kez alınır ve Windows DPAPI ile yalnız mevcut Windows kullanıcısına bağlı olarak şifrelenir. Görev bu kullanıcı oturumu açıkken çalışır; son durum aynı klasördeki günlük dosyasına yazılır (`%LOCALAPPDATA%\EnvanterTR\InstalledSoftware`). Betik görev önceden varsa onu değiştirmez. Kurumsal dağıtımda anahtarın yalnız gereken şirket/varlık ve `assets:write` kapsamıyla oluşturulması önerilir.
- `GET /api/v1/accessories`
- `GET /api/v1/employees`
- `GET /openapi/customer-api-v1.json` — bearer şeması, arama/sayfalama parametreleri, yanıt alanları ve hata durumları.
- Oturum içi anahtar yönetimi: `GET/POST /api/integrations/tokens`, `POST /api/integrations/tokens/{id}/revoke` (değişikliklerde CSRF doğrulaması).

`401` geçersiz, sona ermiş veya iptal edilmiş anahtar; `403` geçerli anahtarın kapsamı dışındaki endpoint; `429` hız sınırı yanıtıdır. Tüm şirket verileri sunucuda etkin şirket kimliğiyle filtrelenir.
