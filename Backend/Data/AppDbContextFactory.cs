using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Spot4Hire.Backend.Data;

// Used ONLY by EF Core tooling (migrations add / database update) at design time,
// when the Aspire AppHost is not running to supply the connection string.
// Override with the SPOT4HIRE_DB environment variable if your local server differs.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("SPOT4HIRE_DB")
            ?? "Data Source=localhost;Initial Catalog=Spot4Hire;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString, sql => sql.UseNetTopologySuite())
            .Options;

        return new AppDbContext(options);
    }
}
