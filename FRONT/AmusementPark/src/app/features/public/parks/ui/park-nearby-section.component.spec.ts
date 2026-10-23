import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import { NEARBY_DESTINATIONS_PORT } from '@features/public/nearby-destinations/nearby-destinations.ports';
import { mixedNearbyDestinations } from '@features/public/nearby-destinations/nearby-destinations.fixture';
import { ParkNearbySectionComponent } from './park-nearby-section.component';

describe('ParkNearbySectionComponent shared public suggestions', () => {
  const getNearby = vi.fn();

  beforeEach(async () => {
    getNearby.mockReset().mockReturnValue(of(mixedNearbyDestinations()));
    await TestBed.configureTestingModule({
      imports: [ParkNearbySectionComponent, TranslateModule.forRoot()],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting(),
        { provide: NEARBY_DESTINATIONS_PORT, useValue: { getNearby } },
        { provide: CountryDisplayService, useValue: { resolveLocalizedCountryName: () => 'France' } }
      ]
    }).compileComponents();
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('fr', { parks: { detail: { nearby: { title: 'À proximité', viewDestination: 'Voir la fiche' } } } });
    translate.use('fr');
  });

  it.each(['Park', 'StandaloneAttraction'] as const)('renders the same mixed cards with correct public links for %s', kind => {
    const fixture = TestBed.createComponent(ParkNearbySectionComponent);
    fixture.componentRef.setInput('sourceKind', kind);
    fixture.componentRef.setInput('sourceId', 'source');
    fixture.componentRef.setInput('currentLang', 'fr');
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(Array.from(root.querySelectorAll('.ui-park-card__title')).map(el => el.textContent?.trim())).toEqual(['Luge Alpine', 'Parc voisin']);
    expect(Array.from(root.querySelectorAll('.ui-park-card__cta')).map(el => el.getAttribute('href'))).toEqual([
      '/fr/attraction/alpine/luge-alpine', '/fr/park/park/parc-voisin'
    ]);
    expect(Array.from(root.querySelectorAll('.ui-park-card__cta')).every(el => el.textContent?.includes('Voir la fiche'))).toBe(true);
    expect(getNearby.mock.calls[0][0]).toBe(kind);
  });

  it('updates localized links without reloading and reloads when the source changes', () => {
    const fixture = TestBed.createComponent(ParkNearbySectionComponent);
    fixture.componentRef.setInput('sourceId', 'first');
    fixture.detectChanges();
    fixture.componentRef.setInput('currentLang', 'fr');
    fixture.detectChanges();
    expect(getNearby).toHaveBeenCalledTimes(1);
    expect(fixture.nativeElement.querySelector('.ui-park-card__cta').getAttribute('href')).toBe('/fr/attraction/alpine/luge-alpine');
    fixture.componentRef.setInput('sourceId', 'second');
    fixture.detectChanges();
    expect(getNearby).toHaveBeenCalledTimes(2);
  });
});
