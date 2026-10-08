(function(root){
'use strict';
const clamp=(v,a,b)=>Math.max(a,Math.min(b,v));
class Match {
 constructor(random=Math.random){this.random=random;this.reset();}
 reset(practice=false){this.practice=practice;this.score=[0,0];this.period=1;this.clock=practice?300:720;this.phase='ready';this.owner=0;this.hold=0;this.ball=null;this.players=[[1.5,4.5,7.5],[1.5,4.5,7.5]];this.selected=1;this.dives=[0,0,0];this.aim=4.5;this.timeouts=[0,0];this.subs=[0,0];this.halfTimeouts=[0,0];this.halfSubs=[0,0];this.extraTimeouts=[0,0];this.extraSubs=[0,0];this.penalty=null;this.wait=0;this.aiWait=2;this.events=[];this.shootCount=[0,0];this.shootGoals=[0,0];this.emit('Partido preparado. Pulsa Enter para empezar.');}
 emit(text,type='info'){this.events.push({text,type});}
 start(){if(this.phase==='ready'){this.phase='play';this.emit('Silencio. Play. Balón de tu equipo.');}}
 pause(){if(['play','pause'].includes(this.phase)){this.phase=this.phase==='play'?'pause':'play';this.emit(this.phase==='pause'?'Juego pausado.':'Play.');}}
 resume(){if(this.phase==='break'){this.phase='play';this.hold=0;this.owner=this.period===2?1:0;this.aiWait=2;this.emit('Play.');}}
 move(d,dt){if(this.phase==='play')this.players[0][this.selected]=clamp(this.players[0][this.selected]+d*dt*4,0.5,8.5);}
 dive(){if(this.phase==='play'&&this.owner!==0){this.dives[this.selected]=0.65;this.emit('Bloqueo.','dive');}}
 violation(team,reason){this.ball=null;this.hold=0;this.penalty={offender:team,reason};this.owner=1-team;this.aiWait=2.6;this.emit(`Penalti: ${reason}. Lanza ${team===0?'el rival':'tu equipo'}. Un único defensor.`,'whistle');}
 referee(reason,team=0){if(this.phase==='play')this.violation(team,reason);}
 throwBall(team,target,power=0.65,style='normal'){
  if(this.phase!=='play'||this.owner!==team||this.ball)return false;
  if(style==='high'){this.violation(team,'balón alto: sin contacto previo en zona de equipo');return false;}
  if(style==='long'){this.violation(team,'balón largo: sin contacto en zona neutral');return false;}
  if(style==='short'){this.ball=null;this.owner=1-team;this.hold=0;this.aiWait=2;this.emit('Balón corto. Cambio de posesión.','whistle');return false;}
  const x=this.penalty?this.players[team][1]:this.players[team][team===0?this.selected:1];
  this.ball={x,y:team===0?2:16,target:clamp(target,-1,10),speed:7+clamp(power,0,1)*9,team,fromX:x,travel:0};this.hold=0;this.emit(team===0?'Lanzamiento.':'Lanza el rival.','throw');return true;
 }
 timeout(team=0){if(this.phase!=='play'||this.ball||this.penalty)return false;const extra=this.period>2;const limit=extra?5:4;const halfLimit=extra?1:3;if((extra?this.extraTimeouts[team]>=1:this.timeouts[team]>=limit||this.halfTimeouts[team]>=halfLimit)){this.emit('No quedan tiempos muertos.');return false;}this.timeouts[team]++;this.halfTimeouts[team]++;if(extra)this.extraTimeouts[team]++;this.phase='timeout';this.wait=45;this.emit('Tiempo muerto: 45 segundos.','whistle');return true;}
 substitute(team=0){if(this.phase!=='play'||this.ball||this.penalty)return false;const extra=this.period>2;if((extra?this.extraSubs[team]>=1:this.subs[team]>=4||this.halfSubs[team]>=3)){this.emit('No quedan sustituciones.');return false;}this.subs[team]++;this.halfSubs[team]++;if(extra)this.extraSubs[team]++;this.emit('Sustitución completada. La alineación conserva sus posiciones.','whistle');return true;}
 finish(){this.phase='finished';const draw=this.score[0]===this.score[1];this.emit(draw?'Fin: empate.':`Fin del partido. ${this.score[0]>this.score[1]?'Gana tu equipo.':'Gana el rival.'}`,'whistle');}
 nextPeriod(){if(this.practice){this.finish();return;}if(this.period===2&&this.score[0]!==this.score[1]){this.finish();return;}if(this.period===4){this.phase='play';this.period=5;this.clock=0;this.penalty={offender:1,reason:'lanzamientos extra'};this.owner=0;this.hold=0;this.emit('Lanzamientos extra. Tres por equipo; después, muerte súbita.','whistle');return;}this.period++;this.clock=this.period<=2?720:180;this.halfTimeouts=[0,0];this.halfSubs=[0,0];this.phase='break';this.wait=this.period===2?180:this.period===3?180:0;this.ball=null;this.emit(this.period===2?'Descanso: 3 minutos. Enter para continuar.':this.period===3?'Empate. Prórroga: dos mitades de 3 minutos, gol de oro. Enter para continuar.':'Cambio de campo. Enter para continuar.','whistle');}
 resolve(goal){const team=this.ball.team;this.ball=null;if(goal){this.score[team]++;this.emit(`Gol. Tu equipo ${this.score[0]}, rival ${this.score[1]}.`,'goal');}else this.emit('Balón controlado.','save');
  if(this.period===5){this.shootCount[team]++;if(goal)this.shootGoals[team]++;const [a,b]=this.shootCount;const [ga,gb]=this.shootGoals;const decided=(a<=3&&b<=3&&(ga>gb+(3-b)||gb>ga+(3-a)))||(a===b&&a>=3&&ga!==gb);if(decided){this.finish();return;}this.owner=1-team;this.penalty={offender:team,reason:'lanzamientos extra'};}
  else{this.penalty=null;this.owner=1-team;if(goal&&(Math.abs(this.score[0]-this.score[1])>=10||this.period>=3)){this.finish();return;}}
  this.hold=0;this.aiWait=2.5;
 }
 step(dt){dt=clamp(dt,0,0.1);if(this.phase==='timeout'){this.wait-=dt;if(this.wait<=0){this.phase='play';this.emit('Fin del tiempo muerto. Play.');}return;}if(this.phase!=='play')return;
  this.dives=this.dives.map(v=>Math.max(0,v-dt));if(!this.penalty&&this.period<5){this.clock=Math.max(0,this.clock-dt);if(this.clock===0){this.nextPeriod();return;}}
  if(!this.ball){this.hold+=dt;if(this.hold>=10){this.violation(this.owner,'diez segundos');return;}if(this.owner===1){this.aiWait-=dt;if(this.aiWait<=0)this.throwBall(1,0.5+this.random()*8,0.4+this.random()*0.55);}return;}
  const b=this.ball;b.travel+=dt*b.speed;b.y+=dt*b.speed*(b.team===0?1:-1);const f=clamp(b.travel/14,0,1);b.x=b.fromX+(b.target-b.fromX)*f;
  if(b.x<0||b.x>9){const offender=b.team;this.ball=null;this.penalty=null;this.owner=1-offender;this.hold=0;this.aiWait=2;this.emit('Fuera. Cambio de posesión.','whistle');return;}
  const reached=b.team===0?b.y>=16:b.y<=2;if(!reached)return;
  const defense=1-b.team;let saved=false;if(defense===1){const candidates=this.penalty?[1]:[0,1,2];saved=candidates.some(i=>Math.abs(this.players[1][i]-b.x)<(this.penalty?0.7:0.95)&&this.random()<0.72);}
  else{const candidates=this.penalty?[this.selected]:[0,1,2];saved=candidates.some(i=>Math.abs(this.players[0][i]-b.x)<(this.dives[i]>0?1.35:0.32));}
  this.resolve(!saved);
 }
}
if(typeof module!=='undefined')module.exports={Match};else root.Match=Match;
})(globalThis);
