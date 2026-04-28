namespace CooperativaMercado.Model
{
    public class HistorialPuesto
    {
        public int IdHistorial { get; set; }
        public int IdPuesto { get; set; }
        public int IdSocio { get; set; }
        public string? NombreSocio { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string? MotivoRetiro { get; set; }
        public bool Activo { get; set; }
    }
}
