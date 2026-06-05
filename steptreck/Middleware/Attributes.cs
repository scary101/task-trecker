namespace steptreck.API.Middleware
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class SkipSubscriptionCheckAttribute : Attribute { }

}
