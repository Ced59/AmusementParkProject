using System.Reflection;
using AmusementPark.Application.Features.Parks.Handlers;
using AmusementPark.WebAPI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class ParkDataEditorOfficialMapsControllerTests
{
    [Fact]
    public void UploadAsync_ShouldAllowTwentyFiveMebibyteFileWithMultipartEnvelope()
    {
        MethodInfo method = typeof(ParkDataEditorOfficialMapsController)
            .GetMethod(nameof(ParkDataEditorOfficialMapsController.UploadAsync))
            ?? throw new InvalidOperationException("The official-map upload endpoint was not found.");
        CustomAttributeData requestSizeLimit = method.CustomAttributes
            .Single(attribute => attribute.AttributeType == typeof(RequestSizeLimitAttribute));
        long requestSizeLimitBytes = Assert.IsType<long>(
            requestSizeLimit.ConstructorArguments.Single().Value);

        Assert.Equal(
            UploadParkOfficialMapFileCommandHandler.MaximumFileSizeInBytes + (64 * 1024),
            requestSizeLimitBytes);
        Assert.True(requestSizeLimitBytes > UploadParkOfficialMapFileCommandHandler.MaximumFileSizeInBytes);
    }
}
