using System;
using System.Globalization;
using System.Security.Claims;
using System.Web.Helpers;
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using Sustainsys.Saml2;
using Sustainsys.Saml2.Configuration;
using Sustainsys.Saml2.Metadata;
using Sustainsys.Saml2.Owin;
using Sustainsys.Saml2.WebSso;

[assembly: OwinStartup("SASimulatorConfiguration", typeof(Edubase.Web.UI.StartupSASimulator))]

namespace Edubase.Web.UI
{
    public class StartupSASimulator
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }

        public void ConfigureAuth(IAppBuilder app)
        {
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login"),
                Provider = new CookieAuthenticationProvider(),
                ExpireTimeSpan = StartupSecureAccess.ConfiguredExpireTimeSpan,
                CookieSecure = CookieSecureOption.SameAsRequest
            });

            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);
            app.UseSaml2Authentication(CreateAuthServicesOptions());
            AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.NameIdentifier;
        }

        private static Saml2AuthenticationOptions CreateAuthServicesOptions()
        {
            var spOptions = CreateSPOptions();
            var authServicesOptions = new Saml2AuthenticationOptions(false)
            {
                SPOptions = spOptions
            };

            var idp = new IdentityProvider(new EntityId("http://dfe-sign-in-simulator.azurewebsites.net/Metadata"), spOptions)
            {
                AllowUnsolicitedAuthnResponse = true,
                Binding = Saml2BindingType.HttpRedirect,
                SingleSignOnServiceUrl = new Uri("http://dfe-sign-in-simulator.azurewebsites.net/")
            };

            authServicesOptions.IdentityProviders.Add(idp);
            new Federation("http://dfe-sign-in-simulator.azurewebsites.net/Federation", true, authServicesOptions);
            return authServicesOptions;
        }

        private static SPOptions CreateSPOptions()
        {
            //var swedish = CultureInfo.GetCultureInfo("sv-se");
            var swedish = "sv-se";

            var organization = new Organization();
            organization.Names.Add(new LocalizedName("Edubase", swedish));
            organization.DisplayNames.Add(new LocalizedName("Edubase", swedish));
            organization.Urls.Add(new LocalizedUri(new Uri("http://www.edubase.gov.uk"), swedish));

            var spOptions = new SPOptions
            {
                EntityId = new EntityId("http://edubase.gov"),
                Organization = organization,
                ReturnUrl = StartupSecureAccess.ExternalAuthDefaultCallbackUrl,
                MinIncomingSigningAlgorithm = "http://www.w3.org/2000/09/xmldsig#rsa-sha1"
            };

            var techContact = new ContactPerson
            {
                Type = ContactType.Technical
            };
            techContact.EmailAddresses.Add("authservices@example.com");
            spOptions.Contacts.Add(techContact);

            var supportContact = new ContactPerson
            {
                Type = ContactType.Support
            };

            supportContact.EmailAddresses.Add("support@example.com");
            spOptions.Contacts.Add(supportContact);

            var attributeConsumingService = new AttributeConsumingService()
            {
                ServiceNames = { new LocalizedName("AuthServices", "en") },
                IsDefault = true,
            };

            attributeConsumingService.RequestedAttributes.Add(
                new RequestedAttribute("urn:someName")
                {
                    FriendlyName = "Some Name",
                    IsRequired = true,
                    NameFormat = RequestedAttribute.AttributeNameFormatUri
                });

            attributeConsumingService.RequestedAttributes.Add(
                new RequestedAttribute("Minimal"));

            spOptions.AttributeConsumingServices.Add(attributeConsumingService);
            
            return spOptions;
        }
    }
}
