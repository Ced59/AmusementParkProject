import { Signal, WritableSignal, computed, signal } from '@angular/core';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import {
  createEmptyScreenState,
  createErrorScreenState,
  createLoadingScreenState,
  createReadyScreenState
} from './screen-state.helpers';

export class SignalScreenStateStore<TData, TError = string> {
  private readonly stateSignal: WritableSignal<ScreenState<TData, TError>> = signal(createLoadingScreenState<TData, TError>());

  public readonly state: Signal<ScreenState<TData, TError>> = this.stateSignal.asReadonly();
  public readonly kind = computed(() => this.stateSignal().kind);
  public readonly data = computed(() => this.stateSignal().data);
  public readonly error = computed(() => this.stateSignal().error);
  public readonly isLoading = computed(() => this.stateSignal().kind === 'loading');
  public readonly isReady = computed(() => this.stateSignal().kind === 'ready');
  public readonly isEmpty = computed(() => this.stateSignal().kind === 'empty');
  public readonly isError = computed(() => this.stateSignal().kind === 'error');

  setLoading(data?: TData): void {
    this.stateSignal.set(createLoadingScreenState<TData, TError>(data));
  }

  setReady(data: TData): void {
    this.stateSignal.set(createReadyScreenState<TData, TError>(data));
  }

  setEmpty(data?: TData): void {
    this.stateSignal.set(createEmptyScreenState<TData, TError>(data));
  }

  setError(error: TError, data?: TData): void {
    this.stateSignal.set(createErrorScreenState<TData, TError>(error, data));
  }
}
