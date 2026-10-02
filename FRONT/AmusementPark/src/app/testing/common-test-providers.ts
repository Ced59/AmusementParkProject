import { EnvironmentProviders, importProvidersFrom, ModuleWithProviders, Provider, Type } from '@angular/core';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { MessageService } from '@shared/ui/primitives/api';

export const COMMON_TEST_IMPORTS: Array<Type<unknown> | ModuleWithProviders<unknown>> = [];

export function provideCommonTestDependencies(): Array<Provider | EnvironmentProviders> {
  return [
    importProvidersFrom(TranslateModule.forRoot()),
    provideHttpClient(withInterceptorsFromDi()),
    provideHttpClientTesting(),
    provideRouter([]),
    MessageService,
  ];
}
