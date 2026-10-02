const databaseName = process.env.MONGO_APP_DATABASE || 'AmusementPark';
const database = db.getSiblingDB(databaseName);
const migrationId = 'hist-04-history-events-v1';
const cutoverVersion = 'hist-canonical-cutover-v1';
const supersededCollectionNames = [
  'historyEvents',
  'history-events-cutover-source-hist-04-v1',
  'history-events-backup-hist-04-v1',
];

const narratives = database.getCollection('historical-narratives');
const normalizedNarratives = narratives.updateMany(
  { cutoverVersion },
  { $unset: { cutoverVersion: '' } },
);

const anomalies = database.getCollection('historical-migration-anomalies');
const deletedAnomalies = anomalies.deleteMany({ migrationId });
const migrations = database.getCollection('historical-migrations');
const deletedMigrationStates = migrations.deleteMany({ _id: migrationId });

const droppedCollections = [];
for (const collectionName of supersededCollectionNames) {
  if (database.getCollectionInfos({ name: collectionName }).length > 0) {
    database.getCollection(collectionName).drop();
    droppedCollections.push(collectionName);
  }
}

if (anomalies.countDocuments({}) === 0
    && database.getCollectionInfos({ name: 'historical-migration-anomalies' }).length > 0) {
  anomalies.drop();
  droppedCollections.push('historical-migration-anomalies');
}
if (migrations.countDocuments({}) === 0
    && database.getCollectionInfos({ name: 'historical-migrations' }).length > 0) {
  migrations.drop();
  droppedCollections.push('historical-migrations');
}

printjson({
  normalizedNarratives: normalizedNarratives.modifiedCount,
  deletedAnomalies: deletedAnomalies.deletedCount,
  deletedMigrationStates: deletedMigrationStates.deletedCount,
  droppedCollections,
});
