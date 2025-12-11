# Variables
# Update path to match new structure
COMPOSE_FILE := ./ops/local-dev/docker-compose.yml
CERTS_DIR := ./ops/local-dev/certs
CERT_NAME := local-cert.pem
KEY_NAME := local-key.pem
# Added the wildcard for subdomains to cover everything
DOMAINS := "propertea.localhost" "*.propertea.localhost" "localhost" 127.0.0.1 ::1

HOSTS_ENTRIES := propertea.localhost auth.propertea.localhost organization.propertea.localhost secrets.propertea.localhost mail.propertea.localhost flags.propertea.localhost fga.propertea.localhost grafana.propertea.localhost logs.propertea.localhost

# --- Certificates & Host Management ---

certs:
	@mkdir -p $(CERTS_DIR)
	@echo "🔐 Generating SSL certificates..."
	@mkcert -key-file $(CERTS_DIR)/$(KEY_NAME) -cert-file $(CERTS_DIR)/$(CERT_NAME) $(DOMAINS)
	@echo "✅ Certificates generated in $(CERTS_DIR)"

hosts:
	@echo "📝 Updating /etc/hosts..."
	@for domain in $(HOSTS_ENTRIES); \
	do \
		if ! grep -q "$$domain" /etc/hosts; then \
			echo "Adding $$domain to /etc/hosts"; \
			echo "127.0.0.1 $$domain" | sudo tee -a /etc/hosts > /dev/null; \
		else \
			echo "$$domain already exists in /etc/hosts"; \
		fi \
	done
	@echo "✅ Hosts updated"

# --- Docker Operations ---

up:
	@echo "🚀 Starting ProperTea Local Stack..."
	@docker compose -f $(COMPOSE_FILE) up -d
	@echo "✅ Stack is running!"
	@echo "   ---------------------------------------"
	@echo "   🌍 BFF:       https://propertea.localhost"
	@echo "   🛡️  Auth:      https://auth.propertea.localhost"
	@echo "   🔑 Secrets:   https://secrets.propertea.localhost"
	@echo "   🚩 Flags:     https://flags.propertea.localhost"
	@echo "   📧 Mail:      https://mail.propertea.localhost"
	@echo "   🚫 AuthZ:     https://fga.propertea.localhost"
	@echo "   📊 Grafana:   https://grafana.propertea.localhost"
	@echo "   🪵 Logs:      https://logs.propertea.localhost"
	@echo "   ---------------------------------------"

down:
	@docker compose -f $(COMPOSE_FILE) down

restart: down up

logs:
	@docker compose -f $(COMPOSE_FILE) logs -f

# --- Utilities ---

# Run the Idempotent DB Script (Use this when you add new tools/databases!)
init-db:
	@echo "⚙️  Running Idempotent Infra DB Init Script..."
	@docker exec propertea-infra-postgres /bin/bash /docker-entrypoint-initdb.d/01-init-infra.sh

# Nuke everything (Volumes included) - CAREFUL!
clean: down
	@echo "🧹 Cleaning up containers and volumes..."
	@docker compose -f $(COMPOSE_FILE) down -v
	@rm -rf $(CERTS_DIR)
	@echo "✅ Cleaned up certificates and volumes."
