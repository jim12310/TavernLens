const assert=require('node:assert/strict'),fs=require('node:fs');
const {simulate}=require('./simulate.cjs');
function minion(Id,controller,attack,health,cardId='BG20_100',extra={}){return {Id,CardId:cardId,Tags:{CONTROLLER:String(controller),CARDTYPE:'MINION',ZONE:'PLAY',ATK:String(attack),HEALTH:String(health),...extra}};}
function fight(a,b){return {Key:'fixture',Stage:'After start-of-combat',Turn:4,GameEntity:1,FriendlyController:1,EnemyController:9,Friendly:a,Enemy:b,All:[{Id:1,Tags:{CARDTYPE:'GAME'}},{Id:2,Tags:{CARDTYPE:'PLAYER',CONTROLLER:'1',HERO_ENTITY:'20',PLAYER_TECH_LEVEL:'2'}},{Id:3,Tags:{CARDTYPE:'PLAYER',CONTROLLER:'9',HERO_ENTITY:'21',PLAYER_TECH_LEVEL:'2'}},{Id:20,CardId:'TB_BaconShop_HERO_PH',Tags:{CARDTYPE:'HERO',HEALTH:'30'}},{Id:21,CardId:'TB_BaconShop_HERO_PH',Tags:{CARDTYPE:'HERO',HEALTH:'30'}},...a,...b]};}
let r=simulate(fight([minion(50,1,10,10)],[minion(60,9,1,1)]));assert.equal(r.win,100);assert.equal(r.loss,0);
r=simulate(fight([minion(50,1,1,1)],[minion(60,9,1,1)]));assert.equal(r.draw,100);
r=simulate(fight([minion(50,1,1,1,'BG20_100',{DIVINE_SHIELD:'1'})],[minion(60,9,1,1)]));assert.equal(r.win,100);
r=simulate(fight([minion(50,1,1,1,'BG_EX1_556')],[minion(60,9,1,1)]));assert.equal(r.win,100,'Harvest Golem must summon a surviving deathrattle minion');
r=simulate(fight([minion(50,1,1,1)],[minion(60,9,10,10)]));assert.equal(r.loss,100);assert.equal(r.win+r.draw+r.loss,100);
assert.throws(()=>simulate(fight([minion(50,1,1,1,'UNKNOWN_CARD')],[minion(60,9,1,1)])));
process.stdout.write('8 combat assertions passed, including divine shield and deathrattle.\n');
