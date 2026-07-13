const fs=require('fs');
const html=fs.readFileSync('hexa-arrows.html','utf8');
const logic=html.split('//<logic>')[1].split('//</logic>')[0];
eval(logic);
const DIRS=[[1,0],[0,1],[-1,1],[-1,0],[0,-1],[1,-1]];
const cfg={R:5,lengths:[3,4,5,6,7,7,8],minEdges:3}; // level 3-4
let bad=0;
for(let s=1;s<=300;s++){
  const lvl=genLevel(s*991+13,cfg);
  for(const arr of lvl.arrows){
    // yol sıralı hücrelerden oluşuyor mu: ardışık her çift komşu olmalı
    for(let i=0;i+1<arr.cells.length;i++){
      const [q1,r1]=arr.cells[i].split(',').map(Number);
      const [q2,r2]=arr.cells[i+1].split(',').map(Number);
      const adj=DIRS.some(d=>q1+d[0]===q2&&r1+d[1]===r2);
      if(!adj){ console.log('seed',s,'renk',arr.color,'KOPUK:',arr.cells[i],'→',arr.cells[i+1]); bad++; }
    }
    // ayrıca hücre sayısı = beklenen uzunluk mu
    if(arr.cells.length!==arr.len) { console.log('seed',s,'renk',arr.color,'uzunluk tutarsız',arr.cells.length,arr.len); bad++; }
  }
}
console.log(bad===0?'300 seed: tüm oklar bitişik':'KOPUK YOL SAYISI: '+bad);
