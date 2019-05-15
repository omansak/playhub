using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Playhub
{
    public static class GameServer
    {
        //Game Settings
        public static string GameAdrress { get; set; }
        public static int GamePort { get; set; }
        public static string GameName { get; set; }
        public static int GameRefreshTime { get; set; }
        public static int GameWinPoint { get; set; }
        public static int GameShapeRed { get; set; }
        public static int GameShapeBlue { get; set; }
        public static int GameShapeYellow { get; set; }
        public static int GamePanelSizeX { get; set; }
        public static int GamePanelSizeY { get; set; }

        private static bool GameRunning = false;

        //Game List
        private static ObservableCollection<ProtocolModel.Player> Players { get; set; }
        private static List<ProtocolModel.ConnectionClient> Clients { get; set; }
        private static List<ProtocolModel.Shapes> ShapesPanel { get; set; }

        //Data Handler event
        public static event EventHandler<DataReceivedEvent> OnDataReceived;

        //Stop tokens (not using)
        private static CancellationTokenSource _cancellationServerToken;
        private static CancellationTokenSource _cancellationClientToken;

        //Socket
        private static TcpListener GameSocket { get; set; }
        private static TcpClient PlayerClient { get; set; }
        //Create
        public static void CreateSocket()
        {
            if (GameAdrress == null || GamePort == 0)
            {
                throw new ArgumentNullException("GameAdrress or GamePort");
            }
            GameSocket = new TcpListener(IPAddress.Parse(GameAdrress), GamePort);
            GameSocket.Start();

        }
        public static async void CreateClient()
        {
            if (GameAdrress == null || GamePort == 0)
            {
                throw new ArgumentNullException("GameAdrress or GamePort");
            }

            if (PlayerClient == null)
            {
                PlayerClient = new TcpClient
                {
                    NoDelay = true
                };
                await PlayerClient.ConnectAsync(IPAddress.Parse(GameAdrress), GamePort);
            }

        }
        //Listener
        public static async Task StartServerSocketAsync()
        {
            _cancellationServerToken = CancellationTokenSource.CreateLinkedTokenSource(new CancellationToken());
            while (!_cancellationServerToken.Token.IsCancellationRequested)
            {
                await Task.Run(async () =>
                {
                    TcpClient client = await GameSocket.AcceptTcpClientAsync();
                    client.NoDelay = true;
                    ProtocolModel.ConnectionClient connectionClient = new ProtocolModel.ConnectionClient
                    {
                        Id = Guid.NewGuid().ToString(),
                        Client = client
                    };
                    Clients.Add(connectionClient);
                    ReadFromClient(connectionClient);
                }, _cancellationServerToken.Token);
            }
        }

        //Listener Process From Client Data
        private static async void ReadFromClient(ProtocolModel.ConnectionClient connectionClient)
        {
            try
            {
                await Task.Run(async () =>
                {
                    while (connectionClient.Client.Connected)
                    {
                        if (connectionClient.Client.GetStream().DataAvailable)
                        {
                            NetworkStream stream = connectionClient.Client.GetStream();
                            byte[] data = new byte[1024];
                            string json = string.Empty;
                            var bytes = stream.Read(data, 0, data.Length);
                            if (bytes > 0)
                            {
                                json = Encoding.ASCII.GetString(data, 0, bytes);
                            }
                            //Data Processing
                            var token = json.Split(new[] { "\\;omansak;\\" }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var msgToken in token)
                            {
                                ProtocolModel.Base baseMessage = JsonConvert.DeserializeObject<ProtocolModel.Base>(msgToken);
                                switch (baseMessage.Type)
                                {
                                    // Join new player and send current players
                                    case ProtocolModel.MessageType.RequestJoin:                                       
                                        {
                                            ProtocolModel.Player player = JsonConvert.DeserializeObject<ProtocolModel.Player>(baseMessage.Data.ToString());
                                            player.Id = connectionClient.Id;
                                            Players.Add(player);
                                            BroadcastPlayers();
                                            RequestSendMessage($"--> {Players.First(i => i.Id == connectionClient.Id).Name} is joined.");
                                            break;
                                        }
                                    // Lobby Messages
                                    case ProtocolModel.MessageType.LobbyMessage:
                                        {
                                            BroadcastFromServer(json);
                                            break;
                                        }
                                    // Start Game
                                    case ProtocolModel.MessageType.RequestStart:
                                        {
                                            GameRunning = true;
                                            GenerateShapes();
                                            RequestSendMessage($"--> Game started by {Players.First(i => i.Id == connectionClient.Id).Name} (host)");
                                            break;
                                        }
                                    // Game Settings
                                    case ProtocolModel.MessageType.RequestGameSettings:
                                        {
                                            BroadcastFromServer(JsonConvert.SerializeObject(new ProtocolModel.Base
                                            {
                                                Type = ProtocolModel.MessageType.RequestGameSettings,
                                                Data = new ProtocolModel.GameSetting
                                                {
                                                    GameName = GameName,
                                                    Red = GameShapeRed.ToString(),
                                                    Blue = GameShapeBlue.ToString(),
                                                    Yellow = GameShapeYellow.ToString(),
                                                    Timer = GameRefreshTime.ToString(),
                                                    Win = GameWinPoint.ToString(),
                                                    PanelSize = new Point(GamePanelSizeX, GamePanelSizeY)
                                                }

                                            }));
                                            break;
                                        }
                                    // Check coor inside objects
                                    case ProtocolModel.MessageType.RequestSendCoor:
                                        {
                                            ProtocolModel.ClickCoor point = JsonConvert.DeserializeObject<ProtocolModel.ClickCoor>(baseMessage.Data.ToString());
                                            for (int j = ShapesPanel.Count - 1; j >= 0; j--)
                                            {
                                                var item = ShapesPanel[j];
                                                if (item.Type == 0)
                                                {
                                                    if (ContainsEllipse(point.Coor, item.Point))
                                                    {
                                                        ShapesPanel.Remove(item);
                                                        Players.First(i => i.Id == connectionClient.Id).Point += GameShapeRed;
                                                        BroadcastShapes();
                                                        BroadcastPlayers();
                                                        RequestSendMessage($"--> {Players.First(i => i.Id == connectionClient.Id).Name} is gained {GameShapeRed} with Red");
                                                        break;
                                                    }
                                                }
                                                if (item.Type == 1)
                                                {
                                                    if (ContainsRectangle(point.Coor, item.Point))
                                                    {
                                                        ShapesPanel.Remove(item);
                                                        Players.First(i => i.Id == connectionClient.Id).Point += GameShapeBlue;
                                                        BroadcastShapes();
                                                        BroadcastPlayers();
                                                        RequestSendMessage($"--> {Players.First(i => i.Id == connectionClient.Id).Name} is gained {GameShapeBlue} with Blue");
                                                        break;
                                                    }
                                                }
                                                if (item.Type == 2)
                                                {
                                                    if (ContainsTriangle(point.Coor, item.Point))
                                                    {
                                                        ShapesPanel.Remove(item);
                                                        Players.First(i => i.Id == connectionClient.Id).Point += GameShapeYellow;
                                                        BroadcastShapes();
                                                        BroadcastPlayers();
                                                        RequestSendMessage($"--> {Players.First(i => i.Id == connectionClient.Id).Name} is gained {GameShapeYellow} with Yellow");
                                                        break;
                                                    }
                                                }
                                            }
                                            break;
                                        }
                                }
                            }
                        }
                        Thread.Sleep(25);
                    }
                });
            }
            finally
            {
                Players.Remove(Players.First(i => i.Id == connectionClient.Id));
                connectionClient.Client.Close();
                BroadcastPlayers();
            }

        }
        public static async Task StartClientSocketAsync()
        {
            try
            {
                _cancellationClientToken = CancellationTokenSource.CreateLinkedTokenSource(new CancellationToken());
                while (!_cancellationClientToken.Token.IsCancellationRequested)
                {
                    await Task.Run(() =>
                    {
                        if (PlayerClient.Connected && PlayerClient.GetStream().DataAvailable &&
                            PlayerClient.GetStream().CanRead)
                        {
                            NetworkStream stream = PlayerClient.GetStream();
                            byte[] data = new byte[1024 * 1024];
                            string json = string.Empty;
                            var bytes = stream.Read(data, 0, data.Length);
                            if (bytes > 0)
                            {
                                json = Encoding.ASCII.GetString(data, 0, bytes);
                            }
                            OnDataReceived?.Invoke(null, new DataReceivedEvent(json));
                        }
                        Thread.Sleep(25);
                    }, _cancellationClientToken.Token);
                }
            }
            catch
            {
                MessageBox.Show("Client disconnected");
            }
            finally
            {
                PlayerClient.Client.Close();
            }

        }
        //Game
        static GameServer()
        {
            Players = new ObservableCollection<ProtocolModel.Player>();
            Clients = new List<ProtocolModel.ConnectionClient>();
            ShapesPanel = new List<ProtocolModel.Shapes>();
        }
        static void CreateRandomShape()
        {
            Random rnd = new Random();
            switch (rnd.Next(0, 3))
            {
                case 0:
                    {
                        ProtocolModel.Shapes shape = new ProtocolModel.Shapes
                        {
                            Type = 0,
                            Color = Color.Red,
                            Point = new RectangleF(new PointF(rnd.Next(0, GamePanelSizeX), rnd.Next(0, GamePanelSizeY)),
                                new SizeF(50, 50))
                        };
                        ShapesPanel.Add(shape);
                        break;
                    }
                case 1:
                    {
                        ProtocolModel.Shapes shape = new ProtocolModel.Shapes
                        {
                            Type = 1,
                            Color = Color.Blue,
                            Point = new RectangleF(new PointF(rnd.Next(0, GamePanelSizeX), rnd.Next(0, GamePanelSizeY)),
                                new SizeF(30, 30))
                        };
                        ShapesPanel.Add(shape);
                        break;
                    }
                case 2:
                    {
                        ProtocolModel.Shapes shape = new ProtocolModel.Shapes
                        {
                            Type = 2,
                            Color = Color.Yellow,
                            Point = new RectangleF(new PointF(rnd.Next(0, GamePanelSizeX), rnd.Next(0, GamePanelSizeY)),
                                new SizeF(40, 40))
                        };
                        ShapesPanel.Add(shape);
                        break;
                    }

            }
            BroadcastShapes();
        }
        //Broadcast
        public static void BroadcastFromServer(string data)
        {
            Task.Run(() =>
          {
              for (int i = 0; i < Clients.Count; i++)
              {
                  var client = Clients[i];
                  if (client.Client.Connected && client.Client.GetStream().CanWrite)
                  {
                      try
                      {
                          NetworkStream stream = client.Client.GetStream();
                          byte[] buffer = Encoding.ASCII.GetBytes(data + "\\;omansak;\\");
                          if (client.Client.Connected)
                              stream.Write(buffer, 0, buffer.Length);
                      }
                      catch
                      {

                      }

                  }
                  else
                  {
                      client.Client.Close();
                      Clients.Remove(client);
                  }

              }
          });
        }
        public static void BroadcastPlayers()
        {
            BroadcastFromServer(JsonConvert.SerializeObject(new ProtocolModel.Base
            {
                Type = ProtocolModel.MessageType.ResponsePlayers,
                DateTime = DateTime.Now,
                Data = new ProtocolModel.PlayerList
                {
                    Players = new ObservableCollection<ProtocolModel.Player>(Players.OrderByDescending(i => i.Point))
                }
            }));
            BroadcastWinner();
        }
        public static void BroadcastWinner()
        {
            if (Players.Any(i => i.Point >= GameWinPoint))
            {
                ShapesPanel.Clear();
                BroadcastShapes();
                RequestSendMessage($"{Players.First(i => i.Point >= GameWinPoint).Name} ({Players.First(i => i.Point >= GameWinPoint).Point}) is Won");
                RequestSendMessage($"Game Finished");
                GameRunning = false;
            }

        }
        public static void BroadcastShapes()
        {
            try
            {
                BroadcastFromServer(JsonConvert.SerializeObject(new ProtocolModel.Base
                {
                    Type = ProtocolModel.MessageType.ResponseShapes,
                    DateTime = DateTime.Now,
                    Data = ShapesPanel
                }));
            }
            catch
            {
                //
            }

        }
        //Request
        public static void SendFromClient(string data)
        {
            if (PlayerClient.Connected)
            {
                NetworkStream stream = PlayerClient.GetStream();
                byte[] buffer = Encoding.ASCII.GetBytes(data + "\\;omansak;\\");
                stream.Write(buffer, 0, buffer.Length);
            }

        }
        public static void RequestJoin(string playerName)
        {
            var json = JsonConvert.SerializeObject(new ProtocolModel.Base
            {
                DateTime = DateTime.Now,
                Type = ProtocolModel.MessageType.RequestJoin,
                Data = new ProtocolModel.Player
                {
                    Name = playerName,
                    Point = 0
                }
            });
            SendFromClient(json);
        }
        public static void RequestSendMessage(string message, string userName = "")
        {
            var json = JsonConvert.SerializeObject(new ProtocolModel.Base
            {
                DateTime = DateTime.Now,
                Type = ProtocolModel.MessageType.LobbyMessage,
                Data = new ProtocolModel.LobbyMessage()
                {
                    Player = userName,
                    Message = message
                }
            });
            SendFromClient(json);
        }
        public static void RequestGameSettings()
        {
            var json = JsonConvert.SerializeObject(new ProtocolModel.Base
            {
                DateTime = DateTime.Now,
                Type = ProtocolModel.MessageType.RequestGameSettings,
                Data = ""
            });
            SendFromClient(json);
        }
        public static void RequestStartGame()
        {
            var json = JsonConvert.SerializeObject(new ProtocolModel.Base
            {
                DateTime = DateTime.Now,
                Type = ProtocolModel.MessageType.RequestStart,
                Data = ""
            });
            SendFromClient(json);
        }
        public static void RequestSendClickCoor(int x, int y)
        {
            var json = JsonConvert.SerializeObject(new ProtocolModel.Base
            {
                DateTime = DateTime.Now,
                Type = ProtocolModel.MessageType.RequestSendCoor,
                Data = new ProtocolModel.ClickCoor
                {
                    Coor = new Point(x, y)
                }
            });
            SendFromClient(json);
        }
        //Utilites
        private static bool ContainsEllipse(Point location, RectangleF shape)
        {

            return (((int)Math.Pow((location.X - ((shape.X + (shape.Width / 2)))), 2) /
                    (int)Math.Pow(shape.Width / 2, 2)) +
                   ((int)Math.Pow((location.Y - (shape.Y + (shape.Height / 2))), 2) /
                    (int)Math.Pow(shape.Height / 2, 2))) < 1;

        }
        private static bool ContainsRectangle(Point location, RectangleF shape)
        {

            return (location.X > shape.X &&
                    location.X < (shape.Width + shape.X) &&
                    location.Y > shape.Y &&
                    location.Y < (shape.Height + shape.Y));
        }
        private static bool ContainsTriangle(Point location, RectangleF shape)
        {
            PointF[] point = new PointF[3];
            point[0].X = shape.X + (shape.Width / 2);
            point[0].Y = (shape.Y);
            point[1].X = shape.X;
            point[1].Y = shape.Y + (shape.Height);
            point[2].X = (shape.X + shape.Width);
            point[2].Y = (shape.Y + shape.Height);
            double abc = TriangleArea(point[0], point[1], point[2]);
            double xbc = TriangleArea(location, point[1], point[2]);
            double axc = TriangleArea(point[0], location, point[2]);
            double abx = TriangleArea(point[0], point[1], location);
            return (abc == xbc + axc + abx);
        }
        private static double TriangleArea(PointF x, PointF y, PointF z)
        {
            return Math.Abs((x.X * (y.Y - z.Y) + y.X * (z.Y - x.Y) + z.X * (x.Y - y.Y)) / 2.0);
        }
        private static void GenerateShapes()
        {
            Task.Run(() =>
            {
                while (GameRunning)
                {
                    CreateRandomShape();
                    Thread.Sleep(GameRefreshTime);
                }
            });
        }
    }
}
