## 26 Ekim 2026 — kesin onboarding9585 staging ve kurtarma
- Durum: TAMAM (exactstage/tazeyedek/kurtarma);tamCI/yayınbekliyor. 9585fae1d97980f861dbe519252bc1c422230476webveNginxderlemeleri exit0.
- Taze183807ZSQLrestore,95tabloözgünalankoruması,izoleruntimeveüçhostready122/0geçti. YeniGET/POSTanonim401;gerçekstageproxyüçhosttanCSS200/text-cssveriyor. Migrationveproductionrestoreyok.
- 143MBrollback184132Zpaketiveayrıdosya/imajkurtarmaprovası exit0. Kanıt: tests/reports/onboarding-server-staging-20261026.mdve9585ekleri. Canlı56edeğişmedi.
- KesinCI40379203209PostgreSQL121771140114canlıçalışırken doğrulandı;3işhenüzdevam. Sıradaki aynıCIsonucu,ardındanreleasegatesgeçersetekyayın. YenikullanıcıOTPsiistenmedi.

## 26 Ekim 2026 — onboarding CSS proxy yolu
- Yayınöncesikaynakincelemesi /girisapexaliasında yeniCSSiçin pazarlamaHTMLfallback riskinibuldu. Production/localNginx'e yalnız /company-onboarding.css geçişieklendi;tenantwildcardzaten tümuygulamayollarınıiletiyor.
- GerçekikiNginxizoleproxytesti HTTP200/text-css/içerikiddialarıyla exit0geçti;testkonteyner/ağıtemizlendi. onboarding-proxy-20261026.logvecleanupkanıtı. Eskiadaystagebaşarısıyeniadayyerinesayılmaz;canlı56edeğişmedi.
- Sıradaki kesinCI/yeniadaystage/tazegeri dönüşveyayın.

## 26 Ekim 2026 — onboarding mevcut tarayıcı seçicisi düzeltmesi
- c51394dCI40344206355Linuxgüvenlik121647308303mevcutfirma-profili testindeki belirsiz[data-page]seçicisini yakaladı. Rehberin yeni düğmesiyle ikieşleşmevardı;test #sidebar ilehedeflendi,işlevseliddialarazaltılmadı.
- Tamhostedbrowserimajı yeniden derlendi,tümmevcutentrypoint exit0/78PASSsatırıverdi;78benzersiztestiddiasıyok. Kanıt: onboarding-browser-build/regression-20261026.log. Ürünkodudeğişmedi;kesinyeniCIbekliyor.

## 26 Ekim 2026 — onboarding ilkCI envanter düzeltmesi
- 6cd4f0ePR25'egönderildi;run40342129450Linuxgüvenlik12163934488216s'de statikUIenvanterinin eskiolduğunu buldu. TamCIgeçti sayılmadı,canlıdeğişmedi.
- docs/ui-inventory.md kaynaktan yenidenüretildi:2304kontrol/71rotakontrolü/976düğme. Envanter--check ve2260satırlık5dildictionarykontrolü geçti;korumalargevşetilmedi. Kanıt: tests/reports/company-onboarding-20261026.md eki.
- Sıradaki kesin yeniCI;tamstagingveyayınkapıları açık.

## 26 Ekim 2026 — beş adımlı şirket kurulumu yerel kabulü
- Durum: TAMAM (yerel kod/API/UI kabulü); tamCI/staging/yayın henüz açık. Canlı56e/schema122korunuyor.
- GenelBakış'a gerçek firma profili/konum/çalışan/varlık/zimmet kayıtlarından hesaplanan5adımlı rehber eklendi. Mevcut formlara gider; boş/mükerrer başlangıç verisi veya kullanıcı hesabı oluşturmaz. settings.manage+üyelik+CSRFve normal sınırlar; şirket satırkilidiyle tek tamamlama audit'i. Migration/auth değişikliği yok.
- İlk API1/3ardından timestamp düzeltmesiyle6/6regresyon44.3536sgeçti. Yetkisizmembership401,viewer403,CSRF400,eksik409; diğerşirket/pasif/arşiv/silinen kayıtlar sayılmaz,paralel tamamlama tekolay/timestamp. İlk hatalar saklandı.
- Chromium sentetiktransportta5rota,kısmi/tamhazır/hata/yenileme,CSRFPOST,tekraraçılışta tamamlandı,viewerhidden,açık/koyutema,390pxtaşmasızlık veJSerrorsız geçti. Birleşik gerçek müşteri kabulü sayılmaz.
- Kanıt: tests/reports/company-onboarding-20261026.md, API/build/UIlogları veonboarding-ui-20261026PNGleri. Yeni tarayıcı senaryosu CI'a eklendi. Sıradaki kesinCIveyayınkapıları; merchant/Santaimza açık.
- TAMAM dediğim1yerel iş paketinin1'i test çıktısıyla doğrulanmıştır.

## 26 Ekim 2026 — ücretli/deneme e-posta düzeltmeleri canlıda
- Durum: TAMAM (bu iki düzeltmenin sürüm yayını ve anlık canlı kontrolü); bütün hedef/satış hazır değil.
- Exact56e4083 üç CI işi başarılı. İzole gerçek sunucu build/staging, taze134409ZSQLrestore,95tablo eski alan koruması ve143MB135152Zgeri dönüş paketinin135452Zdosya/imaj kurtarma provası geçti. Migration yok.
- Coolify0woxca0tgg33mdcozvfyga2q Success4m37s; gerçek web/public56eimajları, DBhealthy, sıfırrestart, webreadonly, SMTPrelayTLS587 ve kapalı ödeme/deneme sınırları doğrulandı. Kaydedilmiş HTTPScallbackruntime'a uygulandı; merchantAPI/secret hâlâ eksik.
- Üçhostready122/0;8plan,8galeridosyası kaynak hash eşitliği, uygulamaHTML eşitliği ve anonimPDF/e-posta401kontrolü geçti. Kanıt: tests/reports/paid-trial-notification-release-20261026.md ve56eekleri. Gerçek müşteri kaydı/maili/ödemesi oluşturulmadı.
- Kullanıcı iyzico hesabı olmadığını doğruladı; anahtar tekrar istenmez. Sıradaki P0: Hedef12.1beşadımlı onboarding; docs/P0-ONBOARDING-GAP.md. Gerçek Santa/imza/merchant kabulü açık.
- TAMAM dediğim1yayının1'i test çıktısıyla doğrulanmıştır.

## 26 Ekim 2026 — ücretli/deneme e-posta düzeltmeleri CI'da
- Durum: TAMAM (PR/CI sevki); tam uzak kabul ve yayın bekliyor.
- 56e40838f702063203178915145770de7010a009 normal push ile mevcut PR25'e gitti. Gerçek PR üç işi başlatılmış gösterdi: run40331368345,PostgreSQLjob121601340401. PR açıklaması son SMTP/rollback kapsamı ve32/11yerel grup kanıtlarıyla güncellendi; gruplar örtüşür,43benzersiztest iddiası yok.
- Kanıt: tests/reports/trial-welcome-20261026.md eki ve trial-welcome-pr-ci-started-20261026.png. Yerel sentetik Compose DB/ağı temizlendi; iki test imajı korunuyor. Canlı5018/schema122 ve ödeme/deneme kapalı durumu değişmedi.
- Sıradaki: aynı CI sonucunu izleme, exact56e staging/taze kurtarma ve yayın. Yeni OTP veya merchant sorusu açılmadı.

## 26 Ekim 2026 — deneme kaydına erişim e-postası ve hata geri alması
- Durum: TAMAM (yerel kod ve kabul); canlıya alınmadı.
- Önceki akış son erişim e-postasını göndermeden şirketi kesinleştiriyordu. İlk iki test bunu200/503uyuşmazlığıyla üretti. SMTP kabulü25s sınırla mevcut transaction içine alındı; hata503döndürür ve deneme hakkını tüketmeden yeni kayıtlar geri alınır.
- Son regresyon11/11geçti: yeni/mevcut owner, aynı proof ile paralel yeniden denemede tek şirket/tek15gün/tekmail, doğruportal, parola/stamp, eskiCSRF/plan/süre/kota/SMTPgizlilik ve ücretli kurulum regresyonları. Web build0hata/0uyarı; iki kaynak denetimi başarılı.
- Kanıt: tests/reports/trial-welcome-20261026.md ve ilk/build/regresyon/source logları. Migration veya arayüz değişikliği yok. Canlı5018/schema122,değişmedi.
- Sıradaki: ücretli+deneme düzeltmelerinin tam uzak CI'ı, exact staging ve taze yedek/geri dönüş; gerçek merchant ve Bölüm20kabulü açık.
- TAMAM dediğim1düzeltmenin1'i test çıktısıyla doğrulanmıştır.

## 26 Ekim 2026 — ücretli yeni şirket kurulumunda SMTP hata geri alması
- Durum: TAMAM (yerel düzeltme ve kabul); canlıya alınmadı.
- İlk iki test gerçek hatayı izole ortamda üretti: SMTP reddinde sipariş Provisioned kalıyordu. Yeni şirket kaydı/rol/abonelik, SMTP kabulünden önce commit edilmiyor;25s sınır var. Hata rollback sonrası doğrulanmış ödeme referansı ProvisioningFailed olarak korunur.
- Son hedefli2/2 ve ilgili geniş regresyon32/32 geçti; başarılı tek audit kaydı, aynı ödemeyle yeniden deneme, yeni/mevcut owner ve parola/stamp koruması denetlendi. Web build0hata/0uyarı; iki kaynak denetimi başarılı.
- Kanıt: tests/reports/paid-provisioning-email-20261026.md ve ilk/son/regresyon/build/source logları. Migration yok, canlı5018/schema122 ve kapalı ödeme/deneme sınırı korundu.
- Sıradaki: deneme kaydının hoş geldin bildirimi; ardından tam uzak CI ve yayın kapıları. SMTP dış yan etkisinin DB ile atomik geri alınabileceği iddia edilmedi.
- TAMAM dediğim1düzeltmenin1'i test çıktısıyla doğrulanmıştır.

## 26 Ekim 2026 — gerçek merchant bağımlılığı kullanıcıdan doğrulandı
- Durum: ENGELLENDİ (yalnız gerçek iyzico kabulü; ürün entegrasyonunun yerel testiyle karıştırılmadı).
- Kullanıcı henüz iyzico hesabı olmadığını bildirdi. Resmî Türkçe başvuru sayfası kontrol edilip boş form hazır bırakıldı. Kişisel bilgi/credential gönderilmedi, sözleşme kabulü veya hesap oluşturma yapılmadı.
- Kalıcı bağlam ACCOUNT-SETUP-CONTEXT ve P0-CHECKOUT-GAPS başına yazıldı; aynı APIanahtarı sorusu yeniden açılmayacak. Gate8raporu güncellendi. Callback hazırlığı kaydedilmiş durumda;runtime değişmedi,ödeme/deneme kapalı.
- Sıradaki bağımlılık: kullanıcının merchant başvurusu/onayı ve kendi şirket oturumu/imza kabulü. Tek bu engelde tüm hedef blocked olarak işaretlenmedi; eşik denetimi korunur.

## 26 Ekim 2026 — Kapı8 kanıtı ve iyzico callback hazırlığı
- Durum: TAMAM (callback ayarının kalıcı kaydı ve15.9kapı raporu); Kapı8GEÇMEDİ.
- Kodun kabul ettiği HTTPS callback mevcut üretim değişkenine kaydedildi, reload sonrası doğrulandı. Preview değişmedi;redeploy yok,runtimeAPI/secret eksik,checkout/deneme kapalı.
- Gate8dosyasında15.10'un dört alt şartı doğrudan kaynak kanıtlarıyla eşlendi. 24saatlik plan gerekliliği tamamlanmış24saatgözlemle karıştırılmıyor;gereksizbeklemeşartıyok.
- Yeni gerçek Santa sekmesi loginForm gösterdi. Mevcut çalışan profili/belge Devam et yolu kodda doğrulandı;gereksiz yeni yönlendirme/yetki gevşetmesi yapılmadı. Kullanıcıdan yeniden MFAistenmedi.
- Kanıt: tests/reports/gate-8-20261026.md,iyzico-callback-saved-20261026.png. Sıradaki:gerçek merchant yapılandırması ve kullanıcıya ait şirket/imza kabulü.09:57:18UTCüçhostready122/0.

## 26 Ekim 2026 — imzalı PDF/e-posta sürümü canlıda
- Durum: TAMAM (CI, eklemeli migration, kesin sürüm yayını ve ilk kontroller).
- CI 40296954613 üç işte başarılı; PostgreSQL işi 24m54s. Kaynak 5018bc5 kaydedilip yeniden yüklenerek doğrulandı. Doğrulanmış SQL üretimde exit0; şema122, yeni gönderim tablosu boş.
- Coolify zsmt40hqy0qdjaha4eiu5mab Success/Finished. Gerçek web/public imajları5018; DB healthy, restart0, SMTP relay TLS587 korundu. Dış üç host122/0, anonim belge uçları401, sekiz plan ve kapalı checkout/deneme sınırı, sekiz görsel ve şirket HTML hashleri, tam ekran aç/kapat kontrolleri geçti.
- Kanıt: tests/reports/production-handover-pdf-email-release-20261026.md. Sıradaki: gerçek Santa imza/PDF/e-posta ve kalan Bölüm20/iyzico kabulü. 24 saat gözlem tamamlanmadı; kullanıcıdan tekrar kod istenmedi.
- TAMAM dediğim 3 yayın maddesinin 3'ü test/dağıtım çıktısıyla doğrulanmıştır; gerçek müşteri belge kabulü bu üç maddeye dahil değildir.

## 26 Ekim 2026 — 5018 için taze geri dönüş provası
- Durum: TAMAM (yedek/dosya/imaj kurtarma); yeni canlı yayın yok.
- Güncel091423Z SQL yedeğiyle141MB rollback-20261026T092057Z oluşturuldu,exit0. Ayrı-drill-20261026T092201Z dizinine medya/anahtar arşivleri açılıp karşılaştırıldı;önceki iki uygulama imajı yüklendi,exit0.
- Kanıt:tests/reports/5018-server-staging-20261026.md,5018-rollback-drill-pass-20261026.png. CI40296954613PostgreSQL121485218470son gözlemde17dakikada sürüyor;yeniden başlatılmadı. Sıradaki:aynı koşunun sonucu ve ancak başarı sonrası canlı122 geçişi.

## 26 Ekim 2026 — 5018 kesin aday sunucu provasından geçti
- Durum: TAMAM (yalnız izole staging/veri koruma); canlı yayın açık.
- PR25 head5018bc5 gönderildi. CI40296954613 Linux security ve Windows başarılı, PostgreSQL işi son gözlemde sürüyor.
- Exact imaj build exit0. Taze üretim SQL yedeği ayrı DB kopyasına yüklendi;121→122 SQL iki kez çalıştı,94 mevcut tablonun özgün alan projeksiyonları eşit kaldı. İzole runtime üç host ready122/0, anonim varlık ve PDF/e-posta401,CSRF/Secure kontrolleri başarılı.
- Kanıt: tests/reports/5018-server-staging-20261026.md ve5018-stage-http-pass-20261026.png. Sıradaki: aynı CI tamamlanması, taze rollback/dosya-imaj provası ve yayın. Üretim4237/schema121.

## 26 Ekim 2026 — imzalı PDF için kontrollü e-posta gönderimi
- Yerel uygulama ve kabul tamamlandı; canlıya çıkmadı. Kayıtlı çalışan adresi ve belge gösterilerek açık onay alınır; şirket/konum/rol/CSRF ve imza bütünlüğü korunur.
- Kalıcı gönderim kaydı aynı anahtarla paralel/tekrar istekte ikinci SMTP gönderimini engeller. Belirsiz sonuç otomatik tekrarlanmaz; yeniden gönderim ayrı onay ister. SMTP kabulü alıcı teslimi olarak sunulmaz.
- Beş API/PDF senaryosu, ayrı migration testi ve Chromium akışı başarılı. 121→122 eklemeli SQL iki kez uygulanarak mevcut alan hashleri korundu. Tam uzak CI/staging/güncel yedek ve geri dönüş kapıları açık; canlı4237/schema121.
- Kanıt: tests/reports/handover-email-20261026.md. Gerçek Santa imzası ve Farma MFA kullanıcı adımları bekliyor; yeniden kod istenmedi.

## 26 Ekim 2026 — CI'daki eski tenant host sözleşmesi düzeltildi
- 40251402034Linuxsecurity121343907959, eskiSQL-içi slugkarşılaştırmasıregexinde başarısız; Windows121343907875başarılı, PostgreSQL121343907721inceleme anında sürüyor. Yerelde aynı Nodehatası yeniden üretildi; ürünC# kodu değiştirilmedi.
- Kontrol kaldırılmadı: ayrıhostguard401/return, çerezsilmemesi, öncesindeüyelik/rol geçerliliği ve5gerçekkabultestinin varlığı zorunlu oldu. CIyml80benzersizNodetools komutu yerelde tamamıexit0;5/5Identitykabul mevcutürünkoduiçingeçerli.
- Kanıt: tenant-host-session-continuity-20261026.md eki, stale/fixed/all-ci-source logları, c22-ci-source-contract-failed-20261026.png. Yeni kontrolcommit'i için tamCIbaşlatılacak; mevcutbaşarısızlık saklandı, canlı124değişmedi.

## 26 Ekim 2026 — oturum sürekliliği düzeltmesi PR25/CI'da
- c22ce1c normal push ile mevcutfeat/verified-cloud-trials-20261021dalına gitti. GerçekPR25aynıhead'i ve üççalışanCIişini gösterdi:40251402034/PostgreSQLjob121343907721. PRdraftkorundu, açıklama sonkapsam/5test/açıkyayınkapılarıyla güncellendi.
- Kanıt: tenant-host-session-continuity-20261026.md eki ve tenant-host-continuity-pr25-20261026.png. CIhenüzbitmedi; eski124başarısı yeniheadyerine sayılmaz. Sonrakiadım aynıCI'ıtamamlananakadartakip ve kesinadayıstaging/güncelyedek/restore ilehazırlamak. Üretim ve kullanıcıMFAayarları değişmedi.

