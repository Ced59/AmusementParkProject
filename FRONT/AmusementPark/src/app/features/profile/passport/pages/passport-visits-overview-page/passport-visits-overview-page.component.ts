import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { TranslationService } from '@app/services/translation.service';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { LocalizedPluralPipe } from '@shared/pipes/localized-plural.pipe';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiPrimitiveTone, UiSurfaceDirective } from '@ui/primitives';
import { PassportVisitQuickCreateComponent } from '../../components/passport-visit-quick-create/passport-visit-quick-create.component';
import { PassportExportPanelComponent } from '../../components/passport-export-panel/passport-export-panel.component';
import { PassportAnonymousImportPanelComponent } from '../../anonymous-drafts/components/passport-anonymous-import-panel/passport-anonymous-import-panel.component';
import { PassportVisitOverviewItemViewModel } from '../../models/passport-visits-overview.models';
import { PassportVisitsOverviewStateFacade } from '../../state/passport-visits-overview-state.facade';

@Component({
  selector: 'app-passport-visits-overview-page',
  templateUrl: './passport-visits-overview-page.component.html',
  styleUrl: './passport-visits-overview-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PassportVisitsOverviewStateFacade],
  imports: [
    TranslateModule,
    LocalizedPluralPipe,
    PageStateComponent,
    PassportVisitQuickCreateComponent,
    PassportAnonymousImportPanelComponent,
    PassportExportPanelComponent,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSurfaceDirective
  ]
})
export class PassportVisitsOverviewPageComponent implements OnInit {
  protected readonly facade = inject(PassportVisitsOverviewStateFacade);
  protected readonly quickCreateVisible = signal<boolean>(false);

  private readonly router = inject(Router);
  private readonly translationService = inject(TranslationService);

  public ngOnInit(): void {
    this.facade.load();
  }

  protected openVisit(visitId: string): void {
    void this.router.navigate([
      '/',
      this.currentLanguage(),
      'profile',
      'visits',
      visitId
    ]);
  }

  protected backToProfile(): void {
    void this.router.navigate(['/', this.currentLanguage(), 'profile']);
  }

  protected openQuickCreate(): void {
    this.quickCreateVisible.set(true);
  }

  protected onQuickCreateVisibleChange(visible: boolean): void {
    this.quickCreateVisible.set(visible);
  }

  protected onVisitCreated(): void {
    this.facade.load();
  }

  protected statusTone(status: PassportVisitOverviewItemViewModel['status']): UiPrimitiveTone {
    if (status === 'Completed') {
      return 'lime';
    }

    return status === 'Archived' ? 'soft' : 'sky';
  }

  protected trackVisit(_index: number, visit: PassportVisitOverviewItemViewModel): string {
    return visit.id;
  }

  private currentLanguage(): string {
    return this.translationService.getCurrentLang() || this.router.url.split('/')[1] || 'en';
  }
}
