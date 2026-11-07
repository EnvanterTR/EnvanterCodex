// EnvanterTR örnek veri yükleyici (yalnız test şirketleri için).
// Oturum açılmış şirket portalında tarayıcı konsolunda çalışır; veriyi uygulamanın kendi API'si üzerinden girer
// (her kayıt normal kullanıcı girişi gibi doğrulanır ve işlem geçmişine yazılır). Veritabanına doğrudan yazmaz.
// Kullanım: await window.envanterDemoSeed({ mediaBase: 'https://raw.githubusercontent.com/<repo>/<sha>/tools/demo-media/' })
// Güvenlik: şirkette "SNT-" etiketli varlık varsa hiçbir şey yazmadan durur (iki kez çalıştırmaya karşı).
window.envanterDemoSeed = async function envanterDemoSeed({ mediaBase, log = console.log } = {}) {
  if (!mediaBase) throw new Error('mediaBase gerekli');
  const csrf = await (await fetch('/api/session/csrf', { credentials: 'same-origin' })).json();
  const token = csrf.requestToken;
  const stats = { ok: 0, fail: 0, errors: [] };
  async function call(method, path, body) {
    const headers = { 'X-CSRF-TOKEN': token };
    let payload = body;
    if (body && !(body instanceof FormData)) { headers['Content-Type'] = 'application/json'; payload = JSON.stringify(body); }
    const response = await fetch('/api' + path, { method, credentials: 'same-origin', headers, body: payload });
    const text = await response.text();
    let json = null; try { json = text ? JSON.parse(text) : null; } catch {}
    if (!response.ok) {
      stats.fail++;
      const detail = json ? [json.message, ...Object.values(json.errors || {}).flat()].filter(Boolean).join(' ') : text.slice(0, 160);
      stats.errors.push(`${method} ${path} → ${response.status} ${detail}`);
      log(`HATA ${method} ${path} → ${response.status} ${detail}`);
      return null;
    }
    if (method !== 'GET') stats.ok++;
    return json;
  }
  const get = path => call('GET', path);
  const post = (path, body) => call('POST', path, body);
  async function upload(path, file) {
    try {
      let blob = await (await fetch(mediaBase + file)).blob();
      if (!blob.size) throw new Error('boş görsel');
      if (blob.size > 1800000) {
        const bitmap = await createImageBitmap(blob);
        const scale = Math.min(1, 1600 / Math.max(bitmap.width, bitmap.height));
        const canvas = document.createElement('canvas'); canvas.width = Math.round(bitmap.width * scale); canvas.height = Math.round(bitmap.height * scale);
        const ctx = canvas.getContext('2d'); ctx.fillStyle = '#fff'; ctx.fillRect(0, 0, canvas.width, canvas.height); ctx.drawImage(bitmap, 0, 0, canvas.width, canvas.height);
        blob = await new Promise(resolve => canvas.toBlob(resolve, 'image/jpeg', 0.86)); file = file.replace(/\.[a-z]+$/i, '.jpg');
      }
      const form = new FormData(); form.append('file', blob, file);
      return await post(path, form);
    } catch (error) { stats.fail++; stats.errors.push(`görsel ${file}: ${error.message}`); return null; }
  }
  const day = offset => { const d = new Date(); d.setUTCDate(d.getUTCDate() + offset); return d.toISOString().slice(0, 10); };
  const list = value => Array.isArray(value) ? value : (value?.items || value?.rows || value?.data || []);

  // 0) Çift çalıştırma koruması
  const existing = await get('/assets?page=1&pageSize=5&q=SNT-');
  if (list(existing).some(item => String(item.assetTag || '').startsWith('SNT-'))) {
    log('Bu şirkette zaten SNT- etiketli örnek varlıklar var; hiçbir şey yazılmadı.');
    return { skipped: true };
  }

  // 1) Markalar (logolu), kategoriler, modeller (görselli)
  log('1/12 Markalar, kategoriler, modeller…');
  const brandNames = ['Dell', 'HP', 'Lenovo', 'Cisco', 'Fortinet', 'Ubiquiti', 'Samsung', 'Logitech', 'Yealink', 'Eaton', 'Brother', 'Samsonite'];
  const brandLogo = { Dell: 'brand-dell.png', HP: 'brand-hp.png', Lenovo: 'brand-lenovo.png', Cisco: 'brand-cisco.png', Fortinet: 'brand-fortinet.png', Ubiquiti: 'brand-ubiquiti.png', Samsung: 'brand-samsung.png', Logitech: 'brand-logitech.png', Yealink: 'brand-yealink.png', Eaton: 'brand-eaton.png', Brother: 'brand-brother.png', Samsonite: 'brand-samsonite.png' };
  const brands = {};
  const knownBrands = list(await get('/lookups/brands'));
  for (const name of brandNames) {
    const found = knownBrands.find(x => x.name?.toLowerCase() === name.toLowerCase());
    const created = found || await post('/lookups/brands', { name, website: `https://www.${name.toLowerCase()}.com` });
    if (created?.id) { brands[name] = created.id; if (!found) await upload(`/brands/${created.id}/logo`, brandLogo[name]); }
  }
  const categoryDefs = [
    ['Dizüstü Bilgisayar', 36], ['Masaüstü Bilgisayar', 48], ['Monitör', 60], ['Sunucu', 60], ['Ağ Anahtarı (Switch)', 60],
    ['Güvenlik Duvarı', 60], ['Kablosuz Erişim Noktası', 48], ['Yazıcı', 48], ['IP Telefon', 48], ['Kesintisiz Güç Kaynağı (UPS)', 60]
  ];
  const categories = {};
  const knownCategories = list(await get('/lookups/categories'));
  for (const [name, months] of categoryDefs) {
    const found = knownCategories.find(x => x.name?.toLowerCase() === name.toLowerCase());
    const created = found || await post('/lookups/categories', { name, depreciationMonths: months });
    if (created?.id) categories[name] = created.id;
  }
  const modelDefs = [
    { key: 'latitude', name: 'Latitude 5440', brand: 'Dell', category: 'Dizüstü Bilgisayar', number: 'LAT-5440', photo: 'catalog-latitude.jpg', cost: 38500 },
    { key: 'elitebook', name: 'EliteBook 840 G10', brand: 'HP', category: 'Dizüstü Bilgisayar', number: '840G10', photo: 'catalog-elitebook.png', cost: 41200 },
    { key: 'thinkpad', name: 'ThinkPad T14 Gen 4', brand: 'Lenovo', category: 'Dizüstü Bilgisayar', number: '21HD', photo: 'catalog-laptop.png', cost: 39900 },
    { key: 'optiplex', name: 'OptiPlex 7020 SFF', brand: 'Dell', category: 'Masaüstü Bilgisayar', number: 'OPT-7020', photo: 'catalog-desktop.jpg', cost: 27800 },
    { key: 'prodesk', name: 'ProDesk 400 G9', brand: 'HP', category: 'Masaüstü Bilgisayar', number: '400G9', photo: 'catalog-prodesk.png', cost: 25400 },
    { key: 'thinkcentre', name: 'ThinkCentre M90q Gen 4', brand: 'Lenovo', category: 'Masaüstü Bilgisayar', number: 'M90Q4', photo: 'catalog-thinkcentre.png', cost: 29900 },
    { key: 'hpmonitor', name: 'E24 G5 24" Monitör', brand: 'HP', category: 'Monitör', number: 'E24G5', photo: 'catalog-hp-monitor.jpg', cost: 7400 },
    { key: 'samsungmon', name: 'ViewFinity S6 27"', brand: 'Samsung', category: 'Monitör', number: 'LS27C', photo: 'catalog-monitor.png', cost: 9800 },
    { key: 'r760', name: 'PowerEdge R760', brand: 'Dell', category: 'Sunucu', number: 'R760', photo: 'catalog-poweredge-r760.jpg', cost: 412000 },
    { key: 'thinksystem', name: 'ThinkSystem SR650 V3', brand: 'Lenovo', category: 'Sunucu', number: 'SR650V3', photo: 'catalog-thinksystem.png', cost: 386000 },
    { key: 'switch', name: 'Catalyst 9200L 48P', brand: 'Cisco', category: 'Ağ Anahtarı (Switch)', number: 'C9200L-48P', photo: 'catalog-switch.jpg', cost: 98000 },
    { key: 'firewall', name: 'FortiGate 100F', brand: 'Fortinet', category: 'Güvenlik Duvarı', number: 'FG-100F', photo: 'catalog-firewall.jpg', cost: 156000 },
    { key: 'ap', name: 'U6 Pro', brand: 'Ubiquiti', category: 'Kablosuz Erişim Noktası', number: 'U6-PRO', photo: 'catalog-ap.webp', cost: 8900 },
    { key: 'printer', name: 'MFC-L8900CDW', brand: 'Brother', category: 'Yazıcı', number: 'MFCL8900', photo: 'catalog-multifunction-printer.webp', cost: 46500 },
    { key: 'phone', name: 'T54W IP Telefon', brand: 'Yealink', category: 'IP Telefon', number: 'SIP-T54W', photo: 'catalog-phone-t54w.jpg', cost: 6900 },
    { key: 'ups', name: '9PX 3000VA', brand: 'Eaton', category: 'Kesintisiz Güç Kaynağı (UPS)', number: '9PX3000', photo: 'catalog-ups.jpg', cost: 74000 }
  ];
  const models = {};
  for (const m of modelDefs) {
    const created = await post('/lookups/models', { name: m.name, brandId: brands[m.brand], categoryId: categories[m.category], modelNumber: m.number });
    if (created?.id) { models[m.key] = { ...m, id: created.id }; await upload(`/models/${created.id}/photo`, m.photo); }
  }

  // 2) Binalar → katlar → odalar
  log('2/12 Binalar, katlar, odalar…');
  const loc = {};
  const locationTree = [
    ['building', 'Merkez Bina', null], ['floor1', 'Kat 1', 'building'], ['floor2', 'Kat 2', 'building'], ['floor3', 'Kat 3', 'building'],
    ['server', 'Sunucu Odası', 'floor1'], ['rack1', 'Kabin A1', 'server'], ['rack2', 'Kabin A2', 'server'], ['itoffice', 'Bilgi İşlem Ofisi', 'floor1'],
    ['store', 'Bilgi İşlem Deposu', 'floor1'], ['office2', 'Açık Ofis 2', 'floor2'], ['meeting', 'Toplantı Odası', 'floor2'],
    ['office3', 'Açık Ofis 3', 'floor3'], ['archive', 'Finans Arşivi', 'floor3'], ['branch', 'Ankara Şubesi', null], ['branchOffice', 'Ankara Ofis', 'branch']
  ];
  for (const [key, name, parent] of locationTree) {
    const created = await post('/locations/catalog', { name, parentLocationId: parent ? loc[parent]?.id : null });
    if (created?.id) loc[key] = { id: created.id, name };
  }
  const locPath = key => ({ building: 'Merkez Bina', floor1: 'Merkez Bina / Kat 1', floor2: 'Merkez Bina / Kat 2', floor3: 'Merkez Bina / Kat 3', server: 'Merkez Bina / Kat 1 / Sunucu Odası', rack1: 'Merkez Bina / Kat 1 / Sunucu Odası / Kabin A1', rack2: 'Merkez Bina / Kat 1 / Sunucu Odası / Kabin A2', itoffice: 'Merkez Bina / Kat 1 / Bilgi İşlem Ofisi', store: 'Merkez Bina / Kat 1 / Bilgi İşlem Deposu', office2: 'Merkez Bina / Kat 2 / Açık Ofis 2', meeting: 'Merkez Bina / Kat 2 / Toplantı Odası', office3: 'Merkez Bina / Kat 3 / Açık Ofis 3', archive: 'Merkez Bina / Kat 3 / Finans Arşivi', branchOffice: 'Ankara Şubesi / Ankara Ofis' }[key] || 'Merkez Bina');

  // 2b) Şirket birimleri
  const units = {};
  for (const [key, name, code, parent, unitType] of [['hq', 'Santa Teknoloji A.Ş.', 'GM', null, 'legal_entity'], ['ankara', 'Ankara Şubesi', 'ANK', 'hq', 'branch'], ['rnd', 'Ar-Ge Merkezi', 'ARGE', 'hq', 'organizational']]) {
    const created = await post('/company-units', { name, code, parentCompanyUnitId: parent ? units[parent] : null, unitType });
    if (created?.id) units[key] = created.id;
  }

  // 3) Departmanlar
  log('3/12 Departmanlar…');
  const dept = {};
  const deptDefs = [
    ['it', 'Bilgi Teknolojileri', null, 'floor1'], ['it-sys', 'Sistem ve Ağ', 'it', 'floor1'], ['it-dev', 'Yazılım Geliştirme', 'it', 'floor1'],
    ['finance', 'Finans ve Muhasebe', null, 'floor3'], ['hr', 'İnsan Kaynakları', null, 'floor2'], ['ops', 'Operasyon', null, 'floor2'],
    ['sales', 'Satış ve Pazarlama', null, 'floor2'], ['admin', 'İdari İşler ve Satın Alma', null, 'floor3']
  ];
  for (const [key, name, parent, floor] of deptDefs) {
    const created = await post('/departments', { name, parentDepartmentId: parent ? dept[parent]?.id : null, locationCatalogId: loc[floor]?.id });
    if (created?.id) dept[key] = { id: created.id, name, floor };
  }

  // 4) Çalışanlar (profil görselli)
  log('4/12 Çalışanlar…');
  const people = [
    ['Deniz Arslan', 'it', 'Bilgi Teknolojileri Müdürü'], ['Ece Yıldız', 'it-dev', 'Kıdemli Yazılım Uzmanı'], ['Mert Kaya', 'it-dev', 'Uygulama Geliştirici'],
    ['Selin Demir', 'it-sys', 'Sistem Yöneticisi'], ['Kaan Öztürk', 'it-sys', 'Ağ ve Güvenlik Uzmanı'], ['Duru Aydın', 'finance', 'Finans Müdürü'],
    ['Emre Şahin', 'finance', 'Muhasebe Uzmanı'], ['Elif Koç', 'finance', 'Bütçe Analisti'], ['Bora Çelik', 'finance', 'Ön Muhasebe Uzmanı'],
    ['Nehir Aslan', 'finance', 'Finans Asistanı'], ['Zeynep Aksoy', 'hr', 'İnsan Kaynakları Müdürü'], ['Arda Polat', 'hr', 'İşe Alım Uzmanı'],
    ['İrem Yalçın', 'hr', 'Bordro Uzmanı'], ['Can Güneş', 'hr', 'Eğitim Uzmanı'], ['Kerem Eren', 'ops', 'Operasyon Müdürü'],
    ['Sude Kılıç', 'ops', 'Lojistik Uzmanı'], ['Ozan Doğan', 'ops', 'Depo Sorumlusu'], ['Naz Yılmaz', 'ops', 'Saha Koordinatörü'],
    ['Tuna Ateş', 'ops', 'Saha Teknisyeni'], ['Yasemin Taş', 'sales', 'Satış Müdürü'], ['Ali Ekinci', 'sales', 'Kurumsal Satış Uzmanı'],
    ['Mina Çetin', 'sales', 'Müşteri Temsilcisi'], ['Efe Kurt', 'sales', 'Pazarlama Uzmanı'], ['Defne Bulut', 'sales', 'Dijital Pazarlama Uzmanı'],
    ['Barış Yalın', 'admin', 'İdari İşler Müdürü'], ['Aslı Toprak', 'admin', 'Satın Alma Uzmanı'], ['Eylül Korkmaz', 'admin', 'Tedarik Uzmanı'],
    ['Umut Sarı', 'admin', 'Ofis Yöneticisi'], ['Melis Er', 'admin', 'Resepsiyon Sorumlusu'], ['Onur Karaca', 'it-sys', 'Yardım Masası Uzmanı']
  ];
  const employees = [];
  for (let i = 0; i < people.length; i++) {
    const [displayName, deptKey, jobTitle] = people[i];
    const manager = ['it-dev', 'it-sys'].includes(deptKey) ? employees[0] : employees.find(e => e.deptKey === deptKey && e.isManager);
    const d = dept[deptKey];
    const created = await post('/employees', {
      employeeNumber: `SNT-P${String(i + 1).padStart(3, '0')}`, displayName,
      email: `personel${String(i + 1).padStart(2, '0')}@santa.example.test`, department: d?.name, jobTitle,
      phone: `+90 555 010 ${String(i + 1).padStart(4, '0')}`, location: locPath(d?.floor), startedOn: day(-1800 + i * 47),
      managerEmployeeId: manager?.id || null
    });
    if (created?.id) {
      employees.push({ id: created.id, name: displayName, deptKey, isManager: /Müdürü$/.test(jobTitle) });
      await upload(`/employees/${created.id}/photo`, `person-${String(i + 1).padStart(2, '0')}.jpg`);
    }
  }

  // 5) Tedarikçiler (logolu)
  log('5/12 Tedarikçiler…');
  const supplierDefs = [
    ['Bilişim Dünyası Teknoloji A.Ş.', 'Ahmet Usta', 'satis@bilisimdunyasi.example.test', 'supplier-086e8df1.png'],
    ['Anadolu Ağ Çözümleri Ltd.', 'Seda Gür', 'destek@anadoluag.example.test', 'supplier-17cef4c9.png'],
    ['Marmara Ofis Malzemeleri', 'Hakan Er', 'siparis@marmaraofis.example.test', 'supplier-7151d064.png'],
    ['Ege Sunucu ve Depolama', 'Gizem Ak', 'info@egesunucu.example.test', 'supplier-a3c923db.png'],
    ['Yazılım Lisans Merkezi', 'Okan Tek', 'lisans@ylm.example.test', 'supplier-c1bdde03.png'],
    ['Güvenli Enerji Sistemleri', 'Pınar Su', 'servis@guvenlienerji.example.test', 'supplier-e896ba48.png']
  ];
  const suppliers = [];
  for (const [name, contactName, email, logo] of supplierDefs) {
    const created = await post('/suppliers', { name, contactName, email, phone: '+90 212 555 00 00', website: null, notes: 'Örnek tedarikçi' });
    if (created?.id) { suppliers.push({ id: created.id, name }); await upload(`/suppliers/${created.id}/logo`, logo); }
  }
  const sup = i => suppliers[i % Math.max(1, suppliers.length)]?.name || null;

  // 6) Varlıklar (50)
  log('6/12 Varlıklar…');
  const assetPlan = [
    ['latitude', 8, 'itoffice'], ['elitebook', 6, 'office2'], ['thinkpad', 6, 'office3'], ['optiplex', 5, 'office2'], ['prodesk', 3, 'office3'],
    ['thinkcentre', 3, 'archive'], ['hpmonitor', 5, 'office2'], ['samsungmon', 3, 'itoffice'], ['r760', 2, 'rack1'], ['thinksystem', 1, 'rack2'],
    ['switch', 2, 'rack1'], ['firewall', 1, 'rack1'], ['ap', 2, 'office2'], ['printer', 1, 'floor2'], ['phone', 1, 'meeting'], ['ups', 1, 'server']
  ];
  const assets = [];
  let n = 0;
  for (const [modelKey, count, locKey] of assetPlan) {
    const m = models[modelKey]; if (!m) continue;
    for (let k = 0; k < count; k++) {
      n++;
      const tag = `SNT-${String(n).padStart(3, '0')}`;
      const purchased = -900 + n * 13;
      const created = await post('/assets', {
        assetTag: tag, name: `${m.name} · ${String(k + 1).padStart(2, '0')}`, categoryId: categories[m.category], brandId: brands[m.brand], modelId: m.id,
        serialNumber: `SN${m.number.replace(/[^A-Z0-9]/gi, '').slice(0, 6).toUpperCase()}${String(240000 + n * 37)}`, location: locPath(locKey),
        purchaseDate: day(purchased), warrantyExpiresAt: day(purchased + (n % 5 === 0 ? 400 : 1095)), purchaseCost: m.cost, currency: 'TRY',
        isRequestable: ['latitude', 'elitebook', 'thinkpad'].includes(modelKey), defaultLocationCatalogId: loc[locKey]?.id || null,
        notes: n % 7 === 0 ? 'Yıllık bakım sözleşmesi kapsamında.' : null
      });
      if (created?.id) assets.push({ id: created.id, tag, modelKey, locKey });
    }
  }

  // 7) Zimmet, transfer, iade, denetim, bakım
  log('7/12 Zimmet, transfer, iade, denetim, bakım…');
  const laptopsAll = assets.filter(a => ['latitude', 'elitebook', 'thinkpad'].includes(a.modelKey));
  const userDevices = [...laptopsAll.slice(0, 16), ...assets.filter(a => ['hpmonitor', 'samsungmon'].includes(a.modelKey)).slice(0, 6)];
  const assigned = [];
  for (let i = 0; i < Math.min(userDevices.length, employees.length); i++) {
    const result = await post(`/assets/${userDevices[i].id}/assign`, { employeeId: employees[i % employees.length].id, note: 'İşe başlangıç zimmeti', expectedReturnOn: i % 6 === 0 ? day(120) : null });
    if (result !== null) assigned.push(userDevices[i]);
  }
  for (const a of assigned.slice(-2)) await post(`/assets/${a.id}/return`, { note: 'Cihaz yenileme nedeniyle iade alındı.' });
  for (const a of assets.filter(x => x.modelKey === 'optiplex').slice(0, 2)) await post(`/assets/${a.id}/transfer`, { targetLocation: locPath('branchOffice'), notes: 'Ankara şubesine sevk edildi.' });
  for (const a of assets.slice(0, 12)) await post(`/assets/${a.id}/audit`, { auditDate: day(-20), notes: 'Yıllık fiziksel sayım', nextAuditOn: day(345), outcome: 'Accepted' });
  const servers = assets.filter(a => ['r760', 'thinksystem', 'switch', 'firewall', 'ups'].includes(a.modelKey));
  for (const [i, a] of servers.entries()) {
    await post(`/assets/${a.id}/maintenances/schedule`, { maintenanceType: i % 2 ? 'Önleyici bakım' : 'Firmware güncellemesi', supplier: sup(3), description: 'Planlı periyodik bakım', plannedOn: day(10 + i * 9), cost: 4500, currency: 'TRY', notes: null, responsibleEmployeeId: employees[3]?.id || null });
  }
  const busy = new Set(assigned.slice(0, -2).map(a => a.id));
  const repair = [assets.find(a => a.modelKey === 'printer'), assets.find(a => ['thinkcentre', 'prodesk'].includes(a.modelKey) && !busy.has(a.id))].filter(Boolean);
  for (const a of repair) {
    const started = await post(`/assets/${a.id}/maintenances`, { maintenanceType: 'Arıza onarımı', supplier: sup(0), description: a.modelKey === 'printer' ? 'Kağıt sıkışması ve tambur değişimi' : 'Fan gürültüsü, termal macun yenileme', startedOn: day(-6), cost: 2750, currency: 'TRY', notes: null, responsibleEmployeeId: employees[29]?.id || null });
    if (started?.id && a.modelKey === 'printer') await post(`/assets/${a.id}/maintenances/${started.id}/complete`, { completedOn: day(-2), notes: 'Parça değişimi yapıldı, test baskısı alındı.' });
  }

  // 8) Rezervasyonlar
  log('8/12 Rezervasyonlar…');
  const free = laptopsAll.slice(16, 19);
  for (const [i, a] of free.entries()) await post('/reservations', { assetId: a.id, employeeId: employees[10 + i]?.id, startsOn: day(3 + i * 4), endsOn: day(6 + i * 4), requestNote: 'Fuar / müşteri ziyareti için geçici kullanım' });

  // 9) Lisanslar ve sözleşmeler
  log('9/12 Lisanslar ve sözleşmeler…');
  const licenseDefs = [
    ['Microsoft 365 Business Standard', 'Microsoft', '2026', 'Subscription', 40, 540, day(-200), day(165), true],
    ['Windows 11 Pro', 'Microsoft', '23H2', 'Perpetual', 30, 6800, day(-500), null, false],
    ['Adobe Acrobat Pro', 'Adobe', '2026', 'Subscription', 10, 4100, day(-120), day(245), true],
    ['AutoCAD LT', 'Autodesk', '2026', 'Subscription', 3, 21500, day(-90), day(25), false],
    ['Kaspersky Endpoint Security', 'Kaspersky', '12', 'Subscription', 60, 610, day(-300), day(65), true],
    ['Veeam Backup & Replication', 'Veeam', '12', 'Subscription', 4, 38000, day(-60), day(305), true]
  ];
  const licenses = [];
  for (const [name, publisher, version, type, seats, unitCost, purchaseDate, expirationDate, autoRenew] of licenseDefs) {
    const created = await post('/software-licenses', { name, publisher, productVersion: version, licenseType: type, licenseReference: null, supplier: sup(4), seats, unitCost, currency: 'TRY', purchaseDate, expirationDate, autoRenew, notes: null });
    if (created?.id) licenses.push({ id: created.id, name, seats });
  }
  if (licenses[0]) for (const e of employees.slice(0, 18)) await post(`/software-licenses/${licenses[0].id}/assign`, { employeeId: e.id, assetId: null, notes: null, eulaAccepted: false });
  if (licenses[2]) for (const e of employees.filter(x => ['finance', 'admin'].includes(x.deptKey)).slice(0, 6)) await post(`/software-licenses/${licenses[2].id}/assign`, { employeeId: e.id, assetId: null, notes: null, eulaAccepted: false });
  if (licenses[1]) for (const a of assets.filter(x => ['optiplex', 'prodesk', 'thinkcentre'].includes(x.modelKey))) await post(`/software-licenses/${licenses[1].id}/assign`, { employeeId: null, assetId: a.id, notes: null, eulaAccepted: false });
  const contractDefs = [
    ['Sunucu Donanım Bakım Sözleşmesi', 'SZL-2026-001', 'Bakım', 3, day(-180), day(185), 96000, servers.map(a => a.id), []],
    ['Yazıcı Kiralama ve Sarf Sözleşmesi', 'SZL-2026-002', 'Kiralama', 2, day(-60), day(670), 54000, assets.filter(a => a.modelKey === 'printer').map(a => a.id), []],
    ['Microsoft Kurumsal Anlaşma', 'SZL-2026-003', 'Lisans', 4, day(-200), day(165), 259200, [], licenses.slice(0, 2).map(l => l.id)],
    ['Ağ Cihazları Destek Sözleşmesi', 'SZL-2026-004', 'Destek', 1, day(-30), day(28), 42000, assets.filter(a => ['switch', 'firewall', 'ap'].includes(a.modelKey)).map(a => a.id), []]
  ];
  for (const [name, contractNumber, contractType, s, startDate, endDate, cost, assetIds, licenseIds] of contractDefs)
    await post('/contracts', { name, contractNumber, contractType, supplierId: suppliers[s]?.id || null, supplier: null, startDate, endDate, cost, currency: 'TRY', notes: null, assetIds, licenseIds });

  // 10) Aksesuarlar, sarf malzemeleri, bileşenler, kitler
  log('10/12 Aksesuar, sarf, bileşen, kit…');
  const accCats = {};
  for (const name of ['Bağlantı ve Kablo', 'Çevre Birimi', 'Taşıma']) { const c = await post('/accessory-categories', { name }); if (c?.id) accCats[name] = c.id; }
  const accessoryDefs = [
    ['USB-C Dock İstasyonu', 'Bağlantı ve Kablo', 'Dell', 'WD19S', 25, 4, 3900, 'accessory-dock.jpg'],
    ['HDMI Kablo 2 m', 'Bağlantı ve Kablo', null, 'HDMI-2M', 60, 10, 180, 'accessory-hdmi.jpg'],
    ['DisplayPort Kablo 1.8 m', 'Bağlantı ve Kablo', null, 'DP-18', 40, 8, 220, 'accessory-displayport.jpg'],
    ['Cat6 Patch Kablo 3 m', 'Bağlantı ve Kablo', null, 'CAT6-3M', 120, 20, 65, 'accessory-cat6.jpg'],
    ['USB-C Hub 7 Port', 'Bağlantı ve Kablo', null, 'HUB7', 30, 5, 750, 'accessory-hub.jpg'],
    ['Kablosuz Klavye-Fare Seti', 'Çevre Birimi', 'Logitech', 'MK540', 35, 6, 1350, 'accessory-keyboard-set.png'],
    ['Kablosuz Fare', 'Çevre Birimi', 'Logitech', 'M185', 50, 10, 420, 'accessory-mouse.png'],
    ['65 W USB-C Şarj Adaptörü', 'Bağlantı ve Kablo', 'Lenovo', 'ADL65', 20, 4, 980, 'accessory-adapter.png'],
    ['Dizüstü Bilgisayar Çantası 15.6"', 'Taşıma', 'Samsonite', 'GUARDIT', 25, 5, 1650, 'santa-accessory-bag.jpg']
  ];
  const accessories = [];
  for (const [name, cat, brand, model, qty, min, cost, photo] of accessoryDefs) {
    const created = await post('/accessories', { name, category: cat, modelNumber: model, itemNumber: null, supplier: sup(2), location: locPath('store'), minimumQuantity: min, quantity: qty, unitCost: cost, currency: 'TRY', purchaseDate: day(-150), notes: null, isRequestable: true, brandId: brand ? brands[brand] : null, categoryId: accCats[cat] || null });
    if (created?.id) { accessories.push({ id: created.id, name }); await upload(`/accessories/${created.id}/photo`, photo); }
  }
  for (let i = 0; i < 12 && accessories.length; i++) await post(`/accessories/${accessories[i % 3 === 0 ? 0 : i % 3 === 1 ? 5 : 6].id}/checkout`, { targetType: 'Employee', employeeId: employees[i]?.id, assetId: null, location: null, quantity: 1, notes: 'Masaüstü çalışma seti' });
  const consumableDefs = [
    ['Brother TN-910 Siyah Toner', 'Toner', 'TN910BK', 12, 3, 2450], ['Brother TN-910 Renkli Toner Seti', 'Toner', 'TN910C', 6, 2, 6900],
    ['A4 Fotokopi Kağıdı (koli)', 'Kağıt', 'A4-80G', 40, 10, 640], ['AA Alkalin Pil (24\'lü)', 'Pil', 'AA-24', 15, 4, 390],
    ['Ekran Temizleme Seti', 'Temizlik', 'CLN-01', 25, 5, 120], ['Termal Etiket Rulosu 50x25', 'Etiket', 'LBL-5025', 30, 6, 210]
  ];
  const consumables = [];
  for (const [name, category, model, qty, min, cost] of consumableDefs) {
    const created = await post('/consumables', { name, category, modelNumber: model, itemNumber: null, supplier: sup(2), location: locPath('store'), minimumQuantity: min, quantity: qty, unitCost: cost, currency: 'TRY', purchaseDate: day(-40), notes: null, isRequestable: true });
    if (created?.id) consumables.push({ id: created.id, name });
  }
  for (let i = 0; i < 5 && consumables.length; i++) await post(`/consumables/${consumables[i % 2 ? 2 : 0].id}/checkout`, { employeeId: employees[24 + i]?.id, quantity: 1, notes: null });
  const componentDefs = [
    ['16 GB DDR5 SO-DIMM Bellek', 'Bellek', 'DDR5-16S', 20, 4, 1850], ['32 GB DDR5 RDIMM Bellek', 'Bellek', 'DDR5-32R', 12, 2, 5200],
    ['1 TB NVMe SSD', 'Depolama', 'NVME-1T', 15, 3, 2600], ['2.4 TB SAS HDD', 'Depolama', 'SAS-24T', 8, 2, 7400], ['800 W Yedek Güç Kaynağı', 'Güç', 'PSU-800', 4, 1, 6100]
  ];
  const components = [];
  for (const [name, category, model, qty, min, cost] of componentDefs) {
    const created = await post('/components', { name, category, modelNumber: model, itemNumber: null, serialNumber: null, supplier: sup(3), location: locPath('store'), minimumQuantity: min, quantity: qty, unitCost: cost, currency: 'TRY', purchaseDate: day(-75), notes: null, warrantyExpiresAt: day(650) });
    if (created?.id) components.push({ id: created.id, name });
  }
  const laptops = assets.filter(a => a.modelKey === 'latitude').slice(0, 3);
  for (const a of laptops) if (components[0]) await post(`/components/${components[0].id}/allocate`, { assetId: a.id, quantity: 1, notes: 'Bellek yükseltmesi' });
  for (const a of servers.filter(x => ['r760', 'thinksystem'].includes(x.modelKey))) if (components[1]) await post(`/components/${components[1].id}/allocate`, { assetId: a.id, quantity: 2, notes: 'Sanallaştırma kapasite artışı' });
  if (accessories.length >= 7) {
    await post('/accessory-kits', { name: 'Yeni Personel Başlangıç Kiti', description: 'İşe başlayan personele verilen standart set', items: [{ accessoryId: accessories[5].id, quantity: 1 }, { accessoryId: accessories[0].id, quantity: 1 }, { accessoryId: accessories[1].id, quantity: 1 }] });
    await post('/accessory-kits', { name: 'Toplantı Odası Kiti', description: 'Sunum ve video konferans bağlantı seti', items: [{ accessoryId: accessories[1].id, quantity: 2 }, { accessoryId: accessories[4].id, quantity: 1 }] });
  }

  // 11) Siparişler ve stok talepleri
  log('11/12 Siparişler ve talepler…');
  await post('/asset-orders', { orderNumber: 'PO-2026-0101', supplier: sup(0), orderDate: day(-15), expectedOn: day(10), taxRatePercent: 20, discountAmount: 0, freightAmount: 0, currency: 'TRY', notes: 'Yeni personel dizüstü bilgisayarları', items: models.latitude ? [{ name: 'Latitude 5440', categoryId: categories['Dizüstü Bilgisayar'], brandId: brands.Dell, modelId: models.latitude.id, quantity: 4, unitCost: 38500, warrantyExpiresAt: day(1100), notes: null, locationCatalogId: loc.store?.id || null }] : [] });
  if (accessories.length) {
    await post('/accessory-orders', { orderNumber: 'AO-2026-0201', supplier: sup(2), orderDate: day(-8), expectedOn: day(5), currency: 'TRY', taxRatePercent: 20, discountAmount: 0, freightAmount: 150, notes: null, items: [{ accessoryId: accessories[1].id, quantity: 30, unitCost: 175, notes: null }, { accessoryId: accessories[6].id, quantity: 20, unitCost: 400, notes: null }] });
  }
  if (consumables.length) {
    await post('/stock-orders', { orderNumber: 'SO-2026-0301', supplier: sup(2), orderDate: day(-4), expectedOn: day(7), currency: 'TRY', taxRatePercent: 20, discountAmount: 0, freightAmount: 0, notes: 'Aylık sarf siparişi', items: [{ consumableId: consumables[0].id, componentId: null, quantity: 6, unitCost: 2400, notes: null }, { consumableId: consumables[2].id, componentId: null, quantity: 20, unitCost: 630, notes: null }] });
  }
  if (accessories.length && consumables.length) {
    await post('/requests', { employeeId: employees[21]?.id, itemType: 'Accessory', itemId: accessories[0].id, quantity: 1, note: 'Evden çalışma için dock istasyonu' });
    await post('/requests', { employeeId: employees[13]?.id, itemType: 'Consumable', itemId: consumables[3].id, quantity: 1, note: 'Toplantı odası kumandaları için' });
    await post('/requests', { employeeId: employees[16]?.id, itemType: 'Accessory', itemId: accessories[8].id, quantity: 1, note: 'Saha ziyaretleri için çanta' });
  }

  // 12) Görevler
  log('12/12 Görevler…');
  const taskDefs = [
    ['Sunucu odası UPS akü testi', 'Eaton 9PX akü sağlık testini yapın ve raporlayın.', 'High', 3, 5],
    ['FortiGate firmware güncellemesi', 'Bakım penceresinde 7.4.x sürümüne yükseltin.', 'High', 4, 9],
    ['Yıllık fiziksel sayım – Kat 2', 'Kat 2 açık ofisteki tüm cihazların etiketlerini okutun.', 'Normal', 29, 14],
    ['AutoCAD LT lisans yenilemesi', 'Lisans 25 gün içinde sona eriyor; teklif alın.', 'High', 25, 7],
    ['Yeni personel cihaz hazırlığı', '4 adet Latitude 5440 için imaj ve zimmet hazırlığı.', 'Normal', 29, 12],
    ['Eski monitörlerin hurdaya ayrılması', 'Garantisi biten monitörleri listeleyin.', 'Low', 26, 30]
  ];
  for (const [title, description, priority, emp, due] of taskDefs) await post('/tasks', { title, description, priority, assignedEmployeeId: employees[emp]?.id || null, dueAt: new Date(Date.now() + due * 864e5).toISOString() });

  const summary = { written: stats.ok, failed: stats.fail, assets: assets.length, employees: employees.length, models: Object.keys(models).length, errors: stats.errors.slice(0, 40) };
  log('BİTTİ ' + JSON.stringify({ written: summary.written, failed: summary.failed, assets: summary.assets, employees: summary.employees }));
  return summary;
};
