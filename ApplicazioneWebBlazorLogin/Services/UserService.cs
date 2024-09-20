namespace ApplicazioneWebBlazorLogin.Services
{
    public class UserService
    {
        private readonly HttpClient _httpClient;

        public UserService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetUsernameByIdAsync(int userId)
        {
            var response = await _httpClient.GetAsync($"/api/Auth/{userId}/username");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                // Gestisci gli errori (es. log, messaggio all'utente)
                Console.WriteLine($"Errore durante il recupero del nome utente: {response.StatusCode}");
                return null;
            }
        }
    }
}
