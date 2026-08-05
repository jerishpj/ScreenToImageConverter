# ScreenToImageConverter - Docker Containerization Complete ✅

## What's Been Created

Your application is now fully containerized and ready for local testing! Here's what has been set up:

### 📦 Core Docker Files

| File | Purpose |
|------|---------|
| `Dockerfile` | Multi-stage build for containerized application |
| `.dockerignore` | Excludes unnecessary files from build context |
| `docker-compose.yml` | Orchestrates Worker + RabbitMQ + Azurite locally |
| `docker-helper.sh` | Bash helper script (Linux/Mac) |
| `docker-helper.ps1` | PowerShell helper script (Windows) |

### 🔧 Configuration Files

| File | Purpose |
|------|---------|
| `appsettings.Docker.json` | Docker-specific configuration |
| `DOCKER_SETUP_GUIDE.md` | Comprehensive setup documentation |
| `DOCKER_CONTAINERIZATION_SUMMARY.md` | This file - quick reference |

---

## Quick Start (Choose Your Platform)

### 🪟 Windows Users

```powershell
# Make script executable (first time only)
Set-ExecutionPolicy -ExecutionScope CurrentUser -Policy RemoteSigned

# Option 1: Use helper script (recommended)
.\docker-helper.ps1 start

# Option 2: Manual docker-compose
docker-compose up --build -d
```

### 🐧 Linux/Mac Users

```bash
# Make script executable (first time only)
chmod +x docker-helper.sh

# Option 1: Use helper script (recommended)
./docker-helper.sh start

# Option 2: Manual docker-compose
docker-compose up --build -d
```

---

## Service URLs & Credentials

Once services are running (wait 30-40 seconds for health checks):

| Service | URL | Credentials | Purpose |
|---------|-----|-------------|---------|
| Worker Health | http://localhost:8080/health | N/A | Check if worker is running |
| RabbitMQ AMQP | amqp://localhost:5672 | guest/guest | Message broker connection |
| RabbitMQ UI | http://localhost:15672 | guest/guest | Monitor queues & messages |
| Azurite Blob | http://localhost:10000 | See connection string below | Local blob storage |

**Azurite Connection String:**
```
DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=dGVzdGtleQ==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1/;
```

---

## Common Tasks

### ✅ Verify Everything is Working

**Windows:**
```powershell
.\docker-helper.ps1 health
```

**Linux/Mac:**
```bash
./docker-helper.sh health
```

**Expected Output:**
```
✅ RabbitMQ is healthy
✅ Azurite is healthy
✅ Worker service is healthy
```

### 📊 View Live Logs

**Windows:**
```powershell
# All services
.\docker-helper.ps1 logs

# Specific service
.\docker-helper.ps1 logs worker
```

**Linux/Mac:**
```bash
# All services
./docker-helper.sh logs

# Specific service
./docker-helper.sh logs worker
```

### 📤 Send Test Message

**Windows:**
```powershell
.\docker-helper.ps1 test
```

**Linux/Mac:**
```bash
./docker-helper.sh test
```

### 🛑 Stop Services

**Windows:**
```powershell
.\docker-helper.ps1 stop
```

**Linux/Mac:**
```bash
./docker-helper.sh stop
```

### 🗑️ Clean Everything

**Windows:**
```powershell
.\docker-helper.ps1 clean
```

**Linux/Mac:**
```bash
./docker-helper.sh clean
```

---

## What Happens When You Run It?

### 1. **Build Phase** (5-10 minutes, first time only)
   - Downloads .NET SDK base image (~500 MB)
   - Compiles your application in Release mode
   - Optimizes image size
   - Creates final container image (~1.2-1.5 GB)

### 2. **Service Startup** (30-40 seconds)
   - **RabbitMQ** starts and opens port 5672
   - **Azurite** starts and opens port 10000
   - **Worker Service** starts and waits for dependencies
   - Health checks verify all services are ready

### 3. **Ready State**
   - Worker listens for messages on RabbitMQ
   - All services healthy and interconnected
   - Ready to process screenshot requests

---

## How to Test It

### Step 1: Verify Services Running
```bash
# Windows
.\docker-helper.ps1 status

# Linux/Mac
./docker-helper.sh status
```

### Step 2: Check Health
```bash
# Windows
.\docker-helper.ps1 health

# Linux/Mac
./docker-helper.sh health
```

