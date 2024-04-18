using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace GrpcServer.Models
{
    public class ForecastModel
    {
        public ObjectId _id { get; set; }
        public string? UTC { get; set; }
        public double Amb_RH { get; set; }
        public double Amb_Temp { get; set; }
        public double WindSpeed { get; set; }
        public double O3_PPB { get; set; }
        public double PDR_Conc { get; set; }
        public double WindDirection { get; set; }
    }
  
}