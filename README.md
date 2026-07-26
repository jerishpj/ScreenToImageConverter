# ScreenToImageConverter

<div align="center">

![GitHub](https://img.shields.io/badge/GitHub-jerishpj%2FScreenToImageConverter-blue?logo=github)
![.NET 9](https://img.shields.io/badge/.NET-9-blueviolet)
![Status](https://img.shields.io/badge/Status-Production%20Ready-brightgreen)
![Tests](https://img.shields.io/badge/Tests-185%2F185%20Passing-brightgreen)
![License](https://img.shields.io/badge/License-MIT-green)

**A production-ready .NET 9 Worker Service that converts HTML web pages into image screenshots**

</div>

---

## 📋 What It Does

**ScreenToImageConverter** is an asynchronous, cloud-native service that:
- 🌐 Converts HTML web pages to image screenshots
- 📤 Processes high-volume requests via Azure Service Bus
- 💾 Stores images in Azure Blob Storage with time-limited access
- 🔄 Publishes completion events for downstream systems
- ✅ 185/185 tests passing | Production ready

---

## 📚 Documentation

**Complete, consolidated documentation in two master documents:**

### 🔹 [GUIDE.md](./Docs/GUIDE.md) - Complete Operational & Development Guide
Covers everything you need to **use, deploy, and develop** ScreenToImageConverter:
- ✅ Getting Started & Setup
- ✅ Configuration Reference (all options)
- ✅ Deployment (Docker, Container Apps, AKS, ACI, CI/CD)
- ✅ Development Guide (architecture, patterns, extending)
- ✅ Testing Guide (test infrastructure, running tests)
- ✅ Troubleshooting (common issues & solutions)
- ✅ Operations & Monitoring

### 🔹 [REFERENCE.md](./Docs/REFERENCE.md) - Technical Reference & API
Comprehensive technical reference for **architects and integrators**:
- ✅ Architecture & Design Patterns
- ✅ Features & Functional Flows
- ✅ API Reference (message contracts, SDK examples in C#, Python, Node.js, Bash)
- ✅ Security Architecture (authentication, authorization, data protection)

**Start here based on your role:**
- **New user?** → Read [GUIDE.md](./Docs/GUIDE.md) – Getting Started section
- **DevOps/Ops?** → Read [GUIDE.md](./Docs/GUIDE.md) – Deployment & Operations sections
- **Developer?** → Read [GUIDE.md](./Docs/GUIDE.md) – Development & Testing sections
- **Integrator?** → Read [REFERENCE.md](./Docs/REFERENCE.md) – API Reference section
- **Architect?** → Read [REFERENCE.md](./Docs/REFERENCE.md) – Architecture & Design sections

---

## 🚀 Quick Build & Run

`bash
# Clone & setup
git clone https://github.com/jerishpj/ScreenToImageConverter.git
cd ScreenToImageConverter

# Restore & build
dotnet restore
dotnet build

# Run tests (all should pass)
dotnet test

# Run the service
dotnet run --project src/ScreenToImageConverter.Worker
`

**Next**: See [Getting Started Guide](./Docs/getting-started.md) for Azure setup and configuration.

---

## 🏗️ Architecture

**Pattern**: Vertical Slice Architecture | **Tests**: 185/185 ✅ | **Status**: Production Ready

`
Request → Service Bus → ConvertHtmlToImage Feature → Screenshot & Upload → Event Published
                          ├─ Validate
                          ├─ Capture (Playwright)
                          ├─ Upload (Blob Storage)
                          └─ Publish Event
`

---

## 🧠 Technology Stack

- .NET 9 | Azure Service Bus | Azure Blob Storage | Playwright | Serilog | XUnit + Moq

---

## ✨ Key Features

| Feature | Details |
|---------|---------|
| **Async Processing** | Scalable via Azure Service Bus |
| **Screenshot Capture** | Microsoft Playwright automation |
| **Cloud Storage** | Azure Blob with SAS URLs |
| **Event-Driven** | Publishes completion events |
| **Health Checks** | Browser, storage, configuration |
| **Structured Logging** | Serilog + Application Insights |
| **Production Ready** | Security, monitoring, tested |

---

## 📞 Support & Contributing

- **Repository**: https://github.com/jerishpj/ScreenToImageConverter
- **Issues**: [GitHub Issues](https://github.com/jerishpj/ScreenToImageConverter/issues)
- **Documentation**: See `Docs/` folder

---

**For complete documentation, start with [Docs/INDEX.md](./Docs/INDEX.md)** 📖
