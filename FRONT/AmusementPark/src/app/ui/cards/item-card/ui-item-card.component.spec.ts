import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';

import {
  COMMON_TEST_IMPORTS,
  provideCommonTestDependencies,
} from '@app/testing/common-test-providers';
import { ParkItemCardViewModel } from '@features/public/park-items/models/park-item-card.model';
import { UiItemCardComponent } from './ui-item-card.component';

describe('UiItemCardComponent', () => {
  let fixture: ComponentFixture<UiItemCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, UiItemCardComponent],
      providers: [...provideCommonTestDependencies()],
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('fr', {
      parkItems: {
        statuses: {
          removed: 'Supprimé / démonté',
        },
        actions: {
          viewDetails: 'Voir la fiche',
        },
      },
      parkExplorer: {
        categories: { attraction: 'Attractions' },
        types: { rollerCoaster: 'Montagnes russes' },
      },
    });
    translateService.use('fr');

    fixture = TestBed.createComponent(UiItemCardComponent);
  });

  it('renders a prominent localized marker for a removed park item', () => {
    const card: ParkItemCardViewModel = {
      id: 'item-1',
      name: 'California Screamin’',
      subtitle: null,
      description: null,
      categoryLabelKey: 'parkExplorer.categories.attraction',
      typeLabelKey: 'parkExplorer.types.rollerCoaster',
      typeIconClass: 'pi pi-bolt',
      zoneName: 'Pixar Pier',
      imageUrl: null,
      imageSrcSet: null,
      lifecycleStatus: {
        labelKey: 'parkItems.statuses.removed',
        label: null,
        tone: 'rose',
        iconClass: 'pi pi-ban',
      },
      highlights: [],
      itemLink: null,
    };

    fixture.componentInstance.card = card;
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const marker: HTMLElement | null = host.querySelector(
      '.ui-item-card__chips .app-chip--rose',
    );

    expect(marker?.textContent).toContain('Supprimé / démonté');
    expect(marker?.querySelector('.pi-ban')).not.toBeNull();
  });
});
