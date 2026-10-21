# Ticari paket kararı — 21 Ekim 2026

Kaynak: kullanıcının bu sohbette verdiği yeni fiyat kararı. Eski sınırsız Cloud kullanıcı kuralının yerini paket bazlı hesap kotaları alır. Ana Web V1 hedefi korunur.

Cloud:15 gün deneme, ardından12 aylık tek ödeme. Aylık değer yalnız yıllık toplam/12 karşılaştırmasıdır; aylık tahsilat veya taksit vaadi değildir. Kart alınmadan deneme başlatma, yıllık ödeme ve otomatik yenileme ayrı akışlardır; otomatik tahsilat şu anda doğrulanmış/etkin kabul edilmez.

| Grup | Kullanıcı | Aylık karşılık | Yıllık ödeme |
|---|---:|---:|---:|
| Bireysel |1|200 TL|2.400 TL|
| İşletme |4|350 TL|4.200 TL|
| İşletme |10|500 TL|6.000 TL|
| Kurumsal |50|750 TL|9.000 TL|
| Kurumsal |100|1.000 TL|12.000 TL|
| Kurumsal |250|1.200 TL|14.400 TL|
| Kurumsal |1.000+ (kota anlamı bekliyor)|2.000 TL|24.000 TL|
| Kurumsal |2.000+ (kota anlamı bekliyor)|3.000 TL|36.000 TL|

Windows masaüstü:1 kullanıcı,10.000 TL/yıl. Windows Server ve Linux aynı kullanıcı/fiyat skalasını izler; Web V1 önceliği değişmez ve henüz satın alınabilir ürün gibi gösterilmez.

Yayın öncesi bekleyen kullanıcı kararı: KDV dahil/hariç; son iki büyük paketin kesin üst sınırı. Varlık, depolama, destek SLA veya kurulum hizmeti kotası verilmedi; bunlar uydurulmaz.

Ticari öneri: Kurumsal tek kart altında kullanıcı kademesi seçici; 1.000/2.000 üzeri talepler için sözleşmeli teklif. Paketler aktif giriş hesabını saymalı; envantere kayıtlı çalışan ile giriş hesabı ayrılmalı. Büyük paketlerde kişi başına fiyat ciddi düşer:250 kullanıcı paketinde4,80 TL/ay;2.000 kullanıcı varsayımında1,50 TL/ay. Destek ve kaynak maliyeti kapasiteyle doğrulanmadan sınırsız kaynak/SLA sözü verilmez.

Gerekli uygulama: plan CRUD/API ve kartlarda Cloud sınırsız kullanıcı zorlamasını kaldır; davet/yeniden etkinleştirme/plan küçültme kontrollerini kota ile eşle; yıllık-only checkout,15 günlük gerçek tenant denemesi ve ödeme sonrası aynı şirketin dönüşümü; süre bitimi ve yinelenen webhook güvenliği; sağlayıcı/SMTP kabulü. Mevcut veri veya erişim otomatik daraltılmaz. Bu dosya yayın veya tamamlanma kanıtı değildir.
