import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { resolveLocalizedValue } from '../../commons/localized-item.utils';
import { Park } from '../../models/parks/park';
import { ApiService } from '../../services/api.service';
import { TranslationService } from '../../services/translation.service';

@Component({
  selector: 'app-park-detail',
  templateUrl: './park-detail.component.html',
  styleUrls: ['./park-detail.component.scss']
})
export class ParkDetailComponent implements OnInit, OnDestroy {
  park: Park | undefined;
  currentLang: string = 'en';

  private readonly subscriptions: Subscription = new Subscription();

  get localizedDescription(): string | undefined {
    return resolveLocalizedValue(this.park?.descriptions, this.currentLang);
  }

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly apiService: ApiService,
    private readonly translationService: TranslationService
  ) {}

  ngOnInit(): void {
    this.subscriptions.add(
      this.route.paramMap.subscribe((params: ParamMap) => {
        const id: string | null = params.get('id');

        if (id && this.park?.id !== id) {
          this.loadPark(id);
        }
      })
    );

    if (this.route.parent) {
      this.subscriptions.add(
        this.route.parent.paramMap.subscribe((params: ParamMap) => {
          const lang: string = params.get('lang') ?? 'en';
          this.currentLang = lang;
        })
      );
    }

    this.subscriptions.add(
      this.translationService.languageChanged.subscribe((lang: string) => {
        this.currentLang = lang;
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  goBack(): void {
    this.router.navigate([`/${this.currentLang}/parks`]);
  }

  private loadPark(id: string): void {
    this.apiService.getParkById(id).subscribe((park: Park) => {
      this.park = park;
    });
  }
}
