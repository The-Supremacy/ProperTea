# Variables
CERTS_DIR := ./ops/local-dev/certs
CERT_NAME := local-cert.pem
KEY_NAME := local-key.pem

# Added pghero to the host list
HOSTS_ENTRIES := propertea.localhost auth.propertea.localhost organization.propertea.localhost secrets.propertea.localhost mail.propertea.localhost flags.propertea.localhost fga.propertea.localhost grafana.propertea.localhost logs.propertea.localhost pghero.propertea.localhost cadvisor.propertea.localhost

# Added wildcard for subdomains
DOMAINS := "propertea.localhost" "*.propertea.localhost" "localhost" 127.0.0.1 ::1

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

up:
	@echo "🚀 Starting ProperTea Local Stack..."
	@echo "   Docker will auto-load files defined in COMPOSE_FILE from .env"
	@docker compose --project-directory ./ops/local-dev up -d
	@echo "✅ Stack is running!"
	@echo "   ---------------------------------------"
	@echo "   🛡️ Auth:      https://auth.propertea.localhost"
	@echo "   🔑 Secrets:   https://secrets.propertea.localhost"
	@echo "   🚩 Flags:     https://flags.propertea.localhost"
	@echo "   📧 Mail:      https://mail.propertea.localhost"
	@echo "   🚫 AuthZ:     https://fga.propertea.localhost"
	@echo "   📊 Grafana:   https://grafana.propertea.localhost"
	@echo "   🪵 Logs:      https://logs.propertea.localhost"
	@echo "   📊 cAdvisor:  https://cadvisor.propertea.localhost"
	@echo "   🐘 PgHero:    https://pghero.propertea.localhost"
	@echo "   🌍 Services:  "
	@echo "   Organization: https://organization.propertea.localhost"
	@echo "   ---------------------------------------"

down:
	@echo "🛑 Stopping ProperTea Local Stack..."
	@docker compose --project-directory ./ops/local-dev down

restart: down up

logs:
	@docker compose --project-directory ./ops/local-dev logs -f

init-db:
	@echo "⚙️  Running Idempotent Infra DB Init Script..."
	@docker exec propertea-infra-postgres /bin/bash /docker-entrypoint-initdb.d/init-infra.sh

clean: down
	@echo "🧹 Cleaning up containers and volumes..."
	@docker compose --project-directory ./ops/local-dev down -
