# Terraform Module: Azure Platform

Provisions shared Azure platform resources for ProperTea environments.

## Resources

- Azure Database for PostgreSQL Flexible Server
- Azure Cache for Redis
- Azure Key Vault (with RBAC authorization)
- Private Endpoints + DNS zones for all services
- Virtual Network + subnets

## Usage

```hcl
module "azure_platform" {
  source = "../../terraform/modules/azure-platform"

  resource_group_name  = var.resource_group_name
  location             = var.location
  vnet_address_space   = var.vnet_address_space
  postgres_sku_name    = var.postgres_sku_name
  postgres_storage_mb  = var.postgres_storage_mb
  redis_sku_name       = var.redis_sku_name
  redis_capacity       = var.redis_capacity
  key_vault_sku        = var.key_vault_sku
  tags                 = var.tags
}
```

## Inputs

| Name | Description | Type | Required |
|------|-------------|------|----------|
| `resource_group_name` | Azure Resource Group | `string` | yes |
| `location` | Azure region | `string` | yes |
| `vnet_address_space` | VNet CIDR block | `string` | yes |
| `postgres_sku_name` | Postgres Flexible Server SKU | `string` | yes |
| `postgres_storage_mb` | Storage in MB | `number` | yes |
| `redis_sku_name` | Redis Cache SKU (Basic/Standard/Premium) | `string` | yes |
| `redis_capacity` | Redis Cache capacity | `number` | yes |
| `key_vault_sku` | Key Vault SKU (standard/premium) | `string` | yes |
| `tags` | Azure resource tags | `map(string)` | no |

## Outputs

| Name | Description |
|------|-------------|
| `postgres_fqdn` | PostgreSQL server FQDN |
| `redis_host` | Redis Cache hostname |
| `key_vault_uri` | Key Vault URI |
| `vnet_id` | Virtual Network resource ID |
