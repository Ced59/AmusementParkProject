using System.Security.Cryptography;
using System.Text;
using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Visits;

public enum PassportAuditOrigin
{
    User = 1,
    Import = 2,
    System = 3,
}
