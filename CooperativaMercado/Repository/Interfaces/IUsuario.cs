using CooperativaMercado.Model;

namespace CooperativaMercado.Repository.Interfaces
{
    public interface IUsuario
    {
        Usuario ValidarUsuario(string user, string pass);
    }
}
