using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Configuration.Authentication;

namespace AmusementPark.Infrastructure.Services.Sharing;

public sealed class HmacSharePublicationPreviewApprovalProtector
    : ISharePublicationPreviewApprovalProtector
{
    private const string SigningPurpose = "AmusementPark.SharePublicationPreviewApproval.v2";
    private readonly byte[] signingKey;

    public HmacSharePublicationPreviewApprovalProtector(JwtSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (string.IsNullOrWhiteSpace(settings.Key))
        {
            throw new InvalidOperationException(
                "The authentication signing key is required to protect share preview approvals.");
        }

        using HMACSHA256 keyDerivation = new HMACSHA256(Encoding.UTF8.GetBytes(settings.Key));
        this.signingKey = keyDerivation.ComputeHash(Encoding.UTF8.GetBytes(SigningPurpose));
    }

    public string CreateToken(
        string ownerUserId,
        SharePublicationType publicationType,
        string sourceScopeKey,
        long sourceVersion,
        SharePublicationApprovalState publicationState,
        ShareContentPolicy contentPolicy)
    {
        byte[] digest = this.ComputeDigest(
            ownerUserId,
            publicationType,
            sourceScopeKey,
            sourceVersion,
            publicationState,
            contentPolicy);
        return Convert.ToBase64String(digest)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public bool IsValid(
        string approvalToken,
        string ownerUserId,
        SharePublicationType publicationType,
        string sourceScopeKey,
        long sourceVersion,
        SharePublicationApprovalState publicationState,
        ShareContentPolicy contentPolicy)
    {
        if (!TryDecode(approvalToken, out byte[] suppliedDigest))
        {
            return false;
        }

        byte[] expectedDigest = this.ComputeDigest(
            ownerUserId,
            publicationType,
            sourceScopeKey,
            sourceVersion,
            publicationState,
            contentPolicy);
        return suppliedDigest.Length == expectedDigest.Length
            && CryptographicOperations.FixedTimeEquals(suppliedDigest, expectedDigest);
    }

    private byte[] ComputeDigest(
        string ownerUserId,
        SharePublicationType publicationType,
        string sourceScopeKey,
        long sourceVersion,
        SharePublicationApprovalState publicationState,
        ShareContentPolicy contentPolicy)
    {
        ArgumentNullException.ThrowIfNull(contentPolicy);
        StringBuilder canonical = new StringBuilder();
        Append(canonical, ownerUserId?.Trim() ?? string.Empty);
        Append(canonical, publicationType.ToString());
        Append(canonical, sourceScopeKey?.Trim() ?? string.Empty);
        Append(canonical, sourceVersion.ToString(CultureInfo.InvariantCulture));
        Append(canonical, publicationState.PublicationId ?? string.Empty);
        Append(
            canonical,
            publicationState.PersistenceVersion?.ToString(CultureInfo.InvariantCulture)
                ?? string.Empty);
        Append(canonical, contentPolicy.SchemaVersion.ToString(CultureInfo.InvariantCulture));
        Append(canonical, contentPolicy.DatePrecision.ToString());
        foreach (ShareContentField field in contentPolicy.IncludedFields)
        {
            Append(canonical, field.ToString());
        }

        using HMACSHA256 hmac = new HMACSHA256(this.signingKey);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString()));
    }

    private static void Append(StringBuilder canonical, string value)
    {
        canonical.Append(value.Length.ToString(CultureInfo.InvariantCulture))
            .Append(':')
            .Append(value);
    }

    private static bool TryDecode(string? token, out byte[] digest)
    {
        digest = Array.Empty<byte>();
        string normalized = token?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            return false;
        }

        string base64 = normalized.Replace('-', '+').Replace('_', '/');
        int paddingLength = (4 - base64.Length % 4) % 4;
        base64 = base64.PadRight(base64.Length + paddingLength, '=');
        try
        {
            digest = Convert.FromBase64String(base64);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
