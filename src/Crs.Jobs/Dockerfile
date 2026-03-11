# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Crs.sln", "./"]
COPY ["src/Crs.Jobs/Crs.Jobs.csproj", "src/Crs.Jobs/"]
COPY ["src/Crs.Core/Crs.Core.csproj", "src/Crs.Core/"]
COPY ["src/Crs.Infrastructure/Crs.Infrastructure.csproj", "src/Crs.Infrastructure/"]
COPY ["src/Crs.Recommendation/Crs.Recommendation.csproj", "src/Crs.Recommendation/"]
COPY ["src/Crs.Llm/Crs.Llm.csproj", "src/Crs.Llm/"]

# Restore dependencies
RUN dotnet restore "src/Crs.Jobs/Crs.Jobs.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/Crs.Jobs"
RUN dotnet build "Crs.Jobs.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Crs.Jobs.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published app
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Crs.Jobs.dll"]
