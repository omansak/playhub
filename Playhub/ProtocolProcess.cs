using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Playhub
{
    public static class ProtocolProcess
    {
        //Datas
        public static ObservableCollection<ProtocolModel.Player> Players { get; set; }
        public static ObservableCollection<ProtocolModel.Shapes> PanelShapes { get; set; }
        public static ObservableCollection<string> Messages { get; set; }
        public static event EventHandler<GameSettingsReceivedEvent> OnGameSettingsReceivedEvent;
        static ProtocolProcess()
        {
            Players = new ObservableCollection<ProtocolModel.Player>();
            Messages = new ObservableCollection<string>();
            PanelShapes = new ObservableCollection<ProtocolModel.Shapes>();
            GameServer.OnDataReceived += GameServerOnDataReceived;
        }

        private static void GameServerOnDataReceived(object sender, DataReceivedEvent e)
        {
            var token = e.Data.Split(new[] { "\\;omansak;\\" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var messageToken in token)
            {
                ProtocolModel.Base baseMessage = JsonConvert.DeserializeObject<ProtocolModel.Base>(messageToken);
                switch (baseMessage.Type)
                {
                    // Get PlayerList
                    case ProtocolModel.MessageType.ResponsePlayers:
                        {
                            Players.Clear();
                            foreach (var item in JsonConvert.DeserializeObject<ProtocolModel.PlayerList>(baseMessage.Data.ToString()).Players)
                            {
                                Players.Add(item);
                            }
                            break;
                        }
                    case ProtocolModel.MessageType.LobbyMessage:
                        {
                            ProtocolModel.LobbyMessage message = JsonConvert.DeserializeObject<ProtocolModel.LobbyMessage>(baseMessage.Data.ToString());
                            Messages.Add($"({baseMessage.DateTime}) {message.Player} : {message.Message}");
                            break;
                        }
                    case ProtocolModel.MessageType.RequestGameSettings:
                        {
                            OnGameSettingsReceivedEvent?.Invoke(null, new GameSettingsReceivedEvent(baseMessage.Data.ToString()));
                            break;
                        }
                    case ProtocolModel.MessageType.ResponseShapes:
                        {
                            PanelShapes.Clear();
                            foreach (var item in JsonConvert.DeserializeObject<List<ProtocolModel.Shapes>>(baseMessage.Data.ToString()))
                            {
                                PanelShapes.Add(item);
                            }
                            break;
                        }
                }
            }

        }
        public static void CreateGame(string gameAdrress, int gamePort, string gameName)
        {
            GameServer.GameAdrress = gameAdrress;
            GameServer.GamePort = gamePort;
            GameServer.GameName = gameName;
            GameServer.CreateSocket();
        }
        public static void SetSettings(int refreshTime, int red, int blue, int yellow, int winPoint, int panelX, int panelY)
        {
            GameServer.GameRefreshTime = refreshTime;
            GameServer.GameShapeRed = red;
            GameServer.GameShapeBlue = blue;
            GameServer.GameShapeYellow = yellow;
            GameServer.GamePanelSizeX = panelX;
            GameServer.GamePanelSizeY = panelY;
            GameServer.GameWinPoint = winPoint;
        }
        public static void CreatePlayer(string gameAdrress, int gamePort)
        {
            GameServer.GameAdrress = gameAdrress;
            GameServer.GamePort = gamePort;
            GameServer.CreateClient();

        }
        public static async void StartServer()
        {
            await GameServer.StartServerSocketAsync();
        }
        public static async void StartClient()
        {
            await GameServer.StartClientSocketAsync();
        }
        public static void JoinGame(string playerName)
        {
            GameServer.RequestJoin(playerName);
        }
        public static void SendMessage(string message, string playerName)
        {
            GameServer.RequestSendMessage(message, playerName);
        }
        public static void RequestGameSettings()
        {
            GameServer.RequestGameSettings();
        }
        public static void SendClickCoor(int x, int y)
        {
            GameServer.RequestSendClickCoor(x, y);
        }

        public static void StartGame()
        {
            GameServer.RequestStartGame();
        }
    }
}
