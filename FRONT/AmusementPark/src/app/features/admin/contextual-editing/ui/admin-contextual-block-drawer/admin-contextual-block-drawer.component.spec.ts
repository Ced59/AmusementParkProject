import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Signal, WritableSignal, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { ContextualBlockPreviewResult } from '@shared/models/admin/contextual-block-preview.models';
import { AdminContextualBlockInstance } from '../../models/admin-contextual-block.model';
import { AdminContextualBlockApplyFacade } from '../../state/admin-contextual-block-apply.facade';
import { AdminContextualBlockChildAddFacade, AdminContextualBlockChildAddZoneOption } from '../../state/admin-contextual-block-child-add.facade';
import { AdminContextualBlockExportFacade } from '../../state/admin-contextual-block-export.facade';
import { AdminContextualBlockFormFacade, AdminContextualBlockLocalizedFormField, AdminContextualBlockLocationForm } from '../../state/admin-contextual-block-form.facade';
import {
  AdminContextualBlockPhotoAddFacade,
  AdminContextualBlockPhotoCategoryOption,
  AdminContextualBlockPhotoMetadataRow,
  AdminContextualBlockPhotoTagOption,
  AdminContextualPhotoSourceMode
} from '../../state/admin-contextual-block-photo-add.facade';
import { AdminContextualBlockParkGraphUpsertFacade } from '../../state/admin-contextual-block-park-graph-upsert.facade';
import { AdminContextualBlockPreviewFacade } from '../../state/admin-contextual-block-preview.facade';
import { AdminContextualBlockSelectionFacade } from '../../state/admin-contextual-block-selection.facade';
import { AdminPublicViewModeFacade } from '../../state/admin-public-view-mode.facade';
import { AdminContextualBlockDrawerComponent } from './admin-contextual-block-drawer.component';

