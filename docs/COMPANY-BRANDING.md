# Şirket profili ve marka görünümü

Şirket sahibi ve yöneticileri şirket alanındaki **Şirket ayarları → Firma profili / Logo ve tema** ekranından şirket adını, iletişim e-postasını, ana rengi ve logosunu değiştirebilir. PNG, JPEG ve WebP yüklemeleri dosya imzasına göre kontrol edilir, 2 MB ile sınırlandırılır ve rastgele dosya adıyla saklanır.

Şirket üyeleri kendi şirketlerine ait renk ve logoyu oturum açtıktan sonra görür. Logo indirme ve ayar API'leri oturumdaki şirket üyeliğini doğrular; logo yolu istek parametresinden alınmaz. Böylece bir şirketin logo isteği başka şirketin depolama yoluna erişemez.

Yerel Docker Compose dağıtımında logo dosyaları `envantertr_company_media` named volume'unda, PostgreSQL kayıtları ise ayrı `envantertr_database` volume'unda bulunur. Uygulama imajını güncellemek bu iki volume'u silmez. `docker compose down -v` volume'ları siler; bu komut mevcut yerel şirket verisini ve logolarını kalıcı olarak kaldırabileceğinden yedekleme/geri yükleme prosedürü hazır olmadan kullanılmamalıdır. Logo ve veritabanı yedekleme/geri yükleme aracı henüz uygulanmamıştır.
