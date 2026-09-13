using AmusementPark.Application.Common.Contracts;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

internal sealed class TestMongoGeolocatedDocument : MongoGeolocatedDocumentBase
{
}
