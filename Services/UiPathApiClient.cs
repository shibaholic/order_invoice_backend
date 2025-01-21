using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.OpenApi.Extensions;

namespace HospitalSupply.Services;

public interface IUiPathApiClient
{
    Task<string> StartOrderInvoiceCheck();
}

public record AccessTokenResponse
{
    public string access_token { get; init; }
    public int expires_in { get; init; }
    public string token_type { get; init; }
    public string scope { get; init; }
}

public class UiPathApiClient : IUiPathApiClient
{
    private readonly string _uiPathAccessToken;
    private readonly string _apiTriggerSlug;

    public UiPathApiClient(string uiPathAccessToken, string apiTriggerSlug)
    {
        _uiPathAccessToken = uiPathAccessToken;
        _apiTriggerSlug = apiTriggerSlug;
    }

    public async Task<string> StartOrderInvoiceCheck()
    {
        var client = new HttpClient();
        
        // send request to api trigger
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _uiPathAccessToken);
        var triggerResponse = await client.PostAsync(_apiTriggerSlug, new StringContent("{}", Encoding.UTF8, "application/json"));
        var triggerContent = await triggerResponse.Content.ReadAsStringAsync();
        var triggerStatus = triggerResponse.StatusCode;
        
        Console.WriteLine($"[StartOrderInvoice] status: {triggerStatus}");
        Console.WriteLine($"[StartOrderInvoice] content: {triggerContent}");
        
        triggerResponse.EnsureSuccessStatusCode();
        
        return "";
    }
}