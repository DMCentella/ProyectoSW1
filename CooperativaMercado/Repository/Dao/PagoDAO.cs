using CooperativaMercado.Model;
using CooperativaMercado.Repository.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CooperativaMercado.Repository.Dao
{
    public class PagoDao : IPago
    {
        private readonly string connectionString;

        public PagoDao()
        {
            connectionString = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build()
                .GetConnectionString("dataBase")!;
        }

        public int RegistrarPago(int idDeuda, decimal monto, string metodoPago)
        {
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_RegistrarPago", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdDeuda", idDeuda);
                cmd.Parameters.AddWithValue("@Monto", monto);
                cmd.Parameters.AddWithValue("@MetodoPago", (object?)metodoPago ?? DBNull.Value);
                return cmd.ExecuteNonQuery();
            }
        }

        private Pago MapearPago(SqlDataReader dr)
        {
            return new Pago()
            {
                IdPago = dr.GetInt32(0), 
                IdDeuda = dr.GetInt32(1),  
                ConceptoDeuda = dr.IsDBNull(2) ? null : dr.GetString(2),
                Monto = dr.GetDecimal(3),    
                Fecha = dr.GetDateTime(4),    
                NumeroRecibo = dr.GetString(5), 
                MetodoPago = dr.IsDBNull(6) ? null : dr.GetString(6) 
            };
        }

        public List<Pago> Listar()
        {
            List<Pago> lista = new List<Pago>();
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_ListarPagos", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    lista.Add(MapearPago(dr));
                }
            }
            return lista;
        }

        public List<Pago> ObtenerPorPuesto(int idPuesto)
        {
            List<Pago> lista = new List<Pago>();
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_ReportePagosPorPuesto", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdPuesto", idPuesto);
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    lista.Add(MapearPago(dr));
                }
            }
            return lista;
        }

        public decimal TotalRecaudadoPorRango(DateTime inicio, DateTime fin)
        {
            decimal total = 0;
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_ReporteRecaudadoPorRango", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@FechaInicio", inicio.Date);
                cmd.Parameters.AddWithValue("@FechaFin", fin.Date);
                var result = cmd.ExecuteScalar();
                if (result != null)
                    total = Convert.ToDecimal(result);
            }
            return total;
        }

        public Pago? ObtenerDatosBoleta(int idDeuda)
        {
            Pago? pago = null;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("sp_ObtenerBoletaPago", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@IdDeuda", idDeuda);
                SqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    pago = new Pago()
                    {
                        NumeroRecibo = reader.GetString(0),
                        Fecha = reader.GetDateTime(1),
                        Monto = reader.GetDecimal(2),
                        MetodoPago = reader.GetString(3),
                        NombreSocio = reader.IsDBNull(4) ? null : reader.GetString(4),
                        NumeroPuesto = reader.GetString(5),
                        ConceptoDeuda = reader.GetString(6)
                    };
                }
            }
            return pago;
        }
    }
}
