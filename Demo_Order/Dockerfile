# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["Demo_Order.csproj", "./"]
RUN dotnet restore "Demo_Order.csproj"

# Copy all source files
COPY . .
RUN dotnet build "Demo_Order.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Demo_Order.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "Demo_Order.dll"]
