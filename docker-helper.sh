#!/bin/bash
# Docker helper script for ScreenToImageConverter
# Usage: ./docker-helper.sh [command]

set -e

COMPOSE_FILE="docker-compose.yml"
IMAGE_NAME="screentoimageconverter"
IMAGE_TAG="latest"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Helper functions
print_info() {
	echo -e "${BLUE}ℹ️  $1${NC}"
}

print_success() {
	echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
	echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
	echo -e "${RED}❌ $1${NC}"
}

# Commands
build_image() {
	print_info "Building Docker image: $IMAGE_NAME:$IMAGE_TAG"
	docker build -t "$IMAGE_NAME:$IMAGE_TAG" .
	print_success "Image built successfully!"
}

start_services() {
	print_info "Starting services with Docker Compose..."
	docker-compose -f "$COMPOSE_FILE" up -d
	print_success "Services started!"

	print_info "Waiting for services to be healthy (30 seconds)..."
	sleep 30

	show_status
}

stop_services() {
	print_info "Stopping services..."
	docker-compose -f "$COMPOSE_FILE" stop
	print_success "Services stopped!"
}

restart_services() {
	print_info "Restarting services..."
	docker-compose -f "$COMPOSE_FILE" restart
	print_success "Services restarted!"
}

down_services() {
	print_warning "Removing all containers and networks..."
	docker-compose -f "$COMPOSE_FILE" down
	print_success "Services removed!"
}

down_with_volumes() {
	print_warning "Removing all containers, networks, and volumes (DATA WILL BE DELETED)..."
	docker-compose -f "$COMPOSE_FILE" down -v
	print_success "Everything cleaned up!"
}

show_status() {
	print_info "Service Status:"
	docker-compose -f "$COMPOSE_FILE" ps
}

show_logs() {
	local service=$1
	if [ -z "$service" ]; then
		print_info "Showing logs from all services (Ctrl+C to exit)..."
		docker-compose -f "$COMPOSE_FILE" logs -f
	else
		print_info "Showing logs from $service (Ctrl+C to exit)..."
		docker-compose -f "$COMPOSE_FILE" logs -f "$service"
	fi
}

health_check() {
	print_info "Performing health checks..."

	print_info "Checking RabbitMQ..."
	if curl -s -u guest:guest http://localhost:15672/api/aliveness-test | grep -q "ok"; then
		print_success "RabbitMQ is healthy"
	else
		print_error "RabbitMQ is not responding"
	fi

	print_info "Checking Azurite..."
	if curl -s http://localhost:10000/devstoreaccount1?comp=list > /dev/null; then
		print_success "Azurite is healthy"
	else
		print_error "Azurite is not responding"
	fi

	print_info "Checking Worker Service..."
	if curl -s http://localhost:8080/health | grep -i healthy; then
		print_success "Worker service is healthy"
	else
		print_error "Worker service is not responding"
	fi
}

open_ui() {
	print_info "Opening RabbitMQ Management UI..."
	if command -v xdg-open > /dev/null; then
		xdg-open http://localhost:15672
	elif command -v open > /dev/null; then
		open http://localhost:15672
	else
		print_warning "Please open http://localhost:15672 in your browser"
		print_info "Username: guest, Password: guest"
	fi
}

send_test_message() {
	print_info "Sending test message to RabbitMQ..."

	if ! command -v python3 &> /dev/null; then
		print_error "Python3 is required for this command"
		print_info "Install it and retry, or use the manual steps in DOCKER_SETUP_GUIDE.md"
		return 1
	fi

	python3 << 'EOF'
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
EOF
}

view_rabbitmq_queues() {
	print_info "RabbitMQ Queues:"
	curl -s -u guest:guest http://localhost:15672/api/queues | python3 -m json.tool 2>/dev/null || \
	print_warning "Could not fetch queues. Is RabbitMQ running?"
}

show_help() {
	cat << EOF
${BLUE}ScreenToImageConverter Docker Helper${NC}

${GREEN}Usage:${NC}
	./docker-helper.sh [command]

${GREEN}Commands:${NC}
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

${GREEN}Examples:${NC}
	./docker-helper.sh build
	./docker-helper.sh start
	./docker-helper.sh logs worker
	./docker-helper.sh health
	./docker-helper.sh test
	./docker-helper.sh down

${BLUE}Service Ports:${NC}
	RabbitMQ:           localhost:5672 (AMQP) or :15672 (UI)
	Azurite:            localhost:10000 (Blob Storage)
	Worker Service:     localhost:8080 (Health check)

${BLUE}RabbitMQ Credentials:${NC}
	Username: guest
	Password: guest

${BLUE}For more information:${NC}
	See DOCKER_SETUP_GUIDE.md
EOF
}

# Main script logic
case "${1:-help}" in
	build)
		build_image
		;;
	start)
		build_image
		start_services
		;;
	stop)
		stop_services
		;;
	restart)
		restart_services
		;;
	down)
		down_services
		;;
	clean)
		down_with_volumes
		;;
	status)
		show_status
		;;
	logs)
		show_logs "$2"
		;;
	health)
		health_check
		;;
	ui)
		open_ui
		;;
	test)
		send_test_message
		;;
	queues)
		view_rabbitmq_queues
		;;
	help)
		show_help
		;;
	*)
		print_error "Unknown command: $1"
		show_help
		exit 1
		;;
esac
