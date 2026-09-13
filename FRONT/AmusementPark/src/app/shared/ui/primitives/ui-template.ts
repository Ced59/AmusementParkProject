import { Directive, Input, TemplateRef } from '@angular/core';

@Directive({
  selector: '[appUiTemplate]',
  standalone: true
})
export class UiTemplate {
  @Input('appUiTemplate') name: string = '';

  constructor(public readonly template: TemplateRef<unknown>) {
  }
}
