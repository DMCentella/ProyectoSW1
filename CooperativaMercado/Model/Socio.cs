namespace CooperativaMercado.Model
{
    public class Socio
    {
        public int IdSocio { get; set; }
        public string Nombre { get; set; }
        public string DNI { get; set; }
        public string Telefono { get; set; }
        public bool Activo { get; set; }

        public ICollection<Puesto>? Puestos { get; set; }

    }
}
