namespace CooperativaMercado.Model
{
    public class IngresoDiario
    {
        public int IdIngreso { get; set; }
        public int IdPuesto { get; set; }

        public string? NumeroPuesto { get; set; }
        public string? NombreUsuario { get; set; }

        public DateTime Fecha { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaRegistro { get; set; }
        public int? IdUsuario { get; set; }
    }
}
