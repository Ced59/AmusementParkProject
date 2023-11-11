import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-countries-flags',
  standalone: true,
  template: `<img [src]="getFlagUrl()" [alt]="countryCode" style="width: 50px; height: auto;">`
})
export class CountriesFlagsComponent {
  @Input() countryCode: string = "US";

  getFlagUrl(): string {
    return `./node_modules/svg-country-flags/svg/${this.countryCode.toLowerCase()}.svg`;
  }
}

