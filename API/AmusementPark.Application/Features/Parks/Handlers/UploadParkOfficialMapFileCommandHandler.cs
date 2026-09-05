using System.Text;
using System.Text.RegularExpressions;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Commands;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Services;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Handlers;

public sealed class UploadParkOfficialMapFileCommandHandler
    : ICommandHandler<UploadParkOfficialMapFileCommand, ApplicationResult<ParkOfficialMapStoredFile>>
{
    public const long MaximumFileSizeInBytes = 25 * 1024 * 1024;
    private static readonly Regex SafeIdentifier = new Regex(
        "^[A-Za-z0-9][A-Za-z0-9_-]{0,79}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private readonly IParkRepository parkRepository;
    private readonly IParkOfficialMapBinaryStorage storage;

    public UploadParkOfficialMapFileCommandHandler(
        IParkRepository parkRepository,
        IParkOfficialMapBinaryStorage storage)
    {
        this.parkRepository = parkRepository;
        this.storage = storage;
    }

    public async Task<ApplicationResult<ParkOfficialMapStoredFile>> HandleAsync(
        UploadParkOfficialMapFileCommand command,
        CancellationToken cancellationToken = default)
    {
        ParkOfficialMapFileUploadRequest request = command.Request;
        if (string.IsNullOrWhiteSpace(request.ParkId)
            || !SafeIdentifier.IsMatch(request.OfficialMapId ?? string.Empty)
            || request.File.Length <= 0
            || request.File.Length > MaximumFileSizeInBytes
            || !request.File.Content.CanSeek)
        {
            return ApplicationResult<ParkOfficialMapStoredFile>.Failure(ParkApplicationErrors.InvalidOfficialMapFile());
        }

        Park? park = await this.parkRepository.GetByIdAsync(request.ParkId.Trim(), true, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<ParkOfficialMapStoredFile>.Failure(ParkApplicationErrors.ParkNotExists());
        }

        ParkOfficialMapFileType? detectedFileType = await ResolveFileTypeAsync(request.File, cancellationToken);
        if (!detectedFileType.HasValue)
        {
            return ApplicationResult<ParkOfficialMapStoredFile>.Failure(ParkApplicationErrors.UnsupportedOfficialMapFile());
        }

        ParkOfficialMapFileType fileType = detectedFileType.Value;
        string originalFileName = NormalizeFileName(request.File.FileName, fileType.Extension);
        string storageVersion = Guid.NewGuid().ToString("N");
        string storageKey = ParkOfficialMapStorageKeys.Build(
            park.Id,
            request.OfficialMapId!,
            storageVersion,
            fileType.Extension);
        FilePayload normalizedFile = new FilePayload
        {
            FileName = originalFileName,
            ContentType = fileType.ContentType,
            Length = request.File.Length,
            Content = request.File.Content,
        };

        try
        {
            await this.storage.SaveAsync(storageKey, normalizedFile, fileType.ContentType, cancellationToken);
            return ApplicationResult<ParkOfficialMapStoredFile>.Success(new ParkOfficialMapStoredFile
            {
                StorageKey = storageKey,
                OriginalFileName = originalFileName,
                ContentType = fileType.ContentType,
                SizeInBytes = request.File.Length,
                SuggestedFormat = fileType.Format,
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ApplicationResult<ParkOfficialMapStoredFile>.Failure(ParkApplicationErrors.OfficialMapFileStorageFailed());
        }
    }

    private static async Task<ParkOfficialMapFileType?> ResolveFileTypeAsync(
        FilePayload file,
        CancellationToken cancellationToken)
    {
        byte[] header = new byte[4096];
        file.Content.Position = 0;
        int bytesRead = await file.Content.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        file.Content.Position = 0;
        ReadOnlySpan<byte> bytes = header.AsSpan(0, bytesRead);

        if (bytes.StartsWith("%PDF-"u8))
        {
            return new ParkOfficialMapFileType("pdf", "application/pdf", ParkOfficialMapFormat.Pdf);
        }

        if (bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff)
        {
            return new ParkOfficialMapFileType("jpg", "image/jpeg", ParkOfficialMapFormat.Image);
        }

        if (bytes.StartsWith(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }))
        {
            return new ParkOfficialMapFileType("png", "image/png", ParkOfficialMapFormat.Image);
        }

        if (bytes.Length >= 12
            && bytes[..4].SequenceEqual("RIFF"u8)
            && bytes.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return new ParkOfficialMapFileType("webp", "image/webp", ParkOfficialMapFormat.Image);
        }

        if (bytes.StartsWith("GIF87a"u8) || bytes.StartsWith("GIF89a"u8))
        {
            return new ParkOfficialMapFileType("gif", "image/gif", ParkOfficialMapFormat.Image);
        }

        string extension = Path.GetExtension(file.FileName).TrimStart('.').ToLowerInvariant();
        if ((extension == "zip" || extension == "kmz")
            && bytes.Length >= 4
            && bytes[0] == 0x50
            && bytes[1] == 0x4b
            && bytes[2] is 0x03 or 0x05 or 0x07
            && bytes[3] is 0x04 or 0x06 or 0x08)
        {
            string contentType = extension == "kmz"
                ? "application/vnd.google-earth.kmz"
                : "application/zip";
            return new ParkOfficialMapFileType(extension, contentType, ParkOfficialMapFormat.Other);
        }

        if (extension == "kml")
        {
            string text = Encoding.UTF8.GetString(bytes);
            if (text.Contains("<kml", StringComparison.OrdinalIgnoreCase))
            {
                return new ParkOfficialMapFileType("kml", "application/vnd.google-earth.kml+xml", ParkOfficialMapFormat.Other);
            }
        }

        return null;
    }

    private static string NormalizeFileName(string? value, string extension)
    {
        string fileName = Path.GetFileName(value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return $"official-map.{extension}";
        }

        return fileName.Length <= 180 ? fileName : $"official-map.{extension}";
    }

    private readonly record struct ParkOfficialMapFileType(
        string Extension,
        string ContentType,
        ParkOfficialMapFormat Format);
}
