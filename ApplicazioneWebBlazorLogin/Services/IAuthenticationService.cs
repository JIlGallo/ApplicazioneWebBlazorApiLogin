namespace ApplicazioneWebBlazorLogin.Services
{
    public interface IAuthenticationService
    {
        Task<string?> Login(string username, string password);
        Task Logout();
    }
}
