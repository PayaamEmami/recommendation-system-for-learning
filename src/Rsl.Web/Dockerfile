# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Rsl.sln", "./"]
COPY ["src/Rsl.Web/Rsl.Web.csproj", "src/Rsl.Web/"]

# Restore dependencies
RUN dotnet restore "src/Rsl.Web/Rsl.Web.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/Rsl.Web"
RUN dotnet build "Rsl.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Rsl.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

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

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Rsl.Web.dll"]
