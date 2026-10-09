# Uygulama arayüzü, temalar ve diller

## Ürün kararı

Bu kapsam, tanıtım/marketing ana sayfası hariç oturum açılan tüm EnvanterTR uygulamasını kapsar: şirket ve platform yönetimi, tablolar, formlar, ayrıntı ekranları, raporlar, bildirimler, yardım ve kurulum akışları. Her modül aynı modern tasarım sistemini kullanmalı; kullanıcıya göre izin verilen veriyi ve işe yarayan eylemleri öne çıkaran, masaüstü/tablet/telefon uyumlu panolar sunmalıdır. Referans ekranlardaki bilgi yoğunluğu korunurken hiyerarşi, boşluk, okunabilirlik, grafikler ve klavye/dokunmatik erişilebilirliği iyileştirilir.

## Kullanıcı kabul durumu

- 2026-10-09: Rezervasyon modülünün başlık, form, filtre, tablo, boş/yükleniyor durumu, durum/eylem metinleri ve tarih gösterimi beş dile bağlandı. Dil tercihi kaydedilirken rezervasyonlar açıksa dinamik satırlar seçili dille yenilenir; CI sözlük denetimi bu ekranın çekirdek metinlerini zorunlu kontrol eder. Bu, diğer modüllerin çeviri kapsamını tamamlamaz.
- 2026-10-09: Aksesuar stok listesi beş dilde başlık/açıklama, arama, stok ve sıralama filtreleri, sütun seçimi, tablo, sayfalama, boş/yükleniyor durumları, stok rozeti, satır eylemleri ve toplu seçim/düzeltme/teslim etiketlerini gösterir. Dil değişiminde açık liste yeniden yüklenir; sözlük denetimi bu ekranın çekirdek etiketlerini zorunlu kontrol eder. Ürün ve katalog adları kullanıcı verisi olduğu için özgün kalır. Aksesuar ayrıntısı, oluşturma/düzenleme ve teslim formları bu değişiklikte yerelleştirilmedi; görsel tasarım kabulü yapılmadı.
- 2026-10-09: Kullanıcı mevcut tema seçeneklerinin, dashboard düzeninin ve menü ikonlarının beğenilmediğini; arayüzün karmaşık göründüğünü belirtti. Mevcut görsel tur kabul edilmiş sayılmaz. Görsel cilayı şimdilik ertele; ürün işlevleri ilerledikten sonra tüm uygulama için tutarlı, daha sade ve profesyonel bir tasarımı yeniden ele al. Yeni görsel değişiklikleri kabul edilmiş tasarım gibi tanıtma.
- 2026-10-09: Giriş formunda `.auth-card label { display:block }` kuralı `hidden` nitelikli MFA ve şirket seçimi alanlarının görünmesini engelliyordu. Gizli etiketlere açık `display:none` kuralı eklendi. İlk açılışta bu hesap için şirket seçme alanı ve MFA kodu görünmez; kimlik bilgileri doğrulandıktan sonra yalnız birden fazla etkin üyeliği olan hesap için sunucunun döndürdüğü şirketler gösterilir. Şirket seçimi API'si de yalnız hesaba ait etkin üyelikleri döndürür; ayrıntı ve PostgreSQL kabul kontrolleri `docs/PRODUCT-COVERAGE.md` içindedir.

## Temalar

- En az Açık, Koyu ve Sistem görünümü; uygulamadaki bütün sayfalar, diyaloglar, açılır listeler, grafikler, tablolar, giriş/kurulum ve hata durumları aynı tema tercihini izler.
- İlk arayüz adımında şirket uygulamasının üst çubuğuna görünüm paneli eklendi. Açık/Koyu/Sistem ve dil seçimleri kullanıcı/şirket kapsamlı API'de saklanıyor, tarayıcı depolaması ilk ekranda API yanıtı gelene kadar geri dönüş sağlıyor. Koyu yüzey stilleri ortak kabuk, tablolar, formlar ve temel diyaloglar için başladı. Görsel tasarım henüz son kullanıcı kabulünden geçmedi ve tüm modüllerin tema kabulünden geçtiği anlamına gelmiyor.
- 2026-10-09 görsel iyileştirme turunda şirket alanının yan menüsü ve pano kartları daha düzenli boşluk, tutarlı çizgi ikonlar ve daha sakin yüzey renkleriyle güncellendi. Pano ilk açılışta alt menüleri kapalı tutar; ana dört ölçüt ile beş ek envanter ölçütü ayrı hizalı satırlarda sunulur. Bu, tasarım kabulü değildir: ayrıntı sayfaları, formlar, grafikler ve dar ekranlar için modül modül görsel inceleme sürmelidir.
- 2026-10-09: Kullanıcı paylaştığı dashboard örneğine göre özet kartı ikonlarında pastel/saten, ölçülü ve birbirinden ayrışan renkler istedi. Pano metrik kartlarına yumuşak renk geçişleri, renge uyumlu büyük çizgi ikon filigranları ve koyu tema karşılıkları eklendi; küçük ekranda ikonlar daraltılır. Bu değişiklik yalnızca özet kartlarını kapsar; tüm tema veya pano tasarımının kullanıcı tarafından onaylandığı anlamına gelmez.
- 2026-10-09: Kullanıcının Snipe-IT örneklerinde mavi alanların bağlantı gibi çalışması ve ürün satırı sonunda eylem düğmeleri bulunması isteğiyle aksesuar, sarf ve bileşen tablolarında kategori/model/tedarikçi/konum metinleri filtre bağlantısına dönüştürüldü. Satır sonlarına yetkiye göre düzenle/stok/teslim/çıkış/ata eylemleri eklendi; varlık satırlarının mevcut saten ikon grubu ve etiket yazdırması korunuyor. Gerçek oturum gerektiren yerel liste görünümü bu turda kullanıcıyla görsel kabul edilmedi.
- Görünüm panelinde Sistem/Açık/Koyu seçimleri önizleme kartlarıyla sunulur; fare, dokunma, ok tuşları ve Home/End ile seçim yapılabilir. Tema kartları mevcut tercih API'sindeki üç desteklenen temayı kullanır; renk paleti çeşitliliği bu turda kapsam dışı bırakılmayıp sonraki tema sistemi adımına açıkça bırakılmıştır.
- Ana panoda barkod okutma ve yeni varlık oluşturma sık kullanılan birincil işlemler olarak kalır; pano düzenleme, dışa aktarma, çöp kutusu ve yetkiye bağlı veri işlemleri “Daha fazla” menüsünde gruplanır. Menüde görünür eylem yoksa menü de gizlenir; dar ekranda menü taşmadan açılır.
- Kullanıcı seçimi cihazlar arasında devam eder; sistem tercihi işletim sisteminin `prefers-color-scheme` ayarını izler. Odak halkası, kontrast ve renk körlüğü için yalnız renge dayanmayan durum göstergeleri her temada korunur.
- Renk, yazı, kenarlık, gölge, boşluk ve yüzey değerleri ortak tasarım tokenlarıyla verilir. Modül bazlı ayrı CSS renkleri veya birbirinden kopuk ekran tasarımları eklenmez.
- Ana panolar modül/kullanıcı rolüne göre gerçek ölçüm, son hareket, yaklaşan iş ve eylem kısayollarını gösterecek; sahte örnek değer üretmeyecektir. Kullanıcı panelleri kişiselleştirebilir ve dar ekranda içerik taşmadan kullanılabilir.

