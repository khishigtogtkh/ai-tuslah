using System.Net.Mail;
using AiAgent.Infrastructure.Identity;
using AiAgent.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace AiAgent.Api;

internal static class AdminProvisioner
{
    public static async Task<int> RunAsync(IServiceProvider services, string[] args)
    {
        if (args.Length != 2 || args[0] != "--email" || !MailAddress.TryCreate(args[1], out var address)
            || address.Address != args[1] || args[1].Length > 254)
        {
            Console.Error.WriteLine("Usage: provision-admin --email admin@example.com (password is prompted securely)");
            return 1;
        }
        await using var scope = services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            var user = await users.FindByEmailAsync(args[1]);
            if (user is not null && await users.IsInRoleAsync(user, PlatformRoles.Admin))
            {
                Console.WriteLine("Platform admin already exists. Password was not changed.");
                return 0;
            }
            if (!await roles.RoleExistsAsync(PlatformRoles.Admin))
                Ensure(await roles.CreateAsync(new IdentityRole<Guid>(PlatformRoles.Admin)));
            if (user is null)
            {
                Console.Write("Password (at least 12 characters): ");
                var password = ReadPassword();
                user = new ApplicationUser { Id = Guid.NewGuid(), UserName = args[1], Email = args[1] };
                Ensure(await users.CreateAsync(user, password));
            }
            Ensure(await users.AddToRoleAsync(user, PlatformRoles.Admin));
            await transaction.CommitAsync();
            Console.WriteLine("Platform admin provisioned. Existing passwords are never changed.");
            return 0;
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
        catch (Exception)
        {
            Console.Error.WriteLine("Provisioning failed. Check database availability and applied migrations.");
            return 1;
        }
    }

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException("Provisioning rejected: " + string.Join(", ", result.Errors.Select(x => x.Code)));
    }

    private static string ReadPassword()
    {
        if (Console.IsInputRedirected) return Console.ReadLine() ?? "";
        var value = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return value.ToString(); }
            if (key.Key == ConsoleKey.Backspace) { if (value.Length > 0) value.Length--; }
            else if (!char.IsControl(key.KeyChar)) value.Append(key.KeyChar);
        }
    }
}
