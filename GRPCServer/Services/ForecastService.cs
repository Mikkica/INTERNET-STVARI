using System;
using System.Threading.Tasks;
using Grpc.Core;
using MongoDB.Bson;
using MongoDB.Driver;
using Google.Protobuf.WellKnownTypes;
using GrpcServer.Models;
using GrpcServer;

namespace GrpcServer.Services;

public class ForecastService : ForecastS.ForecastSBase
{
    private readonly IMongoCollection<ForecastModel> _collection;

    public ForecastService(IMongoDatabase database)
    {
        _collection = database.GetCollection<ForecastModel>("ForecastingOzone");
    }

    public override async Task<ValueMessage> GetForecastValueById(ForecastId request, ServerCallContext context)
    {
        var Id = request.Id;

        Console.WriteLine(Id);


        ObjectId objectId;
        try
        {
            objectId = ObjectId.Parse(Id);
        }
        catch (FormatException)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid ObjectId format"));
        }

        var filter = Builders<ForecastModel>.Filter.Eq(x => x._id, objectId);

        var forecast = await _collection.Find(filter).FirstOrDefaultAsync();

        if (forecast == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Forecast value not found"));
        }


        return await Task.FromResult(new ValueMessage
        {
            Id = forecast._id.ToString(),
            Forecastvalue = new ForecastValue
            {
                UTC = forecast.UTC,
                AmbRH = forecast.Amb_RH,
                AmbTemp = forecast.Amb_Temp,
                WindSpeed = forecast.WindSpeed,
                O3PPB = forecast.O3_PPB,
                PDRConc = forecast.PDR_Conc,
                WindDirection = forecast.WindDirection,

            },
            Message = "Forecast value found"
        });
    }
    public override async Task<ValueMessage> AddForecastValue(ForecastValue request, ServerCallContext context)
    {
        var objectId = ObjectId.GenerateNewId();

        var filter = Builders<ForecastModel>.Filter.Eq(x => x._id, objectId);

        var forecastValue = await _collection.Find(filter).FirstOrDefaultAsync();
        Console.WriteLine(request.AmbRH);
        if (forecastValue != null)
        {
            return await Task.FromResult(new ValueMessage
            {
                Id = forecastValue._id.ToString(),
                Message = "There is a forecast with the same id in database"
            });
        }
        var newValue = new ForecastModel
        {
            _id = objectId,
            UTC = request.UTC,
            Amb_RH = request.AmbRH,
            Amb_Temp = request.AmbTemp,
            WindSpeed = request.WindSpeed,
            O3_PPB = request.O3PPB,
            PDR_Conc = request.PDRConc,
            WindDirection = request.WindDirection,

        };
        await _collection.InsertOneAsync(newValue);

        return await Task.FromResult(new ValueMessage
        {
            Id = newValue._id.ToString(),
            Message = "Forecast value added successfully"
        });

    }


    public override async Task<ValueMessage> DeleteForecastById(ForecastId request, ServerCallContext context)
    {
        var Id = request.Id;
        ObjectId objectId;
        try
        {
            objectId = ObjectId.Parse(Id);
        }
        catch (FormatException)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid ObjectId format"));
        }
        var filter = Builders<ForecastModel>.Filter.Eq(x => x._id, objectId);

        var forecastValue = await _collection.FindOneAndDeleteAsync(filter);

        if (forecastValue == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Forecast with the requested Id not found"));
        }

        return await Task.FromResult(new ValueMessage
        {
            Id = objectId.ToString(),
            Message = "Forecast with the requested Id has been successfully deleted"
        });
    }

    public override async Task<ValueMessage> UpdateForecastById(ForecastValue request, ServerCallContext context)
    {
        var Id = request.Id;
        ObjectId objectId;
        try
        {
            objectId = ObjectId.Parse(Id);
        }
        catch (FormatException)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid ObjectId format"));
        }

        var filter = Builders<ForecastModel>.Filter.Eq(x => x._id, objectId);

        var forecastValue = await _collection.Find(filter).FirstOrDefaultAsync();

        if (forecastValue == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "There is no forecast with the same Id in database"));
        }

        forecastValue.UTC = request.UTC;
        forecastValue.Amb_RH = request.AmbRH;
        forecastValue.Amb_Temp = request.AmbTemp;
        forecastValue.WindSpeed = request.WindSpeed;
        forecastValue.O3_PPB = request.O3PPB;
        forecastValue.PDR_Conc = request.PDRConc;
        forecastValue.WindDirection = request.WindDirection;


        await _collection.ReplaceOneAsync(filter, forecastValue);

        return new ValueMessage
        {
            Id = objectId.ToString(),
            Message = "Forecast with the requested ID has been successfully updated"
        };
    }

 public override async Task<AggregationValue> ForecastAggregation(ForecastAggregationRequest request, ServerCallContext context)
{

    string startTimestamp = request.StartTimestamp;
    string endTimestamp = request.EndTimestamp;

    var filter = Builders<ForecastModel>.Filter.And(
        Builders<ForecastModel>.Filter.Gte(x => x.UTC, startTimestamp),
        Builders<ForecastModel>.Filter.Lte(x => x.UTC, endTimestamp)
    );
    

    var values = await _collection.Find(filter).ToListAsync();


    double result;
    if (values.Count > 0)
    {

        switch (request.Operation.ToLower())
        {
            case "min":
                result = values.Min(v => Convert.ToDouble(v.GetType().GetProperty(request.FieldName)?.GetValue(v)));
                break;
            case "max":
                result = values.Max(v => Convert.ToDouble(v.GetType().GetProperty(request.FieldName)?.GetValue(v)));
                break;
            case "avg":
                result = values.Average(v => Convert.ToDouble(v.GetType().GetProperty(request.FieldName)?.GetValue(v)));
                break;
            case "sum":
                result = values.Sum(v => Convert.ToDouble(v.GetType().GetProperty(request.FieldName)?.GetValue(v)));
                break;
            default:
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid aggregation operation type"));
        }
    }
    else
    {
        throw new RpcException(new Status(StatusCode.NotFound, "No data found in the specified time range"));
    }


    return new AggregationValue
    {
        Result = result
    };
}


}
