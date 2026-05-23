import { ChangeDetectionStrategy, Component, Signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { AnalyticsConsentService } from '@core/analytics/analytics-consent.service';
import { AnalyticsConsentDecision } from '@core/analytics/analytics-consent.model';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';

interface PrivacyPolicySection {
  readonly iconClass: string;
  readonly titleKey: string;
  readonly bodyKeys: readonly string[];
}

@Component({
  selector: 'app-privacy-policy-page',
  templateUrl: './privacy-policy-page.component.html',
  styleUrl: './privacy-policy-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslateModule, UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective]
})
export class PrivacyPolicyPageComponent {
  protected readonly analyticsDecision: Signal<AnalyticsConsentDecision | null> = this.analyticsConsentService.decision;
  protected readonly sections: readonly PrivacyPolicySection[] = [
    {
      iconClass: 'pi pi-user',
      titleKey: 'privacyPage.sections.controller.title',
      bodyKeys: [
        'privacyPage.sections.controller.body1',
        'privacyPage.sections.controller.body2'
      ]
    },
    {
      iconClass: 'pi pi-database',
      titleKey: 'privacyPage.sections.data.title',
      bodyKeys: [
        'privacyPage.sections.data.body1',
        'privacyPage.sections.data.body2'
      ]
    },
    {
      iconClass: 'pi pi-bullseye',
      titleKey: 'privacyPage.sections.purposes.title',
      bodyKeys: [
        'privacyPage.sections.purposes.body1',
        'privacyPage.sections.purposes.body2'
      ]
    },
    {
      iconClass: 'pi pi-chart-line',
      titleKey: 'privacyPage.sections.analytics.title',
      bodyKeys: [
        'privacyPage.sections.analytics.body1',
        'privacyPage.sections.analytics.body2',
        'privacyPage.sections.analytics.body3'
      ]
    },
    {
      iconClass: 'pi pi-lock',
      titleKey: 'privacyPage.sections.cookies.title',
      bodyKeys: [
        'privacyPage.sections.cookies.body1',
        'privacyPage.sections.cookies.body2'
      ]
    },
    {
      iconClass: 'pi pi-server',
      titleKey: 'privacyPage.sections.hosting.title',
      bodyKeys: [
        'privacyPage.sections.hosting.body1',
        'privacyPage.sections.hosting.body2'
      ]
    },
    {
      iconClass: 'pi pi-clock',
      titleKey: 'privacyPage.sections.retention.title',
      bodyKeys: [
        'privacyPage.sections.retention.body1',
        'privacyPage.sections.retention.body2'
      ]
    },
    {
      iconClass: 'pi pi-shield',
      titleKey: 'privacyPage.sections.rights.title',
      bodyKeys: [
        'privacyPage.sections.rights.body1',
        'privacyPage.sections.rights.body2'
      ]
    }
  ];

  constructor(private readonly analyticsConsentService: AnalyticsConsentService) {
  }

  protected acceptAnalytics(): void {
    this.analyticsConsentService.acceptAnalytics();
  }

  protected refuseAnalytics(): void {
    this.analyticsConsentService.revokeAnalyticsConsent();
  }

  protected resetAnalyticsChoice(): void {
    this.analyticsConsentService.resetAnalyticsChoice();
  }
}
