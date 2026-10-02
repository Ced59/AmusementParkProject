import { GoogleIdentityService } from './google-identity.service';

describe('GoogleIdentityService', () => {
  const scriptSelector: string = 'script[src="https://accounts.google.com/gsi/client"]';
  let previousGoogle: GoogleApi | undefined;
  let service: GoogleIdentityService;
  let googleApi: GoogleApi;

  beforeEach(() => {
    vi.useFakeTimers();
    previousGoogle = window.google;
    window.google = undefined;
    document.querySelectorAll(scriptSelector).forEach((script: Element): void => script.remove());
    service = new GoogleIdentityService('browser' as unknown as object, document);
    googleApi = {
      accounts: { id: { initialize: vi.fn(), renderButton: vi.fn(), disableAutoSelect: vi.fn() } }
    };
  });

  afterEach(() => {
    document.querySelectorAll(scriptSelector).forEach((script: Element): void => script.remove());
    window.google = previousGoogle;
    vi.useRealTimers();
  });

  it('does not load the library during ordinary browsing or auto-selection cleanup', () => {
    service.disableAutoSelect();

    expect(document.querySelector(scriptSelector)).toBeNull();
    expect(vi.getTimerCount()).toBe(0);
  });

  it('loads one script for concurrent sign-in buttons and initializes the SDK once', async () => {
    const firstContainer: HTMLDivElement = document.createElement('div');
    const secondContainer: HTMLDivElement = document.createElement('div');
    const firstRender: Promise<void> = service.renderButtonAsync(firstContainer, vi.fn());
    const secondRender: Promise<void> = service.renderButtonAsync(secondContainer, vi.fn());
    const scripts: NodeListOf<HTMLScriptElement> = document.querySelectorAll(scriptSelector);
    expect(scripts).toHaveLength(1);
    expect(scripts[0].async).toBe(true);

    window.google = googleApi;
    scripts[0].dispatchEvent(new Event('load'));
    await Promise.all([firstRender, secondRender]);

    expect(googleApi.accounts.id.initialize).toHaveBeenCalledOnce();
    expect(googleApi.accounts.id.renderButton).toHaveBeenCalledTimes(2);
    expect(vi.getTimerCount()).toBe(0);
  });

  it('uses an already available library without a network request and forwards credentials', async () => {
    window.google = googleApi;
    const callback = vi.fn();
    await service.renderButtonAsync(document.createElement('div'), callback);
    const configuration: GoogleIdConfiguration = vi.mocked(googleApi.accounts.id.initialize).mock.calls[0][0];
    const state: string | undefined = vi.mocked(googleApi.accounts.id.renderButton).mock.calls[0][1].state;
    const response: GoogleCredentialResponse = { credential: 'test-credential', select_by: 'btn', state };
    configuration.callback(response);

    expect(callback).toHaveBeenCalledWith(response);
    expect(document.querySelector(scriptSelector)).toBeNull();
  });

  it('removes a failed script and permits a later explicit retry', async () => {
    const failedRender: Promise<void> = service.renderButtonAsync(document.createElement('div'), vi.fn());
    const failure = expect(failedRender).rejects.toThrow('could not be loaded');
    document.querySelector(scriptSelector)?.dispatchEvent(new Event('error'));
    await failure;
    expect(document.querySelector(scriptSelector)).toBeNull();

    const retry: Promise<void> = service.renderButtonAsync(document.createElement('div'), vi.fn());
    window.google = googleApi;
    document.querySelector(scriptSelector)?.dispatchEvent(new Event('load'));
    await retry;
    expect(googleApi.accounts.id.renderButton).toHaveBeenCalledOnce();
  });

  it('times out a stalled script without leaving listeners or retry timers', async () => {
    const render: Promise<void> = service.renderButtonAsync(document.createElement('div'), vi.fn());
    const failure = expect(render).rejects.toThrow('could not be loaded');
    await vi.advanceTimersByTimeAsync(10_000);
    await failure;

    expect(document.querySelector(scriptSelector)).toBeNull();
    expect(vi.getTimerCount()).toBe(0);
  });

  it('does not load or initialize identity services on the server', async () => {
    const serverService = new GoogleIdentityService('server' as unknown as object, document);
    await serverService.renderButtonAsync(document.createElement('div'), vi.fn());
    serverService.disableAutoSelect();

    expect(document.querySelector(scriptSelector)).toBeNull();
    expect(vi.getTimerCount()).toBe(0);
  });

  it('does not render a button whose dialog closes while the shared library is loading', async () => {
    const controller: AbortController = new AbortController();
    const render: Promise<void> = service.renderButtonAsync(document.createElement('div'), vi.fn(), controller.signal);
    controller.abort();
    window.google = googleApi;
    document.querySelector(scriptSelector)?.dispatchEvent(new Event('load'));
    await render;

    expect(googleApi.accounts.id.initialize).not.toHaveBeenCalled();
    expect(googleApi.accounts.id.renderButton).not.toHaveBeenCalled();
    expect(vi.getTimerCount()).toBe(0);
  });

  it('does not deliver an old popup response to a reopened dialog', async () => {
    window.google = googleApi;
    const firstController: AbortController = new AbortController();
    const firstCallback = vi.fn();
    const secondCallback = vi.fn();
    await service.renderButtonAsync(document.createElement('div'), firstCallback, firstController.signal);
    const firstState: string | undefined = vi.mocked(googleApi.accounts.id.renderButton).mock.calls[0][1].state;
    firstController.abort();
    await service.renderButtonAsync(document.createElement('div'), secondCallback, new AbortController().signal);
    const secondState: string | undefined = vi.mocked(googleApi.accounts.id.renderButton).mock.calls[1][1].state;
    const configuration: GoogleIdConfiguration = vi.mocked(googleApi.accounts.id.initialize).mock.calls[0][0];

    expect(firstState).not.toBe(secondState);
    configuration.callback({ credential: 'old-credential', select_by: 'btn', state: firstState });
    expect(firstCallback).not.toHaveBeenCalled();
    expect(secondCallback).not.toHaveBeenCalled();
    configuration.callback({ credential: 'new-credential', select_by: 'btn', state: secondState });
    expect(secondCallback).toHaveBeenCalledOnce();
  });

  it('ignores credential responses without a recognized button state', async () => {
    window.google = googleApi;
    const callback = vi.fn();
    await service.renderButtonAsync(document.createElement('div'), callback);
    const configuration: GoogleIdConfiguration = vi.mocked(googleApi.accounts.id.initialize).mock.calls[0][0];
    configuration.callback({ credential: 'test-credential', select_by: 'btn' });
    configuration.callback({ credential: 'test-credential', select_by: 'btn', state: 'unknown' });

    expect(callback).not.toHaveBeenCalled();
  });
});
