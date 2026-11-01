# EnvanterTR — gerçek durum denetimi

**Kontrol tarihi:** 2026-11-01 (Europe/Istanbul)
**Kapsam:** `../../../Hedef.txt` v30, mevcut `origin/main`, PR/CI ve salt-okunur canlı kontroller.
**Durum:** Başlangıç kesiti; Hedef'in her alt maddesi henüz kod/test/canlı kanıtına bağlanmadı. Bu rapor tamamlanma ilanı değildir.

## Hedef v30 bölüm bazlı denetim indeksi

Bu 22 satır, Hedef'in her bölümü denetimden geçirecek indekstir; satır “TAMAM” demek için değil, ilgili kabulün doğru kanıt alanına yönlenmek içindir. Ürün kapsamlı bölümlerde bölüm düzeyinde kapanış için alt maddeler ve test kapsamı matrise eklenmelidir.

| Bölüm | Gereksinim grubu | Başlangıç kanıtı | Güncel durum |
|---|---|---|---|
| 0 | Çalışma sözleşmesi, kanıt ve dürüstlük | Hedef v30 §§0.1–0.6; yeni tamamlama talimatı | Süreç kuralı mevcut; tüm V1 maddeleri denetlenmedi. |
| 1 | Teknoloji ve oturum hafızası | `git log --reverse` ilk main commit'i `38744c6`; ASP.NET Core main CI | Aktif ürün C#/.NET; ilk tarihçede ayrı PHP/Snipe referansı var. İki repo geçmişi ayrıştırıldı. |
| 2 | Satılabilir V1 ve öncelik | Hedef §2; ödeme status `available:false` | V1 tamamlanmadı; ödeme sağlayıcısı dış bağımlılığı açık. |
| 3 | İlerleme ve kod kalitesi | `PROGRESS.md`, `AUDIT-REALITY.md`, CI | Güncel kayıtlar eklendi; eski yüzdelerin izlenebilir kabul matrisi tamam değil. |
| 4 | Mevcut gerçeklik ve eski oran denetimi | `AUDIT-REALITY.md`; canlı sağlık/plan/ödeme GET; eski snapshot taraması | Başlangıç ve yüzde iddiaları indekslendi; her alt özellik için kod/test/canlı denetimi sürüyor. |
| 5 | Ürün, müşteri, platform ve test hesapları | Tarihli Santa/Farma raporları ve sohbet kayıtları | Hesap etkileşimi kanıtları var; tüm roller ve işlemler canlı müşteri kabulinden geçmedi. |
| 6 | Tenant / CompanyId izolasyonu | Main CI run 41270953843 4/4; PR #30 head `fdff917` run 41282512318 4/4; güncel kullanıcı kararı: CompanyId şirket verisinde zorunlu, ortak kullanıcı/plan/platform kök kayıtlarında ortak | PR #30 tenant query filter ve PostgreSQL/endpoint regression suite'i yeşil, ancak draft/open ve merge edilmedi. Ortak-kök / tenant-verisi entity envanteri, main'e alma, prod rollout ve canlı karşılıklı ID kabulü açık. Hedef §6.1'in “her tabloda” metni güncel kullanıcı kararıyla açıklanmıştır. |
| 7 | Snipe-IT özellik paritesi | `PRODUCT-COVERAGE.md`, tarihli modül UI/PG raporları | Kapsam kataloğu var; bütün Snipe menü/form/işlevlerini kapatan güncel regresyon kabulü yok. |
| 8 | 2026 modern özellikleri | Hedef §8 ve dağınık tarihli raporlar | Özelliklerin tekil kullanıcı kabul matrisi kurulmadı; bölüm açık. |
| 9 | Platform yönetim paneli | Tarihli platform admin PostgreSQL/Chromium raporları; main CI | Alt özellik kanıtı var; tüm kritik yazma/MFA/audit işlemleri canlıda uçtan uca doğrulanmadı. |
| 10 | Müşteri firma portalı | Wildcard live report; apex/firma güncel health | Yönlendirme/sağlık kanıtı var; Santa/Farma tam iş akışı ve erişim sınırı kabulü açık. |
| 11 | Satış ve pazarlama sitesi | Coolify production commit `4789b509c0b719c1bf71a0b8032f7224bae9fd43`; live script, `cda5cf6` source blob ile birebir eşleşiyor; canlı Chromium testi 2026-11-01 | Etiket görsel galerisi ve tam ekran davranışı canlıda doğrulandı. V1 genel kapısı ayrı açık. |
| 12 | Kayıt, onboarding, ödeme, provisioning | `/api/plans` mevcut, checkout disabled | Gerçek satın alma ve otomatik provision tamamlanmadı; iyzico hesabı yok. |
| 13 | UI test matrisi | `ui-inventory.md`/hosted Chromium tarihli raporlar | Geçmiş UI kapsamı geniş; güncel main'de Hedef A–M maddelerinin tam kapsamı kanıtlanmadı. |
| 14 | İstek/kod/UI/regresyon kalite kontrolü | Main run 41270953843: 4/4 success | Bu merge için CI kapısı geçti; tüm ürün modüllerine yönelik QA dörtlemesi henüz kanıtlanmadı. |
| 15 | Sekiz V1 kabul kapısı | Bu rapordaki Hedef §15 tablosu | Sekiz kapının hiçbiri bölüm kapsamıyla tamamen kapanmış kanıta sahip değil; detay satırları tabloda. |
| 16 | Teknik mimari | Main source ve CI; canlı migration status 122/0 | Ana teknoloji/sağlık biliniyor; tam mimari, operasyon ve güvenlik gereksinimleri ayrı kabul edilmeli. |
| 17 | Çalışma yöntemi ve fazlar | Tamamlatma promptu, progress kaydı | Sıra ve devam yöntemi tanımlandı; ürün teslimatı yerine geçmez. |
| 18 | Rapor formatı ve delil | `AUDIT-REALITY.md`, `PROGRESS.md` | Kayıt var; tüm kabul kanıtları henüz tek tabloya bağlanmadı. |
| 19 | Başlangıç/proje kapsamı | Hedef başlangıç komutu; ilk commit geçmişi | Bu tur için audit uygulanıyor; Hedef'in tüm teslimatları henüz sağlanmadı. |
| 20 | Santa/Farma/üçüncü firma canlı kabulü | Tarihli canlı MFA/portal raporları; main tenant CI | Demo3 üretimden provision/silme ve tam Santa/Farma iş akışları kanıtlanmadı. |
| 21 | Sohbetten bağlayıcı ürün kararları | Güncel tamamlama promptu ve önceki karar kayıtları | Fiyat, SaaS/slug, görseller, profil ve Authenticator talepleri izleniyor; her biri canlı kabul edilmedi. |

