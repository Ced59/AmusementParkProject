import { TestBed } from '@angular/core/testing';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Panel } from './panel';

describe('Panel presentation', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [Panel, TranslateModule.forRoot()] }).compileComponents();
    const translations = TestBed.inject(TranslateService);
    translations.setTranslation('fr', { actions: { expand: 'Déplier', collapse: 'Replier' } });
    translations.use('fr');
  });

  it('keeps the full named header inside one native toggle and updates its expanded state', () => {
    const fixture = TestBed.createComponent(Panel);
    fixture.componentRef.setInput('header', 'Informations pratiques');
    fixture.componentRef.setInput('toggleable', true);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    const button = root.querySelector<HTMLButtonElement>('.p-panel-toggle')!;
    expect(button.type).toBe('button');
    expect(button.textContent).toContain('Informations pratiques');
    expect(button.getAttribute('aria-label')).toBe('Replier — Informations pratiques');
    expect(button.getAttribute('aria-expanded')).toBe('true');
    button.querySelector('span')!.click();
    fixture.detectChanges();
    expect(fixture.componentInstance.collapsed).toBe(true);
    expect(button.getAttribute('aria-label')).toBe('Déplier — Informations pratiques');
    expect(button.getAttribute('aria-expanded')).toBe('false');
    expect(root.querySelector('.p-panel-content')).toBeNull();
    button.click();
    fixture.detectChanges();
    expect(fixture.componentInstance.collapsed).toBe(false);
    expect(root.querySelector('.p-panel-content')).not.toBeNull();
  });

  it('leaves non-toggleable headers inert and names a toggle even without a heading', () => {
    const fixture = TestBed.createComponent(Panel);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    root.querySelector<HTMLElement>('.p-panel-header')!.click();
    expect(fixture.componentInstance.collapsed).toBe(false);
    expect(root.querySelector('button')).toBeNull();
    fixture.componentRef.setInput('toggleable', true);
    fixture.detectChanges();
    expect(root.querySelector('button')!.getAttribute('aria-label')).toBe('Replier');
  });
});
