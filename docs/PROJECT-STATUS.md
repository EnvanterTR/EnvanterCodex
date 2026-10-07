# Proje ilerleme raporu

**Tarih:** 7 Ekim 2026
**Ölçüm kapsamı:** Şirketlerin kullanabileceği envanter ürünü; uygulama işlevleri, üretim hazırlığı ve Windows/Linux teslimatı.

## Kısa durum

- **İşlev aileleri kapsamı: yaklaşık %40.** `PRODUCT-COVERAGE.md` içindeki 30 ana ailenin 24'ü kısmi, 4'ü temel seviyede, 1'i yok ve kurulum/güncelleme/yedekleme ailesi prototip düzeyindedir. Planlama hesabında kısmi=40, temel=55, prototip=10, yok=0 puan kullanılmıştır: `(24×40 + 4×55 + 1×10 + 1×0) / 30 = %39,7`.
- **Teslim edilebilir ürünün tamamı: yaklaşık %30 (±5 puan).** Tahmin; toplam değerlendirmenin %70'ini işlev kapsamına, %15'ini üretim/operasyon hazırlığına, %15'ini Windows/Linux kurulum paketlerine ayırır. İşlev puanı %39,7; operasyon puanı yaklaşık %20 (yerel şifreli yedekleme ve izole geri yükleme önizlemesi var, ancak canlı geri dönüş prosedürü ve üretim güvenlik kabulü yok); yerel kurulum paketi puanı %0'dır. Bu bir ilerleme planı göstergesidir, kod satırı veya kesin iş saati ölçümü değildir.
- **Windows Server ve Linux:** Her iki sistem için müşteriye kurulabilir yerel paket henüz yoktur. Windows'ta ve Linux'ta Docker Compose ile geliştirici önizlemesi ve yedekleme betikleri bulunur; bunlar kurulum sihirbazı, servis yönetimi, güncelleme/geri alma veya üretim desteği anlamına gelmez.
- **Yerel web uygulaması:** Docker Release derlemesi başarılı; `http://localhost:8080/` yanıt veriyor; API durumu `ready`, 25 migration uygulanmış, bekleyen migration 0.

## Uygulanmış ana akışlar

- Varlık ve çalışan kayıtları, arama/filtreleme, CSV dışa aktarımı; temel envanter sözlükleri ve zimmet/iade/hareket akışları.
- Lisans koltukları, aksesuar/sarf/bileşen stokları, aksesuar kitleri, sipariş ve tedarikçi kataloğu, bakım ve planlama.
- Barkod/QR etiket ayarları ve tarayıcıyla varlık bulma; varlık/çalışan fotoğrafı ve yetkili ek dosyaları.
- Çalışan yönetici hiyerarşisi, görev/telefon/konum/işe başlama alanları, özel İK notları ve işe başlama tarihi aralığı filtresi.
- Serbest metin departman/konumlardan şirket özeti ve denetimli yeniden adlandırma; konum listesindeki harf büyüklüğü/baş ve son boşluk farklılıklarını tek konum olarak gruplayıp ilişkili kayıtları birlikte güncelleme.
- Çalışan CSV içe aktarma: indirilebilir şablon, satır doğrulama önizlemesi, dosya içi ve mevcut kayıtlarla personel numarası çakışma kontrolü, en fazla 5.000 yeni kaydı tek transaction içinde ekleme.
- Sarf CSV içe aktarma: indirilebilir UTF-8 şablonu, alan/tarih/miktar doğrulaması, mevcut ve dosya içi ürün numarası tekrar kontrolü, en fazla 5.000 kaydı atomik ekleme; başlangıç stoğu için stok hareketi ve denetim izi.
- Bileşen CSV içe aktarma: ürün ve seri numarası tekrar kontrolü, doğrulama önizlemesi ve atomik yeni kayıt aktarımı; başlangıç stoğu hareket geçmişine eklenir, mevcut varlık atamaları etkilenmez.
- Aksesuar CSV içe aktarma: indirilebilir şablon, doğrulama önizlemesi, şirket/dosya içi ad ve ürün no tekrar kontrolü, başlangıç stoğu hareketi ve denetim iziyle atomik yeni kayıt ekleme; mevcut zimmetler korunur.
- Lisans CSV içe aktarma: 5.000 satıra kadar şablon ve önizleme, tür/koltuk/maliyet/tarih doğrulaması, şirket/dosya içi kayıt tekrar kontrolü ve atomik yeni kayıt ekleme; lisans anahtarı/referansı CSV kapsamına alınmaz ve mevcut atamalar değişmez.
- Lisans CSV dışa aktarma: arama metnine göre filtrelenir, en fazla 10.000 satır ve atanmış koltuk sayısını içerir; CSV formül başlatıcıları nötrlenir, lisans anahtarı/referansı dışarı verilmez.
- Şirket yöneticileri için ZIP dışa aktarma eklendi: şirket JSON verisini (departman ve konum katalogları dahil) şirket medya dosyalarıyla (logo, varlık ekleri, çalışan fotoğrafları/belgeleri ve şirkete üye hesapların profil fotoğrafları) aynı arşive koyar; diğer şirketlerin hesap dosyalarını dışarıda bırakır; 100.000 dosya/4 GiB medya sınırı ve symlink atlama uygular, tam sistem yedeği/geri yüklemesi değildir. JSON önizleme bu kataloglarda yinelenen kayıt, şirket kapsamı, üst konum ve yol/derinlik kurallarını da salt okunur denetler; katalog içe alımı henüz yoktur.
- Şirket yöneticileri artık çalışan atanmadan departman kataloğu girdisi ekleyebilir; katalog ve eski serbest metin çalışan değerleri tek listede birleşir. Yeniden adlandırma/birleştirme aynı şirketteki çalışanlarla kataloğu günceller; yeni çalışan formları katalog adlarını önerir.
- Konumlar için bina → kat → oda biçiminde, şirket kapsamlı üç seviyeli katalog eklendi. Üst konum/tekrar denetimi, düzenleme, alt seviyeler önce arşivleme/geri alma ve eski serbest metinli konumları kataloğa kaydetme sunucu tarafında uygulanır; katalog yolları giriş formlarında önerilir. Varlık, çalışan, aksesuar, sarf ve bileşen kayıtları katalog düğümüne şirket bileşik yabancı anahtarıyla bağlanır; migration eşleşen eski yolları geriye dönük bağlar; kayıt, CSV aktarımı ve katalog adı değişikliği bağlı kayıt yollarını korur. Varsayılan konum ve konum yöneticileri sonraki aşamada.
- Profil fotoğrafları için sunucu tarafında 2 MiB sınırı ve istemci tarafında sınır aşımı uyarısı.
- Yerel geliştirme veritabanı ve şirket medyası için elle/zamanlanmış şifreli yedek üretimi ve doğrulama araçları.
- Windows ve Linux için izole kurtarma önizlemesi: SHA-256 doğrulamasından sonra PostgreSQL 17'ye geçici, ağ/port/volume bağlantısı olmayan konteynerde geri yükleme; medya arşivini açma; migration ve dosya sayısını raporlama; işlem sonunda konteyneri kaldırma.

