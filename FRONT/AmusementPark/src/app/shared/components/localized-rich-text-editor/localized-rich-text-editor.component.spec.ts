import { ViewEncapsulation } from '@angular/core';

import { LocalizedRichTextEditorComponent } from './localized-rich-text-editor.component';

describe('LocalizedRichTextEditorComponent', () => {
  it('uses unscoped component styles so Quill CSS can stay out of the public global bundle', () => {
    expect((LocalizedRichTextEditorComponent as unknown as { ɵcmp: { encapsulation: ViewEncapsulation } }).ɵcmp.encapsulation)
      .toBe(ViewEncapsulation.None);
  });

  it('contains wide tabs and the editor toolbar inside the available mobile width', () => {
    const styles: string = (
      LocalizedRichTextEditorComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('.localized-rich-text-editor app-ui-tabs');
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('.localized-rich-text-editor .p-tablist');
    expect(styles).toContain('.localized-rich-text-editor .p-editor-toolbar');
    expect(styles).toContain('overflow-x: auto');
  });
});
