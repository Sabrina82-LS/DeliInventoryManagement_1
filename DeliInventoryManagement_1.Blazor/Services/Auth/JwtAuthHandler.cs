using System.Net.Http.Headers;

namespace DeliInventoryManagement_1.Blazor.Services.Auth;

public sealed class JwtAuthHandler : DelegatingHandler
{
    private readonly AuthState _state;

    public JwtAuthHandler(AuthState state) => _state = state;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_state.Token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _state.Token);

        return base.SendAsync(request, cancellationToken);
    }
}