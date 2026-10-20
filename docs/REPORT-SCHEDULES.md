# Rapor takvimi

Raporlar ekranındaki **Rapor takvimi**, oturum açmış kullanıcının kendi şirketindeki takvimlerini yönetir. Bu uçlar müşteri Bearer API v1 kapsamında değildir; uygulamanın şirket oturumu, asset.read yetkisi ve rapor plan özelliği gerekir. Yazma istekleri mevcut CSRF doğrulamasını kullanır.

- `GET /api/reports/schedules`: kendi takvimleri, doğrulanmış adres durumu ve posta yapılandırmasının hazır olup olmadığı.
- `POST /api/reports/schedules`: takvim oluşturma (üyelik başına en fazla 10).
- `PUT /api/reports/schedules/{id}`: düzenleme veya durdurma/etkinleştirme. Son yanıtın `version` alanını gönderin.
- `DELETE /api/reports/schedules/{id}?version=...`: kendi takvimini silme; eski sürümde 409.
- `GET /api/reports/schedules/{id}/preview`: mevcut yetkilerle güncel özet. E-posta göndermez; kapalı takvimler ve SMTP kurulmamış ortamda da kullanılabilir. Yanıt `name`, `language`, `generatedAt`, `headers`, `rows` içerir. Başka kullanıcı/şirket kaydı 404 verir.

Türler: `inventory`, `maintenance`, `licenses`, `stock`. Bakım raporu ayrıca bakım özelliğini gerektirir. Günlük/haftalık/aylık zamanlama, saat dilimi ve beş rapor dili vardır. Aylık gönderim ayın 1–28 günleriyle sınırlıdır. Özel kaydedilmiş raporlar henüz zamanlanmaz.

Önizleme aynı veri oluşturucusunu kullanır; CSV düğmesi gösterilen anlık özetin tamamını indirir. Formül başlangıçları metin olarak korunur. Önizleme tarayıcıda hazırlanmış bir örnek veri değildir.

Otomatik gönderim yalnız kendi güncel doğrulanmış e-posta adresine yapılır. Arka plan çalışanı her gönderimde güncel şirket, üyelik, rol, paket ve konum/birim kapsamını tekrar uygular. Başarısız teslimat en çok üç kez denenir, denemeler arasında bir saat beklenir. İşletim sistemi güncellemesi sonrası saat dilimi kullanılamazsa takvim `InvalidSchedule` koduyla durdurulur; kullanıcı düzenleyip yeniden etkinleştirebilir. SMTP teslimatı kesin tek-sefer garantisi sunmaz: süreç SMTP teslimi ile durum kaydı arasında kapanırsa tekrar teslim olabilir.

Derleme başarısı gerçek posta sağlayıcısında teslimat veya tüm arayüz işlemlerinin kabulü anlamına gelmez. Gerçek SMTP ve oturumlu takvim kabulü henüz açık.
