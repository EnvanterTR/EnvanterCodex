# EnvanterTR — çalışma devam kaydı

Bu kayıt, `docs/ACTIVE-WEB-V1-GOAL.md` ve depo kökündeki kullanıcı hedefiyle birlikte okunur. Eski zaman damgalı notlar güncel kanıt değildir.

## 2026-11-01 — profil fotoğrafı depolama CI tamamlandı

- Durum: CI doğrulandı; PR hâlâ taslak/açık, canlıya alınmadı.
- PR #29, commit `940a8b6a9ee1607a3b9ac0375aac21e2b34c065d`: dört işin dördü `success`. Linux PostgreSQL tenant kabul işi `19m34s` sonra başarıyla tamamlandı; run `41269102206` terminal sonucu `completed/success`.
- Web build yerelde 0 uyarı/0 hata; SecurityAcceptance build 4 mevcut deprecation uyarısı/0 hata. Yerel hedefli HTTP/PostgreSQL kabul testi çalıştırılmadı; aynı senaryo hosted CI PostgreSQL işinde geçti.
- PR #29 hâlâ Draft ve `main` dalına merge edilmemiştir. Canlıya uygulanmadı. PR #28 de ayrı bir açık taslak olup 4/4 CI geçmiştir.
- Bu tur aynı canlı kontrolleri tekrar etmedi; PR merge/yayın için kullanıcı oturumu sağlayan GitHub bağlantısı mevcut araçlarda bulunamadı. GitHub web sayfaları anonim görünümde; CI logları giriş gerektiriyor. Repo erişim aracı önerildi, kullanıcı bağlantısı doğrulanmadan kullanılmayacak.
- Sıradaki: güvenli GitHub bağlantısı geldikten sonra, önceki “canlıya koy” yetkisi kapsamında PR #28/#29’u normal PR akışıyla ana dala almak; Coolify source/deploy chain’i ve backup gate’i doğrulayıp dağıtmak; canlı bundle SHA/özellikleri HTTP ile karşılaştırmak. O sırada P1 ve diğer V1 işleri sürer.

## 2026-11-01 — hedef promptu ve güncel teslim kapısı

- Durum: DEVAM EDİYOR
- Değişen dosyalar: depo dışındaki `PROJEYI-TAMAMLAMA-PROMPTU.md` (çelişkili/eski talimatlar için güvenli ve güncel yürütme önceliği eklendi); bu dosya.
- Çalışma kökü doğrulandı: `Envanter20260101/Envanter20260101`. Mevcut checkout `feat/platform-checkout-readiness-20261020` @ `36c5937440229b6105c3c8b5f9e86cafa655e2c0`; `origin/main` `ba5b1ef0afae7fa5ad961eba30d4f3f045a5f203`. Mevcut checkout güncel ana dal değildir. Kullanıcı değişikliklerini koru.
- Canlı salt-okunur HTTP kanıtı: `https://envantertr.com/api/status` ve `https://firma.envantertr.com/api/status` → 200, `ready`, 122 migration / 0 bekleyen. `/api/plans` → sekiz Cloud yıllık planı. `/api/checkout/status` → `available:false` (ödeme sağlayıcısı yok).
- PR #28: `d2437c92afce73c611fedb223947b5af80421bb6`, açık taslak; dört CI işi `success`. PR #29: `940a8b6a9ee1607a3b9ac0375aac21e2b34c065d`, açık taslak; Linux tenant security, Windows safety ve gallery CI `success`, Linux PostgreSQL tenant acceptance `in_progress` (2026-11-01 son kontrolü). PR merge edilmiş veya canlıya alınmış sayılmaz.
- Canlı `product-gallery.js` SHA-256 `88912A4D3DFA5B0255FCBFD3995E166680FB64395C97CB2F16FDFC5DB0995291`, ETag `6ae42d12-1362`, Last-Modified `Fri, 30 Oct 2026 05:37:22 GMT`; `origin/main` JS kaynak SHA-256 `557FBB335AC89601C3DF01CB0B0A4BA745DF4882D1EE4427E6D4223B9CA8298C`. Kaynak ve canlı dosya aynı değil; merge/deploy sonrası canlı SHA yeniden doğrulanmalı.
- Doğrulama sınırı: HTTP health, plan ve dosya SHA kontrolleri canlı uygulamanın tam müşteri kabulü, ödeme veya deploy onayı değildir. Windows UI bilgisayar-kullanım aracı bu turda URL güvenini sağlayamadığı için durdu; UI üzerinden PR/Coolify değişikliği yapılmadı.
- Komut + çıktı: Git worktree/branch/HEAD sorguları; GitHub REST PR/check-run GET; dört canlı HTTPS GET ve JS karşılaştırması. CI #41268300851 başarı; #41269102206 son kontrolde halen çalışıyor.
- Test: Bu kayıt turunda yeni ürün testi çalıştırılmadı. PR #28 CI ve yerel Chromium kabulü önceki kayıtta doğrulanmış; PR #29 yerel build geçmiş, hedefli PostgreSQL kabulü CI sonucunu bekliyor.
- Sıradaki iş: aynı Actions run `41269102206` terminal sonucunu kontrol et; PR #28’in yeşil değişikliğini canlıya alma akışında ilerlet (mevcut source SHA, deploy ayarı ve manuel/otomatik dağıtım yolunu güvenilir salt-okunur kaynaktan doğrulamadan deploy iddiası yok); sonra PR #29’u yalnız CI terminal/yeşil olduktan sonra kapat. Iyzico hesabı olmadan checkout dış bağımlılığı açık kalır; diğer V1 maddeleri devam eder.

