import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateModule } from '@ngx-translate/core';
import { from } from 'rxjs';

import { TranslationService } from '@app/services/translation.service';
import { UiChipComponent, UiKickerComponent, UiPrimitiveTone, UiSurfaceDirective } from '@ui/primitives';
import { siteVersion, type ReleaseHistoryEntry } from '../../../../../environments/version.generated';

type ReleaseHistoryLevel = 'major' | 'minor' | 'patch';
type ReleaseHistoryPatchLoader = () => Promise<readonly ReleaseHistoryEntry[]>;

interface ReleaseHistorySummaryNode {
  readonly id: string;
  readonly groupKey: string;
  readonly level: 'major' | 'minor';
  readonly entry: ReleaseHistoryEntry;
  readonly childCount: number;
  readonly children: readonly ReleaseHistorySummaryNode[];
}

interface VersionHistoryNode {
  readonly id: string;
  readonly groupKey: string;
  readonly level: ReleaseHistoryLevel;
  readonly entry: ReleaseHistoryEntry;
  readonly childCount: number;
  readonly children: readonly VersionHistoryNode[];
}

@Component({
  selector: 'app-version-history-page',
  templateUrl: './version-history-page.component.html',
  styleUrl: './version-history-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, TranslateModule, UiChipComponent, UiKickerComponent, UiSurfaceDirective]
})
export class VersionHistoryPageComponent implements OnInit {
  protected readonly siteVersion: string = siteVersion;
  protected readonly selectedLanguage = signal<string>('en');
  protected readonly nodes = signal<readonly VersionHistoryNode[]>([]);
  protected readonly historyLoading = signal<boolean>(true);
  protected readonly historyLoadFailed = signal<boolean>(false);
  protected readonly expandedNodeIds = signal<ReadonlySet<string>>(new Set<string>());
  protected readonly loadingNodeIds = signal<ReadonlySet<string>>(new Set<string>());
  protected readonly loadedChildrenByNodeId = signal<Readonly<Record<string, readonly VersionHistoryNode[]>>>({});

  private patchLoaders: Readonly<Record<string, ReleaseHistoryPatchLoader>> = {};

