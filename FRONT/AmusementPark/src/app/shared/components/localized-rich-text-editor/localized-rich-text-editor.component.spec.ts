import { ViewEncapsulation } from '@angular/core';

import { LocalizedRichTextEditorComponent } from './localized-rich-text-editor.component';
import { HtmlSecurityService } from '@shared/utils/security';
import { UrlSecurityService } from '@shared/utils/security/url-security.service';

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
    expect(styles).toContain('img.rich-text__image--left');
    expect(styles).toContain('img.rich-text__image--right');
    expect(styles).toContain('float: none');
  });

  it('keeps managed images opt-in and preserves their stable tag across languages', () => {
    const component: LocalizedRichTextEditorComponent = new LocalizedRichTextEditorComponent(
      new HtmlSecurityService(document, new UrlSecurityService())
    );
    expect(component.allowManagedImages).toBe(false);
    component.allowManagedImages = true;
    component.writeValue([
      {
        languageCode: 'fr',
        value: '<p>Texte</p><img src="/images/0123456789abcdef0123456789abcdef" class="rich-text__image rich-text__image--right" alt="Parc">'
      },
      {
        languageCode: 'en',
        value: '<p>Text</p><img src="/images/0123456789abcdef0123456789abcdef" class="rich-text__image rich-text__image--right" alt="Park">'
      }
    ]);

    const french = component.entries.find((entry) => entry.languageCode === 'fr');
    const english = component.entries.find((entry) => entry.languageCode === 'en');
    expect(french?.value).toContain(
      'src="/images/0123456789abcdef0123456789abcdef" class="rich-text__image rich-text__image--right"'
    );
    expect(english?.value).toContain(
      'src="/images/0123456789abcdef0123456789abcdef" class="rich-text__image rich-text__image--right"'
    );
  });

  it('deletes an image draft only after it disappears from every language', async () => {
    const component: LocalizedRichTextEditorComponent = new LocalizedRichTextEditorComponent(
      new HtmlSecurityService(document, new UrlSecurityService())
    );
    const imageId: string = '0123456789abcdef0123456789abcdef';
    const removed: string[] = [];
    component.allowManagedImages = true;
    component.managedImageRemoved = (removedImageId: string): void => {
      removed.push(removedImageId);
    };
    component.writeValue([
      {
        languageCode: 'fr',
        value: `<p>Texte</p><img src="/images/${imageId}" class="rich-text__image rich-text__image--full" alt="">`
      },
      {
        languageCode: 'en',
        value: `<p>Text</p><img src="/images/${imageId}" class="rich-text__image rich-text__image--full" alt="">`
      }
    ]);

    const french = component.entries.find((entry) => entry.languageCode === 'fr');
    const english = component.entries.find((entry) => entry.languageCode === 'en');
    if (!french || !english) {
      throw new Error('Expected French and English entries.');
    }

    french.value = '<p>Texte</p>';
    component.managedImageRemovalHandler('fr')(imageId);
    await Promise.resolve();
    expect(removed).toEqual([]);

    english.value = '<p>Text</p>';
    component.managedImageRemovalHandler('en')(imageId);
    await Promise.resolve();
    expect(removed).toEqual([imageId]);
  });

  it('preserves published image tags for an owner who cannot add new images', () => {
    const component: LocalizedRichTextEditorComponent = new LocalizedRichTextEditorComponent(
      new HtmlSecurityService(document, new UrlSecurityService())
    );
    component.allowManagedImages = false;
    component.preserveManagedImages = true;
    component.writeValue([{
      languageCode: 'fr',
      value: '<p>Texte corrigé</p><img src="/images/0123456789abcdef0123456789abcdef" class="rich-text__image rich-text__image--left" alt="Parc">'
    }]);

    expect(component.entries.find((entry) => entry.languageCode === 'fr')?.value).toContain(
      'src="/images/0123456789abcdef0123456789abcdef" class="rich-text__image rich-text__image--left"'
    );
  });
});
