// See https://aka.ms/new-console-template for more information
using IdentityService.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Client;
using System.Net.Http.Headers;

var configuration = new ConfigurationBuilder()
     .AddJsonFile($"appsettings.json");
var config = configuration.Build();

var certThumbprint = config.GetRequiredSection("TestClient").GetSection("CertificateThumbprint").Value;


var services = new ServiceCollection();

services.AddOpenIddict()

    // Register the OpenIddict client components.
    .AddClient(options =>
    {
        // Allow grant_type=client_credentials to be negotiated.
        options.AllowClientCredentialsFlow();

        // Disable token storage, which is not necessary for non-interactive flows like
        // grant_type=password, grant_type=client_credentials or grant_type=refresh_token.
        options.DisableTokenStorage();

        // Register the System.Net.Http integration and use the identity of the current
        // assembly as a more specific user agent, which can be useful when dealing with
        // providers that use the user agent as a way to throttle requests (e.g Reddit).
        options.UseSystemNetHttp()
               .SetProductInformation(typeof(Program).Assembly);

        // Add a client registration matching the client application definition in the server project.
        options.AddRegistration(new OpenIddictClientRegistration
        {
            Issuer = new Uri("https://localhost:7192/", UriKind.Absolute),
            
            ClientId = "my-console-app",
            ClientSecret = "388D45FA-B36B-4988-BA59-B187D329C207"
        });
    });

await using var provider = services.BuildServiceProvider();

var token = await GetTokenAsync(provider);
Console.WriteLine("Access token: {0}", token);
Console.WriteLine();

//var resource = await GetResourceAsync(provider, token);
//Console.WriteLine("API response 1: {0}", resource);
//Console.ReadLine();

var resource2 = await GetResourceAsyncWithClientCertificate(provider, token, certThumbprint);
Console.WriteLine("API response 2: {0}", resource2);
Console.ReadLine();

static async Task<string> GetTokenAsync(IServiceProvider provider)
{
    var service = provider.GetRequiredService<OpenIddictClientService>();

    var result = await service.AuthenticateWithClientCredentialsAsync(new OpenIddictClientModels.ClientCredentialsAuthenticationRequest()
    {
        Scopes = new List<string> { "my-console-app-scope" }
       
    });
    return result.AccessToken;
}

static async Task<string> GetResourceAsync(IServiceProvider provider, string token)
{
    using var client = provider.GetRequiredService<HttpClient>();
    using var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:7176/api/message");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    using var response = await client.SendAsync(request);
    response.EnsureSuccessStatusCode();

    return await response.Content.ReadAsStringAsync();
}

static async Task<string> GetResourceAsyncWithClientCertificate(IServiceProvider provider, string token, string certThumbprint)
{
    try
    {
        // Load the client certificate
        var clientCertificate = CertificateExtension.GetCertificate(certThumbprint);
        //X509Certificate2 clientCertificate = new X509Certificate2("path/to/client/certificate.pfx", "password");

        // Create an HTTP client with the certificate
        HttpClientHandler handler = new HttpClientHandler();
        handler.ClientCertificates.Add(clientCertificate);
        HttpClient client = new HttpClient(handler);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Call the API endpoint
        HttpResponseMessage response = await client.GetAsync("https://localhost:7176/api/message");

        if (response.IsSuccessStatusCode)
        {
            // Process the successful response
            string responseContent = await response.Content.ReadAsStringAsync();
            return responseContent;
        }
        else
        {
            // Handle authentication failure or other errors
            //Console.WriteLine("Authentication failed: {0}", response.StatusCode);
            return $"Authentication failed: {response.StatusCode}";
        }
    }
    catch (Exception ex)
    {
        //Console.WriteLine("Error: {0}", ex.Message);
        return $"Authentication failed with error: {ex.Message}";
    }
}
