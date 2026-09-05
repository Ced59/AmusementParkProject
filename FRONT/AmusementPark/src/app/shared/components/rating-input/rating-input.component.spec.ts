import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RatingInputComponent } from './rating-input.component';

describe('RatingInputComponent', () => {
  let fixture: ComponentFixture<RatingInputComponent>;
  let component: RatingInputComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [RatingInputComponent] }).compileComponents();
    fixture = TestBed.createComponent(RatingInputComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('label', 'Note du tour');
    fixture.componentRef.setInput('emptyLabel', 'Non noté');
    fixture.componentRef.setInput('clearLabel', 'Effacer la sélection');
    fixture.componentRef.setInput('decreaseLabel', 'Diminuer la note');
    fixture.componentRef.setInput('increaseLabel', 'Augmenter la note');
    fixture.componentRef.setInput('outOfLabel', 'sur');
    fixture.componentRef.setInput('scaleHint', 'De 0,5 à 5, par demi-point.');
    fixture.componentRef.setInput('locale', 'fr');
    fixture.detectChanges();
  });

  it('makes the unrated state and the full scale explicit', () => {
    const element: HTMLElement = fixture.nativeElement as HTMLElement;
    const range: HTMLInputElement | null = element.querySelector('input[type="range"]');

    expect(element.textContent).toContain('Non noté');
    expect(element.textContent).toContain('De 0,5 à 5, par demi-point.');
    expect(range?.min).toBe('0.5');
    expect(range?.max).toBe('5');
    expect(range?.step).toBe('0.5');
    expect(range?.getAttribute('aria-valuetext')).toBe('Non noté');
  });

  it('emits the selected half-point value and exposes it out of five', () => {
    const values: Array<number | null> = [];
    component.valueChange.subscribe((value: number | null): void => {
      values.push(value);
    });
    const range: HTMLInputElement = (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLInputElement>('input[type="range"]')!;
    range.value = '4.5';

    range.dispatchEvent(new Event('input'));
    fixture.componentRef.setInput('value', 4.5);
    fixture.detectChanges();

    expect(values).toEqual([4.5]);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('4,5');
    expect(range.getAttribute('aria-valuetext')).toBe('4,5 sur 5');
  });

  it('supports keyboard-sized step actions and clearing a new selection', () => {
    const values: Array<number | null> = [];
    component.valueChange.subscribe((value: number | null): void => {
      values.push(value);
    });
    fixture.componentRef.setInput('value', 2.5);
    fixture.componentRef.setInput('showClear', true);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement as HTMLElement;
    const decrease: HTMLButtonElement = element.querySelector<HTMLButtonElement>(
      'button[aria-label="Diminuer la note"]')!;
    const increase: HTMLButtonElement = element.querySelector<HTMLButtonElement>(
      'button[aria-label="Augmenter la note"]')!;
    const clear: HTMLButtonElement = element.querySelector<HTMLButtonElement>('.app-rating-input__clear')!;

    decrease.click();
    increase.click();
    clear.click();

    expect(values).toEqual([2, 3, null]);
  });

  it('disables every mutation control in read-only mode', () => {
    fixture.componentRef.setInput('value', 4);
    fixture.componentRef.setInput('showClear', true);
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('fieldset')?.hasAttribute('disabled')).toBe(true);
    expect(element.querySelectorAll('button')).toHaveLength(2);
    expect(Array.from(element.querySelectorAll('button')).every((button: HTMLButtonElement) => button.disabled)).toBe(true);
  });

  it('keeps the control inside narrow mobile viewports', () => {
    const styles: string = (
      RatingInputComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('grid-template-columns: 2.75rem minmax(0, 1fr) 2.75rem');
    expect(styles).toContain('touch-action: pan-y');
    expect(styles).toContain('@media (max-width: 360px)');
    expect(styles).toContain('overflow-wrap: anywhere');
  });
});
