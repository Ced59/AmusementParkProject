namespace WebAPI.Settings.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SwaggerOrderAttribute : Attribute
    {
        public SwaggerOrderAttribute(int order)
    {
        Order = order;
    }

        public int Order { get; private set; }
    }
}