  constructor(
    private readonly translationService: TranslationService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnInit(): void {
    this.selectedLanguage.set(this.translationService.getCurrentLang() || 'en');
    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((language: string): void => {
      this.selectedLanguage.set(language);
    });

    this.loadHistorySummary();
  }

  protected label(entry: ReleaseHistoryEntry): string {
    const language: string = this.selectedLanguage();
    return entry.labels[language] ?? entry.labels['en'] ?? entry.version;
  }

  protected levelKey(level: ReleaseHistoryLevel): string {
    return `versionHistoryPage.levels.${level}`;
  }

  protected chipTone(level: ReleaseHistoryLevel): UiPrimitiveTone {
    if (level === 'major') {
      return 'rose';
    }

    return level === 'minor' ? 'sky' : 'lime';
  }

  protected isExpanded(node: VersionHistoryNode): boolean {
    return this.expandedNodeIds().has(node.id);
  }

  protected hasChildren(node: VersionHistoryNode): boolean {
    return node.childCount > 0;
  }

  protected isLoadingChildren(node: VersionHistoryNode): boolean {
    return this.loadingNodeIds().has(node.id);
  }

  protected loadedChildren(node: VersionHistoryNode): readonly VersionHistoryNode[] {
    return this.loadedChildrenByNodeId()[node.id] ?? [];
  }

  protected toggleNode(node: VersionHistoryNode): void {
    if (!this.hasChildren(node)) {
      return;
    }

    if (this.isExpanded(node)) {
      this.expandedNodeIds.update((current: ReadonlySet<string>) => {
        const next = new Set<string>(current);
        next.delete(node.id);
        return next;
      });
      return;
    }

    this.expandedNodeIds.update((current: ReadonlySet<string>) => {
      const next = new Set<string>(current);
      next.add(node.id);
      return next;
    });
    this.loadPatchChildren(node);
  }

  protected trackByNode(_: number, node: VersionHistoryNode): string {
    return node.id;
  }

  private loadHistorySummary(): void {
    from(Promise.all([
      import('../../../../../environments/release-history-summary.generated'),
      import('../../../../../environments/release-history-loaders.generated')
    ])).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: ([summaryModule, loadersModule]: [
        { releaseHistorySummary: readonly ReleaseHistorySummaryNode[] },
        { releaseHistoryPatchLoaders: Readonly<Record<string, ReleaseHistoryPatchLoader>> }
      ]) => {
        const nodes: VersionHistoryNode[] = summaryModule.releaseHistorySummary.map((node: ReleaseHistorySummaryNode) => this.mapSummaryNode(node));
        this.patchLoaders = loadersModule.releaseHistoryPatchLoaders;
        this.nodes.set(nodes);
        this.historyLoading.set(false);
        this.expandCurrentBranch(nodes);
      },
      error: (error: unknown) => {
        console.error('Error loading version history', error);
        this.historyLoading.set(false);
        this.historyLoadFailed.set(true);
      }
    });
  }

  private expandCurrentBranch(nodes: readonly VersionHistoryNode[]): void {
    const parts = parseVersion(siteVersion);
    const expandedIds = new Set<string>();
    const majorNode: VersionHistoryNode | undefined = nodes.find((node: VersionHistoryNode) => node.groupKey === `${parts.major}`);

    if (!majorNode) {
      this.expandedNodeIds.set(expandedIds);
      return;
    }

    expandedIds.add(majorNode.id);
    this.expandedNodeIds.set(expandedIds);
    this.loadPatchChildren(majorNode);

    if (parts.minor <= 0) {
      return;
    }

    const minorNode: VersionHistoryNode | undefined = majorNode.children.find((node: VersionHistoryNode) => node.groupKey === `${parts.major}.${parts.minor}`);
    if (!minorNode) {
      return;
    }

    expandedIds.add(minorNode.id);
    this.expandedNodeIds.set(new Set<string>(expandedIds));
    this.loadPatchChildren(minorNode);
  }

  private loadPatchChildren(node: VersionHistoryNode): void {
    const existingChildren: readonly VersionHistoryNode[] | undefined = this.loadedChildrenByNodeId()[node.id];
    if (existingChildren || this.loadingNodeIds().has(node.id)) {
      return;
    }

    const loader: ReleaseHistoryPatchLoader | undefined = this.patchLoaders[this.patchGroupKey(node)];
    if (!loader) {
      this.loadedChildrenByNodeId.update((current: Readonly<Record<string, readonly VersionHistoryNode[]>>) => ({
        ...current,
        [node.id]: []
      }));
      return;
    }

    this.loadingNodeIds.update((current: ReadonlySet<string>) => new Set<string>(current).add(node.id));
    from(loader()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (entries: readonly ReleaseHistoryEntry[]) => {
        this.loadedChildrenByNodeId.update((current: Readonly<Record<string, readonly VersionHistoryNode[]>>) => ({
          ...current,
          [node.id]: entries.map((entry: ReleaseHistoryEntry) => this.mapPatchNode(entry))
        }));
        this.removeLoadingNode(node.id);
      },
      error: (error: unknown) => {
        console.error('Error loading version history children', error);
        this.loadedChildrenByNodeId.update((current: Readonly<Record<string, readonly VersionHistoryNode[]>>) => ({
          ...current,
          [node.id]: []
        }));
        this.removeLoadingNode(node.id);
      }
    });
  }

  private removeLoadingNode(nodeId: string): void {
    this.loadingNodeIds.update((current: ReadonlySet<string>) => {
      const next = new Set<string>(current);
      next.delete(nodeId);
      return next;
    });
  }

  private patchGroupKey(node: VersionHistoryNode): string {
    return node.level === 'major' ? `${node.groupKey}.0` : node.groupKey;
  }

  private mapSummaryNode(node: ReleaseHistorySummaryNode): VersionHistoryNode {
    return {
      id: node.id,
      groupKey: node.groupKey,
      level: node.level,
      entry: node.entry,
      childCount: node.childCount,
      children: node.children.map((child: ReleaseHistorySummaryNode) => this.mapSummaryNode(child))
    };
  }

  private mapPatchNode(entry: ReleaseHistoryEntry): VersionHistoryNode {
    return {
      id: `patch-${entry.version.replace(/\./g, '-')}`,
      groupKey: entry.version,
      level: 'patch',
      entry,
      childCount: 0,
      children: []
    };
  }
}

function parseVersion(version: string): { major: number; minor: number; patch: number } {
  const parts: number[] = version.split('.').map((part: string): number => Number.parseInt(part, 10));

  return {
    major: Number.isFinite(parts[0]) ? parts[0] : 0,
    minor: Number.isFinite(parts[1]) ? parts[1] : 0,
    patch: Number.isFinite(parts[2]) ? parts[2] : 0
  };
}
