namespace SM4C.Engine.Durable.TestApp
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Newtonsoft.Json.Linq;
    using SM4C.Model;

    public static class HttpStarter
    {
        [FunctionName(nameof(ManualStart))]
        public static async Task<IActionResult> ManualStart(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "workflows/{workflowName}")] HttpRequest req,
            [DurableClient] IDurableClient client,
            string workflowName,
            ILogger log)
        {
            StateMachine? workflow = await LoadWorkflowAsync(workflowName);
            if (workflow == null)
            {
                return new NotFoundObjectResult($"Could not find workflow named '{workflowName}'.");
            }

            var input = new JObject();
            if (req.ContentLength != 0)
            {
                input = JObject.Parse(await req.ReadAsStringAsync());
            }

            var args = new StartWorkflowArgs(workflow, input);
            string instanceId = await client.StartWorkflowAsync(args);

            log.LogInformation($"Started new workflow '{workflowName}' with ID = '{instanceId}.");
            return client.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName(nameof(EventStart))]
        public static async Task<IActionResult> EventStart(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "workflows/{workflowName}/{instanceId}")] HttpRequest req,
            [DurableClient] IDurableClient client,
            string workflowName,
            string instanceId,
            ILogger log)
        {
            StateMachine? workflow = await LoadWorkflowAsync(workflowName);
            if (workflow == null)
            {
                return new NotFoundObjectResult($"Could not find workflow named '{workflowName}'.");
            }

            if (req.ContentLength == 0)
            {
                return new BadRequestObjectResult($"The request payload must contain a valid cloud event JSON object.");
            }

            WorkflowEvent? eventData = null;
            using var reader = new JsonTextReader(new StreamReader(req.Body));
            try
            {
                JObject json = await JObject.LoadAsync(reader);
                eventData = json.ToObject<WorkflowEvent>();
            }
            catch (JsonReaderException)
            {
                log.LogWarning("Received a request payload that was not JSON!");
            }

            if (eventData == null ||
                eventData.EventType == null ||
                eventData.EventName == null)
            {
                return new BadRequestObjectResult($"The request payload must be a valid cloud event JSON object.");
            }

            try
            {
                await client.RaiseWorkflowEventAsync(
                    instanceId,
                    workflow,
                    eventData);
            }
            catch (InvalidOperationException e)
            {
                return new BadRequestObjectResult(e.Message);
            }

            log.LogInformation($"Raised event of type '{eventData.EventType}' to '{workflowName}' with ID = '{instanceId}.");
            return client.CreateCheckStatusResponse(req, instanceId);
        }

        static async Task<StateMachine?> LoadWorkflowAsync(string workflowName)
        {
            string definitionJson;
            try
            {
                definitionJson = await File.ReadAllTextAsync($"Workflows/{workflowName}");
            }
            catch (FileNotFoundException)
            {
                return null;
            }

            StateMachine workflow = JsonConvert.DeserializeObject<StateMachine>(definitionJson);
            return workflow;
        }
    }
}
