# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Rsl.sln", "./"]
COPY ["src/Rsl.Web/Rsl.Web.csproj", "src/Rsl.Web/"]
COPY ["src/Rsl.Core/Rsl.Core.csproj", "src/Rsl.Core/"]

# Restore dependencies
RUN dotnet restore "src/Rsl.Web/Rsl.Web.csproj"

# Copy all source code
COPY . .

# Publish the WebAssembly app
WORKDIR "/src/src/Rsl.Web"
RUN dotnet publish "Rsl.Web.csproj" -c Release -o /app/publish

# Runtime stage - use nginx to serve static files
FROM nginx:alpine AS final

# Copy nginx configuration
COPY src/Rsl.Web/nginx.conf /etc/nginx/nginx.conf

# Copy published WebAssembly files to nginx html directory
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

# Expose port
EXPOSE 8080

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
