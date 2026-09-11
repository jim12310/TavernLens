const fs=require('node:fs'),path=require('node:path');
const sharp=require('../art-runtime/node_modules/sharp');
const root=path.resolve(__dirname,'..');
const clean=s=>String(s||'').replace(/<[^>]*>|\[x\]/g,'').replace(/&nbsp;/g,' ').replace(/\b1 turns\b/g,'1 turn').replace(/\s+/g,'').toLowerCase();
function match(card,base,source){
 if(!source||source.externalId!==base.id||+source.id!==base.dbfId)return false;
 const golden=card.normal>0;
 return (!golden||(base.premium===card.dbfId&&card.normal===base.dbfId&&+source.dbfIdGold===card.dbfId))&&
 +source[golden?'attackGold':'attack']===card.attack&&+source[golden?'healthGold':'health']===card.health&&+source.tier===card.tier&&
 clean(source[golden?'textGold':'text'])===clean(card.text);
}
async function get(url){let r=await fetch(url,{signal:AbortSignal.timeout(20000)});if(!r.ok)throw Error('HTTP '+r.status);return r;}
async function cache(ids){
 const meta=JSON.parse(fs.readFileSync(path.join(root,'data/cards.json')));const byId=new Map(meta.cards.map(c=>[c.id,c])),byDbf=new Map(meta.cards.map(c=>[c.dbfId,c]));
 const dir=path.join(root,'data/art/verified-v2',meta.build);fs.mkdirSync(dir,{recursive:true});
 let ok=0,failed=0;const errors=[];
 const queue=[...new Set(ids)].filter(id=>/^[A-Za-z0-9_-]+$/.test(id)&&byId.has(id)&&(process.argv.includes('--force')||!fs.existsSync(path.join(dir,id+'.png'))));
 const bases=[...new Set(queue.map(id=>{let c=byId.get(id);return c.normal||c.dbfId}))];const sources=new Map();
 // Batches keep requests below the public API quota, including the live hover path.
 for(let i=0;i<bases.length;i+=20){try{const r=await get('https://hsbg.cards/api/v1/cards?pool=all&identifiers='+bases.slice(i,i+20).join(','));const result=await r.json();for(const c of result.data||[])sources.set(+c.id,c);}catch(e){errors.push('Card source: '+e.message);}}
 async function worker(){while(queue.length){const id=queue.shift(),c=byId.get(id),base=byDbf.get(c.normal)||c;try{
  let url,provider;const source=sources.get(base.dbfId);
  if(c.type==='MINION'){
   if(!match(c,base,source))throw Error('Image metadata does not match current card data');
   const image=c.normal?source.imageGold:source.image;
   if(!image||!image.startsWith('/cards/')||image.includes('..')||(c.normal&&image===source.image))throw Error('Golden image missing');
   url='https://hsbg.cards'+image;provider='HS BG Cards';
  }else{url=`https://art.hearthstonejson.com/v1/bgs/latest/enUS/256x/${id}.png`;provider='HearthstoneJSON';}
  let r;try{r=await get(url);}catch(e){if(c.type==='MINION')throw e;url=`https://art.hearthstonejson.com/v1/render/latest/enUS/256x/${id}.png`;try{r=await get(url);}catch(e){if(c.type!=='HERO')throw e;url=`https://art.hearthstonejson.com/v1/orig/${id}.png`;r=await get(url);}}if(r.headers.get('X-Image-Fallback'))throw Error('Incorrect image variant');
  const bytes=Buffer.from(await r.arrayBuffer());if(bytes.length>3000000)throw Error('Oversized image');
  const info=await sharp(bytes).metadata();if(!info.width||(c.type!=='HERO'&&(info.width/info.height>0.85||info.width/info.height<0.5)))throw Error('Not a full card render');
  const image=await sharp(bytes).png().toBuffer();const file=path.join(dir,id+'.png');fs.writeFileSync(file+'.tmp',image);fs.renameSync(file+'.tmp',file);
  fs.writeFileSync(file+'.json',JSON.stringify({id,dbfId:c.dbfId,normal:c.normal,premium:c.premium,attack:c.attack,health:c.health,text:c.text,build:meta.build,provider,url,checkedAt:new Date().toISOString()}));ok++;
 }catch(e){failed++;errors.push(id+': '+e.message);}}}
 await Promise.all(Array.from({length:4},worker));return {ok,failed,build:meta.build,errors};
}
async function main(){let ids;if(process.argv.includes('--all')){let c=JSON.parse(fs.readFileSync(path.join(root,'data/cards.json'))),m=JSON.parse(fs.readFileSync(path.join(root,'data/meta.json')));const db=new Map(c.cards.map(x=>[x.dbfId,x]));ids=c.cards.filter(x=>x.pool||x.hero||x.spell).flatMap(x=>[x.id,db.get(x.premium)?.id]).concat(m.comps.flatMap(x=>[...x.core,...x.addons]),m.heroes.map(x=>x.id)).filter(Boolean);}else{let input='';for await(const s of process.stdin)input+=s;ids=JSON.parse(input);}console.log(JSON.stringify(await cache(ids)));}
module.exports={match,clean};
if(require.main===module)main().catch(e=>{console.error(e.message);process.exitCode=1;});
