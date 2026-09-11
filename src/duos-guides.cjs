// Illustrated mechanic guides, not population-ranked compositions.
module.exports=function(cards,build){
 const byId=new Map(cards.map(c=>[c.id,c]));
 return [
 ['Pass scaling',['BGDUO_100','BGDUO_114','BGDUO_117','BGDUO31_211','BGDUO31_212'],['BGDUO_120','BG31_243','BG31_244'],'Use discounted passes to activate Passenger and Mantid King. Puddle Prancer grows when passed; Transport Reactor scales with team passes. Check remaining gold before moving a combat piece.'],
 ['Team economy',['BGDUO31_201','BGDUO_104','BGDUO_118','BGDUO31_205'],['BGDUO31_207','BGDUO_110'],'Gathering Stormer pays your teammate when sold. Saloonkeeper supplies a Coin; Plunder Pal pays both players at turn start. Selfless Sightseer raises the team gold cap. Balance support slots against board strength.'],
 ['Blood Gem support',['BGDUO_111','BGDUO31_202'],['BGDUO_109'],'Generous Geomancer supplies both players with Blood Gems. Loyal Mobster applies gems to your teammate’s whole warband at turn end; stronger gems improve that support. Support System can add Divine Shield to a teammate’s minion.'],
 ['Shared Tavern buffs',['BGDUO_121','BGDUO_119'],['BGDUO_120'],'Man’ari Messenger permanently buffs both Taverns. Orc-estra Conductor improves with copies played by either player. Use passing to share useful Battlecry minions, while leaving enough gold to play them.'],
 ['Deathrattle supply',['BGDUO31_203','BGDUO_112','BGDUO31_208'],['BGDUO31_207','BGDUO_110'],'Shifty Snake and Grave Narrator generate minions for your teammate. San’layn Scribe counts deaths across the team. These pieces support different plans; they are not a prescribed seven-minion formation.'],
 ['Spell and discovery sharing',['BGDUO_122','BGDUO31_209'],['BG31_242','BGDUO33_140'],'Storm Splitter copies the first Tavern spell you pass each turn. Private Chef generates and passes a minion of a chosen type. Bargain Bundle gives your teammate the other Discover options. Coordinate the target tribe before spending resources.']
 ].map(([name,core,addons,notes])=>({name,tier:'Guide',unranked:true,notes,avg:0,first:0,top4:0,games:'',url:'https://hearthstone.blizzard.com/en-us/battlegrounds',build,core:core.filter(id=>byId.get(id)?.pool||byId.get(id)?.spell),addons:addons.filter(id=>byId.get(id)?.pool||byId.get(id)?.spell)})).filter(c=>c.core.length>=2);
};
