using Microsoft.Extensions.DependencyInjection;

namespace XPlat.VUI
{
    public static class XPlatVUIServiceCollectionExtensions
    {
        public static IServiceCollection AddAssistant<T1, T2>(this IServiceCollection services)
           where T1 : class, IAssistant
           where T2 : AssistantBase, T1, new()
        {
            var assistant = new T2();
            return services.AddSingleton<T1, T2>(_ => assistant);
        }
    }
}
