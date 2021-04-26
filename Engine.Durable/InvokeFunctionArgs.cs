namespace SM4C.Engine.Durable
{
    using System;
    using System.Collections.Generic;

    class InvokeFunctionArgs
    {
        public InvokeFunctionArgs(string operation, IDictionary<string, object> parameters)
        {
            this.Operation = operation ?? throw new ArgumentNullException(nameof(operation));
            this.Parameters = parameters;
        }

        public string Operation { get; private set; }

        public IDictionary<string, object>? Parameters { get; private set; }
    }
}
