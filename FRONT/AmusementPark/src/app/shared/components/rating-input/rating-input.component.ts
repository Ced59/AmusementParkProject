import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-rating-input',
  templateUrl: './rating-input.component.html',
  styleUrl: './rating-input.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RatingInputComponent {
  @Input() label: string = '';
  @Input() value: number | null = null;
  @Input() disabled: boolean = false;
  @Input() showClear: boolean = false;
  @Input() locale: string = 'en';
  @Input() emptyLabel: string = '';
  @Input() clearLabel: string = '';
  @Input() decreaseLabel: string = '';
  @Input() increaseLabel: string = '';
  @Input() outOfLabel: string = '';
  @Input() scaleHint: string = '';
  @Input() describedBy: string | null = null;

  @Output() valueChange: EventEmitter<number | null> = new EventEmitter<number | null>();

  protected readonly minimum: number = 0.5;
  protected readonly maximum: number = 5;
  protected readonly step: number = 0.5;

  private formatterLocale: string = '';
  private integerFormatter: Intl.NumberFormat | null = null;
  private decimalFormatter: Intl.NumberFormat | null = null;

  protected rangeValue(): number {
    return this.value ?? this.minimum;
  }

  protected progressPercent(): number {
    if (this.value === null) {
      return 0;
    }

    return ((this.value - this.minimum) / (this.maximum - this.minimum)) * 100;
  }

  protected formattedValue(value: number): string {
    const locale: string = this.locale.trim() || 'en';
    if (this.formatterLocale !== locale || this.integerFormatter === null || this.decimalFormatter === null) {
      this.formatterLocale = locale;
      this.integerFormatter = new Intl.NumberFormat(locale, { maximumFractionDigits: 0 });
      this.decimalFormatter = new Intl.NumberFormat(locale, {
        minimumFractionDigits: 1,
        maximumFractionDigits: 1
      });
    }

    return (value % 1 === 0 ? this.integerFormatter : this.decimalFormatter).format(value);
  }

  protected ariaValueText(): string {
    return this.value === null
      ? this.emptyLabel
      : `${this.formattedValue(this.value)} ${this.outOfLabel} ${this.formattedValue(this.maximum)}`;
  }

  protected canDecrease(): boolean {
    return this.value !== null && this.value > this.minimum;
  }

  protected canIncrease(): boolean {
    return this.value === null || this.value < this.maximum;
  }

  protected selectRangeValue(event: Event): void {
    const input: HTMLInputElement = event.target as HTMLInputElement;
    const value: number = Number(input.value);
    if (Number.isFinite(value)) {
      this.emitNormalized(value);
    }
  }

  protected decrease(): void {
    if (!this.canDecrease() || this.value === null) {
      return;
    }

    this.emitNormalized(this.value - this.step);
  }

  protected increase(): void {
    if (!this.canIncrease()) {
      return;
    }

    this.emitNormalized((this.value ?? (this.minimum - this.step)) + this.step);
  }

  protected clear(): void {
    if (!this.disabled && this.showClear && this.value !== null) {
      this.valueChange.emit(null);
    }
  }

  private emitNormalized(value: number): void {
    const bounded: number = Math.min(this.maximum, Math.max(this.minimum, value));
    const stepCount: number = Math.round((bounded - this.minimum) / this.step);
    const normalized: number = Math.round((this.minimum + (stepCount * this.step)) * 10) / 10;
    if (!this.disabled && normalized !== this.value) {
      this.valueChange.emit(normalized);
    }
  }
}
