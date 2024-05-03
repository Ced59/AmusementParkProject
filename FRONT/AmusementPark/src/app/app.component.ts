import { Component } from '@angular/core';
import { TranslationService } from './services/translation.service';
import {ActivatedRoute} from "@angular/router";

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  title = "Amusement Parks";
  constructor(
      private translationService: TranslationService,
      private route: ActivatedRoute
  )
  {
    // Écouter les changements de paramètres de route
    this.route.root.paramMap.subscribe(paramMap => {
      const lang = paramMap.get('lang');
      console.log("Current Lang:", lang);
      if (lang) {
        this.translationService.useLang(lang);
      }
    });

  }
}


