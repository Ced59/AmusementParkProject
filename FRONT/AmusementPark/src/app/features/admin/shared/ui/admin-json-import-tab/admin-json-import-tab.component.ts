import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import { LocalizedContentApiService, LocalizedContentApplyResult, LocalizedContentEntityType } from '@data-access/admin/localized-content-api.service';

@Component({
  selector: 'app-admin-json-import-tab',
  templateUrl: './admin-json-import-tab.component.html',
  styleUrls: ['./admin-json-import-tab.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, TranslateModule]
})
export class AdminJsonImportTabComponent {
  @Input({ required: true }) entityType!: LocalizedContentEntityType;
  @Input() entityId: string | null = null;
  @Input() exampleJson: string = '{\n  "descriptions": []\n}';
  @Input() disabledReasonKey: string = 'admin.jsonImport.disabledUntilSave';

  @Output() applied: EventEmitter<LocalizedContentApplyResult> = new EventEmitter<LocalizedContentApplyResult>();

  protected jsonText: string = '';
  protected isApplying: boolean = false;
  protected errorMessage: string | null = null;
  protected successMessage: string | null = null;

  constructor(
    private readonly localizedContentApiService: LocalizedContentApiService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
  }

  ngOnChanges(): void {
    if (!this.jsonText.trim()) {
      this.jsonText = this.exampleJson;
    }
  }

  protected resetExample(): void {
    this.jsonText = this.exampleJson;
    this.errorMessage = null;
    this.successMessage = null;
  }

  protected applyJson(): void {
    if (!this.entityId) {
      this.errorMessage = this.disabledReasonKey;
      this.successMessage = null;
      return;
    }

    let parsedJson: unknown;
    try {
      parsedJson = JSON.parse(this.jsonText);
    } catch {
      this.errorMessage = 'admin.jsonImport.messages.invalidJson';
      this.successMessage = null;
      return;
    }

    this.isApplying = true;
    this.errorMessage = null;
    this.successMessage = null;
    this.localizedContentApiService.applyJson(this.entityType, this.entityId, parsedJson).subscribe({
      next: (result: LocalizedContentApplyResult): void => {
        this.isApplying = false;
        this.successMessage = 'admin.jsonImport.messages.success';
        this.applied.emit(result);
        this.changeDetectorRef.markForCheck();
      },
      error: (): void => {
        this.isApplying = false;
        this.errorMessage = 'admin.jsonImport.messages.error';
        this.changeDetectorRef.markForCheck();
      }
    });
  }
}
