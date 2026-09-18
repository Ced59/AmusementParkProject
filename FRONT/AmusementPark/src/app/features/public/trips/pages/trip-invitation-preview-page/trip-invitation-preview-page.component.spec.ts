import { provideLocationMocks } from '@angular/common/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateModule } from '@ngx-translate/core';
import { NEVER, Subject } from 'rxjs';

import { TranslationService } from '@app/services/translation.service';
import {
  TRIP_INVITATIONS_DATA_PORT,
  TripInvitationsDataPort
} from '@features/trips/state/trip-invitation-data.port';
import { SeoService } from '@core/seo/seo.service';
import { ModalService } from '@app/services/modal/modal.service';
import { TripInvitationPreviewPageComponent } from './trip-invitation-preview-page.component';

describe('TripInvitationPreviewPageComponent', () => {
  it('tracks in-place language and invitation token navigation', async () => {
    const languageChanged = new Subject<string>();
    const data: TripInvitationsDataPort = {
      list: vi.fn(),
      create: vi.fn(),
      revoke: vi.fn(),
      preview: vi.fn().mockReturnValue(NEVER)
    };
    TestBed.configureTestingModule({
      imports: [TranslateModule.forRoot()],
      providers: [
        provideRouter([{
          path: ':lang/trip-invitations/:token',
          component: TripInvitationPreviewPageComponent
        }]),
        provideLocationMocks(),
        { provide: TRIP_INVITATIONS_DATA_PORT, useValue: data },
        { provide: TranslationService, useValue: { getCurrentLang: (): string => 'en', languageChanged } },
        { provide: SeoService, useValue: { applyRouteDefaults: vi.fn() } },
        { provide: ModalService, useValue: { openModal: vi.fn() } }
      ]
    });
    const harness = await RouterTestingHarness.create();
    const page = await harness.navigateByUrl(
      '/en/trip-invitations/opaque-token',
      TripInvitationPreviewPageComponent
    );
    harness.detectChanges();
    expect(homeLink(harness)).toBe('/en/home');

    languageChanged.next('fr');
    await harness.navigateByUrl('/fr/trip-invitations/opaque-token', TripInvitationPreviewPageComponent);
    harness.detectChanges();

    expect(homeLink(harness)).toBe('/fr/home');
    expect(data.preview).toHaveBeenCalledOnce();

    expect(await harness.navigateByUrl(
      '/fr/trip-invitations/second-token',
      TripInvitationPreviewPageComponent
    )).toBe(page);
    expect(data.preview).toHaveBeenNthCalledWith(2, 'second-token');
  });
});

function homeLink(harness: RouterTestingHarness): string | null {
  return harness.routeNativeElement
    ?.querySelector<HTMLAnchorElement>('.invitation-page__breadcrumb a')
    ?.getAttribute('href') ?? null;
}
