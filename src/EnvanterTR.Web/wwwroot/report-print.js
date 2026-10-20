(() => {
  'use strict';
  const maximumRows = 50000;

  async function readCsv(response, isCurrent) {
    const reader = response.body.getReader(), decoder = new TextDecoder('utf-8');
    const parts = []; let bytes = 0, complete = false;
    try {
      while (isCurrent()) {
        const result = await new Promise((resolve, reject) => {
          const timeout = setTimeout(() => reject(new Error('Rapor dosyası tamamlanmadı; tekrar deneyin.')), 60000);
          reader.read().then(value => { clearTimeout(timeout); resolve(value); }, error => { clearTimeout(timeout); reject(error); });
        });
        if (result.done) { complete = true; break; }
        bytes += result.value.byteLength;
        if (bytes > 32 * 1024 * 1024) throw new Error('Rapor çok büyük; arama veya filtreleri daraltın.');
        parts.push(decoder.decode(result.value, { stream: true }));
      }
      if (!complete) return null;
      parts.push(decoder.decode());
      return parseCsv(parts.join(''));
    } finally {
      if (!complete) { try { await reader.cancel(); } catch {} }
      reader.releaseLock();
    }
  }

  function parseCsv(source) {
    const text = String(source).replace(/^\uFEFF/, '');
    if (text.length > 32 * 1024 * 1024) throw new Error('Rapor çok büyük; arama veya filtreleri daraltın.');
    // Delimiter detection ignores punctuation inside a quoted header.
    let quoted = false, semicolons = 0, commas = 0;
    for (let index = 0; index < text.length; index++) {
      const char = text[index];
      if (char === '"') {
        if (quoted && text[index + 1] === '"') { index++; continue; }
        quoted = !quoted;
      }
      if (!quoted && (char === '\r' || char === '\n')) break;
      if (!quoted && char === ';') semicolons++;
      if (!quoted && char === ',') commas++;
    }
    const delimiter = semicolons >= commas ? ';' : ',';
    const records = []; let row = [], cell = ''; quoted = false;
    const finishRow = () => {
      row.push(cell); cell = '';
      if (row.some(value => value !== '')) records.push(row);
      row = [];
      if (records.length > maximumRows + 1) throw new Error('Yazdırma en fazla 50.000 kayıt içerir; filtreleri daraltın.');
    };
    for (let index = 0; index < text.length; index++) {
      const char = text[index];
      if (char === '"') {
        if (quoted && text[index + 1] === '"') { cell += '"'; index++; }
        else quoted = !quoted;
      } else if (!quoted && char === delimiter) { row.push(cell); cell = ''; }
      else if (!quoted && (char === '\r' || char === '\n')) {
        if (char === '\r' && text[index + 1] === '\n') index++;
        finishRow();
      } else cell += char;
    }
    if (quoted) throw new Error('Rapor dosyası tamamlanmadı; tekrar deneyin.');
    if (cell !== '' || row.length) finishRow();
    const headers = records.shift() || [];
    if (!headers.length || headers.length > 100 || records.some(record => record.length !== headers.length))
      throw new Error('Rapor sütunları okunamadı.');
    return { headers, rows: records };
  }

  function open(options) {
    const popup = window.open('', '_blank');
    if (!popup) throw new Error(options.labels.popupBlocked);
    popup.opener = null;
    const doc = popup.document;
    doc.open(); doc.write('<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head><body></body></html>'); doc.close();
    doc.documentElement.lang = options.language || 'tr';
    doc.title = options.title;
    const labels = options.labels;
    function element(tag, text, parent) {
      const node = doc.createElement(tag);
      if (text != null) node.textContent = text;
      if (parent) parent.append(node);
      return node;
    }
    const style = element('style', null, doc.head);
    const fontUrl = new URL('/assets/fonts/InterVariable.woff2', location.origin).href;
    style.textContent = `@font-face{font-family:Inter;src:url("${fontUrl}") format("woff2")}*{box-sizing:border-box}body{margin:0;background:#e9eef5;color:#172033;font:12px Inter,Arial,sans-serif}.tools{position:sticky;top:0;z-index:1;display:flex;align-items:center;gap:12px;flex-wrap:wrap;padding:16px;background:#11243e;color:#fff}button,select{font:inherit;border:1px solid #b6c8df;border-radius:6px;padding:9px 14px}button{cursor:pointer;background:#fff;color:#172033}button.primary{background:#2870dc;border-color:#2870dc;color:white}button:disabled{opacity:.55;cursor:wait}.tools label{display:flex;align-items:center;gap:7px}.tools p{width:100%;margin:0;line-height:1.5}.paper{background:#fff;margin:20px auto;padding:24px;max-width:1500px;box-shadow:0 4px 18px #14284520}.brand{color:#245da8;font-weight:700;font-size:14px}h1{font-size:22px;margin:12px 0}.metadata{color:#56667d;white-space:pre-line;line-height:1.7}.status{padding:12px 0;color:#56667d}.error{color:#b42318}table{width:100%;border-collapse:collapse;font-size:10px}th,td{padding:7px 6px;border:1px solid #ced7e4;vertical-align:top;text-align:left;overflow-wrap:anywhere;white-space:pre-wrap}th{background:#eaf0f8;font-weight:700}thead{display:table-header-group}tr{break-inside:avoid}footer{margin-top:14px;color:#56667d}@media print{body{background:white}.tools,.status{display:none!important}.paper{box-shadow:none;margin:0;padding:0;max-width:none}.brand{-webkit-print-color-adjust:exact;print-color-adjust:exact}h1{font-size:16pt}table{font-size:8pt}th,td{padding:4px}th{-webkit-print-color-adjust:exact;print-color-adjust:exact}}`;
    const pageStyle = element('style', '@page{size:A4 landscape;margin:12mm}', doc.head);
    const tools = element('div', null, doc.body); tools.className = 'tools';
    const printButton = element('button', labels.print, tools); printButton.type = 'button'; printButton.className = 'primary'; printButton.disabled = true;
    const paperLabel = element('label', labels.paperSize, tools), paper = element('select', null, paperLabel);
    for (const value of ['A4', 'A3']) { const option = element('option', value, paper); option.value = value; }
    const orientationLabel = element('label', labels.orientation, tools), orientation = element('select', null, orientationLabel);
    for (const [value, title] of [['landscape', labels.landscape], ['portrait', labels.portrait]]) {
      const option = element('option', title, orientation); option.value = value;
    }
    const updatePage = () => { pageStyle.textContent = `@page{size:${paper.value} ${orientation.value};margin:12mm}`; };
    paper.addEventListener('change', updatePage); orientation.addEventListener('change', updatePage);
    const close = element('button', labels.close, tools); close.type = 'button'; close.addEventListener('click', () => popup.close());
    element('p', labels.pdfHint, tools);
    const main = element('main', null, doc.body); main.className = 'paper';
    element('div', options.company || 'EnvanterTR', main).className = 'brand';
    element('h1', options.title, main);
    element('p', options.metadata || '', main).className = 'metadata';
    const status = element('p', labels.loading, main); status.className = 'status'; status.setAttribute('role', 'status');
    const table = element('table', null, main); table.hidden = true;
    const footer = element('footer', '', main);
    printButton.addEventListener('click', async () => {
      if (printButton.disabled) return;
      await doc.fonts.ready;
      if (!options.isCurrent()) { popup.close(); return; }
      if (!popup.closed) { popup.focus(); popup.print(); }
    });
    return {
      get closed() { return popup.closed; },
      close() { if (!popup.closed) popup.close(); },
      error(message) {
        if (popup.closed) return;
        table.hidden = true; table.replaceChildren(); printButton.disabled = true;
        status.classList.add('error'); status.textContent = message;
      },
      async setRows(headers, rows) {
        if (popup.closed) return;
        if (!options.isCurrent()) { popup.close(); return; }
        if (!headers.length || rows.length > maximumRows) throw new Error(labels.tooLarge);
        const thead = element('thead', null, table), headRow = element('tr', null, thead);
        for (const name of headers) { const th = element('th', name, headRow); th.scope = 'col'; }
        const body = element('tbody', null, table);
        for (let offset = 0; offset < rows.length; offset += 250) {
          if (popup.closed) return;
          if (!options.isCurrent()) { popup.close(); return; }
          const fragment = doc.createDocumentFragment();
          for (const values of rows.slice(offset, offset + 250)) {
            if (values.length > headers.length) throw new Error('Rapor sütunları okunamadı.');
            const tr = element('tr', null, fragment);
            for (let index = 0; index < headers.length; index++) element('td', String(values[index] ?? ''), tr);
          }
          body.append(fragment);
          // Yield between batches so closing/canceling the preview stays responsive.
          await new Promise(resolve => setTimeout(resolve, 0));
        }
        status.textContent = labels.ready.replace('{count}', new Intl.NumberFormat(options.locale || 'tr-TR').format(rows.length));
        footer.textContent = new Intl.DateTimeFormat(options.locale || 'tr-TR', { dateStyle: 'long', timeStyle: 'short' }).format(new Date());
        table.hidden = false; printButton.disabled = false;
      }
    };
  }

  window.EnvanterReportPrint = Object.freeze({ open, readCsv });
})();
