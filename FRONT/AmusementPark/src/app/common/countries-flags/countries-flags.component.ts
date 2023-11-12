import { CommonModule } from '@angular/common';
import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-countries-flags',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div (click)="toggleDropdown()">
      <img [src]="getFlagUrl()" [alt]="countryCode" style="width: 50px; height: auto;">
      <!-- Ajouter une icône de flèche ici si nécessaire -->
    </div>
    <div *ngIf="showDropdown">
      <!-- Liste des drapeaux pour la sélection -->
      <div *ngFor="let country of countries" (click)="selectCountry(country)">
        <img [src]="getFlagUrl(country)" [alt]="country" style="width: 50px; height: auto;">
      </div>
    </div>
  `
})
export class CountriesFlagsComponent {
  @Input() countryCode: string = "GB";
  @Output() countrySelected = new EventEmitter<string>();

  showDropdown = false;
  countries = ['GB', 'FR']; // Liste des codes des pays disponibles

  getFlagUrl(country: string = this.countryCode): string {
    return `./assets/svg-country-flags/svg/${country.toLowerCase()}.svg`;
  }

  toggleDropdown() {
    this.showDropdown = !this.showDropdown;
  }

  selectCountry(country: string) {
    this.countryCode = country;
    this.showDropdown = false;
    this.countrySelected.emit(country);
    console.log("Country selected: ", country);
  }
}

