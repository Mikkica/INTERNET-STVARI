const mqtt = require('mqtt');
const WebSocket = require('ws');
const http = require('http');
const fs = require('fs');
const path = require('path');

// Kreiraj HTTP server za serviranje HTML fajlova
const server = http.createServer((req, res) => {
    if (req.method === 'GET' && req.url === '/') {
        fs.readFile(path.join(__dirname, './public/index.html'), (err, data) => {
            if (err) {
                res.writeHead(500);
                res.end('Error loading index.html');
            } else {
                res.writeHead(200, { 'Content-Type': 'text/html' });
                res.end(data);
            }
        });
    }
});

// Kreiraj WebSocket server
const wss = new WebSocket.Server({ server });

// MQTT konfiguracija
const mqttClient = mqtt.connect('mqtt://localhost:1883');

mqttClient.on('connect', () => {
    console.log('Connected to MQTT broker');
    mqttClient.subscribe('sensor_data', (err) => {
        if (err) {
            console.error('Failed to subscribe to sensor_data topic:', err);
        } else {
            console.log('Subscribed to sensor_data topic');
        }
    });
});

mqttClient.on('message', (topic, message) => {
    const messageStr = message.toString();
    
    try {
        const data = JSON.parse(messageStr);
        
        if (data && data.Amb_Temp > 33) {
            console.log(`MQTT Message with Amb_Temp > 33 received: ${messageStr}`);
            // Pošalji poruku svim povezanim WebSocket klijentima
            wss.clients.forEach(client => {
                if (client.readyState === WebSocket.OPEN) {
                    client.send(messageStr);
                }
            });
        }
    } catch (err) {
        console.error('Failed to parse MQTT message:', err);
    }
});

wss.on('connection', ws => {
    console.log('WebSocket client connected');
    ws.on('message', message => {
        console.log(`WebSocket Message: ${message}`);
    });
    ws.on('close', () => {
        console.log('WebSocket client disconnected');
    });
});

// Pokreni server
const PORT = 8080;
server.listen(PORT, () => {
    console.log(`Server is running on http://localhost:${PORT}`);
});
