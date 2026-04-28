using CooperativaMercado.Model;
using CooperativaMercado.Repository.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CooperativaMercado.Repository.Dao
{
    public class UsuarioDAO : IUsuario
    {
        private readonly string connectionString;

        public UsuarioDAO() 
        {
            connectionString = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json")
               .Build()
               .GetConnectionString("dataBase")!;
        }

        public Usuario ValidarUsuario(string user, string pass)
        {
            Usuario? usuario = null;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("sp_ValidarUsuario", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Username", user);
                command.Parameters.AddWithValue("@Password", pass);
                SqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    usuario = new Usuario()
                    {
                        id = reader.GetInt32(0),  
                        username = reader.GetString(1),
                        rol = reader.GetString(2),
                        idSocio = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                        activo = reader.GetBoolean(4)
                    };
                }
            }
            return usuario;
        }

        public Usuario? ObtenerPorUsername(string username)
        {
            Usuario? usuario = null;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("sp_ObtenerUsuarioPorUsername", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Username", username);
                SqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    usuario = new Usuario()
                    {
                        id = reader.GetInt32(0),
                        username = reader.GetString(1),
                        rol = reader.GetString(2),
                        idSocio = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                        activo = reader.GetBoolean(4)
                    };
                }
            }
            return usuario;
        }

        public (int idUsuario, int idSocio) CrearUsuarioConSocio(string username, string password, string nombreSocio, string? dni, string? telefono)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("sp_CrearUsuarioConSocio", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@Password", password);
                command.Parameters.AddWithValue("@NombreSocio", nombreSocio);
                command.Parameters.AddWithValue("@DNI", (object?)dni ?? DBNull.Value);
                command.Parameters.AddWithValue("@Telefono", (object?)telefono ?? DBNull.Value);

                SqlParameter idSocioParam = new SqlParameter("@IdSocioOut", SqlDbType.Int) { Direction = ParameterDirection.Output };
                SqlParameter idUsuarioParam = new SqlParameter("@IdUsuarioOut", SqlDbType.Int) { Direction = ParameterDirection.Output };
                command.Parameters.Add(idSocioParam);
                command.Parameters.Add(idUsuarioParam);

                command.ExecuteNonQuery();

                return ((int)idUsuarioParam.Value, (int)idSocioParam.Value);
            }
        }

    }
}
