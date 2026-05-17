import { Component, EventEmitter, Input, Output } from '@angular/core';
import { AbstractControl, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { EntitySelectOption } from '@app/models/shared/entity-select-option';
import { AdminParkTypeOption } from '@features/admin/parks/models/admin-park-edit.model';
import { Bind } from 'primeng/bind';
import { ButtonDirective } from 'primeng/button';
import { RouterLink } from '@angular/router';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { EntitySelectComponent } from '@app/components/shared/entity-select/entity-select.component';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { InputNumber } from 'primeng/inputnumber';
import { TranslateModule } from '@ngx-translate/core';


@Component({
    selector: 'app-admin-park-general-tab',
    templateUrl: './admin-park-general-tab.component.html',
    styleUrls: ['./admin-park-general-tab.component.scss'],
    imports: [FormsModule, ReactiveFormsModule, Bind, ButtonDirective, RouterLink, InputText, Select, EntitySelectComponent, ToggleSwitch, InputNumber, TranslateModule]
})
export class AdminParkGeneralTabComponent {
  @Input({ required: true }) form!: FormGroup;
  @Input() parkTypeOptions: AdminParkTypeOption[] = [];
  @Input() countryOptions: { code: string; label: string }[] = [];
  @Input() founderOptions: EntitySelectOption[] = [];
  @Input() operatorOptions: EntitySelectOption[] = [];
  @Input() foundersLoading: boolean = false;
  @Input() operatorsLoading: boolean = false;
  @Input() founderAddLink: string[] = [];
  @Input() founderAddQueryParams: Record<string, string | number> = {};
  @Input() operatorAddLink: string[] = [];
  @Input() operatorAddQueryParams: Record<string, string | number> = {};
  @Input() currentLang: string = 'en';
  @Input() parkId: string | null = null;
  @Input() isEditMode: boolean = false;
  @Input() isSaving: boolean = false;

  @Output() saveSection: EventEmitter<void> = new EventEmitter<void>();

  get nameControl(): AbstractControl | null {
    return this.form.get('name');
  }

  get isVisibleControl(): AbstractControl | null {
    return this.form.get('isVisible');
  }

  get isFeaturedOnHomeControl(): AbstractControl | null {
    return this.form.get('isFeaturedOnHome');
  }

  get isFeaturedOnHomeSponsoredControl(): AbstractControl | null {
    return this.form.get('isFeaturedOnHomeSponsored');
  }
}
