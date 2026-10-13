(() => {
  'use strict';
  const $ = (selector) => document.querySelector(selector);
  const state = { page: 1, pageSize: 50, total: 0, loading: false };
  const esc = (value) => String(value ?? '').replace(/[&<>"']/g, (character) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[character]);
  const date = (value) => value ? new Intl.DateTimeFormat('tr-TR', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) : '—';
  const roleLabel = (value) => ({ owner: 'Şirket sahibi', admin: 'Yönetici', manager: 'Envanter yöneticisi', auditor: 'Denetçi', viewer: 'Salt okunur' })[value] || value || '—';
  const api = async (url, options = {}) => {
    const response = await fetch(url, {
      ...options,
      credentials: 'same-origin',
      headers: { Accept: 'application/json', ...(options.headers || {}) }
    });
    if (!response.ok) {
      let message = `İstek başarısız (${response.status}).`;
      try { const body = await response.json(); message = body.message || body.title || message; } catch { /* Keep generic status. */ }
      throw new Error(message);
    }
    return response.json();
  };

  async function loadMembers(page = state.page) {
    if (state.loading) return;
    state.loading = true;
    const q = $('#memberSearch').value.trim();
    const params = new URLSearchParams({ page: String(Math.max(1, page)), pageSize: String(state.pageSize) });
    if (q) params.set('q', q);
    $('#membersTable').innerHTML = '<div class="empty">Hesaplar yükleniyor…</div>';
    $('#memberDetail').hidden = true;
    try {
      const result = await api(`/api/platform/members?${params}`);
      state.page = result.page;
      state.total = result.total;
      $('#memberCount').textContent = `${result.total} hesap`;
      $('#memberPage').textContent = `Sayfa ${result.page} · ${result.total} hesap`;
      $('#prevMembers').disabled = result.page <= 1;
      $('#nextMembers').disabled = result.page * result.pageSize >= result.total;
      $('#membersTable').innerHTML = result.items.length ? `<div class="table-wrap"><table><thead><tr><th>Hesap</th><th>İletişim</th><th>Güvenlik</th><th>İlk şirket üyeliği</th><th>Şirketler</th><th>Son görülme</th><th></th></tr></thead><tbody>${result.items.map((account) => {
        const companies = account.memberships.length ? account.memberships.map((membership) => `${esc(membership.name)} · ${esc(roleLabel(membership.roleKey))}${membership.isActive ? '' : ' · pasif'}`).join('<br>') : 'Şirket üyeliği yok';
        return `<tr><td class="company-name">${esc(account.displayName)}<span class="sub">${esc(account.email || 'E-posta yok')} · ${account.emailConfirmed ? 'doğrulanmış' : 'doğrulanmamış'}</span></td><td>${esc(account.phoneNumber || '—')}</td><td>${account.twoFactorEnabled ? '<span class="pill">2FA açık</span>' : '<span class="pill warn">2FA kapalı</span>'}${account.isLockedOut ? '<span class="sub"><span class="pill red">Kilitli</span></span>' : ''}</td><td>${date(account.firstMembershipAt)}</td><td>${companies}<span class="sub">${account.membershipCount} üyelik</span></td><td>${date(account.lastSeenAt)}</td><td><button class="btn small" data-member-detail="${esc(account.id)}">Ayrıntı</button></td></tr>`;
      }).join('')}</tbody></table></div>` : '<div class="empty">Aramayla eşleşen hesap bulunamadı.</div>';
    } catch (error) {
      $('#memberCount').textContent = '—';
      $('#memberPage').textContent = '';
      $('#membersTable').innerHTML = `<div class="empty">${esc(error.message)}</div>`;
    } finally { state.loading = false; }
  }

  async function loadMemberDetail(id) {
    const body = $('#memberDetailBody');
    body.innerHTML = '<div class="empty">Hesap bilgileri yükleniyor…</div>';
    $('#memberDetail').hidden = false;
    try {
      const result = await api(`/api/platform/members/${encodeURIComponent(id)}`);
      const account = result.account;
      $('#memberDetailTitle').textContent = account.displayName || account.email || 'Hesap ayrıntısı';
      $('#memberDetailMeta').textContent = `${account.email || 'E-posta yok'} · ${result.membershipCount} şirket üyeliği`;
      const details = [
        ['E-posta doğrulaması', account.emailConfirmed ? 'Doğrulandı' : 'Doğrulanmadı'],
        ['Telefon', account.phoneNumber || 'Belirtilmemiş'],
        ['Telefon doğrulaması', account.phoneNumberConfirmed ? 'Doğrulandı' : 'Doğrulanmadı'],
        ['İki aşamalı doğrulama', account.twoFactorEnabled ? 'Açık' : 'Kapalı'],
        ['Hesap kilidi', account.isLockedOut ? 'Kilitli' : 'Kilitli değil'],
        ['Başarısız giriş sayısı', account.accessFailedCount],
        ['Son görülme', date(account.lastSeenAt)],
        ['İlk şirket üyeliği', date(account.firstMembershipAt)]
      ];
      const memberships = result.memberships.map((membership) => {
        const subscription = membership.subscription;
        const subscriptionInfo = subscription ? `${esc(subscription.name)} (${esc(subscription.code)}) · ${esc(subscription.status)}${subscription.isDemo ? ' · Demo' : ''}<span class="sub">${date(subscription.startsAt)} – ${date(subscription.endsAt)}</span><span class="sub">Ödeme referansı: ${esc(subscription.paymentReference || '—')}</span>` : 'Abonelik kaydı yok';
        return `<tr><td class="company-name">${esc(membership.name)}<span class="sub">${esc(membership.slug || '')}${membership.isActive ? '' : ' · şirket pasif'}</span></td><td>${esc(roleLabel(membership.roleKey))}</td><td>${date(membership.createdAt)}</td><td>${subscriptionInfo}</td></tr>`;
      }).join('');
      body.innerHTML = `<div class="cards" style="grid-template-columns:repeat(auto-fit,minmax(190px,1fr));margin:0">${details.map(([label, value]) => `<div class="card"><div class="card-top">${esc(label)}</div><strong style="font-size:1rem">${esc(value)}</strong></div>`).join('')}</div><div><h3>Şirket üyelikleri</h3>${result.membershipsTruncated ? '<div class="notice warning">Güvenli görüntüleme sınırı nedeniyle üyeliklerin yalnızca ilk 200 kaydı gösteriliyor.</div>' : ''}${memberships ? `<div class="table-wrap"><table><thead><tr><th>Şirket</th><th>Rol</th><th>Üyelik tarihi</th><th>Plan ve dönem</th></tr></thead><tbody>${memberships}</tbody></table></div>` : '<div class="empty">Bu hesap herhangi bir şirkete üye değil.</div>'}</div><section class="panel" style="margin-top:18px"><div class="panel-head"><div><h3>Hesap yaşam çizelgesi</h3><p class="muted">Üyelik, demo, satın alma, ödeme ve güvenlik işlemleri</p></div></div><div id="memberTimeline"><div class="empty">Hesap hareketleri yükleniyor…</div></div></section>`;
      await loadMemberTimeline(id, 1, false);
      $('#memberDetail').scrollIntoView({ behavior: 'smooth', block: 'start' });
    } catch (error) {
      body.innerHTML = `<div class="empty">${esc(error.message)}</div>`;
    }
  }

  async function loadMemberTimeline(userId, page, append) {
    const host = $('#memberTimeline');
    if (!host) return;
    if (!append) host.innerHTML = '<div class="empty">Hesap hareketleri yükleniyor…</div>';
    const loadMore = host.querySelector('[data-load-more-timeline]');
    if (loadMore) loadMore.disabled = true;
    try {
      const result = await api(`/api/platform/members/${encodeURIComponent(userId)}/timeline?page=${page}&pageSize=50`);
      const rows = result.items.map((item) => {
        const amount = item.amount == null ? '' : ` · ${money(item.amount, item.currency)}`;
        const payment = item.paymentReference ? `<div class="sub">Ödeme referansı: ${esc(item.paymentReference)}</div>` : '';
        return `<li style="position:relative;padding:0 0 18px 20px;border-left:1px solid var(--line,#dbe3ef)"><span aria-hidden="true" style="position:absolute;left:-5px;top:5px;width:9px;height:9px;border-radius:50%;background:var(--accent,#2876ef)"></span><div style="display:flex;justify-content:space-between;gap:12px;flex-wrap:wrap"><strong>${esc(item.title)}${amount}</strong><time class="muted">${date(item.occurredAt)}</time></div><div class="muted">${esc(item.description)}${item.companyName ? ` · ${esc(item.companyName)}` : ''}</div>${payment}</li>`;
      }).join('');
      const existing = append ? host.querySelector('ol')?.innerHTML || '' : '';
      const note = result.truncated ? '<div class="notice warning">Çok eski hareketlerin bir bölümü bu görünümde sınırlandırıldı; ayrıntılı denetim kayıtları için Denetim bölümünü kullanın.</div>' : '';
      const timeline = existing || rows
        ? `<ol style="list-style:none;margin:14px 0 0;padding:0 0 0 8px">${existing}${rows}</ol>`
        : '<div class="empty">Henüz hesap hareketi yok.</div>';
      host.innerHTML = `${note}${timeline}${result.hasMore ? '<button type="button" class="btn small" data-load-more-timeline style="margin-top:8px">Daha eski hareketleri yükle</button>' : ''}`;
      const more = host.querySelector('[data-load-more-timeline]');
      if (more) more.addEventListener('click', () => loadMemberTimeline(userId, page + 1, true), { once: true });
    } catch (error) {
      host.innerHTML = `<div class="empty">Hesap hareketleri yüklenemedi: ${esc(error.message)}</div>`;
    }
  }

  function money(value, currency) {
    try { return new Intl.NumberFormat('tr-TR', { style: 'currency', currency: currency || 'TRY' }).format(value); }
    catch { return `${Number(value).toLocaleString('tr-TR')} ${esc(currency || 'TRY')}`; }
  }

  let companySearchRequest = 0;
  let companySearchTimer;
  async function loadInviteCompanies(query = '') {
    const request = ++companySearchRequest;
    const select = $('#memberInviteCompany');
    select.disabled = true;
    select.innerHTML = '<option value="">Şirketler aranıyor…</option>';
    try {
      const params = new URLSearchParams({ active: 'true', page: '1', pageSize: '200' });
      if (query.trim()) params.set('q', query.trim());
      const result = await api(`/api/platform/companies?${params}`);
      if (request !== companySearchRequest) return;
      select.innerHTML = result.items.length
        ? `<option value="">Şirket seçin</option>${result.items.map((company) => `<option value="${esc(company.id)}">${esc(company.name)}${company.slug ? ` · ${esc(company.slug)}` : ''}</option>`).join('')}`
        : '<option value="">Eşleşen etkin şirket yok</option>';
      if (result.total > result.items.length && !query.trim())
        $('#memberInviteFeedback').textContent = `İlk ${result.items.length} şirket gösteriliyor. Arama alanıyla şirketi bulun.`;
      select.disabled = result.items.length === 0;
    } catch (error) {
      if (request !== companySearchRequest) return;
      select.innerHTML = '<option value="">Şirket listesi yüklenemedi</option>';
      $('#memberInviteFeedback').textContent = error.message;
    }
  }

  function openMemberInvite() {
    const form = $('#memberInviteForm');
    form.reset();
    $('#memberInviteFeedback').textContent = '';
    $('#memberInviteDialog').showModal();
    loadInviteCompanies();
    form.elements.fullName.focus();
  }

  $('#nav').addEventListener('click', (event) => {
    if (event.target.closest('[data-view="members"]')) {
      loadMembers(1);
    }
  });
  $('#searchMembers').addEventListener('click', () => loadMembers(1));
  $('#memberSearch').addEventListener('keydown', (event) => { if (event.key === 'Enter') loadMembers(1); });
  $('#refreshMembers').addEventListener('click', () => loadMembers(state.page));
  $('#prevMembers').addEventListener('click', () => loadMembers(state.page - 1));
  $('#nextMembers').addEventListener('click', () => loadMembers(state.page + 1));
  $('#closeMemberDetail').addEventListener('click', () => { $('#memberDetail').hidden = true; });
  $('#membersTable').addEventListener('click', (event) => {
    const button = event.target.closest('[data-member-detail]');
    if (button) loadMemberDetail(button.dataset.memberDetail);
  });
  document.querySelectorAll('[data-go="members"]').forEach((button) => button.addEventListener('click', () => $('#nav [data-view="members"]').click()));
  $('#openMemberInvite').addEventListener('click', openMemberInvite);
  $('#closeMemberInvite').addEventListener('click', () => $('#memberInviteDialog').close());
  $('#cancelMemberInvite').addEventListener('click', () => $('#memberInviteDialog').close());
  $('#memberInviteCompanySearch').addEventListener('input', () => {
    clearTimeout(companySearchTimer);
    companySearchTimer = setTimeout(() => loadInviteCompanies($('#memberInviteCompanySearch').value), 220);
  });
  $('#memberInviteForm').addEventListener('submit', async (event) => {
    event.preventDefault();
    const form = event.currentTarget;
    const submit = $('#submitMemberInvite');
    submit.disabled = true;
    $('#memberInviteFeedback').textContent = 'Davet kaydediliyor…';
    try {
      const csrf = await api('/api/session/csrf');
      const result = await api('/api/platform/members/invite', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': csrf.requestToken },
        body: JSON.stringify({
          companyId: form.elements.companyId.value,
          fullName: form.elements.fullName.value,
          email: form.elements.email.value,
          roleKey: form.elements.roleKey.value
        })
      });
      $('#memberInviteDialog').close();
      $('#memberSearch').value = form.elements.email.value.trim();
      await loadMembers(1);
      const toast = $('#toast');
      toast.textContent = result.message;
      toast.classList.add('show');
      setTimeout(() => toast.classList.remove('show'), 3200);
    } catch (error) {
      $('#memberInviteFeedback').textContent = error.message;
    } finally { submit.disabled = false; }
  });
})();
