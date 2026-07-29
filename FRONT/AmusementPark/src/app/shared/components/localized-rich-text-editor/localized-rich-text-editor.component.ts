import { ChangeDetectionStrategy, Component, Input, ViewEncapsulation, forwardRef } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, FormsModule } from '@angular/forms';

import { LANGUAGES } from '@shared/models/localization';
import { isRichTextEmpty } from '@shared/utils/localization';
import { HtmlSecurityService } from '@shared/utils/security';
import { LocalizedItem } from '@app/models/shared/localized-item';
import { Tabs, TabList, Tab, TabPanels, TabPanel } from '@shared/ui/primitives/tabs';
import {
  Editor,
  ManagedImagePreviewResolver,
  ManagedImageRemovalHandler,
  ManagedImageUploadHandler
} from '@shared/ui/primitives/editor';
import { UiTemplate } from '@shared/ui/primitives/api';
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
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => LocalizedRichTextEditorComponent),
      multi: true
    }
  ],
  imports: [Tabs, TabList, Tab, TabPanels, TabPanel, Editor, FormsModule, UiTemplate, TranslateModule]
})
export class LocalizedRichTextEditorComponent implements ControlValueAccessor {
  @Input() placeholderKey: string = 'admin.parks.descriptions.placeholder';
  @Input() editorHeight: string = '18rem';
  @Input() allowManagedImages: boolean = false;
  @Input() preserveManagedImages: boolean = false;
  @Input() managedImageUpload: ManagedImageUploadHandler | null = null;
  @Input() managedImageRemoved: ManagedImageRemovalHandler | null = null;
  @Input() managedImagePreviewUrl: ManagedImagePreviewResolver | null = null;

  activeTabIndex: number = 0;
  entries: LocalizedRichTextEntry[] = this.buildEntries([]);
  isDisabled: boolean = false;

  private onChange: (value: LocalizedItem<string>[]) => void = () => {};
  private onTouched: () => void = () => {};
  private readonly imageRemovalHandlers: Map<string, ManagedImageRemovalHandler> =
    new Map<string, ManagedImageRemovalHandler>();

  constructor(private readonly htmlSecurityService: HtmlSecurityService) {
  }

  writeValue(value: LocalizedItem<string>[] | null): void {
    const sanitizedItems: LocalizedItem<string>[] = (value ?? []).map((item: LocalizedItem<string>) => ({
      languageCode: item.languageCode,
      value: this.sanitizeEntryValue(item.value)
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

  managedImageLabel(key: string): string {
    return `comments.editor.images.${key}`;
  }

  managedImageRemovalHandler(languageCode: string): ManagedImageRemovalHandler {
    const existingHandler: ManagedImageRemovalHandler | undefined =
      this.imageRemovalHandlers.get(languageCode);
    if (existingHandler) {
      return existingHandler;
    }

    const handler: ManagedImageRemovalHandler = (imageId: string): void => {
      queueMicrotask((): void => {
        const imageSource: string = `src="/images/${imageId}"`;
        const isStillReferenced: boolean = this.entries.some(
          (entry: LocalizedRichTextEntry) => entry.value.includes(imageSource)
        );
        if (!isStillReferenced) {
          this.managedImageRemoved?.(imageId);
        }
      });
    };
    this.imageRemovalHandlers.set(languageCode, handler);
    return handler;
  }

  private propagateChanges(): void {
    const values: LocalizedItem<string>[] = this.entries
      .filter((entry: LocalizedRichTextEntry) => !isRichTextEmpty(entry.value))
      .map((entry: LocalizedRichTextEntry) => ({
        languageCode: entry.languageCode,
        value: this.sanitizeEntryValue(entry.value)
      }));

    this.onChange(values);
  }

  private sanitizeEntryValue(value: string): string {
    return this.preserveManagedImages || this.allowManagedImages
      ? this.htmlSecurityService.sanitizeManagedImageRichHtml(value)
      : this.htmlSecurityService.sanitizeRichHtml(value);
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
