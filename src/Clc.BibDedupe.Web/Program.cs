using Clc.Polaris.Api;
using Clc.Polaris.Api.Configuration;
using System.Data;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Clc.BibDedupe.Web.Data;
using Clc.BibDedupe.Web.Services;
using Clc.BibDedupe.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using System.Threading.Tasks;
using Clc.BibDedupe.Web.Extensions;

namespace Clc.BibDedupe.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration
                .SetBasePath(builder.Environment.ContentRootPath)
                .AddJsonFile("Config/settings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"Config/settings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            IPapiSettings? papiConfig = builder.Configuration.GetSection("Papi").Get<PapiSettings>();

            if (papiConfig is null)
            {
                builder.Services.AddSingleton<IRecordLoader, TestFileRecordLoader>();
            }
            else
            {
                builder.Services
                    .AddSingleton(papiConfig)
                    .AddSingleton<IPapiClient, PapiClient>()
                    .AddSingleton<IRecordLoader, PapiRecordLoader>();
            }

            var bibDedupeConn = builder.Configuration.GetConnectionString("BibDedupeDb");

            if (string.IsNullOrWhiteSpace(bibDedupeConn))
            {
                builder.Services.AddSingleton<IBibDupePairRepository, TestFileBibDupePairRepository>();
            }
            else
            {
                builder.Services
                    .AddScoped<IDbConnection>(sp => new SqlConnection(bibDedupeConn))
                    .AddScoped<IBibDupePairRepository, BibDupePairRepository>();
            }

            var authorizedUsers = builder.Configuration.GetSection("AuthorizedUsers").Get<string[]>();

            if (authorizedUsers is not null && authorizedUsers.Length > 0)
            {
                builder.Services.AddSingleton<IUserAuthorizationService>(new ListUserAuthorizationService(authorizedUsers));
            }
            else
            {
                builder.Services.AddSingleton<IUserAuthorizationService, SqlUserAuthorizationService>();
            }

            builder.Services
                .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
                .AddSingleton<IDecisionStore, SessionDecisionStore>()
                .AddSingleton<ICurrentPairStore, SessionCurrentPairStore>();

            builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

            builder.Services.PostConfigure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                var previous = options.Events.OnRedirectToIdentityProvider;

                options.Events.OnRedirectToIdentityProvider = async context =>
                {
                    if (previous is not null)
                    {
                        await previous(context);
                    }

                    if (context.Properties?.Items.TryGetValue("prompt", out var prompt) == true)
                    {
                        context.ProtocolMessage.Prompt = prompt;
                    }
                };
            });

            builder.Services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    string? userName = null;
                    var principal = context.HttpContext.User;

                    if (principal?.Identity?.IsAuthenticated == true)
                    {
                        userName = principal.GetEmail();

                        if (string.IsNullOrEmpty(userName))
                        {
                            userName = principal.FindFirst(ClaimTypes.Upn)?.Value ??
                                       principal.FindFirst(ClaimTypes.Name)?.Value ??
                                       principal.Identity?.Name;
                        }
                    }

                    context.HttpContext.Session.SetAuthMessage(
                        "You are not authorized to access this application.",
                        userName);
                    context.Response.Redirect("/");
                    return Task.CompletedTask;
                };
            });

            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession();

            builder.Services.AddSingleton<IAuthorizationHandler, AuthorizedUserHandler>();
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AuthorizedUser", policy =>
                    policy.RequireAuthenticatedUser().AddRequirements(new AuthorizedUserRequirement()));
            });


            // Add services to the container.
            builder.Services.AddControllersWithViews()
                .AddMicrosoftIdentityUI();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
