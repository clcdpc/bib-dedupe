using Clc.Polaris.Api;
using Clc.Polaris.Api.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;
using Clc.BibDedupe.Web.Data;
using Clc.BibDedupe.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

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
                builder.Services.AddSingleton<IRecordXmlLoader, TestFileRecordXmlLoader>();
            }
            else
            {
                builder.Services
                    .AddSingleton(papiConfig)
                    .AddSingleton<IPapiClient, PapiClient>()
                    .AddSingleton<IRecordXmlLoader, PapiRecordXmlLoader>();
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

            builder.Services
                .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
                .AddSingleton<IDecisionStore, SessionDecisionStore>();

            builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession();


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
