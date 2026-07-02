import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { AppComponent } from './app.component';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { ToastMessageService } from '@app/services/messages/toast-message.service';

describe('AppComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AppComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;

    expect(app).toBeTruthy();
  });

  it('should expose the application title', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;

    expect(app.title).toEqual('Amusement Parks');
  });

  it('renders global toast notifications from the application message service', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const toastMessageService: ToastMessageService = TestBed.inject(ToastMessageService);

    fixture.detectChanges();
    toastMessageService.add('success', 'Saved', 'Your change was saved.');
    fixture.detectChanges();

    const toastElement: HTMLElement = fixture.debugElement.query(By.css('.p-toast-message')).nativeElement;

    expect(toastElement.textContent).toContain('Saved');
    expect(toastElement.textContent).toContain('Your change was saved.');
    expect(toastElement.getAttribute('role')).toBe('status');
  });
});
