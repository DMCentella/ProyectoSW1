namespace CooperativaMercado.Model
{
    public class TipoDeuda
    {
        public int IdTipoDeuda { get; set; }
        public string? Nombre { get; set; }
        public decimal? MontoBase { get; set; }

        public bool Activo { get; set; }

        public ICollection<Deuda> Deudas { get; set; }

    }
}
