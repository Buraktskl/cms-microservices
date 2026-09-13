using System.Net;
using ContentService.Application.Exceptions;
using ContentService.Application.Interfaces;

namespace ContentService.Infrastructure.HttpClients;

public class UserServiceClient(HttpClient httpClient) : IUserServiceClient
{
    public async Task<bool> UserExistsAsync(Guid userId, string correlationId, CancellationToken ct = default)
    {
        httpClient.DefaultRequestHeaders.Remove("X-Correlation-Id");
        httpClient.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId);

        try
        {
            var response = await httpClient.GetAsync($"/users/{userId}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return false;

            if (response.IsSuccessStatusCode)
                return true;

            throw new UserServiceUnavailableException($"Unexpected status code: {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            throw new UserServiceUnavailableException(ex.Message);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new UserServiceUnavailableException($"Request timed out: {ex.Message}");
        }
    }
}
