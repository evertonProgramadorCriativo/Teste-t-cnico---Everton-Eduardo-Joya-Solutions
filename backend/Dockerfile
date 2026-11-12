FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Joya.Pedidos.Api/Joya.Pedidos.Api.csproj Joya.Pedidos.Api/
RUN dotnet restore Joya.Pedidos.Api/Joya.Pedidos.Api.csproj

COPY Joya.Pedidos.Api/ Joya.Pedidos.Api/
RUN dotnet publish Joya.Pedidos.Api/Joya.Pedidos.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Joya.Pedidos.Api.dll"]
