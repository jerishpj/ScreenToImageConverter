# Docker Containerization Guide for ScreenToImageConverter

## Quick Start

### Prerequisites
- Docker Desktop installed (Windows/Mac) or Docker Engine (Linux)
- Docker version 20.10+
- 4GB+ available RAM
- Disk space for images and volumes (~2GB)

### One-Command Local Testing
```bash
# Start all services with docker-compose
docker-compose up --build

# In another terminal, test the service
curl http://localhost:8080/health
```

---

## Step-by-Step Docker Setup

### Step 1: Verify Docker Installation

**Windows/Mac:**
```bash
# Check Docker version
docker --version

# Verify Docker Desktop is running
docker ps
```

**Linux:**
```bash
# Check Docker daemon
sudo systemctl status docker

# If not running, start it
sudo systemctl start docker
```

### Step 2: Build the Docker Image

```bash
# Build the image locally
docker build -t screentoimageconverter:latest .

# Verify the image was created
docker images | grep screentoimageconverter
```

**Build Output Expected:**
```
Step 1/N : FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
...
Step N/N : ENTRYPOINT ["dotnet", "ScreenToImageConverter.Worker.dll"]
Successfully tagged screentoimageconverter:latest
```

**Image Size:** ~1.2-1.5 GB (due to Playwright dependencies)

### Step 3: Local Testing with Docker Compose

Docker Compose orchestrates three services:
- **RabbitMQ**: Message broker (port 5672)
- **Azurite**: Azure Storage emulator (port 10000)
- **Worker**: ScreenToImageConverter service (port 8080)

#### Start the Services

```bash
# Start all services in detached mode
docker-compose up -d

# View logs from all services
docker-compose logs -f

# View logs from specific service
docker-compose logs -f worker
docker-compose logs -f rabbitmq
docker-compose logs -f azurite
```

#### Verify Services Are Running

```bash
# List running containers
docker-compose ps

# Expected output:
# NAME                                COMMAND                 STATE              PORTS
# screentoimageconverter-worker       "dotnet ScreenToImag…"  Up (healthy)       8080/tcp
# screentoimageconverter-rabbitmq     "rabbitmq-server"       Up (healthy)       5672/tcp, 15672/tcp
# screentoimageconverter-azurite      "node /opt/nodejs…"     Up (healthy)       10000-10002/tcp
```

### Step 4: Health Check Verification

```bash
# Check worker service health
curl http://localhost:8080/health

# Expected response:
# Healthy

# Check RabbitMQ health
curl http://localhost:15672/api/aliveness-test -u guest:guest

# Expected response:
# {"status":"ok"}

# Check Azurite storage
curl http://localhost:10000/devstoreaccount1?comp=list

# Expected response:
# XML container list (or empty if no containers)
```

### Step 5: Access Management UIs

**RabbitMQ Management Console:**
- URL: http://localhost:15672
- Username: guest
- Password: guest
- You should see: No messages, queue setup in progress

**Azurite Storage Explorer:**
- Connect via Azure Storage Explorer (Desktop app)
- Connection string: `DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=dGVzdGtleQ==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1/;`
- Should show: Empty containers (or 'screenshots' container after first message)

### Step 6: View Container Logs

```bash
# Tail live logs from worker service
docker-compose logs -f worker

# Expected log patterns:
# 🎯 Worker service started
# 📢 Starting message consumer with resilience handling
# ✅ Worker service ready
# 💾 Initializing Playwright screenshot provider
```

### Step 7: Send Test Message to RabbitMQ

```bash
# Using Python (install pika: pip install pika)
python3 << 'EOF'
import pika
import json
from datetime import datetime

# Connect to RabbitMQ
connection = pika.BlockingConnection(pika.ConnectionParameters('localhost'))
channel = connection.channel()

# Declare exchange and queue
channel.exchange_declare(exchange='screenshot-requests', exchange_type='topic', durable=True)
channel.queue_declare(queue='screenshot-requests-queue', durable=True)
channel.queue_bind(exchange='screenshot-requests', queue='screenshot-requests-queue', routing_key='screenshot.request')

# Create test message
test_message = {
	"requestId": "test-001",
	"url": "https://www.example.com",
	"viewportWidth": 1920,
	"viewportHeight": 1080,
	"timeoutMs": 30000,
	"sourceId": "test-source"
}

# Publish message
channel.basic_publish(
	exchange='screenshot-requests',
	routing_key='screenshot.request',
	body=json.dumps(test_message)
)

print("✅ Test message published!")
connection.close()
EOF
```

