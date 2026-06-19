using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Infrastructure.Data
{
    public record DummyJsonQuoteResponse(int id, string quote, string author);

    public class ExternalApiService
    {
        private static readonly HttpClient _httpClient = new();
        private readonly string _baseUrl;

        public ExternalApiService(IConfiguration configuration)
        {
            // Dynamically fetch config value
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? throw new ArgumentNullException("BaseUrl configuration is missing.");
        }

        public async Task<string> FetchExternalDataAsync(int studentId, CancellationToken cancellationToken)
        {
            int maxRetries = 3;
            int delayMilliseconds = 1000;

            for (int retry = 1; retry <= maxRetries; retry++)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var startTime = DateTime.Now;
                    HttpResponseMessage response = await _httpClient.GetAsync(_baseUrl, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    string jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
                    
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var quoteData = JsonSerializer.Deserialize<DummyJsonQuoteResponse>(jsonString, options);

                    var duration = DateTime.Now - startTime;
                    
                    // Production Logging (Info)
                    Log.Information("Student ID {StudentId}: Fetch finished in {Duration}ms (Try #{Retry})", studentId, duration.TotalMilliseconds, retry);
                    
                    if (quoteData != null)
                    {
                        return $"\"{quoteData.quote}\" — {quoteData.author}";
                    }
                    return "No Data";
                }
                catch (OperationCanceledException)
                {
                    Log.Warning("Student ID {StudentId}: Operation timed out or canceled.", studentId);
                    throw; 
                }
                catch (Exception ex)
                {
                    // Production Logging (Error / Warning)
                    Log.Error(ex, "Student ID {StudentId}: Try #{Retry} failed.", studentId, retry);

                    if (retry == maxRetries)
                    {
                        return $"Failed to load quote insights after {maxRetries} attempts.";
                    }

                    await Task.Delay(delayMilliseconds * retry, cancellationToken);
                }
            }

            return "No Data Available";
        }
    }
}