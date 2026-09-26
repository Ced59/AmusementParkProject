const databaseName = process.env.MONGO_APP_DATABASE || 'AmusementPark';
const database = db.getSiblingDB(databaseName);
const legacyCollectionName = 'historyEvents';
const migrationId = 'hist-04-history-events-v1';
const migrationActor = 'system:hist-04-migration';
const methodologyVersion = 'hist-v1-legacy';

const narratives = database.getCollection('historical-narratives');
const facts = database.getCollection('historical-facts');
const sources = database.getCollection('historical-sources');
const anomalies = database.getCollection('historical-migration-anomalies');
const migrations = database.getCollection('historical-migrations');
const backup = database.getCollection('history-events-backup-hist-04-v1');

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

const legacyExists = database.getCollectionInfos({ name: legacyCollectionName }).length > 0;
if (legacyExists) {
  const unfreezeResult = database.runCommand({
    collMod: legacyCollectionName,
    validator: {},
    validationLevel: 'off',
  });
  if (!unfreezeResult.ok) {
    throw new Error('Could not unfreeze the legacy historical collection.');
  }
}

printjson({
  legacyCollectionUnfrozen: legacyExists,
  deletedNarratives: narrativeResult.deletedCount,
  deletedFacts: factResult.deletedCount,
  deletedSources: sourceResult.deletedCount,
  deletedAnomalies: anomalyResult.deletedCount,
  deletedMigrationStates: migrationResult.deletedCount,
  deletedBackupDocuments: backupResult.deletedCount,
});
