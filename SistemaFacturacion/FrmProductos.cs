
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace SistemaFacturacion
{
    public partial class FrmProductos : Form
    {
        private readonly string conexionString =
            @"Server=localhost\SQLEXPRESS;Database=TiendaFacturacionDB;Trusted_Connection=True;";

        private TextBox txtCodigo;
        private TextBox txtNombre;
        private TextBox txtPrecio;
        private TextBox txtBuscar;
        private NumericUpDown nudStock;
        private ComboBox cboCategoria;
        private DataGridView dgvProductos;

        private int idSeleccionado = 0;
        private bool productoActivo = true;
        private Button btnEstado;

        public FrmProductos()
        {
            InitializeComponent();
            CrearInterfaz();
            CargarCategorias();
            CargarProductos();
        }

        private Label CrearEtiqueta(string texto, int x, int y)
        {
            return new Label
            {
                Text = texto,
                Location = new Point(x, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 10)
            };
        }

        private TextBox CrearTexto(int x, int y, int ancho)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Width = ancho,
                Font = new Font("Segoe UI", 10)
            };
        }

        private Button CrearBoton(
            string texto, int x, int y, Color color,
            EventHandler accion)
        {
            Button boton = new Button
            {
                Text = texto,
                Location = new Point(x, y),
                Size = new Size(135, 38),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            boton.FlatAppearance.BorderSize = 0;
            boton.Click += accion;
            Controls.Add(boton);

            return boton;
        }

        private void CrearInterfaz()
        {
            Text = "Administración de productos";
            Size = new Size(950, 680);
            MinimumSize = new Size(950, 680);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(245, 247, 250);
            Font = new Font("Segoe UI", 10);

            Label titulo = new Label
            {
                Text = "ADMINISTRACIÓN DE PRODUCTOS",
                Location = new Point(25, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(35, 55, 80)
            };
            Controls.Add(titulo);

            Controls.Add(CrearEtiqueta("Código", 30, 85));
            txtCodigo = CrearTexto(30, 110, 160);
            Controls.Add(txtCodigo);

            Controls.Add(CrearEtiqueta("Nombre", 220, 85));
            txtNombre = CrearTexto(220, 110, 300);
            Controls.Add(txtNombre);

            Controls.Add(CrearEtiqueta("Precio ($)", 550, 85));
            txtPrecio = CrearTexto(550, 110, 130);
            Controls.Add(txtPrecio);

            Controls.Add(CrearEtiqueta("Existencias", 710, 85));
            nudStock = new NumericUpDown
            {
                Location = new Point(710, 110),
                Width = 160,
                Maximum = 1000000,
                Minimum = 0
            };
            Controls.Add(nudStock);

            Controls.Add(CrearEtiqueta("Categoría", 30, 160));
            cboCategoria = new ComboBox
            {
                Location = new Point(30, 185),
                Width = 300,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            Controls.Add(cboCategoria);

            CrearBoton("Agregar", 30, 240,
                Color.SeaGreen, BtnAgregar_Click);

            CrearBoton("Actualizar", 180, 240,
                Color.SteelBlue, BtnActualizar_Click);

            btnEstado = CrearBoton("Desactivar", 330, 240,
            Color.IndianRed, BtnDesactivar_Click);

            CrearBoton("Limpiar", 480, 240,
                Color.DimGray, BtnLimpiar_Click);

            Controls.Add(CrearEtiqueta("Buscar producto", 30, 315));
            txtBuscar = CrearTexto(30, 340, 450);
            txtBuscar.TextChanged += (s, e) => CargarProductos();
            Controls.Add(txtBuscar);

            dgvProductos = new DataGridView
            {
                Location = new Point(30, 390),
                Size = new Size(860, 220),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode =
                    DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };

            dgvProductos.CellClick += DgvProductos_CellClick;
            Controls.Add(dgvProductos);
        }

        private void CargarCategorias()
        {
            try
            {
                using (SqlConnection conexion =
                    new SqlConnection(conexionString))
                {
                    SqlDataAdapter adaptador = new SqlDataAdapter(
                        "SELECT ID_Categoria, Nombre FROM Categorias ORDER BY Nombre",
                        conexion);

                    DataTable tabla = new DataTable();
                    adaptador.Fill(tabla);

                    cboCategoria.DataSource = tabla;
                    cboCategoria.DisplayMember = "Nombre";
                    cboCategoria.ValueMember = "ID_Categoria";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar categorías: " + ex.Message);
            }
        }

        private void CargarProductos()
        {
            try
            {
                using (SqlConnection conexion =
                    new SqlConnection(conexionString))
                {
                    string consulta = @"
                        SELECT
                            p.ID_Producto,
                            p.Codigo,
                            p.Nombre,
                            p.Precio,
                            p.Stock,
                            p.ID_Categoria,
                            c.Nombre AS Categoria,
                            p.Activo
                        FROM Productos p
                        LEFT JOIN Categorias c
                            ON p.ID_Categoria = c.ID_Categoria
                        WHERE p.Codigo LIKE @Buscar
                           OR p.Nombre LIKE @Buscar
                        ORDER BY p.ID_Producto DESC";

                    using (SqlCommand comando =
                        new SqlCommand(consulta, conexion))
                    {
                        comando.Parameters.AddWithValue(
                            "@Buscar", "%" + txtBuscar.Text.Trim() + "%");

                        SqlDataAdapter adaptador =
                            new SqlDataAdapter(comando);

                        DataTable tabla = new DataTable();
                        adaptador.Fill(tabla);

                        dgvProductos.DataSource = tabla;

                        dgvProductos.Columns["ID_Producto"].Visible = false;
                        dgvProductos.Columns["ID_Categoria"].Visible = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar productos: " + ex.Message);
            }
        }

        private bool ValidarCampos()
        {
            if (string.IsNullOrWhiteSpace(txtCodigo.Text) ||
                string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("Ingrese código y nombre.");
                return false;
            }

            if (!decimal.TryParse(txtPrecio.Text, out decimal precio) ||
                precio <= 0)
            {
                MessageBox.Show("Ingrese un precio válido mayor que cero.");
                return false;
            }

            if (cboCategoria.SelectedValue == null ||
                cboCategoria.SelectedValue is DataRowView)
            {
                MessageBox.Show("Seleccione una categoría.");
                return false;
            }

            return true;
        }

        private void BtnAgregar_Click(object sender, EventArgs e)
        {
            if (!ValidarCampos()) return;

            try
            {
                using (SqlConnection conexion =
                    new SqlConnection(conexionString))
                {
                    string consulta = @"
                        INSERT INTO Productos
                            (Codigo, Nombre, Precio, Stock, Activo, ID_Categoria)
                        VALUES
                            (@Codigo, @Nombre, @Precio, @Stock, 1, @Categoria)";

                    using (SqlCommand comando =
                        new SqlCommand(consulta, conexion))
                    {
                        AgregarParametros(comando);
                        conexion.Open();
                        comando.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Producto agregado correctamente.");
                LimpiarCampos();
                CargarProductos();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al agregar: " + ex.Message);
            }
        }

        private void BtnActualizar_Click(object sender, EventArgs e)
        {
            if (idSeleccionado == 0)
            {
                MessageBox.Show("Seleccione un producto de la tabla.");
                return;
            }

            if (!ValidarCampos()) return;

            try
            {
                using (SqlConnection conexion =
                    new SqlConnection(conexionString))
                {
                    string consulta = @"
                        UPDATE Productos
                        SET Codigo = @Codigo,
                            Nombre = @Nombre,
                            Precio = @Precio,
                            Stock = @Stock,
                            ID_Categoria = @Categoria
                        WHERE ID_Producto = @ID";

                    using (SqlCommand comando =
                        new SqlCommand(consulta, conexion))
                    {
                        AgregarParametros(comando);
                        comando.Parameters.Add("@ID", SqlDbType.Int)
                            .Value = idSeleccionado;

                        conexion.Open();
                        comando.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Producto actualizado.");
                LimpiarCampos();
                CargarProductos();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar: " + ex.Message);
            }
        }

        private void BtnDesactivar_Click(object sender, EventArgs e)
        {
            if (idSeleccionado == 0)
            {
                MessageBox.Show("Seleccione un producto.");
                return;
            }

            string accion = productoActivo ? "desactivar" : "reactivar";

            DialogResult respuesta = MessageBox.Show(
                $"¿Desea {accion} este producto?",
                "Confirmar",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (respuesta != DialogResult.Yes)
                return;

            try
            {
                using (SqlConnection conexion =
                    new SqlConnection(conexionString))
                using (SqlCommand comando = new SqlCommand(
                    @"UPDATE Productos
              SET Activo = @Activo
              WHERE ID_Producto = @ID", conexion))
                {
                    comando.Parameters.Add("@Activo", SqlDbType.Bit)
                        .Value = !productoActivo;

                    comando.Parameters.Add("@ID", SqlDbType.Int)
                        .Value = idSeleccionado;

                    conexion.Open();

                    int filas = comando.ExecuteNonQuery();

                    if (filas == 0)
                    {
                        MessageBox.Show("No se encontró el producto.");
                        return;
                    }
                }

                MessageBox.Show(
                    productoActivo
                        ? "Producto desactivado correctamente."
                        : "Producto reactivado correctamente.");

                LimpiarCampos();
                CargarProductos();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al cambiar el estado: " + ex.Message);
            }
        }

        private void AgregarParametros(SqlCommand comando)
        {
            comando.Parameters.Add("@Codigo", SqlDbType.VarChar, 20)
                .Value = txtCodigo.Text.Trim();

            comando.Parameters.Add("@Nombre", SqlDbType.VarChar, 100)
                .Value = txtNombre.Text.Trim();

            SqlParameter parametroPrecio =
                comando.Parameters.Add("@Precio", SqlDbType.Decimal);

            parametroPrecio.Precision = 10;
            parametroPrecio.Scale = 2;
            parametroPrecio.Value = decimal.Parse(txtPrecio.Text);

            comando.Parameters.Add("@Stock", SqlDbType.Int)
                .Value = Convert.ToInt32(nudStock.Value);

            comando.Parameters.Add("@Categoria", SqlDbType.Int)
                .Value = Convert.ToInt32(cboCategoria.SelectedValue);
        }

        private void DgvProductos_CellClick(
    object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow fila = dgvProductos.Rows[e.RowIndex];

            idSeleccionado = Convert.ToInt32(
                fila.Cells["ID_Producto"].Value);

            txtCodigo.Text = Convert.ToString(
                fila.Cells["Codigo"].Value);

            txtNombre.Text = Convert.ToString(
                fila.Cells["Nombre"].Value);

            txtPrecio.Text = Convert.ToDecimal(
                fila.Cells["Precio"].Value).ToString("0.00");

            nudStock.Value = Convert.ToDecimal(
                fila.Cells["Stock"].Value);

            object categoria = fila.Cells["ID_Categoria"].Value;

            if (categoria != DBNull.Value)
                cboCategoria.SelectedValue = Convert.ToInt32(categoria);
            else
                cboCategoria.SelectedIndex = -1;

            // Identificar si el producto está activo.
            productoActivo = Convert.ToBoolean(
                fila.Cells["Activo"].Value);

            // Cambiar el texto del botón.
            btnEstado.Text = productoActivo
                ? "Desactivar"
                : "Reactivar";

            // Cambiar el color del botón.
            btnEstado.BackColor = productoActivo
                ? Color.IndianRed
                : Color.SeaGreen;
        }

        private void BtnLimpiar_Click(object sender, EventArgs e)
        {
            LimpiarCampos();
        }

        private void LimpiarCampos()
        {
            idSeleccionado = 0;
            txtCodigo.Clear();
            txtNombre.Clear();
            txtPrecio.Clear();
            nudStock.Value = 0;
            cboCategoria.SelectedIndex = -1;
            dgvProductos.ClearSelection();
            productoActivo = true;
            btnEstado.Text = "Desactivar";
            btnEstado.BackColor = Color.IndianRed;
        }
    }
}
