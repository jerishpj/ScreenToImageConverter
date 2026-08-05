# 📌 Docker Quick Reference Card

## 🚀 Quick Start (Copy-Paste Ready)

### Windows PowerShell
```powershell
.\docker-helper.ps1 start
```

### Linux/Mac Bash
```bash
./docker-helper.sh start
```

---

## 🎯 Most Common Commands

### Check Status
```bash
docker-compose ps
```
**Result:** Shows all services with status (Up/Down/healthy/unhealthy)

### View Logs
```bash
docker-compose logs -f worker
```
**Result:** Live stream of worker logs (Ctrl+C to exit)

### Health Check
```bash
# Windows
.\docker-helper.ps1 health

# Linux/Mac
./docker-helper.sh health
```
**Result:** All ✅ means everything is working

### Stop Services
```bash
docker-compose stop
```
**Result:** Services stop but data persists in volumes

### Clean Everything
```bash
docker-compose down -v
```
**Result:** Removes containers, networks, and volumes (⚠️ deletes data!)

---

## 🌐 Service Endpoints

| Service | URL | Login |
|---------|-----|-------|
| Worker | http://localhost:8080/health | None |
| RabbitMQ | http://localhost:15672 | guest:guest |
| RabbitMQ API | http://localhost:5672 | guest:guest |
| Azurite | http://localhost:10000 | API key |

---

## ✅ Verification Tests

```bash
# Test Worker
curl http://localhost:8080/health

# Test RabbitMQ
curl -u guest:guest http://localhost:15672/api/aliveness-test

# Test Azurite
curl http://localhost:10000/devstoreaccount1?comp=list
```

**Expected:** No errors = ✅ Working

---

## 📊 Container Status Reference

| Status | Meaning | Action |
|--------|---------|--------|
| Up (healthy) | ✅ Running & ready | None needed |
| Up (starting) | ⏳ Initializing | Wait 30-40 sec |
| Up (unhealthy) | ❌ Running but failed health check | Check logs |
| Exited | ❌ Not running | Check logs, restart |

---

## 🐛 Quick Troubleshooting

| Problem | Command | Solution |
|---------|---------|----------|
| Docker not running | `docker ps` | Start Docker Desktop |
| Need to see errors | `docker-compose logs -f` | Check log output |
| Services not healthy | `docker-compose ps` | Wait 30-40 seconds |
| Restart needed | `docker-compose restart` | Restarts all services |
| Need fresh start | `docker-compose up --build -d` | Rebuild & restart |

---

## 📈 Expected Timeline

| Phase | Time | Action |
|-------|------|--------|
| Build Image | 5-10 min | `docker-helper start` |
| Start Services | 1 min | Docker compose up |
| Health Check | 30-40 sec | Services stabilize |
| Ready | 0 sec | All services healthy ✅ |
| **Total First Run** | **15-20 min** | Automatic |
| **Subsequent Runs** | **2-3 min** | Cached builds |

---

## 🔧 Helper Script All Commands

```bash
# Windows
.\docker-helper.ps1 build      # Build only
.\docker-helper.ps1 start      # Build + Start
.\docker-helper.ps1 stop       # Stop services
.\docker-helper.ps1 restart    # Restart services
.\docker-helper.ps1 down       # Remove containers
.\docker-helper.ps1 clean      # Delete everything
.\docker-helper.ps1 status     # Show status
.\docker-helper.ps1 logs       # View all logs
.\docker-helper.ps1 health     # Health check
.\docker-helper.ps1 ui         # Open RabbitMQ UI
.\docker-helper.ps1 test       # Send test message
.\docker-helper.ps1 help       # Show help

# Linux/Mac
./docker-helper.sh build       # Build only
./docker-helper.sh start       # Build + Start
./docker-helper.sh stop        # Stop services
./docker-helper.sh restart     # Restart services
./docker-helper.sh down        # Remove containers
./docker-helper.sh clean       # Delete everything
./docker-helper.sh status      # Show status
./docker-helper.sh logs        # View all logs
./docker-helper.sh health      # Health check
./docker-helper.sh ui          # Open RabbitMQ UI
./docker-helper.sh test        # Send test message
./docker-helper.sh help        # Show help
```

---

## 📚 Documentation Quick Links

| Need | Document | Time |
|------|----------|------|
| Start now | DOCKER_START_HERE.md | 3 min |
| Quick commands | DOCKER_QUICK_START.md | 2 min |
| Lost? | DOCKER_INDEX.md | 5 min |
| Details | DOCKER_SETUP_GUIDE.md | 15 min |
| Reference | DOCKER_CONTAINERIZATION_SUMMARY.md | 5 min |
| Validate | DOCKER_CHECKLIST.md | 20 min |

---

