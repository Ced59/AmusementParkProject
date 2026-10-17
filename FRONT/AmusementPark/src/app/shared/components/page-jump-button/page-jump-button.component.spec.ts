import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NgZone } from '@angular/core';
import { By } from '@angular/platform-browser';

import {
  COMMON_TEST_IMPORTS,
  provideCommonTestDependencies,
} from '@app/testing/common-test-providers';
import { PageJumpButtonComponent } from './page-jump-button.component';

interface ExposedPageJumpButtonComponent {
  isVisible: boolean;
  isNearPageBottom: boolean;
  labelKey: string;
  togglePageScroll(): void;
  updatePageScrollState(): void;
}

describe('PageJumpButtonComponent', () => {
  let originalScrollHeightDescriptor: PropertyDescriptor | undefined;
  let originalBodyScrollHeightDescriptor: PropertyDescriptor | undefined;
  let originalInnerHeightDescriptor: PropertyDescriptor | undefined;
  let originalScrollYDescriptor: PropertyDescriptor | undefined;
  let mobileNav: HTMLElement | undefined;
  let originalNavHeight: string;
  let originalToolbarHeight: string;
  let toolbar: HTMLElement | undefined;
  let replacementToolbar: HTMLElement | undefined;

  beforeEach(async () => {
    originalNavHeight = document.documentElement.style.getPropertyValue('--bottom-nav-height');
    originalToolbarHeight = document.documentElement.style.getPropertyValue('--app-admin-toolbar-height');
    originalScrollHeightDescriptor = Object.getOwnPropertyDescriptor(
      document.documentElement,
      'scrollHeight',
    );
    originalBodyScrollHeightDescriptor = Object.getOwnPropertyDescriptor(
      document.body,
      'scrollHeight',
    );
    originalInnerHeightDescriptor = Object.getOwnPropertyDescriptor(
      window,
      'innerHeight',
    );
    originalScrollYDescriptor = Object.getOwnPropertyDescriptor(
      window,
      'scrollY',
    );

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, PageJumpButtonComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();
  });

  afterEach(() => {
    mobileNav?.remove();
    mobileNav = undefined;
    toolbar?.remove();
    replacementToolbar?.remove();
    toolbar = undefined;
    replacementToolbar = undefined;
    document.documentElement.style.setProperty('--bottom-nav-height', originalNavHeight);
    document.documentElement.style.setProperty('--app-admin-toolbar-height', originalToolbarHeight);
    restoreProperty(
      document.documentElement,
      'scrollHeight',
      originalScrollHeightDescriptor,
    );
    restoreProperty(
      document.body,
      'scrollHeight',
      originalBodyScrollHeightDescriptor,
    );
    restoreProperty(window, 'innerHeight', originalInnerHeightDescriptor);
    restoreProperty(window, 'scrollY', originalScrollYDescriptor);
  });

  it('stays hidden when the page is not meaningfully scrollable', () => {
    const context = createComponent();
    setViewportMetrics(900, 800, 0);

    context.exposed.updatePageScrollState();
    context.fixture.detectChanges();

    expect(context.exposed.isVisible).toBe(false);
    expect(
      context.fixture.debugElement.query(By.css('.page-jump-button')),
    ).toBeNull();
  });

  it('shows a bottom jump action until the user reaches the page end', () => {
    const context = createComponent();
    setViewportMetrics(2000, 800, 0);

    context.exposed.updatePageScrollState();
    context.fixture.detectChanges();

    let button: HTMLElement = context.fixture.debugElement.query(
      By.css('.page-jump-button'),
    ).nativeElement;
    expect(context.exposed.isVisible).toBe(true);
    expect(context.exposed.isNearPageBottom).toBe(false);
    expect(context.exposed.labelKey).toBe('actions.scrollBottom');
    expect(button.querySelector('.pi-arrow-down')).not.toBeNull();

    setViewportMetrics(2000, 800, 1080);
    context.exposed.updatePageScrollState();
    context.fixture.detectChanges();

    button = context.fixture.debugElement.query(
      By.css('.page-jump-button'),
    ).nativeElement;
    expect(context.exposed.isNearPageBottom).toBe(true);
    expect(context.exposed.labelKey).toBe('actions.scrollTop');
    expect(button.querySelector('.pi-arrow-up')).not.toBeNull();
  });

  it('scrolls to the page bottom or top from the floating action', () => {
    const context = createComponent();
    const scrollToSpy = vi.spyOn(window, 'scrollTo');
    setViewportMetrics(2000, 800, 0);

    context.exposed.isNearPageBottom = false;
    context.exposed.togglePageScroll();

    const firstScrollOptions: unknown = vi.mocked(scrollToSpy).mock.calls[0][0];
    expect(firstScrollOptions).toEqual({
      top: 2000,
      behavior: 'smooth',
    } as ScrollToOptions);

    context.exposed.isNearPageBottom = true;
    context.exposed.togglePageScroll();

    const secondScrollOptions: unknown =
      vi.mocked(scrollToSpy).mock.calls[1][0];
    expect(secondScrollOptions).toEqual({
      top: 0,
      behavior: 'smooth',
    } as ScrollToOptions);
  });

  it('tracks the actual wrapped mobile navigation without repeated style writes', () => {
    const context = createComponent();
    mobileNav = document.createElement('nav');
    mobileNav.className = 'app-mobile-bottom-nav';
    document.body.append(mobileNav);
    const height = vi.spyOn(mobileNav, 'offsetHeight', 'get').mockReturnValue(262);
    const style = document.documentElement.style;
    const setProperty = vi.spyOn(style, 'setProperty');

    context.exposed.updatePageScrollState();
    expect(style.getPropertyValue('--bottom-nav-height')).toBe('262px');
    setProperty.mockClear();
    context.exposed.updatePageScrollState();
    expect(setProperty).not.toHaveBeenCalled();

    height.mockReturnValue(144);
    context.exposed.updatePageScrollState();
    expect(style.getPropertyValue('--bottom-nav-height')).toBe('144px');
    height.mockReturnValue(0);
    context.exposed.updatePageScrollState();
    expect(style.getPropertyValue('--bottom-nav-height')).toBe('');
  });

  it('removes its measured navigation height when destroyed', () => {
    const context = createComponent();
    document.documentElement.style.setProperty('--bottom-nav-height', '262px');
    document.documentElement.style.setProperty('--app-admin-toolbar-height', '187px');
    context.fixture.destroy();
    expect(document.documentElement.style.getPropertyValue('--bottom-nav-height')).toBe('');
    expect(document.documentElement.style.getPropertyValue('--app-admin-toolbar-height')).toBe('');
  });

  it('reuses its resize observer for a lazy toolbar and releases replaced DOM targets', () => {
    const observer = {
      observe: vi.fn(), unobserve: vi.fn(), disconnect: vi.fn(),
    };
    vi.stubGlobal('ResizeObserver', vi.fn(function (): ResizeObserver {
      return observer as unknown as ResizeObserver;
    }));
    const context = createComponent();
    try {
      context.fixture.detectChanges();
      toolbar = document.createElement('app-admin-public-view-toolbar');
      document.body.append(toolbar);
      vi.spyOn(toolbar, 'offsetHeight', 'get').mockReturnValue(187);
      context.exposed.updatePageScrollState();
      expect(observer.observe).toHaveBeenCalledWith(toolbar);
      expect(document.documentElement.style.getPropertyValue('--app-admin-toolbar-height')).toBe('187px');
      observer.observe.mockClear();
      context.exposed.updatePageScrollState();
      expect(observer.observe).not.toHaveBeenCalled();

      toolbar.remove();
      replacementToolbar = document.createElement('app-admin-public-view-toolbar');
      document.body.append(replacementToolbar);
      vi.spyOn(replacementToolbar, 'offsetHeight', 'get').mockReturnValue(44);
      context.exposed.updatePageScrollState();
      expect(observer.unobserve).toHaveBeenCalledWith(toolbar);
      expect(observer.observe).toHaveBeenCalledWith(replacementToolbar);
      expect(document.documentElement.style.getPropertyValue('--app-admin-toolbar-height')).toBe('44px');

      replacementToolbar.remove();
      context.exposed.updatePageScrollState();
      expect(observer.unobserve).toHaveBeenCalledWith(replacementToolbar);
      expect(document.documentElement.style.getPropertyValue('--app-admin-toolbar-height')).toBe('');
      context.fixture.destroy();
      expect(observer.disconnect).toHaveBeenCalledOnce();
    } finally {
      if (!context.fixture.componentRef.hostView.destroyed) {
        context.fixture.destroy();
      }
      vi.unstubAllGlobals();
    }
  });

  it('registers passive viewport listeners outside Angular and cleans them up', () => {
    const ngZone: NgZone = TestBed.inject(NgZone);
    const runOutsideAngularSpy = vi.spyOn(ngZone, 'runOutsideAngular');
    const addEventListenerSpy = vi.spyOn(window, 'addEventListener');
    const removeEventListenerSpy = vi.spyOn(window, 'removeEventListener');
    const context = createComponent();

    context.fixture.detectChanges();

    const scrollListener: EventListenerOrEventListenerObject =
      findWindowListener(addEventListenerSpy, 'scroll');
    const resizeListener: EventListenerOrEventListenerObject =
      findWindowListener(addEventListenerSpy, 'resize');

    expect(runOutsideAngularSpy).toHaveBeenCalled();
    expect(findWindowListenerOptions(addEventListenerSpy, 'scroll')).toEqual({
      passive: true,
    });
    expect(findWindowListenerOptions(addEventListenerSpy, 'resize')).toEqual({
      passive: true,
    });

    context.fixture.destroy();

    const scrollRemoveArgs: Parameters<typeof window.removeEventListener> =
      findWindowRemoveListenerArgs(removeEventListenerSpy, 'scroll');
    const resizeRemoveArgs: Parameters<typeof window.removeEventListener> =
      findWindowRemoveListenerArgs(removeEventListenerSpy, 'resize');
    expect(scrollRemoveArgs[1]).toBe(scrollListener);
    expect(scrollRemoveArgs[2] as unknown).toEqual({
      passive: true,
    } as AddEventListenerOptions);
    expect(resizeRemoveArgs[1]).toBe(resizeListener);
    expect(resizeRemoveArgs[2] as unknown).toEqual({
      passive: true,
    } as AddEventListenerOptions);
  });
});

