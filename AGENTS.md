# ENVANTERTR — HEDEF 4 (BİRLEŞİK ANA HEDEF) — v4.0

Bu belge Hedef 1, 2 (eski "ULTRA HEDEF PROMPT v11.0") ve Hedef 3 (v2.1) belgelerinin YERİNE geçer. **Üçündeki hiçbir hedef silinmedi.** Hepsi Bölüm 5'teki hedef kataloğunda kodlu satırlar olarak duruyor. Değişen tek şey: işin NASIL yapılacağı. Eski belgelerdeki ölçülemeyen ve birbiriyle çelişen kurallar, Codex'in aylarca aynı yerde dönmesine sebep oldu. Bu belge onları ölçülebilir, döngüye girmeyen kurallarla değiştirir.

Bu dosyayı depo kökünde `AGENTS.md` olarak tut. Her çalıştırmanın başında oku.

---

## 0. ESKİ KURALLARDAN NELER DEĞİŞTİ (ve neden)

| Eski kural | Yeni durum | Neden |
|---|---|---|
| "24 saatte %85/%80 bitir" | Kaldırıldı. Yerine çalışma paketleri (WP) ve kapılar | Ölçülemez, yüzdeler şişip düştü |
| "20 Codex yan yana çalışıyormuş gibi" | Tek ajan; Bölüm 4'te 12 rol gözlüğü | Codex paralel çalışamaz, bu cümle sadece telaş üretti |
| "Snipe-IT ile gram farkı olmadan aynı" | İşlev eşdeğerliği, kopya değil (Bölüm 9) | Snipe-IT AGPL-3.0 lisanslı |
| "Snipe-IT ibaresi kodda geçmeyecek" | Kaldırıldı. Lisans bildirimleri silinmez | Telif bildirimini silmek ayrı bir sorun |
| Next.js / Prisma / NextAuth yığını | Yığın C# / ASP.NET Core, DEĞİŞMEZ | Depo C#, geçiş iki ay boşa gitmesine sebep olur |
| "30 sn çabala ATLA" ve "60 dk takıl" (çelişkili) | Tek kural: en fazla 3 deneme (Bölüm 2, R4) | Çelişki döngü üretiyor |
| "Kullanıcı dur demedikçe durma" | Her çalıştırma en çok 5 WP, sonra DUR (R8) | Sonsuz çalışma emri döngü ve şişirilmiş rapor üretir |
| "Mükemmeliyetçilik YASAK" | Küçük, doğrulanmış değişiklik | Kanıtsız hız hata birikimi yaratır |
| "Yapamıyorum demek YASAK" | Yapamadığını ENGELLİ diye açıkça yaz | Aksi halde uydurma "bitti" çıkar |
| %95 kuralı | Korundu: kategori başına ≥ %95 PASS ve kritik FAIL = 0 | Ölçülebilir eşik |
| Prompt içindeki parola satırı | Kaldırıldı. Parola hiçbir yere yazılmaz | Güvenlik |
| "Yüzde ilerleme raporu" | Yasak. Sayı ve kanıt (örn. 380/412 PASS) | Şişirme sorunu |

Değişmeyen asıl hedef: **Snipe-IT'nin işlev olarak 2026'daki daha modern, daha güvenli, çok kiracılı, çok dilli hâli. Ve her buton, her menü, her seçenek gerçekten çalışacak.**

---

## 1. DEĞİŞMEZ GERÇEKLER

1. Yığın: **ASP.NET Core / C#** (`src/EnvanterTR.Web`, `src/EnvanterTR.PublicSite`), PostgreSQL, Docker, Coolify, Cloudflare DNS. Teknolojiyi DEĞİŞTİRME.
2. Alan adı `envantertr.com`; müşteri adresleri `isim.envantertr.com` (wildcard). Google Workspace: `admin@envantertr.com`.
3. Sahibi: Celal Belge. Sahibin şirketi henüz kurulu değil: **Iyzico ve her türlü gerçek ödeme/tahsilat KİLİTLİDİR (F14).** Şirket kurulmadan düzenli para alınmaz, fatura kesilmez; ücretsiz pilot ve demo serbesttir.
4. README'ye göre (20 Ekim 2026) canlıda 92 migration var, kodda 117; yayımlı plan yok, checkout kapalı, canlı SMTP yok. Firma alan adı 503 sorunu 20 Ekim'de giderilmiş görünüyor. **Doğrula, varsayma:** `README.md`, `docs/PROJECT-STATUS.md`, `tools/Check-CoolifyProduction.mjs`.
5. Çalışan hiçbir şeyi silme. Geri dönüşsüz işlem (migration silme, veri silme, canlı veri) sahibinden açık onay ister.
6. Önceki oturumların ve eski dokümanların yüzdelerine/iddialarına güvenme. Her kapıyı kendin yeniden doğrula.

---

## 2. DÖNGÜ VE HATA KORUMA SİSTEMİ (EN ÖNEMLİ BÖLÜM)

### 2.1 On iki kural

**R1. Tek paket.** Aynı anda yalnızca BİR çalışma paketi (WP) açıktır. DONE ya da BLOCKED olmadan yenisi başlamaz.

**R2. "Bitti" bir komuttur.** Her WP başında "bitti tanımı" yazılır: çalıştırılacak komut/test ve beklenen sonuç. Bitti = o komut geçer. Başka hiçbir şey "bitti" değildir. "Yaptım" cümlesi kanıt sayılmaz.

**R3. Hata düzeltme sırası.** (a) hatayı yeniden üreten başarısız test yaz (kırmızı) → (b) en küçük düzeltmeyi yap → (c) test yeşil → (d) ilgili regresyon seti yeşil → (e) commit. Kırmızı test olmadan düzeltme yok (yalnızca yazım/metin hatası hariç).

**R4. Üç deneme sınırı.** Bir WP en fazla 3 deneme alır. Her deneme FARKLI bir hipotez olmalı. Aynı hata imzası (hata mesajı + dosya + satır) ikinci kez çıkarsa yaklaşımı değiştir. Üçüncü başarısızlıkta WP **BLOCKED** olur: kök neden raporu yaz (denediklerin, bulgular, seçenekler), sıradaki bağımsız WP'ye geç. "Bir de şunu deneyeyim" yok.

**R5. Üstüne yama yok, geri al.** Bir değişiklik daha önce geçen bir testi bozarsa, üstüne yama yapma. Değişikliği geri al (`git revert`/`git restore`), nedenini anla, yeniden yap.

**R6. Okuma/yazma döngüsü yasağı.** Aynı dosyayı değiştirmeden 2'den fazla okuma, aynı dosyayı bir WP içinde 3. kez baştan yazma YOK. Gerekirse dur ve LEDGER'a yaz.

