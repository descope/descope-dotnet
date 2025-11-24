.PHONY: build generate generate-mgmt generate-auth clean help

# Default target
.DEFAULT_GOAL := build

# Management API OpenAPI spec file location
MGMT_OPENAPI_SPEC := $(HOME)/dev/go/src/github.com/descope/managementservice/pkg/managementservice/proto/v1/doc/management.openapi.yaml

# Management API Kiota generation parameters
MGMT_KIOTA_LANG := CSharp
MGMT_KIOTA_CLASS := DescopeMgmtKiotaClient
MGMT_KIOTA_NAMESPACE := Descope.Mgmt
MGMT_KIOTA_OUTPUT := ./Generated/Mgmt

help: ## Show this help message
	@echo 'Usage: make [target]'
	@echo ''
	@echo 'Available targets:'
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  %-15s %s\n", $$1, $$2}'

build: generate dotnet-build ## Regenerate Kiota files and rebuild C# DLLs (default)

generate: generate-mgmt generate-auth ## Regenerate all Kiota client files

generate-mgmt: ## Regenerate Management API Kiota client files from OpenAPI spec
	@echo "Checking for Management API OpenAPI spec file..."
	@if [ ! -f "$(MGMT_OPENAPI_SPEC)" ]; then \
		echo "ERROR: Management API OpenAPI spec file not found at: $(MGMT_OPENAPI_SPEC)"; \
		exit 1; \
	fi
	@echo "Management API OpenAPI spec found: $(MGMT_OPENAPI_SPEC)"
	@echo "Generating Management API Kiota client files..."
	kiota generate -l $(MGMT_KIOTA_LANG) -c $(MGMT_KIOTA_CLASS) -n $(MGMT_KIOTA_NAMESPACE) -d $(MGMT_OPENAPI_SPEC) -o $(MGMT_KIOTA_OUTPUT)
	@echo "Management API Kiota generation complete."

generate-auth: ## Regenerate Auth API Kiota client files from OpenAPI spec (placeholder)
	@echo "Auth API generation not yet implemented."

dotnet-build: ## Build the C# project
	@echo "Building C# project..."
	dotnet build
	@echo "Build complete."

clean: ## Clean build artifacts
	@echo "Cleaning build artifacts..."
	dotnet clean
	@echo "Clean complete."
