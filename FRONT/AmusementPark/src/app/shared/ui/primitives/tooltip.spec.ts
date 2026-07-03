import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';

import { Tooltip } from './primitives';

@Component({
  standalone: true,
  imports: [Tooltip],
  template: '<button type="button" [appUiTooltip]="text" tooltipPosition="bottom">?</button>'
})
class TooltipHostComponent {
  text = 'Helpful details';
}

describe('Tooltip primitive', () => {
  afterEach(() => {
    document.querySelectorAll('.app-ui-tooltip').forEach((element: Element) => element.remove());
  });

  it('shows a positioned tooltip on hover and removes it on mouse leave', async () => {
    await TestBed.configureTestingModule({
      imports: [TooltipHostComponent]
    }).compileComponents();

    const fixture: ComponentFixture<TooltipHostComponent> = TestBed.createComponent(TooltipHostComponent);
    fixture.detectChanges();

    const button: HTMLButtonElement = fixture.debugElement.query(By.css('button')).nativeElement;
    button.dispatchEvent(new Event('mouseenter'));
    fixture.detectChanges();

    const tooltip = document.querySelector('.app-ui-tooltip') as HTMLElement | null;

    expect(tooltip).not.toBeNull();
    if (tooltip === null) {
      fail('Expected tooltip to be rendered.');
      return;
    }

    expect(tooltip.textContent).toBe('Helpful details');
    expect(button.getAttribute('aria-describedby')).toBe(tooltip.id);

    button.dispatchEvent(new Event('mouseleave'));
    fixture.detectChanges();

    expect(document.querySelector('.app-ui-tooltip')).toBeNull();
    expect(button.hasAttribute('aria-describedby')).toBeFalse();
  });
});
