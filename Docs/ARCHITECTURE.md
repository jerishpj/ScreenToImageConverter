# Architecture Overview

This document provides a high-level overview of ScreenToImageConverter's architecture. For detailed information, diagrams, and step-by-step guides, refer to [DEVELOPMENT.md](./DEVELOPMENT.md).

## Architecture Pattern: Vertical Slice

ScreenToImageConverter uses **Vertical Slice Architecture**, where each feature contains all the code necessary to implement that feature across all layers.

### What is Vertical Slice Architecture?

In vertical slices, features are organized by business capability rather than technical layer:

`
Traditional Layered          Vertical Slice
(by Technical Concern)       (by Feature)

Controllers/          →      Feature/
  Screenshot                  ├─ Commands/
  Blob                        ├─ Handlers/
  ServiceBus                  ├─ Models/
                              ├─ Interfaces/
Services/                     └─ Exceptions/
  Screenshot
  Blob
  ServiceBus

Data/
  Screenshot
  Blob
`

### Benefits

✅ **Self-Contained** – Features have all code they need  
✅ **Independent Teams** – No conflicts between features  
✅ **Easy to Test** – Feature-scoped dependencies  
✅ **Easy to Extend** – Add new features without touching others  
✅ **Clear Boundaries** – Shared contracts define feature interfaces  

## Solution Structure

`
ScreenToImageConverter/
├── src/
│   ├── ScreenToImageConverter.Shared/
│   │   ├── Configuration/          Settings classes
│   │   ├── Interfaces/             Service abstractions
│   │   ├── Messages/               Message contracts
│   │   ├── Exceptions/             Domain exceptions
│   │   └── Results/                Result patterns
│   │
│   └── ScreenToImageConverter.Worker/
│       ├── Program.cs              DI registration
│       ├── Worker.cs               Entry point
│       └── Features/               Vertical Slices
│           ├── ScreenshotCapture/
│           ├── BlobStorageUpload/
│           └── ServiceBusMessaging/
│
├── tests/
│   └── ScreenToImageConverter.Tests/
│
└── Docs/                           Documentation
`

## Three Core Features

### 1. Screenshot Capture
Captures screenshots from URLs using Playwright browser automation.

### 2. Blob Storage Upload
Uploads captured screenshots to Azure Blob Storage.

### 3. Service Bus Messaging
Consumes requests and publishes completion events via Azure Service Bus.

## Key Principles

- **Vertical Organization** – Features group all layers of functionality
- **Shared Contracts** – Features communicate via interfaces/messages
- **Independent Teams** – Each feature can be developed/deployed independently
- **Easy Testing** – Dependencies are scoped to features
- **Clear Boundaries** – Feature interfaces define contracts

## For More Information

- **Setup & First Run:** [GETTING_STARTED.md](./GETTING_STARTED.md)
- **Development & Extension:** [DEVELOPMENT.md](./DEVELOPMENT.md)
- **Configuration Reference:** [CONFIGURATION.md](./CONFIGURATION.md)
- **Quick Cheat Sheet:** [QUICK_REFERENCE.md](./QUICK_REFERENCE.md)

---

**Pattern:** Vertical Slice Architecture | **Status:** ✅ Production Ready
