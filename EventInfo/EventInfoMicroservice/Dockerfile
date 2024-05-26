# Koristi .NET Core SDK kao osnovu
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# Kopiraj csproj i restore-uj zavisnosti pre ostalog koda
COPY *.csproj ./
RUN dotnet restore

# Kopiraj sve ostale izvore i izgradi aplikaciju
COPY . .
RUN dotnet publish -c Release -o out

# Koristi .NET runtime kao osnovu za minimalni kontejner
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./
ENTRYPOINT ["dotnet", "EventInfoMicroservice.dll"]
