import fs from 'node:fs';
import path from 'node:path';

const root = process.cwd();
const i18nDir = path.join(root, 'src', 'assets', 'i18n');
const sourceRoot = path.join(i18nDir, 'source');
const languages = ['en', 'fr', 'es', 'de', 'it', 'nl', 'pl', 'pt'];
const checkOnly = process.argv.includes('--check');

function readJson(filePath) {
  return JSON.parse(fs.readFileSync(filePath, 'utf8'));
}

function writeJson(filePath, value) {
  fs.writeFileSync(filePath, `${JSON.stringify(value, null, 2)}\n`, 'utf8');
}

function isPlainObject(value) {
  return value && typeof value === 'object' && !Array.isArray(value);
}

function deepMerge(base, override) {
  const result = { ...base };

  for (const [key, value] of Object.entries(override)) {
    const baseValue = result[key];

    if (isPlainObject(baseValue) && isPlainObject(value)) {
      result[key] = deepMerge(baseValue, value);
      continue;
    }

    result[key] = value;
  }

  return result;
}

function walkJsonFiles(directory) {
  const result = [];

  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const fullPath = path.join(directory, entry.name);

    if (entry.isDirectory()) {
      result.push(...walkJsonFiles(fullPath));
      continue;
    }

    if (entry.isFile() && entry.name.endsWith('.json')) {
      result.push(fullPath);
    }
  }

  return result.sort((left, right) => left.localeCompare(right, 'en'));
}

function buildLanguage(language) {
  const languageSourceDir = path.join(sourceRoot, language);

  if (!fs.existsSync(languageSourceDir)) {
    throw new Error(`Missing i18n source directory: ${languageSourceDir}`);
  }

  const files = walkJsonFiles(languageSourceDir);

  if (files.length === 0) {
    throw new Error(`No i18n source files found for ${language}.`);
  }

  return files.reduce((translations, filePath) => deepMerge(translations, readJson(filePath)), {});
}

const outdatedFiles = [];

for (const language of languages) {
  const outputPath = path.join(i18nDir, `${language}.json`);
  const translations = buildLanguage(language);
  const nextContent = `${JSON.stringify(translations, null, 2)}\n`;

  if (checkOnly) {
    const currentContent = fs.existsSync(outputPath) ? fs.readFileSync(outputPath, 'utf8') : '';

    if (currentContent !== nextContent) {
      outdatedFiles.push(path.relative(root, outputPath));
    }

    continue;
  }

  writeJson(outputPath, translations);
}

if (outdatedFiles.length > 0) {
  for (const filePath of outdatedFiles) {
    console.error(`[i18n:build] Generated file is not up to date: ${filePath}`);
  }

  process.exit(1);
}

if (checkOnly) {
  console.log(`i18n generated files are up to date for ${languages.length} languages.`);
} else {
  console.log(`i18n files generated for ${languages.length} languages.`);
}
