using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FlowCore.Models;

namespace FlowCore.Services.Ai;

public sealed class OpenAiTaskExtractionService : IAiTaskExtractionService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly ILogger<OpenAiTaskExtractionService> _logger;

    public OpenAiTaskExtractionService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAiTaskExtractionService> logger)
    {
        _httpClient = httpClient;
        _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        _model = configuration["OpenAI:Model"] ?? "gpt-5.4-mini";
        _logger = logger;
    }

    public async Task<AiTaskDraft> ExtractAsync(string prompt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new AiTaskExtractionConfigurationException();

        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var body = new
        {
            model = _model,
            store = false,
            instructions = $"Extract exactly one task from the user's request. Today is {today}. Use ISO 8601 dates (yyyy-MM-dd). Resolve relative dates using today. Do not invent missing details: use null. Keep title concise. Priority must be low, medium, or high.",
            input = prompt,
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "task_draft",
                    strict = true,
                    schema = new
                    {
                        type = "object",
                        additionalProperties = false,
                        properties = new
                        {
                            title = new { type = "string" },
                            description = new { type = new[] { "string", "null" } },
                            priority = new { type = "string", @enum = new[] { "low", "medium", "high" } },
                            dueDate = new { type = new[] { "string", "null" } }
                        },
                        required = new[] { "title", "description", "priority", "dueDate" }
                    }
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "responses")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using var response = await _httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("OpenAI task extraction failed with status {StatusCode}.", (int)response.StatusCode);
            throw new AiTaskExtractionException();
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var json = document.RootElement.GetProperty("output_text").GetString();
            if (string.IsNullOrWhiteSpace(json)) throw new JsonException();
            using var task = JsonDocument.Parse(json);
            var root = task.RootElement;
            var title = root.GetProperty("title").GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(title) || title.Length > 200) throw new JsonException();

            var description = root.GetProperty("description").ValueKind == JsonValueKind.Null
                ? null : root.GetProperty("description").GetString()?.Trim();
            var priority = root.GetProperty("priority").GetString() switch
            {
                "low" => TaskPriority.Low,
                "high" => TaskPriority.High,
                _ => TaskPriority.Medium
            };
            var dueDateText = root.GetProperty("dueDate").ValueKind == JsonValueKind.Null
                ? null : root.GetProperty("dueDate").GetString();
            DateTime? dueDate = dueDateText is not null && DateOnly.TryParseExact(dueDateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? date.ToDateTime(TimeOnly.MinValue) : null;

            return new AiTaskDraft(title, description, priority, dueDate);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "OpenAI returned an unusable task draft.");
            throw new AiTaskExtractionException();
        }
    }
}

public sealed class AiTaskExtractionConfigurationException : Exception
{
}

public sealed class AiTaskExtractionException : Exception
{
}
