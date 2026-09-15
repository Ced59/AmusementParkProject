import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { optimizeHtmlForRobotNoJs } from '@core/ssr/robot-html-optimizer';
import { Paginator } from './paginator';

describe('Paginator optional public links', () => {
  it('keeps existing button-only consumers unchanged', () => {
    const fixture = TestBed.createComponent(Paginator);
    fixture.componentRef.setInput('totalRecords', 30);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('a')).toHaveLength(0);
    const emit = vi.spyOn(fixture.componentInstance.onPageChange, 'emit');
    fixture.nativeElement.querySelectorAll('.p-paginator-page')[1].click();
    expect(emit).toHaveBeenCalledWith({ page: 1, first: 10, rows: 10, pageCount: 3 });
  });

  it('renders localized hrefs that survive script/style stripping', () => {
    const fixture = createLinkedPaginator();
    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('a[rel="prev"]')?.getAttribute('href')).toBe('/fr/parks');
    expect(element.querySelector('a[rel="next"]')?.getAttribute('href')).toBe('/fr/parks?page=3');
    expect(element.querySelector('a[aria-current="page"]')?.getAttribute('href')).toBe('/fr/parks?page=2');
    expect(element.querySelector('a[rel="next"]')?.textContent).toContain('Page suivante');
    const stripped = new DOMParser().parseFromString(optimizeHtmlForRobotNoJs(element.outerHTML).html, 'text/html');
    expect(stripped.querySelector('a[href="/fr/parks?page=3"]')).not.toBeNull();
    expect(stripped.body.textContent).toContain('Page suivante');
  });

  it('emits exactly one normal navigation while preserving modified clicks', () => {
    const fixture = createLinkedPaginator();
    const emit = vi.spyOn(fixture.componentInstance.onPageChange, 'emit');
    const link = fixture.debugElement.query(By.css('a[rel="next"]'));
    const normal = new MouseEvent('click', { button: 0, cancelable: true });
    link.triggerEventHandler('click', normal);
    expect(normal.defaultPrevented).toBe(true);
    expect(emit).toHaveBeenCalledTimes(1);
    expect(emit).toHaveBeenCalledWith({ page: 2, first: 18, rows: 9, pageCount: 3 });
    emit.mockClear();
    for (const options of [{ ctrlKey: true }, { metaKey: true }, { shiftKey: true }, { button: 1 }]) {
      const modified = new MouseEvent('click', { ...options, cancelable: true });
      link.triggerEventHandler('click', modified);
      expect(modified.defaultPrevented).toBe(false);
    }
    expect(emit).not.toHaveBeenCalled();
  });

  it('does not link past either boundary and preserves row-size changes', () => {
    const fixture = createLinkedPaginator();
    fixture.componentRef.setInput('first', 0);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('a[rel="prev"]')).toBeNull();
    fixture.componentRef.setInput('first', 18);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('a[rel="next"]')).toBeNull();
    const emit = vi.spyOn(fixture.componentInstance.onPageChange, 'emit');
    fixture.componentInstance.changeRows(18);
    expect(emit).toHaveBeenCalledWith(expect.objectContaining({ page: 0, rows: 18, first: 0 }));
  });
});

function createLinkedPaginator() {
  const fixture = TestBed.createComponent(Paginator);
  fixture.componentRef.setInput('rows', 9);
  fixture.componentRef.setInput('first', 9);
  fixture.componentRef.setInput('totalRecords', 20);
  fixture.componentRef.setInput('pageLanguage', 'fr');
  fixture.componentRef.setInput('pageHref', (page: number) => `/fr/parks${page > 0 ? `?page=${page + 1}` : ''}`);
  fixture.detectChanges();
  return fixture;
}
