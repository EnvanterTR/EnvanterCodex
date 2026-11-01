# İlerleme kaydı

## 2026-11-01 — P1 tenant query filter

- Hedef: §6.2 ve §6.5 için şirket kapsamını `CompanyId` taşıyan EF Core kök entity'lerine genel query filter olarak eklemek; uç noktalardaki mevcut yetki/kapsam kontrollerini korumak.
- Değişiklik: `AppDbContext`, doğrulanmış şirket oturumunun `company_id` değerini model filtrelerine uygular. İsteğe bağlı `CompanyId` alanlarında aynı şirketin ve paylaşılan `null` kayıtların görünürlüğü korunur. Mevcut silinmiş varlık filtresi tenant filtresiyle birleştirilir. Platform API'sindeki kapsam kaldırma, yalnız platform yöneticisi DB'den doğrulandıktan sonra etkinleşir. HTTP isteği dışındaki migration/işçi/test bağlamları mevcut davranışta kalır.
- Güvenlik incelemesi: Ana daldaki 27 `IgnoreQueryFilters` çağrısı tarandı; bulunan çağrılar asset sorgularında ve sorgu/raw SQL koşullarında `CompanyId` açıkça kullanılıyor. Bu inceleme PostgreSQL uçtan uca kabul testinin yerine geçmez.
- Doğrulama: `dotnet test tests/EnvanterTR.SecurityAcceptance/EnvanterTR.SecurityAcceptance.csproj --no-restore --filter FullyQualifiedName~AppDbContextTenantFilterTests` — 1/1 geçti; bütün CompanyId taşıyan EF kök entity'lerinde filtre bulunduğu, ortak Identity kullanıcı tablosunun CompanyId filtresi almadığı, aynı `DbContext`'te tenant değişiminin SQL'e uygulandığı ve Asset soft-delete koşulunun korunduğu doğrulandı. Test projesi build'i başarılı; 4 mevcut ASP.NET deprecation uyarısı var, yeni ürün kodu uyarısız derlendi. `git diff --check` geçti.
- Açık: `TenantQueryFilterAcceptanceTests` PostgreSQL-backed senaryosu henüz çalıştırılmadı; bu makinede `ENVANTERTR_SECURITY_TEST_CONNECTION` yok. Platform admin API kabulü, tüm P1 güvenlik regresyonu ve canlı doğrulama da açık. Bu iş paketi Hedef §6.2/§6.5'i tamamlanmış saymaz.
- Sonraki adım: değişikliği ayrı PR'da CI PostgreSQL tenant kabuline gönder; `IgnoreQueryFilters` yollarıyla birlikte tam tenant güvenlik paketinin sonucunu incele. Başarı olmadan birleştirme veya canlıya alma iddiası yok.

## 2026-11-01 — P1 giriş/üyelik kapsamı regresyonu

- CI run `41274988090` içindeki PostgreSQL tenant kabul job'ı başarısızdı. Job log'una bağlı bağlantı bu oturumda erişim vermedi; check özeti ve mevcut acceptance kaynağı incelendi. Girişte şirket üyeliği seçimi, doğrulanmış kullanıcının bütün etkin üyeliklerini görmelidir; `/api/session` ise kullanıcıya bağlı diğer şirket portallarını da göstermelidir. Tenant query filter bu kimlik/üyelik temelli çapraz-kiracı sorguları daraltmamalıdır.
- Düzeltme: `AccountEndpoints` giriş/şirket seçimi ve session üyelik listesini, kullanıcı/üyelik kimliği ve etkinlik koşulları açık kalacak biçimde `IgnoreQueryFilters` ile çalıştırıyor. Platform dışı endpoint kapsamı genişletilmedi. MFA'yı kapatma kontrolü tüm aktif şirket üyeliklerinde zorunlu MFA olup olmadığını denetliyor.
- Doğrulama: web projesi `dotnet build --no-restore` başarılı, 0 uyarı/0 hata. Tenant filter testleri 1/1 geçti; mevcut 4 ASP.NET deprecation uyarısı test projesinde sürüyor. PostgreSQL kabul job'ı bu makinede yeniden koşturulamadı (`ENVANTERTR_SECURITY_TEST_CONNECTION` yok); yeni commit sonrası GitHub CI sonucu beklenmeli. `git diff --check` bir sonraki commit öncesi yapılacak.

## 2026-11-01 — read-only P0 live readiness recheck

- Public HTTP recheck: `firma.envantertr.com/health`, `farma.envantertr.com/health`, `santa.envantertr.com/health` each returned HTTP 200 `Healthy`; this resolves only the old §4.6.2 outage observation for these endpoints at this timestamp, not account workflow acceptance.
- `https://envantertr.com/api/plans` returned 8 Cloud annual plans with annual prices 2,400 / 4,200 / 6,000 / 9,000 / 12,000 / 14,400 / 24,000 / 36,000 TRY and `monthlyPrice=0`. `/api/checkout/status` returned `available=false`, provider `null`; live purchase remains unavailable. The user confirmed there is no iyzico merchant account; production checkout cannot be declared complete until provider credentials/account are configured and end-to-end payment/webhook acceptance passes.
- This read-only check does not verify admin area, database migration count, email delivery, customer data, or backup restoration. It made no production writes.
