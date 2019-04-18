using System.Windows.Forms;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System;

namespace Cliente
{
    public partial class Cliente : Form
    {
        private string _NomeUsuario = string.Empty;
        private StreamWriter _stwEnviador;
        private StreamReader _strReceptor;
        private Thread _mensagemThread;
        private bool _Conectado;
        private TcpClient _cliente;

        public Cliente()
        {
            Application.ApplicationExit += new EventHandler(OnApplicationExit);
            InitializeComponent();
        }

        /*
         * Necessário para atualizar o formulário com mensagens da outra thread
         */
        private delegate void AtualizaLogCallBack(string strMensagem);

        /*
         * Necessário para definir o formulário para o estado "Desconectado" de outra thread
         */
        private delegate void FechaConexaoCallBack(string strMotivo);

        private void btnSubmeter_Click(object sender, EventArgs e)
        {
            if (!_Conectado)
            {
                _cliente = new TcpClient();
                _NomeUsuario = txtNick.Text;
                _cliente.Connect(txtIP.Text, Convert.ToInt32(txtPort.Text));

                _Conectado = true;
                txtIP.Enabled = false;
                txtPort.Enabled = false;
                txtNick.Enabled = false;
                btnSubmeter.Text = "Desconectar";

                _stwEnviador = new StreamWriter(_cliente.GetStream());
                _stwEnviador.WriteLine(_NomeUsuario);
                _stwEnviador.Flush();

                _mensagemThread = new Thread(new ThreadStart(RecebeMenssagem));
                _mensagemThread.Start();
            }
            
        }

        private void RecebeMenssagem()
        {
            _strReceptor = new StreamReader(_cliente.GetStream());
            var conResposta = _strReceptor.ReadLine();

            if (conResposta != null)
            {
                this.Invoke(new AtualizaLogCallBack(this.AtualizaLog), new object[] { "Conectado com sucesso!" });
            }
            //else
            //{
            //    string Motivo = "Não Conectado: ";
            //    Motivo += conResposta.Substring(2, conResposta.Length - 2);
            //    this.Invoke(new FechaConexaoCallBack(this.FechaConexao), new object[] { Motivo });
            //    return;
            //}

            while (_Conectado)
            {
                this.Invoke(new AtualizaLogCallBack(this.AtualizaLog), new object[] { _strReceptor.ReadLine() });
            }
        }

        public void EnviaMensagem()
        {
            if (txtEntrada.Lines.Length >= 1)
            {
                _stwEnviador.WriteLine(txtEntrada.Text);
                _stwEnviador.Flush();
                txtEntrada.Lines = null;
            }

            txtEntrada.Text = string.Empty;
        }


        private void txtEntrada_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                EnviaMensagem();
            }
        }

        private void btnEnviar_Click(object sender, EventArgs e)
        {
           EnviaMensagem();
        }

        private void AtualizaLog(string strMensagem)
        {
            txtSaida.AppendText(strMensagem + "\r\n");
        }

        private void FechaConexao(string Motivo)
        {
            txtEntrada.AppendText(Motivo + "\r\n");

             txtIP.Enabled = true;
            txtNick.Enabled = true;
            btnEnviar.Enabled = false;
            btnSubmeter.Text = "Conectar";

            _Conectado = false;
            _stwEnviador.Close();
            _strReceptor.Close();
            _cliente.Close();
        }

        public void OnApplicationExit(object sender, EventArgs e)
        {
            if (_Conectado == true)
            {
                _Conectado = false;
                _stwEnviador.Close();
                _strReceptor.Close();
                _cliente.Close();
            }
        }

    }
}
