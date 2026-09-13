using System.Net;
using System.Text;

namespace AmusementPark.Infrastructure.Services.Email;

public sealed record BrandedEmailAction(string Label, string Url);
