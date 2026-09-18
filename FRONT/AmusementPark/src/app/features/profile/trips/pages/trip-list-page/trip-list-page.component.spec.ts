import { signal, WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TranslationService } from '@app/services/translation.service';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { TripListStateFacade } from '../../state/trip-list-state.facade';
import { TripListPageComponent } from './trip-list-page.component';

describe('TripListPageComponent', () => {
  it('locks every creation field while the request is pending', async () => {
    const creating: WritableSignal<boolean> = signal<boolean>(false);
    const facade = {
      trips: signal([]).asReadonly(),
      status: signal('ready').asReadonly(),
      creating: creating.asReadonly(),
      createError: signal(false).asReadonly(),
      createdTripId: signal(null).asReadonly(),
      load: vi.fn(),
      create: vi.fn(),
      clearCreatedTrip: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, TripListPageComponent],
      providers: [
        ...provideCommonTestDependencies(),
        { provide: TranslationService, useValue: { getCurrentLang: (): string => 'fr' } }
      ]
    })
      .overrideComponent(TripListPageComponent, {
        set: { providers: [{ provide: TripListStateFacade, useValue: facade }] }
      })
      .compileComponents();

    const fixture: ComponentFixture<TripListPageComponent> = TestBed.createComponent(TripListPageComponent);
    const component = fixture.componentInstance as unknown as {
      composerVisible: WritableSignal<boolean>;
      startDate: WritableSignal<string>;
      endDate: WritableSignal<string>;
      datesTooLong: () => boolean;
    };
    component.composerVisible.set(true);
    component.startDate.set('2026-10-03');
    fixture.detectChanges();

    const fields: HTMLInputElement[] = Array.from(
      fixture.nativeElement.querySelectorAll('.trip-composer input')
    );
    expect(fields).toHaveLength(4);
    expect(fields.every((field: HTMLInputElement): boolean => !field.disabled)).toBe(true);

    component.startDate.set('2026-01-01');
    component.endDate.set('2027-01-02');
    fixture.detectChanges();
    expect(component.datesTooLong()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('trips.feedback.dateRangeTooLong');

    creating.set(true);
    fixture.detectChanges();

    expect(fields.every((field: HTMLInputElement): boolean => field.disabled)).toBe(true);
  });
});
