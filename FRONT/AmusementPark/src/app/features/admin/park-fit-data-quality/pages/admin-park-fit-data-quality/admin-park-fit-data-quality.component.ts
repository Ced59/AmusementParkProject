import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import {
  ParkFitDataQualityIssue,
  ParkFitDataQualityStatus
} from '@app/models/admin/park-fit/park-fit-data-quality.models';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { UiTemplate } from '@shared/ui/primitives/api';
import { ButtonDirective } from '@shared/ui/primitives/button';
import { Card } from '@shared/ui/primitives/card';
import { Tag } from '@shared/ui/primitives/tag';
import { AdminParkFitDataQualityFacade } from '../../state/admin-park-fit-data-quality.facade';

@Component({
  selector: 'app-admin-park-fit-data-quality',
  templateUrl: './admin-park-fit-data-quality.component.html',
  styleUrl: './admin-park-fit-data-quality.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminParkFitDataQualityFacade],
  imports: [
    CommonModule,
    RouterLink,
    TranslateModule,
    ButtonDirective,
    Card,
    UiTemplate,
    Tag,
    PageStateComponent
  ]
})
export class AdminParkFitDataQualityComponent implements OnInit {
  protected readonly state = this.facade.state;
  protected readonly loading = this.facade.loading;
  protected readonly assessments = this.facade.assessments;
  protected readonly pagination = this.facade.pagination;
  protected readonly eligibleCount = this.facade.eligibleCount;
  protected readonly actionRequiredCount = this.facade.actionRequiredCount;
  protected readonly averageCoveragePercent = this.facade.averageCoveragePercent;

  constructor(
    private readonly facade: AdminParkFitDataQualityFacade,
    private readonly router: Router
  ) {
  }

  ngOnInit(): void {
    this.facade.load();
  }

  protected previousPage(): void {
    this.facade.previousPage();
  }

  protected refresh(): void {
    this.facade.load(this.pagination().currentPage);
  }

  protected nextPage(): void {
    this.facade.nextPage();
  }

  protected statusLabelKey(status: ParkFitDataQualityStatus): string {
    return `admin.parkFitDataQuality.status.${status}`;
  }

  protected issueLabelKey(issue: ParkFitDataQualityIssue): string {
    return `admin.parkFitDataQuality.issue.${issue}`;
  }

  protected statusSeverity(
    status: ParkFitDataQualityStatus
  ): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    if (status === 'EligibleForFitComparison') {
      return 'success';
    }

    if (status === 'EligibleForDiscoveryOnly') {
      return 'info';
    }

    if (status === 'TemporarilyStale') {
      return 'warn';
    }

    if (status === 'Insufficient' || status === 'Suspended') {
      return 'danger';
    }

    return 'secondary';
  }

  protected parkItemsRoute(parkId: string): string[] {
    return ['/', this.currentLang, 'admin', 'parks', 'edit', parkId, 'items'];
  }

  protected parkItemRoute(parkId: string, parkItemId: string): string[] {
    return ['/', this.currentLang, 'admin', 'parks', 'edit', parkId, 'items', parkItemId];
  }

  private get currentLang(): string {
    return this.router.url.split('/')[1] || 'fr';
  }
}
