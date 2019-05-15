using System;
using System.Collections.Specialized;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Playhub
{
    public partial class Form2 : Form
    {
        private Graphics _graphicPanel;
        public Form2()
        {
            InitializeComponent();
            ProtocolProcess.Players.CollectionChanged += PlayersOnCollectionChanged;
            ProtocolProcess.Messages.CollectionChanged += MessagesOnCollectionChanged;
            ProtocolProcess.PanelShapes.CollectionChanged += PanelShapesOnCollectionChanged;
            _graphicPanel = panel1.CreateGraphics();
            if (PlayerSetting.IsHost)
            {
                button2.Visible = true;
            }
        }

        private void PanelShapesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _graphicPanel.Clear(DefaultBackColor);

            foreach (var item in ProtocolProcess.PanelShapes)
            {
                if (item.Type == 0)
                {
                    _graphicPanel.FillEllipse(new SolidBrush(item.Color), item.Point);
                }
                else if (item.Type == 1)
                {
                    _graphicPanel.FillRectangle(new SolidBrush(item.Color), item.Point);
                }
                else if (item.Type == 2)
                {
                    PointF[] point = new PointF[3];
                    point[0].X = item.Point.X + (item.Point.Width / 2);
                    point[0].Y = (item.Point.Y);

                    point[1].X = item.Point.X;
                    point[1].Y = item.Point.Y + (item.Point.Height);

                    point[2].X = (item.Point.X + item.Point.Width);
                    point[2].Y = (item.Point.Y + item.Point.Height);
                    _graphicPanel.FillPolygon(new SolidBrush(item.Color), point);
                }
            }

        }

        private void MessagesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            textBox2.Invoke((MethodInvoker)(() => textBox2.Text += $"{ProtocolProcess.Messages[e.NewStartingIndex]}" + Environment.NewLine));
        }

        private void PlayersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            textBox1.Invoke((MethodInvoker)(() => textBox1.Text = ""));
            foreach (var item in ProtocolProcess.Players)
            {
                textBox1.Invoke((MethodInvoker)(() => textBox1.Text += $"{item.Name} ({item.Point})" + Environment.NewLine));
            }
        }

        private void Form2_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }
        private void Form2_Load(object sender, EventArgs e)
        {
            label9.Text = PlayerSetting.Username;
            ProtocolProcess.RequestGameSettings();
            ProtocolProcess.OnGameSettingsReceivedEvent += delegate (object o, GameSettingsReceivedEvent ev)
                 {
                     if (ev.GameSettings != null)
                     {
                         textBox4.Invoke((MethodInvoker)(() => textBox4.Text = ev.GameSettings.GameName));
                         textBox5.Invoke((MethodInvoker)(() => textBox5.Text = ev.GameSettings.Red));
                         textBox6.Invoke((MethodInvoker)(() => textBox6.Text = ev.GameSettings.Blue));
                         textBox7.Invoke((MethodInvoker)(() => textBox7.Text = ev.GameSettings.Yellow));
                         textBox8.Invoke((MethodInvoker)(() => textBox8.Text = ev.GameSettings.Timer));
                         textBox9.Invoke((MethodInvoker)(() => textBox9.Text = ev.GameSettings.Win));
                         //this.Invoke((MethodInvoker)(() => this.Size = new Size(ev.GameSettings.PanelSize.X + 300, ev.GameSettings.PanelSize.Y + 300)));
                     }
                 };
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            ProtocolProcess.SendMessage(textBox3.Text, PlayerSetting.Username);
            textBox3.Text = "";
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            ProtocolProcess.StartGame();
            button2.Visible = false;
        }

        private void panel1_MouseClick(object sender, MouseEventArgs e)
        {
            ProtocolProcess.SendClickCoor(e.X, e.Y);
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            textBox2.SelectionStart = textBox2.Text.Length;
            textBox2.ScrollToCaret();
        }
    }
}
