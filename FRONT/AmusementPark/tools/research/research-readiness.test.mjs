import assert from 'node:assert/strict';
import test from 'node:test';

import {
  isProductGrowthRoadmapPath,
  isResearchExtensionPath,
  requiredBetaGateIds,
  requiredBetaGateRequirements,
  requiredEvidenceIds,
  requiredExtensionDocumentsByProgram,
  requiredProfileIds,
  requiredProgramIds,
  requiredRoadmapByProgram,
  requiredTaskContextFields,
  requiredTaskIds,
  validateResearchCatalog,
  validateResearchDocumentContent,
  validateResearchReadiness,
} from './check-research-readiness.mjs';

function item(id, fields = {}) {
  return { id, ...fields };
}

function validProgram(id) {
  return {
    id,
    roadmap: requiredRoadmapByProgram[id],
    gate: `${id}-G`,
    businessQuestion: 'Question métier',
    firstSuccess: 'Premier succès',
    profileIds: ['journal-enthusiast', 'assistive-technology', 'modest-device-network'],
    taskContext: Object.fromEntries(requiredTaskContextFields.map((field) => [field, field])),
    stopConditions: ['stop one', 'stop two'],
    generalizationEvidence: ['proof one', 'proof two'],
    extensionDocuments: [...(requiredExtensionDocumentsByProgram[id] ?? [])],
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
      requirements: [...requiredBetaGateRequirements[id]],
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

test('rejects empty or structurally incomplete required protocol documents', () => {
  const readmePath = 'docs/product/research/README.md';
  assert.deepEqual(
    validateResearchDocumentContent(readmePath, '   \n'),
    [`Document de recherche vide: ${readmePath}.`],
  );

  const contentWithoutStopConditions = [
    '# Protocole commun de recherche produit',
    '## 2. Préparer une session',
    '## 3. Conduire les huit tâches',
    '## 4. Séparer les faits des décisions',
    '## 5. Passer une gate de bêta',
  ].join('\n');
  assert.deepEqual(
    validateResearchDocumentContent(readmePath, contentWithoutStopConditions),
    [`Document ${readmePath}: section obligatoire absente ## 6. Conditions d'arrêt communes.`],
  );

  const passportProtocolPath = 'docs/product/passport-beta-validation-protocol.md';
  const incompletePassportProtocol = [
    '# Protocole de validation de la bêta passeport',
    '## Objectif',
    '## Cohorte minimale',
    '## Préparation',
    '## Scénarios',
    '## Fiche de résultat',
  ].join('\n');
  const passportErrors = validateResearchDocumentContent(
    passportProtocolPath,
    incompletePassportProtocol,
  );
  assert.ok(passportErrors.some((error) => error.includes('Critères de validation qualitative')));
  assert.ok(passportErrors.some((error) => error.includes("Conditions d'arrêt")));

  const sessionTemplatePath = 'docs/product/research/session-result-template.md';
  const incompleteSessionTemplate = [
    '# Fiche de session produit',
    '## Cadre',
    '## Résultats par tâche',
    '## Problèmes',
    '## Synthèse minimisée',
    '## Clôture',
    ...requiredTaskIds
      .filter((taskId) => taskId !== 'delayed-return')
      .map((taskId) => `| ${taskId} | | | | |`),
  ].join('\n');
  assert.ok(validateResearchDocumentContent(sessionTemplatePath, incompleteSessionTemplate)
    .some((error) => error.includes('ligne de tâche obligatoire absente delayed-return')));
});

test('rejects a missing program and duplicate canonical task', () => {
  const catalog = validCatalog();
  catalog.programs = catalog.programs.slice(1);
  catalog.commonTasks[1].id = catalog.commonTasks[0].id;

  const errors = validateResearchCatalog(catalog);
  assert.ok(errors.some((error) => error.includes('Programme') && error.includes('RANK')));
  assert.ok(errors.some((error) => error.includes('dupliqué')));
});

test('enforces the canonical order of comparable tasks', () => {
  const catalog = validCatalog();
  [catalog.commonTasks[0], catalog.commonTasks[1]] = [catalog.commonTasks[1], catalog.commonTasks[0]];

  const errors = validateResearchCatalog(catalog);
  assert.ok(errors.some((error) => error.includes('consent') && error.includes('position 1')));
  assert.ok(errors.some((error) => error.includes('primary-value') && error.includes('position 2')));
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

test('requires every canonical beta gate requirement in its stable order', () => {
  const catalog = validCatalog();
  catalog.betaGates.find((gate) => gate.id === 'closed-beta').requirements.splice(1, 1);
  const generalAvailability = catalog.betaGates.find((gate) => gate.id === 'general-availability');
  [generalAvailability.requirements[0], generalAvailability.requirements[1]] = [
    generalAvailability.requirements[1],
    generalAvailability.requirements[0],
  ];
  [catalog.betaGates[0], catalog.betaGates[1]] = [catalog.betaGates[1], catalog.betaGates[0]];

  const errors = validateResearchCatalog(catalog);
  assert.ok(errors.some((error) => error.includes('closed-beta') && error.includes('données réelles')));
  assert.ok(errors.some((error) => error.includes('general-availability') && error.includes('position 1')));
  assert.ok(errors.some((error) => error.includes('internal-alpha') && error.includes('position 1')));
});

test('reports malformed catalog collections without throwing', () => {
  const catalog = validCatalog();
  catalog.commonTasks = {};
  catalog.evidenceSchema = null;
  catalog.betaGates = [{ id: 'internal-alpha' }];
  catalog.programs = {};

  const errors = validateResearchCatalog(catalog);
  assert.ok(errors.some((error) => error.includes('Tâches communes: tableau absent')));
  assert.ok(errors.some((error) => error.includes('Schéma de preuve: tableau absent')));
  assert.ok(errors.some((error) => error.includes('internal-alpha') && error.includes('insuffisantes')));
  assert.ok(errors.some((error) => error.includes('Programmes: tableau absent')));
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
  [catalog.programs[0].roadmap, catalog.programs[1].roadmap] = [
    catalog.programs[1].roadmap,
    catalog.programs[0].roadmap,
  ];
  catalog.programs[1].gate = 'WRONG-G';
  catalog.evidenceSchema.find((field) => field.id === 'objective').required = false;

  const errors = validateResearchCatalog(catalog);
  assert.ok(errors.some((error) => error.includes('RANK') && error.includes('roadmap canonique')));
  assert.ok(errors.some((error) => error.includes('PASS') && error.includes('roadmap canonique')));
  assert.ok(errors.some((error) => error.includes('gate attendue PASS-G')));
  assert.ok(errors.some((error) => error.includes('objective') && error.includes('true')));
});

test('keeps every canonical program extension attached to its protocol', () => {
  const catalog = validCatalog();
  const passportProgram = catalog.programs.find((program) => program.id === 'PASS');
  passportProgram.extensionDocuments = [];

  const errors = validateResearchCatalog(catalog);
  assert.ok(errors.some((error) => error.includes('PASS')
    && error.includes('extension canonique absente')
    && error.includes('passport-beta-validation-protocol.md')));
});
