// Public, read-only data adapters. No account credentials or game logs leave this app.
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const data = path.join(root, 'data');
const decode = s => String(s ?? '').replace(/&#(\d+);/g, (_, n) => String.fromCodePoint(+n)).replace(/&#x([\da-f]+);/gi,(_,n)=>String.fromCodePoint(parseInt(n,16))).replace(/&amp;/g,'&').replace(/&quot;/g,'"').replace(/&lt;/g,'<').replace(/&gt;/g,'>').replace(/&nbsp;/g,' ');
const plain = s => decode(s.replace(/<script\b[\s\S]*?<\/script>/gi,'').replace(/<[^>]*>/g,' ').replace(/\s+/g,' ').trim());
function atomic(name, value) { const p=path.join(data,name);fs.writeFileSync(p+'.tmp',JSON.stringify(value));fs.renameSync(p+'.tmp',p); }
async function get(url) {const r=await fetch(url,{signal:AbortSignal.timeout(30000),headers:{'User-Agent':'TavernLens/0.1 (personal desktop tracker)'}});if(!r.ok)throw Error(`${new URL(url).host}: HTTP ${r.status}`);return r.text();}
function heroes(html) {
 const rows=[...html.matchAll(/<tr\b[\s\S]*?<\/tr>/g)].filter(m=>m[0].includes('data-name='));
 return rows.map(([s])=>{const td=[...s.matchAll(/<td\b[^>]*>([\s\S]*?)<\/td>/g)].map(m=>plain(m[1]));const id=s.match(/\/orig\/([\w-]+)\.png/);const url=s.match(/href="(\/heroes\/[^"?]+)/);if(!id||!url||td.length<8)throw Error('Hero page format changed');return {id:id[1],name:decode(s.match(/data-name="([^"]+)"/)[1]),rank:+td[1],avg:+td[3],pick:parseFloat(td[4]),games:td[7],url:'https://bgbuddy.gg'+url[1]};});
}
function comps(html) {
 const result=[];
 for(const [section] of html.matchAll(/<section\b[\s\S]*?<\/section>/g)) {
  const tier=section.match(/Power Tier ([SABCDF])/);if(!tier)continue;
  for(const [s] of section.matchAll(/<a\b[^>]*data-filter-list-target="item"[\s\S]*?<\/a>/g)) {
   const t=plain(s),name=decode(s.match(/data-name="([^"]+)"/)[1]),url='https://bgbuddy.gg'+decode(s.match(/href="([^"]+)"/)[1]);
   const avg=t.match(/Avg Placement\s+([\d.]+)/),first=t.match(/1st:\s*([\d.]+)/),top=t.match(/Top-4:\s*([\d.]+)/),games=t.match(/Games:\s*([\d.km]+)/i);
   if(!avg||!first||!top||!games)throw Error('Composition page format changed');
   const ids=[...new Set([...s.matchAll(/\/orig\/([\w-]+)\.png/g)].map(m=>m[1]))];
   result.push({name,tier:tier[1],url,avg:+avg[1],first:+first[1],top4:+top[1],games:games[1],core:ids,addons:[],build:''});
  }
 }
 return result;
}
function board(html) {
 const chunk=html.split('<!-- Board cards -->')[1]?.split('<!--')[0];if(!chunk)throw Error('Composition board format changed');
 const groups=chunk.split(/>\s*(Core|Add-ons|Enablers|Cycle)\s*<\/span>/);let core=[],addons=[];
 for(let i=1;i<groups.length;i+=2){const ids=[...new Set([...groups[i+1].matchAll(/\/orig\/([\w-]+)\.png/g)].map(m=>m[1]))];if(groups[i]==='Core')core=ids;else addons.push(...ids);}
 return {core,addons:[...new Set(addons)]};
}
function patch(html) {
 const m=html.match(/var stickyBlogList\s*=\s*(\[[\s\S]*?\]);/);if(!m)throw Error('Official news format changed');
 const entries=JSON.parse(m[1]).filter(x=>/patch|hotfix/i.test(x.title)).sort((a,b)=>b.publish-a.publish);
 if(!entries.length)throw Error('No official patch article found');const p=entries[0];return {title:p.title,date:new Date(p.publish).toISOString(),url:`https://hearthstone.blizzard.com/en-us/news/${p.id}/${p.slug}`,version:(p.title.match(/\d+\.\d+(?:\.\d+)?/)||[])[0]||'Unknown'};
}
async function update() {
 fs.mkdirSync(data,{recursive:true});const status={checkedAt:new Date().toISOString(),errors:[]};
 try {
  const listing=await get('https://api.hearthstonejson.com/v1/latest/');const build=listing.match(/\/v1\/(\d+)\//)?.[1];if(!build)throw Error('Card build unavailable');
  const all=JSON.parse(await get(`https://api.hearthstonejson.com/v1/${build}/enUS/cards.json`));if(!Array.isArray(all)||all.length<10000)throw Error('Incomplete card database');
  const cards=all.filter(c=>c.set==='BATTLEGROUNDS'||c.battlegroundsPremiumDbfId||c.battlegroundsNormalDbfId||c.isBattlegroundsHero||c.id.startsWith('TB_Bacon')).map(c=>({id:c.id,dbfId:c.dbfId,name:c.name||c.id,text:plain(c.text||''),type:c.type,tribe:(c.races||[]).join(' / '),tier:c.techLevel||0,attack:c.attack||0,health:c.health||0,duosOnly:!!c.isBattlegroundsDuosExclusive,solosOnly:!!c.isBattlegroundsSolosExclusive,pool:!!c.isBattlegroundsPoolMinion,hero:!!c.isBattlegroundsHero,spell:!!c.isBattlegroundsPoolSpell,normal:c.battlegroundsNormalDbfId||0,premium:c.battlegroundsPremiumDbfId||0}));
  atomic('combat-cards.json',{build,cards:all});atomic('cards.json',{build,updatedAt:status.checkedAt,cards});status.cardBuild=build;
 } catch(e){status.errors.push('Cards: '+e.message);}
 try {atomic('patch.json',{...patch(await get('https://hearthstone.blizzard.com/en-us/news')),checkedAt:status.checkedAt});}catch(e){status.errors.push('Patch news: '+e.message);}
 try {
  const hs=await get('https://bgbuddy.gg/heroes?game_mode=solo&mmr_bracket=all&time_period=last-patch');
  const cs=await get('https://bgbuddy.gg/comps?time_period=last-patch');
  const build=hs.match(/Patch\s+(\d{5,})/)?.[1];const compBuild=cs.match(/Patch\s+(\d{5,})/)?.[1];
  if(!build||build!==compBuild)throw Error('Sources updated during refresh; retry later');
  const h=heroes(hs),c=comps(cs);if(h.length<30||c.length<5)throw Error('Source returned incomplete rankings');
  for(const comp of c) {try {const detail=await get(comp.url);if(detail.match(/Patch\s+(\d{5,})/)?.[1]!==build)throw Error('Build changed');Object.assign(comp,board(detail));comp.build=build;}catch(e){comp.detailError=e.message;comp.core=[];}}
  atomic('meta.json',{build,updatedAt:status.checkedAt,sourceAge:plain(hs.match(/Updated\s+([^<]+)/)?.[0]||'Source age unavailable'),source:'Battlegrounds Buddy / Firestone community data',url:'https://bgbuddy.gg/about',scope:'Solo / all ranks / current source patch',heroes:h,comps:c});
 }catch(e){status.errors.push('Rankings: '+e.message);}
 try {
  const url='https://bgbuddy.gg/heroes?game_mode=duo&mmr_bracket=all&time_period=last-patch';
  const html=await get(url),h=heroes(html),build=html.match(/Patch\s+(\d{5,})/)?.[1];
  if(!build||h.length<30||h.some(x=>x.avg<1||x.avg>4))throw Error('Incomplete Duos team rankings');
  atomic('meta-duos.json',{build,updatedAt:status.checkedAt,sourceAge:plain(html.match(/Updated\s+([^<]+)/)?.[0]||'Source age unavailable'),source:'Battlegrounds Buddy / Firestone community data',url,scope:'Duos / all ranks / team placement 1–4 / current source patch',heroes:h,comps:[]});
 }catch(e){status.errors.push('Duos rankings: '+e.message);}
 atomic('update-status.json',status);console.log(JSON.stringify(status));if(status.errors.length)process.exitCode=2;
}
module.exports={heroes,comps,board,patch,plain};
if(require.main===module)update().catch(e=>{console.error(e.message);process.exitCode=1;});
