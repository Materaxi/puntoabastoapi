FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["PuntoAbasto.Api.csproj", "./"]
RUN dotnet restore "PuntoAbasto.Api.csproj"

COPY . .
RUN dotnet build "PuntoAbasto.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "PuntoAbasto.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# El puerto lo define Railway en tiempo de ejecución vía $PORT; Program.cs
# lo lee explícitamente (app.Run) en vez de depender de ASPNETCORE_URLS,
# porque ENV de Docker no interpola variables de entorno de runtime.
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "PuntoAbasto.Api.dll"]
