import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, Signal, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { Park } from '@app/models/parks/park';
import { OwnedImageItem } from '@shared/models/images/owned-image-item.model';
import { Bind } from 'primeng/bind';
import { Tag } from 'primeng/tag';
import { EditorSaveToolbarComponent } from '../../../../shared/editor-save-toolbar/editor-save-toolbar.component';
import { Card } from 'primeng/card';
import { Tabs, TabList, Tab, TabPanels, TabPanel } from 'primeng/tabs';
import { Ripple } from 'primeng/ripple';
import { AdminParkGeneralTabComponent } from './tabs/admin-park-general-tab/admin-park-general-tab.component';
import { AdminParkLocationTabComponent } from './tabs/admin-park-location-tab/admin-park-location-tab.component';
import { AdminParkDescriptionsTabComponent } from './tabs/admin-park-descriptions-tab/admin-park-descriptions-tab.component';
import { AdminParkLogosTabComponent } from './tabs/admin-park-logos-tab/admin-park-logos-tab.component';
import { AdminParkPhotosTabComponent } from './tabs/admin-park-photos-tab/admin-park-photos-tab.component';
import { AdminJsonImportTabComponent } from '../../../shared/admin-json-import-tab/admin-json-import-tab.component';
import { PARK_TYPE_OPTIONS } from '@shared/utils/display/display-options';
import { AdminParkTypeOption } from '@features/admin/parks/models/admin-park-edit.model';
import {
  buildAdminParkEditSnapshot,
  createAdminParkEditForm,
  getAdminParkFirstInvalidTabIndex,
  mapAdminParkEditFormToPark,
  patchAdminParkEditForm
} from '@features/admin/parks/mappers/admin-park-edit-form.mapper';
import { AdminParkReferenceDataFacade } from '@features/admin/parks/state/admin-park-reference-data.facade';
import { AdminParkLocationStateFacade } from '@features/admin/parks/state/admin-park-location-state.facade';
import { AdminParkLogosStateFacade } from '@features/admin/parks/state/admin-park-logos-state.facade';
import { AdminParkPhotosStateFacade } from '@features/admin/parks/state/admin-park-photos-state.facade';
import { AdminParkEditStateFacade } from '@features/admin/parks/state/admin-park-edit-state.facade';


type SaveMode = 'stay' | 'back';
type SaveScope = 'section' | 'all';