## 2026-11-01 — önceki durum notlarının düzeltilmesi ve tamamlama talimatı

- Durum: Denetim sürüyor; proje tamamlandı veya canlıya alındı denmiyor.
- Değişen dosya: depo dışı `PROJEYI-TAMAMLAMA-PROMPTU.md` baştan düzenlendi. Tekrarlı audit'i önleme, ilk günden bugüne kaynak tarihçesi, Hedef'ten tek izlenebilir kabul tablosu, sır saklama, güvenli üretim yayını, yüzde hesabı ve sürekli devam kuralları tek talimatta toplandı.
- Kanıtlı kaynak güncellemesi: `origin/main` = `d16f2fb2a6e4f333b617ea4a9bf1cf673d58946d`; PR #28 `4d8d2bccc89cbf0ba4a09911b860c050cf078282`, PR #29 `d16f2fb2a6e4f333b617ea4a9bf1cf673d58946d` olarak ana dala alınmış. Eski bu dosyadaki “PR'lar açık/taslak” notları bu kayıtla geçersizdir.
- Ana dal CI run `41270953843` HEAD'i `d16f2fb2a6e4f333b617ea4a9bf1cf673d58946d` için son kontrolde `in_progress`: galeri tarayıcı işi success; Windows deployment/backup, Linux tenant security, Linux PostgreSQL tenant acceptance hâlâ çalışıyordu. Henüz yeşil terminal sonuç yok.
- Güncel canlı salt-okunur HTTPS kanıtı: `envantertr.com/api/status` ve `firma.envantertr.com/api/status` HTTP 200, `ready`, 122 migration / 0 bekleyen. `/api/checkout/status` HTTP 200 `available:false`; iyzico/ödeme sağlayıcısı yapılandırılmadığı için gerçek ödeme akışı kapalı. `/api/plans` HTTP 200 ve Cloud yıllık planları mevcut. Bu durum, son main commit'inin canlıda olduğunu veya gerçek müşteri kabulini kanıtlamaz.
- Çalışma ağacı: gerçek repo `Envanter20260101/Envanter20260101`; aktif checkout `feat/platform-checkout-readiness-20261020` @ `36c5937`; kirli/untracked mevcut çalışma korunuyor. 219 status kaydı olduğu için o checkout'ta toplu reset/stash/temizlik yapılmamalı. `origin/main` ayrı SHA'dadır.
- İlk gözlemde GitHub API çağrısı credential protokol/host sağlamadan reddedildi; doğru `protocol=https, host=github.com` girdisiyle mevcut credential helper çalıştı. Kimlik bilgisi ekrana yazdırılmadı. Canlı API sorguları PowerShell HTTPS GET ile alındı.
- Prompt dışındaki durum belgelerinin bir kısmı eski (ACTIVE-WEB-V1-GOAL.md, USER-REQUIREMENTS-CHECKLIST.md ve tarihli PRODUCT-COVERAGE/PROJECT-STATUS notları). Geçmiş snapshot'lar güncel kanıt diye kullanılmamalı; birleştirilmiş Hedef kabul tablosu henüz bu turda tamamlanmadı.
- Test: Bu oturumda ürün testi çalıştırılmadı; gerekli ana dal CI hâlâ çalışıyor. Prompt dosyası içerik güncellemesidir.
- Sıradaki somut işler: CI run `41270953843` terminal sonucunu bir kez kontrol et; canlı bundle SHA'sını ana dal kaynağıyla karşılaştır; Coolify'nin güncel kaynak/otomatik-manuel yayın ayarını ve son başarılı release SHA'sını yetkili read-only kanıtla doğrula. Yayın eşleşmiyorsa açıkça yayın engelini kaydet ve kalan P1 kabul maddesine geç.

## 2026-11-01 — Hedef v30 gerçek durum başlangıç denetimi

