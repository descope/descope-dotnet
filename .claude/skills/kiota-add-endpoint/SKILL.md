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

Take from a changed shared model only what your endpoint needs, not the whole regenerated file — most of its drift belongs to other endpoints. A *new* model it references has to come in whole, or the build fails on a missing type. An existing one that merely drifted should be reverted, unless it **is** your endpoint's own request or response type: there the new properties are your endpoint's surface, and reverting them narrows it silently without breaking the build. Say which properties you kept.

`make generate` already applied `Obsolete.csv`, so do not run `make post-process-obsolete` again afterwards — on files that are already annotated it appends a second `[Obsolete]` and the build fails with CS0579. If the new method needs an extension-method wrapper, add its `Obsolete.csv` row and let the next full regeneration apply it.
