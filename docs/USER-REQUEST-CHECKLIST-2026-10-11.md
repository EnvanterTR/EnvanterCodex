# EnvanterTR — İstenenlerin Tam Listesi (Onay Listesi)

Bu liste **senin kendi dokümanlarından** çıkarıldı. Kaynaklar:
- `docs/PRODUCT-REQUIREMENTS.md` (ürün modülleri, kullanım biçimleri, yetki ilkeleri)
- `docs/PRODUCT-COVERAGE.md` (30 işlev ailesi kapsam matrisi)
- `envantertr.com/demo/` menüsü (10 grup, 48 alt menü — senin onayladığın ürün haritası)
- Eski sohbet notlarındaki tasarım tercihlerin

**Durum işaretleri:** ✅ Var · 🟡 Kısmen var (eksik) · ❌ Hiç yok · ⚠️ Riskli/karar gerekiyor

**Nasıl kullanacaksın:** Her satırın solundaki kutuyu işaretle → "bunu istiyorum" demek.
İstemediğini sil, notunu yanına yaz. Sonunda bana geri ver, ona göre plan çıkarayım.

---

## 0. ÜRÜN HATLARI (5 hedef)

- [ ] **0.1 Bulut SaaS** — `firma.envantertr.com` alt alan adı, çok kiracılı, tarayıcıdan. 🟡
- [ ] **0.2 Kurum içi Windows tek bilgisayar** — indir, çalıştır, ileri-ileri kur (klasik sihirbaz). ❌
- [ ] **0.3 Kurum içi Windows Server** — ofis sunucusunda servis olarak, LAN'dan tarayıcıyla. 🟡
- [ ] **0.4 Kurum içi Linux (Ubuntu)** — Docker Compose + Caddy HTTPS. 🟡
- [ ] **0.5 Mobil uygulama (iOS + Android)** — ⚠️ Şu an kapsam dışı, ayrı proje. Karar senin.

---

## A. TASARIM, MARKA VE DİL

### A1. Şikayet ettiğin konular (öncelikli düzeltme)
- [x] **A1.1** Tarayıcı sekme başlığı "EnvanterTR — Şirket Alanı" ✅ — 15 Ekim 2026: `localhost:8080/` HTTP 200 yanıtının gerçek `<title>` değeri doğrulandı; `Verify-AppNavigation.mjs` regresyon kontrolü eklendi.
- [ ] **A1.2** Uygulama ile `envantertr.com/demo/` **birebir aynı görünmeli** (kabuk, renk, tipografi, boşluk) 🟡
- [ ] **A1.3** Snipe-IT'ten gelen terimler temizlenecek (`Varlıklar`, `Aksesuarlar`, `Sarf malzemeleri` → senin terminolojin) 🟡
- [ ] **A1.4** Snipe-IT'ten tek satır kod/isim/görsel kalmayacak ⚠️ Diskte 5 kopya var, temizlik kararı gerekiyor
- [ ] **A1.5** Kafana göre tasarım yapılmayacak → her görsel karar önce sana gösterilecek ❌ (süreç kuralı)
- [ ] **A1.6** Boş/geliştirilmemiş ekranlar "yakında" diye açıkça söylenecek, dashboard göstermeyecek 🟡

