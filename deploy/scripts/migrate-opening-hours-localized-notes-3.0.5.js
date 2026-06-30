const databaseName = process.env.MONGO_APP_DATABASE || 'AmusementPark';
const openingHoursCollectionName = process.env.MONGO_PARK_OPENING_HOURS_COLLECTION || 'parkOpeningHours';
const dryRun = String(process.env.DRY_RUN || '').toLowerCase() === 'true';

const database = db.getSiblingDB(databaseName);
const collection = database.getCollection(openingHoursCollectionName);
const now = new Date();

function normalizeText(value) {
  return typeof value === 'string' && value.trim().length > 0 ? value.trim() : null;
}

function hasLocalizedValue(values, languageCode) {
  return Array.isArray(values)
    && values.some((value) => value
      && typeof value.languageCode === 'string'
      && value.languageCode.trim().toLowerCase() === languageCode
      && normalizeText(value.value));
}

function appendFrenchValue(values, legacyValue) {
  const normalizedValue = normalizeText(legacyValue);
  const localizedValues = Array.isArray(values) ? values.filter((value) => value && typeof value === 'object') : [];

  if (!normalizedValue || hasLocalizedValue(localizedValues, 'fr')) {
    return localizedValues;
  }

  localizedValues.push({
    languageCode: 'fr',
    value: normalizedValue,
  });

  return localizedValues;
}

function migrateEntry(entry) {
  if (!entry || typeof entry !== 'object') {
    return false;
  }

  let changed = false;

  if (Object.prototype.hasOwnProperty.call(entry, 'label')) {
    entry.labels = appendFrenchValue(entry.labels, entry.label);
    delete entry.label;
    changed = true;
  }

  if (Object.prototype.hasOwnProperty.call(entry, 'reason')) {
    entry.reasons = appendFrenchValue(entry.reasons, entry.reason);
    delete entry.reason;
    changed = true;
  }

  return changed;
}

const cursor = collection.find({
  $or: [
    { 'regularRules.label': { $exists: true } },
    { 'regularRules.reason': { $exists: true } },
    { 'dateOverrides.label': { $exists: true } },
    { 'dateOverrides.reason': { $exists: true } },
  ],
});

let matchedCount = 0;
let modifiedCount = 0;

cursor.forEach((document) => {
  matchedCount += 1;
  let changed = false;

  if (Array.isArray(document.regularRules)) {
    document.regularRules.forEach((rule) => {
      changed = migrateEntry(rule) || changed;
    });
  }

  if (Array.isArray(document.dateOverrides)) {
    document.dateOverrides.forEach((dateOverride) => {
      changed = migrateEntry(dateOverride) || changed;
    });
  }

  if (!changed) {
    return;
  }

  if (dryRun) {
    print(`[dry-run] ${openingHoursCollectionName}/${document._id}: opening-hour notes would be moved to fr labels/reasons`);
    return;
  }

  const result = collection.updateOne(
    { _id: document._id },
    {
      $set: {
        regularRules: document.regularRules || [],
        dateOverrides: document.dateOverrides || [],
        updatedAt: now,
      },
    },
  );

  modifiedCount += result.modifiedCount;
});

print(`opening-hour localized notes migration: matched=${matchedCount}, modified=${dryRun ? 0 : modifiedCount}, dryRun=${dryRun}`);
