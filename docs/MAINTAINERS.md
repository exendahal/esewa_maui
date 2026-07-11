# Maintainer & build notes

This document is for people **building/publishing** the `esewa_maui` package —
not for app developers consuming it (see the root `README.md` for that).

## Repository layout

| Path | What it is |
| ---- | ---------- |
| `src/Plugin.Esewa/` | The binding + unified API library (multi-targets `net10.0-android`, `net10.0-ios`). Package id is `esewa_maui`; assembly/namespace is `Plugin.Esewa`. |
| `samples/EsewaSample/` | A MAUI app that calls `PayAsync` from a button. |
| `native packages/` | The original native artifacts provided by eSewa. |
| `src/Plugin.Esewa/Native/` | Copies of the native artifacts consumed by the binding. |

Public API surface (root of `Plugin.Esewa`): `CrossEsewaPayment`,
`IEsewaPayment`, `EsewaPaymentRequest`, `EsewaPaymentResult` /
`EsewaPaymentStatus`, `EsewaEnvironment`.

## Native SDK versions being bound

- **iOS** — `EsewaSDK.xcframework`, `CFBundleShortVersionString` **1.0**
  (build 1), min iOS 11.0, built with the iOS 18 SDK / Xcode 16 / Swift 6.0.
- **Android** — `eSewaPaymentSdk.aar`, package `com.f1soft.esewapaymentsdk`,
  minSdk 21. The AAR does **not** embed a version string, so track the version
  you downloaded separately.

## How each platform is bound

### Android — `eSewaPaymentSdk.aar`

A standard **.NET Android Java binding** (`<AndroidLibrary Bind="true" />`).
`Platforms/Android/Transforms/Metadata.xml` smooths over the R8-obfuscated
release artifact (the `ESEWA_CONFIGURATION` / `ESEWA_PAYMENT` string constants
would otherwise generate members that collide with their enclosing types).

The native flow uses `startActivityForResult`, so the plugin ships a small
transparent relay activity — `EsewaLauncherActivity` — that launches
`com.f1soft.esewapaymentsdk.ui.screens.EsewaPaymentActivity`, receives the
result in `OnActivityResult`, and completes the awaiting `Task`. It registers
itself via `[Activity]`, so consuming apps need no manifest changes.

The eSewa AAR pulls in runtime dependencies MAUI does not provide transitively.
These are declared as `PackageReference`s (and flow to consumers via the
package): `Xamarin.Kotlin.StdLib`, `Xamarin.AndroidX.DataBinding.ViewBinding`,
`Xamarin.AndroidX.ConstraintLayout`, `Square.OkHttp3`, and
`Square.OkHttp3.LoggingInterceptor`.

This target builds and links completely on Windows.

### iOS — `EsewaSDK.xcframework` (requires a one-time macOS step)

`EsewaSDK` is a **Swift** framework. Its payment entry points —
`EsewaSDK.init(inViewController:environment:delegate:)` and `initiatePayment(...)`
— take the Swift enum `EsewaSDKEnvironment`, which is **not representable in
Objective-C**. Because .NET binds native iOS libraries through their
Objective-C surface, those methods have no selector and are unreachable from
managed code as shipped. (Only the `@objc` `EsewaSDKPaymentDelegate` protocol is
exposed.)

The fix is a thin **`@objc` Swift shim** that re-exposes the calls using
Objective-C-compatible types (`NSString` instead of the enum):

- `Native/Shim/EsewaBridge.swift` — the shim (`@objc class EsewaBridge`).
- `Native/Shim/build-shim.sh` — builds `Native/EsewaBridge.xcframework` from it.
- `Platforms/iOS/ApiDefinition.cs` — binds both the delegate protocol and the
  shim's `EsewaBridge` class.

**Before packaging/building for iOS**, build the shim once on a Mac:

```bash
cd src/Plugin.Esewa/Native/Shim
./build-shim.sh          # produces ../EsewaBridge.xcframework
```

The plugin csproj references `EsewaBridge.xcframework` only if it exists, so the
managed assembly still restores and builds on Windows; the shim is required at
**app link time** (which happens on a Mac for iOS anyway).

> If eSewa later ships an `@objc`-annotated payment API in the framework itself,
> the shim can be deleted and `ApiDefinition.cs` pointed at the framework's own
> class instead.

**Runtime notes (verified against the framework binary):**

- The shim's Objective-C selector is pinned explicitly with
  `@objc(initiatePaymentFromViewController:…:delegate:)`, so it can never drift
  from the `[Export(...)]` in `ApiDefinition.cs`.
- The managed `PaymentDelegate` is kept rooted in a static set until a native
  callback fires. The SDK may hold its `delegate` weakly, and without rooting
  the GC could reclaim the managed peer mid-flow and crash on callback.
- `EsewaSDK.framework` is a **dynamic** Swift framework, so it is embedded and
  signed into the app bundle. Its Swift runtime dependencies (`libswiftCore`,
  etc.) are ABI-stable and shipped by iOS 12.2+, so no Swift dylibs need
  bundling. It is referenced **without** `ForceLoad` (only valid for the static
  shim).

## Building

Prerequisites: .NET 10 SDK with the `android`, `ios`, and `maui` workloads.

```bash
# Build the binding library
dotnet build src/Plugin.Esewa/Plugin.Esewa.csproj -f net10.0-android
dotnet build src/Plugin.Esewa/Plugin.Esewa.csproj -f net10.0-ios

# Build / run the sample (Android)
dotnet build samples/EsewaSample/EsewaSample.csproj -f net10.0-android
```

iOS builds/deploys require a paired Mac, and the shim step above must be done
first.

## Packaging (NuGet)

```bash
dotnet pack src/Plugin.Esewa/Plugin.Esewa.csproj -c Release -o ./nupkg
```

Package layout:

- **Android** — the AAR is embedded into the managed assembly and flows to
  consumers automatically; runtime deps flow as NuGet dependencies.
- **iOS** — native frameworks are **not** embedded. The package ships
  `buildTransitive/esewa_maui.targets` plus `EsewaSDK.xcframework` (and
  `EsewaBridge.xcframework` when present). The `.targets` file auto-imports into
  the consuming iOS app and re-declares the `NativeReference`s, resolving the
  frameworks relative to the package.

> **Publish from macOS** (after running `build-shim.sh`) so that
> `EsewaBridge.xcframework` is included. A package built on Windows is missing
> the shim, and the iOS payment API will not link in consuming apps. The
> `.targets` file emits a build warning if the shim is missing.
