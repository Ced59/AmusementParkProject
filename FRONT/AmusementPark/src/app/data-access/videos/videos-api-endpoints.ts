export const VIDEOS_API_ENDPOINTS = {
  getVideos: 'videos',
  getParkItemVideosByPark: (parkId: string) => `videos/parks/${encodeURIComponent(parkId)}/park-items`,
  getVideo: (id: string) => `videos/${id}`,
  resolveMetadata: 'videos/resolve-metadata',
  createVideo: 'videos',
  updateVideo: (id: string) => `videos/${id}`,
  deleteVideo: (id: string) => `videos/${id}`,
  getVideoTags: 'videos/tags',
  createVideoTag: 'videos/tags',
  updateVideoTag: (id: string) => `videos/tags/${id}`
};
