//************************For Application permission***********************************

//using Microsoft.Extensions.Configuration;
//using Microsoft.Identity.Client;
//using System.Net.Http.Headers;

//var config = new ConfigurationBuilder()
//    .AddUserSecrets<Program>()
//    .Build();

//string tenantId = config["Entra:TenantId"]
//    ?? throw new Exception("TenantId is missing.");

//string clientId = config["Entra:ClientId"]
//    ?? throw new Exception("ClientId is missing.");

//string clientSecret = config["Entra:ClientSecret"]
//    ?? throw new Exception("ClientSecret is missing.");

//string scope = config["Entra:Scope"]
//    ?? throw new Exception("Scope is missing.");

//string apiUrl = config["Api:Url"]
//    ?? throw new Exception("Api:Url is missing.");

//// Create a confidential client application.
//var app = ConfidentialClientApplicationBuilder
//    .Create(clientId)
//    .WithClientSecret(clientSecret)
//    .WithAuthority(
//        $"https://login.microsoftonline.com/{tenantId}")
//    .Build();

//try
//{
//    // Step 1: Get an application access token.
//    var tokenResult = await app
//        .AcquireTokenForClient(new[] { scope })
//        .ExecuteAsync();

//    Console.WriteLine("Access token obtained successfully.");
//    Console.WriteLine($"Token expires: {tokenResult.ExpiresOn}");

//    //// Step 2: Call the protected API.
//    using var httpClient = new HttpClient();

//    httpClient.DefaultRequestHeaders.Authorization =
//        new AuthenticationHeaderValue(
//            "Bearer", tokenResult.AccessToken);

//    var response = await httpClient.GetAsync(apiUrl);
//    var responseBody = await response.Content.ReadAsStringAsync();

//    Console.WriteLine($"API status: {(int)response.StatusCode} " +
//                      response.StatusCode);

//    Console.WriteLine("API response:");
//    Console.WriteLine(responseBody);
//    Console.ReadLine();
//}
//catch (MsalServiceException ex)
//{
//    Console.WriteLine("Token acquisition failed.");
//    Console.WriteLine($"Error: {ex.ErrorCode}");
//    Console.WriteLine(ex.Message);
//    Console.ReadLine();

//}
//catch (HttpRequestException ex)
//{
//    Console.WriteLine("API request failed.");
//    Console.WriteLine(ex.Message);
//    Console.ReadLine();

//}


//************************For Delegated permission***********************************

using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

string tenantId = config["Entra:TenantId"]
    ?? throw new Exception("TenantId is missing.");

string clientId = config["Entra:ClientId"]
    ?? throw new Exception("ClientId is missing.");

string scope = config["Entra:Scope"]
    ?? throw new Exception("Scope is missing.");

string apiUrl = config["Api:Url"]
    ?? throw new Exception("Api:Url is missing.");

// Public client: no client secret.
var app = PublicClientApplicationBuilder
    .Create(clientId)
    .WithAuthority(
        $"https://login.microsoftonline.com/{tenantId}")
    .WithDefaultRedirectUri()
    .Build();

try
{
    // Sign in the user and request delegated permission.
    var authResult = await app
        .AcquireTokenInteractive(new[] { scope })
        .ExecuteAsync();

    Console.WriteLine("User signed in successfully.");
    Console.WriteLine($"User: {authResult.Account?.Username}");

    using var httpClient = new HttpClient();

    httpClient.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue(
            "Bearer", authResult.AccessToken);

    var response = await httpClient.GetAsync(apiUrl);
    var responseBody = await response.Content.ReadAsStringAsync();

    Console.WriteLine(
        $"API status: {(int)response.StatusCode} {response.StatusCode}");

    Console.WriteLine(responseBody);
    Console.ReadLine();
}
catch (MsalException ex)
{
    Console.WriteLine($"Authentication error: {ex.ErrorCode}");
    Console.WriteLine(ex.Message);
    Console.ReadLine();
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"API request error: {ex.Message}");
    Console.ReadLine();
}