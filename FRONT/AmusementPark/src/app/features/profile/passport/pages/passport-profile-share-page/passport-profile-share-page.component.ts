import { ChangeDetectionStrategy, Component, OnInit, Signal, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import {
  PassportProfileSharePreview,
  ShareContentField,
  ShareVisibility
} from '@app/models/sharing/share-publication.models';
import { TranslationService } from '@app/services/translation.service';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { UiButtonDirective, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { PassportProfileShareStateFacade } from '../../state/passport-profile-share-state.facade';

@Component({
  selector: 'app-passport-profile-share-page',
  templateUrl: './passport-profile-share-page.component.html',
  styleUrl: './passport-profile-share-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PassportProfileShareStateFacade],
  imports: [TranslateModule, PageStateComponent, UiButtonDirective, UiKickerComponent, UiSurfaceDirective]
})
export class PassportProfileSharePageComponent implements OnInit {
  protected readonly facade = inject(PassportProfileShareStateFacade);
  protected readonly preview: Signal<PassportProfileSharePreview | null> = this.facade.preview;

  private readonly router = inject(Router);
  private readonly translationService = inject(TranslationService);

  public ngOnInit(): void {
    this.facade.load();
  }

  protected hasYear(year: number): boolean { return this.facade.selectedYears().includes(year); }
  protected hasPark(parkId: string): boolean { return this.facade.selectedParkIds().includes(parkId); }
  protected hasRating(selectionKey: string): boolean { return this.facade.selectedRatingKeys().includes(selectionKey); }
  protected hasField(field: ShareContentField): boolean { return this.facade.includedFields().includes(field); }

  protected setVisibility(value: string): void {
    if (value === 'Public' || value === 'Unlisted') {
      this.facade.setVisibility(value as ShareVisibility);
    }
  }

  protected formatRating(value: number | null | undefined): string {
    return value == null
      ? '–'
      : new Intl.NumberFormat(this.currentLanguage(), { minimumFractionDigits: 1, maximumFractionDigits: 1 }).format(value);
  }

  protected countryName(countryCode: string): string {
    try {
      return new Intl.DisplayNames([this.currentLanguage()], { type: 'region' }).of(countryCode) ?? countryCode;
    } catch {
      return countryCode;
    }
  }

  protected openPublicPassport(): void {
    const shareId: string | null | undefined = this.facade.settings()?.shareId;
    if (shareId) {
      void this.router.navigate(['/', this.currentLanguage(), 'passport', 'shared', 'profiles', shareId]);
    }
  }

  protected backToPassport(): void {
    void this.router.navigate(['/', this.currentLanguage(), 'profile', 'passport']);
  }

  private currentLanguage(): string {
    return this.translationService.getCurrentLang() || this.router.url.split('/')[1] || 'en';
  }
}
