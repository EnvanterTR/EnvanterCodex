(() => {
  const supportedScreens = new Set([
    'overview', 'assetList', 'assignments', 'licenses', 'accessories', 'consumables',
    'components', 'employees', 'locations', 'maintenance', 'reports'
  ]);
  const messages = {
    tr: {
      title: 'Bu demo ekranında örnek kayıt yok',
      description: 'Bu alt menü için örnek kayıtlar henüz eklenmedi. Bu ekranın kendi verileri hazırlanıyor.',
      hint: 'Bu başlık altında başka bir modülün kayıtları gösterilmez.',
      column: 'Ekran durumu', status: 'Demo içeriği hazırlanıyor', count: '0 kayıt',
      cards: [['Ekran', 'Alt menü'], ['İçerik', 'Henüz eklenmedi'], ['Veri', 'Örnek'], ['İşlemler', 'Salt okunur']]
    },
    en: {
      title: 'No sample records for this demo screen',
      description: 'Sample records for this submenu have not been added yet. Its own screen data is being prepared.',
      hint: 'Records from another module are not shown under this heading.',
      column: 'Screen status', status: 'Demo content is being prepared', count: '0 records',
      cards: [['Screen', 'Submenu'], ['Content', 'Not added'], ['Data', 'Sample'], ['Actions', 'Read-only']]
    },
    ru: {
      title: 'В демо-экране нет примеров записей',
      description: 'Примеры записей для этого подменю ещё не добавлены. Данные для этого экрана готовятся.',
      hint: 'Под этим заголовком не показываются записи другого модуля.',
      column: 'Состояние экрана', status: 'Демо-данные готовятся', count: '0 записей',
      cards: [['Экран', 'Подменю'], ['Содержимое', 'Не добавлено'], ['Данные', 'Пример'], ['Действия', 'Только чтение']]
    },
    uk: {
      title: 'У цьому демо-екрані немає прикладів записів',
      description: 'Приклади записів для цього підменю ще не додано. Дані для цього екрана готуються.',
      hint: 'Під цим заголовком не відображаються записи іншого модуля.',
      column: 'Стан екрана', status: 'Демо-вміст готується', count: '0 записів',
      cards: [['Екран', 'Підменю'], ['Вміст', 'Не додано'], ['Дані', 'Приклад'], ['Дії', 'Лише читання']]
    },
    mk: {
      title: 'Нема примерни записи за овој демо-екран',
      description: 'Примерните записи за ова подмени сè уште не се додадени. Податоците за екранот се подготвуваат.',
      hint: 'Под овој наслов не се прикажуваат записи од друг модул.',
      column: 'Статус на екранот', status: 'Демо-содржината се подготвува', count: '0 записи',
      cards: [['Екран', 'Подмени'], ['Содржина', 'Не е додадена'], ['Податоци', 'Пример'], ['Дејства', 'Само за читање']]
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
    stats.replaceChildren(...copy.cards.map(([label, value]) => {
      const card = document.createElement('article');
      card.className = 'stat';
      const heading = document.createElement('label');
      const content = document.createElement('strong');
      const note = document.createElement('small');
      heading.textContent = label;
      content.textContent = value;
      note.textContent = copy.status;
      card.append(heading, content, note);
      return card;
    }));
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
      emptyScreenKey = null;
      delete document.body.dataset.demoEmptyNav;
      setControlsDisabled(false);
      setContextualSectionsHidden(false);
      return;
    }
    event.preventDefault();
    event.stopImmediatePropagation();
    showEmptyScreen(button, key);
  }, true);

  document.addEventListener('click', event => {
    if (!emptyScreenKey || !event.target.closest('[data-view-link]')) return;
    emptyScreenKey = null;
    delete document.body.dataset.demoEmptyNav;
    setControlsDisabled(false);
    setContextualSectionsHidden(false);
  }, true);

  document.querySelector('#languageSelect')?.addEventListener('change', () => {
    if (!emptyScreenKey) return;
    const button = document.querySelector(`#demoNav [data-nav-item="${CSS.escape(emptyScreenKey)}"]`);
    if (button) showEmptyScreen(button, emptyScreenKey);
  });
})();
