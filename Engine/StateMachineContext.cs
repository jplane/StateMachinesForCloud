using SM4C.Engine.Extensions;
using SM4C.Integration;
using SM4C.Model;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace SM4C.Engine
{
    internal class StateMachineContext
    {
        public StateMachineContext(StateMachine workflow, IStateMachineHost host, JToken? input, CancellationToken cancelToken)
        {
            workflow.CheckArgNull(nameof(workflow));
            host.CheckArgNull(nameof(host));

            this.Workflow = workflow;
            this.Host = new HostProxy(host);
            this.Input = input ?? new JObject();
            this.Output = new JObject();
            this.CancelToken = cancelToken;
        }

        public StateMachine Workflow { get; }

        public IStateMachineHost Host { get; }

        public JToken Input { get; }

        public JToken Output { get; }

        public CancellationToken CancelToken { get; }
    }
}
