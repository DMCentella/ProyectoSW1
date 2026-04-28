using CooperativaMercado.Model;

namespace CooperativaMercado.Repository.Interfaces
{
    public interface ISocio
    {
        List<Socio> Listar();
        Socio ObtenerPorId(int id);
        int Registrar(Socio socio);
        int Actualizar(Socio socio);
        bool PuedeRetirarse(int idSocio);
        void RetirarSocio(int idSocio, string motivoRetiro);
        void ReactivarSocio(int idSocio);
    }
}
