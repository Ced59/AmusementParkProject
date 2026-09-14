import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, Subscription, of, switchMap, throwError } from 'rxjs';

import { PublicHtmlSitemapNode } from '@app/models/seo/public-html-sitemap-node';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { applySsrPublicDataErrorStatus } from '@core/ssr/ssr-public-error-status';
import { PUBLIC_SITEMAP_DATA_PORT, PublicSitemapDataPort } from './public-sitemap-data.ports';
import { PUBLIC_SITEMAP_PAGE_SIZE, PublicSitemapLocation, buildPublicSitemapQuery } from './public-sitemap-location';

export interface PublicSitemapBreadcrumb {
  readonly label: string;
  readonly queryParams: Record<string, string | number>;
}

export interface PublicSitemapPageNode extends PublicHtmlSitemapNode {
  readonly branchQueryParams: Record<string, string | number> | null;
}

interface PublicSitemapBranch {
  readonly nodes: readonly PublicHtmlSitemapNode[];
  readonly breadcrumbs: readonly PublicSitemapBreadcrumb[];
}

@Injectable()
export class PublicSitemapStateFacade {
  private readonly nodesSignal = signal<readonly PublicSitemapPageNode[]>([]);
  private readonly breadcrumbsSignal = signal<readonly PublicSitemapBreadcrumb[]>([]);
  private readonly loadingSignal = signal(false);
  private readonly errorKeySignal = signal<string | null>(null);
  private readonly pageSignal = signal(1);
  private readonly pageCountSignal = signal(1);
  private loadSubscription: Subscription | null = null;

  public readonly nodes: Signal<readonly PublicSitemapPageNode[]> = this.nodesSignal.asReadonly();
  public readonly breadcrumbs: Signal<readonly PublicSitemapBreadcrumb[]> = this.breadcrumbsSignal.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();
  public readonly page: Signal<number> = this.pageSignal.asReadonly();
  public readonly pageCount: Signal<number> = this.pageCountSignal.asReadonly();

  constructor(
    @Inject(PUBLIC_SITEMAP_DATA_PORT) private readonly dataPort: PublicSitemapDataPort,
    private readonly destroyRef: DestroyRef,
    private readonly ssrHttpStatusService: SsrHttpStatusService
  ) {
  }

  loadPage(language: string, location: PublicSitemapLocation): void {
    this.loadSubscription?.unsubscribe();
    this.nodesSignal.set([]);
    this.breadcrumbsSignal.set([]);
    this.pageSignal.set(location.page);
    this.pageCountSignal.set(1);
    this.errorKeySignal.set(null);
    this.loadingSignal.set(location.isValid);

    if (!location.isValid) {
      this.setNotFound();
      return;
    }

    this.loadSubscription = this.loadBranch(language || 'en', location.nodeIds, 0, [])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (branch: PublicSitemapBranch): void => {
          const pageCount: number = Math.max(1, Math.ceil(branch.nodes.length / PUBLIC_SITEMAP_PAGE_SIZE));
          this.loadingSignal.set(false);
          if (location.page > pageCount) {
            this.setNotFound();
            return;
          }

          this.breadcrumbsSignal.set(branch.breadcrumbs);
          this.pageCountSignal.set(pageCount);
          const start: number = (location.page - 1) * PUBLIC_SITEMAP_PAGE_SIZE;
          this.nodesSignal.set(branch.nodes.slice(start, start + PUBLIC_SITEMAP_PAGE_SIZE).map((node: PublicHtmlSitemapNode): PublicSitemapPageNode => ({
            ...node,
            branchQueryParams: node.hasChildren ? buildPublicSitemapQuery([...location.nodeIds, node.id]) : null
          })));
        },
        error: (error: unknown): void => {
          this.loadingSignal.set(false);
          applySsrPublicDataErrorStatus(error, this.ssrHttpStatusService);
          this.errorKeySignal.set('sitemapPage.error');
        }
      });
  }

  private loadBranch(
    language: string,
    nodeIds: readonly string[],
    depth: number,
    breadcrumbs: readonly PublicSitemapBreadcrumb[]
  ): Observable<PublicSitemapBranch> {
    const parentNodeId: string | null = depth === 0 ? null : nodeIds[depth - 1];
    // Load one sibling collection at a time. The full snapshot is never needed.
    return this.dataPort.getNodes(language, parentNodeId, false).pipe(
      switchMap((nodes: PublicHtmlSitemapNode[]): Observable<PublicSitemapBranch> => {
        if (depth === nodeIds.length) {
          return of({ nodes, breadcrumbs });
        }

        const selectedNode: PublicHtmlSitemapNode | undefined = nodes.find((node: PublicHtmlSitemapNode): boolean => node.id === nodeIds[depth] && node.hasChildren);
        if (!selectedNode) {
          return throwError(() => ({ status: 404 }));
        }

        return this.loadBranch(language, nodeIds, depth + 1, [
          ...breadcrumbs,
          { label: selectedNode.label, queryParams: buildPublicSitemapQuery(nodeIds.slice(0, depth + 1)) }
        ]);
      })
    );
  }

  private setNotFound(): void {
    this.ssrHttpStatusService.setNotFound();
    this.errorKeySignal.set('sitemapPage.empty');
  }
}
