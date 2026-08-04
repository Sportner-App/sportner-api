using Mapster;

namespace Sportner.Application.Common.Mapping;

public static class MappingConfig
{
    public static void Configure()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(MappingConfig).Assembly);
    }
}
