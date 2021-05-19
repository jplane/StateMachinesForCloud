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
    using Microsoft.Extensions.Configuration;
    using SM4C.Integration;

    public class HttpStarter
    {
        private readonly IConfiguration _config;

        public HttpStarter(IConfiguration config)
        {
            _config = config;
        }

        [FunctionName(nameof(StartStateMachine))]
        public async Task<IActionResult> StartStateMachine(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "statemachine/{instanceId}")] HttpRequest req,
            [DurableClient] IDurableClient client,
            string instanceId,
            ILogger log)
        {
            StateMachine? workflow = null;
            JObject? input = null;
            ObservableAction[]? actions = null;

            if (req.ContentLength != 0)
            {
                var json = JObject.Parse(await req.ReadAsStringAsync());

                workflow = json.Property("workflow")?.Value.ToObject<StateMachine>();

                input = (JObject) (json.Property("input")?.Value ?? new JObject());

                actions = json.Property("actions")?.Value.ToObject<ObservableAction[]>();
            }

            if (workflow == null)
            {
                return new BadRequestObjectResult("Unable to deserialize state machine definition in request payload.");
            }

            var args = new StartWorkflowArgs(workflow, input, actions, _config["TELEMETRY_URI"]);
            
            await client.StartWorkflowAsync(args, instanceId);

            log.LogInformation($"Started new workflow '{workflow.Name}' with ID = '{instanceId}.");
            
            return client.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName(nameof(GetInstanceStatus))]
        public async Task<IActionResult> GetInstanceStatus(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "statemachine/{instanceId?}")] HttpRequest req,
            [DurableClient] IDurableClient client,
            string instanceId,
            ILogger log)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                var statuses = await client.GetStatusAsync();   // TODO: fix this

                log.LogInformation($"Getting status for all workflows.");

                return new ObjectResult(statuses);
            }
            else
            {
                var status = await client.GetStatusAsync(instanceId);

                log.LogInformation($"Getting status for workflow with ID = '{instanceId}.");

                return new ObjectResult(status);
            }
        }

        [FunctionName(nameof(RaiseEvent))]
        public async Task<IActionResult> RaiseEvent(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "statemachine/{instanceId}/event")] HttpRequest req,
            [DurableClient] IDurableClient client,
            string instanceId,
            ILogger log)
        {
            if (req.ContentLength == 0)
            {
                return new BadRequestObjectResult($"The request payload must contain a valid cloud event JSON object.");
            }

            var json = JObject.Parse(await req.ReadAsStringAsync());

            var eventData = json.ToObject<WorkflowEvent>();

            if (eventData == null || eventData.EventType == null || eventData.EventName == null)
            {
                return new BadRequestObjectResult($"The request payload must be a valid cloud event JSON object.");
            }

            await client.RaiseWorkflowEventAsync(instanceId, eventData);

            log.LogInformation($"Raised event of type '{eventData.EventType}' to workflow ID = '{instanceId}.");

            return client.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
