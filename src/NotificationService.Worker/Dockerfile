# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["src/NotificationService.Worker/NotificationService.Worker.csproj", "src/NotificationService.Worker/"]
COPY ["src/NotificationService.Application/NotificationService.Application.csproj", "src/NotificationService.Application/"]
COPY ["src/NotificationService.Domain/NotificationService.Domain.csproj", "src/NotificationService.Domain/"]
COPY ["src/NotificationService.Infrastructure/NotificationService.Infrastructure.csproj", "src/NotificationService.Infrastructure/"]

RUN dotnet restore "src/NotificationService.Worker/NotificationService.Worker.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/src/NotificationService.Worker"
RUN dotnet build "NotificationService.Worker.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "NotificationService.Worker.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --home /app --gecos '' appuser && chown -R appuser /app
USER appuser

COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p logs

ENTRYPOINT ["dotnet", "NotificationService.Worker.dll"]