# Coolify SHA ve kaydetme uyarısı — 23 Ekim 2026

## Sonuç

- GitHub `origin/main`: `4d55270827be312775ebffe60c5a6d74011d7802` (`Merge pull request #24 ... home-product-gallery-20261021`).
- Coolify Git Source alanları: repository `EnvanterTR/EnvanterCodex`, branch `main`, commit SHA `4d55270827be312775ebffe60c5a6d74011d7802`.
- Coolify'de **Save changes** tıklanınca `Success Application source updated!` bildirimi geldi. Sayfa yenilenince de repo, branch ve SHA aynı kaldı.
- Buna rağmen `You have changes that haven't been saved yet.` bandı yenileme sonrasında görünmeye devam etti. **Reset** tıklaması da bandı kaldırmadı. Bu gözlem, kaydetme başarısızlığını göstermiyor; Coolify arayüzünün uyarı durumu başarı bildirimi ve kalıcı kaynak değerleriyle çelişiyor. Kök neden kanıtlanmadı.
- Coolify dağıtım geçmişindeki en yeni başarılı kayıt `4d55270`, PR #24 merge commit'i; zaman `2026-10-21 17:23:57 UTC`, süre `52 s`. Aynı SHA'nın dağıtım geçmişinde zaten başarılı ve canlı olduğu doğrulandı; bu nedenle yinelenen/no-op bir dağıtım başlatılmadı.
- Önceki canlı görsel kabulü `tests/reports/coolify-main-sha-verification-20261023.png` içindedir: ana sayfa 4 görselli otomatik etiket galerisi ve duraklatma kontrolünü, portal önizlemesi de tam ekran açılışını gösterdi.

## Veri koruma kapısı

- Canlı DB'nin PostgreSQL custom-format yedeği: `/var/backups/envantertr/production-pre-v1-gate-20261023T134348Z.dump`, SHA-256 `0497224f980d22f4b25ca72943a744ebc58c0b20289f3dce7638cd8b19c22e15`, 443,719 bayt, izin `0600`.
- Şirket medyası + veri koruma anahtarları: `/var/backups/envantertr/production-media-keys-20261023T134348Z.tar.gz`, SHA-256 `28b487401c3bd5c8a4cf17e30b43bf18e706c0b3041e244b6fabe681830f5b6b`, 220,546 bayt, izin `0600`.
- DB arşivi `pg_restore --list` ile okunabilir bulundu ve canlı DB ile aynı PostgreSQL imaj özeti kullanılarak `--network none` + tmpfs üstünde ayrı bir geçici PostgreSQL'e `pg_restore --exit-on-error` ile geri yüklendi. Restore'da `__EFMigrationsHistory` sayısı 118.
- Salt-okunur tablo sayımı canlı ve restore kopyasında aynı: 95 public tablo, toplam 168 satır, tablo/satır özeti MD5 `49d77bc6ef149bb3da96fa29d65dcfa6`.
- Geçici restore konteyneri durduruldu; ardından `docker ps -a --filter name=envtr-p0b-restore-20261023` boş sonuç verdi.
- Bu kopyalar aynı sunucudadır. Coolify'de planlanmış yedek ve S3 hedefi bulunmadığına ilişkin önceki kontrol geçerlidir; bu rapor off-site yedek olduğunu iddia etmez.

## Canlıya alma kararı

Görsel PR #24'ün doğru SHA'sı zaten başarılı dağıtım ve canlı sayfa kanıtıyla doğrulandığından yeni deploy gerekmedi. Bu turda deploy tetiklenmedi. Güncel hedefteki staging + yedek + geri dönüş kapıları tüm sonraki canlı değişiklikler için geçerlidir.
