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
    try
    {
        // Definišite format datuma i vremena koji očekujete (npr. "yyyy-MM-ddTHH:mm:ssZ")
        string format = "yyyy-MM-ddTHH:mm:ssZ";

        // Parsirajte `StartTimestamp` i `EndTimestamp` koristeći `DateTime.TryParseExact`
        if (!DateTime.TryParseExact(request.StartTimestamp, format, null, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime startTimestamp))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid StartTimestamp format"));
        }

        if (!DateTime.TryParseExact(request.EndTimestamp, format, null, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime endTimestamp))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid EndTimestamp format"));
        }

        // Kreiranje filtera i projekcije
        var filter = Builders<ForecastModel>.Filter.And(
            Builders<ForecastModel>.Filter.Gte("UTC", startTimestamp),
            Builders<ForecastModel>.Filter.Lte("UTC", endTimestamp)
        );

        var projection = Builders<ForecastModel>.Projection.Include(request.FieldName);

        // Nabavite rezultate
        var values = await _collection.Find(filter)
                                      .Project(projection)
                                      .ToListAsync();

        // Sakupljanje i obrada vrednosti
        var fieldValues = new List<double>();
        foreach (var document in values)
        {
            if (document.TryGetValue(request.FieldName, out BsonValue fieldValue))
            {
                if (fieldValue.IsNumeric)
                {
                    fieldValues.Add(fieldValue.AsDouble);
                }
            }
        }

        // Računanje tražene agregacije
        double result = 0;
        switch (request.Operation.ToLower())
        {
            case "min":
                if (fieldValues.Any())
                {
                    result = fieldValues.Min();
                }
                else
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "No values found for minimum operation"));
                }
                break;
            case "max":
                if (fieldValues.Any())
                {
                    result = fieldValues.Max();
                }
                else
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "No values found for maximum operation"));
                }
                break;
            case "avg":
                if (fieldValues.Any())
                {
                    result = fieldValues.Average();
                }
                else
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "No values found for average operation"));
                }
                break;
            case "sum":
                result = fieldValues.Sum();
                break;
            default:
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid aggregation operation type"));
        }

        return new AggregationValue
        {
            Result = result
        };
    }
    catch (Exception ex)
    {
        throw new RpcException(new Status(StatusCode.Internal, $"Error performing aggregation: {ex.Message}"));
    }
}


}
