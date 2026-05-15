import { Component, forwardRef, Input } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, FormsModule } from '@angular/forms';

import { LANGUAGES } from '@shared/models/localization';
import { isRichTextEmpty } from '@shared/utils/localization';
import { HtmlSecurityService } from '@shared/utils/security';
import { LocalizedItem } from '@app/models/shared/localized-item';
import { Bind } from 'primeng/bind';
import { Tabs, TabList, Tab, TabPanels, TabPanel } from 'primeng/tabs';
import { Ripple } from 'primeng/ripple';
import { Editor } from 'primeng/editor';
import { PrimeTemplate } from 'primeng/api';
import { TranslateModule } from '@ngx-translate/core';

interface LocalizedRichTextEntry {
  languageCode: string;
  languageLabel: string;
  value: string;
}

@Component({
    selector: 'app-localized-rich-text-editor',
    templateUrl: './localized-rich-text-editor.component.html',
    styleUrls: ['./localized-rich-text-editor.component.scss'],
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => LocalizedRichTextEditorComponent),
            multi: true
        }
    ],
    imports: [Bind, Tabs, TabList, Ripple, Tab, TabPanels, TabPanel, Editor, FormsModule, PrimeTemplate, TranslateModule]
})
export class LocalizedRichTextEditorComponent implements ControlValueAccessor {
  @Input() placeholderKey: string = 'admin.parks.descriptions.placeholder';
  @Input() editorHeight: string = '18rem';

  activeTabIndex: number = 0;
  entries: LocalizedRichTextEntry[] = this.buildEntries([]);
  isDisabled: boolean = false;

  private onChange: (value: LocalizedItem<string>[]) => void = () => {};
  private onTouched: () => void = () => {};

  constructor(private readonly htmlSecurityService: HtmlSecurityService) {
  }

  writeValue(value: LocalizedItem<string>[] | null): void {
    const sanitizedItems: LocalizedItem<string>[] = (value ?? []).map((item: LocalizedItem<string>) => ({
      languageCode: item.languageCode,
      value: this.htmlSecurityService.sanitizeRichHtml(item.value)
    }));

    this.entries = this.buildEntries(sanitizedItems);
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

  trackByLanguageCode(index: number, entry: LocalizedRichTextEntry): string {
    return entry.languageCode;
  }

  onEntryValueChange(): void {
    this.propagateChanges();
    this.onTouched();
  }

  private propagateChanges(): void {
    const values: LocalizedItem<string>[] = this.entries
      .filter((entry: LocalizedRichTextEntry) => !isRichTextEmpty(entry.value))
      .map((entry: LocalizedRichTextEntry) => ({
        languageCode: entry.languageCode,
        value: this.htmlSecurityService.sanitizeRichHtml(entry.value)
      }));

    this.onChange(values);
  }

  private buildEntries(items: LocalizedItem<string>[]): LocalizedRichTextEntry[] {
    const normalizedItems: LocalizedItem<string>[] = items.map((item: LocalizedItem<string>) => ({
      languageCode: item.languageCode.toLowerCase(),
      value: item.value ?? ''
    }));

    const knownEntries: LocalizedRichTextEntry[] = LANGUAGES.map((language) => {
      const existingItem = normalizedItems.find((item: LocalizedItem<string>) => item.languageCode === language.value);

      return {
        languageCode: language.value,
        languageLabel: language.label,
        value: existingItem?.value ?? ''
      };
    });

    const extraEntries: LocalizedRichTextEntry[] = normalizedItems
      .filter((item: LocalizedItem<string>) => !LANGUAGES.some((language) => language.value === item.languageCode))
      .map((item: LocalizedItem<string>) => ({
        languageCode: item.languageCode,
        languageLabel: item.languageCode.toUpperCase(),
        value: item.value
      }));

    return [...knownEntries, ...extraEntries];
  }
}
