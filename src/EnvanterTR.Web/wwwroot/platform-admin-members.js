(() => {
  'use strict';
  const $ = (selector) => document.querySelector(selector);
  const state = { page: 1, pageSize: 50, total: 0, loading: false };
  const esc = (value) => String(value ?? '').replace(/[&<>"']/g, (character) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[character]);
  const date = (value) => value ? new Intl.DateTimeFormat('tr-TR', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) : '—';
  const roleLabel = (value) => ({ owner: 'Şirket sahibi', admin: 'Yönetici', manager: 'Envanter yöneticisi', auditor: 'Denetçi', viewer: 'Salt okunur' })[value] || value || '—';
  const api = async (url) => {
    const response = await fetch(url, { credentials: 'same-origin', headers: { Accept: 'application/json' } });
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
      body.innerHTML = `<div class="cards" style="grid-template-columns:repeat(auto-fit,minmax(190px,1fr));margin:0">${details.map(([label, value]) => `<div class="card"><div class="card-top">${esc(label)}</div><strong style="font-size:1rem">${esc(value)}</strong></div>`).join('')}</div><div><h3>Şirket üyelikleri</h3>${result.membershipsTruncated ? '<div class="notice warning">Güvenli görüntüleme sınırı nedeniyle üyeliklerin yalnızca ilk 200 kaydı gösteriliyor.</div>' : ''}${memberships ? `<div class="table-wrap"><table><thead><tr><th>Şirket</th><th>Rol</th><th>Üyelik tarihi</th><th>Plan ve dönem</th></tr></thead><tbody>${memberships}</tbody></table></div>` : '<div class="empty">Bu hesap herhangi bir şirkete üye değil.</div>'}</div>`;
      $('#memberDetail').scrollIntoView({ behavior: 'smooth', block: 'start' });
    } catch (error) {
      body.innerHTML = `<div class="empty">${esc(error.message)}</div>`;
    }
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
})();
