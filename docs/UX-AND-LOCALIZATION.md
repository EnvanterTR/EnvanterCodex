# Uygulama arayüzü, temalar ve diller

## Ürün kararı

Bu kapsam, tanıtım/marketing ana sayfası hariç oturum açılan tüm EnvanterTR uygulamasını kapsar: şirket ve platform yönetimi, tablolar, formlar, ayrıntı ekranları, raporlar, bildirimler, yardım ve kurulum akışları. Her modül aynı modern tasarım sistemini kullanmalı; kullanıcıya göre izin verilen veriyi ve işe yarayan eylemleri öne çıkaran, masaüstü/tablet/telefon uyumlu panolar sunmalıdır. Referans ekranlardaki bilgi yoğunluğu korunurken hiyerarşi, boşluk, okunabilirlik, grafikler ve klavye/dokunmatik erişilebilirliği iyileştirilir.

## Temalar

- En az Açık, Koyu ve Sistem görünümü; uygulamadaki bütün sayfalar, diyaloglar, açılır listeler, grafikler, tablolar, giriş/kurulum ve hata durumları aynı tema tercihini izler.
- İlk arayüz adımında şirket uygulamasının üst çubuğuna görünüm paneli eklendi. Açık/Koyu/Sistem ve dil seçimleri kullanıcı/şirket kapsamlı API'de saklanıyor, tarayıcı depolaması ilk ekranda API yanıtı gelene kadar geri dönüş sağlıyor. Koyu yüzey stilleri ortak kabuk, tablolar, formlar ve temel diyaloglar için başladı. Görsel tasarım henüz son kullanıcı kabulünden geçmedi ve tüm modüllerin tema kabulünden geçtiği anlamına gelmiyor.
- 2026-10-09 görsel iyileştirme turunda şirket alanının yan menüsü ve pano kartları daha düzenli boşluk, tutarlı çizgi ikonlar ve daha sakin yüzey renkleriyle güncellendi. Pano ilk açılışta alt menüleri kapalı tutar; ana dört ölçüt ile beş ek envanter ölçütü ayrı hizalı satırlarda sunulur. Bu, tasarım kabulü değildir: ayrıntı sayfaları, formlar, grafikler ve dar ekranlar için modül modül görsel inceleme sürmelidir.
- Görünüm panelinde Sistem/Açık/Koyu seçimleri önizleme kartlarıyla sunulur; fare, dokunma, ok tuşları ve Home/End ile seçim yapılabilir. Tema kartları mevcut tercih API'sindeki üç desteklenen temayı kullanır; renk paleti çeşitliliği bu turda kapsam dışı bırakılmayıp sonraki tema sistemi adımına açıkça bırakılmıştır.
- Ana panoda barkod okutma ve yeni varlık oluşturma sık kullanılan birincil işlemler olarak kalır; pano düzenleme, dışa aktarma, çöp kutusu ve yetkiye bağlı veri işlemleri “Daha fazla” menüsünde gruplanır. Menüde görünür eylem yoksa menü de gizlenir; dar ekranda menü taşmadan açılır.
- Kullanıcı seçimi cihazlar arasında devam eder; sistem tercihi işletim sisteminin `prefers-color-scheme` ayarını izler. Odak halkası, kontrast ve renk körlüğü için yalnız renge dayanmayan durum göstergeleri her temada korunur.
- Renk, yazı, kenarlık, gölge, boşluk ve yüzey değerleri ortak tasarım tokenlarıyla verilir. Modül bazlı ayrı CSS renkleri veya birbirinden kopuk ekran tasarımları eklenmez.
- Ana panolar modül/kullanıcı rolüne göre gerçek ölçüm, son hareket, yaklaşan iş ve eylem kısayollarını gösterecek; sahte örnek değer üretmeyecektir. Kullanıcı panelleri kişiselleştirebilir ve dar ekranda içerik taşmadan kullanılabilir.

## Dil kapsamı

Arayüz dilleri: Türkçe (`tr-TR`), İngilizce (`en`), Rusça (`ru`), Ukraynaca (`uk`) ve Makedonca (`mk`). Varsayılan mevcut şirket/hesap dilidir; kullanıcı dili profilinden değiştirebilir. Tercih, oturum ve cihazlar arasında korunur.

Çeviri yalnız menü başlıklarını değil; her modüldeki buton, form etiketi, tablo sütunu, filtre, durum, boş/yükleniyor/hata durumu, doğrulama metni, onay penceresi, toast, e-posta özeti, rapor başlığı, CSV dışa aktarım etiketi, erişilebilirlik metni ve kurulum/kurtarma akışını kapsar. Tarih, saat, sayı, para birimi, çoğul ve sıralama kuralları seçilen yerel ayara göre biçimlenir. Kaynak dil anahtarları kodda tutulur; kullanıcı girdisi ve API kodları çevrilmez. Bir çeviri eksikse ham anahtar kullanıcıya gösterilmez; güvenli İngilizce yedeği ve geliştirme denetimi kullanılır.

Görünüm paneli beş dil için tercih kaydını başlattı. Türkçe, İngilizce, Rusça, Ukraynaca ve Makedonca seçimi menüyü, üst çubuğu, ana pano metinlerini ve görünüm panelini değiştirir; diğer modül metinleri çevrilme sırasını bekliyor. Eksik çeviri kapsamı kullanıcıya açıkça bildiriliyor. Tercihler kullanıcı ve etkin şirket kapsamında API'de saklanır. Tarih/sayı biçimleme, tam modül çevirisi ve profesyonel görsel kabul tamamlanmayı bekliyor.

## Teslim ve kabul kapıları

1. Tüm oturum açılmış ekranların metin/tema envanterini çıkar; ürün işlevi olan her rotayı ve rolü kapsama dahil et.
2. Tema tokenları ile `auto/light/dark` tercihini ortak kabukta uygula; grafik ve bileşenleri aynı tokenlara bağla.
3. Merkezi, tür denetimli çeviri kaynakları ve yerel ayar biçimlendirme katmanını kur; mevcut sabit metinleri ekran grupları halinde anahtarlara taşı.
4. Beş dilin tüm kullanıcıya görünen kaynaklarda anahtar kapsamı ve placeholder/plural uyumunu CI'da doğrula.
5. Her modülün açık/koyu temada, masaüstü ve mobil kırılımında görsel kabulünü; klavye, kontrast ve ekran okuyucu kontrollerini tamamla.

Bir modül, beş dilde ve tüm desteklenen temalarda arayüz/çıktı metinleri kapsanmadan veya yeni özellik bu kapsama eklenmeden “tamamlandı” sayılmaz. Gerçek kullanıcı kabulü ve profesyonel çeviri gözden geçirmesi ayrıca gerekir.
