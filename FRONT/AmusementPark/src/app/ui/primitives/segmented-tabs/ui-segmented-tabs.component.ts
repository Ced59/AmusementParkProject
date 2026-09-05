import { ChangeDetectionStrategy, Component, ElementRef, EventEmitter, Input, Output, QueryList, ViewChildren } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

export interface UiSegmentedTab {
  id: string;
  labelKey: string;
  count?: number | null;
}

@Component({
  selector: 'app-ui-segmented-tabs',
  templateUrl: './ui-segmented-tabs.component.html',
  styleUrls: ['./ui-segmented-tabs.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslateModule]
})
export class UiSegmentedTabsComponent {
  @Input() tabs: readonly UiSegmentedTab[] = [];
  @Input() activeTabId: string = '';
  @Input() ariaLabelKey: string = '';
  @Input() idPrefix: string = 'segmented-tab';
  @Input() panelId: string = 'segmented-tab-panel';

  @Output() tabSelected: EventEmitter<string> = new EventEmitter<string>();

  @ViewChildren('tabButton') private readonly tabButtons!: QueryList<ElementRef<HTMLButtonElement>>;

  selectTab(tabId: string): void {
    this.tabSelected.emit(tabId);
  }

  onKeydown(event: KeyboardEvent, currentIndex: number): void {
    let targetIndex: number | null = null;
    if (event.key === 'ArrowRight' || event.key === 'ArrowDown') {
      targetIndex = (currentIndex + 1) % this.tabs.length;
    } else if (event.key === 'ArrowLeft' || event.key === 'ArrowUp') {
      targetIndex = (currentIndex - 1 + this.tabs.length) % this.tabs.length;
    } else if (event.key === 'Home') {
      targetIndex = 0;
    } else if (event.key === 'End') {
      targetIndex = this.tabs.length - 1;
    }

    if (targetIndex === null || this.tabs.length === 0) {
      return;
    }

    event.preventDefault();
    const tab = this.tabs[targetIndex];
    this.tabSelected.emit(tab.id);
    this.tabButtons.get(targetIndex)?.nativeElement.focus();
  }

  tabElementId(tabId: string): string {
    return `${this.idPrefix}-${tabId}`;
  }
}
