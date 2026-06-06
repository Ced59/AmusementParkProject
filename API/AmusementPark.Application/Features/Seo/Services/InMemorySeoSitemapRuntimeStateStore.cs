using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;

namespace AmusementPark.Application.Features.Seo.Services;

/// <summary>
/// État runtime léger pour exposer une progression de génération au panneau admin.
/// </summary>
public sealed class InMemorySeoSitemapRuntimeStateStore : ISeoSitemapRuntimeStateStore
{
    private readonly object syncRoot = new object();
    private SitemapRuntimeState current = new SitemapRuntimeState();

    public SitemapRuntimeState GetCurrent()
    {
        lock (this.syncRoot)
        {
            return this.current;
        }
    }

    public bool TryStart(string step)
    {
        lock (this.syncRoot)
        {
            if (this.current.Status == SitemapGenerationStatus.Running)
            {
                return false;
            }

            DateTime now = DateTime.UtcNow;
            this.current = new SitemapRuntimeState
            {
                Status = SitemapGenerationStatus.Running,
                CurrentStep = NormalizeStep(step),
                ProgressPercentage = 1,
                StartedAtUtc = now,
                UpdatedAtUtc = now,
                Message = null,
            };

            return true;
        }
    }

    public void Update(string step, int progressPercentage, string? message = null)
    {
        lock (this.syncRoot)
        {
            this.current = new SitemapRuntimeState
            {
                Status = SitemapGenerationStatus.Running,
                CurrentStep = NormalizeStep(step),
                ProgressPercentage = Math.Clamp(progressPercentage, 0, 100),
                StartedAtUtc = this.current.StartedAtUtc,
                UpdatedAtUtc = DateTime.UtcNow,
                Message = message,
            };
        }
    }

    public void Complete(string step, string? message = null)
    {
        lock (this.syncRoot)
        {
            this.current = new SitemapRuntimeState
            {
                Status = SitemapGenerationStatus.Succeeded,
                CurrentStep = NormalizeStep(step),
                ProgressPercentage = 100,
                StartedAtUtc = this.current.StartedAtUtc,
                UpdatedAtUtc = DateTime.UtcNow,
                Message = message,
            };
        }
    }

    public void Fail(string step, string message)
    {
        lock (this.syncRoot)
        {
            this.current = new SitemapRuntimeState
            {
                Status = SitemapGenerationStatus.Failed,
                CurrentStep = NormalizeStep(step),
                ProgressPercentage = this.current.ProgressPercentage,
                StartedAtUtc = this.current.StartedAtUtc,
                UpdatedAtUtc = DateTime.UtcNow,
                Message = message,
            };
        }
    }

    private static string NormalizeStep(string step)
    {
        return string.IsNullOrWhiteSpace(step) ? "idle" : step.Trim();
    }
}
