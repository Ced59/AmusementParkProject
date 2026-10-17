import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';

const projectRoot = resolve(import.meta.dirname, '../..');
const read = (relativePath) => readFileSync(resolve(projectRoot, relativePath), 'utf8');

const appRoot = read('src/app/app.component.html');
const languageEntry = read('src/app/features/public/language-entry/pages/language-entry-page.component.html');
const languageEntryState = read('src/app/features/public/language-entry/pages/language-entry-page.component.ts');
const languageEntryStyles = read('src/app/features/public/language-entry/pages/language-entry-page.component.scss');
const cookieConsent = read('src/app/ui/layouts/cookie-consent-banner/cookie-consent-banner.component.html');
const cookieConsentState = read('src/app/ui/layouts/cookie-consent-banner/cookie-consent-banner.component.ts');
const cookieConsentStyles = read('src/app/ui/layouts/cookie-consent-banner/cookie-consent-banner.component.scss');
const dialog = read('src/app/shared/ui/primitives/dialog.ts');
const toast = read('src/app/shared/ui/primitives/toast.ts');
const tooltip = read('src/app/shared/ui/primitives/tooltip.ts');
const primitiveStyles = read('src/styles/_ui-primitives.scss');
const pageState = read('src/app/shared/components/page-state/page-state.component.html');
const stateStyles = read('src/styles/_states.scss');
const richText = read('src/app/shared/components/localized-rich-text-editor/localized-rich-text-editor.component.html');
const richTextState = read('src/app/shared/components/localized-rich-text-editor/localized-rich-text-editor.component.ts');
const editorState = read('src/app/shared/ui/primitives/editor.ts');
const richTextStyles = read('src/app/shared/components/localized-rich-text-editor/localized-rich-text-editor.component.scss');
const ownerUpload = read('src/app/shared/components/owner-image-upload-dialog/owner-image-upload-dialog.component.html');
const quickCreateStyles = read('src/app/components/admin/park-items/workbench/admin-park-item-quick-create-drawer.component.scss');
const pageJump = read('src/app/shared/components/page-jump-button/page-jump-button.component.html');
const pageJumpStyles = read('src/app/shared/components/page-jump-button/page-jump-button.component.scss');
const pageJumpState = read('src/app/shared/components/page-jump-button/page-jump-button.component.ts');
const navigationStyles = read('src/styles/_navigation.scss');
const accessibilityStyles = read('src/styles/_accessibility-polish.scss');
const cardMixins = read('src/styles/_mixins.scss');
const contentSummaryStyles = read('src/app/features/public/parks/ui/park-content-summary.component.scss');
const zoneListStyles = read('src/app/features/public/park-items/ui/park-items-zone-list.component.scss');

test('upload selection and drawer backdrops remain named native buttons without container clicks', () => {
  const drawer = read('src/app/components/admin/park-items/workbench/admin-park-item-quick-create-drawer.component.html');
  const uploadStyles = read('src/app/shared/components/owner-image-upload-dialog/owner-image-upload-dialog.component.scss');
  assert.match(drawer, /<button type="button" tabindex="-1" class="quick-create-backdrop"[\s\S]*?\(click\)="closed\.emit\(\)"/);
  assert.match(ownerUpload, /<button\s+type="button"\s+class="drop-zone__picker"[\s\S]*?\[disabled\]="isUploading"[\s\S]*?\(click\)="openFilePicker\(\)"/);
  assert.ok(ownerUpload.includes('[attr.tabindex]="previewUrl ? 0 : -1"'));
  assert.doesNotMatch(ownerUpload, /<div[^>]*\(click\)/);
  assert.doesNotMatch(uploadStyles, /border-radius:\s*(?:1rem|0\.75rem)/);
  assert.ok(uploadStyles.includes('.drop-zone__picker:focus-visible'));
});

test('the language entry keeps eight choices without reusing the legacy standalone logo', () => {
  assert.equal(languageEntryState.match(/^\s{2}(?:en|fr|es|de|it|pl|nl|pt):/gm)?.length, 8);
  assert.ok(languageEntry.includes("[routerLink]=\"['/', language.value, 'home']\""));
  assert.ok(languageEntry.includes('(click)="selectLanguage(language.value)"'));
  assert.ok(languageEntry.includes('[attr.lang]="language.code"'));
  assert.ok(languageEntry.includes('language-entry__playground-mark'));
  assert.ok(!languageEntry.includes('logo-amusementpark-language'));
});

test('the small language-entry eyebrow uses a readable semantic foreground', () => {
  const eyebrow = languageEntryStyles.match(/\.language-entry__eyebrow\s*\{([^}]+)\}/)?.[1];
  assert.ok(eyebrow, 'the language-entry eyebrow rule must exist');
  assert.match(eyebrow, /color:\s*var\(--app-primary-strong\)/);
  assert.doesNotMatch(eyebrow, /color:\s*var\(--c-orange\)/);
});

