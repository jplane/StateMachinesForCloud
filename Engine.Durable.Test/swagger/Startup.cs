using AzureFunctions.Extensions.Swashbuckle;
using SM4C.Engine.Durable.TestApp;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace SM4C.Engine.Durable.TestApp
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.AddSwashBuckle(this.GetType().Assembly);
        }
    }
}
