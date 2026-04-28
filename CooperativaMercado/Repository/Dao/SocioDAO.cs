using CooperativaMercado.Model;
using CooperativaMercado.Repository.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CooperativaMercado.Repository.Dao
{
    public class SocioDao : ISocio
    {
        private readonly string connectionString;

        public SocioDao()
        {
            connectionString = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build()
                .GetConnectionString("dataBase")!;
        }

        public List<Socio> Listar()
        {
            List<Socio> lista = new List<Socio>();
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_ListarSocios", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    var socio = new Socio()
                    {
                        IdSocio = dr.GetInt32(0),
                        Nombre = dr.GetString(1),
                        DNI = dr.GetString(2),
                        Telefono = dr.GetString(3),
                        Activo = dr.GetBoolean(4)
                    };
                    lista.Add(socio);
                }
                dr.Close();

                foreach (var socio in lista)
                {
                    socio.Puestos = CargarPuestosPorSocio(socio.IdSocio, cn);
                }
            }
            return lista;
        }

        public Socio ObtenerPorId(int id)
        {
            Socio socio = null;
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_ObtenerSocioPorId", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdSocio", id);
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    socio = new Socio()
                    {
                        IdSocio = dr.GetInt32(0),
                        Nombre = dr.GetString(1),
                        DNI = dr.GetString(2),
                        Telefono = dr.GetString(3),
                        Activo = dr.GetBoolean(4)
                    };
                }
                dr.Close();

                if (socio != null)
                {
                    socio.Puestos = CargarPuestosPorSocio(socio.IdSocio, cn);
                }
            }
            return socio;
        }

        private List<Puesto> CargarPuestosPorSocio(int idSocio, SqlConnection cn)
        {
            List<Puesto> puestos = new List<Puesto>();
            SqlCommand cmd = new SqlCommand("sp_ObtenerPuestosPorSocio", cn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IdSocio", idSocio);
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                puestos.Add(new Puesto()
                {
                    IdPuesto = dr.GetInt32(0),
                    Numero = dr.GetString(1),
                    Metraje = dr.IsDBNull(2) ? null : dr.GetDecimal(2),
                    Ubicacion = dr.IsDBNull(3) ? null : dr.GetString(3),
                    Giro = dr.IsDBNull(4) ? null : dr.GetString(4),
                    MontoAlquiler = dr.GetDecimal(5),
                    Activo = dr.GetBoolean(6)
                });
            }
            dr.Close();
            return puestos;
        }

        public int Registrar(Socio socio)
        {
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_RegistrarSocio", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Nombre", socio.Nombre);
                cmd.Parameters.AddWithValue("@DNI", (object?)socio.DNI ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Telefono", (object?)socio.Telefono ?? DBNull.Value);
                return cmd.ExecuteNonQuery();
            }
        }

        public int Actualizar(Socio socio)
        {
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_ActualizarSocio", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdSocio", socio.IdSocio);
                cmd.Parameters.AddWithValue("@Nombre", socio.Nombre);
                cmd.Parameters.AddWithValue("@DNI", (object?)socio.DNI ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Telefono", (object?)socio.Telefono ?? DBNull.Value);
                return cmd.ExecuteNonQuery();
            }
        }

        public bool PuedeRetirarse(int idSocio)
        {
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_ValidarRetiroSocio", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdSocio", idSocio);
                var result = cmd.ExecuteScalar();
                return Convert.ToInt32(result) == 0;
            }
        }

        public void RetirarSocio(int idSocio, string motivoRetiro)
        {
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_RetirarSocio", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdSocio", idSocio);
                cmd.Parameters.AddWithValue("@MotivoRetiro", motivoRetiro);
                cmd.ExecuteNonQuery();
            }
        }

        public void ReactivarSocio(int idSocio)
        {
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_ReactivarSocio", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdSocio", idSocio);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
