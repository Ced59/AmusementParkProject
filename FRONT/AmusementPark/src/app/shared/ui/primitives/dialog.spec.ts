import { TestBed } from '@angular/core/testing';
import { InteractivityChecker } from '@angular/cdk/a11y';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Dialog } from './dialog';

describe('Dialog presentation', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Dialog, TranslateModule.forRoot()],
      providers: [{ provide: InteractivityChecker, useValue: {
        // The unit DOM has no layout; visibility remains a browser QA criterion.
        isFocusable: (element: HTMLElement): boolean => element.tagName === 'BUTTON' && element.tabIndex >= 0,
        isTabbable: (element: HTMLElement): boolean => element.tagName === 'BUTTON' && element.tabIndex >= 0
      } }]
    }).compileComponents();
    const translations = TestBed.inject(TranslateService);
    translations.setTranslation('fr', { actions: { close: 'Fermer' } });
    translations.use('fr');
  });

  it('captures and loops modal focus, emits the existing close outputs once and restores the opener', async () => {
    const opener = document.createElement('button');
    document.body.appendChild(opener);
    opener.focus();
    const fixture = TestBed.createComponent(Dialog);
    try {
      const hidden = vi.fn();
      const visibility = vi.fn();
      fixture.componentInstance.onHide.subscribe(hidden);
      fixture.componentInstance.visibleChange.subscribe(visibility);
      fixture.componentRef.setInput('header', 'Confirmation');
      fixture.componentRef.setInput('modal', true);
      fixture.componentRef.setInput('visible', true);
      fixture.detectChanges();
      await fixture.whenStable();
      const root: HTMLElement = fixture.nativeElement;
      const close = root.querySelector<HTMLButtonElement>('.p-dialog-header-close')!;
      expect(close.getAttribute('aria-label')).toBe('Fermer');
      expect(root.querySelector('[role="dialog"]')!.getAttribute('aria-label')).toBe('Confirmation');
      expect(root.querySelector('[role="dialog"]')!.getAttribute('aria-modal')).toBe('true');
      expect(document.activeElement).toBe(close);
      const anchors = root.querySelectorAll<HTMLElement>('.cdk-focus-trap-anchor');
      expect(anchors.length).toBe(2);
      anchors[0].focus();
      expect(document.activeElement).toBe(close);
      anchors[1].focus();
      expect(document.activeElement).toBe(close);
      close.click();
      fixture.detectChanges();
      await fixture.whenStable();
      expect(hidden).toHaveBeenCalledTimes(1);
      expect(visibility).toHaveBeenCalledExactlyOnceWith(false);
      expect(root.querySelector('[role="dialog"]')).toBeNull();
      expect(document.activeElement).toBe(opener);
    } finally {
      fixture.destroy();
      opener.remove();
    }
  });

  it('retains outside close only for dismissable masks and never closes on content clicks', () => {
    const fixture = TestBed.createComponent(Dialog);
    fixture.componentRef.setInput('visible', true);
    fixture.componentRef.setInput('dismissableMask', true);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    const hidden = vi.fn();
    fixture.componentInstance.onHide.subscribe(hidden);
    root.querySelector<HTMLElement>('.p-dialog-content')!.click();
    expect(fixture.componentInstance.visible).toBe(true);
    const backdrop = root.querySelector<HTMLButtonElement>('.p-dialog-mask__backdrop')!;
    expect(backdrop.getAttribute('aria-label')).toBe('Fermer');
    expect(backdrop.tabIndex).toBe(-1);
    backdrop.click();
    expect(fixture.componentInstance.visible).toBe(false);
    expect(hidden).toHaveBeenCalledTimes(1);
  });

  it('preserves non-dismissable and non-closable states without claiming modal semantics', () => {
    const fixture = TestBed.createComponent(Dialog);
    fixture.componentRef.setInput('visible', true);
    fixture.componentRef.setInput('closable', false);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('.p-dialog-mask__backdrop')).toBeNull();
    expect(root.querySelector('.p-dialog-header-close')).toBeNull();
    expect(root.querySelector('[role="dialog"]')!.hasAttribute('aria-modal')).toBe(false);
    root.querySelector<HTMLElement>('.p-dialog-mask')!.click();
    expect(fixture.componentInstance.visible).toBe(true);
    expect(Array.from(root.querySelectorAll<HTMLElement>('.cdk-focus-trap-anchor')).every(anchor => anchor.tabIndex === -1)).toBe(true);
  });
});
