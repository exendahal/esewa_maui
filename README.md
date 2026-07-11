# eSewa .NET MAUI Bindings

.NET MAUI bindings for the native **eSewa payment SDKs** on Android and iOS.

The goal is a single, unified C# API:

```csharp
var result = await CrossEsewaPayment.Current.PayAsync(new EsewaPaymentRequest
{
    ClientId    = "<merchant-client-id>",
    SecretKey   = "<merchant-secret>",
    Amount      = "100",
    ProductName = "Test Product",
    ProductId   = "TXN-001",
    CallbackUrl = "https://example.com/callback",
    Environment = EsewaEnvironment.Test,
});

if (result.IsSuccess)
    Console.WriteLine(result.Message);
```

## Repository layout

| Path | What it is |
| ---- | ---------- |
| `src/Plugin.Esewa/` | The binding + unified API library (multi-targets `net10.0-android` and `net10.0-ios`). |
| `samples/EsewaSample/` | A MAUI app that calls `PayAsync` from a button. |
| `native packages/` | The original native artifacts you provided. |
| `src/Plugin.Esewa/Native/` | Copies of the native artifacts consumed by the binding. |

The public API surface is small and lives at the root of `Plugin.Esewa`:

- `CrossEsewaPayment.Current` / `.IsSupported` — the entry point.
- `IEsewaPayment.PayAsync(request, ct)` — launches the native flow.
- `EsewaPaymentRequest` — input.
- `EsewaPaymentResult` + `EsewaPaymentStatus` — normalized output.
- `EsewaEnvironment` — `Test` / `Production`.

## How each platform is bound

### Android — `eSewaPaymentSdk.aar`

A standard **.NET Android Java binding**. The AAR is added with
`<AndroidLibrary Bind="true" />`, and `Platforms/Android/Transforms/Metadata.xml`
smooths over the R8-obfuscated release artifact (the `ESEWA_CONFIGURATION` /
`ESEWA_PAYMENT` constants would otherwise generate members that collide with
their enclosing types).

The native flow uses `startActivityForResult`, so the plugin ships a small
transparent relay activity — `EsewaLauncherActivity` — that launches
`com.f1soft.esewapaymentsdk.ui.screens.EsewaPaymentActivity`, receives the
result in `OnActivityResult`, and completes the awaiting `Task`. It registers
itself automatically via `[Activity]`, so host apps need no manifest changes.

This target builds and links completely on Windows.

### iOS — `EsewaSDK.xcframework` (requires a one-time macOS step)

`EsewaSDK` is a **Swift** framework. Its payment entry points —
`EsewaSDK.init(inViewController:environment:delegate:)` and
`initiatePayment(...)` — take the Swift enum `EsewaSDKEnvironment`, which is
**not representable in Objective-C**. Because .NET binds native iOS libraries
through their Objective-C surface, those two methods have no selector and are
unreachable from managed code as shipped. (Only the `@objc`
`EsewaSDKPaymentDelegate` protocol is exposed.)

The standard fix for a Swift-only API is a thin **`@objc` Swift shim** that
re-exposes the calls using Objective-C-compatible types (`NSString` instead of
the enum). That shim is provided:

- `Native/Shim/EsewaBridge.swift` — the shim (`@objc class EsewaBridge`).
- `Native/Shim/build-shim.sh` — builds `Native/EsewaBridge.xcframework` from it.
- `Platforms/iOS/ApiDefinition.cs` — binds both the delegate protocol and the
  shim's `EsewaBridge` class.

**Before building the iOS app**, run the shim build once on a Mac:

```bash
cd src/Plugin.Esewa/Native/Shim
./build-shim.sh          # produces ../EsewaBridge.xcframework
```

The plugin csproj references `EsewaBridge.xcframework` only if it exists, so the
managed assembly still restores and builds on Windows; the shim is required at
**app link time** (which happens on a Mac anyway for iOS).

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
  bundling. It is referenced **without** `ForceLoad` (that flag is only valid
  for the static shim).

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

## Notes

- The sample uses eSewa's public sandbox test credentials. Swap in your own
  merchant `ClientId` / `SecretKey` and set `Environment = Production` to go live.
- `EsewaPaymentRequest.Properties` is forwarded to the SDK's extra
  properties map on both platforms.

## Further reading

For merchant onboarding, obtaining credentials, callback configuration, and the
underlying SDK behaviour, refer to the official eSewa developer portal:
<https://developer.esewa.com.np/>

## License

Released under the **MIT License**.

```
MIT License

Copyright (c) Santosh Dahal

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

> Note: the MIT license covers this binding/plugin code. The native eSewa SDKs
> bundled under `native packages/` are the property of eSewa and are subject to
> eSewa's own licensing terms — see <https://developer.esewa.com.np/>.
