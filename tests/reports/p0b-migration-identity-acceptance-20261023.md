# P0-B — canlı migration kimlik karşılaştırması — 23 Ekim 2026

## Sonuç

Canlı veritabanının uygulanmış migration kimlikleri, dağıtılmış `origin/main` migration kümesiyle birebir eşleşiyor. Canlıya uygulanmamış olan tek fark, mevcut şirket yıllık checkout çalışmasının release adayı migration'ıdır; beklenen ve açıkça belgelenmiş bir release farkıdır.

| Kaynak | Adet | İlk / son migration | Sıralı kimlik listesi MD5 |
|---|---:|---|---|
| Coolify'deki canlı PostgreSQL `__EFMigrationsHistory` (salt-okunur transaction) | 118 | `20261002192729_InitialSchema` / `20261020174959_AddSubscriptionCoupons` | `c8694e4a153c5f81d5cface1e0c9f366` |
| `origin/main` (`4d55270827be312775ebffe60c5a6d74011d7802`), migration attribute kümesi | 118 | `20261002192729_InitialSchema` / `20261020174959_AddSubscriptionCoupons` | `c8694e4a153c5f81d5cface1e0c9f366` |

Liste özeti, alfabetik sıralanmış migration kimliklerinin LF ile birleştirilmesiyle hesaplandı. Canlı sorgu `BEGIN READ ONLY` içinde `COUNT`, `MIN`, `MAX` ve `md5(string_agg("MigrationId", chr(10) ORDER BY "MigrationId"))` döndürdü; ardından `ROLLBACK` çalıştı. Müşteri tabloları veya satır içerikleri okunmadı.

## Release adayı farkı ve veri güvenliği

- Aday kod: `036b52a654da6cda87f7dec15285373303f4f9e5`; EF kümesi 119 kimliktir.
- Tek ek kimlik: `20261023190000_AddExistingCompanyCheckoutContext`.
- `Up()` yalnızca `CompanyCheckoutOrders` üzerine iki nullable UUID alanı ve bir check constraint ekler; var olan satırları silmez veya değiştirmez. `Down()` yeni alanları düşürdüğünden, rollout sonrası checkout verisi yazılmışsa otomatik `Down()` güvenli geri dönüş sayılmaz; geri dönüş, yedek/restore provasıyla önceki imaja dönüş olmalıdır.
- Migration burada çalıştırılmadı; canlı veritabanı ve Coolify ayarları değiştirilmedi.
- 23 Ekim taze PostgreSQL + medya/anahtar yedeği ve izole restore eşit satır provası `tests/reports/coolify-sha-save-triage-20261023.md` içinde. Otomatik/off-site yedek yoktur.

## Doğrulama

- Yerel `origin/main` migration attribute taraması: 118 benzersiz kimlik; canlı liste özetiyle count, uç kimlikler ve MD5 eşleşti.
- GitHub Actions run `40019065157`, aynı head `217261746e62a5f6debbe6010b71e40a033c7b2d`: Linux tenant security, Linux PostgreSQL tenant acceptance ve Windows deployment/backup safety **3/3 success**.

## Kapsam sınırı

Canlı uygulama `origin/main` ile uyumlu; PR #25 release adayı bir ileri migration içeriyor. Bu rapor staging'de candidate migration çalıştırıldığını veya canlıya deploy yapıldığını iddia etmez. Staging ortamı ve gerçek iyzico/SMTP erişimi sağlanmadan migration/deploy/satış kapısı açılmaz.
