import assert from 'node:assert/strict';
import test from 'node:test';

import {
  isProductGrowthRoadmapPath,
  isResearchExtensionPath,
  requiredBetaGateIds,
  requiredBetaGateRequirements,
  requiredEvidenceConstraints,
  requiredEvidenceIds,
  requiredExtensionDocumentsByProgram,
  requiredProfileIds,
  requiredProfileIdsByProgram,
  requiredProfileLabels,
  requiredProtocolDocumentHeadings,
  requiredProgramDecisionsByProgram,
  requiredProgramIds,
  requiredRoadmapByProgram,
  requiredTaskContextFields,
  requiredTaskContextsByProgram,
  requiredTaskIds,
  requiredTaskSemantics,
  requiredStatusPolicyStatement,
  validateResearchCatalog,
  validateResearchDocumentContent,
  validateResearchReadiness,
} from './check-research-readiness.mjs';

function item(id, fields = {}) {
  return { id, ...fields };
}

function validProgram(id) {
  const profileIds = requiredProfileIdsByProgram[id]
    ?? ['journal-enthusiast', 'assistive-technology', 'modest-device-network'];
  const decision = requiredProgramDecisionsByProgram[id];
  return {
    id,
    roadmap: requiredRoadmapByProgram[id],
    gate: `${id}-G`,
    businessQuestion: decision.businessQuestion,
    firstSuccess: decision.firstSuccess,
    profileIds: [...profileIds],
    taskContext: { ...requiredTaskContextsByProgram[id] },
    stopConditions: [...decision.stopConditions],
    generalizationEvidence: [...decision.generalizationEvidence],
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
      statement: requiredStatusPolicyStatement,
    },
    canonicalProfiles: requiredProfileIds.map((id) => item(id, {
      label: requiredProfileLabels[id],
    })),
    commonTasks: requiredTaskIds.map((id) => item(id, requiredTaskSemantics[id])),
    evidenceSchema: requiredEvidenceIds.map((id) => item(id, {
      required: id !== 'authorizedQuote',
      constraint: requiredEvidenceConstraints[id],
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
  assert.ok(validateResearchDocumentContent(readmePath, contentWithoutStopConditions)
    .some((error) => error === `Document ${readmePath}: section obligatoire absente ## 6. Conditions d'arrêt communes.`));

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

  const emptyPassportProtocol = [
    '# Protocole de validation de la bêta passeport',
    '## Objectif',
    '## Cohorte minimale',
    '## Préparation',
    '## Scénarios',
    '## Questions après chaque session',
    '## Fiche de résultat',
    '## Critères de validation qualitative `PASS-G` — suivi non bloquant',
    "## Conditions d'arrêt ou de réduction",
  ].join('\n');
  const emptyPassportErrors = validateResearchDocumentContent(
    passportProtocolPath,
    emptyPassportProtocol,
  );
  assert.ok(emptyPassportErrors.some((error) => error.includes('section obligatoire vide ou insuffisante ## Scénarios')));
  assert.ok(emptyPassportErrors.some((error) => error.includes('section obligatoire vide ou insuffisante ## Critères de validation qualitative')));
  assert.ok(emptyPassportErrors.some((error) => error.includes("section obligatoire vide ou insuffisante ## Conditions d'arrêt")));
  const fencedPassportProtocol = `\`\`\`markdown\n${emptyPassportProtocol
    .split('\n')
    .map((heading) => `${heading}\nCe texte assez long reste un exemple non rendu et non exécutable.`)
    .join('\n')}\n\`\`\``;
  assert.ok(validateResearchDocumentContent(passportProtocolPath, fencedPassportProtocol)
    .some((error) => error.includes('section obligatoire absente ## Scénarios')));
  const longerFencedPassportProtocol = [
    '````markdown',
    '```',
    ...emptyPassportProtocol
      .split('\n')
      .flatMap((heading) => [heading, 'Ce contenu assez long reste un exemple non exécutable.']),
    '```',
    '````',
  ].join('\n');
  assert.ok(validateResearchDocumentContent(passportProtocolPath, longerFencedPassportProtocol)
    .some((error) => error.includes('section obligatoire absente ## Scénarios')));
  const indentedPassportProtocol = emptyPassportProtocol
    .split('\n')
    .map((heading) => `    ${heading}\n    Ce contenu reste un exemple de code non rendu.`)
    .join('\n');
  assert.ok(validateResearchDocumentContent(passportProtocolPath, indentedPassportProtocol)
    .some((error) => error.includes('section obligatoire absente ## Scénarios')));

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
    '<!--',
    '| delayed-return | ligne commentée et donc absente du document rendu | | | |',
    '-->',
  ].join('\n');
  assert.ok(validateResearchDocumentContent(sessionTemplatePath, incompleteSessionTemplate)
    .some((error) => error.includes('ligne de tâche obligatoire absente delayed-return')));

  const fencedTaskRows = [
    '# Fiche de session produit',
    'Ce modèle contient les informations nécessaires pour préparer une session.',
    '## Cadre',
    '- Date de suppression prévue pour cette fiche :',
    'Ce cadre décrit le contexte de recherche sans identifier la personne.',
    '## Résultats par tâche',
    'Les exemples suivants ne constituent pas un tableau de résultats utilisable.',
    '```markdown',
    ...requiredTaskIds.map((taskId) => `| ${taskId} | | | | |`),
    '```',
    '## Problèmes',
    'Les problèmes observés sont consignés ici avec leur sévérité.',
    '## Synthèse minimisée',
    'La synthèse sépare les faits, les hypothèses et les décisions.',
    '## Clôture',
    '- Fiche supprimée à la date prévue :',
    'La clôture confirme la suppression des données arrivées à échéance.',
  ].join('\n');
  const fencedTaskErrors = validateResearchDocumentContent(sessionTemplatePath, fencedTaskRows);
  for (const taskId of requiredTaskIds) {
    assert.ok(fencedTaskErrors.some((error) => error.includes(`ligne de tâche obligatoire absente ${taskId}`)));
  }
  const missingRetentionErrors = validateResearchDocumentContent(
    sessionTemplatePath,
    fencedTaskRows.replace('- Date de suppression prévue pour cette fiche :', ''),
  );
  assert.ok(missingRetentionErrors.some((error) => error.includes('champ de rétention obligatoire absent')));
  const narrativeTaskRows = fencedTaskRows.replace(
    /```markdown[\s\S]*?```/,
    requiredTaskIds
      .map((taskId) => `Référence narrative | ${taskId} | sans cellule de résultat`)
      .join('\n'),
  );
  const narrativeTaskErrors = validateResearchDocumentContent(sessionTemplatePath, narrativeTaskRows);
  assert.ok(narrativeTaskErrors.some((error) => error.includes('tableau de résultats canonique absent')));
  for (const taskId of requiredTaskIds) {
    assert.ok(narrativeTaskErrors.some((error) => error.includes(`ligne de tâche obligatoire absente ${taskId}`)));
  }
  const indentedTable = [
    '    | Tâche commune | Contexte produit présenté | Résultat | Faits observés | Aide minimale donnée |',
    '    |---|---|---|---|---|',
    ...requiredTaskIds.map((taskId) => `    | ${taskId} | | | | |`),
  ].join('\n');
  const indentedTableErrors = validateResearchDocumentContent(
    sessionTemplatePath,
    fencedTaskRows.replace(/```markdown[\s\S]*?```/, indentedTable),
  );
  assert.ok(indentedTableErrors.some((error) => error.includes('tableau de résultats canonique absent')));

  for (const [programId, roadmapPath] of Object.entries(requiredRoadmapByProgram)) {
    assert.ok(validateResearchDocumentContent(roadmapPath, '# Roadmap incomplète')
      .some((error) => error.includes(`Gate finale \`${programId}-G\``)
        || error.includes(`\`${programId}-G\` — socle technique`)));

    const [gateHeading] = requiredProtocolDocumentHeadings[roadmapPath];
    assert.ok(validateResearchDocumentContent(roadmapPath, gateHeading)
      .some((error) => error.includes('section obligatoire vide ou insuffisante')));
    assert.ok(validateResearchDocumentContent(
      roadmapPath,
      `${gateHeading}\n\n### Ceci est seulement un sous-titre descriptif assez long`,
    ).some((error) => error.includes('section obligatoire vide ou insuffisante')));
    assert.ok(validateResearchDocumentContent(
      roadmapPath,
      `${gateHeading}\n\n<!--\nCe critère est commenté et ne doit jamais rendre la gate exécutable.\n-->`,
    ).some((error) => error.includes('section obligatoire vide ou insuffisante')));
    assert.ok(validateResearchDocumentContent(
      roadmapPath,
      `\`\`\`markdown\n${gateHeading}\n\nCe critère assez long reste enfermé dans un exemple.\n\`\`\``,
    ).some((error) => error.includes('section obligatoire absente')));
    assert.ok(validateResearchDocumentContent(
      roadmapPath,
      `    ${gateHeading}\n\n    Ce critère assez long reste un bloc de code indenté.`,
    ).some((error) => error.includes('section obligatoire absente')));
  }
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

test('binds every comparable task to its canonical instruction and measure', () => {
  const catalog = validCatalog();
  const consent = catalog.commonTasks.find((task) => task.id === 'consent');
  const delayedReturn = catalog.commonTasks.find((task) => task.id === 'delayed-return');
  [consent.instruction, delayedReturn.instruction] = [
    delayedReturn.instruction,
    consent.instruction,
  ];
  [consent.comparableMeasure, delayedReturn.comparableMeasure] = [
    delayedReturn.comparableMeasure,
    consent.comparableMeasure,
  ];

  const errors = validateResearchCatalog(catalog);
  assert.ok(errors.some((error) => error.includes('consent') && error.includes('instruction canonique')));
  assert.ok(errors.some((error) => error.includes('consent') && error.includes('mesure comparable')));
  assert.ok(errors.some((error) => error.includes('delayed-return') && error.includes('instruction canonique')));
  assert.ok(errors.some((error) => error.includes('delayed-return') && error.includes('mesure comparable')));
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

test('preserves every mandatory research cohort for its product program', () => {
  const catalog = validCatalog();
  const passportProgram = catalog.programs.find((program) => program.id === 'PASS');
  passportProgram.profileIds = passportProgram.profileIds
    .filter((profileId) => profileId !== 'journal-enthusiast');
  passportProgram.profileIds.push('family-planner');

  const errors = validateResearchCatalog(catalog);
  assert.ok(errors.some((error) => error.includes('PASS')
    && error.includes('profil canonique absent journal-enthusiast')));

  const shareProgram = catalog.programs.find((program) => program.id === 'SHARE');
  const fitProgram = catalog.programs.find((program) => program.id === 'FIT');
  [shareProgram.profileIds, fitProgram.profileIds] = [
    fitProgram.profileIds,
    shareProgram.profileIds,
  ];
  const swappedErrors = validateResearchCatalog(catalog);
  assert.ok(swappedErrors.some((error) => error.includes('SHARE')
    && error.includes('profil canonique absent journal-enthusiast')));
  assert.ok(swappedErrors.some((error) => error.includes('FIT')
    && error.includes('profil canonique absent tool-free-enthusiast')));
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

test('binds every task context to its canonical product program', () => {
  const catalog = validCatalog();
  const rankingProgram = catalog.programs.find((program) => program.id === 'RANK');
  const passportProgram = catalog.programs.find((program) => program.id === 'PASS');
  [rankingProgram.taskContext, passportProgram.taskContext] = [
    passportProgram.taskContext,
    rankingProgram.taskContext,
  ];

  const errors = validateResearchCatalog(catalog);
  assert.ok(errors.some((error) => error.includes('RANK') && error.includes('contexte canonique modifié')));
  assert.ok(errors.some((error) => error.includes('PASS') && error.includes('contexte canonique modifié')));
});

test('binds gate decisions and evidence to their canonical product program', () => {
  const catalog = validCatalog();
  const rankingProgram = catalog.programs.find((program) => program.id === 'RANK');
  const passportProgram = catalog.programs.find((program) => program.id === 'PASS');
  [rankingProgram.businessQuestion, passportProgram.businessQuestion] = [
    passportProgram.businessQuestion,
    rankingProgram.businessQuestion,
  ];
  [rankingProgram.firstSuccess, passportProgram.firstSuccess] = [
    passportProgram.firstSuccess,
    rankingProgram.firstSuccess,
  ];
  [rankingProgram.stopConditions, passportProgram.stopConditions] = [
    passportProgram.stopConditions,
    rankingProgram.stopConditions,
  ];
  [rankingProgram.generalizationEvidence, passportProgram.generalizationEvidence] = [
    passportProgram.generalizationEvidence,
    rankingProgram.generalizationEvidence,
  ];

  const errors = validateResearchCatalog(catalog);
  for (const programId of ['RANK', 'PASS']) {
    for (const field of [
      'businessQuestion',
      'firstSuccess',
      'stopConditions',
      'generalizationEvidence',
    ]) {
      assert.ok(errors.some((error) => error.includes(programId)
        && error.includes(`décision canonique modifiée ${field}`)));
    }
  }
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

test('binds profile labels and evidence constraints to their canonical IDs', () => {
  const catalog = validCatalog();
  const journalProfile = catalog.canonicalProfiles.find((profile) => profile.id === 'journal-enthusiast');
  const occasionalProfile = catalog.canonicalProfiles.find((profile) => profile.id === 'occasional-visitor');
  [journalProfile.label, occasionalProfile.label] = [
    occasionalProfile.label,
    journalProfile.label,
  ];
  const observedFacts = catalog.evidenceSchema.find((field) => field.id === 'observedFacts');
  const hypotheses = catalog.evidenceSchema.find((field) => field.id === 'hypotheses');
  [observedFacts.constraint, hypotheses.constraint] = [
    hypotheses.constraint,
    observedFacts.constraint,
  ];
  catalog.statusPolicy.statement = 'Protocole prêt.';

  const errors = validateResearchCatalog(catalog);
  assert.ok(errors.some((error) => error.includes('journal-enthusiast')
    && error.includes('libellé canonique')));
  assert.ok(errors.some((error) => error.includes('occasional-visitor')
    && error.includes('libellé canonique')));
  assert.ok(errors.some((error) => error.includes('observedFacts')
    && error.includes('contrainte canonique')));
  assert.ok(errors.some((error) => error.includes('hypotheses')
    && error.includes('contrainte canonique')));
  assert.ok(errors.some((error) => error.includes('déclaration canonique')));
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

  const catalogWithWrongOwner = validCatalog();
  const rankingProgram = catalogWithWrongOwner.programs.find((program) => program.id === 'RANK');
  rankingProgram.extensionDocuments.push('docs/product/passport-beta-validation-protocol.md');
  const wrongOwnerErrors = validateResearchCatalog(catalogWithWrongOwner);
  assert.ok(wrongOwnerErrors.some((error) => error.includes('RANK')
    && error.includes('réservée à PASS')
    && error.includes('passport-beta-validation-protocol.md')));
});
