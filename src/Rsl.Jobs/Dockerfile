# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Rsl.sln", "./"]
COPY ["src/Rsl.Jobs/Rsl.Jobs.csproj", "src/Rsl.Jobs/"]
COPY ["src/Rsl.Core/Rsl.Core.csproj", "src/Rsl.Core/"]
COPY ["src/Rsl.Infrastructure/Rsl.Infrastructure.csproj", "src/Rsl.Infrastructure/"]
COPY ["src/Rsl.Recommendation/Rsl.Recommendation.csproj", "src/Rsl.Recommendation/"]
COPY ["src/Rsl.Llm/Rsl.Llm.csproj", "src/Rsl.Llm/"]

# Restore dependencies
RUN dotnet restore "src/Rsl.Jobs/Rsl.Jobs.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/Rsl.Jobs"
RUN dotnet build "Rsl.Jobs.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Rsl.Jobs.csproj" -c Release -o /app/publish /p:UseAppHost=false

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

ENTRYPOINT ["dotnet", "Rsl.Jobs.dll"]
