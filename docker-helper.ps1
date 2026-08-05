# Docker helper script for ScreenToImageConverter (PowerShell)
# Usage: .\docker-helper.ps1 [command]

param(
	[string]$Command = "help",
	[string]$Service = ""
)

$COMPOSE_FILE = "docker-compose.yml"
$IMAGE_NAME = "screentoimageconverter"
$IMAGE_TAG = "latest"

# Helper functions
function Print-Info {
	param([string]$Message)
	Write-Host "ℹ️  $Message" -ForegroundColor Cyan
}

function Print-Success {
	param([string]$Message)
	Write-Host "✅ $Message" -ForegroundColor Green
}

function Print-Warning {
	param([string]$Message)
	Write-Host "⚠️  $Message" -ForegroundColor Yellow
}

function Print-Error {
	param([string]$Message)
	Write-Host "❌ $Message" -ForegroundColor Red
}

# Commands
function Build-Image {
	Print-Info "Building Docker image: $IMAGE_NAME`:$IMAGE_TAG"
	docker build -t "$IMAGE_NAME`:$IMAGE_TAG" .
	Print-Success "Image built successfully!"
}

function Start-Services {
	Print-Info "Starting services with Docker Compose..."
	docker-compose -f $COMPOSE_FILE up -d
	Print-Success "Services started!"

	Print-Info "Waiting for services to be healthy (30 seconds)..."
	Start-Sleep -Seconds 30

	Show-Status
}

function Stop-Services {
	Print-Info "Stopping services..."
	docker-compose -f $COMPOSE_FILE stop
	Print-Success "Services stopped!"
}

function Restart-Services {
	Print-Info "Restarting services..."
	docker-compose -f $COMPOSE_FILE restart
	Print-Success "Services restarted!"
}

function Down-Services {
	Print-Warning "Removing all containers and networks..."
	docker-compose -f $COMPOSE_FILE down
	Print-Success "Services removed!"
}

function Down-WithVolumes {
	Print-Warning "Removing all containers, networks, and volumes (DATA WILL BE DELETED)..."
	docker-compose -f $COMPOSE_FILE down -v
	Print-Success "Everything cleaned up!"
}

function Show-Status {
	Print-Info "Service Status:"
	docker-compose -f $COMPOSE_FILE ps
}

function Show-Logs {
	param([string]$ServiceName)
	if ([string]::IsNullOrEmpty($ServiceName)) {
		Print-Info "Showing logs from all services (Ctrl+C to exit)..."
		docker-compose -f $COMPOSE_FILE logs -f
	}
	else {
		Print-Info "Showing logs from $ServiceName (Ctrl+C to exit)..."
		docker-compose -f $COMPOSE_FILE logs -f $ServiceName
	}
}

function Health-Check {
	Print-Info "Performing health checks..."

	Print-Info "Checking RabbitMQ..."
	try {
		$response = curl.exe -s -u "guest:guest" "http://localhost:15672/api/aliveness-test" -ErrorAction SilentlyContinue
		if ($response -like "*ok*") {
			Print-Success "RabbitMQ is healthy"
		}
		else {
			Print-Error "RabbitMQ is not responding"
		}
	}
	catch {
		Print-Error "RabbitMQ is not responding"
	}

	Print-Info "Checking Azurite..."
	try {
		$response = curl.exe -s "http://localhost:10000/devstoreaccount1?comp=list" -ErrorAction SilentlyContinue
		Print-Success "Azurite is healthy"
	}
	catch {
		Print-Error "Azurite is not responding"
	}

	Print-Info "Checking Worker Service..."
	try {
		$response = curl.exe -s "http://localhost:8080/health" -ErrorAction SilentlyContinue
		if ($response -like "*ealthy*") {
			Print-Success "Worker service is healthy"
		}
		else {
			Print-Error "Worker service is not responding"
		}
	}
	catch {
		Print-Error "Worker service is not responding"
	}
}

function Open-UI {
	Print-Info "Opening RabbitMQ Management UI..."
	Start-Process "http://localhost:15672"
	Print-Info "Username: guest, Password: guest"
}

function Send-TestMessage {
	Print-Info "Sending test message to RabbitMQ..."

	$pythonScript = @"
import pika
import json
import sys

try:
	connection = pika.BlockingConnection(pika.ConnectionParameters('localhost'))
	channel = connection.channel()

	channel.exchange_declare(exchange='screenshot-requests', exchange_type='topic', durable=True)
	channel.queue_declare(queue='screenshot-requests-queue', durable=True)
	channel.queue_bind(exchange='screenshot-requests', queue='screenshot-requests-queue', routing_key='screenshot.request')

	test_message = {
		"requestId": "docker-test-001",
		"url": "https://www.example.com",
		"viewportWidth": 1920,
		"viewportHeight": 1080,
		"timeoutMs": 30000,
		"sourceId": "docker-test"
	}

	channel.basic_publish(
		exchange='screenshot-requests',
		routing_key='screenshot.request',
		body=json.dumps(test_message)
	)

	print("✅ Test message published!")
	connection.close()
except Exception as e:
	print(f"❌ Error: {e}", file=sys.stderr)
	sys.exit(1)
"@

	$pythonScript | python.exe
}

function View-RabbitMQQueues {
	Print-Info "RabbitMQ Queues:"
	try {
		$response = curl.exe -s -u "guest:guest" "http://localhost:15672/api/queues"
		$response | python.exe -m json.tool
	}
	catch {
		Print-Warning "Could not fetch queues. Is RabbitMQ running?"
	}
}

function Show-Help {
	Write-Host @"
ScreenToImageConverter Docker Helper

Usage:
	.\docker-helper.ps1 [command]

Commands:
	build               Build Docker image
	start               Start all services (build + compose up)
	stop                Stop all services
	restart             Restart all services
	down                Stop and remove all containers
	clean               Remove everything including volumes (WARNING: deletes data!)

	status              Show container status
	logs [service]      View logs (optionally for specific service)
	health              Perform health checks on all services

	ui                  Open RabbitMQ management UI
	test                Send test message to RabbitMQ
	queues              View RabbitMQ queues

	help                Show this help message

Examples:
	.\docker-helper.ps1 build
	.\docker-helper.ps1 start
	.\docker-helper.ps1 logs worker
	.\docker-helper.ps1 health
	.\docker-helper.ps1 test
	.\docker-helper.ps1 down

Service Ports:
	RabbitMQ:           localhost:5672 (AMQP) or :15672 (UI)
	Azurite:            localhost:10000 (Blob Storage)
	Worker Service:     localhost:8080 (Health check)

RabbitMQ Credentials:
	Username: guest
	Password: guest

For more information:
	See DOCKER_SETUP_GUIDE.md
"@ -ForegroundColor Cyan
}

# Main script logic
switch ($Command) {
	"build" {
		Build-Image
	}
	"start" {
		Build-Image
		Start-Services
	}
	"stop" {
		Stop-Services
	}
	"restart" {
		Restart-Services
	}
	"down" {
		Down-Services
	}
	"clean" {
		Down-WithVolumes
	}
	"status" {
		Show-Status
	}
	"logs" {
		Show-Logs -ServiceName $Service
	}
	"health" {
		Health-Check
	}
	"ui" {
		Open-UI
	}
	"test" {
		Send-TestMessage
	}
	"queues" {
		View-RabbitMQQueues
	}
	"help" {
		Show-Help
	}
	default {
		Print-Error "Unknown command: $Command"
		Show-Help
		exit 1
	}
}
