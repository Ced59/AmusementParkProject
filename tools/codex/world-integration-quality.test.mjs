import test from 'node:test';
import assert from 'node:assert/strict';
import {auditWorld, inspectEntry, markdown, csv} from './world-integration-quality.mjs';

const source=(url,accessedAt='2026-11-15')=>({label:'Site',url,accessedAt});
const row=(extra={})=>({entryId:'GP-example',name:'Exemple',entityKind:'ToQualify',proposedEntityKind:'Park',presence:'PossibleExisting',
  editorialStatus:'ToQualify',siteId:null,locality:'Petit-Bourg',sources:[source('https://a.example/a')],possibleMatches:[],
  integrationHistory:[],lastDataIntegrationAt:null,nextAction:'Vérifier.',...extra});
const doc=(code,entries=[],extra={})=>({countryCode:code,countryName:code,geographicGroup:'Océanie',
  coverageStatus:'Recherche nationale ou locale à compléter',sourceObservationCount:0,entries,...extra});
test('P0 : zéro observation ne prouve jamais une recherche complète',()=>{
  const a=auditWorld([doc('PF'),doc('VU')]);
  assert.equal(a.zeroObservations,2);
  assert.equal(a.countries.every(x=>x.researchCompleted===false),true);
  assert.match(markdown(a),/recherche locale non documentée/);
});
test('P0 : un annuaire RCDB seul est distingué des vérifications locales',()=>{
  const a=auditWorld([doc('BT',[],{coverageStatus:'Aucune piste repérée ; annuaire RCDB parcouru'})]);
  assert.match(a.countries[0].coverageGap,/RCDB parcouru/);
});
test('P1 : absence de correspondance non vérifiée ne gonfle pas les créations',()=>{
  const a=auditWorld([doc('GP',[row()],{sourceObservationCount:1})]);
  assert.equal(a.totals.candidatePark,0);
  assert.equal(a.totals.possibleExisting,1);
  assert.equal(a.queue[0].priority,'P1');
});
test('P1 : deux URLs d’un même domaine restent une seule famille de sources',()=>{
  const e=row({sources:[source('https://www.example.org/a'),source('https://example.org/b')]});
  assert.equal(inspectEntry(e,Date.parse('2026-11-15')).domains,1);
  e.sources.push(source('https://office.fr/park'));
  assert.equal(inspectEntry(e,Date.parse('2026-11-15')).domains,2);
});
test('P3 : les dates sources fausses, futures et anciennes restent visibles',()=>{
  assert.ok(inspectEntry(row({sources:[source('https://example.org','2026-01-01')]}),Date.parse('2026-11-15')).flags.includes('source-review-over-180-days'));
  assert.ok(inspectEntry(row({sources:[source('https://example.org','2026-02-30'),source('https://office.fr','2027-02-01')]}),Date.parse('2026-11-15')).flags.includes('source-metadata-invalid'));
});
test('P0, P1, P2 : triage sans suppression des états hérités',()=>{
  const a=auditWorld([doc('ZZ',[row({entryId:'ZZ-1',siteId:'park-1',presence:'Hidden',entityKind:'Park',editorialStatus:'ToIntegrate'})]),doc('GP',[
    row({entryId:'GP-1'}),row({entryId:'GP-2',presence:'NotFound'}),row({entryId:'GP-3',presence:'Hidden',entityKind:'Park',siteId:'park-3',editorialStatus:'ToIntegrate'}),
    row({entryId:'GP-4',presence:'Hidden',entityKind:'Park',siteId:'park-4',siteReviewStatusAtInventory:'NotRelevant',editorialStatus:'Excluded'})])]);
  assert.equal(a.unresolvedCountryEntries,1);assert.equal(a.totals.candidatePark,1);assert.equal(a.totals.hiddenPark,2);assert.equal(a.totals.notRelevant,1);
  assert.equal(a.queue.find(x=>x.entryId==='ZZ-1').priority,'P0');
  assert.equal(a.queue.find(x=>x.entryId==='GP-1').priority,'P1');
  assert.equal(a.queue.find(x=>x.entryId==='GP-2').priority,'P2');
  assert.equal(a.queue.find(x=>x.entryId==='GP-4').priority,'P2');
});
test('un UUID de fiche partagé est signalé sans fusion',()=>{
  const a=auditWorld([doc('GP',[row({entryId:'GP-1',siteId:'same-id',presence:'Published'})]),doc('PF',[row({entryId:'PF-1',siteId:'same-id',presence:'Published'})])]);
  assert.equal(a.siteIdCollisions.length,1);assert.equal(a.entryCount,2);
});
test('identifiants de backlog dupliqués interdits mondialement',()=>{
  assert.throws(()=>auditWorld([doc('GP',[row()]),doc('PF',[row()])]),/entryId dupliqué/);
});
test('export CSV garde les identifiants et protège les guillemets',()=>{
  const a=auditWorld([doc('GP',[row({nextAction:'Vérifier, puis "trancher".'})])]);
  assert.match(csv(a),/GP-example/);assert.match(csv(a),/""trancher""/);assert.match(csv(a,'countries'),/\"country\",\"name\"/);
});
test('date de référence invalide rejetée',()=>assert.throws(()=>auditWorld([],{asOf:'2026-02-30'}),/invalide/));
