import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { of } from 'rxjs';

import { TechnicalPage } from '@app/models/technical-pages/technical-page';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import {
  PUBLIC_TECHNICAL_PAGES_API_SERVICE_PORT,
  PublicTechnicalPagesApiServicePort
} from '../state/public-technical-pages-data.ports';
import { TechnicalPagesPageComponent } from './technical-pages-page.component';

describe('TechnicalPagesPageComponent', (): void => {
  it('loads the public list through the technical-pages port', (): void => {
    const page: TechnicalPage = { slug: 'lift' } as TechnicalPage;
    const apiPort: jasmine.SpyObj<PublicTechnicalPagesApiServicePort> =
      jasmine.createSpyObj<PublicTechnicalPagesApiServicePort>('PublicTechnicalPagesApiServicePort', ['getAllPublicPages']);
    apiPort.getAllPublicPages.and.returnValue(of([page]));

    TestBed.configureTestingModule({
      imports: [TechnicalPagesPageComponent],
      providers: [
        { provide: PUBLIC_TECHNICAL_PAGES_API_SERVICE_PORT, useValue: apiPort },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: convertToParamMap({ lang: 'fr' }) },
            paramMap: of(convertToParamMap({ lang: 'fr' })),
            parent: null
          }
        },
        { provide: Router, useValue: { url: '/fr/technical' } },
        { provide: TranslationService, useValue: { getCurrentLang: (): string => 'fr' } },
        { provide: SeoService, useValue: jasmine.createSpyObj<SeoService>('SeoService', ['applyRouteDefaults']) }
      ]
    }).overrideComponent(TechnicalPagesPageComponent, {
      set: { template: '' }
    });

    const fixture: ComponentFixture<TechnicalPagesPageComponent> = TestBed.createComponent(TechnicalPagesPageComponent);
    fixture.detectChanges();

    expect(apiPort.getAllPublicPages).toHaveBeenCalledTimes(1);
  });
});
