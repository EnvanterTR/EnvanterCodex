## 26 Ekim 2026 — güncel kanıt özeti; alttaki eski açıklar tarihsel

- Canlı temel sürüm `9585fae`, şema122/0. Sekiz plan yayımlı; checkout ve public deneme kapalı. HTTPS callback çalışan ortama uygulanmıştır; önceki “runtime'a uygulanmadı” kaydı artık güncel değildir. Kanıt: `tests/reports/paid-trial-notification-release-20261026.md`, `tests/reports/onboarding-production-release-20261026.md`.
- SMTP genel olarak engelli değildir: uygulama parola yenileme iletisi Google posta kutusuna, Farma iletisi dış Microsoft SMTP sunucusuna TLS ile teslim edilmiştir. Dış SMTP kabulü gelen kutusu konumu veya her belge e-postasının teslim kanıtı değildir. Kanıt: `tests/reports/google-relay-production-acceptance-20261025.md`.
- Yıllık yaşam döngüsünün beş belirli senaryosu gerçek Identity/API/PostgreSQL ve sentetik sağlayıcıyla5/5 geçti: kalan ücretli sürede plan değişimi reddi, süre dolunca plan değişimi, iptal edilmiş dönemde aynı planla yenileme ve askıdaki şirket reddi. Bu yerel kapsam yeniden eksikmiş gibi ele alınmaz; gerçek sağlayıcı ve tam müşteri kabulü açıktır. Kanıt: `tests/reports/annual-lifecycle-acceptance-20261024.md`.
- Son davet/parola düzeltmesi `27e9120` yerel6/6 API, tarayıcı ve exact sunucu staging/kurtarma kabulünden geçti. Tam CI40390600764 PostgreSQL işi sürerken doğrulandı; düzeltme henüz canlı değildir. Kanıt: `tests/reports/activation-atomicity-20261026.md`, `tests/reports/activation-atomicity-server-staging-20261026.md`.
- Gerçek iyzico hesabı/ödeme, Santa imzası ve PDF alıcı teslimi, operatör/Farma/tam üçüncü şirket ve iki yönlü canlı GET/PUT izolasyonu açık kalır. Bu özet tam P0 veya satışa hazır kabulü değildir.

## 26 Ekim 2026 — P0 onboarding rehberi yayımlandı; gerçek akış kabulü açık

Hedef12.1'in beş adımlı müşteri kurulum rehberi9585fae/schema122ile canlıda. Yerel6/6API, Chromium açık/koyu/mobil, tam3CIişi, exactstaging/95tablo korunması ve taze yedek/kurtarma geçti. Üçhostta yeniCSSbaytları, şirketHTMLi, runtimeimajı ve anonim401sınırları doğrulandı. Gerçek oturumlu müşteri tamamlama/merchant uçtanuca kabulü sayılmaz. Kanıt/sınırlar: tests/reports/onboarding-production-release-20261026.md; tarihsel bulgular: docs/P0-ONBOARDING-GAP.md.

## 26 Ekim 2026 — kullanıcı iyzico hesabı olmadığını doğruladı

Kullanıcı doğrudan “Henüz iyzico hesabım yok” yanıtını verdi. Eksiklik, kaybolmuş/parolası bilinmeyen bir hesap olarak ele alınmaz; API anahtarı tekrar tekrar istenmez. Gerçek ödeme için işyeri başvurusu ve onay gereklidir. Resmî Türkçe sayfa https://www.iyzico.com/isim-icin/hesap-olustur adresinde kontrol edildi ve IAB'de kullanıcı için boş bırakıldı; hiçbir kimlik/vergi/telefon/parola bilgisi gönderilmedi, sözleşme kabul edilmedi ve hesap oluşturulmadı. Başvurunun kimlik/sözleşme adımı kullanıcıya aittir.

Mevcut EnvanterTR callback hazırlığı kalıcı kaydedildi fakat runtime'a uygulanmadı. API/secret yok; gerçek ödeme ve yeni denemeler kapalı kalır. Yerel sentetik sağlayıcı testleri resmî sandbox veya canlı merchant kabulü değildir. Resmî sandbox dokümanı ayrı hesap/anahtar çifti öngörür: https://docs.iyzico.com/en/getting-started/preliminaries/sandbox. Canlı ortam test anahtarıyla açıkmış gibi gösterilmez.

Önceki teknik denetim ve yerel kabul kayıtları aşağıdadır; tarihsel canlı118 bilgileri son5018yayınıyla karıştırılmaz.

# P0 checkout release gaps — 23 October 2026

These are open requirements, not completed features. Local synthetic acceptance and a green PostgreSQL suite do not establish live sales readiness.

