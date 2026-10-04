# EnvanterTR ürün durumu ve öncelik sırası

Bu dosya, ürün kararlarını ve teslim edilmiş parçaları tek yerde tutar. Yerel geliştirme önizlemesi, müşteri kullanıma hazır ürün anlamına gelmez. **Hazır** yazan maddeler yalnızca mevcut kod kapsamını belirtir; tüm uçtan uca kabul ve güvenlik doğrulamaları tamamlanmış sayılmaz.

## Şu an kullanılabilir geliştirme parçaları

- Şirket hesabı ve rol tabanlı oturum; varlık, çalışan, kategori, marka ve model kayıtları.
- Varlık arama, filtreleme, sıralama, sayfalama, şirket kapsamlı ve seçili kayıt CSV dışa aktarımı; en fazla 100 seçili varlığın güvenli toplu durum değişikliği. Kullanıcı sütun, satır aralığı ve sıralama görünümünü şirket ve tarayıcı bazında saklayabilir. Model/marka/çalışan/konum ilişki filtreleri ayrı ayrı veya birlikte kaldırılabilir; zimmet, iade, durum ve hareket geçmişi.
- Varlık ve çalışan tablolarında ad bağlantıları klavyeyle erişilebilir; ad seçildiğinde ilgili ayrıntı profili açılır, model/departman gibi ilişki bağlantıları kendi filtre akışını korur. Hızlı aramada geciken eski istek yanıtları yeni listenin üzerine yazılmaz.
- Varlık detayında genel bakış, bağlantılı kayıtlar ve gerçek işlem geçmişi; etiket kopyalama ve QR/PDF417/Data Matrix ile C128/C39/EAN5/EAN13/UPC-A/UPC-E seçilebilir yazdırılabilir varlık kartı. Etiket boyutu, X/Y hizası, iç boşluk ve gösterilen alanlar ayarlanır; QR varlık etiketi ya da oturum sonrası varlık ayrıntısı bağlantısı taşır.
- Dashboard, canlı şirket varlık durumlarını erişilebilir halka grafik ve tıklanabilir filtrelerle özetler; ayrıntılı hazır rapor ekranına geçiş sağlar.
- Çalışanlarda departman filtreleme, isteğe bağlı yönetici ilişkisi, doğrudan raporlayanlar ve zimmetli varlıkları gösteren profiller. Yönetici döngüleri ve başka şirkete ait yönetici seçimi sunucuda engellenir.
- Departman ve konum ekranları şirket verisinden özet çıkarır; departman çalışanlarını ve konumdaki varlıkları tek tıkla filtreler. Departman özetleri baştaki/sondaki boşluk ve harf büyüklüğü farklarını birleştirir; bölüm filtresi büyük/küçük harfe duyarsız eşleşir. Değerler hâlâ serbest metinden türetildiği için katalog yönetimi ve güvenli yeniden adlandırma sonraki adımdır.
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
- Veritabanı ve şirket medyası için otomatik, şifreli yedek; geri yükleme önizlemesi, güvenli geri alma ve düzenli geri yükleme tatbikatı. Bugünkü elle yerel yedek betikleri bu gereksinimi karşılamaz.
- Özel `EnvanterTR/EnvanterCodex` deposu kaynak kodun yayımlama hedefidir; yerel commit'ler GitHub Desktop üzerinden uzak `main` dalıyla eşitlenir. Yayımdan önce dal ve çalışma ağacı durumu kontrol edilmelidir.
- Lisans ve üçüncü taraf bağımlılık envanteri yayıma çıkmadan önce gözden geçirilmeli; ürün adı ve görünümü, lisans/attribution yükümlülüklerini gizlemek için kullanılmamalı.

### P1 — Kurum içi kurulum ve ağ erişimi

- Windows 11 ve Windows Server için imzalı, ileri-ileri kurulum sihirbazı ve servis yönetimi.
- Ubuntu/Linux için tekrarlanabilir kurulum ve güncelleme betiği.
- Ana bilgisayarın LAN adresi, DHCP değişimi veya sabit IP/DNS seçeneği, HTTPS, güvenlik duvarı kuralı ve istemci tarayıcı bağlantısı. Geliştirme Compose ayarı yalnızca localhost'a bağlıdır; şirket ağına açılmamalıdır.
- Güncelleme/kurulum sırasında veritabanını veya dosyaları silmeme, önce yedek alma ve geri alma prosedürü.

### P1 — Envanter iş akışlarının kapsamı

- Varlık fotoğrafı ve ek dosyalar; telefon kamerasıyla QR/barkod okutma, seçili varlıkları toplu etiketleme ve şirketler arasında eşitlenen kalıcı yazdırma tercihleri. Tek varlık için dokuz yaygın barkod türü ve ölçü/hizalama/alan ayarlı kart yazdırma eklendi; tercihler şimdilik yerel tarayıcıda tutulur.
- Aksesuar modülünde sipariş/dosya ilişkileri, katalog bağlantıları, sütun/çıktı/toplu işlem ve isteğe bağlı kullanıcı akışları; ayrıca lisans, bileşen, sarf malzemesi, kit, transfer, rezervasyon, bakım, denetim ve garanti kayıtları.
- Departman ve konum için yapılandırılmış kataloglar ve ad birleştirme/düzeltme akışı. Çalışan departmanı ve varlık konumu bugün serbest metindir; mevcut ekranlar yalnızca özetler ve filtreler.
- CSV varlık biçimi için salt okunur sütun/çakışma önizlemesi ve mevcut katalog kayıtlarını kullanan sınırlı, atomik kayıt ekleme eklendi; Excel içe aktarma, alan eşleme, katalog kaydı oluşturma ve güvenli JSON geri yükleme akışı tamamlanmalı.
- Raporlarda maliyet denetimi ve daha ayrıntılı ölçütler; ayrıca gerçek veriden beslenen takvim. Mevcut rapor durum/kategori adetleri, alış tarihi aralığı ve kayıtlı alış bedeli özetidir; muhasebe değerlemesi değildir.

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
- Etiket kartı etiket motorunda sunulan 1D (C128, C39, EAN5, EAN13, UPCA, UPCE) ve 2D (QR Code, PDF417, Data Matrix) biçim seçeneklerini karşılar. Çalışan telefonunda kamera ile okutma, toplu etiket sayfaları/şablonları ve sunucuya kaydedilen şirket düzeyi ayarlar henüz yoktur.
- Şirket logosu yükleme vardır; varlık fotoğrafı ve ek dosya saklama henüz yoktur.
- JSON dosyası önizlemesi vardır; gerçek içe alma/geri yükleme henüz yoktur.
- `docs/ARCHITECTURE.md`, `docs/DATA-PORTABILITY.md` ve `docs/PLATFORM-ADMIN.md` daha ayrıntılı teknik kapsamı tutar.
