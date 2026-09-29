import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import {
  PRODUCT_QUALITY_DASHBOARDS,
  ProductQualityDashboardDefinition,
  ProductQualityEvidenceState,
  ProductQualitySignalChannel
} from '../../models/product-quality-dashboard.models';

@Component({
  selector: 'app-admin-product-quality',
  templateUrl: './admin-product-quality.component.html',
  styleUrl: './admin-product-quality.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslateModule]
})
export class AdminProductQualityComponent {
  protected readonly dashboards: readonly ProductQualityDashboardDefinition[] =
    PRODUCT_QUALITY_DASHBOARDS;

  constructor(private readonly router: Router) {
  }

  protected buildAdminRoute(segments: readonly string[]): string[] {
    const lang: string = this.router.url.split('/')[1] || 'en';
    return ['/', lang, 'admin', ...segments];
  }

  protected moduleKey(dashboard: ProductQualityDashboardDefinition, key: string): string {
    return `admin.productQuality.modules.${dashboard.id}.${key}`;
  }

  protected stateKey(state: ProductQualityEvidenceState): string {
    return `admin.productQuality.states.${state}`;
  }

  protected channelKey(channel: ProductQualitySignalChannel): string {
    return `admin.productQuality.channels.${channel}`;
  }
}