- Durum: Başlangıç denetimi belgelendi; Hedef'in tüm gereksinim matrisi hâlâ açık.
- Değişen dosya: `docs/AUDIT-REALITY.md` eklendi. İlk commit tarihi 2026-10-04, eski Snipe-IT referans ağacı ile mevcut C# repo ayrımı, PR/CI/main, canlı health/checkout, galerinin source/live SHA farkı ve P0/tenant kapıları kanıtlarla özetlendi. Tarihli 65–75% raporlarının güncel yüzde olmadığı işaretlendi.
- Bu denetim, Hedef 4.2'nin bütün `PROJECT-STATUS.md` / `PRODUCT-COVERAGE.md` / `ROADMAP.md` yüzde iddialarını tek tek kaynak/test satırına indirme işini henüz bitirmiyor; tek güncel atomik kabul tablosu oluşturulmadı. Önceki raporlardaki modül yüzdeleri güncel gerçek kabul kabul edilmedi.
- Run `41270953843` yeniden aynı head için sorgulandı; üç iş `success`, Linux PostgreSQL tenant acceptance `in_progress` kaldı. Çalışma hâlâ aktiftir; yeniden tetiklenmedi.
- `Hedef.txt` v30 §4.6 ve §20'ye göre bu kanıtta: canlı migration bekleyen sayısı ve firma health sorunu şu an yok; gerçek checkout/ödeme disabled; firma oturum ve Santa/Farma/Demo3 tam uçtan uca izolasyon kabulü, taze deploy/rollback kapısı açık.
- Doğrulama: `git log --reverse origin/main` ile ilk commit `38744c6` / 2026-10-04; hedef dosyada 54 Markdown bölüm başlığı; kaynak/live JS hash karşılaştırması; canlı iki health + checkout + plan HTTPS GET; main run/jobs authenticated GitHub API read-only.
- Sıradaki: ana dal CI run terminal sonucunu kontrol et. Aynı anda Hedef §4.2 için tekillik korunarak geçmiş yüzdeleri/iddiaları atomik kabul satırlarına dönüştür; en yüksek açık ve uygulanabilir P0/P1 işten kod/test kanıtıyla devam et.

- Denetim devamı: `docs/AUDIT-REALITY.md` artık Hedef §15'in 8 kapısını ve `PROJECT-STATUS.md` / `PRODUCT-COVERAGE.md` / `ROADMAP.md` içindeki tarihli yüzde satırlarını tek tek listeliyor. `rg` taramasında ROADMAP'te yüzde iddiası bulunmadı; PROJECT-STATUS/PRODUCT-COVERAGE sayı/payda/test kanıtları bugünkü tam ürün durumu değil.
- Güncel GitHub Actions run `41270953843` tekrar canlı olarak sorgulandı: 3/4 iş `success`; Linux PostgreSQL tenant acceptance hâlâ `in_progress` (03:54:09Z başlangıç; son gözlemde test adımı çalışıyordu). Bu doğrulanmış bekleyiştir; aynı run'ı durdurmadım/yeniden başlatmadım.
- Canlı health/checkout aynı turda yeniden sorgulandı: apex + firma 200 ready, 122/0; checkout `available:false`. `origin/main` `d16f2fb`; yerel ana checkout `36c5937`, status girişleri 220'ye yükselmiş (bu eklenen raporlar da untracked kayıtlarına dahil); kullanıcı dosyaları korunuyor.

## 2026-11-01 — Coolify release gerçekliği ve tarihli yüzde iddiaları

- `deploy/coolify/README.md` başına güncel, kanıtlı deployment kesiti eklendi. Belgeleniyor: main CI ayrı, Coolify deploy otomatik olduğu varsayılamaz; main SHA ve live bundle eşleşmiyor; üretim migration/deploy bu turda yapılmadı. Altındaki 20 Ekim snapshotları geçmiş kabul olarak kaldı.
- `docs/AUDIT-REALITY.md` Hedef §15 için 8 V1 kapısını ve PROJECT-STATUS/PRODUCT-COVERAGE yüzdelerinin her tarihli kaynağını/paydasını/test kapsamını tabloya bağladı; ROADMAP içinde yüzde ifadesi bulunmadı. Hepsi güncel V1 sonucu yerine geçmiyor.
- `git diff --check` geçti; Hedef bağlantısı dosya sistemiyle doğrulandı. Ürün kodu değiştirilmedi, ürün testi çalıştırılmadı.
- Aynı canlı Actions run `41270953843` son kez sorgulandığında 3/4 success, Linux PostgreSQL tenant acceptance `in_progress`; gözlem adımı `Run isolated PostgreSQL acceptance suite`. Gerçek run halen aktif olduğu için onu beklemeye bırakıyorum.

## 2026-11-01 — main CI terminal success

- GitHub Actions run `41270953843` head `d16f2fb2a6e4f333b617ea4a9bf1cf673d58946d` now terminal `completed/success`, 4/4: Linux tenant security, homepage gallery browser, Linux PostgreSQL tenant acceptance, Windows deployment/backup safety.
- `docs/AUDIT-REALITY.md` ve `deploy/coolify/README.md` CI sonucuna göre düzeltildi; profil fotoğrafı/güvenlik görseli kaynak kabulü doğrulandı. Canlı SHA doğrulanmadı.
- Yeni read-only Coolify kök HTTP GET'i `http://169.58.53.189:8000/` HTTP 200 / title Coolify verdi. Deploy ile ilgili bu makine ortam değişkeni adı bulunmadı. Panelin uygulama kaynağı/deploy ayarı authenticated yönetim görünümünden doğrulanamadı; build/CI yeşil diye deploy başlatılmış sayılmaz. Kimlik bilgisi denemesi yapılmadı.
- `git diff --check` geçti (belge değişikliği). Canlı API aynı turda hazır/122/0; ödeme `available:false`; galeri dosya SHA'sı main'den farklıydı.
- Sıradaki: Coolify'de doğru resource'un main SHA `d16f2fb` ile deploy edilmesini güvenli ve yetkili bir yönetim oturumuyla doğrula; taze yedek/rollback koşulunu kontrol et; sonra canlı asset SHA ve gerçek UI kabulini yap. Bu yetki yolu açılana dek Hedef'in diğer açık P1 test/ürün gereksinimlerini sürdür.

