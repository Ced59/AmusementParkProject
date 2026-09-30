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

export function validateResearchCatalog(catalog) {
  const errors = [];
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
  for (const profile of catalog?.canonicalProfiles ?? []) {
    if (!isNonEmptyText(profile?.label)) {
      errors.push(`Profil ${profile?.id ?? '<sans-id>'}: libellé absent.`);
    }
  }

  hasOnlyRequiredIds(catalog?.commonTasks, requiredTaskIds, 'Tâches communes', errors);
  for (const task of catalog?.commonTasks ?? []) {
    if (!isNonEmptyText(task?.instruction) || !isNonEmptyText(task?.comparableMeasure)) {
      errors.push(`Tâche ${task?.id ?? '<sans-id>'}: instruction ou mesure comparable absente.`);
    }
  }

  hasOnlyRequiredIds(catalog?.evidenceSchema, requiredEvidenceIds, 'Schéma de preuve', errors);
  for (const field of catalog?.evidenceSchema ?? []) {
    if (typeof field?.required !== 'boolean' || !isNonEmptyText(field?.constraint)) {
      errors.push(`Preuve ${field?.id ?? '<sans-id>'}: obligation ou contrainte absente.`);
    }
    const expectedRequired = field?.id !== 'authorizedQuote';
    if (requiredEvidenceIds.includes(field?.id) && field?.required !== expectedRequired) {
      errors.push(`Preuve ${field.id}: required doit valoir ${expectedRequired}.`);
    }
  }

  hasOnlyRequiredIds(catalog?.betaGates, requiredBetaGateIds, 'Gates bêta', errors);
  for (const gate of catalog?.betaGates ?? []) {
    if (!Array.isArray(gate?.requirements)
      || gate.requirements.length < 3
      || gate.requirements.some((requirement) => !isNonEmptyText(requirement))) {
      errors.push(`Gate ${gate?.id ?? '<sans-id>'}: exigences insuffisantes.`);
    }
  }

  const programs = catalog?.programs;
  hasOnlyRequiredIds(programs, requiredProgramIds, 'Programmes', errors);
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
    }
  }

  const outcomeConstraint = catalog?.evidenceSchema?.find((field) => field.id === 'outcome')?.constraint ?? '';
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

  for (const program of catalog.programs ?? []) {
    const documentPaths = [program.roadmap, ...(program.extensionDocuments ?? [])];
    for (const documentPath of documentPaths) {
      if (!isProductGrowthRoadmapPath(documentPath) && !isResearchExtensionPath(documentPath)) {
        continue;
      }
      try {
        await readFile(resolve(repositoryRoot, documentPath), 'utf8');
      } catch (error) {
        errors.push(`Programme ${program.id}: document illisible ${documentPath} (${error instanceof Error ? error.code ?? error.message : 'erreur inconnue'}).`);
      }
    }
  }

  for (const documentPath of requiredProtocolDocumentPaths) {
    try {
      await readFile(resolve(repositoryRoot, documentPath), 'utf8');
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
