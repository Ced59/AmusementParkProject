import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { skip } from 'rxjs/operators';

import { PublicHtmlSitemapNode } from '@app/models/seo/public-html-sitemap-node';
import { TranslationService } from '@app/services/translation.service';
import { SsrRuntimeService } from '@core/ssr/ssr-runtime.service';
import { SeoService } from '@core/seo/seo.service';
import { findNearestLanguageActivatedRoute, resolveLanguageFromActivatedRoute, resolveLanguageFromParamMap } from '@shared/utils/routing/route-language.utils';
import { UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { PublicSitemapStateFacade } from '../state/public-sitemap-state.facade';

@Component({
  selector: 'app-public-sitemap-page',
  templateUrl: './public-sitemap-page.component.html',
  styleUrls: ['./public-sitemap-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PublicSitemapStateFacade],
  imports: [
    CommonModule,
    RouterLink,
    TranslateModule,
    UiKickerComponent,
    UiSurfaceDirective
  ]
})
export class PublicSitemapPageComponent implements OnInit {
  protected readonly currentLang = signal<string>('en');
  protected readonly rootNodes: Signal<PublicHtmlSitemapNode[]> = this.stateFacade.rootNodes;
  protected readonly loading: Signal<boolean> = this.stateFacade.loading;
  protected readonly errorKey: Signal<string | null> = this.stateFacade.errorKey;
  private activeLanguage: string | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly seoService: SeoService,
    private readonly ssrRuntimeService: SsrRuntimeService,
    private readonly stateFacade: PublicSitemapStateFacade,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnInit(): void {
    const initialLanguage: string = resolveLanguageFromActivatedRoute(this.route, this.translationService.getCurrentLang() || 'en');
    this.applyLanguage(initialLanguage);
    this.watchRouteLanguageChanges();

    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((language: string): void => {
      this.applyLanguage(language);
    });
  }

  protected toggleNode(node: PublicHtmlSitemapNode): void {
    this.stateFacade.toggleNode(node);
  }

  protected childrenFor(nodeId: string): PublicHtmlSitemapNode[] {
    return this.stateFacade.childrenFor(nodeId);
  }

  protected isExpanded(nodeId: string): boolean {
    return this.stateFacade.isExpanded(nodeId);
  }

  protected isNodeLoading(nodeId: string): boolean {
    return this.stateFacade.isNodeLoading(nodeId);
  }

  protected hasNodeError(nodeId: string): boolean {
    return this.stateFacade.hasNodeError(nodeId);
  }

  protected trackNode(_: number, node: PublicHtmlSitemapNode): string {
    return node.id;
  }

  private watchRouteLanguageChanges(): void {
    const languageRoute: ActivatedRoute | null = findNearestLanguageActivatedRoute(this.route);

    languageRoute?.paramMap.pipe(
      skip(1),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((params: ParamMap): void => {
      this.applyLanguage(resolveLanguageFromParamMap(params, this.currentLang()));
    });
  }

  private applyLanguage(language: string): void {
    if (this.activeLanguage === language) {
      return;
    }

    this.activeLanguage = language;
    this.currentLang.set(language);
    this.seoService.applyRouteDefaults(this.router.url);
    const loadDescendantsInInitialRequest: boolean = this.ssrRuntimeService.isServerSideRender();
    this.stateFacade.loadRoot(language, true, loadDescendantsInInitialRequest);
  }
}
