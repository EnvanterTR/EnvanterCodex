# Dünden gelen istekler ve kontrol listesi

**Kullanıcının başlangıç listesi:** Alttaki 0–T bölümleri ve durum işaretleri, kullanıcının gönderdiği notlardan aynen korunmuştur. İşaretler bağımsız olarak doğrulanmış güncel sonuç değildir.

## 19 Ekim 2026 — dağıtım hedefleri için ayrı güncel durum

**Kullanıcının öncelik talimatı (19 Ekim 2026 tekrar teyit edildi):** İlk ve öncelikli teslim web uygulamasını tüm açık hata ve eksik işlevleri tek tek kapatıp kabul ölçütleriyle doğrulayarak %100'e ulaştırmaktır. Windows 11, Windows Server ve Ubuntu dağıtımlarını bu web hedefinin ilerleme yüzdesine karıştırma; her birini kendi hedefi ve kabul kanıtıyla takip et. Yüzde 100 yalnızca web kapsamındaki maddeler doğrulandığında yazılabilir; henüz doğrulanmayan canlı SaaS/tenant erişimini tamamlanmış sayma.

**19 Ekim 2026 — son talimat:** Öncelik sırası kesin olarak web projesinin %100 tamamlanmasıdır. Web kapsamındaki tüm açık hata/işlevler ve arayüz menüleri, alt fonksiyonları, kısayolları, açılır pencereleri ve bağlantıları kabul kanıtıyla kapanmadan bu hedef bitmiş sayılmaz. Windows 11, Windows Server ve Ubuntu hedefleri ayrı raporlanır.

**19 Ekim 2026 21:36 TRT — son web kabul kanıtı:** Tam hosted Chromium regresyonu exit 0, hedefli izole PostgreSQL satın alma/tenant testi 1/1 geçti. Bu, lisans satın alma özelliğini kanıtlar; genel web V1 kabul yüzdesini tek başına 100 yapmaz.

- **Yerel geliştirme Docker'ı (20 Ekim 2026 00:19 TRT):** `localhost:8080` API ready, yerel veritabanı 117 migration / 0 bekleyen. Doğrulanmış yedek/izole restore ardından şirket güvenlik politikası güncellemesi uygulandı; gerçek yönetici hesabıyla API girişi, güvenlik politikasını okuma ve çıkış geçti. Bu yalnızca bu bilgisayardaki ortamdır.
- **Canlı Cloud SaaS (20 Ekim 2026 yeniden kontrol):** `https://envantertr.com/api/status` HTTP 200 `ready`, 92 migration / 0 bekleyen. `/api/setup/status`: şirket kurulumu gerekli, şirket kurulum anahtarı sunucuda ayarlı. `/api/platform/bootstrap/status`: ilk platform yöneticisi gerekli, platform kurulum anahtarı ayarlı değil. `https://firma.envantertr.com/api/status` HTTP 503 `no available server`; wildcard/proxy yönlendirmesi hâlâ kabul edilmedi. `/api/plans` HTTP 200 ve 0 yayımlı plan; `/api/checkout/status` HTTP 200 ama checkout kullanılamıyor. Canlı DB'de ilk hesap/şirket olmadığı için canlı şirket girişi kabulü tamamlanmış değildir. Yerel 117 migrationlı ortam canlıyla aynı sürüm değildir.

- **Windows 11:** imzasız Setup.exe önizlemesi var; temiz makine kurulumu ve imzalı son paket kabul edilmedi.
- **Windows Server:** ayrı servis/kurulum hedefi; gereksinimlerde prototip/kısmi.
- **Ubuntu:** ayrı Docker Compose + Caddy hedefi; yapılandırma prototipi var, temiz sunucu baştan sona kabul edilmedi.

Canlı ve yerel Cloud kontrolünün tarihli ayrıntısı: [`SAAS-DEPLOYMENT-STATUS-2026-10-13.md`](SAAS-DEPLOYMENT-STATUS-2026-10-13.md). Aşağıdaki ilk kontrol notları 11 Ekim'deki **tarihsel** snapshot'tır; içlerinde geçen 75 migration gibi sayılar güncel durum değildir.

## 11 Ekim 2026 — başlangıç kontrol fotoğrafı (tarihsel)

- Hedef bu konuşmada **aktif** durumda. Kullanıcının giriş hatası, önceki Snipe-IT ekran/notlarından türetilen kabul ölçütleri, menü/arka plan/buton denetimi ve v1'e kadar sürdürme isteği tek ürün hedefinin kapsamındadır; ara ekrandaki eski “durakladı” görüntüsü mevcut hedef durumunu değiştirmez.
- Windows 11 için tek dosyalı `Setup.exe` önizlemesi yerel olarak derlendi ve gömülü 65 dosya `--verify` ile doğrulandı. Paket imzasız; kurulum çalıştırılmadı ve temiz Windows 11 hedef kabulü yok. Bu nedenle ürün indirme sayfasında yayımlanmamalı.

- Docker Desktop Engine çalışıyor. `envantertr-product-dev` altında `web` (`localhost:8080`), `public-site` (`localhost:8081`) ve sağlıklı PostgreSQL 17 servisi ayakta.
- Yerel `/api/status`: `ready`, 75 migration, 0 bekleyen; web uygulaması `localhost:8080` ve `localhost:8081/demo/` HTTP 200.
- Bu çalışmadaki varlık denetimi: Snipe-IT referansındaki 19 sütun kaynağa alınmış tabloda birer kez bulundu; eksik/çift başlık yok. Varlık ayrıntısında yaş, model amortisman ilerlemesi ve tahmini mevcut değer eklendi. İzole PostgreSQL güvenlik/tenant paketi 76/76, yeni 12/36 ay ve 1.200 USD hesaplama senaryosu 1/1 geçti; beş dilde 1.447 UI metni ve üç gömülü JavaScript bloğu doğrulandı. Docker web imajı yenilendi; localhost:8080/api/status ready/75 migration/0 bekleyen, app ve demo HTTP 200.
- Önceki kontrol kaydı (9cb4349): Release Docker web imajı, 76/76 güvenlik/tenant testi ve sözleşme kapsamı doğrulanmıştı. Bu kayıt yeni varlık yaşı/değeri kontrolünün öncesindendir.
- Yerel Compose kurtarma provası: şifreli arşiv/HMAC, 74 migration, dokuz temel tablo sayısı, bir medya dosyası ve iki Data Protection anahtar arşiv girdisi doğrulandı; geçici dosyaların içerik hash'leri geri yüklenen arşivlerle eşleşti. Prova konteyneri, parola, arşiv ve geçici volume dosyaları temizlendi. Bu, gerçek Windows Server/Ubuntu host kabulü değildir.
- GitHub senkronu/ücret kontrolü: yerel main ve origin/main bu çalışmanın başında 42d32aa düzeyinde eşitti. Varlık yaşı/değeri özelliği, kabul testi ve durum belgeleri 836e5d5 [skip ci] commit olarak GitHub main dalına başarıyla gönderildi. Hosted workflow tetiklenmedi; doğrulama yerel Docker/.NET ortamında yapıldı.
- Canlı `https://envantertr.com/api/status`: bu kontrolde `migration-required`, 72 migration, 2 bekleyen olarak yanıt verdi. Son iki kaynak migration'ı mevcut varlıklara isteğe bağlı varsayılan konum bağlantısı/model numarası ve varlık notu alanı ekliyor. Güncelleme betikleri geçişten önce şifreli yedek ve izole geri yükleme provası ister; canlı veritabanı yedeğinin sunucuda doğrulandığına dair erişilebilir kanıt olmadan canlı migration çalıştırılmayacak.
- Yerel giriş kanıtı (11 Ekim 2026): parola kurtarma aracından sonra CSRF korumalı login API HTTP 200 döndü; oturum sorgusu HTTP 200 ile etkin şirket bağlamını doğruladı; oturumdan çıkış için giriş sonrası yenilenmiş CSRF belirteci kullanıldığında HTTP 204 alındı. İlk çıkış isteğinde giriş öncesi belirteç 400 döndürdü; bu beklenen kimlik-bağlı antiforgery davranışıdır. Bu kanıt yerel Docker içindir; tarayıcı UI ve canlı/müşteri sunucu kabulü değildir.
- Demo menü kontrolü: yerel demoda 50 hedef tarandı; 11 desteklenen örnek ekran ve 39 açık “yakında” ekranı doğrulandı. Boş ekranlarda genel bakış kartları, ilgisiz kayıtlar, arama/dışa aktarma ve varlığa özgü işlemler görünmüyor; Varlıklar ekranına dönünce araçlar geri geliyor. Boş durum metni beş dilde örnek bir menüde, menülerin tamamı Türkçe'de tarayıcıda kontrol edildi. Bu kanıt misafir demo ile sınırlı; kimlik doğrulamalı ürün ekranlarının tam kabulü değildir.

Durum işaretleri: ✅ Var · 🟡 Kısmen var (eksik) · ❌ Hiç yok · ⚠️ Riskli/karar gerekiyor

Nasıl kullanacaksın: Her satırın solundaki kutuyu işaretle → "bunu istiyorum" demek. İstemediğini sil, notunu yanına yaz. Sonunda bana geri ver, ona göre plan çıkarayım.

0. ÜRÜN HATLARI (5 hedef)
 0.1 Bulut SaaS — firma.envantertr.com alt alan adı, çok kiracılı, tarayıcıdan. 🟡
 0.2 Kurum içi Windows tek bilgisayar — indir, çalıştır, ileri-ileri kur (klasik sihirbaz). ❌
 0.3 Kurum içi Windows Server — ofis sunucusunda servis olarak, LAN'dan tarayıcıyla. 🟡
 0.4 Kurum içi Linux (Ubuntu) — Docker Compose + Caddy HTTPS. 🟡
 0.5 Mobil uygulama (iOS + Android) — ⚠️ Şu an kapsam dışı, ayrı proje. Karar senin.
