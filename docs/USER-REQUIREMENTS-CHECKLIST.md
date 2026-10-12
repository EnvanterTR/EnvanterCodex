# Dünden gelen istekler ve kontrol listesi

**Kullanıcının başlangıç listesi:** Alttaki 0–T bölümleri ve durum işaretleri, kullanıcının gönderdiği notlardan aynen korunmuştur. İşaretler bağımsız olarak doğrulanmış güncel sonuç değildir.

## 11 Ekim 2026 — şu anki kontrol fotoğrafı

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
 B.8 Kendi profil fotoğrafı yükleme ✅
 B.9 LDAP / Active Directory / SSO ile giriş ❌
 B.10 Oturum açan cihazların listesi (nereden girildi) ❌
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
 C1.10 Yazdırılabilir liste / tam ekran görünüm 🟡 (tam ekran, seçili sütunlar ve tüm filtreli sayfalar yazdırılıyor; gerçek tarayıcı görsel kabulü açık)
 C1.11 Excel (.xlsx) dışa aktarma ✅
 C1.12 Toplu işlem: en fazla 100 kayıtta durum değiştirme / zimmet / iade / transfer / denetim ✅
 C1.13 Satır sonu hızlı eylem ikonları (detay, zimmet, iade, denetim, etiket, kopyala, düzenle, arşiv) ✅
 C1.14 BYOD (kendi cihazını getir) ayrı alan ✅
 C1.15 İstenebilir (requestable) varlık işaretleme ✅
 C1.16 Özel alanlar: metin, sayı, tarih, onay kutusu, seçenek listesi ✅
 C1.17 Özel alanlarda şifreli/hassas alan türü ✅ — ASP.NET Data Protection ile alan ve şirket kapsamlı şifreleme; yalnız şirket sahibi/yöneticisi okuyabilir ve değiştirebilir. Liste/ayrıntı, CSV/XLSX aktarım, toplu işlem, filtre ve özel rapor sınırları doğrulanır; hassas değerler dışa aktarımlara yazılmaz. Anahtar volume yedek kapsamındadır.
 C1.18 Model/kategori bazlı alan grupları ❌
 C1.19 Varlık durumu etiketlerinin adını şirket özelleştirebilsin ✅
 C1.20 Yeni durum türleri ve durum geçiş kuralları ❌
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
 C2.9 Dosya/geçmiş zaman çizelgesi görünümü 🟡
 C2.10 Varlık yaşı göstergesi (alış tarihinden hesaplanır) ✅
 C2.11 Amortisman / mevcut değer gösterimi (liste ve varlık ayrıntısında model politikasından hesaplanır; veri eksikse hesaplanamaz) ✅
C3. Sipariş ve satın alma (varlık)
 C3.1 Çok kalemli varlık satın alma siparişi ✅
 C3.2 Tedarikçi, tarih, para birimi, vergi, iskonto, nakliye ✅
 C3.3 Kısmi teslim alma → otomatik varlık kartı/etiketi oluşturma ✅
 C3.4 Sipariş iptali + denetim izi ✅
 C3.5 Sipariş belgeleri (PDF/PNG/JPEG/WebP, 30 dosya) ✅
 C3.6 Sipariş ↔ cihaz profili iki yönlü geçiş ✅
 C3.7 Onay ve bütçe iş akışı ❌
C4. Durum kuyrukları
 C4.1 Kullanılabilir / Atanmış / Beklemede / Bakımda / Kullanılamaz / Arşiv ✅
 C4.2 Arşivden kullanılabilire geri alma (kilitli + denetim izli) ✅
 C4.3 Beklemede ve kullanılamaz kuyrukları ✅
 C4.4 Denetimi gelen/geçen ayrı kuyruklar 🟡
 C4.5 İadesi gelen/geciken ayrı kuyruklar 🟡
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
 D.11 CSV dışa aktarma (yeniden içe alınabilir) ✅
 D.12 Departman kataloğu (ana/alt, kat, tam yol) ✅
 D.13 Departman pasife alma + çalışanları kademeli taşıma ❌
 D.14 Departman yetkilisi/ekip yöneticisi atama ❌
 D.15 Şirket üyelikleri ekranı (hesap ↔ çalışan bağı) ✅
 D.16 Konum kataloğu: bina → kat → oda → kabin (6 seviye) ✅
 D.17 Konum varsayılanı + konum sorumlusu ✅
 D.18 Konum bazlı erişim yetkileri ❌
 D.19 Şirket birimleri (tüzel kişilik/şube ayrımı) 🟡
