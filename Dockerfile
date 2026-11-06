FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# 1. Copy only the csproj file first to leverage Docker layer caching for restores
COPY CampusLift.API/CampusLift.API.csproj CampusLift.API/
RUN dotnet restore CampusLift.API/CampusLift.API.csproj

# 2. Copy the rest of the source code and publish
COPY . .
RUN dotnet publish CampusLift.API/CampusLift.API.csproj -c Release -o out

# 3. Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Render requires the app to listen on a specific port (usually 10000 or 8080)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CampusLift.API.dll"]
