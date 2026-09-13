import { CommonModule, DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  Inject,
  Input,
  OnChanges,
  signal
} from '@angular/core';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { ProfileComparisonCategory } from '@app/models/sharing/profile-comparison-invitation.models';
import { ShareContentField } from '@app/models/sharing/share-publication.models';
import { UiButtonDirective, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { ProfileComparisonInvitationCreatorStateFacade } from '../../state/profile-comparison-invitation-creator-state.facade';

@Component({
  selector: 'app-profile-comparison-invitation-creator',
  templateUrl: './profile-comparison-invitation-creator.component.html',
  styleUrl: './profile-comparison-invitation-creator.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ProfileComparisonInvitationCreatorStateFacade],
  imports: [CommonModule, TranslateModule, UiButtonDirective, UiKickerComponent, UiSurfaceDirective]
})
export class ProfileComparisonInvitationCreatorComponent implements OnChanges {
  @Input() public includedFields: ShareContentField[] = [];

  protected readonly facade: ProfileComparisonInvitationCreatorStateFacade;
  protected readonly copied = signal<boolean>(false);
  protected availableCategories: ProfileComparisonCategory[] = [];
  protected readonly categories: ProfileComparisonCategory[] = [
    'VisitedParks',
    'PersonalRatings',
    'YearlyActivity',
    'MissedItems'
  ];

  constructor(
    facade: ProfileComparisonInvitationCreatorStateFacade,
    private readonly translateService: TranslateService,
    @Inject(DOCUMENT) private readonly document: Document
  ) {
    this.facade = facade;
  }

  public ngOnChanges(): void {
    this.availableCategories = this.resolveAvailableCategories(this.includedFields);
    this.facade.initialize(this.availableCategories);
  }

  protected isAvailable(category: ProfileComparisonCategory): boolean {
    return this.availableCategories.includes(category);
  }

  protected isSelected(category: ProfileComparisonCategory): boolean {
    return this.facade.selected().includes(category);
  }

  protected invitationLink(): string {
    const token: string = this.facade.invitation()?.token ?? '';
    const language: string = this.translateService.currentLang || 'fr';
    const origin: string = this.document.defaultView?.location.origin ?? '';
    return `${origin}/${language}/profile/passport/comparisons/invitations/${encodeURIComponent(token)}`;
  }

  protected async copyInvitation(): Promise<void> {
    const clipboard: Clipboard | undefined = this.document.defaultView?.navigator.clipboard;
    if (!clipboard || !this.facade.invitation()) {
      return;
    }

    await clipboard.writeText(this.invitationLink());
    this.copied.set(true);
  }

  private resolveAvailableCategories(fields: ShareContentField[]): ProfileComparisonCategory[] {
    const selected: Set<ShareContentField> = new Set(fields);
    return this.categories.filter((category: ProfileComparisonCategory): boolean => {
      switch (category) {
        case 'VisitedParks':
          return selected.has('GeographicStatistics');
        case 'PersonalRatings':
          return selected.has('GlobalRatings');
        case 'YearlyActivity':
          return selected.has('GeographicStatistics') && selected.has('RideCount');
        case 'MissedItems':
          return selected.has('MissedItems');
      }
    });
  }
}
