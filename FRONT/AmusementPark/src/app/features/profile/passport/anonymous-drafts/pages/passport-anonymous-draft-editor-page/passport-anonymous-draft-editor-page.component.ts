import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { PassportRideOccurrenceStatus } from '@app/models/passport/passport-ride-occurrence.models';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { Dialog } from '@shared/ui/primitives/dialog';
import { UiButtonDirective, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { PassportAttractionSelectionDraft, PassportVisitEditorAttraction } from '../../../models/passport-visit-editor.models';
import { PassportAnonymousRideDraft } from '../../models/passport-anonymous-draft.models';
import { PassportAnonymousDraftEditorStateFacade } from '../../state/passport-anonymous-draft-editor-state.facade';

type PassportAnonymousRideForm = FormGroup<{
  status: FormControl<PassportRideOccurrenceStatus>;
  count: FormControl<number>;
  localTime: FormControl<string>;
  isApproximate: FormControl<boolean>;
  privateNote: FormControl<string>;
  confirmHistoricalConflict: FormControl<boolean>;
}>;

@Component({
  selector: 'app-passport-anonymous-draft-editor-page',
  templateUrl: './passport-anonymous-draft-editor-page.component.html',
  styleUrl: './passport-anonymous-draft-editor-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PassportAnonymousDraftEditorStateFacade],
  imports: [
    Dialog,
    PageStateComponent,
    ReactiveFormsModule,
    TranslateModule,
    UiButtonDirective,
    UiKickerComponent,
    UiSurfaceDirective
  ]
})
export class PassportAnonymousDraftEditorPageComponent implements OnInit {
  protected readonly facade = inject(PassportAnonymousDraftEditorStateFacade);
  protected readonly attractionSearch = new FormControl<string>('', { nonNullable: true });
  protected readonly selectedAttraction = signal<PassportVisitEditorAttraction | null>(null);
  protected readonly deleteConfirmationVisible = signal<boolean>(false);
  protected readonly rideForm: PassportAnonymousRideForm = new FormGroup({
    status: new FormControl<PassportRideOccurrenceStatus>('Completed', { nonNullable: true }),
    count: new FormControl<number>(1, { nonNullable: true }),
    localTime: new FormControl<string>('', { nonNullable: true }),
    isApproximate: new FormControl<boolean>(false, { nonNullable: true }),
    privateNote: new FormControl<string>('', { nonNullable: true }),
    confirmHistoricalConflict: new FormControl<boolean>(false, { nonNullable: true })
  });

  private readonly route: ActivatedRoute = inject(ActivatedRoute);
  private readonly router: Router = inject(Router);

  ngOnInit(): void {
    void this.facade.load(this.route.snapshot.paramMap.get('draftId') ?? '');
  }

  protected search(): void {
    this.selectedAttraction.set(null);
    this.facade.searchAttractions(this.attractionSearch.value);
  }

  protected selectAttraction(attraction: PassportVisitEditorAttraction): void {
    this.selectedAttraction.set(attraction);
    this.rideForm.controls.confirmHistoricalConflict.setValue(false);
  }

  protected async addRide(): Promise<void> {
    const attraction: PassportVisitEditorAttraction | null = this.selectedAttraction();
    if (!attraction) {
      return;
    }

    const values = this.rideForm.getRawValue();
    const selection: PassportAttractionSelectionDraft = {
      parkItemId: attraction.id,
      attractionName: attraction.name,
      status: values.status,
      count: values.count,
      localTime: values.localTime,
      isApproximate: values.isApproximate,
      privateNote: values.privateNote,
      confirmHistoricalConflict: values.confirmHistoricalConflict
    };
    if (!await this.facade.addRide(selection)) {
      return;
    }
    this.selectedAttraction.set(null);
    this.rideForm.reset({
      status: 'Completed',
      count: 1,
      localTime: '',
      isApproximate: false,
      privateNote: '',
      confirmHistoricalConflict: false
    });
  }

  protected backToDrafts(): void {
    void this.router.navigate(['/', this.currentLanguage(), 'passport', 'local']);
  }

  protected async deleteDraft(): Promise<void> {
    if (await this.facade.deleteDraft()) {
      this.deleteConfirmationVisible.set(false);
      this.backToDrafts();
    }
  }

  protected trackAttraction(_index: number, attraction: PassportVisitEditorAttraction): string {
    return attraction.id;
  }

  protected trackRide(_index: number, ride: PassportAnonymousRideDraft): string {
    return ride.id;
  }

  private currentLanguage(): string {
    return this.router.url.split('/')[1] || 'en';
  }
}
