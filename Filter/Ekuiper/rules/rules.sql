-- ekuiper/rules/rules.sql
CREATE STREAM SensorData (
    _id STRING,
    UTC STRING,
    Amb_RH DOUBLE,
    Amb_Temp DOUBLE,
    WindSpeed DOUBLE,
    O3_PPB DOUBLE,
    PDR_Conc DOUBLE,
    WindDirection DOUBLE
) WITH (
    FORMAT = 'json',
    TYPE = 'mqtt',
    TOPIC = 'sensor_data_topic'
);

CREATE STREAM DetectedEvents AS
    SELECT * FROM SensorData WHERE Amb_Temp > 35;