**Or using curl + rabbitmq-admin plugin:**
```bash
# First, enable rabbitmq_management plugin (usually enabled by default)
docker-compose exec rabbitmq rabbitmq-plugins enable rabbitmq_management

# Then publish via HTTP API
curl -i -u guest:guest -H "content-type:application/json" \
  -XPOST http://localhost:15672/api/exchanges/%2F/screenshot-requests/publish \
  -d'{"properties":{},"routing_key":"screenshot.request","payload":"test payload","payload_encoding":"string"}'
```

### Step 8: Monitor Message Processing

```bash
# Watch worker logs for message processing
docker-compose logs -f worker

# Expected output:
# 📨 Processing message [RequestId: test-001]
# 📸 Capturing screenshot [CorrelationId: ...]
# ✅ Screenshot captured: 100 KB
# ☁️ Uploading to blob storage
# ✅ Image uploaded to blob storage
# 🎉 HTML to image conversion completed successfully
```

---

## Troubleshooting

### Issue: Docker daemon not running

**Windows/Mac:**
```bash
# Start Docker Desktop application manually
# OR restart Docker service
# Windows: Services → Docker Desktop → Restart
```

**Linux:**
```bash
sudo systemctl start docker
sudo usermod -aG docker $USER  # Add user to docker group
newgrp docker  # Refresh group membership
```

### Issue: Port already in use

```bash
# Find process using port
# Windows
netstat -ano | findstr :5672
# Or use specific tool
lsof -i :5672

# Kill the process or change docker-compose port mapping
# Edit docker-compose.yml and change port: "5672:5672" to "5673:5672"
```

### Issue: Azurite connection failed

```bash
# Check Azurite logs
docker-compose logs azurite

# Verify connection string format
# http://azurite:10000/devstoreaccount1 (from inside container)
# http://127.0.0.1:10000/devstoreaccount1 (from local host)

# Test connection
curl http://127.0.0.1:10000/devstoreaccount1?comp=list
```

### Issue: RabbitMQ connection timeout

```bash
# Check RabbitMQ status
docker-compose logs rabbitmq

# Verify container is healthy
docker-compose ps rabbitmq

# Restart RabbitMQ
docker-compose restart rabbitmq

# Wait for startup (30s)
sleep 30

# Test connection
curl -u guest:guest http://localhost:15672/api/vhosts
```

### Issue: Worker service keeps restarting

```bash
# Check worker logs for errors
docker-compose logs worker

# Common causes:
# - RabbitMQ not ready (wait 30-40 seconds)
# - Azurite not accessible (check DNS resolution)
# - Invalid configuration in appsettings.Docker.json

# Rebuild image if config changed
docker-compose up --build
```

### Issue: Out of memory errors

```bash
# Docker Compose might need more resources
# Windows/Mac: Docker Desktop → Settings → Resources → increase memory to 4GB+
# Linux: Check available memory with 'free -h'

# Alternatively, limit service memory in docker-compose.yml
# Add to service:
# deploy:
#   resources:
#     limits:
#       memory: 2G
```

---

## Stopping and Cleanup

```bash
# Stop all containers (preserves data)
docker-compose stop

# Stop and remove containers
docker-compose down

# Remove everything including volumes (WARNING: deletes data!)
docker-compose down -v

# Remove unused images and volumes
docker image prune -a
docker volume prune

# Remove specific image
docker rmi screentoimageconverter:latest
```

---

## Docker File Structure

