const databaseName = process.env.MONGO_APP_DATABASE || 'AmusementPark';
const database = db.getSiblingDB(databaseName);
const legacyCollectionName = 'historyEvents';
const frozenCollectionName = 'history-events-cutover-source-hist-04-v1';
const narrativeCollectionName = 'historical-narratives';
const cutoverVersion = 'hist-canonical-cutover-v1';
const canonicalizationVersion = 'hist-canonical-v2';
const legacyInfo = database.getCollectionInfos({ name: legacyCollectionName });
const frozenInfo = database.getCollectionInfos({ name: frozenCollectionName });

if (legacyInfo.length === 1 && legacyInfo[0].type === 'view') {
  if (legacyInfo[0].options.viewOn !== frozenCollectionName
      || frozenInfo.length !== 1
      || frozenInfo[0].type !== 'collection') {
    throw new Error('The historical source name is already used by an unexpected view.');
  }
} else if (legacyInfo.length === 0 && frozenInfo.length === 0) {
  print('No superseded historical source collection requires freezing.');
} else {
  if (legacyInfo.length === 1 && frozenInfo.length === 1) {
    throw new Error('Both historical source collections exist; refusing an ambiguous freeze.');
  }
  if (legacyInfo.length === 1) {
    if (legacyInfo[0].type !== 'collection') {
      throw new Error('The historical source namespace has an unsupported type.');
    }
    database.getCollection(legacyCollectionName).renameCollection(frozenCollectionName, false);
  } else if (frozenInfo[0].type !== 'collection') {
    throw new Error('The frozen historical source namespace is not a collection.');
  }
  database.createView(legacyCollectionName, frozenCollectionName, []);
  print('The superseded historical source is now exposed through a read-only cutover view.');
}

const narrativeInfo = database.getCollectionInfos({ name: narrativeCollectionName });
if (narrativeInfo.length === 0) {
  database.createCollection(narrativeCollectionName);
} else if (narrativeInfo[0].type !== 'collection') {
  throw new Error('The canonical historical narrative namespace is not a collection.');
}

const narrativeFreeze = database.runCommand({
  collMod: narrativeCollectionName,
  validator: {
    $or: [
      { cutoverVersion },
      {
        migrationVersion: canonicalizationVersion,
        canonicalizationState: { $in: ['Canonicalized', 'Blocked'] },
      },
    ],
  },
  validationLevel: 'strict',
  validationAction: 'error',
});
if (!narrativeFreeze.ok) {
  throw new Error('Could not freeze non-canonical historical narrative writes.');
}

print('Historical narrative writes are restricted to the canonical cutover authority.');
