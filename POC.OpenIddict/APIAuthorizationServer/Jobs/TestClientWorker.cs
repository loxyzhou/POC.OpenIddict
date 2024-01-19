using APIAuthorizationServer.Data;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace APIAuthorizationServer.Jobs
{
    public class TestClientWorker : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public TestClientWorker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureCreatedAsync(cancellationToken);

            var appManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
            var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

            var myConsoleAppScopeDescriptor = new OpenIddictScopeDescriptor
            {
                Name = "my-console-app-scope",
                DisplayName = "My Console App Scope",
                Resources = { "my-console-app-resource" }
            };

            await CreateApiClientScope(myConsoleAppScopeDescriptor, scopeManager, cancellationToken);

            var myConsoleAppClient = new OpenIddictApplicationDescriptor
            {
                // Client Id
                ClientId = "my-console-app",
                // Client Secret
                ClientSecret = "388D45FA-B36B-4988-BA59-B187D329C207",
                DisplayName = "My Console App",
                ClientType = ClientTypes.Confidential,
                ApplicationType = ApplicationTypes.Native,
                Permissions =
                    {
                        Permissions.Endpoints.Introspection,
                        Permissions.Endpoints.Token,                // allowed token endpoint                              
                        Permissions.GrantTypes.ClientCredentials,   // allowed client credentials flow
                        Permissions.Prefixes.Scope + myConsoleAppScopeDescriptor.Name
                    }
            };

            await CreateApiClient(myConsoleAppClient, appManager, cancellationToken);
        }

        private async ValueTask CreateApiClientScope(OpenIddictScopeDescriptor scopeDescriptor, IOpenIddictScopeManager scopeManager, CancellationToken cancellationToken)
        {
            if (scopeDescriptor != null && !string.IsNullOrWhiteSpace(scopeDescriptor.Name) && scopeManager != null)
            {
                var scopeInstance = await scopeManager.FindByNameAsync(scopeDescriptor.Name, cancellationToken);

                if (scopeInstance == null)
                {
                    await scopeManager.CreateAsync(scopeDescriptor, cancellationToken);
                }
                else
                {
                    await scopeManager.UpdateAsync(scopeInstance, scopeDescriptor, cancellationToken);
                }
            }
        }

        private async ValueTask CreateApiClient(OpenIddictApplicationDescriptor appDescriptor, IOpenIddictApplicationManager appManager, CancellationToken cancellationToken)
        {
            if (appDescriptor != null && !string.IsNullOrWhiteSpace(appDescriptor.ClientId) && appManager != null)
            {

                var clientInstance = await appManager.FindByClientIdAsync(appDescriptor.ClientId, cancellationToken);

                if (clientInstance == null)
                {
                    await appManager.CreateAsync(appDescriptor, cancellationToken);
                }
                else
                {
                    await appManager.UpdateAsync(clientInstance, appDescriptor, cancellationToken);
                }

            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
