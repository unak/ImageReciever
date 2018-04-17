using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageReciever
{
    public partial class FormMain : Form
    {
        Socket server;
        Thread listener;
        CountdownEvent waiter = new CountdownEvent(1);
        bool quit;

        public FormMain()
        {
            InitializeComponent();

            quit = false;
            waiter.Reset();
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(new IPEndPoint(IPAddress.Parse("0.0.0.0"), 13908));
            this.Text = string.Format("{0} - {1}", this.Port, "ImageReciever");
            server.Listen(1000);
            listener = new Thread(() => {
                while (!quit)
                {
                    server.BeginAccept((ar) => {
                        var server = ar.AsyncState as Socket;
                        var socket = server.EndAccept(ar);
                        waiter.Signal();
                        var ms = new MemoryStream();
                        var capa = 1024 * 1024;
                        var buf = new byte[capa];
                        var size = capa;
                        while (size > 0)
                        {
                            try
                            {
                                size = socket.Receive(buf, capa, SocketFlags.None);
                            }
                            catch
                            {
                                socket.Close();
                                return;
                            }
                            ms.Write(buf, 0, size);
                        }
                        socket.Close();

                        Image image;
                        try
                        {
                            image = Image.FromStream(ms);
                        }
                        catch
                        {
                            return;
                        }
                        Invoke((MethodInvoker)(() =>
                        {
                            pictureBox.Image = null;
                            pictureBox.Refresh();
                            Thread.Sleep(100);  // わざと0.1秒待って、空白表示時間を作る
                            pictureBox.Image = image;
                            pictureBox.Refresh();
                        }), null);
                    }, server);
                    waiter.Wait();
                    waiter.Reset();
                }
            });
            listener.Start();
        }

        private int Port
        {
            get {
                var endPoint = server.LocalEndPoint as IPEndPoint;
                return endPoint.Port;
            }
        }

        private void FormMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            quit = true;
            waiter.Signal();
            for (var i = 0; i < 10; i++)
            {
                if (!listener.IsAlive)
                {
                    break;
                }
                Thread.Sleep(100);
            }
            if (!listener.IsAlive)
            {
                listener.Abort();
            }
        }
    }
}
