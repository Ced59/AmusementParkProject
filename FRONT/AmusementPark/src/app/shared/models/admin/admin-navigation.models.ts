export interface AdminNavigationItem {
  readonly descriptionKey: string;
  readonly exact: boolean;
  readonly iconClass: string;
  readonly id: string;
  readonly segments: readonly string[];
  readonly titleKey: string;
}

export const ADMIN_NAVIGATION_ITEMS: readonly AdminNavigationItem[] = [
  {
    id: 'parks',
    segments: ['parks'],
    iconClass: 'pi pi-map',
    titleKey: 'admin.parks.title',
    descriptionKey: 'admin.dashboard.shortcuts.parks',
    exact: false
  },
  {
    id: 'park-items',
    segments: ['items'],
    iconClass: 'pi pi-ticket',
    titleKey: 'admin.parkItems.title',
    descriptionKey: 'admin.dashboard.shortcuts.parkItems',
    exact: false
  },
  {
    id: 'standalone-attractions',
    segments: ['standalone-attractions'],
    iconClass: 'pi pi-compass',
    titleKey: 'admin.standaloneAttractions.title',
    descriptionKey: 'admin.dashboard.shortcuts.standaloneAttractions',
    exact: false
  },
  {
    id: 'field-mode',
    segments: ['field-mode'],
    iconClass: 'pi pi-compass',
    titleKey: 'admin.fieldMode.navTitle',
    descriptionKey: 'admin.dashboard.shortcuts.fieldMode',
    exact: false
  },
  {
    id: 'operators',
    segments: ['operators'],
    iconClass: 'pi pi-building',
    titleKey: 'admin.operators.title',
    descriptionKey: 'admin.dashboard.shortcuts.operators',
    exact: false
  },
  {
    id: 'founders',
    segments: ['founders'],
    iconClass: 'pi pi-sparkles',
    titleKey: 'admin.parkFounders.title',
    descriptionKey: 'admin.dashboard.shortcuts.founders',
    exact: false
  },
  {
    id: 'manufacturers',
    segments: ['manufacturers'],
    iconClass: 'pi pi-wrench',
    titleKey: 'admin.manufacturers.title',
    descriptionKey: 'admin.dashboard.shortcuts.manufacturers',
    exact: false
  },
  {
    id: 'technical-pages',
    segments: ['technical-pages'],
    iconClass: 'pi pi-cog',
    titleKey: 'admin.technicalPages.title',
    descriptionKey: 'admin.dashboard.shortcuts.technicalPages',
    exact: false
  },
  {
    id: 'images',
    segments: ['images'],
    iconClass: 'pi pi-image',
    titleKey: 'admin.images.title',
    descriptionKey: 'admin.dashboard.shortcuts.images',
    exact: true
  },
  {
    id: 'image-batch',
    segments: ['images', 'batch'],
    iconClass: 'pi pi-images',
    titleKey: 'admin.images.batch.navTitle',
    descriptionKey: 'admin.dashboard.shortcuts.imageBatch',
    exact: false
  },
  {
    id: 'videos',
    segments: ['videos'],
    iconClass: 'pi pi-video',
    titleKey: 'admin.videos.title',
    descriptionKey: 'admin.dashboard.shortcuts.videos',
    exact: false
  },
  {
    id: 'users',
    segments: ['users'],
    iconClass: 'pi pi-users',
    titleKey: 'admin.users.title',
    descriptionKey: 'admin.dashboard.shortcuts.users',
    exact: false
  },
  {
    id: 'data',
    segments: ['data'],
    iconClass: 'pi pi-cog',
    titleKey: 'admin.dataSources.title',
    descriptionKey: 'admin.dashboard.shortcuts.data',
    exact: false
  },
  {
    id: 'park-graph-upserts',
    segments: ['park-graph-upserts'],
    iconClass: 'pi pi-sitemap',
    titleKey: 'admin.parkGraphUpserts.navTitle',
    descriptionKey: 'admin.dashboard.shortcuts.parkGraphUpserts',
    exact: false
  },
  {
    id: 'bulk-park-graph-upserts',
    segments: ['bulk-park-graph-upserts'],
    iconClass: 'pi pi-download',
    titleKey: 'admin.bulkParkGraphUpserts.navTitle',
    descriptionKey: 'admin.dashboard.shortcuts.bulkParkGraphUpserts',
    exact: false
  },
  {
    id: 'history',
    segments: ['history'],
    iconClass: 'pi pi-history',
    titleKey: 'admin.history.navTitle',
    descriptionKey: 'admin.dashboard.shortcuts.history',
    exact: false
  },
  {
    id: 'audit-logs',
    segments: ['audit-logs'],
    iconClass: 'pi pi-shield',
    titleKey: 'admin.auditLogs.title',
    descriptionKey: 'admin.dashboard.shortcuts.auditLogs',
    exact: false
  },
  {
    id: 'seo-sitemaps',
    segments: ['seo-sitemaps'],
    iconClass: 'pi pi-search',
    titleKey: 'admin.seoSitemaps.navTitle',
    descriptionKey: 'admin.dashboard.shortcuts.seoSitemaps',
    exact: false
  },
  {
    id: 'park-weather',
    segments: ['park-weather'],
    iconClass: 'pi pi-cloud',
    titleKey: 'admin.parkWeather.navTitle',
    descriptionKey: 'admin.dashboard.shortcuts.parkWeather',
    exact: false
  },
  {
    id: 'contact-grievances',
    segments: ['contact-grievances'],
    iconClass: 'pi pi-inbox',
    titleKey: 'admin.contactGrievances.navTitle',
    descriptionKey: 'admin.dashboard.shortcuts.contactGrievances',
    exact: false
  },
  {
    id: 'social-share',
    segments: ['social-share'],
    iconClass: 'pi pi-share-alt',
    titleKey: 'admin.socialShare.navTitle',
    descriptionKey: 'admin.dashboard.shortcuts.socialShare',
    exact: false
  },
  {
    id: 'social-publications',
    segments: ['social-publications'],
    iconClass: 'pi pi-send',
    titleKey: 'admin.socialPublishing.navTitle',
    descriptionKey: 'admin.dashboard.shortcuts.socialPublishing',
    exact: false
  },
  {
    id: 'technical-stats',
    segments: ['technical-stats'],
    iconClass: 'pi pi-server',
    titleKey: 'admin.technicalStats.navTitle',
    descriptionKey: 'admin.dashboard.shortcuts.technicalStats',
    exact: false
  }
];
