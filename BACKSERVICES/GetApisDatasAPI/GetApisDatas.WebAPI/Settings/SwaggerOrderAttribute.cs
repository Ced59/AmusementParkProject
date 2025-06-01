namespace GetApisDatas.WebAPI.Settings
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SwaggerOrderAttribute : Attribute
    {
        public int Order { get; }

        public SwaggerOrderAttribute(int order)
        {
            Order = order;
        }
    }

}
