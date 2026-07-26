# Real-World Operational Testing Plan - ScreenToImageConverter

## 📋 Overview

This guide explains how to **deploy and operate the ScreenToImageConverter application** in a real environment and test it with actual use cases. This is NOT about unit tests—this is about **running the application live** and verifying it works end-to-end.

**What You'll Do:**
- Set up Azure resources (Service Bus, Blob Storage)
- Configure the application
- Start the worker service
- Send real screenshot requests
- Verify output and results
- Monitor application health

**Duration:** 1-2 hours for complete setup and testing  
**Environment:** Local development or Azure cloud

---

## 🎯 Real-World Use Cases

### Use Case 1: Website Thumbnail Generation
**Scenario:** Generate preview thumbnails of websites  
**Example:** "Create a 1920x1080 screenshot of www.microsoft.com"

### Use Case 2: PDF Report Generation
**Scenario:** Capture web content as images for inclusion in PDF reports  
**Example:** "Screenshot a report webpage and save it for document embedding"

### Use Case 3: Content Archival
**Scenario:** Archive web pages as images for compliance/audit  
**Example:** "Capture and store webpage screenshots with timestamps"

### Use Case 4: Automated Testing
**Scenario:** Generate visual baselines for regression testing  
**Example:** "Screenshot multiple URLs in different viewport sizes"

---

## 📦 Prerequisites

Before you start, you need:

### Software
```
✅ Visual Studio 2026 Community (already have)
✅ .NET 9 SDK
✅ Azure Storage Explorer (free tool)
✅ Azure Service Bus Explorer or Postman
✅ PowerShell (system terminal)
```

### Azure Resources (Required)
```
✅ Azure Service Bus Namespace (with Topic & Subscription)
✅ Azure Storage Account (with Blob Container)
✅ Connection strings & access keys for both
```

### Azure Credentials
```
✅ Azure Subscription
✅ Service Principal or User Account with permissions
✅ RBAC roles: Storage Account Contributor, Service Bus Data Owner
```

---

## 🏗️ Phase 1: Setup Azure Resources (15-20 minutes)

### Step 1.1: Create Azure Service Bus Namespace

