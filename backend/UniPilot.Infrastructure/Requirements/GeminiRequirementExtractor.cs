using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using UniPilot.Application.Requirements;
using UniPilot.Domain.Requirements;

namespace UniPilot.Infrastructure.Requirements;

public sealed class GeminiRequirementExtractor(
    HttpClient httpClient,
    IConfiguration configuration)
    : IRequirementExtractor
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true
        };

    public async Task<IReadOnlyList<ExtractedRequirement>>
        ExtractAsync(
            IReadOnlyList<RequirementSourcePage> pages,
            CancellationToken cancellationToken = default)
    {
        if (pages.Count == 0)
        {
            return [];
        }

        var apiKey =
            configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException(
                "Gemini API key was not configured.");

        var model =
            configuration["Gemini:Model"]
            ?? "gemini-3.5-flash";

        var documentText = BuildDocumentText(pages);

        var requestBody = new
        {
            systemInstruction = new
            {
                parts = new[]
                {
                    new
                    {
                        text =
                            """
                            You analyze university assignment and project documents.

                            Extract only explicit requirements addressed to the student
                            or project team.

                            A valid requirement must explicitly ask the student or team
                            to build, implement, submit, document, test, present, use,
                            avoid, or complete something.

                            Valid requirements include:
                            - Required system features.
                            - Technical or implementation constraints.
                            - Non-functional requirements.
                            - Required deliverables.
                            - Submission instructions.
                            - Explicit deadlines.
                            - Explicit grading or evaluation criteria.

                            Do not extract:
                            - Lecture explanations.
                            - Definitions or general facts.
                            - Examples used for teaching.
                            - Algorithms described only as course material.
                            - Mathematical properties or theoretical rules.
                            - Recommendations that are not mandatory.
                            - Requirements invented or inferred from the topic.

                            If the document contains no explicit assignment or project
                            requirements, return an empty requirements array.

                            Classify every requirement using exactly one type:
                            Functional, NonFunctional, Constraint,
                            Deliverable, Deadline,
                            EvaluationCriterion, Other.

                            Classify priority using exactly one value:
                            Unknown, Low, Medium, High, Critical.

                            Keep each title concise.
                            Preserve the original meaning.
                            Return the exact page number containing the requirement.
                            """
                    }
                }
            },

            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new
                        {
                            text =
                                "Extract the project requirements " +
                                "from this document:\n\n" +
                                documentText
                        }
                    }
                }
            },

            generationConfig = new
            {
                temperature = 0.2,

                responseMimeType = "application/json",

                responseJsonSchema = new
                {
                    type = "object",

                    properties = new
                    {
                        requirements = new
                        {
                            type = "array",

                            items = new
                            {
                                type = "object",

                                properties = new
                                {
                                    title = new
                                    {
                                        type = "string"
                                    },

                                    description = new
                                    {
                                        type = "string"
                                    },

                                    type = new
                                    {
                                        type = "string",

                                        @enum = new[]
                                        {
                                            "Functional",
                                            "NonFunctional",
                                            "Constraint",
                                            "Deliverable",
                                            "Deadline",
                                            "EvaluationCriterion",
                                            "Other"
                                        }
                                    },

                                    priority = new
                                    {
                                        type = "string",

                                        @enum = new[]
                                        {
                                            "Unknown",
                                            "Low",
                                            "Medium",
                                            "High",
                                            "Critical"
                                        }
                                    },

                                    sourcePageNumber = new
                                    {
                                        type = "integer",
                                        minimum = 1
                                    }
                                },

                                required = new[]
                                {
                                    "title",
                                    "description",
                                    "type",
                                    "priority",
                                    "sourcePageNumber"
                                }
                            }
                        }
                    },

                    required = new[]
                    {
                        "requirements"
                    }
                }
            }
        };

        var requestUri =
            $"https://generativelanguage.googleapis.com/" +
            $"v1beta/models/{Uri.EscapeDataString(model)}:" +
            "generateContent";

        var requestJson =
            JsonSerializer.Serialize(requestBody);

        var responseBody =
            await SendWithRetryAsync(
                requestUri,
                apiKey,
                requestJson,
                cancellationToken);

        using var responseJson =
            JsonDocument.Parse(responseBody);

        var outputText =
            responseJson.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

        if (string.IsNullOrWhiteSpace(outputText))
        {
            return [];
        }

        var parsed =
            JsonSerializer.Deserialize<GeminiResponse>(
                outputText,
                JsonOptions);

        if (parsed is null)
        {
            return [];
        }

        var results =
            new List<ExtractedRequirement>();

        foreach (var item in parsed.Requirements)
        {
            if (!Enum.TryParse<RequirementType>(
                    item.Type,
                    ignoreCase: true,
                    out var type))
            {
                type = RequirementType.Other;
            }

            if (!Enum.TryParse<RequirementPriority>(
                    item.Priority,
                    ignoreCase: true,
                    out var priority))
            {
                priority = RequirementPriority.Unknown;
            }

            results.Add(
                new ExtractedRequirement(
                    item.Title,
                    item.Description,
                    type,
                    priority,
                    item.SourcePageNumber));
        }

        return results;
    }

    private async Task<string> SendWithRetryAsync(
        string requestUri,
        string apiKey,
        string requestJson,
        CancellationToken cancellationToken)
    {
        const int maximumAttempts = 3;

        for (var attempt = 1;
             attempt <= maximumAttempts;
             attempt++)
        {
            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    requestUri);

            request.Headers.Add(
                "x-goog-api-key",
                apiKey);

            request.Content =
                new StringContent(
                    requestJson,
                    Encoding.UTF8,
                    "application/json");

            using var response =
                await httpClient.SendAsync(
                    request,
                    cancellationToken);

            var responseBody =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return responseBody;
            }

            var isTemporaryFailure =
                response.StatusCode ==
                    HttpStatusCode.TooManyRequests ||
                response.StatusCode ==
                    HttpStatusCode.ServiceUnavailable;

            if (isTemporaryFailure &&
                attempt < maximumAttempts)
            {
                var delay =
                    TimeSpan.FromSeconds(
                        Math.Pow(2, attempt));

                await Task.Delay(
                    delay,
                    cancellationToken);

                continue;
            }

            throw new InvalidOperationException(
                $"Gemini request failed with status " +
                $"{(int)response.StatusCode}: " +
                Truncate(responseBody, 1000));
        }

        throw new InvalidOperationException(
            "Gemini request failed after multiple attempts.");
    }

    private static string BuildDocumentText(
        IReadOnlyList<RequirementSourcePage> pages)
    {
        const int maximumCharacters = 120_000;

        var builder = new StringBuilder();

        foreach (var page in pages.OrderBy(
                     page => page.PageNumber))
        {
            var pageHeader =
                $"\n\n--- PAGE {page.PageNumber} ---\n";

            if (builder.Length +
                pageHeader.Length >= maximumCharacters)
            {
                break;
            }

            builder.Append(pageHeader);

            var availableCharacters =
                maximumCharacters - builder.Length;

            if (page.Text.Length <= availableCharacters)
            {
                builder.Append(page.Text);
                continue;
            }

            builder.Append(
                page.Text.AsSpan(
                    0,
                    availableCharacters));

            break;
        }

        return builder.ToString();
    }

    private static string Truncate(
        string value,
        int maximumLength)
    {
        return value.Length <= maximumLength
            ? value
            : value[..maximumLength];
    }

    private sealed class GeminiResponse
    {
        public List<GeminiRequirement> Requirements { get; init; }
            = [];
    }

    private sealed class GeminiRequirement
    {
        public string Title { get; init; }
            = string.Empty;

        public string Description { get; init; }
            = string.Empty;

        public string Type { get; init; }
            = "Other";

        public string Priority { get; init; }
            = "Unknown";

        public int SourcePageNumber { get; init; }
    }
}
