import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { LocalizedRichTextEditorComponent } from '@app/components/shared/localized-rich-text-editor/localized-rich-text-editor.component';
import { Bind } from 'primeng/bind';
import { ButtonDirective } from 'primeng/button';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-admin-park-descriptions-tab',
    templateUrl: './admin-park-descriptions-tab.component.html',
    styleUrls: ['./admin-park-descriptions-tab.component.scss'],
    imports: [FormsModule, ReactiveFormsModule, LocalizedRichTextEditorComponent, Bind, ButtonDirective, TranslateModule]
})
export class AdminParkDescriptionsTabComponent {
  @Input({ required: true }) form!: FormGroup;
  @Input() isSaving: boolean = false;

  @Output() saveSection: EventEmitter<void> = new EventEmitter<void>();
}
