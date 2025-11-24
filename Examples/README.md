# Running Examples

The Descope .NET SDK includes two example applications demonstrating different usage patterns.

## Running the Examples

```bash
# Instance Example
cd Examples/InstanceExample
dotnet run

# Service Example  
cd Examples/ServiceExample
dotnet run
```

## Examples Overview

### InstanceExample
Demonstrates direct client instantiation - suitable for simple applications or scripts.

### ServiceExample
Demonstrates dependency injection pattern with HttpClientFactory, logging, and Polly policies - recommended for production applications.

## Configuration

Both examples use hardcoded credentials for demonstration. Update the following constants in the example files:
- `PROJECT_ID`
- `MANAGEMENT_KEY`
- `BASE_URL`
