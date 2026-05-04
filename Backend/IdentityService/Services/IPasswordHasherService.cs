namespace IdentityService.Services
{
    public interface IPasswordHasherService
    {
        string Hash(string password);
        bool Verify(string password, string storedHash);
    }
}
