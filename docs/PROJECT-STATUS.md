# EnvanterTR — geçerli durum

Kaynak: depodaki `AGENTS.md` (CODEX-HEDEF-4 v4); güncelleme 2026-11-03. Anlık kanıta dayanır; yüzde/tahmin kullanılmaz. Masaüstünde anılan `CODEX-HEDEF-4.md` bu konumda yoktu; etkin v4 metni depo `AGENTS.md` dosyasından alındı.

| Hedef | Kapsam | Durum | Kanıt / kalan kabul |
|---|---|---|---|
| F0 | Ölçüm baz çizgisi | MEASURED | WP-028'de doğrulanan güncel HEAD `7446037f249487581f3acbe655b05c95ca92b29f`: Release build 0 uyarı/0 hata; izole PostgreSQL güvenlik/tenant kabulü 297/297 PASS; tam hosted Chromium runner exit 0; ana sayfa galeri testleri PASS; UI envanteri 2.252 kontrol/65 rota; kaynak suite 84/84 PASS. PR #34'ün aynı SHA'sındaki resmi GitHub Actions zorunlu 5/5 işi başarıyla geçti. Canlı yayın yapılmadı. |
| F1 | Güvenli migration / yedek / geri dönüş | IN_PROGRESS | WP-007–WP-009 yedek/hash/restore/no-op/rollback provası PASS. WP-019 sonrası dal gerçek restored yedek klonuyla 122/122 migration ID eşit; pending model change yok; klonda update no-op. WP-028 restore klonunu yeniden okuyarak 96 public tablo, 122 migration, 3 şirket, 1 varlık doğruladı; klon ağsız, portsuz ve mountsuz. PR #34 baş SHA'sındaki resmi CI 5/5 PASS. Açık: PR taslak ve birleştirilmedi; Coolify canlı rollout'u ve yayın sonrası health/tenant smoke sahibi tarafından yapılmadı. Full-history EF idempotent SQL sözdizimi bulgusu gelecekteki migration yolu için ayrıca çözülmeli; uygulanmış migration değiştirilmedi. Canlı DB/deploy değişmedi. |
| F2 | Plan, kayıt, ödemesiz aktivasyon | ÇALIŞTIRILMADI | Canlı uçtan uca yeni firma kaydı ve manuel aktivasyon kabulü yok. |
| F3 | SMTP ve davet / parola e-postaları | KISMEN | WP-020 imzalı zimmet PDF indirme ve kullanıcı onaylı SMTP API'si; WP-021 çalışan arşiv ekranı. Güncel tam hosted Chromium runner exit 0; ayrı yanıt-kaybı/aynı-idempotency-key tekrar testi WP-022'de BLOCKED. Gerçek alıcı teslimi ve davet/parola e-postalarının canlı kabuli yok. SMTP-capture backend testi 5/5 PASS; gerçek SMTP `Accepted` teslim kanıtı değildir. |
| F4 | Kiracı sızıntı testi, rota envanteri, CI | KISMEN | PostgreSQL security/tenant 297/297 PASS; hosted Chromium exit 0; UI inventory 2.252/65; kaynak doğrulayıcı 84/84. PR #34 baş SHA'sında GitHub Actions'ın platform MFA, Linux tenant security, Linux PostgreSQL tenant, homepage gallery ve Windows deployment/backup safety işleri 5/5 PASS. Tenant endpoint envanteri ve hedefin tam kategori/kabul eşlemesi tamamlanmadı; F4 açık. |
| F5 | A–G özellikleri ve UI matrisi | KISMEN | Güncel dal tam hosted Chromium runner exit 0; public-site görsel galerisi, otomatik sola geçiş, tam ekran ve mobil/klavye davranışları PASS. UI matrisi kategori başına hedef eşlemesi ve %95 PASS koşulu henüz ölçülmedi; bu yüzden tam özellik eşdeğerliği kanıtlanmış sayılmaz. |
| F6 | Teknik güvenlik / KVKK listesi | ÇALIŞTIRILMADI | Güncel tam kontrol listesi ve hukuki onay bu oturumda yok. |
| F7 | Canlı gerçek firma kabulü | ÇALIŞTIRILMADI | Farma/Santa/Demo3 canlı müşteri kabul adımları bu oturumda yürütülmedi. |
| F8–F13 | V1 sonrası kurulum, ölçek ve operasyon işleri | ÇALIŞTIRILMADI | Bu fazlar henüz başlamadı. |
| F14 | Iyzico gerçek ödeme | KİLİTLİ | Iyzico işyeri hesabı ve şirket kuruluşu yok; hedef gereği kapalı. |
| F15 | Sonraki faz | ÇALIŞTIRILMADI | Önceki kapılar tamamlanmadı. |
| A01 | Varlık: oluştur/oku/güncelle/sil, klonla, toplu güncelle, toplu sil, arşivle, iade, zimmet | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| A02 | Model: marka + model + kategori + üretici + model no + ömür sonu (EOL) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| A03 | Durum etiketi: Dağıtılabilir / Dağıtımda / Arşivlendi / Arızalı (renk kodlu) | KISMEN | Hosted Chromium: durum etiketi düzenleme/renk, geçiş matrisi, arşiv sınırı ve yenilenen seçiciler PASS. |
| A04 | Kategori, üretici, tedarikçi, departman, lokasyon, grup | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| A05 | Değer, satın alma, garanti, sigorta, amortisman (%) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| A06 | Dinamik özel alanlar: metin, açılır liste, onay kutusu, tarih | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| A07 | Varlık fotoğrafı, dosya eki | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| A08 | Varlık geçmişi (zaman çizelgesi) | KISMEN | Hosted Chromium: varlık geçmişi not kaydı ve timeline yenilemesi PASS. |
| B01 | Giriş/çıkış (checkin/checkout): kullanıcıya, lokasyona, varlığa | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| B02 | Zimmet zaman çizelgesi; kimde ne kadar kaldı | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| B03 | Toplu zimmet / toplu iade | KISMEN | Hosted Chromium: çoklu atama ve iade akışları PASS; kapsamın tamamı tekil doğrulanmadı. |
| B04 | Zimmet formu: PDF + dijital imza + e-posta + QR | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| B05 | Bekleyen talepler; rezervasyon (gelecek tarihli) | KISMEN | Hosted Chromium: gelecek tarihli rezervasyon oluşturma/onay/ret/iptal PASS. |
| C01 | Ad, koltuk, kalan, bitiş, anahtar | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| C02 | Koltuk atama / geri alma | KISMEN | Hosted Chromium: lisans tekli/toplu atama, iptal ve EULA akışları PASS. |
| C03 | Lisans dosyası yükleme | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| C04 | Süre yaklaşan uyarılar (30/15/7/3/1 gün) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| C05 | Maliyet takibi | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| D01 | Aksesuar (klavye, mouse, kablo): zimmetlenir | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| D02 | Sarf (toner, pil, kağıt): tükenir, minimum stok | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| D03 | Bileşen (RAM, SSD): varlığa takılır | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| D04 | Kit: laptop + çanta + mouse = hazır set | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| D05 | Hepsi: ekle/düzenle/sil + zimmet + stok + minimum stok uyarısı | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| E01 | Arıza, bakım, onarım: tip + başlangıç + bitiş + maliyet | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| E02 | Fiziksel sayım (audit); son/gelecek denetim | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| E03 | Bakım geçmişi, dosya eki | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| F01 | Ekle/düzenle/sil, profil fotoğrafı, departman, lokasyon, yönetici | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| F02 | Toplu içe aktarma (CSV) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| F03 | Devre dışı bırakınca otomatik iade | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| F04 | Roller: Süper Admin, Şirket Yöneticisi, Editör, Okuyucu, Sınırlı, Muhasebe | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| G01 | Dosya yükleme (varlık, kullanıcı, lisans) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| G02 | Etkinlik günlüğü (audit) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| G03 | Raporlar: varlık, lisans, aksesuar, bakım, denetim, maliyet, zimmet | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| G04 | İçe/dışa aktarma (Excel/CSV) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| G05 | LDAP / AD / SAML / SSO / Google OAuth (2FA ayrıca H10'da) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| G06 | REST API + API anahtarı | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| G07 | Ayarlar: logo, para birimi, tarih, barkod | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| G08 | Bildirimler (e-posta) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| G09 | Yedekleme (SQL + Excel/CSV/JSON) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| H01 | Gerçek çok kiracılık + mimari düzeyde kiracılar arası sızıntı engeli | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| H02 | Sınırsız şirket + paket bazlı kullanıcı (0–200 / 200–500 / 500+) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| H03 | Whitelabel (logo, renk, favicon) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| H04 | Modern tek sayfa deneyimi, sağ tık menüsü, Ctrl+K komut paleti, kısayollar, hover kart | KISMEN | Hosted Chromium: Ctrl+K ve tüm 53 yan menü hedefi PASS; hover/sağ tık/kısayol kapsamı tam değil. |
| H05 | Barkod/QR/kamera (kamera ile okuma, USB okuyucu, etiket tasarımı) | KISMEN | Hosted Chromium: gerçek QR/Code128 video kare çözümü ve kamera hata durumları PASS. |
| H06 | Dijital imza + anında PDF rapor | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| H07 | Tek tıkla tam sistem yedeği (SQL, Excel, CSV, JSON) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| H08 | Iyzico abonelik + webhook + anında şirket kurulumu (KİLİTLİ, F14) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| H09 | Canlı dashboard grafikleri | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| H10 | 2FA (TOTP), KVKK uyumu için teknik kontrol listesi, otomatik yedekleme | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| H11 | Optimistik arayüz; saten parlak modern tasarım; koyu/açık tema | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| H12 | 5 dil: Türkçe (ana dil), İngilizce, Makedonca, Rusça, Ukraynaca (teknik terimler İngilizce kalabilir) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| I01 | 2FA zorunlu (Google/Microsoft Authenticator); IP beyaz liste (opsiyonel); ayrıntılı oturum kayıtları (IP, cihaz, tarayıcı, konum); 5 hatalı denemede 5 dakika kilit; kritik işlemlerde MFA yeniden doğrulama; oturum 8 saat (beni hatırla 30 gün) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| I02 | toplam şirket (aktif+pasif+demo), aylık gelir (MRR ₺), yeni kayıtlar, 7 gün içinde bitecek abonelikler, başarısız ödemeler (acil), aktif kullanıcı sayısı, sistem sağlığı (CPU/RAM/Disk/DB), bugünkü girişler | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| I03 | 12 aylık gelir trendi (çizgi), şirket büyümesi (çubuk), plan dağılımı (pasta), en çok kullanılan modül (bar), coğrafi dağılım (harita) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| I04 | son 10 kayıt, son 10 ödeme, yaklaşan yenilemeler | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| I05 | ödemesi 3+ gün gecikmiş (kırmızı), lisansı 7 gün içinde bitecek (turuncu), bugün kayıt olup işlem yapmayan (mavi), kapasite limitine yaklaşan (sarı) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| I06 | kolonlar (logo, firma, subdomain, plan, kullanıcı sayısı, durum, kayıt tarihi, aylık ücret, bitiş tarihi); arama, filtre (plan/durum/tarih), sıralama, sayfalama (50/100/250), toplu işlem (askıya al/sil/duyuru/plan değiştir), CSV/Excel dışa aktarma | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| I07 | (1) Genel: künye, subdomain, logo, plan, yetkili, vergi, MRR, LTV; (2) Kullanıcılar: liste, şifre sıfırla, rol değiştir, devre dışı, sil; (3) Abonelik: plan, aylık ücret, sonraki ödeme, geçmiş (PDF), manuel fatura, uzat (1/3/12 ay bedava), plan değiştir, askıya al, aktifleştir, iptal, iade; (4) Kullanım: varlık, kullanıcı, lisans, disk, API, kapasite (%); (5) İletişim: mesaj, bildirim, geçmiş; (6) Log: giriş/çıkış, API, kritik işlem; (7) Tehlikeli: sil (30 gün geri alınabilir), sıfırla, tüm verileri indir | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| I08 | "Panele Gir" düğmesi; parolasız giriş; sarı banner ("ABC Ltd. paneline taklit modunda giriş – Çıkış"); Süper Admin adına log; çıkışta süper admin paneline dönüş; müşteriye bildirim gitmez | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| I09 | 4 düzenlenebilir plan, fiyat geçmişi, karşılaştırma önizlemesi, kuponlar (yüzde/sabit/ilk ay bedava), ekstra kaynak (disk, kullanıcı, premium modül) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| I10 | aktif abonelikler; Iyzico ödeme takipleri; başarısız ödeme için 1/3/7. gün yeniden deneme; fatura üretimi; KDV raporu; demo hesap yönetimi (14→30 gün uzatma, sıfırlama) (ödemeye bağlı kısımlar KİLİTLİ) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| I11 | toplu/hedefli mesaj (e-posta/uygulama/SMS), çok dilli şablonlar (hoş geldin, ödeme, bakım), üst banner duyuruları | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| I12 | tüm şirketlerdeki kullanıcılar, parola sıfırlama zorunluluğu, oturum sonlandırma, ücretsiz özel kullanıcı rozetleri | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| I13 | gerçek zamanlı sağlık (CPU/RAM/Disk/Ağ); servis durumları (uygulama, Postgres, proxy, Cloudflare, Iyzico, Google); günlük/haftalık otomatik yedek; 30 günlük yedek geçmişi; yedek indir/geri yükle; ayrıntılı sistem logları | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| I14 | gelir (12 aylık, net, KDV); müşteri (churn oranı, ARPU); sistem kullanım yoğunluğu | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| I15 | genel site; Iyzico API (sandbox/canlı); SMTP e-posta; güvenlik (MFA, kaba kuvvet koruması); entegrasyonlar (Google, Cloudflare, Slack, Webhook, SMS) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| I16 | sol kenar çubuğu `#0A0A0F` koyu lacivert; Inter yazı tipi; saten parlak kartlar; 300 ms cubic-bezier geçişler; menü: Dashboard, Şirketler, Kullanıcılar, Abonelikler, Faturalar, Planlar, Kuponlar, Demo, Duyurular, Loglar, Raporlar, Sistem, Yedekleme, Ayarlar, Çıkış; üst çubuk: arama (Ctrl+K), hızlı işlem, bildirim, profil, tema; kısayollar: Ctrl+K, G+D/S/U, N+C/U, Esc | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| J01 | Dashboard: varlık/zimmet/lisans/bakım özeti | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| J02 | Varlık oluşturma/düzenleme/silme, transfer | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| J03 | Lisans, aksesuar, sarf yönetimi | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| J04 | Bakım ve sayım modülleri | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| J05 | Paket limitleri içinde kullanıcı/rol yönetimi | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| J06 | Departman ve lokasyon tanımları | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| J07 | Şirket içi özel raporlar | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| J08 | Excel/CSV dışa aktarma | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| J09 | Whitelabel ayarları | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| J10 | Veri yedeği indirme | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| J11 | firma yöneticileri başka şirketin verisini, süper admin panelini ve sistem geneli fiyat/plan yönetim ekranlarını ASLA göremez | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| K01 | Hero: ürün tanıtımı + 2 dakikalık video | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| K02 | 9+ modül özellik kartı | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| K03 | Fiyatlandırma tablosu: 4 plan karşılaştırması (ödeme açılana kadar "Demo iste/Ön kayıt", çalışmayan "Satın al" düğmesi YOK) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| K04 | Çözüm segmentleri: KOBİ / orta / kurumsal | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| K05 | Kaynaklar: dokümantasyon, video, SSS | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| K06 | İletişim: `admin@envantertr.com` + WhatsApp (wa.me) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| K07 | 5 dil, koyu/açık tema, tam mobil uyum | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| K08 | Formlarda aydınlatma metni linki; pazarlama e-postası için önceden onay | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| L01 | Kayıt formu: firma, subdomain, e-posta, parola, yetkili | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| L02 | Subdomain canlı kontrol | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| L03 | E-posta doğrulama | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| L04 | Plan seçimi | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| L05 | Iyzico güvenli ödeme (KİLİTLİ) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| L06 | Webhook → otomatik şirket kurulumu (KİLİTLİ) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| L07 | Karşılama + 5 adımlı onboarding: (1) firma bilgileri, (2) logo + renk, (3) ilk toplu veri aktarımı, (4) kullanıcı davetleri, (5) ilk varlık kaydı | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| L08 | Ödeme açılana kadar: platform yöneticisi manuel aktivasyon (ücretsiz pilot; para tahsilatı yok) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| M01 | Her kiracı tablosunda zorunlu kiracı anahtarı | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| M02 | Kiracılar arası veri sızıntısı İMKANSIZ: otomatik testle kanıtlanır | KISMEN | İzole PostgreSQL/security kabul: 244/244 PASS; bu, tüm endpoint ve her hostile-ID sınıfı için canlı kabul değildir. |
| M03 | Ara katman alt alan adından kiracıyı çözer | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| M04 | Tüm sorgulara kiracı filtresi otomatik uygulanır | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| M05 | Oturum + kiracı bağlamı birlikte doğrulanır | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| M06 | Önbellek/kuyruk/dosya depolama anahtarları kiracı önekli | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| M07 | Sızıntı testleri her yayında otomatik koşar | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| M08 | Mevcut altyapıyı koru: sadece EKLE/DÜZELT; silmek için gerekçe + veri taşıma planı + risk analizi + sahibin onayı | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| M09 | Wildcard yönlendirme: `^[a-z0-9-]+\.envantertr\.com$` (mevcut Coolify/proxy kurulumuyla uyumlu) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| N01 | Bulut SaaS (V1 kapsamı, öncelik) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| N02 | Yerel Windows EXE | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| N03 | Windows Server kurulumu | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| N04 | Linux kurulumu | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| N05 | Mobil iOS/Android | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| O01 | Her API'de yetki + kiracı kontrolü | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| O02 | Oturum + yenileme, CSRF, hız sınırı | KISMEN | İzole PostgreSQL/security ve hosted Chromium oturum/CSRF/MFA akışları PASS; tam güvenlik matrisi henüz eşlenmedi. |
| O03 | Girdi doğrulama/temizleme (XSS, SQL enjeksiyonu) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| O04 | CSP başlığı, zorunlu HTTPS | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| O05 | Güçlü parola özeti (mevcut mekanizma korunur, maliyet ayarı güvenli düzeyde) | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| O06 | Sırlar yalnızca ortam değişkeninde; `.env` depoya girmez | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| O07 | KVKK teknik kontrol listesi ve güncel mevzuat izleme | ÇALIŞTIRILMADI | F0: Bu gereksinim için tekil kabul kanıtı henüz eşleştirilmedi; var kabul edilmedi. |
| P01 | Önceden kanıtlı iddia edilen platform, lisans, CRM, güvenlik, Docker ve login teşhis modüllerinin korunup yeniden doğrulanması | ÇALIŞTIRILMADI | F0: listedeki her modül için ayrı kabul eşlemesi henüz yok. |
| Q01 | Önceden kısmen tamam denilen tüm SaaS, tenant, modül, UI, güvenlik, CI, kurulum ve dokümantasyon kapsamı | KISMEN | Geniş kapsamın parçaları bu oturumda test edildi; 244/244 PostgreSQL/security ve tam hosted Chromium paketi PASS. Ancak Q maddelerinin her biri tekil kanıtla eşleştirilmedi. |
