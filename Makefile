# ProperTea Development Makefile
.DEFAULT_GOAL := help

ENV := dev

# Variables
## Docker Compose files
COMPOSE_FILES := 	-f docker-compose.shared.yml \
					-f docker-compose.infrastructure.yml \
					-f docker-compose.observability.yml
## Paths
SERVICES_PATH := ./services
FRONTEND_PATH := ./frontend

## TLS domains
TLS_DOMAINS := 	traefik.propertea.dev \
				jaeger.propertea.dev \
				grafana.propertea.dev \
				gateway.propertea.dev

# Docker resources
NETWORK_NAME := propertea-network

.PHONY: help bootstrap clean build test up down restart logs health-check

help: ## Show this help message
	@echo "ProperTea Development Commands:"
	@echo ""
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "  $(YELLOW)%-20s$(NC) %s\n", $$1, $$2}'

up: ## Start all services
	@echo "$(GREEN) Starting all services...$(NC)"
	docker-compose $(COMPOSE_FILES) up -d
	@echo "$(GREEN)✅ All services started$(NC)"

down: ## Stop all services
	@echo "$(YELLOW) Stopping all services...$(NC)"
	docker-compose $(COMPOSE_FILES) down 
	@echo "$(GREEN) All services stopped$(NC)"

restart: down up ## Restart all services

logs: ## View logs from all services
	docker-compose $(COMPOSE_FILES) logs

clean: ## Clean all containers, volumes, and generated files
	@echo "$(RED) Cleaning up...$(NC)"
	@$(MAKE) down
	docker system prune -f
	@echo "$(GREEN) Cleanup complete$(NC)"

compose-certs: ## Generate local TLS certificates for docker-compose
	@echo "$(YELLOW) Generating local certificates...$(NC)"
	@mkdir -p .docker-compose/certs
	@if ! command -v mkcert >/dev/null 2>&1; then \
		echo "$(RED)❌ mkcert not found. Please install it first.$(NC)"; \
		echo "$(YELLOW)Ubuntu/Debian: curl -JLO 'https://dl.filippo.io/mkcert/latest?for=linux/amd64' && chmod +x mkcert-v*-linux-amd64 && sudo mv mkcert-v*-linux-amd64 /usr/local/bin/mkcert$(NC)"; \
		exit 1; \
	fi
	@echo "$(YELLOW)Installing CA certificate...$(NC)"
	mkcert -install
	@echo "$(YELLOW)Generating certificates for local domains...$(NC)"
	cd docker-compose/certs && mkcert \
		--cert-file "_wildcard.dev.pem" \
		--key-file "_wildcard.dev-key.pem" \
		*.dev \
		localhost \
		127.0.0.1 \
		::1 \
		127.0.0.1 \
		$(TLS_DOMAINS)
	@echo "$(GREEN)Certificates generated in docker-compose/certs$(NC)"

compose-dns: ## Add local DNS entries to /etc/hosts
	@echo "$(YELLOW) Setting up local DNS entries...$(NC)"
	@echo "# ProperTea local development domains" | sudo tee -a /etc/hosts
	@echo 127.0.0.1 $(TLS_DOMAINS) | sudo tee -a /etc/hosts
	@echo "$(GREEN) DNS entries added to /etc/hosts$(NC)"