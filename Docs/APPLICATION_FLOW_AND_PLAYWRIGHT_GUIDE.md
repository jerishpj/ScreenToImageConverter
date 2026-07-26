# 📊 Application Flow: HTML to Image Conversion & Playwright Browser Automation

## Overview

Your application is a **worker service** that continuously listens for conversion requests and processes them asynchronously. When a request arrives, it performs a complete workflow: validate → capture screenshot → upload to storage → publish completion event.

---

## 🔄 Complete Application Flow

### Phase 1: Application Startup

```
dotnet run
	↓
Program.cs starts
	↓
1. Configure Serilog (structured logging)
2. Register Application Insights
3. Register application configuration
4. Register ConvertHtmlToImage feature (DI setup)
5. Register health checks
6. Register hosted services (Worker)
	↓
Build and start the host
	↓
Initialize Playwright (download browser binaries)
	↓
Start the Worker service (BackgroundService)
	↓
✅ Application running and listening for messages
```

### Phase 2: Request Reception

```
Message arrives from Service Bus/RabbitMQ
	↓
Queue: screenshot-requests (or Service Bus subscription)
	↓
IMessageConsumer (ServiceBusConsumer or RabbitMqConsumer) receives message
	↓
Deserializes message → HtmlScreenshotRequest object
	↓
Passes to Worker's ProcessMessageAsync callback
	↓
Worker creates scope and gets ConvertHtmlToImageHandler
```

### Phase 3: Request Processing (3-Step Workflow)

```
┌─────────────────────────────────────────┐
│  STEP 1: VALIDATION                     │
├─────────────────────────────────────────┤
│ Input: HtmlScreenshotRequest            │
│ ├─ URL: string (required)               │
│ ├─ ViewportWidth: int? (optional)       │
│ ├─ ViewportHeight: int? (optional)      │
│ ├─ TimeoutMs: int? (optional)           │
│ ├─ RequestId: string                    │
│ └─ SourceId: string                     │
│                                         │
│ Validation Checks:                      │
│ ✓ URL is not empty                      │
│ ✓ URL is a valid URI                    │
│ ✓ ViewportWidth > 0 (if specified)      │
│ ✓ ViewportHeight > 0 (if specified)     │
│ ✓ TimeoutMs > 0 (if specified)          │
│                                         │
│ If validation fails → Return error      │
│ If validation passes → Continue         │
└─────────────────────────────────────────┘
		 ↓
┌─────────────────────────────────────────┐
│  STEP 2: SCREENSHOT CAPTURE             │
│  (Playwright Headless Browser)          │
├─────────────────────────────────────────┤
│ Input:                                  │
│ ├─ URL                                  │
│ ├─ ViewportWidth (default: 1920px)      │
│ ├─ ViewportHeight (default: 1080px)     │
│ └─ TimeoutMs (default: 30000ms = 30s)   │
│                                         │
│ Process (with up to 3 retry attempts):  │
│ 1. Launch Chromium headless browser     │
│ 2. Create browser context with viewport │
│ 3. Create new page                      │
│ 4. Set timeout settings                 │
│ 5. Navigate to URL (wait for loaded)    │
│ 6. Wait 500ms for additional content    │
│ 7. Capture screenshot (PNG format)      │
│ 8. Close page and context               │
│                                         │
│ Output: byte[] (image data)             │
│ Size: typically 50-500 KB               │
│                                         │
│ Retry Logic:                            │
│ • On timeout → Retry (max 3 times)      │
│ • On network error → Retry              │
│ • On other errors → Fail immediately    │
└─────────────────────────────────────────┘
		 ↓
┌─────────────────────────────────────────┐
│  STEP 3: UPLOAD TO BLOB STORAGE         │
├─────────────────────────────────────────┤
│ Input: byte[] (screenshot)              │
│                                         │
│ Process:                                │
│ 1. Generate blob name:                  │
│    Path: screenshots/yyyy/MM/dd/        │
│    File: {RequestId}_{HHmmss}.png       │
│    Example: screenshots/2024/01/15/     │
│              req123_143025.png          │
│                                         │
│ 2. Upload to Azure Blob Storage         │
│    Container: "screenshots"             │
│    Properties:                          │
│    • Content-Type: image/png            │
│    • Metadata: RequestId, CorrelationId │
│                                         │
│ 3. Generate SAS URL (expires in 1 hour) │
│                                         │
│ Output: UploadResult                    │
│ ├─ BlobUri (direct download)            │
│ ├─ SasUrl (signed access, limited time) │
│ ├─ ContainerName                        │
│ └─ Metadata                             │
└─────────────────────────────────────────┘
		 ↓
Build response and return success
```