describe('AdminContextualBlockDrawerComponent', () => {
  let fixture: ComponentFixture<AdminContextualBlockDrawerComponent>;
  let publicViewModeFacade: AdminPublicViewModeFacade;
  let selectionFacade: AdminContextualBlockSelectionFacade;
  let exportFacade: {
    isExporting: Signal<boolean>;
    errorKey: Signal<string | null>;
    canExport: jasmine.Spy;
    exportBlock: jasmine.Spy;
  };
  let previewFacade: {
    jsonDraft: Signal<string>;
    previewResult: Signal<ContextualBlockPreviewResult | null>;
    isPreviewing: Signal<boolean>;
    errorKey: Signal<string | null>;
    canPreview: jasmine.Spy;
    setJsonDraft: jasmine.Spy;
    clearDraft: jasmine.Spy;
    previewBlock: jasmine.Spy;
    resetForBlock: jasmine.Spy;
  };
  let applyFacade: {
    applyResult: Signal<ContextualBlockPreviewResult | null>;
    isApplying: Signal<boolean>;
    errorKey: Signal<string | null>;
    canApply: jasmine.Spy;
    hasAcceptedPreview: jasmine.Spy;
    applyBlock: jasmine.Spy;
    resetForBlock: jasmine.Spy;
    clearResult: jasmine.Spy;
  };
  let formFacade: {
    localizedFields: Signal<readonly AdminContextualBlockLocalizedFormField[]>;
    locationForm: Signal<AdminContextualBlockLocationForm | null>;
    isLoading: Signal<boolean>;
    isSaving: Signal<boolean>;
    errorKey: Signal<string | null>;
    successKey: Signal<string | null>;
    canEditForm: jasmine.Spy;
    resetForBlock: jasmine.Spy;
    loadForm: jasmine.Spy;
    updateLocalizedValue: jasmine.Spy;
    updateLocationPosition: jasmine.Spy;
    updateLocationLatitude: jasmine.Spy;
    updateLocationLongitude: jasmine.Spy;
    clearLocation: jasmine.Spy;
    saveForm: jasmine.Spy;
  };
  let locationFormSignal: WritableSignal<AdminContextualBlockLocationForm | null>;
  let childAddFacade: {
    itemName: Signal<string>;
    selectedZoneId: Signal<string | null>;
    zoneOptions: Signal<readonly AdminContextualBlockChildAddZoneOption[]>;
    isLoadingZones: Signal<boolean>;
    isCreating: Signal<boolean>;
    errorKey: Signal<string | null>;
    successKey: Signal<string | null>;
    createdItemAdminRoute: Signal<readonly string[] | null>;
    canAddChild: jasmine.Spy;
    resetForBlock: jasmine.Spy;
    updateItemName: jasmine.Spy;
    updateSelectedZoneId: jasmine.Spy;
    createChild: jasmine.Spy;
  };
  let photoAddFacade: {
    sourceMode: Signal<AdminContextualPhotoSourceMode>;
    selectedFile: Signal<File | null>;
    remoteSourceUrl: Signal<string>;
    previewUrl: Signal<string | null>;
    metadataRows: Signal<readonly AdminContextualBlockPhotoMetadataRow[]>;
    description: Signal<string>;
    withWatermark: Signal<boolean>;
    isPublished: Signal<boolean>;
    setAsCurrent: Signal<boolean>;
    categoryOptions: Signal<readonly AdminContextualBlockPhotoCategoryOption[]>;
    selectedCategorySlug: Signal<string>;
    tagOptions: Signal<readonly AdminContextualBlockPhotoTagOption[]>;
    selectedTagIds: Signal<readonly string[]>;
    isLoadingTags: Signal<boolean>;
    isReadingMetadata: Signal<boolean>;
    isUploading: Signal<boolean>;
    errorKey: Signal<string | null>;
    successKey: Signal<string | null>;
    canAddPhoto: jasmine.Spy;
    resetForBlock: jasmine.Spy;
    setSourceMode: jasmine.Spy;
    selectFile: jasmine.Spy;
    updateRemoteSourceUrl: jasmine.Spy;
    previewRemoteSourceUrl: jasmine.Spy;
    updateDescription: jasmine.Spy;
    updateWithWatermark: jasmine.Spy;
    updateSelectedCategorySlug: jasmine.Spy;
    toggleTag: jasmine.Spy;
    updateIsPublished: jasmine.Spy;
    updateSetAsCurrent: jasmine.Spy;
    uploadPhoto: jasmine.Spy;
  };
  let parkGraphUpsertFacade: {
    isCopying: Signal<boolean>;
    isDownloading: Signal<boolean>;
    isImporting: Signal<boolean>;
    errorKey: Signal<string | null>;
    successKey: Signal<string | null>;
    canUseDraft: jasmine.Spy;
    getDraft: jasmine.Spy;
    resetForBlock: jasmine.Spy;
    copyDraft: jasmine.Spy;
    downloadDraft: jasmine.Spy;
    importDraftFile: jasmine.Spy;
  };

  beforeEach(async () => {
    const isExportingSignal = signal<boolean>(false);
    const errorKeySignal = signal<string | null>(null);
    const jsonDraftSignal = signal<string>('');
    const previewResultSignal = signal<ContextualBlockPreviewResult | null>(null);
    const isPreviewingSignal = signal<boolean>(false);
    const previewErrorKeySignal = signal<string | null>(null);
    const applyResultSignal = signal<ContextualBlockPreviewResult | null>(null);
    const isApplyingSignal = signal<boolean>(false);
    const applyErrorKeySignal = signal<string | null>(null);
    const localizedFieldsSignal = signal<readonly AdminContextualBlockLocalizedFormField[]>([
      { languageCode: 'en', value: 'English description' },
      { languageCode: 'fr', value: '' }
    ]);
    locationFormSignal = signal<AdminContextualBlockLocationForm | null>(null);
    const isFormLoadingSignal = signal<boolean>(false);
    const isFormSavingSignal = signal<boolean>(false);
    const formErrorKeySignal = signal<string | null>(null);
    const formSuccessKeySignal = signal<string | null>(null);
    const childAddNameSignal = signal<string>('');
    const childAddSelectedZoneIdSignal = signal<string | null>(null);
    const childAddZonesSignal = signal<readonly AdminContextualBlockChildAddZoneOption[]>([
      { id: 'zone-1', label: 'Berlin', latitude: 50.1, longitude: 3.2 }
    ]);
    const isChildAddLoadingZonesSignal = signal<boolean>(false);
    const isChildAddCreatingSignal = signal<boolean>(false);
    const childAddErrorKeySignal = signal<string | null>(null);
    const childAddSuccessKeySignal = signal<string | null>(null);
    const createdItemAdminRouteSignal = signal<readonly string[] | null>(null);
    const photoSourceModeSignal = signal<AdminContextualPhotoSourceMode>('file');
    const photoSelectedFileSignal = signal<File | null>(null);
    const photoRemoteSourceUrlSignal = signal<string>('');
    const photoPreviewUrlSignal = signal<string | null>('https://example.test/photo.jpg');
    const photoMetadataRowsSignal = signal<readonly AdminContextualBlockPhotoMetadataRow[]>([
      { labelKey: 'admin.contextualBlocks.drawer.photoMetadataDimensions', value: '1024 x 768 px', tone: 'neutral' },
      { labelKey: 'admin.contextualBlocks.drawer.photoMetadataGeoLocation', value: '50.100000, 3.200000', tone: 'success' }
    ]);
    const photoDescriptionSignal = signal<string>('');
    const photoWithWatermarkSignal = signal<boolean>(false);
    const photoIsPublishedSignal = signal<boolean>(true);
    const photoSetAsCurrentSignal = signal<boolean>(false);
    const photoCategoryOptionsSignal = signal<readonly AdminContextualBlockPhotoCategoryOption[]>([
      { slug: 'park-gallery', labelKey: 'admin.parks.photos.categories.gallery' }
    ]);
    const photoSelectedCategorySlugSignal = signal<string>('park-gallery');
    const photoTagOptionsSignal = signal<readonly AdminContextualBlockPhotoTagOption[]>([
      { id: 'tag-1', slug: 'night', label: 'Nuit', isCategoryTag: false }
    ]);
    const photoSelectedTagIdsSignal = signal<readonly string[]>([]);
    const isPhotoLoadingTagsSignal = signal<boolean>(false);
    const isPhotoReadingMetadataSignal = signal<boolean>(false);
    const isPhotoUploadingSignal = signal<boolean>(false);
    const photoErrorKeySignal = signal<string | null>(null);
    const photoSuccessKeySignal = signal<string | null>(null);
    const isParkGraphUpsertCopyingSignal = signal<boolean>(false);
    const isParkGraphUpsertDownloadingSignal = signal<boolean>(false);
    const isParkGraphUpsertImportingSignal = signal<boolean>(false);
    const parkGraphUpsertErrorKeySignal = signal<string | null>(null);
    const parkGraphUpsertSuccessKeySignal = signal<string | null>(null);
    exportFacade = {
      isExporting: isExportingSignal.asReadonly(),
      errorKey: errorKeySignal.asReadonly(),
      canExport: jasmine.createSpy('canExport').and.callFake((block: AdminContextualBlockInstance | null): boolean => {
        return Boolean(block?.capabilities.includes('boundedJsonExport'));
      }),
      exportBlock: jasmine.createSpy('exportBlock')
    };
    previewFacade = {
      jsonDraft: jsonDraftSignal.asReadonly(),
      previewResult: previewResultSignal.asReadonly(),
      isPreviewing: isPreviewingSignal.asReadonly(),
      errorKey: previewErrorKeySignal.asReadonly(),
      canPreview: jasmine.createSpy('canPreview').and.callFake((block: AdminContextualBlockInstance | null): boolean => {
        return Boolean(block?.capabilities.includes('boundedJsonPreview'));
      }),
      setJsonDraft: jasmine.createSpy('setJsonDraft').and.callFake((value: string): void => {
        jsonDraftSignal.set(value);
      }),
      clearDraft: jasmine.createSpy('clearDraft').and.callFake((): void => {
        jsonDraftSignal.set('');
      }),
      previewBlock: jasmine.createSpy('previewBlock'),
      resetForBlock: jasmine.createSpy('resetForBlock').and.callFake((block: AdminContextualBlockInstance | null): void => {
        if (!block) {
          jsonDraftSignal.set('');
        }
      })
    };
    formFacade = {
      localizedFields: localizedFieldsSignal.asReadonly(),
      locationForm: locationFormSignal.asReadonly(),
      isLoading: isFormLoadingSignal.asReadonly(),
      isSaving: isFormSavingSignal.asReadonly(),
      errorKey: formErrorKeySignal.asReadonly(),
      successKey: formSuccessKeySignal.asReadonly(),
      canEditForm: jasmine.createSpy('canEditForm').and.callFake((block: AdminContextualBlockInstance | null): boolean => {
        return Boolean(block?.capabilities.includes('contextualFormEdit'));
      }),
      resetForBlock: jasmine.createSpy('resetForBlock'),
      loadForm: jasmine.createSpy('loadForm'),
      updateLocalizedValue: jasmine.createSpy('updateLocalizedValue').and.callFake((languageCode: string, value: string): void => {
        localizedFieldsSignal.update((fields: readonly AdminContextualBlockLocalizedFormField[]) => fields.map((field: AdminContextualBlockLocalizedFormField) => {
          return field.languageCode === languageCode ? { ...field, value } : field;
        }));
      }),
      updateLocationPosition: jasmine.createSpy('updateLocationPosition').and.callFake((latitude: number, longitude: number): void => {
        const currentForm: AdminContextualBlockLocationForm | null = locationFormSignal();
        locationFormSignal.set({
          latitude,
          longitude,
          mapCenter: [latitude, longitude],
          mapZoom: currentForm?.mapZoom ?? 16,
          mapMarkers: [{
            id: 'contextual-location',
            lat: latitude,
            lng: longitude,
            title: 'Phantasialand',
            iconKind: 'park'
          }]
        });
      }),
      updateLocationLatitude: jasmine.createSpy('updateLocationLatitude'),
      updateLocationLongitude: jasmine.createSpy('updateLocationLongitude'),
      clearLocation: jasmine.createSpy('clearLocation').and.callFake((): void => {
        locationFormSignal.set({
          latitude: null,
          longitude: null,
          mapCenter: [48.85, 2.35],
          mapZoom: 16,
          mapMarkers: []
        });
      }),
      saveForm: jasmine.createSpy('saveForm')
    };
    applyFacade = {
      applyResult: applyResultSignal.asReadonly(),
      isApplying: isApplyingSignal.asReadonly(),
      errorKey: applyErrorKeySignal.asReadonly(),
      canApply: jasmine.createSpy('canApply').and.callFake((block: AdminContextualBlockInstance | null): boolean => {
        return Boolean(block?.capabilities.includes('boundedJsonApply'));
      }),
      hasAcceptedPreview: jasmine.createSpy('hasAcceptedPreview').and.returnValue(true),
      applyBlock: jasmine.createSpy('applyBlock'),
      resetForBlock: jasmine.createSpy('resetForBlock'),
      clearResult: jasmine.createSpy('clearResult').and.callFake((): void => {
        applyResultSignal.set(null);
        applyErrorKeySignal.set(null);
      })
    };
    childAddFacade = {
      itemName: childAddNameSignal.asReadonly(),
      selectedZoneId: childAddSelectedZoneIdSignal.asReadonly(),
      zoneOptions: childAddZonesSignal.asReadonly(),
      isLoadingZones: isChildAddLoadingZonesSignal.asReadonly(),
      isCreating: isChildAddCreatingSignal.asReadonly(),
      errorKey: childAddErrorKeySignal.asReadonly(),
      successKey: childAddSuccessKeySignal.asReadonly(),
      createdItemAdminRoute: createdItemAdminRouteSignal.asReadonly(),
      canAddChild: jasmine.createSpy('canAddChild').and.callFake((block: AdminContextualBlockInstance | null): boolean => {
        return Boolean(block?.capabilities.includes('targetedChildAdd'));
      }),
      resetForBlock: jasmine.createSpy('resetForBlock'),
      updateItemName: jasmine.createSpy('updateItemName').and.callFake((value: string): void => {
        childAddNameSignal.set(value);
      }),
      updateSelectedZoneId: jasmine.createSpy('updateSelectedZoneId').and.callFake((value: string | null): void => {
        childAddSelectedZoneIdSignal.set(value);
      }),
      createChild: jasmine.createSpy('createChild')
    };
    photoAddFacade = {
      sourceMode: photoSourceModeSignal.asReadonly(),
      selectedFile: photoSelectedFileSignal.asReadonly(),
      remoteSourceUrl: photoRemoteSourceUrlSignal.asReadonly(),
      previewUrl: photoPreviewUrlSignal.asReadonly(),
      metadataRows: photoMetadataRowsSignal.asReadonly(),
      description: photoDescriptionSignal.asReadonly(),
      withWatermark: photoWithWatermarkSignal.asReadonly(),
      isPublished: photoIsPublishedSignal.asReadonly(),
      setAsCurrent: photoSetAsCurrentSignal.asReadonly(),
      categoryOptions: photoCategoryOptionsSignal.asReadonly(),
      selectedCategorySlug: photoSelectedCategorySlugSignal.asReadonly(),
      tagOptions: photoTagOptionsSignal.asReadonly(),
      selectedTagIds: photoSelectedTagIdsSignal.asReadonly(),
      isLoadingTags: isPhotoLoadingTagsSignal.asReadonly(),
      isReadingMetadata: isPhotoReadingMetadataSignal.asReadonly(),
      isUploading: isPhotoUploadingSignal.asReadonly(),
      errorKey: photoErrorKeySignal.asReadonly(),
      successKey: photoSuccessKeySignal.asReadonly(),
      canAddPhoto: jasmine.createSpy('canAddPhoto').and.callFake((block: AdminContextualBlockInstance | null): boolean => {
        return Boolean(block?.capabilities.includes('contextualPhotoAdd'));
      }),
      resetForBlock: jasmine.createSpy('resetForBlock'),
      setSourceMode: jasmine.createSpy('setSourceMode').and.callFake((mode: AdminContextualPhotoSourceMode): void => {
        photoSourceModeSignal.set(mode);
      }),
      selectFile: jasmine.createSpy('selectFile'),
      updateRemoteSourceUrl: jasmine.createSpy('updateRemoteSourceUrl').and.callFake((value: string): void => {
        photoRemoteSourceUrlSignal.set(value);
      }),
      previewRemoteSourceUrl: jasmine.createSpy('previewRemoteSourceUrl'),
      updateDescription: jasmine.createSpy('updateDescription').and.callFake((value: string): void => {
        photoDescriptionSignal.set(value);
      }),
      updateWithWatermark: jasmine.createSpy('updateWithWatermark').and.callFake((value: boolean): void => {
        photoWithWatermarkSignal.set(value);
      }),
      updateSelectedCategorySlug: jasmine.createSpy('updateSelectedCategorySlug').and.callFake((value: string): void => {
        photoSelectedCategorySlugSignal.set(value);
      }),
      toggleTag: jasmine.createSpy('toggleTag').and.callFake((tagId: string, checked: boolean): void => {
        photoSelectedTagIdsSignal.set(checked ? [tagId] : []);
      }),
      updateIsPublished: jasmine.createSpy('updateIsPublished').and.callFake((value: boolean): void => {
        photoIsPublishedSignal.set(value);
      }),
      updateSetAsCurrent: jasmine.createSpy('updateSetAsCurrent').and.callFake((value: boolean): void => {
        photoSetAsCurrentSignal.set(value);
      }),
      uploadPhoto: jasmine.createSpy('uploadPhoto')
    };
    parkGraphUpsertFacade = {
      isCopying: isParkGraphUpsertCopyingSignal.asReadonly(),
      isDownloading: isParkGraphUpsertDownloadingSignal.asReadonly(),
      isImporting: isParkGraphUpsertImportingSignal.asReadonly(),
      errorKey: parkGraphUpsertErrorKeySignal.asReadonly(),
      successKey: parkGraphUpsertSuccessKeySignal.asReadonly(),
      canUseDraft: jasmine.createSpy('canUseDraft').and.callFake((block: AdminContextualBlockInstance | null): boolean => {
        return Boolean(block?.capabilities.includes('parkGraphUpsertDraft') && block.parkGraphUpsertDraftJson?.trim());
      }),
      getDraft: jasmine.createSpy('getDraft').and.callFake((block: AdminContextualBlockInstance | null): string | null => {
        const draft: string = block?.parkGraphUpsertDraftJson?.trim() ?? '';
        return draft.length > 0 ? draft : null;
      }),
      resetForBlock: jasmine.createSpy('resetForBlock'),
      copyDraft: jasmine.createSpy('copyDraft'),
      downloadDraft: jasmine.createSpy('downloadDraft'),
      importDraftFile: jasmine.createSpy('importDraftFile')
    };

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AdminContextualBlockDrawerComponent],
      providers: [
        ...provideCommonTestDependencies(),
        AdminPublicViewModeFacade,
        AdminContextualBlockSelectionFacade,
        {
          provide: AdminContextualBlockExportFacade,
          useValue: exportFacade
        },
        {
          provide: AdminContextualBlockPreviewFacade,
          useValue: previewFacade
        },
        {
          provide: AdminContextualBlockApplyFacade,
          useValue: applyFacade
        },
        {
          provide: AdminContextualBlockFormFacade,
          useValue: formFacade
        },
        {
          provide: AdminContextualBlockChildAddFacade,
          useValue: childAddFacade
        },
        {
          provide: AdminContextualBlockPhotoAddFacade,
          useValue: photoAddFacade
        },
        {
          provide: AdminContextualBlockParkGraphUpsertFacade,
          useValue: parkGraphUpsertFacade
        }
      ]
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('fr', {
      admin: {
        contextualBlocks: {
          drawer: {
            ariaLabel: 'Bloc editable selectionne',
            kicker: 'Bloc contexte',
            close: 'Fermer',
            type: 'Type de bloc',
            entity: 'Entite',
            label: 'Element courant',
            capabilities: 'Capacites',
            localizedLanguages: 'Localise en',
            jsonScope: 'Perimetre JSON',
            openAdminEdit: 'Ouvrir edition admin complete',
            downloadJson: 'Telecharger le JSON du bloc',
            downloadJsonBusy: 'Telechargement...',
            downloadJsonError: 'Le telechargement JSON a echoue.',
            downloadJsonUnavailable: 'Export JSON indisponible',
            exportJsonAriaLabel: 'Telecharger le JSON borne de ce bloc',
            jsonDraft: 'Brouillon JSON',
            jsonDraftAriaLabel: 'Brouillon JSON borne du bloc',
            previewJson: 'Previsualiser',
            previewJsonBusy: 'Previsualisation...',
            clearJsonDraft: 'Effacer',
            previewJsonInvalid: 'JSON invalide',
            previewJsonError: 'La previsualisation JSON a echoue.',
            previewJsonUnavailable: 'Previsualisation JSON indisponible',
            previewJsonCanApply: 'Previsualisation valide',
            previewJsonBlocked: 'Previsualisation bloquee',
            applyJson: 'Appliquer',
            applyJsonBusy: 'Application...',
            applyJsonInvalid: 'JSON invalide',
            applyJsonError: 'L application JSON a echoue.',
            applyJsonUnavailable: 'Application JSON indisponible',
            applyJsonPreviewRequired: 'Previsualise avant d appliquer.',
            applyJsonSucceeded: 'JSON applique',
            formTitle: 'Formulaire rapide',
            formLoading: 'Chargement du formulaire...',
            formReload: 'Recharger',
            formSave: 'Enregistrer',
            formSaveBusy: 'Enregistrement...',
            formLoadError: 'Le formulaire n a pas pu charger.',
            formSaveError: 'L enregistrement a echoue.',
            formSaveSucceeded: 'Formulaire enregistre.',
            formNoChanges: 'Aucun changement a enregistrer.',
            formUnavailable: 'Formulaire indisponible',
            localizedFieldAriaLabel: 'Champ localise',
            locationPickerHint: 'Clique sur la carte pour placer le point.',
            locationLatitude: 'Latitude',
            locationLongitude: 'Longitude',
            locationClear: 'Retirer la localisation',
            locationInvalid: 'Coordonnees invalides.',
            addChildTitle: 'Ajouter un item',
            addChildHint: 'Cree cache et a relire.',
            addChildNameLabel: 'Nom',
            addChildNamePlaceholder: 'Nom de l item',
            addChildZoneLabel: 'Zone',
            addChildNoZoneOption: 'Sans zone',
            addChildZonesLoading: 'Chargement des zones...',
            addChildCreate: 'Creer',
            addChildCreateBusy: 'Creation...',
            addChildError: 'Creation impossible.',
            addChildNameRequired: 'Nom obligatoire.',
            addChildUnavailable: 'Ajout indisponible.',
            addChildSucceeded: 'Item cree.',
            openCreatedChild: 'Ouvrir l item',
            parkGraphUpsertTitle: 'Upsert constructeur',
            parkGraphUpsertHint: 'Copie telecharge ou importe ce JSON constructeur.',
            parkGraphUpsertDraftAriaLabel: 'Brouillon upsert constructeur',
            copyParkGraphUpsert: 'Copier le JSON',
            copyParkGraphUpsertBusy: 'Copie...',
            downloadParkGraphUpsert: 'Telecharger le JSON',
            openParkGraphUpsertImport: 'Ouvrir l import JSON',
            importParkGraphUpsert: 'Importer un JSON',
            importParkGraphUpsertBusy: 'Import...',
            parkGraphUpsertCopied: 'JSON copie.',
            parkGraphUpsertDownloaded: 'JSON telecharge.',
            parkGraphUpsertImported: 'JSON importe.',
            parkGraphUpsertCopyError: 'Copie impossible.',
            parkGraphUpsertDownloadError: 'Telechargement impossible.',
            parkGraphUpsertImportError: 'Import impossible.',
            parkGraphUpsertImportBlocked: 'JSON bloque.',
            parkGraphUpsertImportInvalidFile: 'Fichier JSON requis.',
            parkGraphUpsertImportInvalidJson: 'JSON invalide.',
            parkGraphUpsertUnavailable: 'Brouillon indisponible.',
            photoAddTitle: 'Ajouter une photo',
            photoSourceFile: 'Fichier',
            photoSourceRemote: 'Lien',
            photoFileLabel: 'Fichier image',
            photoSelectedFile: 'Fichier: {{ fileName }}',
            photoRemoteUrlLabel: 'URL image',
            photoRemoteUrlPlaceholder: 'https://...',
            photoPreviewRemote: 'Previsualiser le lien',
            photoPreviewAlt: 'Apercu de la photo',
            photoMetadataReading: 'Lecture des metadonnees...',
            photoMetadataFileName: 'Nom',
            photoMetadataContentType: 'Type',
            photoMetadataSize: 'Taille',
            photoMetadataDimensions: 'Dimensions',
            photoMetadataGeoLocation: 'Geoloc',
            photoMetadataGeoUnavailable: 'Verification apres import',
            photoMetadataGeoMissing: 'Aucune coordonnee detectee',
            photoDescriptionLabel: 'Description',
            photoDescriptionPlaceholder: 'Description courte',
            photoCategoryLabel: 'Categorie',
            photoTagsLoading: 'Chargement des tags...',
            photoTagsLabel: 'Tags photo',
            photoPublishedLabel: 'Publier la photo',
            photoSetCurrentLabel: 'Definir comme image principale',
            photoUpload: 'Ajouter la photo',
            photoUploading: 'Ajout...',
            photoSourceRequired: 'Source requise',
            photoInvalidRemoteUrl: 'URL invalide',
            photoUploadError: 'Ajout impossible',
            photoUploadSucceeded: 'Photo ajoutee',
            photoTagsLoadError: 'Tags indisponibles',
            previewChanged: 'Modifies',
            previewErrors: 'Erreurs',
            previewWarnings: 'Alertes',
            previewChanges: 'Changements',
            previewCurrentValue: 'Actuel',
            previewProposedValue: 'Propose',
            previewJsonNoChanges: 'Aucun changement detecte',
            emptyValue: 'Vide'
          },
          capabilities: {
            fullAdminEdit: 'Edition admin complete disponible',
            boundedJsonExport: 'Export JSON borne disponible',
            boundedJsonPreview: 'Previsualisation JSON borne disponible',
            boundedJsonApply: 'Application JSON borne disponible',
            contextualFormEdit: 'Formulaire contextuel disponible',
            contextualPhotoAdd: 'Ajout photo contextuel disponible',
            parkGraphUpsertDraft: 'Brouillon upsert JSON disponible',
            targetedChildAdd: 'Ajout cible disponible',
            boundedJsonExportPlanned: 'Export JSON borne prevu',
            boundedJsonUpsertPlanned: 'Upsert JSON borne prevu',
            formEditPlanned: 'Formulaire contextuel prevu'
          },
          blocks: {
            parkDescription: {
              label: 'Description du parc',
              description: 'Description localisee'
            },
            parkImages: {
              label: 'Photos du parc',
              description: 'Galerie publique'
            },
            manufacturerReference: {
              label: 'Constructeur',
              description: 'Brouillon upsert du constructeur'
            }
          }
        }
      }
    });
    translateService.use('fr');

    publicViewModeFacade = TestBed.inject(AdminPublicViewModeFacade);
    selectionFacade = TestBed.inject(AdminContextualBlockSelectionFacade);
    fixture = TestBed.createComponent(AdminContextualBlockDrawerComponent);
  });

  it('stays absent until a block is selected', () => {
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('.admin-contextual-block-drawer')).toBeNull();
  });

  it('renders selected block diagnostics and bounded JSON actions without exposing submit forms', () => {
    publicViewModeFacade.setViewMode('adminPreview');
    publicViewModeFacade.setEditionModeEnabled(true);
    selectionFacade.selectBlock(createBlock());
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const drawer: HTMLElement = host.querySelector('.admin-contextual-block-drawer') as HTMLElement;
    const formSection: HTMLElement | null = host.querySelector('.admin-contextual-block-drawer__form');
    const idsSection: HTMLElement | null = host.querySelector('.admin-contextual-block-drawer__ids');
    const adminLink: HTMLAnchorElement | null = host.querySelector('.admin-contextual-block-drawer__footer .admin-contextual-block-drawer__action--primary');
    const exportButton: HTMLButtonElement | null = host.querySelector('.admin-contextual-block-drawer__footer .admin-contextual-block-drawer__action--secondary');
    const previewTextArea: HTMLTextAreaElement | null = host.querySelector('.admin-contextual-block-drawer__json-input');

    expect(drawer.textContent).toContain('park.description');
    expect(drawer.textContent).toContain('park-1');
    expect(drawer.textContent).toContain('fr');
    expect(drawer.textContent).toContain('en');
    expect(previewTextArea).not.toBeNull();
    expect(drawer.textContent).toContain('Formulaire rapide');
    expect(formSection).not.toBeNull();
    expect(idsSection).not.toBeNull();
    expect(Boolean((formSection as HTMLElement).compareDocumentPosition(idsSection as HTMLElement) & Node.DOCUMENT_POSITION_FOLLOWING)).toBeTrue();
    expect(exportButton?.textContent).toContain('Telecharger le JSON du bloc');
    expect(drawer.textContent).toContain('Appliquer');
    expect(adminLink?.textContent).toContain('Ouvrir edition admin complete');
    expect(adminLink?.target).toBe('_blank');
    expect(adminLink?.rel).toContain('noopener');
    expect(drawer.querySelector('button[type="submit"]')).toBeNull();
  });

  it('delegates bounded JSON downloads to the export facade', () => {
    const block: AdminContextualBlockInstance = createBlock();
    publicViewModeFacade.setViewMode('adminPreview');
    publicViewModeFacade.setEditionModeEnabled(true);
    selectionFacade.selectBlock(block);
    fixture.detectChanges();

    const exportButton: HTMLButtonElement = (fixture.nativeElement as HTMLElement)
      .querySelector('.admin-contextual-block-drawer__footer .admin-contextual-block-drawer__action--secondary') as HTMLButtonElement;
    exportButton.click();

    expect(exportFacade.exportBlock).toHaveBeenCalledOnceWith(block);
  });

  it('delegates bounded JSON previews without clearing the draft', () => {
    const block: AdminContextualBlockInstance = createBlock();
    publicViewModeFacade.setViewMode('adminPreview');
    publicViewModeFacade.setEditionModeEnabled(true);
    selectionFacade.selectBlock(block);
    fixture.detectChanges();

    const textArea: HTMLTextAreaElement = (fixture.nativeElement as HTMLElement)
      .querySelector('.admin-contextual-block-drawer__json-input') as HTMLTextAreaElement;
    textArea.value = '{ "block": { "parkId": "park-1" } }';
    textArea.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const previewButton: HTMLButtonElement = (fixture.nativeElement as HTMLElement)
      .querySelector('.admin-contextual-block-drawer__preview .admin-contextual-block-drawer__preview-actions .admin-contextual-block-drawer__action--primary') as HTMLButtonElement;
    previewButton.click();

    expect(previewFacade.setJsonDraft).toHaveBeenCalledWith('{ "block": { "parkId": "park-1" } }');
    expect(previewFacade.previewBlock).toHaveBeenCalledOnceWith(block);
    expect(applyFacade.clearResult).toHaveBeenCalled();
    expect(previewFacade.clearDraft).not.toHaveBeenCalled();
  });

  it('delegates bounded JSON apply after an accepted preview', () => {
    const block: AdminContextualBlockInstance = createBlock();
    publicViewModeFacade.setViewMode('adminPreview');
    publicViewModeFacade.setEditionModeEnabled(true);
    selectionFacade.selectBlock(block);
    fixture.detectChanges();

    const buttons: NodeListOf<HTMLButtonElement> = (fixture.nativeElement as HTMLElement)
      .querySelectorAll('.admin-contextual-block-drawer__preview .admin-contextual-block-drawer__preview-actions .admin-contextual-block-drawer__action');
    const applyButton: HTMLButtonElement = buttons.item(1);
    applyButton.click();

    expect(applyFacade.applyBlock).toHaveBeenCalledOnceWith(block);
  });

  it('delegates contextual form edits to the form facade', () => {
    const block: AdminContextualBlockInstance = createBlock();
    publicViewModeFacade.setViewMode('adminPreview');
    publicViewModeFacade.setEditionModeEnabled(true);
    selectionFacade.selectBlock(block);
    fixture.detectChanges();

    const textarea: HTMLTextAreaElement = (fixture.nativeElement as HTMLElement)
      .querySelector('.admin-contextual-block-drawer__form-fields textarea') as HTMLTextAreaElement;
    textarea.value = 'Updated description';
    textarea.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const saveButton: HTMLButtonElement = (fixture.nativeElement as HTMLElement)
      .querySelector('.admin-contextual-block-drawer__form .admin-contextual-block-drawer__action--primary') as HTMLButtonElement;
    saveButton.click();

    expect(formFacade.updateLocalizedValue).toHaveBeenCalledWith('en', 'Updated description');
    expect(formFacade.saveForm).toHaveBeenCalledOnceWith(block);
  });

  it('renders the contextual location form and delegates map actions', () => {
    const block: AdminContextualBlockInstance = createLocationBlock();
    locationFormSignal.set({
      latitude: 48.85,
      longitude: 2.35,
      mapCenter: [48.85, 2.35],
      mapZoom: 16,
      mapMarkers: [{
        id: 'contextual-location',
        lat: 48.85,
        lng: 2.35,
        title: 'Phantasialand',
        iconKind: 'park'
      }]
    });
    publicViewModeFacade.setViewMode('adminPreview');
    publicViewModeFacade.setEditionModeEnabled(true);
    selectionFacade.selectBlock(block);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const latitudeInput: HTMLInputElement = host.querySelector('input[aria-label="Latitude"]') as HTMLInputElement;
    const longitudeInput: HTMLInputElement = host.querySelector('input[aria-label="Longitude"]') as HTMLInputElement;
    latitudeInput.value = '50.1';
    latitudeInput.dispatchEvent(new Event('input'));
    longitudeInput.value = '3.2';
    longitudeInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const buttons: NodeListOf<HTMLButtonElement> = host.querySelectorAll('.admin-contextual-block-drawer__form .admin-contextual-block-drawer__action');
    buttons.item(0).click();
    buttons.item(1).click();

    expect(host.querySelector('app-leaflet-map')).not.toBeNull();
    expect(formFacade.updateLocationLatitude).toHaveBeenCalledWith('50.1', block);
    expect(formFacade.updateLocationLongitude).toHaveBeenCalledWith('3.2', block);
    expect(formFacade.saveForm).toHaveBeenCalledOnceWith(block);
    expect(formFacade.clearLocation).toHaveBeenCalledOnceWith(block);
  });

  it('delegates targeted child creation to the child add facade', () => {
    const block: AdminContextualBlockInstance = createHeroBlock();
    publicViewModeFacade.setViewMode('adminPreview');
    publicViewModeFacade.setEditionModeEnabled(true);
    selectionFacade.selectBlock(block);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const nameInput: HTMLInputElement = host.querySelector('.admin-contextual-block-drawer__child-add input') as HTMLInputElement;
    const zoneSelect: HTMLSelectElement = host.querySelector('.admin-contextual-block-drawer__child-add select') as HTMLSelectElement;
    nameInput.value = 'New ride';
    nameInput.dispatchEvent(new Event('input'));
    zoneSelect.value = 'zone-1';
    zoneSelect.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    const createButton: HTMLButtonElement = host.querySelector('.admin-contextual-block-drawer__child-add .admin-contextual-block-drawer__action--primary') as HTMLButtonElement;
    createButton.click();

    expect(childAddFacade.updateItemName).toHaveBeenCalledWith('New ride');
    expect(childAddFacade.updateSelectedZoneId).toHaveBeenCalledWith('zone-1');
    expect(childAddFacade.createChild).toHaveBeenCalledOnceWith(block);
  });

  it('renders contextual photo additions and delegates upload choices to the photo facade', () => {
    const block: AdminContextualBlockInstance = createPhotoBlock();
    publicViewModeFacade.setViewMode('adminPreview');
    publicViewModeFacade.setEditionModeEnabled(true);
    selectionFacade.selectBlock(block);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const sourceButtons: NodeListOf<HTMLButtonElement> = host.querySelectorAll('.admin-contextual-block-drawer__segmented button');
    sourceButtons.item(1).click();
    fixture.detectChanges();

    const remoteInput: HTMLInputElement = host.querySelector('.admin-contextual-block-drawer__photo-add input[type="url"]') as HTMLInputElement;
    remoteInput.value = 'https://example.test/photo.jpg';
    remoteInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const descriptionInput: HTMLInputElement = host.querySelector('.admin-contextual-block-drawer__photo-add input[type="text"]') as HTMLInputElement;
    descriptionInput.value = 'Night view';
    descriptionInput.dispatchEvent(new Event('input'));

    const tagInput: HTMLInputElement = host.querySelector('.admin-contextual-block-drawer__tag-list input[type="checkbox"]') as HTMLInputElement;
    tagInput.checked = true;
    tagInput.dispatchEvent(new Event('change'));

    const toggles: NodeListOf<HTMLInputElement> = host.querySelectorAll('.admin-contextual-block-drawer__toggles input[type="checkbox"]');
    toggles.item(0).checked = true;
    toggles.item(0).dispatchEvent(new Event('change'));
    toggles.item(2).checked = true;
    toggles.item(2).dispatchEvent(new Event('change'));
    fixture.detectChanges();

    const previewButton: HTMLButtonElement = host.querySelector('.admin-contextual-block-drawer__photo-add .admin-contextual-block-drawer__action--secondary') as HTMLButtonElement;
    const uploadButton: HTMLButtonElement = host.querySelector('.admin-contextual-block-drawer__photo-add .admin-contextual-block-drawer__action--primary') as HTMLButtonElement;
    previewButton.click();
    uploadButton.click();

    expect(host.textContent).toContain('Ajouter une photo');
    expect(host.textContent).toContain('1024 x 768 px');
    expect(photoAddFacade.setSourceMode).toHaveBeenCalledWith('remote');
    expect(photoAddFacade.updateRemoteSourceUrl).toHaveBeenCalledWith('https://example.test/photo.jpg');
    expect(photoAddFacade.updateDescription).toHaveBeenCalledWith('Night view');
    expect(photoAddFacade.updateWithWatermark).toHaveBeenCalledWith(true);
    expect(photoAddFacade.toggleTag).toHaveBeenCalledWith('tag-1', true);
    expect(photoAddFacade.updateSetAsCurrent).toHaveBeenCalledWith(true);
    expect(photoAddFacade.previewRemoteSourceUrl).toHaveBeenCalled();
    expect(photoAddFacade.uploadPhoto).toHaveBeenCalledOnceWith(block);
  });

  it('renders manufacturer JSON upsert draft actions', () => {
    const block: AdminContextualBlockInstance = createManufacturerBlock();
    publicViewModeFacade.setViewMode('adminPreview');
    publicViewModeFacade.setEditionModeEnabled(true);
    selectionFacade.selectBlock(block);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const textarea: HTMLTextAreaElement = host.querySelector('.admin-contextual-block-drawer__park-graph-upsert textarea') as HTMLTextAreaElement;
    const actions: NodeListOf<HTMLButtonElement> = host.querySelectorAll('.admin-contextual-block-drawer__park-graph-upsert button');
    const importInput: HTMLInputElement = host.querySelector('.admin-contextual-block-drawer__park-graph-upsert input[type="file"]') as HTMLInputElement;
    const importFile: File = new File(['{ "documentType": "AmusementParkParkGraphUpsert" }'], 'manufacturer.json', { type: 'application/json' });

    actions.item(0).click();
    actions.item(1).click();
    Object.defineProperty(importInput, 'files', {
      configurable: true,
      value: [importFile]
    });
    importInput.dispatchEvent(new Event('change'));

    expect(host.textContent).toContain('Upsert constructeur');
    expect(textarea.value).toContain('AmusementParkParkGraphUpsert');
    expect(parkGraphUpsertFacade.copyDraft).toHaveBeenCalledOnceWith(block);
    expect(parkGraphUpsertFacade.downloadDraft).toHaveBeenCalledOnceWith(block);
    expect(host.textContent).toContain('Importer un JSON');
    expect(importInput.accept).toContain('.json');
    expect(parkGraphUpsertFacade.importDraftFile).toHaveBeenCalledOnceWith(block, importFile);
  });

  it('clears the selected block from the close action', () => {
    publicViewModeFacade.setViewMode('adminPreview');
    publicViewModeFacade.setEditionModeEnabled(true);
    selectionFacade.selectBlock(createBlock());
    fixture.detectChanges();

    const closeButton: HTMLButtonElement = (fixture.nativeElement as HTMLElement)
      .querySelector('.admin-contextual-block-drawer__close') as HTMLButtonElement;
    closeButton.click();
    fixture.detectChanges();

    expect(selectionFacade.selectedBlock()).toBeNull();
    expect((fixture.nativeElement as HTMLElement).querySelector('.admin-contextual-block-drawer')).toBeNull();
  });
});

