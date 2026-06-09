import { TranslateLoader } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

/**
 * Charge les traductions directement depuis les assets du build SSR.
 *
 * Le TranslateHttpLoader fonctionne côté navigateur, mais côté SSR il peut
 * résoudre les URLs relatives contre l'URL publique courante
 * (https://amusement-parks.fun/assets/i18n/...). En production cela fait
 * repasser Node par Nginx Proxy Manager et peut créer une boucle/504 lorsque
 * le front SSR est déjà saturé.
 */
export class ServerTranslateLoader implements TranslateLoader {
  private readonly translationsByLanguage: Map<string, Record<string, unknown>> = new Map<string, Record<string, unknown>>();

  getTranslation(lang: string): Observable<Record<string, unknown>> {
    const normalizedLanguage: string = this.normalizeLanguage(lang);
    const cachedTranslation: Record<string, unknown> | undefined = this.translationsByLanguage.get(normalizedLanguage);

    if (cachedTranslation !== undefined) {
      return of(cachedTranslation);
    }

    const translation: Record<string, unknown> = this.readTranslation(normalizedLanguage);
    this.translationsByLanguage.set(normalizedLanguage, translation);

    return of(translation);
  }

  private readTranslation(language: string): Record<string, unknown> {
    const candidates: string[] = this.buildCandidatePaths(language);
    const filePath: string | undefined = candidates.find((candidatePath: string): boolean => existsSync(candidatePath));

    if (filePath === undefined) {
      if (language !== 'en') {
        return this.readTranslation('en');
      }

      console.error(`SSR translation file not found. Checked: ${candidates.join(', ')}`);
      return {};
    }

    try {
      const content: string = readFileSync(filePath, 'utf8');
      return JSON.parse(content) as Record<string, unknown>;
    } catch (error: unknown) {
      console.error(`SSR translation file could not be read: ${filePath}`, error);

      if (language !== 'en') {
        return this.readTranslation('en');
      }

      return {};
    }
  }

  private buildCandidatePaths(language: string): string[] {
    const currentWorkingDirectory: string = process.cwd();

    return [
      join(currentWorkingDirectory, 'dist', 'amusement-park', 'browser', 'assets', 'i18n', `${language}.json`),
      join(currentWorkingDirectory, 'browser', 'assets', 'i18n', `${language}.json`),
      join(currentWorkingDirectory, 'assets', 'i18n', `${language}.json`),
      join(currentWorkingDirectory, 'src', 'assets', 'i18n', `${language}.json`)
    ];
  }

  private normalizeLanguage(language: string): string {
    const trimmedLanguage: string = (language || 'en').trim().toLowerCase();

    if (!trimmedLanguage) {
      return 'en';
    }

    return trimmedLanguage.split(/[-_]/, 1)[0] || 'en';
  }
}
