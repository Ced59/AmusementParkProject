import { createHash } from 'node:crypto';
import { existsSync, readFileSync, readdirSync, statSync, writeFileSync } from 'node:fs';
import { dirname, extname, join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const repositoryRoot = resolve(scriptDirectory, '../../../..');
const catalogPath = join(repositoryRoot, 'docs/privacy/personal-data-catalog.json');
const documentRoot = join(
  repositoryRoot,
  'API/AmusementPark.Infrastructure/Persistence/Mongo/Documents',
);

const requiredPolicyKeys = [
  'purpose',
  'necessity',
  'visibility',
  'legalBasisOrConsent',
  'retention',
  'export',
  'deletion',
  'processor',
  'analytics',
  'encryption',
  'supportAccess',
  'audit',
];

const personalFieldPattern = /^(?:UserId|OwnerUserId|AuthorUserId|ActorUserId|CreatorUserId|AcceptorUserId|ReviewedByUserId|ChangedByUserId|RequestedByUserId|DecidedByUserId|CandidateUserId|AcceptingUserId|Email|ActorEmail|FirstName|LastName|PublicDisplayName|HashedPassword|TokenHash|IpAddress|UserAgent|PrivateComment|PrivateNote|TargetEmailHmac|InviterDisplayName|ReporterUserId|DraftOwnerId|Message)$/;

function normalizePath(value) {
  return value.replaceAll('\\', '/');
}

function listFilesRecursively(directory) {
  const files = [];

  for (const entry of readdirSync(directory)) {
    const absolutePath = join(directory, entry);
    if (statSync(absolutePath).isDirectory()) {
      files.push(...listFilesRecursively(absolutePath));
      continue;
    }

    files.push(absolutePath);
  }

  return files;
}

function resolveSource(source) {
  const normalizedSource = normalizePath(source);
  if (!normalizedSource.endsWith('/*Document.cs')) {
    return [resolve(repositoryRoot, normalizedSource)];
  }

  const directory = resolve(
    repositoryRoot,
    normalizedSource.slice(0, -'/*Document.cs'.length),
  );

  return readdirSync(directory)
    .filter((entry) => entry.endsWith('Document.cs'))
    .sort((left, right) => left.localeCompare(right))
    .map((entry) => join(directory, entry));
}

function readDocumentShape(absolutePath) {
  const source = readFileSync(absolutePath, 'utf8');
  const classMatch = source.match(/(?:public|internal)\s+(?:sealed\s+)?(?:partial\s+)?class\s+(\w+)/);
  const recordMatch = source.match(/(?:public|internal)\s+(?:sealed\s+)?(?:partial\s+)?record\s+(\w+)/);
  const typeName = classMatch?.[1] ?? recordMatch?.[1];
  const properties = [...source.matchAll(
    /public\s+(?:required\s+)?[^\r\n{]+?\s+(\w+)\s*\{\s*get;/g,
  )]
    .map((match) => match[1])
    .sort((left, right) => left.localeCompare(right));

  if (!typeName || properties.length === 0) {
    throw new Error(
      `Le document ${normalizePath(relative(repositoryRoot, absolutePath))} n'expose aucune forme persistée analysable.`,
    );
  }

  return {
    source: normalizePath(relative(repositoryRoot, absolutePath)),
    typeName,
    properties,
  };
}

export function computeSurfaceShape(surface) {
  const paths = surface.sources
    .flatMap(resolveSource)
    .sort((left, right) => left.localeCompare(right));
  const documents = paths.map(readDocumentShape);
  const canonicalShape = documents
    .map((document) => `${document.source}|${document.typeName}|${document.properties.join(',')}`)
    .join('\n');

  return {
    documents,
    digest: createHash('sha256').update(canonicalShape).digest('hex'),
  };
}

function validatePolicy(surface, errors) {
  for (const key of requiredPolicyKeys) {
    const value = surface.policy?.[key];
    if (typeof value !== 'string' || value.trim().length < 8) {
      errors.push(`${surface.id}: la politique « ${key} » est absente ou trop vague.`);
    }
  }
}

function validateDiscovery(catalog, coveredSources, errors) {
  const exclusions = new Map(
    catalog.discoveryExclusions.map((entry) => [normalizePath(entry.source), entry.reason]),
  );
  const candidates = listFilesRecursively(documentRoot)
    .filter((path) => extname(path) === '.cs' && path.endsWith('Document.cs'))
    .map(readDocumentShape);
  const candidatesBySource = new Map(
    candidates.map((candidate) => [candidate.source, candidate]),
  );

  for (const [source, reason] of exclusions) {
    if (typeof reason !== 'string' || reason.trim().length < 12) {
      errors.push(`${source}: l'exclusion de découverte doit être justifiée.`);
    }
    if (!existsSync(resolve(repositoryRoot, source))) {
      errors.push(`${source}: le fichier exclu de la découverte n'existe plus.`);
    }
    if (coveredSources.has(source)) {
      errors.push(`${source}: une source cataloguée ne doit pas aussi être exclue.`);
    }
    const candidate = candidatesBySource.get(source);
    if (candidate && !candidate.properties.some((property) => personalFieldPattern.test(property))) {
      errors.push(`${source}: exclusion obsolète, aucun marqueur personnel probable ne subsiste.`);
    }
  }

  for (const candidate of candidates) {
    const hasPersonalMarker = candidate.properties.some((property) => personalFieldPattern.test(property));
    if (!hasPersonalMarker) {
      continue;
    }

    if (!coveredSources.has(candidate.source) && !exclusions.has(candidate.source)) {
      errors.push(
        `${candidate.source}: contient un champ personnel probable mais n'est ni catalogué ni exclu explicitement.`,
      );
    }
  }
}

export function validateCatalog(catalog) {
  const errors = [];
  const coveredSources = new Set();
  let fieldCount = 0;

  if (catalog.schemaVersion !== 1) {
    errors.push('Le catalogue doit utiliser schemaVersion 1.');
  }
  if (!/^\d{4}-\d{2}-\d{2}$/.test(catalog.reviewedAt)) {
    errors.push('reviewedAt doit être une date ISO YYYY-MM-DD.');
  }
  if (!Array.isArray(catalog.surfaces) || catalog.surfaces.length === 0) {
    errors.push('Le catalogue ne contient aucune surface de données.');
  }
  if (!Array.isArray(catalog.discoveryExclusions)) {
    errors.push('discoveryExclusions doit être une liste explicite.');
  }

  const surfaceIds = new Set();
  for (const surface of catalog.surfaces ?? []) {
    if (surfaceIds.has(surface.id)) {
      errors.push(`La surface ${surface.id} est déclarée plusieurs fois.`);
    }
    surfaceIds.add(surface.id);

    if (!Array.isArray(surface.sources) || surface.sources.length === 0) {
      errors.push(`${surface.id}: aucune source persistée n'est déclarée.`);
      continue;
    }

    validatePolicy(surface, errors);

    let shape;
    try {
      shape = computeSurfaceShape(surface);
    } catch (error) {
      errors.push(`${surface.id}: ${error.message}`);
      continue;
    }

    if (shape.digest !== surface.reviewedSchemaSha256) {
      errors.push(
        `${surface.id}: la forme persistée a changé (${shape.digest}). Réexaminer chaque champ puis actualiser le catalogue.`,
      );
    }

    for (const document of shape.documents) {
      if (coveredSources.has(document.source)) {
        errors.push(`${document.source}: document classé dans plusieurs surfaces.`);
      }
      coveredSources.add(document.source);
      fieldCount += document.properties.length;
    }
  }

  validateDiscovery(catalog, coveredSources, errors);

  return {
    errors,
    fieldCount,
    documentCount: coveredSources.size,
  };
}

function refreshCatalog(catalog) {
  for (const surface of catalog.surfaces) {
    surface.reviewedSchemaSha256 = computeSurfaceShape(surface).digest;
  }
  catalog.reviewedAt = new Date().toISOString().slice(0, 10);
  writeFileSync(catalogPath, `${JSON.stringify(catalog, null, 2)}\n`, 'utf8');
}

export const personalDataCatalogPath = catalogPath;

function run() {
  const catalog = JSON.parse(readFileSync(catalogPath, 'utf8'));
  if (process.argv.includes('--refresh-reviewed-shapes')) {
    refreshCatalog(catalog);
    console.log('Empreintes du catalogue de données personnelles actualisées après revue.');
  }

  const currentCatalog = JSON.parse(readFileSync(catalogPath, 'utf8'));
  const result = validateCatalog(currentCatalog);
  if (result.errors.length > 0) {
    console.error('Catalogue de données personnelles invalide :');
    for (const error of result.errors) {
      console.error(`- ${error}`);
    }
    process.exitCode = 1;
  } else {
    console.log(
      `Catalogue vérifié : ${currentCatalog.surfaces.length} surfaces, ${result.documentCount} documents, ${result.fieldCount} champs persistés.`,
    );
  }
}

if (resolve(process.argv[1] ?? '') === fileURLToPath(import.meta.url)) {
  run();
}
