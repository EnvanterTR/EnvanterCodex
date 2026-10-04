# Ürün kapsamı ve özellik durumu

Bu belge EnvanterTR'nin kurumsal envanter yönetiminde beklenen ana işlevleri ne ölçüde karşıladığını izler. Amaç, her düğmeyi körü körüne kopyalamak değil; aynı işi yapan kurumsal envanter akışlarını, izinleri ve kayıt ilişkilerini eksiltmeden ürün kararlarıyla tamamlamaktır. Ürün gereksinimi olan bir satır, uygulama ve kabul kontrolü yapılana kadar tamamlanmış sayılmaz.

## Durum anahtarı

- **Var:** İşlevin çalışan bir EnvanterTR akışı bulunuyor; bu, üretim kabulünden geçtiği anlamına gelmez.
- **Kısmi:** İlk ekran veya temel akış var; ilişkiler, operasyonlar, kalıcılık ya da kapsamın bir kısmı eksik.
- **Yok:** Uygulanmış müşteri akışı yok.
- **Karar gerekli:** Kaynakta özellik var; ürün gereksinimi, güvenlik veya müşteri ihtiyacı açısından dahil edilip edilmeyeceği kararlaştırılmalı.

## Envanter yönetimi işlev aileleri

| İşlev ailesi | EnvanterTR durumu | Kapsam / eksik iş |
|---|---|---|
| Ana sayfa ve canlı özet | **Kısmi** | Şirket kapsamlı varlık durumları, etkinlik ve temel rapor bağlantıları var. Çoklu grafik/panel seçenekleri ve tüm modül sayaçları yok. |
| Varlık kayıtları ve tabloları | **Kısmi** | Varlık CRUD, arama, filtre, sıralama, sayfalama, sütun tercihi, CSV, detay, geçmiş, zimmet/iade ve durum akışları var. Gelişmiş arama kaydedilmiş sorguları, silinen kayıt geri alma, sipariş ve tüm toplu işlemler eksik. |
| Varlık durum kuyrukları | **Kısmi** | Kullanılabilir, zimmetli, bakım ve arşiv durumları temel düzeyde var. Beklemede, kullanılamaz, BYOD, istenebilir, denetimi/iadesi gelen-geçen gibi ayrı operasyon kuyrukları eksik. |
| Varlık detay ilişkileri | **Kısmi** | Genel bakış, ilişkili kayıt bağlantıları ve gerçek hareket geçmişi var. Lisans, bileşen, bağlı varlık, aksesuar, bakım, denetim, not, dosya ve model dosyası sekmeleri yok. |
| Barkod, QR ve etiketler | **Kısmi** | Tek varlık kartında C128/C39/EAN5/EAN13/UPC-A/UPC-E ve QR/PDF417/Data Matrix; ölçü, X/Y hizası, boşluk ve alan ayarı var. Seçili varlıklara toplu etiket sayfası, şirket sunucu ayarları, mobil kamerayla tarama ve tarayarak zimmet/iade yok. |
| Kişi/çalışan kayıtları | **Kısmi** | Çalışan CRUD, departman filtresi, yönetici ilişkisi, doğrudan raporlayanlar, zimmetli varlıklar ve profil var. Yazılım kullanıcı hesabı ile sadece personel kaydı ayrımının tamamı, lisans/aksesuar/sarf/bakım/file/EULA sekmeleri, hesap durum kuyrukları ve toplu kişi işlemleri eksik. |
| Departmanlar | **Kısmi** | Çalışanlardaki metinden özet ve çalışan listesi filtresi var. Yönetilebilir katalog, yeniden adlandırma/birleştirme ve silme kuralları yok. |
| Konumlar | **Kısmi** | Varlık metninden özet çıkarma ve konumdaki varlıkları filtreleme var. Hiyerarşik konum kataloğu, kullanıcı/varsayılan konum/aksesuar/sarf/bileşen ilişkileri ve yöneticiler yok. |
| Modeller, kategoriler, markalar | **Kısmi** | Temel kataloglar ve varlık formunda seçiciler var. Model ayrıntı sayfasında varlık/sipariş/dosya/geçmiş ilişkileri, toplu yönetim ve gelişmiş alanlar eksik. |
| Lisanslar | **Temel yaşam döngüsü** | Şirket kapsamlı lisans kataloğu, yayıncı/sürüm/tür/referans, koltuk sayısı, aktif çalışana koltuk atama ve geri alma, maliyet/para birimi, alış/bitiş/otomatik yenileme, ayrıntı ve olay geçmişi var. Cihaz ataması, dosyalar, toplu işlem ve bildirim kuralları sonraki kapsam. Ürün anahtarı saklanmaz. |
| Aksesuarlar | **Kısmi** | Şirket kapsamlı katalog, arama/sıralama/sayfalama, düşük stok filtresi, stok girişi/düzeltme, miktarlı çalışan/varlık/konum teslimi, kısmi iade, hareket geçmişi, birim maliyet, etkin arama/stok filtresini koruyan CSV dışa aktarma, tarayıcıda saklanan sütun görünümü ve sayfa başına 12/25/50/100 satır seçimi var; mevcut sayfa yazdırılabilir ve tam ekran açılabilir. Sipariş/dosya sekmeleri, gelişmiş arama, toplu işlem, kategori/tedarikçi katalog ilişkileri ve istek onayı eksik. |
| Sarf malzemeleri | **Temel stok akışı** | Şirket kapsamlı katalog, başlangıç stoğu, stok girişi/sayım düzeltmesi, çalışana tüketim çıkışı, minimum stok uyarısı, arama/sıralama ve hareket geçmişi. Talep onayı ve satın alma siparişi sonraki kapsam. |
| Bileşenler | **Yok** | Miktar/seri takibi, varlığa atanan bileşen, iade/çıkarma, sipariş ve raporlama oluşturulmalı. |
| Kitler | **Yok** | Önceden tanımlı varlık/aksesuar setleri ve kullanılabilirlik/teslim akışı oluşturulmalı. |
| Bakım kayıtları | **Yok** | Bakım türü, tedarikçi, maliyet, başlangıç/bitiş, durum, not/dosya ve varlık geçmişi akışı oluşturulmalı. |
| Denetim ve kabul | **Kısmi** | Hareket/audit kayıtları ve geçmiş görüntüleme var. Varlık sayımı/denetim oturumu, kullanıcı kabulü, hatırlatma, kabul edilmeyen öğe ve sonuç raporları yok. |
| Transfer ve rezervasyon | **Yok** | Konumlar arası transfer, onay/rezervasyon, çakışma ve işlem geçmişi tasarlanmalı. |
| Siparişler ve tedarik | **Yok** | Sipariş, sipariş kalemi, tedarikçi ve teslim alım/varlığa dönüştürme ilişkileri yok. |
| Ek dosya ve fotoğraflar | **Yok** | Şirket logosu yüklemesi, varlık/kişi/modül dosya depolaması yerine geçmez. Erişim, boyut, tür, silme ve yedekleme politikalarıyla eklenmeli. |
| İçe aktarma / dışa aktarma | **Kısmi** | Varlık CSV dışa aktarma ve sınırlı önizlemeli CSV varlık ekleme; şirket JSON dışa aktarma/önizleme var. Modül bazlı import, kolon eşleme, katalog oluşturma, JSON geri yükleme, Excel dosyası ve atomik geri alma eksik. |
| Raporlar | **Kısmi** | Varlık durum/kategori, alış tarihi ve kaydedilmiş maliyet özetleri ile CSV var. Lisans, bileşen, bakım, denetim, teslim alınmamış öğeler, amortisman ve özel rapor oluşturucu eksik. |
| Kullanıcı, roller ve izinler | **Kısmi** | Şirket oturumu ve temel owner/admin/operator rolleri ile şirket üyeleri var. İnce taneli modül/işlem izin matrisi ve her uçta kabul kapsamı tamamlanmalı. |
| Şirket ve tenant ayrımı | **Kısmi** | Çok şirketli kimlik/veri modeli hedefi ve API kapsam denetimleri mevcut. Uçtan uca tenant güvenlik kabulü, plan sınırları ve üretim operasyonu açık. |
| Bildirimler ve e-posta | **Yok** | SMTP teslimatı, parola sıfırlama e-postası, bakım/denetim/iade uyarıları ve kullanıcı bildirim tercihleri henüz yok. |
| API, entegrasyon ve otomasyon | **Kısmi** | Dahili JSON API var. Belgelenmiş müşteri API'si, anahtar yaşam döngüsü, webhook olayları, hız sınırlaması ve entegrasyon yönetimi yok. |
| Kimlik ve oturum güvenliği | **Kısmi** | Parola tabanlı oturum, CSRF ve temel roller var. MFA/kurtarma, LDAP/AD/SSO, oturum denetimi, parola e-postası ve güvenlik kabul matrisi eksik. |
| Dil ve kullanıcı tercihleri | **Kısmi** | Türkçe arayüz ağırlıklı. Tam Türkçe/İngilizce yerelleştirme, tarih/sayı/para biçimleri ve kullanıcı bazlı kalıcı tercihler tamamlanmalı. |
| Kurulum, güncelleme ve yedekleme | **Yok / prototip** | Yerel geliştirici Compose ve elle yedek betikleri var. İmzalı Windows/Linux kurulumu, LAN/HTTPS, otomatik şifreli yedek, geri yükleme ve güncelleme geri alma yok. |
| Platform abonelik ve ticari yönetimi | **Kısmi** | Platform admin temel şirket/plan/abonelik görünümü ve bazı operasyonlar var. Gerçek satın alma, ödeme, fatura, yenileme, mutabakat ve destek iş akışı yok. |

