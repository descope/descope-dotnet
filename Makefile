.PHONY: build generate generate-mgmt generate-auth clean test examples help add-mgmt add-auth

# Default target
.DEFAULT_GOAL := build

# Management API OpenAPI spec file location
MGMT_OPENAPI_SPEC := $(GODESCOPE)/backend/managementservice/pkg/managementservice/proto/v1/doc/management.openapi.yaml

# Management API Kiota generation parameters
MGMT_KIOTA_LANG := CSharp
MGMT_KIOTA_CLASS := DescopeMgmtKiotaClient
MGMT_KIOTA_NAMESPACE := Descope.Mgmt
MGMT_KIOTA_OUTPUT := ./Descope/Generated/Mgmt
MGMT_KIOTA_EXCLUDE_PATHS := \
	/scim/** \
	/v1/mgmt/user/history \
	/v1/mgmt/accesskey/import \
	/v1/mgmt/accesskey/delete/batch \
	/v1/mgmt/accesskey/activate/batch \
	/v1/mgmt/accesskey/deactivate/batch \
	/v1/mgmt/authz/re/deleteresourcesrelations \
	/v1/mgmt/connector/useraudit/set \
	/v1/mgmt/inboundapp/** \
	/v1/mgmt/infra \
	/v1/mgmt/localization/** \
	/v1/mgmt/mcp/** \
	/v1/mgmt/outbound/app/create/bydcrpreset \
	/v1/mgmt/outbound/app/create/bytemplate \
	/v1/mgmt/outbound/app/tenant/token \
	/v1/mgmt/outbound/app/tenant/token/latest \
	/v1/mgmt/outbound/app/user/token \
	/v1/mgmt/outbound/app/user/token/latest \
	/v1/mgmt/outbound/apps-with-user-token \
	/v1/mgmt/outbound/token \
	/v1/mgmt/outbound/user/tokens \
	/v1/mgmt/project/clone/async \
	/v1/mgmt/project/clone/async/** \
	/v1/mgmt/project/export \
	/v1/mgmt/project/import \
	/v1/mgmt/project/signkey/** \
	/v1/mgmt/role/delete/batch \
	/v1/mgmt/tenant/adminlinks/sso/authenticated \
	/v1/mgmt/tenant/adminlinks/sso/send \
	/v1/mgmt/tenant/sso-user-remove \
	/v1/mgmt/thirdparty/app/delete/batch \
	/v1/mgmt/token/clientassertion \
	/v1/mgmt/user/customattribute/** \
	/v1/mgmt/user/customattributes \
	/v1/mgmt/user/search \
	/v2/mgmt/sso/settings/all \
	/v2/mgmt/tenant/adminlinks/sso/generate \
	/v2/mgmt/theme/** \
	/v2/mgmt/user/update/role/add

# Auth API OpenAPI spec file location
AUTH_OPENAPI_SPEC := $(GODESCOPE)/backend/onetimeservice/pkg/onetimeservice/proto/v1/doc/onetime.openapi.yaml

# Auth API Kiota generation parameters
AUTH_KIOTA_LANG := CSharp
AUTH_KIOTA_CLASS := DescopeAuthKiotaClient
AUTH_KIOTA_NAMESPACE := Descope.Auth
AUTH_KIOTA_OUTPUT := ./Descope/Generated/Auth
AUTH_KIOTA_INCLUDE_PATHS := /v1/auth/**
AUTH_KIOTA_EXCLUDE_PATHS := /v1/auth/validate # not intended for direct SDK use, instead the SDK validates session JWTs internally with cached keys

# Targeted endpoint addition scratch directory (see README-maintainer.md)
override KIOTA_SCRATCH := .kiota-scratch

help: ## Show this help message
	@echo 'Usage: make [target]'
	@echo ''
	@echo 'Available targets:'
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  %-15s %s\n", $$1, $$2}'

build: generate dotnet-build ## Regenerate Kiota files and rebuild C# DLLs (default)

generate: check-kiota generate-mgmt generate-auth post-process-obsolete ## Regenerate all Kiota client files

check-kiota: ## Check if Kiota is installed, install if not found
	@echo "Checking for Kiota..."
	@which kiota > /dev/null 2>&1 || { \
		echo "Kiota not found. Installing..."; \
		dotnet tool install --global Microsoft.OpenApi.Kiota; \
	}
	@echo "Kiota is available."

generate-mgmt: ## Regenerate Management API Kiota client files from OpenAPI spec
	@echo "Checking for Management API OpenAPI spec file..."
	@if [ ! -f "$(MGMT_OPENAPI_SPEC)" ]; then \
		echo "ERROR: Management API OpenAPI spec file not found at: $(MGMT_OPENAPI_SPEC)"; \
		exit 1; \
	fi
	@echo "Management API OpenAPI spec found: $(MGMT_OPENAPI_SPEC)"
	@echo "Generating Management API Kiota client files..."
	kiota generate -l $(MGMT_KIOTA_LANG) -c $(MGMT_KIOTA_CLASS) -n $(MGMT_KIOTA_NAMESPACE) -d $(MGMT_OPENAPI_SPEC) -o $(MGMT_KIOTA_OUTPUT) $(foreach p,$(MGMT_KIOTA_EXCLUDE_PATHS),--exclude-path $(p)) --clean-output
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

add-mgmt: API := MGMT
add-mgmt: EXAMPLE := /v1/mgmt/user/create
add-mgmt: ## Generate one Management API endpoint into the scratch dir (ENDPOINT=/v1/mgmt/foo)
add-auth: API := AUTH
add-auth: EXAMPLE := /v1/auth/otp/signin/email
add-auth: ## Generate one Auth API endpoint into the scratch dir (ENDPOINT=/v1/auth/foo)

add-mgmt add-auth: SCRATCH = $(KIOTA_SCRATCH)/$(@:add-%=%)
add-mgmt add-auth: check-kiota
	@if [ -z "$(ENDPOINT)" ]; then \
		echo "ERROR: ENDPOINT is required, e.g. make $@ ENDPOINT=$(EXAMPLE)"; \
		exit 1; \
	fi
	@if [ ! -f "$($(API)_OPENAPI_SPEC)" ]; then \
		echo "ERROR: OpenAPI spec file not found at: $($(API)_OPENAPI_SPEC)"; \
		exit 1; \
	fi
	@for pattern in $($(API)_KIOTA_EXCLUDE_PATHS); do \
		case "$(ENDPOINT)" in $$pattern) \
			echo "ERROR: $(ENDPOINT) is excluded from the SDK by $$pattern in $(API)_KIOTA_EXCLUDE_PATHS."; \
			echo "       Remove that entry first, or the next 'make generate' will delete the addition."; \
			exit 1;; \
		esac; \
	done
	@echo "Generating $(ENDPOINT) into $(SCRATCH)..."
	kiota generate -l $($(API)_KIOTA_LANG) -c $($(API)_KIOTA_CLASS) -n $($(API)_KIOTA_NAMESPACE) -d $($(API)_OPENAPI_SPEC) -o $(SCRATCH) --include-path "$(ENDPOINT)" --clean-output
	@if [ -z "$$(find $(SCRATCH) -mindepth 2 -name '*.cs')" ]; then \
		echo "ERROR: ENDPOINT $(ENDPOINT) matched no path in the OpenAPI spec"; \
		exit 1; \
	fi
	@echo "Scratch generation complete. Merge with the kiota-add-endpoint skill, or by hand."

dotnet-build: ## Build the C# project
	@echo "Building C# project..."
	cd Descope && dotnet build
	@echo "Build complete."

test: ## Run tests for all target frameworks (net6.0, net8.0, net9.0, net10.0)
	@echo "Checking for required .NET SDK versions..."
	@for version in 6.0 8.0 9.0 10.0; do \
		if ! dotnet --list-sdks | grep -q "^$$version"; then \
			echo "ERROR: .NET SDK $$version is not installed. Please install it first from: https://dotnet.microsoft.com/en-us/download/dotnet"; \
			exit 1; \
		fi; \
	done
	@echo "All required .NET SDK versions found."
	@echo ""
	@echo "Running unit tests for all target frameworks..."
	@echo ""
	@echo "=== Testing net6.0 ==="
	@cd Descope.Test && dotnet test --framework net6.0 --logger "console;verbosity=normal" || (echo "net6.0 tests FAILED" && exit 1)
	@echo ""
	@echo "=== Testing net8.0 ==="
	@cd Descope.Test && dotnet test --framework net8.0 --logger "console;verbosity=normal" || (echo "net8.0 tests FAILED" && exit 1)
	@echo ""
	@echo "=== Testing net9.0 ==="
	@cd Descope.Test && dotnet test --framework net9.0 --logger "console;verbosity=normal" || (echo "net9.0 tests FAILED" && exit 1)
	@echo ""
	@echo "=== Testing net10.0 ==="
	@cd Descope.Test && dotnet test --framework net10.0 --logger "console;verbosity=normal" || (echo "net10.0 tests FAILED" && exit 1)
	@echo ""
	@echo "All framework tests complete."

test-quick: ## Run tests for default framework only (faster)
	@echo "Running unit tests (quick)..."
	cd Descope.Test && dotnet test --framework net8.0
	@echo "Quick tests complete."

cover: ## Run tests for default framework with coverage report - can be viewed in VSCode using Coverage Gutters extension
	@echo "Cleaning previous test results..."
	@rm -rf Descope.Test/TestResults 2>/dev/null || true
	@echo "Checking for ReportGenerator..."
	@dotnet tool list -g | grep -q dotnet-reportgenerator-globaltool || { \
		echo "ReportGenerator not found. Installing..."; \
		dotnet tool install -g dotnet-reportgenerator-globaltool; \
	}
	@echo "Running unit tests with coverage..."
	cd Descope.Test && dotnet test --framework net8.0 --collect:"XPlat Code Coverage" --results-directory ./TestResults
	@echo ""
	@echo "Coverage Summary:"
	@reportgenerator -reports:"Descope.Test/TestResults/**/coverage.cobertura.xml" -reporttypes:"TextSummary" -targetdir:"." 2>/dev/null || true
	@cat Summary.txt 2>/dev/null && rm -f Summary.txt || echo "Coverage report not generated"
	@echo ""
	@echo "Full coverage report available in Descope.Test/TestResults/"

