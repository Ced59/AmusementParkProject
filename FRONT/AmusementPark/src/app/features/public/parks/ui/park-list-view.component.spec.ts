import { ParkListViewComponent } from './park-list-view.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';

describe('Park list pagination layout', () => {
  it('lets the localized trail wrap and keeps existing pagination controls bounded on mobile', () => {
    const trail = (ParkListViewComponent as unknown as { ɵcmp: { styles: string[] } }).ɵcmp.styles.join('\n');
    const pagination = (PaginationComponent as unknown as { ɵcmp: { styles: string[] } }).ɵcmp.styles.join('\n');
    expect(trail).toContain('.parks-pagination-trail');
    expect(trail).toContain('flex-wrap: wrap');
    expect(trail).toContain('overflow-wrap: anywhere');
    expect(trail).toContain('min-width: 0');
    expect(trail).toContain('max-width: 100%');
    expect(pagination).toContain('flex-wrap: wrap');
    expect(pagination).toContain('max-width: 680px');
    expect(pagination).toContain('max-width: 100%');
  });
});
