import { PassportAnonymousImportPanelComponent } from './components/passport-anonymous-import-panel/passport-anonymous-import-panel.component';
import { PassportAnonymousDraftEditorPageComponent } from './pages/passport-anonymous-draft-editor-page/passport-anonymous-draft-editor-page.component';
import { PassportAnonymousDraftsPageComponent } from './pages/passport-anonymous-drafts-page/passport-anonymous-drafts-page.component';

describe('anonymous passport responsive contract', () => {
  it.each([
    PassportAnonymousDraftsPageComponent,
    PassportAnonymousDraftEditorPageComponent,
    PassportAnonymousImportPanelComponent
  ])('bounds %s to the viewport and reflows narrow layouts', (component: unknown) => {
    const styles: string = (
      component as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('overflow-x: clip');
    expect(styles).toMatch(/@media \(max-width: (680|780)px\)/);
    expect(styles).toContain('grid-template-columns: 1fr');
  });
});
