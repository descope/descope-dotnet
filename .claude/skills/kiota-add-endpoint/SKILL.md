---
name: kiota-add-endpoint
description: Add a single API endpoint to the Kiota-generated Descope .NET client without a full regeneration. Use when asked to add an endpoint, add a Kiota endpoint, regenerate one endpoint, wire up a new /v1/mgmt/... or /v1/auth/... path, or expose a new API operation in the SDK.
---

# Add one Kiota endpoint

Kiota cannot generate incrementally, so a full `make generate` imports all spec drift accumulated since the last one. Background and rationale: "Targeted Endpoint Additions" in `README-maintainer.md`.

## Step 1: generate into the scratch tree

`/v1/mgmt/**` and `/v2/mgmt/**` use `add-mgmt`; `/v1/auth/**` uses `add-auth`.

```bash
make add-mgmt ENDPOINT=/v1/mgmt/accesskey/rotate
```

Never invoke `kiota` directly — the Makefile owns the spec paths, class names and namespaces.

Two failures are the target telling you the request is wrong, not something to work around:

- `matched no path in the OpenAPI spec` — the path does not exist. Leaf paths are more specific than expected (`/v1/auth/otp/signin/email`, not `/v1/auth/otp/signin`). Grep the spec for the real path.
- `is excluded from the SDK by <pattern>` — the project deliberately withholds this endpoint. Do not work around it. Adding it requires removing that entry from `MGMT_KIOTA_EXCLUDE_PATHS` / `AUTH_KIOTA_EXCLUDE_PATHS` first, which is a decision for the user.

## Step 2: classify

```bash
.claude/skills/kiota-add-endpoint/classify.sh mgmt   # or: auth
```

This prints one tagged line per file. Do **not** read the generated `.cs` files to work this out yourself — a realistic endpoint is ~170KB of C# and the script answers it in a few hundred bytes.

| Tag | Action |
|---|---|
| `NEW` | `cp` it from the scratch tree. New leaf builders and new models. |
| `SAME` | Nothing. |
| `PRUNED` | Leave alone. It differs only because `--include-path` pruned its siblings. Never copy these. |
| `MERGE` | Step 3. The `nav: +Name` is the property to add; `-` entries are pruning noise. |
| `ADDITIVE` | `cp` it. Shared model gained properties; existing callers keep compiling. Report which. |
| `BLOCKED` | Stop. Script exits 3. Step 4. |
| `RESHAPED` | Differs with no property change (attribute or serializer churn). Diff it and decide. |

An all-`SAME` readout with no `MERGE` or `NEW` means the endpoint is already in the client. Say so; it is not a failure.

## Step 3: merge the one navigation property

Exactly one file is tagged `MERGE`. Into it, copy from its scratch counterpart:

1. the `using` line for the new child namespace
2. the 4-line nav property block for the `+Name`, with its `/// <summary>The <name> property</summary>` comment

Both lists are already alphabetically sorted — insert in order. Change nothing else in the file, and touch no other chain file.

`Descope/Sdk/DescopeClient.cs` needs no change: it exposes `client.Mgmt.V1`/`V2`/`Auth.V1` as the generated builders directly.

## Step 4: BLOCKED models

A `BLOCKED` model lost or retyped a property. Copying it can break other endpoints sharing the model; skipping it can break this one. Show the user the `props:` delta and ask. Never guess.

Note that a stale model still **compiles**, so a green build does not mean this step was done.

## Step 5: verify and report

```bash
make dotnet-build
rm -rf .kiota-scratch
```

Use `rm -rf`, not `make clean` — `clean` also runs `dotnet clean` and would discard the build you just did.

Report the files copied, the property merged and into which file, and any model deltas applied.

If the new method needs an extension-method wrapper, add its `Obsolete.csv` row per `README-maintainer.md`, but do **not** run `make post-process-obsolete` — it is not idempotent and re-running it over already-annotated files fails the build with CS0579.

## When to use a full regeneration instead

An endpoint introducing a brand-new top-level segment (a `/v3/...` prefix) has no existing parent chain: several chain files need merges and `Descope/Sdk/DescopeClient.cs` needs a new version property. Run `make generate` and review the whole diff.
