﻿using System.IO;
using System.Security.Cryptography.X509Certificates;
using ActiveLogin.Authentication.BankId.AspNetCore;
using ActiveLogin.Authentication.BankId.AspNetCore.Azure;
using ActiveLogin.Authentication.GrandId.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Standalone.MvcSample
{
    public class Startup
    {
        private readonly IHostingEnvironment _environment;

        public Startup(IConfiguration configuration, IHostingEnvironment environment)
        {
            _environment = environment;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie()
                .AddBankId(builder =>
                {
                    builder.AddSameDevice(BankIdAuthenticationDefaults.SameDeviceAuthenticationScheme, "BankID (SameDevice)", options => { })
                           .AddOtherDevice(BankIdAuthenticationDefaults.OtherDeviceAuthenticationScheme, "BankID (OtherDevice)", options => { });

                    if (Configuration.GetValue("ActiveLogin:BankId:UseDevelopmentEnvironment", false))
                    {
                        builder.UseDevelopmentEnvironment();
                    }
                    else if (Configuration.GetValue("ActiveLogin:BankId:UseTestEnvironment", false))
                    {
                        builder.UseTestEnvironment()
                               .UseClientCertificate(() => new X509Certificate2(Path.Combine(_environment.ContentRootPath, Configuration.GetValue<string>("ActiveLogin:BankId:ClientCertificate"))))
                               .UseRootCaCertificate(Path.Combine(_environment.ContentRootPath, Configuration.GetValue<string>("ActiveLogin:BankId:CaCertificate:FilePath")));
                    }
                    else
                    {
                        builder.UseProductionEnvironment()
                               .UseClientCertificateFromAzureKeyVault(Configuration.GetSection("ActiveLogin:BankId:ClientCertificate"))
                               .UseRootCaCertificate(Path.Combine(_environment.ContentRootPath, Configuration.GetValue<string>("ActiveLogin:BankId:CaCertificate:FilePath")));
                    }
                })
                .AddGrandId(builder =>
                {
                    builder.AddBankIdSameDevice(GrandIdAuthenticationDefaults.BankIdSameDeviceAuthenticationScheme, "GrandID (SameDevice)", options =>
                    {
                        options.GrandIdAuthenticateServiceKey = Configuration.GetValue<string>("ActiveLogin:GrandId:BankIdSameDeviceServiceKey");
                    })
                    .AddBankIdOtherDevice(GrandIdAuthenticationDefaults.BankIdOtherDeviceAuthenticationScheme, "GrandID (OtherDevice)", options =>
                    {
                        options.GrandIdAuthenticateServiceKey = Configuration.GetValue<string>("ActiveLogin:GrandId:BankIdOtherDeviceServiceKey");
                    })
                    .AddBankIdChooseDevice(GrandIdAuthenticationDefaults.BankIdChooseDeviceAuthenticationScheme, "GrandID (ChooseDevice)", options =>
                    {
                        options.GrandIdAuthenticateServiceKey = Configuration.GetValue<string>("ActiveLogin:GrandId:BankIdChooseDeviceServiceKey");
                    });

                    if (Configuration.GetValue("ActiveLogin:GrandId:UseDevelopmentEnvironment", false))
                    {
                        builder.UseDevelopmentEnvironment();
                    }
                    else if (Configuration.GetValue("ActiveLogin:GrandId:UseTestEnvironment", false))
                    {
                        builder.UseTestEnvironment(Configuration.GetValue<string>("ActiveLogin:GrandId:ApiKey"));
                    }
                    else
                    {
                        builder.UseProductionEnvironment(Configuration.GetValue<string>("ActiveLogin:GrandId:ApiKey"));
                    }
                });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseDeveloperExceptionPage();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}