export const MANUFACTURERS_API_ENDPOINTS = {
  getAttractionManufacturers: 'attraction-manufacturers',
  getAttractionManufacturerById: (id: string) => `attraction-manufacturers/${id}`,
  createAttractionManufacturer: 'attraction-manufacturers',
  updateAttractionManufacturer: (id: string) => `attraction-manufacturers/${id}`,
  updateAttractionManufacturersBulkReviewStatus: 'attraction-manufacturers/bulk-review-status'
};