function createComponent(): {
  fixture: ComponentFixture<PageJumpButtonComponent>;
  exposed: ExposedPageJumpButtonComponent;
} {
  const fixture: ComponentFixture<PageJumpButtonComponent> =
    TestBed.createComponent(PageJumpButtonComponent);
  return {
    fixture,
    exposed:
      fixture.componentInstance as unknown as ExposedPageJumpButtonComponent,
  };
}

function setViewportMetrics(
  scrollHeight: number,
  innerHeight: number,
  scrollY: number,
): void {
  Object.defineProperty(document.documentElement, 'scrollHeight', {
    configurable: true,
    value: scrollHeight,
  });
  Object.defineProperty(document.body, 'scrollHeight', {
    configurable: true,
    value: scrollHeight,
  });
  Object.defineProperty(window, 'innerHeight', {
    configurable: true,
    value: innerHeight,
  });
  Object.defineProperty(window, 'scrollY', {
    configurable: true,
    value: scrollY,
  });
}

function restoreProperty(
  target: object,
  propertyName: string,
  descriptor: PropertyDescriptor | undefined,
): void {
  if (descriptor) {
    Object.defineProperty(target, propertyName, descriptor);
    return;
  }

  delete (target as Record<string, unknown>)[propertyName];
}

function findWindowListener(
  spy: Mock,
  eventName: string,
): EventListenerOrEventListenerObject {
  const calls = vi.mocked(spy).mock.calls as unknown as Array<
    Parameters<typeof window.addEventListener>
  >;
  const args: Parameters<typeof window.addEventListener> | undefined =
    calls.find((callArgs) => callArgs[0] === eventName);

  if (!args) {
    throw new Error(`Missing ${eventName} listener.`);
  }

  return args[1];
}

function findWindowListenerOptions(
  spy: Mock,
  eventName: string,
): boolean | AddEventListenerOptions | undefined {
  const calls = vi.mocked(spy).mock.calls as unknown as Array<
    Parameters<typeof window.addEventListener>
  >;
  const args: Parameters<typeof window.addEventListener> | undefined =
    calls.find((callArgs) => callArgs[0] === eventName);

  if (!args) {
    throw new Error(`Missing ${eventName} listener options.`);
  }

  return args[2];
}

function findWindowRemoveListenerArgs(
  spy: Mock,
  eventName: string,
): Parameters<typeof window.removeEventListener> {
  const calls = vi.mocked(spy).mock.calls as unknown as Array<
    Parameters<typeof window.removeEventListener>
  >;
  const args: Parameters<typeof window.removeEventListener> | undefined =
    calls.find((callArgs) => callArgs[0] === eventName);

  if (!args) {
    throw new Error(`Missing ${eventName} remove listener.`);
  }

  return args;
}
