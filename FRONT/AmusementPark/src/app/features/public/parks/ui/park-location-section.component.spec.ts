import { NO_ERRORS_SCHEMA } from '@angular/core';
import { DeferBlockBehavior, DeferBlockState, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { UiMapSlotComponent } from '@ui/maps';
import { mapParkToDetailViewModel } from '../mappers/park-detail-view.mapper';
import { ParkLocationSectionComponent } from './park-location-section.component';

describe('ParkLocationSectionComponent deferred map', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ParkLocationSectionComponent], providers: provideCommonTestDependencies(),
      deferBlockBehavior: DeferBlockBehavior.Manual
    }).overrideComponent(ParkLocationSectionComponent, {
      set: { imports: [TranslateModule, UiMapSlotComponent], schemas: [NO_ERRORS_SCHEMA] }
    }).compileComponents();
  });

  it('keeps location text and reserves map space before loading the map on visibility', async () => {
    const fixture = TestBed.createComponent(ParkLocationSectionComponent);
    fixture.componentRef.setInput('park', mapParkToDetailViewModel({
      id: 'park-1', name: 'Example Park', status: 'Operating', latitude: 48, longitude: 2
    }, 'fr'));
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('app-leaflet-static-map')).toBeNull();
    expect(fixture.nativeElement.querySelector('.park-location-card__map-placeholder')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.park-location-card__coordinates').textContent).toContain('48');
    const blocks = await fixture.getDeferBlocks();
    expect(blocks.length).toBe(1);
    await blocks[0].render(DeferBlockState.Complete);
    expect(fixture.nativeElement.querySelector('app-leaflet-static-map')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.park-location-card__map-placeholder')).toBeNull();
  });

  it('does not instantiate a map or defer block for unavailable coordinates', async () => {
    const fixture = TestBed.createComponent(ParkLocationSectionComponent);
    const park = mapParkToDetailViewModel({ id: 'park-1', name: 'Example Park', status: 'Operating', latitude: 48, longitude: 2 }, 'fr');
    park.hasLocationInfo = false;
    fixture.componentRef.setInput('park', park);
    fixture.detectChanges();
    expect(await fixture.getDeferBlocks()).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('app-leaflet-static-map')).toBeNull();
  });
});
