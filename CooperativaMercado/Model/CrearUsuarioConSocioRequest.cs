namespace CooperativaMercado.Model
{
    public class CrearUsuarioConSocioRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string NombreSocio { get; set; } = string.Empty;
        public string? DNI { get; set; }
        public string? Telefono { get; set; }
    }
}