## Öncelik ve tamamlanma ölçütü

1. **Önce güvenli temel:** tenant/rol denetimi, yedekleme-geri yükleme, oturum ve üretim kurulum gereksinimleri.
2. **Sonra envanter yaşam döngüsü:** departman/konum katalogları, ilişki ayrıntıları, transfer/rezervasyon, denetim ve ek dosyalar.
3. **Modül paritesi:** lisans, aksesuar, sarf, bileşen, kit ve bakım modüllerini her biri için liste + filtre/sıralama + detay ilişkileri + oluştur/düzenle + toplu işlem + geçmiş + izin + dışa aktarma ile tamamlamak.
4. **Operasyonel işlevler:** toplu etiket ve tarayıcı iş akışları, modül bazlı içe/dışa aktarma, bildirim, raporlar ve API/webhook.
5. **Ürün teslimatı:** Windows/Linux kurum içi kurulum, barındırılan hizmet, dil ve ticari akışlar.

Bir modül ancak temel liste ve detayın yanında bağlantılı kayıtlar, hata/boş durumları, filtrelerin korunması, yetki kontrolleri, işlem geçmişi ve veri taşıma davranışı gözden geçirildiğinde “Var” durumuna alınır. Kullanıcı tarafından paylaşılan referans ekran görüntüleri etkileşim/tasarım kabul ölçütüdür; yerel kaynak envanteri davranış karşılaştırmasının referansıdır.

