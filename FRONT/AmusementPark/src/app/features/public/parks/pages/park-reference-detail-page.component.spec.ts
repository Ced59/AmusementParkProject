import type { Mock, MockedObject } from 'vitest';
import { EventEmitter, WritableSignal, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  ActivatedRoute,
  ParamMap,
  Router,
  convertToParamMap,
} from '@angular/router';
import { Subject } from 'rxjs';

import { TranslationService } from '@app/services/translation.service';
import { AdminContextualBlockRefreshEvents } from '@features/admin/contextual-editing/state/admin-contextual-block-refresh-events.service';
import { ParkReferenceDetailPageComponent } from './park-reference-detail-page.component';
import { ParkReferenceDetailStateFacade } from '../state/park-reference-detail-state.facade';

describe('ParkReferenceDetailPageComponent', () => {
  let fixture: ComponentFixture<ParkReferenceDetailPageComponent>;
  let routeData: Record<string, unknown>;
  let router: MockedObject<Router>;
  let paramMapSubject: Subject<ParamMap>;
  let stateFacadeStub: {
    state: WritableSignal<{
      kind: string;
    }>;
    reference: WritableSignal<null>;
    attractionsLoading: WritableSignal<boolean>;
    setCurrentLanguage: Mock;
    loadReference: Mock;
    loadManufacturerAttractionsPage: Mock;
  };

  beforeEach(async () => {
    routeData = { referenceKind: 'manufacturer' };
    router = {
      navigate: vi.fn().mockName('Router.navigate'),
    } as unknown as MockedObject<Router>;
    paramMapSubject = new Subject<ParamMap>();

    stateFacadeStub = {
      state: signal({ kind: 'ready' }),
      reference: signal(null),
      attractionsLoading: signal(false),
      setCurrentLanguage: vi.fn(),
      loadReference: vi.fn(),
      loadManufacturerAttractionsPage: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [ParkReferenceDetailPageComponent],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              data: routeData,
              paramMap: convertToParamMap({ id: 'manufacturer-1' }),
            },
            paramMap: paramMapSubject.asObservable(),
          },
        },
        { provide: Router, useValue: router },
        {
          provide: TranslationService,
          useValue: {
            getCurrentLang: (): string => 'en',
            languageChanged: new EventEmitter<string>(),
          },
        },
      ],
    })
      .overrideComponent(ParkReferenceDetailPageComponent, {
        set: {
          template: '',
          providers: [
            {
              provide: ParkReferenceDetailStateFacade,
              useValue: stateFacadeStub,
            },
          ],
        },
      })
      .compileComponents();

    fixture = TestBed.createComponent(ParkReferenceDetailPageComponent);
  });

  it('returns from a manufacturer profile to the public manufacturer list', () => {
    const component: ParkReferenceDetailPageTestingSurface =
      fixture.componentInstance as unknown as ParkReferenceDetailPageTestingSurface;
    component.currentLang.set('fr');

    component.goBack();

    expect(router.navigate).toHaveBeenCalledWith(['/', 'fr', 'manufacturers']);
  });

  it('keeps other reference profiles returning to the public park list', () => {
    routeData['referenceKind'] = 'operator';
    const component: ParkReferenceDetailPageTestingSurface =
      fixture.componentInstance as unknown as ParkReferenceDetailPageTestingSurface;
    component.currentLang.set('fr');

    component.goBack();

    expect(router.navigate).toHaveBeenCalledWith(['/', 'fr', 'parks']);
  });

  it('reloads the current manufacturer when a contextual upsert import applies', () => {
    const refreshEvents: AdminContextualBlockRefreshEvents = TestBed.inject(
      AdminContextualBlockRefreshEvents,
    );
    fixture.detectChanges();
    stateFacadeStub.loadReference.mockClear();

    refreshEvents.notifyBlockApplied({
      blockType: 'reference.manufacturer',
      entityType: 'AttractionManufacturer',
      entityId: 'manufacturer-1',
      appliedAtUtc: '2026-06-22T00:00:00Z',
    });

    expect(stateFacadeStub.loadReference).toHaveBeenCalledTimes(1);

    expect(stateFacadeStub.loadReference).toHaveBeenCalledWith(
      'manufacturer',
      'manufacturer-1',
    );
  });
});

interface ParkReferenceDetailPageTestingSurface {
  readonly currentLang: WritableSignal<string>;
  goBack(): void;
}
