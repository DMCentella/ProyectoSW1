namespace CooperativaMercado.Model
{
    public class Deuda
    {
        public int IdDeuda { get; set; }
        public int IdPuesto { get; set; }
        public int IdTipoDeuda { get; set; }

        public string? NumeroPuesto { get; set; }
        public string? NombreTipoDeuda { get; set; }

        public string? Descripcion { get; set; }
        public decimal Monto { get; set; }
        public decimal Mora { get; set; }
        public decimal MontoTotal { get; set; }
        public int Mes { get; set; }
        public int Anio { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public DateTime? FechaAplicacionMora { get; set; }
        public int? UsuarioAplicaMora { get; set; }
        public string? Estado { get; set; }
    }
}
