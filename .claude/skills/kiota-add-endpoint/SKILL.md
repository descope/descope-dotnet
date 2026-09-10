---
name: kiota-add-endpoint
description: Add a single endpoint to the Kiota-generated Descope .NET client without shipping unrelated spec drift. Use when asked to add an endpoint, expose a new API operation, or wire up a new /v1/mgmt/... or /v1/auth/... path in the SDK.
---

# Add one Kiota endpoint

Follow the "Adding a Single Endpoint" section of `README-maintainer.md` exactly. It is the source of truth for this procedure; do not improvise a shortcut around it.

Two points specific to doing this as an agent:

- Do not work around step 1. An endpoint on the exclude list is a deliberate decision. Stop and ask rather than removing the entry yourself.
- Finish by reporting which files you kept and which shared-model properties you took, so the diff can be reviewed without re-running the generation.