## İlk günden bugüne doğrulanmış tarihçe

| Dönem | Kanıt | Sonuç |
|---|---|---|
| 2026-10-04 | `git log --reverse origin/main` | Aktif EnvanterTR deposunun ilk commit'i `38744c6 Initial EnvanterTR project`. Ana dal geçmişi envanter uygulaması, web/satış, güvenlik ve modül geliştirmeleriyle devam ediyor. |
| 2026-09 / 2026-10 başlangıcı | Üst dizindeki `snipe-it-8.7.2` arşiv ve repo kayıtları (önceki inceleme notları) | PHP/Laravel tabanlı referans/önceki çalışma ile mevcut C# ürün ağacı aynı uygulama/depo değil. Geçiş kararının tam konuşma anı Git geçmişinden kanıtlanmıyor. |
| 2026-10-20/21 | `tests/reports/web-release-118-20261021.md`, `wildcard-live-acceptance-20261020.md` | 118 migrationlı kontrollü yayın, yedek/restore provası ve wildcard alt alan yönlendirme kabulü tarihli raporlanmış. Bunlar 2026-11-01'deki canlı SHA veya tam ürün kabulü yerine geçmez. |
| 2026-10-21 | `origin/main` commitleri ve PR #24 | Dönen etiket galerisi, portal önizlemesi ve tam ekran akışının ilk birleşimi ana dala alınmış. |
| 2026-11-01 | PR #28/#29 merge kayıtları | Tam ekran güvenlik önizlemesi ve profil fotoğrafı depolama hata ele alma ana dala alınmış. |

## Güncel doğrulanmış kaynak ve canlı durumu

