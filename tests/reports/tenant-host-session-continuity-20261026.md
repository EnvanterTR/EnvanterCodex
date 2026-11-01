# Yanlış tenant hostunda geçerli ortak oturumun korunması

## Sorun ve sınır

Üretim Compose ortak `.envantertr.com` oturum çerezi kullanıyor. Middleware, şirket üyeliği sorgusuna istek hostunun slug'ını da dahil ediyordu. Geçerli Santa oturumuyla Farma alanına bir API isteği geldiğinde sorgu boş dönüyor ve tüm alt alanlar için oturum çerezi siliniyordu. Aynı sorun, geçerli root platform yöneticisi şirket portalına yanlışlıkla girdiğinde de vardı. Bu kod yolu gerçek Identity/PostgreSQL kabulünde yeniden üretildi. Canlıdaki her geçmiş oturum kapanmasının tek nedeni olduğu iddia edilmez.

## Değişiklik

Önce gerçek oturum, aktif üyelik/şirket ve rol geçerliliği doğrulanır. Geçerli şirket kimliği yanlış hosttan geliyorsa401döner ve endpoint çalışmaz; ortak oturum çerezi silinmez. Geçerli platform yöneticisi de tenant portalında401alır, root oturumu korunur. Süresi bitmiş/iptal edilmiş oturum, aktif olmayan şirket, kaldırılmış üyelik, geçersiz rol/principal kontrolleri korunur. MFA,600s kritik yetki, CSRF, cookie domain veya süre değiştirilmedi.

Bu, aynı tarayıcıda farklı kullanıcıların eşzamanlı bağımsız ortak çerezlere sahip olmasını sağlamaz. Başka hesapla başarılı giriş yine aynı tarayıcının oturumunu değiştirir. Normal cross-tenant404/403kabulünün yerine de geçmez; yanlış-host401korumasını sınar.

## Gerçek izole kabul

- `tenant-host-continuity-red-20261026.log`: değişiklikten önce1/1başarısız. Yanlış host401verirken `EnvanterTR.Session` Set-Cookie ile silindi; assertion bu somut nedenle başarısız.
- İlk düzeltme kapsamı `tenant-host-continuity-green-20261026.log`: şirket+platform yöneticisi2/2, exit0.
- Son kaynak `tenant-host-continuity-expanded-20261026.log`: **5/5**, exit0,1.7294dk. İki geçerli kimlikte yabancı hostGET+PUT401/no-session-Set-Cookie ve kendi alanında aynı kimlikle200. Üç negatif durumda (revokedsession, suspendedcompany, removedmembership) çerez temizliği ve kendi alanında401zorunlu.
- Testler gerçek ASP.NET Core Identity cookie middleware, gerçek PostgreSQL17ve HTTP cookie handler ile çalıştı. Platform MFA yalnız sentetik izole hesaba aittir; canlı MFA/kurtarma kodu kullanılmadı.
- `Verify-AuthErrorFlows`, `Verify-CoolifyTenantRouting`, `Verify-SessionAndTokenDialogs` ve `git diff --check` geçti. Bunlar tamCIyerine sayılmaz.
- Yalnız `tenant-host-continuity-20261026` izole Compose ortamı temizlendi.

## Yayın durumu

### İlk uzak CI ve kaynak sözleşmesi düzeltmesi

40251402034'te Windows işi121343907875başarılı; Linux security121343907959, “Verify SaaS tenant slug profile and session binding” adımında başarısız. Kaynak kontrolü eski `/company\\.Slug == tenantSlug/` biçimini zorunlu tuttuğundan yeni ayrı host guard'ını tanımadı. Aynı hata yerelde `tenant-slug-profile-stale-check-20261026.log` ile yeniden üretildi. Kontrol kaldırılmadı: yeni guard401/return, SignOut olmaması, üyelik/rol geçerliliğinin önce çalışması ve gerçek5senaryo kapsamı zorunlu kılındı. `tenant-slug-profile-fixed-check-20261026.log` geçti; CIyml'deki **80** benzersiz Node tools komutunun tamamı yerelde exit0, `tenant-host-all-ci-source-checks-20261026.log`.

Bu düzeltmede ürün C# kodu değişmedi; önceki5/5kabul geçerlidir. Önceki PostgreSQL işi121343907721inceleme anında çalışıyordu; sonucu başarılı sayılmadı veya gözlem gecikmesiyle yeniden başlatılmadı. Yeni gerçek kontrol düzeltmesi commit'i için tamCIgereklidir. Workflow, yeni push geldiğinde aynıPR'ın eski devam eden işini concurrency politikasıyla iptal edebilir; bu başarısız kaydı silmez.

Kod/evidence commit `c22ce1c` mevcutPR25dalına normal push ile gönderildi. GitHub gerçekPRekranı bu head'i ve üç çalışanCIişini gösterdi: run **40251402034**, PostgreSQLjob **121343907721**. PRdraft kaldı; açıklaması son kapsam,5/5kanıt ve açık canlı kapılarıyla güncellendi. Bu üç işin bitişi henüz görülmedi. Kanıt: tenant-host-continuity-pr25-20261026.png. Yeni bir PR/merge/deploy yapılmadı.

Ürün kodu yerelde düzeltildi; yeni tam uzakCI, staging, taze yedek/restore ve üretim rollout kapıları ayrıca gereklidir. Canlı halen124b2d1; bu rapor yayını yapılmış saymaz. Kullanıcı FarmaMFA'yı şimdi tamamlayamıyor; imza sekmesi loginForm'a dönmüştü. Yeni kod isteği gönderilmedi, imza/PDFve normaloperatör/Farma kabulü tamamlanmadı.

TAMAM dediğim5izole güvenlik senaryosunun5'i gerçek test çıktısıyla doğrulanmıştır.