## Tamamlanması gereken ana işler

1. **P0 — Veri kurtarma ve güvenlik:** İzole kurtarma provası tamamlandı; canlı sistem için geri yükleme/geri alma tasarımı, yedek doğrulama/izleme; tenant ve izin kapsamı için kabul matrisi; oturum ve parola kurtarma güvenlik incelemesi sürüyor.
2. **P1 — Windows Server ve Linux teslimatı:** kurulum/servis yönetimi, kalıcı yapılandırma, ön yedek, güncelleme ve geri alma; LAN/HTTPS ve güvenlik duvarı rehberi.
3. **P1 — Envanter kapsamı:** konum kataloğunu aksesuar/sarf/bileşen kayıtlarına kimlikle bağlama, varsayılan konum ve konum yöneticileri; varlık durumu kuyrukları; daha fazla modülde ilişkili dosya/geçmiş/toplu işlemler; planlı denetim ve iade akışları.
4. **P1 — Veri taşıma:** çalışan/varlık dışındaki modüllerde CSV içe aktarma, kolon eşleme ve güvenli JSON geri yükleme.
5. **P2 — Kurumsal kullanım:** bildirim/e-posta, MFA/SSO, müşteri API anahtarları ve webhooklar, İngilizce yerelleştirme, kapsamlı raporlar ve kabul doğrulaması.

## Son değişikliklerin doğrulaması

Windows kurtarma önizlemesi mevcut `2026-10-06` yerel yedeğinde başarıyla çalıştırıldı: PostgreSQL dökümü geçici PostgreSQL 17 konteynerine geri yüklendi, 19 migration kaydı okundu, medya arşivi açıldı (0 dosya) ve konteyner kaldırıldı. Sarf, bileşen ve aksesuar CSV içe aktarma kodları ile şirket konum kataloğu Docker Release derlemesinden geçti; 22 migration uygulandı, yerel uygulama `ready` döndü ve katalog arayüzü `http://localhost:8080` sayfasında servis ediliyor. Şirket JSON/ZIP dışa aktarımına departman ve konum katalogları da eklendi. Sonraki Release derlemesiyle varlık konum kimliği geçişi uygulandı: tam eşleşen eski konum yolları geriye dönük bağlandı; kayıt, düzenleme, CSV aktarımı, tekli/toplu transfer katalog kimliğini saklıyor; katalog ad değişikliği bağlantılı varlık yollarını koruyor. Aynı kimlik bağı çalışanlara ve aksesuar/sarf/bileşen stoklarına da eklendi; eşleşen eski yollar ve CSV aktarımı bağlanıyor, katalog adı değişikliği bağlı kayıt yollarını güncelliyor. Yerel servis Release derlemelerinden geçti; 25 migration uygulandı ve `/api/status` `ready`, bekleyen migration 0 döndürdü. CSV aktarım uçları doğrulanmış dosyayı henüz müşteri verisine uygulamadı. Linux kurtarma betiği eklendi ancak bu Windows ana makinesinde kullanılabilir Linux kabuğu bulunmadığından Linux ortamında çalıştırıldığı doğrulanmadı. Aktif uygulama veritabanına yedek geri yüklemesi yapılmadı.

Bu raporun puanları `docs/PRODUCT-COVERAGE.md` kapsam tablosundan hesaplanmıştır; her satırdaki alt özellik sayısını veya eforu ölçmediği için yaklaşık kabul edilmelidir.