## 🎯 First Run Checklist

- [ ] Docker Desktop running (Windows/Mac) or daemon running (Linux)
- [ ] Terminal open in solution directory
- [ ] Run `docker-helper start`
- [ ] Wait 15-20 minutes for first build
- [ ] Run `docker-helper health`
- [ ] See all ✅ = Success!

---

## 🔐 Security Quick Notes

| Aspect | Status |
|--------|--------|
| Non-root user | ✅ Configured (appuser) |
| No secrets in image | ✅ Yes, env vars only |
| Health checks | ✅ Enabled |
| Latest base images | ✅ Use latest tags |
| For Azure | ⚠️ Use Key Vault |

---

## 💾 Data Persistence

```bash
# Volumes created
docker volume ls | grep screentoimageconverter

# Expected volumes
screentoimageconverter_rabbitmq-data
screentoimageconverter_azurite-data
screentoimageconverter_logs
```

---

## 🚨 Emergency Commands

```bash
# View all errors
docker-compose logs --tail=100 worker

# Restart everything
docker-compose restart

# Kill and rebuild
docker-compose down -v
docker-compose up --build -d

# Monitor resources
docker stats
```

---

## 📱 Handy Shortcuts

### Save These URLs
- RabbitMQ: `http://localhost:15672` (guest:guest)
- Worker: `http://localhost:8080/health`
- Azurite: `http://localhost:10000`

### Save These Commands
```bash
# Check health
docker-compose ps

# View logs
docker-compose logs -f worker

# Stop
docker-compose stop

# Start
docker-compose up -d
```

---

## ⏱️ Time Estimates

- First build: 10-15 minutes
- Subsequent starts: 2-3 minutes
- Adding service: 30 seconds
- Health checks: 30-40 seconds
- Complete setup: 30 minutes
- Full validation: 1 hour

---

## 🎓 Key Concepts

| Concept | Meaning |
|---------|---------|
| **Bridge Network** | Services communicate via container names |
| **Volume** | Persistent storage across container restarts |
| **Health Check** | Automatic verification service is ready |
| **Compose** | Orchestrate multiple containers together |
| **Dockerfile** | Container build specification |
| **Image** | Template for running containers |
| **Container** | Running instance of an image |

---

## 📝 Log Patterns to Look For

### Success Indicators
```
✅ Worker service ready
✅ Screenshot captured
✅ Image uploaded to blob storage
🎉 HTML to image conversion completed
```

### Error Indicators
```
❌ ERROR
❌ CRITICAL
❌ Connection refused
❌ TimeoutException
```

---

## 🔗 Port Reference

| Service | Port | Type |
|---------|------|------|
| RabbitMQ AMQP | 5672 | Internal |
| RabbitMQ UI | 15672 | Browser |
| Azurite Blob | 10000 | API |
| Azurite Queue | 10001 | API |
| Azurite Table | 10002 | API |
| Worker | 8080 | Health |

---

## 🎯 Success Verification

Run this command:
```bash
docker-compose ps
```

You should see:
```
NAME                           STATE           PORTS
screentoimageconverter-worker   Up (healthy)   8080/tcp
screentoimageconverter-rabbitmq Up (healthy)   5672/tcp, 15672/tcp
screentoimageconverter-azurite Up (healthy)   10000-10002/tcp
```

✅ All showing "Up (healthy)" = Success!

---

## 📋 Maintenance

### Daily
- ✅ Check logs for errors
- ✅ Monitor services with `docker-compose ps`

### Weekly
- ✅ Clean up old containers
- ✅ Review and update images if needed

### Monthly
- ✅ Update base images
- ✅ Security scanning
- ✅ Performance review

---

## 🚀 Moving to Azure

Once local testing is complete:

1. Build image: ✅ Done!
2. Create ACR: `az acr create ...`
3. Push image: `docker push ...`
4. Create AKS: `az aks create ...`
5. Deploy: `kubectl apply -f ...`

See `DOCKER_SETUP_GUIDE.md` for details.

---

## 💡 Pro Tips

1. **Use helper scripts** - They do everything correctly
2. **Check logs first** - Most issues visible there
3. **Wait for health checks** - Services need time
4. **Keep this card handy** - For quick reference
5. **Read the guides** - They're comprehensive
6. **Use docker-compose ps** - Always verify status
7. **Clean volumes regularly** - Reclaim disk space

---

## 🎉 You're Ready!

Everything is set up. Now:

1. Run: `docker-helper start`
2. Wait: 15-20 minutes
3. Verify: `docker-helper health`
4. Celebrate: All ✅!

---

**Print This Card** 📄 | **Bookmark It** 🔖 | **Share It** 📤

*Last Updated: 2024*
*Quick Reference v1.0*
