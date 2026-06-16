import { ViewEncapsulation } from '@angular/core';

import { LocalizedRichTextEditorComponent } from './localized-rich-text-editor.component';

describe('LocalizedRichTextEditorComponent', () => {
  it('uses unscoped component styles so Quill CSS can stay out of the public global bundle', () => {
    expect((LocalizedRichTextEditorComponent as unknown as { ɵcmp: { encapsulation: ViewEncapsulation } }).ɵcmp.encapsulation)
      .toBe(ViewEncapsulation.None);
  });
});
