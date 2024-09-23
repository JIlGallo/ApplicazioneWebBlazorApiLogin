using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
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

        public async Task<bool> UpdateUsername(string newUsername)
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            return await UpdateProfileData($"api/auth/{GetUserIdFromToken(token)}/username", new { NewUsername = newUsername }, token);
        }

        public async Task<bool> UpdatePassword(string oldPassword, string newPassword)
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            return await UpdateProfileData($"api/auth/{GetUserIdFromToken(token)}/password", new { OldPassword = oldPassword, NewPassword = newPassword }, token);
        }

        private async Task<bool> UpdateProfileData(string endpointUrl, object requestData, string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PutAsJsonAsync(endpointUrl, requestData);

            if (response.IsSuccessStatusCode)
            {
                // Leggi il nuovo token dalla risposta
                var result = await response.Content.ReadFromJsonAsync<UpdateResult>();
                if (!string.IsNullOrEmpty(result.Token))
                {
                    await SetAuthorizationHeader(result.Token);
                }
                else
                {
                    ((CustomAuthenticationStateProvider)_authenticationStateProvider).RefreshAuthenticationState();
                }
                return true;
            }
            else
            {
                // Gestisci gli errori
                Console.WriteLine($"Errore durante l'aggiornamento del profilo: {response.StatusCode}");
                return false;
            }
        }
        private async Task SetAuthorizationHeader(string token)
        {
            await _localStorage.SetItemAsync("authToken", token);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        private int GetUserIdFromToken(string token)
        {
            try
            {
                // Verifica se il token ha il formato corretto
                var parts = token.Split('.');
                if (parts.Length != 3)
                {
                    throw new FormatException("Formato del token JWT non valido.");
                }

                var payload = parts[1];

                // Sostituisci eventuali caratteri non validi per Base64
                payload = payload.Replace('_', '/').Replace('-', '+');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                // Decodifica il payload Base64 e deserializza il JSON
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                var claims = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                // Cerca il claim "nameid"
                if (claims.TryGetValue("nameid", out object userIdObj) && int.TryParse(userIdObj.ToString(), out int userId))
                {
                    return userId;
                }

                // Se "nameid" non è presente, cerca "sub" (Subject)
                if (claims.TryGetValue("sub", out object subjectObj) && int.TryParse(subjectObj.ToString(), out userId))
                {
                    return userId;
                }

                throw new KeyNotFoundException("Claim 'nameid' o 'sub' non trovato nel token.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante l'estrazione dell'ID utente dal token: {ex.Message}");
                throw; // Potresti voler gestire l'eccezione in modo diverso, ad esempio restituendo 0 o null
            }
        }
    }

    public class LoginResult
    {
        public string Token { get; set; }
    }
    public class UpdateResult
    {
        public string Token { get; set; }
    }
}