function createBlock(): AdminContextualBlockInstance {
  return {
    id: 'park.description:park-1',
    type: 'park.description',
    entityType: 'Park',
    entityId: 'park-1',
    contextLabel: 'Phantasialand',
    ids: { parkId: 'park-1' },
    labelKey: 'admin.contextualBlocks.blocks.parkDescription.label',
    descriptionKey: 'admin.contextualBlocks.blocks.parkDescription.description',
    iconClass: 'pi pi-align-left',
    capabilities: ['fullAdminEdit', 'boundedJsonExport', 'boundedJsonPreview', 'boundedJsonApply', 'contextualFormEdit'],
    jsonScope: ['park.id', 'park.descriptions[*].value'],
    localizedLanguageCodes: ['fr', 'en'],
    locationFallbackCenter: null,
    adminRoute: ['/', 'fr', 'admin', 'parks', 'edit', 'park-1']
  };
}

function createManufacturerBlock(): AdminContextualBlockInstance {
  return {
    id: 'reference.manufacturer:manufacturer-1',
    type: 'reference.manufacturer',
    entityType: 'AttractionManufacturer',
    entityId: 'manufacturer-1',
    contextLabel: 'Mack Rides',
    ids: { manufacturerId: 'manufacturer-1' },
    labelKey: 'admin.contextualBlocks.blocks.manufacturerReference.label',
    descriptionKey: 'admin.contextualBlocks.blocks.manufacturerReference.description',
    iconClass: 'pi pi-wrench',
    capabilities: ['fullAdminEdit', 'parkGraphUpsertDraft'],
    jsonScope: ['references.manufacturers[*].name'],
    localizedLanguageCodes: ['fr', 'en'],
    locationFallbackCenter: null,
    adminRoute: ['/', 'fr', 'admin', 'manufacturers', 'edit', 'manufacturer-1'],
    parkGraphUpsertDraftJson: '{ "documentType": "AmusementParkParkGraphUpsert" }',
    parkGraphUpsertFileName: 'manufacturer-1-manufacturer-upsert.json'
  };
}

