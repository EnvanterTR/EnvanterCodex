# Sonraki adım

- F0 WP-001 ölçüm baz çizgisi tamamlandı.
- F1 WP-002 dağıtım kaynağı denetimi tamamlandı: Coolify son başarılı SHA 4789b509c0b719c1bf71a0b8032f7224bae9fd43 (2026-11-01 08:42 UTC); kaynak 122 migration ve canlı ready/122/0 ile eşleşiyor.
- Mevcut çalışma ağacı bu SHA ile migration seti bakımından farklı: canlıda 5 eski migration mevcut değil; çalışma ağacında 4 farklı 2026-11-01 migration mevcut. Migration sayısı tek başına uyumluluk kanıtı değildir. İki listedeki tam ID'ler summary.json içindedir.
- F1 sonraki iş: istenen aday branch/SHA'yı canlı çizgisiyle uzlaştır, sonra yalnız canlı kopya DB üzerinde yedek→migration→veri karşılaştırma→restore→rollback provası. Canlıya deploy etme veya canlı DB'ye migration çalıştırma.
- Baz testler: Release build 0 uyarı/0 hata; PostgreSQL/security 244/244; hosted Chromium tam paket exit 0, 53 sidebar hedefi; source verifier 83/83; UI inventory güncel: 2.240 kontrol ve 66 rota.
- Üretim DB/deploy değiştirilmedi. Çalışma ağacındaki başka değişiklikleri koru.
