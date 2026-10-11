(() => {
  const supportedScreens = new Set([
    'overview', 'assetList', 'assignments', 'licenses', 'accessories', 'consumables',
    'components', 'employees', 'locations', 'maintenance', 'reports'
  ]);
  const messages = {
    tr: {
      title: 'Demo ekranı yakında',
      description: 'Bu alt menüye ait örnek ekran henüz hazırlanmadı.',
      hint: 'Yakında: Bu menüye özel demo ekranı hazırlanıyor. Hazır olana kadar başka bir modülün kayıtları gösterilmez.',
      column: 'Ekran durumu', status: 'Yakında', count: '0 örnek kayıt'
    },
    en: {
      title: 'Demo screen coming soon',
      description: 'A sample screen for this submenu is not ready yet.',
      hint: 'Coming soon: this submenu’s demo screen is being prepared. Until then, records from another module will not be shown.',
      column: 'Screen status', status: 'Coming soon', count: '0 sample records'
    },
    ru: {
      title: 'Демо-экран скоро появится',
      description: 'Пример экрана для этого подраздела ещё не готов.',
      hint: 'Скоро: готовится демо-экран для этого подраздела. До его появления здесь не будут показываться записи другого модуля.',
      column: 'Состояние экрана', status: 'Скоро', count: '0 примеров записей'
    },
    uk: {
      title: 'Демоекран незабаром з’явиться',
      description: 'Приклад екрана для цього підменю ще не готовий.',
      hint: 'Незабаром: готується демоекран для цього підменю. Поки він не готовий, записи інших модулів тут не відображатимуться.',
      column: 'Стан екрана', status: 'Незабаром', count: '0 прикладів записів'
    },
    mk: {
      title: 'Демо-екранот наскоро',
      description: 'Примерниот екран за ова подмени сè уште не е подготвен.',
      hint: 'Наскоро: се подготвува демо-екран за ова подмени. Дотогаш, тука нема да се прикажуваат записи од друг модул.',
      column: 'Статус на екранот', status: 'Наскоро', count: '0 примерни записи'
    }
  };

  let emptyScreenKey = null;
  const previousDisabledState = new WeakMap();
  const controls = ['#search', '#statusFilter', '#categoryFilter', '#exportButton']
    .map(selector => document.querySelector(selector)).filter(Boolean);
  const contextualSections = [
    document.querySelector('.page-actions'),
    document.querySelector('#activityRows')?.closest('.panel')
  ].filter(Boolean);

  function setControlsDisabled(disabled) {
    for (const control of controls) {
      if (disabled) {
        if (!previousDisabledState.has(control)) {
          previousDisabledState.set(control, {
            disabled: control.disabled,
            hidden: control.hidden,
            display: control.style.display
          });
        }
        control.disabled = true;
        control.hidden = true;
        control.style.display = 'none';
      } else if (previousDisabledState.has(control)) {
        const previousState = previousDisabledState.get(control);
        control.disabled = previousState.disabled;
        control.hidden = previousState.hidden;
        control.style.display = previousState.display;
        previousDisabledState.delete(control);
      }
    }
  }

  function setContextualSectionsHidden(hidden) {
    contextualSections.forEach(section => {
      section.hidden = hidden;
      section.style.display = hidden ? 'none' : '';
    });
  }

  function restoreScreen() {
    emptyScreenKey = null;
    delete document.body.dataset.demoEmptyNav;
    const stats = document.querySelector('#stats');
    if (stats) stats.hidden = false;
    setControlsDisabled(false);
    setContextualSectionsHidden(false);
  }

  function copyForCurrentLanguage() {
    const language = document.querySelector('#languageSelect')?.value || document.documentElement.lang || 'tr';
    return messages[language] || messages.tr;
  }

  function showEmptyScreen(button, key) {
    emptyScreenKey = key;
    const copy = copyForCurrentLanguage();
    const title = button.innerText.trim();
    document.body.dataset.demoEmptyNav = key;
    document.querySelectorAll('#demoNav [data-nav-item]').forEach(item => {
      item.classList.toggle('active', item === button);
      if (item === button) item.setAttribute('aria-current', 'page');
      else item.removeAttribute('aria-current');
    });
    document.querySelector('#pageTitle').textContent = title;
    document.querySelector('#crumbTitle').textContent = title;
    document.querySelector('#pageSubtitle').textContent = copy.hint;
    document.querySelector('#tableTitle').textContent = copy.title;
    document.querySelector('#tableHint').textContent = copy.hint;
    document.querySelector('#tableHead').innerHTML = `<tr><th>${copy.column}</th></tr>`;
    const row = document.createElement('tr');
    const cell = document.createElement('td');
    cell.className = 'empty';
    cell.colSpan = 1;
    cell.textContent = `${copy.description} ${copy.status}.`;
    row.append(cell);
    document.querySelector('#tableRows').replaceChildren(row);
    document.querySelector('#count').textContent = copy.count;
    document.querySelector('#search').value = '';
    document.querySelector('#statusFilter').hidden = true;
    document.querySelector('#categoryFilter').hidden = true;
    document.querySelector('#detailBackdrop')?.classList.remove('open');
    document.querySelector('#imageBackdrop')?.classList.remove('open');
    const stats = document.querySelector('#stats');
    stats.replaceChildren();
    stats.hidden = true;
    setControlsDisabled(true);
    setContextualSectionsHidden(true);
    document.querySelector('.sidebar')?.classList.remove('mobile-open');
    document.querySelector('#mobileMenuBackdrop')?.classList.remove('open');
    document.querySelector('#mobileMenuToggle')?.setAttribute('aria-expanded', 'false');
  }

  document.querySelector('#demoNav')?.addEventListener('click', event => {
    const button = event.target.closest('[data-view]');
    if (!button) return;
    const key = button.dataset.navExtra || button.dataset.navItem || button.dataset.view;
    if (supportedScreens.has(key)) {
      restoreScreen();
      return;
    }
    event.preventDefault();
    event.stopImmediatePropagation();
    showEmptyScreen(button, key);
  }, true);

  document.addEventListener('click', event => {
    if (!emptyScreenKey || !event.target.closest('[data-view-link]')) return;
    restoreScreen();
  }, true);

  document.querySelector('#languageSelect')?.addEventListener('change', () => {
    if (!emptyScreenKey) return;
    const button = document.querySelector(`#demoNav [data-nav-item="${CSS.escape(emptyScreenKey)}"]`);
    if (button) showEmptyScreen(button, emptyScreenKey);
  });
})();
