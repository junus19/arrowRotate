const fs=require('fs');
const logic=fs.readFileSync('hexa-arrows.html','utf8').split('//<logic>')[1].split('//</logic>')[0];
eval(logic);
const DIRS2=[[1,0],[0,1],[-1,1],[-1,0],[0,-1],[1,-1]];
const cfg={R:7,lengths:[3,3,4,4,5,5,6,6,7,7,8,8,9,10],minEdges:6,ice:3,colors:10};
let fails=0, reuseTotal=0;
for(let s=1;s<=200;s++){
  const lvl=genLevel(s*6367+29,cfg);
  const M=lvl.arrows.length;
  if(M!==14){console.log(s,'ok sayısı',M);fails++;}
  // palet atanmış mı, sınırda mı
  for(const a of lvl.arrows) if(!(a.palette>=0&&a.palette<10)){console.log(s,'palet yok/sınır dışı',a.color,a.palette);fails++;}
  // KRİTİK: aynı paletteki iki ok hiçbir hücrede komşu olmamalı
  const owner=new Map();
  for(const [k,c] of lvl.cells) owner.set(k,c.color);
  for(const [k,c] of lvl.cells){
    const [q,r]=k.split(',').map(Number);
    for(const d of DIRS2){
      const o=owner.get((q+d[0])+','+(r+d[1]));
      if(o!==undefined && o!==c.color &&
         lvl.arrows[o].palette===lvl.arrows[c.color].palette){
        console.log(s,'AYNI PALET DEĞİYOR! oklar:',c.color,o,'palet',lvl.arrows[o].palette);
        fails++;
      }
    }
  }
  // kaç palet tekrar kullanılmış (bilgi)
  const cnt={}; lvl.arrows.forEach(a=>cnt[a.palette]=(cnt[a.palette]||0)+1);
  reuseTotal+=Object.values(cnt).filter(n=>n>1).length;
  // buz kurallı tam çözüm (ok bazında)
  for(const c of lvl.cells.values()) c.rot=0;
  lvl.arrows.forEach(a=>a.exited=false);
  let exited=0,guard=0;
  while(exited<M&&guard++<50){
    let prog=false;
    for(let c=0;c<M;c++){
      const a=lvl.arrows[c];
      if(a.exited||a.freezeAt>exited) continue;
      const res=traceConnected(lvl,c);
      if(!res.connected){console.log(s,'trace koptu',c);fails++;break;}
      if(rayBlockers(lvl,c,res.headCell,res.exitDir).length===0){
        a.exited=true;a.cells.forEach(k=>lvl.cells.delete(k));exited++;prog=true;
      }
    }
    if(!prog){console.log(s,'DEADLOCK');fails++;break;}
  }
  if(exited!==M){console.log(s,'bitmedi',exited);fails++;}
}
console.log('ort. tekrar kullanılan palet sayısı/level:',(reuseTotal/200).toFixed(1));
console.log(fails===0?'PALET TESTLERİ GEÇTİ (200 seed, 14 ok/10 renk)':'HATA: '+fails);
