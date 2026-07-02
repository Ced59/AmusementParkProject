import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';

import { MessageService } from './api';
import { Toast } from './toast';

describe('Toast', () => {
  let fixture: ComponentFixture<Toast>;
  let component: Toast;
  let messageService: MessageService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Toast]
    }).compileComponents();

    fixture = TestBed.createComponent(Toast);
    component = fixture.componentInstance;
    messageService = TestBed.inject(MessageService);
    fixture.detectChanges();
  });

  it('marks the OnPush view for check when a toast message is added', () => {
    const changeDetectorRef = (component as unknown as { changeDetectorRef: { markForCheck(): void } }).changeDetectorRef;
    spyOn(changeDetectorRef, 'markForCheck');

    messageService.add({ severity: 'success', summary: 'Saved', detail: 'Your change was saved.' });

    expect(changeDetectorRef.markForCheck).toHaveBeenCalled();
  });

  it('renders and dismisses toast messages from the shared message service', () => {
    jasmine.clock().install();

    try {
      messageService.add({ severity: 'success', summary: 'Saved', detail: 'Your change was saved.' });
      fixture.detectChanges();

      const toastElement: HTMLElement = fixture.debugElement.query(By.css('.p-toast-message')).nativeElement;
      expect(toastElement.textContent).toContain('Saved');
      expect(toastElement.textContent).toContain('Your change was saved.');
      expect(toastElement.getAttribute('role')).toBe('status');
      expect(toastElement.querySelector('.pi')).toBeNull();

      jasmine.clock().tick(5000);
      fixture.detectChanges();

      expect(fixture.debugElement.query(By.css('.p-toast-message'))).toBeNull();
    } finally {
      jasmine.clock().uninstall();
    }
  });

  it('announces error messages assertively', () => {
    messageService.add({ severity: 'error', summary: 'Error', detail: 'Unable to save.' });
    fixture.detectChanges();

    const toastElement: HTMLElement = fixture.debugElement.query(By.css('.p-toast-message')).nativeElement;

    expect(toastElement.getAttribute('role')).toBe('alert');
    expect(toastElement.getAttribute('aria-live')).toBe('assertive');
  });
});