| Alan | Kanıt | Durum |
|---|---|---|
| Ana kaynak | `origin/main` SHA `d16f2fb2a6e4f333b617ea4a9bf1cf673d58946d`; #28 merge `4d8d2bccc89cbf0ba4a09911b860c050cf078282`; #29 merge SHA ile aynı | PR'lar main'de. |
| CI | GitHub Actions run [41270953843](https://github.com/EnvanterTR/EnvanterCodex/actions/runs/41270953843), head `d16f2fb2a6e4f333b617ea4a9bf1cf673d58946d` | 4/4 iş terminal `success`: Linux tenant security, homepage gallery browser, Linux PostgreSQL tenant acceptance ve Windows deployment/backup safety. |
| Canlı sağlık | `GET https://envantertr.com/api/status`, `GET https://firma.envantertr.com/api/status` | İki uç da HTTP 200 `ready`, 122 migration / 0 bekleyen. Bu, `main` SHA'sının yayında olduğunu kanıtlamaz. |
| Canlı ödeme | `GET https://envantertr.com/api/checkout/status` | HTTP 200, `available:false`; kullanıcı iyzico hesabı olmadığını bildirdi. Gerçek ödeme kapısı açık. |
| Canlı paketler | `GET https://envantertr.com/api/plans` | HTTP 200; yıllık Cloud paketleri listeleniyor. Katalog görünmesi ödeme tahsilatı/provisioning kabulü değildir. |
| Ana sayfa görselleri | 2026-11-01 canlı HTML/JS GET; üretimde `product-gallery.js?v=cda5cf6`; canlı browser DOM, tıklama, dialog ve kapanış; deployed branch `codex/home-gallery-live-20261101` commit `4789b509c0b719c1bf71a0b8032f7224bae9fd43`, kaynak kod commit `cda5cf6f89a241bebb56fb8fc8392c350b8ace39` | HTML ve JS HTTP 200. Canlı JS (5.078 karakter) kaynak commit blob'u ile birebir eşleşiyor ve `registerExpandable` içeriyor. Tarayıcıda dönen galeri slaydı tıklanınca native dialog açıldı; kaynak görsel/1.200×1.311 doğal çözünürlük korundu; “Kapat” ile kapandı ve odak açıcı görsele döndü. 5,4 saniyede slayt değişimi gözlendi; 555 px viewportta yatay taşma yok. Önceki bu tablodaki “tam ekran bağlı değil / yayın doğrulanmadı” cümlesi bu yeni kanıtla geçersizdir; tarihsel gözlem olarak kalır. |
| Çalışma ağacı | `git status --short --branch` ve `git worktree list` | Ana repo checkout'u `feat/platform-checkout-readiness-20261020` dalında, `36c5937`; 219 mevcut değişiklik/dosya kaydı. Bunlara reset/stash/toplu temizlik uygulanmamalı. `origin/main` farklı commit. |

## Hedef v30 kritik kapıları

| Hedef maddesi | Mevcut kanıt | Gerçek durum |
|---|---|---|
| Üretim migration farkı (4.6.1, 15.10) | Canlı health 122/0; `origin/main` kaynak ağacında 118 migration dosyası | Canlı süreç kendi sürümüne göre pending bildirmiyor; source/live migration sayısı ve SHA farklı görünüyor. Tam migration listesi/SHA/SQL karşılaştırması ve güncel yedek+restore provası açık. |
| Firma portalı 503 (4.6.2) | Canlı `firma` health HTTP 200, `ready` | Eski 503 bulgusu bu uç için artık güncel değil. Alt alan login, tenant kimliği ve veri izolasyon kabulü ayrı açık. |
| Plan/ödeme/checkout (4.6.3, 12, 17.3 P0-D) | 8 Cloud planı ve checkout `available:false`; kullanıcı iyzico hesabı olmadığını söyledi | Paket kataloğu var; tahsilat, sağlayıcı webhook'u ve otomatik satın alma/provisioning tamamlanmış değil. Sağlayıcı hesabı dış bağımlılık. |
| SMTP ve gerçek müşteri kabulü (4.6.4–4.6.5, 20) | Google Admin mail log ekranı geçmişte bir iletinin TLS ile teslimini gösterdi; sohbet kaydında davet token'ında `Invalid token` sorunu bildirildi | Tek e-posta teslimi tüm parola sıfırlama/davet akışlarının veya Santa/Farma uçtan uca kabulünün geçtiğini kanıtlamaz. Taze test/rapor gerekli. |
| Güvenli yedek/geri dönüş ve canlı deploy (4.6.6–4.6.7, 15.10) | Tarihli 2026-10-21 release/restore raporları; canlı bundle SHA main ile farklı | Geçmiş prova kanıtı var, fakat güncel kaynak deploy'u ve bugünkü taze yedek/geri dönüş kapısı doğrulanmadı. |
| Santa, Farma, üçüncü tenant (20.2–20.10) | Önceki canlı giriş/tenant yönlendirme gözlemleri ve kullanıcı hesap etkileşimleri var | Her iki firmanın 10 adımı, karşılıklı ID saldırı testi, platform MFA yazma sınırı ve Demo3 sıfırdan otomatik provision/silme testi bu oturumda kanıtlanmadı. Kapı açık. |
| Görsel galerisi ve tam ekran (kullanıcı isteği, 11. bölüm) | Canlı HTML/JS exact source match; 2026-11-01 Chromium click→dialog→close/focus, 5,4 sn slide movement | **TAMAM (bu görsel isteği):** canlı ana sayfa galerisi otomatik dönüyor; tıklama tam ekranı açıyor. Genel V1 kabul kapısı açık. |
| Profil fotoğrafı depolama (PR #29) | Main CI run `41270953843` 4/4 `success`, Linux PostgreSQL tenant acceptance dahil | Birleşik CI kabul kanıtı var; canlıya deploy ve gerçek hesap kabulü yok. |

## 2026-11-01 — canlı readiness yeniden kontrolü ve tek provisioning akışı

- Read-only GET sonuçları: apex, `firma`, Farma ve Santa `/api/status` HTTP 200 `ready`, her biri `migrationCount=122`, `pendingMigrationCount=0`. `GET /api/plans` HTTP 200 ile sekiz Cloud planı döndürdü; `GET /api/checkout/status` `available:false`, `provider:null`. Bu sonuçlar eski 503/boş plan bulgusunu bu endpoint'ler için geçersiz kılar; canlı SHA, migration SQL içeriği, kullanıcı girişi ve gerçek ödeme kabulü kanıtı değildir.
- `origin/main` SHA `d16f2fb2a6e4f333b617ea4a9bf1cf673d58946d` migration source count: 118; all live status endpoints report 122. The earlier “22 pending” symptom is absent, but the four-migration count divergence means live/source alignment remains unresolved.
- Hedef §6.9 için kaynak taraması (`rg 'Companies\.Add|new Company \{' src/EnvanterTR.Web/Data`) başlangıçta ilk-owner setup, platform şirket API'si ve checkout callback/webhook yollarını gösterdi. Branch `codex/tenant-provisioning-service`, head `1e2b51d` hepsini `TenantProvisioningService` çağrılarına yönlendiriyor; `Companies.Add` yalnız servis içinde kaldı.
- `dotnet build src/EnvanterTR.Web/EnvanterTR.Web.csproj --no-restore`: 0 uyarı, 0 hata; `git diff --check` geçti. İlgili mevcut platform/checkout PostgreSQL kabul testleri çağrıldı, ancak disposable `ENVANTERTR_SECURITY_TEST_CONNECTION` eksik olduğundan fixture aşamasında durdu; ürün assertion çalışmadı. Ayrıntı `tests/reports/tenant-provisioning-20261101.md`. Bu branch için PR açılmadı, dolayısıyla CI çalışmadı; merge veya canlı dağıtım yapılmadı. §6.9 `DOĞRULANMADI` kalır.
- Ödeme hesabı kullanıcının önceki beyanına göre mevcut değil. Bu bağımlılık gerçek tahsilat kabulini engelliyor; şirket açma altyapısı, testler ve diğer bağımsız kapsam işleri sürüyor.

## Hedef §15 — sekiz V1 kabul kapısı

| Kapı | Kapsam | Şu anki kanıt ve durum |
|---|---|---|
| 1 | Giriş, rol, parola kurtarma | Giriş ve MFA özellikleri kaynakta/tarihli CI'da var; canlı Santa/Farma gerçek oturum + parola kurtarma kabul kanıtı eksik. **Açık.** |
| 2 | Tenant izolasyonu | Main CI Linux tenant security geçti; PostgreSQL acceptance aynı run'da hâlâ çalışıyor; canlı karşılıklı ID saldırı kabulü yok. **Açık.** |
| 3 | Envanter ve ilişkiler | Tarihli izole PostgreSQL/Chromium raporları çok sayıda modülü kapsıyor; Hedef'in tüm varlık, zimmet, satın alma, lisans ve stok ilişkileri tek güncel kabul paketinde yeniden bağlanmadı. **Açık.** |
| 4 | Arayüz matrisi | Eski 52 sidebar hedefi ve modül bazlı tarayıcı raporları mevcut; current main için Hedef §13'teki A–M tam matrisi ve %95 eşiğinin güncel sonucu yok. **Açık.** |
| 5 | Demo ve kayıt | Canlı plan listesi mevcut ama gerçek checkout kapalı; Demo3 sıfırdan satın alma/provision kabul kanıtı yok. **Açık.** |
| 6 | Platform yönetimi | Kaynak, platform admin CI ve tarihli test kanıtları mevcut; canlıdaki tüm kritik işlemler/MFA ve audit sınırı güncel uçtan uca kabul edilmedi. **Açık.** |
| 7 | Yedekleme ve kurtarma | 2026-10-21 tarihli canlı DB/medya/anahtar yedek ve izole restore raporu mevcut; bugün geçerli taze yedek/rollback paketi ve güncel main deploy provası doğrulanmadı. **Açık.** |
| 8 | Canlı Cloud SaaS | Apex/firma health hazır, ancak checkout devre dışı, bundle SHA main'den farklı ve gerçek müşteri/Demo3 kabulü eksik. **Açık.** |

## Eski ilerleme yüzdelerinin denetimi

Üç durum belgesinde `%` kullanımlarını taradım. Bulunan satırlar aşağıda tekilleştirilmiştir; `ROADMAP.md`'de yüzde iddiası yok. Her sayı bir tarihli rapor görünümüne ilişkindir. Satırdaki test ve payda o tarihte ilgili ilerleme raporunu doğrular; bugünkü Hedef kapsamının tamamlandığını kanıtlamaz. Bunları güncel oran olarak sunmak doğru değildir.

| Kaynak/satır | İddia edilen oran ve payda | Satırın kendi kanıtı | Bugünkü hüküm |
|---|---|---|---|
| `docs/PROJECT-STATUS.md:63` | 67,06%; 88 toplam / 81 kapsam / 17 tamam / 8 açık | Local API, PostgreSQL ve Chromium tarihli testleri; 16 Ekim 2026 | Tarihsel snapshot; üretim ve Hedef §15 kapılarını göstermez. |
| `docs/PROJECT-STATUS.md:64`, `docs/PRODUCT-COVERAGE.md:24` | 66,65%; 87 / 80 / 16 / 12 | 16 Ekim tarihli rapor aritmetiği ve o günkü local testleri | Tek tarihli aynı snapshot'ın iki belgede tekrarı; güncel oran değildir. |
| `docs/PROJECT-STATUS.md:572–573` | 65,35% / 65%; 84–87 satır, 13 tamam | Progress filtresi için hosted Chromium kabulü; 15 Ekim 2026 | Ekran filtresi testi, tüm modüllerin kabulü değildir. |
| `docs/PROJECT-STATUS.md:586`; `docs/PRODUCT-COVERAGE.md:673` | 65,79%→66,23%; 85→86, sonra 78 kapsam içi / 14 tamam | İlgili varlık modeli için PG 1/1, Chromium, source verifier; 16 Ekim 2026 | O modülün tarihli kabulidir; genel V1 tamamlanması değildir. |
| `docs/PROJECT-STATUS.md:593` | 65%; local ortam satırı %60 bekliyor | Rapor JavaScript/Chromium kontrolü; 16 Ekim 2026 | Rapor gösteriminin testidir, ürünün tamamlanma kanıtı değildir. |
| `docs/PROJECT-STATUS.md:608` | 66,65%; 87 / 80 / 16 / 12 | Tarihli rapor aritmetiği ve rapor Chromium kontrolü; 16 Ekim 2026 | Tekrarlı snapshot; güncel durum değildir. |
| `docs/PROJECT-STATUS.md:702` | Tahmin 74,10%; ayrıca 39/103 = 37,86% kanıtlı | Yerel rapor satırı/Playwright; tarihli snapshot | Tahmin güvenilir tamamlanma oranı değildir; kanıtlı payda da farklıdır. |
| `docs/PROJECT-STATUS.md:708` | Tahmin 74,35%; ayrıca 40/104 = 38,46% kanıtlı | Playwright rapor kontrolü; tarihli snapshot | Farklı payda ve tahmin yöntemi; güncel oran değildir. |
| `docs/PROJECT-STATUS.md:721` | Tahmin 74,83%; ayrıca 42/106 = 39,62% kanıtlı | Playwright rapor kontrolü; tarihli snapshot | Farklı payda; güncel oran değildir. |
| `docs/PROJECT-STATUS.md:727` | Tahmin 75,07%; ayrıca 43/107 = 40,19% kanıtlı | Tam hosted Chromium, 80/80 source verifier; tarihli snapshot | Source/UI regression yeşil olması tüm Hedef maddelerini kapatmaz. |

Satır numaraları kaynak değişince kayabilir; her yeni denetimde `rg` ile tekrar bulunmalıdır. Hedef v30 §4.1–4.2'nin esas istediği kod dosyası + test + gerçek durum eşlemesi, aşağıdaki kabul kapılarıyla sürüyor; geçmişteki tahmini skorları bugüne kopyalamayacağım. Güncel Hedef kabul matrisi kurulmadan yeni genel yüzde hesaplanmayacak.

## Açık denetim işi

Hedef'in 54 bölüm/alt başlık kapsamı atomik kabul maddelerine dönüştürülmeli ve mevcut test raporları, main CI, ilgili kaynak dosyaları ve canlı kabul ile eşleştirilmelidir. Bu başlangıç raporu yalnız güncel dağıtım/ana satış kapılarını kanıtlar; tam Hedef matrisi değildir. Bir sonraki teknik adım: run `41270953843` terminal sonucunu al, Coolify release SHA ile canlı bundle farkını açıklığa kavuştur, sonra hedefin kalan P0/P1 gereksinimlerini tek kabul tablosuna bağla.

## 2026-11-01 — canlı galeri yayını ve güncel P0 durumu

Bu ek, yukarıdaki 23 Ekim tarihli sayısal/altyapı anlık görüntüsünü güncel durum gibi göstermemek için eklendi. Önceki kayıt tarihsel kalır; bu canlı kontrol yeni kaynak/payda hesabı veya V1 kabulü değildir.

| Gereksinim | Kaynak/test/kanıt | Güncel sonuç |
|---|---|---|
| Ana sayfada müşteri görsellerinin otomatik kayması ve tıklanınca tam ekran açılması | Üretim commit'i `4789b509c0b719c1bf71a0b8032f7224bae9fd43`; `src/EnvanterTR.PublicSite/product-gallery.js` ve `src/EnvanterTR.PublicSite/index.html` (`product-gallery.js?v=cda5cf6`); canlı tarayıcıda galeri görseline tıklama ve “Kapat” kontrolü; Coolify deployment `bjkynxtfewzdddggcqz15tbv` Success/Finished | **TAMAM (canlı görsel isteği):** ana sayfadaki galeri açıldı, carousel ve tam ekran penceresi gözle görüldü. Bu, V1 genel kabulü değildir. |
| Firma/tenant host sağlık durumu | Salt-okunur `GET /api/status`, 2026-11-01 08:49 UTC; apex, `firma`, `farma`, `santa` | Dört host HTTP 200, `ready`, 122 migration, 0 bekleyen. Bu, migration kimliklerinin repository ile eşitliğini veya tam müşteri akışını tek başına kanıtlamaz. |
| Canlı plan kataloğu | Salt-okunur `GET https://envantertr.com/api/plans`, aynı zaman | HTTP 200, 8 plan; yıllık Cloud fiyatları API yanıtında mevcut. |
| Gerçek ödeme ve satın alma | Salt-okunur `GET /api/checkout/status`; kullanıcı önceki açık beyanında iyzico işyeri hesabı bulunmadığını söyledi | HTTP 200, `available:false`, `provider:null`. **ENGELLENDİ:** onaylı merchant hesabı/anahtarları ve gerçek ödeme kabulü yok; satın alma tamamlanmış sayılamaz. |
| SMTP ve müşteri kabulü | Tarihli `tests/reports/google-relay-production-acceptance-20261025.md`; Bölüm 20 kabul kayıtları | Google relay için 1 ileti teslim kanıtı var. Bu, Bölüm 20'nin tüm Santa/Farma/Demo3 akışlarını tamamlamaz; gerçek tam kabul açık. |
| Veritabanı yedeği ve geri dönüş | Önceki yedek/restore raporları 20–21 Ekim tarihli; bu denetimde yeni yedek alınmadı veya restore edilmedi | **DOĞRULANMADI (bugün):** tarihli eski prova güncel yayın kapısının taze yedeği sayılmaz. |

### Yüzde denetimi — tam tarama ve madde aileleri

İlk kısa tarama sayıdan önce yazılmış `%39,7` gibi biçimleri kaçırmıştı; önceki “13 satır” sayısı geçersizdir. 2026-11-01'de iki biçimi de arayan tam dosya taraması (`(?:\d+(?:[,.]\d+)?\s?%|%\s?\d+(?:[,.]\d+)?)`) `PROJECT-STATUS.md` içinde 144, `PRODUCT-COVERAGE.md` içinde 21, `ROADMAP.md` içinde 1 olmak üzere **166 yüzde içeren satır** buldu. Tekrarlanan aynı sayacın/kanıt notunun her kaynak konumu aşağıdaki ailelere dahil edildi; “%” işaretini içeren `%PDF-` dosya imzası sayı olmadığı için oran sayılmadı.

| Madde | İddia | Kodda karşılığı (dosya:satır) | Test | Gerçek durum |
|---|---|---|---|---|
| M1 — Hedef §4.1; `PROJECT-STATUS.md:271–273, 941–947` | “%66 tamamlandı” savı; karşılaştırmalardaki %40/%39,7 işlev tahmini ve %30±5 teslim edilebilir ürün tahmini; hesabın elle seçtiği %70/%15/%15 ağırlıkları ve %25 operasyon varsayımı | **Yüzde hesaplayıcı yok.** 271–273 satırlarındaki puanlar elle seçilmiş ağırlıklara dayanıyor; 941–947 checklist sayımı ürün kabul algoritması değil. | %66'yı üreten veya 118 Hedef maddesini ortak paydada sayan güncel test bulunmadı. | **DOĞRULANMADI:** %66 kanıtlanmadı; farklı tahminler birbirini doğrulamıyor. |
| M2 — Tamamlanma hedefi; `PROJECT-STATUS.md:8,11,935`, `ROADMAP.md:5` | Web V1 için %100 hedefi | Hedefi ölçen ürün kodu yok. | Hedef yüzdesi testi yok; özellik testleri ayrı tutuluyor. | **HEDEF:** ölçüm veya bitmiş olma iddiası değil. |
| M3 — Tarihli pano ve alt metrikler; `PROJECT-STATUS.md:59–64, 572–728`, tekrarlar `PRODUCT-COVERAGE.md:23–24, 673` | 23,81; 33,33; 34,02; 36,63; 37,25; 56; 59; 60; 65; 65,35; 65,79; 66; 66,23; 66,65; 67; 67,06; 67,46; 68; 68,24; 68,98; 69; 69,33; 71,31; 71,62; 72; 72,21; 72,49; 72,78; 73,58; 73,84; 74; 74,10; 74,35; 74,59; 74,83; 75,07 ve tekil %100 | Bu ortalama/alt puanları Hedef kapsamından hesaplayan kanonik kaynak bulunmadı; değerler tarihli durum metinlerinde elle raporlanmış. | `tests/reports/ui-test-results-2026-10-19.md` ve `tests/reports/d19-company-unit-types-2026-10-18.md` pano görünümü/filtrelerini kontrol ediyor; hesaplama paydasını ya da ürünün tamamlanmasını doğrulamıyor. | **TARİHSEL / SINIRLI:** Pano görüntüleme kanıtı var; puan hesabı ve V1 geneline uygulanması doğrulanmadı. |
| M4 — Birikimli kabul alt sayaçları; `PROJECT-STATUS.md:22–41, 727–928, 1012`, tekrarlar `PRODUCT-COVERAGE.md:6, 704–814` | 39/103=%37,86; 40/104=%38,46; 41/105=%39,05; 42/106=%39,62; 43/107=%40,19; 44/107=%41,12; 45/107=%42,06 | Sayacı üreten kanonik kod yok; tekil özellik kaynakları ilgili tarihli günlük satırlarında anılıyor. | Günlük notlarında dar Chromium/PostgreSQL testleri var; paydadaki tüm maddeleri aynı Hedef kapsamıyla doğrulayan tek test paketi yok. | **TARİHSEL ALT KABUL:** O gün belirtilen senaryolara dair kanıttır; güncel güvenlik/canlı durumu veya V1 tamamlanma oranı değildir. |
| M5 — Checklist ve teslim süresi tahmini; `PROJECT-STATUS.md:941–947` | 180/279=%64,52; P.7 çıkarılınca 180/254=%70,87; 204/254=%80 eşiği; 10–15 / 20–30 iş günü tahmini | Bu sayıları oluşturan güncel kaynak dosyası bulunmadı; pay/payda manuel checklist sayımı. | Checklist satır sayımı ürün akışlarının uçtan uca çalışmasını doğrulamaz; süre tahminini kanıtlayan test yok. | **CHECKLIST / PLAN:** Güncel ürün oranı veya süre garantisi değil. |
| M6 — Finans örneği; `PRODUCT-COVERAGE.md:10` | Satın alma siparişinde %20 KDV | `src/EnvanterTR.Web/Data/AssetPurchaseOrderEndpoints.cs:61,431` | `tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs:16648–16753`, `Asset_purchase_orders_are_tenant_scoped_export_formula_safe_and_receive_assets_atomically`; Chromium notu `PRODUCT-COVERAGE.md:10`. | **İLERLEME YÜZDESİ DEĞİL:** Finans test girdisi; sipariş tutarı hesabı doğrulanmış. |
### Değer-kaynak indeksi

54 normalize edilmiş sayısal değer üç kaynak belgedeki yüzde yazımlarının tamamını satır konumlarına bağlar. `%x` ve `x%` biçimleri aynı değerde birleştirildi. `M1`–`M6`, yukarıdaki beş sütunlu denetim tablosunun id'leridir; her değerin kod/test hükmü ilgili aile satırında yer alır.

| Normalize değer (%) | Kaynak satırlar | Denetim satırı |
|---|---|---|| 100 (hedef) | `PROJECT-STATUS.md:8,11,935`; `ROADMAP.md:5` | M2 |
| 100 (tarihli tekil pano sonucu) | `PROJECT-STATUS.md:63,593`; `PRODUCT-COVERAGE.md:23` | M3 |
| 15 | `PROJECT-STATUS.md:273` | M1 |
| 20 | `PRODUCT-COVERAGE.md:10` | M6 |
| 23,81 | `PROJECT-STATUS.md:624` | M3 |
| 25 | `PROJECT-STATUS.md:273` | M1 |
| 30 | `PROJECT-STATUS.md:273` | M1 |
| 33,33 | `PROJECT-STATUS.md:662` | M3 |
| 34,02 | `PROJECT-STATUS.md:667, 680` | M3 |
| 36,63 | `PROJECT-STATUS.md:695` | M3 |
| 37,25 | `PROJECT-STATUS.md:695` | M3 |
| 37,86 | `PROJECT-STATUS.md:702` | M4 |
| 38,46 | `PROJECT-STATUS.md:708, 714` | M4 |
| 39,05 | `PROJECT-STATUS.md:714, 721` | M4 |
| 39,62 | `PROJECT-STATUS.md:721, 728` | M4 |
| 39,7 | `PROJECT-STATUS.md:271, 273` | M1 |
| 40 | `PROJECT-STATUS.md:271` | M1 |
| 40,19 | `PROJECT-STATUS.md:727–728, 747–770, 772–792`; `PRODUCT-COVERAGE.md:6, 704, 708` | M4 |
| 41,12 | `PROJECT-STATUS.md:25–41, 805–810, 812, 816, 818, 820, 822, 824, 827, 831–833`; `PRODUCT-COVERAGE.md:721, 763, 767, 771, 775, 779, 783, 787, 791, 795, 803` | M4 |
| 42,06 | `PROJECT-STATUS.md:22–24, 834–844, 848, 852, 854, 857, 861, 865, 877, 887, 892, 897, 902, 907, 913, 917, 1012`; `PRODUCT-COVERAGE.md:806, 810, 814` | M4 |
| 56 | `PROJECT-STATUS.md:620, 638, 702` | M3 |
| 59 | `PROJECT-STATUS.md:572, 620, 702` | M3 |
| 60 | `PROJECT-STATUS.md:593` | M3 |
| 64,52 | `PROJECT-STATUS.md:941, 946` | M5 |
| 65 | `PROJECT-STATUS.md:572–573, 593` | M3 |
| 65,35 | `PROJECT-STATUS.md:572` | M3 |
| 65,79 | `PROJECT-STATUS.md:586`; `PRODUCT-COVERAGE.md:673` | M3 |
| 66 | `PROJECT-STATUS.md:586` | M3 |
| 66,23 | `PROJECT-STATUS.md:608`; `PRODUCT-COVERAGE.md:673` | M3 |
| 66,65 | `PROJECT-STATUS.md:63–64, 608`; `PRODUCT-COVERAGE.md:24` | M3 |
| 67 | `PROJECT-STATUS.md:64, 608, 620` | M3 |
| 67,06 | `PROJECT-STATUS.md:63`; `PRODUCT-COVERAGE.md:23` | M3 |
| 67,46 | `PROJECT-STATUS.md:59–62, 612, 618` | M3 |
| 68 | `PROJECT-STATUS.md:620` | M3 |
| 68,24 | `PROJECT-STATUS.md:618, 620` | M3 |
| 68,98 | `PROJECT-STATUS.md:629` | M3 |
| 69 | `PROJECT-STATUS.md:629` | M3 |
| 69,33 | `PROJECT-STATUS.md:629` | M3 |
| 70 | `PROJECT-STATUS.md:273` | M1 |
| 70,87 | `PROJECT-STATUS.md:946` | M5 |
| 71,31 | `PROJECT-STATUS.md:644` | M3 |
| 71,62 | `PROJECT-STATUS.md:650` | M3 |
| 72 | `PROJECT-STATUS.md:662, 667, 680` | M3 |
| 72,21 | `PROJECT-STATUS.md:662` | M3 |
| 72,49 | `PROJECT-STATUS.md:667, 680` | M3 |
| 72,78 | `PROJECT-STATUS.md:680` | M3 |
| 73,58 | `PROJECT-STATUS.md:695` | M3 |
| 73,84 | `PROJECT-STATUS.md:695` | M3 |
| 74 | `PROJECT-STATUS.md:638, 695, 702` | M3 |
| 74,10 | `PROJECT-STATUS.md:702` | M3 |
| 74,35 | `PROJECT-STATUS.md:708, 714` | M3 |
| 74,59 | `PROJECT-STATUS.md:714, 721` | M3 |
| 74,83 | `PROJECT-STATUS.md:721, 728` | M3 |
| 75,07 | `PROJECT-STATUS.md:727–728` | M3 |
| 80 | `PROJECT-STATUS.md:947` | M5 |

Tarama çıktısı bu 166 satırı ve dosya dağılımını sayısal olarak doğrular; yukarıdaki aralıklar aynı tarihli kayıt ailelerinin tam kaynak konumlarıdır. P0-A tablosu, tarihsel oranların kapsamını sınıflandırır; her tarihli kabul kaydının gerçek kaynak/test dosyası ve Hedef atomik maddesiyle satır-bazlı eşlemesi henüz bitmedi. Süper-admin giriş ekranı bu oturumda açık değil; açığa çıkmış parola kullanılmayacak. Bu nedenle P0-A **KISMEN TAMAM**, Hedef §4.2 izlenebilirlik ve §473 başlangıç kanıtı **DOĞRULANMADI**; yeni özellik veya P0-B'ye geçilmeyecek.

## 2026-11-01 continuation — PR #30 tenant security and live readiness

- Branch `codex/p1-tenant-query-filter`, PR #30 head `25599a1e6d94342ed028353d7f175911cbb7f423`. GitHub Actions run [41277184261](https://github.com/EnvanterTR/EnvanterCodex/actions/runs/41277184261) finished `success` for all 4 jobs: Linux PostgreSQL tenant acceptance, Linux tenant security acceptance, Windows deployment/backup safety, and homepage gallery browser acceptance. This applies to the PR head, not current production `main`.
- The PostgreSQL suite initially caught two regressions in company-choice/session membership visibility and cross-company MFA policy enforcement. Login/session lookups were corrected to bypass the global tenant filter only where user identity/active membership explicitly constrain the result. The full PG acceptance suite then passed on PR head. Live two-company cross-ID authorization remains untested.
- Public read-only probes at this timestamp: `/health` returned HTTP 200 `Healthy` on `firma.envantertr.com`, `farma.envantertr.com`, and `santa.envantertr.com`. `/api/plans` returned eight Cloud annual plans (2,400; 4,200; 6,000; 9,000; 12,000; 14,400; 24,000; 36,000 TRY; monthly price 0). `/api/checkout/status` returned `available:false`, `provider:null`. User has stated no iyzico merchant account exists; paid checkout and real purchase acceptance remain open.
- No production write/deploy occurred. These probes do not prove current migration parity, user login/role workflows, e-mail delivery, live backup/restore, customer acceptance, or main deployment.
- Current progress note: PR #30 CI proves the code change is regression-clean in the isolated test suite; V1 remains incomplete and this PR remains draft.
- Additional GET recheck of `https://envantertr.com/api/status`, `https://farma.envantertr.com/api/status`, and `https://firma.envantertr.com/api/status`: all returned HTTP 200 `ready`, `migrationCount=122`, `pendingMigrationCount=0`. This resolves Hedef §4.6.1's historical “22 pending” observation for the current live endpoints at this timestamp; it does not establish the local-vs-live migration-content diff or certify the latest PR was deployed.

## 2026-11-01 continuation — tenant query-filter PR terminal result

GitHub API recheck of Actions run [41278497378](https://github.com/EnvanterTR/EnvanterCodex/actions/runs/41278497378) reports `completed/success` at head `a13fd84c84a85e2589859e9b9aa3133a59779af9`, all four jobs successful (Linux PostgreSQL tenant acceptance, Linux tenant security acceptance, Windows deployment/backup safety, homepage gallery browser acceptance). PR [#30](https://github.com/EnvanterTR/EnvanterCodex/pull/30) is still open/draft, base `main`, mergeable. No merge, production write, or deployment was performed.

Read-only source audit enumerated production `IgnoreQueryFilters()` call sites in the PR tree and inspected them for explicit tenant/identity predicates. Inspected tenant-sensitive usages included company ID in asset locks, exports, archived-asset lookup, asset reservations, inventory requests, purchase returns and asset-event joins; account/session exceptions constrain by authenticated user and active membership. This is code review evidence only; it does not close live cross-tenant endpoint adversarial acceptance or P1 backup/restore.

## 2026-11-01 — background report tenant test result

PR #30 commit `730defae087cdb2af03bb8a84b3a002f5e07f86c` adds a PostgreSQL-backed acceptance test for tenant scoping of scheduled inventory, license and stock aggregates in a background context without a request tenant. Hosted run [41280818308](https://github.com/EnvanterTR/EnvanterCodex/actions/runs/41280818308) completed successfully; PostgreSQL tenant acceptance passed in 19m41s and the other three CI jobs also passed. The local test project compiled successfully (0 errors; four existing ASP.NET deprecation warnings); local database execution was unavailable.

PR #30 remains a draft on a branch, not main or production. This test is narrow evidence for scheduled report aggregate isolation; it does not establish full live cross-tenant attack acceptance, live backup/restore, or release deployment.

## 2026-11-01 09:08 UTC — canlı görsel galerisi kabulinin yenilenmesi

Önceki satırların “yayın henüz doğrulanmadı” ve “tam ekran bağlı değil” sonuçları bu tarihli canlı ölçümle güncellenmiştir. Bu düzeltme yalnızca ana sayfa galeri gereksinimine ilişkindir.

- Üretim ana sayfası `/` HTTP 200 ve `/product-gallery.js?v=cda5cf6` referansını veriyor. Bu JavaScript HTTP 200, 5.078 karakter. Sunulan script metni `git show cda5cf6f89a241bebb56fb8fc8392c350b8ace39:src/EnvanterTR.PublicSite/product-gallery.js` çıktısıyla tam eşleşti (`Exact text match=True`). Deploy commit `4789b509c0b719c1bf71a0b8032f7224bae9fd43`, kaynak düzeltme commit'i `cda5cf6f89a241bebb56fb8fc8392c350b8ace39`.
- Canlı tarayıcı 555×1052 viewportta dönen etiket görsellerini ve kontrolleri yükledi. Galerideki aktif görsele tıklama native `product-preview-dialog` penceresini açtı. Rulo dosyasının doğal ölçüsü 1200×1311; dialog aynı `/assets/images/asset-label-roll-transparent.png` kaynağını kullandı. Kapatma ile dialog kapandı, odak açıcı görsele döndü.
- Canlı otomatik döngü ölçümünde 5,4 saniyede slayt indeksi son slayttan başa geçti; geçişin ardından 555 px viewport için `scrollWidth <= innerWidth` idi. Görsel sıkıştırma veya dönüştürme yapılmadı.
- Aynı salt-okunur kontrolde apex, `firma`, `farma`, `santa` `/api/status`: HTTP 200 `ready`, 122 migration, 0 bekleyen.
- Bu, kullanıcı galeri isteğini canlıda doğrular; tüm SaaS V1, ödeme, migration içerik paritesi, yeni DB yedeği/restore veya Santa/Farma/Demo3 kabulini kapatmaz. P0-A yüzde kaydı ve P0-B migration kimlik karşılaştırması sürüyor.
