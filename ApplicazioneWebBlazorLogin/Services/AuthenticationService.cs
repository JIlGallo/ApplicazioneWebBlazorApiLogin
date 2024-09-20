using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BCrypt.Net;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace ApplicazioneWebBlazorLogin.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage; // Inietta ILocalStorageService
        private readonly AuthenticationStateProvider _authenticationStateProvider;

        public AuthenticationService(HttpClient httpClient, ILocalStorageService localStorage, AuthenticationStateProvider authenticationStateProvider)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
            _authenticationStateProvider = authenticationStateProvider;
        }

        public async Task<string?> Login(string username, string password)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/Auth/authenticate", new { Username = username, Password = password });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResult>();
                await _localStorage.SetItemAsync("authToken", result.Token); // Memorizza il token nel localStorage
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token); // Imposta l'header Authorization
                return result.Token;
            }

            return null;
        }

        public async Task Logout()
        {
            await _localStorage.RemoveItemAsync("authToken");
            _httpClient.DefaultRequestHeaders.Authorization = null;
            // Chiama il metodo pubblico RefreshAuthenticationState
            ((CustomAuthenticationStateProvider)_authenticationStateProvider).RefreshAuthenticationState();
        }
    }

    public class LoginResult
    {
        public string Token { get; set; }
    }
}
