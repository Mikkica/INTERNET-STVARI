import json
import time
import paho.mqtt.client as mqtt

# Povezivanje sa MQTT brokerom
mqtt_client = mqtt.Client()

# Predefinisani uslovi za detekciju anomalija
def detect_anomalies(sensor_data):
    anomalies = []
    try:
        # Primer uslova - visoka temperatura
        if float(sensor_data["Amb_Temp"]) > 35:
            anomalies.append("high_temperature")
        # Primer uslova - visoka koncentracija ozona
        if float(sensor_data["O3_PPB"]) > 120:
            anomalies.append("high_ozone")
        # Uslovi za relativnu vlažnost
        if float(sensor_data["Amb_RH"]) < 30:
            anomalies.append("low_humidity")
        elif float(sensor_data["Amb_RH"]) > 95:
            anomalies.append("high_humidity")
        # Uslovi za brzinu vetra
        if float(sensor_data["WindSpeed"]) > 1:
            anomalies.append("high_wind_speed")
        # Uslovi za koncentraciju čestica
        if float(sensor_data["PDR_Conc"]) > 100:
            anomalies.append("high_particle_concentration")
    except ValueError as e:
        print(f"Error converting sensor data: {e}")
    return anomalies

def on_connect(client, userdata, flags, rc):
    print("Connected to MQTT broker")
    client.subscribe("sensor_data")

def on_message(client, userdata, msg):
    try:
        sensor_data = json.loads(msg.payload.decode("utf-8"))
        # Ignoriši podatke ako bilo koja vrednost nije numerička
        for key in ["UTC","Amb_Temp", "O3_PPB", "Amb_RH", "WindSpeed", "PDR_Conc"]:
            if sensor_data[key] == 'NA':
                print(f"Skipping data due to NA value: {sensor_data}")
                return
        anomalies = detect_anomalies(sensor_data)
        if anomalies:
            event = {
                "type": anomalies,
                "values": {
                    "Amb_Temp": sensor_data["Amb_Temp"],
                    "O3_PPB": sensor_data["O3_PPB"],
                    "Amb_RH": sensor_data["Amb_RH"],
                    "WindSpeed": sensor_data["WindSpeed"],
                    "PDR_Conc": sensor_data["PDR_Conc"],
                    "UTC": sensor_data["UTC"]
                }

            }
            client.publish("sensor_alerts", json.dumps(event))
            print("Published anomaly event to MQTT broker:", event)
    except Exception as e:
        print(f"Error processing message: {e}")

mqtt_client.on_connect = on_connect
mqtt_client.on_message = on_message

mqtt_client.connect("localhost", 1883, 60)
mqtt_client.loop_forever()
