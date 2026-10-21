import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';

import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { mapAttractionManufacturerToReferenceDetailViewModel } from '../mappers/park-reference-detail-view.mapper';
import { ParkReferenceDetailViewComponent } from './park-reference-detail-view.component';

describe('ParkReferenceDetailViewComponent attraction type labels', () => {
  let fixture: ComponentFixture<ParkReferenceDetailViewComponent>;
  let translateService: TranslateService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ParkReferenceDetailViewComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();

    translateService = TestBed.inject(TranslateService);
    translateService.setTranslation('fr', {
      parkExplorer: { types: { rollerCoaster: 'Montagnes russes' } },
    });
    translateService.setTranslation('pt', {
      parkExplorer: { types: { rollerCoaster: 'Montanhas-russas' } },
    });
    translateService.use('fr');

    const attractions: ParkItemAdminRow[] = [
      {
        id: 'ride-1',
        parkId: 'park-1',
        parkName: 'Parc test',
        name: 'Attraction avec lien',
        category: 'Attraction',
        type: 'RollerCoaster',
        isVisible: true,
        adminReviewStatus: 'Validated',
      },
      {
        id: 'ride-2',
        parkId: '',
        parkName: '',
        name: 'Attraction sans lien',
        category: 'Attraction',
        type: 'RollerCoaster',
        isVisible: true,
        adminReviewStatus: 'Validated',
      },
    ];
    fixture = TestBed.createComponent(ParkReferenceDetailViewComponent);
    fixture.componentRef.setInput('state', signal<ScreenState>({ kind: 'ready' }));
    fixture.componentRef.setInput('reference', signal(
      mapAttractionManufacturerToReferenceDetailViewModel(
        { id: 'manufacturer-1', name: 'Constructeur' }, 'fr', [], attractions,
      ),
    ));
    fixture.componentRef.setInput('attractionsLoading', signal(false));
    fixture.componentRef.setInput('currentLang', 'fr');
    fixture.detectChanges();
  });

  it('translates API type values in linked and unlinked attraction cards', () => {
    const element: HTMLElement = fixture.nativeElement;

    expect(element.querySelector('a.attraction-card .attraction-card__meta')?.textContent?.trim())
      .toBe('Montagnes russes');
    expect(element.querySelector('article.attraction-card .attraction-card__meta')?.textContent?.trim())
      .toBe('Montagnes russes');
    expect(element.textContent).not.toContain('RollerCoaster');
  });

  it('updates both labels when the active language changes', () => {
    translateService.use('pt');
    fixture.componentRef.setInput('currentLang', 'pt');
    fixture.detectChanges();

    const element: HTMLElement = fixture.nativeElement;
    const labels: string[] = Array.from(element.querySelectorAll('.attraction-card__meta'))
      .map((label: Element) => label.textContent?.trim() ?? '');

    expect(labels).toEqual(['Montanhas-russas', 'Montanhas-russas']);
  });
});
