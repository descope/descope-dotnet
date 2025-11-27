.PHONY: build generate generate-mgmt generate-auth clean test examples help

# Default target
.DEFAULT_GOAL := build

# Management API OpenAPI spec file location
MGMT_OPENAPI_SPEC := $(HOME)/dev/go/src/github.com/descope/managementservice/pkg/managementservice/proto/v1/doc/management.openapi.yaml

# Management API Kiota generation parameters
MGMT_KIOTA_LANG := CSharp
MGMT_KIOTA_CLASS := DescopeMgmtKiotaClient
MGMT_KIOTA_NAMESPACE := Descope.Mgmt
MGMT_KIOTA_OUTPUT := ./Descope/Generated/Mgmt
MGMT_KIOTA_EXCLUDE_PATHS := /scim/** # SCIM endpoints are not intended for direct SDK use

# Auth API OpenAPI spec file location
AUTH_OPENAPI_SPEC := $(HOME)/dev/go/src/github.com/descope/onetimeservice/pkg/onetimeservice/proto/v1/doc/onetime.openapi.yaml

# Auth API Kiota generation parameters
AUTH_KIOTA_LANG := CSharp
AUTH_KIOTA_CLASS := DescopeAuthKiotaClient
AUTH_KIOTA_NAMESPACE := Descope.Auth
AUTH_KIOTA_OUTPUT := ./Descope/Generated/Auth
AUTH_KIOTA_INCLUDE_PATHS := /v1/auth/**
AUTH_KIOTA_EXCLUDE_PATHS := /v1/auth/validate # not intended for direct SDK use, instead the SDK validates session JWTs internally with cached keys

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
	kiota generate -l $(MGMT_KIOTA_LANG) -c $(MGMT_KIOTA_CLASS) -n $(MGMT_KIOTA_NAMESPACE) -d $(MGMT_OPENAPI_SPEC) -o $(MGMT_KIOTA_OUTPUT) --exclude-path $(MGMT_KIOTA_EXCLUDE_PATHS) --clean-output
	@echo "Management API Kiota generation complete."

generate-auth: ## Regenerate Auth API Kiota client files from OpenAPI spec
	@echo "Checking for Auth API OpenAPI spec file..."
	@if [ ! -f "$(AUTH_OPENAPI_SPEC)" ]; then \
		echo "ERROR: Auth API OpenAPI spec file not found at: $(AUTH_OPENAPI_SPEC)"; \
		exit 1; \
	fi
	@echo "Auth API OpenAPI spec found: $(AUTH_OPENAPI_SPEC)"
	@echo "Generating Auth API Kiota client files..."
	kiota generate -l $(AUTH_KIOTA_LANG) -c $(AUTH_KIOTA_CLASS) -n $(AUTH_KIOTA_NAMESPACE) -d $(AUTH_OPENAPI_SPEC) -o $(AUTH_KIOTA_OUTPUT) --include-path $(AUTH_KIOTA_INCLUDE_PATHS) --exclude-path $(AUTH_KIOTA_EXCLUDE_PATHS) --clean-output
	@echo "Auth API Kiota generation complete."

dotnet-build: ## Build the C# project
	@echo "Building C# project..."
	cd Descope && dotnet build
	@echo "Build complete."

test: ## Run all unit tests
	@echo "Running unit tests..."
	cd Descope.Test && dotnet test
	@echo "Tests complete."

examples: ## Run both example applications (TODO: remove after testing)
	@echo "Running InstanceExample..."
	cd Examples/InstanceExample && dotnet run
	@echo ""
	@echo "Running ServiceExample..."
	cd Examples/ServiceExample && dotnet run
	@echo "Examples complete."

clean: ## Clean build artifacts
	@echo "Cleaning build artifacts..."
	cd Descope && dotnet clean
	@echo "Clean complete."
