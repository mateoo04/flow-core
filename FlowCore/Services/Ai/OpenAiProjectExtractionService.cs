using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FlowCore.Models;
using FlowCore.Models.Ai;

namespace FlowCore.Services.Ai;

public sealed class OpenAiProjectExtractionService : IAiProjectExtractionService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly ILogger<OpenAiProjectExtractionService> _logger;

    public OpenAiProjectExtractionService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAiProjectExtractionService> logger)
    {
        _httpClient = httpClient;
        _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        _model = configuration["OpenAI:Model"] ?? "gpt-5.4-mini";
        _logger = logger;
    }

    public async Task<AiProjectDraft> ExtractAsync(string prompt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new AiProjectExtractionConfigurationException();

        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var body = new
        {
            model = _model,
            store = false,
            instructions = $"Extract exactly one project from the user's request. Today is {today}. Use ISO 8601 dates (yyyy-MM-dd) and resolve relative dates using today. Do not invent missing details: use null for dates and description. Keep the name concise. Status must be planning, active, onhold, completed, or archived. Priority must be low, medium, high, or critical.",
            input = prompt,
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "project_draft",
                    strict = true,
                    schema = new
                    {
                        type = "object",
                        additionalProperties = false,
                        properties = new
                        {
                            name = new { type = "string" },
                            description = new { type = new[] { "string", "null" } },
                            status = new { type = "string", @enum = new[] { "planning", "active", "onhold", "completed", "archived" } },
                            priority = new { type = "string", @enum = new[] { "low", "medium", "high", "critical" } },
                            startDate = new { type = new[] { "string", "null" } },
                            dueDate = new { type = new[] { "string", "null" } }
                        },
                        required = new[] { "name", "description", "status", "priority", "startDate", "dueDate" }
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
            _logger.LogWarning("OpenAI project extraction failed with status {StatusCode}.", (int)response.StatusCode);
            throw new AiProjectExtractionException();
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var json = ExtractOutputText(document.RootElement);
            if (string.IsNullOrWhiteSpace(json)) throw new JsonException();
            using var project = JsonDocument.Parse(json);
            var root = project.RootElement;
            var name = root.GetProperty("name").GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 200) throw new JsonException();

            var description = NullableString(root, "description")?.Trim();
            var status = root.GetProperty("status").GetString() switch
            {
                "active" => ProjectStatus.Active,
                "onhold" => ProjectStatus.OnHold,
                "completed" => ProjectStatus.Completed,
                "archived" => ProjectStatus.Archived,
                _ => ProjectStatus.Planning
            };
            var priority = root.GetProperty("priority").GetString() switch
            {
                "low" => ProjectPriority.Low,
                "high" => ProjectPriority.High,
                "critical" => ProjectPriority.Critical,
                _ => ProjectPriority.Medium
            };
            var startDate = ParseDate(NullableString(root, "startDate"));
            var dueDate = ParseDate(NullableString(root, "dueDate"));
            if (startDate is not null && dueDate is not null && startDate > dueDate) throw new JsonException();

            return new AiProjectDraft(name, description, status, priority, startDate, dueDate);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            _logger.LogWarning(exception, "OpenAI returned an unusable project draft.");
            throw new AiProjectExtractionException();
        }
    }

    private static string? NullableString(JsonElement root, string property) => root.GetProperty(property).ValueKind == JsonValueKind.Null
        ? null
        : root.GetProperty(property).GetString();

    private static DateTime? ParseDate(string? value)
    {
        if (value is null) return null;
        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            throw new JsonException("Project date must use yyyy-MM-dd.");
        return date.ToDateTime(TimeOnly.MinValue);
    }

    private static string? ExtractOutputText(JsonElement response)
    {
        if (response.TryGetProperty("output_text", out var outputText) && outputText.ValueKind == JsonValueKind.String)
            return outputText.GetString();
        if (!response.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
            throw new JsonException("OpenAI response did not contain output.");

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array) continue;
            foreach (var part in content.EnumerateArray())
            {
                if (part.TryGetProperty("type", out var type) && type.GetString() == "output_text"
                    && part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                    return text.GetString();
            }
        }

        throw new JsonException("OpenAI response did not contain output text.");
    }
}

public sealed class AiProjectExtractionConfigurationException : Exception
{
}

public sealed class AiProjectExtractionException : Exception
{
}
