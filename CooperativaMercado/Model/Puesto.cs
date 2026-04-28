namespace CooperativaMercado.Model
{
    public class Puesto
    {
        public int IdPuesto { get; set; }
        public string Numero { get; set; }
        public decimal? Metraje { get; set; }
        public string? Ubicacion { get; set; }
        public string? Giro { get; set; }
        public decimal MontoAlquiler { get; set; }
        public int? IdSocio { get; set; }
        public string? NombreSocio { get; set; }
        public bool Activo { get; set; }

    }
}
