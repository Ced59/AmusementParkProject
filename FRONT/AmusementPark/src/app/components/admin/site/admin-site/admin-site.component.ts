import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { ImageDto } from '@app/models/images/image-dto';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { PageStateComponent } from '../../../shared/page-state/page-state.component';
import { ImageDisplayComponent } from '../../../shared/image-display/image-display.component';
import { AdminSiteStateFacade } from '@features/admin/site/state/admin-site-state.facade';

@Component({
  selector: 'app-admin-site',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule, PageStateComponent, ImageDisplayComponent],
  templateUrl: './admin-site.component.html',
  styleUrl: './admin-site.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminSiteStateFacade]
})
export class AdminSiteComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly images = this.stateFacade.images;
  protected readonly tags = this.stateFacade.tags;
  protected readonly selectedImage = this.stateFacade.selectedImage;
  newTagSlug: string = '';

  constructor(
    public readonly imagesApiService: ImagesApiService,
    private readonly stateFacade: AdminSiteStateFacade
  ) {
  }

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.stateFacade.reload();
  }

  selectImage(image: ImageDto): void {
    this.stateFacade.selectImage(image);
  }

  saveImage(): void {
    const selectedImage: ImageDto | null = this.selectedImage();

    if (!selectedImage) {
      return;
    }

    this.imagesApiService.updateAdminImage(selectedImage.id, {
      description: selectedImage.description,
      geoLocation: selectedImage.geoLocation ?? null,
      altTexts: selectedImage.altTexts ?? [],
      captions: selectedImage.captions ?? [],
      credits: selectedImage.credits ?? [],
      tagIds: selectedImage.tagIds ?? [],
      isPublished: selectedImage.isPublished
    }).subscribe({
      next: () => {
        this.reload();
      },
      error: () => {
        this.stateFacade.setError();
      }
    });
  }

  createTag(): void {
    const slug: string = this.newTagSlug.trim().toLowerCase();

    if (!slug) {
      return;
    }

    this.imagesApiService.createAdminImageTag({
      slug,
      labels: [{ languageCode: 'fr', value: slug }],
      descriptions: []
    }).subscribe({
      next: () => {
        this.newTagSlug = '';
        this.reload();
      },
      error: () => {
        this.stateFacade.setError();
      }
    });
  }

  updateSelectedImageDescription(value: string): void {
    this.stateFacade.updateSelectedImage({ description: value });
  }

  updateSelectedImagePublished(isPublished: boolean): void {
    this.stateFacade.updateSelectedImage({ isPublished });
  }

  updateSelectedImageLatitude(latitude: number): void {
    const selectedImage: ImageDto | null = this.selectedImage();

    if (!selectedImage?.geoLocation) {
      return;
    }

    this.stateFacade.updateSelectedImage({
      geoLocation: {
        ...selectedImage.geoLocation,
        latitude
      }
    });
  }

  updateSelectedImageLongitude(longitude: number): void {
    const selectedImage: ImageDto | null = this.selectedImage();

    if (!selectedImage?.geoLocation) {
      return;
    }

    this.stateFacade.updateSelectedImage({
      geoLocation: {
        ...selectedImage.geoLocation,
        longitude
      }
    });
  }

  toggleTag(tagId: string, checked: boolean): void {
    this.stateFacade.toggleTag(tagId, checked);
  }

  trackById(_: number, item: { id: string }): string {
    return item.id;
  }
}
