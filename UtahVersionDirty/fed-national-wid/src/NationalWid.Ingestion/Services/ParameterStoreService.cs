using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace NationalWid.Ingestion.Services;

/// <summary>
/// Reads parameters and secrets from AWS Systems Manager Parameter Store.
/// </summary>
public sealed class ParameterStoreService(IAmazonSimpleSystemsManagement ssm)
{
    public async Task<string> GetParameterAsync(string name, CancellationToken cancellationToken = default)
    {
        var request = new GetParameterRequest
        {
            Name = name,
            WithDecryption = true,
        };

        var response = await ssm.GetParameterAsync(request, cancellationToken);
        return response.Parameter.Value;
    }

    public async Task<string?> GetParameterOrDefaultAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetParameterAsync(name, cancellationToken);
        }
        catch (ParameterNotFoundException)
        {
            return null;
        }
    }
}
