const assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
const {match}=require('./cache-art.cjs');const root=path.resolve(__dirname,'..');const data=require('../data/cards.json'),byDbf=new Map(data.cards.map(c=>[c.dbfId,c]));let checks=0;const check=(v)=>{assert.ok(v);checks++};
const c=data.cards.find(c=>c.id==='BG31_803'),g=byDbf.get(c.premium);const source={id:c.dbfId,externalId:c.id,dbfIdGold:g.dbfId,attack:c.attack,health:c.health,attackGold:g.attack,healthGold:g.health,tier:c.tier,text:c.text,textGold:g.text};
check(match(g,c,source));check(!match(g,c,{...source,dbfIdGold:1}));check(!match(g,c,{...source,attackGold:999}));check(!match(g,c,{...source,textGold:'Wrong effect'}));check(!match(g,c,{...source,externalId:'unrelated'}));check(!match(g,c,{...source,tier:7}));
for(const base of data.cards.filter(c=>c.pool)){for(const card of [base,byDbf.get(base.premium)].filter(Boolean)){
 const file=path.join(root,'data/art/verified-v2',data.build,card.id+'.png');check(fs.existsSync(file));const meta=JSON.parse(fs.readFileSync(file+'.json'));check(meta.dbfId===card.dbfId&&meta.attack===card.attack&&meta.health===card.health&&meta.text===card.text&&meta.build===data.build);const png=fs.readFileSync(file);check(png.readUInt32BE(0)===0x89504e47&&png.readUInt32BE(16)/png.readUInt32BE(20)<.85);
}}
console.log(JSON.stringify({passed:checks,poolCards:data.cards.filter(c=>c.pool).length,build:data.build}));