```
ScreenToImageConverter/
├── Dockerfile                          # Multi-stage build configuration
├── docker-compose.yml                  # Local orchestration
├── .dockerignore                       # Exclude files from build context
├── src/
│   └── ScreenToImageConverter.Worker/
│       ├── appsettings.json           # Default config
│       ├── appsettings.Docker.json    # Docker-specific config
│       ├── Program.cs                 # Application entry point
│       └── [other source files]
├── tests/                              # Test projects
└── [other files]
└── tests/
	└── ScreenToImageConverter.Tests/
		└── [test files]
```

---

## Building for Different Environments

### Development (RabbitMQ, local Azurite)
```bash
docker-compose up -d
```

### Production-Like (Service Bus, Azure Storage)
```bash
# Create docker-compose.prod.yml with Azure credentials
# Build with:
docker build -t screentoimageconverter:prod .
docker run -e ServiceBus__FullyQualifiedNamespace=your-ns.servicebus.windows.net \
		   -e BlobStorage__ConnectionString=your-connection-string \
		   screentoimageconverter:prod
```

### Azure Container Registry
```bash
# Login to ACR
az acr login --name your-acr-name

# Tag image for ACR
docker tag screentoimageconverter:latest your-acr-name.azurecr.io/screentoimageconverter:latest

# Push to ACR
docker push your-acr-name.azurecr.io/screentoimageconverter:latest

# Deploy to AKS
kubectl apply -f k8s-deployment.yaml
```

---

## Next Steps: Azure Kubernetes Service (AKS)

Once you've verified the container works locally, you can:

1. **Create ACR** (Azure Container Registry)
   ```bash
   az acr create --resource-group myResourceGroup --name myRegistry --sku Basic
   ```

2. **Push image to ACR**
   ```bash
   docker tag screentoimageconverter:latest myRegistry.azurecr.io/screentoimageconverter:latest
   docker push myRegistry.azurecr.io/screentoimageconverter:latest
   ```

3. **Create AKS cluster**
   ```bash
   az aks create --resource-group myResourceGroup --name myCluster \
	 --node-count 1 --generate-ssh-keys \
	 --attach-acr myRegistry
   ```

4. **Deploy to AKS**
   ```bash
   kubectl apply -f k8s-deployment.yaml
   ```

See `DOCKER_TO_AKS_GUIDE.md` for detailed Azure Kubernetes Service deployment steps.

---

## Performance Tips

1. **Image Size Optimization**
   - Current: ~1.2-1.5 GB (includes all Playwright deps)
   - Consider: Use `mcr.microsoft.com/dotnet/runtime:9.0` if browser not needed
   - Alternative: Alpine-based images (smaller but less tested)

2. **Build Caching**
   ```bash
   # Leverage docker build cache
   docker build --cache-from screentoimageconverter:latest .

   # Clear cache if needed
   docker build --no-cache .
   ```

3. **Resource Limits**
   ```bash
   # Add to docker-compose.yml service
   deploy:
	 resources:
	   limits:
		 cpus: '2'
		 memory: 2G
	   reservations:
		 cpus: '1'
		 memory: 1G
   ```

---

## Security Considerations

✅ **Current Implementation:**
- Non-root user (appuser, UID 1001)
- Health checks enabled
- No hardcoded secrets in image
- Environment variables for configuration

⚠️ **For Production:**
- Use Azure Key Vault for secrets
- Enable container scanning (ACR)
- Implement RBAC in AKS
- Use private container registry
- Enable network policies
- Regular image updates

---

## Additional Resources

- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)
- [.NET Docker Images](https://github.com/dotnet/dotnet-docker)
- [Playwright Installation](https://playwright.dev/dotnet/docs/browsers#browser-dependencies)
- [Azure Container Registry](https://learn.microsoft.com/en-us/azure/container-registry/)
- [Azure Kubernetes Service](https://learn.microsoft.com/en-us/azure/aks/)

---

## Questions?

For issues or questions:
1. Check logs: `docker-compose logs -f [service-name]`
2. Verify health: `docker-compose ps`
3. Test connectivity: `docker-compose exec worker curl http://rabbitmq:5672`
4. Rebuild: `docker-compose up --build`