## Demo arayüzü gözlemleri (4 Ekim 2026)

Kullanıcı isteğiyle herkese açık bir envanter demosu salt okunur biçimde incelendi. Demo bazı işlevlerin kapalı olduğunu belirtiyor. Bu nedenle gözlem, bütün izin rolleri ve her olası düğme davranışı için tam bir davranış sertifikası değildir; veri değiştirme, silme, içe aktarma ve ayar kaydetme işlemleri çalıştırılmadı.

Gezinme ve ekran incelemesinde doğrulanan kapsam:

- **Gösterge paneli:** altı varlık ailesi için sayaç kartları; yakın etkinlik tablosu ve araçları; gün/sonraki takvim; duruma göre grafik; düşük stok; denetim, iade, talep ve kullanım ömrü uyarıları; konum ve kategori özetleri.
- **Varlık listesi:** tümü, zimmetli, dağıtıma hazır, beklemede, kullanılamaz, BYOD, arşiv, talep edilebilir, denetim/iade zamanı gelen ve silinmiş kuyrukları; filtreleme, sayfa boyutu, sütun/çıktı araçları, gelişmiş arama ve seçili satırlar için toplu işlem. Ayrı akışlarda hızlı taramayla iade, toplu zimmet ve tarayıcıyla toplu denetim alanları var. Seri numarasıyla tarama ayarı benzersizlik yapılandırmasına bağlı.
- **Varlık detayı:** bilgi, lisans, bileşen, bağlı varlık, aksesuar, bakım, denetim, not, dosya, modele ait ek dosya ve geçmiş sekmeleri; durum, son/gelecek iade, denetim, cihaz ömrü, özel model alanları, amortisman, toplam maliyet, QR ve işlem sayacı. Düzenle, kopyala, zimmet, denetim gibi ayrı işlemler bulunuyor.
- **Lisans detayı ve teslimi:** koltuklar, kullanılabilir koltuklar, dosya ve geçmiş sekmeleri; toplam/kalan koltuk, ürün anahtarı, süre sonu, tedarikçi/üretici, birim maliyet ve istek/yeniden atama durumu. Koltuk kullanıcıya veya varlığa atanabiliyor; not, yerinde EULA kabulü, e-posta bildirimleri ve geri dönüş hedefi seçenekleri var. Demo ayarları EULA/e-posta işlemlerinin gerçek teslimini kısıtlayabilir.
- **Stok aileleri:** aksesuar, sarf ve bileşen listelerinde modüle göre değişen model/ürün kodu, konum, son alış tarihi, minimum/toplam/kalan miktar, zimmet, yüzde, son birim maliyeti, sipariş numarası ve toplam maliyet; satırda klonla, düzenle, adet düzelt, teslim/iade ve (izin varsa) sil işlemleri; liste sütunları/çıktı, gelişmiş arama ve seçili satır toplu işlemleri. Aksesuar detayında QR ve Checked Out, Files, Orders, History sekmeleri; sipariş tablosu alış tarihi/numarası/tedarikçi/adet/birim ve toplam maliyet/not/dosya alanlarını içerir. Teslim formu kullanıcı/varlık/konum seçer, miktar ve not alır, kullanıcıda EULA ve e-posta uyarıları ile yerinde kabul seçeneği gösterebilir; iade formu tarih ve not kaydeder. Sarf/bileşen detayları da ilgili teslim ve geçmiş ilişkilerini taşır.
- **Kişiler:** tüm kullanıcılar, süper kullanıcılar, yöneticiler, silinmiş, oturumu açık/kapalı filtreleri; kullanıcı profilinde varlık/lisans/aksesuar/sarf/bakım/dosya/EULA/konum/yönetilen kullanıcı/geçmiş ilişkileri. Profil ayrıca giriş ve MFA durumu, cihaz kaydı, API token sayısı, otomatik lisans atama ve rol/izin özetlerini gösteriyor.
- **Takvim ve raporlar:** denetim, beklenen iade, zimmet, varlık ömrü, garanti, lisans ve işten ayrılma etkinlikleri; gün/hafta/ay/liste görünümü. Rapor menüsünde etkinlik, varlık/bileşen/sarf, denetim, amortisman, lisans, bakım ve kabul edilmeyen öğe raporları yer alıyor.
- **Ayarlar ve kataloglar:** özel alan kümeleri, model/kategori/marka/tedarikçi/departman/konum/şirket/amortisman katalogları; durum etiketleri; otomatik varlık etiketi için sıradaki numara, isteğe bağlı önek ve sıfır doldurma uzunluğu; yeni etiket motoru ve 33 seçenekli yazıcı/kağıt şablon tablosu. Demo özel alan listesinde 21 alan ve iki küme görünüyor; metin, onay kutusu, radyo, markdown, tarih/saat, şifreli ve düzenli ifade ile doğrulanan alan örnekleri model kümelerine bağlanmış. Etiket ayarı şablon ölçüsü/adedi, 1D/2D kod, şirket logosu, başlık, 2D öneki/hedefi, üst boş satır ofseti ve yeniden sıralanabilir alan etiketi/veri kaynağı seçeneklerini içeriyor; canlı önizleme ve kaydetme adımı var. Yerelleştirme ekranında varsayılan dil, ad biçimi, tarih/saat, haftanın ilk günü, para birimi ve basamak ayıracı ayarlanıyor. Bildirim ayarlarında üst menü uyarıları ve e-posta, stok eşiği, lisans süresi eşiği, iade/denetim uyarı günleri ve denetim aralığı yapılandırılıyor. Deneysel eşitleme bağdaştırıcıları 20 harici cihaz yönetim sağlayıcısını destekliyor; yapılandırma varlıkları toplu oluşturup güncelleyebildiği için yedek ve şirket kapsamı uyarıları içeriyor. Bildirim, güvenlik ve kimlik sağlayıcı ayarları ayrı ekran ve izin yüzeyleridir.
- **İçe aktarma, kit, talep ve bakım:** içe aktarma ekranında kişi/varlık/lisans/aksesuar/sarf için yüklenmiş örnek CSV kayıtları ve geçmiş görünür; demo kurulumunda import gönderimi kapalıdır. Kit listesi bu demoda boş. Bekleyen talep tablosunda görsel, öğe, konum, beklenen iade, kalan/adet, başlangıç-bitiş, talep eden ve tarih alanları bulunur; talep edilebilir varlık görünümünde varlık grupları ve gelişmiş liste araçları vardır. Bakım listesi tür, varlık, durum, kullanıcı, konum, başlangıç/tamamlanma tarihi, URL, not, garanti ve maliyeti gösterir; tamamlandı işaretleme için toplu işlem vardır.

Bu gözlemler ürün fark listesini zenginleştirir. Henüz uygulamaya alınmamış bir aileyi tamamlanmış saymaz ve 8.8.0 demosunun kapalı düğmelerini gözlemlemiş gibi göstermez.

## Kaynaklar ve denetim sınırı

- Yerel referans kopyası: ürün kapsamını izlemek için kullanılan kaynak envanteri; uygulama deposunun dışında tutulur.
- EnvanterTR durumu: `ROADMAP.md`, `PRODUCT-REQUIREMENTS.md`, mevcut API uçları ve `src/EnvanterTR.Web/wwwroot/index.html`.
- Bu ilk karşılaştırma ürün/modül düzeyindedir; referans üründeki her butonun her izin kombinasyonundaki davranışını henüz tek tek doğruladığını iddia etmez. Her modül geliştirildikçe kaynak route/controller/table/policy ile kabul senaryoları bu matrise işlenmelidir.
