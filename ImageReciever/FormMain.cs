using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
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
        bool quit = false;
        bool dragging = false;
        Point dragPoint;
        Point drawPos;

        public FormMain()
        {
            InitializeComponent();

            toolTip.SetToolTip(pictureBox, null);

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
                            var oldImage = pictureBox.Image;
                            pictureBox.Image = null;
                            toolTip.SetToolTip(pictureBox, null);
                            pictureBox.Refresh();
                            if (oldImage != null)
                            {
                                oldImage.Dispose();
                            }

                            Thread.Sleep(100);  // わざと0.1秒待って、空白表示時間を作る

                            pictureBox.Image = image;
                            drawPos.X = (pictureBox.ClientRectangle.Width - image.Width) / 2;
                            drawPos.Y = (pictureBox.ClientRectangle.Height - image.Height) / 2;
                            dragging = false;
                            pictureBox.Refresh();
                            toolTip.SetToolTip(pictureBox, string.Format("{0}x{1} {2}", image.Width, image.Height, image.PixelFormat));
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

        private void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || pictureBox.Image == null)
            {
                return;
            }

            this.Cursor = Cursors.Hand;

            dragging = true;
            dragPoint = new Point(e.X, e.Y);
        }

        private void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (pictureBox.Image == null || !dragging)
            {
                return;
            }

            var pos = new Point();
            pos.X = drawPos.X + e.X - dragPoint.X;
            pos.Y = drawPos.Y + e.Y - dragPoint.Y;

            var canvas = new Bitmap(pictureBox.ClientRectangle.Width, pictureBox.ClientRectangle.Height);
            var graphics = Graphics.FromImage(canvas);
            graphics.FillRectangle(new SolidBrush(pictureBox.BackColor), pictureBox.ClientRectangle);
            graphics.DrawImage(pictureBox.Image, pos.X, pos.Y, pictureBox.Image.Width, pictureBox.Image.Height);
            graphics.Dispose();
            graphics = pictureBox.CreateGraphics();
            graphics.DrawImage(canvas, new Point(0, 0));
            graphics.Dispose();
            canvas.Dispose();
        }

        private void pictureBox_MouseLeave(object sender, EventArgs e)
        {
            if (pictureBox.Image == null || !dragging)
            {
                return;
            }

            this.Cursor = Cursors.Arrow;

            dragging = false;

            var canvas = new Bitmap(pictureBox.ClientRectangle.Width, pictureBox.ClientRectangle.Height);
            var graphics = Graphics.FromImage(canvas);
            graphics.FillRectangle(new SolidBrush(pictureBox.BackColor), pictureBox.ClientRectangle);
            graphics.DrawImage(pictureBox.Image, drawPos.X, drawPos.Y, pictureBox.Image.Width, pictureBox.Image.Height);
            graphics.Dispose();
            graphics = pictureBox.CreateGraphics();
            graphics.DrawImage(canvas, new Point(0, 0));
            graphics.Dispose();
            canvas.Dispose();
        }

        private void pictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (pictureBox.Image == null || !dragging)
            {
                return;
            }

            this.Cursor = Cursors.Arrow;

            dragging = false;

            drawPos.X += e.X - dragPoint.X;
            drawPos.Y += e.Y - dragPoint.Y;

            var canvas = new Bitmap(pictureBox.ClientRectangle.Width, pictureBox.ClientRectangle.Height);
            var graphics = Graphics.FromImage(canvas);
            graphics.FillRectangle(new SolidBrush(pictureBox.BackColor), pictureBox.ClientRectangle);
            graphics.DrawImage(pictureBox.Image, drawPos.X, drawPos.Y, pictureBox.Image.Width, pictureBox.Image.Height);
            graphics.Dispose();
            graphics = pictureBox.CreateGraphics();
            graphics.DrawImage(canvas, new Point(0, 0));
            graphics.Dispose();
            canvas.Dispose();
        }
    }
}
