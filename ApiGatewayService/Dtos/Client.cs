
namespace ApiGatewayService.Dtos.Clients
{
    public record CreateClientDto
    {
        public string FirstName { get; init; } = null!;
        public string LastName { get; init; } = null!;
        public string Email { get; init; } = null!;
        public string PhoneNumber { get; init; } = null!;
        public string Username { get; init; } = null!;
        public string Password { get; init; } = null!;
        public string BirthDate { get; init; } = null!;
        public string Address { get; init; } = null!;
    }


    public record UpdateClientDto
    {
        public string? FirstName { get; set; } = null!;
        public string? LastName { get; set; } = null!;
        public string? Email { get; set; } = null!;
        public string? Username { get; set; } = null!;
        public string? BirthDate { get; set; } = null!;  
        public string? Address { get; set; } = null!;
        public string? PhoneNumber { get; set; } = null!;
        public string? Password { get; set; } 
    }
}