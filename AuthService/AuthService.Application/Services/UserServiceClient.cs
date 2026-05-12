using System.Net;
using System.Net.Http.Json;
using AuthService.Application.Contracts;
using AuthService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Services;

public class UserServiceClient : IUserServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserServiceClient> _logger;

    public UserServiceClient(HttpClient httpClient, ILogger<UserServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<InternalUserByEmailResponse?> GetByEmailAsync(string email)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/internal/user/by-email/{Uri.EscapeDataString(email)}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<InternalUserByEmailResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling UserService internal get by email");
            throw;
        }
    }
}

