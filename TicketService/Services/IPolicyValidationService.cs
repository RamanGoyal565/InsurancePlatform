using TicketService.DTOs;

namespace TicketService.Services
{
    public interface IPolicyValidationService
    {
        Task ValidateForTicketCreationAsync(CreateTicketRequest request, CurrentUser user, CancellationToken cancellationToken);
    }
}