### Step 3: View RabbitMQ Console
```bash
# Windows
.\docker-helper.ps1 ui

# Linux/Mac
./docker-helper.sh ui
```
- Opens http://localhost:15672
- Username: **guest**
- Password: **guest**

### Step 4: Send Test Message
```bash
# Windows
.\docker-helper.ps1 test

# Linux/Mac
./docker-helper.sh test
```

### Step 5: Monitor Processing
```bash
# Windows - watch worker logs
.\docker-helper.ps1 logs worker

# Linux/Mac
./docker-helper.sh logs worker
```

**Expected Output in Logs:**
```
🎯 Worker service started. Initializing ConvertHtmlToImage feature...
📢 Starting message consumer with resilience handling...
✅ Worker service ready. Listening for HTML to image conversion requests...
💾 Initializing Playwright screenshot provider...
✅ Playwright screenshot provider initialized
📨 Processing message [RequestId: docker-test-001]
📸 Capturing screenshot
✅ Screenshot captured: 100 KB
☁️ Uploading to blob storage
✅ Image uploaded to blob storage
🎉 HTML to image conversion completed successfully
```

---

## Troubleshooting

### ❌ "Docker daemon is not running"

**Windows/Mac:**
- Open Docker Desktop application

**Linux:**
```bash
sudo systemctl start docker
```

### ❌ "Port already in use (5672, 10000, 8080)"

Find and kill the process:
```bash
# Windows
netstat -ano | findstr :5672
taskkill /PID [PID] /F

# Linux/Mac
lsof -i :5672
kill -9 [PID]
```

Or change ports in `docker-compose.yml`:
```yaml
ports:
  - "5673:5672"    # Changed from 5672
```

### ❌ "Containers keep restarting"

Check logs:
```bash
# Windows
.\docker-helper.ps1 logs

# Linux/Mac
./docker-helper.sh logs
```

Common causes:
- RabbitMQ not ready yet (wait 30-40 seconds)
- Port conflicts
- Missing configuration

### ❌ "Out of memory"

Docker Compose needs more resources:
- **Windows/Mac**: Docker Desktop → Settings → Resources → Increase to 4GB+
- **Linux**: Check with `free -h`

---

## Docker Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                  Docker Compose Network                     │
│  (screentoimageconverter-network - bridge mode)            │
│                                                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌────────────┐ │
│  │   RabbitMQ      │  │   Azurite       │  │   Worker   │ │
│  │                 │  │                 │  │   Service  │ │
│  │ Port 5672       │  │ Port 10000      │  │ Port 8080  │ │
│  │ AMQP broker     │  │ Blob storage    │  │ .NET app   │ │
│  │                 │  │                 │  │            │ │
│  │ Mgmt: 15672     │  │ Volumes:        │  │ Logs:      │ │
│  │                 │  │ azurite-data    │  │ ./logs     │ │
│  │ Volumes:        │  │                 │  │            │ │
│  │ rabbitmq-data   │  │                 │  │ Image:     │ │
│  └─────────────────┘  └─────────────────┘  │ 1.2-1.5 GB │ │
│                                             └────────────┘ │
└─────────────────────────────────────────────────────────────┘
			↕                    ↕                    ↕
	   localhost:5672      localhost:10000      localhost:8080
	   localhost:15672
```

---

## Next Steps: Azure Kubernetes Service

Once you've confirmed the container works locally:

### 1. Create Azure Container Registry (ACR)
```bash
az acr create --resource-group myResourceGroup --name myRegistry --sku Basic
```

### 2. Tag and Push Image
```bash
docker tag screentoimageconverter:latest myRegistry.azurecr.io/screentoimageconverter:latest
docker push myRegistry.azurecr.io/screentoimageconverter:latest
```

### 3. Create AKS Cluster
```bash
az aks create --resource-group myResourceGroup --name myCluster \
  --node-count 1 --generate-ssh-keys \
  --attach-acr myRegistry
