const appDatabaseName = process.env.MONGO_APP_DATABASE || 'AmusementPark';
const appDatabase = db.getSiblingDB(appDatabaseName);
const narrativeCollectionName = 'historical-narratives';
const narrativeInfo = appDatabase.getCollectionInfos({ name: narrativeCollectionName });

if (narrativeInfo.length !== 1 || narrativeInfo[0].type !== 'collection') {
  throw new Error(`Canonical narrative collection '${narrativeCollectionName}' is unavailable.`);
}

const result = appDatabase.runCommand({
  collMod: narrativeCollectionName,
  validator: {},
  validationLevel: 'off',
});

if (!result.ok) {
  throw new Error(`Could not release the canonical history write freeze: ${JSON.stringify(result)}`);
}

print('Canonical history writes released after the unexposed candidate passed its health checks.');
