using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using System.Net.Security;

var builder = WebApplication.CreateBuilder(args);

string[] allowedThumbprints = new string[] { "4FEA5767CD0071995A88F25F0D0A5B4A04CD4EB6" };

//Client Certificate Validation
//Client Certificate Creation in PowerShell
//https://stackoverflow.com/questions/75473378/required-client-certificate-asp-net-core-web-api

 //The following code cannot be used together with OpenIddict library to validate client certifcate
builder.WebHost.UseKestrel(options =>
{
    options.ConfigureHttpsDefaults(opt =>
    {
        opt.ClientCertificateMode = ClientCertificateMode.RequireCertificate;

        opt.CheckCertificateRevocation = false;
        opt.ClientCertificateValidation = (certificate, chain, errors) =>
        {
            // Check if the certificate is valid
            if (errors != SslPolicyErrors.None)
            {
                return false;
            }

            // Validate the certificate thumbprint
            return allowedThumbprints.Contains(certificate.Thumbprint, StringComparer.OrdinalIgnoreCase);
        };
    });
});


/*
//THE FOLLOWING CODE IS ONLY USED FOR Client Certificate Authentication that cannot be used together with OpenIddict
builder.Services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
    .AddCertificate(options =>
    {
        // Specify the allowed certificate thumbprints
        string[] allowedThumbprints = new string[] {
            "4FEA5767CD0071995A88F25F0D0A5B4A04CD4EB6", // Replace with actual thumbprints
            // Add more thumbprints if needed
        };

        options.RevocationMode = X509RevocationMode.NoCheck;
        options.AllowedCertificateTypes = CertificateTypes.All;
        options.Events = new CertificateAuthenticationEvents
        {
            OnCertificateValidated = context =>
            {
                // Validate the thumbprint
                X509Certificate2 clientCertificate = context.ClientCertificate;
                bool isValid = allowedThumbprints.Contains(clientCertificate.Thumbprint.ToUpper());

                if (!isValid)
                {
                    context.Fail("Invalid client certificate.");
                }

                return Task.CompletedTask;
            },

            OnAuthenticationFailed = context =>
            {
                context.Fail("Invalid client certificate.");
                return Task.CompletedTask;
            }
        };
    });
*/

builder.Services.AddOpenIddict()
                .AddValidation(options =>
                {
                    // Note: the validation handler uses OpenID Connect discovery
                    // to retrieve the address of the introspection endpoint.
                    options.SetIssuer("https://localhost:7192/");
                    options.AddAudiences(new string[] { "my-console-app-resource" });

                    // Register the System.Net.Http integration.
                    options.UseSystemNetHttp();

                    // Register the ASP.NET Core host.
                    options.UseAspNetCore();
                });

//var certThumbprint = builder.Configuration["OpenIddict:CertificateThumbprint"];
//var cert = CertificateExtension.GetCertificate(certThumbprint);

//var tokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
//{
//    ValidIssuer = builder.Configuration["Jwt:Issuer"],
//    ValidAudience = builder.Configuration["Jwt:Audience"],
//    NameClaimType = OpenIdConnectConstants.Claims.Name,
//    RoleClaimType = OpenIdConnectConstants.Claims.Role,
//    ValidateAudience = false,
//    ValidateIssuer = false,
//    ValidateLifetime = false,
//    ValidateIssuerSigningKey = true,
//    TokenDecryptionKey = new Microsoft.IdentityModel.Tokens.X509SecurityKey(cert),
//    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.X509SecurityKey(cert)
//};




builder.Services.AddAuthorization();
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
/*
 * The following code is to create swagger bearer token input box
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "API Resource", Version = "v1" });
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });

    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});
*/

//The following code is to define oauth scope and api authentication authority endpoint
builder.Services.AddSwaggerGen(
        c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "API Resource", Version = "v1" });

            c.AddSecurityDefinition(
                "oauth",
                new OpenApiSecurityScheme
                {
                    Flows = new OpenApiOAuthFlows
                    {
                        ClientCredentials = new OpenApiOAuthFlow
                        {
                            Scopes = new Dictionary<string, string>
                            {
                                ["my-console-app-scope"] = "api scope description"
                            },
                            TokenUrl = new Uri("https://localhost:7192/connect/token"),
                        },
                    },
                    In = ParameterLocation.Header,
                    Name = HeaderNames.Authorization,
                    Type = SecuritySchemeType.OAuth2
                }
            );
            c.AddSecurityRequirement(
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth" },
                        },
                        new[] { "my-console-app-scope" }
                    }
                }
            );
        }
    );

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    //app.UseSwaggerUI();
    //The following code is to auto fill up client id, secret and scope for api authorization before making any actual api call
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Resource v1");
        c.OAuthClientId("my-console-app");
        c.OAuthClientSecret("388D45FA-B36B-4988-BA59-B187D329C207");
        c.OAuthScopes("my-console-app-scope");
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
