# Build context is ./backend (see docker-compose.yml) - paths below are relative to that root.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Project files only, first - so `dotnet restore`'s layer is cached across source-only changes.
COPY src/DailyMart.Domain/DailyMart.Domain.csproj src/DailyMart.Domain/
COPY src/DailyMart.Application/DailyMart.Application.csproj src/DailyMart.Application/
COPY src/DailyMart.Infrastructure/DailyMart.Infrastructure.csproj src/DailyMart.Infrastructure/
COPY src/DailyMart.API/DailyMart.API.csproj src/DailyMart.API/
RUN dotnet restore src/DailyMart.API/DailyMart.API.csproj

COPY src/ src/
RUN dotnet publish src/DailyMart.API/DailyMart.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# .NET 8's aspnet image otherwise defaults to 8080 anyway, but pinned explicitly so docker-compose's
# port mapping never silently drifts if the base image's default ever changes.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DailyMart.API.dll"]
