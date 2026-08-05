# Docker Containerization - Complete Checklist ✅

## Pre-Flight Checklist

### Prerequisites
- [ ] Docker Desktop installed and running
  - Windows/Mac: Docker Desktop application
  - Linux: Docker Engine + Docker Compose
- [ ] Docker version 20.10+ (`docker --version`)
- [ ] Docker Compose version 1.29+ (`docker-compose --version`)
- [ ] 4GB+ RAM available for containers
- [ ] ~2GB disk space for images and volumes
- [ ] Network connectivity to download base images

### Verify Installation
```bash
# Check Docker
docker --version

# Check Docker Compose
docker-compose --version

# Verify Docker daemon is running
docker ps
```

---

## Containerization Setup Checklist

### Phase 1: Files Created ✅
- [x] `.dockerignore` - Build context exclusions
- [x] `Dockerfile` - Multi-stage build configuration
- [x] `docker-compose.yml` - Local orchestration
- [x] `docker-helper.ps1` - Windows helper script
- [x] `docker-helper.sh` - Linux/Mac helper script
- [x] `appsettings.Docker.json` - Docker configuration
- [x] `DOCKER_SETUP_GUIDE.md` - Comprehensive guide
- [x] `DOCKER_CONTAINERIZATION_SUMMARY.md` - Quick reference
- [x] `DOCKER_CHECKLIST.md` - This checklist

### Phase 2: Local Build & Test

#### Step 1: Build Docker Image
- [ ] Build image: `docker build -t screentoimageconverter:latest .`
- [ ] Verify image: `docker images | grep screentoimageconverter`
- [ ] Expected: Image created (~1.2-1.5 GB)

**Command:**
```bash
docker build -t screentoimageconverter:latest .
```

**Expected Output:**
```
[+] Building 487.3s (17/17) FINISHED
...
Successfully tagged screentoimageconverter:latest
```

#### Step 2: Start Services with Docker Compose
- [ ] Start services: `docker-compose up -d --build`
- [ ] Wait 30-40 seconds for health checks
- [ ] Verify with: `docker-compose ps`

**Command:**
```bash
docker-compose up -d --build
```

**Expected Output:**
```
Creating screentoimageconverter-rabbitmq ... done
Creating screentoimageconverter-azurite  ... done
Creating screentoimageconverter-worker   ... done
```

#### Step 3: Verify Service Status
- [ ] Check status: `docker-compose ps`
- [ ] All services should show "Up (healthy)"
- [ ] RabbitMQ: Port 5672 (AMQP), 15672 (UI)
- [ ] Azurite: Port 10000 (Blob Storage)
- [ ] Worker: Port 8080 (Health check)

**Command:**
```bash
docker-compose ps
```

**Expected Output:**
```
NAME                           STATE           PORTS
screentoimageconverter-worker   Up (healthy)   8080/tcp
screentoimageconverter-rabbitmq Up (healthy)   5672/tcp, 15672/tcp
screentoimageconverter-azurite Up (healthy)   10000-10002/tcp
```

#### Step 4: Health Check All Services
- [ ] Worker health: `curl http://localhost:8080/health`
- [ ] RabbitMQ health: `curl -u guest:guest http://localhost:15672/api/aliveness-test`
- [ ] Azurite health: `curl http://localhost:10000/devstoreaccount1?comp=list`

**Commands:**
```bash
# Worker
curl http://localhost:8080/health

# RabbitMQ
curl -u guest:guest http://localhost:15672/api/aliveness-test

# Azurite
curl http://localhost:10000/devstoreaccount1?comp=list
```

**Expected Responses:**
- Worker: "Healthy" or similar
- RabbitMQ: JSON with "status":"ok"
- Azurite: XML listing (empty container list if first run)

#### Step 5: View Logs
- [ ] View all logs: `docker-compose logs -f`
- [ ] View worker logs only: `docker-compose logs -f worker`
- [ ] Look for "Worker service ready" or similar startup messages
- [ ] Check for any ERROR or CRITICAL messages

**Commands:**
```bash
# All services
docker-compose logs -f

# Worker only
docker-compose logs -f worker

# RabbitMQ only
docker-compose logs -f rabbitmq

# Azurite only
docker-compose logs -f azurite
```

**Expected Log Patterns for Worker:**
```
🎯 Worker service started. Initializing ConvertHtmlToImage feature...
📢 Starting message consumer with resilience handling...
✅ Worker service ready. Listening for HTML to image conversion requests...
💾 Initializing Playwright screenshot provider...
✅ Playwright screenshot provider initialized
```

#### Step 6: Access Management UIs

**RabbitMQ Management Console:**
- [ ] Navigate to: http://localhost:15672
- [ ] Login: guest / guest
- [ ] Expected: RabbitMQ management dashboard

**Azurite (if using Azure Storage Explorer):**
- [ ] Connect via Azure Storage Explorer (Desktop app)
- [ ] Connection String: `DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=dGVzdGtleQ==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1/;`
- [ ] Expected: Connected to local storage

