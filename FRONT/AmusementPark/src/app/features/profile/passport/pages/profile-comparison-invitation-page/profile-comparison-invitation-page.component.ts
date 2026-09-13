import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { ProfileComparisonCategory } from '@app/models/sharing/profile-comparison-invitation.models';
import { TranslationService } from '@app/services/translation.service';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { UiButtonDirective, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { ProfileComparisonInvitationAcceptanceStateFacade } from '../../state/profile-comparison-invitation-acceptance-state.facade';

@Component({
  selector: 'app-profile-comparison-invitation-page',
  templateUrl: './profile-comparison-invitation-page.component.html',
  styleUrl: './profile-comparison-invitation-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ProfileComparisonInvitationAcceptanceStateFacade],
  imports: [CommonModule, TranslateModule, PageStateComponent, UiButtonDirective, UiKickerComponent, UiSurfaceDirective]
})
export class ProfileComparisonInvitationPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly translationService = inject(TranslationService);
  protected readonly facade = inject(ProfileComparisonInvitationAcceptanceStateFacade);

  public ngOnInit(): void {
    this.facade.load(this.route.snapshot.paramMap.get('token') ?? '');
  }

  protected categoryKey(category: ProfileComparisonCategory): string {
    return `profileComparisonInvitation.categories.${category}.title`;
  }

  protected backToPassport(): void {
    const language: string = this.translationService.getCurrentLang() || 'fr';
    void this.router.navigate(['/', language, 'profile', 'passport']);
  }

  protected openComparison(): void {
    const shareId: string | undefined = this.facade.acceptance()?.shareId;
    if (!shareId) {
      return;
    }
    const language: string = this.translationService.getCurrentLang() || 'fr';
    void this.router.navigate([
      '/',
      language,
      'passport',
      'shared',
      'comparisons',
      shareId
    ]);
  }
}
