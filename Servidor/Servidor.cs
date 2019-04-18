using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace Servidor
{
    public partial class Servidor : Form
    {
        private TcpListener _tcpListnerChatSever;
        public static Hashtable hstClients;
        public static Hashtable hstConexoes;
        public bool conectado = false;

        private delegate void AtualizaBotoes();

        public Servidor()
        {
            InitializeComponent();
            InicializaEnderecamento();

        }

        private void InicializaEnderecamento()
        {
            var hostName = Dns.GetHostName();
            var ipHost = Dns.GetHostAddresses(hostName);
            stNomeDaMaquina.Text = hostName;
            txtIp.Text = ipHost[ipHost.Length - 1].ToString();
            txtIp.Enabled = false;
        }

        private void txtPorta_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != (char)Keys.Back && e.KeyChar != (char)Keys.Enter && e.KeyChar != (char)Keys.End)
            {
                e.Handled = true;
                MessageBox.Show("Somente números nesse campo", "Aviso", MessageBoxButtons.OK ,MessageBoxIcon.Information);
            }
        }

        private void btIniciar_Click(object sender, EventArgs e)
        {
            hstClients = new Hashtable(150);
            hstConexoes = new Hashtable(150);

           
            var thread = new Thread(Inicia)
            {
                IsBackground = true
            };
            thread.Start();
        }

        private void AtualizaBt()
        {
            txtPorta.Enabled = !conectado;
            btIniciar.Enabled = !conectado;
            lblStatus.Enabled = !conectado;
            lblLiberada.Enabled = conectado;
        }

        private void Inicia()
        {
            try
            {
                var iphost = Dns.GetHostAddresses(Dns.GetHostName());
                var ip = new IPEndPoint(iphost[iphost.Length - 1], Convert.ToInt32(txtPorta.Text));
                _tcpListnerChatSever = new TcpListener(ip);
                conectado = true;
                Invoke(new AtualizaBotoes(AtualizaBt), new object[] { });
            }
            catch (Exception e)
            {
                MessageBox.Show("Ops, deu algo de errado ao tentar startar o sever!", "Atenção", MessageBoxButtons.OK);
            }
            while (conectado)
            {
                _tcpListnerChatSever.Start();
                if (_tcpListnerChatSever.Pending())
                {
                    var client = _tcpListnerChatSever.AcceptTcpClient();
                    var newConexao = new Conexao(client);
                }
            }
        }

        public static void EnviaMenssagem(string nick, string msg)
        {
            StreamWriter writer;
            var tcpClient = new TcpClient[hstClients.Count];
            hstClients.Values.CopyTo(tcpClient, 0);



            for (int cnt = 0; cnt < tcpClient.Length; cnt++)
            {
                try
                {
                    if (msg.Trim() == "" || tcpClient[cnt] == null)
                        continue;
                    writer = new StreamWriter(tcpClient[cnt].GetStream());
                    writer.WriteLine(nick + ": " + msg);
                    writer.Flush();
                    writer = null;
                }
                catch (SocketException)
                {
                    MessageBox.Show("Ops! menssagem não enviou...");
                }
            }
        }

        public static void EnviaMenssagemAdmin(string msg)
        {
            StreamWriter writer;
            var tcpClient = new TcpClient[hstClients.Count];
            hstClients.Values.CopyTo(tcpClient, 0);
            for (int i = 0; i < tcpClient.Length; i++)
            {
                try
                {
                    if (msg.Trim() == "" || tcpClient[i] == null)
                        continue;
                    writer = new StreamWriter(tcpClient[i].GetStream());
                    writer.WriteLine(msg + " conectou ...");
                    writer.Flush();
                    writer = null;
                }
                catch (Exception)
                {
                    hstClients.Remove(hstConexoes[tcpClient[i]]);
                    hstConexoes.Remove(tcpClient[i]);
                }
            }
        }

        public static void RemoveClient()
        {

        }

        private void btParar_Click(object sender, EventArgs e)
        {
            conectado = false;

            if (_tcpListnerChatSever != null)
                _tcpListnerChatSever.Stop();

            lblLiberada.Enabled = false;
            lblStatus.Enabled = true;
            Invoke(new AtualizaBotoes(AtualizaBt), new object[] { });
        }
    }

    internal class Conexao
    {
        private TcpClient _tcpClient;
        private StreamReader _str;
        private StreamWriter _stw;
        private string nickName;

        public Conexao(TcpClient cliente)
        {
            _tcpClient = cliente;
            var thread = new Thread(AceitaClient);
            thread.Start();

        }

        private void AceitaClient()
        {
            _str = new StreamReader(_tcpClient.GetStream());
            _stw = new StreamWriter(_tcpClient.GetStream());
            nickName = _str.ReadLine();
            _stw.WriteLine("Conecta!");
            _stw.Flush();
            while (Servidor.hstClients.Contains(nickName))
            {
                _stw.WriteLine($"{nickName}: tentou conexão!");
                Servidor.hstClients.Remove(nickName);
            }
            Servidor.hstClients.Add(nickName, _tcpClient);
            Servidor.hstConexoes.Add(_tcpClient, nickName);

            Servidor.EnviaMenssagemAdmin(nickName);
            var thread = new Thread(IniciaObservadorChat);
            thread.Start();
        }


        private void IniciaObservadorChat()
        {

            try
            {
                string line = "";
                while (true)
                {
                    line = _str.ReadLine();
                    Servidor.EnviaMenssagem(nickName, line);
                }
            }
            catch (Exception e44)
            {
                Console.WriteLine(e44);
            }
        }
    }

}


