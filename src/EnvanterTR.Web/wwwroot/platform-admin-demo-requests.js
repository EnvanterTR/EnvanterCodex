(() => {
  'use strict';

  const $ = (selector) => document.querySelector(selector);
  const state = { page: 1, pageSize: 25, total: 0, admins: [], selectedId: null, companySearchTimer: 0,
    leadRequestVersion: 0, companyRequestVersion: 0, detailRequestVersion: 0 };
  const statusLabels = { New: 'Yeni', Contacted: 'İletişime geçildi', Qualified: 'Nitelikli', DemoScheduled: 'Demo planlandı', DemoGranted: 'Demo açıldı', Converted: 'Müşteri oldu', Closed: 'Kapalı' };
  const esc = (value) => String(value ?? '').replace(/[&<>"']/g, (character) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[character]);
  const date = (value) => value ? new Intl.DateTimeFormat('tr-TR', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) : '—';
  const localDateTime = (value) => {
    if (!value) return '';
    const d = new Date(value);
    return new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
  };

  async function api(url, options = {}) {
    const response = await fetch(url, { ...options, credentials: 'same-origin', headers: { Accept: 'application/json', ...(options.headers || {}) } });
    if (!response.ok) {
      let message = `İstek başarısız (${response.status}).`;
      try {
        const body = await response.json();
        message = body.message || body.title || Object.values(body.errors || {})?.flat()?.[0] || message;
      } catch { /* Keep generic status. */ }
      throw new Error(message);
    }
    return response.status === 204 ? null : response.json();
  }

  function table(headers, rows) {
    return `<div class="table-wrap"><table><thead><tr>${headers.map((header) => `<th>${esc(header)}</th>`).join('')}</tr></thead><tbody>${rows}</tbody></table></div>`;
  }

  async function loadLeads(page = state.page) {
    const requestVersion = ++state.leadRequestVersion;
    const params = new URLSearchParams({ page: String(Math.max(1, page)), pageSize: String(state.pageSize) });
    const q = $('#demoLeadSearch').value.trim();
    const status = $('#demoLeadStatus').value;
    const followUp = $('#demoLeadFollowUp').value;
    if (q) params.set('q', q);
    if (status) params.set('status', status);
    if (followUp) params.set('followUp', followUp);
    $('#demoLeadsTable').innerHTML = '<div class="empty">Demo talepleri yükleniyor…</div>';
    try {
      const result = await api(`/api/platform/demo-requests?${params}`);
      if (requestVersion !== state.leadRequestVersion) return;
      state.page = result.page; state.total = result.total;
      $('#demoLeadCount').textContent = `${result.total} talep`;
      $('#demoLeadPage').textContent = `Sayfa ${result.page} · ${result.total} kayıt`;
      $('#prevDemoLeads').disabled = result.page <= 1;
      $('#nextDemoLeads').disabled = result.page * result.pageSize >= result.total;
      $('#demoLeadsTable').innerHTML = result.items.length ? table(
        ['Şirket / Talep sahibi', 'E-posta', 'Durum', 'Sorumlu', 'Takip', 'Talep tarihi', 'İşlem'],
        result.items.map((lead) => `<tr><td class="company-name">${esc(lead.companyName)}<span class="sub">${esc(lead.contactName)}${lead.phone ? ` · ${esc(lead.phone)}` : ''}</span>${lead.linkedCompanyName ? `<span class="sub">Bağlı hesap: ${esc(lead.linkedCompanyName)}</span>` : ''}</td><td>${esc(lead.email)}</td><td><span class="pill ${lead.status === 'Closed' ? 'red' : lead.status === 'DemoGranted' || lead.status === 'Converted' ? '' : 'warn'}">${esc(statusLabels[lead.status] || lead.status)}</span></td><td>${esc(lead.assignedAdminName || 'Atanmamış')}</td><td>${date(lead.followUpAt)}</td><td>${date(lead.createdAt)}</td><td><button class="btn small" data-demo-lead-detail="${esc(lead.id)}">Ayrıntı</button></td></tr>`).join('')) : '<div class="empty">Bu filtrelerle eşleşen demo talebi bulunamadı.</div>';
    } catch (error) {
      if (requestVersion !== state.leadRequestVersion) return;
      $('#demoLeadsTable').innerHTML = `<div class="empty">${esc(error.message)}</div>`;
    }
  }

  async function loadCompanies(query = '') {
    const select = $('#demoLeadCompany');
    if (!select) return;
    const requestVersion = ++state.companyRequestVersion;
    const selectedId = select.value || select.dataset.selected || '';
    const params = new URLSearchParams({ active: 'true', page: '1', pageSize: '200' });
    if (query.trim()) params.set('q', query.trim());
    select.disabled = true; select.innerHTML = '<option value="">Şirketler yükleniyor…</option>';
    try {
      const result = await api(`/api/platform/companies?${params}`);
      if (requestVersion !== state.companyRequestVersion) return;
      const options = result.items.map((company) => `<option value="${esc(company.id)}">${esc(company.name)}${company.slug ? ` · ${esc(company.slug)}` : ''}</option>`).join('');
      select.innerHTML = `<option value="">Şirketle ilişkilendirme</option>${options}`;
      if (selectedId && !result.items.some((company) => company.id === selectedId)) {
        const current = await api(`/api/platform/companies/${encodeURIComponent(selectedId)}`);
        if (requestVersion !== state.companyRequestVersion) return;
        select.insertAdjacentHTML('beforeend', `<option value="${esc(current.company.id)}">${esc(current.company.name)}</option>`);
      }
      if (requestVersion !== state.companyRequestVersion) return;
      select.value = selectedId;
    } catch (error) {
      if (requestVersion !== state.companyRequestVersion) return;
      select.innerHTML = '<option value="">Şirket listesi yüklenemedi</option>';
      $('#demoLeadFeedback').textContent = error.message;
    } finally { if (requestVersion === state.companyRequestVersion) select.disabled = false; }
  }

  async function showLead(id) {
    const requestVersion = ++state.detailRequestVersion;
    state.companyRequestVersion++;
    state.selectedId = id;
    $('#demoLeadDetail').hidden = false;
    $('#demoLeadDetailBody').innerHTML = '<div class="empty">Talep ayrıntısı yükleniyor…</div>';
    try {
      const [{ lead, history }, admins] = await Promise.all([
        api(`/api/platform/demo-requests/${encodeURIComponent(id)}`),
        state.admins.length ? Promise.resolve(state.admins) : api('/api/platform/admins')
      ]);
      if (requestVersion !== state.detailRequestVersion || state.selectedId !== id) return;
      state.admins = admins;
      $('#demoLeadDetailTitle').textContent = lead.companyName;
      $('#demoLeadDetailMeta').textContent = `${lead.contactName} · ${lead.email} · ${date(lead.createdAt)}`;
      const statusOptions = Object.entries(statusLabels).map(([value, label]) => `<option value="${value}" ${lead.status === value ? 'selected' : ''}>${label}</option>`).join('');
      const adminOptions = `<option value="">Atanmamış</option>${admins.map((admin) => `<option value="${esc(admin.id)}" ${lead.assignedAdminUserId === admin.id ? 'selected' : ''}>${esc(admin.name)} · ${esc(admin.email)}</option>`).join('')}`;
      const historyRows = history.length ? history.map((item) => `<tr><td>${date(item.occurredAt)}</td><td>${esc(item.actor || 'Yönetici')}</td><td>${esc(item.action)}</td><td>${esc(item.details || '')}</td></tr>`).join('') : '<tr><td colspan="4">Henüz yönetici işlem kaydı yok.</td></tr>';
      $('#demoLeadDetailBody').innerHTML = `<div class="cards" style="grid-template-columns:repeat(auto-fit,minmax(170px,1fr))"><article class="card"><div class="card-top">İstek kaynağı</div><strong style="font-size:1rem">${esc(lead.source)}</strong><small>Onay: ${esc(lead.consentNoticeVersion || 'demo-request-v1')} · ${date(lead.consentAt)}</small></article><article class="card"><div class="card-top">Ekip / Envanter</div><strong style="font-size:1rem">${esc(lead.teamSize || '—')} / ${esc(lead.assetCount || '—')}</strong><small>${esc(lead.jobTitle || 'Görev belirtilmedi')}</small></article><article class="card"><div class="card-top">İletişim</div><strong style="font-size:1rem">${esc(lead.phone || 'Telefon yok')}</strong><small>${esc(lead.email)}</small></article><article class="card"><div class="card-top">Demo / Müşteri</div><strong style="font-size:1rem">${lead.demoGrantedAt ? `Demo · ${date(lead.demoGrantedAt)}` : 'Demo açılmadı'}</strong><small>${lead.companyId ? `Bağlı şirket: ${esc(lead.linkedCompanyName || lead.companyId)}` : 'Şirket hesabına bağlanmadı'}</small></article></div><div class="panel"><div class="panel-head"><h2>Talep ve takip</h2></div><p>${esc(lead.message || 'Müşterinin ek mesajı yok.')}</p><form id="demoLeadEditForm" class="form-grid"><label class="field">Aşama<select name="status" required>${statusOptions}</select></label><label class="field">Sorumlu yönetici<select name="assignedAdminUserId">${adminOptions}</select></label><label class="field">Sonraki takip<input name="followUpAt" type="datetime-local" value="${localDateTime(lead.followUpAt)}"></label><label class="field">Bağlı şirket ara<input id="demoLeadCompanySearch" type="search" maxlength="120" placeholder="Şirket adı veya alt alan adı ara"></label><label class="field wide">Şirket<select name="companyId" id="demoLeadCompany" data-selected="${esc(lead.companyId || '')}"><option value="">Şirketle ilişkilendirme</option></select></label><label class="field wide">İşlem notu<textarea name="note" maxlength="1000" placeholder="Görüşme sonucu veya takip notu"></textarea></label><div class="wide actions"><button class="btn primary" type="submit">Değişiklikleri kaydet</button><span id="demoLeadFeedback" class="muted" role="status"></span></div></form></div><div class="panel"><div class="panel-head"><h2>Yönetici işlem geçmişi</h2></div>${table(['Tarih', 'Yönetici', 'İşlem', 'Değişiklikler / not'], historyRows)}</div>`;
      await loadCompanies();
      if (requestVersion !== state.detailRequestVersion || state.selectedId !== id) return;
      $('#demoLeadCompanySearch').addEventListener('input', () => {
        window.clearTimeout(state.companySearchTimer);
        state.companySearchTimer = window.setTimeout(() => loadCompanies($('#demoLeadCompanySearch').value), 220);
      });
      $('#demoLeadEditForm').addEventListener('submit', saveLead);
    } catch (error) {
      if (requestVersion !== state.detailRequestVersion || state.selectedId !== id) return;
      $('#demoLeadDetailBody').innerHTML = `<div class="empty">${esc(error.message)}</div>`;
    }
  }

  window.platformDemoRequestsShow = showLead;

  async function saveLead(event) {
    event.preventDefault();
    const form = event.currentTarget;
    const feedback = $('#demoLeadFeedback');
    const button = form.querySelector('button[type="submit"]');
    const leadId = state.selectedId;
    const detailRequestVersion = state.detailRequestVersion;
    const payload = { status: form.elements.status.value, assignedAdminUserId: form.elements.assignedAdminUserId.value || null,
      followUpAt: form.elements.followUpAt.value ? new Date(form.elements.followUpAt.value).toISOString() : null,
      companyId: form.elements.companyId.value || null, note: form.elements.note.value.trim() || null };
    button.disabled = true; feedback.textContent = 'Kaydediliyor…';
    try {
      const csrf = await api('/api/session/csrf');
      await api(`/api/platform/demo-requests/${encodeURIComponent(leadId)}`, {
        method: 'PATCH', headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': csrf.requestToken },
        body: JSON.stringify(payload)
      });
      if (state.selectedId !== leadId || state.detailRequestVersion !== detailRequestVersion) return;
      feedback.textContent = 'Kayıt güncellendi ve yönetici geçmişine eklendi.';
      await Promise.all([showLead(leadId), loadLeads(state.page)]);
    } catch (error) { if (state.selectedId === leadId && state.detailRequestVersion === detailRequestVersion) feedback.textContent = error.message; }
    finally { button.disabled = false; }
  }

  $('#nav').addEventListener('click', (event) => { if (event.target.closest('[data-view="demo-requests"]')) loadLeads(1); });
  $('#refreshDemoLeads').addEventListener('click', () => loadLeads(state.page));
  $('#searchDemoLeads').addEventListener('click', () => loadLeads(1));
  $('#demoLeadStatus').addEventListener('change', () => loadLeads(1));
  $('#demoLeadFollowUp').addEventListener('change', () => loadLeads(1));
  $('#demoLeadSearch').addEventListener('keydown', (event) => { if (event.key === 'Enter') { event.preventDefault(); loadLeads(1); } });
  $('#prevDemoLeads').addEventListener('click', () => loadLeads(state.page - 1));
  $('#nextDemoLeads').addEventListener('click', () => loadLeads(state.page + 1));
  $('#closeDemoLeadDetail').addEventListener('click', () => {
    state.detailRequestVersion++; state.companyRequestVersion++;
    $('#demoLeadDetail').hidden = true; state.selectedId = null;
  });
  $('#demoLeadsTable').addEventListener('click', (event) => {
    const button = event.target.closest('[data-demo-lead-detail]');
    if (button) showLead(button.dataset.demoLeadDetail);
  });
})();