@Component({
  selector: 'app-admin-park-edit',
  templateUrl: './admin-park-edit.component.html',
  styleUrls: ['./admin-park-edit.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    AdminParkReferenceDataFacade,
    AdminParkLocationStateFacade,
    AdminParkLogosStateFacade,
    AdminParkPhotosStateFacade,
    AdminParkEditStateFacade
  ],
  imports: [
    Bind,
    Tag,
    EditorSaveToolbarComponent,
    Card,
    FormsModule,
    ReactiveFormsModule,
    Tabs,
    TabList,
    Ripple,
    Tab,
    TabPanels,
    TabPanel,
    AdminParkGeneralTabComponent,
    AdminParkLocationTabComponent,
    AdminParkDescriptionsTabComponent,
    AdminParkLogosTabComponent,
    AdminParkPhotosTabComponent,
    AdminJsonImportTabComponent,
    TranslateModule
  ]
})
export class AdminParkEditComponent implements OnInit {
  public readonly form: FormGroup;
  public readonly parkTypeOptions: AdminParkTypeOption[] = [...PARK_TYPE_OPTIONS];
  public readonly allowMultipleLogoUpload: boolean = true;
  public readonly activeTabIndex = signal(0);
  public readonly currentLang = signal('en');
  public readonly parkId = signal<string | null>(null);
  public readonly isEditMode = computed(() => !!this.parkId());
  public readonly hasPendingChanges = signal(false);

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private lastSavedSnapshot: string = '';
  private isInitializing: boolean = false;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translate: TranslateService,
    private readonly toastMessageService: ToastMessageService,
    private readonly referenceDataFacade: AdminParkReferenceDataFacade,
    private readonly locationStateFacade: AdminParkLocationStateFacade,
    private readonly logosStateFacade: AdminParkLogosStateFacade,
    private readonly photosStateFacade: AdminParkPhotosStateFacade,
    private readonly editStateFacade: AdminParkEditStateFacade
  ) {
    this.form = createAdminParkEditForm(this.formBuilder);
  }

  get isFormDirty(): boolean {
    return this.hasPendingChanges();
  }

  public get isSaving(): Signal<boolean> {
    return this.editStateFacade.isSaving;
  }

  protected get referenceData(): AdminParkReferenceDataFacade {
    return this.referenceDataFacade;
  }

  protected get locationState(): AdminParkLocationStateFacade {
    return this.locationStateFacade;
  }

  protected get logosState(): AdminParkLogosStateFacade {
    return this.logosStateFacade;
  }

  protected get photosState(): AdminParkPhotosStateFacade {
    return this.photosStateFacade;
  }

  get nameControl(): AbstractControl | null {
    return this.form.get('name');
  }

  get isVisibleControl(): AbstractControl | null {
    return this.form.get('isVisible');
  }

  ngOnInit(): void {
    this.initializeContextFromRoute();
    this.isInitializing = true;

    this.referenceDataFacade.load(this.currentLang());
    this.locationStateFacade.bindForm(this.form);
    this.logosStateFacade.setCurrentLanguage(this.currentLang());
    this.photosStateFacade.setCurrentLanguage(this.currentLang());
    this.setupFormSync();

    void this.initializeEditorAsync();
  }

  onSubmit(): void {
    void this.persistParkAsync('stay', 'all');
  }

  saveSection(): void {
    void this.persistParkAsync('stay', 'section');
  }

  saveAll(): void {
    void this.persistParkAsync('stay', 'all');
  }

  saveAndClose(): void {
    void this.persistParkAsync('back', 'all');
  }

  goBack(): void {
    this.navigateToList();
  }

  get parkJsonImportExample(): string {
    const payload: unknown = {
      name: this.form.get('name')?.value || 'Nom du parc',
      countryCode: this.form.get('countryCode')?.value || 'BE',
      type: this.form.get('type')?.value || 'ThemePark',
      websiteUrl: this.form.get('websiteUrl')?.value || null,
      city: this.form.get('city')?.value || null,
      postalCode: this.form.get('postalCode')?.value || null,
      isVisible: this.form.get('isVisible')?.value ?? true,
      descriptions: [
        { languageCode: 'fr', value: '<p>Description en français.</p>' },
        { languageCode: 'en', value: '<p>English description.</p>' }
      ]
    };

    return JSON.stringify(payload, null, 2);
  }

  onTabChange(index: number | string | undefined): void {
    const normalizedIndex: number =
      typeof index === 'string'
        ? Number(index)
        : typeof index === 'number'
          ? index
          : 0;

    this.activeTabIndex.set(Number.isFinite(normalizedIndex) ? normalizedIndex : 0);

    if (this.activeTabIndex() === 1) {
      setTimeout((): void => {
        this.locationStateFacade.refreshFromForm();
      }, 80);
    }
  }

  onMapPositionChange(position: { lat: number; lng: number }): void {
    this.locationStateFacade.updatePosition(position);
  }

  onLogoFileSelected(event: Event): void {
    this.logosStateFacade.selectLogoFiles(event);
  }

  async onUploadLogo(): Promise<void> {
    const parkId: string | null = this.parkId();

    if (!parkId) {
      return;
    }

    await this.logosStateFacade.uploadSelectedLogos(
      parkId,
      this.form.get('name')?.value ?? ''
    );
  }

  onSetCurrentLogo(logo: OwnedImageItem): void {
    this.logosStateFacade.setCurrentLogo(logo);
  }

  onParkPhotoFileSelected(event: Event): void {
    this.photosStateFacade.selectPhotoFiles(event);
  }

  async onUploadParkPhoto(): Promise<void> {
    const parkId: string | null = this.parkId();

    if (!parkId) {
      return;
    }

    await this.photosStateFacade.uploadSelectedPhotos(
      parkId,
      this.form.get('name')?.value ?? ''
    );
  }

  onSetCurrentParkPhoto(photo: OwnedImageItem): void {
    this.photosStateFacade.setCurrentPhoto(photo);
  }

  onDeleteParkPhoto(photo: OwnedImageItem): void {
    if (!confirm(this.translate.instant('admin.parks.photos.deleteConfirm'))) {
      return;
    }

    this.photosStateFacade.deletePhoto(photo);
  }

  onDeleteLogo(logo: OwnedImageItem): void {
    if (!confirm(this.translate.instant('admin.parks.logos.deleteConfirm'))) {
      return;
    }

    this.logosStateFacade.deleteLogo(logo);
  }

  get founderCreateLinkQueryParams(): Record<string, string | number> {
    return {
      returnUrl: this.router.url.split('?')[0],
      returnTab: this.activeTabIndex()
    };
  }

  get operatorCreateLinkQueryParams(): Record<string, string | number> {
    return {
      returnUrl: this.router.url.split('?')[0],
      returnTab: this.activeTabIndex()
    };
  }

  getEditorStatusLabel(): string {
    if (this.isSaving()) {
      return this.translate.instant('admin.parks.editorStatus.saving');
    }

    if (this.isFormDirty) {
      return this.translate.instant('admin.parks.editorStatus.unsavedChanges');
    }

    return this.translate.instant('admin.parks.editorStatus.upToDate');
  }

  private initializeContextFromRoute(): void {
    const languageCode: string =
      this.route.root.firstChild?.snapshot.params['lang']
      ?? this.route.snapshot.params['lang']
      ?? 'en';

    this.currentLang.set(languageCode);
    this.parkId.set(this.route.snapshot.paramMap.get('idPark'));

    const requestedTabIndex: number = Number(
      this.route.snapshot.queryParamMap.get('returnTab')
      ?? this.route.snapshot.queryParamMap.get('tab')
      ?? 0
    );

    this.activeTabIndex.set(
      Number.isFinite(requestedTabIndex) && requestedTabIndex >= 0
        ? requestedTabIndex
        : 0
    );
  }

  private async initializeEditorAsync(): Promise<void> {
    const parkId: string | null = this.parkId();

    if (this.isEditMode() && parkId) {
      const wasLoaded: boolean = await this.loadParkAsync(parkId);

      if (wasLoaded) {
        this.logosStateFacade.loadLogos(parkId);
        this.photosStateFacade.loadPhotos(parkId);
      }

      return;
    }

    this.applySelectionOverridesFromQueryParams();
    this.locationStateFacade.refreshFromForm();
    this.finalizeLoadedFormState();
  }

  private async loadParkAsync(parkId: string): Promise<boolean> {
    try {
      const park: Park = await this.editStateFacade.loadPark(parkId);

      patchAdminParkEditForm(this.form, park);
      this.applySelectionOverridesFromQueryParams();
      this.locationStateFacade.refreshFromForm();
      this.finalizeLoadedFormState();
      return true;
    } catch (error: unknown) {
      console.error('Error loading park', error);
      this.navigateToList();
      return false;
    }
  }

  private applySelectionOverridesFromQueryParams(): void {
    const founderId: string | null = this.route.snapshot.queryParamMap.get('founderId');
    const operatorId: string | null = this.route.snapshot.queryParamMap.get('operatorId');

    if (founderId) {
      this.form.patchValue({ founderId }, { emitEvent: false });
    }

    if (operatorId) {
      this.form.patchValue({ operatorId }, { emitEvent: false });
    }
  }

  private setupFormSync(): void {
    this.form.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((): void => {
        if (this.isInitializing) {
          return;
        }

        this.updatePendingChanges();
      });
  }

  private async persistParkAsync(mode: SaveMode, scope: SaveScope): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.focusFirstInvalidTab();
      return;
    }

    if (this.isSaving()) {
      return;
    }

    const payload: Park = mapAdminParkEditFormToPark(this.form);
    const wasEditMode: boolean = this.isEditMode();

    try {
      const savedPark: Park = await this.editStateFacade.savePark(this.parkId(), payload);
      this.afterSuccessfulSave(savedPark, mode, scope, wasEditMode);
    } catch (error: unknown) {
      console.error('Error saving park', error);
      this.showSaveErrorMessage();
    }
  }

  private afterSuccessfulSave(savedPark: Park, mode: SaveMode, scope: SaveScope, wasEditMode: boolean): void {
    this.captureCurrentSnapshot();
    this.showSaveSuccessMessage(scope, wasEditMode);

    if (!this.parkId() && savedPark.id) {
      this.parkId.set(savedPark.id);

      if (mode === 'back') {
        this.goBack();
        return;
      }

      this.router.navigate(
        ['/', this.currentLang(), 'admin', 'parks', 'edit', savedPark.id],
        {
          replaceUrl: true,
          queryParams: { tab: this.activeTabIndex() }
        }
      );
      return;
    }

    if (mode === 'back') {
      this.goBack();
    }
  }

  private showSaveSuccessMessage(scope: SaveScope, hasIdentifier: boolean): void {
    const detailKey: string = scope === 'section'
      ? 'admin.parks.saveMessages.sectionSaved'
      : (hasIdentifier ? 'admin.parks.saveMessages.parkSaved' : 'admin.parks.saveMessages.parkCreated');

    this.toastMessageService.add(
      'success',
      this.translate.instant('admin.parks.saveMessages.successSummary'),
      this.translate.instant(detailKey)
    );
  }

  private showSaveErrorMessage(): void {
    this.toastMessageService.add(
      'error',
      this.translate.instant('admin.parks.saveMessages.errorSummary'),
      this.translate.instant('admin.parks.saveMessages.errorDetail')
    );
  }

  private focusFirstInvalidTab(): void {
    this.activeTabIndex.set(getAdminParkFirstInvalidTabIndex(this.form));
  }

  private finalizeLoadedFormState(): void {
    this.isInitializing = false;
    this.captureCurrentSnapshot();
  }

  private captureCurrentSnapshot(): void {
    this.lastSavedSnapshot = buildAdminParkEditSnapshot(this.form);
    this.hasPendingChanges.set(false);
    this.form.markAsPristine();
    this.form.markAsUntouched();
  }

  private updatePendingChanges(): void {
    this.hasPendingChanges.set(buildAdminParkEditSnapshot(this.form) !== this.lastSavedSnapshot);
  }

  private navigateToList(): void {
    this.router.navigate(['/', this.currentLang(), 'admin', 'parks']);
  }
}
