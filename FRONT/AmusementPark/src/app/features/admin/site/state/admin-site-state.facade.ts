import { Injectable, Signal, computed } from '@angular/core';
import { forkJoin } from 'rxjs';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageTagDto } from '@app/models/images/image-tag-dto';

interface AdminSiteViewModel {
  images: ImageDto[];
  tags: ImageTagDto[];
  selectedImage: ImageDto | null;
}

@Injectable()
export class AdminSiteStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<AdminSiteViewModel>();

  public readonly state = this.screenStateStore.state;
  public readonly images: Signal<ImageDto[]> = computed(() => this.screenStateStore.data()?.images ?? []);
  public readonly tags: Signal<ImageTagDto[]> = computed(() => this.screenStateStore.data()?.tags ?? []);
  public readonly selectedImage: Signal<ImageDto | null> = computed(() => this.screenStateStore.data()?.selectedImage ?? null);

  constructor(private readonly imagesApiService: ImagesApiService) {
  }

  reload(): void {
    const previousData: AdminSiteViewModel | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    forkJoin({
      images: this.imagesApiService.getAdminImages(),
      tags: this.imagesApiService.getAdminImageTags()
    }).subscribe({
      next: ({ images, tags }: { images: ImageDto[]; tags: ImageTagDto[] }) => {
        const previousSelectionId: string | null = previousData?.selectedImage?.id ?? null;
        const selectedImage: ImageDto | null = this.resolveSelectedImage(images, previousSelectionId);
        const viewModel: AdminSiteViewModel = {
          images,
          tags,
          selectedImage
        };

        this.screenStateStore.setReady(viewModel);
      },
      error: (error: unknown) => {
        console.error('Error loading admin site data', error);
        this.screenStateStore.setError('common.errorMessage', previousData);
      }
    });
  }

  selectImage(image: ImageDto): void {
    const currentData: AdminSiteViewModel | undefined = this.screenStateStore.data();

    if (!currentData) {
      return;
    }

    this.screenStateStore.setReady({
      ...currentData,
      selectedImage: this.cloneImage(image)
    });
  }

  updateSelectedImage(patch: Partial<ImageDto>): void {
    const currentData: AdminSiteViewModel | undefined = this.screenStateStore.data();

    if (!currentData?.selectedImage) {
      return;
    }

    this.screenStateStore.setReady({
      ...currentData,
      selectedImage: {
        ...currentData.selectedImage,
        ...patch
      }
    });
  }

  toggleTag(tagId: string, checked: boolean): void {
    const currentData: AdminSiteViewModel | undefined = this.screenStateStore.data();

    if (!currentData?.selectedImage) {
      return;
    }

    const currentTags: Set<string> = new Set(currentData.selectedImage.tagIds ?? []);

    if (checked) {
      currentTags.add(tagId);
    } else {
      currentTags.delete(tagId);
    }

    this.screenStateStore.setReady({
      ...currentData,
      selectedImage: {
        ...currentData.selectedImage,
        tagIds: Array.from(currentTags)
      }
    });
  }

  setError(): void {
    this.screenStateStore.setError('common.errorMessage', this.screenStateStore.data());
  }

  private resolveSelectedImage(images: ImageDto[], previousSelectionId: string | null): ImageDto | null {
    if (previousSelectionId) {
      const refreshedSelection: ImageDto | undefined = images.find((image: ImageDto) => image.id === previousSelectionId);

      if (refreshedSelection) {
        return this.cloneImage(refreshedSelection);
      }
    }

    if (images[0]) {
      return this.cloneImage(images[0]);
    }

    return null;
  }

  private cloneImage(image: ImageDto): ImageDto {
    const clonedImage: ImageDto = JSON.parse(JSON.stringify(image)) as ImageDto;

    if (!clonedImage.geoLocation) {
      clonedImage.geoLocation = { latitude: 0, longitude: 0 };
    }

    return clonedImage;
  }
}