**R7. Yeşili tekrar koşturma.** Etkilenmeyen, zaten geçen testi tekrar tekrar çalıştırma. Her WP sonunda etkilenen set, her oturum sonunda tam set koşar.

**R8. Çalıştırma sınırı.** Bir çalıştırmada en çok **5 WP**. Sonra: commit → LEDGER ve HANDOFF güncelle → oturum raporu ver → DUR. Devam için sahibi "devam" der. (Eski "durma" emri iptal.)

**R9. Tek gerçek kaynak.** Durum yüzdelerle ya da el yazısıyla değil, `tests/reports/summary.json` ve `docs/LEDGER.md` dosyalarından türetilir. Rapor tablosu bu dosyalardan üretilir.

**R10. Yeni belge yasağı.** Yalnızca şu üç durum dosyası vardır: `docs/PROJECT-STATUS.md` (kapı tablosu), `docs/LEDGER.md` (WP kayıtları), `docs/HANDOFF.md` (sonraki oturum için not). Başka checklist/takip/rapor `.md` dosyası oluşturma. Test kanıt dosyaları (Bölüm 7.4) ve avukat için KVKK taslakları hariç.

**R11. Dürüstlük.** Çalıştıramadığını, erişemediğini ya da doğrulayamadığını açıkça **ÇALIŞTIRILMADI** ya da **ENGELLİ** diye yaz. Canlı (production) ortama erişimin yoksa canlı durum hakkında bir şey iddia etme.

**R12. F1 migration dondurması.** F1 kapısı `DONE` olmadan yeni EF migration oluşturma veya mevcut migration'ı değiştirme. Şema değişikliği gerektiren yeni iş `BEKLEMEDE` olarak kaydedilir; F1 tamamlanana kadar migration üretmeden sürdürülebilen bağımsız işler seçilir. F1 provası yalnızca açıkça onaylanmış, ağsız ve üretimden yalıtılmış klonda migration çalıştırabilir; canlı veritabanına migration uygulanamaz.

**R13. F1 blokajında bağımsız ilerleme.** F1 yedek/restore veya migration-parity kanıtı erişim yüzünden `BLOCKED` ise, sahibinin açık devam talimatıyla migration gerektirmeyen F4/F5/F6 doğrulama ve düzeltme paketleri sürdürülebilir. Bu işler F1'i `DONE` yapmaz; F1'e bağlı release/deploy/DB işlemi açmaz. Her paket kendi kanıtıyla LEDGER'a yazılır; şema değişikliği isteyen işler `BEKLEMEDE` kalır.

### 2.2 Kullanıcıya hiç kırık öğe gösterme
Tamamlanmamış bir özellik arayüzde GÖRÜNMEZ (özellik bayrağı ya da gizleme). Ölü (tıklayınca bir şey yapmayan) buton, boş sayfa, "yakında" yazan çalışmayan menü bırakılmaz. Bir öğe ya çalışır ya yoktur.

### 2.3 Bilinen döngü tuzakları ve karşılıkları

| Tuzak | Karşılık |
|---|---|
| Test yeşil olmuyor, sürekli yama | R4 + R5 |
| Doküman/checklist üretip durma | R10 |
| Yüzde şişirme/düşürme | Yüzde yasak, R9 |
| Güvenlik/izolasyonda sonsuz "alt tur" (D.18 gibi) | Yalnızca kapı kabul testini geçmek için gereken düzeltme yapılır; yeni alt tur açılmaz |
| Rastgele geçen/kalan (flaky) test | 3 kez koş; sonuç değişiyorsa **FLAKY** etiketle, LEDGER'a yaz, ayrı bir WP aç, kapıyı bununla bloklama |
| Derleme/paket/ağ hatası | Temiz ortamda `restore` + `build`; ağ ya da araç engeliyse **ENGELLİ** yaz |
| Uygulanmış migration'ı düzenleme | ASLA. Yeni migration ekle |
| Windows/Linux farkı | CI'da ikisi de koşar; fark varsa WP aç |
| Aynı sayfayı tekrar tekrar baştan yazma | R6 |
| Büyük, çok dosyaya yayılan WP | Böl. Bir WP en çok ~10 dosya ve tek bir amaç |

