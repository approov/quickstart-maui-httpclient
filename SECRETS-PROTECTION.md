# Secrets Protection
You should use this option if you wish to protect access to 3rd party or managed APIs where you are not able to add an Approov token check to the backend. This allows client secrets, or API keys, used for access to be protected with Approov. Rather than build secrets into an app where they might be reverse engineered, they are only provided at runtime by Approov for apps that pass attestation. This substantially improves your protection and prevents these secrets being abused by attackers. Where you are able to modify the backend we recommend you use API Protection for further enhanced flexibility and security.

This quickstart provides straightforward implementation if the secret is currently supplied in a request header to the API. The quickstart interceptor is able to automatically rewrite headers or query parameters as the requests are being made, to automatically substitute in the secret, but only if the app has passed the Approov attestation checks. If the app fails its checks then you can add a custom [rejection](#handling-rejections) handler.

These additional steps require access to the [Approov CLI](https://approov.io/docs/latest/approov-cli-tool-reference/), please follow the [Installation](https://approov.io/docs/latest/approov-installation/) instructions.

## ENABLING MANAGED TRUST ROOTS
Client secrets or API keys also need to be protected in transit. For 3rd party APIs you should not pin against their certificates since you are not in control of when they might be changed. Instead the [Managed Trust Roots](https://approov.io/docs/latest/approov-usage-documentation/#managed-trust-roots) feature can be used to protect TLS.

Ensure managed trust roots are enabled using:

```
approov pin -setManagedTrustRoots on 
```
> Note that this command requires an [admin role](https://approov.io/docs/latest/approov-usage-documentation/#account-access-roles).

This ensures connections may only use official certificates, and blocks the use of self signed certificates that might be used by a Man-in-the-Middle (MitM) attacker.

## ADDING API DOMAINS
In order for secrets to be protected for particular API domains it is necessary to inform Approov about them. Execute the following command:

```
approov api -add your.domain -noApproovToken
```

This informs Approov that it should be active for the domain, but does not need to send Approov tokens for it. Adding the domain ensures that the channel will be protected against Man-in-the-Middle (MitM) attacks.

## MIGRATING THE SECRET INTO APPROOV
It is assumed that you already have some client secrets and/or API keys in your app that you would like to migrate for protection by Approov. To do this you first need to enable the [Secure Strings](https://approov.io/docs/latest/approov-usage-documentation/#secure-strings) feature:

```
approov secstrings -setEnabled
```
> Note that this command requires an [admin role](https://approov.io/docs/latest/approov-usage-documentation/#account-access-roles).

The quickstart integration works by allowing you to replace the secret in your app with a placeholder value instead, and then the placeholder value is mapped to the actual secret value on the fly by the interceptor (if the app passes Approov attestation). The shipped app code will only contain the placeholder values.

If your app currently uses `your-secret` then replace it in your app with the value `your-placeholder`. Choose a suitable placeholder name to reflect the type of the secret.

You must inform Approov that it should substitute `your-placeholder` with `your-secret` in requests as follows:

```
approov secstrings -addKey your-placeholder -predefinedValue your-secret
```

> Note that this command also requires an [admin role](https://approov.io/docs/latest/approov-usage-documentation/#account-access-roles).

You can add up to 16 different secret values to be substituted in this way.

If the secret value is provided on the header `your-header` then it is necessary to notify the `ApproovService` that the header is subject to substitution. You do this by making the call once, after initialization:

```Java
ApproovService.AddSubstitutionHeader("your-header", null);
```

With this in place the Approov interceptor should replace the `your-placeholder` with the `your-secret` as required when the app passes attestation. Since the mapping lookup is performed on the placeholder value you have the flexibility of providing different secrets on different API calls, even if they are passed with the same header name.

Since earlier released versions of the app may have already leaked `your-secret`, you may wish to refresh the secret at some later point when any older version of the app is no longer in use. You can of course do this update over-the-air using Approov without any need to modify the app.

You can see a [worked example](https://github.com/approov/quickstart-maui-httpclient/blob/master/SHAPES-EXAMPLE.md#shapes-app-with-secrets-protection) for the Shapes app.

If the secret value is provided as a parameter in a URL query string with the name `your-param` then it is necessary to notify the `ApproovService` that the query parameter is subject to substitution. You do this by making the call once, after initialization:

```Java
ApproovService.AddSubstitutionQueryParam("your-param");
```

After this the Approov interceptor should transform any instance of a URL such as `https://your.domain/endpoint?your-param=your-placeholder` into `https://your.domain/endpoint?your-param=your-secret`.

## REGISTERING APPS
In order for Approov to recognize the app as being valid it needs to be registered with the service. Change the directory to the top level of your app project and then register the app with Approov:

```
approov registration -add app/build/outputs/apk/debug/app-debug.apk
```

Note, some versions of Android Studio save the app in `app/build/intermediates/apk/debug/app-debug.apk`.

Note, on Windows you need to substitute \ for / in the above command.

If you are targetting iOS, select the `Archive` option of Visual Studio (this option will not be available if using the simulator for iOS). Enabling codesigning is beyond the scope of this guide, if you need assistance please check [Microsoft's codesigning support](https://docs.microsoft.com/en-us/xamarin/ios/deploy-test/provisioning/). Make sure you have selected the correct project (Shapes.App.iOS), build mode (Release) and target device (Generic Device) settings. Select the `Build` menu and then `Archive for Publishing`. Once the archive file is ready you can either `Ad Hoc`, `Enterprise` or `Play Store` depending on the platform, sign it and save it to disk. Locate the `.ipa` file and register it with the Approov service:

```
$ approov registration -add ShapesApp.ipa
```

> **IMPORTANT:** The registration takes up to 30 seconds to propagate across the Approov Cloud Infrastructure, therefore don't try to run the app again before this time has elapsed. During development of your app you can ensure it [always passes](https://approov.io/docs/latest/approov-usage-documentation/#adding-a-device-security-policy) on your device to not have to register the APK each time you modify it.

[Managing Registrations](https://approov.io/docs/latest/approov-usage-documentation/#managing-registrations) provides more details for app registrations, especially for releases to the Play Store. Note that you may also need to apply specific [Android Obfuscation](https://approov.io/docs/latest/approov-usage-documentation/#android-obfuscation) rules for your app when releasing it.

## HANDLING REJECTIONS
If the app is not recognized as being valid by Approov then an `RejectionException` is thrown on the request and the API call is not completed. The secret value will never be communicated to the app in this case.

Your app should specifically catch this exception and provide some feedback to the user to explain why the app is not working. The `RejectionException` has a `ARC` parameter which provides an [Attestation Response Code](https://approov.io/docs/latest/approov-usage-documentation/#attestation-response-code) which can provide more information about the status of the device, without revealing any details to the end user.

If you wish to provide more direct feedback then enable the [Rejection Reasons](https://approov.io/docs/latest/approov-usage-documentation/#rejection-reasons) feature:

```
approov policy -setRejectionReasons on
```

> Note that this command requires an [admin role](https://approov.io/docs/latest/approov-usage-documentation/#account-access-roles).

You will then be able to use `RejectionReasons` on an `RejectionException` to obtain a comma separated list of [device properties](https://approov.io/docs/latest/approov-usage-documentation/#device-properties) responsible for causing the rejection.

## FURTHER OPTIONS
See [Exploring Other Approov Features](https://approov.io/docs/latest/approov-usage-documentation/#exploring-other-approov-features) for information about additional Approov features you may wish to try.

### Header Prefixes
In some cases the value to be substituted on a header may be prefixed by some fixed string. A common case is the presence of `Bearer` included in an authorization header to indicate the use of a bearer token. In this case you can specify a prefix as follows:

```C#
ApproovService.AddSubstitutionHeader("Authorization", "Bearer ");
```

This causes the `Bearer` prefix to be stripped before doing the lookup for the substitution, and the `Bearer` prefix added to the actual secret value as part of the substitution.

### App Instance Secure Strings
As shown, it is possible to set predefined secret strings that are only communicated to passing apps. It is also possible to get and set secure string values for each app instance. These are never communicated to the Approov cloud service, but are encrypted at rest using keys which can only be retrieved by passing apps.

Here is an example of using the required method in `ApproovService`:

```Java
import io.approov.service.okhttp.ApproovException;
import io.approov.service.okhttp.ApproovNetworkException;
import io.approov.service.okhttp.ApproovRejectionException;

...


String key;
String newDef;
String secret;
// define key and newDef here
try {
    secret = ApproovService.FetchSecureString(key, newDef);
}
catch(ApproovRejectionException e) {
    // failure due to the attestation being rejected, e.ARC and e.RejectionReasons may be used to present information to the user
    // (note e.RejectionReasons is only available if the feature is enabled, otherwise it is always an empty string)
}
catch(ApproovNetworkException e) {
    // failure due to a potentially temporary networking issue, allow for a user initiated retry
}
catch(ApproovException e) {
   // a more permanent error, see e.getMessage()
}
// use `secret` as required, but never cache or store its value - note `secret` will be null if the provided key is not defined
```

to lookup a secure string with the given `key`, returning `null` if it is not defined. Note that you should never cache this value in your code. Approov does the caching for you in a secure way. You may define a new value for the `key` by passing a new value in `newDef` rather than `null`. An empty string `newDef` is used to delete the secure string.

This method is also useful for providing runtime secrets protection when the values are not passed on headers. Secure strings set using this method may also be looked up using subsequent networking interceptor header substitutions. 

### Prefetching
If you wish to reduce the latency associated with substituting the first secret, then make this call immediately after initializing `ApproovService`:

```C#
ApproovService.Prefetch();
```

This initiates the process of fetching the required information as a background task, so that it is available immediately when subsequently needed. Note the information will automatically expire after approximately 5 minutes.

### Prechecking
You may wish to do an early check in your app to present a warning to the user if it is not going to be able to access secrets because it fails the attestation process. Here is an example of calling the appropriate method in `ApproovService`:

```C#
....

try {
    ApproovService.Precheck();
}
catch(RejectionException e) {
    // failure due to the attestation being rejected, e.ARC and e.RejectionReasons may be used to present information to the user
    // (note e.RejectionReasons is only available if the feature is enabled, otherwise it is always an empty string)
}
catch(NetworkingErrorException e) {
    // failure due to a potentially temporary networking issue, allow for a user initiated retry
}
catch(PermanentException e) {
   // a more permanent error, see e.Message
}
// app has passed the precheck
```

> Note you should NEVER use this as the only form of protection in your app, this is simply to provide an early indication of failure to your users as a convenience. You must always also have secrets essential to the operation of your app, or access to backend API services, protected with Approov. This is because, although the Approov attestation itself is heavily secured, it may be possible for an attacker to bypass its result or prevent it being called at all. When the app is dependent on the secrets protected, it is not possible for them to be obtained at all without passing the attestation.