A. TASARIM, MARKA VE DİL
A1. Şikayet ettiğin konular (öncelikli düzeltme)
 A1.1 Tarayıcı sekme başlığı "EnvanterTR" ✅ (`localhost:8080/` yanıtı ve uygulama HTML başlığı doğrulandı)
 A1.2 Uygulama ile envantertr.com/demo/ birebir aynı görünmeli (kabuk, renk, tipografi, boşluk) 🟡
 A1.3 Snipe-IT'ten gelen terimler temizlenecek (Varlıklar, Aksesuarlar, Sarf malzemeleri → senin terminolojin) 🟡
 A1.4 Snipe-IT'ten tek satır kod/isim/görsel kalmayacak ⚠️ Diskte 5 kopya var, temizlik kararı gerekiyor
 A1.5 Kafana göre tasarım yapılmayacak → her görsel karar önce sana gösterilecek ❌ (süreç kuralı)
 A1.6 Boş/geliştirilmemiş ekranlar "yakında" diye açıkça söylenecek, dashboard göstermeyecek 🟡
A2. Tasarım tercihlerin (eski notlarından)
 A2.1 Kırmızı ana marka rengi #DC2626 🟡
 A2.2 Koyu temada vurgu mavi #3B82F6 (altın değil) ✅ demoda var
 A2.3 Font Inter, serif (Fraunces) kesinlikle yok ✅
 A2.4 Inter fontu kendi sunucumuzdan yüklenmeli (Google'a bağımlı olmasın) ✅ (Inter 4.1 değişken WOFF2, uygulama ve public-site statik dosyalarından yükleniyor)
 A2.5 Saten/parlak yüzeyler, pastel renk yok 🟡
 A2.6 iPhone gibi yumuşak tema geçişi (ani renk atlaması yok) 🟡
 A2.7 Abartılı büyük punto yok (72px hero kötüydü) 🟡
 A2.8 Açık tema + koyu tema, ikisi de tam çalışacak 🟡
 A2.9 Renk paleti seçici (demoda 5 palet var: Mavi/Yeşil/Mor/Kehribar/Turkuaz) ✅ demoda var
 A2.10 Şirket logosu + ana rengi değiştirilebilir (whitelabel) ✅
 A2.11 Mobil/tablet uyumlu (telefondan da kullanılabilir) 🟡
 A2.12 Erişilebilirlik: klavyeyle gezinme, ekran okuyucu etiketleri 🟡
A3. Dil desteği
 A3.1 Türkçe ✅
 A3.2 İngilizce 🟡
 A3.3 Rusça 🟡
 A3.4 Ukraynaca 🟡
 A3.5 Makedonca 🟡
 A3.6 Sırpça + Almanca (hedef dokümanda geçiyor) ❌
 A3.7 Dil seçici navbar'da bayrak + kod ile (TR TR) ✅
 A3.8 ⚠️ Tüm ekranların çevrilmesi (şu an sadece bir kısmı) 🟡
B. GİRİŞ, HESAP VE GÜVENLİK
 B.1 Şirket hesabıyla giriş ✅
 B.2 Birden fazla şirkete üye hesap → parola sonrası şirket seçimi ✅
 B.3 Parola sıfırlama e-postası (tek kullanımlık token) ✅
 B.4 İki aşamalı doğrulama (Authenticator/TOTP) + kurtarma kodları ✅
 B.5 Aktif oturumları uzaktan kapatma (güvenlik damgası) ✅
 B.6 Giriş denemesi hız sınırı + hesap kilitleme ✅
 B.7 Şirket üyesi davet e-postası ✅
 B.12 Mevcut hesabı başka şirkete ayrı rolüyle davet etme ve girişte şirket seçimi ✅
 B.8 Kendi profil fotoğrafı yükleme ✅
 B.9 LDAP / Active Directory / SSO ile giriş ❌
 B.10 Oturum açan cihazların listesi (nereden girildi) ✅ — kullanıcı hesap ayarlarında etkin oturumları tarayıcı/cihaz, IP ve son etkinlik bilgisiyle görür; diğer oturumları CSRF korumalı uçtan kapatır. Kimlik doğrulamalı çerez her istekte etkin oturum kaydına bağlanır; çıkış, parola değişikliği ve parola sıfırlama oturum kayıtlarını iptal eder.
 B.11 Platform yöneticisi müşteri verisine varsayılan olarak erişemez (impersonate hariç) 🟡
C. VARLIKLAR (Demirbaş) — Snipe-IT'in kalbi
C1. Kayıt ve liste
 C1.1 Varlık ekleme / düzenleme / silme (soft delete + çöp kutusu + geri alma) ✅
 C1.2 Etiket (asset tag) otomatik üretim + elle girme ✅
 C1.3 Seri numarası, model, kategori, marka, konum, durum ✅
 C1.4 Alış tarihi + alış maliyeti + para birimi ✅
 C1.5 Garanti bitiş tarihi + garanti durumu filtresi ✅
 C1.6 Arama, durum/kategori/marka/model/çalışan/konum filtreleri, sıralama, sayfalama ✅
 C1.7 Sütun seçimi + sayfa başına satır (12/25/50/100) ✅
 C1.8 Kayıtlı aramalar (en fazla 25, cihazlar arası eşitlenir) ✅
 C1.9 CSV dışa aktarma (tam/sayfalı/filtreli/seçili) ✅
 C1.10 Yazdırılabilir liste / tam ekran görünüm 🟡 (19 Ekim 22:13 TRT: hosted Chromium tam paketi exit 0; 205 filtreli satırın iki API sayfasından tamamı, seçili 18 veri sütunu, yazdır çağrısı ve tam ekran giriş/çıkış/aria durumu geçti. Gerçek PDF kaydetme ve çıktı görsel kontrolü açık.)
 C1.11 Excel (.xlsx) dışa aktarma ✅ — 19 Ekim: varlık listesi, envanter özeti ve özel raporda gerçek OOXML çıktısı/düğmeleri eklendi; Release derleme başarılı. Kimlikli indirme ve Excel içinde açma kabulü henüz yapılmadı.
 C1.12 Toplu işlem: en fazla 100 kayıtta durum değiştirme / zimmet / iade / transfer / denetim ✅
 C1.13 Satır sonu hızlı eylem ikonları (detay, zimmet, iade, denetim, etiket, kopyala, düzenle, arşiv) ✅
 C1.14 BYOD (kendi cihazını getir) ayrı alan ✅
 C1.15 İstenebilir (requestable) varlık işaretleme ✅
 C1.16 Özel alanlar: metin, sayı, tarih, onay kutusu, seçenek listesi ✅
 C1.17 Özel alanlarda şifreli/hassas alan türü ✅ — ASP.NET Data Protection ile alan ve şirket kapsamlı şifreleme; yalnız şirket sahibi/yöneticisi okuyabilir ve değiştirebilir. Liste/ayrıntı, CSV/XLSX aktarım, toplu işlem, filtre ve özel rapor sınırları doğrulanır; hassas değerler dışa aktarımlara yazılmaz. Anahtar volume yedek kapsamındadır.
 C1.18 Model/kategori bazlı alan grupları ✅ — özel alan tanımı `groupName`, `categoryId` ve `modelId` saklar; varlık formu yalnız eşleşen alanları gruplar halinde gösterir. Model seçimi kategori kapsamına göre daralır. `Asset_custom_field_model_scope_is_company_and_category_validated_and_persisted` izole PostgreSQL kabul testi **1/1 geçti**: geçerli alan oluşturma/listeleme, kategori-model uyuşmazlığının ve yabancı kiracı modelinin reddi, DB kaydının doğrulanması. `Verify-CatalogAndCustomFieldDialogs.mjs` güncel UI kaynak kontrolü geçti; hosted Chromium `asset-detail-actions` akışında model kapsamlı alan grubu/form davranışı da kapsanıyor.
 C1.19 Varlık durumu etiketlerinin adını şirket özelleştirebilsin ✅
 C1.20 Yeni durum türleri ve durum geçiş kuralları ✅ — şirket ayarlarında 8'e kadar özel renkli durum tanımı ve geçiş matrisi; API geçişi sunucu tarafında uygular, kullanılan özel durumun silinmesini ve geçişsiz durumların kullanılabilir duruma dönüşsüz kalmasını engeller. `Company_custom_asset_statuses_enforce_transitions_tenant_scope_and_in_use_protection` izole PostgreSQL kabul testi **1/1 geçti**; `Verify-StatusLabelsUi.mjs` geçti. 18 Ekim 2026'da kaynak ve test kanıtı yeniden kontrol edildi.
 C1.21 Kapsamlı toplu yönetim (çoklu alan güncelleme) ✅ — en fazla 100 varlıkta temel katalog, varsayılan konum, alış/garanti, maliyet/para birimi, talep, BYOD, seri no, kalıcı varlık notu ve seçilen özel alanlar atomik güncellenir. Seçilmeyen alanlar korunur; özel alan tipi, şirket kapsamı ve zorunluluk denetlenir; isteğe bağlı boş değer temizlenebilir. Sürüm, tenant, rol, bütünlük ve denetim kabul testleri `deploy/testing/run-security-acceptance.ps1` içindedir.
C2. Varlık detay
 C2.1 Genel bakış + kategori/marka/model/çalışan/konum bağlantıları ✅
 C2.2 Üst/alt varlık hiyerarşisi (döngü engelli) ✅
 C2.3 Açık aksesuar teslimleri + takılı bileşenler ✅
 C2.4 Yazılım lisansı koltuğu atama/geri alma ✅
 C2.5 Gerçek hareket geçmişi + not ekleme (2.000 karakter) ✅
 C2.6 Fotoğraf/PDF ek yükleme (30 dosya, 10 MiB) ✅
 C2.7 En yeni görsel listede küçük önizleme ✅
 C2.8 Bakım sekmesi ✅
 C2.9 Dosya/geçmiş zaman çizelgesi görünümü ✅ — varlık hareketleri ve dosyalar tek kronolojik akışta; dosya adı, yükleyen, bağlı hareket ve indirme eylemi var. Beş sekmeli detay kabul kaydı: PROJECT-STATUS.md 18 Ekim 09:00; kapsam tanımı: ROADMAP.md varlık detayı.
 C2.10 Varlık yaşı göstergesi (alış tarihinden hesaplanır) ✅
 C2.11 Amortisman / mevcut değer gösterimi (liste ve varlık ayrıntısında model politikasından hesaplanır; veri eksikse hesaplanamaz) ✅
C3. Sipariş ve satın alma (varlık)
 C3.1 Çok kalemli varlık satın alma siparişi ✅
 C3.2 Tedarikçi, tarih, para birimi, vergi, iskonto, nakliye ✅
 C3.3 Kısmi teslim alma → otomatik varlık kartı/etiketi oluşturma ✅
 C3.4 Sipariş iptali + denetim izi ✅
 C3.5 Sipariş belgeleri (PDF/PNG/JPEG/WebP, 30 dosya) ✅
 C3.6 Sipariş ↔ cihaz profili iki yönlü geçiş ✅
 C3.7 Onay ve bütçe iş akışı ✅ — varlık, aksesuar, sarf ve bileşen siparişleri ortak yıllık taahhüt/bütçe hesabına bağlı; gerekçeli yönetici onayı olmadan aşım teslimi engelleniyor. İzole PostgreSQL, hosted Chromium ve migration-upgrade kabul kanıtı ROADMAP.md 17 Ekim 04:09 kaydında.
C4. Durum kuyrukları
 C4.1 Kullanılabilir / Atanmış / Beklemede / Bakımda / Kullanılamaz / Arşiv ✅
 C4.2 Arşivden kullanılabilire geri alma (kilitli + denetim izli) ✅
 C4.3 Beklemede ve kullanılamaz kuyrukları ✅
 C4.4 Denetimi gelen/geçen ayrı kuyruklar ✅ — 20 Ekim: gecikmiş/yaklaşan/plansız ve tamamlanan denetimler; dönem, arama, sayfalama, sonuç/bulgu ve varlık ayrıntısı kabul edildi. Tamamlanan listede gerçek denetim tarihi başlığı ve aynı tarih/varlık için olay kimliğiyle kararlı sıra var. Kuyruktan hızlı kayıt formu yalnız başarılı varlık yüklemesinden sonra açılır; hata olursa önceki varlık için form açılmaz. Salt okunur kullanıcıya yazma eylemi sunulmaz. Kanıt: hosted Chromium kuyruk ve varlık ayrıntısı modülleri exit 0 (52 yan menü rotası; planlı kuyruklar, 52 tamamlanmış kayıtla sayfalama, arama/yenileme, bulgu XSS koruması, ayrıntı açma, hızlı kayıt iptali/yükleme hatası, viewer); mevcut API kabulü 3/3 ve `Completed_audit_queue_preserves_history_filters_tenants_dates_and_pages_stably` 1/1. Testler izole/sentetiktir; bu madde genel web V1 veya canlı kabulünün tamamlandığı anlamına gelmez.
 C4.5 İadesi gelen/geciken ayrı kuyruklar ✅ — geciken, 30 gün içinde gelecek, tarihsiz ve ileri tarihli filtreler; arama/sayfalama, eski yanıt yarış koruması ve CSRF korumalı iade. İzole PostgreSQL API/tenant kabulü 1/1 ve tam hosted Chromium regresyonu exit 0 (PROJECT-STATUS.md 17 Ekim 05:45; D.18 konum regresyonu 14:27).
D. ÇALIŞANLAR VE ORGANİZASYON
 D.1 Çalışan kaydı oluşturma/düzenleme (giriş hesabı açmadan) ✅
 D.2 Görev unvanı, telefon, konum, işe başlama tarihi ✅
 D.3 Yönetici / doğrudan raporlayan ilişkisi (döngü engelli) ✅
 D.4 Özel İK notları (sadece yetkili roller) ✅
 D.5 Etkin/pasif filtresi + işe başlama tarih aralığı ✅
 D.6 Profil fotoğrafı (2 MiB) ✅
 D.7 Çalışan belgeleri (30 dosya, 10 MiB, rol kontrollü) ✅
 D.8 Çalışan profilinde zimmetli varlık/aksesuar/lisans/sarf listesi ✅
 D.9 Sorumlu olduğu bakım kayıtları ✅
 D.10 CSV/Excel içe aktarma (şablon + önizleme + atomik ekleme) ✅
 D.11 CSV/Excel dışa aktarma (yeniden içe alınabilir alanlar) ✅ — 19 Ekim: gerçek XLSX, beş dil başlığı ve aynı çalışan filtreleri eklendi; Release geçti. Yeni Excel indirme/açma kabulü açık.
 D.12 Departman kataloğu (ana/alt, kat, tam yol) ✅
 D.13 Departman pasife alma + çalışanları kademeli taşıma ✅ — 18 Ekim 2026 hedefli izole PostgreSQL tekrar testi **1/1 geçti**; test 205 çalışanı 100+100+5 partilerinde taşıma, kalan sayısı, rol/tenant/CSRF sınırları, alt departman arşiv sırası, arşivli departmana yeni çalışan atamasının reddi ve parti audit kayıtlarını doğruladı. Önceki gerçek ürün HTML Chromium akışı iptal, arşiv, taşıma, sayaç ve geri yüklemeyi; tam hosted Chromium regresyonu ve 80/80 kaynak doğrulayıcısı geçti. Ayrıntı: `docs/PROJECT-STATUS.md` 17 Ekim D.13 ve 18 Ekim hedefli tekrar kayıtları.
 D.14 Departman yetkilisi/ekip yöneticisi atama ✅ — 18 Ekim 2026 hedefli izole PostgreSQL tekrar testi **1/1 geçti**; aynı şirkette etkin çalışan atama/kaldırma, viewer 403, yabancı tenant 404, pasif çalışan 400, arşivli departman 409, tenant FK ve audit izini doğruladı. Önceki gerçek ürün HTML Chromium atama/kaldırma ve CSRF akışı; tam hosted Chromium regresyonu, 80/80 kaynak doğrulayıcısı ve beş dil doğrulaması geçti. Ayrıntı: `docs/PROJECT-STATUS.md` 17 Ekim D.14 ve 18 Ekim hedefli tekrar kayıtları.
 D.15 Şirket üyelikleri ekranı (hesap ↔ çalışan bağı) ✅ — Gerçek ürün Chromium bağlama/kaldırma/CSRF/arama sırası, izole PostgreSQL tenant+benzersizlik+talep bağlama kabulü 1/1.
 D.16 Konum kataloğu: bina → kat → oda → kabin (6 seviye) ✅ — PostgreSQL API 1/1; Chromium altı seviye/tam yol önizleme/CSRF/refresh/yedinci seviye parent yasağı; full suite exit 0.
 D.17 Konum varsayılanı + konum sorumlusu ✅ — PostgreSQL tenant/rol/aktiflik/audit 1/1; Chromium varsayılan değiştirme ve tekillik, sorumlu atama/kaldırma, CSRF ve liste yenileme geçti.
 D.18 Konum bazlı erişim yetkileri 🟡 — API/model, üye yönetim UI ve beş dil; varlık/çalışan/aksesuar/sarf/bileşen listeleri ve CSV, kimlikli detay/işlem uçları, konum kataloğu ve stok taleplerinin çalışan/katalog/liste/CSV/oluşturma/karar/iptal/kit iade uçları kapsamlandı. Dashboard özeti/grafiği; varlık, denetim, garanti, bakım, amortisman, bekleyen teslimat ve yazılım lisansı raporlarının sorgu/CSV kapsamı; lisans envanteri/detayı/atama listesi/atama, stok hareketleri ve aksesuar/stok/varlık satın alma siparişlerinin liste/CSV/detay/ek/teslim/iade/onay/iptal yolları konum kapsamına bağlandı. Karma sipariş bütünüyle gizlenir; ortak şirket bütçesi kısıtlı üyeye gösterilmez. Varlık siparişinde satır hedef konumu aynı şirkete ait etkin katalog kaydına bağlanır; kısıtlı üye yalnız erişebildiği hedef konumlara sipariş oluşturabilir, siparişi görebilir/teslim alabilir/iptal edebilir ve teslimde varlık konumu otomatik atanır. Konumu olmayan eski varlık siparişleri kısıtlı üyeye gizlenir; ortak bütçe/onay işlemleri şirket kapsamındadır ve kısıtlı üyeye kapalıdır. Lokasyona aksesuar atama hareketi etkin katalog tam yoluyla denetlenir. Kişisel yazılım lisansı/EULA uçları bağlı çalışanın konumunu denetler; küresel lisans kataloğu yazma/import/dosya yönetimi kısıtlı üyeye kapalıdır. Takvim/operasyon bildirimi “okundu/gizle/geri al” yazımı da aynı kayıt için etkin konum yetkisini şart koşar; D.18 izole PostgreSQL testi gizli konum bildirimi için 404 ve durum satırı oluşmamasını, izinli kayıt için başarılı yazımı doğruladı. Kişisel hesap dışa aktarımı da üyeliğin etkin konum kümesiyle sınırlandı; kapsam dışı çalışan/varlık ve stok ürünü talepleri, “ben yükledim/olayda ben vardım” bağıyla dışa sızmıyor; izinli bağlı ilişkiler korunuyor. D.18 varlık/konum kabulü 1/1 (sipariş hedef konumu/teslim; varlık create/update/transfer, seçili etiket/toplu işlemler, asset/employee CSV-XLSX içe aktarma kapsam reddi ve yan etkisizlik; kısıtlı admin tam şirket JSON/ZIP yedeği ve geri yükleme uçlarında 403); lisans regresyonu 7/7; stok hareketi kabulü 1/1; önceki satın alma kapsamı kabulü 1/1; önceki Chromium üyelik UI kanıtı ve 80/80 verifier sonucu geçerlidir. Kalan ilişki/indirme uçlarının kapsam envanteri ve kabul kanıtı açık; D.18 tamamlanmadı.
 D.18 ek kanıt — 17 Ekim 2026 13:44 TRT: birleşik satın alma siparişleri API'si ve CSV dışa aktarımı aynı etkin konum scope'una alındı; liste sayımı ve maliyet toplamları yalnız izinli siparişleri kapsar. İzinli varlık/aksesuar/stok siparişleri görünür; karışık stok, dış aksesuar ve konumsuz eski varlık siparişleri gizli. İzole PostgreSQL `Location_access_scope_is_tenant_scoped_inherited_and_enforced_for_assets` 1/1 geçti. Kalan D.18 rapor/ilişki/indirme uçları açık; bu alt iş D.18'i veya V1'i tamamlamaz.
 D.18 ek kanıt — 17 Ekim 2026 13:52 TRT: sözleşme liste/CSV/lookup/detail, link doğrulama, arşivleme/geri yükleme ve tüm ek dosya uçları varlık/lisans konum erişimine bağlandı. Sadece tüm bağlı hedefleri görünür olan ve en az bir konumlu hedefle ilişkili sözleşme açılır; dış/karışık/bağlantısız sözleşmeler gizli. D.18 izinli/dış/karışık sözleşme ve ek erişim testi 1/1; mevcut sözleşme güvenli ek kabulü 1/1 geçti. Bakım kayıtları ve başka modüllerdeki kalan ilişkili indirme uçları açık.
 D.18 ek kanıt — 18 Ekim 2026 05:32 TRT: `/api/session/data-export` üyelik başına etkin konum izin kümesine göre çalışan, varlık ve stok kayıtlarını filtreliyor; dış konumdaki üyelik çalışan bağını exporttan düşürüyor. “Kullanıcı bu dosyayı yükledi” veya “bu olayda aktördü” koşulu konum erişimini aşamıyor; kapsam dışı varlık ekleri ve olay geçmişi dışa aktarılmıyor. İzinli bağlı ilişkilerin korunması ve hassas alanların hariç tutulması aynı testte doğrulandı. İlk iki koşuda çevirilemeyen EF sorgusu ve geçersiz test varlık durumu bulundu; düzeltilmiş izole PostgreSQL kabul testi `Personal_data_export_requires_authentication_and_returns_only_the_callers_linked_non_secret_data` **1/1 geçti**. Canlı ve yerel ürün DB'si değişmedi; yerel web imajı bu turda yeniden dağıtılmadı. D.18 hâlâ kısmi; kalan ilişkili rapor/indirme uçları ayrıca incelenecek.
 D.18 ek kanıt — 18 Ekim 2026 05:37 TRT: aynı hesap dışa aktarımındaki stok talebi için hem bağlı çalışanın hem talep edilen ürünün konumu denetleniyor; kit yalnız içerdiği tüm kalemler erişilebilir durumdaysa aktarılıyor. Kapsam dışı sarf ürününe ait talep notu exportta görünmedi, izinli ilişkiler korundu. İzole PostgreSQL kabul testi tekrar **1/1 geçti**. Canlı/yerel ürün DB, web ve deploy değişmedi; D.18/V1 açık.
 D.18 ek kanıt — 18 Ekim 2026 05:43 TRT: kişisel hesap dışa aktarımındaki bakım ve varlık geçmişi, ilişkili çalışan izinli olsa bile bağlı varlık erişilemiyorsa gizleniyor. Dış konum varlığının çalışan bağıyla sorumlu bakım kaydı ve çalışan/aktör bağlantılı olay dışa aktarılmadı; izinli ilişkiler korundu. İzole PostgreSQL testi `Personal_data_export_requires_authentication_and_returns_only_the_callers_linked_non_secret_data` **1/1 geçti**. Yerel ürün DB/web ve canlı değişmedi; D.18/V1 açık.
 D.18 ek kanıt — 18 Ekim 2026 05:52 TRT: çalışan özel İK notu, fotoğrafı ve belge CRUD/indirme uçları etkin konum yetkisi denetliyor. Yetkisiz konumda not okuma/yazma, fotoğraf ve belge okuma/yükleme/silme 404 ile engellendi; DB satırı ve gizli dosyalar korundu. İzole PostgreSQL birleşik D.18 testi **1/1 geçti**. Yerel web imajı yenilendi; sağlık 107/0, ana sayfa/demo 200. Yerel ürün DB ve canlı değişmedi; D.18/V1 açık.
 D.18 doğrulama — 18 Ekim 2026 06:19 TRT: son çalışan-medya/özel-not ve konum yetkisi değişiklikleri dahil tam `EnvanterTR.SecurityAcceptance` paketi izole PostgreSQL’de **187/187 geçti** (20 dk 36 sn). Ürün ana sayfası/demo tekrar HTTP 200, API ready 107/0; test kaynakları tamamen temizlendi. D.18 kapsamı ve genel V1 tamamlanmadı; canlı/yerel ürün DB değiştirilmedi.
 D.18 yerel dağıtım kanıtı — 18 Ekim 2026 05:47 TRT: doğrulanmış hesap dışa aktarım değişiklikleri yalnız yerel `web` imajına alındı. API `ready`, 107 migration/0 bekleyen; ürün ana sayfası ve demo HTTP 200. Yerel ürün DB, public-site verisi ve canlı ortam değişmedi.
 D.19 Şirket birimleri (tüzel kişilik/şube ayrımı) 🟡
E. ZİMMET, İADE, TRANSFER, REZERVASYON
 E.1 Varlık zimmetleme (tekli) ✅
 E.2 Toplu zimmet (100 varlık, atomik) ✅
 E.3 İade alma (tekli + toplu, kısmi iade) ✅
 E.4 Zimmet kuyruğu: arama, sayfalama, geciken/30 gün/tarihsiz/ileri tarih filtreleri ✅
 E.5 Yazdırılabilir zimmet formu (çalışan + üzerindeki varlıklar + tarih) ✅
 E.6 Dijital imza (ekranda imza atma) + PDF çıktı ✅ (18 Ekim: immutable snapshot, iki taraflı bir-defalık PNG imza, SHA-256, arşiv, yazdırma ve gerçek PDF çıktısı Chromium'da uçtan uca doğrulandı; izole PostgreSQL API kabulü 1/1 geçti. Görsel imza nitelikli e-imza veya kimlik doğrulaması değildir.)
 E.7 EULA/kabul belgesi zimmete bağlama + SHA-256 sabitleme ✅ (18 Ekim: etkin lisans atamalarındaki EULA kabulü sürüm, çalışan e-postası, kabul tarihi, dosya/ek kimliği ve EULA SHA-256 ile immutable zimmet snapshotına eklendi; belge snapshot SHA-256 bu kayıtları da sabitliyor. İzole PostgreSQL API kabulü 1/1 ve Chromium gerçek PDF UI kabulü geçti.)
 E.8 Konum transferi (tekli + toplu 100) ✅
 E.9 Rezervasyon: talep → onay/red → iptal, çakışma engeli ✅
 E.10 Rezervasyon zimmete dönüşünce otomatik tamamlandı ✅
 E.11 Hareket geçmişi: tür/işlem/tarih filtreleri + 10.000 satır CSV ✅
 E.12 Çalışan self-servis talebi (kendi adına) ✅
 E.13 Zamanlı hatırlatma akışı (iade gecikmesi vb.) ✅ — Saatlik worker ve kullanıcı tercihli günlük SMTP özeti; bakım, denetim, lisans, garanti, iade, rezervasyon, zimmet ve varlık ömrü kayıtları alıcının etkin konum erişimine göre filtrelenir. `Asset_return_email_reminders_include_overdue_and_upcoming_items_once_per_day_and_stay_tenant_scoped` izole PostgreSQL kabulü **1/1 geçti**: izinli konum verisi görünür, başka konumdaki iade/bakım/lisans görünmez, tenant dışı veri sızmaz ve aynı gün tekrar gönderilmez. Hosted Chromium tercih/CSRF akışı geçti. Ürün SMTP sağlayıcısına gerçek teslim H.10 altında ayrıca doğrulanmalı.
F. LİSANSLAR VE YAZILIM
 F.1 Lisans kataloğu: yayıncı, sürüm, tür, referans, koltuk sayısı ✅
 F.2 Çalışana koltuk atama/geri alma (tekli) ✅
 F.3 Toplu koltuk atama (100 çalışan, atomik, koltuk sınırı kontrollü) ✅
 F.4 Varlığa koltuk atama ✅
 F.5 Maliyet/para birimi, alış/bitiş tarihi, otomatik yenileme ✅
 F.6 Lisans bitiş raporu (30/90/180/366 gün) + CSV ✅
 F.7 Lisans belgeleri (30 dosya) ✅
 F.8 EULA zorunluluğu + sürüm + SHA-256 + kabul kaydı ✅
 F.9 Ürün anahtarı saklanmıyor (bilinçli karar) ✅ ⚠️ İstersen şifreli saklama eklenebilir
 F.10 Sözleşmeler modülü ✅ — şirket kapsamlı sözleşme listesi/arama/durum filtresi, oluşturma/düzenleme, tedarikçi seçimi, varlık ve yazılım lisansı bağlantıları, ayrıntı, CSV, arşiv/geri alma ve güvenli ek dosyası uçları/arayüzü mevcut. İzole HTTP kabulinde tenant/rol/CSRF, bağlar, CSV, gerçek dosya imzası, dosya bütünlüğü, arşivleme ve denetim izi doğrulanıyor; tam rol bazlı tarayıcı görsel kabul ayrıca sürüyor.
 F.11 Garanti takibi modülü ✅ (rapor olarak)
 F.12 Lisans ↔ tedarikçi/sipariş bağlantısı 🟡 — 19 Ekim: lisans tedarikçi kataloğu bağıntısı ayrıntı ekranından açılıyor. Lisans satın alımlarına tenant kapsamlı `SoftwareLicensePurchaseOrders` kayıtları, tedarikçi ve lisans FK'leri, düzenleme/arşivleme/geri yükleme uçları ve ayrıntı tablosu eklendi; eski sipariş metinleri migration sırasında yeni kayıtlara aktarılıyor, CSV/XLSX içe aktarımlarındaki referanslar da ilişkili kayda çevriliyor. Yerel migration 115/0 ve Release dağıtımı doğrulandı. Gerçek kullanıcı oturumuyla form akışı kabul edilmedi; F.12 bu nedenle kısmi.
 F.13 Yazılım envanteri (kurulu uygulamaları cihazdan toplama) 🟡 — 19 Ekim: tenant/varlık kapsamlı kurulu yazılım tablosu; `assets:write` API anahtarıyla tam tarama eşitleme, kaldırılan uygulamalar için geçmiş kaydı ve varlık ayrıntısında liste eklendi. Windows ajanı `tools/Collect-InstalledSoftware.ps1` kayıt defteri taraması yapar; `tools/Install-InstalledSoftwareTask.ps1` aynı Windows kullanıcısına DPAPI ile şifreli anahtarla günlük ve oturum açılışında zamanlanmış görev kurar, log bırakır. JSON yedek aktarımı hatalı/çakışan kayıtları reddeder. Yerel migration 116/0, son Release derlemesi ve yerel web dağıtımı doğrulandı; iki betiğin PowerShell parse kontrolü geçti. Kullanıcının gerçek Windows cihazında ajan/görev çalıştırma kabulü yapılmadı; bu yüzden F.13 kısmi.
G. AKSESUAR, SARF, BİLEŞEN, KİT
 G.1 Aksesuar kataloğu + model + marka + kategori + ürün fotoğrafı ✅
 G.2 Aksesuar stok girişi/çıkışı, düşük stok uyarısı ✅
 G.3 Aksesuar teslimi: çalışan/varlık/konum, miktarlı ✅
 G.4 Toplu teslim/iade (100 kayıt, değişken adet) ✅
 G.5 Aksesuar CSV/Excel içe/dışa aktarma + şablon ✅ — 19 Ekim: XLSX çıkışı eklendi; Release geçti, indirme/açma kabulü açık.
 G.6 Aksesuar siparişleri (kısmi teslim, iptal, finans alanları, belgeler) ✅
 G.7 Hazır aksesuar kitleri (stoktan üretim, toplu teslim, geçmiş) ✅
 G.8 Karma kitler (aksesuar + sarf + tekil varlık + bileşen birlikte) ✅ — oluşturma/düzenleme, stok özeti, talep/tek işlemde teslim ve karma iade kanıtlandı; API PostgreSQL 1/1 ve hosted Chromium tam paket exit 0 (PROJECT-STATUS.md 16 Ekim 21:16).
 G.9 Satın alma siparişinden kit reçetesi/varlık kalıbı oluşturma ✅ — sipariş kalemlerinden stok değiştirmeden kit reçetesi üretir, isteğe bağlı cihaz ekler; tenant ve yinelenen oluşturma kontrolleri var. İzole PostgreSQL 1/1, hedefli + tam hosted Chromium geçti (ROADMAP.md 16 Ekim 21:57).
 G.10 Sarf malzemesi kataloğu + stok + çalışana tüketim çıkışı ✅
 G.11 Sarf minimum stok uyarısı ✅
 G.12 Sarf CSV/Excel içe/dışa aktarma ✅ — 19 Ekim: XLSX çıkışı eklendi; Release geçti, indirme/açma kabulü açık.
 G.13 Bileşen (RAM, disk vb.) kataloğu + seri takibi ✅
 G.14 Bileşeni varlığa takma/çıkarma (miktarlı) ✅
 G.15 Bileşen toplu sayım ve toplu takma (100) ✅
 G.16 Bileşen CSV/Excel içe/dışa aktarma ✅ — 19 Ekim: XLSX çıkışı eklendi; Release geçti, indirme/açma kabulü açık.
 G.17 Ortak stok hareketleri ekranı (tür/İşlem/tarih filtreli) ✅
 G.18 Stok talepleri: talep → onay/red → otomatik teslim/tüketim ✅
H. BAKIM, DENETİM, TAKVİM, GÖREV
 H.1 Bakım kaydı başlatma/tamamlama ✅
 H.2 Bakım planlama (5 yıla kadar) + başlat/iptal ✅
 H.3 Bakım türü, servis, maliyet, para birimi, sorumlu çalışan ✅
 H.4 Bakım kayıtlarına dosya eki (30 dosya) ✅
 H.5 Fiziksel denetim planlama + denetim kuyruğu (geciken/30/90/180/366 gün) ✅
 H.6 Toplu denetim (100 varlık) + yeniden planlama ✅
 H.7 Denetim sonuç raporu ✅
 H.8 Takvim (liste/gün/hafta/ay, dönem gezinme, CSV) ✅
 H.9 Görevler modülü ✅ şirket kapsamlı liste/arama/filtre/sayfalama, oluşturma-düzenleme, çalışan atama, rol kontrollü durum akışı ve denetim izi mevcut; gerçek tarayıcıda rol bazlı görsel kabul açık (kanıt: `docs/PRODUCT-COVERAGE.md`, `docs/ROADMAP.md`)
 H.10 Otomatik e-posta hatırlatmaları (bakım/denetim/garanti/lisans/iade/rezervasyon) 🟡 — saatlik çalışan, kullanıcı tercihiyle günlük özet ve deduplikasyon kod/test kapsamındadır; gerçek SMTP sağlayıcısında teslimat kabulü açık.
 H.11 Denetim sonucu "kabul edilmeyen öğe" akışı ✅ — 19 Ekim: izole PostgreSQL API kabul testi 1/1 ve hosted Chromium senaryosu geçti. CSRF/rol/şirket kapsamı, bulgu yokken yazım olmaması, uygun değil sonucunun her varlık için olay ve audit kaydına yazılması, sonraki denetim tarihini temizleme ve iki kayıt için başarılı gönderim doğrulandı. Chromium UI sentetik API kullanır; canlıya dağıtım iddiası değildir.
I. BARKOD, QR VE ETİKET
 I.1 14 barkod türü (C128/C39/Code93/Codabar/MSI/EAN5/EAN13/UPC-A/UPC-E) + QR/PDF417/DataMatrix/Aztec ✅
 I.2 Hazır etiket ölçüleri (50×30 … 100×50 mm) + özel ebat ✅
 I.3 X/Y hizası, boşluk, gösterilecek alan seçimi ✅
 I.4 A4/Letter çoklu yerleşim (100 varlığa kadar) ✅
 I.5 Kamera ile okutma (tekli bul/işlem) ✅
 I.6 USB barkod okuyucu desteği ✅
 I.7 Çoklu okutmayla seçim → toplu zimmet/iade ✅
 I.8 Yazıcı/etiket stoğu şablon kataloğu ✅
 I.9 QR hedefi (varlık etiketi veya detay bağlantısı) ✅
J. RAPORLAR
 J.1 Hazır raporlar: durum/kategori/maliyet özeti ✅
 J.2 Garanti raporu ✅
 J.3 Lisans bitiş raporu ✅
 J.4 Bakım maliyet raporu (tür/durum/para birimi) ✅
 J.5 Bileşen stok raporu ✅
 J.6 Tamamlanan denetimler raporu ✅
 J.7 Beklenmeyen/geciken teslimat raporu ✅
 J.8 Amortisman raporu (doğrusal / yarım yıl) ✅
 J.9 Varlık alış kayıtları raporu ✅
 J.10 Özel rapor oluşturucu (15 standart + özel alan sütunları, 50.000 satır CSV) ✅
 J.11 Rapor taslaklarını kaydetme (30 taslak, cihazlar arası eşitleme) ✅
 J.12 Grafik/dashboard raporları (kategori/marka/model/konum/durum) ✅
 J.13 Muhasebe değerlemesi (gerçek amortisman muhasebesi) ❌ ⚠️ Kapsam kararı
 J.14 Zamanlanmış rapor e-postası 🟡 — 19 Ekim: envanter/bakım/lisans/stok özetleri; günlük/haftalık/aylık (ayın 1–28 günleri), saat dilimi, beş dil, doğrulanmış kendi adresine erişim denetimli CSV ekli gönderim, gönderim öncesi güncel veri önizlemesi ve formula-safe CSV indirme eklendi; kullanılamayan saat dilimi takvimi gönderimden önce durdurup devre dışı bırakır. Yerelde 113/0 ile dağıtıldı. Gerçek SMTP teslimatı ve oturumlu arayüz kabulü açık; kayıtlı özel rapor zamanlama yok.
 J.15 PDF rapor çıktısı (CSV dışında) 🟡 — 19 Ekim 22:18 TRT: 9 hazır rapor + özel raporun hosted Chromium önizlemeleri; satır/sütun verisi, özel rapor filtresi, A3+dikey ayarı ve yazdır çağrısı geçti. Her önizlemeden Chromium PDF render API'siyle `%PDF-`, 1 KB üzeri geçerli PDF byte çıktısı alındı; tam hosted suite exit 0. Tarayıcı yazdır penceresinden kullanıcı akışıyla “PDF olarak kaydet” ve kaydedilmiş dosyanın görsel kontrolü açık.
K. VERİ TAŞIMA VE YEDEK
 K.1 Varlık/çalışan/aksesuar/sarf/bileşen/lisans CSV içe aktarma ✅
 K.2 .xlsx içe aktarma (formül hücresi reddi, tarih normalizasyonu) ✅
 K.3 Sütun eşleme profilleri (25 profil/şirket) ✅
 K.4 Lisans süresi bitince veriyi indir, yenilenince geri yükle ✅/🟡 (dışa aktarma var, geri yükleme kısıtlı)
 K.5 Şirket JSON dışa aktarma (salt okunur önizleme ile) ✅
 K.6 Şirket ZIP dışa aktarma (veri + tüm medya) ✅
 K.7 JSON/ZIP geri yükleme (mevcut kayıtlara birleştirerek) ❌ ⚠️ Sadece boş hedefe ekleme var
 K.8 Şifreli tam yedek (AES-256-CBC + HMAC-SHA256 + SHA-256) ✅
 K.9 Günlük otomatik yedek (Windows Görev Zamanlayıcı + systemd) ✅
 K.10 İzole ortamda geri yükleme provası ✅
 K.11 Canlı geri yükleme (volume değiştirmeli, hata halinde geri dönüş) 🟡 prototip
 K.12 Uzak hedefe (SFTP/SMB) yedek kopyası ✅ prototip
 K.13 Yedek tazelik/hash sağlık kontrolü + webhook uyarısı ✅ prototip
 K.14 Yedek saklama süresi temizliği ✅
 K.15 Çöp kutusu (silinen kayıtları kalıcı kurtarma) ❌
L. KULLANICILAR, ROLLER VE YETKİLER
 L.1 Şirket üyesi + rol (owner/admin/operator) ✅
 L.2 Yedi şirket rolü (yönetici, envanter operatörü, denetçi, İK vb.) 🟡
 L.3 İnce taneli modül/işlem izin matrisi (Snipe-IT'teki gibi tek tek yetki) 🟡
 L.4 Rol bazlı ekran/görünürlük kontrolü ✅
 L.5 Her API ucunda yetki + tenant kontrolü 🟡
 L.6 Kullanıcı yönetimi ekranı (yönetici başka hesabı yönetir) 🟡
 L.7 Destek talebi modülü ✅ şirket talep akışı, platform kuyruğu ve kalıcı SMTP bildirim kuyruğu; SMTP/support kutusu yapılandırılmalı
M. ŞİRKET AYARLARI VE GÖRÜNÜM
 M.1 Firma profili (ad, adres, iletişim, vergi bilgisi) 🟡
 M.2 Logo + ana renk ✅
 M.3 Tema seçimi (açık/koyu) + renk paleti ✅ (demoda)
 M.4 Bildirim tercihleri (tür bazında e-posta) ✅
 M.5 Entegrasyonlar (webhook, dış sistem) 🟡 — şirket kapsamlı salt okunur API anahtarı yönetimi var; webhook teslimatı ve harici sistem bağlayıcıları eksik.
 M.6 Güvenlik ayarları (parola politikası, oturum süresi, MFA zorunluluğu) ✅ — 20 Ekim 00:19 TRT: Güvenlik sayfasında şirket kapsamlı parola minimumu (12–64), oturum süresi (1–720 saat) ve zorunlu MFA yönetimi var. Ayar uçları yetki/CSRF/audit denetimli; parola değişimi ve MFA’sız aktif oturum iptali uygulanıyor. İlk giriş TOTP/QR, tek kullanımlık doğrulama ve 10 kurtarma koduyla tamamlanıyor. Bekleyen kurulum bileti hesap kilidi, e-posta onayının kaldırılması veya parola/güvenlik damgası değişiminde reddedilir. Kullanıcı başka şirket panelinden zorunlu MFA’yı kapatamaz. Kanıt: hedefli PostgreSQL güvenlik kabulü 9/9 ve şirketler arası kapatma kabulü 1/1; önceki migration yükseltme testi 1/1 ve hosted Chromium güvenlik/giriş akışı. Yerel 117/0 güncellemesi ve gerçek yönetici API giriş/politika okuma/çıkış kabulü geçti; canlı ortam için aynı iddia yapılmaz.
 M.7 Denetim kayıtları (kim ne zaman ne yaptı, filtrelenebilir + CSV) ✅
 M.8 Yerel ayar: tarih/saat biçimi, para birimi, sayı biçimi ✅
 M.9 Şirket ayarlarını dışa/içe aktarma ✅
N. API VE ENTEGRASYON
 N.1 Sürümlü müşteri API v1 (salt okunur: varlık, aksesuar, çalışan) ✅
 N.2 API anahtarı oluştur/listele/iptal (SHA-256 özeti saklanır) ✅
 N.3 OpenAPI 3.1 tanımı yayında ✅
 N.4 IP başına hız limiti ✅
 N.5 API ile yazma (varlık oluştur/güncelle) 🟡 — Oluşturma/güncelleme, özel alanlar, tenant/konum sınırı, optimistic concurrency ve audit mevcut. 20 Ekim: özel şirket rolleri güncel assets.write izniyle anahtar oluşturabiliyor; izin kaldırılması ve rolün kapatılması mevcut anahtarı da etkiliyor. Yeni özel rol kabulü 1/1, mevcut kapsam/tenant/iptal regresyonu 1/1 geçti. Yerel Release dağıtımı devam ediyor; canlı müşteri API kabulü açık.
 N.6 Webhook aboneliği + teslimat + yineleme ❌
 N.7 MDM entegrasyonları (Intune, Jamf) ❌
 N.8 Active Directory / LDAP senkronizasyonu ❌
 N.9 E-posta (SMTP) yapılandırması ✅
 N.10 Muhasebe/ERP entegrasyonu (Logo, Mikro, Netsis) ❌ ⚠️ Türkiye pazarı için değerli
O. PLATFORM YÖNETİMİ (Süper Admin — sadece sana ait)
 O.1 Platform admin ekranı (firma, plan, abonelik görünümü) 🟡
 O.2 Firma listesi + detay + askıya al + sil 🟡
 O.3 Müşteri paneline girme (impersonate) 🟡
 O.4 Plan/fiyat yönetimi 🟡
 O.5 Kupon oluşturma ❌
 O.6 Abonelik: lisans uzat, manuel fatura, iade, ödeme geçmişi 🟡
 O.7 Demo yönetimi (14→30 gün uzat, kalıcı hesaba çevir) 🟡
 O.8 Ekstra özellik açma (ekstra kullanıcı/disk, premium) 🟡
 O.9 Referans bonusu ❌
 O.10 Tüm müşterilere duyuru gönderme 🟡
 O.11 Loglar: giriş, ödeme, hata, deploy geçmişi 🟡
 O.12 Dashboard: toplam müşteri, aylık gelir, yeni kayıtlar, başarısız ödemeler 🟡
 O.13 Destek talepleri yönetimi ✅ platform kuyruğu, yanıt ve durum yönetimi; gelişmiş SLA/raporlama ayrı kapsam
P. TİCARİ AKIŞ (Satış / Abonelik)
 P.1 Self-servis kayıt (signup) 🟡 (fiyat/teklif sayfası var; self-servis kayıt formu ve API akışı yok)
 P.2 Plan seçimi + ücretsiz deneme (14/30 gün) 🟡
 P.3 Kredi kartı ile ödeme (iyzico) ❌ ⚠️ Başvuru + API anahtarı gerekir
 P.4 Otomatik fatura üretimi ❌ ⚠️ e-Arşiv/e-Fatura entegratörü gerekir
 P.5 Abonelik yenileme / iptal / askıya alma 🟡
 P.6 Ödeme başarısızlığı bildirimi + kurtarma akışı ❌
 P.7 Kullanıcı kotası uygulama (plana göre koltuk sınırı) ❌ (Cloud için sonraki açık gereksinim sınırsız kullanıcıdır; bu kota Cloud'a uygulanmaz)
 P.8 Disk/kayıt kotası + aşım uyarısı 🟡
 P.9 Müşteri alt alan adı (firma.envantertr.com) otomatik açma + SSL 🟡
 P.10 Self-servis kurulum sihirbazı (ilk yönetici hesabı) ✅
 P.11 Fiyatlandırma sayfası + teklif formu ✅
R. KURULUM, GÜNCELLEME, İŞLETİM
 R.1 Windows Server: yayın paketi + servis kurulumu 🟡 prototip
 R.2 Windows Server: PostgreSQL hazırlama betiği 🟡 prototip
 R.3 Windows Server: açık onaylı migration + dosya geri dönüşü 🟡 prototip
 R.4 Windows 11 tek bilgisayar: imzalı kurulum sihirbazı ❌
 R.5 Ubuntu: Production Compose + Caddy HTTPS ✅ prototip
 R.6 Güncelleme öncesi otomatik yedek + doğrulama ✅ prototip
 R.7 Gerçek sunucuda uçtan uca kurulum kabulü ❌ ⚠️ Burada simüle edilebilir, gerçek makine gerekir
 R.8 Müşteri kurulum kitapçığı / adım adım kılavuz ❌
 R.9 Docker'sız Windows kurulum (son kullanıcı Docker görmeyecek) 🟡
 R.10 Sağlık izleme + hata bildirimi (webhook/e-posta) ✅ prototip
 R.11 Üçüncü taraf lisans bildirimleri (THIRD-PARTY-NOTICES) ✅
 R.12 Kod imzalama sertifikası ile imzalı kurulum paketi ❌ ⚠️ Sertifika satın alınmalı
 R.13 Otomatik sürüm güncelleme (uygulama içinden) ❌
S. SNIPE-IT KARŞILAŞTIRMASI (senin "eksik" dediğin yer)
Bu bölümü senin verdiğin eski Snipe-IT ekran görüntüleriyle (56 adet buldum) madde madde karşılaştırıp dolduracağım. Şimdilik bilinen başlıklar:

 S.1 Snipe-IT'te olup EnvanterTR'de hiç olmayan özelliklerin listesi ❌ (çıkarılacak)
 S.2 Snipe-IT'te olup EnvanterTR'de eksik olan özelliklerin listesi 🟡 (çıkarılacak)
 S.3 Snipe-IT'te kötü/dağınık olup EnvanterTR'de daha iyi yapılacaklar ❌ (tasarım kararı)
 S.4 Snipe-IT'ten hiçbir kod/isim/görsel kalmayacak ⚠️ AGPL riski — hukuki görüş gerekli
T. LİSANS VE HUKUK (benim karar veremeyeceğim, senin kararın)
 T.1 Diskteki rebrand edilmiş Snipe-IT kopyaları ne olacak? (silinsin mi, arşiv mi?) ⚠️
 T.2 AGPL yükümlülüğü için hukuki görüş alınacak mı? ⚠️
 T.3 Müşteriye kaynak kodu sunma biçimi (gerekirse) ⚠️
 T.4 Fiyatlandırma, vergi, fatura süreçleri ⚠️
 T.5 KVKK / veri saklama / silme politikası ⚠️
ÖZET SAYIM (kabaca)
Bölüm	Toplam madde	✅ Var	🟡 Kısmi	❌ Yok	⚠️ Karar
0. Ürün hatları	5	0	4	1	1
A. Tasarım/Marka/Dil	29	5	16	6	2
B. Giriş/Güvenlik	11	8	1	2	0
C. Varlıklar	39	27	6	5	0
D. Çalışanlar	19	15	2	2	0
E. Zimmet/İade/Transfer	13	10	1	2	1
F. Lisanslar	13	9	2	2	1
G. Aksesuar/Sarf/Bileşen	18	17	0	1	0
H. Bakım/Denetim/Takvim	11	8	1	2	0
I. Barkod/QR/Etiket	9	9	0	0	0
J. Raporlar	15	11	0	4	1
K. Veri/Yedek	15	9	3	2	1
L. Kullanıcı/Rol	7	2	4	1	0
M. Şirket Ayarları	9	4	4	1	0
N. API/Entegrasyon	10	5	0	5	1
O. Platform Yönetimi	13	0	11	2	0
P. Ticari Akış	11	3	4	4	1
R. Kurulum/İşletim	13	1	8	4	2
S. Snipe-IT Karşılaştırma	4	0	1	3	1
T. Lisans/Hukuk	5	0	0	0	5
Toplam ~280 madde. Kabaca: üçte biri var, üçte biri kısmi, geri kalanı yok veya karar bekliyor.

Bu dosya: EnvanterTR - deepseek\design\ISTENENLER-LISTESI.md Kaynaklar: docs/PRODUCT-REQUIREMENTS.md, docs/PRODUCT-COVERAGE.md, envantertr.com/demo/ menüsü, eski oturum notları
---

# Son konuşma ekleri — 11 Ekim 2026

> Bu bölüm, kullanıcının en son yazılı isteklerinden derlenmiştir. Yukarıdaki 0–T işaretleri kullanıcının sağladığı başlangıç durumudur; her madde ayrıca kod, arayüz ve çalışma davranışı üzerinden doğrulanmalıdır.

## Acil denetim ve görünürlük

- [ ] Menüleri baştan sona denetle: her menü öğesi gerçek ve ilgili bir ekran/işlem açmalı; boş, alakasız veya çalışmayan seçenekler düzeltilmeli ya da açıkça “yakında” olarak işaretlenmeli.
- [ ] Arka plan, sayfa kabuğu, tablo, modal ve düğmelerde tutarsız/uyumsuz durumları düzelt; her düğmenin işlem sonucunu ve hata/boş durumunu kontrol et.
- [ ] Kullanıcının verdiği Snipe-IT ekranlarını özellik ve veri sözlüğü olarak kullan; işlevsel karşılığını EnvanterTR içinde oluştur. Snipe-IT kodunu, adını veya görsellerini kopyalama; lisans yükümlülüklerini ayrıca incele.
- [x] Varlık tablosu referanslarında ortak olan başlıkları tek tabloda birleştir, yinelenen sütun açma. İstenen alanlar: Kimlik, Şirket, Demirbaş Adı, Cihaz resmi, Demirbaş Etiketi, Seri No, Model, Model No., Kategori, Durum, Çıkış Yapılmış Olan Kişi, Çalışan Numarası, Konum, Varsayılan Konum, Üretici, Satın Alma Maliyeti, Mevcut Değer, Giriş/Çıkış ve Eylemler.
- [x] Sütunların kaynak verisini doğru bağla: çalışan no atanmış çalışandan; model no model kataloğundan; güncel konum varlık kaydından; varsayılan konum şirket konum kataloğundan; satın alma maliyeti varlık kaydından, mevcut değer model amortisman politikasından gelir. Amortisman bilgisi eksikse mevcut değer sıfır varsayılmaz.
- [ ] Eylemler (görüntüle, zimmet/iade, denetim, etiket yazdırma, kopyala, düzenle, arşiv) gerçek işlem yapsın; sayaçlar ve hareket geçmişi güncel kayıtlarla tutarlı olsun.
- [ ] CSV/Excel içe/dışa aktarma alan eşlemesi, önizleme, bilinmeyen sütunlar ve hata mesajlarını Snipe-IT örnekleriyle dikkatlice karşılaştır; yanlış kişi/şirket eşlemesini sessizce boş bırakma.
- [ ] GitHub Actions/CI başarısızlık bildirimlerini kök nedenine göre incele; tekrarlayan gereksiz e-postaları azalt, fakat gerçek dağıtım/güvenlik hatalarını gizleme.

## Kurulum ve müşteri yayını

- [ ] Windows 11 tek bilgisayar kurulumunu mümkün olduğunca tek, imzalı ve kolay kurulabilir pakete dönüştür; müşterinin Docker veya karmaşık servis kurulumu yapmasını gerektirme.
- [ ] Windows Server x64 için ekip kullanımına yönelik servis kurulum, PostgreSQL, yedek, güncelleme ve geri dönüş adımlarını çalışır paket ve anlaşılır kılavuzla tamamla.
- [ ] Ubuntu Linux için Docker Compose + Caddy HTTPS akışını müşteri tarafından baştan sona uygulanabilir hale getir; kurulum, alan adı, güvenlik, güncelleme, yedek ve geri yüklemeyi kılavuzla.
- [ ] Sunucuda kurulan örneğe aynı yerel Wi‑Fi/ağdaki cihazlardan tarayıcıyla erişimi açıklayıp doğrula. Ekip cihazlarına ayrıca istemci uygulaması kurulması gerekmemeli; mobil uygulama kapsamını ayrıca karara bağla.
- [ ] İndirme bağlantısı ve “kuruluma hazır” ifadesini yalnızca gerçek, sürümlenmiş, checksum'ı doğrulanmış ve desteklenen paket hazırsa yayımla.
- [ ] envantertr.com demo/yayın güncellemesini yerel Docker demosundan ayrı doğrula. GitHub'a push, tek başına canlıya yayın değildir; Coolify dağıtımını ve canlı sonucu kontrol et.
- [ ] Üretim veritabanı için yedek doğrulanmadan migration uygulama. Canlı ve yerel migration sayısı, yedek/geri dönüş, sağlık uç noktası ve yayın sürümü eşleşmeden uygulamayı “yayında” sayma.

## Docker, GitHub ve devam eden teslimat

- [ ] Docker Desktop Engine ve Compose projesinin durumunu kontrol et; web, public-site ve PostgreSQL servislerinin sağlık/portlarını doğrula.
- [ ] Docker Desktop'ta Compose proje satırını genişletip alt konteynerlerin görünür olduğunu doğrula; beklenen servisler: `web`, `public-site`, `database`.
- [ ] Yerel `localhost:8080` uygulama ve `localhost:8081/demo/` demosu ile canlı envantertr.com durumunu birbirinden ayrı kontrol et.
- [ ] Git çalışma ağacı, commit, `origin/main`, GitHub Actions ve canlı deploy sürümünü birbirinden ayrı raporla. Ağ/DNS veya erişim hatasında doğrulanmamış sonucu başarılı gösterme.
- [ ] Bekleyen kod değişikliklerini izole incele, migration ve tenant güvenliğini doğrula; kabul kontrollerini geçir; sonra uygun küçük commitlerle GitHub'a gönder ve canlı deploy'u ayrıca doğrula.
- [ ] Kullanıcıya çalışma sonunda yapılanlar, test/sağlık sonuçları, GitHub durumu, canlıya yansıma durumu ve açık kalanlar için kısa kontrol tablosu sun.

## Kullanıcının onayıyla kabul ölçütü

- [ ] Kullanıcı bu listede istenen kapsamı gözden geçirip düzeltmeleri işaretleyebilmeli; başlangıçtaki ✅/🟡/❌ göstergeleri bağımsız denetim sonucu gibi sunulmamalı.

D.18 ek kanıt — 17 Ekim 2026 14:04 TRT: Bakım liste/CSV, bakım yaşam döngüsü ve ek dosyalarının dört işlemi; çalışan profilinin kendisi ve ilişkili varlık/aksesuar/lisans/sarf/bakım/rapor/yönetici bilgileri konum kapsamına bağlandı. İzole PostgreSQL güncel D.18 + employee profile koşusu 2/2, bakım yaşam döngüsü/ek regresyonu 4/4, sözleşme kapsamı ve ek regresyonları 1/1 + 1/1 geçti. Profil dış lokasyonda 404, dış yönetici kimliği null döndü. D.18 ve V1 hâlâ açık; kalan modül/rapor/indirme uçları sürüyor.

D.18 ek kanıt — 17 Ekim 2026 14:11 TRT: Görev liste/özet/seçici; yeni atama, düzenleme ve durum değişikliği etkin çalışan konumuna göre filtrelendi. Kısıtlı kullanıcıya atamasız ve dış çalışana bağlı görevler verilmez. İzole PostgreSQL birleşik D.18 + görev regresyonu 2/2; Verify-CompanyTasksUi.mjs ve JS syntax geçti. Kalan D.18 modül ilişkileri/rapor/indirme yolları açık.

D.18 ek kanıt — 17 Ekim 2026 14:16 TRT: Rezervasyon seçenekleri, liste/CSV, oluşturma, karar ve iptal sunucu tarafında etkin konum kapsamına bağlandı. İzole PostgreSQL birleşik D.18 + rezervasyon regresyonu 2/2; Verify-ReservationUi.mjs geçti. Yerel ürün DB'si/canlı değişmedi. D.18 kapsamı ve V1 açık.

D.18 ek kanıt — 17 Ekim 2026 14:27 TRT: Varlık iade kuyruğu hem varlığın hem çalışanın izinli etkin konumda olmasını zorunlu kılıyor. D.18 kapsam + iade kuyruğu regresyonu 2/2; Verify-AssetCheckoutReturnUi, Verify-AssetRowActions, Verify-AssetDetailActions, Verify-ReservationUi ve diff kontrolü geçti. Ürün DB/canlı değişmedi. D.18 ve V1 açık.

D.18 ek kanıt — 17 Ekim 2026 14:34 TRT: Varlık atama/iade, çöp kutusu listesi, taşıma, geri yükleme ve durum güncellemesi etkin konum scope'una alındı. Birleşik D.18 konum testi + varlık çöp kutusu regresyonu 2/2 geçti; Verify-AssetTrashUi.mjs, Verify-AssetCheckoutReturnUi.mjs ve diff kontrolü geçti. Yerel ürün DB'si/canlı/deploy değişmedi. D.18 ve V1 açık; diğer ilişkiler/rapor/indirme uçları sürüyor.

D.18 ek kanıt — 17 Ekim 2026 14:43 TRT: Varlık detayındaki ilişkili varlık listesi konum scope'uyla filtrelendi; denetim takvimi liste/arama/özet sayımları ve çalışan adı da etkin konum kapsamına alındı. İzole PostgreSQL birleşik D.18 testi 1/1; Verify-AuditScheduleUi.mjs, Verify-AssetDetailActions.mjs ve diff kontrolü geçti. D.18 ve V1 açık; kapsam taraması sürüyor.

D.18 ek kanıt — 17 Ekim 2026 14:54 TRT: Takvim bakım/lisans/garanti/denetim/iade/rezervasyon olayları konum scope'una bağlandı; toplamlar da filtrelenmiş sorgulardan hesaplanıyor. İade ve rezervasyon kayıtlarında her iki ilişkili hedef kapsam içinde olmak zorunda. İzole PostgreSQL birleşik D.18 kabul testi 1/1; Verify-AppNavigation.mjs ve diff kontrolü geçti. D.18 ve V1 açık; canlı/ürün DB/deploy değişmedi.

D.18 ek kanıt — 17 Ekim 2026 14:58 TRT: Lokasyon dizininin departman sayım haritası etkin konum scope'una alındı. Dış konumdaki departman adı ve konum kimliği kısıtlı yanıt içinde yok; izole PostgreSQL D.18 kabul testi 1/1, Verify-LocationDirectoryUi.mjs ve diff kontrolü geçti. Canlı/ürün DB/deploy değişmedi; D.18/V1 açık.
D.18 ek kanıt — 17 Ekim 2026 15:03 TRT: `/api/locations` içinde açık, konum hedefli aksesuar zimmet sayısı izinli katalog tam yollarına bağlandı. Kısıtlı kullanıcıya etkin erişimdeki konum sayımı verilir; dış/stale/free-text konum sayısı gizlenir. Birleşik PostgreSQL kabul testi 1/1 ve lokasyon dizini UI doğrulayıcısı geçti. D.18 açık.

D.18 ek kanıt — 17 Ekim 2026 15:35 TRT: Hazır kit liste/arama/CSV, adet özeti, geçmiş ve yazma işlemleri etkin konum kapsamına bağlandı. Tüm kalemleri erişilebilir kit görünür; dış/konumsuz kalemli kit ve dış hedefli geçmiş gizlenir; kapsam dışı kit kalemi oluşturma reddedilir. İzole PostgreSQL D.18 kabul testi 1/1; `Verify-AccessoryKitsUi.mjs`, son web derlemesi (0 hata/uyarı) ve `git diff --check` geçti. Geçici Docker test kaynakları kaldırıldı. D.18/V1 açık; ürün/canlı veritabanı ve yayın değişmedi.

D.18 ek kanıt — 17 Ekim 2026 15:43 TRT: Kısıtlı üye aksesuar CSV/XLSX içe aktarırken her satırda etkin katalogda bulunan, erişilebilir bir konum seçmeli. Dış konum, eski/bozuk serbest metin konum ve konumsuz satır önizleme ile commit aşamalarında reddediliyor. İzole PostgreSQL D.18 kabul testi 1/1; izinli konumlu içe aktarma başarılı, dış/eski konumlu satırlar 422 ve veritabanına yazılmadı. `Verify-StockImportDialogs.mjs` geçti. Geçici test Compose DB/ağı kaldırıldı; yerel ürün DB'si/canlı/deploy değişmedi. Bu D.18 kapsamının bir parçasıdır, üst seviye tamamlanan madde sayısını artırmıyor.

D.18 ek kanıt — 17 Ekim 2026 15:49 TRT: Bileşen ve sarf CSV/XLSX içe aktarmaları da izinli etkin katalog konumuyla sınırlandı. Birleşik PostgreSQL D.18 testi 1/1 geçti: üç modülde erişilebilir satırlar içe aktarıldı; dış konumlu bileşen/sarf önizlemesi ve commit'i reddedildi; kapsam dışı kayıtların yazılmadığı DB'den kontrol edildi. `Verify-StockImportDialogs.mjs` ve `git diff --check` geçti. Test DB/ağı temizlendi. D.18 açık; yerel ürün/canlı DB ve deploy değişmedi.

 D.18 ek kanıt — 18 Ekim 2026: Bakım geçmişi, bakım listesi/CSV araması ve sorumlu çalışan adı etkin konum erişimine bağlandı; kapsam dışı varlıkta bakım planlama/doğrudan başlatma ve konum dışı çalışanla arama engellendi. İzinli varlıkta konum dışı sorumlu adı JSON/CSV’den maskelendi. İzole PostgreSQL D.18 kabul testi 1/1 geçti. Bakım ekleri ve planlı başlat/iptal/tamamlama kapsamı önceki kanıtlarda mevcuttu. D.18 hâlâ kısmi; diğer rapor/ilişki/indirme uçları devam ediyor.
 D.18 ek kanıt — 18 Ekim 2026: Konum üyeliği erişim modu yalnız açık `all` değerinde sınırsız sayılır; `restricted` etkin seçimi ve alt konumlarıyla çalışır, bilinmeyen/bozuk mod sıfır konumla kapalı kalır. Bu fail-closed davranışı izole PostgreSQL D.18 testinde doğrulandı (1/1). Çalışan CSV kabulindeki hata metni beklentisi API’nin mevcut Türkçe mesajına güncellendi ve hedefli test ayrıca 1/1 geçti. Geniş paket düzeltmelerden önce 185/187 idi; son tam koşu yapılmadı. D.18/V1 kısmi.

D.18 ek kanıt — 18 Ekim 2026 06:33 TRT: Bakım eki dosyası erişilebilir olmayan konumda olsa bile kısıtlı kullanıcının indirme ve silme girişimi 404 ile reddediliyor; kabul testi DB eki satırının ve gerçek dosyanın yerinde kaldığını doğruladı. Birleşik izole PostgreSQL D.18 kabul testi 1/1 geçti; bakım eki UI doğrulayıcısı geçti. D.18/V1 kısmi.

D.18 ek kanıt — 18 Ekim 2026 06:35 TRT: Dış konum varlık eki ve aksesuar fotoğrafı için erişim dışı indirme/silme ile yeni dosya yükleme reddedildi; izole PostgreSQL testinde önceki ek/fotoğraf satırları ve gerçek dosyalar değişmeden kaldı. Birleşik D.18 kabul testi tekrar 1/1 geçti. D.18/V1 kısmi.

## Faz J — 18 Ekim 2026 doğrulama kesiti

- İzole PostgreSQL güvenlik/kabul paketi: **187/187 geçti**. Migration, tenant/rol/CSRF, ödeme, MFA, dosya işlemleri ve D.18 dahil; yalnız geçici test kaynakları kullanıldı.
- Tam sabitlenmiş Playwright/Chromium hosted kabul paketi: **exit 0**. Şirket uygulaması ve platform yönetimi sentetik API ile; masaüstü/mobil tablo, formlar, modal/iptal, yetki, CSV/XLSX, raporlar, ödeme kapalı durumu, MFA bootstrap, lokasyon, destek ve 52 menü rotası senaryoları geçti.
- Kaynak UI doğrulayıcıları: **80/80 geçti**. Statik arayüz envanteri `--check`: **2.149 öğe ile güncel**.
- Bunlar yerel kaynak ve sentetik/izole kabul kanıtıdır; canlı müşteri oturumu, gerçek ödeme hesabı veya her gerçek kiracıda üretim kabulü değildir. V1 ve D.18 tamamlanmadı; genel yüzde değiştirilmedi.