### Phase 4: Event Publishing

```
ConvertHtmlToImageHandler
	↓
Publishes completion event (fire-and-forget)
	↓
Message Broker:
├─ Development: RabbitMQ
│  Exchange: screenshot-completed
│  Routing Key: screenshot.completed
│
└─ Production: Azure Service Bus
   Topic: screenshot-completed

Subscribers receive:
├─ RequestId
├─ CorrelationId
├─ IsSuccessful (true/false)
├─ BlobUri (if successful)
├─ ErrorMessage (if failed)
├─ ProcessingDurationMs
└─ Other metadata
```

---

## 🌐 Playwright Browser Automation Explained

### What is Playwright?

**Playwright** is a browser automation library that controls real web browsers programmatically. Your application uses it to:
- Launch a headless Chromium browser
- Navigate to URLs
- Wait for pages to load
- Take screenshots
- Extract content

### What is Headless Mode?

```
Normal Browser:
┌──────────────────────┐
│  [X] ___ □ ◻         │ <- User interface visible
│ ┌──────────────────┐ │
│ │  Website renders │ │ <- User sees the page
│ └──────────────────┘ │
└──────────────────────┘

Headless Browser (Headless Mode):
┌──────────────────────┐
│ NO USER INTERFACE!   │ <- Invisible UI
│ ┌──────────────────┐ │
│ │  Website renders │ │ <- Renders but not displayed
│ └──────────────────┘ │
│ (runs in memory)     │
└──────────────────────┘

Benefits:
✅ Faster (no UI overhead)
✅ Lighter (lower memory)
✅ Automated (perfect for scripts)
✅ Server-friendly (no display needed)
```

### Playwright Screenshot Capture Process