## Existing trial company to paid subscription — INCOMPLETE

The approved commercial flow gives the customer a 15-day trial followed by an annual payment. The existing company, portal, memberships and inventory must survive the change.

The original audit found that the anonymous new-company checkout rejected occupied company slugs both before and after payment. The current candidate adds a distinct authenticated `/api/subscription/checkout` API and immutable existing-company/owner context. It activates the existing company through the shared provisioning service after verified payment. A server-rendered `/subscription/checkout` page now lets a company owner choose an eligible annual Cloud plan, enter billing information, reauthenticate with password and MFA, and continue to the existing iyzico checkout. An expired-trial notice links to that page; the handler independently enforces owner authorization. Existing company data, members and portal identity remain tied to the existing-company transaction path. The expired-trial browser acceptance now passes against actual Kestrel/PostgreSQL with a synthetic provider; real-provider acceptance remains open. Platform subscription extension/edit routes remain separate administrator adjustments.

Required before accepting trial-to-paid sales:

- Bind checkout to the authenticated company owner and its immutable company ID; reject other company/user contexts and enforce CSRF/MFA requirements.
- Snapshot the purchased annual plan, price and payment purpose in the order. Allow the owner to reach this purchase action after trial expiry while retaining normal write restrictions.
- After provider verification, activate the paid period for that same company atomically with the payment/order state. Preserve trial history, company ID, portal, memberships and inventory. Enforce package capacity before accepting a downgrade.
- Reject failed/mismatched payments; replay and concurrent callbacks must not duplicate subscriptions or extend the purchased period twice. Deleting the target company must not turn an existing-company purchase into new-company provisioning.
- Complete real-provider purchase/return/status acceptance. On 24 October, the expired-trial owner browser flow and non-owner denial passed against the actual Kestrel/API/PostgreSQL with a synthetic provider. Full paid-plan lifecycle and live tenant-boundary acceptance remain open.

The original source audit is retained in commit `3c63980`. Current candidate acceptance is recorded separately under `tests/reports/existing-company-checkout-*` and `tests/reports/company-checkout-page-acceptance-*`. The latest isolated PostgreSQL candidate covers concurrent callback delivery: the order-row lock plus the existing company-row lock serialize callback processing, and both callback responses report the single activated subscription. On 24 October, targeted cases also covered a verified provider result with the wrong currency, an unavailable provider result, target-company deactivation and deletion. The payment is never activated in those cases; a deleted company is not recreated. The server-rendered expired-trial browser flow now passes; real provider acceptance and the complete plan-change lifecycle still need verification. New-company checkout acceptance alone does not cover those cases.

The separate existing-company browser acceptance is recorded in `tests/reports/existing-company-browser-acceptance-20261024.md` (exit 0). It verifies password/MFA/cookies, server-rendered form submission, failed-password field handling, mobile layout, provider Location, callback replay, paid history and retained tenant data. Real provider acceptance and the complete plan-change lifecycle remain open.

## Email verification before direct paid checkout — LOCAL ACCEPTANCE PASSED

The original audit found that anonymous `/api/checkout/initialize` accepted an unverified owner email. The candidate now sends a 30-minute protected email/plan-bound link, requires explicit CSRF-protected confirmation, and independently validates `emailVerificationToken` before provider initialization. Missing/altered/expired/wrong-purpose or mismatched proof returns 428 with no provider initialization or order for that request. SMTP configuration is required by public readiness and direct initialization; a previously valid proof cannot bypass removed SMTP configuration. Existing-company owner purchase retains its confirmed-account/password/MFA flow.

`tests/reports/checkout-email-verification-acceptance-20261024.md` records 15/15 isolated API/PostgreSQL cases plus Chromium checkout acceptance and the prior quota regression. SMTP is captured locally and iyzico is synthetic. This closes the local pre-payment ownership gap; real inbox/provider acceptance and release gates remain open.

## Real provider and customer gates — OPEN

Real iyzico merchant acceptance, real SMTP/inbox delivery, full authenticated staging acceptance, and the Santa/Farma/Demo3 acceptance scenario remain required. On 24 October, fresh real SQL/media/key/image backup, restore rehearsal, cloned 118→120 data preservation and eleven basic isolated staging checks passed (real-server-staging-acceptance-20261024.md). These do not close the remaining provider/customer gates or establish sale readiness.

The 24 October read-only public probe is recorded in `tests/reports/p0-live-sales-readiness-probe-20261024.md`: live reports 118 applied migrations, but `/api/plans` is empty, `/api/checkout/status` is unavailable, and the candidate `/api/trials/status` route returns 404. This confirms the candidate sales/onboarding path has not reached production; migration readiness alone does not establish a release.
