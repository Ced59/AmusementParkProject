using AmusementPark.Application.Features.Passport.Models;

namespace AmusementPark.Application.Features.Passport.Ports;

public interface IVisitExportWriter
{
    PassportExportArtifact Write(PassportExportWriteRequest request);
}
