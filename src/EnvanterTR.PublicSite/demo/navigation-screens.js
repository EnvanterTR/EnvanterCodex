(() => {
  const supportedScreens = new Set([
    'overview', 'assetList', 'assignments', 'licenses', 'accessories', 'consumables',
    'components', 'employees', 'locations', 'maintenance', 'reports'
  ]);

  // Guest demo records are illustrative only. Actions change this in-memory sample
  // state and never call the authenticated company API.
  const examples = {
    assetOrders: ['Yeni dizüstü bilgisayar alımı', 'Toplantı odası ekranları', 'Yedek ağ ekipmanı'],
    assetModels: ['ThinkPad T14 Gen 5', 'Dell UltraSharp U2724D', 'HP ProDesk 400 G9'],
    brands: ['Lenovo', 'Dell', 'HP'], categories: ['Bilgisayar', 'Monitör', 'Ağ ekipmanı'],
    statusLabels: ['Kullanılabilir', 'Atanmış', 'Bakımda'], qrBarcodes: ['SNT-AP-037 · QR', 'SNT-MN-014 · Barkod', 'SNT-LT-102 · QR'],
    returns: ['SNT-LT-018 · Ayşe Yılmaz', 'SNT-MN-006 · Mehmet Demir', 'SNT-PH-011 · Elif Kaya'],
    transfers: ['İstanbul → Ankara ofisi', 'Merkez → İzmir ofisi', 'Depo → Ar-Ge katı'],
    reservations: ['Toplantı odası için projektör', 'Yeni çalışan için dizüstü', 'Saha ekibi için tablet'],
    movementHistory: ['Varlık zimmetlendi', 'Lokasyon güncellendi', 'Bakım kaydı tamamlandı'],
    licenseAssignments: ['Microsoft 365 · Ayşe Yılmaz', 'Adobe CC · Tasarım ekibi', 'AutoCAD LT · Mühendislik'],
    contracts: ['Microsoft kurumsal sözleşmesi', 'Donanım bakım sözleşmesi', 'Bulut hizmeti sözleşmesi'],
    warranties: ['SNT-LT-018 · 12 ay kaldı', 'SNT-MN-006 · 4 ay kaldı', 'SNT-SV-003 · Süresi doldu'],
    purchaseOrders: ['PO-2026-014 · Klavye ve mouse', 'PO-2026-015 · Dock istasyonları', 'PO-2026-016 · Kulaklık'],
    stockRequests: ['REQ-2026-031 · Toner', 'REQ-2026-032 · A4 kâğıt', 'REQ-2026-033 · USB bellek'],
    kits: ['Yeni çalışan başlangıç kiti', 'Saha destek kiti', 'Toplantı odası kiti'],
    stockMovements: ['Toner · 5 adet giriş', 'USB bellek · 2 adet çıkış', 'Klavye · 4 adet transfer'],
    appUsers: ['ayse.yilmaz · Yönetici', 'mehmet.demir · Standart kullanıcı', 'elif.kaya · Salt okunur'],
    departments: ['Bilgi Teknolojileri', 'İnsan Kaynakları', 'Ürün ve Tasarım'],
    roles: ['Şirket yöneticisi', 'Envanter sorumlusu', 'Görüntüleyici'],
    buildings: ['İstanbul · A Blok / 3. kat', 'Ankara · Merkez / 2. kat', 'İzmir · Ofis / 1. kat'],
    companyUnits: ['Merkez Ofis', 'Ar-Ge Birimi', 'Saha Operasyonları'],
    maintenancePlans: ['Dizüstü yıllık bakım planı', 'Sunucu üç aylık kontrolü', 'UPS aylık testi'],
    audits: ['2026 Q3 varlık sayımı', 'İstanbul ofis kontrolü', 'Yazılım lisans incelemesi'],
    calendar: ['20 Eki · Sunucu bakımı', '22 Eki · Ofis sayımı', '25 Eki · Lisans yenileme'],
    tasks: ['Etiketi doğrula · SNT-AP-037', 'Teslim alımını onayla', 'Bakım raporunu incele'],
    customReports: ['Lokasyona göre varlıklar', 'Departmana göre zimmetler', 'Yaklaşan yenilemeler'],
    exports: ['Varlık listesi · CSV', 'Zimmet raporu · CSV', 'Lisans özeti · CSV'],
    imports: ['Varlık aktarım şablonu', 'Çalışan aktarım şablonu', 'İçe aktarma geçmişi'],
    dataExports: ['Varlıklar · 50 kayıt', 'Çalışanlar · 24 kayıt', 'Lisanslar · 12 kayıt'],
    backups: ['18 Eki · Otomatik yedek', '17 Eki · Otomatik yedek', '16 Eki · Otomatik yedek'],
    restore: ['18 Eki yedeği · Önizleme', '17 Eki yedeği · Önizleme', '16 Eki yedeği · Önizleme'],
    companyProfile: ['Ardıç Teknoloji A.Ş.', 'Vergi ve fatura bilgileri', 'İletişim bilgileri'],
    branding: ['Mavi tema · Etkin', 'Şirket logosu · Örnek', 'Giriş ekranı · Önizleme'],
    notifications: ['E-posta bildirimleri', 'Bakım hatırlatmaları', 'Lisans bitiş uyarıları'],
    integrations: ['Microsoft Entra ID · Örnek', 'E-posta · Yapılandırılmamış', 'API anahtarları · Demo'],
    security: ['İki aşamalı doğrulama', 'Oturum politikası', 'Parola gereksinimleri'],
    auditLog: ['Kullanıcı oturum açtı', 'Varlık kaydı görüntülendi', 'Rapor dışa aktarıldı'],
    support: ['Kullanım kılavuzu', 'Sık sorulan sorular', 'Destek talebi oluştur']
  };

  const records = Object.entries(examples).flatMap(([key, names]) => names.map((name, index) => ({
    id: `${key}-${index + 1}`, name,
    status: ['Örnek', 'İnceleniyor', 'Tamamlandı'][index],
    category: ['Kayıt', 'İşlem', 'Ayar'][index],
    scope: ['Ardıç Teknoloji A.Ş.', 'İstanbul ofisi', 'Demo verisi'][index]
  })));
  const messages = {
    tr: { title: 'Örnek kayıtlar', hint: 'Bu menü için hazırlanmış örnekleri arayın, filtreleyin ve inceleyin. Demo işlemleri yalnızca bu ekrandaki bellekte geçerlidir.', search: 'Örnek kayıtlarda ara', action: 'Örnek işlemi uygula', detail: 'Detay', empty: 'Aramanızla eşleşen örnek kayıt yok.', count: n => `${n} örnek kayıt`, saved: 'Demo işlemi uygulandı. Gerçek şirket verisi değişmedi.', data: 'Örnek demo verisi', close: 'Kapat', columns: ['Kayıt', 'Durum', 'Kategori', 'Kapsam', 'İşlem'] },
    en: { title: 'Sample records', hint: 'Search, filter and inspect examples prepared for this menu. Demo actions only affect in-memory state on this screen.', search: 'Search sample records', action: 'Run demo action', detail: 'Details', empty: 'No sample records match your search.', count: n => `${n} sample records`, saved: 'Demo action applied. No company data changed.', data: 'Sample demo data', close: 'Close', columns: ['Record', 'Status', 'Category', 'Scope', 'Action'] },
    ru: { title: 'Примеры записей', hint: 'Ищите, фильтруйте и просматривайте примеры для этого меню. Действия демо затрагивают только данные в памяти этого экрана.', search: 'Поиск примеров', action: 'Демо-действие', detail: 'Детали', empty: 'Совпадений не найдено.', count: n => `Примеров: ${n}`, saved: 'Демо-действие выполнено. Данные компании не изменены.', data: 'Примеры демо-данных', close: 'Закрыть', columns: ['Запись', 'Статус', 'Категория', 'Область', 'Действие'] },
    uk: { title: 'Приклади записів', hint: 'Шукайте, фільтруйте та переглядайте приклади для цього меню. Демо-дії змінюють лише дані в пам’яті цієї сторінки.', search: 'Пошук прикладів', action: 'Демо-дія', detail: 'Деталі', empty: 'Збігів не знайдено.', count: n => `Прикладів: ${n}`, saved: 'Демо-дію виконано. Дані компанії не змінено.', data: 'Приклади демо-даних', close: 'Закрити', columns: ['Запис', 'Статус', 'Категорія', 'Область', 'Дія'] },
    mk: { title: 'Пример записи', hint: 'Пребарајте, филтрирајте и прегледајте примери за ова мени. Демо-дејствата влијаат само на податоците во меморијата на оваа страница.', search: 'Пребарај примери', action: 'Демо-дејство', detail: 'Детали', empty: 'Нема совпаѓања.', count: n => `Примери: ${n}`, saved: 'Демо-дејството е извршено. Податоците на компанијата не се променети.', data: 'Пример демо-податоци', close: 'Затвори', columns: ['Запис', 'Статус', 'Категорија', 'Опсег', 'Дејство'] }
  };

  let activeKey = null;
  let activeRecords = [];
  const original = new WeakMap();
  const controls = ['#search', '#statusFilter', '#categoryFilter', '#exportButton']
    .map(s => document.querySelector(s)).filter(Boolean);
  const contextual = [document.querySelector('.page-actions'), document.querySelector('#activityRows')?.closest('.panel')].filter(Boolean);
  const toolsElement = document.querySelector('.panel .tools');
  const originalTools = toolsElement ? [...toolsElement.childNodes] : [];
  const language = () => document.querySelector('#languageSelect')?.value || document.documentElement.lang || 'tr';
  const copy = () => messages[language()] || messages.tr;
  const escape = value => String(value).replace(/[&<>"']/g, char => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[char]);
  function toast(text) { const el = document.querySelector('#toast'); if (!el) return; el.textContent = text; el.classList.add('show'); clearTimeout(toast.timer); toast.timer = setTimeout(() => el.classList.remove('show'), 2600); }
  function setLegacyControls(hidden) {
    controls.forEach(el => {
      if (hidden) { if (!original.has(el)) original.set(el, { hidden: el.hidden, disabled: el.disabled, display: el.style.display }); el.hidden = true; el.disabled = true; el.style.display = 'none'; }
      else if (original.has(el)) { const state = original.get(el); el.hidden = state.hidden; el.disabled = state.disabled; el.style.display = state.display; original.delete(el); }
    });
    contextual.forEach(el => { el.hidden = hidden; el.style.display = hidden ? 'none' : ''; });
  }
  function closeDetail() { document.querySelector('#navExampleDialog')?.remove(); }
  function draw() {
    if (!activeKey) return;
    const t = copy();
    const search = document.querySelector('#navExampleSearch').value.trim().toLocaleLowerCase(language());
    const status = document.querySelector('#navExampleStatus').value;
    const category = document.querySelector('#navExampleCategory').value;
    const rows = activeRecords.filter(row => (!search || Object.values(row).join(' ').toLocaleLowerCase(language()).includes(search)) && (!status || row.status === status) && (!category || row.category === category));
    const tbody = document.querySelector('#tableRows');
    tbody.replaceChildren();
    if (!rows.length) { const tr = document.createElement('tr'); const td = document.createElement('td'); td.colSpan = 5; td.className = 'empty'; td.textContent = t.empty; tr.append(td); tbody.append(tr); }
    else rows.forEach(row => {
      const tr = document.createElement('tr');
      const values = [row.name, row.status, row.category, row.scope];
      values.forEach((value, index) => { const td = document.createElement('td'); td.textContent = value; if (index === 0) td.className = 'item-name'; tr.append(td); });
      const td = document.createElement('td');
      td.innerHTML = `<div class="row-actions"><button class="btn small" type="button" data-nav-detail="${escape(row.id)}">${escape(t.detail)}</button><button class="btn small" type="button" data-nav-action="${escape(row.id)}">${escape(t.action)}</button></div>`;
      tr.append(td); tbody.append(tr);
    });
    document.querySelector('#count').textContent = t.count(rows.length);
  }
  function show(button, key) {
    activeKey = key;
    activeRecords = records.filter(row => row.id.startsWith(`${key}-`)).map(row => ({ ...row }));
    const t = copy();
    document.body.dataset.demoExampleNav = key;
    document.querySelectorAll('#demoNav [data-nav-item]').forEach(item => { const selected = item === button; item.classList.toggle('active', selected); if (selected) item.setAttribute('aria-current', 'page'); else item.removeAttribute('aria-current'); });
    const title = button.innerText.trim();
    document.querySelector('#pageTitle').textContent = title;
    document.querySelector('#crumbTitle').textContent = title;
    document.querySelector('#pageSubtitle').textContent = t.hint;
    document.querySelector('#tableTitle').textContent = t.title;
    document.querySelector('#tableHint').textContent = `${t.data} · ${title}`;
    document.querySelector('#tableHead').innerHTML = `<tr>${t.columns.map(x => `<th>${escape(x)}</th>`).join('')}</tr>`;
    const stats = document.querySelector('#stats');
    stats.hidden = false;
    stats.innerHTML = `<article class="stat"><label>${escape(t.data)}</label><strong>${activeRecords.length}</strong><small>${escape(title)}</small></article><article class="stat"><label>${escape(t.columns[1])}</label><strong>${new Set(activeRecords.map(x => x.status)).size}</strong><small>${escape(t.hint)}</small></article>`;
    document.querySelector('.page-actions')?.setAttribute('hidden', '');
    document.querySelector('#activityRows')?.closest('.panel')?.setAttribute('hidden', '');
    document.querySelector('#detailBackdrop')?.classList.remove('open');
    document.querySelector('#imageBackdrop')?.classList.remove('open');
    document.querySelector('#search').value = '';
    const tools = document.querySelector('.panel .tools');
    tools.innerHTML = `<input id="navExampleSearch" type="search" aria-label="${escape(t.search)}" placeholder="${escape(t.search)}"><select id="navExampleStatus" aria-label="${escape(t.columns[1])}"><option value="">${escape(t.columns[1])} · tümü</option>${[...new Set(activeRecords.map(x => x.status))].map(x => `<option>${escape(x)}</option>`).join('')}</select><select id="navExampleCategory" aria-label="${escape(t.columns[2])}"><option value="">${escape(t.columns[2])} · tümü</option>${[...new Set(activeRecords.map(x => x.category))].map(x => `<option>${escape(x)}</option>`).join('')}</select><span class="count" id="count" aria-live="polite"></span>`;
    setLegacyControls(true);
    const exportButton = document.querySelector('#exportButton');
    exportButton.hidden = false;
    exportButton.disabled = false;
    exportButton.style.display = '';
    exportButton.textContent = '⇩ CSV indir';
    document.querySelector('.sidebar')?.classList.remove('mobile-open');
    document.querySelector('#mobileMenuBackdrop')?.classList.remove('open');
    document.querySelector('#mobileMenuToggle')?.setAttribute('aria-expanded', 'false');
    draw();
  }
  function restore() {
    if (!activeKey) return;
    activeKey = null;
    delete document.body.dataset.demoExampleNav;
    closeDetail();
    const stats = document.querySelector('#stats'); stats.hidden = false;
    toolsElement.replaceChildren(...originalTools);
    setLegacyControls(false);
  }
  document.querySelector('#demoNav')?.addEventListener('click', event => {
    const button = event.target.closest('[data-view]'); if (!button) return;
    const key = button.dataset.navExtra || button.dataset.navItem || button.dataset.view;
    if (supportedScreens.has(key)) { restore(); return; }
    if (!examples[key]) return;
    event.preventDefault(); event.stopImmediatePropagation(); show(button, key);
  }, true);
  document.addEventListener('input', event => { if (activeKey && event.target.closest('#navExampleSearch')) draw(); });
  document.addEventListener('change', event => { if (activeKey && event.target.closest('#navExampleStatus, #navExampleCategory')) draw(); });
  document.addEventListener('click', event => {
    if (!activeKey) return;
    const detail = event.target.closest('[data-nav-detail]');
    if (detail) {
      const row = activeRecords.find(x => x.id === detail.dataset.navDetail); if (!row) return;
      closeDetail();
      const dialog = document.createElement('div'); dialog.id = 'navExampleDialog'; dialog.className = 'detail-backdrop open'; dialog.setAttribute('role', 'dialog'); dialog.setAttribute('aria-modal', 'true');
      dialog.innerHTML = `<section class="dialog"><header class="dialog-head"><div><small>${escape(copy().data)}</small><h2>${escape(row.name)}</h2></div><button class="btn small" data-nav-close>${escape(copy().close)}</button></header><div class="dialog-body">${Object.entries(row).filter(([k]) => k !== 'id').map(([k,v]) => `<div class="detail"><small>${escape(k)}</small><strong>${escape(v)}</strong></div>`).join('')}</div><footer class="dialog-foot"><button class="btn" data-nav-close>${escape(copy().close)}</button></footer></section>`;
      document.body.append(dialog); return;
    }
    if (event.target.closest('[data-nav-close]') || event.target.id === 'navExampleDialog') { closeDetail(); return; }
    const action = event.target.closest('[data-nav-action]'); if (action) { const row = activeRecords.find(x => x.id === action.dataset.navAction); if (row) { row.status = row.status === 'Tamamlandı' ? 'Örnek' : 'Tamamlandı'; draw(); toast(copy().saved); } return; }
    if (event.target.closest('#exportButton')) {
      event.preventDefault(); event.stopImmediatePropagation();
      const q = document.querySelector('#navExampleSearch').value.trim().toLocaleLowerCase(language());
      const status = document.querySelector('#navExampleStatus').value; const category = document.querySelector('#navExampleCategory').value;
      const rows = activeRecords.filter(x => (!q || Object.values(x).join(' ').toLocaleLowerCase(language()).includes(q)) && (!status || x.status === status) && (!category || x.category === category));
      const csv = ['Kayıt,Durum,Kategori,Kapsam', ...rows.map(x => [x.name,x.status,x.category,x.scope].map(v => `"${String(v).replaceAll('"','""')}"`).join(','))].join('\r\n');
      const a = document.createElement('a'); a.href = URL.createObjectURL(new Blob(['\ufeff', csv], { type: 'text/csv;charset=utf-8' })); a.download = `${activeKey}-demo.csv`; a.click(); setTimeout(() => URL.revokeObjectURL(a.href), 1000);
    }
  }, true);
  document.addEventListener('keydown', event => { if (activeKey && event.key === 'Escape') closeDetail(); }, true);
  document.querySelector('#languageSelect')?.addEventListener('change', () => { if (activeKey) { const button = document.querySelector(`#demoNav [data-nav-extra="${CSS.escape(activeKey)}"]`); if (button) show(button, activeKey); } });
})();
