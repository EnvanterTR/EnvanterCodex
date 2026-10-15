(() => {
  const detail = document.querySelector('#companyDetailDialog');
  const detailBody = document.querySelector('#companyDetailBody');
  const toast = message => {
    const target = document.querySelector('#toast');
    target.textContent = message;
    target.classList.add('show');
    setTimeout(() => target.classList.remove('show'), 3500);
  };
  let companyId = null;
  let detailTrigger = null;
  let observerQueued = false;

  async function request(url, options = {}) {
    const headers = new Headers(options.headers || {});
    if (options.body) headers.set('Content-Type', 'application/json');
    if (!['GET', 'HEAD'].includes((options.method || 'GET').toUpperCase())) {
      const csrf = await fetch('/api/session/csrf', { credentials: 'same-origin' });
      if (!csrf.ok) throw new Error('Güvenli oturum başlatılamadı.');
      headers.set('X-CSRF-TOKEN', (await csrf.json()).requestToken);
    }
    const response = await fetch(url, { ...options, headers, credentials: 'same-origin' });
    let data = null;
    try { data = await response.json(); } catch { /* Empty success response. */ }
    if (!response.ok) {
      if (response.status === 401 || response.status === 403) location.reload();
      const message = data?.errors ? Object.values(data.errors).flat().join(' ') : data?.message || data?.title || `İstek başarısız (${response.status}).`;
      throw new Error(message);
    }
    return data;
  }

  const localDateTime = value => {
    const date = new Date(value);
    date.setMinutes(date.getMinutes() - date.getTimezoneOffset());
    return date.toISOString().slice(0, 16);
  };

  function field(labelText, control) {
    const label = document.createElement('label');
    label.className = 'field';
    label.append(document.createTextNode(labelText), control);
    return label;
  }

  async function editSubscription(subscription) {
    const plans = (await request('/api/platform/plans')).filter(plan => plan.isActive && plan.deploymentKind === 'cloud');
    if (!plans.length) throw new Error('Düzenleme için etkin bir Cloud planı bulunamadı.');

    const dialog = document.createElement('dialog');
    dialog.className = 'dialog';
    dialog.style.width = 'min(520px,calc(100% - 24px))';
    const form = document.createElement('form');
    const head = document.createElement('div');
    head.className = 'dialog-head';
    const title = document.createElement('h2');
    title.textContent = 'Abonelik planını ve tarihlerini düzenle';
    const close = document.createElement('button');
    close.type = 'button';
    close.className = 'btn small';
    close.textContent = '✕';
    close.addEventListener('click', () => dialog.close());
    head.append(title, close);

    const body = document.createElement('div');
    body.className = 'dialog-body';
    const plan = document.createElement('select');
    plan.name = 'planId';
    plan.required = true;
    for (const item of plans) {
      const option = document.createElement('option');
      option.value = item.id;
      option.textContent = `${item.name} · ${item.code}`;
      option.selected = item.id === subscription.planId;
      plan.append(option);
    }
    const startsAt = document.createElement('input');
    startsAt.name = 'startsAt';
    startsAt.type = 'datetime-local';
    startsAt.required = true;
    startsAt.value = localDateTime(subscription.startsAt);
    const endsAt = document.createElement('input');
    endsAt.name = 'endsAt';
    endsAt.type = 'datetime-local';
    endsAt.required = true;
    endsAt.value = localDateTime(subscription.endsAt);
    const validatePeriod = () => {
      const invalid = startsAt.value && endsAt.value && new Date(endsAt.value) <= new Date(startsAt.value);
      endsAt.setCustomValidity(invalid ? 'Bitiş tarihi başlangıçtan sonra olmalıdır.' : '');
      return !invalid;
    };
    for (const control of [startsAt, endsAt]) {
      control.addEventListener('input', validatePeriod);
      control.addEventListener('change', validatePeriod);
    }
    const note = document.createElement('input');
    note.name = 'note';
    note.maxLength = 1000;
    note.required = true;
    note.placeholder = 'Zorunlu: ücretsiz uzatma / plan değişikliği gerekçesi';
    body.append(field('Cloud planı', plan), field('Başlangıç tarihi ve saati', startsAt),
      field('Bitiş tarihi ve saati', endsAt), field('İşlem gerekçesi (zorunlu)', note));
    const preview = document.createElement('div');
    preview.className = 'notice';
    preview.setAttribute('aria-live', 'polite');
    const formatDate = value => new Intl.DateTimeFormat('tr-TR', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
    const refreshPreview = () => {
      const selectedPlan = plans.find(item => item.id === plan.value);
      const selectedName = selectedPlan ? `${selectedPlan.name} · ${selectedPlan.code}` : 'Seçilmedi';
      const currentName = subscription.name ? `${subscription.name} · ${subscription.code || ''}` : 'Mevcut paket';
      preview.textContent = `Önizleme: ${currentName} → ${selectedName}. Dönem ${startsAt.value ? formatDate(startsAt.value) : '—'} – ${endsAt.value ? formatDate(endsAt.value) : '—'}. ${subscription.isDemo ? 'Demo olarak kalır.' : 'Ücretli abonelik kaydı olarak kalır.'} Bu yönetici düzenlemesi tahsilat veya iade oluşturmaz, ödeme referansını değiştirmez.`;
    };
    for (const control of [plan, startsAt, endsAt]) control.addEventListener('change', refreshPreview);
    refreshPreview();
    body.append(preview);
    const notice = document.createElement('div');
    notice.className = 'notice warning';
    notice.textContent = 'Gerekçe değiştirilemez yönetici denetim kaydına yazılır. Aynı plan ve tarihlerin tekrar gönderilmesi ikinci bir değişiklik veya audit kaydı oluşturmaz. Firma erişimini durdurmak için firmayı askıya alın.';
    body.append(notice);

    const foot = document.createElement('div');
    foot.className = 'dialog-foot';
    const cancel = document.createElement('button');
    cancel.type = 'button';
    cancel.className = 'btn';
    cancel.textContent = 'Vazgeç';
    cancel.addEventListener('click', () => dialog.close());
    const save = document.createElement('button');
    save.type = 'submit';
    save.className = 'btn primary';
    save.textContent = 'Değişiklikleri kaydet';
    foot.append(cancel, save);
    form.append(head, body, foot);
    dialog.append(form);
    document.body.append(dialog);
    dialog.addEventListener('close', () => dialog.remove(), { once: true });
    form.addEventListener('submit', async event => {
      event.preventDefault();
      if (!validatePeriod()) {
        endsAt.reportValidity();
        return;
      }
      save.disabled = true;
      try {
        await request(`/api/platform/companies/${companyId}/subscriptions/${subscription.id}`, {
          method: 'PUT',
          body: JSON.stringify({
            planId: plan.value,
            startsAt: new Date(startsAt.value).toISOString(),
            endsAt: new Date(endsAt.value).toISOString(),
            note: note.value || null
          })
        });
        dialog.close();
        toast('Abonelik planı ve tarihleri güncellendi.');
        detailTrigger?.click();
        document.querySelector('#searchCompanies')?.click();
        document.querySelector('#refreshAudit')?.click();
      } catch (error) {
        toast(error.message);
      } finally {
        save.disabled = false;
      }
    });
    dialog.showModal();
  }

  async function decorateSubscriptionHistory() {
    observerQueued = false;
    if (!detail.open || !companyId || detailBody.querySelector('[data-admin-edit-subscription]')) return;
    const heading = [...detailBody.querySelectorAll('.panel-head h2')].find(item => item.textContent === 'Abonelik geçmişi');
    const table = heading?.closest('.panel')?.querySelector('table');
    if (!table) return;
    try {
      const data = await request(`/api/platform/companies/${companyId}`);
      const subscriptions = data.subscriptions || [];
      const rows = table.querySelectorAll('tbody tr');
      if (!subscriptions.length || rows.length !== subscriptions.length) return;
      const header = document.createElement('th');
      header.textContent = 'İşlem';
      table.querySelector('thead tr')?.append(header);
      subscriptions.forEach((subscription, index) => {
        const row = rows[index];
        const cell = document.createElement('td');
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'btn small';
        button.textContent = 'Plan / tarih düzenle';
        button.dataset.adminEditSubscription = subscription.id;
        button.addEventListener('click', () => editSubscription(subscription).catch(error => toast(error.message)));
        cell.append(button);
        row.append(cell);
      });
    } catch (error) {
      toast(error.message);
    }
  }

  document.addEventListener('click', event => {
    const trigger = event.target.closest('#companiesTable [data-action="details"]');
    if (trigger) {
      companyId = trigger.dataset.id;
      detailTrigger = trigger;
    }
  }, true);
  new MutationObserver(() => {
    if (observerQueued) return;
    observerQueued = true;
    queueMicrotask(decorateSubscriptionHistory);
  }).observe(detailBody, { childList: true, subtree: true });
})();
