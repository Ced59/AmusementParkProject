import { ChangeDetectionStrategy, Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { UiButtonDirective } from '@ui/primitives';

export interface UnlocatedItemsPanelItem {
  id: string | null;
  name: string;
  categoryLabelKey: string;
  typeLabelKey: string;
  detailLink: string[] | null;
}

@Component({
  selector: 'app-unlocated-items-panel',
  templateUrl: './unlocated-items-panel.component.html',
  styleUrls: ['./unlocated-items-panel.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslateModule, UiButtonDirective]
})
export class UnlocatedItemsPanelComponent implements OnChanges {
  @Input() items: readonly UnlocatedItemsPanelItem[] = [];
  @Input() pageSize: number = 5;
  @Input() titleKey: string = 'parkItems.unlocated.title';
  @Input() subtitleKey: string = 'parkItems.unlocated.subtitle';

  protected currentPage: number = 1;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['items'] || changes['pageSize']) {
      this.currentPage = 1;
    }

    this.currentPage = Math.min(this.currentPage, this.totalPages);
    this.currentPage = Math.max(this.currentPage, 1);
  }

  protected get totalItems(): number {
    return this.items.length;
  }

  protected get normalizedPageSize(): number {
    return Math.max(Math.floor(this.pageSize), 1);
  }

  protected get totalPages(): number {
    return Math.max(Math.ceil(this.totalItems / this.normalizedPageSize), 1);
  }

  protected get pagedItems(): readonly UnlocatedItemsPanelItem[] {
    const startIndex: number = (this.currentPage - 1) * this.normalizedPageSize;
    return this.items.slice(startIndex, startIndex + this.normalizedPageSize);
  }

  protected get hasPagination(): boolean {
    return this.totalItems > this.normalizedPageSize;
  }

  protected get pageLabel(): string {
    return `${this.currentPage} / ${this.totalPages}`;
  }

  protected previousPage(): void {
    this.currentPage = Math.max(this.currentPage - 1, 1);
  }

  protected nextPage(): void {
    this.currentPage = Math.min(this.currentPage + 1, this.totalPages);
  }
}
