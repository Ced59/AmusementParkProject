using System.Reflection;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Filters;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class ContextualBlocksControllerTests
{
    [Fact]
    public void ExportBlockJsonAsync_AuditUsesEntityIdRouteValue()
    {
        MethodInfo method = typeof(ContextualBlocksController).GetMethod(nameof(ContextualBlocksController.ExportBlockJsonAsync))
            ?? throw new InvalidOperationException("ContextualBlocksController.ExportBlockJsonAsync was not found.");

        AdminAuditAttribute attribute = method.GetCustomAttribute<AdminAuditAttribute>()!;

        Assert.Equal("entityId", attribute.TargetIdRouteKey);
    }
}
