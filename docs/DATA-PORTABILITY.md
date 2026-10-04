# Şirket verisi dışa aktarma ve geri yükleme tasarımı

## Mevcut dışa aktarma

`GET /api/company/export.json` yalnızca oturum açmış şirket sahibi/yöneticisine açıktır. Şirket kimliği doğrulanmış oturumdan alınır; istek gövdesinden veya URL'den seçilmez. PostgreSQL `REPEATABLE READ` işlemiyle şirket kaydı ve veri dilimleri aynı tutarlı görünümden okunur. Yanıt özel/no-store olarak işaretlenir ve 500 satırlık dilimler halinde akıtılır.

Şema sürümü 1 şu verileri içerir: şirket tanımı/marka ayarları, çalışan kayıtları, varlık kategorileri/markaları/modelleri, varlıklar, varlık olayları, denetim kayıtları ve üyelerin e-posta/ad/rol bilgileri. Parolalar, parola hashleri, oturumlar, sıfırlama tokenları, platform yöneticileri, lisans imzalama sırları ve ödeme referansları dışarı aktarılmaz.

Varlık nesneleri alış ve garanti bitiş tarihlerini tarih-only `yyyy-AA-gg` değerleriyle taşır. Eski dosyalarda garanti alanı bulunmayabilir; bu alan isteğe bağlıdır.

Bu JSON şu an **taşınabilir veri dışa aktarımıdır; tam sistem yedeği değildir**. Logo ve ek dosyalar ikili içerikleriyle dahil değildir; SaaS aboneliği/lisans durumu, oturumlar ve kimlik bilgileri de dahil değildir. İndirme özelliği geri yükleme yapmaz.

## İçe alma/geri yükleme uygulanırken zorunlu akış

1. Salt okunur önizleme uygulandı: `POST /api/company/import/preview` yalnızca şirket sahibi/yöneticisine, `POST /api/assets/import/preview.csv` ise varlık yazma yetkisine açıktır. İki uç da CSRF ve etkin üyeliği tekrar kontrol eder, 32 MiB sınırı uygular ve hiçbir kayıt yazmaz. JSON önizlemesi biçim/ilişki/eşleşme denetimi; CSV önizlemesi EnvanterTR varlık dışa aktarımının sütun, alan biçimi, kategori/marka/model/çalışan eşleşmesi ve şirket içi etiket çakışma denetimini yapar. CSV'de eski 13 sütun ve garanti bitiş tarihi içeren yeni 14 sütun kabul edilir; indirilebilir CSV şablonu yeni 14 sütunlu sırayı kullanır.
2. Varlık CSV ekleme ucu `POST /api/assets/import.csv` varlık yazma yetkisi, CSRF ve etkin şirket üyeliği gerektirir. UTF-8 EnvanterTR CSV biçimini ve en fazla 5.000 satırı kabul eder. Kategori zorunludur; kategori/marka/model adları ve çalışan numarası+adı aynı şirkette tekil olarak bulunmalıdır. Yeni 14 sütunlu biçim garanti bitiş tarihini içe alır; eski 13 sütunlu biçim geriye uyumludur. Uç yeni katalog/çalışan kaydı oluşturmaz, mevcut etiketleri değiştirmez ve yinelenen etiket varsa tüm isteği reddeder. Her varlık için oluşturma hareketi, yükleme için denetim kaydı yazar. Varlıklar, hareketler ve denetim kaydı tek veritabanı işlemi içinde kaydedilir; benzersiz kısıt çakışması tümünü geri alır.
3. Yükleme boyutu ve JSON derinliği sınırlandırılır. Desteklenmeyen `format` veya `schemaVersion` reddedilir. Alan uzunlukları, durum değerleri ve tarih/tutar kuralları veritabanındaki kurallarla aynı doğrulanır.
4. Salt okunur JSON önizleme biçim/sürüm, GUID tekrarları, kaynak tenant işaretleri, alan uzunlukları, durum/para birimi kuralları ve temel çalışan/kategori/marka/model/varlık/hareket ilişkilerini kontrol eder. Hatalar, dosya kayıt adetleri ve mevcut şirkette çalışan numarası/varlık etiketi/kategori/marka adına göre yaklaşık eşleşme adetleri gösterilir. Eşleşme adedi kayıtların otomatik olarak aynı kişi/varlık olduğu anlamına gelmez; önizleme hiçbir kayıt yazmaz.
5. Üye e-postaları kimlik doğrulama hesabı oluşturmaz ve rol atamaz. Mevcut hesaplar açık e-posta eşleştirmesiyle bulunur; diğer kişiler davet e-postasıyla yeniden katılır. Hiçbir parola veya parola hash'i içe alınmaz.
6. Tam şirket JSON geri yüklemesinde uygulama öncesi mevcut şirket verisinin geri dönüş noktası, dosya adı, içe alınacak adetler ve işlem modu gösterilmelidir. Bu geri yükleme akışı henüz uygulanmadı.
7. Tam şirket JSON aktarımında kaynak kayıt kimlikleri güvenilir kabul edilmez. Şirket kapsamı doğrulanır; yeni GUID eşleme tablosu oluşturulup çalışan, kategori, marka, model, varlık ve olay ilişkileri bu haritayla yeniden bağlanır. Eşsiz alan çakışmaları önizleme aşamasında çözülmeden işlem başlatılmaz.
8. “Yerine koy”/silerek yenileme modu eklenirse ayrı yetki, şirket adını yazarak onay, indirme/yedek doğrulaması ve geri alma adımı zorunludur. Bu mod varsayılan olmayacaktır.

## Sürümleme ve bütünlük

- Dışa aktarma biçimi `format` ve artan `schemaVersion` alanlarıyla sürümlenir.
- Eski biçimlerden yeni biçimlere dönüşüm, sürüm başına açık migratörlerle yapılır; bilinmeyen sürümler reddedilir.
- Tam geri yükleme paketi oluşturulduğunda manifest, dosya listesi, boyutlar ve SHA-256 özetleri taşımalıdır. Özet, aktarım hatalarını saptar; tek başına kaynağın güvenilirliğini kanıtlamaz.
- İndirilen yedek kullanıcıya ait güvenli depoda tutulmalıdır. Uygulama sunucusunda uzun süre saklanması yeni bir veri sızıntısı yüzeyi oluşturur.

## Tamamlanmamış işler

Güncel ürün diliminde şirket JSON dışa aktarma ve salt okunur önizleme vardır; sınırlı EnvanterTR CSV varlık ekleme uygulanmıştır. Yerel Compose yedeği için sonradan manifest, SHA-256, medya arşivi ve PostgreSQL döküm dizinini doğrulayan salt okunur komutlar eklendi; bunlar hiçbir veriyi geri yüklemez. Tam JSON geri yükleme, veri birleştirme arayüzü, ek dosyaları dahil eden şifreli/otomatik tam yedek ve izole geri yükleme tatbikatı henüz uygulanmadı. Lisans sona erdiğinde veriyi dışa alma ilkesi korunmalıdır; tekrar satın alımda geri yükleme açılmadan önce tam akışın yedek/önizleme/işlem/geri alma adımları tamamlanmalıdır.
