(() => {
  'use strict';
  window.EnvanterScheduledReports = {
    create(options) {
      const { api, t, generation, locale, language } = options;
      const root = document.createElement('dialog'); root.className = 'detail-dialog'; root.id = 'scheduledReportsDialog';
      root.setAttribute('aria-labelledby', 'scheduledReportsTitle'); root.style.width = 'min(1100px,calc(100% - 24px))';
      root.innerHTML = `<div class="dialog-head"><div><div class="eyebrow" data-loc="RAPORLAR / E-POSTA"></div><h2 id="scheduledReportsTitle" data-loc="Rapor takvimi"></h2></div><button class="close" type="button" data-close-schedules aria-label="Kapat">✕</button></div>
        <div class="dialog-body" style="max-height:78vh;overflow:auto"><p data-schedule-recipient class="muted"></p><p data-schedule-mail-status class="muted" role="status"></p>
        <p class="muted" data-loc="Özet ve CSV eki yalnızca kendi doğrulanmış e-posta adresinize gönderilir. Gönderim anındaki şirket ve erişim yetkileriniz kullanılır."></p>
        <div class="dialog-actions"><button type="button" class="btn" data-new-schedule data-loc="Yeni rapor takvimi"></button><button type="button" class="btn" data-refresh-schedules data-loc="Yenile"></button></div>
        <div class="table-wrap"><table><thead><tr><th data-loc="Rapor"></th><th data-loc="Zamanlama"></th><th data-loc="Sonraki gönderim"></th><th data-loc="Durum"></th><th data-loc="İşlemler"></th></tr></thead><tbody data-schedule-rows></tbody></table></div>
        <section data-schedule-preview hidden style="margin-top:20px" aria-labelledby="scheduledReportPreviewTitle"><h3 id="scheduledReportPreviewTitle" tabindex="-1"></h3><p class="muted" data-preview-note></p><div class="dialog-actions"><button type="button" class="btn" data-preview-csv data-loc="CSV indir"></button><button type="button" class="btn" data-preview-close data-loc="Kapat"></button></div><div class="table-wrap"><table><thead data-preview-head></thead><tbody data-preview-body></tbody></table></div></section>
        <form data-schedule-form style="margin-top:20px"><h3 data-edit-title></h3><fieldset class="form-grid" data-schedule-fields style="border:0;padding:0;margin:0">
        <label class="detail-field"><span data-loc="Rapor adı"></span><input name="name" maxlength="80" required></label>
        <label class="detail-field"><span data-loc="Rapor türü"></span><select name="reportKind"><option value="inventory" data-loc="Envanter özeti"></option><option value="maintenance" data-loc="Bakım özeti"></option><option value="licenses" data-loc="Lisans özeti"></option><option value="stock" data-loc="Stok özeti"></option></select></label>
        <label class="detail-field"><span data-loc="Gönderim sıklığı"></span><select name="frequency"><option value="daily" data-loc="Günlük"></option><option value="weekly" data-loc="Haftalık"></option><option value="monthly" data-loc="Aylık"></option></select></label>
        <label class="detail-field"><span data-loc="Yerel saat"></span><input type="time" name="time" required></label>
        <label class="detail-field"><span data-loc="Saat dilimi"></span><input name="timeZoneId" list="scheduledReportZones" maxlength="100" required><datalist id="scheduledReportZones"><option value="Europe/Istanbul"></option><option value="Europe/London"></option><option value="Europe/Berlin"></option><option value="Europe/Moscow"></option><option value="Europe/Kyiv"></option><option value="Europe/Skopje"></option><option value="America/New_York"></option><option value="UTC"></option></datalist></label>
        <label class="detail-field" data-weekday-field><span data-loc="Haftanın günü"></span><select name="weekDay"><option value="1" data-loc="Pazartesi"></option><option value="2" data-loc="Salı"></option><option value="3" data-loc="Çarşamba"></option><option value="4" data-loc="Perşembe"></option><option value="5" data-loc="Cuma"></option><option value="6" data-loc="Cumartesi"></option><option value="0" data-loc="Pazar"></option></select></label>
        <label class="detail-field" data-monthday-field><span data-loc="Ayın günü (1–28)"></span><input type="number" name="monthDay" min="1" max="28" value="1" required></label>
        <label class="detail-field"><span data-loc="Rapor dili"></span><select name="language"><option value="tr">Türkçe</option><option value="en">English</option><option value="ru">Русский</option><option value="uk">Українська</option><option value="mk">Македонски</option></select></label>
        <label class="detail-field"><span><input type="checkbox" name="isEnabled"> <span data-loc="Otomatik gönderimi aç"></span></span></label>
        </fieldset><div class="dialog-actions"><button class="btn" type="button" data-reset-schedule data-loc="Vazgeç"></button><button class="btn primary" type="submit" data-loc="Takvimi kaydet"></button></div></form>
        <p data-schedule-feedback class="muted" role="status" aria-live="polite"></p></div>`;
      document.body.append(root);
      const $ = selector => root.querySelector(selector), form = $('[data-schedule-form]'), body = $('[data-schedule-rows]');
      let rows = [], editing = null, pendingDelete = null, previewDocument = null, busy = false, sequence = 0, lifetime = 0, sessionGeneration = -1;
      const kindLabels = { inventory:'Envanter özeti', maintenance:'Bakım özeti', licenses:'Lisans özeti', stock:'Stok özeti' };
      const frequencyLabels = { daily:'Günlük', weekly:'Haftalık', monthly:'Aylık' };
      const statusLabels = { Idle:'Bekliyor', Sending:'Hazırlanıyor', Sent:'Gönderildi', Failed:'Başarısız', Skipped:'Atlandı' };
      const errors = { RecipientOrPermissionUnavailable:'Alıcı veya güncel yetki uygun değil.', DeliveryTimeout:'E-posta teslimi zaman aşımına uğradı.', SmtpDeliveryFailed:'E-posta sunucusu teslimi reddetti.', ReportDeliveryFailed:'Rapor hazırlanamadı veya teslim edilemedi.' };
      errors.InvalidSchedule='Takvim durduruldu. Saat dilimini ve gönderim tarihini düzenleyin.';
      const current = ticket => root.open && sessionGeneration === generation() && ticket === sequence;
      function localize() {
        root.querySelectorAll('[data-loc]').forEach(node => { node.textContent = t(node.dataset.loc); });
        $('[data-close-schedules]').setAttribute('aria-label',t('Kapat'));
      }
      function feedback(message) { $('[data-schedule-feedback]').textContent = t(message); }
      function clearPreview() { previewDocument=null;$('[data-schedule-preview]').hidden=true;$('[data-preview-head]').replaceChildren();$('[data-preview-body]').replaceChildren();$('#scheduledReportPreviewTitle').textContent='';$('[data-preview-note]').textContent=''; }
      function dayFields() { $('[data-weekday-field]').hidden = form.elements.frequency.value !== 'weekly'; $('[data-monthday-field]').hidden = form.elements.frequency.value !== 'monthly'; }
      function reset() {
        editing = null; form.reset(); form.elements.frequency.value = 'weekly'; form.elements.time.value = '09:00';
        form.elements.language.value = language(); form.elements.timeZoneId.value = Intl.DateTimeFormat().resolvedOptions().timeZone || 'Europe/Istanbul';
        $('[data-edit-title]').textContent = t('Yeni rapor takvimi'); dayFields();
      }
      function setBusy(value) {
        busy = value; $('[data-schedule-fields]').disabled = value;
        root.querySelectorAll('button:not([data-close-schedules])').forEach(button => {button.disabled=value;});
      }
      function date(value, zone) {
        if (!value) return '—';
        try { return new Intl.DateTimeFormat(locale(),{dateStyle:'short',timeStyle:'short',timeZone:zone}).format(new Date(value)); }
        catch { return new Date(value).toISOString(); }
      }
      function element(tag,text,parent) { const node=document.createElement(tag);if(text!=null)node.textContent=text;if(parent)parent.append(node);return node; }
      function action(host,key,label,id) {const button=element('button',t(label),host);button.type='button';button.className='btn small';button.dataset.action=key;button.dataset.id=id;button.disabled=busy;return button;}
      function render() {
        body.replaceChildren();
        if (!rows.length) { const cell=element('td',t('Henüz rapor takvimi yok.'),element('tr',null,body));cell.colSpan=5;return; }
        for(const row of rows) {
          const tr=element('tr',null,body), name=element('td',row.name,tr);
          element('small',t(kindLabels[row.reportKind]||row.reportKind),name).style.display='block';
          const timing=element('td',t(frequencyLabels[row.frequency]||row.frequency),tr);
          let detail=String(row.localHour).padStart(2,'0')+':'+String(row.localMinute).padStart(2,'0')+' · '+row.timeZoneId;
          if(row.frequency==='weekly')detail+=' · '+new Intl.DateTimeFormat(locale(),{weekday:'long',timeZone:'UTC'}).format(new Date(Date.UTC(2026,0,4+row.weekDay)));
          if(row.frequency==='monthly')detail+=' · '+t('Ayın günü')+': '+row.monthDay;
          element('small',detail,timing).style.display='block';
          element('td',row.isEnabled?date(row.nextDueAt,row.timeZoneId):t('Kapalı'),tr);
          const state=element('td',row.isEnabled?t(statusLabels[row.lastStatus]||row.lastStatus):t('Kapalı'),tr);
          if(row.lastSentAt)element('small',t('Son gönderim')+': '+date(row.lastSentAt,row.timeZoneId),state).style.display='block';
          if(row.isEnabled&&row.retryAfter)element('small',t('Sonraki deneme')+': '+date(row.retryAfter,row.timeZoneId),state).style.display='block';
          if(row.lastErrorCode)element('small',t(errors[row.lastErrorCode]||row.lastErrorCode),state).style.display='block';
          const actions=element('td',null,tr);
          if(pendingDelete===row.id){action(actions,'confirm-delete','Silmeyi onayla',row.id);action(actions,'cancel-delete','Vazgeç',row.id);}
          else {action(actions,'preview','Raporu önizle',row.id);action(actions,'edit','Düzenle',row.id);action(actions,'toggle',row.isEnabled?'Durdur':'Etkinleştir',row.id);action(actions,'ask-delete','Sil',row.id);}
        }
      }
      async function load() {
        clearPreview();
        const ticket=++sequence;
        const result=await api('/api/reports/schedules');
        if(!current(ticket))return;
        rows=result.items||[];
        $('[data-schedule-recipient]').textContent=t('Alıcı: {email}').replace('{email}',result.emailAddress||'—');
        $('[data-schedule-mail-status]').textContent=!result.emailConfirmed?t('E-posta adresiniz doğrulanmadan gönderim açılamaz.'):
          !result.smtpConfigured?t('E-posta sunucusu yapılandırılmamış. Takvimleri kapalı olarak kaydedebilirsiniz.'):t('E-posta gönderimi yapılandırılmış.');
        form.elements.reportKind.querySelector('[value="maintenance"]').disabled=!result.maintenanceEnabled;
        render();
      }
      function payload(row) {return {name:row.name,reportKind:row.reportKind,frequency:row.frequency,timeZoneId:row.timeZoneId,
        localHour:row.localHour,localMinute:row.localMinute,weekDay:row.weekDay,monthDay:row.monthDay,language:row.language,isEnabled:row.isEnabled,version:row.version};}
      async function mutate(operation,success) {
        if(busy)return;
        setBusy(true); const expectedGeneration=sessionGeneration, expectedLifetime=lifetime;
        try {await operation();if(!root.open||expectedGeneration!==generation()||expectedLifetime!==lifetime)return;reset();pendingDelete=null;await load();if(expectedLifetime===lifetime)feedback(success);}
        catch(error){if(root.open&&expectedGeneration===generation()&&expectedLifetime===lifetime)feedback(error.message);}
        finally{if(expectedLifetime===lifetime)setBusy(false);}
      }
      async function preview(row) {
        if(busy)return;
        clearPreview();setBusy(true);const expectedLifetime=lifetime,ticket=++sequence;
        try {
          const document=await api('/api/reports/schedules/'+row.id+'/preview');
          if(!current(ticket)||expectedLifetime!==lifetime)return;
          previewDocument=document;
          $('#scheduledReportPreviewTitle').textContent=document.name;
          $('[data-preview-note]').textContent=t('Güncel veriler gösteriliyor. Bu işlem e-posta göndermez.')+' · '+date(document.generatedAt,row.timeZoneId);
          const head=element('tr',null,$('[data-preview-head]'));
          for(const title of document.headers){const th=element('th',title,head);th.scope='col';}
          for(const values of document.rows){const tr=element('tr',null,$('[data-preview-body]'));for(const value of values)element('td',value==null?'—':String(value),tr);}
          $('[data-schedule-preview]').hidden=false;$('#scheduledReportPreviewTitle').focus();feedback('');
        } catch(error){if(current(ticket)&&expectedLifetime===lifetime)feedback(error.message);}
        finally{if(expectedLifetime===lifetime)setBusy(false);}
      }
      body.addEventListener('click',event=>{
        const button=event.target.closest('button[data-action]');if(!button||busy)return;
        const row=rows.find(item=>item.id===button.dataset.id);if(!row)return;
        switch(button.dataset.action){
          case 'preview':void preview(row);break;
          case 'edit':
            editing=row;for(const key of ['name','reportKind','frequency','timeZoneId','weekDay','monthDay','language'])form.elements[key].value=row[key];
            form.elements.time.value=String(row.localHour).padStart(2,'0')+':'+String(row.localMinute).padStart(2,'0');form.elements.isEnabled.checked=row.isEnabled;
            $('[data-edit-title]').textContent=t('Rapor takvimini düzenle');dayFields();form.elements.name.focus();break;
          case 'toggle':{const data=payload(row);data.isEnabled=!row.isEnabled;void mutate(()=>api('/api/reports/schedules/'+row.id,{method:'PUT',body:JSON.stringify(data)}),'Rapor takvimi güncellendi.');break;}
          case 'ask-delete':pendingDelete=row.id;render();break;
          case 'cancel-delete':pendingDelete=null;render();break;
          case 'confirm-delete':void mutate(()=>api('/api/reports/schedules/'+row.id+'?version='+encodeURIComponent(row.version),{method:'DELETE'}),'Rapor takvimi silindi.');break;
        }
      });
      form.addEventListener('submit',event=>{
        event.preventDefault();if(busy||!form.reportValidity())return;
        const [localHour,localMinute]=form.elements.time.value.split(':').map(Number);
        const data={name:form.elements.name.value.trim(),reportKind:form.elements.reportKind.value,frequency:form.elements.frequency.value,timeZoneId:form.elements.timeZoneId.value.trim(),
          localHour,localMinute,weekDay:Number(form.elements.weekDay.value),monthDay:Number(form.elements.monthDay.value),language:form.elements.language.value,isEnabled:form.elements.isEnabled.checked,version:editing?.version||null};
        const id=editing?.id;
        void mutate(()=>api('/api/reports/schedules'+(id?'/'+id:''),{method:id?'PUT':'POST',body:JSON.stringify(data)}),'Rapor takvimi kaydedildi.');
      });
      form.elements.frequency.addEventListener('change',dayFields);
      $('[data-preview-close]').addEventListener('click',()=>{clearPreview();$('[data-new-schedule]').focus();});
      $('[data-preview-csv]').addEventListener('click',()=>{
        if(!previewDocument||busy||sessionGeneration!==generation())return;
        const cell=value=>{let text=value==null?'':String(value);if(/^[=+\-@]/.test(text.trimStart()))text="'"+text;return '"'+text.replaceAll('"','""')+'"';};
        const csv='\uFEFF'+[previewDocument.headers,...previewDocument.rows].map(row=>row.map(cell).join(';')).join('\r\n');
        const url=URL.createObjectURL(new Blob([csv],{type:'text/csv;charset=utf-8'})),link=document.createElement('a');
        link.href=url;link.download='report-'+previewDocument.id+'.csv';document.body.append(link);link.click();link.remove();setTimeout(()=>URL.revokeObjectURL(url),1000);
      });
      $('[data-close-schedules]').addEventListener('click',()=>root.close());
      $('[data-new-schedule]').addEventListener('click',()=>{reset();feedback('');form.elements.name.focus();});
      $('[data-reset-schedule]').addEventListener('click',()=>{reset();feedback('');});
      $('[data-refresh-schedules]').addEventListener('click',()=>{if(!busy)void load().catch(error=>feedback(error.message));});
      root.addEventListener('close',()=>{sequence++;lifetime++;rows=[];editing=null;pendingDelete=null;clearPreview();body.replaceChildren();form.reset();$('[data-schedule-recipient]').textContent='';$('[data-schedule-mail-status]').textContent='';feedback('');});
      return {open(){if(root.open)return;sessionGeneration=generation();const opening=++lifetime;setBusy(false);localize();reset();body.replaceChildren();feedback('Yükleniyor…');root.showModal();void load().then(()=>{if(root.open&&opening===lifetime)feedback('');}).catch(error=>{if(root.open&&opening===lifetime)feedback(error.message);});}};
    }
  };
})();
