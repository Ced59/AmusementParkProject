import { Component } from '@angular/core';

@Component({
  selector: 'app-menu-bar',
  templateUrl: './menu-bar.component.html',
  styleUrls: ['./menu-bar.component.css']
})
export class MenuBarComponent {
  selectedCountry = "US";

  onCountryChange(newCountry: string) {
    this.selectedCountry = newCountry;
    // Ici, vous pouvez ajouter d'autres logiques nécessaires lorsque le pays change
  }
}

