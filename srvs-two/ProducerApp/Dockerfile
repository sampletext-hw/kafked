FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["ProducerApp.csproj", "./"]
RUN dotnet restore "ProducerApp.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "ProducerApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProducerApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProducerApp.dll"]
