export const anonymousDraftIds = ['apf-ci-local-draft', 'apf-ci-import-locked-draft'];
export const anonymousDraftParkId = '00000000-0000-4000-8000-000000000050';

export function createAnonymousDraftFixtures(date) {
  return anonymousDraftIds.map((id, index) => ({ schemaVersion: 1, id, visitOperationId: `ci-visit-${index}`, rideOperationId: `ci-ride-${index}`,
    parkName: 'Parc de recette locale uniquement — nom long Äéł',
    visit: { parkId: anonymousDraftParkId, date: { year: 2026, month: null, day: null, precision: 'Year', isApproximate: true },
      timeZoneId: null, serviceDayConvention: 'VisitStartLocalDate', title: 'Visite locale de recette uniquement', privateNote: null },
    rides: [{ id: `ci-ride-row-${index}`, parkItemId: '00000000-0000-4000-8000-000000001100',
      attractionName: 'Attraction de recette locale uniquement — nom long Äéł', moment: { localTime: null, isApproximate: true },
      status: 'Completed', privateNote: null, confirmHistoricalConflict: false, count: 2 }],
    pendingImport: index ? { choice: 'Separate', targetVisitId: null, metadataChoice: 'KeepServer', startedAtUtc: date } : null,
    createdAtUtc: date, updatedAtUtc: date }));
}

// Executed only in the CI-only denied-network origin. Never a user's database.
export async function seedAnonymousDraftBrowser(origin, drafts) {
  if (location.origin !== origin || origin !== 'https://apf-account-layout.invalid') throw new Error('Anonymous seed origin is not isolated.');
  if (!Array.isArray(drafts) || drafts.length > 2) throw new Error('Anonymous seed exceeds its explicit fixture bound.');
  return new Promise((resolve, reject) => {
    const request = indexedDB.open('amusement-park-passport', 2);
    request.onupgradeneeded = () => {
      const database = request.result;
      if (!database.objectStoreNames.contains('anonymous-visit-drafts')) database.createObjectStore('anonymous-visit-drafts', { keyPath: 'id' }).createIndex('updatedAtUtc', 'updatedAtUtc');
      if (!database.objectStoreNames.contains('product-milestones')) database.createObjectStore('product-milestones');
    };
    request.onerror = () => reject(request.error);
    request.onsuccess = () => {
      const database = request.result;
      const transaction = database.transaction(['anonymous-visit-drafts', 'product-milestones'], 'readwrite');
      const store = transaction.objectStore('anonymous-visit-drafts');
      store.clear();
      transaction.objectStore('product-milestones').clear();
      for (const draft of drafts) store.put(draft);
      transaction.oncomplete = () => { database.close(); resolve(drafts.length); };
      transaction.onerror = transaction.onabort = () => { database.close(); reject(transaction.error ?? new Error('Isolated draft seed failed.')); };
    };
  });
}

export function anonymousDraftFixtureResponse(request, url) {
  return request.method === 'GET' && url.pathname === `/api/park-items/park/${anonymousDraftParkId}`
    && url.search === '?page=1&size=20&closedFilter=all&category=Attraction'
    ? { status: 200, body: { items: [{ id: '00000000-0000-4000-8000-000000001100', parkId: anonymousDraftParkId,
      name: 'Attraction de recette locale uniquement — nom long Äéł', category: 'Attraction', type: 'Attraction',
      latitude: null, longitude: null, isVisible: true, mainImageId: null, zoneId: null, attractionDetails: null }],
      currentPage: 1, pageSize: 20, totalItems: 1, totalPages: 1 } } : null;
}
