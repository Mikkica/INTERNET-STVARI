import time
import json
import pymongo
import bson.json_util
import paho.mqtt.client as mqtt

# Povezivanje sa MongoDB bazom
mongo_client = pymongo.MongoClient("mongodb://localhost:27017/")
db = mongo_client["Baza"]
collection = db["ForecastingOzone"]

# Povezivanje sa MQTT brokerom
mqtt_client = mqtt.Client()

def on_connect(client, userdata, flags, rc):
    print("Connected to MQTT broker")
    sensor_data_list = collection.find()
    for sensor_data in sensor_data_list:
        sensor_data['_id'] = str(sensor_data['_id'])
        payload = {
            "_id": sensor_data['_id'],
            "UTC": sensor_data["UTC"],
            "Amb_RH": sensor_data["Amb_RH"],
            "Amb_Temp": sensor_data["Amb_Temp"],
            "WindSpeed": sensor_data["WindSpeed"],
            "O3_PPB": sensor_data["O3_PPB"],
            "PDR_Conc": sensor_data["PDR_Conc"],
            "WindDirection": sensor_data["WindDirection"]
        }
        mqtt_client.publish("sensor_data", json.dumps(payload))
        print("Published sensor data to MQTT broker:", payload)

mqtt_client.on_connect = on_connect
mqtt_client.connect("localhost", 1883, 60)
mqtt_client.loop_start()

