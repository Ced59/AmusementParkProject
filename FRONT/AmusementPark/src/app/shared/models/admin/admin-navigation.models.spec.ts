import { ADMIN_NAVIGATION_ITEMS, AdminNavigationItem } from './admin-navigation.models';

describe('ADMIN_NAVIGATION_ITEMS', () => {
  it('lists every admin destination shared by the sidebar and dashboard', () => {
    const routes: string[] = ADMIN_NAVIGATION_ITEMS.map((item: AdminNavigationItem) => item.segments.join('/'));

    expect(routes).toEqual([
      'parks',
      'items',
      'standalone-attractions',
      'field-mode',
      'operators',
      'founders',
      'manufacturers',
      'technical-pages',
      'images',
      'images/batch',
      'videos',
      'users',
      'data',
      'park-graph-upserts',
      'bulk-park-graph-upserts',
      'history',
      'audit-logs',
      'seo-sitemaps',
      'park-weather',
      'contact-grievances',
      'social-share',
      'passport-beta',
      'social-publications',
      'rating-rankings',
      'technical-stats'
    ]);
    expect(new Set(routes).size).toBe(routes.length);
  });
});
