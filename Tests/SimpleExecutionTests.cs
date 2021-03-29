using SM4C.Engine;
using SM4C.Engine.Lite;
using SM4C.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SM4C.Tests
{
    [TestClass]
    public class SimpleExecutionTests
    {
        private static Host TheHost;

        [TestInitialize]
        public void Init()
        {
            TheHost = new Host();
        }

        [TestMethod]
        public async Task HelloWorld()
        {
            var definition = File.ReadAllText("ExampleWorkflows//helloworld.json");

            var workflow = JsonConvert.DeserializeObject<StateMachine>(definition);

            var json = await StateMachineRunner.RunAsync(workflow, TheHost);

            Assert.IsNotNull(json);
            Assert.AreEqual(JTokenType.Object, json.Type);
            Assert.IsTrue(((JObject)json).ContainsKey("result"));
            Assert.AreEqual("Hello World!", json["result"].Value<string>());
        }

        [TestMethod]
        public async Task SendCloudEventOnProvision()
        {
            var definition = File.ReadAllText("ExampleWorkflows//sendcloudeventonprovision.json");

            var workflow = JsonConvert.DeserializeObject<StateMachine>(definition);

            var input = JObject.Parse(@"
            {
                'orders': [
                    {
                        'orderId': '6C5D6632-6DAB-4548-A6DF-3C95D0AF37DF',
                        'customerId': 501,
                        'itemId': 'jaguars earlobes',
                        'quantity': 37
                    },
                    {
                        'orderId': '4210781D-8D5B-4C59-B03C-C06867CE14A1',
                        'customerId': 923,
                        'itemId': 'european swallow',
                        'quantity': 2
                    }
                ]
            }");

            static Task<JObject> testFunction(JObject order)
            {
                var copy = (JObject) order.DeepClone();
                copy["provisioned"] = true;
                return Task.FromResult(copy);
            };

            TheHost.Functions.Add("http://myapis.org/provisioning.json#doProvision", (Func<JObject, Task<JObject>>) testFunction);

            var json = await StateMachineRunner.RunAsync(workflow, TheHost, input);

            Assert.IsNotNull(json);

            var evt = TheHost.Dequeue();

            Assert.IsNotNull(evt);
            Assert.IsNotNull(evt.Data);
            Assert.AreEqual(JTokenType.Array, evt.Data.Type);
            Assert.AreEqual(2, evt.Data.Value<JArray>().Count);
        }

        [TestMethod]
        public async Task CreditCheckApproved()
        {
            var definition = File.ReadAllText("ExampleWorkflows//customercreditcheck.json");

            var workflow = JsonConvert.DeserializeObject<StateMachine>(definition);

            var input = JObject.Parse(@"
            {
              'customer': {
                'id': 'customer123',
                'name': 'John Doe',
                'SSN': 123456,
                'yearlyIncome': 50000,
                'address': '123 MyLane, MyCity, MyCountry',
                'employer': 'MyCompany'
              }
            }");

            static Task<JObject> doCreditCheck(JObject order)
            {
                var evt = new Event
                {
                    EventId = Guid.NewGuid().ToString(),
                    EventName = "CreditCheckCompletedEvent",
                    EventType = "creditCheckCompleteType",
                    EventSource = "creditCheckSource",
                    Timestamp = DateTimeOffset.UtcNow,
                    Data = new JObject
                    {
                        ["id"] = "customer123",
                        ["score"] = 700,
                        ["decision"] = "Approved",
                        ["reason"] = "Good credit score"
                    },
                    ContextAttributes =
                    {
                        { "customerId", "customer123" }
                    }
                };

                TheHost.Enqueue(evt);

                return Task.FromResult(new JObject());
            };

            TheHost.Functions.Add("http://myapis.org/creditcheckapi.json#doCreditCheck", (Func<JObject, Task<JObject>>) doCreditCheck);

            static Task<JObject> sendRejectionEmail(JObject order)
            {
                return Task.FromResult(new JObject());
            };

            TheHost.Functions.Add("http://myapis.org/creditcheckapi.json#rejectionEmail", (Func<JObject, Task<JObject>>) sendRejectionEmail);

            var json = await StateMachineRunner.RunAsync(workflow, TheHost, input);

            Assert.IsNotNull(json);
            Assert.AreEqual("approved", json["status"].Value<string>());
        }

        [TestMethod]
        public async Task VetAppointment()
        {
            var definition = File.ReadAllText("ExampleWorkflows//vetappointment.json");

            var workflow = JsonConvert.DeserializeObject<StateMachine>(definition);

            var input = JObject.Parse(@"
            {
              'patientInfo': {
                'name': 'Mia',
                'breed': 'German Shepherd',
                'age': 5,
                'reason': 'Bee sting',
                'patientId': 'Mia1'
              }
            }");

            Func<Task> startEventWatcher = async () =>
            {
                while (true)
                {
                    var eventFromWorkflow = TheHost.Dequeue();

                    if (eventFromWorkflow != null)
                    {
                        var evt = new Event
                        {
                            EventId = Guid.NewGuid().ToString(),
                            EventName = "VetAppointmentInfo",
                            EventType = "vetAppointmentInfoType",
                            EventSource = "VetServiceSource",
                            Timestamp = DateTimeOffset.UtcNow,
                            Data = new JObject
                            {
                                ["time"] = new DateTimeOffset(2020, 02, 22, 8, 0, 0, TimeSpan.Zero),
                                ["address"] = "123 Main Street Atlanta GA 30092"
                            }
                        };

                        TheHost.Enqueue(evt);

                        break;
                    }

                    await Task.Delay(1000);
                }
            };

            var watcherTask = startEventWatcher();

            var workflowTask = StateMachineRunner.RunAsync(workflow, TheHost, input);

            await watcherTask;

            var json = await workflowTask;

            Assert.IsNotNull(json);
            Assert.AreEqual("123 Main Street Atlanta GA 30092", json["appointmentInfo"].Value<string>("address"));
        }

        [TestMethod]
        public async Task ErrorHandling()
        {
            var definition = File.ReadAllText("ExampleWorkflows//errorhandling.json");

            var workflow = JsonConvert.DeserializeObject<StateMachine>(definition);

            var input = JObject.Parse(@"
            {
              'order': {
                'item': 'laptop',
                'quantity': 10
              }
            }");

            static Task<JObject> doProvision(JObject order)
            {
                if (!order.TryGetValue("id", out JToken id))
                {
                    throw new Exception("Missing order id");
                }

                if (!order.TryGetValue("item", out JToken item))
                {
                    throw new Exception("Missing order item");
                }

                if (!order.TryGetValue("quantity", out JToken quantity))
                {
                    throw new Exception("Missing order quantity");
                }

                order["status"] = "accepted";

                return Task.FromResult(order);
            };

            TheHost.Functions.Add("http://myapis.org/provisioningapi.json#doProvision", (Func<JObject, Task<JObject>>) doProvision);

            var workflowTask = StateMachineRunner.RunAsync(workflow, TheHost, input);

            var json = await workflowTask;

            Assert.IsNotNull(json);
            Assert.AreEqual("handleMissingIdExceptionWorkflow", json["invokedWorkflowId"].Value<string>());
        }

        [TestMethod]
        public async Task TravelBookingSaga()
        {
            var definition = File.ReadAllText("ExampleWorkflows//travelbookingsaga.json");

            var workflow = JsonConvert.DeserializeObject<StateMachine>(definition);

            var input = JObject.Parse(@"
            {
              'flightDetails': {
                'destination': 'Seattle',
                'depart': '4/1/2021',
                'return': '4/5/2021'
              },
              'hotelDetails': {
                'minPrice': 100,
                'maxPrice': 200
              },
              'autoDetails': {
                'size': 'compact'
              }
            }");

            static Task<JObject> makeFlightReservation(JObject flightDetails)
            {
                var evt = new Event
                {
                    EventId = Guid.NewGuid().ToString(),
                    EventName = "flightBookedEvent",
                    EventType = "FlightBooked",
                    EventSource = "reservationSystem",
                    Timestamp = DateTimeOffset.UtcNow,
                    Data = new JObject
                    {
                        ["airline"] = "Delta",
                        ["flightNumber"] = 118,
                        ["price"] = 560d
                    }
                };

                TheHost.Enqueue(evt);

                return Task.FromResult(new JObject());
            };

            TheHost.Functions.Add("http://reservations.org/flight#makeReservation", (Func<JObject, Task<JObject>>)makeFlightReservation);

            static Task<JObject> makeHotelReservation(JObject hotelDetails, JObject bookedFlight)
            {
                var evt = new Event
                {
                    EventId = Guid.NewGuid().ToString(),
                    EventName = "hotelBookedEvent",
                    EventType = "HotelBooked",
                    EventSource = "reservationSystem",
                    Timestamp = DateTimeOffset.UtcNow,
                    Data = new JObject
                    {
                        ["hotel"] = "The W",
                        ["address"] = "123 Congress Street Seattle WA",
                        ["nights"] = 4,
                        ["price"] = 650d
                    }
                };

                TheHost.Enqueue(evt);

                return Task.FromResult(new JObject());
            };

            TheHost.Functions.Add("http://reservations.org/hotel#makeReservation", (Func<JObject, JObject, Task<JObject>>)makeHotelReservation);

            static Task<JObject> makeAutoReservation(JObject autoDetails, JObject bookedFlight)
            {
                var evt = new Event
                {
                    EventId = Guid.NewGuid().ToString(),
                    EventName = "autoBookedEvent",
                    EventType = "AutoBooked",
                    EventSource = "reservationSystem",
                    Timestamp = DateTimeOffset.UtcNow,
                    Data = new JObject
                    {
                        ["make"] = "Honda",
                        ["model"] = "Accord",
                        ["days"] = 5,
                        ["price"] = 325d
                    }
                };

                TheHost.Enqueue(evt);

                return Task.FromResult(new JObject());
            };

            TheHost.Functions.Add("http://reservations.org/auto#makeReservation", (Func<JObject, JObject, Task<JObject>>)makeAutoReservation);

            var workflowTask = StateMachineRunner.RunAsync(workflow, TheHost, input);

            var json = await workflowTask;

            Assert.IsNotNull(json);
            Assert.IsNotNull(json["bookedFlight"]);
            Assert.IsNotNull(json["bookedHotel"]);
            Assert.IsNotNull(json["bookedAuto"]);
        }
    }
}