## 2026-11-01 — Hedef v30 bölüm indeksi ve yeniden doğrulama

- `docs/AUDIT-REALITY.md`'ye Hedef'in Bölüm 0–21 için 22 satırlık bölüm bazlı denetim indeksi eklendi; açık/kanıtlı/eksik alanlar ile önceki 8 kabul kapısı eşlendi. Bu özet, Hedef'in çok sayıdaki alt gereksinimini tek tek doğrulama işinin yerine geçmez.
- Güncel recheck: main `d16f2fb2a6e4f333b617ea4a9bf1cf673d58946d`, run `41270953843` completed/success 4/4; apex ve firma 122/0 ready; checkout `available:false`; canlı galeri SHA hâlâ kaynak SHA'dan farklı ve Last-Modified 30 Ekim. Bu nedenle canlı deployment doğrulanmış değil.
- Yerel test ortamı incelemesi `docker ps` çağrısında Docker Desktop Linux Engine pipe bulunmadı (`dockerDesktopLinuxEngine` erişilemiyor). Yerel servis/test çalıştırması bu ortamda şu an mümkün değil; CI main kabul kanıtı var.
- `git diff --check` geçti. Kod veya production değişikliği yapılmadı.
- Sıradaki: Coolify authenticated resource/revision/back-up şartını güvenli yetkili erişimle doğrula; ayrıca Hedef alt maddeleri için ayrı atomik kabul matrisi üretmeye devam et. Docker erişimi gelirse yerel E2E/DB kabulini yeniden değerlendirmek yerine CI kapsamındaki eksik canlı kabullere geç.

## 2026-11-01 — Hedef gereksinim ID envanteri

- `docs/REQUIREMENT-TRACEABILITY.md` üretildi: Hedef v30'da açıkça numaralanmış 94 hüküm, kaynak satır numarası ve türüyle listelendi. Hiçbir gereksinim kanıtsız tamamlanmış işaretlenmedi. Hedef'teki numarasız listeler, UI öğeleri ve §20 adımları bu sayının dışında; ayrıca atomik olarak eklenmeli.
- `PROJEYI-TAMAMLAMA-PROMPTU.md` bu matrisi tamamlama ve her gereksinimi test/CI/canlı kanıtla bağlama adımıyla güncellendi.
- `docs/AUDIT-REALITY.md` 22 ana bölümü ve 8 V1 kabul kapısını indeksliyor. Tam alt madde eşlemesi hâlâ devam ediyor. Genel yüzde üretilmedi.

## 2026-11-01 — gallery live sürümünün commit düzeyinde tespiti

- Önceki “main SHA farklı, gallery live doğrulanmadı” notu ayrıntılı karşılaştırmayla netleştirildi. `product-gallery.js` canlı SHA-256, main'in `4d55270` revision'ındaki file ile byte-byte aynı. Bu revision ana PR #24 gallery mergeridir.
- Canlı original roll (`asset-label-roll-transparent.png`) ve kullanıcıların üç görseli (`asset-label-devices.png`, `asset-label-hardware.png`, `asset-label-desktop.png`) HTTP 200 `image/png` döndürüyor. Canlı JavaScript'te bu üç source, 5 saniyelik `setTimeout`, sola `translateX`, geçiş ve dialog preview mevcut; carousel ve modal kabuğu yayında.
- Fakat canlı sürümde `registerExpandable` bulunmuyor. Git diff `ba5b1ef` bu fonksiyonla her gallery slaytını click/Enter/Space'a bağlamış; mevcut live kaynakla bu davranış eksik olduğu doğrulanıyor. Main CI #41270953843 gallery browser acceptance dahil 4/4 success; rapor `tests/reports/product-gallery-20261101.md` 4 slaytta click/keyboard, Escape/odak dönüşü, 5 saniye kayma ve mobil taşmayı kabul ediyor.
- Bu nedenle yapılacak şey bütün galeriyi yeniden yapmak değil; `ba5b1ef` ve diğer main düzeltmelerini tam main SHA ile Coolify'den deploy etmek, sonra canlı davranışı/hash'i doğrulamaktır. Deployment hâlâ yapılmadı.
- Rapor düzeltmeleri: `docs/AUDIT-REALITY.md` ve `deploy/coolify/README.md`. Önceki hash mismatch artık eski sürümün kimliğini ve eksik tek özelliği gösteriyor.

## 2026-11-01 — canlı galeri davranışlarının net ayrımı

