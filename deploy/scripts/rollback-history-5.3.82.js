const databaseName = process.env.MONGO_APP_DATABASE || 'AmusementPark';
const database = db.getSiblingDB(databaseName);
const legacyCollectionName = 'historyEvents';
const frozenCollectionName = 'history-events-cutover-source-hist-04-v1';
const migrationId = 'hist-04-history-events-v1';
const migrationActor = 'system:hist-04-migration';
const methodologyVersion = 'hist-v1-legacy';
const canonicalCutoverVersion = 'hist-canonical-cutover-v1';
const legacyInfo = database.getCollectionInfos({ name: legacyCollectionName });
const frozenExists = database.getCollectionInfos({ name: frozenCollectionName }).length > 0;

if (legacyInfo.length === 1 && legacyInfo[0].type === 'view'
    && (legacyInfo[0].options.viewOn !== frozenCollectionName || !frozenExists)) {
  throw new Error('The historical cutover view cannot be restored safely.');
}
if (legacyInfo.length === 1 && legacyInfo[0].type === 'collection' && frozenExists) {
  throw new Error('Both historical source collections exist; refusing an ambiguous rollback.');
}
if (legacyInfo.length === 1
    && legacyInfo[0].type !== 'view'
    && legacyInfo[0].type !== 'collection') {
  throw new Error('The legacy historical namespace has an unsupported type.');
}

const narratives = database.getCollection('historical-narratives');
const facts = database.getCollection('historical-facts');
const sources = database.getCollection('historical-sources');
const anomalies = database.getCollection('historical-migration-anomalies');
const migrations = database.getCollection('historical-migrations');
const backup = database.getCollection('history-events-backup-hist-04-v1');

const stagedNarrativeIds = narratives
  .find({ cutoverVersion: canonicalCutoverVersion }, { _id: 1 })
  .toArray()
  .map(document => document._id.toString());
const stagedFacts = stagedNarrativeIds.length === 0
  ? []
  : facts.find(
    { narrativeContentId: { $in: stagedNarrativeIds } },
    { sources: 1 },
  ).toArray();
const stagedSourceIds = [...new Set(stagedFacts.flatMap(document =>
  (document.sources || []).map(reference => reference.sourceId),
))];
const canonicalFactResult = stagedNarrativeIds.length === 0
  ? { deletedCount: 0 }
  : facts.deleteMany({ narrativeContentId: { $in: stagedNarrativeIds } });
const canonicalSourceResult = stagedSourceIds.length === 0
  ? { deletedCount: 0 }
  : sources.deleteMany({ sourceId: { $in: stagedSourceIds } });
const canonicalNarrativeResult = narratives.deleteMany({
  cutoverVersion: canonicalCutoverVersion,
});

const narrativeResult = narratives.deleteMany({ migrationVersion: migrationId });
const factResult = facts.deleteMany({
  revisionOrigin: 'LegacyMigration',
  publicationMethodologyVersion: methodologyVersion,
});
const sourceResult = sources.deleteMany({
  revisionOrigin: 'LegacyMigration',
  'transitionReviewEvent.actorUserId': migrationActor,
});
const anomalyResult = anomalies.deleteMany({ migrationId });
const migrationResult = migrations.deleteOne({ _id: migrationId });
const backupResult = backup.deleteMany({});

let legacyCollectionRestored = false;
if (legacyInfo.length === 1 && legacyInfo[0].type === 'view') {
  database.getCollection(legacyCollectionName).drop();
  database.getCollection(frozenCollectionName).renameCollection(legacyCollectionName, false);
  legacyCollectionRestored = true;
} else if (legacyInfo.length === 0 && frozenExists) {
  database.getCollection(frozenCollectionName).renameCollection(legacyCollectionName, false);
  legacyCollectionRestored = true;
} else if (legacyInfo.length === 1 && legacyInfo[0].type === 'collection') {
  const unfreezeResult = database.runCommand({
    collMod: legacyCollectionName,
    validator: {},
    validationLevel: 'off',
  });
  if (!unfreezeResult.ok) {
    throw new Error('Could not unfreeze the legacy historical collection.');
  }
  legacyCollectionRestored = true;
}

printjson({
  legacyCollectionRestored,
  deletedNarratives: narrativeResult.deletedCount,
  deletedCanonicalNarratives: canonicalNarrativeResult.deletedCount,
  deletedFacts: factResult.deletedCount,
  deletedCanonicalFacts: canonicalFactResult.deletedCount,
  deletedSources: sourceResult.deletedCount,
  deletedCanonicalSources: canonicalSourceResult.deletedCount,
  deletedAnomalies: anomalyResult.deletedCount,
  deletedMigrationStates: migrationResult.deletedCount,
  deletedBackupDocuments: backupResult.deletedCount,
});
