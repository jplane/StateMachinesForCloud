namespace SM4C.Engine.Durable.TestApp
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;

    static class DemoApis
    {
        [FunctionName(nameof(Flights_MakeReservation))]
        public static async Task<IActionResult> Flights_MakeReservation(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "flights/reserve")] HttpRequest request,
            ILogger log)
        {
            string details = await request.ReadAsStringAsync();
            log.LogWarning($"✈ Received flight reservation details: {details}");

            // NOTE: This runs as a background thread
            ProcessReservation(
                request,
                callbackEventPayload: new
                {
                    id = Guid.NewGuid().ToString(),
                    subject = "flightBookedEvent",
                    type = "FlightBooked",
                    source = "reservationSystem",
                    time = DateTime.UtcNow.ToString("s"),
                    data = new
                    {
                        airline = "Delta",
                        flightNumber = 118,
                        price = 560d
                    }
                });

            return new AcceptedResult();
        }

        [FunctionName(nameof(Hotels_MakeReservation))]
        public static async Task<IActionResult> Hotels_MakeReservation(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "hotels/reserve")] HttpRequest request,
            ILogger log)
        {
            string details = await request.ReadAsStringAsync();
            log.LogWarning($"🏨 Received hotel reservation details: {details}");

            // NOTE: This runs as a background thread
            ProcessReservation(
                request,
                callbackEventPayload: new
                {
                    id = Guid.NewGuid().ToString(),
                    subject = "hotelBookedEvent",
                    type = "HotelBooked",
                    source = "reservationSystem",
                    time = DateTime.UtcNow.ToString("o"),
                    data = new
                    {
                        hotel = "The W",
                        address = "123 Congress Street Seattle WA",
                        nights = 4,
                        price = 650d
                    }
                });

            return new AcceptedResult();
        }

        [FunctionName(nameof(Auto_MakeReservation))]
        public static async Task<IActionResult> Auto_MakeReservation(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "autos/reserve")] HttpRequest request,
            ILogger log)
        {
            string details = await request.ReadAsStringAsync();
            log.LogWarning($"🚗 Received auto reservation details: {details}");

            // NOTE: This runs as a background thread
            ProcessReservation(
                request,
                callbackEventPayload: new
                {
                    id = Guid.NewGuid().ToString(),
                    subject = "autoBookedEvent",
                    type = "AutoBooked",
                    source = "reservationSystem",
                    time = DateTime.UtcNow.ToString("o"),
                    data = new
                    {
                        make = "Honda",
                        model = "Accord",
                        days = 5,
                        price = 325d
                    }
                });

            return new AcceptedResult();
        }

        const string CallbackUrlTemplate = "http://localhost:7071/api/statemachine/{instanceId}/event";

        static readonly HttpClient SharedHttpClient = new HttpClient();
        static readonly JsonMediaTypeFormatter SharedFormatter = new JsonMediaTypeFormatter();

        static async void ProcessReservation(HttpRequest request, object callbackEventPayload)
        {
            string instanceId = request.Headers["x-ms-workflow-instance-id"];

            // Simulate background processing
            await Task.Delay(TimeSpan.FromSeconds(10));

            string callbackUrl = CallbackUrlTemplate.Replace("{instanceId}", instanceId);
            await SharedHttpClient.PostAsync(callbackUrl, callbackEventPayload, SharedFormatter);
        }
    }
}
