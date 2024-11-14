using MySql.Data.MySqlClient;
using System;
using System.IO.Ports;
using System.Windows.Forms;

namespace proyectoFinal
{
    public partial class Form1 : Form
    {
        private SerialPort SP;
        private Timer SDTimer;
        private MySqlConnection SQLConnection;
        private string SQLString = "Server=localhost;Database=elevador_db;Uid=root;Pwd=8j97d32sY_;";

        // Variables para almacenar datos del elevador
        private int pisoActual = 1;
        //private float pesoActual = 0;
        private bool limiteExcedido = false;
        private bool gasDetectado = false;
        private bool obstaculoDetectado = false;
        public Form1()
        {
            InitializeComponent();
            InitializeSP();
            InitializeDB();
            InitializeTimer();
        }

        private void InitializeSP()
        {
            SP = new SerialPort("COM4", 9600); // Cambia COM3 según corresponda
            SP.DataReceived += SP_DataReceived;
            SP.Open();
        }

        private void InitializeDB()
        {
            try
            {
                SQLConnection = new MySqlConnection(SQLString);
                SQLConnection.Open();
                MessageBox.Show("Conexión a la base de datos exitosa.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al conectar a la base de datos: {ex.Message}");
            }
        }

        private void InitializeTimer()
        {
            SDTimer = new Timer();
            SDTimer.Interval = 30000;
            SDTimer.Tick += SDTimer_Marca;
            SDTimer.Start();
        }

        private void SP_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!IsHandleCreated || IsDisposed) return; // Verifica que el formulario esté activo

            string data = SP.ReadLine();
            Invoke(new Action(() => ParseMostrarDatos(data)));
        }

        private void ParseMostrarDatos(string data)
        {
            string[] lines = data.Split('\n');
            foreach (string line in lines)
            {
                if (line.StartsWith("PISO:"))
                {
                    pisoActual = int.Parse(line.Substring(5));
                    lblPiso.Text = pisoActual.ToString();
                }
                /*else if (line.StartsWith("PESO:"))
                {
                    pesoActual = float.Parse(line.Substring(5));
                    lblPeso.Text = pesoActual.ToString("0.00");
                    limiteExcedido = pesoActual > 100;
                }*/
                else if (line.StartsWith("GAS:"))
                {
                    gasDetectado = line.Substring(4).Trim() == "DETECTADO";
                    lblGas.Text = gasDetectado ? "Sí" : "No";
                }
                else if (line.StartsWith("OBSTACULO:"))
                {
                    obstaculoDetectado = line.Substring(10).Trim() == "DETECTADO";
                    lblObstaculo.Text = obstaculoDetectado ? "Sí" : "No";
                }

                // Guarda en la base de datos si se cumplen condiciones
                if (limiteExcedido || gasDetectado || obstaculoDetectado)
                {
                    SDaBaseDatos();
                }
            }
        }
        private void SDTimer_Marca(object sender, EventArgs e)
        {
            SDaBaseDatos();
        }

        private void SDaBaseDatos()
        {
            //string query = "INSERT INTO datos_elevador (fecha_hora, piso_actual, peso, limite_excedido, gas_detectado, obstaculo_detectado) " +
                           //"VALUES (@fecha_hora, @piso_actual, @peso, @limite_excedido, @gas_detectado, @obstaculo_detectado)";

            string query = "INSERT INTO datos_elevador (fecha_hora, piso_actual, gas_detectado, obstaculo_detectado) " +
                           "VALUES (@fecha_hora, @piso_actual, @gas_detectado, @obstaculo_detectado)";
            try
            {
                using (MySqlCommand cmd = new MySqlCommand(query, SQLConnection))
                {
                    cmd.Parameters.AddWithValue("@fecha_hora", DateTime.Now);
                    cmd.Parameters.AddWithValue("@piso_actual", pisoActual);
                    //cmd.Parameters.AddWithValue("@peso", pesoActual);
                    //cmd.Parameters.AddWithValue("@limite_excedido", limiteExcedido);
                    cmd.Parameters.AddWithValue("@gas_detectado", gasDetectado);
                    cmd.Parameters.AddWithValue("@obstaculo_detectado", obstaculoDetectado);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar en la base de datos: {ex.Message}");
            }
        }

        private void btnPiso1_Click(object sender, EventArgs e)
        {
            SP.WriteLine("PISO:1\n");
        }

        private void btnPiso2_Click(object sender, EventArgs e)
        {
            SP.WriteLine("PISO:2\n");
        }

        private void btnPiso3_Click(object sender, EventArgs e)
        {
            SP.WriteLine("PISO:3\n");
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                SDTimer?.Stop();
                SDTimer?.Dispose();

                if (SP != null && SP.IsOpen)
                {
                    SP.DataReceived -= SP_DataReceived; // Eliminar la suscripción al evento
                    SP.Close();
                }
                SP?.Dispose();

                if (SQLConnection != null && SQLConnection.State == System.Data.ConnectionState.Open)
                {
                    SQLConnection.Close();
                }
                SQLConnection?.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cerrar recursos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