### 2.4 Savunma katmanları (hata yakalama sırası)
1. Derleme uyarıları ve statik analiz (uyarılar hata gibi ele alınır)
2. Birim testleri
3. Entegrasyon testleri (izole PostgreSQL; mevcut: `deploy/testing`, `tests/EnvanterTR.SecurityAcceptance`)
4. Tarayıcı/E2E testleri (Playwright; mevcut `tools/Verify-*.mjs` betikleri)
5. Çok kiracılık sızıntı testleri (CI'da zorunlu)
6. Regresyon taban çizgisi: her oturum başında tam set bir kez koşar ve sonuç kaydedilir; sonra düşüş olursa R5 uygulanır
7. Migration provası: `deploy/coolify/rehearse-migrations.sh`
8. Sağlık/durum kontrolü: `/api/status`, `tools/Check-CoolifyProduction.mjs`

---

## 3. ÇALIŞMA PAKETİ (WP) SÖZLEŞMESİ VE LEDGER

Her WP, `docs/LEDGER.md` içinde şu alanlarla bir satır/blok olarak tutulur:

```
WP-014 | Hedef: A01 toplu silme onay penceresi | Kapı: F5
Bitti tanımı: `node tools/Verify-BulkAssetActions.mjs` → exit 0
Dosya sınırı: en çok 10 dosya
Durum: TODO → IN_PROGRESS → VERIFY → DONE | BLOCKED
Deneme: 1/3  (hata imzası: <mesaj|dosya|satır>)
Kanıt: <komut + çıktı özeti + commit hash>
```

Kurallar:
1. WP numarası sırayla artar, silinmez.
2. VERIFY aşamasında Bölüm 4'teki rol gözlüklerinin ilgili sorularını sor (3–5 dakikalık kontrol listesi).
3. DONE'a geçmek için: bitti tanımı geçti + regresyon seti geçti + commit atıldı.
4. BLOCKED ise: kök neden + sahibine sorular `HANDOFF.md`'ye yazılır.
5. Bir WP "başka bir WP'nin bozduğu şeyi" düzeltiyorsa, ilk WP'ye referans verilir (sebep zinciri görünür olsun).

---

## 4. ROL GÖZLÜKLERİ ("40 mühendis" yerine 12 kontrol listesi)

Codex tek ajandır; 40 kişi paralel çalışamaz. Bunun yerine her WP'nin VERIFY aşamasında şu 12 gözlük sırayla takılır. Her gözlüğün 2–4 sorusu vardır; ilgisiz olanlar atlanır, ama atlandığı yazılır.

1. **Mimar:** Mevcut yapıya uyuyor mu? Yeni bağımlılık gerekli mi (gerekmiyorsa ekleme)? Çalışan bir şeyi bozuyor mu?
2. **Backend:** Yetki, tenant anahtarı, doğrulama, hata yönetimi var mı? Hata yutulmuyor, kayda ve kullanıcıya gidiyor mu?
3. **Frontend/UX:** Yükleniyor, boş, hata, yetkisiz durumları var mı? Çift tıklama engelli mi? Klavye ve Esc çalışıyor mu?
4. **Veritabanı/Migration:** Yeni migration mı, eski mi düzenlendi? Boş DB'de ve 92'lik kopyada çalışıyor mu? Geri dönüş planı var mı?
5. **Güvenlik:** Yetkisiz rol erişemiyor mu? CSRF, girdi doğrulama, çıktı kaçışı, sır sızıntısı var mı?
6. **QA/Test:** Önce kırmızı test yazıldı mı? Regresyon seti geçti mi? Test gerçekten veritabanı değişikliğini doğruluyor mu?
7. **DevOps/Canlı:** Yapılandırma ortam değişkeniyle mi? Windows/Linux ve Docker'da çalışıyor mu? Geri alma yolu var mı?
8. **KVKK (teknik):** Kişisel veri nerede tutuluyor? Silme/dışa aktarma/saklama süresi/denetim izi var mı?
9. **Performans:** 500+ satırda yavaşlıyor mu? Sayfalama ve indeks var mı?
10. **Erişilebilirlik ve çok dillilik:** Etiketler, kontrast, klavye; metinler 5 dilde ve tarih/sayı biçimi dile uygun mu?
11. **Ürün/Parite:** Hedef kataloğundaki satırı tam karşılıyor mu? Mantıksal olarak olması gereken seçenek eksik mi?
12. **Teknik yazar:** Kullanıcıya görünen metin doğru ve anlaşılır mı? HANDOFF/LEDGER güncel mi?

---

## 5. HEDEF KATALOĞU (EKSİKSİZ — HİÇBİR SATIR SİLİNEMEZ)

Kural: Bu listedeki her satıra K0/F0 ölçümünde **VAR / KISMEN / YOK / ÇALIŞTIRILMADI** durumu yazılır (kanıtlı). Hiçbir satır "kapsam dışı" diye silinmez; yalnızca faz sırası belirlenir (Bölüm 6). `docs/PROJECT-STATUS.md` ürün gereksinimleri ile F0–F15 faz kapılarını ayrı tablolarda tutar; ürün gereksinimi sayımına faz kapıları dahil edilmez. Her iki tabloda da **Kodda / Canlıda / Kabul edildi** durumları ayrı sütunlarda ve kanıtlarıyla yazılır.

### A. Çekirdek varlıklar
- **A01** Varlık: oluştur/oku/güncelle/sil, klonla, toplu güncelle, toplu sil, arşivle, iade, zimmet
- **A02** Model: marka + model + kategori + üretici + model no + ömür sonu (EOL)
- **A03** Durum etiketi: Dağıtılabilir / Dağıtımda / Arşivlendi / Arızalı (renk kodlu)
- **A04** Kategori, üretici, tedarikçi, departman, lokasyon, grup
- **A05** Değer, satın alma, garanti, sigorta, amortisman (%)
- **A06** Dinamik özel alanlar: metin, açılır liste, onay kutusu, tarih
- **A07** Varlık fotoğrafı, dosya eki
- **A08** Varlık geçmişi (zaman çizelgesi)

### B. Zimmet ve yaşam döngüsü
- **B01** Giriş/çıkış (checkin/checkout): kullanıcıya, lokasyona, varlığa
- **B02** Zimmet zaman çizelgesi; kimde ne kadar kaldı
- **B03** Toplu zimmet / toplu iade
- **B04** Zimmet formu: PDF + dijital imza + e-posta + QR
- **B05** Bekleyen talepler; rezervasyon (gelecek tarihli)

### C. Lisans yönetimi
- **C01** Ad, koltuk, kalan, bitiş, anahtar
- **C02** Koltuk atama / geri alma
- **C03** Lisans dosyası yükleme
- **C04** Süre yaklaşan uyarılar (30/15/7/3/1 gün)
- **C05** Maliyet takibi

### D. Aksesuar, sarf, bileşen, kit
- **D01** Aksesuar (klavye, mouse, kablo): zimmetlenir
- **D02** Sarf (toner, pil, kağıt): tükenir, minimum stok
- **D03** Bileşen (RAM, SSD): varlığa takılır
- **D04** Kit: laptop + çanta + mouse = hazır set
- **D05** Hepsi: ekle/düzenle/sil + zimmet + stok + minimum stok uyarısı

### E. Bakım, onarım, denetim
- **E01** Arıza, bakım, onarım: tip + başlangıç + bitiş + maliyet
- **E02** Fiziksel sayım (audit); son/gelecek denetim
- **E03** Bakım geçmişi, dosya eki

### F. Kullanıcı ve yetki
- **F01** Ekle/düzenle/sil, profil fotoğrafı, departman, lokasyon, yönetici
- **F02** Toplu içe aktarma (CSV)
- **F03** Devre dışı bırakınca otomatik iade
- **F04** Roller: Süper Admin, Şirket Yöneticisi, Editör, Okuyucu, Sınırlı, Muhasebe

### G. Diğer çekirdek özellikler
- **G01** Dosya yükleme (varlık, kullanıcı, lisans)
- **G02** Etkinlik günlüğü (audit)
- **G03** Raporlar: varlık, lisans, aksesuar, bakım, denetim, maliyet, zimmet
- **G04** İçe/dışa aktarma (Excel/CSV)
- **G05** LDAP / AD / SAML / SSO / Google OAuth (2FA ayrıca H10'da)
- **G06** REST API + API anahtarı
- **G07** Ayarlar: logo, para birimi, tarih, barkod
- **G08** Bildirimler (e-posta)
- **G09** Yedekleme (SQL + Excel/CSV/JSON)

### H. 2026 modern farklar
- **H01** Gerçek çok kiracılık + mimari düzeyde kiracılar arası sızıntı engeli
- **H02** Sınırsız şirket + paket bazlı kullanıcı (0–200 / 200–500 / 500+)
- **H03** Whitelabel (logo, renk, favicon)
- **H04** Modern tek sayfa deneyimi, sağ tık menüsü, Ctrl+K komut paleti, kısayollar, hover kart
- **H05** Barkod/QR/kamera (kamera ile okuma, USB okuyucu, etiket tasarımı)
- **H06** Dijital imza + anında PDF rapor
- **H07** Tek tıkla tam sistem yedeği (SQL, Excel, CSV, JSON)
- **H08** Iyzico abonelik + webhook + anında şirket kurulumu (KİLİTLİ, F14)
- **H09** Canlı dashboard grafikleri
- **H10** 2FA (TOTP), KVKK uyumu için teknik kontrol listesi, otomatik yedekleme
- **H11** Optimistik arayüz; saten parlak modern tasarım; koyu/açık tema
- **H12** 5 dil: Türkçe (ana dil), İngilizce, Makedonca, Rusça, Ukraynaca (teknik terimler İngilizce kalabilir)

### I. Süper admin paneli (`envantertr.com/admin`, yalnızca Süper Admin)
- **I01 Güvenlik:** 2FA zorunlu (Google/Microsoft Authenticator); IP beyaz liste (opsiyonel); ayrıntılı oturum kayıtları (IP, cihaz, tarayıcı, konum); 5 hatalı denemede 5 dakika kilit; kritik işlemlerde MFA yeniden doğrulama; oturum 8 saat (beni hatırla 30 gün)
- **I02 Dashboard kartları:** toplam şirket (aktif+pasif+demo), aylık gelir (MRR ₺), yeni kayıtlar, 7 gün içinde bitecek abonelikler, başarısız ödemeler (acil), aktif kullanıcı sayısı, sistem sağlığı (CPU/RAM/Disk/DB), bugünkü girişler
- **I03 Dashboard grafikleri:** 12 aylık gelir trendi (çizgi), şirket büyümesi (çubuk), plan dağılımı (pasta), en çok kullanılan modül (bar), coğrafi dağılım (harita)
- **I04 Dashboard tabloları:** son 10 kayıt, son 10 ödeme, yaklaşan yenilemeler
- **I05 Dikkat gerektirenler:** ödemesi 3+ gün gecikmiş (kırmızı), lisansı 7 gün içinde bitecek (turuncu), bugün kayıt olup işlem yapmayan (mavi), kapasite limitine yaklaşan (sarı)
- **I06 Şirket yönetimi:** kolonlar (logo, firma, subdomain, plan, kullanıcı sayısı, durum, kayıt tarihi, aylık ücret, bitiş tarihi); arama, filtre (plan/durum/tarih), sıralama, sayfalama (50/100/250), toplu işlem (askıya al/sil/duyuru/plan değiştir), CSV/Excel dışa aktarma
- **I07 Şirket detay, 7 sekme:** (1) Genel: künye, subdomain, logo, plan, yetkili, vergi, MRR, LTV; (2) Kullanıcılar: liste, şifre sıfırla, rol değiştir, devre dışı, sil; (3) Abonelik: plan, aylık ücret, sonraki ödeme, geçmiş (PDF), manuel fatura, uzat (1/3/12 ay bedava), plan değiştir, askıya al, aktifleştir, iptal, iade; (4) Kullanım: varlık, kullanıcı, lisans, disk, API, kapasite (%); (5) İletişim: mesaj, bildirim, geçmiş; (6) Log: giriş/çıkış, API, kritik işlem; (7) Tehlikeli: sil (30 gün geri alınabilir), sıfırla, tüm verileri indir
- **I08 Taklit modu:** "Panele Gir" düğmesi; parolasız giriş; sarı banner ("ABC Ltd. paneline taklit modunda giriş – Çıkış"); Süper Admin adına log; çıkışta süper admin paneline dönüş; müşteriye bildirim gitmez
- **I09 Fiyatlandırma ve kuponlar:** 4 düzenlenebilir plan, fiyat geçmişi, karşılaştırma önizlemesi, kuponlar (yüzde/sabit/ilk ay bedava), ekstra kaynak (disk, kullanıcı, premium modül)
- **I10 Abonelik ve faturalama:** aktif abonelikler; Iyzico ödeme takipleri; başarısız ödeme için 1/3/7. gün yeniden deneme; fatura üretimi; KDV raporu; demo hesap yönetimi (14→30 gün uzatma, sıfırlama) (ödemeye bağlı kısımlar KİLİTLİ)
- **I11 İletişim ve duyurular:** toplu/hedefli mesaj (e-posta/uygulama/SMS), çok dilli şablonlar (hoş geldin, ödeme, bakım), üst banner duyuruları
- **I12 Global kullanıcı yönetimi:** tüm şirketlerdeki kullanıcılar, parola sıfırlama zorunluluğu, oturum sonlandırma, ücretsiz özel kullanıcı rozetleri
- **I13 Sistem yönetimi:** gerçek zamanlı sağlık (CPU/RAM/Disk/Ağ); servis durumları (uygulama, Postgres, proxy, Cloudflare, Iyzico, Google); günlük/haftalık otomatik yedek; 30 günlük yedek geçmişi; yedek indir/geri yükle; ayrıntılı sistem logları
- **I14 Raporlar:** gelir (12 aylık, net, KDV); müşteri (churn oranı, ARPU); sistem kullanım yoğunluğu
- **I15 Ayarlar:** genel site; Iyzico API (sandbox/canlı); SMTP e-posta; güvenlik (MFA, kaba kuvvet koruması); entegrasyonlar (Google, Cloudflare, Slack, Webhook, SMS)
- **I16 Tasarım dili:** sol kenar çubuğu `#0A0A0F` koyu lacivert; Inter yazı tipi; saten parlak kartlar; 300 ms cubic-bezier geçişler; menü: Dashboard, Şirketler, Kullanıcılar, Abonelikler, Faturalar, Planlar, Kuponlar, Demo, Duyurular, Loglar, Raporlar, Sistem, Yedekleme, Ayarlar, Çıkış; üst çubuk: arama (Ctrl+K), hızlı işlem, bildirim, profil, tema; kısayollar: Ctrl+K, G+D/S/U, N+C/U, Esc

### J. Firma (müşteri) portalı (`firma.envantertr.com`)
- **J01** Dashboard: varlık/zimmet/lisans/bakım özeti
- **J02** Varlık oluşturma/düzenleme/silme, transfer
- **J03** Lisans, aksesuar, sarf yönetimi
- **J04** Bakım ve sayım modülleri
- **J05** Paket limitleri içinde kullanıcı/rol yönetimi
- **J06** Departman ve lokasyon tanımları
- **J07** Şirket içi özel raporlar
- **J08** Excel/CSV dışa aktarma
- **J09** Whitelabel ayarları
- **J10** Veri yedeği indirme
- **J11 İzolasyon sınırı:** firma yöneticileri başka şirketin verisini, süper admin panelini ve sistem geneli fiyat/plan yönetim ekranlarını ASLA göremez

### K. Satış sitesi (`envantertr.com`)
- **K01** Hero: ürün tanıtımı + 2 dakikalık video
- **K02** 9+ modül özellik kartı
- **K03** Fiyatlandırma tablosu: 4 plan karşılaştırması (ödeme açılana kadar "Demo iste/Ön kayıt", çalışmayan "Satın al" düğmesi YOK)
- **K04** Çözüm segmentleri: KOBİ / orta / kurumsal
- **K05** Kaynaklar: dokümantasyon, video, SSS
- **K06** İletişim: `admin@envantertr.com` + WhatsApp (wa.me)
- **K07** 5 dil, koyu/açık tema, tam mobil uyum
- **K08** Formlarda aydınlatma metni linki; pazarlama e-postası için önceden onay

### L. Kayıt, onboarding, ödeme
- **L01** Kayıt formu: firma, subdomain, e-posta, parola, yetkili
- **L02** Subdomain canlı kontrol
- **L03** E-posta doğrulama
- **L04** Plan seçimi
- **L05** Iyzico güvenli ödeme (KİLİTLİ)
- **L06** Webhook → otomatik şirket kurulumu (KİLİTLİ)
- **L07** Karşılama + 5 adımlı onboarding: (1) firma bilgileri, (2) logo + renk, (3) ilk toplu veri aktarımı, (4) kullanıcı davetleri, (5) ilk varlık kaydı
- **L08** Ödeme açılana kadar: platform yöneticisi manuel aktivasyon (ücretsiz pilot; para tahsilatı yok)

### M. Çok kiracılı mimari garantisi
- **M01** Her kiracı tablosunda zorunlu kiracı anahtarı
- **M02** Kiracılar arası veri sızıntısı İMKANSIZ: otomatik testle kanıtlanır
- **M03** Ara katman alt alan adından kiracıyı çözer
- **M04** Tüm sorgulara kiracı filtresi otomatik uygulanır
- **M05** Oturum + kiracı bağlamı birlikte doğrulanır
- **M06** Önbellek/kuyruk/dosya depolama anahtarları kiracı önekli
- **M07** Sızıntı testleri her yayında otomatik koşar
- **M08** Mevcut altyapıyı koru: sadece EKLE/DÜZELT; silmek için gerekçe + veri taşıma planı + risk analizi + sahibin onayı
- **M09** Wildcard yönlendirme: `^[a-z0-9-]+\.envantertr\.com$` (mevcut Coolify/proxy kurulumuyla uyumlu)

### N. Ürün hatları
- **N01** Bulut SaaS (V1 kapsamı, öncelik)
- **N02** Yerel Windows EXE
- **N03** Windows Server kurulumu
- **N04** Linux kurulumu
- **N05** Mobil iOS/Android

### O. Güvenlik ve KVKK (Bölüm 8'de ayrıntı)
- **O01** Her API'de yetki + kiracı kontrolü
- **O02** Oturum + yenileme, CSRF, hız sınırı
- **O03** Girdi doğrulama/temizleme (XSS, SQL enjeksiyonu)
- **O04** CSP başlığı, zorunlu HTTPS
- **O05** Güçlü parola özeti (mevcut mekanizma korunur, maliyet ayarı güvenli düzeyde)
- **O06** Sırlar yalnızca ortam değişkeninde; `.env` depoya girmez
- **O07** KVKK teknik kontrol listesi ve güncel mevzuat izleme

### P. Önceden "kanıtlı" iddia edilen modüller (KORUNACAK: yeniden doğrula, bozma)
Platform yönetici girişinde teşhis (migration/503 kontrolü); platformda Ctrl+K firma/üye araması; lisans CSV içe aktarma ve ek silme onayı; yazılım lisansı arşivleme/geri yükleme; lisans atama listesi (arama/filtre/sayfalama); lisans atama arayüzü (çalışan/cihaz/toplu/EULA); müşteri API varlık yazımı ve hassas özel alanlar; çalışan pasife alınca zimmet iadesi; atanmış yöneticiye CRM takip e-postası; demo CRM liste/arama/yarış koruması; başarısız tahsilat takip paneli (dahili kayıt); güvenlik sayfası oturum yönetimi; yerel Docker geliştirme ve migration sağlığı; login kök neden teşhisi.

### Q. Önceden "kısmen tamam" iddia edilen modüller (durumları F0'da ÖLÇÜLÜR, yüzde YAZILMAZ)
Bulut ürün hedefi; kiracı yapısı; çoklu şirket üyeliği; platform yönetici girişi ayrımı; güvenli davet/parola sıfırlama; varlık listesi; varlık detay; zimmet/iade/transfer; marka/kategori/model/QR/barkod; satın alma/tedarikçi; yazılım lisansı; aksesuar/sarf/bileşen/kit; çalışan/ekip/departman; lokasyon/bina/oda; bakım/denetim/takvim; raporlar/CSV/XLSX; yedek/dışa aktarma; paket/fiyat sayfası; demo talep formu ve CRM; şirket kaydı/kiracı kurulumu; checkout/ödeme; idempotent webhook; fatura/vergi/iade; yenileme/kupon/churn; platform genel görünüm; şirket listesi; global üye dizini; demo talebi/satış; paket kataloğu; abonelik uzatma; ödeme/sipariş/fatura; destek/denetim/KVKK; ana menü rotaları; menü alt fonksiyonları; hızlı eylemler/kısayollar; modal sekmeleri; tablo genişliği; mobil/erişilebilirlik; TOTP MFA; kritik işlemlerde MFA; kiracı izolasyonu; oturum/CSRF; audit kayıtları; KVKK/gizlilik; Docker veri kalıcılığı; Windows Server kurulumu; Linux kurulumu; GitHub Actions/CI; üretim migration; dokümantasyon/destek.

### R. Bilinen açık kapılar (20 Ekim'den; DOĞRULA)
Canlıda bekleyen migration (92 → 117); firma alan adının sağlığı (20 Ekim'de giderildiği yazıyor); yayımlı plan yok; checkout kapalı; canlı SMTP yok; gerçek müşteri hesabıyla uçtan uca kabul yapılmadı.

---

## 6. FAZ VE KAPI SIRASI (SIRAYLA, ATLAMA YOK)

Her faz, yukarıdaki katalog satırlarını kapsar. Bir faz bitmeden sonraki başlamaz (F14 hariç, o kilitli).

| Faz | Kapsam | Kabul (hepsi komutla kanıtlı) |
|---|---|---|
| **F0 Ölçüm** | Derle, tüm testleri koş, migration say, katalog durumlarını (VAR/KISMEN/YOK) kanıtla doldur, `PROJECT-STATUS.md`'yi tek tabloya indir | Tablo: her satırda durum + kanıt |
| **F1 Güvenli migration** | R | 3 Kasım yedeğinin SHA-256'sını manifestle karşılaştır; yedeği ağsız/portsuz izole klona geri yükle; klon migration kimliklerini kaynakla karşılaştır; klonda onaylı migration provasını yap; önce/sonra tablo satır sayılarını ve geri yüklemeyi doğrula; yazılı geri dönüş planı. F1 `DONE` olana kadar R12 gereği yeni migration yok; canlıya migration/deploy uygulanmaz. |
| **F2 Plan, kayıt, ödemesiz aktivasyon** | K03, L01–L04, L07, L08, I09 plan kısmı | Sıfırdan yeni şirket açıp panele girmeyi gösteren otomatik test. Planlar sahibinin verdiği adlarla yayımlanır (uydurma yok) |
| **F3 E-posta** | G08, SMTP ayarı | Sahte SMTP testi + sahibin canlıda yaptığı test e-postası; doğrulama/davet/sıfırlama e-postaları gider |
| **F4 Kiracı sızıntı testi** | M01–M09, O01 | Tüm API uç noktası envanteri; A şirketi B'nin verisine hiçbir uçtan (kayıt, dosya, rapor, dışa aktarma) erişemez; CI'da zorunlu |
| **F5 Çekirdek kabul + UI eylem matrisi** | A–G, P, Q, Bölüm 7 | Kategori başına ≥ %95 PASS, kritik FAIL = 0, sayılarla rapor |
| **F6 KVKK ve güvenlik** | O02–O07, Bölüm 8 | Kontrol listesi, her madde için kanıt ya da "sahibine/avukata devredildi" |
| **F7 Canlı kabul (sahibiyle)** | M09, K, canlı ortam, yedek, geri dönüş | Sahibin onayladığı canlı kontrol listesi. **Bu kapıdan sonra "satılabilir ilk sürüm (ödemesiz)" denir** |
| **F8 Parite boşlukları** | Snipe-IT işlev listesi (Bölüm 9 kuralıyla) ile karşılaştır; A–G'deki boşluklar | VAR/KISMEN/YOK tablosu; P1 boşluklar sırayla eklenir (her biri: uygulama + test + F5 matrisine satır). Öncelik (P1/P2/P3) sahibiyle onaylanır |
| **F9 Süper admin tam kapsam** | I01–I16 (ödeme dışı) | Her alt madde için test + F5 matrisi satırı |
| **F10 Firma portalı tam kapsam** | J01–J11, H03 | Aynı |
| **F11 Satış sitesi tam kapsam** | K01–K08 | Aynı |
| **F12 Tasarım, dil, tema, mobil, erişilebilirlik** | H04, H09, H11, H12, Bölüm 7 K–M | Aynı |
| **F13 Entegrasyonlar** | G05 (LDAP/AD/SAML/SSO/Google OAuth), I11 SMS, I15 Slack/Webhook/Cloudflare | Aynı |
| **F14 KİLİTLİ: ödeme ve satış** | H08, L05, L06, I10 (ödemeye bağlı), fatura/kupon/yenileme/KDV raporu | Sahibi yazılı olarak "şirket kuruldu, mükellefiyet tamam, Iyzico hesabı onaylı" demeden BAŞLAMA. Mevcut Iyzico kodu yalnızca sandbox'ta çalışır tutulur |
| **F15 Ürün hatları** | N02–N05 | Windows EXE, Windows Server, Linux, mobil: ayrı WP'ler, ayrı kabul |

---

## 7. UI/EYLEM TEST MATRİSİ (HER TIKLANABİLİR ÖĞE)

### 7.1 Amaç ve yöntem
Uygulamadaki **her buton, bağlantı, tıklanabilir yazı, menü ve alt menü, açılır liste (dropdown) ve içindeki her seçenek, sekme, modal, satır menüsü (3 nokta), toplu işlem seçeneği, kısayol ve form** doğru sonuca ulaşır; eksik, ölü ya da mantıksız çalışan öğe kalmaz. Listeyi elle yazma: mevcut `tools/Generate-UiInventory.mjs`, `docs/ui-inventory.md` ve `tools/Verify-*.mjs` betiklerini YENİDEN YAZMA, GENİŞLET. Her sayfayı Playwright ile gez, tıklanabilir her öğeyi bul, tıkla, sonucu kaydet; iç içe ve açılır menülerin her seçeneğini tek tek aç; her rolle (şirket sahibi, yönetici, kısıtlı rol, platform yöneticisi) ayrı tara.

Her öğe için bir satır: `sayfa | öğe | beklenen sonuç | gerçek sonuç | PASS/FAIL | kanıt`. Beklenen sonuç öğenin mantığına uygun olmalı (örn. "Sil": onay ister, kayıt silinir, denetim kaydı düşer, yetkisiz rol butonu görmez). Veri gerçekten kaydoluyor mu veritabanında doğrula; sadece "başarılı" mesajına bakma.

Mantıken olması gereken ama olmayan seçenek bulursan **EKSİK** diye listele: küçükse ekle, büyükse F8 listesine yaz. Hiçbir şey yapmayan (ölü) buton: ya gerçek işleve bağla ya gizle/kaldır.

### 7.2 Kategori kontrol listeleri (her biri tam olarak sorgulanır)

**A. Bağlantılar:** hedef doğru · tıklanınca gidiyor · yetkisiz → 403 · kırık bağlantı (404) yok · dış bağlantı yeni sekmede (`target="_blank"`)

**B. Butonlar:** tıklanabilir (devre dışı değilken) · tıklama işlevi bağlı · veritabanında değişiklik oluyor · yükleniyor göstergesi · başarı/hata mesajı · çift tıklama engeli · yetkisiz rol görmüyor · klavyeyle erişilebilir

**C. Menü ve alt menüler:** ana menü açılıyor · alt menü açılıyor · her alt öğe tıklanabilir · aktif sayfa vurgulu · mobil hamburger · klavyeyle gezinme · Esc kapatıyor

**D. Kısayollar:** Ctrl+K komut paleti · `/` arama odağı · Ctrl+N yeni kayıt penceresi · Esc pencere kapatır · `?` yardım · G+D/S/U gezinme · kısayol çakışması yok

**E. Tablolar:** kolonlar doğru · başlığa tıklayınca sıralama · filtre · arama · sayfalama · sayfa boyutu (10/25/50/100) · satır seçimi (onay kutusu) · toplu işlem menüsü · satır menüsü (3 nokta) · hover efekti · boş liste durumu · yükleniyor iskeleti · 500+ satırda performans

**F. Formlar:** her alan yazılabilir · zorunlu alan (*) denetimi · doğrulama mesajı · e-posta biçimi · parola güç göstergesi · parola göster/gizle · Enter ile gönderme · hatalı alan kırmızı · başarı sonrası yönlendirme · çift gönderim engeli · sunucu tarafı doğrulama

**G. Modallar:** açılıyor · Esc kapatıyor · dış alana tıklayınca kapanıyor · X düğmesi · içerik kaydırma · mobilde tam ekran · arka plan bulanıklığı · odak tuzağı (focus trap) · Evet/Hayır düğmeleri çalışıyor

**H. Sekmeler:** her sekme tıklanabilir · içerik değişiyor · adres değişiyor (`?tab=xyz`) · aktif vurgu · klavye (← →) · yenilemede aynı sekme

**I. Açılır liste ve seçimler:** açılıyor · seçenekler görünüyor · seçim yapılabiliyor · klavye (↑↓ Enter) · uzun listede arama · çoklu seçim

**J. Bildirimler (toast):** 3 sn sonra kaybolur · başarı yeşil, hata kırmızı, uyarı sarı · elle kapatılabilir · üst üste yığılır

**K. Duyarlı tasarım:** 375 · 414 · 768 · 1024 · 1920 px · yatay kaydırma yok · tablolar kart görünümüne dönüyor · dokunma hedefleri ≥ 44 px

**L. Tema:** değiştirici çalışıyor · tüm sayfalarda yumuşak geçiş · tercih saklanıyor · yenilemede korunuyor · kontrast ≥ 4.5:1 · grafikler temayla uyumlu

**M. Dil:** değiştirici çalışıyor · tüm metinler çevrili · eksik çeviri yok · tarih biçimi dile uygun · sayı biçimi (1.000,00 / 1,000.00) · para birimi dile uygun · tercih saklanıyor

### 7.3 Test kanıt biçimi
Başarılı:
```
[TEST]: Bağlantı - "Dashboard"
[BEKLENEN]: /dashboard sayfasına gider
[AKSİYON]: Tıklandı
[SONUÇ]: PASS
[KANIT]: URL /dashboard, sayfa yüklendi, kenar çubuğu aktif
```
Başarısız:
```
[TEST]: Buton - "Kaydet"
[BEKLENEN]: Varlık veritabanına kaydedilir
[AKSİYON]: Tıklandı
[SONUÇ]: FAIL
[KANIT]: POST /api/assets → 500, konsol: "tenantId missing"
[KÖK NEDEN]: ...
[DÜZELTME]: ... (R3 sırasıyla: kırmızı test → düzeltme → yeşil)
```

### 7.4 Test paketi ve kanıt dosyaları
- Tarayıcı testleri (Playwright): bağlantılar, butonlar, formlar, modallar, kısayollar, duyarlılık (5 çözünürlük), tema, 5 dilli çeviri. Mevcut betiklere ekle.
- Birim testleri ve entegrasyon testleri (API uç noktaları, izole PostgreSQL). Kapsam ölçümü araçla yapılır; kritik modüllerde hedef ≥ %70, sayı kanıtlıdır.
- Uçtan uca (E2E) kritik akışlar: kayıt → şirket kurulumu → panel (ödeme hariç, F14'e kadar); varlık → zimmet → iade; kullanıcı daveti → rol → giriş; abonelik iptali → veri → yeniden aktifleştirme.
- Kanıt dosyaları: `tests/reports/ui-inventory-YYYY-MM-DD.md`, `ui-test-results-YYYY-MM-DD.md`, `screenshots/` (başarısız öğeler), `summary-YYYY-MM-DD.json`.

### 7.5 Toplu rapor şablonu (her 5 WP'de bir ve oturum sonunda)
```
UI TEST ÖZETİ (WP-xxx)
Tarih:                 Toplam öğe: X   PASS: X   FAIL: X   ATLANMIŞ: X
Bağlantılar: 138/142 · Butonlar: 380/412 · Menüler: ../.. · Formlar: ../.. ...
Yeni eklenen öğeler: X bağlantı, X buton, X modal
Düzeltilen hatalar:  ...
Açık kalanlar:       ...
Sıradaki WP:         ...
```
Kapı eşiği: kategori başına ≥ %95 PASS ve kritik FAIL = 0. Bu özet olmadan sonraki fazın ilk WP'si başlamaz. Sayılar test çıktısından gelir, elle yazılmaz.

### 7.6 Dörtlü kalite kontrolü (her WP için)
- **QA-1 İstek:** İstenen oldu mu? Kabul kriteri karşılandı mı?
- **QA-2 Kod:** API doğru mu? Yetki, kiracı anahtarı, hata durumu var mı?
- **QA-3 Arayüz:** Düğme, modal, duyarlılık, bağlantı/menü/kısayol testleri geçti mi?
- **QA-4 Regresyon:** Eski özellikler bozulmadı mı? Test koşuldu mu?
Dördü de PASS değilse WP DONE olmaz.

### 7.7 Sekiz zorunlu V1 kabul kapısı
1. Şirket/platform giriş, rol, parola kurtarma (MFA dahil)
2. Tüm ilgili API ve dosyalarda kiracı izolasyonu
3. Envanter, ilişki, zimmet, hareket, dosya, QR, içe/dışa aktarma
4. Menü/kısayol/modal/sekme/filtre/tablo/duyarlılık test envanteri
5. Demo/kayıt/abonelik akışı (ödeme hariç, ödeme F14'te)
6. Platform şirket/üye/davet/demo/plan/audit (ödeme hariç)
7. Yedekleme/geri yükleme/izleme/e-posta/destek/dokümantasyon
8. Bulut ortamı, DNS/TLS, migration, sır yönetimi, yedek, geri dönüş

---

## 8. GÜVENLİK, KVKK VE MEVZUAT KURALLARI

### 8.1 Teknik güvenlik (O01–O06)
Her API'de yetki + kiracı kontrolü · oturum yönetimi + yenileme · CSRF · hız sınırı · girdi doğrulama ve çıktı kaçışı (XSS, SQL enjeksiyonu) · CSP başlığı · zorunlu HTTPS · güçlü parola özeti (mevcut mekanizmayı koru) · sırlar yalnızca ortam değişkeninde, koda/loga/dokümana/commit'e/test çıktısına yazılmaz · `.env` depoya girmez · depoda parola/anahtar bulursan temizle ve sahibine raporla.

### 8.2 KVKK ve mevzuat (O07). Ceza riskini azaltmak için, hukuki görüş DEĞİLDİR
1. **"KVKK uyumlu" DEME.** Doğru ifade: "teknik kontrol listesi tamamlandı; hukuki uygunluk avukat onayı gerektirir."
2. **Kaynak önceliği:** Resmî Gazete ve `kvkk.gov.tr` (Kurul kararları, rehberler). Sosyal medya, forum ve haber önerileri hukuki dayanak DEĞİLDİR; "ADAY MADDE" diye ayrı listelenir, resmî kaynakla doğrulanmadan uygulamaya girmez.
3. **Roller:** Müşteri şirketlerin çalışan verisinde uygulama sağlayıcısı büyük ihtimalle "veri işleyen"dir; kendi topladığı verilerde (demo formu, kayıt, iletişim) "veri sorumlusu"dur. Müşteri sözleşmelerine eklenecek **veri işleme sözleşmesi taslağı** hazırla (avukat için ayrı dosya).
4. **Teknik özellikler:** aydınlatma metni ve gizlilik sayfaları (taslak); çerez/izin bildirimi; şirket verisi dışa aktarma ve silme; saklama ve imha süreleri; denetim izi; yedek şifreleme; MFA; oturum yönetimi; hız sınırı; güvenlik başlıkları.
5. **Yurt dışı aktarım envanteri:** Hangi veri hangi servise (Cloudflare, Google Workspace, hosting) gidiyor ve sunucular nerede? Tablo hazırla; 2024 sonrası aktarım kuralları (yeterlilik kararı, standart sözleşme ve bildirim vb.) için avukata götürülecek liste çıkar.
6. **VERBİS:** Kayıt yükümlülüğü eşiklerini resmî kaynaktan doğrula ve sahibine sor (çalışan sayısı, bilanço, faaliyet türüne bağlıdır).
7. **Veri ihlali prosedürü:** Yazılı süreç (müşteriye ve Kurul'a bildirim; süre ve içerik resmî kaynaktan doğrulanır).
8. **Siber Güvenlik Kanunu (7545)** ve ikincil düzenlemelerin gelişimini izle; uygulanabilirliği avukata sor.
9. **Ticari elektronik ileti:** Pazarlama e-postası için önceden onay; formlarda aydınlatma metni.
10. **Satış sitesi:** Ödeme açılana kadar çalışmayan "Satın al" düğmesi, para tahsilatı ve fatura yok. Fiyatlar "lansman öncesi, değişebilir" notuyla gösterilir. Satış başladığında işletme bilgileri (unvan, adres, iletişim) yayımlanır. Detay için sahibin mali müşaviri.
11. Taslak metinleri ve listeleri avukat için tek bir klasörde topla (`docs/legal-drafts/`). Bu klasör R10'un istisnasıdır.

### 8.3 Alan modelleri (her biri kiracı anahtarı + oluşturma/güncelleme/silinme tarihi taşır)
Kiracı, Kullanıcı (2FA gizli anahtarı güvenli saklanır), Varlık, Model, Kategori, Üretici, Tedarikçi, Durum Etiketi, Lokasyon, Departman, Lisans, Aksesuar, Sarf, Bileşen, Kit, Zimmet, Bakım, Denetim, Abonelik, Fatura, Ödeme, Denetim Kaydı, Bildirim, Duyuru, Özel Alan, Alan Değeri, API Anahtarı, Webhook, E-posta Şablonu. Mevcut şemada karşılığı varsa onu kullan, yeniden yaratma; eksik olanı yeni migration ile ekle.

### 8.4 Kullanılacak yetenekler
Mevcut C# yığınında karşılığı olan şeyi kullan, yeni bağımlılık eklemeden önce gerekçe yaz. Gerekli yetenekler: ORM/migration, kimlik doğrulama + TOTP, QR üretimi, kamera ile barkod/QR okuma, PDF üretimi, Excel/CSV, grafikler, form doğrulama, test (Playwright + birim/entegrasyon), Docker + Coolify + proxy. Bağımlılık eklenirse sürüm sabitlenir ve `docs/THIRD-PARTY-*` kayıtları güncellenir.

---

## 9. SNIPE-IT KURALI (LİSANS)

1. Hedef, Snipe-IT ile **işlev eşdeğerliği** ve üstüne yeni özelliklerdir. Kod, metin ya da tasarım kopyası DEĞİL.
2. Snipe-IT **AGPL-3.0** lisanslıdır. Kaynak kodunu, ekran metinlerini, çevirilerini, veritabanı şemasını ya da görsel düzenini kopyalama, uyarlama ya da PHP'den C#'a satır satır çevirme.
3. "Tersine mühendislik" = **davranış gözlemi** (dokümantasyon, canlı demo, ekranın ne yaptığı). Kaynak kodu okuyup taklit etmek değil. Önce özelliğin ne yaptığını kendi cümlenle yaz, sonra kendi tasarımınla uygula.
4. Snipe-IT'nin adını, logosunu ve markasını üründe kullanma.
5. Depodaki lisans/telif bildirimlerini ve üçüncü taraf notlarını SİLME. Doğrudan kopya olduğunu düşündüğün bir parça bulursan silme, sahibine listele. Hukuki karar avukata aittir.
6. Karşılaştırma aracı: Snipe-IT'nin herkese açık dokümantasyonundan işlev listesi çıkar; EnvanterTR'deki karşılığı VAR/KISMEN/YOK diye işaretle (F8).

---

## 10. OTURUM SONU RAPORU

1. Kapı/faz tablosu: Durum | Kanıt komutu | Çıktı özeti | Açık sorun. (Yüzde YOK.)
2. Bu oturumda kapanan WP'ler ve commit'ler.
3. BLOCKED olanlar: kök neden ve sahibine sorular.
4. Çalıştırılamayan testler ve nedenleri (ÇALIŞTIRILMADI/ENGELLİ).
5. Değişen dosyalar.
6. Sıradaki ilk WP.
7. Sahibinden gereken bilgiler (Bölüm 11).

Rapor dili Türkçe, kısa ve düz. Bahane ve süs yok.

## 11. SAHİBİNDEN İSTENECEKLER (sor, UYDURMA)

Plan adları, fiyatlar, kullanıcı limitleri, KDV oranı · SMTP gönderici adresi · canlıya basma, DNS ve Coolify işlemleri (sahibi yapar, sen komut ve kontrol listesi ver) · şirketin ve Iyzico hesabının durumu · F8 boşluklarının öncelik onayı · hosting/sunucu konumu bilgisi (KVKK aktarım envanteri için).

## 12. DUR VE SOR KOŞULLARI

Şu durumlarda işi durdur ve sahibine sor: geri dönüşsüz işlem gerekiyorsa · sır/anahtar/parola gerekiyorsa · faz sırası ile yapılacak iş çelişiyorsa · bir kapıyı geçmek için bu belgenin dışında iş gerekiyorsa · bir WP üç denemede BLOCKED olduysa ve sıradaki bağımsız WP kalmadıysa.

---

BAŞLA: Bölüm 2'yi oku, `docs/PROJECT-STATUS.md` ve `docs/HANDOFF.md` varsa oku (yoksa HANDOFF'u oluştur), F0 ölçümüne başla. Her çalıştırmada en çok 5 WP, sonra rapor ve DUR.
