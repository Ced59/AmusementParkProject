import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EventEmitter } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { TranslationService } from '@app/services/translation.service';
import { VersionHistoryPageComponent } from './version-history-page.component';

describe('VersionHistoryPageComponent', () => {
  let fixture: ComponentFixture<VersionHistoryPageComponent>;

  beforeEach(async () => {
    const translationService: jasmine.SpyObj<TranslationService> = jasmine.createSpyObj<TranslationService>(
      'TranslationService',
      ['getCurrentLang', 'useLang'],
      { languageChanged: new EventEmitter<string>() }
    );
    translationService.getCurrentLang.and.returnValue('fr');
    translationService.useLang.and.returnValue(of(null));

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, VersionHistoryPageComponent],
      providers: [
        ...provideCommonTestDependencies(),
        { provide: TranslationService, useValue: translationService }
      ],
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('fr', {
      versionHistoryPage: {
        kicker: 'Historique',
        title: 'Ce qui a change',
        lead: 'Version actuelle : {{version}}. Retrouve les nouveautes visibles du site au fil des mises a jour.',
        timelineAriaLabel: 'Historique des versions',
        loading: 'Chargement des versions...',
        loadError: 'Impossible de charger l historique.',
        actions: {
          expand: 'Voir {{count}}',
          collapse: 'Reduire'
        },
        levels: {
          major: 'Majeure',
          minor: 'Palier',
          patch: 'Mise a jour'
        }
      }
    });
    translateService.use('fr');

    fixture = TestBed.createComponent(VersionHistoryPageComponent);
    fixture.detectChanges();
  });

  it('marks version milestones and fixes with separate indentation levels', async () => {
    await settleVersionHistory(fixture);
    await expandFirstMajorWithChildren(fixture);
    await expandFirstMinorWithChildren(fixture);

    const host: HTMLElement = fixture.nativeElement as HTMLElement;

    expect(host.querySelector('.version-entry--major')).not.toBeNull();
    expect(host.querySelector('.version-entry--minor')).not.toBeNull();
    expect(host.querySelector('.version-entry--patch')).not.toBeNull();
    expect(host.querySelector('.version-entry--minor.version-entry--indented')).not.toBeNull();
    expect(host.querySelector('.version-entry--patch.version-entry--indented')).not.toBeNull();
  });

  it('collapses patches for an expanded milestone', async () => {
    await settleVersionHistory(fixture);
    await expandFirstMajorWithChildren(fixture);
    await expandFirstMinorWithChildren(fixture);

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const patchVersionsBeforeCollapse: string[] = getPatchVersions(host);

    expect(patchVersionsBeforeCollapse.length).toBeGreaterThan(0);

    const currentMajorToggle: HTMLButtonElement | null = host.querySelector(
      '.version-entry--major.version-entry--expanded .version-entry__toggle'
    );
    expect(currentMajorToggle).not.toBeNull();

    currentMajorToggle?.click();
    fixture.detectChanges();

    expect(getPatchVersions(host).length).toBe(0);
  });
});

async function settleVersionHistory(fixture: ComponentFixture<VersionHistoryPageComponent>): Promise<void> {
  await waitForVersionHistorySelector(fixture, '.version-entry--major');
}

async function expandFirstMajorWithChildren(fixture: ComponentFixture<VersionHistoryPageComponent>): Promise<void> {
  const host: HTMLElement = fixture.nativeElement as HTMLElement;
  const majorToggle: HTMLButtonElement | null = host.querySelector<HTMLButtonElement>(
    '.version-entry--major .version-entry__toggle'
  );

  expect(majorToggle).not.toBeNull();

  majorToggle?.click();
  fixture.detectChanges();

  await waitForVersionHistorySelector(fixture, '.version-entry--minor');
}

async function expandFirstMinorWithChildren(fixture: ComponentFixture<VersionHistoryPageComponent>): Promise<void> {
  const host: HTMLElement = fixture.nativeElement as HTMLElement;
  const minorToggle: HTMLButtonElement | null = host.querySelector<HTMLButtonElement>(
    '.version-entry--minor .version-entry__toggle'
  );

  expect(minorToggle).not.toBeNull();

  minorToggle?.click();
  fixture.detectChanges();

  await waitForVersionHistorySelector(fixture, '.version-entry--patch');
}

async function waitForVersionHistorySelector(
  fixture: ComponentFixture<VersionHistoryPageComponent>,
  selector: string
): Promise<void> {
  for (let attempt = 0; attempt < 20; attempt += 1) {
    await fixture.whenStable();
    await new Promise<void>((resolve: () => void): void => {
      window.setTimeout(resolve, 0);
    });
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    if (host.querySelector(selector)) {
      return;
    }
  }
}

function getPatchVersions(host: HTMLElement): string[] {
  return Array.from(host.querySelectorAll('.version-entry--patch strong'))
    .map((element: Element) => element.textContent?.trim() ?? '')
    .filter((value: string) => value.length > 0);
}
