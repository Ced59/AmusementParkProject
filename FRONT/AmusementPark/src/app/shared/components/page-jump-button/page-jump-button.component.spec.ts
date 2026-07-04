import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
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

  beforeEach(async () => {
    originalScrollHeightDescriptor = Object.getOwnPropertyDescriptor(document.documentElement, 'scrollHeight');
    originalBodyScrollHeightDescriptor = Object.getOwnPropertyDescriptor(document.body, 'scrollHeight');
    originalInnerHeightDescriptor = Object.getOwnPropertyDescriptor(window, 'innerHeight');
    originalScrollYDescriptor = Object.getOwnPropertyDescriptor(window, 'scrollY');

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, PageJumpButtonComponent],
      providers: provideCommonTestDependencies()
    }).compileComponents();
  });

  afterEach(() => {
    restoreProperty(document.documentElement, 'scrollHeight', originalScrollHeightDescriptor);
    restoreProperty(document.body, 'scrollHeight', originalBodyScrollHeightDescriptor);
    restoreProperty(window, 'innerHeight', originalInnerHeightDescriptor);
    restoreProperty(window, 'scrollY', originalScrollYDescriptor);
  });

  it('stays hidden when the page is not meaningfully scrollable', () => {
    const context = createComponent();
    setViewportMetrics(900, 800, 0);

    context.exposed.updatePageScrollState();
    context.fixture.detectChanges();

    expect(context.exposed.isVisible).toBeFalse();
    expect(context.fixture.debugElement.query(By.css('.page-jump-button'))).toBeNull();
  });

  it('shows a bottom jump action until the user reaches the page end', () => {
    const context = createComponent();
    setViewportMetrics(2000, 800, 0);

    context.exposed.updatePageScrollState();
    context.fixture.detectChanges();

    let button: HTMLElement = context.fixture.debugElement.query(By.css('.page-jump-button')).nativeElement;
    expect(context.exposed.isVisible).toBeTrue();
    expect(context.exposed.isNearPageBottom).toBeFalse();
    expect(context.exposed.labelKey).toBe('actions.scrollBottom');
    expect(button.querySelector('.pi-arrow-down')).not.toBeNull();

    setViewportMetrics(2000, 800, 1080);
    context.exposed.updatePageScrollState();
    context.fixture.detectChanges();

    button = context.fixture.debugElement.query(By.css('.page-jump-button')).nativeElement;
    expect(context.exposed.isNearPageBottom).toBeTrue();
    expect(context.exposed.labelKey).toBe('actions.scrollTop');
    expect(button.querySelector('.pi-arrow-up')).not.toBeNull();
  });

  it('scrolls to the page bottom or top from the floating action', () => {
    const context = createComponent();
    const scrollToSpy = spyOn(window, 'scrollTo');
    setViewportMetrics(2000, 800, 0);

    context.exposed.isNearPageBottom = false;
    context.exposed.togglePageScroll();

    const firstScrollOptions: unknown = scrollToSpy.calls.argsFor(0)[0];
    expect(firstScrollOptions).toEqual({
      top: 2000,
      behavior: 'smooth'
    } as ScrollToOptions);

    context.exposed.isNearPageBottom = true;
    context.exposed.togglePageScroll();

    const secondScrollOptions: unknown = scrollToSpy.calls.argsFor(1)[0];
    expect(secondScrollOptions).toEqual({
      top: 0,
      behavior: 'smooth'
    } as ScrollToOptions);
  });
});

function createComponent(): {
  fixture: ComponentFixture<PageJumpButtonComponent>;
  exposed: ExposedPageJumpButtonComponent;
} {
  const fixture: ComponentFixture<PageJumpButtonComponent> = TestBed.createComponent(PageJumpButtonComponent);
  return {
    fixture,
    exposed: fixture.componentInstance as unknown as ExposedPageJumpButtonComponent
  };
}

function setViewportMetrics(scrollHeight: number, innerHeight: number, scrollY: number): void {
  Object.defineProperty(document.documentElement, 'scrollHeight', {
    configurable: true,
    value: scrollHeight
  });
  Object.defineProperty(document.body, 'scrollHeight', {
    configurable: true,
    value: scrollHeight
  });
  Object.defineProperty(window, 'innerHeight', {
    configurable: true,
    value: innerHeight
  });
  Object.defineProperty(window, 'scrollY', {
    configurable: true,
    value: scrollY
  });
}

function restoreProperty(target: object, propertyName: string, descriptor: PropertyDescriptor | undefined): void {
  if (descriptor) {
    Object.defineProperty(target, propertyName, descriptor);
    return;
  }

  delete (target as Record<string, unknown>)[propertyName];
}
