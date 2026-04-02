import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { EditorModule } from 'primeng/editor';
import { TabsModule } from 'primeng/tabs';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { InputTextModule } from 'primeng/inputtext';
import { DialogModule } from 'primeng/dialog';

import { LeafletMapComponent } from './leaflet-map/leaflet-map.component';
import { LocalizedRichTextEditorComponent } from './localized-rich-text-editor/localized-rich-text-editor.component';
import { LocalizedTextInputComponent } from './localized-text-input/localized-text-input.component';
import { EntitySelectComponent } from './entity-select/entity-select.component';
import { EditorSaveToolbarComponent } from './editor-save-toolbar/editor-save-toolbar.component';
import { OwnerImageUploadDialogComponent } from './owner-image-upload-dialog/owner-image-upload-dialog.component';

@NgModule({
  declarations: [
    LeafletMapComponent,
    LocalizedRichTextEditorComponent,
    LocalizedTextInputComponent,
    EntitySelectComponent,
    EditorSaveToolbarComponent,
    OwnerImageUploadDialogComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    TranslateModule,
    EditorModule,
    TabsModule,
    ButtonModule,
    SelectModule,
    InputTextModule,
    DialogModule
  ],
  exports: [
    LeafletMapComponent,
    LocalizedRichTextEditorComponent,
    LocalizedTextInputComponent,
    EntitySelectComponent,
    EditorSaveToolbarComponent,
    OwnerImageUploadDialogComponent
  ]
})
export class SharedModule { }
