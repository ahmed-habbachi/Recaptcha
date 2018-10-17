# Getting started

## Get an API key

First you need to get an API key from google at [https://www.google.com/recaptcha/admin](http://www.google.com/recaptcha/admin) and you can refer to [https://developers.google.com/recaptcha/intro](https://developers.google.com/recaptcha/intro) for more documentation about reCAPTCHA.

## Install the package

Will be writen down in future.

## Configure the service

 In your ["Startup"](https://docs.asp.net/en/latest/fundamentals/startup.html) class add the "RecaptchaService" in the ["ConfigureServices"](https://docs.asp.net/en/latest/fundamentals/startup.html#the-configureservices-method) method:

```csharp
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc();

        services.AddRecaptcha(new RecaptchaOptions
        {
            SiteKey = Configuration["Recaptcha:SiteKey"],
            SecretKey = Configuration["Recaptcha:SecretKey"]
        });
    }
```

The "SiteKey" and "SecretKey" are required (you get those from [your google admin console](https://www.google.com/recaptcha/admin)).  
For development I recommend that you use the [user secrets configuration extension](https://docs.asp.net/en/latest/security/app-secrets.html) to store those settings to avoid committing them to source control.

## Using the control

The control is a [TagHelper](https://docs.asp.net/en/latest/mvc/views/tag-helpers/intro.html), so the first thing you need to do is include the TagHelpers from this package in your ["_viewImport.cshtml"](https://docs.asp.net/en/latest/mvc/views/tag-helpers/intro.html#managing-tag-helper-scope) file.

```csharp
    @addTagHelper *, CaseTunisia.Recaptcha
```

Then you can just add the TagHelper to place the reCAPTCHA control

```html
    <recaptcha/>
```

and in your script section

```html
    <script src="~/lib/jquery/dist/jquery.js"></script>
    <script src="~/lib/jquery-validation/dist/jquery.validate.js"></script>
    <script src="~/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js"></script>
    <recaptcha-script/>
```

The TagHelper "recaptcha-script" is required, it adds the script reference for the control, out of the box it integrated with jquery validation and need to be place just after those scripts. You can disable jquery validation if you want to like this:

```html
    <recaptcha-script jquery-validation="false" />
```

## Validating the reCaptcha response in your controller

Apply the "ValidateRecaptcha" attribute on your action and check if the "ModelState" is valid.

```csharp
[ValidateRecaptcha]
[HttpPost]
public IActionResult Index(YourViewModel viewModel)
{
    if (ModelState.IsValid)
    {
        return new OkResult();
    }

    return View();
}
```

## Show reCAPTCHA after a configurable number of fail atempt

First make sure to add to your configuration settings the "FailAttemptBeforeCaptcha" and make sure it is an int value, add it to your **RecaptchaOptions**:

```csharp
    public void ConfigureServices(IServiceCollection services)
    {
        ...
        services.AddRecaptcha(new RecaptchaOptions
        {
            SiteKey = Configuration["Recaptcha:SiteKey"],
            SecretKey = Configuration["Recaptcha:SecretKey"],
            FailAttemptBeforeCaptcha = int.Parse(Configuration["Recaptcha:FailAttemptBeforeCaptcha"])
        });
        ...
    }
```

Second create a *[TempData]* variable to hold the number of fails in your **AccountController** and make it spin in your **post Login** Action, down bellow you'll see me commparing the failed count and the configured fail attempt value and passing it to a property in the model, you can do the same or pass the value with a **ViewData**:

```csharp
    public class AccountController : Controller
    {
        ...

        [TempData]
        private int FailCount { get; set; }
        ...
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [ValidateRecaptcha]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ...
            FailCount++;
            model.ShowCaptcha = FailCount > _recaptchaConfigurationService.FailAttemptBeforeCaptcha;
            ...
    }
```

Last but not least we need to tweak the reCAPTCHA tag helper by adding an attribute  **asp-isvisible** like following:

```html
    <recaptcha class="g-recaptcha" asp-isvisible="@Model?.ShowCaptcha != null ? Model.ShowCaptcha : false" />
```

the **asp-isvisible** attribute must have a *boolean* value as you can conclude, and that's it.

## Localization support

You can localize the recaptcha control in two ways:

* By setting the language code when adding the "RecaptchaService":

```csharp
    public void ConfigureServices(IServiceCollection services)
    {
        ...
        services.AddRecaptcha(new RecaptchaOptions
        {
            SiteKey = Configuration["Recaptcha:SiteKey"],
            SecretKey = Configuration["Recaptcha:SecretKey"],
            LanguageCode = "en-US"
        });
        ...
    }
```

* By using the aspnetcore [localization middleware](https://docs.asp.net/en/latest/fundamentals/localization.html):

```csharp
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            ...
            var supportedCultures = new[]
            {
                  new CultureInfo("en-US"),
                  new CultureInfo("en-AU"),
                  new CultureInfo("en-GB"),
                  new CultureInfo("en"),
                  new CultureInfo("es-ES"),
                  new CultureInfo("es-MX"),
                  new CultureInfo("es"),
                  new CultureInfo("fr-FR"),
                  new CultureInfo("fr")
            };

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en-US"),
                // Formatting numbers, dates, etc.
                SupportedCultures = supportedCultures,
                // UI strings that we have localized.
                SupportedUICultures = supportedCultures
            });

            app.UseMvc(routes =>
            {
                ...
            });
        }
```

**NOTE**: Only the language codes (languagecode2-country/regioncode2 format) [supported by google](https://developers.google.com/recaptcha/docs/language) will work, but it does fallback if you provide a code that is not supported.

## Known issues

## Tab index is not working

the "data-tabindex" attribute offered by the [google api](https://developers.google.com/recaptcha/docs/display#render_param) doesn't seem to work.

Use this [workaround](http://stackoverflow.com/a/28637691/6524718), from [Alexander O'Mara](http://stackoverflow.com/users/3155639/alexander-omara), to fix it:

```html
<script>
//Loop over the .g-recaptcha wrapper elements.
Array.prototype.forEach.call(document.getElementsByClassName('g-recaptcha'), function(element) {
    //Add a load event listener to each wrapper, using capture.
    element.addEventListener('load', function(e) {
        //Get the data-tabindex attribute value from the wrapper.
        var tabindex = e.currentTarget.getAttribute('data-tabindex');
        //Check if the attribute is set.
        if (tabindex) {
            //Set the tabIndex on the iframe.
            e.target.tabIndex = tabindex;
        }
    }, true);
});
</script>
```