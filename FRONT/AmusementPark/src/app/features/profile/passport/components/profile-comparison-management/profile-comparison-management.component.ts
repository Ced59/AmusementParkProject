import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  Inject,
  OnInit,
  signal,
} from '@angular/core';
import { Router } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { ProfileComparisonCategory } from '@app/models/sharing/profile-comparison-invitation.models';
import { ProfileComparisonSummary } from '@app/models/sharing/profile-comparison.models';
import {
  UiButtonDirective,
  UiKickerComponent,
  UiSurfaceDirective,
} from '@ui/primitives';
import { ProfileComparisonManagementStateFacade } from '../../state/profile-comparison-management-state.facade';

@Component({
  selector: 'app-profile-comparison-management',
  templateUrl: './profile-comparison-management.component.html',
  styleUrl: './profile-comparison-management.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ProfileComparisonManagementStateFacade],
  imports: [
    TranslateModule,
    UiButtonDirective,
    UiKickerComponent,
    UiSurfaceDirective,
  ],
})
export class ProfileComparisonManagementComponent implements OnInit {
  protected readonly pendingRevocation = signal<string | null>(null);
  protected readonly copiedShareId = signal<string | null>(null);

  constructor(
    protected readonly facade: ProfileComparisonManagementStateFacade,
    private readonly router: Router,
    private readonly translateService: TranslateService,
    @Inject(DOCUMENT) private readonly document: Document,
  ) {}

  public ngOnInit(): void {
    this.facade.load();
  }

  protected categoryKey(category: ProfileComparisonCategory): string {
    return `profileComparisonInvitation.categories.${category}.title`;
  }

  protected otherName(comparison: ProfileComparisonSummary): string {
    return (
      comparison.otherDisplayName ||
      this.translateService.instant('profileComparison.result.member')
    );
  }

  protected open(comparison: ProfileComparisonSummary): void {
    void this.router.navigate([
      '/',
      this.translateService.currentLang || 'fr',
      'passport',
      'shared',
      'comparisons',
      comparison.shareId,
    ]);
  }

  protected comparisonLink(comparison: ProfileComparisonSummary): string {
    const origin: string = this.document.defaultView?.location.origin ?? '';
    const language: string = this.translateService.currentLang || 'fr';
    return `${origin}/${language}/passport/shared/comparisons/${encodeURIComponent(comparison.shareId)}`;
  }

  protected async copy(comparison: ProfileComparisonSummary): Promise<void> {
    const clipboard: Clipboard | undefined =
      this.document.defaultView?.navigator.clipboard;
    if (!clipboard) {
      return;
    }
    await clipboard.writeText(this.comparisonLink(comparison));
    this.copiedShareId.set(comparison.shareId);
  }

  protected confirmRevocation(shareId: string): void {
    this.pendingRevocation.set(shareId);
  }

  protected cancelRevocation(): void {
    this.pendingRevocation.set(null);
  }

  protected revoke(shareId: string): void {
    this.pendingRevocation.set(null);
    this.facade.revoke(shareId);
  }
}
