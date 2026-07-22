# ScreenToImageConverter - HTML to Image Screenshot Service

<div align="center">

![GitHub](https://img.shields.io/badge/GitHub-jerishpj%2FScreenToImageConverter-blue?logo=github)
![.NET 9](https://img.shields.io/badge/.NET-9-blueviolet)
![Status](https://img.shields.io/badge/Status-Production%20Ready-brightgreen)
![Tests](https://img.shields.io/badge/Tests-9%2F9%20Passing-brightgreen)
![License](https://img.shields.io/badge/License-MIT-green)

**A production-ready .NET 9 Worker Service that converts HTML web pages into image screenshots**

[Quick Start](#quick-start) • [Documentation](#documentation) • [Architecture](#architecture) • [Features](#features)

</div>

---

## 📋 Overview

**ScreenToImageConverter** is an asynchronous, cloud-native service that:
- 🌐 Converts HTML web pages to image screenshots
- 📤 Processes high-volume requests via Azure Service Bus
- 💾 Stores images in Azure Blob Storage with time-limited access
- 🔄 Publishes completion events for downstream processing
- 🏗️ Uses vertical slice architecture for maintainability

**Perfect for**: PDF report generation, website previews, content archiving, presentation automation

---

## 🎯 Quick Start

### Prerequisites
- .NET 9 SDK or later
- Visual Studio 2022/2026 (optional)
- Azure Storage Account
- Azure Service Bus Namespace

### Build & Run

`bash
# Clone
git clone https://github.com/jerishpj/ScreenToImageConverter.git
cd ScreenToImageConverter

# Restore & Build
dotnet restore
dotnet build

# Run
dotnet run --project src/ScreenToImageConverter.Worker

# Test
dotnet test
`

---

## 🏗️ Architecture

**Pattern**: Vertical Slice Architecture with a single feature (ConvertHtmlToImage)

`
Worker Service (Main Entry Point)
    ↓
Service Bus Consumer (Message Receiver)
    ↓
ConvertHtmlToImage Feature (Business Logic)
    ├─ Validate Request
    ├─ Capture Screenshot (Playwright)
    ├─ Upload to Blob Storage
    └─ Publish Completion Event
`

---

## 📚 Documentation

Complete documentation is available:

- **[Docs/SOLUTION_OVERVIEW.md](./Docs/SOLUTION_OVERVIEW.md)** ⭐ **START HERE** - Complete guide with:
  - Full functional flow with diagrams
  - Complete architecture overview
  - Core components description
  - Configuration details
  - Integration points
  - Deployment options
  - Troubleshooting guide
  - Technology stack

---

## 🧠 Technology Stack

- **Runtime**: .NET 9
- **Service Type**: Worker Service (BackgroundService)
- **Message Queue**: Azure Service Bus
- **Storage**: Azure Blob Storage
- **Browser**: Microsoft Playwright
- **Logging**: Serilog
- **Monitoring**: Application Insights
- **Testing**: XUnit + Moq
- **Pattern**: Vertical Slice Architecture

---

## ✨ Key Features

| Feature | Details |
|---------|---------|
| **Async Processing** | Azure Service Bus for scalable message handling |
| **Screenshot Capture** | Microsoft Playwright with Chromium/Firefox/WebKit |
| **Cloud Storage** | Azure Blob Storage with SAS URL generation |
| **Event Publishing** | Publishes completion events for downstream processing |
| **Health Checks** | Playwright, Blob Storage, and Configuration validation |
| **Structured Logging** | Serilog + Application Insights telemetry |
| **Production Ready** | Security, performance, monitoring, documentation |

---

## 🧪 Testing

`ash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity detailed

# Run specific test class
dotnet test --filter "FullyQualifiedName~ConvertHtmlToImageHandlerTests"
`

**Test Coverage**: 9/9 tests passing ✅

---

## 🔒 Security

- ✅ **Managed Identity** - Azure AD authentication (no credentials in code)
- ✅ **SAS URLs** - Time-limited access (1-hour expiration)
- ✅ **Input Validation** - URL format, dimension bounds, timeout values
- ✅ **Correlation IDs** - Distributed tracing support

---

## 📞 Support

- **Repository**: https://github.com/jerishpj/ScreenToImageConverter
- **Documentation**: [Docs/SOLUTION_OVERVIEW.md](./Docs/SOLUTION_OVERVIEW.md)

---

## ✅ Project Status

- ✅ Production Ready
- ✅ 9/9 Tests Passing
- ✅ Full Documentation
- ✅ Security Best Practices
- ✅ Clean Architecture

<div align="center">

**Built with ❤️ using .NET 9 and Azure Cloud Services**

</div>
