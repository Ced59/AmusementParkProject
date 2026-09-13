using System.Net;
using System.Net.Http.Headers;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Images;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using DomainImage = AmusementPark.Core.Domain.Images.Image;
using ImageSharpImage = SixLabors.ImageSharp.Image;

namespace AmusementPark.Infrastructure.Services.Images;

internal sealed class DownloadedImage : IDisposable
{
    public DownloadedImage(MemoryStream content, string fileName, string contentType)
    {
        this.Content = content;
        this.FileName = fileName;
        this.ContentType = contentType;
    }

    public MemoryStream Content { get; }

    public string FileName { get; }

    public string ContentType { get; }

    public void Dispose()
    {
        this.Content.Dispose();
    }
}
