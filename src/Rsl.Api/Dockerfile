# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Rsl.sln", "./"]
COPY ["src/Rsl.Api/Rsl.Api.csproj", "src/Rsl.Api/"]
COPY ["src/Rsl.Core/Rsl.Core.csproj", "src/Rsl.Core/"]
COPY ["src/Rsl.Infrastructure/Rsl.Infrastructure.csproj", "src/Rsl.Infrastructure/"]
COPY ["src/Rsl.Recommendation/Rsl.Recommendation.csproj", "src/Rsl.Recommendation/"]
COPY ["src/Rsl.Llm/Rsl.Llm.csproj", "src/Rsl.Llm/"]

# Restore dependencies
RUN dotnet restore "src/Rsl.Api/Rsl.Api.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/Rsl.Api"
RUN dotnet build "Rsl.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Rsl.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

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

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Rsl.Api.dll"]
