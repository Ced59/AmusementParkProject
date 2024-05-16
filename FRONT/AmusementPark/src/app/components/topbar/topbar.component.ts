import { Component } from '@angular/core';
import { TranslationService } from '../../services/translation.service';

@Component({
  selector: 'app-topbar',
  templateUrl: './topbar.component.html',
  styleUrls: ['./topbar.component.scss']
})
export class TopbarComponent {
  languages = [
    { label: 'English', value: 'en' },
    { label: 'French', value: 'fr' },
    { label: 'Spanish', value: 'es' },
    { label: 'German', value: 'de' }
  ];
  selectedLanguage = this.languages[0].value;

  constructor(private translationService: TranslationService) {}

  changeLanguage(lang: string) {
    this.translationService.useLang(lang).subscribe();
  }
}
