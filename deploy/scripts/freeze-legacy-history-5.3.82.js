const databaseName = process.env.MONGO_APP_DATABASE || 'AmusementPark';
const database = db.getSiblingDB(databaseName);
const legacyCollectionName = 'historyEvents';
const frozenCollectionName = 'history-events-cutover-source-hist-04-v1';
const legacyInfo = database.getCollectionInfos({ name: legacyCollectionName });
const frozenInfo = database.getCollectionInfos({ name: frozenCollectionName });

if (legacyInfo.length === 1 && legacyInfo[0].type === 'view') {
  if (legacyInfo[0].options.viewOn !== frozenCollectionName) {
    throw new Error('The legacy historical name is already used by an unexpected view.');
  }
  print('Legacy historical collection is already exposed as the expected read-only cutover view.');
  quit(0);
}

if (legacyInfo.length === 0 && frozenInfo.length === 0) {
  print('Legacy historical collection is absent; no physical freeze is required.');
  quit(0);
}

if (legacyInfo.length === 1 && frozenInfo.length === 1) {
  throw new Error('Both legacy and cutover source collections exist; refusing an ambiguous freeze.');
}

if (legacyInfo.length === 1) {
  if (legacyInfo[0].type !== 'collection') {
    throw new Error('The legacy historical namespace is neither a collection nor the expected view.');
  }
  database.getCollection(legacyCollectionName).renameCollection(frozenCollectionName, false);
}

database.createView(legacyCollectionName, frozenCollectionName, []);
print('Legacy historical collection is exposed through a read-only view for HIST-04 cutover.');
