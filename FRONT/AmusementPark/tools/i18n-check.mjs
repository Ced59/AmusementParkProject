import fs from 'node:fs';
import path from 'node:path';

const root = process.cwd();
const i18nDir = path.join(root, 'src', 'assets', 'i18n');
const overridesDir = path.join(i18nDir, 'overrides');
const sourceDir = path.join(root, 'src', 'app');
const languages = ['en', 'fr', 'es', 'de', 'it', 'nl', 'pl', 'pt'];

function readJson(filePath) {
  return JSON.parse(fs.readFileSync(filePath, 'utf8'));
}

function deepMerge(base, override) {
  const result = { ...base };

  for (const [key, value] of Object.entries(override)) {
    const baseValue = result[key];

    if (
      value &&
      typeof value === 'object' &&
      !Array.isArray(value) &&
      baseValue &&
      typeof baseValue === 'object' &&
      !Array.isArray(baseValue)
    ) {
      result[key] = deepMerge(baseValue, value);
      continue;
    }

    result[key] = value;
  }

  return result;
}

function readMergedLanguage(language) {
  const baseFilePath = path.join(i18nDir, `${language}.json`);
  const overrideFilePath = path.join(overridesDir, `${language}.json`);
  const base = readJson(baseFilePath);

  if (!fs.existsSync(overrideFilePath)) {
    return base;
  }

  return deepMerge(base, readJson(overrideFilePath));
}

function flatten(value, prefix = '') {
  if (value && typeof value === 'object' && !Array.isArray(value)) {
    return Object.entries(value).flatMap(([key, child]) => {
      const nextPrefix = prefix ? `${prefix}.${key}` : key;
      return flatten(child, nextPrefix);
    });
  }

  return [[prefix, value]];
}

function walkFiles(directory, extensions) {
  const result = [];

  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const fullPath = path.join(directory, entry.name);

    if (entry.isDirectory()) {
      result.push(...walkFiles(fullPath, extensions));
      continue;
    }

    if (extensions.some((extension) => entry.name.endsWith(extension))) {
      result.push(fullPath);
    }
  }

  return result;
}

function extractLiteralKeys() {
  const files = walkFiles(sourceDir, ['.ts', '.html']);
  const keys = new Set();
  const patterns = [
    /['"]([A-Za-z0-9_.-]+(?:\.[A-Za-z0-9_.-]+)+)['"]\s*\|\s*translate/g,
    /\.\s*(?:instant|get|stream)\s*\(\s*['"]([A-Za-z0-9_.-]+(?:\.[A-Za-z0-9_.-]+)+)['"]/g,
    /(?:labelKey|titleKey|subtitleKey|textKey|kickerLabelKey|placeholderTitleKey|placeholderMessageKey|emptyStateTitleKey|emptyStateMessageKey|badgeKey|descriptionTitleKey|emptyDescriptionKey|sourceLinkLabelKey|categoryLabelKey|allCategoriesLabelKey|currentLabelKey|lightboxTitleKey|previousLabelKey|nextLabelKey|closeFullscreenLabelKey|openFullscreenLabelKey|displayCountLabelKey|clearActionLabelKey|backLabelKey|metaLabelKey|routeLabelKey|valueKey|messageKey|summaryKey|ariaLabelKey|tooltipKey)\s*[:=]\s*['"]([A-Za-z0-9_.-]+(?:\.[A-Za-z0-9_.-]+)+)['"]/g
  ];

  for (const filePath of files) {
    const content = fs.readFileSync(filePath, 'utf8');

    for (const pattern of patterns) {
      for (const match of content.matchAll(pattern)) {
        keys.add(match[1]);
      }
    }
  }

  return keys;
}

const flattenedByLanguage = new Map();
const errors = [];
const warnings = [];

for (const language of languages) {
  const filePath = path.join(i18nDir, `${language}.json`);

  if (!fs.existsSync(filePath)) {
    errors.push(`Missing i18n file: ${filePath}`);
    continue;
  }

  const entries = new Map(flatten(readMergedLanguage(language)));
  flattenedByLanguage.set(language, entries);

  for (const [key, value] of entries) {
    if (typeof value === 'string' && value.trim().length === 0) {
      errors.push(`${language}: empty translation for ${key}`);
    }
  }
}

const english = flattenedByLanguage.get('en') ?? new Map();
const englishKeys = new Set(english.keys());

for (const language of languages.filter((item) => item !== 'en')) {
  const entries = flattenedByLanguage.get(language) ?? new Map();
  const keys = new Set(entries.keys());

  for (const key of englishKeys) {
    if (!keys.has(key)) {
      errors.push(`${language}: missing key ${key}`);
    }
  }

  for (const key of keys) {
    if (!englishKeys.has(key)) {
      errors.push(`${language}: key not present in merged en translations: ${key}`);
    }
  }
}

const usedKeys = extractLiteralKeys();
for (const key of usedKeys) {
  if (!englishKeys.has(key)) {
    errors.push(`Used translation key is missing from merged en translations: ${key}`);
  }
}

const sameAsEnglishCandidates = [];
for (const language of languages.filter((item) => item !== 'en')) {
  const entries = flattenedByLanguage.get(language) ?? new Map();

  for (const [key, value] of entries) {
    const englishValue = english.get(key);

    if (typeof value === 'string' && typeof englishValue === 'string' && value.trim() === englishValue.trim()) {
      sameAsEnglishCandidates.push(`${language}: ${key}`);
    }
  }
}

if (sameAsEnglishCandidates.length > 0) {
  warnings.push(`${sameAsEnglishCandidates.length} same-as-English candidates remain. Review them manually because some are valid names, acronyms or technical labels.`);
}

const unusedLiteralKeys = [...englishKeys].filter((key) => !usedKeys.has(key));
if (unusedLiteralKeys.length > 0) {
  warnings.push(`${unusedLiteralKeys.length} keys are not referenced as static literals. Do not delete automatically: several keys are provided through component inputs or built dynamically.`);
}

for (const warning of warnings) {
  console.warn(`[i18n:warning] ${warning}`);
}

if (errors.length > 0) {
  for (const error of errors) {
    console.error(`[i18n:error] ${error}`);
  }

  process.exit(1);
}

console.log(`i18n check passed for ${languages.length} languages and ${englishKeys.size} merged keys.`);
