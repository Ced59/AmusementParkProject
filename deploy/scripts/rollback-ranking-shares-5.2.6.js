const databaseName = process.env.MONGO_APP_DATABASE || 'AmusementPark';
const database = db.getSiblingDB(databaseName);
const legacyCollectionName = 'userRankingShares';
const publicationCollectionName = 'share-publications';
const migrationCollectionName = 'share-publication-migrations';
const migrationId = 'share-04a-personal-ranking-v1';

if (database.getCollectionInfos({ name: legacyCollectionName }).length === 0) {
  database.createCollection(legacyCollectionName);
}

const legacy = database.getCollection(legacyCollectionName);
const publications = database.getCollection(publicationCollectionName);
const latestByOwner = new Map();
publications.find({ type: 'PersonalRanking' })
  .sort({ ownerUserId: 1, updatedAt: -1, _id: -1 })
  .forEach((publication) => {
    if (!latestByOwner.has(publication.ownerUserId)) {
      latestByOwner.set(publication.ownerUserId, publication);
    }
  });

const operations = [];
latestByOwner.forEach((publication, ownerUserId) => {
  const isPublic = publication.status === 'Published'
    && (publication.visibility === 'Unlisted' || publication.visibility === 'Public')
    && typeof publication.shareToken === 'string';
  const setFields = {
    userId: ownerUserId,
    isPublic,
    createdAt: publication.createdAt,
    updatedAt: publication.updatedAt,
  };
  const update = {
    $set: setFields,
    $setOnInsert: { _id: publication._id },
  };
  if (isPublic) {
    setFields.shareId = publication.shareToken;
    setFields.publishedAtUtc = publication.publishedAtUtc;
  } else {
    update.$unset = { shareId: '', publishedAtUtc: '' };
  }

  operations.push({
    updateOne: {
      filter: { userId: ownerUserId },
      update,
      upsert: true,
    },
  });
});

if (operations.length > 0) {
  legacy.bulkWrite(operations, { ordered: true, bypassDocumentValidation: true });
}

publications.deleteMany({ type: 'PersonalRanking' });
database.getCollection(migrationCollectionName).deleteOne({ _id: migrationId });

const unfreezeResult = database.runCommand({
  collMod: legacyCollectionName,
  validator: {},
  validationLevel: 'off',
});
if (!unfreezeResult.ok) {
  throw new Error('Could not unfreeze the legacy personal ranking share collection.');
}

print(`Rolled back ${latestByOwner.size} central personal ranking share states.`);
