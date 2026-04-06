import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { TranslateModule } from '@ngx-translate/core';
import { ImageDto } from '../../../../models/images/image-dto';
import { ImageTagDto } from '../../../../models/images/image-tag-dto';
import { ViewState } from '../../../../models/shared/view-state';
import { ApiService } from '../../../../services/api.service';
import { PageStateComponent } from '../../../shared/page-state/page-state.component';

@Component({
  selector: 'app-admin-site',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule, PageStateComponent],
  templateUrl: './admin-site.component.html',
  styleUrl: './admin-site.component.scss'
})
export class AdminSiteComponent implements OnInit {
  images: ImageDto[] = [];
  tags: ImageTagDto[] = [];
  selectedImage: ImageDto | null = null;
  newTagSlug: string = '';
  pageState: ViewState = ViewState.Loading;

  constructor(public readonly apiService: ApiService) {}

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.pageState = ViewState.Loading;

    forkJoin({
      images: this.apiService.getAdminImages(),
      tags: this.apiService.getAdminImageTags()
    }).subscribe({
      next: ({ images, tags }) => {
        this.images = images;
        this.tags = tags;

        if (this.selectedImage) {
          const refreshedSelection: ImageDto | undefined = images.find((image: ImageDto) => image.id === this.selectedImage?.id);
          this.selectedImage = refreshedSelection ? this.cloneImage(refreshedSelection) : (images[0] ? this.cloneImage(images[0]) : null);
        } else {
          this.selectedImage = images[0] ? this.cloneImage(images[0]) : null;
        }

        this.pageState = ViewState.Ready;
      },
      error: () => {
        this.pageState = ViewState.Error;
      }
    });
  }

  selectImage(image: ImageDto): void {
    this.selectedImage = this.cloneImage(image);
  }

  saveImage(): void {
    if (!this.selectedImage) {
      return;
    }

    this.apiService.updateAdminImage(this.selectedImage.id, {
      description: this.selectedImage.description,
      geoLocation: this.selectedImage.geoLocation ?? null,
      altTexts: this.selectedImage.altTexts ?? [],
      captions: this.selectedImage.captions ?? [],
      credits: this.selectedImage.credits ?? [],
      tagIds: this.selectedImage.tagIds ?? [],
      isPublished: this.selectedImage.isPublished
    }).subscribe({
      next: () => {
        this.reload();
      },
      error: () => {
        this.pageState = ViewState.Error;
      }
    });
  }

  createTag(): void {
    const slug: string = this.newTagSlug.trim().toLowerCase();

    if (!slug) {
      return;
    }

    this.apiService.createAdminImageTag({
      slug,
      labels: [{ languageCode: 'fr', value: slug }],
      descriptions: []
    }).subscribe({
      next: () => {
        this.newTagSlug = '';
        this.reload();
      },
      error: () => {
        this.pageState = ViewState.Error;
      }
    });
  }

  toggleTag(tagId: string, checked: boolean): void {
    if (!this.selectedImage) {
      return;
    }

    const current: Set<string> = new Set(this.selectedImage.tagIds ?? []);

    if (checked) {
      current.add(tagId);
    } else {
      current.delete(tagId);
    }

    this.selectedImage.tagIds = Array.from(current);
  }

  trackById(_: number, item: { id: string }): string {
    return item.id;
  }

  private cloneImage(image: ImageDto): ImageDto {
    const clonedImage: ImageDto = JSON.parse(JSON.stringify(image)) as ImageDto;

    if (!clonedImage.geoLocation) {
      clonedImage.geoLocation = { latitude: 0, longitude: 0 };
    }

    return clonedImage;
  }
}
