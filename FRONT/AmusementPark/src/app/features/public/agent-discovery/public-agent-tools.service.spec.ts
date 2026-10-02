import { DOCUMENT } from '@angular/common';
import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { SearchApiResponse } from '@app/models/search/search-api-response';
import { SKIP_AUTHORIZATION_HEADER, SKIP_PUBLIC_VIEW_SIMULATION } from '@core/http/auth/auth-request-policy';
import { PARK_LIST_STATE_SEARCH_API_SERVICE_PORT } from '../parks/state/park-list-state-data.ports';
import { PublicAgentTool } from './public-agent-tool.model';
import { PublicAgentToolsService } from './public-agent-tools.service';

describe('PublicAgentToolsService', () => {
  const getSearch = vi.fn();
  const registerTool = vi.fn();
  const unregisterTool = vi.fn();
  let document: Document;
  let service: PublicAgentToolsService;

  beforeEach(() => {
    getSearch.mockReset(); registerTool.mockReset(); unregisterTool.mockReset();
    TestBed.configureTestingModule({ providers: [
      PublicAgentToolsService,
      { provide: PLATFORM_ID, useValue: 'browser' },
      { provide: PARK_LIST_STATE_SEARCH_API_SERVICE_PORT, useValue: { getSearch } }
    ] });
    document = TestBed.inject(DOCUMENT);
    document.documentElement.lang = 'fr';
    Object.defineProperty(document, 'modelContext', { configurable: true, value: { registerTool, unregisterTool } });
    service = TestBed.inject(PublicAgentToolsService);
  });

  afterEach(() => {
    TestBed.resetTestingModule();
    delete (document as Document & { modelContext?: unknown }).modelContext;
  });

  it('registers one read-only tool without making any initial request', async () => {
    service.initialize(); service.initialize();
    await Promise.resolve();
    expect(registerTool).toHaveBeenCalledTimes(1);
    expect(registerTool.mock.calls[0][0].annotations.readOnlyHint).toBe(true);
    expect(getSearch).not.toHaveBeenCalled();
    TestBed.resetTestingModule();
    expect(unregisterTool).toHaveBeenCalledWith('search_public_parks');
  });

  it('does nothing in an unsupported browser', () => {
    delete (document as Document & { modelContext?: unknown }).modelContext;
    service.initialize();
    expect(registerTool).not.toHaveBeenCalled();
    expect(getSearch).not.toHaveBeenCalled();
  });

  it('does nothing on the server', () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({ providers: [PublicAgentToolsService,
      { provide: PLATFORM_ID, useValue: 'server' },
      { provide: PARK_LIST_STATE_SEARCH_API_SERVICE_PORT, useValue: { getSearch } }
    ] });
    TestBed.inject(PublicAgentToolsService).initialize();
    expect(registerTool).not.toHaveBeenCalled();
  });

  it('returns only public park links with an anonymous bounded request', async () => {
    getSearch.mockReturnValue(of({ data: [
      { originalId: 'park_park-1', category: 'Park', title: 'Phantasialand', description: '', city: 'Brühl' },
      { originalId: 'item-1', category: 'ParkItem', title: 'Private result', description: '' }
    ], pagination: { currentPage: 1, totalItems: 2, pageSize: 10, totalPages: 1 } }));
    service.initialize();
    const tool: PublicAgentTool = registerTool.mock.calls[0][0];
    const result = await tool.execute({ query: ' Phantasialand ' });
    expect(getSearch.mock.calls[0].slice(0, 4)).toEqual(['Phantasialand', ['park'], 1, 10]);
    expect(getSearch.mock.calls[0][4].context.get(SKIP_AUTHORIZATION_HEADER)).toBe(true);
    expect(getSearch.mock.calls[0][4].context.get(SKIP_PUBLIC_VIEW_SIMULATION)).toBe(true);
    expect(result).toEqual({ language: 'fr', parks: [{ name: 'Phantasialand', url: '/fr/park/park-1/phantasialand', city: 'Brühl', countryCode: null }] });
  });

  it('rejects invalid input and simultaneous searches', async () => {
    const response = new Subject<SearchApiResponse>();
    getSearch.mockReturnValue(response);
    service.initialize();
    const tool: PublicAgentTool = registerTool.mock.calls[0][0];
    await expect(tool.execute({ query: ' ' })).rejects.toThrow('between 2 and 120');
    expect(getSearch).not.toHaveBeenCalled();
    const pending = tool.execute({ query: 'Phantasialand' });
    await expect(tool.execute({ query: 'Disneyland' })).rejects.toThrow('already in progress');
    response.error(new Error('Transport failure'));
    await expect(pending).rejects.toThrow('Transport failure');
    getSearch.mockReturnValue(of({ data: [], pagination: null }));
    await expect(tool.execute({ query: 'Disneyland' })).resolves.toEqual({ language: 'fr', parks: [] });
  });

  it('cleans up a registration resolved after destruction', async () => {
    let finish: (() => void) | undefined;
    registerTool.mockReturnValue(new Promise<void>((resolve) => { finish = resolve; }));
    service.initialize();
    const tool: PublicAgentTool = registerTool.mock.calls[0][0];
    TestBed.resetTestingModule();
    finish?.();
    await Promise.resolve();
    expect(unregisterTool).toHaveBeenCalledWith('search_public_parks');
    await expect(tool.execute({ query: 'Phantasialand' })).rejects.toThrow('no longer available');
  });
});
