import { EventEmitter, WritableSignal, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';

import { TranslationService } from '@app/services/translation.service';
import { ParkReferenceDetailPageComponent } from './park-reference-detail-page.component';
import { ParkReferenceDetailStateFacade } from '../state/park-reference-detail-state.facade';

describe('ParkReferenceDetailPageComponent', () => {
  let fixture: ComponentFixture<ParkReferenceDetailPageComponent>;
  let routeData: Record<string, unknown>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    routeData = { referenceKind: 'manufacturer' };
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);

    const stateFacadeStub: unknown = {
      state: signal({ kind: 'ready' }),
      reference: signal(null),
      attractionsLoading: signal(false),
      setCurrentLanguage: jasmine.createSpy('setCurrentLanguage'),
      loadReference: jasmine.createSpy('loadReference'),
      loadManufacturerAttractionsPage: jasmine.createSpy('loadManufacturerAttractionsPage')
    };

    await TestBed.configureTestingModule({
      imports: [ParkReferenceDetailPageComponent],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              data: routeData
            }
          }
        },
        { provide: Router, useValue: router },
        {
          provide: TranslationService,
          useValue: {
            getCurrentLang: (): string => 'en',
            languageChanged: new EventEmitter<string>()
          }
        }
      ]
    })
      .overrideComponent(ParkReferenceDetailPageComponent, {
        set: {
          template: '',
          providers: [
            { provide: ParkReferenceDetailStateFacade, useValue: stateFacadeStub }
          ]
        }
      })
      .compileComponents();

    fixture = TestBed.createComponent(ParkReferenceDetailPageComponent);
  });

  it('returns from a manufacturer profile to the public manufacturer list', () => {
    const component: ParkReferenceDetailPageTestingSurface = fixture.componentInstance as unknown as ParkReferenceDetailPageTestingSurface;
    component.currentLang.set('fr');

    component.goBack();

    expect(router.navigate).toHaveBeenCalledWith(['/', 'fr', 'manufacturers']);
  });

  it('keeps other reference profiles returning to the public park list', () => {
    routeData['referenceKind'] = 'operator';
    const component: ParkReferenceDetailPageTestingSurface = fixture.componentInstance as unknown as ParkReferenceDetailPageTestingSurface;
    component.currentLang.set('fr');

    component.goBack();

    expect(router.navigate).toHaveBeenCalledWith(['/', 'fr', 'parks']);
  });
});

interface ParkReferenceDetailPageTestingSurface {
  readonly currentLang: WritableSignal<string>;
  goBack(): void;
}
