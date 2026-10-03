const appDatabaseName = process.env.MONGO_APP_DATABASE || 'AmusementPark';
const appDatabase = db.getSiblingDB(appDatabaseName);
const editorialCollectionNames = [
  'historical-narratives',
  'historical-facts',
  'historical-sources',
  'historical-relations',
];

for (const collectionName of editorialCollectionNames) {
  const collectionInfo = appDatabase.getCollectionInfos({ name: collectionName });
  if (collectionInfo.length !== 1 || collectionInfo[0].type !== 'collection') {
    throw new Error(`Canonical history collection '${collectionName}' is unavailable.`);
  }

  const result = appDatabase.runCommand({
    collMod: collectionName,
    validator: { $expr: { $eq: [1, 0] } },
    validationLevel: 'strict',
    validationAction: 'error',
  });
  if (!result.ok) {
    throw new Error(`Could not freeze canonical history writes: ${JSON.stringify(result)}`);
  }
}

print('Canonical history writes remain frozen through authority promotion and legacy cleanup.');
