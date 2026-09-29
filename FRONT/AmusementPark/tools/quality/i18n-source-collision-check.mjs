import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const TOOL_DIRECTORY = path.dirname(fileURLToPath(import.meta.url));
const FRONTEND_ROOT = path.resolve(TOOL_DIRECTORY, '..', '..');
const SOURCE_ROOT = path.join(FRONTEND_ROOT, 'src', 'assets', 'i18n', 'source');
const BASELINE_PATH = path.join(TOOL_DIRECTORY, 'i18n-source-collision-baseline.json');
const LANGUAGES = ['en', 'fr', 'es', 'de', 'it', 'nl', 'pl', 'pt'];

function walkJsonFiles(directory) {
  const files = [];

  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const fullPath = path.join(directory, entry.name);

    if (entry.isDirectory()) {
      files.push(...walkJsonFiles(fullPath));
      continue;
    }

    if (entry.name.endsWith('.json')) {
      files.push(fullPath);
    }
  }

  return files.sort((left, right) => left.localeCompare(right, 'en'));
}

function flattenDefinitions(value, prefix = '') {
  if (value && typeof value === 'object' && !Array.isArray(value)) {
    const branch = prefix ? [{ key: prefix, kind: 'branch' }] : [];
    const descendants = Object.entries(value).flatMap(([key, child]) => {
      const nextPrefix = prefix ? `${prefix}.${key}` : key;
      return flattenDefinitions(child, nextPrefix);
    });

    return [...branch, ...descendants];
  }

  return [{ key: prefix, kind: 'leaf' }];
}

export function findLeafCollisions(documents) {
  const definitionsByKey = new Map();

  for (const document of documents) {
    for (const definition of flattenDefinitions(document.value)) {
      const definitions = definitionsByKey.get(definition.key) ?? [];
      definitions.push({ owner: document.owner, kind: definition.kind });
      definitionsByKey.set(definition.key, definitions);
    }
  }

  return [...definitionsByKey.entries()]
    .filter(([, definitions]) => {
      const leafOwners = new Set(definitions.filter((definition) => definition.kind === 'leaf').map((definition) => definition.owner));
      const branchOwners = new Set(definitions.filter((definition) => definition.kind === 'branch').map((definition) => definition.owner));
      const allOwners = new Set(definitions.map((definition) => definition.owner));

      return leafOwners.size > 1 || (leafOwners.size > 0 && branchOwners.size > 0 && allOwners.size > 1);
    })
    .map(([key, definitions]) => ({
      key,
      owners: [...new Set(definitions.map((definition) => definition.owner))]
        .sort((left, right) => left.localeCompare(right, 'en'))
    }))
    .sort((left, right) => left.key.localeCompare(right.key, 'en'));
}

export function collisionFingerprint(language, collision) {
  return `${language}:${collision.key}:${collision.owners.join('|')}`;
}

function scanSources() {
  return LANGUAGES.flatMap((language) => {
    const languageRoot = path.join(SOURCE_ROOT, language);
    const documents = walkJsonFiles(languageRoot).map((filePath) => ({
      owner: path.relative(languageRoot, filePath).replaceAll('\\', '/'),
      value: JSON.parse(fs.readFileSync(filePath, 'utf8'))
    }));

    return findLeafCollisions(documents).map((collision) => ({
      fingerprint: collisionFingerprint(language, collision),
      language,
      key: collision.key,
      owners: collision.owners
    }));
  });
}

function run() {
  const current = scanSources();

  if (process.argv.includes('--update-baseline')) {
    fs.writeFileSync(BASELINE_PATH, `${JSON.stringify({ version: 1, collisions: current }, null, 2)}\n`, 'utf8');
    console.log(`i18n collision baseline updated with ${current.length} known collisions.`);
    return;
  }

  const baseline = JSON.parse(fs.readFileSync(BASELINE_PATH, 'utf8')).collisions ?? [];
  const currentFingerprints = new Set(current.map((collision) => collision.fingerprint));
  const baselineFingerprints = new Set(baseline.map((collision) => collision.fingerprint));
  const added = current.filter((collision) => !baselineFingerprints.has(collision.fingerprint));
  const resolved = baseline.filter((collision) => !currentFingerprints.has(collision.fingerprint));

  for (const collision of added) {
    console.error(`[i18n:collision] ${collision.fingerprint} is defined by ${collision.owners.join(', ')}`);
  }

  for (const collision of resolved) {
    console.error(`[i18n:baseline] Resolved collision still present in baseline: ${collision.fingerprint}`);
  }

  if (added.length > 0 || resolved.length > 0) {
    console.error('i18n source collision baseline changed. Remove accidental overrides before refreshing intentional debt reduction.');
    process.exit(1);
  }

  console.log(`i18n source collision check passed with ${current.length} tracked legacy collisions and no regression.`);
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  run();
}
