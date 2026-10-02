import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GoogleIdentityService } from '@app/services/auth/google-identity.service';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { AuthModalComponent } from './auth-modal.component';

describe('AuthModalComponent Google loading lifecycle', () => {
  let fixture: ComponentFixture<AuthModalComponent>;
  let rejectRender: (reason: Error) => void;
  let renderRequest: Promise<void>;
  let renderButtonAsync: ReturnType<typeof vi.fn>;
  let addToast: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    renderRequest = new Promise<void>((_resolve, reject): void => {
      rejectRender = reject;
    });
    renderButtonAsync = vi.fn().mockReturnValue(renderRequest);
    addToast = vi.fn();
    TestBed.overrideComponent(AuthModalComponent, {
      set: { template: '<div #googleButtonContainer></div>' }
    });
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AuthModalComponent],
      providers: [
        ...provideCommonTestDependencies(),
        { provide: GoogleIdentityService, useValue: { renderButtonAsync } },
        { provide: ToastMessageService, useValue: { add: addToast } }
      ]
    }).compileComponents();
    fixture = TestBed.createComponent(AuthModalComponent);
    fixture.detectChanges();
  });

  it('cancels a closed dialog and suppresses its delayed SDK error', async () => {
    const signal: AbortSignal = renderButtonAsync.mock.calls[0][2];
    expect(signal.aborted).toBe(false);
    fixture.destroy();
    expect(signal.aborted).toBe(true);
    rejectRender(new Error('Google unavailable'));
    await renderRequest.catch((): void => {});
    await fixture.whenStable();

    expect(addToast).not.toHaveBeenCalled();
  });

  it('still reports an SDK failure to the currently open dialog', async () => {
    const logError = vi.spyOn(console, 'error').mockImplementation((): void => {});
    rejectRender(new Error('Google unavailable'));
    await renderRequest.catch((): void => {});
    await fixture.whenStable();

    expect(addToast).toHaveBeenCalledOnce();
    logError.mockRestore();
  });
});
