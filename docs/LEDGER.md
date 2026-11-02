# Çalışma paketi ledger'ı

| WP | Hedef / kapı | Bitti tanımı | Dosya sınırı | Durum | Deneme | Kanıt |
|---|---|---|---:|---|---:|---|
| WP-001 | F0 ölçüm baz çizgisi: Release build, tüm test projeleri, migration envanteri, katalog ölçümü | Release build başarılı; mevcut tek test projesi ve tam hosted Chromium kabulü çalıştı; migration sayısı ve katalog durumları kaydedildi; `PROJECT-STATUS.md` tek tabloya indirildi | 10 | DONE | 2/3 | Release build 0 uyarı/0 hata; izole PostgreSQL/security 244/244 PASS (40:12); tam Chromium kabulü exit 0, 53 sidebar hedefi; 83 source verifier 80 PASS/3 FAIL; UI envanteri eski; kaynak migration 121, canlı salt-okunur bildirim 122. `tests/reports/summary.json`. Üretim DB/deploy değiştirilmedi. |

## F0 baz çizgisi notları — 2 Kasım 2026

- Hosted Chromium ilk tam koşusu rezervasyon fixture'ında sabit geçmiş tarih kullandığı için başarısızdı. Test tarihi çalışma anına göre ileri alındı; ardından “Faturalar” menü hedefinin 53. rota olduğu ve `companyInvoicesPage` yüzeyine bağlanması gerektiği kaynak HTML ile doğrulandı. Ayrıntılı modül ve tam regresyon paketi PASS.
- Canlı salt-okunur kontrol: apex ve firma `/api/status` HTTP 200/ready; `/api/plans` 8 plan; checkout `available:false`. Uygulama 122 migration bildiriyor, bu çalışma ağacında 121 migration var. `node tools/Check-CoolifyProduction.mjs` bu farkla exit 1 verdi; F1'e geçiş öncesi deployed revision/migration ID açıklanmalı.
- Üç kaynak verifier hatası ayrı ayrı kaydedildi: AssetModelDetailsUi eski kaynak dizesi beklentisi; PlatformAdminExperience ve PlatformMfaBrowserAcceptance yeniden düzenlenmiş akışla uyumsuz kaynak regex'i. İlgili PostgreSQL/model ve Chromium davranış kabulü geçti; verifier'lar düzeltilmiş kabul sayılmadı.
- Korunan 1 Kasım gözlemleri: tenant host health sonucu tek başına her hostta ayrı tenant kanıtı değildir; slug yönlendirme değişikliği yalnız statik verifier ile görülmüştü; MFA challenge düzeltmesi kaynakta yapılmıştı. Güncel tam hosted Chromium bu oturumda login/MFA kabul bölümünü geçti; Santa/Farma/Demo3 canlı firma kabulü ve MFA bug'ının gerçek hesapta doğrulanması yapılmadı.