test('the root shell keeps toast, privacy consent and the page jump control', () => {
  for (const selector of ['<router-outlet>', '<app-ui-toast', '<app-cookie-consent-banner>', '<app-page-jump-button>']) {
    assert.ok(appRoot.includes(selector), `${selector} must remain mounted`);
  }
  assert.ok(cookieConsent.includes('(click)="acceptOptionalCookies()"'));
  assert.ok(cookieConsent.includes('(click)="continueWithNecessaryCookiesOnly()"'));
  assert.ok(cookieConsent.includes("['/', currentLanguage(), 'privacy']"));
  assert.ok(cookieConsentState.includes('afterNextRender'));
  assert.ok(cookieConsentState.includes('isConsentRequired'));
});

test('dialogs, toasts and tooltips retain accessible roles and dismissal behavior', () => {
  assert.ok(dialog.includes('role="dialog"'));
  assert.ok(dialog.includes("[attr.aria-modal]=\"modal ? 'true' : null\""));
  assert.ok(dialog.includes('[cdkTrapFocus]="modal"'));
  assert.ok(dialog.includes('dismissableMask'));
  assert.ok(dialog.includes('visibleChange.emit(false)'));
  assert.ok(toast.includes("message.severity === 'error' ? 'alert' : 'status'"));
  assert.ok(toast.includes("message.severity === 'error' ? 'assertive' : 'polite'"));
  assert.ok(toast.includes('(click)="dismiss(message)"'));
  assert.ok(toast.includes('setTimeout'));
  for (const contract of ["@HostListener('focusin')", "@HostListener('focusout')", "@HostListener('keydown.escape')", "'aria-describedby'", "role', 'tooltip'"]) {
    assert.ok(tooltip.includes(contract), `${contract} must remain in the tooltip`);
  }
  assert.ok(primitiveStyles.includes('.app-ui-tooltip'));
});

test('loading, empty and error states remain announced and motion-safe', () => {
  assert.ok(pageState.includes('role="status" aria-live="polite" aria-busy="true"'));
  assert.ok(pageState.includes('role="alert" aria-live="assertive"'));
  assert.equal(pageState.match(/role="status"/g)?.length, 2);
  assert.ok(stateStyles.includes('@media (prefers-reduced-motion: reduce)'));
  assert.match(stateStyles, /prefers-reduced-motion[\s\S]*?animation:\s*none/);
});

test('localized Quill editing preserves sanitization, languages and managed images', () => {
  assert.ok(richText.includes('@for (entry of entries'));
  assert.ok(richText.includes('[readonly]="isInteractionDisabled"'));
  assert.ok(richText.includes('[managedImageUpload]="managedImageUpload"'));
  assert.ok(richText.includes('class="ql-clean"'));
  assert.ok(richTextState.includes('sanitizeManagedImageRichHtml'));
  assert.ok(richTextState.includes('sanitizeRichHtml'));
  assert.ok(editorState.includes('sanitizeEditorHtml'));
  assert.ok(editorState.includes('sanitizeManagedImageRichHtml'));
  assert.ok(richTextStyles.includes('overflow-x: auto'));
});

test('uploads and floating controls retain disabled, focus and safe-area states', () => {
  assert.ok(ownerUpload.includes('[dismissableMask]="!isUploading"'));
  assert.ok(ownerUpload.includes('[closable]="!isUploading"'));
  assert.ok(ownerUpload.includes('(drop)="onDrop($event)"'));
  assert.ok(ownerUpload.includes('(click)="upload()"'));
  assert.ok(ownerUpload.includes('(click)="cancel()"'));
  assert.ok(pageJump.includes('[attr.aria-label]="labelKey | translate"'));
  assert.ok(pageJump.includes('(click)="togglePageScroll()"'));
  assert.ok(pageJumpStyles.includes('var(--bottom-nav-height)'));
  assert.ok(pageJumpState.includes("['.app-mobile-bottom-nav', '--bottom-nav-height']"));
  assert.ok(pageJumpState.includes('element?.offsetHeight'));
  assert.match(navigationStyles, /padding: 0\.35rem 0\.5rem max\(0\.35rem, env\(safe-area-inset-bottom\)\)/);
  assert.match(navigationStyles, /@media \(max-width: 390px\)\s*\{\s*\.app-mobile-bottom-nav\s*\{[^}]*padding-bottom: max\(0\.4rem, env\(safe-area-inset-bottom\)\)/);
});

test('rare and previously missed surfaces use flat Playground styling', () => {
  const flattenedStyles = [
    languageEntryStyles,
    cookieConsentStyles,
    quickCreateStyles,
    accessibilityStyles,
    cardMixins,
    contentSummaryStyles,
    zoneListStyles,
    pageJumpStyles,
  ].join('\n');
  assert.match(languageEntryStyles, /border-top:\s*4px solid var\(--c-orange\)/);
  assert.match(cookieConsentStyles, /background:\s*var\(--app-surface\)/);
  assert.match(contentSummaryStyles, /border-left:\s*4px solid var\(--app-primary\)/);
  assert.doesNotMatch(flattenedStyles, /(?:linear|radial|conic|repeating-linear)-gradient\(/);
  assert.doesNotMatch(flattenedStyles, /backdrop-filter\s*:/);
});