function createLocationBlock(): AdminContextualBlockInstance {
  return {
    id: 'park.location:park-1',
    type: 'park.location',
    entityType: 'Park',
    entityId: 'park-1',
    contextLabel: 'Phantasialand',
    ids: { parkId: 'park-1' },
    labelKey: 'admin.contextualBlocks.blocks.parkLocation.label',
    descriptionKey: 'admin.contextualBlocks.blocks.parkLocation.description',
    iconClass: 'pi pi-map-marker',
    capabilities: ['fullAdminEdit', 'boundedJsonExport', 'boundedJsonPreview', 'boundedJsonApply', 'contextualFormEdit'],
    jsonScope: ['park.id', 'park.latitude', 'park.longitude'],
    localizedLanguageCodes: [],
    locationFallbackCenter: [48.85, 2.35],
    adminRoute: ['/', 'fr', 'admin', 'parks', 'edit', 'park-1']
  };
}

function createPhotoBlock(): AdminContextualBlockInstance {
  return {
    id: 'park.images:park-1',
    type: 'park.images',
    entityType: 'Park',
    entityId: 'park-1',
    contextLabel: 'Phantasialand',
    ids: { parkId: 'park-1' },
    labelKey: 'admin.contextualBlocks.blocks.parkImages.label',
    descriptionKey: 'admin.contextualBlocks.blocks.parkImages.description',
    iconClass: 'pi pi-images',
    capabilities: ['fullAdminEdit', 'contextualPhotoAdd'],
    jsonScope: ['park.id', 'image.file', 'image.sourceUrl'],
    localizedLanguageCodes: [],
    locationFallbackCenter: null,
    adminRoute: ['/', 'fr', 'admin', 'parks', 'edit', 'park-1']
  };
}

function createHeroBlock(): AdminContextualBlockInstance {
  return {
    id: 'park.hero:park-1',
    type: 'park.hero',
    entityType: 'Park',
    entityId: 'park-1',
    contextLabel: 'Phantasialand',
    ids: { parkId: 'park-1' },
    labelKey: 'admin.contextualBlocks.blocks.parkHero.label',
    descriptionKey: 'admin.contextualBlocks.blocks.parkHero.description',
    iconClass: 'pi pi-image',
    capabilities: ['fullAdminEdit', 'targetedChildAdd'],
    jsonScope: ['park.id'],
    localizedLanguageCodes: [],
    locationFallbackCenter: null,
    adminRoute: ['/', 'fr', 'admin', 'parks', 'edit', 'park-1']
  };
}