examples: ## Run both example applications (TODO: remove after testing)
	@echo "Running InstanceExample..."
	cd Examples/InstanceExample && dotnet run
	@echo ""
	@echo "Running ServiceExample..."
	cd Examples/ServiceExample && dotnet run
	@echo "Examples complete."

post-process-obsolete: ## Apply post-processing obsolete annotations to Kiota generated method we want to mark as obsolete
	@echo "Applying post-processing annotations from Obsolete.csv..."
	@if [ ! -f "Obsolete.csv" ]; then \
		echo "ERROR: Obsolete.csv not found, cannot perform post-processing"; \
		exit 1; \
	fi
	@tail -n +2 Obsolete.csv | while IFS=, read -r filepath method replacement; do \
		if [ -f "$$filepath" ]; then \
			echo "Processing $$filepath: marking $$method as obsolete (use $$replacement)..."; \
			awk -v method="$$method" -v replacement="$$replacement" \
				'/'"$$method"'/ { print "        [Obsolete(\"Use " replacement " instead\")]"; } {print}' \
				"$$filepath" > "$$filepath.tmp" && mv "$$filepath.tmp" "$$filepath"; \
		else \
			echo "ERROR: File not found: $$filepath" && exit 1; \
		fi; \
	done
	@echo "Post-processing complete."

clean: ## Clean build artifacts
	@echo "Cleaning build artifacts..."
	cd Descope && dotnet clean
	@rm -rf $(KIOTA_SCRATCH)
	@echo "Clean complete."
