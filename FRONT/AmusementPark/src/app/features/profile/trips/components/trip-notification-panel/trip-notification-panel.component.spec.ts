import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal, WritableSignal } from '@angular/core';

import {
  COMMON_TEST_IMPORTS,
  provideCommonTestDependencies
} from '@app/testing/common-test-providers';
import { TripNotificationFacade } from '../../state/trip-notification.facade';
import { TripNotificationPanelComponent } from './trip-notification-panel.component';

describe('TripNotificationPanelComponent', () => {
  it('loads once per trip and ignores facade signal changes', async () => {
    const loading: WritableSignal<boolean> = signal(false);
    const facade = {
      state: signal(null).asReadonly(),
      loading: loading.asReadonly(),
      saving: signal(false).asReadonly(),
      error: signal(false).asReadonly(),
      load: vi.fn((): void => {
        loading();
      }),
      toggle: vi.fn(),
      markRead: vi.fn(),
      refresh: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, TripNotificationPanelComponent],
      providers: provideCommonTestDependencies()
    })
      .overrideComponent(TripNotificationPanelComponent, {
        set: { providers: [{ provide: TripNotificationFacade, useValue: facade }] }
      })
      .compileComponents();

    const fixture: ComponentFixture<TripNotificationPanelComponent> =
      TestBed.createComponent(TripNotificationPanelComponent);
    fixture.componentRef.setInput('tripPlanId', 'trip-1');
    fixture.componentRef.setInput('currentLanguage', 'fr');
    fixture.detectChanges();
    await fixture.whenStable();

    expect(facade.load).toHaveBeenCalledTimes(1);
    expect(facade.load).toHaveBeenLastCalledWith('trip-1');

    loading.set(true);
    fixture.detectChanges();
    loading.set(false);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(facade.load).toHaveBeenCalledTimes(1);

    fixture.componentRef.setInput('tripPlanId', 'trip-2');
    fixture.detectChanges();
    await fixture.whenStable();

    expect(facade.load).toHaveBeenCalledTimes(2);
    expect(facade.load).toHaveBeenLastCalledWith('trip-2');
  });
});
