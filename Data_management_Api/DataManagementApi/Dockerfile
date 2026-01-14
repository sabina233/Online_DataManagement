# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["DataManagementApi.csproj", "./"]
RUN dotnet restore "DataManagementApi.csproj"
COPY . .
RUN dotnet publish "DataManagementApi.csproj" -c Release -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DataManagementApi.dll"]
