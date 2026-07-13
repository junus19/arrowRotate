const fs=require('fs');
const html=fs.readFileSync('hexa-arrows.html','utf8');
const logic=html.split('//<logic>')[1].split('//</logic>')[0];
eval(logic);
const CFGS=[
  {name:'giriş', cfg:{R:3,lengths:[3,3,4],minEdges:1}},
  {name:'orta',  cfg:{R:5,lengths:[3,4,5,6,7,7,8],minEdges:3}},
  {name:'büyük', cfg:{R:6,lengths:[3,4,5,6,7,7,8,8,9,10],minEdges:5}},
];
let fails=0;
for(const {name,cfg} of CFGS){
  let edgeSum=0;
  for(let s=1;s<=40;s++){
    const lvl=genLevel(s*12345+7,cfg);
    const N=lvl.arrows.length;
    lvl.arrows.forEach(a=>a.exited=false);
    for(let c=0;c<N;c++) if(traceConnected(lvl,c).connected){console.log(name,s,'baştan bağlı',c);fails++;}
    for(const cell of lvl.cells.values()) cell.rot=0;
    for(let c=0;c<N;c++) if(!traceConnected(lvl,c).connected){console.log(name,s,'çözümde kopuk',c);fails++;}
    let exited=0,guard=0;
    while(exited<N&&guard++<40){
      let prog=false;
      for(let c=0;c<N;c++){
        const a=lvl.arrows[c]; if(a.exited)continue;
        const res=traceConnected(lvl,c);
        if(!res.connected){console.log(name,s,'trace koptu',c);fails++;break;}
        if(rayBlockers(lvl,c,res.headCell,res.exitDir).length===0){
          a.exited=true;a.cells.forEach(k=>lvl.cells.delete(k));exited++;prog=true;
        }
      }
      if(!prog){console.log(name,s,'DEADLOCK');fails++;break;}
    }
    edgeSum+=lvl.edgeCount;
  }
  console.log(name+': ok, ort. bağımlılık='+(edgeSum/40).toFixed(1));
}
console.log(fails===0?'TÜM TESTLER GEÇTİ':'HATA: '+fails);
