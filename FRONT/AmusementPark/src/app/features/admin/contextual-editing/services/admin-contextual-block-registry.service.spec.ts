import { TestBed } from '@angular/core/testing';

import { LANGUAGES, LanguageOption } from '@shared/models/localization';
import { AdminContextualBlockInstance } from '../models/admin-contextual-block.model';
import { AdminContextualBlockRegistryService } from './admin-contextual-block-registry.service';

describe('AdminContextualBlockRegistryService', () => {
  let service: AdminContextualBlockRegistryService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AdminContextualBlockRegistryService]
    });

    service = TestBed.inject(AdminContextualBlockRegistryService);
  });

  it('registers the park pilot blocks for contextual editing', () => {
    expect(service.getDefinitions().map((definition) => definition.type)).toEqual([
      'park.hero',
      'park.description',
      'park.images',
      'park.practical',
      'park.location',
      'parkItem.description',
      'parkItem.images',
      'parkItem.location',
      'reference.manufacturer'
    ]);
  });

  it('creates a park block with the ids and full admin route required by the drawer', () => {
    const block: AdminContextualBlockInstance | null = service.createParkBlock(
      'park.hero',
      'park-1',
      'Phantasialand',
      'fr'
    );

    expect(block?.id).toBe('park.hero:park-1');
    expect(block?.entityType).toBe('Park');
    expect(block?.entityId).toBe('park-1');
    expect(block?.ids).toEqual({ parkId: 'park-1' });
    expect(block?.adminRoute).toEqual(['/', 'fr', 'admin', 'parks', 'edit', 'park-1']);
    expect(block?.capabilities).toContain('targetedChildAdd');
    expect(block?.locationFallbackCenter).toBeNull();
  });

  it('bounds localized description blocks to every supported language', () => {
    const block: AdminContextualBlockInstance | null = service.createParkBlock(
      'park.description',
      'park-1',
      'Phantasialand',
      'fr'
    );

    expect(block?.localizedLanguageCodes).toEqual(LANGUAGES.map((language: LanguageOption) => language.value));
    expect(block?.capabilities).toContain('boundedJsonExport');
    expect(block?.capabilities).toContain('boundedJsonPreview');
    expect(block?.capabilities).toContain('boundedJsonApply');
    expect(block?.capabilities).toContain('contextualFormEdit');
    expect(block?.jsonScope).toContain('park.descriptions[*].languageCode');
    expect(block?.jsonScope).toContain('park.descriptions[*].value');
  });

  it('creates a park location block focused on bounded coordinates and map fallback', () => {
    const block: AdminContextualBlockInstance | null = service.createParkBlock(
      'park.location',
      'park-1',
      'Phantasialand',
      'fr',
      [50.1, 3.2]
    );

    expect(block?.id).toBe('park.location:park-1');
    expect(block?.entityType).toBe('Park');
    expect(block?.entityId).toBe('park-1');
    expect(block?.ids).toEqual({ parkId: 'park-1' });
    expect(block?.localizedLanguageCodes).toEqual([]);
    expect(block?.locationFallbackCenter).toEqual([50.1, 3.2]);
    expect(block?.capabilities).toContain('boundedJsonExport');
    expect(block?.capabilities).toContain('contextualFormEdit');
    expect(block?.jsonScope).toEqual(['park.id', 'park.latitude', 'park.longitude']);
  });

  it('creates a park images block for targeted contextual photo additions', () => {
    const block: AdminContextualBlockInstance | null = service.createParkBlock(
      'park.images',
      'park-1',
      'Phantasialand',
      'fr'
    );

    expect(block?.id).toBe('park.images:park-1');
    expect(block?.entityType).toBe('Park');
    expect(block?.entityId).toBe('park-1');
    expect(block?.ids).toEqual({ parkId: 'park-1' });
    expect(block?.capabilities).toContain('contextualPhotoAdd');
    expect(block?.jsonScope).toContain('image.file');
    expect(block?.jsonScope).toContain('image.tagIds[*]');
  });

  it('creates a park item description block with parent ids and a full admin route', () => {
    const block: AdminContextualBlockInstance | null = service.createParkItemBlock(
      'parkItem.description',
      'item-1',
      'park-1',
      'Wakala',
      'fr'
    );

    expect(block?.id).toBe('parkItem.description:item-1');
    expect(block?.entityType).toBe('ParkItem');
    expect(block?.entityId).toBe('item-1');
    expect(block?.ids).toEqual({ parkId: 'park-1', parkItemId: 'item-1' });
    expect(block?.adminRoute).toEqual(['/', 'fr', 'admin', 'parks', 'edit', 'park-1', 'items', 'item-1']);
    expect(block?.localizedLanguageCodes).toEqual(LANGUAGES.map((language: LanguageOption) => language.value));
    expect(block?.capabilities).toContain('contextualFormEdit');
    expect(block?.jsonScope).toContain('parkItem.descriptions[*].languageCode');
    expect(block?.jsonScope).toContain('parkItem.descriptions[*].value');
  });

  it('creates a park item location block attached to its parent park', () => {
    const block: AdminContextualBlockInstance | null = service.createParkItemBlock(
      'parkItem.location',
      'item-1',
      'park-1',
      'Wakala',
      'fr',
      [50.2, 3.3]
    );

    expect(block?.id).toBe('parkItem.location:item-1');
    expect(block?.entityType).toBe('ParkItem');
    expect(block?.entityId).toBe('item-1');
    expect(block?.ids).toEqual({ parkId: 'park-1', parkItemId: 'item-1' });
    expect(block?.localizedLanguageCodes).toEqual([]);
    expect(block?.locationFallbackCenter).toEqual([50.2, 3.3]);
    expect(block?.adminRoute).toEqual(['/', 'fr', 'admin', 'parks', 'edit', 'park-1', 'items', 'item-1']);
    expect(block?.jsonScope).toEqual(['parkItem.id', 'parkItem.parkId', 'parkItem.latitude', 'parkItem.longitude']);
  });

  it('creates a park item images block attached to its parent park', () => {
    const block: AdminContextualBlockInstance | null = service.createParkItemBlock(
      'parkItem.images',
      'item-1',
      'park-1',
      'Wakala',
      'fr'
    );

    expect(block?.id).toBe('parkItem.images:item-1');
    expect(block?.entityType).toBe('ParkItem');
    expect(block?.entityId).toBe('item-1');
    expect(block?.ids).toEqual({ parkId: 'park-1', parkItemId: 'item-1' });
    expect(block?.capabilities).toContain('contextualPhotoAdd');
    expect(block?.jsonScope).toContain('parkItem.parkId');
    expect(block?.jsonScope).toContain('image.sourceUrl');
  });

  it('creates a manufacturer reference block with a graph upsert draft and import route', () => {
    const block: AdminContextualBlockInstance | null = service.createManufacturerBlock(
      'manufacturer-1',
      'Mack Rides',
      'fr',
      '{ "documentType": "AmusementParkParkGraphUpsert" }',
      'mack-rides-park-graph-upsert.json'
    );

    expect(block?.id).toBe('reference.manufacturer:manufacturer-1');
    expect(block?.entityType).toBe('AttractionManufacturer');
    expect(block?.entityId).toBe('manufacturer-1');
    expect(block?.ids).toEqual({ manufacturerId: 'manufacturer-1' });
    expect(block?.adminRoute).toEqual(['/', 'fr', 'admin', 'manufacturers', 'edit', 'manufacturer-1']);
    expect(block?.parkGraphUpsertImportRoute).toEqual(['/', 'fr', 'admin', 'park-graph-upserts']);
    expect(block?.parkGraphUpsertDraftJson).toContain('AmusementParkParkGraphUpsert');
    expect(block?.parkGraphUpsertFileName).toBe('mack-rides-park-graph-upsert.json');
    expect(block?.capabilities).toContain('parkGraphUpsertDraft');
    expect(block?.localizedLanguageCodes).toEqual(LANGUAGES.map((language: LanguageOption) => language.value));
    expect(block?.jsonScope).toContain('references.manufacturers[*].biography[*].value');
  });

  it('keeps unsupported hero exports as planned capabilities only', () => {
    const block: AdminContextualBlockInstance | null = service.createParkBlock(
      'park.hero',
      'park-1',
      'Phantasialand',
      'fr'
    );

    expect(block?.capabilities).toContain('boundedJsonExportPlanned');
    expect(block?.capabilities).toContain('targetedChildAdd');
    expect(block?.capabilities).not.toContain('boundedJsonExport');
    expect(block?.capabilities).not.toContain('boundedJsonPreview');
  });

  it('does not create a block without a park id', () => {
    expect(service.createParkBlock('park.practical', null, 'No id', 'fr')).toBeNull();
    expect(service.createParkItemBlock('parkItem.description', 'item-1', null, 'No park', 'fr')).toBeNull();
    expect(service.createParkBlock('parkItem.description', 'item-1', 'Wrong factory', 'fr')).toBeNull();
    expect(service.createParkItemBlock('park.location', 'item-1', 'park-1', 'Wrong factory', 'fr')).toBeNull();
    expect(service.createParkBlock('reference.manufacturer', 'manufacturer-1', 'Wrong factory', 'fr')).toBeNull();
    expect(service.createManufacturerBlock(null, 'No id', 'fr', '{}', 'manufacturer.json')).toBeNull();
  });
});
