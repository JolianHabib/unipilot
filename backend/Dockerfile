FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["UniPilot.Domain/UniPilot.Domain.csproj", "UniPilot.Domain/"]
COPY ["UniPilot.Application/UniPilot.Application.csproj", "UniPilot.Application/"]
COPY ["UniPilot.Infrastructure/UniPilot.Infrastructure.csproj", "UniPilot.Infrastructure/"]
COPY ["UniPilot.API/UniPilot.API.csproj", "UniPilot.API/"]

RUN dotnet restore "UniPilot.API/UniPilot.API.csproj"

COPY . .

RUN dotnet publish "UniPilot.API/UniPilot.API.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 10000

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "UniPilot.API.dll"]