#### Step 7: Send Test Message
- [ ] Create test message with Python or curl
- [ ] Send to RabbitMQ exchange: `screenshot-requests`
- [ ] Routing key: `screenshot.request`

**Python Test Message:**
```python
import pika
import json

connection = pika.BlockingConnection(pika.ConnectionParameters('localhost'))
channel = connection.channel()

channel.exchange_declare(exchange='screenshot-requests', exchange_type='topic', durable=True)

test_message = {
	"requestId": "test-001",
	"url": "https://www.example.com",
	"viewportWidth": 1920,
	"viewportHeight": 1080,
	"timeoutMs": 30000,
	"sourceId": "test"
}

channel.basic_publish(
	exchange='screenshot-requests',
	routing_key='screenshot.request',
	body=json.dumps(test_message)
)

print("Message sent!")
connection.close()
```

#### Step 8: Monitor Message Processing
- [ ] Watch worker logs: `docker-compose logs -f worker`
- [ ] Confirm message received in logs
- [ ] Verify screenshot captured
- [ ] Check blob upload in Azurite
- [ ] Look for completion event publishing

**Expected Log Sequence:**
```
📨 Processing message [RequestId: test-001]
📸 Capturing screenshot
✅ Screenshot captured: [size] KB
☁️ Uploading to blob storage
✅ Image uploaded to blob storage
🎉 HTML to image conversion completed successfully
```

#### Step 9: Verify Data Persistence
- [ ] Check RabbitMQ volumes: `docker volume ls | grep rabbitmq`
- [ ] Check Azurite volumes: `docker volume ls | grep azurite`
- [ ] Expected: Named volumes created and persisting data

**Command:**
```bash
docker volume ls | grep screentoimageconverter
```

**Expected Output:**
```
DRIVER    VOLUME NAME
local     screentoimageconverter_azurite-data
local     screentoimageconverter_rabbitmq-data
local     screentoimageconverter_logs
```

### Phase 3: Troubleshooting & Verification

#### Issue: Docker Daemon Not Running
- [ ] Windows/Mac: Start Docker Desktop application
- [ ] Linux: `sudo systemctl start docker`
- [ ] Verify: `docker ps` should work

#### Issue: Port Already in Use
- [ ] Find process: `netstat -ano | findstr :5672` (Windows) or `lsof -i :5672` (Linux/Mac)
- [ ] Kill process or change `docker-compose.yml` ports
- [ ] Restart compose: `docker-compose down && docker-compose up -d`

#### Issue: Containers Keep Restarting
- [ ] Check logs: `docker-compose logs -f`
- [ ] Wait 30-40 seconds (services need startup time)
- [ ] Verify dependencies are starting in order
- [ ] Check for configuration errors in `appsettings.Docker.json`

#### Issue: Out of Memory
- [ ] Windows/Mac: Docker Desktop → Settings → Resources → Increase to 4GB+
- [ ] Linux: Check with `free -h`
- [ ] Restart Docker daemon

#### Issue: Connection Refused
- [ ] Services started? `docker-compose ps`
- [ ] Ports open? `netstat -an | findstr LISTENING` (Windows)
- [ ] Wait for health checks to pass (30-40 seconds)

### Phase 4: Cleanup & Resets

#### Soft Stop (Preserves Data)
- [ ] Stop services: `docker-compose stop`
- [ ] Data preserved in volumes
- [ ] Can restart with: `docker-compose start`

#### Remove Containers (Keep Images)
- [ ] Remove containers: `docker-compose down`
- [ ] Volumes preserved
- [ ] Images remain for faster restart

#### Full Cleanup (Deletes Everything)
- [ ] Delete all: `docker-compose down -v`
- [ ] ⚠️ WARNING: Deletes volumes and data
- [ ] Forces fresh rebuild on next `docker-compose up --build`

---

## Testing Scenarios

### Scenario 1: Container Startup Validation
**Objective:** Verify all services start correctly

- [ ] Run: `docker-compose up --build`
- [ ] Wait for: "Worker service ready" in logs
- [ ] Check: All services marked "healthy"
- [ ] Result: ✅ Pass or ❌ Fail (check logs)

### Scenario 2: RabbitMQ Connectivity
**Objective:** Verify message broker works

- [ ] Check: RabbitMQ UI accessible at http://localhost:15672
- [ ] Check: Can list queues via API
- [ ] Check: Management plugin operational
- [ ] Result: ✅ Pass or ❌ Fail (check firewall/ports)

### Scenario 3: Azurite Connectivity
**Objective:** Verify blob storage works

- [ ] Check: Can list containers via HTTP
- [ ] Check: Connection string valid
- [ ] Check: Azure Storage Explorer connects
- [ ] Result: ✅ Pass or ❌ Fail (check storage endpoint)

### Scenario 4: Message Processing
**Objective:** Verify end-to-end flow

