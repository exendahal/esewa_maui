import UIKit
import EsewaSDK

/// Objective-C compatible shim over the Swift-only `EsewaSDK` payment API.
///
/// `EsewaSDK.initiatePayment(...)` and its initializer take an
/// `EsewaSDKEnvironment` Swift enum, which is not representable in
/// Objective-C. Because .NET binds native iOS libraries through their
/// Objective-C surface, those methods are unreachable from managed code as
/// shipped. This class re-exposes them using only Objective-C compatible
/// types (`NSString`, `UIViewController`, `NSDictionary`, and the already
/// `@objc` `EsewaSDKPaymentDelegate`), so the .NET binding can call them.
@objc(EsewaBridge)
public class EsewaBridge: NSObject {

    /// The live SDK instance. Held here so it outlives `initiatePayment`
    /// while the modal payment flow is on screen.
    private var sdk: EsewaSDK?

    /// - Parameters:
    ///   - viewController: Presenting view controller.
    ///   - environment: `"production"` for live, anything else for development/test.
    ///   - merchantId: eSewa merchant/client id.
    ///   - merchantSecret: eSewa merchant secret key.
    ///   - productName: Product / service name.
    ///   - productAmount: Amount as a string.
    ///   - productId: Merchant-side unique product id.
    ///   - callbackUrl: Registered callback url.
    ///   - paymentProperties: Optional extra key/value properties.
    ///   - delegate: Receives success / error callbacks.
    @objc(initiatePaymentFromViewController:environment:merchantId:merchantSecret:productName:productAmount:productId:callbackUrl:paymentProperties:delegate:)
    public func initiatePayment(
        fromViewController viewController: UIViewController,
        environment: String,
        merchantId: String,
        merchantSecret: String,
        productName: String,
        productAmount: String,
        productId: String,
        callbackUrl: String,
        paymentProperties: [String: Any]?,
        delegate: EsewaSDKPaymentDelegate
    ) {
        let env: EsewaSDKEnvironment = (environment == "production") ? .production : .development

        let sdk = EsewaSDK(inViewController: viewController, environment: env, delegate: delegate)
        self.sdk = sdk

        sdk.initiatePayment(
            merchantId: merchantId,
            merchantSecret: merchantSecret,
            productName: productName,
            productAmount: productAmount,
            productId: productId,
            callbackUrl: callbackUrl,
            paymentProperties: paymentProperties
        )
    }
}
