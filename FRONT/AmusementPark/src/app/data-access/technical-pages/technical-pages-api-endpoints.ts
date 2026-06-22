export const TECHNICAL_PAGES_API_ENDPOINTS = {
  getPublicPages: 'technical-pages',
  getAdminPages: 'technical-pages/admin',
  getById: (id: string): string => `technical-pages/by-id/${encodeURIComponent(id)}`,
  getBySlug: (slug: string): string => `technical-pages/slug/${encodeURIComponent(slug)}`,
  create: 'technical-pages',
  update: (id: string): string => `technical-pages/${encodeURIComponent(id)}`,
  upsertJson: 'technical-pages/upsert-json'
} as const;
