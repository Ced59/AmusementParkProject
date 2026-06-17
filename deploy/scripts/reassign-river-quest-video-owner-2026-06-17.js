const databaseName = process.env.MONGO_APP_DATABASE || 'AmusementPark';
const videosCollectionName = process.env.MONGO_VIDEOS_COLLECTION || 'videos';
const parkItemsCollectionName = process.env.MONGO_PARK_ITEMS_COLLECTION || 'parkItems';
const dryRun = String(process.env.DRY_RUN || 'true').toLowerCase() !== 'false';

const videoId = process.env.VIDEO_ID || '8d19c41814834ca3866f802421b0aac0';
const expectedCurrentOwnerType = process.env.EXPECTED_CURRENT_OWNER_TYPE || 'Park';
const expectedCurrentOwnerId = process.env.EXPECTED_CURRENT_OWNER_ID || '50555bb2-abd4-4c3a-b0d2-b2fa5f547c6e';
const targetOwnerType = process.env.TARGET_OWNER_TYPE || 'ParkItem';
const targetOwnerId = process.env.TARGET_OWNER_ID || 'f75bd8a9-dfb6-4802-89f8-b6ad70bfecd8';
const allowUnexpectedCurrentOwner = String(process.env.ALLOW_UNEXPECTED_CURRENT_OWNER || '').toLowerCase() === 'true';

const database = db.getSiblingDB(databaseName);
const videosCollection = database.getCollection(videosCollectionName);
const parkItemsCollection = database.getCollection(parkItemsCollectionName);

function fail(message) {
  print(`[abort] ${message}`);
  quit(1);
}

function formatOwner(ownerType, ownerId) {
  return `${ownerType || '(missing)'}/${ownerId || '(missing)'}`;
}

function localizedName(document) {
  if (!document || !Array.isArray(document.names)) {
    return null;
  }

  const frenchName = document.names.find((name) => name && name.languageCode === 'fr' && name.value);
  const fallbackName = document.names.find((name) => name && name.value);
  return (frenchName || fallbackName || null)?.value || null;
}

const video = videosCollection.findOne({ _id: videoId });

if (!video) {
  fail(`Video ${videoId} was not found in ${databaseName}.${videosCollectionName}.`);
}

const targetParkItem = parkItemsCollection.findOne({ _id: targetOwnerId });

if (!targetParkItem) {
  fail(`Target park item ${targetOwnerId} was not found in ${databaseName}.${parkItemsCollectionName}.`);
}

const currentOwner = formatOwner(video.ownerType, video.ownerId);
const expectedOwner = formatOwner(expectedCurrentOwnerType, expectedCurrentOwnerId);
const targetOwner = formatOwner(targetOwnerType, targetOwnerId);

print(`Database: ${databaseName}`);
print(`Video: ${videoId}`);
print(`Current owner: ${currentOwner}`);
print(`Expected current owner: ${expectedOwner}`);
print(`Target owner: ${targetOwner}`);
print(`Target park item name: ${localizedName(targetParkItem) || '(unknown)'}`);
print(`Dry run: ${dryRun}`);

if (video.ownerType === targetOwnerType && video.ownerId === targetOwnerId) {
  print('[noop] The video is already attached to the target park item.');
  quit(0);
}

if (!allowUnexpectedCurrentOwner
  && (video.ownerType !== expectedCurrentOwnerType || video.ownerId !== expectedCurrentOwnerId)) {
  fail('The video is not attached to the expected current owner. Set ALLOW_UNEXPECTED_CURRENT_OWNER=true to override.');
}

if (dryRun) {
  print(`[dry-run] Would update ${videoId}: ${currentOwner} -> ${targetOwner}.`);
  quit(0);
}

const now = new Date();
const result = videosCollection.updateOne(
  {
    _id: videoId,
    ownerType: video.ownerType,
    ownerId: video.ownerId,
  },
  {
    $set: {
      ownerType: targetOwnerType,
      ownerId: targetOwnerId,
      updatedAt: now,
    },
  },
);

print(`Update result: matched=${result.matchedCount}, modified=${result.modifiedCount}`);

if (result.matchedCount !== 1 || result.modifiedCount !== 1) {
  fail('The update did not modify exactly one document.');
}

const updatedVideo = videosCollection.findOne({ _id: videoId });
print(`Updated owner: ${formatOwner(updatedVideo.ownerType, updatedVideo.ownerId)}`);
