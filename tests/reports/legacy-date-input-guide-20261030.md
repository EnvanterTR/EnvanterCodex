# Eksik geçmiş tarihlerin sağlanması — 30 Ekim 2026

Kullanıcı kararı: eski tarihleri kullanıcı sağlayacak. Tarih tahmini veya otomatik tarih uydurma yapılmayacak.

Salt okunur kabul edilmiş kopyadan 138 kayıt için özel CSV üretildi. CSV yalnız tablo adı, gerçek birincil anahtar (bileşik anahtarlar dahil), varsa CompanyId, boş CreatedAtUtc ve SourceReference içerir. Parola, token değeri, e-posta adresi veya içerik alanı seçilmez. Veritabanı değiştirilmedi.

Özel sunucu dosyası: `/var/lib/envantertr-staging/eae37c7-20261030/legacy-created-dates-input.private.csv` (0600). Dosya public repoya veya web köküne konulmaz. Güvenli yerel aktarım tamamlandı: kimlik doğrulamalı Coolify Files indirmesi sonrası masaüstünde EnvanterTR-Eksik-Tarihler-20261030.csv oluşturuldu;138satır,13923bayt ve SHA25682ae50099debc5d03a49b420913a918a5f4f94884746aa208a13717e443782e1 doğrulandı. Kullanıcının doldurulmuş tarihleri sağlaması açık. Özel CSV Git içine alınmadı.

Doldurma kuralları:
- Table, PrimaryKeyJson ve CompanyId değiştirilmez.
- CreatedAtUtc alanına doğrulanabilir tarih/saat, UTC olarak ISO 8601 biçiminde yazılır: YYYY-MM-DDTHH:mm:ssZ.
- Kaynak belge/kayıt açıklaması SourceReference alanına yazılır; parola veya erişim anahtarı yazılmaz.
- Bilinmeyen tarih boş bırakılır, bugünün tarihiyle doldurulmaz.
- Eski audit olaylarının OccurredAt gibi mevcut zamanları otomatik olarak oluşturulma tarihi kabul edilmez.

Bu dosya yalnız CreatedAt eksik mevcut kayıtları kapsar; UpdatedAt/DeletedAt ve yeni kayıtların zorunlu audit alanları ayrı iş olarak açık. Girdi alınmadan backfill uygulanmayacak. İçe aktarma öncesi anahtar eşleşmesi, kapsam, tarih biçimi ve mevcut değerlerin korunması doğrulanmalı; eski kopya ile canlı veri arasındaki fark yeniden kontrol edilmelidir.

Kanıt: export-legacy-date-input-20261030.py, legacy-date-input-ready-20261030.png. Tarih alanlarının tamamlandığı iddia edilmez.
