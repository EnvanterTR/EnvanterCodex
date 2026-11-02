# Sonraki adım

- F0 WP-001 ölçüm baz çizgisi tamamlandı; Release build, 244/244 PostgreSQL/security, tam hosted Chromium, 83/83 source verifier ve güncel UI envanteri kanıtlandı.
- F1: Coolify son başarılı SHA 4789b509c0b719c1bf71a0b8032f7224bae9fd43. Canlı 122 migration ile ready; committe çalışma ağacında olmayan 5 migration, çalışma ağacında ise committe olmayan 4 migration var.
- Kaynak geçmişi de ayrışıyor: origin/main ile feature branch 36c5937 ortak atasından sonra 74/13 commit ayrılmış; çalışma ağacı kirli ve checkout/ödeme/fatura değişiklikleri içeriyor. Bu nedenle merge/deploy/migration yapılmadı. Tam karşılaştırma summary.json içinde.
- Güvenli sonraki işlem: hedef release branch/SHA'yı seçip ayrı temiz worktree'de entegre et; mevcut iş ağacını koru; testleri çalıştır; sonra ayrı bir canlı DB kopyasında backup→migration→data comparison→restore/rollback provası. Canlıya deploy/migration owner yapar.
- Ödeme kapısı kilitli: şirket kuruluşu ve iyzico işyeri hesabı yok.
