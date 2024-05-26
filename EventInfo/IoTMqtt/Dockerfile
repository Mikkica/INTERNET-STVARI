# Koristi zvanični Python image sa Docker Hub-a
FROM python:3.9-slim

# Postavi radni direktorijum u kontejneru
WORKDIR /app

# Kopiraj Python skript i requirements.txt u kontejner
COPY main.py .

# Instaliraj paho-mqtt modul
RUN pip install paho-mqtt pymongo

# Pokreni Python skript kada se kontejner pokrene
CMD ["python", "main.py"]
