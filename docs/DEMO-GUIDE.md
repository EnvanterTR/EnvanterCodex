# EnvanterTR kısa ürün demosu

Bu rehber, yerel geliştirme önizlemesinde bugün gösterilebilen akışlara göre hazırlanmıştır. Paket, lisans, kurulum ve güvenlik konusunda tamamlanmamış işleri hazır özellik gibi anlatmayın.

## 30 saniyelik anlatım

> EnvanterTR, şirket demirbaşlarını ve stoklarını tek bir şirket alanında izlemeyi; kimin neyi, ne zaman teslim aldığını ve kaydın nasıl değiştiğini görünür kılmayı hedefleyen bir envanter yönetim ürünüdür. Bugünkü geliştirme önizlemesinde varlık arama, zimmet ve iade, etiket basımı, stok takibi, satın alma teslimleri ve dışa aktarma akışlarını gösterebiliriz. Windows/Linux kurulum paketi ve üretim hizmeti henüz yayıma hazır değildir.

## Sunumdan önce

- Uygulamayı açın: <http://localhost:8080/>. Bu adres yalnızca bu bilgisayarda çalışır; ekrandan paylaşın veya aynı bilgisayarda gösterin.
- Sağlık durumunu kontrol edin: <http://localhost:8080/api/status>. `status` değeri `ready` olmalıdır.
- Tanıtım sayfası taslağı: <http://localhost:8081/>. Bu sayfa da yalnızca yerel önizlemedir.
- Mevcut kurulum ve şirket kayıtlarını sıfırlamayın. Sunum için gerçek dışı/kişisel verileri ekranda göstermeyin; kayıtlar boşsa bunu canlı veri olmadığı şeklinde açıkça belirtin.
- Hazır test verisi veya herkese açık demo hesabı vaat etmeyin. Sunum ortamında kullanılabilir şirket hesabı yoksa, önce önizlemeyi kuran kişiden hesap hazırlamasını isteyin.

## 5 dakikalık akış

### 1. Genel görünüm — 30 saniye

Şirketin varlık durumlarını gösteren özetle başlayın. Durum grafiğinden varlık listesindeki ilgili kayda geçişin amacı, özetle operasyon ekranı arasındaki bağlantıyı göstermek.

**Anlatım:** “Özet ekranı kayıtların durumunu topluca gösteriyor; sayıdan ilgili listeye geçip kaydı inceleyebiliyoruz.”

### 2. Varlık bulma ve profil — 1 dakika

Varlıklar sayfasında etiket, ad, seri numarası veya konumla arama; durum, kategori ve garanti filtrelerini gösterin. Çalışan profilinde yönetici/raporlayan ilişkileri ve yetkiniz varsa zimmetli varlık, aksesuar, lisans ve sarf tüketim kayıtlarını gösterin. Bir varlığın profilinde temel alanlar ve mevcutsa hareket geçmişi/ekler görünür.

**Anlatım:** “Varlık kaydını filtreleyebiliyor, ayrıntısına ve uygulamadaki işlem geçmişine ulaşabiliyoruz.”

### 3. Etiket — 45 saniye

Yetkiniz varsa etiket ayarları sayfasında barkod türü, ebat ve hizalama seçeneklerini gösterin. Yazdırma ekranını yalnızca sunum bilgisayarında yazıcı/önizleme uygun olduğunda açın.

**Anlatım:** “Etiket düzeninde barkod biçimi, ölçü, hizalama ve kartta yer alacak bilgiler ayarlanabiliyor.”

### 4. Aksesuar, hazır kit ve satın alma — 1,5 dakika

Aksesuar listesinde arama, stok filtresi ve CSV indirmeyi gösterin. Hazır kitlerde bir başlangıç setinin içeriğini ve tam kit stok sayısını gösterin; arama filtresiyle listeyi daraltıp kit CSV'sini indirin. Sunum verisi uygunsa çalışan veya varlık seçip kiti teslim edin. Kit teslimi stoğu değiştirir ve tüm aksesuar kalemlerini tek işlemde zimmet/geçmiş kayıtlarına yazar. Ardından kitin “Geçmiş” görünümünde parçaların teslim, iade ve kalan adetlerini gösterin. Satın alma siparişlerinde durum ve tarih aralığı filtresi, geciken teslim işareti ve satır düzeyindeki CSV dışa aktarımı anlatın. Sadece sunum için hazırlanmış bir kayıt varsa sipariş ayrıntısı ve kısmi teslim alma akışını açın; bu işlem de stoğu değiştirir.

**Anlatım:** “Siparişteki kalemler teslim alındıkça aksesuar stoğuna işleniyor; siparişin kalan miktarı ve hareketi takip ediliyor.”

### 5. Rapor ve kapanış — 45 saniye

Hazır raporları ve CSV dışa aktarmayı gösterin. Ürünün hangi ortamda çalıştığı ve bugün hazır olmayan dağıtım kapsamı konusunda açık olun.

**Anlatım:** “Bugünkü sürüm çalışan bir yerel ürün önizlemesi. Kurulum, yedekleme/geri yükleme ve kurum ağına güvenli dağıtım işleri tamamlanma planında.”

## Bugün için doğru kapsam beyanı

- **Gösterilebilir:** varlık arama/filtreleme, kayıt ayrıntıları, zimmet-iade akışları, etiket ayarları, aksesuar stokları, aksesuar hazır kitleri ve atomik kit teslimi, aksesuar satın alma/teslim alma, temel raporlar ve CSV.
- **Henüz hazır değil:** imzalı Windows/Windows Server veya Linux kurucusu, şirket ağına açılmış HTTPS dağıtımı, otomatik şifreli yedekleme ve uygulama içi geri yükleme, LDAP/AD/SSO, e-posta bildirimleri ve tam modül kapsamı. İsteğe bağlı Authenticator tabanlı MFA ve tek kullanımlık kurtarma kodları uygulamada mevcuttur.
- `localhost` adresi internetten müşterilerin erişebileceği bir demo bağlantısı değildir. Ürünü bugün dışarıya açık hizmet veya üretim kurulumuyla sunmayın.

## Sorulara kısa yanıtlar

**“Bugün kurabilir miyiz?”** — Bugün gösterilebilen kurulum, geliştirici önizlemesidir. Son kullanıcı Windows/Linux kurucuları henüz hazır değildir.

**“Şirket ağında kullanabilir miyiz?”** — Mevcut önizleme yalnızca `localhost` adresine bağlıdır. LAN, TLS, güvenlik duvarı ve üretim yedeklemesi tamamlanmadan şirket ağına açılmamalıdır.

**“Veriyi geri yükleyebilir miyiz?”** — Tam, uygulama içinden geri yüklenebilir yedek akışı henüz yoktur. Mevcut JSON/CSV dışa aktarma, tam sistem yedeğinin yerine geçmez.

**“Eksik modüller tamamlandı mı?”** — Hayır. Kapsam ve durumlar [PRODUCT-COVERAGE.md](PRODUCT-COVERAGE.md) içinde izleniyor; her modül tamamlanmadan hazır olarak sunulmuyor.