- Canlı carousel kabulinin gerçek sınırı artık belirgin: 4 görsel (roll + 3 kullanıcı PNG'si) ve 5 saniye/sola geçiş/preview dialog script'i canlıda mevcut. Dört image endpoint'i 200 `image/png` döndürüyor.
- Canlı `product-gallery.js` byte SHA, `origin/main~3` / `4d55270` blob SHA ile eşleşiyor. Main commit `ba5b1ef` bu dosyaya `registerExpandable` ekleyerek tüm galeri slaytlarında click/Enter/Space tam ekran, keyboard testleri ve kabul raporunu getirmiştir. Live bu tam ekran event'lerini içermiyor. Önceki “galeri SHA uyuşmadı, dolayısıyla galeri live bilinmiyor” kaydı bu daha iyi kanıtla güncellendi.
- Main CI 41270953843 4/4 success ve rapor `tests/reports/product-gallery-20261101.md` kabul testleri main kodunu doğruluyor; canlı gerçek Chromium testini doğrulamıyor. Browser automation runtime'ında Playwright modülü kullanılamadı; `Computer Use` tool previous browser URL confidence stop nedeniyle bu test yolu kapalı. Coolify page user-action sorusu gönderildi, yanıt bekleniyor.
- Düzeltme raporları: `docs/AUDIT-REALITY.md` / `deploy/coolify/README.md`; ilerleme kaydı güncel. Kod değişmedi.

## 2026-11-01 — requirement inventory count corrected

- The initial traceability parser only counted bold IDs with no label text (94). Hedef also has IDs whose descriptive title is inside the bold span (e.g. `1.1 GERÇEK TEKNOLOJİ`); parser corrected and `docs/REQUIREMENT-TRACEABILITY.md` regenerated with 118 ID clauses. This count covers only explicitly numbered clauses; unnumbered acceptance bullets/UI inventory remain to be atomized.
- `1.1` technology constraint is now mapped `TAMAM`: main web project targets net10.0, EF Core Design/Npgsql EF provider are 10.0.0, Coolify Compose uses PostgreSQL 17, and current main CI is 4/4 success. This is a technology fact only, not a V1 completion signal.
- Current recheck: main/run still `d16f2fb` / run `41270953843 success`; live gallery SHA still exactly matches old pre-fullscreen-fix `4d55270` source and lacks `registerExpandable`.

- CompanyId yorumuna ait güncel kullanıcı kararı izleyiciye işlendi: CompanyId yalnız şirketin tenant verisinde zorunlu; ortak kullanıcı, plan ve platform kayıtları ortak/kök kalır. Bu karar Hedef §6.1'in eski mutlak cümlesinden önceliklidir. `docs/AUDIT-REALITY.md` ve `docs/REQUIREMENT-TRACEABILITY.md` §6.1/§6 satırları bunu not ediyor; kapsamlı entity/endpoints doğrulaması henüz açık.

## 2026-11-01 — P0-A yüzde iddiaları envanteri güncellendi
- Durum: Kapsam sınıflaması tamamlandı; Hedef maddeleriyle kod/test izlenebilirliği açık.
- Değişen dosyalar: `docs/AUDIT-REALITY.md`, `docs/PROGRESS.md`.
- Komut + çıktı: `Select-String -Pattern '\d+(?:[,.]\d+)?\s?%'` ile `PROJECT-STATUS.md`, `PRODUCT-COVERAGE.md`, `ROADMAP.md` tarandı; 13 satır bulundu (11 + 2 + 0). Tarihsel pano ortalamaları, tarihli kabul alt sayaçları, tahminler ve yinelenen notlar ayrı gruplandı. `ROADMAP.md` içindeki tek %100 ifade hedeftir.
- Test: Ürün testi çalıştırılmadı; yalnız doküman taraması ve sınıflaması yapıldı. `git diff --check` ile doküman değişikliği doğrulanacak.
- Kanıt yolu: `docs/AUDIT-REALITY.md` → “Yüzde iddiaları için 1 Kasım kapsam sınıflaması”.
- Sıradaki iş: P0-A için her tarihli kabul iddiasını kaynak/test artifact ve Hedef maddesiyle satır satır eşle; ardından P0-B'de repository/live migration kimliği farkını güvenli salt-okunur kanıtla çöz.

## 2026-11-01 — PR #30 tenant regression closeout

- Commit `25599a1e6d94342ed028353d7f175911cbb7f423` on `codex/p1-tenant-query-filter` is the tested PR head. After the two PostgreSQL regressions were corrected, GitHub Actions run `41277184261` passed 4/4: Linux PostgreSQL tenant acceptance, Linux tenant security acceptance, Windows deployment and backup safety, homepage gallery browser acceptance.
- `dotnet build src/EnvanterTR.Web/EnvanterTR.Web.csproj --no-restore`: 0 warnings, 0 errors. Focused `AppDbContextTenantFilterTests`: 1/1 passed (test project has 4 existing ASP.NET deprecation warnings). `git diff --check` passed before the code commit.
- This evidence closes the isolated PR test run only. PR #30 remains draft/open; no merge or production deployment. Live tenant adversarial test and fresh production backup/rollback proof remain open.
- Further public read-only `GET /api/status` on apex, firma and Farma returned HTTP 200 `ready`, 122 migrations and 0 pending. Historical §4.6.1 migration backlog is not present at the current endpoint; local/live migration identity comparison and deployment SHA remain unverified.

## 2026-11-01 — P1 tenant-filter CI closeout and bypass audit
- PR #30 head `a13fd84c84a85e2589859e9b9aa3133a59779af9` CI run `41278497378` is terminal `completed/success`, 4/4 jobs: Linux PostgreSQL tenant acceptance, Linux tenant security acceptance, Windows deployment/backup safety, and homepage gallery browser acceptance. PR remains open as a draft; this does not equal merge or production release.
- `git diff --check` passed on the PR worktree; worktree is clean.
- Reviewed every production `IgnoreQueryFilters()` call site found by `rg` in `src/EnvanterTR.Web`. Tenant-sensitive call sites inspected use explicit CompanyId/identity predicates; login/session cross-company membership reads are identity constrained. This was a source audit, not a substitute for endpoint attack tests or a compiler/test run.
- Remaining P1 evidence: live cross-tenant ID attacks, MFA/session/CSRF live acceptance, and current production backup/restore rehearsal. P0 external payment remains unavailable because no iyzico merchant account exists; existing user authorization allows the independent P1 security work to continue.
- Next: preserve draft PR until reviewed/merged through the normal flow; continue the highest-risk unproven P1 acceptance and record one specific test result per package. Do not repeat the completed PR CI run.

## 2026-11-01 — P1 background scheduled-report tenant boundary CI result
- PR #30 commit `730defae087cdb2af03bb8a84b3a002f5e07f86c` adds `Background_scheduled_report_data_is_limited_to_the_report_company` for inventory, license, and stock aggregates generated without an HTTP request tenant context.
- Local SecurityAcceptance project build passed with 0 errors and 4 existing ASP.NET deprecation warnings. Local PG execution was unavailable because no test connection is configured and Docker Desktop engine is unavailable.
- Hosted CI run [41280818308](https://github.com/EnvanterTR/EnvanterCodex/actions/runs/41280818308) completed successfully; its Linux PostgreSQL tenant acceptance job passed in 19m41s, alongside Linux tenant security, Windows deployment/backup safety, and homepage gallery acceptance. This result is for PR branch code only.
- PR #30 remains open/draft and no production deploy occurred. The test closes this focused background-report aggregate check; it does not close every P1 tenant endpoint attack or live backup/restore gate.
- Next: continue P1 cross-tenant endpoint attack coverage; keep live restore and production rollout separate until authorized deployment path and current backup evidence are available.

## 2026-11-01 — live readiness and PR #30 current CI recheck (07:05 UTC)
- Active project target verified at `C:\Users\Lenovo\Desktop\EnvanterTR\Hedef.txt` (v30.0, 473 lines, 118 numbered clauses). The similarly named Desktop-level v11 file is outside this project root and is not used as the active target.
- PR #30 remote head is `fdff9175600a37dc474560470caf690f86208a4a`; `git ls-remote` confirms it matches both `origin/codex/p1-tenant-query-filter` and `refs/pull/30/head`. Main is `d16f2fb2a6e4f333b617ea4a9bf1cf673d58946d`. PR remains Draft/open.
- GitHub Actions run [41282512318](https://github.com/EnvanterTR/EnvanterCodex/actions/runs/41282512318), head `fdff917`, is still `in_progress` at 07:05 UTC. Linux tenant security, Windows deployment/backup safety, and homepage gallery jobs are `success`; Linux PostgreSQL tenant acceptance job `124777136823` is still running. No new commit was pushed while this job runs.
- Fresh read-only HTTPS checks: apex, firma, Farma, and Santa `/api/status` each returned HTTP 200 `ready`, 122 migrations and 0 pending. `/api/checkout/status` returned HTTP 200 with `available:false`; paid checkout is not active. These checks do not identify the running release SHA or certify login, tenant attacks, backup/restore, or purchase acceptance.
- Bounded source review examined the three raw SQL lock queries that did not show a CompanyId predicate within the local scan window: shared `SubscriptionCoupons`, guest-checkout `CompanyCheckoutOrders`, and the opaque-ticket `CompanyMfaEnrollmentChallenges` flow. Endpoint-level attacker acceptance remains open.
- Evidence: `tests/reports/live-readiness-and-tenant-sql-audit-20261101.md`.
- Next: poll the same live Actions run; after its terminal result, continue the highest-risk unproven tenant endpoint acceptance without repeating completed tests.

## 2026-11-01 — first-day project-history audit (07:10 UTC)
- Added `docs/PROJECT-HISTORY-AUDIT.md` using the conversation archive's declared period and current `origin/main` commit graph.
- Verified the user's archived initial project discussion is dated 2026-09-25; active C# repository first commit is 2026-10-04 12:13:16 TRT (`38744c6`). The archive ends 2026-10-11 and cannot substantiate later conversations.
- Current `origin/main` has 1,087 commits (1,013 first-parent, 24 merge commits) through `d16f2fb` at 2026-11-01 06:53:49 TRT. Per-day metadata counts are recorded in the report and explicitly are not treated as feature or completion counts.
- This adds an evidence-based timeline, not a full conversation reconstruction. Hedef-to-code/test/live requirement audit remains incomplete.
- Added `docs/FEATURE-MATRIX.md`, atomizing the unnumbered §§7.1–7.7 functional-parity scope into 77 separately traceable acceptances. All begin `DOĞRULANMADI`; current source/test/live mapping is the next task, so the inventory does not imply features are missing or complete.

## 2026-11-01 — PR #30 terminal CI and tenant attack coverage review (07:14 UTC)
- Supersedes the 07:05 “run still in progress” note: GitHub Actions run [41282512318](https://github.com/EnvanterTR/EnvanterCodex/actions/runs/41282512318), PR head `fdff9175600a37dc474560470caf690f86208a4a`, completed `success`, 4/4 jobs (PostgreSQL tenant acceptance, tenant security acceptance, Windows deployment/backup safety, homepage gallery browser acceptance).
- PR #30 remains draft/open. The successful run proves its commit's CI only; it is not merged or deployed evidence.
- Reviewed existing tenant security acceptance coverage before adding tests to avoid repeating work: `TenantSessionAcceptanceTests.cs` already exercises direct cross-tenant status change, trash, restore-from-trash, archive restore, bulk update, and API asset update attempts, with 404 expectations (notably around lines 8980–9050, 9240, 9330–9445, 10790–10810, 16010–16030). Therefore no duplicate endpoint test was added in this pass.
- Live state from the same read-only probe remains: apex/firma/Farma/Santa return 122 migrations and 0 pending; checkout is unavailable. These checks do not prove deployment SHA, interactive customer acceptance, or current backup restore.
- Work performed this pass: updated top-level `PROJEYI-TAMAMLAMA-PROMPTU.md` to explicitly exclude exposed credentials from use and to prevent repeated completed checks. No application code, PR, or production state changed.
- Next: update the source/test/live mapping for the existing attack tests, then select the next uncovered P1 requirement. Keep live backup/restore and deployed SHA as separate open acceptance items.

## 2026-11-01 — P1 requirement evidence mapped to existing tests (07:25 UTC)
- Rechecked GitHub REST: PR #30 head `fdff9175600a37dc474560470caf690f86208a4a`, open/draft, base `d16f2fb`; latest associated run `41282512318` is `completed/success`, all 4 jobs pass. The static Actions HTML view showed stale `in progress`; REST run/job state is the authoritative result.
- Rechecked live `/api/status` on apex, firma, Farma, and Santa: all four returned `ready`. This endpoint does not establish running SHA or customer workflow acceptance. Checkout availability was last directly observed false in the preceding read-only run; no paid flow was changed.
- Updated `docs/FEATURE-MATRIX.md` and `docs/REQUIREMENT-TRACEABILITY.md`: §6.2 and §6.5 now map to the PR's EF query filter, PostgreSQL tenant visibility test, direct-ID negative endpoint tests, the passing PR CI, and explicitly unresolved draft/live/deployment evidence. Updated asset CRUD/bulk update rows so they no longer request duplicate tests or claim CI is still running.
- `docker info` still fails because Docker Desktop's Linux Engine named pipe is absent. Local PostgreSQL E2E is unavailable here; hosted PostgreSQL CI is green.
- No product code or production data changed. Next: continue source-to-test mapping of the remaining §6 tenant cache/queue/files and provisioning requirements, then implement the highest-risk uncovered behavior. Keep live release identity and backup/restore as open P1 gates.
- Additional user-requested homepage recheck: `https://envantertr.com/` and `/product-gallery.js` return HTTP 200; the original roll plus three submitted PNGs under `/assets/images/` each return HTTP 200 `image/png`. Current live JS schedules a 5-second leftward carousel and creates a dialog, but the live HTML's original image has no `data-preview-expand`, and the live JS does not register newly created slides. Thus the click/keyboard full-screen request is not proven working live (the exact current code path leaves those slides unbound). `origin/main` contains `registerExpandable` for both existing and generated slides, covered by passing gallery CI; it has not been deployed. Updated `docs/AUDIT-REALITY.md` with this fresh source/live distinction. Coolify UI is not currently authenticated/available in the enabled browser inventory, so no deploy was attempted; do not claim this request is live-fixed.

## 2026-11-01 — Shared tenant provisioning implementation
- Fresh read-only production check: apex, `firma`, Farma, and Santa `/api/status` all HTTP 200 `ready`, `migrationCount=122`, `pendingMigrationCount=0`; `/api/plans` returns eight Cloud plans; `/api/checkout/status` returns `available:false`. This supersedes older 503/no-plans observations for those endpoints, but does not prove source SHA, login acceptance, migration content parity, or paid checkout. User previously confirmed no iyzico account exists.
- `origin/main` at `d16f2fb2a6e4f333b617ea4a9bf1cf673d58946d` contains 118 EF migration source files while live health reports 122 applied. The live process reports no pending migrations against its own assembly, but repository/live version parity is unresolved; do not run a migration or claim migration equality from counts alone.
- Hedef §6.9 source audit found three company-creation paths: first-owner setup API, platform paid-company API, and checkout callback/webhook. Isolated branch `codex/tenant-provisioning-service` commit `1e2b51d` routes all three through `TenantProvisioningService`; source search finds `Companies.Add` only in that service. It preserves endpoint-owned transactions, payment/idempotency logic, platform audit, first-owner password policy, company MFA policy, and invitation flow.
- Final web build succeeded with 0 warnings/0 errors; `git diff --check` passed. The two targeted paid-platform and checkout acceptance tests were invoked and both stopped in fixture initialization because `ENVANTERTR_SECURITY_TEST_CONNECTION` is absent; no product assertions ran. Report: `tests/reports/tenant-provisioning-20261101.md`. No local database or production state was changed.
- PR state: branch pushed; GitHub REST shows no PR for this branch, so no CI has run for this change. No PR was submitted, merged, or deployed. Next: obtain PR CI and PostgreSQL acceptance evidence. Keep §6.9 `DOĞRULANMADI` until those checks pass.

## 2026-11-01 — P0-A güncel canlı durum ve galeri kabulü
- Durum: P0-A `DOĞRULANMADI` (tarihsel ilerleme yüzdelerinin tam satır-kod-test eşlemesi ve Bölüm 19 yönetici ekranı kabulü henüz tamamlanmadı); kullanıcı görsel isteği için canlı galeri kabulü `TAMAM`.
- Değişen dosyalar: `docs/AUDIT-REALITY.md`, `docs/PROGRESS.md`.
- Canlı release kaydı: Coolify `bjkynxtfewzdddggcqz15tbv` Success/Finished, kaynak SHA `4789b509c0b719c1bf71a0b8032f7224bae9fd43`. Cache-bust adresi `/product-gallery.js?v=cda5cf6`; canlı ana sayfada görsel tıklamasıyla fullscreen dialog ve kapatma düğmesi gözle doğrulandı.
- Read-only production probes (2026-11-01 08:49 UTC): apex/firma/farma/santa `/api/status` HTTP 200 `ready`, 122 migration / 0 pending; `/api/plans` HTTP 200 ve 8 plan; `/api/checkout/status` HTTP 200, `available:false`, `provider:null`.
- Yorum: `%42,06` (45/107) kabul alt-sayacı ve `%70,87` (180/254) kontrol-listesi oranı farklı paydalardır; birleştirilmedi. İyzico hesabı olmadığına dair önceki kullanıcı beyanı nedeniyle gerçek ödeme kapısı açık. Bugün yeni DB yedeği/restore provası yapılmadı.
- Test/kanıt: canlı tarayıcı galeri tıklama ve Kapat gözlemi; salt-okunur HTTPS API çıktıları; Coolify deployment Success. Uygulama kodu bu ilerleme kaydında değiştirilmedi; build veya ürün testi çalıştırılmadı.
- Sıradaki iş: P0-A'da `PROJECT-STATUS.md`, `PRODUCT-COVERAGE.md`, `ROADMAP.md` içindeki güncel ve tarihsel tamamlanma/ilerleme metriklerini gruplayıp her metrik ailesine kod, test ve güncel hüküm bağlamak; sonra P0-B güncel migration kimlik farkı.

## 2026-11-01 09:08 UTC — canlı galeri uçtan uca gözlem
- Durum: **TAMAM (ana sayfa görsel isteği); P0-A genel denetimi açık.**
- Değişen dosyalar: `docs/AUDIT-REALITY.md`, `docs/PROGRESS.md`, `tests/reports/live-gallery-runtime-acceptance-20261101.md`.
- Kanıt: canlı script HTTP 200 ve 5.078 karakter; üretimdeki dosya `cda5cf6` kaynak blob'u ile birebir eşleşiyor; deploy commit `4789b509c0b719c1bf71a0b8032f7224bae9fd43`.
- Canlı tarayıcı: etiket slaydı tıklama → tam ekran dialog açıldı; kaynak görselin 1200×1311 doğal çözünürlüğü korundu; Kapat ile kapandı ve odak geri döndü. 5,4 saniyede otomatik döngü ilerledi; 555 px genişlikte yatay taşma görülmedi.
- Read-only sağlık: apex/firma/farma/santa dört host HTTP 200 `ready`, 122 migration / 0 pending.
- Komut/test: canlı HTML/JS HTTP kontrolleri ve kaynak blob karşılaştırması geçti; public browser live acceptance adımları geçti. Build veya uygulama testi bu dokümantasyon doğrulama paketinde çalıştırılmadı.
- Sıradaki iş: P0-A'daki ilerleme metriği satırlarının tam source/test eşlemesini bitir; ardından P0-B için migration kimliklerini karşılaştır. Canlı yedek/restore ve gerçek ödeme kapıları açık kalır.
