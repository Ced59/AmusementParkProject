using System.Collections.Concurrent;
using System.Text;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Infrastructure.Configuration.Images;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace AmusementPark.Infrastructure.Services.Images;

internal sealed class SocialPreviewGenerationLockEntry
{
    public object SyncRoot { get; } = new object();

    public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);

    public int ReferenceCount { get; set; }
}
