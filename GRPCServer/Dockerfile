# Koristi .NET SDK image kao osnovu za izgradnju projekta
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# Kopiraj projekat u radni direktorijum
COPY . .

# Pokreni dotnet watch run u pozadini
CMD ["dotnet", "watch", "run"]
