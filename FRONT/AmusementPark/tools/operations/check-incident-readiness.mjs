import { readFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const toolDirectory = dirname(fileURLToPath(import.meta.url));
const repositoryRoot = resolve(toolDirectory, '../../../..');
const defaultCatalogPath = resolve(repositoryRoot, 'docs/operations/incidents/catalog.json');
const defaultMonitorConfigPath = resolve(toolDirectory, 'production-alerts.config.json');

export const requiredIncidentIds = Object.freeze([
  'ranking-inconsistent',
  'orphaned-visit',
  'revoked-share-still-visible',
  'duplicate-notification',
  'live-source-unavailable',
  'account-export-blocked',
  'account-purge-failed',
  'privacy-incident',
  'production-rollback',
]);

export const requiredRunbookHeadings = Object.freeze([
  '## Déclenchement',
  '## Triage',
  '## Protection immédiate',
  '## Diagnostic',
  '## Rétablissement',
  '## Vérification',
  '## Escalade',
  '## Preuves à conserver',
]);

const allowedSeverities = new Set(['medium', 'high', 'critical']);
const allowedDetectionModes = new Set(['automated', 'admin-diagnostic', 'support-escalation']);
const requiredTextFields = [
  'id',
  'title',
  'product',
  'severity',
  'owner',
  'detectionMode',
  'signal',
  'threshold',
  'window',
  'runbook',
  'fallback',
  'recoveryProof',
];

export function validateCatalogStructure(catalog) {
  const errors = [];
  if (catalog?.schemaVersion !== 1) {
    errors.push('Le catalogue doit utiliser schemaVersion 1.');
  }

  if (!Array.isArray(catalog?.incidents)) {
    return [...errors, 'Le catalogue doit contenir un tableau incidents.'];
  }

  const ids = new Set();
  for (const incident of catalog.incidents) {
    for (const field of requiredTextFields) {
      if (typeof incident?.[field] !== 'string' || incident[field].trim().length === 0) {
        errors.push(`Incident ${incident?.id ?? '<sans-id>'}: champ ${field} absent.`);
      }
    }

    if (typeof incident?.id === 'string') {
      if (ids.has(incident.id)) {
        errors.push(`Incident dupliqué: ${incident.id}.`);
      }
      ids.add(incident.id);
    }

    if (!allowedSeverities.has(incident?.severity)) {
      errors.push(`Incident ${incident?.id ?? '<sans-id>'}: sévérité inconnue.`);
    }

    if (!allowedDetectionModes.has(incident?.detectionMode)) {
      errors.push(`Incident ${incident?.id ?? '<sans-id>'}: mode de détection inconnu.`);
    }

    if (typeof incident?.runbook === 'string'
      && (!incident.runbook.startsWith('docs/operations/incidents/') || !incident.runbook.endsWith('.md'))) {
      errors.push(`Incident ${incident?.id ?? '<sans-id>'}: chemin de runbook hors du catalogue opérationnel.`);
    }
  }

  for (const requiredId of requiredIncidentIds) {
    if (!ids.has(requiredId)) {
      errors.push(`Incident obligatoire absent: ${requiredId}.`);
    }
  }

  return errors;
}

export function validateRunbookContent(incidentId, content) {
  return requiredRunbookHeadings
    .filter((heading) => !content.includes(heading))
    .map((heading) => `Runbook ${incidentId}: section absente ${heading}.`);
}

export function validateMonitorConfig(config, catalog) {
  const errors = [];
  const incidentsById = new Map(catalog.incidents.map((incident) => [incident.id, incident]));
  if (!Number.isInteger(config?.confirmationAttempts) || config.confirmationAttempts < 2) {
    errors.push('Le moniteur doit confirmer un incident avec au moins deux tentatives.');
  }
  if (!Number.isInteger(config?.confirmationDelayMilliseconds) || config.confirmationDelayMilliseconds < 10000) {
    errors.push('Le délai de confirmation doit être d’au moins dix secondes.');
  }
  if (!Array.isArray(config?.baseline?.targets) || config.baseline.targets.length === 0) {
    errors.push('Le moniteur doit contenir au moins une cible.');
    return errors;
  }

  const targetKeys = new Set();
  for (const target of config.baseline.targets) {
    if (typeof target.key !== 'string' || target.key.length === 0 || targetKeys.has(target.key)) {
      errors.push(`Cible de moniteur invalide ou dupliquée: ${target.key ?? '<sans-clé>'}.`);
    }
    targetKeys.add(target.key);

    const incident = incidentsById.get(target.incidentId);
    if (!incident) {
      errors.push(`Cible ${target.key}: incident inconnu ${target.incidentId ?? '<absent>'}.`);
    } else if (incident.detectionMode !== 'automated') {
      errors.push(`Cible ${target.key}: l’incident ${target.incidentId} n’est pas déclaré automatisé.`);
    }
  }
  return errors;
}

export async function validateOperationalReadiness(catalogPath = defaultCatalogPath, monitorConfigPath = defaultMonitorConfigPath) {
  const catalog = JSON.parse(await readFile(catalogPath, 'utf8'));
  const monitorConfig = JSON.parse(await readFile(monitorConfigPath, 'utf8'));
  const errors = [
    ...validateCatalogStructure(catalog),
    ...validateMonitorConfig(monitorConfig, catalog),
  ];

  for (const incident of catalog.incidents ?? []) {
    try {
      const content = await readFile(resolve(repositoryRoot, incident.runbook), 'utf8');
      errors.push(...validateRunbookContent(incident.id, content));
    } catch (error) {
      errors.push(`Runbook ${incident.id} illisible: ${error instanceof Error ? error.code ?? error.message : 'erreur inconnue'}.`);
    }
  }

  return errors;
}

async function runCli() {
  const errors = await validateOperationalReadiness();
  if (errors.length > 0) {
    process.stderr.write(`${errors.join('\n')}\n`);
    process.exitCode = 1;
    return;
  }

  process.stdout.write(`Préparation opérationnelle validée: ${requiredIncidentIds.length} incidents et leurs runbooks.\n`);
}

const invokedPath = process.argv[1] ? pathToFileURL(resolve(process.argv[1])).href : null;
if (invokedPath === import.meta.url) {
  await runCli();
}
