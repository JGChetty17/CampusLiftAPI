FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY CampusLift.API/CampusLift.API.csproj CampusLift.API/
RUN dotnet restore CampusLift.API/CampusLift.API.csproj
COPY . .
RUN dotnet publish CampusLift.API/CampusLift.API.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CampusLift.API.dll"]