## 26 Ekim 2026 — yanlış host isteğinin geçerli oturumu silmesi düzeltildi
- Durum: TAMAM (yerel kod+izole5senaryo); yeni tam uzakCI/staging/yayın açık, canlı124değişmedi.
- Ortak domain cookie ile başka şirket hostuna gelen geçerli oturum isteğinde SignOut üretildiği gerçek Identity/PostgreSQL testinde1/1başarısızlıkla yeniden üretildi. Üyelik/rol geçerliliği ve hostuyumu ayrıldı; geçerli kendi-şirket/rootadmin oturumu yanlışhost401sonrasında korunur.
- Son test5/5exit0,1.7294dk: iki geçerli kimlik, üç geçersiz (revoked/suspended/removedmembership), yabancıGET/PUTreddi, cookie ve kendi-host dönüşü. İlk2/2ara kapsam ayrılogda. MFA/süre/CSRF/domain değişmedi. İzoleCompose temizlendi.
- Kanıt: tests/reports/tenant-host-session-continuity-20261026.md ve red/green/expandedloglar. Üç kaynak doğrulaması+diffkontrolü geçti.
- Sıradaki: mevcutPR25'e gönder, tamCI; sonra güncelstaging/yedek/restore ve aynıkanıtlıSHAyayını. Kullanıcı şu anMFAyapamıyor, tekraristenmedi. Santa7/10vefullHedef açık; imza/operatör/Farma/cross-tenant/iyzico tamamlanmadı.

## 26 Ekim 2026 — Santa operatör erişim e-postası gerçek dış teslimatı
- Durum: TAMAM (yeni erişim iletisinin Microsoft SMTP kabulü); operatör login/yetki ve imza/PDF açık.
- Google günlüğü26Ekim01:18:45TRT c.belge alıcısı için52.101.202.157/TLS teslim1/1, kaynak169.58.53.189/TLS. Inbox/spam konumu doğrulanmadı.
- Kanıt: santa-e2e-20261025/santa-operator-access-mail-delivered-20261026.png ve operator-handover-preparation-20261026.md eki.
- Kullanıcı imza penceresini bulamadı/FarmaMFAşimdi yapamıyor. Santa belge sekmesi gerçektenloginForm'a dönmüş. Yeniden kod istenmedi; ortak çerezin yanlış tenant isteğinde silinmesi ayrı gerçek Identity testinde yeniden üretildi. Sıradaki: erişim reddini koruyarak geçerli oturumun gereksiz silinmesini düzeltmek; henüz canlı değişmedi.

## 26 Ekim 2026 — onaylı Santa operatör üyeliği ve aktif zimmet belgesi
- Durum: TAMAM (onaylanan üyelik ekleme/DBrol koruma ve imzasız aktif teslim snapshot); tam Editör ve imza/PDF kabulü açık.
- Kullanıcının açık eylem anı onayıyla c.belge normal davet formundan Santa asset_operator oldu. DB Demo3owner/Farmaowner/Santaasset_operator, her biri1üyelik; diğer roller korundu. Ürün e-posta gönderim yanıtı verdi, teslimat henüz ayrı doğrulanmadı.
- Varlık yeniden kabul çalışanına normal zimmetlendi; ilk boşimzasız taslak iptal edilip yeni ZMB-20261025-a11cc2306 oluşturuldu. DB1doğru varlık/Santa kapsamı, iki imza bekliyor. Kullanıcıya iki imza alanı ve print adımı bırakıldı.
- Farma normal parola doğrulandı; açık sekme c.belge Authenticator kodu bekliyor. Santa owner korunuyor. Kurtarma kodları veya yeni credential kullanılmadı.
- Kanıt: tests/reports/santa-e2e-20261025/operator-handover-preparation-20261026.md;4ekran/2SQL. Santa7/10değişmedi; userimza ve FarmaMFA soruları açık. SonraPDF/e-posta ve gerçekoperatör yetki/Farma/cross-tenant.

## 26 Ekim 2026 — Santa lisans, CSV ve gerçek Arşivden geri alma
- Durum: TAMAM (20.2.5/.7/.9); Santa7/10, tam Bölüm20/ödeme açık.
- Normal owner ile iki koltuklu lisans/çalışana1atama; UI1/2 ve DB2toplam/1atanmış/1kalan, CompanyId eşitlikleri true. CSV indirildi; tek Santa satırı ve aynı sayımlar dosyadan doğrulandı.
- Ayrı çöp kutusu/geri al ve gerçek Archived→Arşivden geri al→Available normal formlardan geçti. Aynı varlık alanları(Status/UpdatedAt hariç) tutarlılık özeti eşit, bakım1korundu, geçmiş6→8→9→10. READONLY sorgular exit0. Liste arşiv eyleminin çöp kutusu açması adlandırma tutarsızlığı olarak kaydedildi.
- Kanıt: tests/reports/santa-e2e-20261025/license-csv-archive-20261026.md;CSV,SQLve10ekran.
- Sıradaki: PDF/imza/e-posta; Editör için c.belge hesabına mevcut asset_operator sınırlı Santa erişimi taslağı hazır, eylem anı kullanıcı onayı bekleniyor. Henüz davet/üyelik eklenmedi. Logout en sonda; Farma/cross-tenant/gerçek ödeme açık.

## 25 Ekim 2026 — normal Santa owner, varlık, zimmet ve bakım kabulü
- Durum: TAMAM (20.2.1/.2/.3/.6); tam Bölüm20 ve ödeme açık.
- Kullanıcı Santa QR kurulumunu tamamladı; gerçek normalowner dashboard Santa adıyla açıldı. Yeni varlık/kategori, kabul çalışanı ve normal zimmet oluşturuldu. Bakım planı, çalışan iadesi, başlatma ve tamamlama normal ürün formlarından geçti.
- SQL: gerçek varlık8041958a-e66b-4b7d-a2d1-4748235a08c2 SantaCompanyId3526d59e-d0bb-4755-a90e-6f3e7eccc341; kategori/çalışan/olay kapsamları true. BakımCompleted, varlıkAvailable,6olay aynıCompanyId. İki READONLY sorgu exit0.
- Kanıt: tests/reports/santa-e2e-20261025/normal-owner-asset-maintenance.md;6ekran ve2SQL. Credential/QR/kurtarma kodları kullanılmadı veya kaydedilmedi.
- Sıradaki: PDF/imza/e-posta alt adımları, lisans/koltuk, CSV, Editör, arşiv/restore/logout, Farma ve canlıGET/PUT. Mevcut Santa normal oturumu açık; tekrar giriş gereksiz.

## 25 Ekim 2026 — Santa aktif, gerçek parola kabul edildi; token tekrar kullanımı incelendi
- Durum: TAMAM (gerçek EmailConfirmed + parola doğrulaması ve izole token ret/retry/replay testi); Santa MFA/dashboard/kalan Bölüm20 açık.
- Taze ayrı platform sekmesi Santa Doğrulandı gösterdi; eski cache kanıt sayılmadı. Yeni davet reauth iptal edildi, ikinci e-posta gönderilmedi. Normal Santa parolası kabul edilip ilk MFA QR kurulumu açıldı; kullanıcıya Santa QR adımı bırakıldı. Yeni platform kodu/davet gerekmez.
- Test: gerçek SMTP e-postasındaki fragment token ile kısa ve Identity kuralı retleri, kimlik alanı koruma, aynı token'la aktivasyon ve tekrar reddi; son kapsam1/1 exit0,1.1185dk. Ürün değişmedi/deploy yok. İzole Compose temizlendi.
- Kanıt: tests/reports/invitation-password-retry-20261025.md, genişletilmiş test logu ve santa-owner-email-confirmed-20261025.png. Credential/QR/token repoya alınmadı.
- Sıradaki: kullanıcının Santa QR kurulumu; sonra normal owner dashboard ve10adım. Gerçek ödeme açık.

## 25 Ekim 2026 — Santa erişim daveti gerçek gönderildi
- Durum: TAMAM (normal platform eylemi + Google dahili sunucusuna teslim1/1); hesap aktivasyonu/owner girişi henüz doğrulanmadı.
- Kullanıcının girişinden sonra Santa Detay → Yetkili erişim e-postasını yeniden gönder kullanıldı. Google günlüğü22:39:31 TRT cloudmailhome alıcısı, IPv4/TLS ile teslim gösterdi; inbox/spam konumu iddia edilmedi.
- Kanıt: tests/reports/santa-owner-invitation-google-delivered-20261025.png; santa-e2e-20261025/README.md ek bölümü.
- Sıradaki: kullanıcı davet bağlantısından parolasını belirleyip hesabı etkinleştirecek (credential handoff); ardından normal Santa owner ve Bölüm20. Davet token'ı okunmadı, DB doğrulaması atlanmadı, yeni OTP sorusu açılmadı.

## 25 Ekim 2026 — Farma dış alıcı e-posta teslimatı
- Durum: TAMAM (gerçek uygulama iletisinin Hotmail SMTP sunucusuna teslimi); inbox/spam konumu ve tam owner kabulü doğrulanmadı.
- Normal Farma portalındaki kurtarma formu tek istekte kullanıldı. Google günlüğü19:20:48 TRT Microsoft52.101.60.70/TLS teslim1/1, kaynak169.58.53.189/TLS gösterdi. Parola değişmedi, token okunmadı.
- Kanıt: google-relay-farma-hotmail-delivered-20261025.png ve google-relay-production-acceptance-20261025.md ek bölümü.
- Santa davet yeniden gönderimi normal kritik doğrulama bekliyor; açık soru tekrar edilmedi, yeni MFA atlatma/süre uzatma yok. Sıradaki: bu doğrulama sonrası Santa daveti, aktivasyon ve Bölüm20 normal kullanıcı kabulü; gerçek iyzico açık.

## 25 Ekim 2026 — gerçek SMTP ve uygulama parola yenileme teslimatı
- Durum: TAMAM (relay + runtime + gerçek parola yenileme e-posta teslimi); tam P0/Bölüm20/ödeme açık.
- Kullanıcının eylem anı onayı sonrası Google tek IPv4/kayıtlı kullanıcı/TLS kuralı etkinleşti; yeni credential veya MFA değişikliği yok. IPv6 host denemesi reddi korundu, yetki genişletilmedi; IPv4 testi gerçek posta kutusuna teslim.
- Coolify p0flhitgplemaxbk2548ll8a Success/Finished, aynı124 SHA; runtime160415. Yeni SQL/rollback/restore gate'i kullanıldı. Sır göstermeyen çalışma zamanı SMTP eşitlik kontrolleri geçti; üç HTTPS host200/121/0.
- Normal uygulama Parolamı unuttum akışının admin adresine iletisi Google günlüğünde19:10:59 TRT Gmail posta kutusuna teslim1/1, kaynakIPv4/TLS. Reset bağlantısı açılmadı ve parola değişmedi.
- Kanıt: tests/reports/google-relay-production-acceptance-20261025.md ve yedi ekran; health logu.
- Sıradaki: Santa davet/aktivasyon ve normal owner kabulü; gerçek iyzico. Eski uygulama parolası/SMTP engeli artık güncel değildir.

## 25 Ekim 2026 — son şirket durumunu içeren güncel kurtarma noktası
- Durum: TAMAM (yedek + ayrı dosya/imaj provası + izole SQL restore); gerçek SMTP/ödeme ve tam Bölüm 20 açık.
- Değişen dosyalar: relay-preparation-recovery-20261025.md, ACCOUNT-SETUP-CONTEXT.md ve üç kanıt görüntüsü.
- Komut + çıktı: taze 145446Z SQL SHA doğrulandı; 140 MB 145517Z bundle exit 0; 145609Z drill exit 0; ayrı restore DB başarıyla oluşturuldu.
- Test: gerçek canlı/restore 95 public tablo sayımı eşit, COUNT_COMPARE_EXIT=0. Bu satır içerik eşitliği veya uygulama E2E değildir.
- Kanıt yolu: tests/reports/relay-preparation-recovery-20261025.md.
- Sıradaki iş: eylem anı onayı gelince sınırlı Google relay ayarını kaydetmek; henüz Google/Coolify relay veya çalışan imaj değiştirilmedi. Yeni OTP istenmedi.

## 23 Ekim 2026 — P0-A gerçeklik denetimi güncellemesi
- Durum: ENGELLENDİ (yüzde ve canlı sağlık denetimi tamamlandı; yönetici oturum ekranı kanıtı alınamadı).
- Değişen dosyalar: `docs/AUDIT-REALITY.md`, `docs/PROGRESS.md`.
- Komut + çıktı: Yüzde iddiaları 11 `PROJECT-STATUS.md` + 2 `PRODUCT-COVERAGE.md` + 0 `ROADMAP.md` olarak doğrulandı. `node tools/Check-CoolifyProduction.mjs` tekrarında apex ve firma HTTP 200 `ready` 118/0; canlı plan sayısı 0; checkout `available:false`. PR #25 HEAD `036b52a654da6cda87f7dec15285373303f4f9e5`; Linux güvenlik, Linux PostgreSQL ve Windows deployment/backup CI işleri 3/3 success (run `40008494630`). Aday migration sayısı 119; `origin/main` 118; fark `20261023190000_AddExistingCompanyCheckoutContext` ve model snapshot.
- Test: Bu denetim salt-okunur. Yüzde kod/test izleri `docs/AUDIT-REALITY.md` tablosunda; CI sonucu aynı commit için tam üç işin başarılı olduğunu doğruluyor. Canlı admin girişi yapılmadı; mevcut Windows bilgisayar-kullanım yönergesi auth pencerelerini otomatikleştirmeyi yasakladığından ekran görüntüsü adımı ENGELLENDİ.
- Kanıt yolu: `docs/AUDIT-REALITY.md`; `node tools/Check-CoolifyProduction.mjs` çıktısı; https://github.com/EnvanterTR/EnvanterCodex/actions/runs/40008494630.
- Sıradaki iş: P0-B'de canlı migration kimliklerini doğrulanmış kopya DB ile karşılaştır; tam eşitlik public health endpoint'iyle kanıtlanamıyor. Canlı admin görüntüsü için kullanıcı oturum açmalı veya izinli başka doğrulama yolu sağlanmalı.

