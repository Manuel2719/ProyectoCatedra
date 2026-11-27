using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace SistemaFacturacion
{
    public partial class FrmLogin : Form
    {
        string conexionString = @"Server=localhost\SQLEXPRESS; Database=TiendaFacturacionDB; Trusted_Connection=True;";

        public FrmLogin()
        {
            InitializeComponent();
        }

        private void btnIngresar_Click(object sender, EventArgs e)
        {
            string usuario = txtUsuario.Text.Trim();
            string contrasena = txtContrasena.Text;

            if (string.IsNullOrWhiteSpace(usuario) ||
                string.IsNullOrWhiteSpace(contrasena))
            {
                MessageBox.Show("Ingrese su usuario y contraseña.");
                return;
            }

            try
            {
                using (SqlConnection conexion = new SqlConnection(conexionString))
                {
                    conexion.Open();

                    string consulta = @"SELECT COUNT(*) FROM Usuarios
                                        WHERE Usuario = @Usuario
                                        AND ContrasenaHash = HASHBYTES('SHA2_256', @Contrasena)
                                        AND Activo = 1";

                    using (SqlCommand comando = new SqlCommand(consulta, conexion))
                    {
                        comando.Parameters.Add("@Usuario", SqlDbType.NVarChar, 50).Value = usuario;
                        comando.Parameters.Add("@Contrasena", SqlDbType.NVarChar, -1).Value = contrasena;

                        int resultado = Convert.ToInt32(comando.ExecuteScalar());

                        if (resultado > 0)
                        {
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Usuario o contraseña incorrectos.");
                            txtContrasena.Clear();
                            txtContrasena.Focus();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error de conexión: " + ex.Message);
            }
        }
    }
}