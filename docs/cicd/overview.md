# CI/CD Overview
# CI/CD Overview

## Strategy

ProperTea uses GitHub Actions for continuous integration and deployment with a focus on simplicity, reliability, and cost-effectiveness. The pipeline supports both local development and production deployment to Azure.

## Pipeline Architecture

### Branch Strategy
- **main**: Production-ready code, automatically deployed to staging
- **develop**: Integration branch for feature development
- **feature/***: Feature branches with PR-based integration
- **hotfix/***: Emergency fixes with fast-track deployment

### Deployment Environments
1. **Local**: Developer workstations with Docker Compose
2. **PR Review**: Temporary environments for pull request validation
3. **Staging**: Pre-production environment mirroring production
4. **Production**: Live environment with full observability stack

## Build Pipeline

### Reusable Workflows

#### .NET Service Build
## Goals
- Simple, reliable pipelines with reusable steps
- Fast feedback with caching and parallelization
- Secure handling of secrets

## Platform
- GitHub Actions with composite, reusable workflows
- Build Docker images for services
- Cache layers using GitHub Container Registry (GHCR)

## Workflows (high level)
- PR Validation:
  - Lint, build, unit tests
  - Build Docker images (no push)
  - Publish artifacts (test reports)
- Main Branch:
  - Build and push images to GHCR
  - Run integration tests with Testcontainers (matrix)
  - Generate SBOM and run SAST (CodeQL)
- Release:
  - Tag images with semver
  - Generate changelog and release notes
  - Publish documentation artifacts

## Conventions
- Tags:
  - PR builds: pr-<num>
  - Main: sha and semver
- Environments:
  - local → dev → staging → prod
- Secrets:
  - Local: .env and user-secrets
  - Cloud: Azure Key Vault with USE_KEYVAULT=true

## Future
- Multi-arch builds with buildx
- Canary deployments
- Helm charts for AKS
