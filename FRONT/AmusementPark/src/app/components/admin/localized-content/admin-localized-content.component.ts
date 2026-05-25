import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import { LocalizedContentApiService, LocalizedContentApplyResult, LocalizedContentEntityType, LocalizedContentTarget } from '@data-access/admin/localized-content-api.service';
import { unwrapPagedCollection } from '@data-access/shared/api-helpers';
import { PagedResult } from '@shared/models/contracts';

interface LocalizedEntityTypeOption {
  readonly value: LocalizedContentEntityType;
  readonly labelKey: string;
  readonly hintKey: string;
}

@Component({
  selector: 'app-admin-localized-content',
  templateUrl: './admin-localized-content.component.html',
  styleUrl: './admin-localized-content.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, TranslateModule]
})
export class AdminLocalizedContentComponent {
  protected readonly entityTypes: readonly LocalizedEntityTypeOption[] = [
    { value: 'park', labelKey: 'admin.localizedContent.entityTypes.park', hintKey: 'admin.localizedContent.entityHints.park' },
    { value: 'parkZone', labelKey: 'admin.localizedContent.entityTypes.parkZone', hintKey: 'admin.localizedContent.entityHints.parkZone' },
    { value: 'parkItem', labelKey: 'admin.localizedContent.entityTypes.parkItem', hintKey: 'admin.localizedContent.entityHints.parkItem' },
    { value: 'parkOperator', labelKey: 'admin.localizedContent.entityTypes.parkOperator', hintKey: 'admin.localizedContent.entityHints.parkOperator' },
    { value: 'parkFounder', labelKey: 'admin.localizedContent.entityTypes.parkFounder', hintKey: 'admin.localizedContent.entityHints.parkFounder' },
    { value: 'attractionManufacturer', labelKey: 'admin.localizedContent.entityTypes.attractionManufacturer', hintKey: 'admin.localizedContent.entityHints.attractionManufacturer' },
    { value: 'image', labelKey: 'admin.localizedContent.entityTypes.image', hintKey: 'admin.localizedContent.entityHints.image' },
    { value: 'imageTag', labelKey: 'admin.localizedContent.entityTypes.imageTag', hintKey: 'admin.localizedContent.entityHints.imageTag' }
  ];

  protected selectedEntityType: LocalizedContentEntityType = 'parkItem';
  protected searchTerm: string = '';
  protected jsonText: string = this.buildDefaultJson(this.selectedEntityType);
  protected targets: readonly LocalizedContentTarget[] = [];
  protected selectedTarget: LocalizedContentTarget | null = null;
  protected isSearching: boolean = false;
  protected isApplying: boolean = false;
  protected successMessage: string | null = null;
  protected errorMessage: string | null = null;

  constructor(
    private readonly localizedContentApi: LocalizedContentApiService,
    private readonly changeDetectorRef: ChangeDetectorRef) {
  }

  protected searchTargets(): void {
    this.isSearching = true;
    this.errorMessage = null;
    this.successMessage = null;
    this.selectedTarget = null;
    this.localizedContentApi.searchTargets(this.selectedEntityType, this.searchTerm, 1, 20).subscribe({
      next: (response) => {
        const page: PagedResult<LocalizedContentTarget> = unwrapPagedCollection<LocalizedContentTarget>(response);
        this.targets = page.items;
        this.isSearching = false;
        this.changeDetectorRef.markForCheck();
      },
      error: () => {
        this.targets = [];
        this.isSearching = false;
        this.errorMessage = 'admin.localizedContent.messages.searchError';
        this.changeDetectorRef.markForCheck();
      }
    });
  }

  protected selectTarget(target: LocalizedContentTarget): void {
    this.selectedTarget = target;
    this.successMessage = null;
    this.errorMessage = null;
  }

  protected applyJson(): void {
    if (!this.selectedTarget) {
      this.errorMessage = 'admin.localizedContent.messages.selectTargetFirst';
      return;
    }

    let parsedJson: unknown;
    try {
      parsedJson = JSON.parse(this.jsonText);
    } catch {
      this.errorMessage = 'admin.localizedContent.messages.invalidJson';
      return;
    }

    this.isApplying = true;
    this.errorMessage = null;
    this.successMessage = null;
    this.localizedContentApi.applyJson(this.selectedTarget.entityType, this.selectedTarget.entityId, parsedJson).subscribe({
      next: (result: LocalizedContentApplyResult) => {
        this.isApplying = false;
        this.successMessage = result.updatedFields.length > 0
          ? 'admin.localizedContent.messages.applySuccess'
          : 'admin.localizedContent.messages.applyNoField';
        this.changeDetectorRef.markForCheck();
      },
      error: () => {
        this.isApplying = false;
        this.errorMessage = 'admin.localizedContent.messages.applyError';
        this.changeDetectorRef.markForCheck();
      }
    });
  }

  protected resetExample(): void {
    this.jsonText = this.buildDefaultJson(this.selectedEntityType);
    this.successMessage = null;
    this.errorMessage = null;
  }

  protected onEntityTypeChanged(): void {
    this.targets = [];
    this.selectedTarget = null;
    this.successMessage = null;
    this.errorMessage = null;
    this.jsonText = this.buildDefaultJson(this.selectedEntityType);
  }

  private buildDefaultJson(entityType: LocalizedContentEntityType): string {
    let payload: unknown;

    switch (entityType) {
      case 'parkZone':
        payload = {
          names: {
            fr: 'Nom de la zone',
            en: 'Zone name'
          },
          descriptions: this.buildDescriptionValues()
        };
        break;
      case 'parkOperator':
        payload = {
          description: this.buildDescriptionValues()
        };
        break;
      case 'parkFounder':
      case 'attractionManufacturer':
        payload = {
          biography: this.buildDescriptionValues()
        };
        break;
      case 'image':
        payload = {
          altTexts: {
            fr: 'Texte alternatif en français',
            en: 'English alternative text'
          },
          captions: this.buildDescriptionValues(),
          credits: {
            fr: 'Crédit photo',
            en: 'Photo credit'
          }
        };
        break;
      case 'imageTag':
        payload = {
          labels: {
            fr: 'Libellé du tag',
            en: 'Tag label'
          },
          descriptions: this.buildDescriptionValues()
        };
        break;
      case 'parkItem':
        payload = {
          descriptions: this.buildDescriptionValues(),
          accessConditions: [
            {
              type: 'MinHeight',
              label: [
                { languageCode: 'fr', value: 'Taille minimale' },
                { languageCode: 'en', value: 'Minimum height' }
              ],
              description: [
                { languageCode: 'fr', value: 'Accès soumis à une taille minimale.' },
                { languageCode: 'en', value: 'Access is subject to a minimum height.' }
              ]
            }
          ]
        };
        break;
      case 'park':
      default:
        payload = {
          descriptions: this.buildDescriptionValues()
        };
        break;
    }

    return JSON.stringify(payload, null, 2);
  }

  private buildDescriptionValues(): readonly { readonly languageCode: string; readonly value: string }[] {
    return [
      { languageCode: 'fr', value: '<p>Description en français.</p>' },
      { languageCode: 'en', value: '<p>English description.</p>' }
    ];
  }
}