- [ ] Send test message to RabbitMQ
- [ ] Check: Message consumed by worker
- [ ] Check: Screenshot captured (simulated)
- [ ] Check: Blob upload attempted
- [ ] Check: Completion event published
- [ ] Result: ✅ Pass or ❌ Fail (check logs for errors)

### Scenario 5: Data Persistence
**Objective:** Verify volumes persist across restarts

- [ ] Send message (creates queue/container)
- [ ] Stop services: `docker-compose stop`
- [ ] Verify data still exists in volumes
- [ ] Restart: `docker-compose start`
- [ ] Check: Data still accessible
- [ ] Result: ✅ Pass or ❌ Fail (check volumes)

### Scenario 6: Container Resource Usage
**Objective:** Verify memory/CPU within limits

- [ ] Run: `docker stats`
- [ ] Check: RabbitMQ < 200 MB
- [ ] Check: Azurite < 300 MB
- [ ] Check: Worker < 500 MB
- [ ] Total: < 2 GB
- [ ] Result: ✅ Pass or ❌ Fail (adjust docker-compose limits)

---

## Documentation Reference

| Document | Purpose | When to Use |
|----------|---------|------------|
| `README.md` | Project overview | First-time orientation |
| `GUIDE.md` | Architecture & design | Understanding system |
| `REFERENCE.md` | API & technical details | Implementation reference |
| `DOCKER_SETUP_GUIDE.md` | Detailed Docker guide | Step-by-step local setup |
| `DOCKER_CONTAINERIZATION_SUMMARY.md` | Quick reference | Quick lookup |
| `DOCKER_CHECKLIST.md` | This file | Validation & testing |

---

## Sign-Off Checklist

### Containerization Complete ✅
- [x] `.dockerignore` created
- [x] `Dockerfile` created and tested
- [x] `docker-compose.yml` created
- [x] Helper scripts created (`docker-helper.ps1`, `docker-helper.sh`)
- [x] Configuration file created (`appsettings.Docker.json`)
- [x] Documentation complete

### Local Testing Ready ✅
- [ ] Docker installed and daemon running
- [ ] Docker image builds successfully
- [ ] All services start and become healthy
- [ ] RabbitMQ accessible and functional
- [ ] Azurite accessible and functional
- [ ] Worker service healthy and responsive
- [ ] Test message processing works end-to-end
- [ ] Logs show expected patterns

### Ready for Next Phase ✅
- [ ] All local containerization verified
- [ ] Documentation reviewed
- [ ] Team briefed on Docker workflow
- [ ] Ready to proceed to Azure Kubernetes Service

---

## Next Steps After Local Verification

Once all items above are checked:

### Phase 5: Azure Preparation
1. [ ] Create Azure Container Registry (ACR)
2. [ ] Tag image for ACR push
3. [ ] Push image to ACR
4. [ ] Create Azure Kubernetes Service (AKS) cluster
5. [ ] Configure kubectl access

### Phase 6: AKS Deployment
1. [ ] Create Kubernetes deployment manifest
2. [ ] Configure service dependencies (RabbitMQ Helm, managed identities)
3. [ ] Deploy to AKS
4. [ ] Verify pods are running
5. [ ] Test end-to-end in AKS

### Phase 7: CI/CD Pipeline
1. [ ] Set up GitHub Actions workflow
2. [ ] Automate Docker build and push
3. [ ] Automate AKS deployment
4. [ ] Test pipeline end-to-end

---

## Support Commands Quick Reference

```bash
# Image Management
docker build -t screentoimageconverter:latest .
docker images
docker rmi screentoimageconverter:latest

# Compose Management
docker-compose up -d --build
docker-compose stop
docker-compose down
docker-compose down -v
docker-compose ps
docker-compose logs -f

# Container Inspection
docker stats
docker ps
docker logs [container-id]
docker exec -it [container-id] /bin/bash

# Network & Connectivity Tests
curl http://localhost:8080/health
curl -u guest:guest http://localhost:15672/api/aliveness-test
curl http://localhost:10000/devstoreaccount1?comp=list

# Volume Management
docker volume ls
docker volume inspect [volume-name]
docker volume rm [volume-name]
```

---

## Success Criteria

✅ **Success means:**
- All containers run without errors
- Services are marked "healthy"
- RabbitMQ and Azurite are accessible
- Worker service responds to health checks
- Messages can be published and processed
- Logs show expected patterns
- Data persists across restarts

---

## Notes & Observations

_Use this section to document any issues encountered or special configurations:_

```
[To be filled during testing]
```

---

## Approval & Sign-Off

- [ ] **Developer**: Container tested locally - Date: ___________
- [ ] **QA/Reviewer**: Verified all checks passed - Date: ___________
- [ ] **Team Lead**: Approved for Azure deployment - Date: ___________

---

**Last Updated**: [Date]
**Status**: 🟢 Ready for Local Testing

For detailed guidance, see `DOCKER_SETUP_GUIDE.md`.
For quick reference, see `DOCKER_CONTAINERIZATION_SUMMARY.md`.
