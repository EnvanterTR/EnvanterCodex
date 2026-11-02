# Profil fotoğrafı depolama hata davranışı — 1 Kasım 2026

## Değişiklik
- `/api/session/photo` için dizin oluşturma, önceki fotoğrafı yedekleme ve dosya yazma aynı geri alma korumasına alındı.
- Dosya sistemi `IOException` veya `UnauthorizedAccessException` döndürürse özel dosya yolu/sunucu ayrıntısı istemciye verilmeden 503 döner; ayrıntı sunucu loguna yazılır. Diğer beklenmeyen hatalar gizlenmeden yükseltilir.
- Hedefli HTTP kabul testi `/accounts` yolunu dosyayla çakıştırıp 503, hassas yol sızmaması ve marker byte'larının değişmemesini kontrol ediyor.

## Kanıt ve açık durum
- Web build: başarılı, 0 uyarı / 0 hata.
- Security acceptance test projesi build: başarılı, 4 mevcut `ASPDEPR` uyarısı / 0 hata.
- Testin HTTP sonucu bu yerel oturumda doğrulanmadı; `ENVANTERTR_SECURITY_TEST_CONNECTION` tanımlı değil ve disposable PostgreSQL kabul veritabanı yok.
- Bu rapor tek başına üretimdeki önceki 500'ün kök nedenini kanıtlamaz ve canlı yayını göstermez. Gerçek endpoint kabulini PR CI tamamlayacak.