```
1. BROWSER INITIALIZATION
   ┌──────────────────────────┐
   │ Playwright.CreateAsync() │
   │  (creates browser driver) │
   └────────────┬─────────────┘
				↓
   ┌──────────────────────────────────┐
   │ playwright.Chromium.LaunchAsync()│
   │                                  │
   │ Launch Options:                  │
   │ ├─ Headless: true (invisible)    │
   │ ├─ Args: ["--disable-automation"]│
   │ └─ NoSandbox: true (if enabled)  │
   │                                  │
   │ Returns: IBrowser instance       │
   └────────────┬─────────────────────┘
				↓

2. CONTEXT CREATION
   ┌──────────────────────────────────┐
   │ browser.NewContextAsync()        │
   │                                  │
   │ Context Options:                 │
   │ ├─ ViewportSize:                 │
   │ │  └─ Width: 1920 (customizable) │
   │ │  └─ Height: 1080               │
   │ ├─ DeviceScaleFactor: 1.0        │
   │ └─ Locale: "en-US"               │
   │                                  │
   │ Context = isolated browser       │
   │ session with custom settings     │
   └────────────┬─────────────────────┘
				↓

3. PAGE CREATION
   ┌──────────────────────────────────┐
   │ context.NewPageAsync()           │
   │                                  │
   │ Creates a new tab/page in the    │
   │ context                          │
   │                                  │
   │ Returns: IPage instance          │
   └────────────┬─────────────────────┘
				↓

4. TIMEOUT CONFIGURATION
   ┌──────────────────────────────────┐
   │ page.SetDefaultTimeout(30000)    │
   │                                  │
   │ Ensures:                         │
   │ ├─ Actions timeout after 30s     │
   │ └─ Prevents hanging forever      │
   │                                  │
   │ page.SetDefaultNavigationTimeout │
   │ (30000)                          │
   │                                  │
   │ Ensures page load timeout        │
   └────────────┬─────────────────────┘
				↓

5. NAVIGATION
   ┌──────────────────────────────────┐
   │ page.GotoAsync(url)              │
   │                                  │
   │ Steps:                           │
   │ ├─ Make HTTP request             │
   │ ├─ Receive HTML response         │
   │ ├─ Parse HTML                    │
   │ ├─ Download CSS/JS               │
   │ ├─ Execute JavaScript            │
   │ ├─ Render page                   │
   │ └─ Wait for "networkidle"        │
   │    (no network activity for 0.5s)│
   │                                  │
   │ WaitUntil: NetworkIdle           │
   │ = Page fully loaded with data    │
   └────────────┬─────────────────────┘
				↓

6. CONTENT LOADING
   ┌──────────────────────────────────┐
   │ await Task.Delay(500)            │
   │                                  │
   │ Additional wait for:             │
   │ ├─ Animations to complete        │
   │ ├─ Additional AJAX calls         │
   │ └─ Content to settle             │
   └────────────┬─────────────────────┘
				↓

7. SCREENSHOT CAPTURE
   ┌──────────────────────────────────┐
   │ page.ScreenshotAsync()           │
   │                                  │
   │ Options:                         │
   │ ├─ FullPage: false               │
   │ │  (capture visible viewport)    │
   │ └─ Type: PNG (png format)        │
   │                                  │
   │ Capture everything visible on    │
   │ the screen at 1920x1080          │
   │                                  │
   │ Returns: byte[] (PNG image data) │
   │ Size: 50-500 KB typically        │
   └────────────┬─────────────────────┘
				↓
		   ✅ SUCCESS
```

### Visual Representation: Screenshot Capture

```
URL: https://www.example.com
	↓
[Playwright Headless Chromium]
	↓
Request → Server receives request
	↓
Response ← Server sends HTML + CSS + JS
	↓
Parse & Render
	├─ Parse HTML structure
	├─ Load stylesheets
	├─ Execute JavaScript
	├─ Layout elements
	└─ Render to image
	↓
Viewport: 1920x1080
	┌──────────────────────────────┐
	│                              │ 1080px
	│   Rendered Website Content   │
	│                              │
	└──────────────────────────────┘
	← 1920px →
	↓
Capture Screenshot (PNG)
	↓
Return: PNG byte array (image data)
```

---

## 🚀 Playwright Binary Installation

### The Problem You're Facing

```
Error: Executable doesn't exist at 
C:\Users\LENOVO\AppData\Local\ms-playwright\chromium_headless_shell-1148\chrome-win\headless_shell.exe
```

This means the Chromium binary (the actual browser executable) hasn't been downloaded yet.

### Solution: Install Playwright Binaries

**Option 1: Automatic Installation** (Recommended)
```bash
cd C:\Jerish\Lab-POC\ScreenToImageConverter
pwsh bin/Debug/net9.0/playwright.ps1 install
```

This downloads and installs all required browser binaries.

**Option 2: Using Playwright CLI**
```bash
dotnet tool install -g Microsoft.Playwright.CLI
playwright install
```

**Option 3: Programmatic Installation** (Add to your code)
```csharp
// Add this before InitializePlaywrightAsync() is called
await Microsoft.Playwright.Playwright.CreateAsync();
// This triggers automatic download on first run
```

### What Gets Installed

