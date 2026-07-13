const fs=require('fs');
const logic=fs.readFileSync('hexa-arrows.html','utf8').split('//<logic>')[1].split('//</logic>')[0];
eval(logic);
const DIRS2=[[1,0],[0,1],[-1,1],[-1,0],[0,-1],[1,-1]];
const cfgs=[{R:3,lengths:[3,3,4],minEdges:1},{R:5,lengths:[3,4,5,6,7,7,8],minEdges:3},{R:6,lengths:[3,4,5,6,7,7,8,8,9,10],minEdges:5}];
let bad=0;
for(const cfg of cfgs){
  for(let s=1;s<=200;s++){
    const lvl=genLevel(s*7717+3,cfg);
    // toplam hücre = uzunluk toplamı
    const totalLen=cfg.lengths.reduce((a,b)=>a+b,0);
    if(lvl.cells.size!==totalLen){console.log('hücre sayısı yanlış',lvl.cells.size,totalLen);bad++;}
    for(const arr of lvl.arrows){
      const cs=arr.cells.map(k=>lvl.cells.get(k));
      // her ok: tam 1 tail (başta), tam 1 head (sonda), arası mid, hepsi aynı renk
      if(cs.some(c=>!c)){console.log('eksik hücre',arr.color);bad++;continue;}
      if(cs[0].type!=='tail'){console.log('tail yok!',arr.color);bad++;}
      if(cs[cs.length-1].type!=='head'){console.log('head yok!',arr.color);bad++;}
      if(cs.slice(1,-1).some(c=>c.type!=='mid')){console.log('ara segment mid değil',arr.color);bad++;}
      if(cs.some(c=>c.color!==arr.color)){console.log('renk tutarsız!',arr.color);bad++;}
      if(cs.length!==arr.len){console.log('uzunluk tutarsız',arr.color);bad++;}
      if(cs.filter(c=>c.type==='tail').length!==1||cs.filter(c=>c.type==='head').length!==1){console.log('tail/head sayısı != 1');bad++;}
    }
  }
}
console.log(bad===0?'600 seed × 3 zorluk: her okta tail+mid...+head yapısı sağlam':'YAPISAL HATA: '+bad);
// kanıt görseli için bir level dök
const lvl=genLevel(20260707,{R:5,lengths:[3,4,5,6,7,7,8],minEdges:3});
const dump={cells:[...lvl.cells.values()],arrows:lvl.arrows.map(a=>({color:a.color,len:a.len}))};
fs.writeFileSync('level_dump.json',JSON.stringify(dump));
console.log('örnek level dump edildi:',dump.cells.length,'hücre');
