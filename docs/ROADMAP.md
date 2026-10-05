# EnvanterTR ürün durumu ve öncelik sırası

Bu dosya, ürün kararlarını ve teslim edilmiş parçaları tek yerde tutar. Yerel geliştirme önizlemesi, müşteri kullanıma hazır ürün anlamına gelmez. **Hazır** yazan maddeler yalnızca mevcut kod kapsamını belirtir; tüm uçtan uca kabul ve güvenlik doğrulamaları tamamlanmış sayılmaz.

## Şu an kullanılabilir geliştirme parçaları

- Şirket hesabı ve rol tabanlı oturum; varlık, çalışan, kategori, marka ve model kayıtları.
- Varlık arama, filtreleme, sıralama, sayfalama, şirket kapsamlı ve seçili kayıt CSV dışa aktarımı; en fazla 100 seçili varlığın güvenli toplu durum değişikliği. Kullanıcı sütun, satır aralığı ve sıralama görünümünü şirket ve tarayıcı bazında saklayabilir. İlişki filtreleri ayrı ayrı veya birlikte kaldırılabilir; garanti durumu (süresi dolmuş, 30 gün içinde dolacak, daha uzun süre etkin veya tarihi girilmemiş) filtrelenir ve CSV'ye uygulanır; zimmet, iade, durum ve hareket geçmişi.
- Varlık ve çalışan tablolarında ad bağlantıları klavyeyle erişilebilir; ad seçildiğinde ilgili ayrıntı profili açılır, model/departman gibi ilişki bağlantıları kendi filtre akışını korur. Hızlı aramada geciken eski istek yanıtları yeni listenin üzerine yazılmaz.
- Varlık detayında genel bakış, bağlantılı kayıtlar ve gerçek işlem geçmişi; etiket kopyalama ve QR/PDF417/Data Matrix ile C128/C39/EAN5/EAN13/UPC-A/UPC-E seçilebilir yazdırılabilir varlık kartı. Hazır 50×30, 60×40, 70×35, 86×48 ve 100×50 mm ölçüleri veya özel ebat seçilebilir; X/Y hizası, iç boşluk ve gösterilen alanlar ayarlanır. QR varlık etiketi ya da oturum sonrası varlık ayrıntısı bağlantısı taşır.
- Dashboard, canlı şirket varlık durumlarını erişilebilir halka grafik ve tıklanabilir filtrelerle özetler; ayrıntılı hazır rapor ekranına geçiş sağlar.
- Çalışanlarda departman filtreleme, isteğe bağlı yönetici ilişkisi, doğrudan raporlayanlar ve zimmetli varlıkları gösteren profiller. Yönetici döngüleri ve başka şirkete ait yönetici seçimi sunucuda engellenir.
- Departman ve konum ekranları şirket verisinden özet çıkarır; form alanlarında mevcut şirket departmanları ve konumları önerilir, yeni serbest metin değeri de girilebilir. Ekranlar departman çalışanlarını filtreler, konumdaki her varlık ailesi için aileye özel arama ile açma düğmesi verir. Departman özetleri baştaki/sondaki boşluk ve harf büyüklüğü farklarını birleştirir; yeniden adlandırma eşleşen şirket çalışanlarını transaction ve denetim iziyle günceller. Konum özeti varlık, aksesuar, sarf ve bileşen stoklarını kapsar; yeniden adlandırma bu dört ailedeki seçili konum metnini tek transaction içinde günceller. Hedefte kayıt varsa ekranda onay verilmeden birleştirilmez. Değerler hâlâ serbest metinden türetildiği için yapılandırılmış katalog, bina/oda hiyerarşisi ve kayıtsız konumları yönetme sonraki adımdır.
- Aksesuar stoklarında şirket kapsamlı kayıt, arama/sıralama/sayfalama, düşük stok filtresi, stok girişi/düzeltmesi, miktarlı çalışan/varlık/konum teslimi, kısmi iade, açık teslim listesi, son 50 stok/teslim hareketi, etkin filtre/sıralamayı koruyan CSV dışa aktarma, tarayıcıda saklanan sütun görünümü ve sayfa başına 12/25/50/100 satır seçimi var. Geçerli tablo sayfası yazdırılabilir ve tam ekran açılabilir. Sipariş/dosya sekmeleri, katalog ilişkileri, toplu işlemler ve sarf/bileşen modülleri henüz tamamlanmadı.
- Henüz işlevi bağlanmayan menüler dashboard görünümünü yanlışlıkla göstermiyor; bu önizlemede nelerin hazır olmadığını ve yakın kullanılabilir ekranları açıklıyor.
- Hazır raporlar ekranı şirket varlıklarından durum/kategori sayılarını ve kaydedilmiş alış bedellerini üretir; alış tarihi aralığı rapor ekranında ve CSV'de korunur. Excel uyumlu CSV indirilebilir. Para birimleri ayrı kalır, kategori/durum satırları varlık listesine filtre uygular.
- Şirket logosu/ana rengi, şirket JSON dışa aktarımı ve JSON/EnvanterTR CSV için salt okunur dosya önizlemesi. EnvanterTR CSV dosyası ayrıca mevcut kategori/marka/model/çalışan kayıtlarıyla eşleştirilerek en fazla 5.000 yeni varlığı tek işlem içinde içe aktarabilir; şirket JSON geri yüklemesi yoktur.
- Yerel Compose önizlemesi için Windows PowerShell ve Linux Bash ile elle veritabanı + şirket medya yedeği ve SHA-256 dosya özetleri. Çıktı şifrelenmez, otomatik alınmaz ve uygulamadan geri yüklenemez; müşteri/üretim yedeklemesi sayılmaz.
- Web yanıtlarında `nosniff`, iframe engeli ve sınırlı referrer/izin politikaları; API yanıtlarında önbelleği kapatma; şirket logo yüklemesinde istek gövdesi boyut sınırı.
- Platform yönetici ekranı ve tanıtım sayfası için metin/paket taslakları.
- Windows makinede Docker ile çalışan **geliştirici önizlemesi**: `http://localhost:8080/`. Bu kurulum müşteri kurucusu değildir ve başka bilgisayarlara ağ erişimi açmaz.

