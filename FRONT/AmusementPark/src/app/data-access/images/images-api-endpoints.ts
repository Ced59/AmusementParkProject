export const IMAGES_API_ENDPOINTS = {
  uploadImage: 'images',
  importRemoteImage: 'images/remote',
  linkImage: 'images/links',
  getImages: (ownerType: string, ownerId: string, category: string) => `images/${ownerType}/${ownerId}/${category}`,
  getCurrentImage: (ownerType: string, ownerId: string, category: string) => `images/${ownerType}/${ownerId}/${category}/current`,
  setCurrentImage: (imageId: string) => `images/${imageId}/current`,
  applyWatermark: (imageId: string) => `images/${imageId}/watermark`,
  deleteImage: (imageId: string) => `images/${imageId}`,
  getAdminImages: 'images',
  updateAdminImage: (id: string) => `images/${id}/metadata`,
  getAdminImageTags: 'images/tags',
  createAdminImageTag: 'images/tags',
  updateAdminImageTag: (id: string) => `images/tags/${id}`
};
