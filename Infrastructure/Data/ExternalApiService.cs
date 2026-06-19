using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    // C# 9+ Record type to cleanly model the incoming API JSON format
    public record DummyJsonQuoteResponse(int id, string quote, string author);

    public class ExternalApiService
    {
        private static readonly HttpClient _httpClient = new();

        public async Task<string> FetchExternalDataAsync(int studentId, CancellationToken cancellationToken)
        {
            // Endpoint for retrieving a completely random quote
            string url = "https://dummyjson.com/quotes/random";
            int maxRetries = 3;
            int delayMilliseconds = 1000;

            for (int retry = 1; retry <= maxRetries; retry++)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var startTime = DateTime.Now;
                    HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    string jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
                    
                    // Strong type parsing using System.Text.Json with Case-Insensitive option matching
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var quoteData = JsonSerializer.Deserialize<DummyJsonQuoteResponse>(jsonString, options);

                    var duration = DateTime.Now - startTime;
                    Console.WriteLine($"[BATCH] Student ID {studentId}: Fetch finished in {duration.TotalMilliseconds:F0}ms (Try #{retry})");
                    
                    // Formats the response into a single descriptive string for the profile
                    if (quoteData != null)
                    {
                        return $"\"{quoteData.quote}\" — {quoteData.author}";
                    }
                    
                    return "No Data";
                }
                catch (OperationCanceledException)
                {
                    throw; 
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[WARN] Student ID {studentId}: Try #{retry} failed ({ex.Message}).");
                    Console.ResetColor();

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