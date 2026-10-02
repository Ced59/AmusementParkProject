import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { DestroyRef, Inject, Injectable, PLATFORM_ID } from '@angular/core';
import { firstValueFrom, timeout } from 'rxjs';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { SearchApiResponse } from '@app/models/search/search-api-response';
import { SearchResultItem } from '@app/models/search/search-result-item';
import { buildPublicParkRouteCommands, buildPublicRoutePath } from '@shared/utils/routing/public-detail-route.helpers';
import { resolveSupportedLanguage } from '@shared/utils/routing/localized-route.helpers';
import { PARK_LIST_STATE_SEARCH_API_SERVICE_PORT, ParkListStateSearchApiServicePort } from '../parks/state/park-list-state-data.ports';
import { PublicAgentModelContext, PublicAgentTool } from './public-agent-tool.model';

@Injectable()
export class PublicAgentToolsService {
  private context: PublicAgentModelContext | null = null;
  private initialized: boolean = false;
  private destroyed: boolean = false;
  private registered: boolean = false;
  private searchInProgress: boolean = false;

  constructor(
    @Inject(DOCUMENT) private readonly document: Document,
    @Inject(PLATFORM_ID) private readonly platformId: object,
    @Inject(PARK_LIST_STATE_SEARCH_API_SERVICE_PORT) private readonly searchPort: ParkListStateSearchApiServicePort,
    destroyRef: DestroyRef
  ) {
    destroyRef.onDestroy(() => {
      this.destroyed = true;
      this.unregister();
    });
  }

  initialize(): void {
    if (this.initialized || !isPlatformBrowser(this.platformId)) {
      return;
    }
    this.initialized = true;
    const extendedDocument = this.document as Document & { modelContext?: PublicAgentModelContext };
    const extendedNavigator = this.document.defaultView?.navigator as (Navigator & { modelContext?: PublicAgentModelContext }) | undefined;
    this.context = extendedDocument.modelContext ?? extendedNavigator?.modelContext ?? null;
    if (!this.context || typeof this.context.registerTool !== 'function' || typeof this.context.unregisterTool !== 'function') {
      this.context = null;
      return;
    }
    const tool: PublicAgentTool = {
      name: 'search_public_parks',
      description: 'Search the published amusement park directory by park name or location. Returns up to ten public park names and localized page URLs. Does not access accounts or change data.',
      inputSchema: {
        type: 'object',
        properties: { query: { type: 'string', minLength: 2, maxLength: 120, description: 'Park name or location to search for.' } },
        required: ['query'],
        additionalProperties: false
      },
      annotations: { readOnlyHint: true, untrustedContentHint: true },
      execute: (input: unknown) => this.search(input)
    };
    try {
      void Promise.resolve(this.context.registerTool(tool)).then(() => {
        this.registered = true;
        if (this.destroyed) {
          this.unregister();
        }
      }).catch(() => { this.context = null; });
    } catch {
      this.context = null;
    }
  }

  private async search(input: unknown): Promise<unknown> {
    if (this.destroyed) {
      throw new Error('The public park search tool is no longer available.');
    }
    const query: unknown = input && typeof input === 'object' && !Array.isArray(input)
      ? (input as Record<string, unknown>)['query'] : null;
    if (typeof query !== 'string' || query.trim().length < 2 || query.trim().length > 120) {
      throw new Error('Provide a park name or location between 2 and 120 characters.');
    }
    if (this.searchInProgress) {
      throw new Error('A public park search is already in progress.');
    }
    this.searchInProgress = true;
    try {
      const response: SearchApiResponse = await firstValueFrom(
        this.searchPort.getSearch(query.trim(), ['park'], 1, 10, anonymousHttpOptions()).pipe(timeout(10000))
      );
      const language: string = resolveSupportedLanguage(this.document.documentElement.lang);
      return {
        language,
        parks: response.data.filter((item: SearchResultItem) => ['park', 'parks'].includes(item.category.toLowerCase())).slice(0, 10).map((item: SearchResultItem) => ({
          name: item.title,
          url: buildPublicRoutePath(buildPublicParkRouteCommands({ language, parkId: item.originalId.startsWith('park_') ? item.originalId.substring(5) : item.originalId, parkName: item.title })),
          city: item.city ?? null,
          countryCode: item.countryCode ?? null
        })).filter((park) => park.url !== null)
      };
    } finally {
      this.searchInProgress = false;
    }
  }

  private unregister(): void {
    if (!this.registered || !this.context) {
      return;
    }
    this.registered = false;
    try {
      this.context.unregisterTool('search_public_parks');
    } catch {
      // A browser may have already removed tools during document teardown.
    }
  }
}
