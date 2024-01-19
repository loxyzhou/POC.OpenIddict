using APIAuthorizationServer;
using APIAuthorizationServer.Data;
using APIAuthorizationServer.Jobs;
using IdentityService.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Quartz;

/*
 * 
 * https://wezmag.github.io/posts/protect-api-with-client-credentials-grant-using-openiddict-part-1/
 * https://wezmag.github.io/posts/protect-api-with-client-credentials-grant-using-openiddict-part-2/
 */

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(configuration.GetConnectionString("DefaultDbConnection"));
    options.UseOpenIddict();
});

// OpenIddict offers native integration with Quartz.NET to perform scheduled tasks
// (like pruning orphaned authorizations/tokens from the database) at regular intervals.
//builder.Services.AddQuartz(options =>
//{
//    options.UseSimpleTypeLoader();
//    options.UseInMemoryStore();
//});

// Register the Quartz.NET service and configure it to block shutdown until jobs are complete.
//builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

builder.Services.AddOpenIddict()

            // Register the OpenIddict core components.
            .AddCore(options =>
            {
                // Configure OpenIddict to use the Entity Framework Core stores and models.
                // Note: call ReplaceDefaultEntities() to replace the default OpenIddict entities.
                options.UseEntityFrameworkCore()
                       .UseDbContext<AppDbContext>();
            })

            // Register the OpenIddict server components.
            .AddServer(options =>
            {
                // Enable the client credentials flow.
                options.AllowClientCredentialsFlow();

                // Enable the token endpoint.
                options.SetTokenEndpointUris("connect/token");
                options.SetIntrospectionEndpointUris("connect/introspect");

                // Registering scopes
                options.RegisterScopes("my-console-app");

                // Register the signing and encryption credentials.

                if (!builder.Environment.IsDevelopment())
                {
                    options.AddDevelopmentEncryptionCertificate();
                    options.AddDevelopmentSigningCertificate();
                    options.DisableAccessTokenEncryption();
                }
                else
                {
                    //register real certificate here
                    var certThumbprint = builder.Configuration["OpenIddict:CertificateThumbprint"];
                    var cert = CertificateExtension.GetCertificate(certThumbprint);

                    options.AddEncryptionCertificate(cert);
                    options.AddSigningCertificate(cert);
                }
                options.DisableAccessTokenEncryption();
                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(5));

                // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
                options.UseAspNetCore().EnableTokenEndpointPassthrough();
            });

// Register the OpenIddict validation components.
// It's only required when authorisation server provides resource API access. 
//.AddValidation(options =>
//{
//    // Import the configuration from the local OpenIddict server instance.
//    // It's only used then resource api that is part of authorisation server instance.
//    options.UseLocalServer();

//    // Register the ASP.NET Core host.
//    options.UseAspNetCore();
//});

/*
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder =>
        {
            builder.WithOrigins("https://localhost:7176") // Specify the allowed origin
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});
*/

builder.Services.AddCors();
builder.Services.AddHostedService<TestClientWorker>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseMiddleware<ContentTypeMiddleware>();
//app.UseCors("AllowSpecificOrigin"); // Use the CORS policy
app.UseCors(x => x
                .WithMethods("POST")
                .AllowAnyHeader()
                .SetIsOriginAllowed(origin => true) // allow any origin
                .AllowCredentials());


app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
//app.UseStaticFiles();

app.MapControllers();

app.Run();
