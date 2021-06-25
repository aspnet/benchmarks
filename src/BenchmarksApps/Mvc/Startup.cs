using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography.X509Certificates;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Mvc
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            UseNewtonsoftJson = Configuration["UseNewtonsoftJson"] == "true";
        }

        public IConfiguration Configuration { get; }

        bool UseNewtonsoftJson { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
#if !ONLYAUTH
            var mvcBuilder = services.AddControllers();

            if (UseNewtonsoftJson)
            {
                mvcBuilder.AddNewtonsoftJson();
            }
#endif

#if JWTAUTH
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
            {
                o.TokenValidationParameters.ValidateActor = false;
                o.TokenValidationParameters.ValidateLifetime = true;
                o.TokenValidationParameters.ValidateAudience = true;
                o.TokenValidationParameters.ValidateIssuer = true;
                o.TokenValidationParameters.ValidIssuer = "Test";
                o.TokenValidationParameters.ValidAudience = "test";
                // Note: this key must match what was used to generate the bearer header in the benchmarks.certapi.yml file, if changed, the header should be regenerated
                // This can be done with any of the online jwt generators, issuer and audiance are the only things we validate for now (see above for the expected values)
                o.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String("MFswDQYJKoZIhvcNAQEBBQADSgAwRwJAca32BtkpByiveJTwINuEerWBg2kac7sb"));
            });
#elif CERTAUTH
            services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme).AddCertificate(o =>
            {
                o.AllowedCertificateTypes = CertificateTypes.All;
                o.RevocationFlag = X509RevocationFlag.EntireChain;
                o.RevocationMode = X509RevocationMode.NoCheck;
                o.ValidateCertificateUse = false;
                o.ValidateValidityPeriod = false;
            }).AddCertificateCache();
#elif CUSTOMJWTAUTH
            services.AddAuthentication("Jwt").AddScheme<AuthenticationSchemeOptions, CustomJwtAuth>("Jwt", _ => { });
#elif COOKIEAUTH
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
#endif


#if AUTHORIZE
            services.AddAuthorization();
#endif

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
#if !ONLYAUTH
            if (UseNewtonsoftJson)
            {
                logger.LogInformation("MVC is configured to use Newtonsoft.Json.");
            }

            app.UseRouting();
#endif

#if JWTAUTH || CERTAUTH || CUSTOMJWTAUTH
            logger.LogInformation("MVC is configured to use Authentication.");
            app.UseAuthentication();
#endif

#if AUTHORIZE
            logger.LogInformation("MVC is configured to use Authorization.");
            app.UseAuthorization();
#endif

#if ONLYAUTH
            logger.LogInformation("MVC is configured to skip Endpoints.");

            app.Run(async context =>
            {
                try {

                    // Setting DefaultAuthenticateScheme causes User to be set
                    var user = context.User;
                    var response = context.Response;

#if AUTHORIZE
                    // Deny anonymous request beyond this point.
                    if (user == null || !user.Identities.Any(identity => identity.IsAuthenticated))
                    {
                        // Normally this is a call to challenge
                        response.StatusCode = 401;
                        return;
                    }
#endif

                    response.StatusCode = 200;
                    response.ContentType = "text/plain";
                    await response.WriteAsync("Hello World!");
                }
                catch (Exception e) {
                    logger.LogInformation("Error: "+e);
                }
            });
#else
            logger.LogInformation("MVC is configured to use Endpoints.");
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
#endif
        }
    }
}
