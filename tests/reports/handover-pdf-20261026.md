# İmzalı zimmet belgesinin sunucu PDF çıktısı

## Kapsam ve durum

Bölüm 20.2/20.3 zimmet PDF/e-posta adımı için gerekli ortak çıktı eklendi. Yerel API, arayüz ve görsel kabul geçti. Tam uzak CI, staging ve bu özelliğin canlı yayını henüz yapılmadı. E-posta eki gönderimi sonraki aynı P0 akışının açık parçasıdır; bu rapor zimmet/e-posta kabulünü bitmiş saymaz. Canlı hâlâ 4237b57; gerçek Santa belgesi imzalanmadı ve kullanıcıdan yeni MFA istenmedi.

`GET /api/employees/{employeeId}/handover-documents/{documentId}/pdf` yalnız asset.read yetkisi ve mevcut şirket/çalışan/konum erişimiyle açılır. İki imza, sabit içerik SHA256 ve iki imza SHA256 doğrulanır. Eksik/bozuk veri 409; yabancı şirket, yanlış çalışan ve kapsam dışı konum 404; anonim 401. PNG boyut/piksel sınırı ve gerçek resim okuma kontrolü vardır. Yanıt attachment/application-pdf, no-store ve nosniff olur. Mevcut yazdırma ve imza akışı korundu; imzalı belge satırına PDF indir düğmesi eklendi, 390px pencerede taşma kontrol edildi.

PDFsharp-MigraDoc 6.2.3 ve gömülü Noto Sans Regular/Bold kullanılır; OFL bildirimi dağıtıma dahil, NuGet/vendor envanteri 112 kayda güncellendi. Yeni migration yok. Yazdırılan içerik özetleri uygulamadaki sabit snapshot'a aittir; bu çıktı nitelikli elektronik imza değildir.

## QA ve gerçek komut sonuçları

- İlk API koşusu 0/2: geçerli diğer şirket üyeliği olmayan fixture 404 yerine 401 aldı; ikinci testte MigraDoc varsayılan Courier New hata fontu Linux'ta bulunamadı. İlk hata logu saklandı. Testin beklenen 404'ü değiştirilmedi; gerçek diğer şirket üyeliği kuruldu. MigraDoc hata fontu da gömülü aileye yönlendirildi.
- Web Docker derlemesi 0 hata/0 uyarı. Test projesinde önceki TestServer/nullable uyarıları ayrıca bulunur; sıfır test uyarısı iddia edilmez.
- İlk düzeltmeden sonra API 2/2 geçti. Görsel kontrolde imza başlığının sayfa sonunda tek kalması bulundu. Başlık KeepWithNext düzeltmesi uygulanırken tablo başlığındaki KeepWith=1 ilk veri satırını her sayfada yineledi; pypdf 85 özgün seri kontrolü bunu yakaladı. Hatalı PDF ve görseller `first-*` olarak korundu. Tablo KeepWith kaldırıldı; yalnız bölüm başlığı birlikte tutulur.
- Son C# kaynağı önceki test imajına salt okunur `EmployeeHandoverPdf.cs` bind edilerek yeniden derlendi ve test edildi: **2/2, çıkış 0, 22.7315 saniye**. Kanıt `handover-pdf-layout-fixed-acceptance-20261026.log`. Bu yöntem imaj etiketinin son kaynağı içerdiği iddiası değildir; test edilen dosya gerçek güncel worktree dosyasıdır.
- Chromium employee-records senaryosu son HTML/test dosyaları salt okunur bind ile, network none içinde geçti; çıkış 0. İmzasız kayıtta düğme yok; imzalı API yanıtı doğru dosya adıyla byte eşit indiriliyor; 390px dialog yatay taşmıyor. API sentetiktir, gerçek müşteri kabulü sayılmaz. Kanıt `handover-pdf-browser-final-20261026.log`, `download-ui.png`, `download-ui-mobile.png`.
- Son gerçek API PDF'si Poppler ile 8 A4 sayfa olarak PNG'ye çevrildi. Bütün sayfaların temas sayfası ve imza sayfası görsel incelendi; kesilme/taşma ve yalnız başlık yok. pypdf: **85/85 benzersiz seri**, Türkçe karakterler, iki imzalayan ve özet alanları, başlık/imzaların aynı sayfası geçti. Komut `verify-handover-pdf-fixture.py sample.pdf`; kanıt `handover-pdf-20261026/text-integrity.log`, `sample.pdf`, `contact-sheet.png`, `page-*.png`. İmzalar açıkça sentetik test çizimidir.
- UI envanteri 2290 ile aynı, EmployeeRecords kaynak/JavaScript doğrulaması ve diff kontrolü geçti. Yalnız `handover-pdf-20261026` izole Docker Compose ortamı temizlendi.

## Sıradaki

Doğrulanan aynı sunucu PDF'sini şirket/çalışan kapsamı korunarak, açık kullanıcı eylemiyle e-posta eki göndermek. Sonra ilgili SMTP kabulü, tam CI ve gerçek staging/yedek/geri dönüş kapılarıyla yayın. Gerçek Santa/Farma imza ve kullanıcı kabulü açık kalır.

TAMAM dediğim 2 hedefli API testinin 2'si test çıktısıyla doğrulanmıştır; ayrıca ayrı Chromium ve gerçek PDF içerik/görsel kontrolleri vardır.
