import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonDirective } from '@shared/ui/primitives/button';
import { Card } from '@shared/ui/primitives/card';
import { InputText } from '@shared/ui/primitives/inputtext';
import { UiTemplate } from '@shared/ui/primitives/api';
import { Tag } from '@shared/ui/primitives/tag';

import {
  SocialShareDailyStatsPoint,
  SocialShareDimensionCount,
  SocialShareStatsQuery,
  SocialShareTopTarget
} from '@app/models/social-share/social-share.models';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { AdminSocialShareStatsFacade } from '../../state/admin-social-share-stats.facade';

interface AdminSocialShareFiltersForm {
  readonly fromUtc: FormControl<string>;
  readonly toUtc: FormControl<string>;
}

interface AdminSocialShareChartPoint extends SocialShareDailyStatsPoint {
  readonly heightPercent: number;
}

@Component({
  selector: 'app-admin-social-share-stats',
  templateUrl: './admin-social-share-stats.component.html',
  styleUrl: './admin-social-share-stats.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminSocialShareStatsFacade],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TranslateModule,
    ButtonDirective,
    Card,
    InputText,
    UiTemplate,
    Tag,
    PageStateComponent
  ]
})
export class AdminSocialShareStatsComponent implements OnInit {
  protected readonly state = this.facade.state;
  protected readonly loading = this.facade.loading;
  protected readonly stats = this.facade.stats;
  protected readonly totalEvents = this.facade.totalEvents;
  protected readonly anonymousEvents = this.facade.anonymousEvents;
  protected readonly authenticatedEvents = this.facade.authenticatedEvents;
  protected readonly channels = this.facade.channels;
  protected readonly targetTypes = this.facade.targetTypes;
  protected readonly visitorKinds = this.facade.visitorKinds;
  protected readonly topTargets = this.facade.topTargets;
  protected readonly canShowHeaderTotal = computed(() => !this.loading());
  protected readonly chartPoints = computed<AdminSocialShareChartPoint[]>(() => this.buildChartPoints(this.stats()?.daily ?? []));

  protected readonly filtersForm = new FormGroup<AdminSocialShareFiltersForm>({
    fromUtc: new FormControl<string>('', { nonNullable: true }),
    toUtc: new FormControl<string>('', { nonNullable: true })
  });

  constructor(private readonly facade: AdminSocialShareStatsFacade) {
  }

  ngOnInit(): void {
    this.facade.load();
  }

  protected applyFilters(): void {
    this.facade.load(this.toQueryFilters());
  }

  protected resetFilters(): void {
    this.filtersForm.reset();
    this.facade.load({ fromUtc: null, toUtc: null });
  }

  protected dimensionLabel(prefix: string, item: SocialShareDimensionCount): string {
    return `${prefix}.${item.key}`;
  }

  protected targetLabel(target: SocialShareTopTarget): string {
    const title: string = target.targetTitle?.trim() ?? '';

    if (title.length > 0) {
      return title;
    }

    return target.url;
  }

  protected targetTypeLabel(target: SocialShareTopTarget): string {
    return `admin.socialShare.targets.${target.targetType}`;
  }

  protected trackByDimensionKey(_: number, item: SocialShareDimensionCount): string {
    return item.key;
  }

  protected trackByTarget(_: number, target: SocialShareTopTarget): string {
    return `${target.targetType}-${target.targetId ?? target.url}`;
  }

  private toQueryFilters(): SocialShareStatsQuery {
    const value = this.filtersForm.getRawValue();
    return {
      fromUtc: this.toUtcIso(value.fromUtc),
      toUtc: this.toUtcIso(value.toUtc)
    };
  }

  private toUtcIso(value: string): string | null {
    const normalizedValue: string = value?.trim() ?? '';

    if (normalizedValue.length === 0) {
      return null;
    }

    const date: Date = new Date(normalizedValue);

    if (Number.isNaN(date.getTime())) {
      return null;
    }

    return date.toISOString();
  }

  private buildChartPoints(points: SocialShareDailyStatsPoint[]): AdminSocialShareChartPoint[] {
    const maxCount: number = Math.max(1, ...points.map((point: SocialShareDailyStatsPoint) => point.count));

    return points.map((point: SocialShareDailyStatsPoint) => ({
      ...point,
      heightPercent: Math.max(6, Math.round((point.count / maxCount) * 100))
    }));
  }
}
