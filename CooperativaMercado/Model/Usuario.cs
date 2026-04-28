namespace CooperativaMercado.Model
{
    public class Usuario
    {
        public int id { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string? rol { get; set; }
        public int? idSocio { get; set; }
        public bool? activo { get; set; }    

    }
}
