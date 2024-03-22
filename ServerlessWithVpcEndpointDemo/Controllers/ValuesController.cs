using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using Microsoft.Data.SqlClient;
using Amazon.SecretsManager;
using Amazon;
using Amazon.SecretsManager.Model;
using System.Data;

namespace ServerlessWithVpcEndpointDemo.Controllers;

[Route("api/[controller]")]
public class ValuesController : ControllerBase
{
    [HttpGet]
    public async Task<Product?> Get()
    {
        var connectionString = await GetSecret();

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = "SELECT * FROM [Products] WHERE [Id] = 1";
        command.Connection = connection;

        var result = await command.ExecuteScalarAsync() as Product;

        return result;
    }

    private static async Task<string> GetSecret()
    {
        var secretName = "MyAppDbConnectionString";
        var region = "eu-west-2";

        var client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));

        var request = new GetSecretValueRequest
        {
            SecretId = secretName,
            VersionStage = "AWSCURRENT"
        };

        GetSecretValueResponse response;

        try
        {
            response = await client.GetSecretValueAsync(request);
            var options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };

            var credentials = JsonSerializer.Deserialize<Credentials>(response.SecretString, options)
                ?? throw new InternalServiceErrorException("Failed deserialising connection string.");

            var connectionString = new StringBuilder();
            connectionString.Append($"server={credentials.Host};");
            connectionString.Append($"database={credentials.DbName};");
            connectionString.Append($"user id={credentials.Username};");
            connectionString.Append($"password={credentials.Password}");

            return connectionString.ToString();
        }
        catch (Exception e)
        {
            // For a list of the exceptions thrown, see
            // https://docs.aws.amazon.com/secretsmanager/latest/apireference/API_GetSecretValue.html
            throw e;
        }
    }

    public class Credentials
    {
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Engine { get; set; } = string.Empty;

        public string Host { get; set; } = string.Empty;

        public string Port { get; set; } = string.Empty;

        public string DbName { get; set; } = string.Empty;
    }

    public class Product
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public double Price { get; set; }
    }
}