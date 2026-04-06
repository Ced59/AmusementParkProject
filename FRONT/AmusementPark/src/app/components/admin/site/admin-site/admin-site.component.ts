import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { ImageDto } from '../../../../models/images/image-dto';
import { ImageTagDto } from '../../../../models/images/image-tag-dto';
import { ApiService } from '../../../../services/api.service';

@Component({
  selector: 'app-admin-site',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-site.component.html',
  styleUrl: './admin-site.component.scss'
})
export class AdminSiteComponent implements OnInit {
  images: ImageDto[] = [];
  tags: ImageTagDto[] = [];
  selectedImage: ImageDto | null = null;
  newTagSlug: string = '';
  loading: boolean = false;

  constructor(public readonly apiService: ApiService) {}

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading = true;
    forkJoin({
      images: this.apiService.getAdminImages(),
      tags: this.apiService.getAdminImageTags()
    }).subscribe({
      next: ({ images, tags }) => {
        this.images = images;
        this.tags = tags;
        this.selectedImage = this.selectedImage ? images.find(x => x.id === this.selectedImage!.id) ?? null : (images[0] ?? null);
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  selectImage(image: ImageDto): void {
    this.selectedImage = JSON.parse(JSON.stringify(image)) as ImageDto;
    if (!this.selectedImage.geoLocation) {
      this.selectedImage.geoLocation = { latitude: 0, longitude: 0 };
    }
  }

  saveImage(): void {
    if (!this.selectedImage) return;
    this.apiService.updateAdminImage(this.selectedImage.id, {
      description: this.selectedImage.description,
      geoLocation: this.selectedImage.geoLocation ?? null,
      altTexts: this.selectedImage.altTexts ?? [],
      captions: this.selectedImage.captions ?? [],
      credits: this.selectedImage.credits ?? [],
      tagIds: this.selectedImage.tagIds ?? [],
      isPublished: this.selectedImage.isPublished
    }).subscribe(() => this.reload());
  }

  createTag(): void {
    const slug: string = this.newTagSlug.trim().toLowerCase();
    if (!slug) return;
    this.apiService.createAdminImageTag({ slug, labels: [{ languageCode: 'fr', value: slug }], descriptions: [] }).subscribe(() => {
      this.newTagSlug = '';
      this.reload();
    });
  }

  toggleTag(tagId: string, checked: boolean): void {
    if (!this.selectedImage) return;
    const current: Set<string> = new Set(this.selectedImage.tagIds ?? []);
    if (checked) current.add(tagId); else current.delete(tagId);
    this.selectedImage.tagIds = Array.from(current);
  }

  trackById(_: number, item: { id: string }): string {
    return item.id;
  }
}
