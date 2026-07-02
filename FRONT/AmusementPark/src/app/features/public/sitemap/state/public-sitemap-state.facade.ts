import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { PublicHtmlSitemapNode } from '@app/models/seo/public-html-sitemap-node';
import { PUBLIC_SITEMAP_DATA_PORT, PublicSitemapDataPort } from './public-sitemap-data.ports';

@Injectable()
export class PublicSitemapStateFacade {
  private readonly rootNodesSignal = signal<PublicHtmlSitemapNode[]>([]);
  private readonly childrenByNodeIdSignal = signal<Record<string, PublicHtmlSitemapNode[]>>({});
  private readonly expandedNodeIdsSignal = signal<ReadonlySet<string>>(new Set<string>());
  private readonly loadedNodeIdsSignal = signal<ReadonlySet<string>>(new Set<string>());
  private readonly loadingNodeIdsSignal = signal<ReadonlySet<string>>(new Set<string>());
  private readonly errorNodeIdsSignal = signal<ReadonlySet<string>>(new Set<string>());
  private readonly loadingSignal = signal<boolean>(false);
  private readonly errorKeySignal = signal<string | null>(null);
  private currentLanguage: string = 'en';
  private rootLoadSequence: number = 0;

  public readonly rootNodes: Signal<PublicHtmlSitemapNode[]> = this.rootNodesSignal.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();

