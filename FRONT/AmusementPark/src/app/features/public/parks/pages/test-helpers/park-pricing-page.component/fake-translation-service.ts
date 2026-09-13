import { EventEmitter } from '@angular/core';

export class FakeTranslationService {
  public readonly languageChanged: EventEmitter<string> = new EventEmitter<string>();

  getCurrentLang(): string {
    return 'fr';
  }
}
