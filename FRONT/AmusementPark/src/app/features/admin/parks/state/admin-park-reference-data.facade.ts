import { DestroyRef, Injectable, Signal, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { CountriesApiService } from '@data-access/countries/countries-api.service';
import { ParkFoundersApiService } from '@data-access/parks/park-founders-api.service';
import { ParkOperatorsApiService } from '@data-access/parks/park-operators-api.service';
import { CountryDto } from '@app/models/countries/country-dto';
import { ParkFounder } from '@app/models/parks/park-founder';
import { ParkOperator } from '@app/models/parks/park-operator';
import { EntitySelectOption } from '@app/models/shared/entity-select-option';
import { AdminParkCountryOption } from '../models/admin-park-edit.model';

@Injectable()
export class AdminParkReferenceDataFacade {
  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private readonly countryOptionsSignal = signal<AdminParkCountryOption[]>([]);
  private readonly founderOptionsSignal = signal<EntitySelectOption[]>([]);
  private readonly operatorOptionsSignal = signal<EntitySelectOption[]>([]);
  private readonly countriesLoadingSignal = signal(false);
  private readonly foundersLoadingSignal = signal(false);
  private readonly operatorsLoadingSignal = signal(false);

  public readonly countryOptions: Signal<AdminParkCountryOption[]> = this.countryOptionsSignal.asReadonly();
  public readonly founderOptions: Signal<EntitySelectOption[]> = this.founderOptionsSignal.asReadonly();
  public readonly operatorOptions: Signal<EntitySelectOption[]> = this.operatorOptionsSignal.asReadonly();
  public readonly countriesLoading: Signal<boolean> = this.countriesLoadingSignal.asReadonly();
  public readonly foundersLoading: Signal<boolean> = this.foundersLoadingSignal.asReadonly();
  public readonly operatorsLoading: Signal<boolean> = this.operatorsLoadingSignal.asReadonly();

  constructor(
    private readonly countriesApiService: CountriesApiService,
    private readonly parkFoundersApiService: ParkFoundersApiService,
    private readonly parkOperatorsApiService: ParkOperatorsApiService
  ) {
  }

  load(languageCode: string): void {
    this.loadCountries(languageCode);
    this.loadFounders();
    this.loadOperators();
  }

  private loadCountries(languageCode: string): void {
    this.countriesLoadingSignal.set(true);

    this.countriesApiService.getCountries(languageCode)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (countries: CountryDto[]) => {
          this.countryOptionsSignal.set(countries.map((country: CountryDto) => ({
            code: country.isoCode,
            label: country.name
          })));
          this.countriesLoadingSignal.set(false);
        },
        error: (error: unknown) => {
          console.error('Error loading countries', error);
          this.countriesLoadingSignal.set(false);
        }
      });
  }

  private loadFounders(): void {
    this.foundersLoadingSignal.set(true);

    this.parkFoundersApiService.getParkFounders()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (founders: ParkFounder[]) => {
          this.founderOptionsSignal.set(founders.map((founder: ParkFounder) => ({
            id: founder.id ?? '',
            label: founder.name
          })));
          this.foundersLoadingSignal.set(false);
        },
        error: (error: unknown) => {
          console.error('Error loading founders', error);
          this.foundersLoadingSignal.set(false);
        }
      });
  }

  private loadOperators(): void {
    this.operatorsLoadingSignal.set(true);

    this.parkOperatorsApiService.getParkOperators()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (operators: ParkOperator[]) => {
          this.operatorOptionsSignal.set(operators.map((parkOperator: ParkOperator) => ({
            id: parkOperator.id ?? '',
            label: parkOperator.name
          })));
          this.operatorsLoadingSignal.set(false);
        },
        error: (error: unknown) => {
          console.error('Error loading operators', error);
          this.operatorsLoadingSignal.set(false);
        }
      });
  }
}