  constructor(
    @Inject(PUBLIC_SITEMAP_DATA_PORT) private readonly dataPort: PublicSitemapDataPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  loadRoot(language: string, includeDescendants: boolean = false): void {
    const normalizedLanguage: string = language || 'en';
    const sequence: number = this.rootLoadSequence + 1;
    this.rootLoadSequence = sequence;
    this.currentLanguage = normalizedLanguage;
    this.rootNodesSignal.set([]);
    this.childrenByNodeIdSignal.set({});
    this.expandedNodeIdsSignal.set(new Set<string>());
    this.loadedNodeIdsSignal.set(new Set<string>());
    this.loadingNodeIdsSignal.set(new Set<string>());
    this.errorNodeIdsSignal.set(new Set<string>());
    this.loadingSignal.set(true);
    this.errorKeySignal.set(null);

    this.dataPort.getNodes(normalizedLanguage, null, false)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (nodes: PublicHtmlSitemapNode[]): void => {
          if (sequence !== this.rootLoadSequence) {
            return;
          }

          this.rootNodesSignal.set(nodes);
          this.applyEmbeddedChildren(nodes);
          this.loadingSignal.set(false);

          if (includeDescendants) {
            this.loadEmbeddedDescendants(normalizedLanguage, sequence);
          }
        },
        error: (error: unknown): void => {
          if (sequence !== this.rootLoadSequence) {
            return;
          }

          console.error('Error loading public sitemap root nodes', error);
          this.rootNodesSignal.set([]);
          this.loadingSignal.set(false);
          this.errorKeySignal.set('sitemapPage.error');
        }
      });
  }

  private loadEmbeddedDescendants(language: string, sequence: number): void {
    this.dataPort.getNodes(language, null, true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (nodes: PublicHtmlSitemapNode[]): void => {
          if (sequence !== this.rootLoadSequence || nodes.length === 0) {
            return;
          }

          this.rootNodesSignal.set([...this.rootNodesSignal(), ...nodes]);
          this.applyEmbeddedChildren(this.rootNodesSignal());
        },
        error: (error: unknown): void => {
          if (sequence !== this.rootLoadSequence) {
            return;
          }

          console.error('Error loading public sitemap crawl links', error);
        }
      });
  }

  toggleNode(node: PublicHtmlSitemapNode): void {
    if (!node.hasChildren) {
      return;
    }

    if (this.isExpanded(node.id)) {
      this.expandedNodeIdsSignal.update((expandedNodeIds: ReadonlySet<string>) => {
        const next: Set<string> = new Set<string>(expandedNodeIds);
        next.delete(node.id);
        return next;
      });
      return;
    }

    this.expandedNodeIdsSignal.update((expandedNodeIds: ReadonlySet<string>) => new Set<string>(expandedNodeIds).add(node.id));

    if (!this.loadedNodeIdsSignal().has(node.id) && !this.loadingNodeIdsSignal().has(node.id)) {
      this.loadChildren(node.id);
    }
  }

  childrenFor(nodeId: string): PublicHtmlSitemapNode[] {
    return this.childrenByNodeIdSignal()[nodeId] ?? [];
  }

  isExpanded(nodeId: string): boolean {
    return this.expandedNodeIdsSignal().has(nodeId);
  }

  isNodeLoading(nodeId: string): boolean {
    return this.loadingNodeIdsSignal().has(nodeId);
  }

  hasNodeError(nodeId: string): boolean {
    return this.errorNodeIdsSignal().has(nodeId);
  }

  private loadChildren(nodeId: string): void {
    const languageSnapshot: string = this.currentLanguage;
    this.loadingNodeIdsSignal.update((loadingNodeIds: ReadonlySet<string>) => new Set<string>(loadingNodeIds).add(nodeId));
    this.errorNodeIdsSignal.update((errorNodeIds: ReadonlySet<string>) => {
      const next: Set<string> = new Set<string>(errorNodeIds);
      next.delete(nodeId);
      return next;
    });

    this.dataPort.getNodes(languageSnapshot, nodeId, false)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (nodes: PublicHtmlSitemapNode[]): void => {
          if (languageSnapshot !== this.currentLanguage) {
            return;
          }

          this.childrenByNodeIdSignal.update((childrenByNodeId: Record<string, PublicHtmlSitemapNode[]>) => ({
            ...childrenByNodeId,
            [nodeId]: nodes
          }));
          this.loadedNodeIdsSignal.update((loadedNodeIds: ReadonlySet<string>) => new Set<string>(loadedNodeIds).add(nodeId));
          this.loadingNodeIdsSignal.update((loadingNodeIds: ReadonlySet<string>) => {
            const next: Set<string> = new Set<string>(loadingNodeIds);
            next.delete(nodeId);
            return next;
          });
        },
        error: (error: unknown): void => {
          if (languageSnapshot !== this.currentLanguage) {
            return;
          }

          console.error('Error loading public sitemap child nodes', error);
          this.loadingNodeIdsSignal.update((loadingNodeIds: ReadonlySet<string>) => {
            const next: Set<string> = new Set<string>(loadingNodeIds);
            next.delete(nodeId);
            return next;
          });
          this.errorNodeIdsSignal.update((errorNodeIds: ReadonlySet<string>) => new Set<string>(errorNodeIds).add(nodeId));
        }
      });
  }

  private applyEmbeddedChildren(nodes: readonly PublicHtmlSitemapNode[]): void {
    const childrenByNodeId: Record<string, PublicHtmlSitemapNode[]> = {};
    const loadedNodeIds: Set<string> = new Set<string>();
    const expandedNodeIds: Set<string> = new Set<string>();

    this.collectEmbeddedChildren(nodes, childrenByNodeId, loadedNodeIds, expandedNodeIds);

    this.childrenByNodeIdSignal.set(childrenByNodeId);
    this.loadedNodeIdsSignal.set(loadedNodeIds);
    this.expandedNodeIdsSignal.set(expandedNodeIds);
  }

  private collectEmbeddedChildren(
    nodes: readonly PublicHtmlSitemapNode[],
    childrenByNodeId: Record<string, PublicHtmlSitemapNode[]>,
    loadedNodeIds: Set<string>,
    expandedNodeIds: Set<string>
  ): void {
    for (const node of nodes) {
      if (!node.hasChildren || !Array.isArray(node.children)) {
        continue;
      }

      const children: PublicHtmlSitemapNode[] = [...node.children];
      childrenByNodeId[node.id] = children;
      loadedNodeIds.add(node.id);

      if (children.length > 0) {
        expandedNodeIds.add(node.id);
        this.collectEmbeddedChildren(children, childrenByNodeId, loadedNodeIds, expandedNodeIds);
      }
    }
  }
}
