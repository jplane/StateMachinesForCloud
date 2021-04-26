using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace SM4C.Engine.Durable.TestApp
{
    public static class HttpGreeting
    {
        [FunctionName("HttpGreeting")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "greet/{name}")] HttpRequest req,
            string name,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function processed a request. Found name = '{name}'.");

            if (string.IsNullOrEmpty(name))
            {
                return new BadRequestObjectResult(
                    new { error = "The 'name' parameter was missing." });
            }
            else
            {
                return new OkObjectResult(
                    new { greeting = $"Welcome to Serverless Workflow, {name}!" });
            }
        }
    }
}
