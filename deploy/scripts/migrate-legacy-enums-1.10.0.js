const databaseName = process.env.MONGO_APP_DATABASE || 'AmusementPark';
const imagesCollectionName = process.env.MONGO_IMAGES_COLLECTION || 'images';
const videosCollectionName = process.env.MONGO_VIDEOS_COLLECTION || 'videos';
const parkItemsCollectionName = process.env.MONGO_PARK_ITEMS_COLLECTION || 'parkItems';
const attractionManufacturersCollectionName = process.env.MONGO_ATTRACTION_MANUFACTURERS_COLLECTION || 'attractionManufacturers';
const dryRun = String(process.env.DRY_RUN || '').toLowerCase() === 'true';

const database = db.getSiblingDB(databaseName);
const now = new Date();

function updateMany(collectionName, filter, update, label) {
  const collection = database.getCollection(collectionName);
  const matchedCount = collection.countDocuments(filter);

  if (dryRun) {
    print(`[dry-run] ${label}: ${matchedCount} matching document(s)`);
    return;
  }

  const result = collection.updateMany(filter, update);
  print(`${label}: matched=${result.matchedCount}, modified=${result.modifiedCount}`);
}

updateMany(
  imagesCollectionName,
  { ownerType: 'Attraction' },
  { $set: { ownerType: 'ParkItem', updatedAt: now } },
  'images ownerType Attraction -> ParkItem',
);

updateMany(
  imagesCollectionName,
  { category: 'Attraction' },
  { $set: { category: 'ParkItem', updatedAt: now } },
  'images category Attraction -> ParkItem',
);

updateMany(
  imagesCollectionName,
  { category: 'ParkLogo' },
  { $set: { category: 'Logo', updatedAt: now } },
  'images category ParkLogo -> Logo',
);

const manufacturerLogoImageIds = database
  .getCollection(attractionManufacturersCollectionName)
  .distinct('currentLogoImageId', {
    currentLogoImageId: { $type: 'string', $ne: '' },
  });

if (manufacturerLogoImageIds.length > 0) {
  updateMany(
    imagesCollectionName,
    {
      _id: { $in: manufacturerLogoImageIds },
      ownerType: 'AttractionManufacturer',
      category: { $ne: 'Logo' },
    },
    { $set: { category: 'Logo', updatedAt: now } },
    'images current manufacturer logos -> Logo',
  );
} else {
  print('images current manufacturer logos -> Logo: no referenced logo image');
}

updateMany(
  videosCollectionName,
  { ownerType: 'Attraction' },
  { $set: { ownerType: 'ParkItem', updatedAt: now } },
  'videos ownerType Attraction -> ParkItem',
);

const cinemaTextFilter = {
  $or: [
    { name: /cin(e|\u00e9)ma/i },
    { name: /\b(3d|3-d|3 d|4d|4-d|4 d)\b/i },
    { name: /\btheat(er|re)\b/i },
    { subtype: /cin(e|\u00e9)ma/i },
    { subtype: /\b(3d|3-d|3 d|4d|4-d|4 d)\b/i },
    { subtype: /\btheat(er|re)\b/i },
    { 'descriptions.value': /cin(e|\u00e9)ma/i },
    { 'descriptions.value': /\b(3d|3-d|3 d|4d|4-d|4 d)\b/i },
    { 'descriptions.value': /\btheat(er|re)\b/i },
  ],
};

updateMany(
  parkItemsCollectionName,
  {
    category: 'Attraction',
    type: { $in: ['Attraction', 'Other', 'InteractiveExperience', 'WalkThrough', 'Show'] },
    ...cinemaTextFilter,
  },
  { $set: { type: 'Cinema', updatedAt: now } },
  'parkItems obvious cinema candidates -> Cinema',
);
