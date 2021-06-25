using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using JsonWebToken;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Mvc
{
    public class CustomJwtAuth : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly static SymmetricJwk _key = new SymmetricJwk(Convert.FromBase64String("MFswDQYJKoZIhvcNAQEBBQADSgAwRwJAca32BtkpByiveJTwINuEerWBg2kac7sb"));
        private JwtReader _reader;
        private static TokenValidationPolicy _policy;

        static CustomJwtAuth()
        {
            _policy = new TokenValidationPolicyBuilder()
                           .RequireSignature(_key, SignatureAlgorithm.HmacSha256)
                           .RequireAudience("test")
                           .RequireIssuer("Test")
                           .Build();
        }

        public CustomJwtAuth(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
            _reader = new JwtReader(_key);
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string authorization = Request.Headers[HeaderNames.Authorization];

            // If no authorization header found, nothing to process further
            if (string.IsNullOrEmpty(authorization))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                authorization = authorization.Substring("Bearer ".Length).Trim();
            }

            // If no token found, no further work possible
            if (string.IsNullOrEmpty(authorization))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var result = _reader.TryReadToken(authorization, _policy);

            if (result.Succedeed)
            {
                var claimsIdentity = new ClaimsIdentity("Jwt");
                var payload = result.Token.Payload;
                claimsIdentity.AddClaim(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/email", (string)payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/email"]));
                claimsIdentity.AddClaim(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", (string)payload[ClaimTypes.Name]));

                var principal = new ClaimsPrincipal(claimsIdentity);
                return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, "Jwt")));
            }
            else
            {
                throw new Exception("Failed! header: " + authorization);
            }
        }
    }
}
