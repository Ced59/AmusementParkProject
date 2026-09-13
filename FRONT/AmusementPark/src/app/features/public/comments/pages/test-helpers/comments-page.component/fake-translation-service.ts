import { EventEmitter } from '@angular/core';

export class FakeTranslationService {
  readonly languageChanged: EventEmitter<string> = new EventEmitter<string>();

  getCurrentLang(): string {
    return 'fr';
  }
}