### A2. Tasarım tercihlerin (eski notlarından)
- [ ] **A2.1** Kırmızı ana marka rengi `#DC2626` 🟡
- [ ] **A2.2** Koyu temada vurgu **mavi** `#3B82F6` (altın değil) ✅ demoda var
- [ ] **A2.3** Font **Inter**, serif (Fraunces) kesinlikle yok 🟡
- [x] **A2.4** Inter fontu **kendi sunucumuzdan** yüklenmeli (Google'a bağımlı olmasın) ✅ — 15 Ekim 2026: `/assets/fonts/inter.css` ve `/assets/fonts/InterVariable.woff2` yerel endpoint'leri HTTP 200; doğrulayıcı dış Google Fonts bağımlılığını yasaklıyor.
- [ ] **A2.5** Saten/parlak yüzeyler, pastel renk yok 🟡
- [ ] **A2.6** iPhone gibi **yumuşak tema geçişi** (ani renk atlaması yok) 🟡
- [ ] **A2.7** Abartılı büyük punto yok (72px hero kötüydü) 🟡
- [ ] **A2.8** Açık tema + koyu tema, ikisi de tam çalışacak 🟡
- [ ] **A2.9** Renk paleti seçici (demoda 5 palet var: Mavi/Yeşil/Mor/Kehribar/Turkuaz) ✅ demoda var
- [ ] **A2.10** Şirket logosu + ana rengi değiştirilebilir (whitelabel) ✅
- [ ] **A2.11** Mobil/tablet uyumlu (telefondan da kullanılabilir) 🟡
- [ ] **A2.12** Erişilebilirlik: klavyeyle gezinme, ekran okuyucu etiketleri 🟡

### A3. Dil desteği
- [ ] **A3.1** Türkçe ✅
- [ ] **A3.2** İngilizce 🟡
- [ ] **A3.3** Rusça 🟡
- [ ] **A3.4** Ukraynaca 🟡
- [ ] **A3.5** Makedonca 🟡
- [ ] **A3.6** Sırpça + Almanca (hedef dokümanda geçiyor) ❌
- [ ] **A3.7** Dil seçici navbar'da bayrak + kod ile (TR TR) ✅
- [ ] **A3.8** ⚠️ **Tüm ekranların** çevrilmesi (şu an sadece bir kısmı) 🟡

---

## B. GİRİŞ, HESAP VE GÜVENLİK

- [x] **B.1** Şirket hesabıyla giriş ✅ — 17 Ekim yerel Docker'da güncel 102/0 şemada gerçek oturum açma owner/şirket bağlamıyla başarılı oldu ve çıkış HTTP 204 verdi. Bu kanıt yalnızca localhost içindir; canlı girişin çalıştığını göstermez.
- [ ] **B.2** Birden fazla şirkete üye hesap → parola sonrası şirket seçimi ✅
- [ ] **B.3** Parola sıfırlama e-postası (tek kullanımlık token) ✅
- [ ] **B.4** İki aşamalı doğrulama (Authenticator/TOTP) + kurtarma kodları ✅
- [ ] **B.5** Aktif oturumları uzaktan kapatma (güvenlik damgası) ✅
- [ ] **B.6** Giriş denemesi hız sınırı + hesap kilitleme ✅
- [ ] **B.7** Şirket üyesi davet e-postası ✅
- [ ] **B.8** Kendi profil fotoğrafı yükleme ✅
- [ ] **B.9** LDAP / Active Directory / SSO ile giriş ❌
- [x] **B.10** Oturum açan cihazların listesi (nereden girildi) ✅ — 15 Ekim hosted Chromium kabulinde sentetik mevcut/uzak oturum listesi, cihaz tarayıcısı/IP bilgisi, iptalden vazgeçince yazma olmaması, onaylı CSRF korumalı uzak oturum kapatma ve yenileme sonrası kalan listenin doğruluğu test edildi. Gerçek kullanıcı oturumuna yazılmadı.
- [ ] **B.12** Mevcut hesabı ikinci şirket alanına ayrı rolüyle davet etme; şirket seçimi ve şirketler arası veri ayrımını doğrulama ✅
- [ ] **B.11** Platform yöneticisi müşteri verisine **varsayılan olarak erişemez** (impersonate hariç) 🟡

---

## C. VARLIKLAR (Demirbaş) — Snipe-IT'in kalbi

### C1. Kayıt ve liste
- [ ] **C1.1** Varlık ekleme / düzenleme / silme (soft delete + çöp kutusu + geri alma) ✅
- [ ] **C1.2** Etiket (asset tag) otomatik üretim + elle girme ✅
- [ ] **C1.3** Seri numarası, model, kategori, marka, konum, durum ✅
- [ ] **C1.4** Alış tarihi + alış maliyeti + para birimi ✅
- [ ] **C1.5** Garanti bitiş tarihi + garanti durumu filtresi ✅
- [ ] **C1.6** Arama, durum/kategori/marka/model/çalışan/konum filtreleri, sıralama, sayfalama ✅
- [ ] **C1.7** Sütun seçimi + sayfa başına satır (12/25/50/100) ✅
- [ ] **C1.8** Kayıtlı aramalar (en fazla 25, cihazlar arası eşitlenir) ✅
- [ ] **C1.9** CSV dışa aktarma (tam/sayfalı/filtreli/seçili) ✅
- [x] **C1.10** Yazdırılabilir liste / tam ekran görünüm ✅ — 16 Ekim 2026: gerçek Chromium kabulinde 205 filtreli kayıtla 200’lük sayfa sınırı aşıldı; ilk/son kayıt, eksiksiz toplam, seçili sütunlar, baskı penceresi/yazdırma çağrısı ve tam ekran aç-kapa/ARIA durumu doğrulandı; tam hosted regresyon geçti.
- [ ] **C1.11** Excel (.xlsx) dışa aktarma ✅
- [ ] **C1.12** Toplu işlem: en fazla 100 kayıtta durum değiştirme / zimmet / iade / transfer / denetim ✅
- [ ] **C1.13** Satır sonu hızlı eylem ikonları (detay, zimmet, iade, denetim, etiket, kopyala, düzenle, arşiv) ✅
- [ ] **C1.14** BYOD (kendi cihazını getir) ayrı alan ✅
- [ ] **C1.15** İstenebilir (requestable) varlık işaretleme ✅
- [ ] **C1.16** Özel alanlar: metin, sayı, tarih, onay kutusu, seçenek listesi ✅
- [x] **C1.17** Özel alanlarda şifreli/hassas alan türü ✅ — ASP.NET Data Protection ile şirket+alan bağlamında şifreleme; yalnız owner/admin okur ve yazar. Varlık oluşturma/düzenleme/toplu güncelleme, CSV/XLSX içe aktarma ve API akışları korumayı uygular; dışa aktarma, filtre ve özel raporlar değeri dışarı vermez. Kanıt: `docs/ROADMAP.md` 12 Ekim kabul kaydı; `tests/EnvanterTR.SecurityAcceptance/TenantSessionAcceptanceTests.cs` hassas alan/API rol testleri; `deploy/testing/hosted-checkout-browser/asset-detail-actions.mjs` hassas alanın formda ve klon akışında doğrulanması.
- [x] **C1.18** Model/kategori bazlı alan grupları ✅ — Alan tanımlarında grup adı ve isteğe bağlı kategori/model kapsamı; tenant bileşik FK'leri; varlık oluşturma/düzenleme/kopyalama/detayında kapsam filtresi; CSV önizleme/içe aktarmada satır bazlı doğrulama; beş dil. Kanıt: `20261016205446_AddAssetCustomFieldScopes`, `Asset_custom_field_groups_and_category_model_scopes_are_applied_safely`, `MigrationUpgradeAcceptanceTests.Production_baseline_upgrades_to_current_schema_without_losing_company_assets` ve Chromium klon akışında eşleşen model grubunun görünmesi. Migration yerel ürün DB'sine veya canlıya uygulanmadı.
- [x] **C1.19** Varlık durumu etiketlerinin adını şirket özelleştirebilsin ✅ — Sabit altı durum adı şirket bazında kaydedilir ve tüm varlık seçicilerine yayılır. Kanıt: `Asset_status_display_labels_are_company_scoped_audited_and_manager_only` (izole PostgreSQL; yetki, tenant ayrımı, CSRF ve denetim izi) ve hosted Chromium durum etiketi akışı (kaydetme, CSRF ve liste/toplu işlem/rapor/detay seçicilerinin yenilenmesi). Canlı/ürün veritabanına yazılmadı.
- [x] **C1.20** Yeni durum türleri ve durum geçiş kuralları ✅ — En fazla 8 şirket özel durumu; ad/renk doğrulaması, izin matrisi, her özel durumdan Kullanılabilir'e erişilebilir yol şartı ve kullanımda olan durumu silme koruması. API tenant/rol/CSRF sınırları, geçiş ve arşiv/zimmet iş akışlarının ayrımı izole PostgreSQL kabulinde doğrulandı; `Company_custom_asset_statuses_enforce_transitions_tenant_scope_and_in_use_protection`. Hosted Chromium akışı özel durum ekleme/renk/kaydetme, geçiş matrisi, korumalı arşiv sınırı ve liste/toplu işlem/rapor/detay seçicilerinin yenilenmesini doğruladı (`asset-detail-actions.mjs`, exit 0). `Production_baseline_upgrades_to_current_schema_without_losing_company_assets` migration zincirini temiz izole şemada uygular. Kaynak/dil doğrulayıcıları geçti. Yerel ürün DB'si ve canlı ortama migration/deploy yapılmadı.
- [x] **C1.21** Kapsamlı toplu yönetim (çoklu alan güncelleme) ✅ — 16 Ekim 2026: 100 varlık sınırı, seçili alan-only atomik API, tenant/rol/version/audit ve özel alan doğrulaması; izole PostgreSQL kabul testi geçti. Gerçek Chromium’da iki varlık seçme, iptalin yazmaması, yalnız iki işaretli alanı CSRF ile kaydetme, seçilmemiş sigorta bilgisini koruma ve listenin yenilenmesi geçti.

### C2. Varlık detay
- [x] **C2.1** Genel bakış + kategori/marka/model/çalışan/konum bağlantıları ✅ — Hosted Chromium `asset-detail-actions.mjs` akışında beş varlık detayı bağlantısına ayrı ayrı tıklanıp `/api/assets` isteğindeki doğru `categoryId`, `brandId`, `modelId`, `employeeId` ve `location` filtresi doğrulandı; her filtre temizlenince 200 yanıtıyla tam listeye dönüş de kontrol edildi. Aynı modülün devamındaki varlık detay regresyonları da exit 0 ile geçti. Yerel ürün DB'sine/canlı ortama yazma yapılmadı.
- [x] **C2.2** Üst/alt varlık hiyerarşisi (döngü engelli) ✅ — `Asset_hierarchy_is_tenant_scoped_and_rejects_cycles` temiz, tek kullanımlık PostgreSQL 17 test ortamında **1/1 geçti**: başka tenant varlığına bağlama reddedildi, aynı tenant üst varlık bağlantısı kaydedildi, döngü oluşturma reddedildi ve üst detayındaki alt ilişki doğrulandı. Hosted Chromium `asset-detail-actions.mjs` akışı arama/seçme, CSRF + `expectedUpdatedAt` ile kayıt, üst ve alt bağlantıların yenilenmesi, döngü hatasının kullanıcıya gösterilmesi ve önceki geçerli ilişkinin korunmasını doğruladı; tam modül exit 0. Test konteyneri/ağı temizlendi; ürün DB'si ve canlı ortam değiştirilmedi.
- [x] **C2.3** Açık aksesuar teslimleri + takılı bileşenler ✅ — `Asset_detail_shows_open_accessory_and_component_relations_only_for_its_company` izole PostgreSQL testinde **1/1 geçti**: açık aksesuarın kalan miktarı 2/3, takılı bileşenin 3/5 miktarı döndü; diğer şirkete ait kayıtlar yanıtta yoktu. Hosted Chromium ilişkiler sekmesinde satırları, sayaçları, miktarları, tarihleri ve notları doğruladı; tamamı iade edilmiş/sökülmüş kayıtlar listelenmiyor. Tam `asset-detail-actions.mjs` exit 0; `Verify-AssetDetailActions.mjs`, JS sözdizimi ve `git diff --check` geçti. Test DB/ağı temizlendi, ürün DB'si ve canlı ortam değiştirilmedi.
- [x] **C2.4** Yazılım lisansı koltuğu atama/geri alma ✅ — `Software_license_can_be_assigned_to_same_company_asset_and_is_visible_in_both_details` izole PostgreSQL'de **1/1 geçti**: başka şirket varlığına atama reddi, aynı şirkete atama, mükerrer atama reddi, lisans ve varlık ayrıntısında görünürlük, geri alma ve koltuk sayısının sıfırlanması doğrulandı. Hosted Chromium'da atama sonrası varlığın İlişkiler sekmesinde lisans adı/türü/süresiz tarih/not görünümü; geri alma iptalinde yazma olmaması; onaylı geri almadan sonra boş durum satırı doğrulandı. Tam `asset-detail-actions.mjs` exit 0; `Verify-AssetDetailActions.mjs`, `node --check` ve `git diff --check` geçti. Yalnız sentetik tarayıcı ve tek kullanımlık DB kullanıldı; ürün DB'si/canlı ortam değiştirilmedi.
- [x] **C2.5** Gerçek hareket geçmişi + not ekleme (2.000 karakter) ✅ — `Asset_notes_require_write_access_and_are_audited_in_the_tenant_history` izole PostgreSQL kabulinde **1/1 geçti**: boş not ve 2.001 karakter reddedildi; 2.000 karakter kaydedildi; CSRF, başka şirket varlığına yazma reddi, viewer yazma reddi, varlık hareket geçmişi ve audit kaydı doğrulandı. Hosted Chromium formun 2.000 karakter sınırını, gerçek `NoteAdded` hareketinin yenilenen zaman çizelgesinde görünmesini, CSRF'li isteği ve kayıt sonrası formun temizlenmesini doğruladı. Tam `asset-detail-actions.mjs` exit 0; statik doğrulayıcı ve sözdizim kontrolü geçti. Tek kullanımlık test DB'si temizlendi; ürün DB'si/canlı ortam değiştirilmedi.
- [x] **C2.6** Fotoğraf/PDF ek yükleme (30 dosya, 10 MiB) ✅ — `Asset_attachment_upload_download_and_delete_are_authorized_tenant_scoped_and_audited` izole PostgreSQL kabulinde **1/1 geçti**: geçerli PDF/PNG, tam 10 MiB PNG kabul edildi; bozuk dosya, başka şirkete yükleme ve viewer yazması reddedildi; varlık başına 30 kayıt kabul, 31. kayıt 409 ile reddedildi; indirme, silme, dosya sistemi, geçmiş ve audit doğrulandı. Hosted Chromium dosya seçme→gönderene kadar yazmama, ilişkilendirme, küçük önizleme, zaman çizelgesi/indirme ve silme iptal/onay akışlarını geçti; tam `asset-detail-actions.mjs` exit 0. TestServer Kestrel'in HTTP istek gövdesi limit özelliğini desteklemediği için gerçek Kestrel gövde-limit sınırı bu kabulde doğrulanmadı; API'nin 10 MiB dosya boyutu kontrolü doğrulandı. Ürün DB'si/canlı ortam değiştirilmedi.
- [x] **C2.7** En yeni görsel listede küçük önizleme ✅ — `Asset_attachment_upload_download_and_delete_are_authorized_tenant_scoped_and_audited` izole PostgreSQL kabul testi **1/1 geçti**: aynı varlıktaki iki görselden daha yeni olanın ID'si `/api/assets` listesindeki `photoAttachmentId` oldu; aradaki PDF ekleri görsel seçimine katılmadı, 30 ek sınırı korundu. Hosted Chromium gerçek envanter satırını yenileyerek yüklenen görselin API'den geldiğini, tarayıcıda 54×44 px dış boyutta yüklendiğini ve tıklanınca aynı dosyanın büyütme penceresinde açıldığını doğruladı. Tam `asset-detail-actions.mjs` Chromium paketi exit 0; statik doğrulayıcı, `node --check` ve `git diff --check` geçti. Ürün DB'si/canlı ortam değiştirilmedi.
- [x] **C2.8** Bakım sekmesi ✅ — Hosted Chromium `asset-detail-actions.mjs` varlık bakım sekmesinin seçilmesini/görünmesini, bakım planı oluşturmayı, planlı kaydı başlatmayı, iptal edilen tamamlama formunun yazmamasını ve başarılı tamamlamada durum/notun yenilenmesini doğruladı; tam paket exit 0. İzole PostgreSQL kabul testlerinde `Asset_maintenance_start_and_completion_preserve_authorization_state_and_history` ve `Scheduled_maintenance_cancellation_enforces_csrf_company_and_role_scope_and_preserves_history` **2/2 geçti**: CSRF, viewer ve başka tenant sınırları, tarih/tekrar geçiş doğrulamaları, hareket geçmişi ve bakım listesindeki kalıcılık kapsandı. Statik doğrulayıcı, JS sözdizimi ve `git diff --check` geçti. Ürün DB'si/canlı ortam değiştirilmedi.
- [x] **C2.9** Dosya/geçmiş zaman çizelgesi görünümü ✅ — İşlem olayları ve varlık ekleri tarih sırasıyla aynı geçmiş akışında gösterilir; dosya adı, yükleyen, bağlı hareket ve yetkili indirme bağlantısı yer alır. Ekleme/kaldırma sonrası zaman çizelgesi ve ek sayısı yenilenir. Kanıt: gerçek Chromium asset detail kabulinde CSRF'li ekleme, hareket bağı, zaman çizelgesi kartı/indirme URL'si ve kaldırma yenilemesi.
- [x] **C2.10** Varlık ömrü / yaşlanma göstergesi ✅ — Alış tarihinden bugüne geçen süre detayda yıl/ay gösteriliyor; model amortisman ömrü tanımlıysa geçen/kalan ay ve tahmini defter değeri de sunuluyor. Kanıt: varlık detay API kabul testi (`ageMonths`, `depreciationMonths`, `remainingMonths`, `currentValue`) ve hosted Chromium asset detail akışındaki yaş gösterimi.
- [x] **C2.11** Amortisman / mevcut değer gösterimi ✅ — `Asset_detail_returns_company_scoped_book_value_and_null_when_required_inputs_are_missing` izole PostgreSQL kabul testi **1/1 geçti**: 12 ay geçmiş, 36 aylık doğrusal modelde 24 ay kalan ve 1.200 USD alış bedelinden 800 USD tahmini değer döndü; başka tenant varlığı 404 aldı; alış bedeli, alış tarihi veya model amortismanı eksikken değer null kaldı. Hosted Chromium varlık ayrıntısında geçen/kalan ayları, para birimiyle mevcut değeri ve tahmin açıklamasını; model politikası/tarih/maliyet eksik durumlarının doğru mesajlarını doğruladı. Tam `asset-detail-actions.mjs` paketi exit 0; `Verify-AssetDetailActions.mjs`, JS sözdizimi ve `git diff --check` geçti. Ürün DB'si/canlı ortam değiştirilmedi.

### C3. Sipariş ve satın alma (varlık)
- [x] **C3.1** Çok kalemli varlık satın alma siparişi ✅ — `Asset_purchase_orders_are_tenant_scoped_export_formula_safe_and_receive_assets_atomically` izole PostgreSQL kabul testi **1/1 geçti**: 2 sipariş kalemi (3 laptop + 2 monitör), 42.500 TL ara toplam, 1.500 TL iskonto, 8.200 TL vergi, 600 TL nakliye ve 49.800 TL toplam; iki parçalı kısmi/tam teslimatla beş varlık ve doğru sipariş satırı bağlantısı doğrulandı. Cihazlara kuruş dağıtımı satır maliyeti toplamlarını 43.941,17 TL + 5.858,83 TL = 49.800 TL ile tam eşleştiriyor; farklı tenant erişimi reddi, CSV formül güvenliği, sipariş eki ve satır bazlı iade denetimi de aynı testte geçti. Ürün DB'si/canlı ortam/deploy değiştirilmedi.
- [x] **C3.2** Tedarikçi, tarih, para birimi, vergi, iskonto, nakliye ✅ — İzole PostgreSQL `Asset_purchase_orders_are_tenant_scoped_export_formula_safe_and_receive_assets_atomically` kabul testi ve gerçek ürün HTML'ini kullanan sentetik Chromium UI akışı; tedarikçi/tarih/TRY, iki satırın adet-birim fiyatı, %20 vergi, 1.500 TL iskonto, 600 TL nakliye ve 49.800 TL toplamı birlikte doğruladı. API testi **1/1**, tam hosted Chromium paketi exit **0**. Ürün DB'si/canlı ortam değiştirilmedi.
- [x] **C3.3** Kısmi teslim alma → otomatik varlık kartı/etiketi oluşturma ✅ — İzole PostgreSQL testinde iki aşamalı teslimat, 5 gerçek DB varlığının doğru sipariş satırına bağlanması ve sipariş toplam maliyetinin kuruşu kuruşuna dağıtılması doğrulandı. Hosted Chromium'da teslimat penceresinden vazgeçmek yazmadı; onaylı tek adet teslimat CSRF kullandı, “1/3 · Kısmi teslim alındı” durumunu yeniledi ve doğru sipariş satırına bağlı bir varlık kaydı oluşturdu. API testi **1/1**, tam hosted Chromium paketi exit **0**; UI servisi sentetikti. Ürün DB'si/canlı ortam değiştirilmedi.
- [x] **C3.4** Sipariş iptali + denetim izi ✅ — `Asset_purchase_orders_are_tenant_scoped_export_formula_safe_and_receive_assets_atomically` izole PostgreSQL kabul testi artık CSRF'siz iptal reddini, başka tenant reddini, onaylı iptalin `asset-order.cancelled` audit kaydını, tekrar iptal ve iptal sonrası teslimat reddini doğruluyor. Hosted Chromium `asset-detail-actions.mjs` iptal onay penceresinden vazgeçince yazma olmadığını; onayla tek CSRF'li POST, modal kapanması ve listede `İptal edildi` yenilemesini doğruladı. API testi **1/1 geçti**, güncel tam hosted Chromium paketi exit **0**. Yalnız sentetik/izole test verisi kullanıldı; ürün DB'si/canlı ortam değiştirilmedi.
- [x] **C3.5** Sipariş belgeleri (PDF/PNG/JPEG/WebP, 30 dosya) ✅ — İzole PostgreSQL/HTTP kabulinde gerçek PDF/PNG/JPEG/WebP imzaları, CSRF ve başka tenant reddi, tam 10 MiB PDF kabulü, 10 MiB+1 bayt reddi, sipariş başına 30 ek ve 31. ek için 409, listede kalıcılık ve 30 audit kaydı doğrulandı (**1/1 geçti**). Hosted Chromium'da dört biçim seçilip açık düğmeye basılana kadar yüklenmeme, her yüklemede CSRF, silme penceresinden vazgeçince yazmama, onaylı silme ve listenin yenilenmesi geçti; tam paket exit **0**. TestServer `IHttpMaxRequestBodySizeFeature` sunmadığı için Kestrel'in toplam multipart istek gövdesi limiti ayrıca bu testte kanıtlanmadı; uygulama dosya-boyutu kontrolü sınırları geçti. Ürün DB'si/canlı ortam değiştirilmedi.
- [x] **C3.6** Sipariş ↔ cihaz profili iki yönlü geçiş ✅ — Hosted Chromium'da sipariş ayrıntısındaki teslim alınan cihazı açma, cihaz profilinde sipariş numarası/tedarikçiyi gösterme ve bağlantıya basınca aynı sipariş ayrıntısına dönme doğrulandı. Tam `asset-detail-actions.mjs` paketi exit **0**, tarayıcı hatası yok. Sentetik API kullanıldı; ürün DB'si ve canlı ortam değiştirilmedi.
- [x] **C3.7** Varlık, aksesuar, sarf ve bileşen satın alma siparişlerinde ortak yıllık şirket bütçesi ve gerekçeli onay ✅ — 17 Ekim 2026: aynı şirket/yıl/para birimindeki üç sipariş ailesi ortak taahhüt tutarına katılır; limit aşımındaki sipariş bağımsız yönetici gerekçeli onayı/reddi gelene kadar teslim alınamaz. İzole PostgreSQL/HTTP kabulinde 80 TL aksesuar + 30 TL sarf siparişinin 100 TL ortak bütçeyi aştığı, sarf siparişinin beklemeye geçtiği, erken teslimin reddi, farklı yöneticinin onayı, onay sonrası teslim ve 110 TL toplam taahhüt **1/1 geçti**. Bu turdaki gerçek Chromium akışı bekleyen rozeti, yönetici onay formunu, karar sonrası kontrolleri ve liste yenilenmesini doğruladı; tam hosted Chromium regresyon paketi **exit 0**, migration-upgrade kabul testi **1/1 geçti**. Migration yalnız geçici kabul DB'sinde çalıştı. Yerel ürün DB'si ve canlıya migration/deploy yapılmadı.

### C4. Durum kuyrukları
- [x] **C4.1** Kullanılabilir / Atanmış / Beklemede / Bakımda / Kullanılamaz / Arşiv ✅ — Altı yerleşik durumun her biri için API filtresi ve şirket sınırı izole PostgreSQL kabulinde 1/1 geçti; gerçek Chromium altı kuyruğu, sonuç sayısını ve salt-okunur filtrelemeyi doğruladı. Tam hosted Chromium regresyon paketi exit 0 (17 Ekim 2026).
- [x] **C4.2** Arşivden kullanılabilire geri alma (kilitli + denetim izli) ✅ — Arşivli varlık için yalnız yetkili, tenant kapsamlı geri alma; açık bakımda 409 kilidi, başarılı işlemde olay geçmişi ve audit kaydı izole PostgreSQL kabul testinde 1/1 geçti. Gerçek Chromium arşivle/geri al penceresi, açıklama, CSRF ve yenilenen durum görünümünü doğruladı; tam hosted Chromium regresyon paketi 17 Ekim C4.1 çalıştırmasında exit 0.
- [x] **C4.3** Beklemede ve kullanılamaz kuyrukları ✅ — Pano sayaçları tıklanınca ilgili durum filtresiyle şirket envanterini açıyor; Pending/Unusable sayaçları ve listeleri başka tenant kayıtlarını dışlıyor. İzole PostgreSQL API testi 1/1 ve Chromium pano→filtreli liste akışı geçti; tam hosted Chromium paketi exit 0 (17 Ekim 2026).
- [x] **C4.4** Denetimi gelen/geçen ayrı kuyruklar ✅ — Geciken/yaklaşan/plansız filtreleri, arama, dönem ve sayfalama; izole PostgreSQL tenant kabulü ve gerçek HTML üzerinde sentetik Chromium kabulü geçti (17 Ekim 2026).
- [x] **C4.5** İadesi gelen/geciken ayrı kuyruklar ✅ — Geciken/yaklaşan/tarihsiz/ileri tarihli filtreleri, arama, sayfalama ve eski istek yanıtını yok sayma; iade iptali yazma yapmıyor, onaylı iade CSRF ile kaydedilip listeyi yeniliyor. İzole PostgreSQL tenant/API testi 1/1 ve tam hosted Chromium regresyon paketi geçti (17 Ekim 2026).

---

## D. ÇALIŞANLAR VE ORGANİZASYON

- [x] **D.1** Çalışan kaydı oluşturma/düzenleme (giriş hesabı açmadan) ✅ — Yetki/CSRF, tenant sınırı, yinelenen personel numarası, audit kaydı ve kullanıcı/üyelik tablosuna dokunulmaması izole PostgreSQL API kabulinde doğrulandı; gerçek Chromium çalışan listesinden oluşturma ve düzenleme sentetik API ile doğrulandı.
- [x] **D.2** Görev unvanı, telefon, konum, işe başlama tarihi ✅ — Oluşturma ve düzenleme formlarının dört alanı ile e-posta/departman verileri UI→API akışında; kalıcı alan değerleri API kabulinde doğrulandı.
- [x] **D.3** Yönetici / doğrudan raporlayan ilişkisi (döngü engelli) ✅ — Oluşturma ve profil akışında yönetici atama/değiştirme, doğrudan raporlayan görünümü ve döngü reddi gerçek Chromium’da doğrulandı; API’de döngü/kendini/yabancı tenant ataması, rol, CSRF ve audit testi izole PostgreSQL’de 1/1 geçti.
- [x] **D.4** Özel İK notları (sadece yetkili roller) ✅ — Gerçek profil Chromium akışı ve izole PostgreSQL API kabulinde rol/tenant sınırı, CSRF, kayıt kalıcılığı ve audit doğrulandı; not içeriği audit'e kopyalanmıyor.
- [x] **D.5** Etkin/pasif filtresi + işe başlama tarih aralığı ✅ — Gerçek Chromium akışında varsayılan aktif, pasif/tümü, iki sınır günü dahil tarih aralığı, tarihsiz kayıt dışlama, temizleme ve ters aralığın eşitlenmesi doğrulandı. İzole PostgreSQL API kabulinde tenant kapsamı, aktif/pasif ve dahil sınırlar, ters aralık 400 ve viewer 403 doğrulandı.
- [x] **D.6** Profil fotoğrafı (2 MiB) ✅ — Gerçek profil Chromium akışında fotoğraf yükleme, 2 MiB üst sınırının istek öncesi kontrolü, iptal edilebilir silme onayı ve rol kontrollü düğmeler doğrulandı. İzole PostgreSQL API kabulinde geçersiz/oversize/CSRF/rol reddi, PNG→JPEG değişimi, tenant kapsamı, indirme içerik türü, silme ve audit 1/1 geçti.
- [x] **D.7** Çalışan belgeleri (30 dosya, 10 MiB, rol kontrollü) ✅ — Gerçek HTML Chromium ve izole PostgreSQL kabulinde içerik doğrulama, yükleme/indirme, iptal edilebilir silme, rol/tenant ve audit doğrulandı.
- [x] **D.8** Çalışan profilinde zimmetli varlık/aksesuar/lisans/sarf listesi ✅ — İzole PostgreSQL API kabulinde çalışan detayının şirket kapsamlı varlık, açık aksesuar (verilen/iade/kalan adet), etkin lisans ve sarf teslim hareketlerini doğru sayılarla döndürdüğü doğrulandı. Gerçek ürün HTML’inde Chromium sentetik API ile dört listenin dolu satırları, sayaçları, adetleri, yayıncı/sürüm ve sarf notu doğrulandı. Tam hosted Chromium paketi exit 0; statik UI doğrulayıcı da ilişki alanlarını ve hedef detay çağrılarını kontrol ediyor. Canlı/ürün veritabanına dokunulmadı.
- [x] **D.9** Sorumlu olduğu bakım kayıtları ✅ — İzole PostgreSQL SecurityAcceptance `Employee_profile_lists_only_company_maintenance_assigned_to_that_employee` şirket kapsamı ve yalnızca çalışana atanmış bakım kayıtlarını doğruladı. Gerçek ürün HTML'ini kullanan hosted Chromium çalışan profilindeki bakım satırını, sayacı, tür/servis/durum alanlarını ve varlık ayrıntısı düğmesini sentetik API verisiyle doğruladı; statik UI doğrulayıcısı düğmenin ilgili varlığın bakım sekmesine bağlandığını kontrol etti. Yerel/Canlı ürün verisi değiştirilmedi.
- [x] **D.10** CSV/Excel içe aktarma (şablon + önizleme + atomik ekleme) ✅ — Chromium gerçek ürün HTML'inde CSV şablonu indirildi; geçerli CSV önizlemesi çalışan eklemedi, onay penceresini iptal etmek yazmadı, açık onay aynı önizlenen içeriği CSRF ile yalnız bir kez gönderip çalışanı listeye ekledi. İzole PostgreSQL `Employee_csv_import_preview_is_read_only_and_commit_is_atomic_round_trips_managers_and_audits` ve `Employee_xlsx_import_preview_and_commit_preserve_values_and_audit` **2/2 geçti**: CSV döngü, duplicate, rol/CSRF, manager round-trip, audit ve atomik ekleme; XLSX alan/değer ve audit doğrulandı. Tam paket düzeltme sonrası yeniden çalıştırılmadı. Yerel/Canlı ürün verisi değişmedi.
- [x] **D.11** CSV dışa aktarma (yeniden içe alınabilir) ✅ — İzole PostgreSQL `Employee_list_detail_and_export_are_scoped_to_the_active_company` **1/1 geçti**: tenant izolasyonu, arama/departman/aktiflik/başlama tarihi filtreli dışa aktarma, 12 sütunlu round-trip başlığı, yerelleştirilmiş başlıklar ve desteklenmeyen dilin reddi; aynı CSV içe aktarma önizlemesinde mevcut personel numaraları nedeniyle reddediliyor. Gerçek ürün HTML’inde hosted Chromium filtreleri liste ve dışa aktarmaya uyguladı, UTF-8 BOM’lu indirmeyi ve tarihli dosya adını doğruladı; tekrar önizleme ekleme düğmesini kilitledi, commit çağrısı yapmadı ve mevcut çalışanı değiştirmedi. Düzeltme sonrası birleşik hosted Chromium regresyon paketi exit 0 verdi. Canlı/ürün verisi değişmedi.
- [x] **D.12** Departman kataloğu (ana/alt, kat, tam yol) ✅ — İzole PostgreSQL `Department_hierarchy_can_be_created_moved_and_filtered_by_floor` **1/1 geçti**: ana/alt departman, kat filtresi ve tam yol, viewer 403, yabancı şirket referansı reddi, yeniden ebeveyn/kat taşıma, döngü ve yedi seviyeli ağaç reddi, rename sırasında çalışan serbest metninin güncellenmesi ve yabancı kayıt 404 doğrulandı. Gerçek ürün HTML'inde hedefli Chromium alt departman oluşturma/düzenleme, CSRF, tam yol, çalışan listesi ve kat kısayolunu doğruladı; kat filtreli listede Yenile aynı katı koruyor. Birleşik Chromium regresyonu exit 0; 80/80 Verify modülü, JS sözdizimi ve diff check geçti. İzole test verisi kullanıldı; ürün/canlı verisi, migration ve deploy değiştirilmedi.
- [x] **D.13** Departman pasife alma + çalışanları kademeli taşıma ✅ — 17 Ekim 2026: Etkin alt departman sırası korunarak arşiv/geri yükleme; eski çalışan kayıtları silinmeden saklanıyor; yeni çalışan ataması arşivli departmana kapalı; tenant/rol/CSRF sınırları ve her parti için audit kaydı doğrulandı. Çalışanlar seçilen etkin hedefe en fazla 100 kayıtlık partilerle taşınıyor; kalan sayısı her adımda dönüyor. Kanıt: `Department_can_be_archived_and_employees_moved_in_audited_batches` (205 çalışanla 100+100+5, 1/1 PostgreSQL); `employee-records.mjs` targeted + full Chromium; 80/80 kaynak doğrulayıcısı.
- [x] **D.14** Departman yetkilisi/ekip yöneticisi atama ✅ — Yetkili aynı şirketteki etkin çalışanlardan seçilir; arşivli departmana atama, pasif çalışan ve başka şirkete ait çalışan reddedilir. Departman listesi yetkili adını/durumunu gösterir; yetkili atanabilir veya kaldırılabilir. Tenant kapsamlı composite FK, rol/CSRF, audit kaydı ve yenilenen UI doğrulandı. Kanıt: izole PostgreSQL `Department_manager_is_tenant_scoped_active_and_audited` 1/1; Chromium gerçek ürün HTML'inde atama/temizleme 2/2; 80/80 Verify ve locale doğrulaması. Yerel/Canlı ürün DB'si veya deploy değiştirilmedi.
- [x] **D.15** Şirket üyelikleri ekranı (hesap ↔ çalışan bağı) ✅ — Üye listesinde şirket giriş hesabı mevcut çalışan profiline bağlanabilir veya bağ kaldırılabilir; çalışan araması ve kayıt sonrası yenileme çalışır. Eski/yavaş arama yanıtının yeni sonucu ezmesi engellenir, değişmeyen seçim yazma göndermez. Gerçek ürün HTML'inde Chromium bağlama/kaldırma, CSRF, arama yarışı ve liste yenilemesi geçti. İzole PostgreSQL `Member_employee_link_is_tenant_scoped_unique_and_binds_self_service_requests` 1/1: tenant sınırı, benzersiz bağ, kendine hizmet talebinde doğru çalışan ve kaldırma doğrulandı. Yerel/Canlı ürün DB'si veya deploy değiştirilmedi.
- [x] **D.16** Konum kataloğu: bina → kat → oda → kabin (6 seviye) ✅ — Şirket konum dizininde bina/üst konumdan altıncı seviyeye kadar yeni kayıt ekleme, tam yol önizlemesi ve yenileme doğrulandı. Altıncı seviyedeki konum yeni alt konum için seçilemez; yedinci seviye için sonuçsuz gönderim önlenir. `Location_catalog_supports_six_levels_and_rejects_a_seventh` izole PostgreSQL kabulü 1/1 geçti; `asset-detail-actions.mjs` altı ardışık eklemeyi, CSRF ve UI sınırını geçti. Tam hosted Chromium regresyonu exit 0, 80/80 Verify, beş dil ve diff check geçti. Ürün/canlı DB veya deploy değiştirilmedi.
- [x] **D.17** Konum varsayılanı + konum sorumlusu ✅ — Varsayılan konumu ayarlama/değiştirme/kaldırma ve konum sorumlusu etkin çalışan atama/kaldırma doğrulandı. Varsayılan değişiminde eski konum için audit izi eklendi. İzole PostgreSQL `Location_default_and_manager_are_tenant_scoped_active_and_audited` 1/1; gerçek ürün HTML'inde Chromium CSRF, tek varsayılan, liste yenileme, atama/kaldırma geçti. Ürün/canlı DB ve deploy değiştirilmedi.
- [ ] **D.18** Konum bazlı erişim yetkileri — devam ediyor. Üyelikte `all/restricted` modu ve alt konuma miras; yönetici UI, CSRF/audit/no-op; varlık/çalışan/aksesuar/sarf/bileşen listeleri ve CSV, kimlikli ayrıntı uçları, konum kataloğu ve aksesuar filtre seçenekleri kapsamlandı. Stok taleplerinin çalışan/katalog/liste/CSV/oluşturma/karar/iptal/kit iade uçları da kapsama bağlandı; hazır kit, tüm iç kalemleri izin verilen konumdaysa görünür. Dashboard/raporlar, ilişki/indirme yolları ve kabul testleri üzerinde çalışmalar sürüyor. Departman listeleme ile oluşturma/düzenleme, amir, arşiv, rename ve batch çalışan taşıma için konum sınırları uygulandı. **18 Ekim 2026 09:22 TRT ek kanıt:** satın alma siparişi kapsamı regresyon testindeki fixture düzeltildi; dış konum satırlı sipariş, doğrulanan izinli varlığa bağlandı ve hedefli izole PostgreSQL kabul testi **1/1 geçti**. **09:29 TRT ek kabul:** model ayrıntısı, kullanıcı kimliği eksikse reddediyor; kısıtlı kullanıcının erişilebilir etkin varlığı olmayan modele erişimi 404, model özeti çöpteki varlığı saymıyor; aynı hedefli D.18 PostgreSQL testi **1/1 geçti**. **09:38 TRT ek kabul:** ek listesi ve indirmesi yalnız silinmemiş, erişilebilir konumda etkin varlığı olan model için açılıyor; yalnızca silinmiş varlığı kalan modelin ayrıntısı, ek listesi ve indirmesi 404. Hedefli izole PostgreSQL D.18 testi **1/1 geçti**. Yerel web imajı güncellendi; `/health` ve ana sayfa HTTP 200, `/api/status` `ready` (108 migration/0 bekleyen). Bu kontroller geniş kabul paketinin tamamlandığı anlamına gelmiyor. Önceki D.18 kapsamı: denetim planı, denetim, hareket notu yazma; kapsam dışı varlık/parent/transfer reddi; çalışan/olay kimliği, adı ve not gizliliği; dış konum aksesuar/bileşen satırları. `Verify-AssetDetailActions.mjs` beş sekme ve 13 eylemi doğrulamıştı. **Açık:** diğer modüllerin kalan yazma/ilişki/indirme yolları ve D.18 bütünsel kabulü. Ürün DB ve canlıya migration/deploy yapılmadı.
**18 Ekim 2026 09:44 TRT ek kabul:** Model lookup liste/tekil uçları da aynı konum kuralında; silinmiş varlık sayılmıyor. Hedefli D.18 PostgreSQL testi 1/1, `Verify-AssetModelDetailsUi.mjs` ve diff check geçti. Yerel web imajı yeniden derlendi; `/health` ve `/` HTTP 200, `/api/status` ready 108/0. Canlı ve ürün DB değişmedi, sayaç 44/107.
**18 Ekim 2026 10:10 TRT tam regresyon:** izole PostgreSQL SecurityAcceptance paketi **190/190 geçti** (20 dk 50 sn), D.18 ana testi ve model listesi/ek regresyonları dahil. Geçici Docker test DB/konteyneri/ağı kaldırıldı. Yerel ürün DB/canlı değişmedi; D.18 hâlâ bütünsel olarak açık, genel sayaç 44/107.

**18 Ekim 2026 10:22 TRT D.18 dışa aktarma/toplu işlem kapsamı:** normal ve seçili varlık CSV dışa aktarımları, toplu durum/zimmet/iade etkin konum iznine göre filtrelendi; zimmet hedefi ve iadenin mevcut çalışanı da kapsam içinde doğrulanıyor. Hedefli izole PostgreSQL testi **1/1 geçti**: dış/konumsuz varlık CSV'de yok, dış konumdaki toplu işlemler ve dış çalışanlı varlığın iadesi 404, veritabanı değişikliği yok. `Verify-AssetRowActions.mjs`, `Verify-AssetDetailActions.mjs`, `Verify-AppNavigation.mjs` ve diff kontrolü geçti. Yerel web yeniden kuruldu; canlı ve ürün DB değişmedi. D.18 bütünü açık, genel sayaç 44/107.

**18 Ekim 2026 10:47 TRT tam regresyon:** Bu değişiklikler sonrasındaki tam izole PostgreSQL SecurityAcceptance paketi **190/190 geçti** (21 dk 36 sn). D.18 testinin yanında migration, tenant/rol/CSRF, import/export, dosya, ödeme ve MFA regresyonları dahil. Geçici DB/konteyner/ağ temizlendi. Yerel API 108 migration/0 bekleyen ve hazır; ürün DB/canlı değişmedi. D.18 bütünsel açık, genel sayaç 44/107.

**18 Ekim 2026 10:59 TRT D.18 katalog görselleri:** `/api/models/{id}/photo` ve `/api/accessory-models/{id}/photo` kısıtlı üyede geçerli kullanıcı kimliği olmadan erişim vermez. Model görseli erişim hesabında silinmiş varlıklar dışlanır. D.18 kabul senaryosuna, silinmiş varlığa bağlı model fotoğrafının ve dış konumdaki aksesuar-model fotoğrafının 404 verdiği kontroller eklendi; izole PostgreSQL hedefli test **1/1 geçti**. `Verify-AssetModelDetailsUi.mjs`, `git diff --check` geçti; yerel web hazır (108/0), ürün DB/canlı değişmedi. Bu dar değişiklikten sonra tam 190'lık paket yeniden koşulmadı; D.18 üst maddesi açık ve sayaç 44/107.
- [x] **D.19** Şirket birimleri (tüzel kişilik/şube ayrımı) ✅ — 19 Ekim 2026 kabulü: `Company_units_are_hierarchical_tenant_scoped_and_archived_without_hard_deletes` izole PostgreSQL senaryosu birim türü/hiyerarşi, tenant ve rol sınırı, arşiv/geri yükleme, konum/çalışan/varlık bağlantıları, doğrudan birim erişimi, API görünürlüğü ve kapsamlı liste/rapor/CSV yollarını doğruluyor. `Asset_employee_relations_use_direct_unit_and_inherited_location_scope_in_lists_reports_and_exports` ve stok/çalışan/lisans kapsam senaryoları doğrudan ve konumdan miras alınan birim sınırlarını tamamlıyor. Güncel SecurityAcceptance paketi **199/199**; güncel Chromium UI paketi **exit 0**, şirket birimi oluştur/düzenle ve üye erişim arayüzü geçti. Varlık/çalışan birim bağları ve erişim kısıtları artık açık değil. D.18’in tüm modüller için konum kapsamı ayrı açık kabul maddesidir.

---

## E. ZİMMET, İADE, TRANSFER, REZERVASYON

- [ ] **E.1** Varlık zimmetleme (tekli) ✅
- [ ] **E.2** Toplu zimmet (100 varlık, atomik) ✅
- [ ] **E.3** İade alma (tekli + toplu, kısmi iade) ✅
- [ ] **E.4** Zimmet kuyruğu: arama, sayfalama, geciken/30 gün/tarihsiz/ileri tarih filtreleri ✅
- [ ] **E.5** Yazdırılabilir **zimmet formu** (çalışan + üzerindeki varlıklar + tarih) ✅
- [ ] **E.6** Dijital imza (ekranda imza atma) + PDF çıktı ❌ ⚠️ (eski isteğin)
- [ ] **E.7** EULA/kabul belgesi zimmete bağlama + SHA-256 sabitleme 🟡 (lisans var, zimmete yayılmamış)
- [ ] **E.8** Konum transferi (tekli + toplu 100) ✅
- [ ] **E.9** Rezervasyon: talep → onay/red → iptal, çakışma engeli ✅
- [ ] **E.10** Rezervasyon zimmete dönüşünce otomatik tamamlandı ✅
- [ ] **E.11** Hareket geçmişi: tür/işlem/tarih filtreleri + 10.000 satır CSV ✅
- [ ] **E.12** Çalışan self-servis talebi (kendi adına) ✅
- [x] **E.13** Zamanlı hatırlatma: saatlik worker, kullanıcı tercihli günlük SMTP özeti; bakım/denetim/lisans/garanti/iade/rezervasyon/zimmet/varlık ömrü hatırlatmaları konum erişimine göre filtrelenir. İzole PostgreSQL kabulü 1/1 izinli/yasaklı konum, tenant sınırı ve günlük tekrar engelini geçti. Gerçek SMTP sağlayıcı teslim kabulü H.10 altında açık.

---

## F. LİSANSLAR VE YAZILIM

- [ ] **F.1** Lisans kataloğu: yayıncı, sürüm, tür, referans, koltuk sayısı ✅
- [ ] **F.2** Çalışana koltuk atama/geri alma (tekli) ✅
- [ ] **F.3** Toplu koltuk atama (100 çalışan, atomik, koltuk sınırı kontrollü) ✅
- [ ] **F.4** Varlığa koltuk atama ✅
- [ ] **F.5** Maliyet/para birimi, alış/bitiş tarihi, otomatik yenileme ✅
- [ ] **F.6** Lisans bitiş raporu (30/90/180/366 gün) + CSV ✅
- [ ] **F.7** Lisans belgeleri (30 dosya) ✅
- [ ] **F.8** EULA zorunluluğu + sürüm + SHA-256 + kabul kaydı ✅
- [ ] **F.9** Ürün anahtarı **saklanmıyor** (bilinçli karar) ✅ ⚠️ İstersen şifreli saklama eklenebilir
- [ ] **F.10** Sözleşmeler modülü (contract) ❌ menüde var, arkası boş
- [ ] **F.11** Garanti takibi modülü ✅ (rapor olarak)
- [ ] **F.12** Lisans ↔ tedarikçi/sipariş bağlantısı 🟡
- [ ] **F.13** Yazılım envanteri (kurulu uygulamaları cihazdan toplama) ❌ ⚠️ Ajan gerekir

---

## G. AKSESUAR, SARF, BİLEŞEN, KİT

- [ ] **G.1** Aksesuar kataloğu + model + marka + kategori + ürün fotoğrafı ✅
- [ ] **G.2** Aksesuar stok girişi/çıkışı, düşük stok uyarısı ✅
- [ ] **G.3** Aksesuar teslimi: çalışan/varlık/konum, miktarlı ✅
- [ ] **G.4** Toplu teslim/iade (100 kayıt, değişken adet) ✅
- [ ] **G.5** Aksesuar CSV içe/dışa aktarma + şablon ✅
- [ ] **G.6** Aksesuar siparişleri (kısmi teslim, iptal, finans alanları, belgeler) ✅
- [ ] **G.7** Hazır aksesuar kitleri (stoktan üretim, toplu teslim, geçmiş) ✅
- [ ] **G.8** Karma kitler (aksesuar + sarf + bileşen birlikte) ❌
- [ ] **G.9** Varlık kalıpları (kit ile birlikte cihaz) ❌
- [ ] **G.10** Sarf malzemesi kataloğu + stok + çalışana tüketim çıkışı ✅
- [ ] **G.11** Sarf minimum stok uyarısı ✅
- [ ] **G.12** Sarf CSV içe/dışa aktarma ✅
- [ ] **G.13** Bileşen (RAM, disk vb.) kataloğu + seri takibi ✅
- [ ] **G.14** Bileşeni varlığa takma/çıkarma (miktarlı) ✅
- [ ] **G.15** Bileşen toplu sayım ve toplu takma (100) ✅
- [ ] **G.16** Bileşen CSV içe/dışa aktarma ✅
- [ ] **G.17** Ortak stok hareketleri ekranı (tür/İşlem/tarih filtreli) ✅
- [ ] **G.18** Stok talepleri: talep → onay/red → otomatik teslim/tüketim ✅

---

## H. BAKIM, DENETİM, TAKVİM, GÖREV

- [ ] **H.1** Bakım kaydı başlatma/tamamlama ✅
- [ ] **H.2** Bakım planlama (5 yıla kadar) + başlat/iptal ✅
- [ ] **H.3** Bakım türü, servis, maliyet, para birimi, sorumlu çalışan ✅
- [ ] **H.4** Bakım kayıtlarına dosya eki (30 dosya) ✅
- [ ] **H.5** Fiziksel denetim planlama + denetim kuyruğu (geciken/30/90/180/366 gün) ✅
- [ ] **H.6** Toplu denetim (100 varlık) + yeniden planlama ✅
- [ ] **H.7** Denetim sonuç raporu ✅
- [ ] **H.8** Takvim (liste/gün/hafta/ay, dönem gezinme, CSV) ✅
- [ ] **H.9** Görevler modülü 🟡 — arama/filtre/sayfalama, yönetici oluşturma-düzenleme-atama-durum güncellemesi ve atanan çalışanın başlatma/tamamlama akışı uygulanmış. 15 Ekim 2026: `Verify-CompanyTasksUi.mjs`, izole PostgreSQL tenant/rol/denetim kabul testi 1/1 ve hosted Chromium'da hem yönetici hem çalışan akışı geçti. Çalışan kabulinde yalnız atanmış görev görünürlüğü, yönetici düğmelerinin gizlenmesi ve “Başlat” → “Tamamla” akışı doğrulandı. Diğer görev gereksinimleri ve gerçek ortam kabuli tamamlanmadığından madde kısmi kalıyor.
- [ ] **H.10** Otomatik e-posta hatırlatmaları (bakım/denetim/garanti/lisans/iade/rezervasyon) 🟡 — ürün akışı ve sentetik SMTP kabulü var; canlı SMTP sağlayıcı teslimatı doğrulanmalı.
- [x] **H.11** Denetim sonucu "kabul edilmeyen öğe" akışı — tekil denetimde Kabul edildi/Uygun değil sonucu; uygun değil için bulgu zorunlu, varlık geçmişi + denetim raporu/CSV görünürlüğü. Durum otomatik değişmez. İzole PostgreSQL kabul testi 1/1 ve tam hosted Chromium regresyonu geçti (17 Ekim 2026). Toplu denetim sonuç seçenekleri ve canlı müşteri kabulü bu kapsamın dışındadır.

---

## I. BARKOD, QR VE ETİKET

- [ ] **I.1** 14 barkod türü (C128/C39/Code93/Codabar/MSI/EAN5/EAN13/UPC-A/UPC-E) + QR/PDF417/DataMatrix/Aztec ✅
- [ ] **I.2** Hazır etiket ölçüleri (50×30 … 100×50 mm) + özel ebat ✅
- [ ] **I.3** X/Y hizası, boşluk, gösterilecek alan seçimi ✅
- [ ] **I.4** A4/Letter çoklu yerleşim (100 varlığa kadar) ✅
- [ ] **I.5** Kamera ile okutma (tekli bul/işlem) ✅
- [ ] **I.6** USB barkod okuyucu desteği ✅
- [ ] **I.7** Çoklu okutmayla seçim → toplu zimmet/iade ✅
- [ ] **I.8** Yazıcı/etiket stoğu şablon kataloğu ✅
- [ ] **I.9** QR hedefi (varlık etiketi veya detay bağlantısı) ✅

---

## J. RAPORLAR

- [ ] **J.1** Hazır raporlar: durum/kategori/maliyet özeti ✅
- [ ] **J.2** Garanti raporu ✅
- [ ] **J.3** Lisans bitiş raporu ✅
- [ ] **J.4** Bakım maliyet raporu (tür/durum/para birimi) ✅
- [ ] **J.5** Bileşen stok raporu ✅
- [ ] **J.6** Tamamlanan denetimler raporu ✅
- [ ] **J.7** Beklenmeyen/geciken teslimat raporu ✅
- [ ] **J.8** Amortisman raporu (doğrusal / yarım yıl) ✅
- [ ] **J.9** Varlık alış kayıtları raporu ✅
- [ ] **J.10** Özel rapor oluşturucu (15 standart + özel alan sütunları, 50.000 satır CSV) ✅
- [ ] **J.11** Rapor taslaklarını kaydetme (30 taslak, cihazlar arası eşitleme) ✅
- [ ] **J.12** Grafik/dashboard raporları (kategori/marka/model/konum/durum) ✅
- [ ] **J.13** Muhasebe değerlemesi (gerçek amortisman muhasebesi) ❌ ⚠️ Kapsam kararı
- [ ] **J.14** Zamanlanmış rapor e-postası ❌
- [ ] **J.15** PDF rapor çıktısı (CSV dışında) ❌

---

## K. VERİ TAŞIMA VE YEDEK

- [ ] **K.1** Varlık/çalışan/aksesuar/sarf/bileşen/lisans CSV içe aktarma ✅
- [ ] **K.2** `.xlsx` içe aktarma (formül hücresi reddi, tarih normalizasyonu) ✅
- [ ] **K.3** Sütun eşleme profilleri (25 profil/şirket) ✅
- [ ] **K.4** Lisans süresi bitince **veriyi indir**, yenilenince **geri yükle** ✅/🟡 (dışa aktarma var, geri yükleme kısıtlı)
- [ ] **K.5** Şirket JSON dışa aktarma (salt okunur önizleme ile) ✅
- [ ] **K.6** Şirket ZIP dışa aktarma (veri + tüm medya) ✅
- [ ] **K.7** JSON/ZIP **geri yükleme** (mevcut kayıtlara birleştirerek) ❌ ⚠️ Sadece boş hedefe ekleme var
- [ ] **K.8** Şifreli tam yedek (AES-256-CBC + HMAC-SHA256 + SHA-256) ✅
- [ ] **K.9** Günlük otomatik yedek (Windows Görev Zamanlayıcı + systemd) ✅
- [ ] **K.10** İzole ortamda geri yükleme provası ✅
- [ ] **K.11** Canlı geri yükleme (volume değiştirmeli, hata halinde geri dönüş) 🟡 prototip
- [ ] **K.12** Uzak hedefe (SFTP/SMB) yedek kopyası ✅ prototip
- [ ] **K.13** Yedek tazelik/hash sağlık kontrolü + webhook uyarısı ✅ prototip
- [ ] **K.14** Yedek saklama süresi temizliği ✅
- [ ] **K.15** Çöp kutusu (silinen kayıtları kalıcı kurtarma) ❌

---

## L. KULLANICILAR, ROLLER VE YETKİLER

- [ ] **L.1** Şirket üyesi + rol (owner/admin/operator) ✅
- [ ] **L.2** Yedi şirket rolü (yönetici, envanter operatörü, denetçi, İK vb.) 🟡
- [ ] **L.3** İnce taneli modül/işlem izin matrisi (Snipe-IT'teki gibi tek tek yetki) 🟡
- [ ] **L.4** Rol bazlı ekran/görünürlük kontrolü ✅
- [ ] **L.5** Her API ucunda yetki + tenant kontrolü 🟡
- [ ] **L.6** Kullanıcı yönetimi ekranı (yönetici başka hesabı yönetir) 🟡
- [ ] **L.7** Destek talebi modülü 🟡 — şirket kullanıcısı talep oluşturma/yanıt/kapama; platform kuyruğunda arama, filtre, yanıt ve durum yönetimi. Tenant/rol/CSRF/audit/e-posta outbox API kabul testi 1/1; şirket + platform Chromium akışları geçti. Gerçek SMTP sağlayıcısı teslimi ve canlı müşteri kabulü açık.

---

## M. ŞİRKET AYARLARI VE GÖRÜNÜM

- [ ] **M.1** Firma profili (ad, adres, iletişim, vergi bilgisi) 🟡
- [ ] **M.2** Logo + ana renk ✅
- [ ] **M.3** Tema seçimi (açık/koyu) + renk paleti ✅ (demoda)
- [ ] **M.4** Bildirim tercihleri (tür bazında e-posta) ✅
- [ ] **M.5** Entegrasyonlar (webhook, dış sistem) ❌ menüde var, arkası boş
- [ ] **M.6** Güvenlik ayarları (parola politikası, oturum süresi, MFA zorunluluğu) 🟡
- [ ] **M.7** Denetim kayıtları (kim ne zaman ne yaptı, filtrelenebilir + CSV) ✅
- [ ] **M.8** Yerel ayar: tarih/saat biçimi, para birimi, sayı biçimi ✅
- [ ] **M.9** Şirket ayarlarını dışa/içe aktarma ✅

---

## N. API VE ENTEGRASYON

- [x] **N.1** Sürümlü müşteri API v1 (salt okunur: varlık, aksesuar, çalışan) — müşteri API kabul testi varlık/aksesuar/çalışan kapsamı, sahte şirket başlığına karşı tenant sınırı, aksesuar maliyeti ve özel İK notlarının yanıta sızmaması, arama/sayfalama sözleşmesini kapsar; geçici PostgreSQL tam SecurityAcceptance paketi **157/157 geçti** (15 Ekim 2026). Canlı dağıtım bu kanıta dahil değildir.
- [ ] **N.2** API anahtarı oluştur/listele/iptal (SHA-256 özeti saklanır) ✅
- [ ] **N.3** OpenAPI 3.1 tanımı yayında ✅
- [ ] **N.4** IP başına hız limiti ✅
- [x] **N.5** API ile **yazma** (varlık oluştur/güncelle) ✅ `assets:write` kapsamıyla POST/PUT; etkin özel alanları doğrulama ve korumalı saklama/okuma, hassas alan rol sınırları, tenant/katalog/kota/audit ve eski sürüm reddi izole PostgreSQL kabulinde doğrulandı (2/2 hedefli senaryo; 15 Ekim 2026). Canlı dağıtım bu kanıta dahil değil.
- [ ] **N.6** Webhook aboneliği + teslimat + yineleme ❌
- [ ] **N.7** MDM entegrasyonları (Intune, Jamf) ❌
- [ ] **N.8** Active Directory / LDAP senkronizasyonu ❌
- [ ] **N.9** E-posta (SMTP) yapılandırması ✅
- [ ] **N.10** Muhasebe/ERP entegrasyonu (Logo, Mikro, Netsis) ❌ ⚠️ Türkiye pazarı için değerli

---

## O. PLATFORM YÖNETİMİ (Süper Admin — sadece sana ait)

- [ ] **O.1** Platform admin ekranı (firma, plan, abonelik görünümü) 🟡
- [ ] **O.2** Firma listesi + detay + askıya al + sil 🟡
- [ ] **O.3** Müşteri paneline girme (impersonate) 🟡
- [ ] **O.4** Plan/fiyat yönetimi 🟡
- [ ] **O.5** Kupon oluşturma ❌
- [ ] **O.6** Abonelik: lisans uzat, manuel fatura, iade, ödeme geçmişi 🟡
- [ ] **O.7** Demo yönetimi (14→30 gün uzat, kalıcı hesaba çevir) 🟡
- [ ] **O.8** Ekstra özellik açma (ekstra kullanıcı/disk, premium) 🟡
- [ ] **O.9** Referans bonusu ❌
- [ ] **O.10** Tüm müşterilere duyuru gönderme 🟡
- [ ] **O.11** Loglar: giriş, ödeme, hata, deploy geçmişi 🟡
- [ ] **O.12** Dashboard: toplam müşteri, aylık gelir, yeni kayıtlar, başarısız ödemeler 🟡
- [ ] **O.13** Destek talepleri yönetimi ❌

---

## P. TİCARİ AKIŞ (Satış / Abonelik)

- [ ] **P.1** Self-servis kayıt (signup) 🟡 (fiyat/teklif sayfası var; self-servis kayıt formu ve API akışı yok)
- [ ] **P.2** Plan seçimi + ücretsiz deneme (14/30 gün) 🟡
- [ ] **P.3** Kredi kartı ile ödeme (iyzico) ❌ ⚠️ Başvuru + API anahtarı gerekir
- [ ] **P.4** Otomatik fatura üretimi ❌ ⚠️ e-Arşiv/e-Fatura entegratörü gerekir
- [ ] **P.5** Abonelik yenileme / iptal / askıya alma 🟡
- [ ] **P.6** Ödeme başarısızlığı bildirimi + kurtarma akışı ❌
- [ ] **P.7** Kullanıcı kotası uygulama (plana göre koltuk sınırı) ❌ (Cloud için sonraki açık gereksinim sınırsız kullanıcıdır; bu kota Cloud'a uygulanmaz)
- [ ] **P.8** Disk/kayıt kotası + aşım uyarısı 🟡
- [ ] **P.9** Müşteri alt alan adı (firma.envantertr.com) otomatik açma + SSL 🟡
- [ ] **P.10** Self-servis kurulum sihirbazı (ilk yönetici hesabı) ✅
- [ ] **P.11** Fiyatlandırma sayfası + teklif formu ✅

---

## R. KURULUM, GÜNCELLEME, İŞLETİM

- [ ] **R.1** Windows Server: yayın paketi + servis kurulumu 🟡 prototip
- [ ] **R.2** Windows Server: PostgreSQL hazırlama betiği 🟡 prototip
- [ ] **R.3** Windows Server: açık onaylı migration + dosya geri dönüşü 🟡 prototip
- [ ] **R.4** Windows 11 tek bilgisayar: **imzalı kurulum sihirbazı** ❌
- [ ] **R.5** Ubuntu: Production Compose + Caddy HTTPS ✅ prototip
- [ ] **R.6** Güncelleme öncesi otomatik yedek + doğrulama ✅ prototip
- [ ] **R.7** Gerçek sunucuda uçtan uca kurulum kabulü ❌ ⚠️ Burada simüle edilebilir, gerçek makine gerekir
- [ ] **R.8** Müşteri kurulum kitapçığı / adım adım kılavuz ❌
- [ ] **R.9** Docker'sız Windows kurulum (son kullanıcı Docker görmeyecek) 🟡
- [ ] **R.10** Sağlık izleme + hata bildirimi (webhook/e-posta) ✅ prototip
- [ ] **R.11** Üçüncü taraf lisans bildirimleri (THIRD-PARTY-NOTICES) ✅
- [ ] **R.12** Kod imzalama sertifikası ile imzalı kurulum paketi ❌ ⚠️ Sertifika satın alınmalı
- [ ] **R.13** Otomatik sürüm güncelleme (uygulama içinden) ❌

---

## S. SNIPE-IT KARŞILAŞTIRMASI (senin "eksik" dediğin yer)

Bu bölümü **senin verdiğin eski Snipe-IT ekran görüntüleriyle** (56 adet buldum) madde madde
karşılaştırıp dolduracağım. Şimdilik bilinen başlıklar:

- [ ] **S.1** Snipe-IT'te olup EnvanterTR'de **hiç olmayan** özelliklerin listesi ❌ (çıkarılacak)
- [ ] **S.2** Snipe-IT'te olup EnvanterTR'de **eksik** olan özelliklerin listesi 🟡 (çıkarılacak)
- [ ] **S.3** Snipe-IT'te kötü/dağınık olup EnvanterTR'de **daha iyi** yapılacaklar ❌ (tasarım kararı)
- [ ] **S.4** Snipe-IT'ten **hiçbir kod/isim/görsel** kalmayacak ⚠️ AGPL riski — hukuki görüş gerekli

---

## T. LİSANS VE HUKUK (benim karar veremeyeceğim, senin kararın)

- [ ] **T.1** Diskteki rebrand edilmiş Snipe-IT kopyaları ne olacak? (silinsin mi, arşiv mi?) ⚠️
- [ ] **T.2** AGPL yükümlülüğü için hukuki görüş alınacak mı? ⚠️
- [ ] **T.3** Müşteriye kaynak kodu sunma biçimi (gerekirse) ⚠️
- [ ] **T.4** Fiyatlandırma, vergi, fatura süreçleri ⚠️
- [ ] **T.5** KVKK / veri saklama / silme politikası ⚠️

---

## ÖZET SAYIM (kabaca)

| Bölüm | Toplam madde | ✅ Var | 🟡 Kısmi | ❌ Yok | ⚠️ Karar |
|---|---|---|---|---|---|
| 0. Ürün hatları | 5 | 0 | 4 | 1 | 1 |
| A. Tasarım/Marka/Dil | 29 | 5 | 16 | 6 | 2 |
| B. Giriş/Güvenlik | 11 | 8 | 1 | 2 | 0 |
| C. Varlıklar | 39 | 30 | 5 | 3 | 0 |
| D. Çalışanlar | 19 | 15 | 2 | 2 | 0 |
| E. Zimmet/İade/Transfer | 13 | 10 | 1 | 2 | 1 |
| F. Lisanslar | 13 | 9 | 2 | 2 | 1 |
| G. Aksesuar/Sarf/Bileşen | 18 | 17 | 0 | 1 | 0 |
| H. Bakım/Denetim/Takvim | 11 | 8 | 1 | 2 | 0 |
| I. Barkod/QR/Etiket | 9 | 9 | 0 | 0 | 0 |
| J. Raporlar | 15 | 11 | 0 | 4 | 1 |
| K. Veri/Yedek | 15 | 9 | 3 | 2 | 1 |
| L. Kullanıcı/Rol | 7 | 2 | 4 | 1 | 0 |
| M. Şirket Ayarları | 9 | 4 | 4 | 1 | 0 |
| N. API/Entegrasyon | 10 | 5 | 0 | 5 | 1 |
| O. Platform Yönetimi | 13 | 0 | 11 | 2 | 0 |
| P. Ticari Akış | 11 | 3 | 4 | 4 | 1 |
| R. Kurulum/İşletim | 13 | 1 | 8 | 4 | 2 |
| S. Snipe-IT Karşılaştırma | 4 | 0 | 1 | 3 | 1 |
| T. Lisans/Hukuk | 5 | 0 | 0 | 0 | 5 |

**Toplam ~280 madde.** Kabaca: üçte biri var, üçte biri kısmi, geri kalanı yok veya karar bekliyor.

---

*Bu dosya: `EnvanterTR - deepseek\design\ISTENENLER-LISTESI.md`*
*Kaynaklar: docs/PRODUCT-REQUIREMENTS.md, docs/PRODUCT-COVERAGE.md, envantertr.com/demo/ menüsü, eski oturum notları*
