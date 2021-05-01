using SM4C.Engine.Extensions;
using SM4C.Integration;
using SM4C.Model;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace SM4C.Engine
{
    internal class StateMachineContext
    {
        private readonly ObservableAction[]? _targetActions = null;

        public StateMachineContext(StateMachine workflow,
                                   IStateMachineHost host,
                                   JToken? input,
                                   ObservableAction[]? targetActions,
                                   CancellationToken cancelToken)
        {
            workflow.CheckArgNull(nameof(workflow));
            host.CheckArgNull(nameof(host));

            this.Workflow = workflow;
            this.Host = new HostProxy(host);
            this.Input = input ?? new JObject();
            this.Output = new JObject();
            this.CancelToken = cancelToken;

            _targetActions = targetActions;
        }

        public Task RecordObservableActionAsync(ObservableAction action,
                                                Func<Dictionary<string, object>>? getData = null)
        {
            if (_targetActions == null || !_targetActions.Contains(action))
            {
                return Task.CompletedTask;
            }

            var data = getData?.Invoke() ?? new Dictionary<string, object>();

            data["action"] = action.ToString();
            data["instanceId"] = this.Host.GetInstanceId();
            data["start"] = this.Host.GetStartTime();
            data["name"] = this.Workflow.Name;
            data["version"] = this.Workflow.Version;
            data["input"] = this.Input.ToString();
            data["output"] = this.Output.ToString();

            if (!string.IsNullOrWhiteSpace(this.Workflow.Description))
            {
                data["description"] = this.Workflow.Description;
            }

            return this.Host.OnObservableEventAsync(data);
        }

        public StateMachine Workflow { get; }

        public IStateMachineHost Host { get; }

        public JToken Input { get; }

        public JToken Output { get; }

        public CancellationToken CancelToken { get; }
    }
}
