using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using System.Security.Claims;
using IdentityService.Shared;

namespace APIAuthorizationServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthorizationController : ControllerBase
    {

        private readonly ILogger<AuthorizationController> _logger;
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly IOpenIddictScopeManager _scopeManager;

        public AuthorizationController(ILogger<AuthorizationController> logger,
                                               IOpenIddictApplicationManager applicationManager,
                                               IOpenIddictScopeManager scopeManager)
        {
            _logger = logger;
            _applicationManager = applicationManager;
            _scopeManager = scopeManager;
        }

        [HttpPost("~/connect/token"), IgnoreAntiforgeryToken, Consumes("application/x-www-form-urlencoded"), Produces("application/json")]
        public async Task<IActionResult> Exchange()
        {

            try
            {
                var request = HttpContext.GetOpenIddictServerRequest();
                ClaimsPrincipal claimsPrincipal;

                if (request is null)
                {
                    return BadRequest(new
                    {
                        error = Errors.InvalidRequest,
                        error_description = "The request is not a valid."
                    });
                }

                if (!request.IsClientCredentialsGrantType())
                {
                    return BadRequest(new
                    {
                        error = Errors.UnsupportedGrantType,
                        error_description = "The grand type of the request is not valid."
                    });
                }

                if (string.IsNullOrEmpty(request.ClientId) || string.IsNullOrEmpty(request.ClientSecret))
                {
                    return BadRequest(new
                    {
                        error = Errors.InvalidClient,
                        error_description = "The client id or client secret is not valid."
                    });
                }

                var application = await _applicationManager.FindByClientIdAsync(request.ClientId);
                if (application == null)
                {
                    return BadRequest(new
                    {
                        Error = Errors.InvalidClient,
                        error_description = "The client id or client secret is not valid."
                    });
                }

                //var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, 
                                                  Claims.Name, Claims.Role);

                var clientId = await _applicationManager.GetClientIdAsync(application);            
                // Subject (sub) is a required field, we use the client id as the subject identifier here.
                identity.AddClaim(Claims.Subject, clientId?? request.ClientId, Destinations.AccessToken);

                var clientName = await _applicationManager.GetDisplayNameAsync(application);
                // Don't forget to add destination otherwise it won't be added to the access token.
                if (!string.IsNullOrWhiteSpace(clientName))
                {
                    identity.AddClaim(Claims.Name, clientName, Destinations.AccessToken);
                }

                var resources = await _scopeManager.ListResourcesAsync(request.GetScopes()).ToListAsync();
                identity.SetScopes(request.GetScopes());
                identity.SetResources(resources);

                claimsPrincipal = new ClaimsPrincipal(identity);

                return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
            catch (Exception)
            {

                return BadRequest(new
                {
                    Error = Errors.ServerError,
                    ErrorDescription = "An internal error occurred."
                });
            }
        }
    }
}