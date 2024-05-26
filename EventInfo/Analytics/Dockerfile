# Koristi zvanični Python image sa Docker Hub-a
FROM python:3.9-slim

# Postavi radni direktorijum u kontejneru
WORKDIR /app

# Instaliraj paho-mqtt biblioteku
RUN pip install paho-mqtt

# Kopiraj Python skript u kontejner
COPY main.py .

# Pokreni Python skript kada se kontejner pokrene
CMD ["python", "main.py"]
