using Microsoft.Extensions.Options;
using System.Net;
using TicketService.Config;
using TicketService.DTOs;
using TicketService.Models;

namespace TicketService.Services
{
    public sealed class PolicyValidationService(IHttpClientFactory httpClientFactory, IOptions<PolicyValidationOptions> options) : IPolicyValidationService
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly PolicyValidationOptions _options = options.Value;

        public async Task ValidateForTicketCreationAsync(CreateTicketRequest request, CurrentUser user, CancellationToken cancellationToken)
        {
            if (!request.PolicyId.HasValue)
            {
                if (request.Type == TicketType.Claim)
                {
                    throw new InvalidOperationException("Claim tickets require a valid policy.");
                }

                return;
            }

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

            using var response = await client.GetAsync($"internal/policies/{request.PolicyId.Value}/customers/{user.UserId}/ticket-validation", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException("Policy not found.");
            }

            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<TicketPolicyValidationResult>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Policy validation failed.");

            if (!payload.PolicyExists)
            {
                throw new InvalidOperationException("Policy not found.");
            }

            if (request.Type == TicketType.Claim && !payload.CustomerOwnsPolicy)
            {
                throw new InvalidOperationException("Claim tickets can only be raised for policies bought by the current customer.");
            }
        }
    }
}
