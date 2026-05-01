# Changelog

## [1.6.1](https://github.com/descope/descope-dotnet/compare/Descope-v1.6.0...Descope-v1.6.1) (2026-05-01)


### Bug Fixes

* **test:** fix flaky FgaTest by setting unique names to avoid race conditions in CI ([#193](https://github.com/descope/descope-dotnet/issues/193)) ([4627dba](https://github.com/descope/descope-dotnet/commit/4627dbaf5bee30bffb4dd552ebeb03358e9bcb24))

## [1.6.0](https://github.com/descope/descope-dotnet/compare/Descope-v1.5.1...Descope-v1.6.0) (2026-03-23)


### Features

* add automatic retry on transient HTTP errors ([#190](https://github.com/descope/descope-dotnet/issues/190)) ([ffe5901](https://github.com/descope/descope-dotnet/commit/ffe5901b709e8f50d45cd3ef42bdf55d34d9b426))

## [1.5.1](https://github.com/descope/descope-dotnet/compare/Descope-v1.5.0...Descope-v1.5.1) (2026-03-15)


### Bug Fixes

* JWT key rotation with TTL-based refresh and cache-miss re-fetch ([#186](https://github.com/descope/descope-dotnet/issues/186)) ([da304b9](https://github.com/descope/descope-dotnet/commit/da304b99cada75391f06a906b1f512e15d2c7ac6))

## [1.5.0](https://github.com/descope/descope-dotnet/compare/Descope-v1.4.0...Descope-v1.5.0) (2026-02-26)


### Features

* add text lists CRUD methods ([#177](https://github.com/descope/descope-dotnet/issues/177)) ([d8294b7](https://github.com/descope/descope-dotnet/commit/d8294b77638c7b468d5a6ec32698ac090b6f7fa5))

## [1.4.0](https://github.com/descope/descope-dotnet/compare/Descope-v1.3.4...Descope-v1.4.0) (2026-02-25)


### Features

* add lists CRUD APIs ([#174](https://github.com/descope/descope-dotnet/issues/174)) ([25d386e](https://github.com/descope/descope-dotnet/commit/25d386e602c0447838e3bfc16a54f0b619db2a70))

## [1.3.4](https://github.com/descope/descope-dotnet/compare/Descope-v1.3.3...Descope-v1.3.4) (2026-02-20)


### Bug Fixes

* Support "Manage in cookies" for RefreshJWT and SessionJWT without leaking cookies to mgmt calls and failing them ([#171](https://github.com/descope/descope-dotnet/issues/171)) ([83c9738](https://github.com/descope/descope-dotnet/commit/83c9738b92a2fcfb7882edd0efd9666d74146f8d))

## [1.3.3](https://github.com/descope/descope-dotnet/compare/Descope-v1.3.2...Descope-v1.3.3) (2026-02-08)


### Bug Fixes

* use isolated http client instances to avoid rare race conditions ([#165](https://github.com/descope/descope-dotnet/issues/165)) ([5567e18](https://github.com/descope/descope-dotnet/commit/5567e188ad8bded44101faad3c27bc96db4a6598))

## [1.3.2](https://github.com/descope/descope-dotnet/compare/Descope-v1.3.1...Descope-v1.3.2) (2026-02-05)


### Bug Fixes

* do not append descope headers twice when using DI to create the SDK client ([#163](https://github.com/descope/descope-dotnet/issues/163)) ([e58a933](https://github.com/descope/descope-dotnet/commit/e58a9330a8da2ffafe40948d3bd91eff8fdcfa2e))

## [1.3.1](https://github.com/descope/descope-dotnet/compare/Descope-v1.3.0...Descope-v1.3.1) (2026-02-05)


### Bug Fixes

* consider all user roles in SDK token methods, not just the first role ([#162](https://github.com/descope/descope-dotnet/issues/162)) ([3f0e7dd](https://github.com/descope/descope-dotnet/commit/3f0e7dd8903a48377b9290b4440a92511d370b22))


### Documentation

* explain how to verify version releases in maintainers readme ([#156](https://github.com/descope/descope-dotnet/issues/156)) ([0783485](https://github.com/descope/descope-dotnet/commit/078348511b89b1644ab9563997dfd608b4a8c500))

## [1.3.0](https://github.com/descope/descope-dotnet/compare/Descope-v1.2.0...Descope-v1.3.0) (2026-01-15)


### Features

* simplify access to management flow call results as JSON ([#154](https://github.com/descope/descope-dotnet/issues/154)) ([647a42e](https://github.com/descope/descope-dotnet/commit/647a42e6279606e9d1eb912be416ec36de914622))

## [1.2.0](https://github.com/descope/descope-dotnet/compare/Descope-v1.1.0...Descope-v1.2.0) (2025-12-30)


### Features

* add status field to CreateUserRequest ([#146](https://github.com/descope/descope-dotnet/issues/146)) ([98a28bb](https://github.com/descope/descope-dotnet/commit/98a28bb35e6818a62af51b223a18861b8c5f548e))

## [1.1.0](https://github.com/descope/descope-dotnet/compare/Descope-v1.0.1...Descope-v1.1.0) (2025-12-29)


### Features

* AddDescopeOidcAuthentication wrapper method with demo ([#141](https://github.com/descope/descope-dotnet/issues/141)) ([057e07a](https://github.com/descope/descope-dotnet/commit/057e07a961ceba9187cba4af223f5f4959e03f99))


### Documentation

* update maintainer readme to include details about releasing 1.x.x and 0.x.x versions ([#138](https://github.com/descope/descope-dotnet/issues/138)) ([6de5148](https://github.com/descope/descope-dotnet/commit/6de5148a43c7fb1a6c005cd64be3554f8583a524))

## [1.0.1](https://github.com/descope/descope-dotnet/compare/Descope-v1.0.0...Descope-v1.0.1) (2025-12-16)


### Bug Fixes

* allow multiple concurrent calls to validate session before a public key is cached ([#124](https://github.com/descope/descope-dotnet/issues/124)) ([f11c017](https://github.com/descope/descope-dotnet/commit/f11c01765c65e0964919c9dfa1e4457f59afcd4e))
* bootstrap rp ([#134](https://github.com/descope/descope-dotnet/issues/134)) ([7d6babe](https://github.com/descope/descope-dotnet/commit/7d6babee3f5d5a42fff0c51ebf38699aece603c1))
* release please version xpath ([#133](https://github.com/descope/descope-dotnet/issues/133)) ([fcff20f](https://github.com/descope/descope-dotnet/commit/fcff20f37d50e1a2ac9a009be00fe637be98b566))
