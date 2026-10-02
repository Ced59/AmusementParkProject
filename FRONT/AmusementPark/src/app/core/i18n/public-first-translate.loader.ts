import { HttpClient } from '@angular/common/http';
import { TranslateLoader } from '@ngx-translate/core';
import { Observable, catchError, forkJoin, map, shareReplay } from 'rxjs';

type Dictionary = Record<string, unknown>;

export class PublicFirstTranslateLoader implements TranslateLoader {
  private readonly publicDictionaries = new Map<string, Observable<Dictionary>>();
  private readonly completeDictionaries = new Map<string, Observable<Dictionary>>();
  private readonly legacyDictionaries = new Map<string, Observable<Dictionary>>();
  private completeRequired: boolean = false;

  constructor(private readonly http: HttpClient) {
  }

  get requiresCompleteTranslations(): boolean {
    return this.completeRequired;
  }

  getTranslation(language: string): Observable<Dictionary> {
    return this.completeRequired ? this.getCompleteTranslation(language) : this.getPublicTranslation(language);
  }

  getCompleteTranslation(language: string): Observable<Dictionary> {
    this.completeRequired = true;
    let dictionary: Observable<Dictionary> | undefined = this.completeDictionaries.get(language);
    if (dictionary === undefined) {
      dictionary = forkJoin([
        this.getPublicTranslation(language),
        this.http.get<Dictionary>(`./assets/i18n/admin/${language}.json`)
      ]).pipe(
        map(([publicDictionary, adminDictionary]): Dictionary => ({ ...publicDictionary, ...adminDictionary })),
        catchError(() => this.getLegacyTranslation(language)),
        shareReplay({ bufferSize: 1, refCount: false })
      );
      this.completeDictionaries.set(language, dictionary);
    }
    return dictionary;
  }

  private getPublicTranslation(language: string): Observable<Dictionary> {
    let dictionary: Observable<Dictionary> | undefined = this.publicDictionaries.get(language);
    if (dictionary === undefined) {
      dictionary = this.http.get<Dictionary>(`./assets/i18n/public/${language}.json`).pipe(
        catchError(() => this.getLegacyTranslation(language)),
        shareReplay({ bufferSize: 1, refCount: false })
      );
      this.publicDictionaries.set(language, dictionary);
    }
    return dictionary;
  }

  private getLegacyTranslation(language: string): Observable<Dictionary> {
    let dictionary: Observable<Dictionary> | undefined = this.legacyDictionaries.get(language);
    if (dictionary === undefined) {
      dictionary = this.http.get<Dictionary>(`./assets/i18n/${language}.json`).pipe(
        shareReplay({ bufferSize: 1, refCount: false })
      );
      this.legacyDictionaries.set(language, dictionary);
    }
    return dictionary;
  }
}
