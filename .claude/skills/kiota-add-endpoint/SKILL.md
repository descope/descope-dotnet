---
name: kiota-add-endpoint
description: Add a single API endpoint to the Kiota-generated Descope .NET client without a full regeneration. Use when asked to add an endpoint, add a Kiota endpoint, regenerate one endpoint, wire up a new /v1/mgmt/... or /v1/auth/... path, or expose a new API operation in the SDK.
---

# Add one Kiota endpoint

Kiota has no incremental mode: `kiota generate` always emits a whole client tree for the paths it is given, and `make generate` runs it with `--clean-output`. A full regeneration therefore imports every unrelated spec change accumulated since the last one, which makes a one-endpoint diff unreviewable.

This skill adds one endpoint by generating it in isolation and merging only what is genuinely needed.

## Step 1: generate into the scratch tree

Pick the target by path prefix:

- `/v1/mgmt/**` or `/v2/mgmt/**` -> `make add-mgmt ENDPOINT=<path>`
- `/v1/auth/**` -> `make add-auth ENDPOINT=<path>`

```bash
make add-mgmt ENDPOINT=/v1/mgmt/user/search
```

Never invoke `kiota` directly. The Makefile owns the spec paths, class names, namespaces and generation flags; a second copy of those values will drift.

If `ENDPOINT` matches no path in the spec the target fails with `matched no path in the OpenAPI spec`. That means the path is wrong, not that the endpoint is missing from the SDK — check the spec for the exact path (leaf paths are often more specific than expected, e.g. `/v1/auth/otp/signin/email`, not `/v1/auth/otp/signin`) rather than working around the error.

The target writes to `.kiota-scratch/<mgmt|auth>/` and leaves `Descope/Generated/` untouched. `--include-path` prunes at the spec level, so no other endpoint is ever generated — but the scratch tree still contains the endpoint's full **parent chain** with every sibling pruned out. That pruning is why you cannot simply copy the tree over.

## Step 2: classify every scratch file

For each `*.cs` under the scratch dir, compare it to the same relative path under `Descope/Generated/<Mgmt|Auth>/`:

| State | Action |
|---|---|
| Absent from the real tree | **Copy it.** New leaf request builders and new models. |
| Present and byte-identical | Skip silently. |
| Present and different, and it is a `*RequestBuilder.cs` in the parent chain | **Merge** (step 3). |
| Present and different, and it is under `Models/` | **Reconcile** (step 4). |

An empty copy list is a valid result: it means the endpoint is already in the client, not that the command failed. Say so rather than reporting a problem.

## Step 3: merge the navigation chain

Edit **only the deepest already-existing ancestor** `*RequestBuilder.cs`. From its scratch counterpart, copy across two things, each into its correct existing alphabetical position:

1. the one `using Descope.<Mgmt|Auth>.<...>;` line for the new child namespace
2. the 4-line navigation property block, with its `/// <summary>The <name> property</summary>` comment

Chain files *above* that ancestor (`DescopeMgmtKiotaClient.cs`, `V1/V1RequestBuilder.cs`, and any intermediate builder that already has the child it needs) show as different **only** because their siblings were pruned. Leave them alone. Never copy them.

No change to `Descope/Sdk/DescopeClient.cs` is needed: it exposes `client.Mgmt.V1`/`V2`/`Auth.V1` as the generated request builders directly, so the merged property is reachable immediately.

## Step 4: reconcile changed models

A differing file under `Models/` means the shared model's spec shape changed since the last full regeneration. Compare the two versions by **property name set**, not by raw `diff` — Kiota reorders `using` directives, so a raw diff of an otherwise-additive change looks enormous.

- **Additive only** (properties added, none removed and none retyped): copy the new version. This is the common case and it is safe — existing callers keep compiling and simply gain optional properties. Report which properties were added.
- **Anything removed or retyped**: stop and ask the user. Copying may break other endpoints that share the model; not copying may break this one. Show the delta, do not guess.

**A stale additive model does not fail the build.** Verified: adding `/v1/mgmt/user/search` while leaving `SearchUsersRequest` at its old revision compiles with 0 errors, even though the model was missing 13 properties the spec defines (`Dependent`, `FamilyIds`, `SearchFields`, `SelectedColumns`, `Offset`, …). The endpoint would ship silently unable to express half its request. Step 4 is the only safety net — do not skip it because the build is green.

## Step 5: do not run post-process-obsolete

`make post-process-obsolete` is **not idempotent**. Its awk inserts the `[Obsolete]` attribute before every line matching the method name with no already-present guard, so it is only safe immediately after a `--clean-output` regeneration. Running it after a targeted add double-annotates every entry in `Obsolete.csv`.

If the new method needs an extension-method wrapper, add the `[Obsolete]` attribute to the generated method by hand, **and** add the `Obsolete.csv` row so the next full regeneration reproduces it. See the extension-method rules in `README-maintainer.md`.

## Step 6: verify and report

```bash
make dotnet-build
```

Then report: the files copied, the property merged and into which file, any model deltas applied, and anything left for the user to do. Note that the merge is transitional — the next full `make generate` regenerates the chain file from a spec that by then contains the endpoint, reproducing the same property.

Clean up with `make clean`, which removes `.kiota-scratch/`.

## When to use a full regeneration instead

If the endpoint introduces a brand-new top-level segment (for example a `/v3/...` prefix), the parent chain does not exist yet: several chain files need real merges and `Descope/Sdk/DescopeClient.cs` needs a new version property. Run `make generate` and review the whole diff instead.
