# Terraform Module: AKS

Provisions an Azure Kubernetes Service cluster for ProperTea environments (UAT, Prod).

## Resources

- AKS cluster (System + User node pools)
- Azure Container Registry (ACR) with AKS pull role assignment
- Log Analytics workspace + Container Insights
- Managed identity for kubelet (workload identity ready)

## Usage

```hcl
module "aks" {
  source = "../../terraform/modules/aks"

  resource_group_name = var.resource_group_name
  location            = var.location
  cluster_name        = var.cluster_name
  kubernetes_version  = var.kubernetes_version
  node_count          = var.node_count
  vm_size             = var.vm_size
  tags                = var.tags
}
```

## Inputs

| Name | Description | Type | Required |
|------|-------------|------|----------|
| `resource_group_name` | Azure Resource Group to deploy into | `string` | yes |
| `location` | Azure region | `string` | yes |
| `cluster_name` | AKS cluster name | `string` | yes |
| `kubernetes_version` | Kubernetes version | `string` | yes |
| `node_count` | Initial node count per pool | `number` | yes |
| `vm_size` | VM SKU for nodes | `string` | yes |
| `tags` | Azure resource tags | `map(string)` | no |

## Outputs

| Name | Description |
|------|-------------|
| `cluster_id` | AKS cluster resource ID |
| `kube_config` | Raw kubeconfig (sensitive) |
| `kubelet_identity_id` | Kubelet managed identity object ID |
