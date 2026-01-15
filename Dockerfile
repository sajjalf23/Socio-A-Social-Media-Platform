# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore
COPY ["socio.csproj", "./"]
RUN dotnet restore "socio.csproj"

# Copy everything and build
COPY . .
RUN dotnet publish "socio.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 3000
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "socio.dll"]
ENV ASPNETCORE_URLS=http://0.0.0.0:3000