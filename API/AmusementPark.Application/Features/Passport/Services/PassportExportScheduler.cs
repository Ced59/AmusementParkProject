using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Passport.Models;

namespace AmusementPark.Application.Features.Passport.Services;

public sealed class PassportExportScheduler
{
    private readonly IDurableBackgroundJobRepository jobRepository;

    public PassportExportScheduler(IDurableBackgroundJobRepository jobRepository)
    {
        this.jobRepository = jobRepository;
    }

    public Task<DurableBackgroundJob> ScheduleAsync(
        PassportExport passportExport,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(passportExport);
        PassportExportJobPayload payload = new PassportExportJobPayload(
            passportExport.Id,
            passportExport.UserId,
            passportExport.Format);
        return this.jobRepository.EnqueueExactAsync(
            new EnqueueExactBackgroundJobRequest(
                PassportExportJob.Kind,
                $"passport-export:{passportExport.Id}",
                PassportExportJob.PayloadVersion,
                JsonSerializer.SerializeToElement(payload)),
            cancellationToken);
    }
}
