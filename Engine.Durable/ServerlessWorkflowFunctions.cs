namespace SM4C.Engine.Durable
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Azure.WebJobs.Script.Description;
    using Microsoft.Extensions.Logging;
    using Microsoft.OpenApi.Models;
    using Microsoft.OpenApi.Readers;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using SM4C.Engine;

    /// <summary>
    /// Static class that defines all the built-in functions for executing CNCF Serverless Workflows.
    /// IMPORTANT: Renaming methods in this class is a breaking change!
    /// </summary>
    public class ServerlessWorkflowFunctions : IFunctionProvider
    {
        /// <summary>
        /// The list of supported trigger parameter types and their associated binding metadata.
        /// NOTE: We always assume the parameter name is "context".
        /// </summary>
        static readonly Dictionary<Type, BindingMetadata> SupportedTriggerParams = new Dictionary<Type, BindingMetadata>
        {
            [typeof(IDurableOrchestrationContext)] = BindingMetadata.Create(new JObject(
                new JProperty("type", "orchestrationTrigger"),
                new JProperty("name", "context"))),

            [typeof(IDurableActivityContext)] = BindingMetadata.Create(new JObject(
                new JProperty("type", "activityTrigger"),
                new JProperty("name", "context"))),
        };

        static readonly HttpClient httpClient = new HttpClient();

        // TODO: Not sure what this is for...
        /// <inheritdoc/>
        ImmutableDictionary<string, ImmutableArray<string>> IFunctionProvider.FunctionErrors =>
            new Dictionary<string, ImmutableArray<string>>().ToImmutableDictionary();

        internal static string StarterFunctionName => GetFunctionName(nameof(DurableWorkflowRunner));
        internal static string RESTfulServiceInvokerFunctionName => GetFunctionName(nameof(RESTfulServiceInvoker));

        /// <summary>
        /// Orchestrator function that runs a CNCF Serverless workflow.
        /// </summary>
        public static Task<JToken> DurableWorkflowRunner(IDurableOrchestrationContext context)
        {
            var args = context.GetInput<StartWorkflowArgs>();
            var host = new DurableFunctionsHost(context);
            return StateMachineRunner.RunAsync(args.Definition, host, args.Input);
        }

        /// <summary>
        /// Activity function that implements CNCF Serverless workflow RESTful service invocation.
        /// https://github.com/serverlessworkflow/specification/blob/master/specification.md#using-functions-for-restful-service-invocations
        /// </summary>
        public static async Task<JToken> RESTfulServiceInvoker(IDurableActivityContext context, ILogger logger)
        {
            InvokeFunctionArgs? args = context.GetInput<InvokeFunctionArgs>();
            if (!Uri.TryCreate(args?.Operation, UriKind.Absolute, out Uri target))
            {
                throw new ArgumentException($"Function calls must include an '{nameof(args.Operation).ToLowerInvariant()}' field that is in the form of an absolute URI. Given function operation: '{args?.Operation}'.");
            }

            // CONSIDER: Cache the specs to reduce I/O when a particular file is reused multiple times.
            string specContentText;
            if (target.IsFile)
            {
                // file://myapis/greetingapis.json#greeting -> myapis/greetingapis.json
                string path = string.Concat(target.Host, target.AbsolutePath);
                specContentText = await File.ReadAllTextAsync(path);
            }
            // TODO: Add support for HTTP and HTTPS
            else
            {
                throw new NotSupportedException($"The scheme '{target.Scheme}' is not supported for function operations.");
            }

            string targetOperationId = target.Fragment.TrimStart('#');
            var openApiSpecReader = new OpenApiStringReader();
            OpenApiDocument? document = openApiSpecReader.Read(specContentText, out _);

            // TODO: What is the expectation if there are multiple server values?
            Uri baseUrl = new Uri(document.Servers.FirstOrDefault()?.Url ?? "/");
            HttpRequestMessage? request = null;
            JObject? jsonContent = null;
            foreach ((string pathKey, OpenApiPathItem pathItem) in document.Paths)
            {
                foreach ((OperationType type, OpenApiOperation operation) in pathItem.Operations)
                {
                    if (targetOperationId.Equals(operation.OperationId, StringComparison.OrdinalIgnoreCase))
                    {
                        string relativePath = pathKey.TrimStart('/');
                        foreach ((string name, object? value) in args!.Parameters ?? ImmutableDictionary<string, object>.Empty)
                        {
                            if (operation.Parameters.Any(p => p.In == ParameterLocation.Path &&
                                                              p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                            {
                                // TODO: There are various supported ways of serializing Path parameters
                                //       https://swagger.io/docs/specification/describing-parameters/#path-parameters
                                relativePath = relativePath.Replace($"{{{name}}}", $"{value}");
                            }
                            else
                            {
                                // TODO: Support for more than just JObject
                                if (jsonContent == null)
                                {
                                    jsonContent = new JObject();
                                }

                                jsonContent.Add(name, JToken.FromObject(value));
                            }
                        }

                        var url = new Uri(baseUrl, relativePath);
                        var method = new HttpMethod(type.ToString());
                        request = new HttpRequestMessage(method, url);
                        request.Headers.Add("x-ms-workflow-instance-id", context.InstanceId);

                        if (jsonContent != null)
                        {
                            request.Content = new StringContent(
                                jsonContent.ToString(Formatting.None),
                                Encoding.UTF8,
                                "application/json");
                        }

                        break;
                    }
                }

                if (request != null)
                {
                    break;
                }
            }

            if (request == null)
            {
                throw new ArgumentException($"Could not find an operation with ID '{targetOperationId}'.");
            }

            using HttpResponseMessage response = await httpClient.SendAsync(request);
            string responseText = await response.Content.ReadAsStringAsync();

            JToken result = JValue.CreateNull();
            if (response.Content?.Headers?.ContentType?.MediaType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) == true &&
                response.Content?.Headers?.ContentLength != 0)
            {
                try
                {
                    result = JToken.Parse(responseText);
                }
                catch (JsonReaderException e)
                {
                    logger.LogError(e, "The HTTP response contained a payload but it was not valid JSON and will be ignored.");
                }
            }

            return result;
        }

        /// <inheritdoc/>
        Task<ImmutableArray<FunctionMetadata>> IFunctionProvider.GetFunctionMetadataAsync() =>
            Task.FromResult(this.GetFunctionMetadata().ToImmutableArray());

        /// <summary>
        /// Returns an enumeration of all the function triggers defined in this class.
        /// </summary>
        IEnumerable<FunctionMetadata> GetFunctionMetadata()
        {
            foreach (MethodInfo method in this.GetType().GetMethods())
            {
                // Look for the expected function trigger parameter
                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    if (SupportedTriggerParams.TryGetValue(parameter.ParameterType, out BindingMetadata bindingMetadata))
                    {
                        yield return new FunctionMetadata
                        {
                            Name = GetFunctionName(method.Name),
                            Bindings = { bindingMetadata },
                            ScriptFile = $"assembly:{method.ReflectedType.Assembly.FullName}",
                            EntryPoint = $"{method.ReflectedType.FullName}.{method.Name}",
                            Language = "DotNetAssembly",
                        };

                        break;
                    }
                }
            }
        }

        static string GetFunctionName(string methodName) => $"ServerlessWF::{methodName}";
    }
}
