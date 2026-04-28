using CooperativaMercado.Model;

namespace CooperativaMercado.Repository.Interfaces
{
    public interface IPuesto
    {
        List<Puesto> Listar();
        Puesto ObtenerPorId(int id);

        int Registrar(Puesto puesto);
        int Actualizar(Puesto puesto);

        int AsignarSocio(int idPuesto, int idSocio);
        int DesasignarSocio(int idPuesto, string? motivo = null);
    }
}
