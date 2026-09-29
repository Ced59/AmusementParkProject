import { PublicLiveForecastCardComponent } from './public-live-forecast-card.component';

describe('PublicLiveForecastCardComponent', () => {
  it('formats the public time range in the park timezone', () => {
    const component: PublicLiveForecastCardComponent = new PublicLiveForecastCardComponent();
    component.currentLanguage = 'fr';
    const view = component as unknown as {
      timeRange: (forecast: {
        forecastFromUtc: string;
        forecastToUtc: string;
        timeZoneId: string;
      }) => string;
    };

    expect(view.timeRange({
      forecastFromUtc: '2026-09-29T13:00:00Z',
      forecastToUtc: '2026-09-29T14:00:00Z',
      timeZoneId: 'Europe/Paris'
    })).toMatch(/15:00.*16:00/);
  });

  it('uses a localized explanation instead of exposing a technical method identifier', () => {
    const component: PublicLiveForecastCardComponent = new PublicLiveForecastCardComponent();
    const view = component as unknown as {
      methodLabelKey: (method: string) => string;
    };

    expect(view.methodLabelKey('rolling-weekday-hour-median-v1'))
      .toBe('liveData.forecast.method.weekdayHourMedian');
    expect(view.methodLabelKey('future-method'))
      .toBe('liveData.forecast.method.verifiedHistorical');
  });
});
