import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import {
  buildManufacturerParkGraphUpsertFileName,
  buildManufacturerParkGraphUpsertJson
} from './park-reference-manufacturer-upsert.mapper';

describe('park-reference-manufacturer-upsert.mapper', () => {
  it('builds a bounded park graph upsert draft for the current manufacturer', () => {
    const manufacturer: AttractionManufacturer = {
      id: 'manufacturer-1',
      name: 'Mack Rides',
      legalName: 'Mack Rides GmbH & Co KG',
      foundedYear: 1780,
      closedYear: null,
      contactDetails: {
        websiteUrl: ' https://mack-rides.com ',
        email: 'info@example.test',
        phoneNumber: '+49 000',
        street: 'Mauermattenstrasse 4',
        city: 'Waldkirch',
        postalCode: '79183',
        countryCode: 'de',
        latitude: 48.091,
        longitude: 7.955
      },
      biography: [
        { languageCode: 'fr', value: '<p>Constructeur allemand.</p>' },
        { languageCode: 'en', value: '<p>German manufacturer.</p>' }
      ],
      adminReviewStatus: 'Validated'
    };

    const document = JSON.parse(buildManufacturerParkGraphUpsertJson(manufacturer));
    const draftManufacturer = document.references.manufacturers[0];

    expect(document.documentType).toBe('AmusementParkParkGraphUpsert');
    expect(document.park).toEqual({});
    expect(document.zones).toEqual([]);
    expect(document.items).toEqual([]);
    expect(document.images).toEqual([
      {
        sourceUrl: '',
        ownerKey: 'manufacturer:manufacturer-1',
        category: 'Manufacturer',
        description: '',
        isPublished: true,
        setAsCurrent: false,
        withWatermark: false
      }
    ]);
    expect(draftManufacturer).toEqual(jasmine.objectContaining({
      key: 'manufacturer:manufacturer-1',
      id: 'manufacturer-1',
      name: 'Mack Rides',
      legalName: 'Mack Rides GmbH & Co KG',
      foundedYear: 1780,
      closedYear: null,
      adminReviewStatus: 'Validated'
    }));
    expect(draftManufacturer.contactDetails).toEqual(jasmine.objectContaining({
      websiteUrl: 'https://mack-rides.com',
      countryCode: 'DE',
      latitude: 48.091,
      longitude: 7.955
    }));
    expect(draftManufacturer.biography).toEqual([
      { languageCode: 'fr', value: '<p>Constructeur allemand.</p>' },
      { languageCode: 'en', value: '<p>German manufacturer.</p>' }
    ]);
  });

  it('builds a stable import file name from the manufacturer identity', () => {
    expect(buildManufacturerParkGraphUpsertFileName({ id: 'A.R.M.', name: 'A.R.M.' })).toBe('a-r-m-park-graph-upsert.json');
  });
});
