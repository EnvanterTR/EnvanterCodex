(() => {
  'use strict';

  const $ = (selector) => document.querySelector(selector);
  const dialog = $('#paymentFollowUpDialog');
  const form = $('#paymentFollowUpForm');
  const state = { orderId: null, csrfToken: null };
  const esc = (value) => String(value ?? '').replace(/[&<>"']/g, (character) => ({
    '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
  })[character]);
  const localDate = (value) => value ? new Intl.DateTimeFormat('tr-TR', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) : '—';
  const outcomeLabels = {
    Contacted: 'Müşteriyle iletişim kuruldu', NoResponse: 'Yanıt alınamadı',
    ReminderScheduled: 'Hatırlatma planlandı', CustomerRequestedCallback: 'Müşteri geri arama istedi', Other: 'Diğer'
  };

  async function api(path, options = {}) {
    const headers = new Headers(options.headers || {});
    if (options.method && options.method !== 'GET') {
      if (!state.csrfToken) {
        const csrfResponse = await fetch('/api/session/csrf', { credentials: 'same-origin', cache: 'no-store' });
        if (!csrfResponse.ok) throw new Error('İstek doğrulama bilgisi alınamadı.');
        state.csrfToken = (await csrfResponse.json()).requestToken;
      }
      headers.set('X-CSRF-TOKEN', state.csrfToken);
      headers.set('Content-Type', 'application/json');
    }
    const response = await fetch(path, { ...options, headers, credentials: 'same-origin', cache: 'no-store' });
    if (!response.ok) {
      let message = `İstek başarısız (${response.status}).`;
      try {
        const body = await response.json();
        message = body.message || body.title || Object.values(body.errors || {}).flat()?.[0] || message;
      } catch { /* Keep the generic status message. */ }
      throw new Error(message);
    }
    return response.status === 204 ? null : response.json();
  }

  async function loadHistory() {
    if (!state.orderId) return;
    $('#paymentFollowUpHistory').textContent = 'Takip geçmişi yükleniyor…';
    try {
      const data = await api(`/api/platform/checkout-orders/${encodeURIComponent(state.orderId)}/follow-ups`);
      $('#paymentFollowUpTitle').textContent = `${data.order.companyName} · başarısız ödeme takibi`;
      $('#paymentFollowUpHistory').innerHTML = data.history.length
        ? `<ol class="activity-list">${data.history.map((entry) => {
          let detail = {};
          try { detail = JSON.parse(entry.details || '{}'); } catch { /* Preserve a readable history if an old row is malformed. */ }
          return `<li><strong>${esc(outcomeLabels[detail.outcome] || detail.outcome || 'Takip kaydı')}</strong><span class="sub">${localDate(entry.occurredAt)} · ${esc(entry.actor || 'Yönetici')}</span><p>${esc(detail.note || '')}</p>${detail.nextFollowUpAt ? `<span class="sub">Sonraki takip: ${localDate(detail.nextFollowUpAt)}</span>` : ''}</li>`;
        }).join('')}</ol>`
        : '<div class="empty">Bu ödeme için henüz takip kaydı yok.</div>';
    } catch (error) {
      $('#paymentFollowUpHistory').innerHTML = `<div class="empty">${esc(error.message)}</div>`;
    }
  }

  $('#ordersTable').addEventListener('click', async (event) => {
    const button = event.target.closest('[data-payment-follow-up]');
    if (!button) return;
    state.orderId = button.dataset.paymentFollowUp;
    form.reset();
    $('#paymentFollowUpFeedback').textContent = '';
    dialog.showModal();
    await loadHistory();
  });

  const close = () => { if (dialog.open) dialog.close(); };
  $('#closePaymentFollowUp').addEventListener('click', close);
  $('#cancelPaymentFollowUp').addEventListener('click', close);
  dialog.addEventListener('close', () => { state.orderId = null; $('#paymentFollowUpFeedback').textContent = ''; });

  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    const submit = form.querySelector('[type="submit"]');
    if (submit.disabled || !state.orderId) return;
    submit.disabled = true;
    $('#paymentFollowUpFeedback').textContent = 'Takip kaydı ekleniyor…';
    const followUpValue = form.elements.nextFollowUpAt.value;
    try {
      await api(`/api/platform/checkout-orders/${encodeURIComponent(state.orderId)}/follow-ups`, {
        method: 'POST', body: JSON.stringify({
          outcome: form.elements.outcome.value,
          nextFollowUpAt: followUpValue ? new Date(followUpValue).toISOString() : null,
          note: form.elements.note.value.trim()
        })
      });
      form.reset();
      $('#paymentFollowUpFeedback').textContent = 'Dahili takip kaydı eklendi. E-posta gönderilmedi ve ödeme durumu değişmedi.';
      await loadHistory();
      $('#refreshOrders').click();
    } catch (error) {
      $('#paymentFollowUpFeedback').textContent = error.message;
    } finally { submit.disabled = false; }
  });
})();