E. ZİMMET, İADE, TRANSFER, REZERVASYON
 E.1 Varlık zimmetleme (tekli) ✅
 E.2 Toplu zimmet (100 varlık, atomik) ✅
 E.3 İade alma (tekli + toplu, kısmi iade) ✅
 E.4 Zimmet kuyruğu: arama, sayfalama, geciken/30 gün/tarihsiz/ileri tarih filtreleri ✅
 E.5 Yazdırılabilir zimmet formu (çalışan + üzerindeki varlıklar + tarih) ✅
 E.6 Dijital imza (ekranda imza atma) + PDF çıktı ❌ ⚠️ (eski isteğin)
 E.7 EULA/kabul belgesi zimmete bağlama + SHA-256 sabitleme 🟡 (lisans var, zimmete yayılmamış)
 E.8 Konum transferi (tekli + toplu 100) ✅
 E.9 Rezervasyon: talep → onay/red → iptal, çakışma engeli ✅
 E.10 Rezervasyon zimmete dönüşünce otomatik tamamlandı ✅
 E.11 Hareket geçmişi: tür/işlem/tarih filtreleri + 10.000 satır CSV ✅
 E.12 Çalışan self-servis talebi (kendi adına) ✅
 E.13 Zamanlı hatırlatma akışı (iade gecikmesi vb.) ❌
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
 F.10 Sözleşmeler modülü (contract) ❌ menüde var, arkası boş
 F.11 Garanti takibi modülü ✅ (rapor olarak)
 F.12 Lisans ↔ tedarikçi/sipariş bağlantısı 🟡
 F.13 Yazılım envanteri (kurulu uygulamaları cihazdan toplama) ❌ ⚠️ Ajan gerekir
G. AKSESUAR, SARF, BİLEŞEN, KİT
 G.1 Aksesuar kataloğu + model + marka + kategori + ürün fotoğrafı ✅
 G.2 Aksesuar stok girişi/çıkışı, düşük stok uyarısı ✅
 G.3 Aksesuar teslimi: çalışan/varlık/konum, miktarlı ✅
 G.4 Toplu teslim/iade (100 kayıt, değişken adet) ✅
 G.5 Aksesuar CSV içe/dışa aktarma + şablon ✅
 G.6 Aksesuar siparişleri (kısmi teslim, iptal, finans alanları, belgeler) ✅
 G.7 Hazır aksesuar kitleri (stoktan üretim, toplu teslim, geçmiş) ✅
 G.8 Karma kitler (aksesuar + sarf + bileşen birlikte) ❌
 G.9 Varlık kalıpları (kit ile birlikte cihaz) ❌
 G.10 Sarf malzemesi kataloğu + stok + çalışana tüketim çıkışı ✅
 G.11 Sarf minimum stok uyarısı ✅
 G.12 Sarf CSV içe/dışa aktarma ✅
 G.13 Bileşen (RAM, disk vb.) kataloğu + seri takibi ✅
 G.14 Bileşeni varlığa takma/çıkarma (miktarlı) ✅
 G.15 Bileşen toplu sayım ve toplu takma (100) ✅
 G.16 Bileşen CSV içe/dışa aktarma ✅
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
 H.10 Otomatik e-posta hatırlatmaları (bakım/denetim/garanti/lisans) 🟡
 H.11 Denetim sonucu "kabul edilmeyen öğe" akışı ❌
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
 J.14 Zamanlanmış rapor e-postası ❌
 J.15 PDF rapor çıktısı (CSV dışında) ❌
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
 M.5 Entegrasyonlar (webhook, dış sistem) ❌ menüde var, arkası boş
 M.6 Güvenlik ayarları (parola politikası, oturum süresi, MFA zorunluluğu) 🟡
 M.7 Denetim kayıtları (kim ne zaman ne yaptı, filtrelenebilir + CSV) ✅
 M.8 Yerel ayar: tarih/saat biçimi, para birimi, sayı biçimi ✅
 M.9 Şirket ayarlarını dışa/içe aktarma ✅
N. API VE ENTEGRASYON
 N.1 Sürümlü müşteri API v1 (salt okunur: varlık, aksesuar, çalışan) ✅
 N.2 API anahtarı oluştur/listele/iptal (SHA-256 özeti saklanır) ✅
 N.3 OpenAPI 3.1 tanımı yayında ✅
 N.4 IP başına hız limiti ✅
 N.5 API ile yazma (varlık oluştur/güncelle) ❌
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
 P.1 Self-servis kayıt (signup) 🟡 (sayfa var, arkası yok)
 P.2 Plan seçimi + ücretsiz deneme (14/30 gün) 🟡
 P.3 Kredi kartı ile ödeme (iyzico) ❌ ⚠️ Başvuru + API anahtarı gerekir
 P.4 Otomatik fatura üretimi ❌ ⚠️ e-Arşiv/e-Fatura entegratörü gerekir
 P.5 Abonelik yenileme / iptal / askıya alma 🟡
 P.6 Ödeme başarısızlığı bildirimi + kurtarma akışı ❌
 P.7 Kullanıcı kotası uygulama (plana göre koltuk sınırı) ❌
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
