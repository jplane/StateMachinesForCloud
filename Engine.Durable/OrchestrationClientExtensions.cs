namespace SM4C.Engine.Durable
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using SM4C.Model;

    public static class OrchestrationClientExtensions
    {
        public static Task<string> StartWorkflowAsync(
            this IDurableOrchestrationClient client,
            StartWorkflowArgs args,
            string? instanceId = null)
        {
            ThrowIfNullArgument(args, nameof(args));

            return client.StartNewAsync(
                ServerlessWorkflowFunctions.StarterFunctionName,
                instanceId ?? Guid.NewGuid().ToString("N"),
                args);
        }

        public static async Task RaiseWorkflowEventAsync(
            this IDurableOrchestrationClient client,
            string instanceId,
            StateMachine autostartWorkflowDefinition,
            WorkflowEvent workflowEvent)
        {
            ThrowIfNullArgument(instanceId, nameof(instanceId));
            ThrowIfNullArgument(autostartWorkflowDefinition, nameof(autostartWorkflowDefinition));
            ThrowIfNullArgument(workflowEvent, nameof(workflowEvent));

            DurableOrchestrationStatus? status = await client.GetStatusAsync(instanceId, showInput: false);
            if (status == null)
            {
                // WARNING: This code is NOT thread-safe! Until DF supports auto-start on raised events natively,
                //          the only way to ensure safe execution is to protect these calls with a distributed lock.
                await client.StartWorkflowAsync(
                    new StartWorkflowArgs(autostartWorkflowDefinition, workflowEvent.ToJson()),
                    instanceId);
            }
            else if (status.RuntimeStatus == OrchestrationRuntimeStatus.Completed ||
                     status.RuntimeStatus == OrchestrationRuntimeStatus.Terminated ||
                     status.RuntimeStatus == OrchestrationRuntimeStatus.Failed)
            {
                throw new InvalidOperationException($"Cannot send events to '{instanceId}' because it is in the {status.RuntimeStatus} state.");
            }

            await client.RaiseEventAsync(
                instanceId,
                WorkflowEvent.ExternalEventName,
                workflowEvent);
        }

        static void ThrowIfNullArgument(object arg, string name)
        {
            if (arg == null)
            {
                throw new ArgumentNullException(name);
            }
        }
    }
}
