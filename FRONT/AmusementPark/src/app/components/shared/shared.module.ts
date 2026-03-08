import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { EditorModule } from 'primeng/editor';
import { TabViewModule } from 'primeng/tabview';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { InputTextModule } from 'primeng/inputtext';

import { LeafletMapComponent } from './leaflet-map/leaflet-map.component';
import { LocalizedRichTextEditorComponent } from './localized-rich-text-editor/localized-rich-text-editor.component';
import { LocalizedTextInputComponent } from './localized-text-input/localized-text-input.component';
import { EntitySelectComponent } from './entity-select/entity-select.component';

@NgModule({
  declarations: [
    LeafletMapComponent,
    LocalizedRichTextEditorComponent,
    LocalizedTextInputComponent,
    EntitySelectComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    TranslateModule,
    EditorModule,
    TabViewModule,
    ButtonModule,
    DropdownModule,
    InputTextModule
  ],
  exports: [
    LeafletMapComponent,
    LocalizedRichTextEditorComponent,
    LocalizedTextInputComponent,
    EntitySelectComponent
  ]
})
export class SharedModule { }
