FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY gymappAPI.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "gymappAPI.dll"]
