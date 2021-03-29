using SM4C.Model;
using SM4C.Model.Actions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;

namespace SM4C.Tests
{
    [TestClass]
    public class DeserializationTests
    {
        [TestMethod]
        public void HelloWorld()
        {
            var json = File.ReadAllText("ExampleWorkflows//helloworld.json");

            var workflow = JsonConvert.DeserializeObject<StateMachine>(json);

            Assert.IsNotNull(workflow);
            Assert.AreEqual("Hello World Workflow", workflow.Name);
            Assert.AreEqual("Inject Hello World", workflow.Description);
            Assert.AreEqual(1, workflow.States.Count);

            var action = workflow.States.Single().EnterAction;

            Assert.IsNotNull(action);
            Assert.IsInstanceOfType(action, typeof(InjectDataAction));
            Assert.IsNotNull(((InjectDataAction) action).Expression);
        }

        [TestMethod]
        public void Greeting()
        {
            var json = File.ReadAllText("ExampleWorkflows//greeting.json");

            var workflow = JsonConvert.DeserializeObject<StateMachine>(json);

            Assert.IsNotNull(workflow);
            Assert.AreEqual("Greeting Workflow", workflow.Name);
            Assert.AreEqual("Greet Someone", workflow.Description);

            Assert.AreEqual(1, workflow.Functions.Count);
            Assert.AreEqual("greetingFunction", workflow.Functions.Single().Name);
            Assert.AreEqual("file://myapis/greetingapis.json#greeting", workflow.Functions.Single().Operation);

            Assert.AreEqual(1, workflow.States.Count);

            var state = workflow.States.Single();

            Assert.AreEqual("Greet", state.Name);
            Assert.IsTrue(state.Start);
            Assert.AreEqual(0, state.Transitions.Count);

            var action = workflow.States.Single().EnterAction;

            Assert.IsNotNull(action);
            Assert.IsInstanceOfType(action, typeof(InvokeFunctionAction));

            var invokeAction = (InvokeFunctionAction)action;
            
            Assert.AreEqual("greetingFunction", invokeAction.FunctionName);
            Assert.AreEqual(1, invokeAction.Arguments.Count);
            Assert.AreEqual("${ .person.name }", invokeAction.Arguments["name"]);
        }
    }
}