## Dil kapsamı

Arayüz dilleri: Türkçe (`tr-TR`), İngilizce (`en`), Rusça (`ru`), Ukraynaca (`uk`) ve Makedonca (`mk`). Varsayılan mevcut şirket/hesap dilidir; kullanıcı dili profilinden değiştirebilir. Tercih, oturum ve cihazlar arasında korunur.

Çeviri yalnız menü başlıklarını değil; her modüldeki buton, form etiketi, tablo sütunu, filtre, durum, boş/yükleniyor/hata durumu, doğrulama metni, onay penceresi, toast, e-posta özeti, rapor başlığı, CSV dışa aktarım etiketi, erişilebilirlik metni ve kurulum/kurtarma akışını kapsar. Tarih, saat, sayı, para birimi, çoğul ve sıralama kuralları seçilen yerel ayara göre biçimlenir. Kaynak dil anahtarları kodda tutulur; kullanıcı girdisi ve API kodları çevrilmez. Bir çeviri eksikse ham anahtar kullanıcıya gösterilmez; güvenli İngilizce yedeği ve geliştirme denetimi kullanılır.

Görünüm paneli beş dil için tercih kaydını başlattı. Türkçe, İngilizce, Rusça, Ukraynaca ve Makedonca seçimi menüyü, üst çubuğu, ana pano metinlerini ve görünüm panelini değiştirir. Varlık listesinde sabit başlık ve filtre metinlerine ek olarak sonradan çizilen durum etiketleri, fotoğraf yer tutucuları ve satır eylemi erişilebilir adları da seçili dile çevrilir; varlık, kategori, marka/model, çalışan ve konum adları ile etiket/seri kodları çevrilmez. `tools/Verify-UiLocales.mjs` sözlük girdilerinin beş dilde biçim bütünlüğünü ve varlık satırı eylemlerinin sözlük kapsamını CI'da doğrular; tüm modül metinlerinin çevrilmiş olduğunu kanıtlamaz. Diğer modül metinleri çevrilme sırasını bekliyor. Eksik çeviri kapsamı kullanıcıya açıkça bildiriliyor. Tercihler kullanıcı ve etkin şirket kapsamında API'de saklanır. Tarih/saat ve sayı biçimleyicileri artık etkin tercihten gelen `Intl` yerel ayarını kullanıyor; yeni çizilen modül kayıtları ve yazdırma belgeleri seçili dile uygun tarih/sayı biçimi alır. Arayüzün tüm sabit metinleri, para birimi tercihine göre biçimleme ve profesyonel görsel kabul tamamlanmayı bekliyor.

## Teslim ve kabul kapıları

1. Tüm oturum açılmış ekranların metin/tema envanterini çıkar; ürün işlevi olan her rotayı ve rolü kapsama dahil et.
2. Tema tokenları ile `auto/light/dark` tercihini ortak kabukta uygula; grafik ve bileşenleri aynı tokenlara bağla.
3. Merkezi, tür denetimli çeviri kaynakları ve yerel ayar biçimlendirme katmanını kur; mevcut sabit metinleri ekran grupları halinde anahtarlara taşı.
4. Beş dilin tüm kullanıcıya görünen kaynaklarda anahtar kapsamı ve placeholder/plural uyumunu CI'da doğrula.
5. Her modülün açık/koyu temada, masaüstü ve mobil kırılımında görsel kabulünü; klavye, kontrast ve ekran okuyucu kontrollerini tamamla.

Bir modül, beş dilde ve tüm desteklenen temalarda arayüz/çıktı metinleri kapsanmadan veya yeni özellik bu kapsama eklenmeden “tamamlandı” sayılmaz. Gerçek kullanıcı kabulü ve profesyonel çeviri gözden geçirmesi ayrıca gerekir.
