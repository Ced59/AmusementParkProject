namespace WebAPI.Settings.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class SwaggerOrderAttribute : Attribute
{
    public int Order { get; private set; }

    public SwaggerOrderAttribute(int order)
    {
        Order = order;
    }
}