using Microsoft.Extensions.DependencyInjection;

namespace XPlat.VUI
{
    public static class XPlatVUIServiceCollectionExtensions
    {
        public static IServiceCollection AddAssistant<T1, T2>(this IServiceCollection services)
           where T1 : class, IAssistant
           where T2 : AssistantBase, T1
        {
            return services.AddScoped<T1, T2>();
        }
    }
}