```
C:\Users\LENOVO\AppData\Local\ms-playwright\
├── chromium_headless_shell-1148\
│   └── chrome-win\
│       ├── chrome.exe (or headless_shell.exe)
│       ├── chrome_*.pak
│       ├── .dll files
│       └── dependencies
├── firefox-1234\
│   └── firefox-bin.exe (if Firefox enabled)
└── webkit-1234\
	└── webkit binaries (if WebKit enabled)

Total Size: ~500 MB for all browsers
		   ~300 MB for just Chromium
```

---

## 📋 Application Architecture

### Dependency Injection Container

```
Application Services
	├── Logging (Serilog)
	├── Application Insights (Telemetry)
	├── IMessageConsumer
	│   ├─ ServiceBusConsumer (Production)
	│   └─ RabbitMqConsumer (Development)
	│
	├── IMessagePublisher
	│   ├─ ServiceBusPublisher (Production)
	│   └─ RabbitMqPublisher (Development)
	│
	├── ConvertHtmlToImageHandler
	│   ├─ IScreenshotProvider (Playwright)
	│   ├─ IBlobStorageService (Azure Blob)
	│   └─ IMessagePublisher
	│
	├── IScreenshotProvider
	│   └─ PlaywrightScreenshotProvider
	│
	├── IBlobStorageService
	│   └─ BlobStorageService
	│
	└── Worker (BackgroundService)
		├─ Starts message consumer
		├─ Listens for requests
		└─ Delegates to handler
```

### Message Flow Architecture

```
Message Broker              Worker Service              External Services
	│                            │                            │
	│─ HtmlScreenshotRequest ───→│                            │
	│                    (Async) │                            │
	│                            ├─ Validate                 │
	│                            │                            │
	│                            ├─ CaptureScreenshot ──────→│ Playwright
	│                            │   (Browser Automation)  ←──│ (Chromium)
	│                            │                            │
	│                            ├─ Upload Screenshot ───────→│ Azure Blob
	│                            │   (Store image)         ←──│ Storage
	│                            │                            │
	│ ScreenshotCompletedEvent ←│                            │
	│  (Fire & Forget)           │                            │
	│                            ├─ Publish Event            │
	│                            │                            │
```

---

## 🔧 Configuration

### PlaywrightOptions (appsettings.json)

```json
{
  "Playwright": {
	"BrowserType": "chromium",
	"DefaultViewportWidth": 1920,
	"DefaultViewportHeight": 1080,
	"DefaultTimeoutMs": 30000,
	"WaitUntilEvent": "NetworkIdle",
	"Headless": true,
	"DisableSandbox": false,
	"FullPage": false,
	"DeviceScaleFactor": 1.0,
	"MaxRetryAttempts": 3,
	"RetryDelayMs": 1000
  }
}
```

### Parameter Explanation

| Parameter | Purpose | Default | Notes |
|-----------|---------|---------|-------|
| BrowserType | Which browser to use | chromium | chromium, firefox, webkit |
| DefaultViewportWidth | Screenshot width | 1920 | pixels |
| DefaultViewportHeight | Screenshot height | 1080 | pixels |
| DefaultTimeoutMs | Max wait time | 30000 | milliseconds |
| Headless | Hide browser UI | true | Must be true for servers |
| DisableSandbox | Remove sandbox mode | false | Linux compatibility |
| FullPage | Capture full page | false | If true, scrolls to capture everything |
| MaxRetryAttempts | Retry attempts | 3 | On timeout/network error |
| RetryDelayMs | Delay between retries | 1000 | milliseconds |

---

## 📊 Data Models

### HtmlScreenshotRequest (Input)

```csharp
{
	"RequestId": "req-12345",
	"Url": "https://www.example.com",
	"ViewportWidth": 1920,           // Optional, uses default if null
	"ViewportHeight": 1080,          // Optional
	"TimeoutMs": 30000,              // Optional
	"SourceId": "source-id-123",
	"CorrelationId": "corr-id-456"
}
```

