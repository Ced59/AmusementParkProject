import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';

import { PassportRatingTimelineComponent } from './passport-rating-timeline.component';

describe('PassportRatingTimelineComponent', () => {
  let fixture: ComponentFixture<PassportRatingTimelineComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PassportRatingTimelineComponent, TranslateModule.forRoot()]
    }).compileComponents();
    fixture = TestBed.createComponent(PassportRatingTimelineComponent);
    fixture.componentInstance.titleKey = 'passport.statistics.item.timelineTitle';
    fixture.componentInstance.descriptionKey = 'passport.statistics.item.timelineDescription';
    fixture.componentInstance.points = [{
      id: 'occurrence-1', visitId: 'visit-1', dateLabel: '2025',
      ratingLabel: '4.5 / 5', positionLabel: '1024'
    }];
  });

  it('renders raw rating points as an ordered list and opens the owning visit', () => {
    const visitSelected = vi.fn();
    fixture.componentInstance.visitSelected.subscribe(visitSelected);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;

    expect(root.querySelectorAll('ol > li')).toHaveLength(1);
    expect(root.textContent).toContain('4.5 / 5');
    (root.querySelector('button') as HTMLButtonElement).click();
    expect(visitSelected).toHaveBeenCalledWith('visit-1');
  });

  it('moves visit actions below their labels on narrow viewports', () => {
    const styles: string = (
      PassportRatingTimelineComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('@media (max-width: 520px)');
    expect(styles).toContain('grid-template-columns: 1rem minmax(0, 1fr)');
  });
});
