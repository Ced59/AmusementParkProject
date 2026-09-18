import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { distinctUntilChanged, map } from 'rxjs';

import {
  TripInvitationMemberCountBand,
  TripInvitationRole
} from '@app/models/trips/trip-invitation.models';
import { ModalService } from '@app/services/modal/modal.service';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import {
  findNearestLanguageActivatedRoute,
  resolveLanguageFromParamMap
} from '@shared/utils/routing/route-language.utils';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { TripInvitationPreviewStateFacade } from '../../state/trip-invitation-preview-state.facade';

@Component({
  selector: 'app-trip-invitation-preview-page',
  templateUrl: './trip-invitation-preview-page.component.html',
  styleUrl: './trip-invitation-preview-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [TripInvitationPreviewStateFacade],
  imports: [
    DatePipe,
    RouterLink,
    TranslateModule,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSurfaceDirective
  ]
})
export class TripInvitationPreviewPageComponent implements OnInit {
  protected readonly currentLanguage = signal<string>('en');

  constructor(
    private readonly route: ActivatedRoute,
    private readonly translationService: TranslationService,
    private readonly seoService: SeoService,
    private readonly modalService: ModalService,
    private readonly destroyRef: DestroyRef,
    protected readonly facade: TripInvitationPreviewStateFacade
  ) {
  }

  ngOnInit(): void {
    const languageRoute: ActivatedRoute = findNearestLanguageActivatedRoute(this.route) ?? this.route;
    languageRoute.paramMap.pipe(
      map((params: ParamMap): string => resolveLanguageFromParamMap(
        params,
        this.translationService.getCurrentLang() || 'en'
      )),
      distinctUntilChanged(),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((language: string): void => {
      this.currentLanguage.set(language);
      this.seoService.applyRouteDefaults(`/${language}/trip-invitations/[REDACTED]`);
    });
    this.route.paramMap.pipe(
      map((params: ParamMap): string => params.get('token') ?? ''),
      distinctUntilChanged(),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((token: string): void => this.facade.load(token));
  }

  protected continueWithAccount(): void {
    this.modalService.openModal('loginModal');
  }

  protected roleLabelKey(role: TripInvitationRole): string {
    return `trips.invitations.roles.${role.toLowerCase()}`;
  }

  protected memberBandLabelKey(band: TripInvitationMemberCountBand): string {
    return `trips.invitations.memberBands.${band.toLowerCase()}`;
  }

  protected periodLabel(): string {
    const preview = this.facade.preview();
    if (!preview || preview.periodKind === 'Unspecified' || !preview.startMonth) {
      return 'trips.invitations.periodUnspecified';
    }
    return preview.periodKind === 'SingleMonth'
      ? preview.startMonth
      : `${preview.startMonth} → ${preview.endMonth ?? preview.startMonth}`;
  }
}
