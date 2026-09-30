import { readFile } from 'node:fs/promises';
import { dirname, isAbsolute, relative, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const toolDirectory = dirname(fileURLToPath(import.meta.url));
const repositoryRoot = resolve(toolDirectory, '../../../..');
const defaultCatalogPath = resolve(repositoryRoot, 'docs/product/research/catalog.json');
const roadmapRoot = resolve(repositoryRoot, 'docs/roadmaps/product-growth');
const productDocumentationRoot = resolve(repositoryRoot, 'docs/product');

export const requiredProgramIds = Object.freeze([
  'RANK',
  'PASS',
  'SHARE',
  'FIT',
  'WATCH',
  'TRIP',
  'HIST',
  'LIVE',
]);

export const requiredRoadmapByProgram = Object.freeze({
  RANK: 'docs/roadmaps/product-growth/01-ranking-trust-and-methodology-roadmap.md',
  PASS: 'docs/roadmaps/product-growth/02-visit-passport-and-ride-log-roadmap.md',
  SHARE: 'docs/roadmaps/product-growth/03-shareable-recaps-and-comparisons-roadmap.md',
  FIT: 'docs/roadmaps/product-growth/04-park-fit-recommendation-and-comparison-roadmap.md',
  WATCH: 'docs/roadmaps/product-growth/05-favorites-watchlists-and-factual-alerts-roadmap.md',
  TRIP: 'docs/roadmaps/product-growth/06-collaborative-trip-planning-roadmap.md',
  HIST: 'docs/roadmaps/product-growth/07-park-history-explorer-roadmap.md',
  LIVE: 'docs/roadmaps/product-growth/08-live-wait-times-and-crowd-intelligence-roadmap.md',
});

export const requiredExtensionDocumentsByProgram = Object.freeze({
  PASS: Object.freeze([
    'docs/product/passport-beta-validation-protocol.md',
  ]),
});

export const requiredProfileIds = Object.freeze([
  'journal-enthusiast',
  'tool-free-enthusiast',
  'occasional-visitor',
  'family-planner',
  'assistive-technology',
  'modest-device-network',
  'historical-contributor',
]);

export const requiredTaskIds = Object.freeze([
  'consent',
  'primary-value',
  'core-concept-distinction',
  'label-comprehension',
  'privacy-export-deletion',
  'unknown-data',
  'accessible-use',
  'delayed-return',
]);

export const requiredEvidenceIds = Object.freeze([
  'objective',
  'profile',
  'scenario',
  'outcome',
  'observedFacts',
  'authorizedQuote',
  'issues',
  'hypotheses',
  'decisions',
  'inconclusive',
]);

export const requiredBetaGateIds = Object.freeze([
  'internal-alpha',
  'closed-beta',
  'limited-open-beta',
  'general-availability',
]);

export const requiredBetaGateRequirements = Object.freeze({
  'internal-alpha': Object.freeze([
    'données de test uniquement',
    'aucune publication publique',
    'instrumentation disponible',
    'erreurs visibles',
    'export et suppression vérifiés',
    'feature flag administrable',
  ]),
  'closed-beta': Object.freeze([
    'invitation manuelle',
    'données réelles',
    'consentement clair',
    'support direct',
    'migrations réversibles',
    'limites connues',
    'mesure qualitative',
  ]),
  'limited-open-beta': Object.freeze([
    'capacité VPS vérifiée',
    'support dimensionné',
    'modération dimensionnée',
    'monitoring disponible',
    'documentation disponible',
    'réponse aux incidents opérable',
    'aucune promesse de disponibilité excessive',
  ]),
  'general-availability': Object.freeze([
    'valeur répétée réellement observée',
    'erreurs sous le seuil décidé',
    'confidentialité validée',
    'accessibilité validée',
    'huit langues vérifiées',
    'performance vérifiée',
    'coûts acceptables',
    'flags temporaires retirés',
    'runbook disponible',
  ]),
});

export const requiredTaskContextFields = Object.freeze([
  'primaryScenario',
  'coreConceptDistinction',
  'labelReview',
  'privacyExportDeletionScope',
  'unknownDataScenario',
  'delayedReturnTrigger',
  'accessibleMode',
]);

export const requiredProtocolDocumentPaths = Object.freeze([
  'docs/product/research/README.md',
  'docs/product/research/session-result-template.md',
]);

export const requiredProtocolDocumentHeadings = Object.freeze({
  'docs/product/research/README.md': Object.freeze([
    '# Protocole commun de recherche produit',
    '## 2. Préparer une session',
    '## 3. Conduire les huit tâches',
    '## 4. Séparer les faits des décisions',
    '## 5. Passer une gate de bêta',
    "## 6. Conditions d'arrêt communes",
  ]),
  'docs/product/research/session-result-template.md': Object.freeze([
    '# Fiche de session produit',
    '## Cadre',
    '## Résultats par tâche',
    '## Problèmes',
    '## Synthèse minimisée',
    '## Clôture',
  ]),
});

const allowedOutcomeValues = ['unassisted', 'assisted', 'failed', 'not-observable'];

function isNonEmptyText(value) {
  return typeof value === 'string' && value.trim().length > 0;
}

function hasOnlyRequiredIds(items, requiredIds, label, errors) {
  if (!Array.isArray(items)) {
    errors.push(`${label}: tableau absent.`);
    return new Set();
  }

  const ids = new Set();
  for (const item of items) {
    if (!isNonEmptyText(item?.id)) {
      errors.push(`${label}: identifiant absent.`);
      continue;
    }
    if (ids.has(item.id)) {
      errors.push(`${label}: identifiant dupliqué ${item.id}.`);
    }
    ids.add(item.id);
  }

  for (const requiredId of requiredIds) {
    if (!ids.has(requiredId)) {
      errors.push(`${label}: identifiant obligatoire absent ${requiredId}.`);
    }
  }
  for (const id of ids) {
    if (!requiredIds.includes(id)) {
      errors.push(`${label}: identifiant inconnu ${id}.`);
    }
  }
  return ids;
}

function isPathInside(candidatePath, allowedRoot) {
  if (!isNonEmptyText(candidatePath) || !candidatePath.endsWith('.md')) {
    return false;
  }

  const candidate = resolve(repositoryRoot, candidatePath);
  const pathFromRoot = relative(allowedRoot, candidate);
  return pathFromRoot.length > 0
    && !isAbsolute(pathFromRoot)
    && pathFromRoot !== '..'
    && !pathFromRoot.startsWith(`..${process.platform === 'win32' ? '\\' : '/'}`);
}

export function isProductGrowthRoadmapPath(candidatePath) {
  return isPathInside(candidatePath, roadmapRoot);
}

export function isResearchExtensionPath(candidatePath) {
  return isPathInside(candidatePath, productDocumentationRoot);
}

export function validateResearchDocumentContent(documentPath, content) {
  if (!isNonEmptyText(content)) {
    return [`Document de recherche vide: ${documentPath}.`];
  }

  return (requiredProtocolDocumentHeadings[documentPath] ?? [])
    .filter((heading) => !content.includes(heading))
    .map((heading) => `Document ${documentPath}: section obligatoire absente ${heading}.`);
}

export function validateResearchCatalog(catalog) {
  const errors = [];
  const canonicalProfiles = Array.isArray(catalog?.canonicalProfiles) ? catalog.canonicalProfiles : [];
  const commonTasks = Array.isArray(catalog?.commonTasks) ? catalog.commonTasks : [];
  const evidenceSchema = Array.isArray(catalog?.evidenceSchema) ? catalog.evidenceSchema : [];
  const betaGates = Array.isArray(catalog?.betaGates) ? catalog.betaGates : [];
  const programs = Array.isArray(catalog?.programs) ? catalog.programs : [];
  if (catalog?.schemaVersion !== 1) {
    errors.push('Le catalogue de recherche doit utiliser schemaVersion 1.');
  }

  const statusPolicy = catalog?.statusPolicy;
  if (statusPolicy?.protocolStatus !== 'ready') {
    errors.push('La politique doit déclarer le protocole prêt.');
  }
  if (statusPolicy?.fieldEvidenceStatus !== 'pending') {
    errors.push('Les preuves terrain doivent rester pending tant qu’elles ne sont pas collectées.');
  }
  if (statusPolicy?.blocksDelivery !== false) {
    errors.push('Les observations terrain en attente ne doivent pas bloquer les livraisons indépendantes.');
  }
  if (statusPolicy?.allowGeneralizationWithoutEvidence !== false) {
    errors.push('La généralisation sans preuve terrain doit être interdite.');
  }
  if (!isNonEmptyText(statusPolicy?.statement)) {
    errors.push('La politique doit expliquer honnêtement le statut des preuves.');
  }

  hasOnlyRequiredIds(catalog?.canonicalProfiles, requiredProfileIds, 'Profils', errors);
  for (const profile of canonicalProfiles) {
    if (!isNonEmptyText(profile?.label)) {
      errors.push(`Profil ${profile?.id ?? '<sans-id>'}: libellé absent.`);
    }
  }

  hasOnlyRequiredIds(catalog?.commonTasks, requiredTaskIds, 'Tâches communes', errors);
  for (let index = 0; index < requiredTaskIds.length; index += 1) {
    if (commonTasks[index]?.id !== requiredTaskIds[index]) {
      errors.push(`Tâches communes: ${requiredTaskIds[index]} doit occuper la position ${index + 1}.`);
    }
  }
  for (const task of commonTasks) {
    if (!isNonEmptyText(task?.instruction) || !isNonEmptyText(task?.comparableMeasure)) {
      errors.push(`Tâche ${task?.id ?? '<sans-id>'}: instruction ou mesure comparable absente.`);
    }
  }

  hasOnlyRequiredIds(catalog?.evidenceSchema, requiredEvidenceIds, 'Schéma de preuve', errors);
  for (const field of evidenceSchema) {
    if (typeof field?.required !== 'boolean' || !isNonEmptyText(field?.constraint)) {
      errors.push(`Preuve ${field?.id ?? '<sans-id>'}: obligation ou contrainte absente.`);
    }
    const expectedRequired = field?.id !== 'authorizedQuote';
    if (requiredEvidenceIds.includes(field?.id) && field?.required !== expectedRequired) {
      errors.push(`Preuve ${field.id}: required doit valoir ${expectedRequired}.`);
    }
  }

  hasOnlyRequiredIds(catalog?.betaGates, requiredBetaGateIds, 'Gates bêta', errors);
  for (let index = 0; index < requiredBetaGateIds.length; index += 1) {
    if (betaGates[index]?.id !== requiredBetaGateIds[index]) {
      errors.push(`Gates bêta: ${requiredBetaGateIds[index]} doit occuper la position ${index + 1}.`);
    }
  }
  for (const gate of betaGates) {
    if (!Array.isArray(gate?.requirements)) {
      errors.push(`Gate ${gate?.id ?? '<sans-id>'}: exigences insuffisantes.`);
      continue;
    }
    if (gate.requirements.length < 3
      || gate.requirements.some((requirement) => !isNonEmptyText(requirement))) {
      errors.push(`Gate ${gate?.id ?? '<sans-id>'}: exigences insuffisantes.`);
    }

    const canonicalRequirements = requiredBetaGateRequirements[gate?.id];
    if (!canonicalRequirements) {
      continue;
    }
    if (gate.requirements.length !== canonicalRequirements.length) {
      errors.push(`Gate ${gate.id}: ${canonicalRequirements.length} exigences canoniques attendues.`);
    }
    for (let index = 0; index < canonicalRequirements.length; index += 1) {
      if (gate.requirements[index] !== canonicalRequirements[index]) {
        errors.push(`Gate ${gate.id}: exigence attendue en position ${index + 1}: ${canonicalRequirements[index]}.`);
      }
    }
  }

  hasOnlyRequiredIds(catalog?.programs, requiredProgramIds, 'Programmes', errors);
  const knownProfiles = new Set(requiredProfileIds);
  const roadmapPaths = new Set();
  for (const program of programs ?? []) {
    const programId = program?.id ?? '<sans-id>';
    for (const field of ['gate', 'businessQuestion', 'firstSuccess']) {
      if (!isNonEmptyText(program?.[field])) {
        errors.push(`Programme ${programId}: champ ${field} absent.`);
      }
    }

    if (!isProductGrowthRoadmapPath(program?.roadmap)) {
      errors.push(`Programme ${programId}: roadmap hors du périmètre produit.`);
    } else if (roadmapPaths.has(program.roadmap)) {
      errors.push(`Programme ${programId}: roadmap déjà attribuée à un autre programme.`);
    } else {
      roadmapPaths.add(program.roadmap);
    }

    if (requiredRoadmapByProgram[programId] && program?.roadmap !== requiredRoadmapByProgram[programId]) {
      errors.push(`Programme ${programId}: roadmap canonique attendue ${requiredRoadmapByProgram[programId]}.`);
    }

    if (program?.gate !== `${programId}-G`) {
      errors.push(`Programme ${programId}: gate attendue ${programId}-G.`);
    }

    if (!Array.isArray(program?.profileIds) || program.profileIds.length < 3) {
      errors.push(`Programme ${programId}: au moins trois profils sont requis.`);
    } else {
      const profileIds = new Set(program.profileIds);
      if (profileIds.size !== program.profileIds.length) {
        errors.push(`Programme ${programId}: profil dupliqué.`);
      }
      for (const profileId of profileIds) {
        if (!knownProfiles.has(profileId)) {
          errors.push(`Programme ${programId}: profil inconnu ${profileId}.`);
        }
      }
      for (const accessibilityProfile of ['assistive-technology', 'modest-device-network']) {
        if (!profileIds.has(accessibilityProfile)) {
          errors.push(`Programme ${programId}: profil obligatoire absent ${accessibilityProfile}.`);
        }
      }
    }

    for (const field of requiredTaskContextFields) {
      if (!isNonEmptyText(program?.taskContext?.[field])) {
        errors.push(`Programme ${programId}: contexte de tâche absent ${field}.`);
      }
    }

    for (const [field, minimum] of [['stopConditions', 2], ['generalizationEvidence', 2]]) {
      if (!Array.isArray(program?.[field])
        || program[field].length < minimum
        || program[field].some((value) => !isNonEmptyText(value))) {
        errors.push(`Programme ${programId}: ${field} insuffisant.`);
      }
    }

    if (!Array.isArray(program?.extensionDocuments)) {
      errors.push(`Programme ${programId}: extensionDocuments doit être un tableau.`);
    } else {
      const extensionPaths = new Set();
      for (const extensionPath of program.extensionDocuments) {
        if (!isResearchExtensionPath(extensionPath)) {
          errors.push(`Programme ${programId}: extension hors du périmètre produit.`);
        } else if (extensionPaths.has(extensionPath)) {
          errors.push(`Programme ${programId}: extension dupliquée ${extensionPath}.`);
        } else {
          extensionPaths.add(extensionPath);
        }
      }

      for (const requiredExtensionPath of requiredExtensionDocumentsByProgram[programId] ?? []) {
        if (!extensionPaths.has(requiredExtensionPath)) {
          errors.push(`Programme ${programId}: extension canonique absente ${requiredExtensionPath}.`);
        }
      }
    }
  }

  const outcomeConstraint = evidenceSchema.find((field) => field.id === 'outcome')?.constraint ?? '';
  for (const allowedOutcome of allowedOutcomeValues) {
    const outcomePattern = new RegExp(`(^|[^a-z-])${allowedOutcome}([^a-z-]|$)`, 'i');
    if (!outcomePattern.test(outcomeConstraint)) {
      errors.push(`Schéma de preuve: résultat obligatoire absent ${allowedOutcome}.`);
    }
  }

  return errors;
}

export async function validateResearchReadiness(catalogPath = defaultCatalogPath) {
  const catalog = JSON.parse(await readFile(catalogPath, 'utf8'));
  const errors = validateResearchCatalog(catalog);

  for (const program of Array.isArray(catalog.programs) ? catalog.programs : []) {
    const documentPaths = [program.roadmap, ...(program.extensionDocuments ?? [])];
    for (const documentPath of documentPaths) {
      if (!isProductGrowthRoadmapPath(documentPath) && !isResearchExtensionPath(documentPath)) {
        continue;
      }
      try {
        const content = await readFile(resolve(repositoryRoot, documentPath), 'utf8');
        errors.push(...validateResearchDocumentContent(documentPath, content));
      } catch (error) {
        errors.push(`Programme ${program.id}: document illisible ${documentPath} (${error instanceof Error ? error.code ?? error.message : 'erreur inconnue'}).`);
      }
    }
  }

  for (const documentPath of requiredProtocolDocumentPaths) {
    try {
      const content = await readFile(resolve(repositoryRoot, documentPath), 'utf8');
      errors.push(...validateResearchDocumentContent(documentPath, content));
    } catch (error) {
      errors.push(`Document QUAL-10 illisible ${documentPath} (${error instanceof Error ? error.code ?? error.message : 'erreur inconnue'}).`);
    }
  }

  return errors;
}

async function runCli() {
  const errors = await validateResearchReadiness();
  if (errors.length > 0) {
    process.stderr.write(`${errors.join('\n')}\n`);
    process.exitCode = 1;
    return;
  }

  process.stdout.write(`Préparation recherche validée: ${requiredProgramIds.length} programmes et ${requiredTaskIds.length} tâches comparables.\n`);
}

const invokedPath = process.argv[1] ? pathToFileURL(resolve(process.argv[1])).href : null;
if (invokedPath === import.meta.url) {
  await runCli();
}
