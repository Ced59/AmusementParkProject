import assert from 'node:assert/strict';
import test from 'node:test';

import {
  isProductGrowthRoadmapPath,
  isResearchExtensionPath,
  requiredBetaGateIds,
  requiredEvidenceIds,
  requiredProfileIds,
  requiredProgramIds,
  requiredTaskContextFields,
  requiredTaskIds,
  validateResearchCatalog,
  validateResearchReadiness,
} from './check-research-readiness.mjs';

function item(id, fields = {}) {
  return { id, ...fields };
}

const roadmapByProgram = Object.freeze({
  RANK: 'docs/roadmaps/product-growth/01-ranking-trust-and-methodology-roadmap.md',
  PASS: 'docs/roadmaps/product-growth/02-visit-passport-and-ride-log-roadmap.md',
  SHARE: 'docs/roadmaps/product-growth/03-shareable-recaps-and-comparisons-roadmap.md',
  FIT: 'docs/roadmaps/product-growth/04-park-fit-recommendation-and-comparison-roadmap.md',
  WATCH: 'docs/roadmaps/product-growth/05-favorites-watchlists-and-factual-alerts-roadmap.md',
  TRIP: 'docs/roadmaps/product-growth/06-collaborative-trip-planning-roadmap.md',
  HIST: 'docs/roadmaps/product-growth/07-park-history-explorer-roadmap.md',
  LIVE: 'docs/roadmaps/product-growth/08-live-wait-times-and-crowd-intelligence-roadmap.md',
});

function validProgram(id) {
  return {
    id,
    roadmap: roadmapByProgram[id],
    gate: `${id}-G`,
    businessQuestion: 'Question métier',
    firstSuccess: 'Premier succès',
    profileIds: ['journal-enthusiast', 'assistive-technology', 'modest-device-network'],
    taskContext: Object.fromEntries(requiredTaskContextFields.map((field) => [field, field])),
    stopConditions: ['stop one', 'stop two'],
    generalizationEvidence: ['proof one', 'proof two'],
    extensionDocuments: [],
  };
}

function validCatalog() {
  return {
    schemaVersion: 1,
    statusPolicy: {
      protocolStatus: 'ready',
      fieldEvidenceStatus: 'pending',
      blocksDelivery: false,
      allowGeneralizationWithoutEvidence: false,
      statement: 'Aucune observation revendiquée.',
    },
    canonicalProfiles: requiredProfileIds.map((id) => item(id, { label: id })),
    commonTasks: requiredTaskIds.map((id) => item(id, {
      instruction: id,
      comparableMeasure: id,
    })),
    evidenceSchema: requiredEvidenceIds.map((id) => item(id, {
      required: id !== 'authorizedQuote',
      constraint: id === 'outcome'
        ? 'unassisted, assisted, failed ou not-observable'
        : id,
    })),
    betaGates: requiredBetaGateIds.map((id) => item(id, {
      requirements: ['one', 'two', 'three'],
    })),
    programs: requiredProgramIds.map((id) => validProgram(id)),
  };
}

test('accepts the complete canonical research catalog', () => {
  assert.deepEqual(validateResearchCatalog(validCatalog()), []);
});

test('the repository catalog and all referenced protocols are ready', async () => {
  assert.deepEqual(await validateResearchReadiness(), []);
});

test('rejects a missing program and duplicate canonical task', () => {
  const catalog = validCatalog();
  catalog.programs = catalog.programs.slice(1);
  catalog.commonTasks[1].id = catalog.commonTasks[0].id;

  const errors = validateResearchCatalog(catalog);
  assert.ok(errors.some((error) => error.includes('Programme') && error.includes('RANK')));
  assert.ok(errors.some((error) => error.includes('dupliqué')));
});

test('keeps field research non-blocking without allowing unproven generalization', () => {
  const catalog = validCatalog();
  catalog.statusPolicy.blocksDelivery = true;
  catalog.statusPolicy.allowGeneralizationWithoutEvidence = true;
  catalog.statusPolicy.fieldEvidenceStatus = 'validated';

  const errors = validateResearchCatalog(catalog);
  assert.ok(errors.some((error) => error.includes('ne doivent pas bloquer')));
  assert.ok(errors.some((error) => error.includes('sans preuve terrain')));
  assert.ok(errors.some((error) => error.includes('pending')));
});

test('requires assistive and modest-context profiles for every program', () => {
  const catalog = validCatalog();
  catalog.programs[0].profileIds = [
    'journal-enthusiast',
    'tool-free-enthusiast',
    'occasional-visitor',
  ];

  const errors = validateResearchCatalog(catalog);
  assert.ok(errors.some((error) => error.includes('assistive-technology')));
  assert.ok(errors.some((error) => error.includes('modest-device-network')));
});

test('requires every comparable context and explicit stop evidence', () => {
  const catalog = validCatalog();
  delete catalog.programs[0].taskContext.unknownDataScenario;
  catalog.programs[0].stopConditions = ['only one'];
  catalog.programs[0].generalizationEvidence = [];

  const errors = validateResearchCatalog(catalog);
  assert.ok(errors.some((error) => error.includes('unknownDataScenario')));
  assert.ok(errors.some((error) => error.includes('stopConditions')));
  assert.ok(errors.some((error) => error.includes('generalizationEvidence')));
});

test('rejects document paths that escape their product documentation roots', () => {
  assert.equal(
    isProductGrowthRoadmapPath('docs/roadmaps/product-growth/01-ranking-trust-and-methodology-roadmap.md'),
    true,
  );
  assert.equal(isProductGrowthRoadmapPath('docs/roadmaps/product-growth/../../operations/example.md'), false);
  assert.equal(isResearchExtensionPath('docs/product/passport-beta-validation-protocol.md'), true);
  assert.equal(isResearchExtensionPath('docs/product/../../deploy/README.md'), false);
});

test('requires all four explicit task outcomes in the evidence schema', () => {
  const catalog = validCatalog();
  catalog.evidenceSchema.find((field) => field.id === 'outcome').constraint = 'unassisted or failed';

  const errors = validateResearchCatalog(catalog);
  assert.ok(errors.some((error) => error.includes('assisted')));
  assert.ok(errors.some((error) => error.includes('not-observable')));
});

test('binds each program to its own roadmap, final gate and mandatory evidence', () => {
  const catalog = validCatalog();
  catalog.programs[1].roadmap = catalog.programs[0].roadmap;
  catalog.programs[1].gate = 'WRONG-G';
  catalog.evidenceSchema.find((field) => field.id === 'objective').required = false;

  const errors = validateResearchCatalog(catalog);
  assert.ok(errors.some((error) => error.includes('roadmap déjà attribuée')));
  assert.ok(errors.some((error) => error.includes('gate attendue PASS-G')));
  assert.ok(errors.some((error) => error.includes('objective') && error.includes('true')));
});
