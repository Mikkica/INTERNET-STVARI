# Koristi Node.js image kao osnovu
FROM node:20

# Kreiraj direktorijum za aplikaciju
WORKDIR /app

# Kopiraj package.json i package-lock.json i instaliraj zavisnosti
COPY package*.json ./
RUN npm install

# Kopiraj ostale fajlove
COPY . .

# Port na kojem aplikacija sluša
EXPOSE 3000

# Komanda za pokretanje aplikacije
CMD ["npm", "start"]
