import { MeasurementConversionService } from './measurement-conversion.service';

describe('MeasurementConversionService', () => {
  const service = new MeasurementConversionService();

  it('formats metric values by default', () => {
    expect(service.formatLengthFromMeters(60.96, 'Metric', 'en')).toBe('61 m');
    expect(service.formatSpeedFromKilometersPerHour(120.7, 'Metric', 'en')).toBe('120.7 km/h');
    expect(service.formatAccessHeightFromCentimeters(121.92, 'Metric', 'en')).toBe('121.9 cm');
    expect(service.formatDistanceFromKilometers(4.26, 'Metric', 'en')).toBe('4.3 km');
    expect(service.formatTemperatureFromCelsius(21.4, 'Metric', 'en')).toBe('21\u00b0C');
  });

  it('formats values with imperial units when requested', () => {
    expect(service.formatLengthFromMeters(60.96, 'Imperial', 'en')).toBe('200 ft');
    expect(service.formatSpeedFromKilometersPerHour(120.7, 'Imperial', 'en')).toBe('75 mph');
    expect(service.formatAccessHeightFromCentimeters(121.92, 'Imperial', 'en')).toBe('4 ft');
    expect(service.formatDistanceFromKilometers(4.26, 'Imperial', 'en')).toBe('2.6 mi');
    expect(service.formatTemperatureFromCelsius(21.4, 'Imperial', 'en')).toBe('71\u00b0F');
    expect(service.formatTemperatureFromCelsius(-5, 'Imperial', 'en')).toBe('23\u00b0F');
  });
});
