import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { UserDto } from '@app/models/users/user_dto';
import { AdminUsersViewComponent } from './admin-users-view.component';

describe('AdminUsersViewComponent', () => {
  let fixture: ComponentFixture<AdminUsersViewComponent>;
  let component: AdminUsersViewComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AdminUsersViewComponent],
      providers: provideCommonTestDependencies()
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('en', {
      admin: {
        users: {
          columns: {
            actions: 'Actions',
            email: 'Email',
            firstName: 'First name',
            lastName: 'Last name',
            roles: 'Roles',
            status: 'Status'
          },
          empty: 'No users',
          manage: 'Manage',
          status: {
            active: 'Active',
            blocked: 'Blocked'
          },
          title: 'Users',
          subtitle: 'Manage users'
        }
      },
      actions: {
        edit: 'Edit'
      },
      common: {
        emptyTitle: 'Nothing here'
      }
    });
    translateService.use('en');

    fixture = TestBed.createComponent(AdminUsersViewComponent);
    component = fixture.componentInstance;
    component.users = signal<UserDto[]>([createUser()]);
    component.loading = signal<boolean>(false);
    component.totalRecords = signal<number>(1);
    component.pageSize = signal<number>(10);
    component.currentPage = signal<number>(0);
    component.canShowHeaderTotal = signal<boolean>(false);
    component.resolveAvatarUrlFn = () => '/api/images/avatar-user-1';
  });

  it('renders user avatars with descriptive alt text', () => {
    fixture.detectChanges();

    const avatarImage: HTMLImageElement | null = (fixture.nativeElement as HTMLElement).querySelector('.admin-users-avatar');
    expect(avatarImage).not.toBeNull();
    expect(avatarImage?.getAttribute('src')).toBe('/api/images/avatar-user-1');
    expect(avatarImage?.getAttribute('alt')).toBe('Ced Caudron avatar');
  });
});

function createUser(): UserDto {
  return {
    id: 'user-1',
    email: 'user@example.com',
    firstName: 'Ced',
    lastName: 'Caudron',
    isActivated: true,
    isBlocked: false,
    roles: ['ADMIN'],
    preferredLanguage: 'EN',
    preferredMeasurementSystem: 'Metric',
    avatarUrl: 'avatars/user-1.png',
    createdAt: '2026-06-18T00:00:00Z',
    updatedAt: '2026-06-18T00:00:00Z'
  };
}
