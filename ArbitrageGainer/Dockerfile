FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

EXPOSE 8080

WORKDIR /source

COPY ./*.fsproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish -c release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ArbitrageGainer.dll"]