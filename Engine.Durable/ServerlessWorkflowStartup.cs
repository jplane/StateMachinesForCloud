// Reference: https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection
[assembly: Microsoft.Azure.Functions.Extensions.DependencyInjection.FunctionsStartup(
    typeof(SM4C.Engine.Durable.ServerlessWorkflowSetup))]

namespace SM4C.Engine.Durable
{
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Azure.WebJobs.Script.Description;
    using Microsoft.Extensions.DependencyInjection;

    class ServerlessWorkflowSetup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<IFunctionProvider, ServerlessWorkflowFunctions>();
        }
    }
}
