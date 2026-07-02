import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
  ViewChild
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonDirective } from '@shared/ui/primitives/button';
import { InputText } from '@shared/ui/primitives/inputtext';

import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';
import { ParkZone } from '@app/models/parks/park-zone';
import { EntitySelectOption } from '@app/models/shared/entity-select-option';
import {
  PARK_ITEM_CATEGORY_OPTIONS,
  PARK_ITEM_TYPE_OPTIONS,
  TranslationOption
} from '@shared/utils/display/display-options';
import {
  AdminParkItemDuplicateWarning,
  AdminParkItemQuickCreateDraft
} from '@features/admin/park-items/workbench/models/admin-park-item-workbench.model';
import { getAllowedTypesForAdminParkItemCategory } from '@features/admin/park-items/workbench/mappers/admin-park-item-quick-create.mapper';

@Component({
  selector: 'app-admin-park-item-quick-create-drawer',
  templateUrl: './admin-park-item-quick-create-drawer.component.html',
  styleUrls: ['./admin-park-item-quick-create-drawer.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ButtonDirective,
    FormsModule,
    InputText,
    TranslateModule
  ]
})
export class AdminParkItemQuickCreateDrawerComponent implements OnChanges {
  @Input() isOpen: boolean = false;
  @Input() isCreating: boolean = false;
  @Input() draft: AdminParkItemQuickCreateDraft | null = null;
  @Input() zones: ParkZone[] = [];
  @Input() manufacturerOptions: EntitySelectOption[] = [];
  @Input() duplicateWarnings: AdminParkItemDuplicateWarning[] = [];
  @Input() focusVersion: number = 0;

  @Output() draftChanged: EventEmitter<AdminParkItemQuickCreateDraft> = new EventEmitter<AdminParkItemQuickCreateDraft>();
  @Output() create: EventEmitter<AdminParkItemQuickCreateDraft> = new EventEmitter<AdminParkItemQuickCreateDraft>();
  @Output() createAndNew: EventEmitter<AdminParkItemQuickCreateDraft> = new EventEmitter<AdminParkItemQuickCreateDraft>();
  @Output() createAndOpen: EventEmitter<AdminParkItemQuickCreateDraft> = new EventEmitter<AdminParkItemQuickCreateDraft>();
  @Output() closed: EventEmitter<void> = new EventEmitter<void>();

  @ViewChild('nameInput') private readonly nameInput?: ElementRef<HTMLInputElement>;

  protected localDraft: AdminParkItemQuickCreateDraft | null = null;
  protected readonly categoryOptions: ReadonlyArray<TranslationOption<ParkItemCategory>> = PARK_ITEM_CATEGORY_OPTIONS;
  protected readonly reviewStatusOptions: ReadonlyArray<{ labelKey: string; value: AdminReviewStatus }> = [
    { labelKey: 'admin.reviewStatus.toReview', value: 'ToReview' },
    { labelKey: 'admin.reviewStatus.validated', value: 'Validated' },
    { labelKey: 'admin.reviewStatus.toProcessLater', value: 'ToProcessLater' },
    { labelKey: 'admin.reviewStatus.notRelevant', value: 'NotRelevant' }
  ];

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['draft']) {
      this.localDraft = this.draft ? { ...this.draft } : null;
    }

    if (this.isOpen && (changes['isOpen'] || changes['focusVersion'])) {
      this.focusNameInput();
    }
  }

  protected get typeOptions(): ReadonlyArray<TranslationOption<ParkItemType>> {
    const category: ParkItemCategory = this.localDraft?.category ?? 'Attraction';
    const allowedTypes: ReadonlyArray<ParkItemType> = getAllowedTypesForAdminParkItemCategory(category);

    return PARK_ITEM_TYPE_OPTIONS.filter((option: TranslationOption<ParkItemType>) => allowedTypes.includes(option.value));
  }

  protected onDrawerKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      this.closed.emit();
      return;
    }

    if (event.key !== 'Enter' || this.isCreating || !this.canSubmit()) {
      return;
    }

    event.preventDefault();
    if (event.ctrlKey || event.metaKey) {
      this.emitCreateAndNew();
      return;
    }

    this.emitCreate();
  }

  protected updateName(value: string): void {
    this.updateDraft({ name: value });
  }

  protected updateZone(value: string): void {
    this.updateDraft({ zoneId: value || null });
  }

  protected updateCategory(value: ParkItemCategory): void {
    const allowedTypes: ReadonlyArray<ParkItemType> = getAllowedTypesForAdminParkItemCategory(value);
    const currentType: ParkItemType | null | undefined = this.localDraft?.type;
    const nextType: ParkItemType = currentType && allowedTypes.includes(currentType)
      ? currentType
      : allowedTypes[0];

    this.updateDraft({ category: value, type: nextType });
  }

  protected updateType(value: ParkItemType): void {
    this.updateDraft({ type: value });
  }

  protected updateManufacturer(value: string): void {
    this.updateDraft({ manufacturerId: value || null });
  }

  protected updateVisibility(value: string): void {
    this.updateDraft({ isVisible: value === 'true' });
  }

  protected updateReviewStatus(value: AdminReviewStatus): void {
    this.updateDraft({ adminReviewStatus: value });
  }

  protected emitCreate(): void {
    if (this.localDraft && this.canSubmit()) {
      this.create.emit({ ...this.localDraft });
    }
  }

  protected emitCreateAndNew(): void {
    if (this.localDraft && this.canSubmit()) {
      this.createAndNew.emit({ ...this.localDraft });
    }
  }

  protected emitCreateAndOpen(): void {
    if (this.localDraft && this.canSubmit()) {
      this.createAndOpen.emit({ ...this.localDraft });
    }
  }

  protected canSubmit(): boolean {
    return !!this.localDraft?.name?.trim();
  }

  private updateDraft(changes: Partial<AdminParkItemQuickCreateDraft>): void {
    if (!this.localDraft) {
      return;
    }

    this.localDraft = {
      ...this.localDraft,
      ...changes
    };
    this.draftChanged.emit({ ...this.localDraft });
  }

  private focusNameInput(): void {
    setTimeout(() => this.nameInput?.nativeElement.focus(), 0);
  }
}
