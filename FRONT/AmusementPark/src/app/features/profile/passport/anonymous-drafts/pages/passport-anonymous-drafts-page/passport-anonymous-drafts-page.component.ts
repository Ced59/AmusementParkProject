import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { Dialog } from '@shared/ui/primitives/dialog';
import { UiButtonDirective, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { PassportAnonymousDraft } from '../../models/passport-anonymous-draft.models';
import { PassportAnonymousDraftsStateFacade } from '../../state/passport-anonymous-drafts-state.facade';

@Component({
  selector: 'app-passport-anonymous-drafts-page',
  templateUrl: './passport-anonymous-drafts-page.component.html',
  styleUrl: './passport-anonymous-drafts-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PassportAnonymousDraftsStateFacade],
  imports: [
    Dialog,
    PageStateComponent,
    TranslateModule,
    UiButtonDirective,
    UiKickerComponent,
    UiSurfaceDirective
  ]
})
export class PassportAnonymousDraftsPageComponent implements OnInit {
  protected readonly facade = inject(PassportAnonymousDraftsStateFacade);
  protected readonly clearConfirmationVisible = signal<boolean>(false);
  protected readonly draftPendingDeletion = signal<string | null>(null);
  private readonly router: Router = inject(Router);

  ngOnInit(): void {
    void this.facade.load();
  }

  protected openDraft(draftId: string): void {
    void this.router.navigate([
      '/',
      this.currentLanguage(),
      'passport',
      'local',
      draftId
    ]);
  }

  protected goToParks(): void {
    void this.router.navigate(['/', this.currentLanguage(), 'parks']);
  }

  protected openImport(): void {
    void this.router.navigate(['/', this.currentLanguage(), 'profile', 'passport'], {
      fragment: 'passport-anonymous-import'
    });
  }

  protected async confirmClear(): Promise<void> {
    await this.facade.clear();
    this.clearConfirmationVisible.set(false);
  }

  protected async confirmDelete(): Promise<void> {
    const draftId: string = this.draftPendingDeletion() ?? '';
    if (!draftId) {
      return;
    }

    await this.facade.delete(draftId);
    this.draftPendingDeletion.set(null);
  }

  protected trackDraft(_index: number, draft: PassportAnonymousDraft): string {
    return draft.id;
  }

  private currentLanguage(): string {
    return this.router.url.split('/')[1] || 'en';
  }
}