## Öncelikli tamamlanmamış işler

### P0 — Güvenli geliştirme ve sürüm temeli

- Kimlik doğrulamalı iş akışlarının kabul doğrulaması; şirketler arası veri erişimi, izinler, eşzamanlı değişiklikler ve parola yenileme için güvenlik incelemesi.
- Veritabanı ve şirket medyası için otomatik, şifreli yedek; geri yükleme önizlemesi, güvenli geri alma ve düzenli geri yükleme tatbikatı. Elle yerel yedeklere sonradan SHA-256, tar ve PostgreSQL döküm kataloğu doğrulama aracı eklendi; bu araç şifreleme/otomatik yedekleme/geri yükleme gereksinimlerini karşılamaz.
- Özel `EnvanterTR/EnvanterCodex` deposu kaynak kodun yayımlama hedefidir; doğrulanan yerel değişiklikler `origin/main` dalına gönderilir. Yayımdan önce dal ve çalışma ağacı durumu kontrol edilmelidir.
- Lisans ve üçüncü taraf bağımlılık envanteri yayıma çıkmadan önce gözden geçirilmeli; ürün adı ve görünümü, lisans/attribution yükümlülüklerini gizlemek için kullanılmamalı.

### P1 — Kurum içi kurulum ve ağ erişimi

- Windows 11 ve Windows Server için imzalı, ileri-ileri kurulum sihirbazı ve servis yönetimi.
- Ubuntu/Linux için tekrarlanabilir kurulum ve güncelleme betiği.
- Ana bilgisayarın LAN adresi, DHCP değişimi veya sabit IP/DNS seçeneği, HTTPS, güvenlik duvarı kuralı ve istemci tarayıcı bağlantısı. Geliştirme Compose ayarı yalnızca localhost'a bağlıdır; şirket ağına açılmamalıdır.
- Güncelleme/kurulum sırasında veritabanını veya dosyaları silmeme, önce yedek alma ve geri alma prosedürü.

### P1 — Envanter iş akışlarının kapsamı

