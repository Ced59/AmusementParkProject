import { ChangeDetectionStrategy, Component, Input, forwardRef } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, FormsModule } from '@angular/forms';

import { LANGUAGES } from '@shared/models/localization';
import { LocalizedItem } from '@app/models/shared/localized-item';
import { Bind } from 'primeng/bind';
import { Tabs, TabList, Tab, TabPanels, TabPanel } from 'primeng/tabs';
import { Ripple } from 'primeng/ripple';
import { InputText } from 'primeng/inputtext';
import { ButtonDirective } from 'primeng/button';
import { TranslateModule } from '@ngx-translate/core';

interface LocalizedTextEntry {
  languageCode: string;
  languageLabel: string;
  value: string;
}

@Component({
    selector: 'app-localized-text-input',
    templateUrl: './localized-text-input.component.html',
    styleUrls: ['./localized-text-input.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => LocalizedTextInputComponent),
            multi: true
        }
    ],
    imports: [Bind, Tabs, TabList, Ripple, Tab, TabPanels, TabPanel, FormsModule, InputText, ButtonDirective, TranslateModule]
})
export class LocalizedTextInputComponent implements ControlValueAccessor {
  @Input() placeholderKey: string = 'admin.parks.zones.namePlaceholder';
  @Input() copyAllButtonLabel: string = 'Appliquer à toutes les langues';

  activeTabIndex: number = 0;
  entries: LocalizedTextEntry[] = this.buildEntries([]);
  isDisabled: boolean = false;

  private onChange: (value: LocalizedItem<string>[]) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(value: LocalizedItem<string>[] | null): void {
    this.entries = this.buildEntries(value ?? []);
  }

  registerOnChange(fn: (value: LocalizedItem<string>[]) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.isDisabled = isDisabled;
  }

  trackByLanguageCode(index: number, entry: LocalizedTextEntry): string {
    return entry.languageCode;
  }

  onEntryValueChange(): void {
    this.propagateChanges();
    this.onTouched();
  }

  canApplyValueToAllLanguages(value: string): boolean {
    const normalizedValue: string = value.trim();

    if (normalizedValue.length === 0) {
      return false;
    }

    return !this.entries.every((entry: LocalizedTextEntry) => entry.value.trim() === normalizedValue);
  }

  applyValueToAllLanguages(value: string): void {
    const normalizedValue: string = value.trim();

    if (normalizedValue.length === 0) {
      return;
    }

    this.entries = this.entries.map((entry: LocalizedTextEntry) => ({
      languageCode: entry.languageCode,
      languageLabel: entry.languageLabel,
      value: normalizedValue
    }));

    this.propagateChanges();
    this.onTouched();
  }

  private propagateChanges(): void {
    const values: LocalizedItem<string>[] = this.entries
      .filter((entry: LocalizedTextEntry) => entry.value.trim().length > 0)
      .map((entry: LocalizedTextEntry) => ({
        languageCode: entry.languageCode,
        value: entry.value.trim()
      }));

    this.onChange(values);
  }

  private buildEntries(items: LocalizedItem<string>[]): LocalizedTextEntry[] {
    const normalizedItems: LocalizedItem<string>[] = items.map((item: LocalizedItem<string>) => ({
      languageCode: item.languageCode.toLowerCase(),
      value: item.value ?? ''
    }));

    const knownEntries: LocalizedTextEntry[] = LANGUAGES.map((language) => {
      const existingItem = normalizedItems.find((item: LocalizedItem<string>) => item.languageCode === language.value);

      return {
        languageCode: language.value,
        languageLabel: language.label,
        value: existingItem?.value ?? ''
      };
    });

    const extraEntries: LocalizedTextEntry[] = normalizedItems
      .filter((item: LocalizedItem<string>) => !LANGUAGES.some((language) => language.value === item.languageCode))
      .map((item: LocalizedItem<string>) => ({
        languageCode: item.languageCode,
        languageLabel: item.languageCode.toUpperCase(),
        value: item.value
      }));

    return [...knownEntries, ...extraEntries];
  }
}
