# ProperTea Development Makefile
.DEFAULT_GOAL := help

# Variables
## Docker Compose files
COMPOSE_GATEWAY := services/gateway/docker-compose.gateway.yml
COMPOSE_SERVICES := services/docker-compose.services.yml
COMPOSE_INFRASTRUCTURE := docker-compose.infrastructure.yml
COMPOSE_OBSERVABILITY := docker-compose.observability.yml
COMPOSE := docker-compose.yml

## Paths
SERVICES_PATH := ./services
FRONTEND_PATH := ./frontend/landlord-portal

# Docker resources
NETWORK_NAME := propertea-network
VOLUMES := propertea-postgres-data propertea-redis-data propertea-kafka-data propertea-zookeeper-data propertea-prometheus-data propertea-grafana-data propertea-jaeger-data propertea-loki-data propertea-opensearch-data

# Colors for output
GREEN := \033[0;32m
YELLOW := \033[0;33m
RED := \033[0;31m
NC := \033[0m # No Color

.PHONY: help bootstrap clean build test up down restart logs health-check

help: ## Show this help message
	@echo "ProperTea Development Commands:"
	@echo ""
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "  $(YELLOW)%-20s$(NC) %s\n", $$1, $$2}'

up: ## Start all services
	@echo "$(GREEN)🚀 Starting all services...$(NC)"
	docker-compose -f $(COMPOSE) up -d
	@echo "$(GREEN)✅ All services started$(NC)"

down: ## Stop all services
	@echo "$(YELLOW)⏹️ Stopping all services...$(NC)"
	docker-compose -f $(COMPOSE) down 
	@echo "$(GREEN)✅ All services stopped$(NC)"

restart: down up ## Restart all services

logs: ## View logs from all services
	docker-compose -f $(COMPOSE) logs

bootstrap: ## Complete environment setup (first time)
	@echo "$(GREEN)🚀 Bootstrapping ProperTea development environment...$(NC)"
	@echo "$(YELLOW)Setting up Docker resources...$(NC)"
	@$(MAKE) setup-docker
	@echo "$(YELLOW)Installing prerequisites...$(NC)"
	# We'll add prerequisite checks here
	@echo "$(YELLOW)Setting up certificates...$(NC)"
	@$(MAKE) certs
	@echo "$(YELLOW)Setting up DNS...$(NC)"
	@$(MAKE) setup-dns
	@echo "$(YELLOW)Starting infrastructure...$(NC)"
	@$(MAKE) infra-up
	@echo "$(GREEN)✅ Bootstrap complete! Run 'make up' to start all services$(NC)"
	@echo "$(YELLOW)🌐 Access points:$(NC)"
	@echo "  • Traefik Dashboard: https://traefik.local.test:8080"
	@echo "  • Grafana: https://grafana.local.test"
	@echo "  • Prometheus: https://prometheus.local.test"
	@echo "  • Jaeger: https://jaeger.local.test"
	@echo "  • Kafka UI: https://kafka.local.test"
	@echo "  • OpenSearch: https://search.local.test"

infra-up: setup-docker ## Start infrastructure services only
	@echo "$(YELLOW)🏗️ Starting infrastructure services...$(NC)"
	docker-compose -f $(COMPOSE_INFRASTRUCTURE) up -d
	@echo "$(GREEN)✅ Infrastructure services started$(NC)"

obs-up: setup-docker ## Start infrastructure services only
	@echo "$(YELLOW)🏗️ Starting infrastructure services...$(NC)"
	docker-compose -f $(COMPOSE_OBSERVABILITY) up -d
	@echo "$(GREEN)✅ Infrastructure services started$(NC)"

setup-docker: ## Create Docker network and volumes
	@echo "$(YELLOW)🐳 Setting up Docker resources...$(NC)"
	@echo "$(YELLOW)Creating network $(NETWORK_NAME)...$(NC)"
	@docker network inspect $(NETWORK_NAME) >/dev/null 2>&1 || \
		(docker network create $(NETWORK_NAME) && echo "$(GREEN)✅ Network created$(NC)") || \
		echo "$(GREEN)ℹ️ Network already exists$(NC)"
	@echo "$(YELLOW)Creating volumes...$(NC)"
	@for volume in $(VOLUMES); do \
		docker volume inspect $$volume >/dev/null 2>&1 || \
		(docker volume create $$volume && echo "$(GREEN)✅ Volume $$volume created$(NC)") || \
  		echo "$(GREEN)ℹ️ Volume $$volume already exists$(NC)"; \
	done

certs: ## Generate local TLS certificates
	@echo "$(YELLOW)📜 Generating local certificates...$(NC)"
	@mkdir -p tools/certs
	@if ! command -v mkcert >/dev/null 2>&1; then \
		echo "$(RED)❌ mkcert not found. Please install it first.$(NC)"; \
		echo "$(YELLOW)Ubuntu/Debian: curl -JLO 'https://dl.filippo.io/mkcert/latest?for=linux/amd64' && chmod +x mkcert-v*-linux-amd64 && sudo mv mkcert-v*-linux-amd64 /usr/local/bin/mkcert$(NC)"; \
		exit 1; \
	fi
	@echo "$(YELLOW)Installing CA certificate...$(NC)"
	mkcert -install
	@echo "$(YELLOW)Generating certificates for local domains...$(NC)"
	cd tools/certs && mkcert \
		--cert-file "_wildcard.local.test.pem" \
		--key-file "_wildcard.local.test-key.pem" \
		"*.local.test" \
		"localhost" \
		"127.0.0.1" \
		"::1" \
		"api.local.test" \
		"landlord.local.test" \
		"grafana.local.test" \
		"jaeger.local.test" \
		"prometheus.local.test" \
		"search.local.test" \
		"kafka.local.test" \
		"traefik.local.test"
	@echo "$(GREEN)✅ Certificates generated in tools/certs/$(NC)"
	@echo "$(YELLOW)Creating Traefik TLS configuration...$(NC)"
	@echo 'tls:\n  certificates:\n    - certFile: /etc/traefik/certs/_wildcard.local.test.pem\n      keyFile: /etc/traefik/certs/_wildcard.local.test-key.pem\n      stores:\n        - default\n  stores:\n    default:\n      defaultCertificate:\n        certFile: /etc/traefik/certs/_wildcard.local.test.pem\n        keyFile: /etc/traefik/certs/_wildcard.local.test-key.pem' > tools/traefik/tls.yml
	@echo "$(YELLOW)Note: You may need to add these domains to /etc/hosts:$(NC)"
	@echo "127.0.0.1 api.local.test landlord.local.test grafana.local.test jaeger.local.test prometheus.local.test search.local.test kafka.local.test traefik.local.test"

setup-dns: ## Add local DNS entries to /etc/hosts
	@echo "$(YELLOW)🌐 Setting up local DNS entries...$(NC)"
	@echo "# ProperTea local development domains" | sudo tee -a /etc/hosts
	@echo "127.0.0.1 api.local.test landlord.local.test grafana.local.test jaeger.local.test prometheus.local.test search.local.test kafka.local.test traefik.local.test" | sudo tee -a /etc/hosts
	@echo "$(GREEN)✅ DNS entries added to /etc/hosts$(NC)"

clean: ## Clean all containers, volumes, and generated files
	@echo "$(RED)🧹 Cleaning up...$(NC)"
	@$(MAKE) down
	docker system prune -f
	@echo "$(GREEN)✅ Cleanup complete$(NC)"

clean-volumes: ## Remove all project volumes (⚠️ DATA LOSS)
	@echo "$(RED)⚠️ This will delete all data! Press Ctrl+C to cancel...$(NC)"
	@sleep 5
	@echo "$(YELLOW)Removing volumes...$(NC)"
	@for volume in $(VOLUMES); do \
		docker volume rm $$volume 2>/dev/null && echo "$(GREEN)✅ Volume $$volume removed$(NC)" || \
		echo "$(YELLOW)ℹ️ Volume $$volume not found or already removed$(NC)"; \
		done
	@echo "$(YELLOW)Removing network...$(NC)"
	@docker network rm $(NETWORK_NAME) 2>/dev/null && echo "$(GREEN)✅ Network removed$(NC)" || \
		echo "$(YELLOW)ℹ️ Network not found or already removed$(NC)"