### ImageMetadataResponse (Output)

```csharp
{
	"RequestId": "req-12345",
	"Url": "https://www.example.com",
	"IsSuccessful": true,
	"ErrorMessage": null,
	"BlobFileName": "req-12345_143025.png",
	"BlobContainerName": "screenshots",
	"BlobUri": "https://storage.blob.core.windows.net/screenshots/2024/01/15/req-12345_143025.png",
	"BlobSasUrl": "https://...?sv=2021-06-08&sig=...",
	"SasUrlExpiresAt": "2024-01-15T15:30:25Z",
	"FileSizeBytes": 125432,
	"ContentType": "image/png",
	"ProcessedAt": "2024-01-15T14:30:25Z",
	"ProcessingDurationMs": 2543,
	"ProcessedByInstanceId": "machine-name",
	"RetryAttempts": 0,
	"CorrelationId": "corr-id-456",
	"SourceId": "source-id-123",
	"SchemaVersion": "1.0"
}
```

---

## 🎯 Error Handling & Retry Logic

### Retry Scenarios

```
Screenshot Capture Attempt
	↓
┌─────────────────────────────┐
│ Success?                    │
│ ├─ YES → Return image       │
│ └─ NO → Check error type    │
└─────────────────────────────┘
	↓
┌─────────────────────────────┐
│ Error Type?                 │
├─ Timeout                   │
│  └─ RETRY (up to 3x)       │
├─ Network (net::)           │
│  └─ RETRY (up to 3x)       │
├─ Other                      │
│  └─ FAIL immediately        │
└─────────────────────────────┘
	↓
After each retry:
   Wait 1000ms, then retry
	↓
After all retries exhausted:
   Throw ScreenshotCaptureException
	↓
Handler catches exception
   Return failure response
   Publish failure event
```

---

## 🚀 How to Get Started

### Step 1: Install Playwright Binaries

```powershell
cd C:\Jerish\Lab-POC\ScreenToImageConverter
pwsh bin/Debug/net9.0/playwright.ps1 install
```

Wait for installation to complete (5-10 minutes).

### Step 2: Run the Application

```powershell
dotnet run --project src/ScreenToImageConverter.Worker
```

Expected output:
```
🚀 HtmlToImageWorker Service starting...
Initializing Playwright screenshot provider...
🐰 Initializing RabbitMQ consumer...    [if Development]
Starting Service Bus message consumer...  [if Production]
✅ Worker service ready. Listening for HTML to image conversion requests...
```

### Step 3: Send a Test Request

Use RabbitMQ/Service Bus client to send:
```json
{
	"RequestId": "test-123",
	"Url": "https://www.google.com",
	"SourceId": "test-source"
}
```

### Step 4: Verify in Logs

```
🚀 Starting HTML to image conversion [RequestId: test-123]
📋 Step 1/3: Validating request
✅ Screenshot captured: 125 KB
☁️ Step 3/3: Uploading to blob storage
✅ Image uploaded to blob storage
🎉 HTML to image conversion completed successfully
```

---

## 🎓 Key Concepts Summary

| Concept | Explanation |
|---------|-------------|
| **Headless Browser** | Browser without UI, runs invisibly, perfect for automation |
| **Viewport** | The window size the browser renders at (1920x1080) |
| **Screenshot** | PNG image of the rendered page (what you see in the viewport) |
| **Timeout** | Maximum time to wait for page to load (30 seconds) |
| **NetworkIdle** | Page is considered loaded when no network activity for 0.5s |
| **Context** | Isolated browser session with custom settings |
| **Page** | A tab within a context where content is rendered |
| **Retry** | Automatic retry for timeout/network errors (max 3 times) |

---

## ✅ Status

**Application is ready once you install Playwright binaries!**

Next: Run `pwsh bin/Debug/net9.0/playwright.ps1 install`

