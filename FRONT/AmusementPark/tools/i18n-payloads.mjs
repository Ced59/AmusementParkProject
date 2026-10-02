// Shared public components still use these existing admin-namespaced labels.
const publicAdminPaths = [
  'parks.types', 'parks.audienceClassifications', 'parks.statuses',
  'parks.items.statuses', 'parks.items.waterExposureLevels',
  'parks.items.accessConditionTypes', 'parks.items.accessConditionUnits',
  'parks.photos.categories', 'parks.items.photos.categories',
  'parks.zones.namePlaceholder', 'parks.descriptions.placeholder', 'images.category'
];

export function splitTranslationPayloads(translations) {
  const { admin, ...publicTranslations } = translations;
  const publicAdmin = {};
  for (const path of publicAdminPaths) {
    const segments = path.split('.');
    let source = admin;
    let target = publicAdmin;
    for (const segment of segments.slice(0, -1)) {
      source = source?.[segment];
      target = target[segment] ??= {};
    }
    const leaf = segments.at(-1);
    if (source?.[leaf] === undefined) {
      throw new Error(`Missing shared translation: admin.${path}`);
    }
    target[leaf] = source[leaf];
  }
  return {
    public: { ...publicTranslations, admin: publicAdmin },
    admin: { admin }
  };
}
