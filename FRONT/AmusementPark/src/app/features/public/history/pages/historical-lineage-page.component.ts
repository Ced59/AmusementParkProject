import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, effect, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Data, Router, RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import {
  PublicHistoricalLineage,
  PublicHistoricalLineageRelation,
  PublicHistoricalLineageSubject
} from '@app/models/history/public-park-history.models';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import {
  buildPublicHistoricalLineageRouteCommands,
  buildPublicParkHistoryRouteCommands,
  buildPublicParkRouteCommands,
  buildPublicRoutePath
} from '@shared/utils/routing/public-detail-route.helpers';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { HISTORICAL_LINEAGE_ROUTE_DATA_KEY } from '../state/historical-lineage.resolver';
import { HistoricalLineageBreadcrumbSeoService } from '../state/historical-lineage-breadcrumb-seo.service';
import { formatPublicHistoricalPeriod } from '../utils/historical-period-label';

@Component({
  selector: 'app-historical-lineage-page',
  templateUrl: './historical-lineage-page.component.html',
  styleUrls: ['./historical-lineage-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslateModule]
})
export class HistoricalLineagePageComponent implements OnInit {
  protected readonly lineage = signal<PublicHistoricalLineage | null>(null);
  protected readonly currentLanguage = signal<string>('en');

  private readonly subjectsByKey = signal<ReadonlyMap<string, PublicHistoricalLineageSubject>>(new Map());

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly translateService: TranslateService,
    private readonly seoService: SeoService,
    private readonly breadcrumbSeoService: HistoricalLineageBreadcrumbSeoService,
    private readonly destroyRef: DestroyRef
  ) {
    effect((): void => {
      const currentLineage: PublicHistoricalLineage | null = this.lineage();
      if (currentLineage) {
        this.applySeo(currentLineage);
      }
    });
  }

  ngOnInit(): void {
    this.currentLanguage.set(resolveLanguageFromActivatedRoute(
      this.route,
      this.translationService.getCurrentLang() || 'en'
    ));
    this.route.data
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((data: Data): void => {
        const lineage: PublicHistoricalLineage | null = data[HISTORICAL_LINEAGE_ROUTE_DATA_KEY] ?? null;
        this.lineage.set(lineage);
        this.subjectsByKey.set(new Map(
          (lineage?.subjects ?? []).map((subject: PublicHistoricalLineageSubject): [string, PublicHistoricalLineageSubject] => [subject.key, subject])
        ));
        if (!lineage) {
          this.seoService.applyNotFoundSeo(this.currentLanguage(), this.router.url);
        }
      });
    this.translationService.languageChanged
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((language: string): void => {
        this.currentLanguage.set(language);
        if (!this.lineage()) {
          this.seoService.applyNotFoundSeo(language, this.router.url);
        }
      });
  }

  protected subject(key: string): PublicHistoricalLineageSubject | null {
    return this.subjectsByKey().get(key) ?? null;
  }

  protected relationType(relation: PublicHistoricalLineageRelation): string {
    return this.enumLabel('relationTypes', relation.type);
  }

  protected evidence(relation: PublicHistoricalLineageRelation): string {
    return this.enumLabel('evidence', relation.evidenceState);
  }

  protected period(relation: PublicHistoricalLineageRelation): string {
    return formatPublicHistoricalPeriod(
      relation.period,
      this.currentLanguage(),
      (key: string, parameters?: Record<string, string>): string => this.translateService.instant(key, parameters)
    );
  }

  protected uncertainty(relation: PublicHistoricalLineageRelation): string | null {
    const language: string = this.currentLanguage();
    return relation.uncertaintyExplanations.find((text) => text.languageCode === language)?.value
      ?? relation.uncertaintyExplanations.find((text) => text.languageCode === 'en')?.value
      ?? relation.uncertaintyExplanations[0]?.value
      ?? null;
  }

  protected relationKey(relation: PublicHistoricalLineageRelation, index: number): string {
    return `${relation.sourceKey}:${relation.targetKey}:${relation.type}:${index}`;
  }

  protected contextParkLink(lineage: PublicHistoricalLineage): string[] | null {
    return lineage.contextPark
      ? buildPublicParkRouteCommands({
        language: this.currentLanguage(),
        parkId: lineage.contextPark.id,
        parkName: lineage.contextPark.name
      })
      : null;
  }

  protected contextHistoryLink(lineage: PublicHistoricalLineage): string[] | null {
    return lineage.contextPark
      ? buildPublicParkHistoryRouteCommands({
        language: this.currentLanguage(),
        parkId: lineage.contextPark.id,
        parkName: lineage.contextPark.name
      })
      : null;
  }

  private enumLabel(group: string, value: string): string {
    const key: string = `history.lineage.${group}.${value}`;
    const translated: string = this.translateService.instant(key);
    return translated === key ? value : translated;
  }

  private applySeo(lineage: PublicHistoricalLineage): void {
    const subjectType: string = this.route.snapshot.paramMap.get('subjectType')?.trim() ?? lineage.root.type;
    const subjectId: string = this.route.snapshot.paramMap.get('subjectId')?.trim() ?? '';
    const canonicalCommands: string[] = buildPublicHistoricalLineageRouteCommands({
      language: this.currentLanguage(),
      subjectType,
      subjectId,
      subjectLabel: lineage.root.label
    }) ?? [];
    const canonicalPath: string = buildPublicRoutePath(canonicalCommands) ?? this.router.url;
    const title: string = this.translateService.instant('history.lineage.seoTitle', { subject: lineage.root.label });
    const description: string = this.translateService.instant('history.lineage.seoDescription', { subject: lineage.root.label });
    this.seoService.applyHistoricalLineageSeo(title, description, this.router.url, canonicalPath);
    const parkPath: string | null = buildPublicRoutePath(this.contextParkLink(lineage));
    const historyPath: string | null = buildPublicRoutePath(this.contextHistoryLink(lineage));
    this.breadcrumbSeoService.apply(
      this.currentLanguage(),
      title,
      canonicalPath,
      lineage.contextPark && parkPath && historyPath
        ? {
          parkName: lineage.contextPark.name,
          parkPath,
          historyPath
        }
        : null
    );
  }
}
