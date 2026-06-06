import { PARK_ITEMS_API_ENDPOINTS } from './park-items-api-endpoints';

describe('PARK_ITEMS_API_ENDPOINTS', () => {
  it('builds paginated urls with park, search, filters and sort', () => {
    const endpoint: string = PARK_ITEMS_API_ENDPOINTS.getParkItemsPaginated(3, 50, 'park 1', 'big ride', {
      isVisible: false,
      adminReviewStatus: 'ToReview',
      category: 'Attraction',
      type: 'RollerCoaster',
      manufacturerId: 'manufacturer 1'
    }, {
      sortBy: 'name',
      sortDirection: 'desc'
    });

    expect(endpoint).toBe('park-items?page=3&size=50&parkId=park%201&search=big%20ride&isVisible=false&adminReviewStatus=ToReview&category=Attraction&type=RollerCoaster&manufacturerId=manufacturer%201&sortBy=name&sortDirection=desc');
  });

  it('omits empty filters and default sort', () => {
    const endpoint: string = PARK_ITEMS_API_ENDPOINTS.getParkItemsPaginated(1, 10, null, null, {
      isVisible: null,
      adminReviewStatus: null,
      category: null,
      type: null,
      manufacturerId: null
    }, {
      sortBy: 'default',
      sortDirection: 'asc'
    });

    expect(endpoint).toBe('park-items?page=1&size=10');
  });

  it('preserves explicit false visibility filters', () => {
    expect(PARK_ITEMS_API_ENDPOINTS.getParkItemsPaginated(1, 10, null, null, { isVisible: false }))
      .toBe('park-items?page=1&size=10&isVisible=false');
  });

  it('builds CRUD endpoints', () => {
    expect(PARK_ITEMS_API_ENDPOINTS.getParkItemsByParkId('park-1')).toBe('park-items/park/park-1');
    expect(PARK_ITEMS_API_ENDPOINTS.getParkItemById('item-1')).toBe('park-items/item-1');
    expect(PARK_ITEMS_API_ENDPOINTS.updateParkItem('item-1')).toBe('park-items/item-1');
    expect(PARK_ITEMS_API_ENDPOINTS.deleteParkItem('item-1')).toBe('park-items/item-1');
  });
});
