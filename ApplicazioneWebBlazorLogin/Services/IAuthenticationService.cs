namespace ApplicazioneWebBlazorLogin.Services
{
    public interface IAuthenticationService
    {
        Task<string?> Login(string username, string password);
        Task<bool> UpdateUsername(string newUsername);
        Task<bool> UpdatePassword(string oldPassword, string newPassword);
        Task Logout();
    }
}
