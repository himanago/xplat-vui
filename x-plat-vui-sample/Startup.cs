using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(XPlat.VUI.Sample.Startup))]
namespace XPlat.VUI.Sample
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddAssistant<ILoggableAssistant, MyAssistant>();
        }
    }
}