import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const rootDefault=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../../docs/roadmaps/world-integration');
const live=e=>!['Excluded','Duplicate'].includes(e.editorialStatus);
const absent=e=>live(e)&&e.presence==='NotFound'&&!e.siteId;
const dateMs=d=>{if(!/^\d{4}-\d{2}-\d{2}$/.test(d??''))return null;const v=Date.parse(d+'T00:00:00Z');return Number.isFinite(v)&&new Date(v).toISOString().slice(0,10)===d?v:null;};
const host=s=>{try{return new URL(s.url).hostname.replace(/^www\./,'').toLowerCase();}catch{return null;}};
export function inspectEntry(e,now){
  const sources=Array.isArray(e.sources)?e.sources:[], domains=new Set(sources.map(host).filter(Boolean));
  const ages=sources.map(s=>dateMs(s.accessedAt)===null?null:Math.floor((now-dateMs(s.accessedAt))/86400000));
  const valid=ages.filter(n=>n!==null&&n>=0), newest=valid.length?Math.min(...valid):null;
  const pending=live(e)&&e.editorialStatus!=='Integrated', flags=[];
  if(pending&&!e.locality)flags.push('locality-missing');
  if(pending&&!sources.length)flags.push('sources-missing');
  if(pending&&domains.size<2)flags.push('independent-source-needed');
  if(pending&&sources.some((s,i)=>!host(s)||ages[i]===null||ages[i]<0))flags.push('source-metadata-invalid');
  if(pending&&newest!==null&&newest>180)flags.push('source-review-over-180-days');
  if(pending&&e.presence==='PossibleExisting')flags.push('identity-unresolved');
  if(pending&&e.proposedEntityKind==='ToQualify')flags.push('type-unresolved');
  return {entryId:e.entryId,name:e.name,editorialStatus:e.editorialStatus,presence:e.presence,proposedEntityKind:e.proposedEntityKind,sourceCount:sources.length,domains:domains.size,newestSourceAgeDays:newest,possibleMatches:e.possibleMatches?.length??0,flags,nextAction:e.nextAction??''};
}
export function auditWorld(docs,{asOf='2026-11-15'}={}){
  const now=dateMs(asOf);if(now===null)throw Error('Date --as-of invalide : YYYY-MM-DD.');
  const codes=new Set(),ids=new Set(),sites=new Map(),collisions=[],flags={},queue=[],countries=[];
  const totals={candidatePark:0,candidateStandalone:0,hiddenPark:0,notRelevant:0,possibleExisting:0,typeUnknown:0,integrated:0,publishedPresence:0};
  let sourceRecords=0;
  for(const d of docs){if(!/^[A-Z]{2}$/.test(d.countryCode)||codes.has(d.countryCode))throw Error('Code pays invalide ou dupliqué : '+d.countryCode);codes.add(d.countryCode);
    for(const e of d.entries??[]){if(!e.entryId||ids.has(e.entryId))throw Error('entryId dupliqué : '+e.entryId);ids.add(e.entryId);
      if(e.siteId){if(sites.has(e.siteId))collisions.push([e.siteId,sites.get(e.siteId),d.countryCode]);else sites.set(e.siteId,d.countryCode);}}}
  for(const d of docs){
    const es=d.entries??[];
    const c={code:d.countryCode,name:d.countryName,region:d.geographicGroup??'Autre',entries:es.length,observations:d.sourceObservationCount??0,
      coverageStatus:d.coverageStatus??'',lastSourceAccessAt:null,researchCompleted:false,coverageGap:null,
      candidatePark:0,candidateStandalone:0,hiddenPark:0,notRelevant:0,possibleExisting:0,typeUnknown:0,integrated:0,publishedPresence:0,missingLocality:0,oneSourceDomainOrLess:0};
    let newest=null;
    for(const e of es){
      const report=inspectEntry(e,now);sourceRecords+=report.sourceCount;
      for(const f of report.flags)flags[f]=(flags[f]??0)+1;
      for(const s of e.sources??[]){const t=dateMs(s.accessedAt);if(t!==null&&(newest===null||newest<t))newest=t;}
      if(absent(e)&&e.proposedEntityKind==='Park'){c.candidatePark++;totals.candidatePark++;}
      if(absent(e)&&e.proposedEntityKind==='StandaloneAttraction'){c.candidateStandalone++;totals.candidateStandalone++;}
      if(e.entityKind==='Park'&&e.presence==='Hidden'&&e.siteReviewStatusAtInventory!=='NotRelevant'&&e.editorialStatus!=='Integrated'&&live(e)){c.hiddenPark++;totals.hiddenPark++;}
      if(e.entityKind==='Park'&&e.presence==='Hidden'&&e.siteReviewStatusAtInventory==='NotRelevant'&&!['Duplicate','Integrated'].includes(e.editorialStatus)){c.notRelevant++;totals.notRelevant++;}
      if(e.presence==='PossibleExisting'&&live(e)){c.possibleExisting++;totals.possibleExisting++;}
      if(absent(e)&&e.proposedEntityKind==='ToQualify'){c.typeUnknown++;totals.typeUnknown++;}
      if(e.editorialStatus==='Integrated'){c.integrated++;totals.integrated++;}
      if(e.presence==='Published'&&live(e)){c.publishedPresence++;totals.publishedPresence++;}
      if(live(e)&&e.editorialStatus!=='Integrated'&&!e.locality)c.missingLocality++;
      if(live(e)&&e.editorialStatus!=='Integrated'&&report.domains<=1)c.oneSourceDomainOrLess++;
      const priority=d.countryCode==='ZZ'&&e.editorialStatus!=='Duplicate'?'P0':
        e.siteReviewStatusAtInventory==='NotRelevant'&&e.editorialStatus==='Excluded'?'P2':
        live(e)&&(e.presence==='PossibleExisting'||e.proposedEntityKind==='ToQualify')?'P1':
        live(e)&&(absent(e)||e.presence==='Hidden')?'P2':null;
      if(priority)queue.push({country:d.countryCode,priority,...report});
    }
    if(c.observations===0&&d.countryCode!=='ZZ')c.coverageGap=/annuaire RCDB parcouru/i.test(c.coverageStatus)?
      'RCDB parcouru ; recherche locale non documentée':'Aucune observation ; recherche locale non documentée';
    c.lastSourceAccessAt=newest===null?null:new Date(newest).toISOString().slice(0,10);
    countries.push(c);
  }
  const geo=countries.filter(c=>c.code!=='ZZ');
  return {asOf,countryCount:geo.length,withObservations:geo.filter(c=>c.observations>0).length,zeroObservations:geo.filter(c=>!c.observations).length,
    unresolvedCountryEntries:countries.find(c=>c.code==='ZZ')?.entries??0,entryCount:ids.size,sourceRecords,flags,totals,
    siteIdCollisions:collisions,countries:countries.sort((a,b)=>a.region.localeCompare(b.region,'fr')||a.name.localeCompare(b.name,'fr')),queue};
}
const esc=v=>String(v??'').replaceAll('|','\\|').replaceAll('\n',' ');
export function markdown(a){
  const l=['# Audit qualité du backlog mondial','',
    'Calcul UTC '+a.asOf+' — lecture seule des registres. Une observation ou source enregistrée ne prouve ni identité, ni ouverture, ni exhaustivité.','',
    '## P0 — Couverture et pays ZZ','',
    'Pays/territoires : '+a.countryCount+' ; avec observations : '+a.withObservations+' ; sans observations : '+a.zeroObservations+' ; dossiers ZZ : '+a.unresolvedCountryEntries+'.',
    'Un zéro ne signifie jamais absence de parc ou recherche nationale terminée.','',
    '| Territoire | Groupe | Constat |','| --- | --- | --- |'];
  for(const c of a.countries.filter(x=>x.coverageGap))l.push('| '+esc(c.name)+' ('+c.code+') | '+esc(c.region)+' | '+esc(c.coverageGap)+' |');
  l.push('','## P1 — Rapprochements et qualité des preuves','',
    'PossibleExisting : '+a.totals.possibleExisting+' ; type à décider : '+a.totals.typeUnknown+'.',
    'Sans localité : '+(a.flags['locality-missing']??0)+' ; au plus un domaine de source : '+(a.flags['independent-source-needed']??0)+'.',
    'Source la plus récente âgée de plus de 180 jours : '+(a.flags['source-review-over-180-days']??0)+' ; collisions de siteId : '+a.siteIdCollisions.length+'.',
    '','## P2 — Qualification et intégration','',
    'Candidats Park : '+a.totals.candidatePark+' ; candidates standalone : '+a.totals.candidateStandalone+' ; Park masqués : '+a.totals.hiddenPark+' ; NotRelevant : '+a.totals.notRelevant+'.',
    'Intégrations avec audit final : '+a.totals.integrated+'. Une création exige une vérification PARK_DATA_EDITOR actualisée et un contrôle des doublons.',
    '','## P3 — Fraîcheur documentaire','',
    'La dernière date source est le maximum des accessedAt consignés, pas la preuve du dernier audit complet du pays. Aucun pays marqué terminé automatiquement.','',
    '## Matrice mondiale','',
    '| Pays | Observations | Park candidats | Park masqués | Incertains | Sources <= 1 domaine | Dernière source |',
    '| --- | ---: | ---: | ---: | ---: | ---: | --- |');
  for(const c of a.countries.filter(x=>x.code!=='ZZ'))l.push('| '+esc(c.name)+' ('+c.code+') | '+c.observations+' | '+c.candidatePark+' | '+c.hiddenPark+' | '+c.possibleExisting+' | '+c.oneSourceDomainOrLess+' | '+(c.lastSourceAccessAt??'—')+' |');
  l.push('','Exécution : node tools/codex/world-integration-quality.mjs --format markdown --as-of 2026-11-15','','CSV : ajouter --format csv --scope triage pour la file détaillée, ou --scope countries pour les totaux.','');
  return l.join('\n');
}
export function csv(a,scope='triage'){
  const q=x=>'"'+String(x??'').replaceAll('"','""')+'"';
  const rows=scope==='countries'?[['country','name','observations','candidatePark','hiddenPark','possibleExisting','lastSourceAccessAt'],
    ...a.countries.map(c=>[c.code,c.name,c.observations,c.candidatePark,c.hiddenPark,c.possibleExisting,c.lastSourceAccessAt])]:
    [['country','priority','entryId','name','editorialStatus','presence','proposedEntityKind','sourceCount','domains','newestSourceAgeDays','possibleMatches','flags','nextAction'],
    ...a.queue.map(e=>[e.country,e.priority,e.entryId,e.name,e.editorialStatus,e.presence,e.proposedEntityKind,e.sourceCount,e.domains,e.newestSourceAgeDays,e.possibleMatches,e.flags.join(';'),e.nextAction])];
  return rows.map(r=>r.map(q).join(',')).join('\n')+'\n';
}
export function load(root=rootDefault){return fs.readdirSync(path.join(root,'countries')).filter(x=>x.endsWith('.json')).sort().map(x=>JSON.parse(fs.readFileSync(path.join(root,'countries',x),'utf8')));}
export function main(args=process.argv.slice(2)){
  const o={};for(let i=0;i<args.length;i+=2){if(!args[i]?.startsWith('--')||!args[i+1])throw Error('Paires --format markdown|json|csv --as-of YYYY-MM-DD --scope triage|countries --output CHEMIN.');o[args[i].slice(2)]=args[i+1];}
  const a=auditWorld(load(o.root?path.resolve(o.root):rootDefault),{asOf:o['as-of']??new Date().toISOString().slice(0,10)});
  const f=o.format??'markdown';if(!['markdown','json','csv'].includes(f)||o.scope&&!['triage','countries'].includes(o.scope))throw Error('Format/scope invalide.');
  const content=f==='markdown'?markdown(a):f==='csv'?csv(a,o.scope):JSON.stringify(a,null,2)+'\n';
  if(o.output)fs.writeFileSync(path.resolve(o.output),content,'utf8');else process.stdout.write(content);
  return a;
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){try{main();}catch(e){console.error(e.message);process.exitCode=1;}}
