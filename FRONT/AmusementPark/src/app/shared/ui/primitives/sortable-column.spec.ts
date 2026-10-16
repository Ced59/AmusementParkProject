import { TestBed } from '@angular/core/testing';
import { SortableTableFixtureComponent } from '../../../../testing/fixtures/sortable-table-fixture.component';

describe('SortableColumn keyboard presentation', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [SortableTableFixtureComponent] }).compileComponents();
  });

  it('uses the same ascending/descending sort and server-page outputs for Enter, Space and mouse', () => {
    const fixture = TestBed.createComponent(SortableTableFixtureComponent);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    const header = root.querySelector<HTMLTableCellElement>('th[appUiSortableColumn]')!;
    expect(header.tagName).toBe('TH');
    expect(header.hasAttribute('role')).toBe(false);
    expect(header.tabIndex).toBe(0);
    header.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
    header.dispatchEvent(new KeyboardEvent('keydown', { key: ' ', bubbles: true, cancelable: true }));
    header.click();
    expect(fixture.componentInstance.sortEvents).toEqual([
      { field: 'name', order: 1 }, { field: 'name', order: -1 }, { field: 'name', order: 1 },
    ]);
    expect(fixture.componentInstance.lazyEvents).toEqual([1, -1, 1].map(order => ({ first: 0, rows: 10, sortField: 'name', sortOrder: order })));
    expect(root.querySelectorAll('th')[1].tabIndex).toBe(-1);
  });

  it('ignores other keys and keyboard events originating from a nested input', () => {
    const fixture = TestBed.createComponent(SortableTableFixtureComponent);
    fixture.detectChanges();
    const header = fixture.nativeElement.querySelector('th[appUiSortableColumn]') as HTMLTableCellElement;
    header.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true, cancelable: true }));
    const input = document.createElement('input');
    input.type = 'checkbox';
    header.appendChild(input);
    const event = new KeyboardEvent('keydown', { key: ' ', bubbles: true, cancelable: true });
    input.dispatchEvent(event);
    expect(event.defaultPrevented).toBe(false);
    expect(fixture.componentInstance.sortEvents).toEqual([]);
    expect(fixture.componentInstance.lazyEvents).toEqual([]);
  });
});
