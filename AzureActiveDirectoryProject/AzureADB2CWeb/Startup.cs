using AzureADB2CWeb.Data;
using AzureADB2CWeb.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AzureADB2CWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public static string Tenant = "subinb2c.onmicrosoft.com";
        public static string AzureADB2CHostName = "subinb2c.b2clogin.com";
        public static string ClientID = "b59ba744-ea0b-4b01-a6aa-d7894e136310";
        public static string PolicySignUpSignIn = "B2C_1_signin_up";
        public static string PolicyEditProfile = "B2C_1_Edit";
        public static string Scope = "https://subinb2c.onmicrosoft.com/azureB2CAPI/fullaccess";
        public static string ClientSecret = "Nfh7Q~EBWIqN6aM4uTBscwiHj.Uj~4NDD8mV4";
        public static string AuthorityBase = $"https://{AzureADB2CHostName}/{Tenant}/";
        public static string AuthoritySignInUp = $"{AuthorityBase}{PolicySignUpSignIn}/v2.0";
        public static string AuthorityEditProfile = $"{AuthorityBase}{PolicyEditProfile}/v2.0";

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddDistributedMemoryCache();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromDays(1);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            services.AddHttpClient();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
            });

            services.AddHttpContextAccessor();
            services.AddScoped<IUserService, UserService>();

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            }).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.Authority = Startup.AuthoritySignInUp;
                options.ClientId = Startup.ClientID;
                options.ResponseType = "code"; //id_token - implicit flow,code - auth code flow
                options.SaveTokens = true;
                options.ClientSecret = Startup.ClientSecret;
                //options.Scope.Add(options.ClientId);
                options.Scope.Add(Startup.Scope); //For B2C API Expose
                //options.Scope.Add("api://ba588b5a-9d92-4db7-a47b-3c8cbac4cce1/AdminAccess");
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                {
                    NameClaimType = "name" // copies the value from 'name' claim to User.Identity.Name
                };
                //to add a role claim - for support --> [Authorize(Roles=sdf)]
                options.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = async opt =>
                    {
                        string role = opt.Principal.FindFirstValue("extension_UserRole");

                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Role,role)
                        };

                        var appIdentity = new ClaimsIdentity(claims);
                        opt.Principal.AddIdentity(appIdentity);
                    }
                };
            })
            .AddOpenIdConnect("B2C_1_Edit",GetOpenIdConnectOptions("B2C_1_Edit"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }


        private Action<OpenIdConnectOptions> GetOpenIdConnectOptions(string policy) => options =>
        {
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.Authority = Startup.AuthorityEditProfile;
            options.ClientId = Startup.ClientID;
            options.ResponseType = "code"; //id_token - implicit flow,code - auth cae flow
            options.SaveTokens = true;
            options.ClientSecret = Startup.ClientSecret;
            //options.Scope.Add(options.ClientId);
            options.Scope.Add(Startup.Scope);// for b2c api expose
            options.CallbackPath = "/signin-oidc-" + policy;
            //options.Scope.Add("api://ba588b5a-9d92-4db7-a47b-3c8cbac4cce1/AdminAccess");
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
            {
                NameClaimType = "name" // copies the value from 'name' claim to User.Identity.Name
            };

            
        };
    }
}