### 23 Ekim 2026 — P0-B canlı migration kimlik eşleşmesi
- Durum: TAMAM (dağıtılmış canlı baseline ile `origin/main` birebir eşleşti; aday release migration'ı ayrı ve uygulanmamış).
- Değişen dosyalar: `docs/AUDIT-REALITY.md`, `tests/reports/p0b-migration-identity-acceptance-20261023.md`, `docs/PROGRESS.md`.
- Komut + çıktı: Coolify PostgreSQL konteynerinde yalnız `BEGIN READ ONLY`/SELECT/ROLLBACK sorgusu; canlı 118 ID, `origin/main` 118 benzersiz EF migration ID; uç kimlikler ve sıralı ID-listesi MD5 `c8694e4a153c5f81d5cface1e0c9f366` aynı. `git diff --check` doğrulandı.
- Test: Ek P0-B raporunda canlı ve kaynak kümesi count/range/hash eşitliği; aynı günkü taze dump + medya/anahtar restore eşitliği `coolify-sha-save-triage-20261023.md` ile birlikte. Run `40019065157`, head `217261746e62a5f6debbe6010b71e40a033c7b2d`, Linux tenant security + Linux PostgreSQL + Windows deployment/backup **3/3 success**.
- Kanıt yolu: `tests/reports/p0b-migration-identity-acceptance-20261023.md`, `tests/reports/coolify-sha-save-triage-20261023.md`, GitHub run `https://github.com/EnvanterTR/EnvanterCodex/actions/runs/40019065157`.
- Sınır: PR #25 adayı canlı baseline'dan yalnız `20261023190000_AddExistingCompanyCheckoutContext` ile ileride; `Up()` additive/nullable, canlıya uygulanmadı. Staging uygulaması, yeni release için taze yedek/rollback kapısı ve iyzico/SMTP gerçek kabulü hâlâ gerekir.
- Sıradaki iş: P0-D mevcut şirket checkout'un browser kabulinde provider failure/mismatch/target deletion ve mobil sonucu kapsa; sonra P0-E gerçek sandbox ve Bölüm 20 firma senaryolarına geç. Gerçek staging/provider erişimi olmadan canlı migration/deploy/satış açma.

## 23 Ekim 2026 — P0-A durum yüzdesi denetimi
- Durum: ENGELLENDİ
- Değişen dosyalar: `docs/AUDIT-REALITY.md`, `docs/PROGRESS.md`
- Komut + çıktı: Üç durum dokümanı tarandı; 13 yüzde satırının tamamı 15–16 Ekim tarihli. Salt-okunur canlı GET: apex ve firma `ready` 118/0; plan listesi boş; checkout kullanılamıyor. Düzeltilmiş EF migration sınıf sayısı snapshot hariç 118; `origin/main` migration dosyalarıyla aynı. Public health migration kimliklerini göstermiyor.
- Test: `Verify-HostedCheckoutBrowserAcceptance.mjs` geçti. `Verify-PlatformAdminExperience.mjs` ve `Verify-AssetModelDetailsUi.mjs` başarısız; ayrıntılar denetim dosyasında. Tam hosted Chromium paketi exit 1; eski sınırsız Cloud kotası assertion’ı.
- Kanıt yolu: `docs/AUDIT-REALITY.md`; mevcut endpoint/test izleri tabloda.
- Sıradaki iş: P0-B — production-clone raporundaki yedek/geri yükleme provası var; canlı migration kimliklerinin birebir karşılaştırması ve geri dönüş kabulünün güncelliği teyit edilecek. Canlı yönetici ekran görüntüsü, bilgisayar-kullanım aracı URL güvenlik engeli nedeniyle alınamadı; parola forma gönderilmedi.

### 23 Ekim 2026 — Görsel PR #24 canlı SHA doğrulaması
- Durum: TAMAM
- Değişen dosyalar: `tests/reports/coolify-main-sha-verification-20261023.png`, `docs/PROGRESS.md`
- Komut + çıktı: `git fetch origin main` sonrası `origin/main=4d55270827be312775ebffe60c5a6d74011d7802`. Coolify dağıtım geçmişindeki en yeni başarılı kayıt `4d55270` ve açıklaması PR #24 merge commit'i; uygulama `Running`.
- Test: `https://envantertr.com` canlı ana sayfasında etiket galerisinin 4 görselli otomatik döngüsü ve duraklatma düğmesi mevcut; portal önizlemesindeki “Tam ekran görüntüle” düğmesi tam ekran katmanını açtı. Görsel PR'ın doğru SHA'sı üretimde zaten çalışıyor; bu turda yinelenen deploy tetiklenmedi.
- Kanıt yolu: `tests/reports/coolify-main-sha-verification-20261023.png`; canlı arayüz doğrulaması 23 Ekim 2026.
- Sıradaki iş: P0-B — migration kimliklerinin canlıyla karşılaştırılması, taze yedek ve geri dönüş kabulünün doğrulanması.
- Not: Coolify Git Source sayfası ayrı sekmede de “You have changes that haven't been saved yet” gösteriyor ve kaydetme denemesi bu göstergeden kurtarmadı; kaynak alanında main + doğru SHA görünmesine ve başarılı deploy kaydına rağmen bu UI uyarısının kök nedeni ENGELLENDİ. Canlı deploy geçmişi ve canlı sayfa kanıtı doğru revizyonun yayında olduğunu doğruluyor.

### 23 Ekim 2026 — P0-B canlı yedek tazeliği kontrolü
- Durum: TAMAM (başlangıçta ENGELLENDİ; aynı gün aşağıdaki kayıtta yeni yedek ve restore provasıyla kapatıldı)
- Değişen dosyalar: `docs/PROGRESS.md`
- Komut + çıktı: Coolify production projesi 1 kaynak gösteriyor (uygulama); uygulamanın Backups sayfasında `Schedules: 0`, `Enabled: 0`, `Total executions: 0` ve `No scheduled backups` yazıyor.
- Test: İlk salt-okunur Coolify UI kontrolünde 23 Ekim için taze yedek kanıtı bulunmadı. Daha sonra aynı gün taze PostgreSQL + medya/anahtar yedeği ve izole restore provası tamamlandı; eşit satır sayımları aşağıdaki P0-B kabul kaydında doğrulandı. Coolify'de otomatik yedek planı ve S3 hedefi hâlâ yok.
- Kanıt yolu: Coolify `/project/laj1kik2jvyedpoxirxvuukc/environment/qsgjulvq6cfxpxpvunw7ua8e/application/izrckbfan6xop8b6xgigduvt/backups`; nihai yedek/restore ayrıntısı `tests/reports/coolify-sha-save-triage-20261023.md`.
- Sıradaki iş: Aynı sunucudaki yedek ve geri yükleme kapısı doğrulandı. Otomatik/off-site yedek yapılandırması ayrı açık iş olarak kalır; canlıya yeni sürüm alma hedefteki staging + yedek + rollback koşullarına bağlıdır.

### 23 Ekim 2026 — Coolify SHA kaydetme uyarısı ve canlı yedek geri yükleme kabulü
- Durum: TAMAM (SHA/kaydetme teşhisi ve aynı sunucuda geri yükleme provası); Coolify uyarısının kök nedeni ENGELLENDİ.
- Değişen dosyalar: `tests/reports/coolify-sha-save-triage-20261023.md`, `docs/PROGRESS.md`.
- Komut + çıktı: `git fetch origin main` → `origin/main=4d55270827be312775ebffe60c5a6d74011d7802`. Coolify Git Source `main` ve aynı SHA'yı gösteriyor. Kaydetme `Success Application source updated!` bildirimi verdi; yenileme ve Reset sonrasında kaydedilmemiş değişiklik bandı kaldı. Dağıtım geçmişindeki en yeni başarılı kayıt aynı SHA (PR #24, 2026-10-21 17:23:57 UTC); yeniden deploy edilmedi.
- Test: Canlı ve izole restore DB tabloları/satırları eşleşti: 95 tablo, 168 satır, MD5 `49d77bc6ef149bb3da96fa29d65dcfa6`; restore migration geçmişi 118. Geçici ağsız tmpfs konteyner kaldırıldı.
- Kanıt yolu: `tests/reports/coolify-sha-save-triage-20261023.md`, `tests/reports/coolify-main-sha-verification-20261023.png`.
- Sıradaki iş: Coolify uyarı bandının kök nedeni arayüzden belirlenemedi; mevcut SHA canlıda zaten başarılı olduğundan yayımlama tamam. P0 akışındaki sıradaki engel/işe geç; sonraki canlı deploy öncesi hedefteki staging + taze yedek + geri dönüş kapısını uygula.

### 23 Ekim 2026 — P0-C firma alt alanı canlı sağlık kontrolü
- Durum: TAMAM (HTTP/DNS/TLS sağlık yönlendirmesi); tenant izolasyonu ayrıca doğrulanmadı.
- Değişen dosyalar: `tests/reports/gate-p0c-tenant-host-readiness-20261023.md`, `docs/PROGRESS.md`.
- Komut + çıktı: `firma`, `santa`, `farma` DNS A kayıtları Cloudflare IP'lerine çözümlendi; üç hostta `curl` TLS doğrulaması `0`, `/api/status` HTTP `200`, `ready`, `118/0`. `firma` ana sayfası ve `/giris` HTTP `200`.
- Test: Salt-okunur canlı HTTP/DNS kontrolü; veri değiştirilmedi. Bu, giriş veya çapraz-tenant kabul testi değildir.
- Kanıt yolu: `tests/reports/gate-p0c-tenant-host-readiness-20261023.md`.
- Sıradaki iş: P0-D — canlı plan kataloğu `[]`, checkout `/api/checkout/status` `available:false`; yayımlama/iyzico/provisioning yolunun kök nedenini incele. Canlıya kod veya fiyat kaydı yazmadan önce hedefteki staging + yedek + rollback kapısı uygulanacak.

### 23 Ekim 2026 — P0-D canlı satış engelleri teşhisi
- Durum: ENGELLENDİ
- Değişen dosyalar: `tests/reports/gate-p0d-live-checkout-blockers-20261023.md`, `docs/PROGRESS.md`.
- Komut + çıktı: Canlı `GET /api/plans` HTTP 200 `[]`; `GET /api/checkout/status` HTTP 200 `available:false`. Çalışan web konteynerinde gizli değerleri göstermeden env anahtarları denetlendi: iyzico API/secret/callback ve SMTP host/user/password/from address boş.
- Test: Salt-okunur canlı API + değişken doluluk kontrolü; ödeme ya da mail gönderilmedi. Kod checkout için hem sağlayıcı ayarını hem yayımlanmış, etkin ve fiyatlı Cloud planını zorunlu tutuyor.
- Kanıt yolu: `tests/reports/gate-p0d-live-checkout-blockers-20261023.md`; ilgili kural `src/EnvanterTR.Web/Data/CheckoutEndpoints.cs`.
- Sıradaki iş: Staging ortamında checkout/provisioning kabulini yürüt; sonra iyzico üretim anahtarları ve SMTP uygulama erişimini sunucu gizli değişkenlerine ekle, planları yayımlamadan önce satış koşullarını kabul et. Bu paket canlı DB veya Coolify ayarını değiştirmedi.

### 23 Ekim 2026 — P0-D checkout/webhook/provisioning sentetik kabulü
- Durum: TAMAM (izole sentetik uçtan uca kabul); gerçek iyzico/SMTP ve canlı müşteri kabulü ENGELLENDİ.
- Değişen dosyalar: `tests/reports/p0d-checkout-webhook-provisioning-acceptance-20261023.md`, `docs/PROGRESS.md`.
- Komut + çıktı: `docker compose -f deploy/testing/compose.security-acceptance.yaml -p p0d-checkout run --build --rm acceptance --filter "FullyQualifiedName~Public_checkout_uses_server_price_verifies_provider_and_provisions_once"` → exit 0; 1/1 geçti, 47.5491 sn. Yalıtılmış DB ve sentetik sağlayıcı kullanıldı; compose DB/ağı sonrasında kaldırıldı.
- Test: Sunucu fiyatı/kupon, KVKK onayı, imza ve sipariş eşleştirme, hatalı webhook reddi, tekrar tesliminde tek provisioning, owner/abonelik ve Authenticator içerikli davet e-postası doğrulandı.
- Kanıt yolu: `tests/reports/p0d-checkout-webhook-provisioning-acceptance-20261023.md`.
- Sıradaki iş: P0-D sandbox sağlayıcı/staging; ardından P0-E Bölüm 20 müşteri kabulü. Canlı iyzico ve SMTP sırları boş olduğundan canlı satış açılmadı; canlıya deploy yapılmadı.

### 23 Ekim 2026 — P0-D iyzico istemci ve imza regresyonu
- Durum: TAMAM (5/5 yalıtılmış test); gerçek sandbox bağlantısı ENGELLENDİ.
- Değişen dosyalar: `tests/reports/p0d-iyzico-client-signature-acceptance-20261023.md`, `docs/PROGRESS.md`.
- Komut + çıktı: `docker compose -f deploy/testing/compose.security-acceptance.yaml -p p0d-provider run --build --rm acceptance --filter "FullyQualifiedName~IyzipayCheckoutClientTests|FullyQualifiedName~IyzipayWebhookSignatureTests"` → exit 0; 5/5 geçti, 3.0245 sn. İzole DB/ağ temizlendi.
- Test: Sunucu imzası/gövde bütünlüğü, webhook imzası ve bozulma reddi, eksik credential/güvenilmeyen hostta fail-closed, sağlayıcı hata yanıtı doğrulandı.
- Kanıt yolu: `tests/reports/p0d-iyzico-client-signature-acceptance-20261023.md`.
- Sıradaki iş: Staging'de gerçek sandbox anahtarlarıyla ödeme kabulü ve P0-E Bölüm 20; canlı sırlar hâlâ boş, satış planı yayımlanmadı.
### 23 Ekim 2026 — P0-D bekleyen checkout siparişi sona erme kabulü
- Durum: TAMAM (izole PostgreSQL 17 kabul testi); canlı davranış yayımlanmadı.
- Değişen dosyalar: `src/EnvanterTR.Web/Data/CheckoutEndpoints.cs`, `src/EnvanterTR.Web/Program.cs`, `src/EnvanterTR.Web/Data/CompanyCheckoutOrderExpirationService.cs`, `tests/EnvanterTR.SecurityAcceptance/CheckoutOrderExpirationAcceptanceTests.cs`, `tests/reports/p0d-checkout-order-expiration-acceptance-20261023.md`, `docs/PROGRESS.md`.
- Komut + çıktı: `docker compose -f deploy/testing/compose.security-acceptance.yaml -p p0d-expiration run --build --rm acceptance --filter "FullyQualifiedName~Checkout_expiration_marks_only_overdue_unpaid_orders_and_releases_initialization_leases"` → exit 0; 1/1 geçti iki çalıştırmada. Web derlemesi 0 uyarı / 0 hata; kabul testi derlemesi 4 mevcut ASP.NET kullanımdan kaldırma uyarısı / 0 hata.
- Test: 24 saatten eski `Pending` ve `PaymentStarted` siparişleri `Expired` oldu ve başlatma lease'leri temizlendi; süresi dolmamış, `PaymentReview` ve `ProvisioningFailed` siparişleri korundu; ikinci çalıştırma idempotent. İzole Compose DB/ağı testten sonra kaldırıldı. İlk testte saptanan göç/işçi başlangıç yarışı giderildi; işçi ilk temizleme sorgusunu 15 dakika sonraki ilk periyodik tikte çalıştırır. Rerun bu hatayı üretmedi.
- Kanıt yolu: `tests/reports/p0d-checkout-order-expiration-acceptance-20261023.md`.
- Sıradaki iş: P0-D staging üzerinde gerçek sağlayıcı sandbox kabulü; canlı iyzico/SMTP sırları boş ve plan kataloğu yayımlanmamış durumda olduğundan canlı satış/deploy kapalı kalır.

### 23 Ekim 2026 — P0-E parola kurtarma ve SMTP yakalayıcı kabulü
- Durum: TAMAM (izole SMTP kabul testi); canlı SMTP gönderimi ve teslimatı ENGELLENDİ.
- Değişen dosyalar: `tests/reports/p0e-password-recovery-capture-acceptance-20261023.md`, `docs/PROGRESS.md`.
- Komut + çıktı: `docker compose -f deploy/testing/compose.security-acceptance.yaml -p p0d-expiration run --rm acceptance --filter "FullyQualifiedName~Password_recovery_does_not_disclose_account_existence_and_reset_tokens_are_single_use"` → exit 0; 1/1 geçti, 25.6045 sn.
- Test: Var olan/olmayan hesap yanıtları eşit; izole SMTP yakalayıcısına parola sıfırlama bağlantısı teslim edildi; token tek kullanımlı ve yeni parola doğrulandı. Geçici PostgreSQL/ağ temizlendi. Bu test üretim SMTP'si veya gerçek e-posta teslimatı kanıtı değildir.
- Kanıt yolu: `tests/reports/p0e-password-recovery-capture-acceptance-20261023.md`.
- Sıradaki iş: P0-D gerçek sandbox anahtarı ve P0-E canlı SMTP yapılandırması gerekiyor; production checkout boş plan kataloğu ve sağlayıcı/SMTP sırları nedeniyle kapalı. Farma test kullanıcısının canlı portalda MFA kurup dashboard'a ulaştığı, bu konuşmadaki kullanıcı ekran görüntüsüyle doğrulandı; üçüncü şirket Bölüm 20 kabul senaryosu tamamlanmadı.

### 23 Ekim 2026 — P0-D yıllık katalog ve checkout readiness kabulü
- Durum: TAMAM (izole PostgreSQL kabul testleri); canlı checkout hâlâ kullanılamıyor.
- Değişen dosyalar: `tests/reports/p0d-annual-catalog-checkout-readiness-acceptance-20261023.md`, `docs/PROGRESS.md`.
- Komut + çıktı: `docker compose -f deploy/testing/compose.security-acceptance.yaml -p p0d-expiration run --rm acceptance --filter "FullyQualifiedName~Annual_catalog_drafts_are_authorized_csrf_protected_idempotent_and_preserve_existing_plans|FullyQualifiedName~Platform_checkout_readiness_matches_public_gate_without_exposing_provider_secrets"` → exit 0; 2/2 geçti, 31.6012 sn.
- Test: 15 günlük deneme/KDV hariç yıllık katalog taslağı; yetki, CSRF, idempotency ve mevcut planı koruma; checkout readiness için yayımlı fiyatlı Cloud planı + geçerli provider/callback/API ayarı ve sırların gizlenmesi doğrulandı. İzole DB/ağ temizlendi. Canlıya plan yazılmadı.
- Kanıt yolu: `tests/reports/p0d-annual-catalog-checkout-readiness-acceptance-20261023.md`.
- Sıradaki iş: Canlı salt-okunur kontrolünde `/api/plans=[]`, `/api/checkout/status.available=false`, `/api/status.ready` ve `farma` 118/0 doğrulandı. Gerçek sandbox anahtarları, SMTP ve staging kabul ortamı olmadan P0-D/E canlı ödeme/e-posta/üçüncü firma kabuline geçilmez; sonraki işlem bu dış kapılar için mevcut güvenli yapılandırmayı doğrulamak veya açık engeli belgelemektir.

### 23 Ekim 2026 — P0 bulut denemesi kabulü
- Durum: TAMAM (izole sentetik e-posta kabul testi); canlı e-posta teslimatı ve gerçek müşteri kurulumu doğrulanmadı.
- Değişen dosyalar: `tests/reports/p0-cloud-trial-acceptance-20261023.md`, `tests/reports/cloud-trial-acceptance-20261023.log`, `docs/PROGRESS.md`.
- Komut + çıktı: `docker compose -f deploy/testing/compose.security-acceptance.yaml -p p0d-expiration run --rm acceptance --filter "FullyQualifiedName~Verified_cloud_trial|FullyQualifiedName~Cloud_trial|FullyQualifiedName~Concurrent_cloud_trial|FullyQualifiedName~Concurrent_distinct_cloud_trials"` → exit 0; 6 geçti, 0 başarısız, 1.1533 dakika.
- Test: 15 günlük bulut denemesi, doğrulanmış e-posta, zorunlu MFA, onay tekrarında idempotency/süre uzatmama, mevcut sahip kimlik bilgilerinin korunması, bozuk/süresi dolmuş kanıt ve kapalı planın reddi, aynı e-posta için eşzamanlı denemelerde tek hak doğrulandı. İzole PostgreSQL ve ağ testten sonra kaldırıldı; canlıya yazma yapılmadı.
- Kanıt yolu: `tests/reports/p0-cloud-trial-acceptance-20261023.md`, `tests/reports/cloud-trial-acceptance-20261023.log`.
- Sıradaki iş: P0-D gerçek Iyzico sandbox; P0-E canlı SMTP ve Bölüm 20 üçüncü şirket tenant izolasyonu kabulü. Canlı plan kataloğu boş ve checkout kapalı; bu sentetik kabul satışa açma veya canlıya deploy kanıtı değildir.

### 23 Ekim 2026 — P0-E tenant host ve slug kabulü
- Durum: TAMAM (izole tenant host/session/slug testleri ve canlı wildcard DNS/TLS salt-okunur kontrolü); Bölüm 20 müşteri iş akışı tamamlanmadı.
- Değişen dosyalar: `tests/reports/p0e-tenant-host-slug-acceptance-20261023.md`, `tests/reports/p0e-tenant-host-slug-acceptance-20261023.log`, `docs/PROGRESS.md`.
- Komut + çıktı: İlgili compose kabul testi exit 0; 3/3 geçti, 1.1070 dakika. `demo3.envantertr.com` Cloudflare A kayıtlarına çözüldü; HTTPS `/api/status` HTTP 200, TLS doğrulaması 0.
- Test: Firma hostu/oturumu eşleşmesi, başka/bilinmeyen tenant hostunda mevcut oturumun reddi, geçerli/ayrılmış/çakışan slug ve kullanıcıya açık şirket seçimi doğrulandı. Canlı DNS/TLS sorgusu salt-okunurdu; canlıda Demo3 oluşturulmadı. Wildcard DNS/TLS ile genel health yanıtı Demo3 tenantının varlığını veya verisini kanıtlamaz. Compose DB/ağı kaldırıldı.
- Kanıt yolu: `tests/reports/p0e-tenant-host-slug-acceptance-20261023.md`, `tests/reports/p0e-tenant-host-slug-acceptance-20261023.log`.
- Sıradaki iş: Gerçek Iyzico sandbox ve canlı SMTP dış kapıları; ardından gerçek üçüncü firma oluşturma/giriş/izolasyon/silme kabulü. Santa/Farma Bölüm 20.2–20.5 işlemleri de tamamlanmadı; canlı checkout boş plan kataloğu nedeniyle kapalı.

### 23 Ekim 2026 — canlı bulut deneme rotası
- Durum: ENGELLENDİ — canlıda cloud trial endpoint'i yayımlı değil; satış ve deneme kaydı canlıda doğrulanmadı.
- Değişen dosyalar: `tests/reports/p0-live-cloud-trial-route-20261023.md`, `docs/PROGRESS.md`.
- Komut + çıktı: Salt-okunur canlı GET: `/api/status` 200 (118/0), `/api/plans` 200 (`[]`), `/api/checkout/status` 200 (`available:false`), `/api/trials/status` 404. Aday kaynakta `MapCloudTrialEndpoints()` var; doğrulanmış canlı `origin/main` SHA `4d55270827be312775ebffe60c5a6d74011d7802` içinde endpoint sınıfı ve eşleme yok. `demo3.envantertr.com` wildcard DNS/TLS ile genel `/api/status` 200 verdi; tenant oluşturulmuş olduğunu göstermez.
- Test: Salt-okunur HTTP ve Git kaynak karşılaştırması; üretim DB/Coolify ayarı değişmedi, deploy yapılmadı.
- Kanıt yolu: `tests/reports/p0-live-cloud-trial-route-20261023.md`, mevcut Coolify SHA kanıtı `tests/reports/coolify-sha-save-triage-20261023.md`.
- Sıradaki iş: Cloud trial rotası için aday PR/release incelemesi; canlıya almadan önce staging kabulü, taze yedek ve rollback provası. Iyzico/SMTP sırları ve yayımlanmış plan olmadığı için checkout kapalıdır.

### 23 Ekim 2026 — P0-E yeni Cloud şirket provisioning kabulü
- Durum: TAMAM (izole PostgreSQL + sentetik SMTP); canlı müşteri kabulü ve e-posta teslimi doğrulanmadı.
- Değişen dosyalar: `tests/reports/p0e-new-company-provisioning-acceptance-20261023.md`, `tests/reports/p0e-new-company-provisioning-acceptance-20261023.log`, `docs/PROGRESS.md`.
- Komut + çıktı: `docker compose -f deploy/testing/compose.security-acceptance.yaml -p p0e-provision run --build --rm acceptance --filter "FullyQualifiedName~Platform_can_provision_a_paid_cloud_company_owner_and_retryable_invitation"` → exit 0; 1/1 geçti, 48.9926 sn.
- Test: Platform admin şirketi ve tenant slug'ını oluşturdu; 365 günlük aktif Cloud aboneliği, owner üyeliği, zorunlu MFA, davet e-postası ve daveti tekrar gönderme doğrulandı; çakışan slug reddedildi. İzole Compose DB/ağı kaldırıldı, canlıya yazma yapılmadı.
- Kanıt yolu: `tests/reports/p0e-new-company-provisioning-acceptance-20261023.md`, `tests/reports/p0e-new-company-provisioning-acceptance-20261023.log`.
- Sıradaki iş: Gerçek üçüncü firma için staging ve canlı müşteri akışı, gerçek SMTP teslimi, kiracı verisi ve silme kapsamı. Canlı rota doğrulanmış `origin/main` sürümünde yok; canlı checkout boş plan kataloğu ve Iyzico/SMTP ayarları nedeniyle kullanılamıyor.

### 23 Ekim 2026 — P0-E bulut denemesi Chromium kabulü
- Durum: TAMAM (yerel sentetik API + Chromium); gerçek SMTP ve canlı deneme rotası doğrulanmadı.
- Değişen dosyalar: `tests/reports/p0e-cloud-trial-browser-acceptance-20261023.md`, `docs/PROGRESS.md`.
- Komut + çıktı: Hosted-checkout browser imajı oluşturuldu; `docker run --rm --init --entrypoint node envantertr-hosted-checkout-browser-acceptance:local cloud-trial.mjs` → PASS. Kapalı form, slug önerisi/özel slug, CSRF/onay tek POST, beş dil, 390px taşmasız görünüm, açık kullanıcı onayı, URL'den kanıt temizliği ve dış portal adresi reddi geçti.
- Test: Gerçek ağ sağlayıcısı ve e-posta kullanılmadı; HTTP istekleri sentetik API'ye yönlendirildi. Üretim verisi/ayarına yazılmadı.
- Kanıt yolu: `tests/reports/p0e-cloud-trial-browser-acceptance-20261023.md`.
- Sıradaki iş: Staging ve gerçek SMTP/Iyzico kabul kapıları; canlı trial route 404 ve published plan kataloğu boş.
### 23 Ekim 2026 — P0-D iyzico ortamının açık seçimi
- Durum: TAMAM (izole ödeme readiness kabulü); canlı ödeme yapılandırması/deploy yapılmadı.
- Değişen dosyalar: `deploy/coolify/compose.yaml`, `deploy/server/compose.yaml`, `deploy/server/.env.example`, `src/EnvanterTR.Web/Data/IyzipayCheckoutClient.cs`, `tests/EnvanterTR.SecurityAcceptance/CheckoutReadinessAcceptanceTests.cs`, `tests/reports/p0d-explicit-iyzipay-environment-acceptance-20261023.md`, `docs/PROGRESS.md`.
- Komut + çıktı: Docker içindeki web derlemesi 0 uyarı/0 hata; kabul derlemesi 4 mevcut ASP.NET deprecation uyarısı/0 hata. Her iki üretim Compose dosyası test değerleriyle `config --quiet` doğrulamasından geçti.
- Test: `Platform_checkout_readiness_matches_public_gate_without_exposing_provider_secrets` → 1/1 geçti. Boş BaseUrl, sandbox ve production durumları; secretsız readiness yanıtı ve kapalı checkout doğrulandı. Test PostgreSQL'i ve ağı izoleydi.
- Kanıt yolu: `tests/reports/p0d-explicit-iyzipay-environment-acceptance-20261023.md`.
- Sıradaki iş: P0-D staging üzerinde gerçek iyzico sandbox kabulü ve P0-E SMTP/üçüncü firma kabulü. Gerekli sağlayıcı sırları, canlı SMTP ve staging dış koşulları olmadan satış açılmaz; üretim deploy kapısı staging kabulü + taze yedek + geri dönüş provasıdır.

### 23 Ekim 2026 — P0 katalog ve tarayıcı sözleşmesi düzeltmeleri
- Durum: TAMAM (iki sentetik API/Chromium akışı ve üç kaynak doğrulayıcısı); tam uzak CI ve canlı müşteri kabulü açık.
- Değişen dosyalar: platform browser fixture, `Verify-AssetModelDetailsUi.mjs`, `Verify-HostedCheckoutBrowserAcceptance.mjs`, `Verify-PlatformAdminExperience.mjs`, denetim/progress ve kabul raporu.
- Komut + çıktı: Üç Node doğrulayıcı geçti; Docker Chromium `platform-admin-members.mjs` ve `asset-detail-actions.mjs` exit 0.
- Test: Cloud dört kullanıcı kotası kaydı ve `4 / 4` gösterimi; model ayrıntısı özet/varlık/sipariş/geçmiş/ekler ve bağlantılı varlığa geçiş doğrulandı. Kaynak doğrulayıcıdaki kırılgan metin mesafesi ayrı davranış assertion'larıyla değiştirildi.
- Kanıt yolu: `tests/reports/p0-catalog-browser-contract-acceptance-20261023.md`.
- Sıradaki iş: P0-D'de şirket oluşturmanın panel/deneme/ödeme yollarında ortak servis şartını uygulamak. PR #25 CI proxy fixture imaj indirmesinde Docker Hub timeout nedeniyle başarısız; yerel proxy ilk `ResponseEnded` sonrası tek yeniden denemede geçti. Staging/gerçek iyzico/SMTP kapıları devam ediyor.

### 23 Ekim 2026 — P0-D tek kiracı oluşturma servisi
- Durum: TAMAM (ortak servis ve izole PostgreSQL kabulü); üretimde yayımlanmadı.
- Değişen dosyalar: `TenantProvisioningService.cs`, Account/Platform/Checkout/CloudTrial endpoint'leri, DI kaydı, provisioning kabul testleri, ilgili iki kaynak doğrulayıcısı ve kanıt raporu.
- Komut + çıktı: Docker web build 0 uyarı/0 hata; kabul build 4 mevcut deprecation uyarısı/0 hata. Hedefli PostgreSQL kabulü exit 0, 11/11 geçti, 2.5126 dakika.
- Test: Şirket ekleme tek ortak metotta; başlangıç kurulumu, panel, deneme ve ödeme yolları servisi çağırıyor. Transaction yoksa işlem reddi; Identity hesabı/şirket/üyelik/aboneliğin birlikte rollback'i; ilk sahip kimlik bilgilerinin korunması; deneme geçerlilik/tekrar/eşzamanlılık, ücretli panel kurulumu, imzalı checkout ve Cloud kullanıcı kotası doğrulandı. Üç kaynak kontrolü geçti.
- Kanıt yolu: `tests/reports/p0d-shared-tenant-provisioning-acceptance-20261023.md`, `tests/reports/shared-tenant-provisioning-20261023.log`.
- Sıradaki iş: PR #25 uzak PostgreSQL koşusu 278/279: tek hata yeni trial rotalarının açık API envanterinde bulunmaması; açık rota listesi ve trial rate-limit sözleşmesi güncellenip doğrulanacak. Staging + gerçek iyzico/SMTP ve Bölüm 20 müşteri kabulü açık.

### 23 Ekim 2026 — P0 trial API envanteri CI hatası
- Durum: TAMAM (yerel hedefli doğrulama); güncel tam uzak CI bekleniyor.
- Değişen dosyalar: `TenantSessionAcceptanceTests.cs`, `docs/DATA-API.md`, `tests/reports/p0-trial-api-auth-boundary-acceptance-20261023.md`, `tests/reports/trial-api-auth-boundary-20261023.log`, `docs/PROGRESS.md`.
- Komut + çıktı: `p0-api-boundary` Compose kabulü exit 0, 2/2 geçti, son koşu 41.6320 saniye. Web build 0 uyarı/0 hata; test build dört mevcut ASP.NET deprecation uyarısı/0 hata.
- Test: Açık API rotalarının tam küme eşitliği ve yetki politikaları korundu; anonim trial rotaları açıkça listelendi, iki yazma ucunda checkout rate-limit metadata'sı zorunlu kılındı. Kayıt özelliği kapalı/CSRF eksik/kanıt geçersiz durumlarında şirket açılmadığı doğrulandı.
- Kanıt yolu: `tests/reports/p0-trial-api-auth-boundary-acceptance-20261023.md`, `tests/reports/trial-api-auth-boundary-20261023.log`.
- Sıradaki iş: Üç iş paketini mevcut taslak PR #25'e gönderip yeni CI'ı izlemek; başarılı tam koşu sonrası staging kabul kapısını hazırlamak. Gerçek iyzico/SMTP, Bölüm 20 müşteri kabulü ve canlı yayın koşulları açık. Önceki hedef turu PR açılması ve testli kod gönderimiyle ilerleme olarak sınıflandırıldı.

### 23 Ekim 2026 — P0 platform davetinde Cloud kullanıcı kotası
- Durum: TAMAM (hedefli PostgreSQL kabulü ve kaynak kontrolü).
- Değişen dosyalar: `Verify-PlatformMemberInvites.mjs`, `CloudSeatQuotaAcceptanceTests.cs`, ilgili log/rapor ve kaynak kontrol JSON'u.
- Komut + çıktı: İzole `p0-invite-quota` Compose kabulü exit 0; 3/3 geçti, 1.1135 dakika. CI'daki 78 ayrı Node kaynak kontrolü, sonraki SMTP paketinden önce 78/78 geçti.
- Test: Tenant ve platform yönetici davetinde dolu Cloud kotası reddi, hesap oluşturmama, kota artışıyla kabul, yeniden dolduğunda ret ve mevcut üyeliklerin korunması; davet yeniden gönderme/e-posta senaryosu geçti. CI'ın eski Cloud muafiyeti bekleyen assertion'ı doğru sınır şartıyla değiştirildi.
- Kanıt yolu: `tests/reports/p0-platform-invite-quota-acceptance-20261023.md`, `tests/reports/platform-invite-seat-quota-20261023.log`, `tests/reports/p0-ci-source-checks-20261023.json`.
- Sıradaki iş: SMTP yapılandırması hatalıyken trial/public readiness ile platform sağlık bilgisinin tutarlı kapanmasını sağlamak ve gerçek e-posta kabulüne hazırlamak. Önceki hedef turu üç testli commit/push ile ilerleme olarak sınıflandırıldı.

### 23 Ekim 2026 — P0-E ortak SMTP yapılandırma doğrulaması
- Durum: TAMAM (yapılandırma ve yakalayıcı regresyon kabulü); gerçek SMTP/inbox teslimi açık.
- Değişen dosyalar: `EmailConfigurationReadiness.cs`, CloudTrial/Platform endpoint'leri, platform sağlık açıklaması, ilgili verifier ve kabul testi/doküman/raporlar.
- Komut + çıktı: Web build 0 uyarı/0 hata; test build dört mevcut deprecation uyarısı/0 hata. İlk koşu 3/3, mevcut admin-only sağlık regresyonu ayrıca 1/1 geçti. SMTP adayında 78 kaynak kontrolü 78/78 geçti.
- Test: On yapılandırmada host, port, gönderici, HTTPS bağlantı, eksik credential çifti ve TLS doğrulaması; açık trial durumu ile sağlık tutarlılığı; bozuk ayarda mail gönderilmeden 503; sırların yanıtta bulunmaması; parola kurtarma/gerçek yakalayıcı trial e-postası ve sağlık yetkisi doğrulandı.
- Kanıt yolu: `tests/reports/p0e-smtp-readiness-acceptance-20261023.md`, SMTP iki logu ve kaynak kontrol JSON'u.
- Sıradaki iş: Ücretli şirket oluşturulduktan sonra ilk davet e-postası gönderilemezse görünür başarısızlık, güvenli teşhis logu ve yeniden gönderimle aynı şirket/üyelik/aboneliğin korunması kabulünü tamamlamak. Staging/gerçek sağlayıcı/SMTP ve Bölüm 20 kapıları açık.

### 23 Ekim 2026 — P0-E ücretli şirket davetinde hata ve yeniden gönderim
- Durum: TAMAM (yakalanmış SMTP ve izole PostgreSQL kabulü); gerçek SMTP teslimi açık.
- Değişen dosyalar: `PlatformEndpoints.cs`, mevcut provisioning kabul testi, log/rapor ve progress.
- Komut + çıktı: Web build 0 uyarı/0 hata; test build dört mevcut deprecation uyarısı/0 hata. `p0-owner-mail` Compose kabulü exit 0, 2/2 geçti, 55.0757 saniye. İlgili iki kaynak kontrolü geçti.
- Test: İlk mail hatasında `201/invitationSent=false` ve görünür mesaj; mail yeniden gönderimi hata durumunda `503`; yapılandırma düzeldikten sonra aynı sahibin daveti gönderiliyor. Tek şirket/owner üyelik/abonelik korunuyor. Güvenli warning yalnız şirket ID ve exception türü içeriyor.
- Kanıt yolu: `tests/reports/p0e-paid-owner-invitation-retry-20261023.md`, `tests/reports/paid-owner-invitation-failure-20261023.log`.
- Uzak kontrol: Önceki `06cbd5d` adayında tam PostgreSQL CI 281/281 geçti. Aynı koşunun Linux kaynak işi eski Cloud kotasızlık assertion'ında başarısızdı; bu assertion `17b54f1` içinde düzeltildi. Yeni SMTP adayı için tam CI ayrıca gerekir.
- Sıradaki iş: Bu turdaki üç testli paketi PR #25'e göndermek; mevcut deneme şirketini/verilerini koruyarak yıllık ücretli aboneliğe geçiren self-servis ödeme açığını kaynak denetimiyle belgelemek ve P0-D kapsamında gidermek.

### 23 Ekim 2026 — P0-D denemeden ücretli aboneliğe geçiş açığı
- Durum: EKSİK — mevcut kaynakta müşteri ödeme dönüşümü bulunmuyor; uygulanmış veya çalışma zamanında geçmiş sayılmadı.
- Değişen dosyalar: `docs/P0-CHECKOUT-GAPS.md`, `docs/PROGRESS.md`.
- Komut + çıktı: Checkout şirket-slug kontrolleri ve endpoint envanteri tarandı. `CheckoutEndpoints.cs:247` mevcut şirket slug'ını başlangıçta reddediyor; `:522` doğrulanmış ödeme sonucunda da mevcut şirketi provisioning hatası sayıyor. Yalnız platform yöneticisi için manuel subscription extend/edit rotaları var.
- Kanıt: `docs/P0-CHECKOUT-GAPS.md` bu iki kod yolunu, eksik mevcut-şirket bağlamını ve doğrudan satın almada ödeme öncesi e-posta doğrulaması açığını listeler. Bu bir kaynak denetimidir; yeni şirket checkout testleri deneme dönüşümünün kanıtı değildir.
- Sıradaki iş: P0-D'de mevcut şirket sahibiyle bağlı, süresi dolmuş denemede de erişilebilir yıllık ödeme; doğrulanmış sağlayıcı sonucuyla aynı şirket/veri/üyelikleri koruyan atomik abonelik geçişi; replay/eşzamanlılık/tenant güvenliği ve tarayıcı kabulü. Gerçek sağlayıcı/SMTP, staging ve Bölüm 20 kapıları ayrıca açık.

### 23 Ekim 2026 — PR #25 önceki adayın tam uzak CI sonucu
- Durum: TAMAM (yalnız `3c63980536e11d1712706f882696e24de86b03c8` CI kabulü).
- Komut + çıktı: GitHub Actions API run `39987675342`: completed/success; Linux kaynak/güvenlik, Linux PostgreSQL ve Windows dağıtım/yedek güvenliği işleri 3/3 success.
- Kanıt yolu: `tests/reports/p0-pr25-ci-3c63980-20261023.json`, https://github.com/EnvanterTR/EnvanterCodex/actions/runs/39987675342.
- Sınır: Bu sonuç sonradan yazılan mevcut şirket ödeme dönüşümü kodunu, gerçek iyzico/SMTP'yi veya üretim kabulünü kapsamıyor. Taslak PR satışa hazır veya merge edilebilir sayılmadı.
- Sıradaki iş: Mevcut şirket yıllık checkout API/transaction/migration kabulünü tamamlamak; ardından müşteri arayüzü ve tarayıcı kabulü.

### 23 Ekim 2026 — P0-D mevcut şirket için yıllık ödeme API'si
- Durum: TAMAM (hedefli backend kabulü); müşteri ödeme arayüzü/tarayıcı kabulü ve gerçek sağlayıcı açık.
- Değişen dosyalar: `ExistingCompanyCheckoutEndpoints.cs`, checkout/ortak provisioning servisi, sipariş modeli/EF snapshot/additive migration `20261023190000`, write-lock middleware, yeni PostgreSQL kabulü, API/açık işler belgeleri ve raporlar.
- Komut + çıktı: `p0-existing-checkout` Compose kabulü exit 0; 8/8 geçti, 1.7565 dakika. Web build 0 uyarı/0 hata; test build dört mevcut deprecation uyarısı/0 hata. CI'ın 78 kaynak kontrolü 78/78 geçti; model snapshot ile runtime model arasında bekleyen fark yok.
- Test: Altı yeni senaryo aktif/süresi dolmuş deneme dönüşümü, ücretli süreyi koruyan aynı paket yenilemesi, sunucudan türetilen tenant/sahip bağlamı, CSRF/parola/MFA/kota/istek sınırı ve ayrı owner yetkisi reddi, trailing-slash/canonical retry, anonim sipariş kimliği kullanma reddi, ödeme sonrası sahiplik/kota değişimi, sıralı callback replay, eski tarihler ve kendi/diğer şirketin varlıklarının korunmasını kapsar. İki mevcut senaryo subdomain oturum izolasyonu ve tüm korumalı API'larda anonim reddidir.
- İlk başarısızlıklar: 41 karakter test plan kodu DB'deki 40 karakter sınırını aştı; kod kısaltıldı. İkinci koşu 6/7: altıncı istekte gerçek limiter 429 döndürdü, test 403 bekliyordu. Son koşu hem 429'u hem yeni hostta bağımsız 403 owner reddini doğrular; korumalar kaldırılmadı.
- Kanıt yolu: `tests/reports/p0-existing-company-checkout-acceptance-20261023.md`, `existing-company-checkout-20261023.log`, önceki iki hata logu, `p0-existing-checkout-source-checks-20261023.json`.
- Sınır: Paralel callback, hedef şirketin gerçek silinmesi, bu bağlamdaki başarısız/uyuşmayan ödeme, yanlış MFA birikimi/lockout ve tam plan değiştirme yaşam döngüsü ayrıca doğrulanacak. Şirket arayüzü, gerçek iyzico/SMTP, staging/yedek/geri dönüş ve Bölüm 20 tamamlanmadan satışa hazır/canlı kabulü verilmedi.
- Regresyon: Aynı aday imajında mevcut iki public checkout senaryosu ve üretim-baseline schema yükseltmesinde şirket/varlık koruma testi 3/3 geçti; exit 0, 37.7785 saniye. Kanıt `tests/reports/existing-company-checkout-regression-20261023.log`. Yeni migration yalnız izole kabul DB/schema'larında uygulandı.
- Sıradaki iş: Süresi dolan denemeden yıllık satın alma sayfasına geçiş ve form gönderimi uygulandı; sonraki adım gerçek tarayıcı kabulü. Toplam önceki API/DB paketi 11/11 kabul ve 78/78 kaynak kontrolüyle doğrulandı.

### 23 Ekim 2026 — P0-D mevcut şirket yıllık satın alma sayfası
- Durum: TAMAM (sunucu tarafı form, sentetik sağlayıcı ve hedefli PostgreSQL kabulü); browser/gerçek sağlayıcı/staging açık.
- Değişen dosyalar: `ExistingCompanyCheckoutPage.cs`, `ExistingCompanyCheckoutEndpoints.cs`, `CheckoutEndpoints.cs`, `Program.cs`, portal `index.html`, mevcut-şirket checkout kabul testleri; checkout gap, ilerleme, durum ve test raporları.
- Komut + çıktı: `docker compose -p p0-checkout-page -f deploy/testing/compose.security-acceptance.yaml build acceptance` başarılı; web 0 uyarı/0 hata, test derlemesi 4 mevcut ASP.NET deprecation uyarısı/0 hata. `node tools/Verify-UiLocales.mjs` 5 dilde 2.246 kayıt doğruladı; `git diff --check` geçti.
- Test: İlk UI adayı `FullyQualifiedName~Existing_company` 9/9 geçti (2.1029 dk). Callback izolasyon düzeltmesinden sonra aynı izole PostgreSQL kapsamı 10/10 geçti (2.1253 dk); ayrı paralel callback hedefi 1/1 geçti (1.0272 dk). Sayfa/form kabulü: süresi geçmiş denemede sahibi erişimi, İngilizce/koyu tema, paket ve aylık eşdeğer, hassas parola/TOTP'nin yanıtta olmaması, başka rol için 403; CSRF + MFA/parola sonrası 4.200 TRY yıllık sipariş, değişmez şirket/istek sahibi, `PaymentStarted` ve sentetik iyzico. İki eşzamanlı callback de başarı yanıtı döndürüp tek aktif abonelik ve tek audit kaydı bıraktı. Trial/expired/paid dönüşüm, sıralı replay, kapasite/sahip yeniden kontrolü ve tenant session izolasyonu da geçti.
- Kanıt yolu: `tests/reports/company-checkout-page-build-20261023.log`, `company-checkout-page-regression-20261023.log`, `company-checkout-page-concurrent-callback-20261023.log`, `company-checkout-concurrent-callback-20261023.log`, `company-checkout-page-first-build-20261023.log`, `company-checkout-page-acceptance-20261023.md`.
- Sınır: Gerçek iyzico kart/merchant callback'i, interaktif tarayıcı akışı, SMTP/inbox, staging, yeni doğrulanmış yedek ve geri dönüş provası ile Santa/Farma/Demo3 uçtan uca kabulü yapılmadı. Canlı deploy yapılmadı.
- Sıradaki iş: Tarayıcıda bu formun tenant sahibi, hata/başarılı ödeme dönüşü ve mobil görünüm kabulini eklemek; provider failure/mismatch ve target deletion senaryolarını tamamlamak. Mevcut PR #25 taslak adayına yeni değişikliği ekleyip tam uzak CI'ı çalıştırmak.

### 23 Ekim 2026 — checkout sayfası envanter CI düzeltmesi
- Durum: TAMAM (statik envanter yenilendi); önceki head'in CI sonucu başarısız, düzeltmelerden sonraki head CI bekleniyor.
- Değişen dosyalar: `docs/ui-inventory.md`, `docs/PROGRESS.md`.
- Komut + çıktı: İlk yeni PR CI'da `Verify the static UI inventory matches the source` adımı başarısızdı; kök neden checkout bağlantısı eklendikten sonra üretilmiş statik envanterin eski kalması. `node tools/Generate-UiInventory.mjs` 2.269 kontrol/65 rota/58 bağlantı/955 düğme yazdı; `node tools/Generate-UiInventory.mjs --check` geçti.
- CI: Run `40003911832` iptal edildi. Sonraki head `64b863c0671458b5ffe62af64dc9bcbdaff9b511` için run `40004284465`: Windows deployment/backup ve Linux PostgreSQL işleri başarılı; Linux tenant güvenlik işi `Run hosted Cloud checkout in Chromium` adımında başarısız. İlk Chromium hatası İngilizce süre dolumu bildiriminin beklenen açılış cümlesinin değişmesiydi; beş dildeki cümle başlangıçları geri yüklendi ve yerel aynı hosted Chromium imajı exit 0 ile tamamlandı. Yeni commit'te tam GitHub CI yeniden çalışmalı.
- Kanıt: `docs/ui-inventory.md`; GitHub run `https://github.com/EnvanterTR/EnvanterCodex/actions/runs/40003911832`.
- Sıradaki iş: Chromium yerel kanıtını ve callback yarış düzeltmesini PR #25 taslak dalına itip yeni head revision için 3/3 CI sonucunu doğrulamak.

### 23 Ekim 2026 — P0-D eşzamanlı ödeme callback'i
- Durum: TAMAM (izole PostgreSQL kabulü); yeni remote CI henüz çalıştırılmadı.
- Değişen dosyalar: `CheckoutEndpoints.cs`, `ExistingCompanyCheckoutAcceptanceTests.cs`, checkout kabul raporları ve ilerleme/durum/gap belgeleri.
- Hata ve düzeltme: `SERIALIZABLE` transaction altında aynı mevcut şirket siparişine iki paralel callback PostgreSQL `40001 could not serialize access due to concurrent update` üretti. Mevcut şirket yolu aynı sipariş için `FOR UPDATE` ve ortak provisioning servisindeki şirket satır kilidiyle `READ COMMITTED` kullanır; kilidi bekleyen callback tamamlanmış sipariş durumunu görüp başarı verir. Yeni şirket checkout yolu `SERIALIZABLE` kalır.
- Komut + çıktı: web build 0 uyarı/0 hata, test derlemesi 4 mevcut deprecation uyarısı/0 hata. İzole `p0-checkout-page` PostgreSQL'de paralel callback testi 1/1; bütün existing-company kapsamı 10/10 geçti. Hosted Chromium acceptance imajı exit 0 ile tamamlandı; aynı koşunun tam çıktısı `tests/reports/company-checkout-hosted-chromium-20261023.log` dosyasına alınıyor. 2.246 locale ve 2.269 UI kontrolü doğrulandı; `git diff --check` log sonu whitespace temizliği sonrası tekrarlanacak.
- Kanıt yolu: `tests/reports/company-checkout-page-concurrent-callback-20261023.log` (ilk race), `company-checkout-concurrent-callback-20261023.log` (düzeltme 1/1), `company-checkout-page-regression-20261023.log` (10/10).
- Sıradaki iş: Kanıt loglarının satır sonlarını normalize edip paketi PR #25 taslak dalına eklemek; yeni head'de 3/3 CI doğrulamak.

### 24 Ekim 2026 — P0-D mevcut şirket sağlayıcı güvenlik kapıları
- Durum: TAMAM (hedefli izole API/veritabanı kabulü); tarayıcı/gerçek sağlayıcı/staging hâlâ açık.
- Değişen dosyalar: `ExistingCompanyCheckoutAcceptanceTests.cs`, `docs/P0-CHECKOUT-GAPS.md`, bu ilerleme kaydı ve `tests/reports/p0d-existing-company-provider-guards-acceptance-20261024.md`.
- Komut + çıktı: Mevcut şirket checkout hedefli Compose kabulü 7/7 geçti; exit 0, 1.683 dakika. `git diff --check` geçti.
- Kapsam: Yanlış para birimi sağlayıcı sonucu, başarısız/erişilemeyen sağlayıcı sonucu, callback öncesi şirket devre dışı bırakma ve hedef şirketi callback öncesi gerçekten silme. Hiçbir senaryoda abonelik etkinleşmiyor; silinen kiracı yeniden oluşturulmuyor. Başarısız sağlayıcı sonucu güvenli biçimde 503 veriyor.
- Kanıt: `tests/reports/p0d-existing-company-provider-guards-acceptance-20261024.md`.
- Sınır: Sentetik sağlayıcı ve izole API/DB; interaktif browser, gerçek iyzico/SMTP, staging/geri dönüş ve tam plan değişimi yaşam döngüsü kanıtı değildir.
- Sıradaki iş: Mevcut şirket checkout için gerçek sunucu-rendered sayfa/form tarayıcı kabulünü tamamlamak; değişiklikleri PR #25'e gönderip son head CI'ını doğrulamak.

### 24 Ekim 2026 — P0 canlı satış yolu güncel gözlemi
- Durum: TAMAM (salt okunur canlı kontrol); satış yolu kullanılamıyor.
- Kanıt: `tests/reports/p0-live-sales-readiness-probe-20261024.md`; `GET /api/status` 200 ve 118/0 migration, `GET /api/plans` boş dizi, `GET /api/checkout/status` `available=false`, `GET /api/trials/status` 404.
- Sonuç: Migration hazır cevabı aday checkout/trial kodunun canlıya dağıtıldığını kanıtlamıyor. Canlıya mutasyon yapılmadı.
- Sınır: Bu public probe SMTP gerçek teslimatını doğrulamaz. Plan yayınlama, provider/SMTP doğrulaması, staging, yeni yedek ve rollback provası açık.
- Sıradaki iş: Checkout tarayıcı kabulünü ve Hedef Bölüm 20 müşteri senaryolarını ilerlet; bu kapılar geçmeden üretime dağıtma.

### 24 Ekim 2026 — P0 Bölüm 20.4 tenant sınırı kabulü
- Durum: TAMAM (sentetik iki şirket/izole PostgreSQL); gerçek Santa/Farma kabulü açık.
- Test: `Role_downgrade_revocation_and_company_suspension_take_effect_on_the_next_request`, `Asset_single_edit_is_tenant_scoped_version_checked_and_written_to_history`, `Existing_company_sessions_cannot_be_reused_on_another_tenant_subdomain` — **3/3**, exit 0, 2.1753 dakika.
- Kanıt: `tests/reports/p0-section20-tenant-isolation-acceptance-20261024.md` ve `p0-section20-tenant-isolation-20261024.log`.
- Sınır: Cross-tenant varlık okuma/yazma 404; başka tenant subdomain'inde oturum 401. Sentetik kabul canlı Santa/Farma, üçüncü şirket veya Bölüm 20'nin kalan iş akışlarını kanıtlamaz. Canlı veriye dokunulmadı.
- Sıradaki iş: Gerçek/interactive checkout browser akışı ve müşteri E2E; mevcut Windows tarayıcı bilgisayar-kullanım aracı URL politikası hatası verdiği için browser incelemesi yapılamadı. Kullanılabilir başka güvenli kabul yüzeyiyle devam et.

### 24 Ekim 2026 — P0 Bölüm 20.5 platform admin güvenlik alt kabulü
- Durum: KISMİ (izole kabul); tam Bölüm 20.5 açık.
- Test: `Company_member_and_platform_admin_boundaries_are_enforced_over_http`, `Authenticator_two_factor_requires_second_step_and_recovery_codes_are_single_use`, `Platform_admin_first_login_accepts_password_then_requires_mfa_setup_before_panel` — **3/3**, exit 0, 2.9147 dakika.
- Kanıt: `tests/reports/p0-section20-platform-admin-acceptance-20261024.md` ve `p0-section20-platform-admin-20261024.log`.
- Doğrulanan: Platform rolüyle en az iki şirket görülebiliyor; müşteri tenant yöneticisi platform API'sinden 403 alıyor; ilk platform girişi MFA kurulumu istiyor; MFA olmadan platform overview 428 veriyor; TOTP ve kurtarma kodları testleri geçiyor.
- Açık: Hedef Bölüm 9.5 / 20.5'teki “Panele gir” impersonation, sarı banner, impersonation audit izi ve platforma geri dönüş mevcut aday kaynakta bulunmadı. Canlı platform/sirket oturumu da kabul edilmedi.
- Sıradaki iş: Güvenli impersonation tasarımını ve acceptance kapsamını tamamla; ardından gerçek hesap akışı ve P0-E SMTP/Bölüm 20 kalan adımları.

### 24 Ekim 2026 — P0 hosted Chromium kabul paketi
- Durum: TAMAM (sentetik hosted Chromium suite); mevcut şirket checkout gerçek kullanıcı akışı, gerçek sağlayıcı ve canlı kabul açık.
- Değişen dosyalar: `tests/reports/p0-hosted-browser-acceptance-20261024.md`, `tests/reports/p0-hosted-browser-acceptance-20261024.log`, `docs/PROGRESS.md`.
- Komut + çıktı: `deploy/testing/run-hosted-checkout-browser-acceptance.ps1` → exit 0; **74/74 PASS**. Chromium imajı `mcr.microsoft.com/playwright:v1.64.0-noble`.
- Test: Checkout ödeme kapalıyken kişisel/fatura verisi göndermeme; fiyatı sunucu kotasıyla gösterme; Farma/Santa/Celal slug önerileri ve elle düzenlenmiş slug'ı koruma; CSRF, veri işleme onayı, hassas tutar/kart verisini istemci gövdesine koymama ve yalnız güvenilir sağlayıcıya yönlendirme. Aynı hosted paket diğer şirket portalı, platform UI, varlık, rapor, lisans, satın alma, destek ve zamanlanmış rapor akışlarını da kapsar.
- Kanıt yolu: `tests/reports/p0-hosted-browser-acceptance-20261024.md` ve tam çıktı `.log` dosyası.
- Sınır: Sentetik API/iyzico; gerçek e-posta veya tahsilat yok. Genel yeni şirket checkout testi, `/subscription/checkout` mevcut firma satın alma/dönüş akışını kanıtlamaz. Santa/Farma gerçek hesapları, staging, taze yedek/rollback ve canlı ayar kabulü değildir.
- Sıradaki iş: Checkout P0-D için mevcut şirket browser dönüş akışını ayrı kabul et; ardından P0-E SMTP/Bölüm 20 müşteri senaryolarına ve kalan platform impersonation kabuline ilerle. Üretime yayın kapıları geçmeden canlıya deploy etme.

### 24 Ekim 2026 — P0-D mevcut şirket gerçek uygulama tarayıcı kabulü
- Durum: TAMAM (gerçek Kestrel + PostgreSQL + Chromium; sentetik iyzico); gerçek sağlayıcı/canlı kabul açık.
- Değişen dosyalar: `tests/EnvanterTR.CheckoutBrowser`, browser senaryosu, Dockerfile/Compose/PowerShell runner, CI adımı/kanıt upload'u, checkout gap ve ilerleme belgeleri.
- Komut + çıktı: `deploy/testing/run-existing-checkout-browser-acceptance.ps1` → exit 0; **1/1 uçtan uca senaryo geçti**. Derleme 0 hata/0 uyarı. `git diff --check` doğrulandı.
- Test: Gerçek parola/MFA/cookie girişi, süresi dolmuş denemede 4.200 TRY yıllık / 350 TRY aylık eşdeğer, İngilizce/koyu/390px; yanlış parolada 400, fatura koruma ve gizli alanları temizleme; gerçek CSRF formu, güvenilir 302 Location; callback/replay, aktif ödeme geçmişi; ayrı viewer oturumunda 403. DB: tek sağlayıcı initialize ve tek aktif abonelik; şirket UUID'si, iki üyelik, deneme geçmişi ve iki firmanın varlıkları korunuyor.
- İlk koşular: Onaysız test callback URL'si formu doğru olarak kapattı; test yapılandırması düzeltildi. Sonraki koşu izole ağdan sağlayıcıya yönlendirmede durdu; gerçek Location doğrulanıp yalnız testte yerel sağlayıcı sayfasıyla devam edilecek şekilde browser interception düzeltildi. İki hata logu saklandı; ürün güvenlik kontrolü kaldırılmadı.
- Kanıt: `tests/reports/existing-company-browser-acceptance-20261024.md`, son/ilk/redirect logları ve `existing-company-browser-20261024/checkout-390.png`. Görsel incelendi. Geçici Compose DB/ağ temizlendi.
- Sınır: Gerçek iyzico tahsilatı/e-posta yok; staging, taze yedek/rollback, tam ücretli plan değişimi ve canlı Santa/Farma/Demo3 kabulü açık. Canlı deploy yapılmadı.
- Sıradaki iş: PR #25'te uzak CI doğrulaması; P0-D e-posta doğrulama açığı ve gerçek sağlayıcı, ardından P0-E SMTP/Bölüm 20 kalan adımları.

### 24 Ekim 2026 — P0-D checkout kullanıcı kotası gösterimi
- Durum: TAMAM (yerel ürün düzeltmesi + Chromium kabulü); yeni PR CI/canlı kabulü açık.
- Değişen dosyalar: public `checkout.html/js`, hedefli Chromium senaryosu ve mevcut hosted suite/Docker bağlantısı; kabul raporu ve loglar.
- Hata: 4 kullanıcılı yayımlı paket sipariş özetinde `Sınırsız` görünüyordu; düzeltme öncesi Chromium exit 1 ile yeniden üretildi. Açıklaması olmayan paket de aynı yanlış ifadeyi gösteriyordu.
- Düzeltme: Pozitif tamsayı API kotası gösterilir; yalnız açık `null` sınırsızdır. Eksik/geçersiz kota `—`, boş açıklama boş kalır.
- Test: 1/4/10/2000/null/eksik × 5 dil = **30/30**, exit 0; sıfır ödeme POST'u ve browser JS hatası. Ara testte Node/Chromium ICU ayırıcı farkı yakalandı; sayı değeri bağımsız doğrulandı ve hata logu saklandı. Hosted checkout kaynak doğrulayıcıları ve UI inventory check (2.269 kontrol) geçti. Docker .NET acceptance build exit 0 (değişmeyen C# katmanları cache); `git diff --check` geçti.
- Kanıt: `tests/reports/checkout-user-quota-acceptance-20261024.md`, before/after/ICU/build logları.
- CI: Önceki `dfdb36e` run `40035243704` Windows başarılı; iki Linux işi son kontrolde çalışıyor. Bu paket önce yerel commit olur; devam eden CI'ı iptal etmemek için sonraki toplu PR güncellemesine bırakılır.
- Sıradaki iş: P0-D doğrudan satın almada ödeme öncesi e-posta doğrulaması; aktif CI sonucu ve gerçek iyzico/SMTP kabulü. Canlıya yayın yapılmadı.

### 24 Ekim 2026 — P0-D ödeme öncesi e-posta doğrulaması
- Durum: TAMAM (yerel kod + API/PostgreSQL/SMTP capture/Chromium kabulü); gerçek servis/canlı kabulü açık.
- Değişen dosyalar: `CheckoutEmailEndpoints.cs`, checkout sözleşmesi/başlatma/hazırlık, ayrı e-posta istek sınırı, checkout HTML/JS, API ve browser kabulleri, tek komutlu runner, API/envanter/checkout gap ve kanıt belgeleri.
- Davranış: 30 dakikalık korumalı e-posta/paket kanıtı yalnız e-postaya gider; CSRF ve açık onay gerekir. Fragment kaldırılır, anahtar yalnız sayfa belleğinde kalır. Kanıtsız/bozuk/yanlış e-posta-paket/amaç/süresi dolmuş istek ödeme veya sipariş oluşturmaz (428). SMTP ayarı kapalıysa eski geçerli kanıtla dahi yeni şirket ödemesi başlamaz (503). Mevcut doğrulanmış owner parola/MFA akışı korunur.
- Komut + çıktı: `deploy/testing/run-checkout-email-acceptance.ps1` → **15/15**, exit 0, 5.9858 dakika. Web build 0 hata/0 uyarı; test build 0 hata ve önceki dört ASP.NET deprecation uyarısı. Hedefli gerçek Chromium checkout → exit 0; kota regresyonu 30/30. Kaynak doğrulayıcıları, 2.272 kontrol envanteri ve 2.246 locale kontrolü geçti; `git diff --check` geçti.
- Test: Gerçek yerel SMTP capture; geçerli kanıt/replay; eksik/yanlış/bozuk/expired/purpose reddi; CSRF; SMTP eksikliği/bağlantı hatası/ayar kaldırılması; ayrı rate-limit; API yetki envanteri; mevcut ödeme/provisioning ve owner formu. Browser: çift e-posta gönderimini engelleme, başarısız gönderimden tekrar, yanlış bağlantı, aynı sekme fragment'i, geç gelen eski onayı reddetme, e-posta değiştirme, ödeme sırasında expiry ve bilgi kaybetmeden tekrar doğrulama.
- İlk hatalar: İlk API koşusu 6/12; yeni testlerin global boş sipariş varsayımı diğer fixture siparişlerini gördü. Kontroller ilgili checkout kimliğine bağlandı; diğer veriler silinmedi. İlk browser koşusu aynı sekme hash değişimini yakaladı; hashchange/stale yanıt koruması eklendi. Hata logları saklandı.
- Kanıt: `tests/reports/checkout-email-verification-acceptance-20261024.md`, final API/browser/build/kota ve ilk hata logları. Geçici DB/ağlar temizlendi. Script URL'si yeni doğrulama protokolü için versiyonlandı.
- CI: Önceki `dfdb36e` run `40035243704` **3/3 success**; kanıt `pr25-ci-dfdb36e-20261024.json`. Yeni kota/e-posta commit'leri için ayrıca uzak CI gerekir.
- Sınır/sıradaki iş: Gerçek inbox/iyzico, staging, taze yedek/rollback, Santa/Farma/Demo3 ve platform impersonation kabulü açık. Yeni paketi PR #25'e gönderip tam CI'ı takip et; sonra P0-E ve kalan dış servis kabulünü ilerlet. Canlı deploy yapılmadı.

### 24 Ekim 2026 — P0-E / 20.5 platform destek oturumu
- Durum: TAMAM (yerel uygulama ve hedefli kabul); tam yeni CI, staging ve canlı kabul açık.
- Değişen dosyalar: süreli UserSession metadata/migration/snapshot; PlatformImpersonation API/middleware; session/logout; yönetici üye eylemi ve gerekçe penceresi; müşteri sarı bandı/dönüş; API/Chromium testleri, runner, envanter ve belgeler.
- Davranış: MFA + 10 dakikalık step-up; gerçek müşteri rolü ve tek şirket; en fazla 15 dakika; HttpOnly tek kullanımlı dönüş ve asıl oturum yetkisi kontrolü; gerçek yöneticiye bağlı başlangıç/istek/sonuç/bitiş audit'i. Çıkış yöneticiye döner ve sonraki kritik yazma yeniden doğrulama ister.
- Komut/çıktı: Docker .NET Web build 0 hata/0 uyarı; test build 0 hata, önceki dört deprecation uyarısı. Gerçek Identity/PostgreSQL kabulü 13/13, exit 0 (2.2914 dakika). Chromium müşteri ve yönetici kabulü exit 0; sarı bant ekran görüntüsü incelendi. 2.279 kontrol envanteri, 2.249 locale ve ilgili kaynak doğrulayıcıları geçti.
- İlk hatalar: yeni test reauth alanları/yanıt beklentisi ve olmayan kategori yolu düzeltildi; browser fixture eksik üye id alanı gerçek DTO'ya eşlendi. İlk hata logları saklandı; kontroller kaldırılmadı.
- Kanıt: tests/reports/platform-impersonation-acceptance-20261024.md ve bağlı log/görsel. Geçici DB/ağ temizlendi. Önceki 4e4f255 CI run 40040576315 3/3 success; yeni commit CI'ı ayrıca gereklidir.
- Sınır/sıradaki iş: Hedef 20'nin gerçek Santa/Farma/Demo3 kabulü, SMTP/iyzico, staging, güncel doğrulanmış yedek/rollback açık. Canlı migration/deploy yapılmadı. PR #25'e gönder; tam CI ve dış ortam kapılarını ilerlet.

24 Ekim CI takibi: 54a0600 run 40045415095 Linux kaynak kontrolü Verify-AuthErrorFlows üzerinde başarısız oldu. Yerel tekrar, yeni destek bandını tanımayan VM fixture seçicisini gösterdi. Fixture güncellendi; 401 sonrası destek kimliğinin sıfırlanması ve bandın gizlenmesi ayrıca doğrulanıyor. CI içindeki 79 Node kaynak komutunun tamamı yerelde geçti (platform-impersonation-all-source-20261024.log); hedefli auth-flow logu da başarılı. Ürün davranışı değiştirilmedi. Yeni commit için tam CI gerekir; eski run sırasında iki diğer iş çalışıyordu.

### 24 Ekim 2026 — P0-B güncel 118 → 120 veri koruma kabulü
- Durum: TAMAM (izole kabul); gerçek yedek/restore, staging ve canlı kabul açık.
- Değişen dosyalar: CurrentReleaseMigrationAcceptanceTests.cs, tek komutlu migration kabul runner ve kanıt belgeleri. Ürün/migration kodu değişmedi.
- Test: Tarihsel 118 kimlik listesi MD5 eşitliği ve iki yeni kimlik; public fallback olmadan rastgele schema; sentetik şirket/kullanıcı/plan/oturum/checkout/varlık; tüm özgün tablo alanı/satır projeksiyonları; idempotent SQL ilk/tekrar eşitliği; eski oturum ve checkout yeni alanları null. 1/1 exit 0, 41.9429 saniye; test build 0 hata ve önceki dört uyarı.
- İlk hata: sentetik sipariş 0 tutarla mevcut Amount check constraint kuralına takıldı; planla uyumlu 2400 tutar/ara toplam kullanıldı. Ürün kuralı değişmedi. İlk log saklandı, satır sonu boşlukları temizlendi.
- Kanıt: tests/reports/current-release-migration-acceptance-20261024.md; first/second logları ve salt-okunur production kontrolü. Apex/firma 118/0 ready; kaynak 120; canlı plan boş, checkout kapalı.
- Sınır: Bu sentetik izole schema, güncel üretim yedeğinin staging provasının yerine geçmez. SSH anahtarlı erişimi reddedildi; kullanıcıdan yönetim bağlantısı bilgisi istendi. Canlı değişmedi.
- CI: ba39391 run 40045829382 Windows ve Linux güvenlik işleri başarılı, PostgreSQL paketi sürüyor. Yeni migration kabulü henüz bu CI head içinde değil; mevcut canlı koşuyu sırf zaman doldu diye yeniden başlatmadan yerel commit hazırlanıyor.
- Sıradaki: devam eden tam CI sonucu; bu kabulü PR dalına gönder; erişim sağlanınca taze SQL/medya/anahtar yedeği, gerçek restore/staging ve SMTP/iyzico/Bölüm 20 kabullerini yürüt.

### 24 Ekim 2026 — P0-B somut release SQL dosyası
- Durum: TAMAM (dosya/paket/izole kabul); gerçek üretim restore/staging açık.
- EF CLI 10.0.0 ile 118 → 120 idempotent SQL ve SHA-256 dosyası hazırlandı. Test artık dosya hash'ini ve güncel EF çıktısıyla metin eşitliğini doğrulayıp dosyanın kendisini ilk/tekrar uygular. Test çıktı kopyası ve Docker COPY bağlantısı eklendi.
- Kabul: run-current-release-migration-acceptance.ps1 güncel image ile 1/1, exit 0; 56.3684 saniye. Web build 0 hata/0 uyarı; test build önceki dört uyarı. İlk startup-project, BOM ve izole schema normalizasyon hataları düzeltilip saklandı; kontroller kaldırılmadı.
- Kanıt: current-release-migration-acceptance-20261024.md; SQL/checksum ve exact-sql-packaged-verified logu. Geçici DB/ağ temizlendi.
- CI: ba39391 run 40045829382 üç işte 3/3 success; pr25-ci-ba39391-20261024.json. Bu yeni dosya/paket testinin tam CI sonucu ayrıca gerekir.
- Erişim yeniden doğrulandı: IAB Coolify Terminal sekmesinde açık yönetici oturumu var; localhost sunucu terminaliyle docker ps salt-okunur çalıştı. Yeni credential oluşturulmadı. Kullanıcıdan tekrar bağlantı istemeden mevcut oturumla ilerle.
- Sıradaki: PR dalını güncelle; mevcut Coolify oturumunda taze SQL/medya/anahtar/imaj yedeği, ayrı restore ve staging; ardından tam gerçek müşteri/SMTP/iyzico kabulü. Canlı DB/ayar/deploy henüz değişmedi.

## 24 Ekim 2026 — gerçek yedek, geri dönüş ve staging

Mevcut Coolify oturumuyla erişim sağlandı; SSH/API erişimi engeli bu çalışma için çözüldü. Taze 142 MB geri dönüş paketi doğrulandı; gerçek dump ayrı DB'ye geri yüklendi (95 tablo sayımı eşit). 118→120 exact SQL ve tekrarında 94 veri tablosunun özgün alan/satırları korundu. Aday staging imajları derlendi; yalnız internal ağda, dış port olmadan 11 temel HTTP/güvenlik kontrolü geçti. ae875b6 CI 3/3 success (40073522201). Canlı 118/0 olarak kaldı; dağıtım yapılmadı. Tam kimlik doğrulamalı müşteri/provider/Bölüm 20 kabulü açık. Kanıt: tests/reports/real-server-staging-acceptance-20261024.md ve altı Coolify ekran görüntüsü.

### 24 Ekim 2026 — gerçek satış kapıları ve canlı oturum
- Durum: KISMİ; gerçek SMTP/iyzico ve kimlik doğrulamalı Bölüm 20 açık.
- Gerçek staging checkout available:false, trial available:false (15 gün/yıllık), plan listesi boş. Canlı runtime SMTP host/kullanıcı/parola/gönderici ve Iyzipay anahtarları boş/yok; gizli değer çıktısı alınmadı. Kaydedilmiş tüm Coolify ayarları denetlenmiş sayılmaz.
- Medya/anahtar arşivleri mevcut canlı volume dosyalarıyla tekrar eşleşti; özel migration kanıt arşivi geçerli. Staging üç süreç çalışıyor. ae875b6 tam CI 3/3 success; JSON kanıtı kaydedildi.
- Farma parolası doğrulandı, MFA istendi; kullanıcı giriş yaptığını bildirse de kontrol edilen sekme MFA ekranında. Giriş/kapsam testi geçti denmedi; kurtarma kodu kullanılmadı, MFA değiştirilmedi.
- Kanıt: tests/reports/live-auth-sales-followup-20261024.md. Canlı deploy/migration ve satış açılmadı.

### 24 Ekim 2026 — P0-D yıllık abonelik yaşam döngüsü kabulü
- Durum: TAMAM (izole kapsam); gerçek sağlayıcı/canlı kapsam açık.
- Beş gerçek API/Identity/PostgreSQL + sentetik sağlayıcı senaryosu 5/5, exit 0, 1.2723 dakika. Aktif/iptal edilmiş dönemde farklı paket reddi; süresi biten hesapta paket geçişi; iptal edilmiş aynı paket yenilemesinde kalan süre +365 gün; askıya alınmış hesapta ödeme öncesi ret. Başarılı callback tekrarları tek aktivasyon; özgün tarih/plan ve iki kiracının varlıkları korundu.
- Değişiklik: yalnız ExistingCompanyCheckoutAcceptanceTests.cs ve kanıt belgeleri. Ürün kuralı/migration değişmedi. İlk komutta mevcut Docker entrypoint tekrarlandığı için MSB1008; düzeltilen komutla aynı image kabulü geçti. Geçici DB/ağ kaldırıldı.
- Kanıt: tests/reports/annual-lifecycle-acceptance-20261024.md ve ilk/başarılı loglar. Tam canlı müşteri/SMTP/iyzico kabulü yerine geçmez.

### 24 Ekim 2026 — P0-D/E kayıtlı sağlayıcı ayarı denetimi
- Durum: EKSİK GEREKSİNİM DOĞRULANDI; dış sağlayıcı kabulü tamamlanmadı.
- Coolify production kayıtlarında EMAIL_SMTP_PASSWORD, IYZIPAY_API_KEY ve IYZIPAY_SECRET_KEY yüklenmiş edit alanları hasValue:false. Sadece alan doluluğu okundu; gizli değer, görünür yapma veya kopyalama yok. Üç alan için yalnız uygulanmamış ayar ihtimali elendi. Hiçbir ayar kaydedilmedi/deploy edilmedi.
- Kullanıcıdan sağlayıcı hesaplarının erişilebilirliği istendi; sohbete secret istenmedi. Farma kontrol edilen IAB sekmesi hâlâ MFA bekliyor; giriş kabulü doğrulanmış sayılmadı.
- Kanıt: tests/reports/coolify-saved-provider-config-20261024.md ve üç ekran görüntüsü. c328a2a CI run 40077928353 canlı/in_progress; yeniden başlatılmadı.

### 24 Ekim 2026 — P0 gerçek sağlayıcı kabulünün uygulanabilir listesi
- Mevcut compose ve gerçek endpoint kodundan production ayar eşlemeleri, kullanıcı tarafından güvenli secret girişi, gerçek inbox ve sandbox ödeme doğrulama sırası hazırlandı: deploy/coolify/PROVIDER-ACCEPTANCE.md.
- Liste tamamlanmış kabul değildir. SMTP 202 ile inbox teslimatını, callback HTTP 200 ile Provisioned siparişini ayırır; dışa kapalı staging'in sağlayıcı kabulüne otomatik uygun olduğunu iddia etmez. Gerçek para tahsilatı kullanıcıya bırakılır.
- Ürün kodu/migration değişmedi; yeni test/derleme çalıştırılmadı. c328a2a run 40077928353 PostgreSQL işi canlı/in_progress olarak doğrulandı; diğer iki iş başarılı. Açık sağlayıcı/MFA engelleri sürüyor.

### 24 Ekim 2026 — Bölüm 20 Santa canlı giriş adımı
- Durum: BAŞARISIZ. Hedef 5.3.2 test hesabıyla santa.envantertr.com üzerinde tek giriş denemesi gerçek UI'da “Giriş bilgileri geçersiz veya hesap geçici olarak kullanılamıyor.” döndürdü. Yanlış parola/kilit/üyelik ayrımı bu mesajdan yapılamaz; kök neden iddia edilmedi. Tekrar/reset/MFA bypass yapılmadı.
- Dashboard ve sonraki 20.2 adımları doğrulanmadı. Farma mevcut çerezle yeniden açıldığında giriş ekranı gösterdi; erişilebilir açık oturum yok. Secret veya hesap değeri rapora alınmadı.
- Kanıt: tests/reports/santa-live-login-20261024.md. CI 40077928353 PostgreSQL işi hâlâ canlı; diğer iki iş başarılı. Üretim sürümü/şeması değişmedi.

### 24 Ekim 2026 — Santa canlı tenant kaydı eksikliği
- Salt-okunur gerçek DB sayımı: exact ad Santa 0, slug santa 0, slug farma 1. Santa üyelik/kilit sorgusu 0 satır; bu yüzden kilit/parola kök nedeni iddia edilmedi. Eksik şirket/portal kaydı doğrulandı.
- Normal denetimli provisioning için platform yönetici parolası doğrulandı; yönetici MFA adımı kullanıcıya bırakıldı. Hesap/üyelik/oturum SQL ile oluşturulmadı, MFA değiştirilmedi.
- Kanıt: tests/reports/santa-tenant-diagnosis-20261024.md ve coolify-santa-missing-tenant-20261024.png. CI son görülen running; sonraki DNS gözlem hatası terminal sonuç sayılmadı.

### 24 Ekim 2026 — yönetici oturumu ve doğrulanmış yayın adayı
- Yönetici MFA oturumu gerçek panelde açıldı. Sistem sağlığı SMTP/iyzico eksikliğini ve Cloud planı olmadığını gösterdi. Santa oluşturma eylemi etkin Cloud planı istedi; eski canlı UI Cloud kullanıcılarını hâlâ sınırsız gösterdiği için eski sürümde ücretli katalog yayınlanmadı.
- c328a2a0568d6659add87062a2d708cf1fd38653 tam CI run 40077928353 artık 3/3 success; JSON kanıtı kaydedildi. Adayın src/deploy-coolify/Dockerfile dosyaları gerçek staging'de sınanan ae875b6 ile aynı (git diff boş).
- Yeni dump: pre-web-v1-20261024T103444Z.dump. 142 MB özel rollback paketi /var/backups/envantertr/rollback-20261024T103525Z; yedi checksum ve dosya/imaj restore provası geçti. Yeni dump ayrı env..._1035 DB'ye restore edildi, 95 tablo sayımı canlıyla eşleşti; 118→120 ilk/tekrar provasında 94 özgün veri tablosunun alan/satırları aynı.
- Hash'i doğrulanmış SQL canlıya uygulandı: 118→120 başarılı. Özgün 94 veri tablosunun satır sayımları migration öncesi/sonrası eşit.
- Coolify dalı feat/verified-cloud-trials-20261021, SHA c328a2a olarak kaydedildi ve sayfa reload sonrası korundu. Changes not applied, yeniden yayın bekleyen kayıtlı yapılandırma anlamında görüldü. Deploy we99jqgmmvgojjbf4yj1qrjp başlatıldı; şu anda derleniyor, başarı iddia edilmiyor.
- Ödeme/SMTP/deneme açılmadı; gerçek sağlayıcı ve Bölüm 20 kabulü açık. Üretim şeması artık 120; eski notlardaki 'canlı değişmedi/118' bu andan öncesinin tarihsel durumudur.

### 24 Ekim 2026 — c328a2a canlı kabul yayını başarılı
- Coolify we99jqgmmvgojjbf4yj1qrjp 4dk45sn, yeşil Finished. Çalışan web/public image tag'leri c328a2a, DB healthy; HTTPS apex ve Farma ready/120/0. Eski 'canlı değişmedi' kayıtları tarihsel kaldı.
- Platform yönetici oturumu taşındı; yeni Cloud kota ve yıllık katalog önizlemesi gerçek panelde doğrulandı. Sekiz taslak kaydı ek 10dk yönetici reauth istedi; kullanıcıya MFA alanı bırakıldı, henüz oluşturuldu sayılmadı.
- Tam satış probu beklenen exit 1: 0 public plan, checkout kapalı. Sağlayıcı/inbox ve Bölüm 20 açık; V1/satış tamamlandı denmedi.
- Kanıt: tests/reports/production-c328-release-20261024.md, CI terminal JSON, iki dış probe logu ve sekiz ekran görüntüsü.

### 24 Ekim 2026 — sekiz yıllık Cloud paketi canlı katalogda
- Gerçek yönetim akışında 8 taslak oluşturuldu, her paket gerekçeli kayıtla yayımlandı. Public API ve ana sayfa 1/4/10/50/100/250/1000/2000 giriş hesabı için sırasıyla 2400/4200/6000/9000/12000/14400/24000/36000 TRY yıllık fiyatı doğruladı. Aylık çekim yok; gösterilen aylık tutar yıllık fiyatın eşdeğeri. KDV hariç, 2000 üzeri özel teklif.
- Kanıt: tests/reports/live-annual-catalog-20261024.md, public JSON ve üç ekran görüntüsü. Ödeme/SMTP/trial kabulü tamamlanmadı. Sonraki yayın öncesi bu yeni katalog kayıtlarını içeren yedek alınmalı.
- Farma destek oturumu kritik doğrulaması başarısız kaldı; destek oturumu başlamadı. Kullanıcıdan aynı kod tekrar istenmedi. İlk yönetici MFA girişinin mevcut 10 dakikalık işlem kanıtı olarak kullanılmasına yönelik dar kapsamlı düzeltme yerelde sınanıyor; henüz canlıda değil.

### 24 Ekim 2026 — yönetici girişinden hemen sonra yinelenen MFA istemi düzeltmesi
- Başarılı platform-only parola+MFA girişine mevcut 600 saniyelik platform_reauth_at kanıtı eklendi. Şirket girişi/parola-only oturum bunu alamaz; süre uzatılmadı, mevcut eski oturumlar kendiliğinden yetkilendirilmedi. Migration yok.
- Gerçek PostgreSQL/Identity çerezleriyle 15/15 kabul, exit 0; iki kaynak doğrulayıcı ve diff kontrolü geçti. İlk testte eski takipli scope okuması düzeltilerek kurtarma kodu tüketimi yeni scope ile kanıtlandı.
- Kanıt: tests/reports/platform-fresh-login-acceptance-20261024.md ve ilk/başarılı loglar. Canlı c328 hâlâ bu düzeltmeyi içermiyor; yeni CI/staging/yedek/yayın adımları gerekiyor. Gerçek Farma destek kabulü tamamlandı sayılmadı.

### 24 Ekim 2026 — 8db0343 gerçek staging ve güncel geri dönüş provası
- Tam aday SHA 8db0343753e4e728ce7253ec221af6445118f018 uzak dala gönderildi. CI 40098994968: Linux güvenlik ve Windows deployment/backup başarılı; PostgreSQL kabulü sürüyor. Başarılı iş tekrar başlatılmadı.
- Sunucuda aday imaj derlemesi exit 0; yeni staging web ayrı medya/anahtar kopyalarıyla internal ağda, host portu yok. Üç host için ready/120/0; CSRF, anonim assets 401, ana alan platform API 401, tenant host platform API 404 doğrulandı. Dış sağlayıcı secret'ları boş.
- Yayımlı katalog sonrasındaki taze dump 20261024T134018Z ayrı DB'ye restore edildi; 95 tablo sayımı canlıyla aynı. 140 MB rollback paketi 20261024T134137Z; dosya/imaj geri dönüş provası geçti. Migration farkı yok.
- Kanıt: tests/reports/platform-fresh-login-staging-20261024.md ve üç ekran görüntüsü. CI son işi bitmeden yeni aday canlıya alınmadı; üretim hâlâ c328a2a. Kullanıcıdan yeni OTP istenmedi.

### 24 Ekim 2026 — tam CI test verisi çakışması
- Run 40098994968 terminal BAŞARISIZ: PostgreSQL kabulü 326/327, diğer iki iş başarılı. Tek hata yıllık katalog testinde IX_SubscriptionPlans_Code; yeni MFA testinin ortak disposable DB'de bıraktığı sabit katalog kodlarıyla çakıştı.
- Yeni test artık GUID içeren, yayımlanmayan iki ayrı planı gerçek yetkili API ile oluşturur; iki ardışık yazmanın ve iki PlanCreated denetim kaydının başarılı olmasını doğrular. Katalog endpoint'i/ürün MFA kodu/güvenlik süreleri değiştirilmedi. Katalog + MFA + destek testleri birlikte yeniden çalışıyor; henüz geçti sayılmadı.
- Kanıt: tests/reports/ci-8db-catalog-collision-20261024.log. Üretim yayını yapılmadı; test hata kaydı gizlenmedi, başarısız iş sebepsiz yeniden başlatılmadı.

### 24 Ekim 2026 — katalog/MFA ortak regresyonu düzeltildi
- Benzersiz test planı kodu 40 karakter sınırına uygun hale getirildi. Temiz disposable DB'de katalog + yeni MFA + şirket oturumu ret + destek oturumu senaryoları **16/16 geçti**, exit 0, 3.4624 dakika. Ara 14/16 başarısız deneme logu da korundu; ürün validasyonu gevşetilmedi.
- Ürün/runtime kodu staging'deki 8db0343 ile aynı; yalnız test ve kanıt dosyaları değişti. Tam CI yeni başlık için yeniden geçmeden canlı yayın yapılmayacak. Geçici yerel DB'ler test tamamlandıktan sonra kaldırıldı.
- Kanıt: tests/reports/platform-fresh-login-acceptance-20261024.md, iki ortak regresyon logu, terminal CI JSON ve hata özeti.

### 24 Ekim 2026 — canlı katalog/satış sınırı yeniden doğrulandı
- Salt-okunur Check-CoolifyProduction: kaynak/canlı 120 migration, apex/firma health 200 ready, public plan 8; checkout unavailable. Tam satış probu beklenen exit 1; katalog yayımlamak ödeme kabulü değildir.
- PROJECT-STATUS ve AUDIT-REALITY güncel başlıklarındaki eski 'plan yok' bilgisi düzeltildi; başarısız 8db CI ve yeni bf3ed6d CI 40102471823 ayrıldı. Kullanıcıdan yeni kod istenmedi, mevcut destek oturumu kabul edildi sayılmadı.
- Kanıt: tests/reports/live-catalog-sales-boundaries-20261024.log. Bu salt-okunur kontrol hiçbir ayar/hesap/ödeme değiştirmedi.

### 24 Ekim 2026 — P0 yeni demo şirketinin normal yönetim akışı
- Eksik bulundu: yeni şirket yalnız ücretli satış doğrulamasıyla açılabiliyordu; mevcut demo işlemi şirket önceden var olmasını gerektiriyordu. Aynı route/transaction/TenantProvisioningService üzerinden açık demo modu eklendi. 15 gün varsayılan, 1–60 gün sınırı, zorunlu gerekçe; ödeme bilgisi reddi, Trial/IsDemo ve ayrı audit/timeline kaydı. MFA/CSRF/rol kontrolleri korundu; yeni sahibi e-posta doğrulanmış sayma veya parolasını değiştirme yok.
- API/Identity/PostgreSQL **11/11**, masaüstü+390×844 Chromium exit 0, **79/79** CI kaynak komutu geçti. İlk eski UI inventory çıktısı yenilendi; ilk/son loglar korundu. Migration eklenmedi. Geçici yerel test kaynakları kabul sonrası temizlenecek.
- Kanıt: tests/reports/platform-demo-provisioning-acceptance-20261024.md ve log/iki form görüntüsü. Bu ekleme henüz canlı değil; Santa/Demo3 gerçek kabulü tamamlandı sayılmaz. bf3ed6d MFA-only CI 40102471823 ayrı olarak 3/3 başarılı; yeni demo için kendi tam CI/staging/yayın kabulü gerekir.

### 24 Ekim 2026 — yinelenen yönetici doğrulaması düzeltmesi canlıda
- bf3ed6de538423ed8744047a08e4e7f3d2762c5d, CI 40102471823 3/3 success. Ürün/runtime kaynakları gerçek staging 8db0343 ile aynı; migration farkı yok. Yeni dump 201739Z ayrı DB'ye restore, 95 tablo sayımı eşit; 140 MB rollback paketi 201929Z dosya/imaj provası geçti.
- Coolify kesin SHA kaydı reload ile doğrulandı; is1feyzg0b9enmg0uovqtbtb dağıtımı yeşil Success/Finished. Gerçek web/public imajları bf3ed6d; konteyner son eki 20261024T202457, DB healthy. Apex/Farma HTTPS ready/120/0 exit 0; 8 public plan korundu, checkout kapalı olduğu için tam satış probu exit 1.
- Kanıt: tests/reports/production-bf3-release-20261024.md, CI terminal JSON, iki probe ve altı ekran görüntüsü. Kullanıcıdan yeni OTP istenmedi; yeni canlı MFA girişi yapılmış gibi raporlanmadı. Eski oturumlar kendiliğinden taze kanıt almıyor.
- Demo 5790980 uzak dala gönderildi; ayrı tam CI 40129797684 çalışıyor. Demo özelliği bu MFA yayınına dahil değil. Yerel geçici demo DB/ağları temizlendi. Gerçek SMTP/iyzico ve Bölüm 20 hâlâ açık.

### 24 Ekim 2026 — demo adayı gerçek staging'de
- 5790980 tam kaynak sunucuda exit 0 derlendi; envtr-stage-web-5790980 internal ağda, dış port olmadan ayrı medya/anahtar kopyaları ve staging DB ile açıldı. Apex/Farma/Santa host sağlığı 120/0; yeni demo kontrolü sunulan HTML'de var. Anonim assets/platform API 401, CSRF yanıtı doğrulandı. Sağlayıcı secret'ları boş; üretim verisine/dosyasına bağlanılmadı.
- CI 40129797684 güvenlik ve Windows işleri başarılı, PostgreSQL işi canlı/in_progress. Üretim bf3ed6d olarak korunuyor; yeni demo yayımlandı sayılmadı. Yeni yayın öncesi bf3ed6d imajlarını kapsayan taze yedek/restore gerekir.
- Kanıt: tests/reports/platform-demo-staging-20261024.md ve iki staging görüntüsü. Yeni şirket oluşturma veya gerçek sağlayıcı kabulü bu temel staging probuyla kanıtlanmış sayılmaz.

### 24 Ekim 2026 — demo yayın kapıları geçti, dağıtım başladı
- 5790980 CI 40129797684 üç işte terminal success. Gerçek staging aynı commit ve 120/0; yeni migration yok. Yeni 204650Z dump ayrı 2046 restore DB'sinde 95 tablo sayımı eşit. 140 MB rollback 205007Z, dosya/imaj provası 205224Z; hemen önceki bf3ed6d imajları doğrulandı.
- Coolify SHA 5790980 kaydı reload sonrası korundu; esw998lnesfol9swvgxwq3li dağıtımı doğru commit ile başladı. Henüz tamamlandı iddia edilmedi.
- Kanıt: tests/reports/production-demo-release-20261024.md ve terminal CI JSON/üç yayın hazırlık görüntüsü. Canlıda istenen üç görsel, galeri kodu, portal, yeşil hesap güvenliği ve şeffaf etiket dosyaları kaynak hash'leriyle eşleşti; bf3-live-product-gallery-integrity-20261024.log. Bu kontrol yeni etkileşimli tarayıcı kabulü olarak sayılmadı.

### 25 Ekim 2026 — demo oluşturma özelliği canlıda
- 5790980 tam CI 3/3 + gerçek staging + güncel bf3ed6d SQL/medya/anahtar/imaj yedeği ve ayrı geri dönüş provası sonrası esw998lnesfol9swvgxwq3li yayını yeşil Success/Finished. Gerçek web/public imaj etiketleri 5790980; konteyner son eki 20261024T205706, DB healthy.
- Apex/Farma HTTPS ready/120/0 exit 0. Canlı platform HTML yeni demo düğmesi, açık demo isteği ve 15 gün varsayılanını sunuyor; 8 plan korundu. Checkout eksik sağlayıcı nedeniyle kapalı, tam satış probu exit 1. Canlı yönetici oturumu reload sonrası giriş ekranında kaldı; yeni parola/OTP denemesi yapılmadı.
- Kanıt: tests/reports/production-demo-release-20261024.md, terminal CI JSON, 25 Ekim sağlık/form/satış sınırı logları ve iki tamamlanma görüntüsü. Gerçek Santa/Demo3 oluşturma veya Bölüm 20 tamamlandı iddia edilmedi. SMTP/iyzico gerçek kabulü ve müşteri işlemleri açık.

### 25 Ekim 2026 — firma çöp kutusu ve geri alma adayı
- Hedef 20.9.5 için normal platform akışına gerekçeli, alt alan adı onaylı silme/geri alma eklendi. Yalnız hedef şirketin oturum/API anahtarları kapatılır; ortak kimlik ve diğer kiracı kayıtları korunur. Çöp kutusundan geri alma erişimi askıda bırakır; ödeme durumuna dokunmaz.
- İzole PostgreSQL/Identity 11/11, yeni SQL'i iki kez uygulayan veri koruma testi 1/1, masaüstü/mobil Chromium ve mevcut 79 Node kaynak kontrolü geçti. Yeni migration 121 yalnız nullable DeletedAt ve silinen-pasif kısıtı ekler; yıkıcı Down engellendi. Yeni kaynak doğrulayıcı CI'ye eklendi; tüm taban dallı PR'lar CI kapsamına alındı.
- Henüz canlıda değil: canlı 5790980 / şema 120 korunuyor. Tam CI, sunucu staging, güncel yedek/restore ve geri dönüş provası ardından yayın gerekir. Gerçek Bölüm 20 ve sağlayıcı kabulü açık. Ayrıntı: tests/reports/company-deletion-acceptance-20261025.md.

### 25 Ekim 2026 — firma çöp kutusu canlıda
- 6198ef6 tam CI 3/3, gerçek sunucu staging ve tekrarlı migration sonrası yayınlandı. 94 veri tablosunun özgün alan parmak izleri korundu. Yayın öncesi yeni 023646Z dump, 140 MB 023647Z rollback ve 023832Z dosya/imaj provası; ayrı restore DB'de 95 tablo sayımı eşit.
- Kaynak SHA gerçek reload sonrası korundu. Üretime yalnız eklemeli 120→121 SQL uygulandı; q7up7i4mglpe1w1ismqpnmzn Success/Finished. Gerçek web/public imajları 6198ef6, konteyner son eki 20261025T025226, DB healthy. Apex/Farma HTTPS 200 ready/121/0; canlı çöp kutusu kontrolleri sunuluyor.
- 8 plan korundu, checkout eksik sağlayıcı yüzünden kapalı. Galeri JS/CSS + üç cihaz görseli + portal + portreli güvenlik görseli + şeffaf etiket dosyaları birebir kaynak hash'leriyle eşleşti. Bu, yeni oturumlu müşteri akışı veya yeni canlı etkileşimli galeri testi değildir.
- Yeni kişisel OTP istenmedi. Gerçek Demo3/Santa/Farma ve SMTP/ödeme kabulü açık; tam Hedef veya ana dala merge tamamlandı sayılmıyor. Kanıt: tests/reports/production-company-deletion-release-20261025.md ve ilişkili canlı log/görüntüler.

### 25 Ekim 2026 — P0 Bölüm 20.4 PUT ve çift yönlü izolasyon
- Hedefteki PUT saldırı isteği varlık güncelleme route'unda desteklenmiyordu. PATCH ve PUT aynı mevcut güncelleme handler'ına bağlandı; şirket/rol/CSRF/sürüm/geçmiş denetimleri ortak kaldı. Migration veya UI değişikliği yok.
- İzole gerçek PostgreSQL ve sentetik kimlikli endpoint kabulü iki yöntem için 2/2 geçti. Her iki yönde yabancı GET/güncelleme 404; kendi şirketinde güncelleme/geçmiş, CSRF/izleyici/stale-version retleri doğrulandı. İlk koşudaki yeniden kullanılabilen CSRF çerezi test beklentisi düzeltildi; başarısız log korundu.
- Kanıt: tests/reports/asset-put-section20-acceptance-20261025.md ve ilk/son koşu logları. Canlı 6198ef6 korunuyor; bu değişiklik tam CI/staging/yayın ve gerçek Santa/Farma kabulü yerine geçmez. Kişisel OTP istenmedi.
- Önceki canlı görsel kabulü de kayda alındı: otomatik sola kayan galeri, sabit çerçeve/yazı, orijinal kaynakla tam ekran portal, Escape/fokus/Enter kontrolleri. Kanıt: tests/reports/live-visual-and-mfa-followup-20261025.md.

### 25 Ekim 2026 — PUT adayının sunucu staging ve geri dönüş kapıları
- Kesin 124b2d1 kaynağı sunucuda exit 0 derlendi. Yeni staging veritabanı güncel 034105Z üretim yedeğinden restore edildi; 95 tablo sayımı eşit. Ayrı medya/anahtar kopyaları, internal ağ ve dış portsuz/read-only yeni uygulama ile apex/Farma/Santa host sağlığı 121/0, anonim GET/PUT/PATCH 401 ve Secure CSRF kontrolü geçti.
- Güncel 6198ef6 imajlarını içeren 140 MB 034258Z rollback paketi ve 034349Z dosya/imaj geri dönüş provası geçti. İlk yanlış public konteyner zaman damgası preflight'ta reddedildi; gerçek envanterden düzeltilerek paket tamamlandı.
- CI 40158737966 Linux güvenlik ve Windows işleri başarılı; PostgreSQL işi 121065583776 çalışıyor. Sonuç beklenmeden üretim değiştirilmedi. Kanıt: tests/reports/asset-put-server-preparation-20261025.md, CI JSON ve beş sunucu görüntüsü. Kişisel OTP istenmedi; gerçek SMTP/ödeme/Bölüm 20 kabulü açık.

### 25 Ekim 2026 — Bölüm 20.9 dış DNS/TLS kanıtı
- Salt-okunur public DNS-over-HTTPS ve normal sertifika doğrulamalı HTTPS ile demo3, santa ve yeni acceptance-20261025 alt alan adları dışarıdan doğrulandı: DNS cevapları var, /api/status 200 ready. DNS/proxy/sertifika ayarı veya şirket kaydı değiştirilmedi.
- Bu, önceki staging Host-header kanıtından daha güçlü gerçek dış erişim kanıtıdır; şirket provisioning/giriş/izolasyon/silme kabulü değildir. İlk Windows demo3 DNS sorgusundaki paket hatası bağımsız resolver ve gerçek HTTPS ile yeniden kontrol edildi, kesinti sayılmadı.
- Kanıt: tests/reports/tenant-public-dns-tls-20261025.md ve JSON. Kişisel OTP gerektirmedi. 124b2d1 PostgreSQL CI işi halen çalışıyor; diğer iki iş başarılı, yayın bekliyor.

### 25 Ekim 2026 — gerçek şirket varlığı ile DNS kanıtı ayrıldı
- Güncel üretim PostgreSQL salt-okunur slug agregası: farma 1 aktif kayıt; santa 0, demo3 0. Sadece istenen üç slug sayıldı, kullanıcı/kimlik/credential veya müşteri envanteri çıkarılmadı, yazma yapılmadı.
- Böylece public DNS/HTTPS sağlık başarısının şirket oluşturma kanıtı olmadığı gerçek DB çıktısıyla doğrulandı. Sıradaki gerçek kabul işi normal platform akışından Santa/Demo3 oluşturulmasıdır; DNS/proxy yeniden ayarı gerekçesi yok. Farma'nın 10 müşteri adımı hâlâ ayrı kabul gerektirir.
- Kanıt: tests/reports/tenant-public-dns-tls-20261025.md, live-section20-company-presence-20261025.sql ve görüntüsü. CI PostgreSQL işi 121065583776 halen çalışıyor; yeni OTP istenmedi.

### 25 Ekim 2026 — Hedef 15.10 yayın sonrası gözlem planı
- docs/PRODUCTION-24H-OBSERVATION.md eklendi: T0/5 dakika/ilk saat/24 saat kontrol aralıkları, UTC kanıt dosyaları, health ile ticari kapı ayrımı, müdahale eşikleri, şema 121'i koruyan geri dönüş ve gözlem boşluğu kuralları.
- Mevcut salt-okunur health komutu gerçek apex/Farma/Santa HTTPS üzerinde 200 ready/121/0, exit 0 verdi; pre-put-release-health-baseline-20261025.log. Bu tek anlık yayın öncesi ölçümdür, 24 saat tamamlandı sayılmadı; sürekli görev veya nöbetçi atanmış gibi gösterilmedi.
- 124b2d1 CI PostgreSQL işi çalışıyor. Diğer kapılar hazır; tam Hedef/Bölüm 20/gerçek sağlayıcı kabulü açık.

### 25 Ekim 2026 — PUT sürümü CI kapısını geçti, üretim yayını başladı
- 124b2d1 CI 40158737966 tam 3/3 success. Kalıcı kaynak SHA reload ile doğrulandı. Gözlem kesintisi sonrası mevcut üretim/staging imajları yeniden doğrulandı; yeni dağıtım yoktu.
- Yayın öncesi yedekler tazelendi: 080433Z SQL dump, 140 MB 080518Z bundle, 080618Z dosya/imaj provası ve ayrı 0804 restore DB'de 95 tablo sayımı eşit. Mevcut apex/Farma health ready/121/0.
- Coolify 8eyikaefpne2tcfuetcloj3n, 08:08:22 UTC, kesin 124b2d1 ile başladı; henüz tamamlandı sayılmadı. Kanıt: tests/reports/production-asset-put-release-20261025.md ve dört güncel kurtarma görüntüsü. Kişisel OTP istenmedi; gerçek Bölüm 20/sağlayıcı kabulü açık.

### 25 Ekim 2026 — PUT ve çift yönlü izolasyon sürümü canlıda
- Coolify 8eyikaefpne2tcfuetcloj3n Success/Finished. Gerçek web/public imajları 124b2d1, konteyner son eki 20261025T080842; DB 080841 healthy. Şema 121 korunuyor, migration yapılmadı.
- İlk Farma health gözlemi 10 saniye zaman aşımı verdi; log korundu. Bağımsız normal-TLS HTTP isteği ve ardından iki-host Node probu apex/Farma 200 ready/121/0, exit 0 verdi; tek gözlem gecikmesinde yayın yeniden başlatılmadı.
- 8 public plan korundu, checkout eksik sağlayıcı nedeniyle kapalı. 8 görsel/galeri kaynağı SHA256 ile birebir eşleşti. Kanıt: production-asset-put-release-20261025.md, ilk/son health ve commercial/assets logları, tamamlanma/imaj görüntüleri.
- Güncel status/audit başlıkları yenilendi, 24 saat planındaki geri dönüş paketi 080518Z olarak güncellendi. Tam Hedef/Bölüm 20/SMTP/ödeme kabulü hâlâ açık. Kişisel OTP istenmedi.

### 25 Ekim 2026 08:25 UTC — yayın sonrası sağlık ve gerçek kabul engeli
- Yeni yayın sonrası salt-okunur üç-host sağlık kontrolü 08:25:37 UTC: apex/Farma/Santa 200 ready/121/0, exit 0. Kanıt: put-postrelease-health-20261025T082537Z.log. Önceki 08:14 kontrolü ile bu kontrol arasındaki süre sürekli gözlenmiş sayılmadı; 24 saat tamamlanmadı.
- Normal platform girişinde mevcut parola önceki turda doğrulandı; bu tur gerçek sekme hâlâ Authenticator adımında, yönetim oturumu açılmamış. Mevcut kullanıcı sorusu açık bırakıldı, tekrar kod isteği veya parola denemesi yapılmadı. Santa/Demo3 normal provisioning henüz yürütülemiyor; MFA atlanmadı ve eski kurtarma kodları kullanılmadı.

### 25 Ekim 2026 — gerçek normal şirket oluşturma ve silme koruması
- Kullanıcı platform MFA girişini tamamladı. Normal Demo firma oluştur formundan Santa (Hedef yetkilisi) ve Demo3 (mevcut Farma yetkilisi), 15 gün/4 kullanıcı ile oluşturuldu. İki davet e-postası başarısız; gerçek teslimat kabulü olarak sayılmadı.
- Demo3 normal Sil formundan çöp kutusuna taşındı. Önce/sonra Santa/Farma'nın CompanyId içeren 80 tablo satırı + şirket + ortak parola/stamp/MFA projeksiyonları dahil 82 içerik karşılaştırması eşit; özel fingerprint değerleri sunucuda kaldı. İki DemoCompanyProvisioned ve bir CompanySoftDeleted audit kaydı gerçek panelde görüldü.
- Geri alma denemesi 10 dakikalık kritik yetki dolduğunda yeni doğrulama istedi; iptal edildi, geri alma olmadı. Demo3 çöp kutusunda/pasif, Santa/Farma aktif; her şirkette bir owner üyeliği var, Santa sahibinin e-postası doğrulanmamış. Yeni kod istenmedi, MFA gevşetilmedi.
- Kanıt: tests/reports/santa-e2e-20261025/README.md, UI görüntüleri ve salt-okunur SQL. Owner girişleri/10 adım/canlı saldırı testi ve tam Bölüm 20 hâlâ açık. Güncel status/audit başlıkları şirket varlığına göre yenilendi; sonraki işlem Demo3 geri alma ve gerçek hesap/SMTP kabulü.

### 25 Ekim 2026 — SMTP gönderici yapılandırması hazırlandı
- Google resmi SMTP belgesine göre smtp.gmail.com / TLS 587 profili hazırlandı. Coolify production UI'de host, SMTP kullanıcı ve gönderici admin@envantertr.com, public HTTPS URL kaydedildi; port/TLS/ad Compose varsayılanları korundu, preview alanları değiştirilmedi. Host gerçek reload sonrası yeniden açılıp doğrulandı.
- EMAIL_SMTP_PASSWORD korumalı alanı boş. Kullanıcıdan bir kez yalnız bu alana Google uygulama parolasını kaydetmesi istendi; sohbete secret istenmedi, platform parolası Google parolası sayılmadı. Yeni credential agent tarafından oluşturulmadı.
- Henüz redeploy yok; kaydedilmiş ayarlar çalışan runtime veya gerçek teslimat kanıtı değildir. Cloud trial/checkout açılmadı, iyzico değiştirilmedi. Kanıt: tests/reports/google-smtp-preparation-20261025.md ve yalnız gizli olmayan host görüntüsü. Yeni ortam yayını öncesi Santa/Demo3'ü içeren güncel yedek gerekir.

### 25 Ekim 2026 — kullanıcının doğrulama bağlamı kalıcılaştırıldı
- Google 2 Adımlı Doğrulama'nın testlerdeki tekrarlar nedeniyle kapatıldığı kullanıcı açıklaması, Google/platform MFA ayrımı, kaydedilmiş fakat deploy edilmemiş SMTP ayarları ve Demo3 geri alma bekleyişi docs/ACCOUNT-SETUP-CONTEXT.md içine kaydedildi. USER-REQUEST-TRACKER başına bağlantı eklendi; parola/OTP/secret yazılmadı.
- Gerçek platform penceresi yeniden kontrol edildi: reauth hâlâ bekliyor, geri alma gerçekleşmemiş. Yeni kod sorusu veya tekrar parola denemesi yapılmadı; mevcut handoff korundu. Tam Hedef tamamlandı sayılmadı.

### 25 Ekim 2026 — Demo3 geri alma ve gerçek platform destek kabulü
- Kullanıcı doğrulamayı tamamladı. CompanyRestored 17:18 TRT; ayrı gerekçeli CompanyReactivated 17:21 TRT. Gerçek listede Demo3/Farma/Santa aktif; geri alma önce askıda bıraktı, eski oturumlar geri açılmış sayılmadı.
- Normal Panele gir formuyla Demo3'e 15 dakikalık destek oturumu açıldı; gerçek alt alan adı, Demo3 şirket bağlamı ve sarı destek bandı görüldü. Platform yönetimine dön ile gerçek root Genel Bakışına dönüldü. ImpersonationStarted/Ended 17:23 TRT audit kayıtları aynı oturum için ayrı filtrelerle doğrulandı.
- Kanıt: tests/reports/santa-e2e-20261025/README.md ek kabul ve sekiz yeni ekran. Bu destek oturumu owner girişinin/canlı çapraz GET-PUT kabulünün yerine geçirilmedi; HTTP 428 negatif kanıtı açık. SMTP/Google güvenlik ayarı değişmedi, gerçek teslimat/ödeme ve tam Hedef açık. Eski bekleyen geri alma bilgileri güncel context/status/audit notlarında düzeltildi.
## 26 Ekim 2026 — 4237 kesin adayı izole sunucu kabulünden geçti

- Yeni SHA ayrıca derlendi (çıkış 0), dışa kapalı ağda ayrı SQL/medya/anahtar kopyalarıyla başlatıldı. Üç host ready/121/0; anonim GET/PUT/PATCH 401; CSRF/Secure çerez doğrulandı.
- Taze dump, 140 MB rollback ve dosya/imaj provası başarılı. Geri yüklenen kopyada 95 tablonun satır sayıları üretimle aynı. Kanıt: `tests/reports/4237-server-staging-20261026.md`.
- Güncel CI 40253374749: Windows ve Linux security geçti, PostgreSQL kabul işi sürüyor. Üretim hâlâ 124; yayın kapısı henüz kapanmadı. Kullanıcının imza/MFA adımları bekliyor; yeni kod istenmedi.
## 26 Ekim 2026 — ortak oturum sürekliliği canlıda

- TAMAM: 4237 tam CI 40253374749 üç işte başarılı; gerçek kesin aday staging, taze yedek/restore/rollback ve yayın öncesi 95 tablo sayımı geçti. Coolify wbhegg8zyeiziogdpr401ycm Success/Finished.
- Canlı web/public tam SHA 4237b5735859cb9c2672ddc5a2f3a5ef22ba566c, DB healthy, restart 0; üç HTTPS host ready/121/0. SMTP relay korundu. Sekiz kaynak SHA256 eşit, portal tam ekran aç/kapat ve galeri ilerlemesi canlıda gözlendi. Sekiz plan ve kapalı checkout/deneme sınırı korundu.
- Kanıt: tests/reports/production-session-continuity-release-20261026.md ve bağlı log/ekranlar. Kullanıcının imzası/Farma MFA, tam müşteri ve gerçek iyzico kabulü açık. 24 saat gözlem tamamlanmadı; Hedef aktif.
## 26 Ekim 2026 — sunucu üretimli zimmet PDF'si yerel kabulü

- Yerel kod/API/UI/görsel kabul geçti; tam CI ve canlı yayın açık. İmzalı kayıt satırında PDF indir; şirket/çalışan/konum sınırı, iki imza ve üç SHA256 özeti, no-store/attachment. Gömülü Türkçe font, sayfalama, imzalar ve kaynak özetleri PDF'de.
- Son gerçek PostgreSQL API 2/2, çıkış 0; Chromium indirme/390px kabulü çıkış 0; 8 sayfalı PDF'de 85/85 seri birer kez, Türkçe ve iki imza/özet korundu. İlk iki API hatası ve sonra görsel/parser ile bulunan başlık/tekrarlanan satır hataları saklandı ve düzeltildi. Kaynak/JS/UI envanteri/diff kontrolü geçti.
- Kanıt: tests/reports/handover-pdf-20261026.md ve ilgili log/PNG/PDF. Yeni migration yok, canlı 4237 değişmedi; gerçek kullanıcı imzası veya MFA kullanılmadı. Sıradaki P0: aynı doğrulanmış PDF'nin açık eylemle e-posta eki; sonra tam CI ve yayın kapıları.
