const fs=require('fs');
const logic=fs.readFileSync('hexa-arrows.html','utf8').split('//<logic>')[1].split('//</logic>')[0];
eval(logic);
const cfgs=[
 {name:'6renk-buz', cfg:{R:5,lengths:[3,4,5,6,6,7],minEdges:3,ice:3}},
 {name:'10renk-buz',cfg:{R:6,lengths:[3,4,5,6,7,7,8,8,9,10],minEdges:5,ice:3}},
];
let fails=0;
for(const {name,cfg} of cfgs){
  let fallbackLike=0;
  for(let s=1;s<=150;s++){
    const lvl=genLevel(s*4241+11,cfg);
    const N=lvl.arrows.length;
    // tam 3 buzlu, eşikler {1,2,3}
    const fr=lvl.arrows.filter(a=>a.freezeAt>0);
    const ths=fr.map(a=>a.freezeAt).sort().join(',');
    if(fr.length!==3||ths!=='1,2,3'){console.log(name,s,'buz ataması bozuk:',ths);fails++;}
    // buz kurallı tam çözüm simülasyonu (çözülmüş rotlarla, dinamik ışın kontrolü)
    for(const c of lvl.cells.values()) c.rot=0;
    lvl.arrows.forEach(a=>{a.exited=false;});
    let exited=0,guard=0;
    while(exited<N&&guard++<40){
      let prog=false;
      for(let c=0;c<N;c++){
        const a=lvl.arrows[c];
        if(a.exited) continue;
        if(a.freezeAt>exited) continue;                 // buz henüz kırılmadı
        const res=traceConnected(lvl,c);
        if(!res.connected){console.log(name,s,'trace koptu',c);fails++;break;}
        if(rayBlockers(lvl,c,res.headCell,res.exitDir).length===0){
          a.exited=true;a.cells.forEach(k=>lvl.cells.delete(k));exited++;prog=true;
        }
      }
      if(!prog){console.log(name,s,'BUZ DEADLOCK, çıkan:',exited);fails++;break;}
    }
    if(exited!==N&&guard>=40){console.log(name,s,'bitmedi');fails++;}
  }
  console.log(name+': tamam');
}
console.log(fails===0?'BUZ TESTLERİ GEÇTİ (300 seed)':'HATA: '+fails);
