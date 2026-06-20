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
        lead: 'Version actuelle : {{version}}. Retrouvez les nouveautes visibles du site au fil des mises a jour.',
        timelineAriaLabel: 'Historique des versions',
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

  it('marks version milestones and fixes with separate indentation levels', () => {
    const host: HTMLElement = fixture.nativeElement as HTMLElement;

    expect(host.querySelector('.version-entry--minor')).not.toBeNull();
    expect(host.querySelector('.version-entry--patch')).not.toBeNull();
    expect(host.querySelector('.version-entry--minor.version-entry--indented')).not.toBeNull();
    expect(host.querySelector('.version-entry--patch.version-entry--indented')).not.toBeNull();
  });
});
