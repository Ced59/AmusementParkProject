import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';

import { PassportStatisticsTableViewModel } from '../../models/passport-statistics-view.models';
import { PassportTableComponent } from './passport-table.component';

describe('PassportTableComponent', () => {
  let fixture: ComponentFixture<PassportTableComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PassportTableComponent, TranslateModule.forRoot()]
    }).compileComponents();
    fixture = TestBed.createComponent(PassportTableComponent);
    fixture.componentInstance.table = createTable();
  });

  it('renders a semantic table with translated mobile labels and an explicit action', () => {
    const navigationSelected = vi.fn();
    fixture.componentInstance.navigationSelected.subscribe(navigationSelected);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;

    expect(root.querySelector('table')).not.toBeNull();
    expect(root.querySelector('th')?.getAttribute('scope')).toBe('col');
    expect(root.querySelector('td')?.hasAttribute('data-label')).toBe(true);
    (root.querySelector('button') as HTMLButtonElement).click();
    expect(navigationSelected).toHaveBeenCalledWith(expect.objectContaining({ kind: 'visit' }));
  });

  it('stacks rows below 640px and never gives the host a viewport-breaking minimum width', () => {
    const styles: string = (PassportTableComponent as unknown as { ɵcmp: { styles: string[] } }).ɵcmp.styles.join('\n');

    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('@media (max-width: 640px)');
    expect(styles).toContain('content: attr(data-label)');
    expect(styles).toContain('grid-template-columns: minmax(7rem, 0.8fr) minmax(0, 1fr)');
    expect(styles).toContain('@media (max-width: 360px)');
  });
});

function createTable(): PassportStatisticsTableViewModel {
  return {
    id: 'visits',
    titleKey: 'passport.statistics.item.byVisitTitle',
    descriptionKey: 'passport.statistics.item.byVisitDescription',
    emptyKey: 'passport.statistics.tables.empty',
    columns: [{ key: 'date', labelKey: 'passport.statistics.columns.date' }],
    rows: [{
      id: 'visit-1',
      cells: [{ columnKey: 'date', value: '2025' }],
      navigation: { kind: 'visit', targetId: 'visit-1', labelKey: 'passport.statistics.actions.openVisit' }
    }]
  };
}
