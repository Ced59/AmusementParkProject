import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateModule } from '@ngx-translate/core';

import { TranslationService } from '@app/services/translation.service';
import { UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { releaseHistory, siteVersion } from '../../../../../environments/version.generated';

interface ReleaseHistoryEntry {
  readonly version: string;
  readonly releasedAt: string;
  readonly labels: Record<string, string>;
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
  protected readonly entries: ReleaseHistoryEntry[] = [...releaseHistory]
    .sort((left: ReleaseHistoryEntry, right: ReleaseHistoryEntry): number => this.compareEntries(right, left));

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
  }

  protected label(entry: ReleaseHistoryEntry): string {
    const language: string = this.selectedLanguage();
    return entry.labels[language] ?? entry.labels['en'] ?? entry.version;
  }

  protected level(entry: ReleaseHistoryEntry): 'major' | 'minor' | 'patch' {
    const parts: number[] = entry.version.split('.').map((part: string): number => Number.parseInt(part, 10));
    const minorPart: number = parts[1] ?? Number.NaN;
    const patchPart: number = parts[2] ?? Number.NaN;
    const minor: number = Number.isFinite(minorPart) ? minorPart : 0;
    const patch: number = Number.isFinite(patchPart) ? patchPart : 0;

    if (minor === 0 && patch === 0) {
      return 'major';
    }

    return patch === 0 ? 'minor' : 'patch';
  }

  protected levelKey(entry: ReleaseHistoryEntry): string {
    return `versionHistoryPage.levels.${this.level(entry)}`;
  }

  protected trackByVersion(_: number, entry: ReleaseHistoryEntry): string {
    return entry.version;
  }

  private compareEntries(left: ReleaseHistoryEntry, right: ReleaseHistoryEntry): number {
    const dateComparison: number = left.releasedAt.localeCompare(right.releasedAt);
    if (dateComparison !== 0) {
      return dateComparison;
    }

    return this.compareVersions(left.version, right.version);
  }

  private compareVersions(left: string, right: string): number {
    const leftParts: number[] = left.split('.').map((part: string): number => Number.parseInt(part, 10));
    const rightParts: number[] = right.split('.').map((part: string): number => Number.parseInt(part, 10));

    for (let index: number = 0; index < 3; index++) {
      const leftPart: number = leftParts[index] ?? Number.NaN;
      const rightPart: number = rightParts[index] ?? Number.NaN;
      const leftValue: number = Number.isFinite(leftPart) ? leftPart : 0;
      const rightValue: number = Number.isFinite(rightPart) ? rightPart : 0;
      if (leftValue !== rightValue) {
        return leftValue - rightValue;
      }
    }

    return 0;
  }
}
