const databaseName = process.env.MONGO_APP_DATABASE || 'AmusementPark';
const database = db.getSiblingDB(databaseName);
const legacyCollectionName = 'userRankingShares';
const collections = database.getCollectionInfos({ name: legacyCollectionName });

if (collections.length === 0) {
  print('Legacy personal ranking share collection is absent; no physical freeze is required.');
  quit(0);
}

const result = database.runCommand({
  collMod: legacyCollectionName,
  validator: { $expr: { $eq: [1, 0] } },
  validationLevel: 'strict',
  validationAction: 'error',
});

if (!result.ok) {
  throw new Error('Could not freeze the legacy personal ranking share collection.');
}

print('Legacy personal ranking share collection is frozen for SHARE-04A cutover.');
