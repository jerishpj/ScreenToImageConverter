# Multi-stage build Dockerfile for ScreenToImageConverter Worker Service
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy project files
COPY ["src/ScreenToImageConverter.Worker/ScreenToImageConverter.Worker.csproj", "src/ScreenToImageConverter.Worker/"]

# Restore NuGet packages
RUN dotnet restore "src/ScreenToImageConverter.Worker/ScreenToImageConverter.Worker.csproj"

# Copy remaining source files
COPY . .

# Build the application
WORKDIR "/src/src/ScreenToImageConverter.Worker"
RUN dotnet build "ScreenToImageConverter.Worker.csproj" -c Release -o /app/build

# Publish the application
RUN dotnet publish "ScreenToImageConverter.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0

# Install Playwright dependencies for Chromium browser
# These are required for browser automation in the container
RUN apt-get update && apt-get install -y \
	# Core dependencies for Chromium
	libc6 \
	libx11-6 \
	libx11-xcb1 \
	libxcb1 \
	libxext6 \
	libxfixes3 \
	libxi6 \
	libxrandr2 \
	libxrender1 \
	libxss1 \
	libxtst6 \
	fonts-liberation \
	libappindicator1 \
	libindicator7 \
	libasound2 \
	libatk-bridge2.0-0 \
	libatk1.0-0 \
	libc6 \
	libcairo2 \
	libcups2 \
	libdbus-1-3 \
	libexpat1 \
	libfontconfig1 \
	libfreetype6 \
	libgbm1 \
	libgcc-s1 \
	libgconf-2-4 \
	libgdk-pixbuf2.0-0 \
	libglib2.0-0 \
	libgtk-3-0 \
	libharfbuzz0b \
	libicu72 \
	libjpeg62-turbo \
	libnspr4 \
	libnss3 \
	libpango-1.0-0 \
	libpangocairo-1.0-0 \
	libpango-1.0-0 \
	libpixman-1-0 \
	libpng16-16 \
	libstdc++6 \
	libx11-6 \
	libxcb1 \
	libxcomposite1 \
	libxcursor1 \
	libxdamage1 \
	libxdmcp6 \
	libxext6 \
	libxfixes3 \
	libxfont2 \
	libxft6 \
	libxi6 \
	libxinerama1 \
	libxkbcommon0 \
	libxkbfile1 \
	libxmu6 \
	libxmuu1 \
	libxpm4 \
	libxrandr2 \
	libxrender1 \
	libxres1 \
	libxss1 \
	libxt6 \
	libxtst6 \
	libxvmc1 \
	libxext6 \
	libxfixes3 \
	libxi6 \
	libxrandr2 \
	libxrender1 \
	ca-certificates \
	wget \
	curl \
	&& rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy published application from build stage
COPY --from=build /app/publish .

# Create non-root user for security (optional but recommended)
RUN useradd -m -u 1001 appuser && chown -R appuser:appuser /app
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
	CMD curl -f http://localhost:8080/health || exit 1

# Expose port (not strictly required for backend services, but good practice)
EXPOSE 8080

# Set environment variables
ENV DOTNET_RUNNING_IN_CONTAINER=true \
	DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
	ASPNETCORE_URLS=http://+:8080

# Entry point
ENTRYPOINT ["dotnet", "ScreenToImageConverter.Worker.dll"]
