using ClimateHub.Modules.IAM.Application;
using Microsoft.AspNetCore.Identity;

namespace ClimateHub.Modules.IAM.Infrastructure;

public class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<object> _passwordHasher = new();

    public string HashPassword(string password)
    {
        return _passwordHasher.HashPassword(null!, password);
    }

    public PasswordHashResult VerifyHashedPassword(string hashedPassword, string providedPassword)
    {
        var result = _passwordHasher.VerifyHashedPassword(null!, hashedPassword, providedPassword);
        return result switch
        {
            Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success => PasswordHashResult.Success,
            Microsoft.AspNetCore.Identity.PasswordVerificationResult.SuccessRehashNeeded => PasswordHashResult.Success,
            _ => PasswordHashResult.Failed
        };
    }
}
