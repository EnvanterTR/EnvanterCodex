# Ürün kapsamı

## Ürün hedefi

Şirketlerin varlık envanterini tutabileceği, varlıkları kişilere ve konumlara atayabileceği, geçmiş hareketleri inceleyebileceği ve verilerini dışa aktarıp geri alabileceği bir web uygulaması sunmak.

## Kullanım biçimleri

1. **Barındırılan hizmet:** EnvanterTR platform yöneticisi müşteri şirketlerini ve abonelik durumlarını yönetir. Her müşteri kendi şirket alanında kullanıcılarını, rollerini ve verilerini yönetir. Bir şirket diğer şirketlerin kayıtlarına erişemez.
2. **Kurum içi Windows kurulumu:** Bir kolay kurulum sihirbazı tek ana bilgisayarda uygulama servisini ve veritabanını kurar. Aynı LAN'daki kullanıcılar ana bilgisayar adını veya IP adresini tarayıcıya yazar. Docker veya ayrı istemci veritabanları gerekmemelidir.
3. **Kurum içi Linux kurulumu:** Ubuntu/Linux sunucusunda servis ve veritabanı paketleriyle kurulum; kullanıcı erişimi yine tarayıcı üzerinden.

## Ürün modülleri

- Varlıklar, varlık modelleri, markalar ve kategoriler.
- Çalışanlar, departmanlar, yönetici ve doğrudan raporlayan ilişkileri; şirketler ve konumlar.
- Zimmet, iade, transfer, rezervasyon ve hareket geçmişi.
- Lisanslar, bileşenler, sarf malzemeleri ve aksesuarlar.
- Bakım kayıtları, denetimler, bildirimler ve takvim.
- Rol/izin yönetimi, içe aktarma, dışa aktarma, raporlar ve API.
- QR/barkod etiketi üretimi ve tarama.
- Firma logosu, renkleri, dili ve temel görünüm tercihleri.
- Yedekleme, doğrulama, indirme ve geri yükleme.
- Platform yönetimi: müşteri, plan, abonelik, tahsilat durumu ve destek talepleri.

## Yetki ilkeleri

- Platform yöneticisi müşteri ve abonelik yönetir; müşteri varlık verilerine varsayılan olarak erişmez.
- Şirket yöneticisi kendi şirketindeki kullanıcı ve rolleri yönetir.
- Operatör hesapları envanter kayıtlarını yönetir; çalışan/personel kayıtları yazılım kullanıcı hesabı açılmadan tutulabilir.
- Çalışan hesabı gerekiyorsa yalnızca kendisine tanımlanan işlemleri görür.
- Tüm tenant erişimleri sunucu tarafında şirket bağlamı ile sınırlandırılır.

## Başlangıç dilleri

İlk ürün hedefi Türkçe ve İngilizce; sonraki çeviri dalgası Rusça, Makedonca, Sırpça, Ukraynaca ve Almanca.

## Kurulum deneyimi

Windows kurulum hedefi klasik sihirbazdır: indir, çalıştır, ileri, kur. Sihirbaz ana bilgisayarı, şirket ağı erişimini, yönetici hesabını ve yedek konumunu ayarlamaya yardım eder; bağlantı adresini kurulum sonunda gösterir. Kurulum ve güncelleme hiçbir durumda müşteri veritabanını veya dosya depolamasını sessizce silmemelidir.
