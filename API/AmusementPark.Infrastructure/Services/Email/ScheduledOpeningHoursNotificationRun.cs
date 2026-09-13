using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Infrastructure.Configuration.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Email;

internal sealed record ScheduledOpeningHoursNotificationRun(TimeSpan Delay);
