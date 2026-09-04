import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';

import { PassportStatCardComponent } from './passport-stat-card.component';

describe('PassportStatCardComponent', () => {
  it('renders a value and its explicit denominator detail', async () => {
    await TestBed.configureTestingModule({
      imports: [PassportStatCardComponent, TranslateModule.forRoot()]
    }).compileComponents();
    const fixture: ComponentFixture<PassportStatCardComponent> = TestBed.createComponent(PassportStatCardComponent);
    fixture.componentInstance.card = {
      id: 'coverage', iconClass: 'pi pi-check', labelKey: 'passport.statistics.cards.ratedRides', value: '67%',
      detailKey: 'passport.statistics.cards.coverageDetail', detailParams: { rated: 2, total: 3 }
    };
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('strong')?.textContent).toContain('67%');
    expect(fixture.nativeElement.querySelector('small')).not.toBeNull();
  });

  it('lets long localized labels shrink and wrap inside the card', () => {
    const styles: string = (
      PassportStatCardComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('overflow-wrap: anywhere');
  });
});
