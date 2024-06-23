import express, { Express, Request, Response } from 'express';
import path from 'path';
import { MongoClient, Db, Collection, MongoClientOptions } from 'mongodb';
import { connect, JSONCodec } from 'nats';
import moment from 'moment';

const app: Express = express();

const uri: string = 'mongodb://127.0.0.1:27017/Baza';
let collection: Collection;
let averages: { [key: string]: number } = {};

async function connectToDatabase() {
    const options: MongoClientOptions = {};

    const client: MongoClient = await MongoClient.connect(uri, options);
    const db: Db = client.db('Baza');
    collection = db.collection('ForecastingOzone');
    console.log('Uspešno konektovan sa bazom.');
}

connectToDatabase().catch(error => {
    console.error('Došlo je do greške prilikom konektovanja sa bazom:', error);
});

const staticPath: string = path.join(__dirname, 'public');

app.use(express.static(staticPath));

const fields = ['Amb_RH', 'Amb_Temp', 'WindSpeed', 'O3_PPB', 'PDR_Conc', 'WindDirection'];

app.get('/api/documents', async (req: Request, res: Response) => {
    try {
        const { startDate, endDate } = req.query;

        if (typeof startDate === 'string' && typeof endDate === 'string') {
            const startDateStr = moment(startDate).format('YYYY-MM-DD HH:mm:ss');
            const endDateStr = moment(endDate).format('YYYY-MM-DD HH:mm:ss');

            console.log('Start Date:', startDateStr);
            console.log('End Date:', endDateStr);

            const documents = await collection.find({
                UTC: { $gte: startDateStr, $lte: endDateStr }
            }).toArray();

            //console.log('Documents fetched:', documents);

            averages = {};

            fields.forEach(field => {
                const sum = documents.reduce((acc, obj) => acc + (obj[field] || 0), 0);
                averages[field] = documents.length ? sum / documents.length : 0;
            });

            //console.log('Calculated averages:', averages);

            // Subscribe to NATS first
            await subscribeToNATS('average_data');

            // Now send the averages to NATS
            sendToNATS('average_data', averages).catch(error => {
                console.error('Došlo je do greške prilikom slanja poruke na NATS server:', error);
            });

            res.json(averages);
        } else {
            console.error('startDate ili endDate su undefined.');
            res.status(400).json({ error: 'startDate ili endDate su undefined.' });
        }
    } catch (error) {
        console.error('Došlo je do greške prilikom filtriranja dokumenata:', error);
        res.status(500).json({ error: 'Došlo je do greške prilikom filtriranja dokumenata.' });
    }
});

async function sendToNATS(topic: string, data: any) {
    try {
        const nc = await connect({ servers: 'nats://localhost:4222' });
        const codec = JSONCodec();
        const encodedData = codec.encode(data);

        nc.publish(topic, encodedData);

        await nc.flush();
        await nc.close();

        console.log('Poruka uspešno poslata na NATS server.');
    } catch (error) {
        console.error('Došlo je do greške prilikom slanja poruke na NATS server:', error);
    }
}

async function subscribeToNATS(topic: string) {
    try {
        const nc = await connect({ servers: 'nats://localhost:4222' });
        const subscription = nc.subscribe(topic);
        (async () => {
            for await (const msg of subscription) {
                const data = JSONCodec().decode(msg.data);
                console.log(`Primljena poruka na subjektu: ${msg.subject}:`, data);
            }
        })().catch(error => {
            console.error('Došlo je do greške prilikom obrade poruka:', error);
        });
    } catch (error) {
        console.error('Došlo je do greške prilikom pretplate na NATS subjekat:', error);
    }
}

const PORT: number = 3001;
app.listen(PORT, () => {
    console.log(`Server je pokrenut na http://localhost:${PORT}`);
});