```

### 4. Deploy to AKS
```bash
kubectl apply -f k8s-deployment.yaml
```

See `DOCKER_SETUP_GUIDE.md` for detailed AKS instructions.

---

## File Structure

```
ScreenToImageConverter/
├── Dockerfile                          ← Multi-stage build
├── .dockerignore                       ← Build context exclusions
├── docker-compose.yml                  ← Local orchestration
├── docker-helper.sh                    ← Linux/Mac helper
├── docker-helper.ps1                   ← Windows helper
├── DOCKER_SETUP_GUIDE.md              ← Detailed guide
├── DOCKER_CONTAINERIZATION_SUMMARY.md ← This file
├── appsettings.Docker.json            ← Docker config
│   └── src/ScreenToImageConverter.Worker/
│
├── src/
│   └── ScreenToImageConverter.Worker/
│       ├── appsettings.json           ← Default config
│       ├── appsettings.Docker.json    ← Docker config ← NEW
│       ├── Program.cs
│       └── [other source files]
│
└── tests/
	└── ScreenToImageConverter.Tests/
		└── [test files]
```

---

## Configuration Reference

### Worker Service Environment Variables

| Variable | Value | Purpose |
|----------|-------|---------|
| `ASPNETCORE_ENVIRONMENT` | Development | ASP.NET Core environment |
| `RabbitMq__HostName` | rabbitmq | Container hostname |
| `RabbitMq__Port` | 5672 | AMQP port |
| `RabbitMq__UserName` | guest | RabbitMQ user |
| `RabbitMq__Password` | guest | RabbitMQ password |
| `BlobStorage__ConnectionString` | [see above] | Azurite connection |
| `Playwright__Headless` | true | Browser headless mode |
| `Playwright__DisableSandbox` | true | Browser sandbox disabled |

---

## Performance Notes

### Image Size
- **Current**: ~1.2-1.5 GB
- **Reason**: Includes full Playwright + Chromium dependencies
- **Optimization**: Consider Alpine-based images if browser not needed

### Build Time
- **First build**: 5-10 minutes (downloads base images)
- **Subsequent builds**: 1-2 minutes (uses cache)
- **Tip**: Use `docker build --cache-from` to leverage cache

### Memory Usage
- **RabbitMQ**: ~100-200 MB
- **Azurite**: ~200-300 MB
- **Worker**: ~300-500 MB
- **Total**: ~1-2 GB with headroom

---

## Security Considerations

✅ **Implemented:**
- Non-root user (appuser, UID 1001)
- No hardcoded secrets in image
- Health checks enabled
- Environment variable configuration

⚠️ **For Production:**
- Use Azure Key Vault for secrets
- Enable container registry scanning
- Implement Kubernetes RBAC
- Use private container registry
- Enable network policies
- Regular image updates

---

## Useful Commands Reference

### Docker Image Commands
```bash
# List images
docker images

# View image layers
docker history screentoimageconverter

# Remove image
docker rmi screentoimageconverter:latest

# Build with no cache
docker build --no-cache -t screentoimageconverter .
```

### Docker Container Commands
```bash
# View container logs
docker logs [container-id]

# Exec into container
docker exec -it [container-id] /bin/bash

# View container stats
docker stats

# Remove container
docker rm [container-id]
```

### Docker Compose Commands
```bash
# Start services
docker-compose up -d

# Build services
docker-compose build

# Build and start
docker-compose up --build -d

# Stop services
docker-compose stop

# Remove services
docker-compose down

# View logs
docker-compose logs -f [service]

# Show status
docker-compose ps

# Execute command in service
docker-compose exec [service] [command]

# Clean volumes
docker-compose down -v
```

---

## Support & Resources

- **Issues?** Check logs first: `docker-compose logs -f`
- **Help with setup?** See `DOCKER_SETUP_GUIDE.md`
- **Need more info?** See `GUIDE.md` for architecture details
- **Azure deployment?** See AKS section in this document

---

## Summary

🎉 **Your application is now containerized!**

### What You Can Do Now:
✅ Build Docker image locally
✅ Run services with docker-compose
✅ Test message processing
✅ Monitor with RabbitMQ UI
✅ Access local blob storage
✅ Deploy to Azure Kubernetes Service

### Next Steps:
1. Verify local containerization works
2. Prepare Azure infrastructure (ACR, AKS)
3. Deploy to Azure Kubernetes Service
4. Set up CI/CD pipeline for automated deployments

**Ready to containerize?** Run:
```bash
# Windows
.\docker-helper.ps1 start

# Linux/Mac
./docker-helper.sh start
```

Happy containerizing! 🐳
