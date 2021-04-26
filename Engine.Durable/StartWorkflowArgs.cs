namespace SM4C.Engine.Durable
{
    using System;
    using Newtonsoft.Json.Linq;
    using SM4C.Model;

    public class StartWorkflowArgs
    {
        public StartWorkflowArgs(StateMachine definition, JObject? input)
        {
            this.Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.Input = input;
        }

        public StateMachine Definition { get; protected set; }

        public JObject? Input { get; protected set; }
    }
}
