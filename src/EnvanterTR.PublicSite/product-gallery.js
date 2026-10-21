(() => {
  'use strict';
  const copy={tr:['Tam ekran görüntüle','Kapat','Görselleri duraklat','Görselleri oynat','Görsel'],en:['View full screen','Close','Pause slideshow','Play slideshow','Image'],ru:['На весь экран','Закрыть','Приостановить','Продолжить','Изображение'],uk:['На весь екран','Закрити','Призупинити','Продовжити','Зображення'],mk:['Цел екран','Затвори','Паузирај','Продолжи','Слика']};
  const words=()=>copy[document.documentElement.lang]||copy.tr;
  const dialog=document.createElement('dialog');dialog.className='product-preview-dialog';
  const close=document.createElement('button');close.type='button';close.className='product-preview-close';
  const large=document.createElement('img');dialog.append(close,large);document.body.append(dialog);
  let opener;
  function open(image){opener=image;large.src=image.currentSrc||image.src;large.alt=image.alt;dialog.showModal();document.documentElement.classList.add('product-preview-open');close.focus();}
  close.addEventListener('click',()=>dialog.close());
  dialog.addEventListener('click',event=>{if(event.target===dialog)dialog.close();});
  dialog.addEventListener('close',()=>{document.documentElement.classList.remove('product-preview-open');large.removeAttribute('src');opener?.focus();});
  document.querySelectorAll('[data-preview-expand]').forEach(image=>{image.addEventListener('click',()=>open(image));image.addEventListener('keydown',event=>{if(event.key==='Enter'||event.key===' '){event.preventDefault();open(image);}});});
  const figure=document.querySelector('#assetLabelGallery');if(!figure)return;
  const original=figure.querySelector('img'),windowBox=document.createElement('div'),track=document.createElement('div');
  windowBox.className='label-gallery-window';track.className='label-gallery-track';original.before(windowBox);windowBox.append(track);track.append(original);
  const sources=[['asset-label-devices.png','QR ve barkod etiketlerinin farklı cihazlarda kullanım örnekleri'],['asset-label-hardware.png','Dizüstü bilgisayar, sunucu ve monitör envanter etiketleri'],['asset-label-desktop.png','Masaüstü bilgisayarda QR ve barkod envanter etiketi']];
  original.classList.add('label-gallery-slide');
  for(const [file,alt] of sources){const image=document.createElement('img');image.src='/assets/images/'+file;image.alt=alt;image.loading='lazy';image.decoding='async';image.className='label-gallery-slide';track.append(image);}
  const slides=[...track.children],clone=original.cloneNode(true);clone.alt='';clone.setAttribute('aria-hidden','true');track.append(clone);
  const controls=document.createElement('div');controls.className='label-gallery-controls';windowBox.after(controls);
  let index=0,paused=matchMedia('(prefers-reduced-motion: reduce)').matches,hover=false,focused=false,timer;
  const dots=slides.map((slide,i)=>{const button=document.createElement('button');button.type='button';button.textContent=String(i+1);button.addEventListener('click',()=>{index=i;move();schedule();});controls.append(button);return button;});
  const pause=document.createElement('button');pause.type='button';controls.append(pause);
  function labels(){const w=words();dialog.setAttribute('aria-label',w[0]);close.textContent=w[1]+' ×';document.querySelectorAll('[data-preview-expand]').forEach(image=>{image.setAttribute('aria-label',w[0]);image.title=w[0];});dots.forEach((dot,i)=>{dot.setAttribute('aria-label',w[4]+' '+(i+1));dot.setAttribute('aria-pressed',String(i===index%slides.length));});pause.textContent=paused?'▶':'Ⅱ';pause.setAttribute('aria-label',w[paused?3:2]);}
  function move(){track.style.transform=`translateX(-${index*100}%)`;slides.forEach((slide,i)=>slide.setAttribute('aria-hidden',String(i!==index%slides.length)));labels();}
  function schedule(){clearTimeout(timer);if(!paused&&!hover&&!focused&&!document.hidden)timer=setTimeout(()=>{index=matchMedia('(prefers-reduced-motion: reduce)').matches?(index+1)%slides.length:index+1;move();schedule();},5000);}
  track.addEventListener('transitionend',event=>{if(event.target!==track||index!==slides.length)return;track.style.transition='none';index=0;move();requestAnimationFrame(()=>requestAnimationFrame(()=>track.style.transition=''));});
  pause.addEventListener('click',()=>{paused=!paused;labels();schedule();});
  figure.addEventListener('mouseenter',()=>{hover=true;schedule();});figure.addEventListener('mouseleave',()=>{hover=false;schedule();});
  figure.addEventListener('focusin',()=>{focused=true;schedule();});figure.addEventListener('focusout',event=>{if(!figure.contains(event.relatedTarget)){focused=false;schedule();}});
  document.addEventListener('visibilitychange',schedule);
  new MutationObserver(labels).observe(document.documentElement,{attributes:true,attributeFilter:['lang']});
  move();schedule();
})();
