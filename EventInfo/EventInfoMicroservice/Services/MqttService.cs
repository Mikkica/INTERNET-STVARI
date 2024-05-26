using MQTTnet;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EventInfoMicroservice.Services
{
    public class MqttService
    {
        private readonly IMqttClient _mqttClient;
        private readonly List<SensorData> _receivedData = new List<SensorData>();

        public MqttService()
        {
            var mqttFactory = new MqttFactory();
            _mqttClient = mqttFactory.CreateMqttClient();

            var mqttOptions = new MqttClientOptionsBuilder()
                .WithClientId("EventInfoMicroservice")
                .WithTcpServer("localhost", 1883)
                .Build();

            _mqttClient.ConnectedAsync += async e =>
            {
                await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("sensor_alerts").Build());
                Console.WriteLine("Connected to MQTT broker and subscribed to topic.");
            };

            _mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                try
                {
                    var payloadBytes = e.ApplicationMessage.Payload;
                    var sensorData = JsonSerializer.Deserialize<SensorData>(payloadBytes, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true // Omogućava deserializaciju svojstava bez obzira na veličinu slova
                    });
                    if (sensorData != null)
                    {
                        _receivedData.Add(sensorData);
                        Console.WriteLine("Received message and stored in memory:");
                        Console.WriteLine($"Type: {string.Join(", ", sensorData.Type)}");
                        Console.WriteLine($"Amb_Temp: {sensorData.Values?.Amb_Temp}");
                        Console.WriteLine($"O3_PPB: {sensorData.Values?.O3_PPB}");
                        Console.WriteLine($"Amb_RH: {sensorData.Values?.Amb_RH}");
                        Console.WriteLine($"WindSpeed: {sensorData.Values?.WindSpeed}");
                        Console.WriteLine($"PDR_Conc: {sensorData.Values?.PDR_Conc}");
                        Console.WriteLine($"UTC: {sensorData.Values?.UTC}");
                        Console.WriteLine("Stored data count: " + _receivedData.Count);
                    }
                    else
                    {
                        Console.WriteLine("Deserialization failed.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error processing message: " + ex.Message);
                }
                return Task.CompletedTask;
            };

            _mqttClient.ConnectAsync(mqttOptions).Wait();
        }

        public List<SensorData> GetSensorData() => _receivedData;

        
    }

    public class SensorData
    {
        public string[] Type { get; set; }
        public Values Values { get; set; }
    }

    public class Values
    {
        public double Amb_Temp { get; set; }
        public double O3_PPB { get; set; }
        public double Amb_RH { get; set; }
        public double WindSpeed { get; set; }
        public double PDR_Conc { get; set; }
        public string UTC { get; set; }
    }
}
