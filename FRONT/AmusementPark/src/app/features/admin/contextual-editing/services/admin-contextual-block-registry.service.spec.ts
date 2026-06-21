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
      'park.practical',
      'parkItem.description'
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
  });
});
