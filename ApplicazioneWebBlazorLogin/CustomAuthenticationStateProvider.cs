using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using System.Net.Http.Headers;
using System.Text;

namespace ApplicazioneWebBlazorLogin 
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;
        private readonly HttpClient _httpClient;

        public CustomAuthenticationStateProvider(ILocalStorageService localStorage, HttpClient httpClient)
        {
            _localStorage = localStorage;
            _httpClient = httpClient;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");

            if (string.IsNullOrEmpty(token))
            {
                return NotAuthenticated();
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return Authenticated(token);
        }

        public async Task Login(string token)
        {
            await _localStorage.SetItemAsync("authToken", token); // Memorizza il token nel localStorage
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token); // Imposta l'header Authorization
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync()); // Notifica la modifica dello stato
        }

        public async Task Logout()
        {
            await _localStorage.RemoveItemAsync("authToken");
            _httpClient.DefaultRequestHeaders.Authorization = null;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        private AuthenticationState Authenticated(string token)
        {
            var claims = ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);
            return new AuthenticationState(user);
        }

        private AuthenticationState NotAuthenticated()
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            try
            {
                // Verifica se il token ha il formato corretto con tre parti separate da punti
                var parts = jwt.Split('.');
                if (parts.Length == 3)
                {
                    var payload = parts[1];

                    // Sostituisci eventuali caratteri non validi per Base64
                    payload = payload.Replace('_', '/').Replace('-', '+');
                    switch (payload.Length % 4)
                    {
                        case 2: payload += "=="; break;
                        case 3: payload += "="; break;
                    }

                    var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                    var claims = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    return claims.Select(claim => new Claim(claim.Key, claim.Value.ToString()));
                }
                else
                {
                    // Gestisci il caso in cui il token non ha il formato previsto
                    Console.WriteLine("Formato del token JWT non valido.");
                    return Enumerable.Empty<Claim>();
                }
            }
            catch (Exception ex)
            {
                // Gestisci eventuali eccezioni durante la decodifica del token
                Console.WriteLine($"Errore durante la decodifica del token JWT: {ex.Message}");
                return Enumerable.Empty<Claim>();
            }
        }
    }
}