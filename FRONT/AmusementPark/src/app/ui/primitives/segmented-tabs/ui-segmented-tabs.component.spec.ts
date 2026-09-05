import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';

import { UiSegmentedTabsComponent } from './ui-segmented-tabs.component';

describe('UiSegmentedTabsComponent', () => {
  let fixture: ComponentFixture<UiSegmentedTabsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UiSegmentedTabsComponent, TranslateModule.forRoot()]
    }).compileComponents();

    fixture = TestBed.createComponent(UiSegmentedTabsComponent);
    fixture.componentInstance.tabs = [
      { id: 'first', labelKey: 'first' },
      { id: 'second', labelKey: 'second' }
    ];
    fixture.componentInstance.activeTabId = 'first';
    fixture.detectChanges();
  });

  it('exposes the active tab and panel relationship', () => {
    const buttons: NodeListOf<HTMLButtonElement> = fixture.nativeElement.querySelectorAll('[role="tab"]');

    expect(buttons.item(0).getAttribute('aria-selected')).toBe('true');
    expect(buttons.item(0).getAttribute('tabindex')).toBe('0');
    expect(buttons.item(1).getAttribute('tabindex')).toBe('-1');
  });

  it('selects and focuses the next tab with the keyboard', () => {
    let selectedId: string = '';
    fixture.componentInstance.tabSelected.subscribe((tabId: string) => {
      selectedId = tabId;
    });
    const buttons: NodeListOf<HTMLButtonElement> = fixture.nativeElement.querySelectorAll('[role="tab"]');
    const event: KeyboardEvent = new KeyboardEvent('keydown', {
      key: 'ArrowRight',
      bubbles: true,
      cancelable: true
    });

    buttons.item(0).dispatchEvent(event);

    expect(selectedId).toBe('second');
    expect(document.activeElement).toBe(buttons.item(1));
    expect(event.defaultPrevented).toBe(true);
  });
});
