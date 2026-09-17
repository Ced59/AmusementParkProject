import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateModule } from '@ngx-translate/core';

import { TranslationService } from '@app/services/translation.service';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { NotificationEmailPreferencesFacade } from '../state/notification-email-preferences.facade';

@Component({
  selector: 'app-notification-email-preferences-panel',
  templateUrl: './notification-email-preferences-panel.component.html',
  styleUrl: './notification-email-preferences-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [NotificationEmailPreferencesFacade],
  imports: [
    TranslateModule,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSurfaceDirective
  ]
})
export class NotificationEmailPreferencesPanelComponent implements OnInit {
  protected readonly currentLang = signal<string>('en');

  constructor(
    protected readonly facade: NotificationEmailPreferencesFacade,
    translationService: TranslationService,
    destroyRef: DestroyRef
  ) {
    this.currentLang.set(translationService.getCurrentLang() || 'en');
    translationService.languageChanged
      .pipe(takeUntilDestroyed(destroyRef))
      .subscribe((language: string): void => this.currentLang.set(language || 'en'));
  }

  ngOnInit(): void {
    this.facade.load();
  }
}
