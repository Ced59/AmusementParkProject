export const PARK_OPERATORS_API_ENDPOINTS = {
  getParkOperators: 'park-operators',
  getParkOperatorById: (id: string) => `park-operators/${id}`,
  createParkOperator: 'park-operators',
  updateParkOperator: (id: string) => `park-operators/${id}`,
  updateParkOperatorsBulkReviewStatus: 'park-operators/bulk-review-status'
};
