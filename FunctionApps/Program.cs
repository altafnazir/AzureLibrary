using FunctionApps.Facade;
using FunctionApps.Functions.Model;
using FunctionApps.Properties.Services;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.ConfigureFunctionsWebApplication();

builder.Services.Configure<RetryPolicySettings>(
    builder.Configuration.GetSection("RetryPolicies"));

builder.Services.AddSingleton<IRetryPolicyProvider, RetryPolicyProvider>();

builder.Build().Run();
