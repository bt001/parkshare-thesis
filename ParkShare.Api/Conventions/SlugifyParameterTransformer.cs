namespace ParkShare.Api.Conventions
{
    public class SlugifyParameterTransformer : IOutboundParameterTransformer
    {
        public string? TransformOutbound(object? value)
            => value?.ToString()?.ToLowerInvariant();
    }
}
