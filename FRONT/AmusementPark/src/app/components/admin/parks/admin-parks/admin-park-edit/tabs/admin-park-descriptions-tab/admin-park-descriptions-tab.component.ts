import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormGroup } from '@angular/forms';

@Component({
  selector: 'app-admin-park-descriptions-tab',
  templateUrl: './admin-park-descriptions-tab.component.html',
  styleUrls: ['./admin-park-descriptions-tab.component.scss']
})
export class AdminParkDescriptionsTabComponent {
  @Input({ required: true }) form!: FormGroup;
  @Input() isSaving: boolean = false;

  @Output() saveSection: EventEmitter<void> = new EventEmitter<void>();
}
