const databaseName = process.env.MONGO_APP_DATABASE || 'AmusementPark';
const database = db.getSiblingDB(databaseName);
const migrationId = 'hist-04-history-events-v1';
const cutoverVersion = 'hist-canonical-cutover-v1';
const canonicalizationVersion = 'hist-canonical-v2';
const narrativeCollectionName = 'historical-narratives';
const supersededCollectionNames = [
  'historyEvents',
  'history-events-cutover-source-hist-04-v1',
  'history-events-backup-hist-04-v1',
  'historical-narratives-cutover-backup-hist-canonical-v1',
  'historical-facts-cutover-backup-hist-canonical-v1',
  'historical-sources-cutover-backup-hist-canonical-v1',
  'historical-cutover-state-hist-canonical-v1',
];

if (database.getCollectionInfos({ name: narrativeCollectionName }).length > 0) {
  const unfreezeResult = database.runCommand({
    collMod: narrativeCollectionName,
    validator: {},
    validationLevel: 'off',
  });
  if (!unfreezeResult.ok) {
    throw new Error('Could not release canonical historical narrative writes.');
  }
}

const narratives = database.getCollection(narrativeCollectionName);
const facts = database.getCollection('historical-facts');
const sources = database.getCollection('historical-sources');
const relations = database.getCollection('historical-relations');
const legacyRevisionFilter = { revisionOrigin: 'LegacyMigration' };
const legacyFactsWithoutNarrative = facts.countDocuments({
  ...legacyRevisionFilter,
  $or: [
    { narrativeContentId: { $exists: false } },
    { narrativeContentId: null },
    { narrativeContentId: '' },
  ],
});
if (legacyFactsWithoutNarrative > 0) {
  throw new Error('Legacy historical facts without a recoverable narrative cannot be removed safely.');
}

const legacyNarrativeIds = facts.distinct('narrativeContentId', legacyRevisionFilter);
const completedLegacyNarratives = legacyNarrativeIds.length === 0
  ? 0
  : narratives.countDocuments({
    _id: { $in: legacyNarrativeIds },
    migrationVersion: canonicalizationVersion,
    canonicalizationState: { $in: ['Canonicalized', 'Blocked'] },
  });
if (completedLegacyNarratives !== legacyNarrativeIds.length) {
  throw new Error('Legacy historical facts still depend on a non-final narrative conversion.');
}

const referencedLegacySourceIds = new Set(facts.distinct('sources.sourceId', legacyRevisionFilter));
const legacySourceIds = sources.distinct('sourceId', legacyRevisionFilter);
const orphanedLegacySourceIds = legacySourceIds.filter(
  sourceId => !referencedLegacySourceIds.has(sourceId));
if (orphanedLegacySourceIds.length > 0) {
  throw new Error('Legacy historical sources without a converted fact cannot be removed safely.');
}
const ordinaryFactsUsingLegacySources = legacySourceIds.length === 0
  ? 0
  : facts.countDocuments({
    revisionOrigin: { $ne: 'LegacyMigration' },
    'sources.sourceId': { $in: legacySourceIds },
  });
if (ordinaryFactsUsingLegacySources > 0) {
  throw new Error('Canonical historical facts still reference legacy source revisions.');
}
const ordinaryRelationsUsingLegacySources = legacySourceIds.length === 0
  ? 0
  : relations.countDocuments({
    revisionOrigin: { $ne: 'LegacyMigration' },
    'sources.sourceId': { $in: legacySourceIds },
  });
if (ordinaryRelationsUsingLegacySources > 0) {
  throw new Error('Canonical historical relations still reference legacy source revisions.');
}

const legacyRelationCount = relations.countDocuments(legacyRevisionFilter);
if (legacyRelationCount > 0) {
  throw new Error('Unexpected legacy historical relations require an explicit migration.');
}

const deletedLegacyFacts = facts.deleteMany(legacyRevisionFilter);
const deletedLegacySources = sources.deleteMany(legacyRevisionFilter);
const normalizedNarratives = narratives.updateMany(
  { cutoverVersion },
  {
    $unset: {
      cutoverVersion: '',
      cutoverPreviousCanonicalFactId: '',
    },
  },
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
  deletedLegacyFacts: deletedLegacyFacts.deletedCount,
  deletedLegacySources: deletedLegacySources.deletedCount,
  deletedAnomalies: deletedAnomalies.deletedCount,
  deletedMigrationStates: deletedMigrationStates.deletedCount,
  droppedCollections,
});
