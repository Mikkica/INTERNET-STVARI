const grpc = require('@grpc/grpc-js');
const express = require('express');
const protoLoader = require('@grpc/proto-loader');
const swaggerUi = require('swagger-ui-express');
const swaggerJsdoc = require('swagger-jsdoc'); 
const YAML = require('yamljs');
const { loadSync, loadPackageDefinition } = require('@grpc/proto-loader');

const app = express();
const PORT = 3000;

const packageDefinition = loadSync(__dirname + '/Protos/Forecast.proto');

const protoDescriptor = grpc.loadPackageDefinition(packageDefinition);
console.log(protoDescriptor.Forecast);


const myService = protoDescriptor.forecast.ForecastS;

const client = new myService('localhost:5087', grpc.credentials.createInsecure());

const swaggerDocument = YAML.load('./openAPI.yaml');

app.use(express.json());

app.get('/getForecast/:id', (req, res) => {
    const request = { _id: req.params.id };
    client.GetForecastValueById(request, (error, response) => {
        if (error) {
            if (error.code === grpc.status.INVALID_ARGUMENT) {
                res.status(400).json({ error: 'Invalid ID format' });
            } else if (error.code === grpc.status.NOT_FOUND) {
                res.status(404).json({ error: 'Forecast not found' });
            } else {
                res.status(500).json({ error: 'Internal Server Error' });
            }
            return;
        }
        res.json(response);
    });
});

app.post('/addForecast', (req, res) => {

    const request = {
        UTC: req.body.UTC,
        Amb_RH: req.body.AmbRH,
        Amb_Temp: req.body.AmbTemp,
        WindSpeed: req.body.WindSpeed,
        O3_PPB: req.body.O3PPB,
        PDR_Conc: req.body.PDRConc,
        WindDirection: req.body.WindDirection,
     
    };
    console.log(request);
    client.AddForecastValue(request, (error, response) => {
        if (error) {
            res.status(500).json({ error: 'Internal Server Error' });
            return;
        }
        res.json(response);
    });
});

app.delete('/deleteForecast/:id', (req, res) => {
    const id = req.params.id;
    const request = { _id: id };
    client.DeleteForecastById(request, (error, response) => {
        if (error) {
            if (error.code === grpc.status.INVALID_ARGUMENT) {
                res.status(400).json({ error: 'Invalid ID format' });
            } else if (error.code === grpc.status.NOT_FOUND) {
                res.status(404).json({ error: 'Forecast not found' });
            } else {
                res.status(500).json({ error: 'Internal Server Error' });
            }
            return;
        }

        res.json(response);
    });
});

app.put('/updateForecast', (req, res) => {
    const request = {
        _id: req.body.id,
        UTC: req.body.UTC,
        Amb_RH: req.body.AmbRH,
        Amb_Temp: req.body.AmbTemp,
        WindSpeed: req.body.WindSpeed,
        O3_PPB: req.body.O3PPB,
        PDR_Conc: req.body.PDRConc,
        WindDirection: req.body.WindDirection,
    };

    client.UpdateForecastById(request, (error, response) => {
        if (error) {
            if (error.code === grpc.status.INVALID_ARGUMENT) {
                res.status(400).json({ error: 'Invalid ID format' });
            } else if (error.code === grpc.status.NOT_FOUND) {
                res.status(404).json({ error: 'Forecast not found' });
            } else {
                res.status(500).json({ error: 'Internal Server Error' });
            }
            return;
        }

        res.json(response);
    });
});


app.get('/aggregateForecast', (req, res) => {
    const startTimestamp = req.query.StartTimestamp;
    const endTimestamp = req.query.EndTimestamp;
    const operation = req.query.Operation;
    const fieldName = req.query.FieldName;
 
    if (!startTimestamp || !endTimestamp || !fieldName || !operation) {
        res.status(400).json({ error: 'Missing required parameters' });
        return;
    }
    const request = {
        StartTimestamp: startTimestamp,
        EndTimestamp: endTimestamp,
        Operation: operation,
        FieldName: fieldName
    };
 
    console.log(request);
 
    client.ForecastAggregation(request, (error, response) => {
        if (error) {
            if (error.code === grpc.status.INVALID_ARGUMENT) {
                res.status(400).json({ error: 'Invalid argument for aggregation' });
            } else {
                res.status(500).json({ error: 'Internal Server Error' });
            }
            return;
        }
        res.json(response);
    });
});


app.use('/api-docs', swaggerUi.serve, swaggerUi.setup(swaggerDocument));

app.listen(PORT, () => {
    console.log(`Server is running on port ${PORT}`);
});

