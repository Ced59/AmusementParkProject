import { TestBed } from '@angular/core/testing';

import { ThemeService } from './theme.service';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';

describe('ThemeService', () => {
  let service: ThemeService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: provideCommonTestDependencies(),
    });

    service = TestBed.inject(ThemeService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('can initialize the system theme when reading storage is blocked', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('Storage is blocked');
    });

    expect(() => service.initializeTheme()).not.toThrow();
    expect(document.documentElement.classList.contains(`${service.getCurrentTheme()}-mode`)).toBe(true);
  });

  it('can change the visible theme when persisting storage is blocked', () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('Storage is blocked');
    });

    expect(() => service.changeTheme('light')).not.toThrow();
    expect(service.getCurrentTheme()).toBe('light');
    expect(document.documentElement.classList.contains('light-mode')).toBe(true);
    expect(document.body.classList.contains('dark-mode')).toBe(false);
  });
});
