---
name: kiota-add-endpoint
description: Add a single endpoint to the Kiota-generated Descope .NET client without shipping unrelated spec drift. Use when asked to add an endpoint, expose a new API operation, or wire up a new /v1/mgmt/... or /v1/auth/... path in the SDK.
---

# Add one Kiota endpoint

Kiota cannot generate incrementally, so `make generate` rewrites both clients from the live spec and its diff carries every spec change since the last run. Regenerate everything, then keep only the endpoint you were asked for and discard the rest with git.

1. Check the endpoint is not on the `--exclude-path` list in the `Makefile`. If it is, stop and ask — the exclusion is deliberate, and any regeneration will drop the endpoint again.
2. `make generate`
3. `git status --porcelain Descope/Generated` to see everything it touched.
4. Keep only what the endpoint needs, and revert everything else:
   - the new directory holding its own `*RequestBuilder.cs`
   - the one navigation property (and its `using`) added to the immediate parent `*RequestBuilder.cs`. If that file has other changes, `git restore` it and re-add just those two pieces, in the file's existing alphabetical order
   - any new file under `Models/` that it references
   - revert the rest: `git add` the paths you are keeping, then `git restore --worktree -- Descope/Generated`
     and `git clean -fdx Descope/Generated`. The `-x` is required — a few generated directories match
     boilerplate ignore rules (`Backup*/`), so without it they survive and break the build
5. `make dotnet-build`, then report what you kept.

A shared model under `Models/` can be left stale and still compile, so a green build is not proof the addition is complete. If reverting one breaks the build, the endpoint needs the new version — keep it and say which properties it gained.

Do not run `make post-process-obsolete` by itself; it is not idempotent and re-annotating already-annotated files fails the build. If the new method needs an extension-method wrapper, add its `Obsolete.csv` row and let the next full regeneration apply it.