**Location:** [Azure Portal](https://portal.azure.com)

**Steps:**
1. Go to **Service Bus**
2. Click **Create**
3. Fill in:
   - **Resource Group:** Create new (e.g., `rg-screenshot-converter`)
   - **Namespace Name:** `screenshot-sb-dev` (must be unique)
   - **Location:** Select your region
   - **Pricing Tier:** Standard (or Premium for production)
4. Click **Review + Create** → **Create**
5. Wait for deployment (2-3 minutes)

**Expected Result:**
```
✅ Service Bus Namespace created
✅ Status: Succeeded
```

---

### Step 1.2: Create Service Bus Topic

**In the newly created Service Bus Namespace:**

1. Go to **Topics** in the left menu
2. Click **+ Topic**
3. Fill in:
   - **Name:** `html-screenshot-requests` (inbound requests)
   - **Max size:** 1 GB
   - **Enable partitioning:** No
4. Click **Create**

**Expected Result:**
```
✅ Topic 'html-screenshot-requests' created
```

---

### Step 1.3: Create Service Bus Subscription

**On the 'html-screenshot-requests' topic:**

1. Click the topic name
2. Go to **Subscriptions** in the left menu
3. Click **+ Subscription**
4. Fill in:
   - **Subscription Name:** `screenshot-worker-sub`
   - **Max delivery count:** 10
   - **Enable dead lettering:** Yes
5. Click **Create**

**Expected Result:**
```
✅ Subscription 'screenshot-worker-sub' created
✅ Worker will listen to this subscription
```

---

### Step 1.4: Create Completion Event Topic

**For outbound events:**

1. Go back to Service Bus Namespace
2. Click **+ Topic**
3. Fill in:
   - **Name:** `html-screenshot-completed` (outbound events)
4. Click **Create**

**Expected Result:**
```
✅ Topic 'html-screenshot-completed' created
✅ This is where completion events are published
```

---

### Step 1.5: Get Service Bus Connection String

**Steps:**
1. In Service Bus Namespace, go to **Shared access policies**
2. Click **RootManageSharedAccessKey**
3. Copy the **Primary Connection String**
4. Store it safely (you'll need it for config)

**Expected Result:**
```
Connection String Format:
Endpoint=sb://screenshot-sb-dev.servicebus.windows.net/;
SharedAccessKeyName=RootManageSharedAccessKey;
SharedAccessKey=xxxxxxxxxxxxx
```

---

### Step 1.6: Create Azure Storage Account

**Location:** [Azure Portal](https://portal.azure.com)

**Steps:**
1. Go to **Storage Accounts**
2. Click **Create**
3. Fill in:
   - **Resource Group:** Select the same RG you created earlier
   - **Storage account name:** `screenshotdevelop` (must be globally unique, lowercase)
   - **Region:** Same as Service Bus
   - **Performance:** Standard
   - **Redundancy:** Locally-redundant storage (LRS)
4. Click **Review + Create** → **Create**

**Expected Result:**
```
✅ Storage Account created
✅ Status: Succeeded
```

---

### Step 1.7: Create Blob Container

**In the newly created Storage Account:**

1. Go to **Containers** (in the left menu under Data storage)
2. Click **+ Container**
3. Fill in:
   - **Name:** `screenshots` (lowercase, no spaces)
   - **Public access level:** Private
4. Click **Create**

**Expected Result:**
```
✅ Container 'screenshots' created
```

---

### Step 1.8: Get Storage Connection String

**Steps:**
1. In Storage Account, go to **Access keys**
2. Under **Key1**, copy the **Connection String**
3. Store it safely

**Expected Result:**
```
Connection String Format:
DefaultEndpointsProtocol=https;
AccountName=screenshotdevelop;
AccountKey=xxxxxxxxxxxxx;
EndpointSuffix=core.windows.net
```

---

## ⚙️ Phase 2: Configure the Application (10 minutes)

### Step 2.1: Update Application Configuration

**File:** `src/ScreenToImageConverter.Worker/appsettings.json`

**Current content might look like:**
```json
{
  "ServiceBusOptions": {
	"ConnectionString": "your-connection-string",
	"TopicName": "html-screenshot-requests",
	"SubscriptionName": "screenshot-worker-sub"
  },
  "BlobStorageOptions": {
	"ConnectionString": "your-connection-string",
	"ContainerName": "screenshots"
  }
}
```

**Steps:**
1. Open `appsettings.json` in Visual Studio
2. Replace `ServiceBusOptions.ConnectionString` with the connection string from Step 1.5
3. Replace `BlobStorageOptions.ConnectionString` with the connection string from Step 1.8
4. Verify topic and subscription names match what you created
5. Save the file

**Verification:**
```json
{
  "ServiceBusOptions": {
	"ConnectionString": "Endpoint=sb://screenshot-sb-dev.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=...",
	"TopicName": "html-screenshot-requests",
	"SubscriptionName": "screenshot-worker-sub"
  },
  "BlobStorageOptions": {
	"ConnectionString": "DefaultEndpointsProtocol=https;AccountName=screenshotdevelop;AccountKey=...;EndpointSuffix=core.windows.net",
	"ContainerName": "screenshots"
  }
}
```

---

### Step 2.2: Update Playwright Options

**File:** `appsettings.json`

**Add Playwright configuration:**
```json
{
  "PlaywrightOptions": {
	"Headless": true,
	"DefaultTimeout": 30000,
	"ViewportWidth": 1920,
	"ViewportHeight": 1080,
	"WaitForLoadState": "networkidle"
  }
}
```

**Explanation:**
- `Headless: true` = Run without visible browser (invisible in background)
- `DefaultTimeout: 30000` = Wait max 30 seconds for page load
- `ViewportWidth/Height` = Default screenshot size
- `WaitForLoadState: networkidle` = Wait for page to load completely

---

### Step 2.3: Verify Configuration is Valid

**Steps:**
1. Build the solution: `Ctrl+Shift+B`
2. Look for compilation errors

**Expected Result:**
```
✅ Build succeeds
✅ No errors or warnings
```

---

## 🚀 Phase 3: Start the Application (5 minutes)

### Step 3.1: Start the Worker Service Locally

**Steps:**
1. In Visual Studio, make sure `ScreenToImageConverter.Worker` is the startup project
2. Press `F5` or click the **Start** button (green play button)
3. Wait for the application to start

**Expected Console Output:**
```
🚀 HtmlToImageWorker Service starting...
Environment: Development
Initializing Playwright screenshot provider...
🎯 Worker service started. Initializing ConvertHtmlToImage feature...
📢 Starting Service Bus message consumer...
✅ Worker service ready. Listening for HTML to image conversion requests...
```

**Verification:**
- Application window stays open (doesn't close)
- No errors in the console
- "Listening for HTML to image conversion requests..." message appears

---

### Step 3.2: Verify Application is Connected to Azure

**Look for these log messages:**
```
✅ Connected to Service Bus Namespace: screenshot-sb-dev.servicebus.windows.net
✅ Listening to Topic: html-screenshot-requests
✅ Subscription: screenshot-worker-sub
✅ Blob Storage Container: screenshots
```

**If you see errors:**
- Check connection strings in `appsettings.json`
- Verify Azure resources exist
- Check network connectivity

---

## 📨 Phase 4: Send Test Requests (20-30 minutes)

Now that the worker is running and listening, you'll send screenshot requests and verify they work.

### Test Case 4.1: Send a Valid Screenshot Request

**Tool:** Service Bus Explorer or custom console app

**Request Details:**
```
Topic: html-screenshot-requests
Subscription: screenshot-worker-sub
Message Type: HtmlScreenshotRequest (JSON)

Payload:
{
  "RequestId": "test-001-" + (unique-id),
  "Url": "https://www.microsoft.com",
  "ViewportWidth": 1920,
  "ViewportHeight": 1080,
  "TimeoutMs": 30000
}
```

**Using Azure Service Bus Explorer:**

1. Download and open **Azure Service Bus Explorer**
2. Connect to your Service Bus Namespace (using connection string from Step 1.5)
3. Navigate to Topic → `html-screenshot-requests`
4. Click **Send Message**
5. In the message body, paste the JSON payload above
6. Click **Send**

**Expected Result:**
```
✅ Message sent successfully
✅ Worker processes the message immediately
✅ In worker console, you see: "Processing message [RequestId: test-001-...]"
```

**In Worker Console Output:**
```
📨 Processing message [RequestId: test-001-abc123, CorrelationId: ...]
✅ Screenshot captured successfully: 1920x1080
✅ Image uploaded to blob storage: screenshots/screenshot-test-001-abc123.png
✅ Completion event published to: html-screenshot-completed
✅ Conversion completed in 2.345 seconds
```

---

### Test Case 4.2: Verify Image was Saved to Blob Storage

**Steps:**
1. Open **Azure Storage Explorer** (or Azure Portal)
2. Connect to your Storage Account
3. Navigate to **Blob Containers** → **screenshots**
4. Look for the saved image file

**Expected Result:**
```
✅ File visible: screenshot-test-001-abc123.png (or similar)
✅ File size: 50-200 KB (typical PNG size)
✅ Modified date: Just now
```

**In Azure Storage Explorer:**
- Right-click the file → **Properties** → Check size and date
- Right-click the file → **Download** → Verify it's a valid image

---

### Test Case 4.3: Send Multiple Concurrent Requests

**Purpose:** Verify the application handles multiple requests in parallel

**Steps:**
1. Send 3-5 screenshot requests simultaneously (or quickly one after another)
2. Use different URLs for variety:
   - `https://www.google.com`
   - `https://www.github.com`
   - `https://www.stackoverflow.com`
   - `https://en.wikipedia.org/wiki/Cloud_computing`
   - `https://www.amazon.com`

**Payload for each (change RequestId and Url):**
```json
{
  "RequestId": "concurrent-001",
  "Url": "https://www.google.com",
  "ViewportWidth": 1920,
  "ViewportHeight": 1080,
  "TimeoutMs": 30000
}
```

**Expected Result:**
```
✅ All 5 requests processed successfully
✅ No errors or timeouts
✅ All 5 images saved to blob storage
✅ Processing time per request: 2-5 seconds
✅ Total time for all 5: ~15-20 seconds (parallel processing)
```

**In Worker Console:**
```
📨 Processing message [RequestId: concurrent-001, ...]
📨 Processing message [RequestId: concurrent-002, ...]
📨 Processing message [RequestId: concurrent-003, ...]
...
✅ Multiple screenshots processed concurrently
```

---

### Test Case 4.4: Test Different Viewport Sizes

**Purpose:** Verify the application respects custom viewport dimensions

**Payloads:**

**Small viewport (mobile):**
```json
{
  "RequestId": "mobile-001",
  "Url": "https://www.microsoft.com",
  "ViewportWidth": 375,
  "ViewportHeight": 667,
  "TimeoutMs": 30000
}
```

**Large viewport (4K):**
```json
{
  "RequestId": "4k-001",
  "Url": "https://www.microsoft.com",
  "ViewportWidth": 3840,
  "ViewportHeight": 2160,
  "TimeoutMs": 30000
}
```

**Expected Result:**
```
✅ Mobile request: Creates 375x667 screenshot
✅ 4K request: Creates 3840x2160 screenshot
✅ Different viewport sizes reflected in image dimensions
✅ File sizes vary based on content complexity
```

---

### Test Case 4.5: Test Long-Running Website Capture

**Purpose:** Verify timeout handling and long-load scenarios

**Payload:**
```json
{
  "RequestId": "long-load-001",
  "Url": "https://en.wikipedia.org/wiki/Python_(programming_language)",
  "ViewportWidth": 1920,
  "ViewportHeight": 1080,
  "TimeoutMs": 60000
}
```

**Expected Result:**
```
✅ Request processed successfully
✅ Long page loaded and captured
✅ Takes 5-10 seconds to complete
✅ Image saved with full page content
```

---

### Test Case 4.6: Send Request with Invalid URL

**Purpose:** Verify validation works and rejects bad input

**Payload:**
```json
{
  "RequestId": "invalid-001",
  "Url": "not-a-valid-url",
  "ViewportWidth": 1920,
  "ViewportHeight": 1080,
  "TimeoutMs": 30000
}
```

**Expected Result:**
```
⚠️ Validation failed
❌ Error message: "Url must be a valid HTTP or HTTPS URL."
❌ Request rejected immediately
✅ No image created
✅ Completion event published with error status
```

**In Worker Console:**
```
📨 Processing message [RequestId: invalid-001, ...]
❌ Validation failed: Url must be a valid HTTP or HTTPS URL.
⚠️ Failure response returned without processing
```

---

### Test Case 4.7: Send Request with Missing RequestId

**Purpose:** Verify required field validation

**Payload:**
```json
{
  "Url": "https://www.microsoft.com",
  "ViewportWidth": 1920,
  "ViewportHeight": 1080,
  "TimeoutMs": 30000
}
```

**Expected Result:**
```
❌ Validation failed: "RequestId is required."
❌ Request rejected
✅ No processing occurs
```

---

### Test Case 4.8: Send Request with Zero Viewport Width

**Purpose:** Verify dimension validation

**Payload:**
```json
{
  "RequestId": "invalid-viewport-001",
  "Url": "https://www.microsoft.com",
  "ViewportWidth": 0,
  "ViewportHeight": 1080,
  "TimeoutMs": 30000
}
```

**Expected Result:**
```
❌ Validation failed: "ViewportWidth must be greater than 0."
❌ Request rejected
✅ No image created
```

---

## 📊 Phase 5: Monitor Application Health (10 minutes)

### Step 5.1: Check Application Logs

**Location:** Visual Studio output window

**Look for these healthy signs:**
```
✅ Regular "Processing message" entries
✅ "Screenshot captured successfully" messages
✅ "Image uploaded to blob storage" messages
✅ "Completion event published" messages
✅ No error messages or exceptions
```

**Warning signs:**
```
❌ Connection timeout errors
❌ "Failed to connect to Service Bus"
❌ Repeated retries
❌ "Screenshot capture failed"
```

---

### Step 5.2: Monitor Blob Storage Growth

**Steps:**
1. Open Azure Storage Explorer
2. Go to **screenshots** container
3. Refresh periodically
4. Watch new files appear

**Expected Growth:**
```
After 5 requests: ~5 image files
After 10 requests: ~10 image files
Average file size: 50-150 KB per image
```

---

### Step 5.3: Check Service Bus Metrics

**In Azure Portal:**

1. Go to your Service Bus Namespace
2. Click **Metrics** in the left menu
3. Select timeframe: Last 1 hour
4. Add metric: **Incoming Messages**

**Expected Result:**
```
✅ Chart shows incoming message count
✅ Spikes when you send requests
✅ Confirms Service Bus receiving messages
```

---

### Step 5.4: Monitor Completion Events

**Purpose:** Verify completion events are being published

**Steps:**
1. In Azure Service Bus Explorer
2. Navigate to Topic: `html-screenshot-completed`
3. Look for messages being published

**Expected Result:**
```
✅ Messages appear in the completion topic
✅ One completion event per screenshot request
✅ Each event contains:
   - RequestId
   - Status (Success/Failed)
   - BlobUrl (SAS URL to the image)
   - ProcessingDurationMs
```

---

## ✅ Phase 6: Comprehensive Real-World Test Scenarios (15 minutes)

### Scenario 6.1: Website Preview Generation Workflow

**Business Use Case:** Generate preview thumbnails for a website discovery app

**Steps:**
1. Send 5 screenshot requests for different popular websites
2. Vary the viewport sizes (mobile, tablet, desktop)
3. Verify all images saved correctly

**Requests:**
```
Request 1: https://www.amazon.com (1920x1080 - Desktop)
Request 2: https://www.amazon.com (768x1024 - Tablet)
Request 3: https://www.amazon.com (375x667 - Mobile)
Request 4: https://www.github.com (1920x1080 - Desktop)
Request 5: https://www.github.com (375x667 - Mobile)
```

**Verification Checklist:**
```
☐ All 5 requests processed
☐ All 5 images saved to blob storage
☐ Mobile images are smaller (less content visible)
☐ Desktop images are larger (more content visible)
☐ Same URL different viewports produce different images
☐ File names are unique
☐ Processing times acceptable (2-8 seconds each)
☐ Completion events published for all
```

---

### Scenario 6.2: Batch Processing with Error Handling

**Business Use Case:** Process a batch of URLs, some valid and some invalid

**Steps:**
1. Send 10 requests: 7 valid URLs + 3 invalid URLs
2. Observe how the application handles the mix

**Requests Mix:**
```
Valid URLs:
- https://www.microsoft.com
- https://www.google.com
- https://www.github.com
- https://www.wikipedia.org
- https://www.stackoverflow.com
- https://www.amazon.com
- https://www.netflix.com

Invalid URLs:
- "invalid-url"
- "ftp://not-supported.com"
- "" (empty string)
```

**Verification Checklist:**
```
☐ 7 valid URLs: All successfully processed
☐ 7 images created in blob storage
☐ 3 invalid URLs: All rejected with validation errors
☐ No errors stop the application
☐ 7 success completion events published
☐ 3 failure completion events published
☐ Application continues running (no crashes)
☐ Error messages are clear and descriptive
```

---

### Scenario 6.3: Load Test - Rapid Sequential Requests

**Business Use Case:** High-volume screenshot generation (e.g., daily batch job)

**Steps:**
1. Send 20 screenshot requests rapidly (2-3 per second)
2. Monitor application performance
3. Verify all complete successfully

**Expected Behavior:**
```
Request Rate: 20 requests in 60 seconds
Processing: Parallel (worker processes multiple concurrently)
Expected Total Time: ~40-60 seconds
Average Time Per Request: 2-3 seconds
```

**Verification Checklist:**
```
☐ All 20 requests accepted
☐ No request timeouts
☐ No Service Bus connection errors
☐ All 20 images created successfully
☐ Completion events for all 20 published
☐ Application doesn't crash under load
☐ Memory usage remains stable
☐ CPU usage reasonable (40-60%)
```

---

### Scenario 6.4: Different Viewport Sizes for Responsive Design Testing

**Business Use Case:** Capture website in multiple resolutions for quality assurance

**Steps:**
Send same URL with various viewport sizes:

```
Device Type          Viewport Size
iPhone SE            375 x 667
iPhone 12 Pro        390 x 844
iPad Mini            768 x 1024
iPad Pro             1024 x 1366
Desktop (HD)         1280 x 720
Desktop (FHD)        1920 x 1080
Desktop (4K)         3840 x 2160
```

**Verification Checklist:**
```
☐ All 7 requests processed for same URL
☐ 7 different images created (different dimensions)
☐ File sizes vary (smaller viewport = smaller file)
☐ Each image correctly sized
☐ Website renders properly at all sizes
☐ All completion events contain correct dimensions
☐ Processing times increase with larger viewports
```

---

### Scenario 6.5: Long-Running Content Pages

**Business Use Case:** Capture complex/dynamic websites with significant load time

**Test URLs:**
```
- https://en.wikipedia.org/wiki/World_War_II (large article)
- https://www.amazon.com/s?k=python (product listing)
- https://github.com/torvalds/linux (large repo)
- https://www.bbc.com (news site with many elements)
```

**Verification Checklist:**
```
☐ All pages captured despite high load time
☐ Processing time: 5-15 seconds (reasonable for complex sites)
☐ Timeout handling: No premature timeouts
☐ Full page content captured (scrollable content included)
☐ All images saved successfully
☐ Completion events published with correct duration
☐ No memory leaks after processing heavy pages
```

---

## 🔍 Phase 7: Verify Output Quality (10 minutes)

### Step 7.1: Download and Inspect Screenshots

**Steps:**
1. Open Azure Storage Explorer
2. Right-click a screenshot file
3. Click **Download**
4. Open the image file in an image viewer

**Verification:**
```
✅ Image opens successfully
✅ Image is a valid PNG file
✅ Image shows the website content
✅ Image quality is clear and readable
✅ Layout matches what browser would show
✅ No artifacts or corruption
```

---

### Step 7.2: Compare Screenshots at Different Viewports

**Steps:**
1. Download 3 screenshots of same URL at different sizes:
   - Mobile (375x667)
   - Tablet (768x1024)
   - Desktop (1920x1080)
2. Open all 3 in image viewer
3. Compare the layouts

**Verification:**
```
✅ Mobile version: More content stacked vertically
✅ Tablet version: Medium layout
✅ Desktop version: Full width layout
✅ Responsive design working correctly
✅ Content displays appropriately for each size
```

---

### Step 7.3: Verify Image Metadata

**Using a tool like ExifTool or Image Inspector:**

**Steps:**
1. Check image properties
2. Verify dimensions match what was requested

**Expected Metadata:**
```
File Name: screenshot-test-001-xxxxx.png
Size: 50-200 KB
Dimensions: Match requested viewport (e.g., 1920x1080)
Color Space: RGB
Created: Timestamp of request
```

---

## 📈 Phase 8: Performance & Stress Testing (20 minutes)

### Test 8.1: Measure Processing Time per URL Type

**Steps:**
1. Send 5 requests each to 4 different URL types
2. Record processing time for each

**URL Types to Test:**
```
1. Simple static site: https://www.example.com (~1-2 sec)
2. News site (media-heavy): https://www.bbc.com (~4-6 sec)
3. Large SPA (JavaScript-heavy): https://www.github.com (~3-5 sec)
4. Content-heavy page: https://www.wikipedia.org (~5-8 sec)
```

**Create Measurement Table:**
```
URL Type                | Avg Time (sec) | Min | Max | Status
Static site            | 1.5            | 1.2 | 1.8 | ✅
News site              | 5.2            | 4.8 | 5.8 | ✅
JavaScript-heavy SPA   | 4.1            | 3.9 | 4.5 | ✅
Content-heavy page     | 6.8            | 6.2 | 7.3 | ✅
```

**Acceptance Criteria:**
```
✅ All times < 30 seconds (timeout threshold)
✅ Most requests complete in 2-8 seconds
✅ No timeouts or failures
```

---

### Test 8.2: Stress Test with Sustained Load

**Purpose:** Verify app stability over time

**Steps:**
1. Send 50 requests over 5 minutes (10 per minute)
2. Monitor logs for errors
3. Check memory usage
4. Verify all requests complete

**Expected Results:**
```
✅ All 50 requests processed
✅ No connection drops
✅ Memory usage stable (< 500 MB)
✅ CPU usage reasonable (30-70%)
✅ No application crashes
✅ Consistent processing times
✅ All 50 images saved successfully
```

---

### Test 8.3: Monitor Resource Consumption

**Using Task Manager (Windows):**

1. Open Task Manager (Ctrl+Shift+Esc)
2. Find the dotnet.exe or worker service process
3. Record metrics while processing:

**Baseline (Idle):**
```
Memory: ~80-150 MB
CPU: 0-1%
Threads: ~20-30
```

**During Processing:**
```
Memory: ~250-400 MB (acceptable)
CPU: 30-70% (normal)
Threads: ~40-60 (normal)
```

**Verification Criteria:**
```
✅ Memory doesn't continuously grow
✅ Memory returns near baseline after request completes
✅ CPU usage drops when idle
✅ No handle leaks
✅ Thread count stable
```

---

## 🚨 Phase 9: Error Scenarios & Recovery (15 minutes)

### Scenario 9.1: Network Disconnection Recovery

**Steps:**
1. Stop the worker application gracefully
2. Send a request (will be queued in Service Bus)
3. Restart the worker
4. Verify queued message is processed

**Expected Result:**
```
✅ Message remains in Service Bus queue
✅ Worker processes it upon restart
✅ Completion event published
✅ Screenshot created successfully
```

---

### Scenario 9.2: Blob Storage Temporary Unavailability

**Simulating the Scenario:**
1. Worker is running
2. Azure Storage temporarily goes down (or network access lost)
3. Send screenshot request
4. Restore storage access
5. Retry the request

**Expected Behavior:**
```
❌ First attempt: Screenshot failed (upload error)
⚠️ Error logged: "Failed to upload blob"
✅ Completion event: Status=Failed with error details
✅ Next request: Works normally after storage restored
```

---

### Scenario 9.3: Invalid/Unreachable URL Handling

**Test URLs:**
```
https://this-domain-definitely-does-not-exist-12345.com
https://localhost:12345/not-running
https://192.168.1.1:9999/blocked-port
```

**Expected Behavior:**
```
❌ Connection timeout or connection refused
⚠️ Error logged: "Failed to connect to URL"
✅ Completion event published with error status
⚠️ Error message: "Unable to reach the specified URL"
✅ Application continues running (no crash)
```

---

### Scenario 9.4: Service Restarts and Graceful Shutdown

**Steps:**
1. Worker is running and processing requests
2. Send a request
3. While processing, stop the application (close the window)
4. Check logs for graceful shutdown

**Expected Behavior:**
```
⏸️ Application receives shutdown signal
✅ Message: "Shutting down worker service gracefully..."
✅ Allows current request to complete if possible
✅ Closes connections cleanly
✅ No file corruption or data loss
```

---

## 📋 Phase 10: Final Verification & Sign-Off (10 minutes)

### Verification Checklist - COMPLETE WORKFLOW

```
SETUP & CONFIGURATION
☐ Azure Service Bus created and configured
☐ Azure Storage Account created and configured
☐ Connection strings updated in appsettings.json
☐ Application builds without errors
☐ Playwright configuration correct

APPLICATION STARTUP
☐ Worker starts without errors
☐ "Listening for requests" message appears
☐ No connection errors to Azure services
☐ Logs show normal startup sequence

BASIC FUNCTIONALITY
☐ Valid screenshot request processed successfully
☐ Image saved to blob storage
☐ Completion event published
☐ Image file is valid and viewable
☐ Processing time reasonable (2-8 seconds)

VALIDATION & ERROR HANDLING
☐ Invalid URLs rejected with clear error
☐ Missing required fields rejected
☐ Invalid viewport dimensions rejected
☐ Error messages are descriptive
☐ Errors don't crash the application

CONCURRENT REQUESTS
☐ Multiple requests processed in parallel
☐ No conflicts or race conditions
☐ All requests complete successfully
☐ Processing time scales appropriately

DIFFERENT VIEWPORTS
☐ Mobile viewport (375x667) works
☐ Tablet viewport (768x1024) works
☐ Desktop viewport (1920x1080) works
☐ 4K viewport (3840x2160) works
☐ Different sizes produce different images

BLOB STORAGE
☐ Images saved with unique names
☐ File sizes appropriate for content
☐ Files are valid PNG images
☐ Metadata shows correct dimensions
☐ Files persist after completion

MONITORING & HEALTH
☐ Logs show clear processing flow
☐ No memory leaks over time
☐ CPU usage reasonable
☐ Service Bus metrics show message activity
☐ Application stable under load

ERROR RECOVERY
☐ Service restarts successfully
☐ Queued messages processed after restart
☐ Network disconnections handled
☐ Invalid scenarios don't crash app

PERFORMANCE
☐ Simple sites: 1-2 seconds
☐ Medium complexity: 3-5 seconds
☐ Complex pages: 5-8 seconds
☐ No timeouts or failures
☐ Consistent performance across requests
```

---

## 🎓 Understanding Application Flow

### Request Processing Flow

```
1. REQUEST ARRIVES (Service Bus)
   ↓
   Message: HtmlScreenshotRequest
   Contains: RequestId, Url, ViewportWidth, ViewportHeight, TimeoutMs

2. VALIDATION
   ↓
   Check: RequestId present?
   Check: Url valid HTTP/HTTPS?
   Check: Viewport dimensions > 0?
   Check: Timeout reasonable?

   If invalid → Return failure event, Stop

3. SCREENSHOT CAPTURE
   ↓
   Launch Playwright browser
   Navigate to URL
   Wait for page load
   Set viewport size
   Capture screenshot

4. IMAGE UPLOAD
   ↓
   Upload PNG to Blob Storage
   Generate SAS URL (time-limited access)
   Record blob location

5. PUBLISH COMPLETION EVENT
   ↓
   Publish to "html-screenshot-completed" topic
   Include: RequestId, Status, BlobUrl, Duration, Timestamp

6. COMPLETION
   ↓
   ✅ Image stored in Blob Storage
   ✅ SAS URL available for download
   ✅ Completion event published for subscribers
```

---

## 📞 Troubleshooting Guide

### Issue: "Failed to connect to Service Bus"

**Causes:**
- Connection string incorrect
- Network connectivity issues
- Service Bus namespace doesn't exist
- Incorrect topic/subscription names

**Solutions:**
1. Verify connection string in appsettings.json
2. Test connectivity: `ping screenshot-sb-dev.servicebus.windows.net`
3. Check Azure Portal that namespace exists
4. Verify topic and subscription names match

---

### Issue: "Screenshot capture failed"

**Causes:**
- Website unreachable
- Website blocks automation
- Timeout too short
- Browser crash

**Solutions:**
1. Verify URL is accessible in regular browser
2. Try increasing TimeoutMs to 60000
3. Check Playwright logs for specific error
4. Verify network connectivity

---

### Issue: "Failed to upload blob"

**Causes:**
- Storage account connection string incorrect
- Container doesn't exist
- No permissions to write

**Solutions:**
1. Verify connection string in appsettings.json
2. Check that container "screenshots" exists
3. Verify storage account credentials
4. Check Storage Account access permissions

---

### Issue: Application uses high memory

**Causes:**
- Memory leak in screenshot provider
- Too many concurrent requests
- Browser context not released

**Solutions:**
1. Monitor memory over time
2. Reduce concurrent request rate
3. Restart application periodically
4. Check for any unhandled exceptions

---

## 🎯 Success Criteria - FINAL CHECKLIST

The application is **PRODUCTION-READY** when:

```
✅ Starts without errors
✅ Connects to Azure services
✅ Processes valid requests successfully
✅ Saves images to blob storage
✅ Publishes completion events
✅ Validates input properly
✅ Handles errors gracefully
✅ Processes multiple concurrent requests
✅ Performs consistently under load
✅ Recovers from temporary failures
✅ Generates high-quality screenshots
✅ Uses resources efficiently
✅ Logs all activities clearly
✅ No crashes or memory leaks
✅ All real-world scenarios work
```

---

## 📊 Testing Summary Report Template

Use this to document your testing:

```
TESTING SUMMARY REPORT
======================

Date: [Date]
Tester: [Your Name]
Application: ScreenToImageConverter Worker Service
Environment: [Development/Staging/Production]

TEST RESULTS:
- Setup & Configuration: ✅ PASS / ❌ FAIL
- Basic Functionality: ✅ PASS / ❌ FAIL
- Validation & Errors: ✅ PASS / ❌ FAIL
- Concurrent Processing: ✅ PASS / ❌ FAIL
- Different Viewports: ✅ PASS / ❌ FAIL
- Performance: ✅ PASS / ❌ FAIL
- Error Recovery: ✅ PASS / ❌ FAIL

OVERALL: ✅ PASS / ❌ FAIL

Issues Found:
1. [Issue 1]
2. [Issue 2]
3. [Issue 3]

Recommendations:
1. [Recommendation 1]
2. [Recommendation 2]

Sign-Off: _______________  Date: __________
```

---

## 🚀 Next Steps

After successful testing:

1. **Document results** - Save testing report
2. **Commit changes** - Git commit configuration changes
3. **Deploy to staging** - Test in staging environment
4. **Deploy to production** - After staging validation
5. **Monitor in production** - Set up alerts and monitoring
6. **Gather feedback** - From real users

---

## 📝 Quick Reference - Test Commands

```powershell
# Start the application
cd src\ScreenToImageConverter.Worker
dotnet run

# Build only
dotnet build

# View logs
Get-Content -Path [logfile] -Tail 50

# Check running processes
Get-Process -Name dotnet

# Test connectivity to Service Bus
Test-NetConnection -ComputerName screenshot-sb-dev.servicebus.windows.net -Port 443
```

---

**This completes your real-world operational testing plan!**  
**Use this guide to verify your application works in actual production scenarios.**

