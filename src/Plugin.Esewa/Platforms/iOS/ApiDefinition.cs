using System;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace Plugin.Esewa.Binding;

// @objc public protocol EsewaSDKPaymentDelegate
// (Exposed directly by EsewaSDK.xcframework.)
[Protocol, Model]
[BaseType(typeof(NSObject))]
interface EsewaSDKPaymentDelegate
{
    // - (void)onEsewaSDKPaymentSuccessWithInfo:(NSDictionary<NSString *, id> *)info;
    [Abstract]
    [Export("onEsewaSDKPaymentSuccessWithInfo:")]
    void OnPaymentSuccess(NSDictionary info);

    // - (void)onEsewaSDKPaymentErrorWithErrorDescription:(NSString *)errorDescription;
    [Abstract]
    [Export("onEsewaSDKPaymentErrorWithErrorDescription:")]
    void OnPaymentError(string errorDescription);
}

// @objc(EsewaBridge) public class EsewaBridge : NSObject
// Provided by the Swift shim in Native/Shim/EsewaBridge.swift. It re-exposes
// the Swift-only EsewaSDK payment API through an Objective-C compatible
// selector so it can be reached from managed code.
[BaseType(typeof(NSObject))]
interface EsewaBridge
{
    // - (void)initiatePaymentFromViewController:environment:merchantId:merchantSecret:
    //         productName:productAmount:productId:callbackUrl:paymentProperties:delegate:
    [Export("initiatePaymentFromViewController:environment:merchantId:merchantSecret:productName:productAmount:productId:callbackUrl:paymentProperties:delegate:")]
    void InitiatePayment(
        UIViewController viewController,
        string environment,
        string merchantId,
        string merchantSecret,
        string productName,
        string productAmount,
        string productId,
        string callbackUrl,
        [NullAllowed] NSDictionary paymentProperties,
        EsewaSDKPaymentDelegate paymentDelegate);
}
