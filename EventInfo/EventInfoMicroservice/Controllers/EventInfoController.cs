using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using EventInfoMicroservice.Services;

namespace EventInfoMicroservice.Controllers
{
    public class SensorData
    {
       public string[] Type { get; set; }
        public double Amb_Temp { get; set; }
        public double O3_PPB { get; set; }
        public double Amb_RH { get; set; }
        public double WindSpeed { get; set; }
        public double PDR_Conc { get; set; }
        public string UTC { get; set; }
    }

    [ApiController]
    public class EventInfoController : ControllerBase
    {
        private readonly MqttService _mqttService;

        public EventInfoController(MqttService mqttService)
        {
            _mqttService = mqttService;
        }

        [HttpGet]
        [Route("GetAll")]
        public ActionResult<List<SensorData>> GetAll()
        {
            var sensorData = _mqttService.GetSensorData();
            return Ok(sensorData);
        }
    }
}
