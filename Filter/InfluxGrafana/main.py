import asyncio
import json
from influxdb_client import InfluxDBClient, Point
from influxdb_client.client.write_api import SYNCHRONOUS
from nats.aio.client import Client as NATS
from nats.aio.errors import ErrConnectionClosed, ErrTimeout

# InfluxDB pristupne informacije
INFLUXDB_URL = "http://localhost:8086"  # Promenite ako je vaš URL drugačiji
INFLUXDB_TOKEN = "ilsEXIqXazU86p-C-hu-e1U2vCX67UH6SlzPf7rgl7UJfKQS8Be-QRTjBBksU7X7t9ACvPd3u0lY7W-z9-RTnA=="  # Vaš generisani token
INFLUXDB_ORG = "123"      # Vaša organizacija
INFLUXDB_BUCKET = "123"  # Naziv vašeg bucket-a

async def subscribe_to_nats():
    nc = NATS()

    async def message_handler(msg):
        subject = msg.subject
        data = msg.data.decode()
        print(f"Primljena poruka na subjektu: {subject} - {data}")

        try:
            # Pokušaj parsiranja JSON-a iz primljenih podataka
            json_data = json.loads(data)
            json_data["subject"] = subject  # Dodajemo subjekt kao deo JSON podataka

            await write_to_influxdb(json_data)

        except json.JSONDecodeError as e:
            print(f"Greška pri parsiranju JSON-a: {e}")

    try:
        await nc.connect(servers=["nats://localhost:4222"])  # Postavite server i port NATS-a

        # Postavljanje pretplate na određeni subjekt
        await nc.subscribe("average_data", cb=message_handler)

        print(f"Pretplaćeni na NATS subjekt 'average_data'")

        # Držanje veze otvorenom
        await nc.flush()

        while True:
            await asyncio.sleep(1)

    except ErrConnectionClosed:
        print("Veza sa NATS serverom je zatvorena.")
        await asyncio.sleep(1)
        await subscribe_to_nats()

    except ErrTimeout:
        print("Timeout pri pokušaju komunikacije sa NATS serverom.")
        await asyncio.sleep(1)
        await subscribe_to_nats()

    finally:
        await nc.close()

async def write_to_influxdb(json_data):
    client = InfluxDBClient(url=INFLUXDB_URL, token=INFLUXDB_TOKEN, org=INFLUXDB_ORG)
    write_api = client.write_api(write_options=SYNCHRONOUS)

    try:
        point = Point("average_data") \
            .tag("subject", json_data["subject"]) \
            .field("Amb_RH", json_data["Amb_RH"]) \
            .field("Amb_Temp", json_data["Amb_Temp"]) \
            .field("WindSpeed", json_data["WindSpeed"]) \
            .field("O3_PPB", json_data["O3_PPB"]) \
            .field("PDR_Conc", json_data["PDR_Conc"]) \
            .field("WindDirection", json_data["WindDirection"])

        write_api.write(INFLUXDB_BUCKET, INFLUXDB_ORG, point)
        print("Podaci uspešno upisani u InfluxDB.")

    except Exception as e:
        print(f"Greška prilikom upisa podataka u InfluxDB: {e}")

    finally:
        client.close()

if __name__ == '__main__':
    loop = asyncio.get_event_loop()
    loop.run_until_complete(subscribe_to_nats())
    loop.close()
