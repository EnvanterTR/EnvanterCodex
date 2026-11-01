# Ortak oturum sürekliliği: canlı dağıtım kaydı

Kesin aday: `4237b5735859cb9c2672ddc5a2f3a5ef22ba566c`, PR25 taslak dalı. Tam GitHub CI **40253374749, 3/3 başarılı**. PostgreSQL işi 26m 1s sonunda başarılı; Windows ve Linux security de başarılı. Kanıt `4237-full-ci-pass-20261026.png`.

## Yayın öncesi kapılar

- Gerçek kesin aday staging derlemesi çıkış 0; üç host ready/121/0, anonim GET/PUT/PATCH 401, CSRF ve Secure cookie kontrolleri geçti. Çalışan konteynerde internal ağ, dış port bulunmaması, ayrı medya/anahtar ve kopya SQL bağlantısı ile SMTP/ödeme/bootstrap kapalı değerleri ayrıca doğrulandı.
- Taze SQL `pre-web-v1-20261026T010407Z.dump`; 140 MB rollback `/var/backups/envantertr/rollback-20261026T010459Z`, dosya/imaj provası `-drill-20261026T010559Z`. SQL kopyasına restore exit-on-error başarılı. Yayından hemen önce yeniden yapılan 95 public tablo sayımı üretimle aynı, çıkış 0: `4237-immediate-predeploy-counts-20261026.png`.
- Kaydedilen Coolify kaynak SHA alanı sayfa yeniden yüklenince aynı tam 4237 SHA'yı korudu. Genel “configuration changes not applied” uyarısı, kaydedilmediği varsayılmadı; dağıtımın tamamlanması ayrıca izleniyor.
- Yeni migration yok; şema 121 korunacak. Geri dönüş paketi önceki çalışan 124b2d1 imajlarını içerir. Hata halinde şema Down veya canlı SQL restore otomatik yapılmaz; yeni veri kaybı riski ayrıca değerlendirilir.

## Dağıtım

Coolify **wbhegg8zyeiziogdpr401ycm**, başlangıç **2026-10-26 01:46:37 UTC**. Kaynak satırı ve import günlüğü tam 4237 SHA'yı gösteriyor. Son gözlemde **In progress**; bu aşamada canlı tamamlandı iddiası yok. Aynı işlem izleniyor, yeniden başlatılmadı.

Dağıtım sırasında üç dış HTTPS adresi 200/ready/121/0; `4237-deploying-external-health-20261026.log`, çıkış 0. Bu sonuç yeni imajın çalıştığını tek başına kanıtlamaz. Terminal başarıdan sonra gerçek imaj SHA'ları, DB sağlığı, dış HTTPS, 8 plan/kapalı checkout-deneme sınırı ve görsel kaynak bütünlüğü ayrıca kontrol edilecek. T0, doğrulanan bitiş zamanıdır; 24 saat gözlem tamamlanmış sayılmaz.

Santa imzaları/PDF ve operatör kabulü, Farma normal kullanıcı senaryosu ve iki yönlü canlı tenant saldırı testi, Demo3 normal owner kabulü ve gerçek iyzico hâlâ açık. Kullanıcının mevcut MFA kuralları/süreleri değiştirilmedi ve yeni kod istenmedi.

## Doğrulanan canlı sonuç

Dağıtım **Success / Finished** olarak tamamlandı. Üretim web/public imajları tam `4237b5735859cb9c2672ddc5a2f3a5ef22ba566c`; web suffix `20261026T014656`, public `20261026T014657`. DB `20261026T014656` healthy; web/public restart sayısı 0. Gerçek runtime SMTP relay/587 ve boş SMTP kullanıcı/parola ayarlarını koruyor; denemeler kapalı. `4237-live-images-20261026.png` ve `4237-live-runtime-guards-20261026.png`.

Dağıtım sonrası üç dış HTTPS hostu 200/ready/121/0, çıkış 0 (`4237-live-external-health-20261026.log`). Sekiz yayımlı plan, checkout ve deneme kapalı sınırı korundu (`4237-live-commercial-boundaries-20261026.log`). Kullanıcının üç cihaz fotoğrafı, profil içeren güvenlik önizlemesi, portal ve şeffaf etiket rulosu dahil sekiz görsel/CSS/JS kaynağı yerel dosyalarla SHA256 düzeyinde aynı (`4237-live-assets-20261026.log`).

Canlı ana sayfa yeniden yüklendi; “Tam ekran görüntüle” tıklanınca portal büyütme penceresi açıldı ve Kapat ile geri dönüldü (`4237-live-portal-fullscreen-20261026.png`). Aynı incelemede dört slaytlı galeri seçimi kendiliğinden 1'den 4'e ilerledi; bu gözlem tüm hareket/erişilebilirlik testlerinin yeniden çalıştırıldığı iddiası değildir. Kaynaklar önceki doğrulanmış galeriyle aynıdır.

Başarı ve runtime doğrulamaları **26 Ekim 2026 01:55:56 UTC** saatinden önce gözlendi. Bu saat doğrulama kaydıdır; tam Coolify bitiş saniyesi çıkarılmadı. `docs/PRODUCTION-24H-OBSERVATION.md` planı yeni SHA için uygulanır; 24 saatin tamamı henüz gözlenmedi. Geri dönüş hedefi 124b2d1 ve şema 121'dir.

Yayın iş paketi TAMAM: tam CI, staging/recovery, gerçek SHA ve canlı sağlık/koruma sonuçları kanıtlıdır. Bu, tam Hedef veya satış kabulü değildir.
