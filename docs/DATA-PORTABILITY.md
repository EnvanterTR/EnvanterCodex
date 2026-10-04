# Şirket verisi dışa aktarma ve geri yükleme tasarımı

## Mevcut dışa aktarma

`GET /api/company/export.json` yalnızca oturum açmış şirket sahibi/yöneticisine açıktır. Şirket kimliği doğrulanmış oturumdan alınır; istek gövdesinden veya URL'den seçilmez. PostgreSQL `REPEATABLE READ` işlemiyle şirket kaydı ve veri dilimleri aynı tutarlı görünümden okunur. Yanıt özel/no-store olarak işaretlenir ve 500 satırlık dilimler halinde akıtılır.

Şema sürümü 1 şu verileri içerir: şirket tanımı/marka ayarları, çalışan kayıtları, varlık kategorileri/markaları/modelleri, varlıklar, varlık olayları, denetim kayıtları ve üyelerin e-posta/ad/rol bilgileri. Parolalar, parola hashleri, oturumlar, sıfırlama tokenları, platform yöneticileri, lisans imzalama sırları ve ödeme referansları dışarı aktarılmaz.

Bu JSON şu an **taşınabilir veri dışa aktarımıdır; tam sistem yedeği değildir**. Logo ve ek dosyalar ikili içerikleriyle dahil değildir; SaaS aboneliği/lisans durumu, oturumlar ve kimlik bilgileri de dahil değildir. İndirme özelliği geri yükleme yapmaz.

## İçe alma/geri yükleme uygulanırken zorunlu akış

1. İlk aşama uygulandı: `POST /api/company/import/preview` yalnızca şirket sahibi/yöneticisine açıktır, CSRF doğrular, etkin üyeliği veritabanından tekrar kontrol eder ve 32 MiB sınırını uygular. Dosyadaki şirket kimliği hedef tenant seçmek için kullanılmaz. Gerçek içe alma uç noktası henüz yoktur.
2. Yükleme boyutu ve JSON derinliği sınırlandırılır. Desteklenmeyen `format` veya `schemaVersion` reddedilir. Alan uzunlukları, durum değerleri ve tarih/tutar kuralları veritabanındaki kurallarla aynı doğrulanır.
3. Salt okunur önizleme biçim/sürüm, GUID tekrarları, kaynak tenant işaretleri, alan uzunlukları, durum/para birimi kuralları ve temel çalışan/kategori/marka/model/varlık/hareket ilişkilerini kontrol eder. Hatalar, dosya kayıt adetleri ve mevcut şirkette çalışan numarası/varlık etiketi/kategori/marka adına göre yaklaşık eşleşme adetleri gösterilir. Eşleşme adedi kayıtların otomatik olarak aynı kişi/varlık olduğu anlamına gelmez; önizleme hiçbir kayıt yazmaz.
4. Üye e-postaları kimlik doğrulama hesabı oluşturmaz ve rol atamaz. Mevcut hesaplar açık e-posta eşleştirmesiyle bulunur; diğer kişiler davet e-postasıyla yeniden katılır. Hiçbir parola veya parola hash'i içe alınmaz.
5. Uygula adımından önce mevcut şirket verisinin geri dönüş noktası alınır ve kullanıcıya dosya adı, içe alınacak adetler ve seçilen işlem modu gösterilir. Varsayılan işlem birleştirmedir; mevcut kayıtları sessizce silen/ezip geçen davranış yoktur.
6. Kaynak kayıt kimlikleri güvenilir kabul edilmez. Şirket kapsamı doğrulanır; yeni GUID eşleme tablosu oluşturulup çalışan, kategori, marka, model, varlık ve olay ilişkileri bu haritayla yeniden bağlanır. Eşsiz alan çakışmaları önizleme aşamasında çözülmeden işlem başlatılmaz.
7. Yazma tek veritabanı işlemi içinde yapılır. Herhangi bir doğrulama/ilişki hatasında işlem tamamen geri alınır. Başarıda kullanıcı, zaman, kaynak sürüm ve kayıt sayıları denetim izine yazılır.
8. “Yerine koy”/silerek yenileme modu eklenirse ayrı yetki, şirket adını yazarak onay, indirme/yedek doğrulaması ve geri alma adımı zorunludur. Bu mod varsayılan olmayacaktır.

## Sürümleme ve bütünlük

- Dışa aktarma biçimi `format` ve artan `schemaVersion` alanlarıyla sürümlenir.
- Eski biçimlerden yeni biçimlere dönüşüm, sürüm başına açık migratörlerle yapılır; bilinmeyen sürümler reddedilir.
- Tam geri yükleme paketi oluşturulduğunda manifest, dosya listesi, boyutlar ve SHA-256 özetleri taşımalıdır. Özet, aktarım hatalarını saptar; tek başına kaynağın güvenilirliğini kanıtlamaz.
- İndirilen yedek kullanıcıya ait güvenli depoda tutulmalıdır. Uygulama sunucusunda uzun süre saklanması yeni bir veri sızıntısı yüzeyi oluşturur.

## Tamamlanmamış işler

Güncel ürün diliminde JSON dışa aktarma ve salt okunur yapısal önizleme vardır. Gerçek geri yükleme, mevcut kayıtlarla çakışma analizi, veri birleştirme arayüzü, ek dosyaları dahil eden şifreli tam yedek ve geri yükleme tatbikatı henüz uygulanmadı. Lisans sona erdiğinde veriyi dışa alma ilkesi korunmalıdır; tekrar satın alımda geri yükleme açılmadan önce bu akışın yedek/önizleme/işlem/geri alma adımları tamamlanmalıdır.
