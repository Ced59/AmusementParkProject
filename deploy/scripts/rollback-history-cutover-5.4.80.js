const databaseName = process.env.MONGO_APP_DATABASE || 'AmusementPark';
const database = db.getSiblingDB(databaseName);
const legacyCollectionName = 'historyEvents';
const frozenCollectionName = 'history-events-cutover-source-hist-04-v1';
const narrativeCollectionName = 'historical-narratives';
const factCollectionName = 'historical-facts';
const sourceCollectionName = 'historical-sources';
const narrativeBackupName = 'historical-narratives-cutover-backup-hist-canonical-v1';
const factBackupName = 'historical-facts-cutover-backup-hist-canonical-v1';
const sourceBackupName = 'historical-sources-cutover-backup-hist-canonical-v1';
const cutoverStateName = 'historical-cutover-state-hist-canonical-v1';
const canonicalCutoverVersion = 'hist-canonical-cutover-v1';

function collectionExists(collectionName) {
  return database.getCollectionInfos({ name: collectionName }).length > 0;
}

function restoreDocuments(targetCollection, backupCollection) {
  let restoredCount = 0;
  backupCollection.find({}).forEach(document => {
    targetCollection.replaceOne({ _id: document._id }, document, { upsert: true });
    restoredCount += 1;
  });
  return restoredCount;
}

const legacyInfo = database.getCollectionInfos({ name: legacyCollectionName });
const frozenExists = collectionExists(frozenCollectionName);
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
  throw new Error('The historical source namespace has an unsupported type.');
}

if (collectionExists(narrativeCollectionName)) {
  const unfreezeResult = database.runCommand({
    collMod: narrativeCollectionName,
    validator: {},
    validationLevel: 'off',
  });
  if (!unfreezeResult.ok) {
    throw new Error('Could not restore historical narrative write authority.');
  }
}

const narratives = database.getCollection(narrativeCollectionName);
const facts = database.getCollection(factCollectionName);
const sources = database.getCollection(sourceCollectionName);
const narrativeBackup = database.getCollection(narrativeBackupName);
const factBackup = database.getCollection(factBackupName);
const sourceBackup = database.getCollection(sourceBackupName);
const cutoverState = database.getCollection(cutoverStateName);
const backupStageComplete = collectionExists(cutoverStateName)
  && cutoverState.findOne({ _id: canonicalCutoverVersion }) !== null;

const stagedNarrativeIds = narratives
  .find({ cutoverVersion: canonicalCutoverVersion }, { _id: 1 })
  .toArray()
  .map(document => document._id.toString());
const backedFactIds = collectionExists(factBackupName)
  ? factBackup.distinct('factId')
  : [];
const candidateFactFilter = {
  $or: [
    { narrativeContentId: { $in: stagedNarrativeIds } },
    { factId: { $in: backedFactIds } },
  ],
};
const candidateFacts = !backupStageComplete
    || (stagedNarrativeIds.length === 0 && backedFactIds.length === 0)
  ? []
  : facts.find(candidateFactFilter, { sources: 1 }).toArray();
const candidateSourceIds = candidateFacts.flatMap(document =>
  (document.sources || []).map(reference => reference.sourceId));
const backedSourceIds = collectionExists(sourceBackupName)
  ? sourceBackup.distinct('sourceId')
  : [];
const sourceIdsToRestore = [...new Set([...candidateSourceIds, ...backedSourceIds])];

const deletedFacts = candidateFacts.length === 0
  ? { deletedCount: 0 }
  : facts.deleteMany(candidateFactFilter);
const deletedSources = sourceIdsToRestore.length === 0
  ? { deletedCount: 0 }
  : sources.deleteMany({ sourceId: { $in: sourceIdsToRestore } });
const deletedNarratives = narratives.deleteMany({ cutoverVersion: canonicalCutoverVersion });

const restoredNarratives = collectionExists(narrativeBackupName)
  ? restoreDocuments(narratives, narrativeBackup)
  : 0;
const restoredFacts = collectionExists(factBackupName)
  ? restoreDocuments(facts, factBackup)
  : 0;
const restoredSources = collectionExists(sourceBackupName)
  ? restoreDocuments(sources, sourceBackup)
  : 0;

const droppedBackups = [];
for (const backupName of [
  narrativeBackupName,
  factBackupName,
  sourceBackupName,
  cutoverStateName,
]) {
  if (collectionExists(backupName)) {
    database.getCollection(backupName).drop();
    droppedBackups.push(backupName);
  }
}

let historicalSourceRestored = false;
if (legacyInfo.length === 1 && legacyInfo[0].type === 'view') {
  database.getCollection(legacyCollectionName).drop();
  database.getCollection(frozenCollectionName).renameCollection(legacyCollectionName, false);
  historicalSourceRestored = true;
} else if (legacyInfo.length === 0 && frozenExists) {
  database.getCollection(frozenCollectionName).renameCollection(legacyCollectionName, false);
  historicalSourceRestored = true;
} else if (legacyInfo.length === 1 && legacyInfo[0].type === 'collection') {
  historicalSourceRestored = true;
}

printjson({
  historicalSourceRestored,
  backupStageComplete,
  deletedNarratives: deletedNarratives.deletedCount,
  deletedFacts: deletedFacts.deletedCount,
  deletedSources: deletedSources.deletedCount,
  restoredNarratives,
  restoredFacts,
  restoredSources,
  droppedBackups,
});
