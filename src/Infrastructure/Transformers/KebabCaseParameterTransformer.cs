using Microsoft.AspNetCore.Routing;

namespace Sportner.Infrastructure.Transformers;

public class KebabCaseParameterTransformer : IOutboundParameterTransformer
{
    public string TransformOutbound(object? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        // Convert PascalCase to kebab-case
        return string.Concat(value.ToString()!
            .Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x : x.ToString()))
            .ToLowerInvariant();
    }
}
