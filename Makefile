# Variables
CERTS_DIR := ./infra/local/certs
CERT_NAME := local-cert.pem
KEY_NAME := local-key.pem
DOMAINS := "propertea.localhost" "*.propertea.localhost" "localhost" 127.0.0.1 ::1

HOSTS_ENTRIES := propertea.localhost auth.propertea.localhost organization.propertea.localhost

COMPOSE_PATH := ./infra/local/compose

certs:
	@mkdir -p $(CERTS_DIR)
	@echo "🔐 Generating SSL certificates..."
	@mkcert -key-file $(CERTS_DIR)/$(KEY_NAME) -cert-file $(CERTS_DIR)/$(CERT_NAME) $(DOMAINS)
	@echo "✅ Certificates generated in $(CERTS_DIR)"

hosts:
	@echo "📝 Updating /etc/hosts..."
	@for domain in $(HOSTS_ENTRIES); do \
		if ! grep -q "$$domain" /etc/hosts; then \
			echo "Adding $$domain to /etc/hosts"; \
			echo "127.0.0.1 $$domain" | sudo tee -a /etc/hosts > /dev/null; \
		else \
			echo "$$domain already exists in /etc/hosts"; \
		fi \
	done
	@echo "✅ Hosts updated"

up:
	@infisical run --env=dev docker compose --project-directory $(COMPOSE_PATH) up -d
	@echo "🚀 ProperTea is running!"
	@echo "   BFF:   https://propertea.localhost"
	@echo "   Auth:  https://auth.propertea.localhost"

down:
	@docker compose --project-directory $(COMPOSE_PATH)  down

restart: down up

logs:
	@docker compose --project-directory $(COMPOSE_PATH)  logs -f

clean: down
	@rm -rf $(CERTS_DIR)
	@echo "🧹 Cleaned up certificates"
