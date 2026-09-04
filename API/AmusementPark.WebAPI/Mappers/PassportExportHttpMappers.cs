using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.WebAPI.Contracts.Passport;

namespace AmusementPark.WebAPI.Mappers;

internal static class PassportExportHttpMappers
{
    public static RequestPassportExportCommand ToApplication(
        this RequestPassportExportDto request,
        string userId)
    {
        PassportExportFormat format = request.Format switch
        {
            PassportExportFormatDto.Json => PassportExportFormat.Json,
            PassportExportFormatDto.Csv => PassportExportFormat.Csv,
            _ => (PassportExportFormat)(-1),
        };
        return new RequestPassportExportCommand(userId, format);
    }

    public static PassportExportDto ToHttp(this PassportExport passportExport, string basePath)
    {
        string? downloadUrl = passportExport.Status == PassportExportStatus.Ready
            ? $"{basePath}/{Uri.EscapeDataString(passportExport.Id)}?download=true"
            : null;
        return new PassportExportDto(
            passportExport.Id,
            passportExport.Format.ToString(),
            passportExport.Status.ToString(),
            passportExport.SchemaVersion,
            passportExport.CreatedAtUtc,
            passportExport.UpdatedAtUtc,
            passportExport.ExpiresAtUtc,
            passportExport.CompletedAtUtc,
            passportExport.FileName,
            passportExport.SizeBytes,
            passportExport.ErrorCode,
            downloadUrl);
    }
}
