import { ParkMapItems } from '@app/models/parks/park-map-items';
import { environment } from '../../../../../environments/environment';
import { mapParkOfficialMapsToViewModels } from './park-official-map-view.mapper';

describe('mapParkOfficialMapsToViewModels', () => {
  it('orders editions by descending year and resolves a stored file path', () => {
    const result = mapParkOfficialMapsToViewModels([
      {
        id: 'map-2024',
        year: 2024,
        format: 'Image',
        documentUrl: 'https://park.example/map-2024.png',
        alternativeTexts: [{ languageCode: 'en', value: 'Park map in 2024' }]
      },
      {
        id: 'map-2026',
        year: 2026,
        format: 'Pdf',
        documentUrl: 'parks/park-1/official-maps/map-2026/file',
        isVisible: false,
        titles: [
          { languageCode: 'fr', value: 'Plan 2026' },
          { languageCode: 'en', value: '2026 map' }
        ]
      }
    ], 'fr');

    expect(result.map(map => map.year)).toEqual([2026, 2024]);
    expect(result[0].title).toBe('Plan 2026');
    expect(result[0].documentUrl).toBe(`${environment.apiBaseUrl}parks/park-1/official-maps/map-2026/file`);
    expect(result[0].displayDocumentUrl).toBeNull();
    expect(result[0].isStoredDocument).toBe(true);
  });

  it('ignores entries without a usable year or document URL', () => {
    const response: ParkMapItems = {
      park: { id: 'park-1', name: 'Park', latitude: 0, longitude: 0, isVisible: true },
      items: [],
      zones: [],
      officialMaps: [
        { id: 'invalid-year', year: 0, format: 'Pdf', documentUrl: 'https://park.example/map.pdf' },
        { id: 'invalid-url', year: 2026, format: 'Pdf', documentUrl: '' }
      ]
    };

    expect(mapParkOfficialMapsToViewModels(response.officialMaps, 'en')).toEqual([]);
  });
});
