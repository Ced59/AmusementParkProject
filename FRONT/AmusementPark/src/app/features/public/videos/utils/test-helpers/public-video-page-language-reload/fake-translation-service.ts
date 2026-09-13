import { Subject } from 'rxjs';

export class FakeTranslationService {
  readonly languageChanged: Subject<string> = new Subject<string>();

  getCurrentLang(): string {
    return 'fr';
  }
}
