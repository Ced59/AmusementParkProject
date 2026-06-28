import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ProgressBar } from 'primeng/progressbar';

import {
  AdminPhotoBatchOwnerKind,
  AdminPhotoBatchParkItemOption,
  AdminPhotoBatchPhoto,
  AdminPhotoBatchUploadSelection
} from '../../../models/admin-photo-batch.model';
import { AdminPhotoBatchStateFacade } from '../../../state/admin-photo-batch-state.facade';
import { AdminPhotoBatchCardComponent } from './admin-photo-batch-card.component';

@Component({
  selector: 'app-admin-photo-batch',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, TranslateModule, ProgressBar, AdminPhotoBatchCardComponent],
  templateUrl: './admin-photo-batch.component.html',
  styleUrl: './admin-photo-batch.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminPhotoBatchStateFacade]
})
export class AdminPhotoBatchComponent implements OnInit {
  protected readonly parks = this.stateFacade.parks;
  protected readonly parksLoading = this.stateFacade.parksLoading;
  protected readonly parkSearch = this.stateFacade.parkSearch;
  protected readonly selectedParkId = this.stateFacade.selectedParkId;
  protected readonly selectedParkName = this.stateFacade.selectedParkName;
  protected readonly parkItems = this.stateFacade.parkItems;
  protected readonly parkItemsLoading = this.stateFacade.parkItemsLoading;
  protected readonly photosLoading = this.stateFacade.photosLoading;
  protected readonly selectedFiles = this.stateFacade.selectedFiles;
  protected readonly selectedFileCount = this.stateFacade.selectedFileCount;
  protected readonly selectedFilesAnalyzing = this.stateFacade.selectedFilesAnalyzing;
  protected readonly selectedFilesWithoutGeoCount = this.stateFacade.selectedFilesWithoutGeoCount;
  protected readonly uploading = this.stateFacade.uploading;
  protected readonly uploadProgress = this.stateFacade.uploadProgress;
  protected readonly uploadPercent = this.stateFacade.uploadPercent;
  protected readonly withWatermark = this.stateFacade.withWatermark;
  protected readonly categorySets = this.stateFacade.categorySets;
  protected readonly uncategorizedPhotos = this.stateFacade.uncategorizedPhotos;
  protected readonly parkPhotos = this.stateFacade.parkPhotos;
  protected readonly parkItemPhotos = this.stateFacade.parkItemPhotos;

  constructor(
    private readonly stateFacade: AdminPhotoBatchStateFacade,
    private readonly router: Router
  ) {
  }

  ngOnInit(): void {
    this.stateFacade.loadInitialData();
  }

  protected get currentLang(): string {
    return this.router.url.split('/')[1] || 'en';
  }

  protected updateParkSearch(query: string): void {
    this.stateFacade.setParkSearch(query);
  }

  protected searchParks(): void {
    this.stateFacade.loadParks();
  }

  protected selectPark(parkId: string): void {
    this.stateFacade.selectPark(parkId);
  }

  protected refreshSelectedPark(): void {
    this.stateFacade.refreshSelectedPark();
  }

  protected selectFiles(event: Event): void {
    this.stateFacade.selectFiles(event);
  }

  protected removeSelectedFile(selectionId: string): void {
    this.stateFacade.removeSelectedFile(selectionId);
  }

  protected clearSelectedFiles(): void {
    this.stateFacade.clearSelectedFiles();
  }

  protected setWithWatermark(value: boolean): void {
    this.stateFacade.setWithWatermark(value);
  }

  protected uploadSelectedFiles(): void {
    void this.stateFacade.uploadSelectedFiles();
  }

  protected setPhotoDraftOwnerKind(event: { photoId: string; ownerKind: AdminPhotoBatchOwnerKind }): void {
    this.stateFacade.setPhotoDraftOwnerKind(event.photoId, event.ownerKind);
  }

  protected setPhotoDraftParkItem(event: { photoId: string; parkItemId: string }): void {
    this.stateFacade.setPhotoDraftParkItemId(event.photoId, event.parkItemId);
  }

  protected setPhotoDraftCategory(event: { photoId: string; categorySlug: string }): void {
    this.stateFacade.setPhotoDraftCategorySlug(event.photoId, event.categorySlug);
  }

  protected savePhotoCategorization(photoId: string): void {
    void this.stateFacade.savePhotoCategorization(photoId);
  }

  protected movePhotoToUncategorized(photoId: string): void {
    void this.stateFacade.movePhotoToUncategorized(photoId);
  }

  protected togglePublished(photoId: string): void {
    void this.stateFacade.togglePublished(photoId);
  }

  protected trackSelectionById(_: number, selection: AdminPhotoBatchUploadSelection): string {
    return selection.id;
  }

  protected trackPhotoById(_: number, photo: AdminPhotoBatchPhoto): string {
    return photo.id;
  }

  protected trackParkItemById(_: number, item: AdminPhotoBatchParkItemOption): string {
    return item.id;
  }

  protected formatBytes(value: number | null | undefined): string {
    const bytes: number = value ?? 0;
    if (bytes <= 0) {
      return '-';
    }

    const units: string[] = ['B', 'KB', 'MB', 'GB'];
    let size: number = bytes;
    let unitIndex: number = 0;

    while (size >= 1024 && unitIndex < units.length - 1) {
      size = size / 1024;
      unitIndex++;
    }

    return `${size.toFixed(unitIndex === 0 ? 0 : 1)} ${units[unitIndex]}`;
  }

  protected hasSelectionGeo(selection: AdminPhotoBatchUploadSelection): boolean {
    return Number.isFinite(selection.geoLocation?.latitude) && Number.isFinite(selection.geoLocation?.longitude);
  }
}
