// Local-only adapter for Firestone's MIT-licensed combat engine. No requests are made here.
const fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'..'),base=path.join(root,'simulator/node_modules/@firestone-hs');
const ref=require(path.join(base,'reference-data'));
const engine=require(path.join(base,'simulate-bgs-battle'));
const {CardsData}=require(path.join(base,'simulate-bgs-battle/dist/cards/cards-data'));
console.log=()=>{};console.warn=()=>{};console.error=()=>{};
const n=(e,k)=>Number(e?.Tags?.[k]??e?.Tags?.[ref.GameTag[k]]??0),s=(e,k)=>e?.Tags?.[k]||'';
function prepare(f,db){
 const duos=String(f.Mode||'').includes('DUO');
 if(duos&&(f.SnapshotProblem||!f.FriendlyPartner||!f.EnemyPartner))throw Error(f.SnapshotProblem||'Waiting for all four Duos warbands');
 const all=f.All||[],find=id=>all.find(e=>e.Id===id),warnings=['Experimental: opponent hand and some persistent counters may be unavailable.'];
 function numericTags(e){return Object.fromEntries(Object.entries(e.Tags||{}).map(([k,v])=>[ref.GameTag[k]??k,Number(v)]).filter(([k,v])=>/^\d+$/.test(k)&&Number.isFinite(v)));}
 function ench(id){return all.filter(e=>s(e,'CARDTYPE')==='ENCHANTMENT'&&n(e,'ATTACHED')===id&&e.CardId).map(e=>({cardId:e.CardId,originEntityId:n(e,'CREATOR'),tagScriptDataNum1:n(e,'TAG_SCRIPT_DATA_NUM_1'),tagScriptDataNum2:n(e,'TAG_SCRIPT_DATA_NUM_2'),timing:e.Id}));}
 function unit(e){if(!db.getCard(e.CardId)?.id)throw Error('Unknown card in combat snapshot');return {entityId:e.Id,cardId:e.CardId,attack:n(e,'ATK'),health:Math.max(0,n(e,'HEALTH')-n(e,'DAMAGE')),maxHealth:n(e,'HEALTH'),taunt:!!n(e,'TAUNT'),divineShield:!!n(e,'DIVINE_SHIELD'),poisonous:!!n(e,'POISONOUS'),venomous:!!n(e,'VENOMOUS'),reborn:!!n(e,'REBORN'),windfury:!!n(e,'WINDFURY'),stealth:!!n(e,'STEALTH'),cantAttack:!!n(e,'CANT_ATTACK'),scriptDataNum1:n(e,'TAG_SCRIPT_DATA_NUM_1'),scriptDataNum2:n(e,'TAG_SCRIPT_DATA_NUM_2'),scriptDataNum3:n(e,'TAG_SCRIPT_DATA_NUM_3'),scriptDataNum4:n(e,'TAG_SCRIPT_DATA_NUM_4'),scriptDataNum5:n(e,'TAG_SCRIPT_DATA_NUM_5'),scriptDataNum6:n(e,'TAG_SCRIPT_DATA_NUM_6'),tags:numericTags(e),enchantments:ench(e.Id)};}
 function side(controller,board){
  const player=all.find(e=>s(e,'CARDTYPE')==='PLAYER'&&n(e,'CONTROLLER')===controller);const hero=find(n(player,'HERO_ENTITY'));if(!player||!hero?.CardId)throw Error('Hero context is incomplete');
  const owned=all.filter(e=>n(e,'CONTROLLER')===controller),powers=owned.filter(e=>s(e,'CARDTYPE')==='HERO_POWER'&&s(e,'ZONE')==='PLAY').map(e=>({cardId:e.CardId,entityId:e.Id,used:!!n(e,'EXHAUSTED'),info:n(e,'TAG_SCRIPT_DATA_NUM_1'),info2:n(e,'TAG_SCRIPT_DATA_NUM_2'),info3:n(e,'TAG_SCRIPT_DATA_NUM_3'),info4:n(e,'TAG_SCRIPT_DATA_NUM_4'),info5:n(e,'TAG_SCRIPT_DATA_NUM_5'),info6:n(e,'TAG_SCRIPT_DATA_NUM_6')}));
  const trinkets=owned.filter(e=>s(e,'CARDTYPE')==='BATTLEGROUND_TRINKET'&&e.CardId&&!e.CardId.startsWith('BG30_Trinket_')).map(e=>({cardId:e.CardId,entityId:e.Id,scriptDataNum1:n(e,'TAG_SCRIPT_DATA_NUM_1'),scriptDataNum2:n(e,'TAG_SCRIPT_DATA_NUM_2'),scriptDataNum6:n(e,'TAG_SCRIPT_DATA_NUM_6'),tags:numericTags(e)}));
  const hand=owned.filter(e=>s(e,'ZONE')==='HAND'&&s(e,'CARDTYPE')==='MINION'&&e.CardId).map(unit);
  const quests=owned.filter(e=>s(e,'ZONE')==='SECRET'&&n(e,'QUEST'));if(quests.some(e=>!e.CardId||!db.getCard(e.CardId)?.id))throw Error('Unknown quest');
  const questEntities=quests.map(e=>({CardId:e.CardId,RewardDbfId:n(e,'QUEST_REWARD_DATABASE_ID'),ProgressCurrent:n(e,'QUEST_PROGRESS'),ProgressTotal:n(e,'QUEST_PROGRESS_TOTAL')}));
  const rewards=owned.filter(e=>s(e,'CARDTYPE')==='BATTLEGROUND_QUEST_REWARD'||e.CardId?.includes('_Reward_')&&s(e,'ZONE')==='PLAY');
  if(rewards.some(e=>!db.getCard(e.CardId)?.id))throw Error('Unknown quest reward');
  const questRewardEntities=rewards.map(e=>({cardId:e.CardId,entityId:e.Id,scriptDataNum1:n(e,'TAG_SCRIPT_DATA_NUM_1')}));
  const secretEntities=owned.filter(e=>s(e,'ZONE')==='SECRET'&&!n(e,'QUEST'));if(secretEntities.some(e=>!e.CardId||!db.getCard(e.CardId)?.id||n(e,'QUEST')))throw Error('An unknown secret or quest prevents simulation');
  const secrets=secretEntities.map(e=>({entityId:e.Id,cardId:e.CardId,scriptDataNum1:n(e,'TAG_SCRIPT_DATA_NUM_1'),scriptDataNum2:n(e,'TAG_SCRIPT_DATA_NUM_2'),triggersLeft:n(e,'TAG_SCRIPT_DATA_NUM_3')}));
  const globalInfo={};const mappings={EternalKnightsDeadThisGame:'BACON_ETERNAL_KNIGHTS_DEAD_THIS_GAME',UndeadAttackBonus:'BACON_UNDEAD_ATTACK_BONUS',BloodGemAttackBonus:'BACON_BLOODGEMBUFFATKVALUE',BloodGemHealthBonus:'BACON_BLOODGEMBUFFHEALTHVALUE',SpellsCastThisGame:'NUM_SPELLS_PLAYED_THIS_GAME',TavernSpellsCastThisGame:'BACON_TAVERN_SPELLS_CAST_THIS_GAME',GoldSpentThisGame:'BACON_GOLD_SPENT_THIS_GAME'};
  for(const [field,tag]of Object.entries(mappings))if(player.Tags[tag]!==undefined)globalInfo[field]=n(player,tag);
  const scribe=owned.find(e=>e.CardId==='BGDUO31_208pe'&&s(e,'ZONE')==='PLAY');if(scribe)globalInfo.SanlaynScribesDeadThisGame=n(scribe,'TAG_SCRIPT_DATA_NUM_1');
  return {board:board.map(unit),player:{startOfCombatDone:f.Stage==="After start-of-combat",cardId:hero.CardId,entityId:hero.Id,hpLeft:Math.max(1,n(hero,'HEALTH')-n(hero,'DAMAGE')+n(hero,'ARMOR')),tavernTier:n(player,'PLAYER_TECH_LEVEL')||n(hero,'PLAYER_TECH_LEVEL')||1,heroPowers:powers,questEntities,questRewards:rewards.map(e=>e.CardId),questRewardEntities,secrets,trinkets,hand,globalInfo,enchantments:ench(hero.Id)}};
 }
 if(f.Friendly.length>7||f.Enemy.length>7)throw Error('Combat board has more than seven minions');
 const input={playerBoard:side(f.FriendlyController,f.Friendly),opponentBoard:side(f.EnemyController,f.Enemy),gameState:{currentTurn:f.Turn,anomalies:[]},options:{numberOfSimulations:1500,maxAcceptableDuration:4500,skipInfoLogs:true,includeOutcomeSamples:false}};
 if(duos){
  if(f.FriendlyPartner.Board.length>7||f.EnemyPartner.Board.length>7)throw Error('Invalid reserve warband');
  input.playerTeammateBoard=side(f.FriendlyPartner.PlayerId,f.FriendlyPartner.Board);
  input.opponentTeammateBoard=side(f.EnemyPartner.PlayerId,f.EnemyPartner.Board);
  if(!f.FriendlyFirst)[input.playerBoard,input.playerTeammateBoard]=[input.playerTeammateBoard,input.playerBoard];
  if(!f.EnemyFirst)[input.opponentBoard,input.opponentTeammateBoard]=[input.opponentTeammateBoard,input.opponentBoard];
  warnings.push('Duos team estimate reconstructed after all four warbands are revealed.');
 }
 const game=find(f.GameEntity);if(n(game,'BACON_DARK_GIFTS_ACTIVE'))warnings.push('Dark Gift state is incomplete; these are conditional visible-board estimates.');
 const anomalies=all.filter(e=>s(e,'CARDTYPE')==='BATTLEGROUND_ANOMALY');if(anomalies.length)throw Error('Anomaly combat needs additional state; odds withheld');
 return {input,warnings};
}
function simulate(f){
 const stored=JSON.parse(fs.readFileSync(path.join(root,'data/combat-cards.json')));const current=JSON.parse(fs.readFileSync(path.join(root,'data/cards.json')));const compatibility=JSON.parse(fs.readFileSync(path.join(root,'simulator/compatibility.json')));if(stored.build!==current.build)throw Error('Simulator card data is stale');if(current.build!==compatibility.testedCardBuild)throw Error('New card build: simulator needs a compatibility update');
 const db=new ref.AllCardsService();db.initializeCardsDbFromCards(stored.cards.map(c=>({...c,set:c.set==='BATTLEGROUNDS'?'Battlegrounds':c.set,isBaconPool:!!(c.isBattlegroundsPoolMinion||c.isBattlegroundsPoolSpell),battlegroundsHero:!!c.isBattlegroundsHero,premium:!!c.battlegroundsNormalDbfId,mechanics:c.mechanics||[],otherTags:[],tags:c.tags||{}})));
 const {input,warnings}=prepare(f,db);const cardsData=new CardsData(db);let result;const generator=engine.simulateBattle(input,db,cardsData);while(true){const x=generator.next();result=x.value;if(x.done)break;}
 const total=result.won+result.lost+result.tied;if(total<100)throw Error('Too few completed simulations');
 return {key:f.Key,status:'estimate',scope:String(f.Mode||'').includes('DUO')?'Duos team estimate':'Visible-state estimate',engine:'Firestone 1.1.750',simulations:total,win:100*result.won/total,loss:100*result.lost/total,draw:100*result.tied/total,margin:98/Math.sqrt(total),warnings};
}
module.exports={simulate,prepare};
if(require.main===module)(async()=>{let text='';for await(const c of process.stdin)text+=c;let f=JSON.parse(text);let out;try{out=simulate(f);}catch(e){out={key:f.Key,status:'unavailable',reason:e.message};}process.stdout.write(JSON.stringify(out));})();