- Varlık fotoğrafı ve ek dosyalar; çoklu okutmayla toplu zimmet/iade ve yazıcı/kağıt şablon kataloğu. Kamera veya USB okuyucuyla kayıt açma ve seçili kayıt için yetki kontrollü zimmet/iade akışına geçiş; tek varlık ve en fazla 100 seçili varlık için dokuz yaygın barkod türü, hazır ölçü seçenekleri, özel ebat/hizalama/alan ayarlı kart yazdırma eklendi. Etiket ayarları şirket düzeyinde saklanıp üyeler arasında eşitlenir, yalnızca tablo görünümü tercihleri tarayıcıda kalır.
- Aksesuar modülünde sipariş/dosya ilişkileri, katalog bağlantıları, sütun/çıktı/toplu işlem ve isteğe bağlı kullanıcı akışları; ayrıca lisans, bileşen, sarf malzemesi, kit, transfer, rezervasyon, bakım, denetim ve garanti kayıtları.
- Departman ve konum için yapılandırılmış kataloglar, bina/oda hiyerarşisi ve kayıtsız konumları yönetme akışı. Çalışan departmanı ve varlık konumu bugün serbest metindir; özetleme, filtreleme ve kullanımda olan adları şirket kapsamlı/denetimli yeniden adlandırma desteklenir.
- CSV varlık biçimi için salt okunur sütun/çakışma önizlemesi ve mevcut katalog kayıtlarını kullanan sınırlı, atomik kayıt ekleme eklendi; Excel içe aktarma, alan eşleme, katalog kaydı oluşturma ve güvenli JSON geri yükleme akışı tamamlanmalı.
- Bakım, lisans ve varlık garanti bitişlerini şirket verisinden besleyen; liste, gün, hafta ve ay görünümleri, dönem gezinmesi ve CSV dışa aktarımı içeren takvim eklendi. Denetim/iade/zimmet/varlık ömrü etkinlikleri ve hatırlatmalar hâlâ kapsamda; raporlar da maliyet denetimi ve ayrıntılı ölçütlerle genişletilmeli. Mevcut rapor durum/kategori adetleri, alış tarihi aralığı ve kayıtlı alış bedeli özetidir; muhasebe değerlemesi değildir.

### P1 — Barındırılan hizmet ve ticari akışlar

- Şirket alt alan adıyla ayrılmış müşteri portalları; şirket logosu/teması, kullanıcı rolleri, varlık ve giriş kullanıcı sınırlarının aboneliğe göre uygulanması.
- Deneme açma, çevrim içi ödeme/tahsilat, fatura, yenileme/iptal, ödeme mutabakatı ve satın alma akışları. Mevcut platform plan ekranı tek başına satış veya ödeme sistemi değildir.
- Lisans süresi, çevrim dışı tolerans ve yenileme uyarıları; cihaz saati ve yetkisiz değişikliğe dayanıklı lisans tasarımı.
- Platform yöneticisinde gerçek ödeme/satış raporları, müşteri desteği, demo yönetimi ve denetim.

### P2 — İçerik, yardım ve işletim

- Profesyonel ürün sitesi, ekran görüntüsü/video içerikleri, doğru paket gereksinimleri ve satın alma bilgilendirmeleri.
- Modern kurulum/kullanım kılavuzları, veri taşıma ve yönetici görevleri; Türkçe ve İngilizce ilk dalga.
- SMTP ile teslim edilebilir parola yenileme, destek e-postası ve onaylı WhatsApp iletişim kanalı.
- MFA/kurtarma, oturum denetimi, loglama, izleme ve üretim operasyon prosedürleri.

## Bilinçli ürün sınırları

- Müşteri kaynak deposu ile müşterinin indireceği kurulum paketi ayrı teslim kanallarıdır. Kurulum paketi henüz üretilmiyor veya imzalanmıyor.
- Etiket kartı C128/C39/EAN5/EAN13/UPC-A/UPC-E ve QR/PDF417/Data Matrix biçimlerini, 50×30 ile 100×50 mm arasındaki beş hazır ölçüyü, özel ebatları, hizalamayı ve şirket düzeyinde eşitlenen ayarları destekler. Kamera ile kayıt bulma ve en fazla 100 seçili varlığı yazdırma vardır. Yazıcı/kağıt kataloğundan çoklu etiket sayfası düzeni ve seri okutmayla toplu zimmet/iade henüz yoktur.
- Şirket logosu yükleme vardır; varlık fotoğrafı ve ek dosya saklama henüz yoktur.
- JSON dosyası önizlemesi vardır; gerçek içe alma/geri yükleme henüz yoktur.
- `docs/ARCHITECTURE.md`, `docs/DATA-PORTABILITY.md` ve `docs/PLATFORM-ADMIN.md` daha ayrıntılı teknik kapsamı tutar.
