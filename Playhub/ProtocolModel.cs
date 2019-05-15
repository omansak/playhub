using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Net.Sockets;

namespace Playhub
{
    public class ProtocolModel
    {
        public class Base
        {
            public MessageType Type { get; set; }
            public DateTime DateTime { get; set; }
            public object Data { get; set; }

        }

        public class PlayerList
        {
            public ObservableCollection<Player> Players { get; set; }
        }
        public class Shapes
        {
            public Color Color { get; set; }
            public int Type { get; set; }
            public RectangleF Point { get; set; }
        }
        public class ClickCoor
        {
            public Point Coor;
        }
        public class LobbyMessage
        {
            public string Player { get; set; }
            public string Message { get; set; }
        }
        public class Player
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public int Point { get; set; }
        }
        public class ConnectionClient
        {
            public string Id { get; set; }
            public TcpClient Client { get; set; }
        }
        public class GameSetting
        {
            public string GameName { get; set; }
            public string Timer { get; set; }
            public string Win { get; set; }
            public string Red { get; set; }
            public string Blue { get; set; }
            public string Yellow { get; set; }
            public Point PanelSize { get; set; }
        }
        public enum MessageType
        {
            //Fixed
            LobbyMessage,
            //Responses
            ResponsePlayers,
            ResponseShapes,
            //Requests
            RequestJoin,
            RequestSendCoor,
            RequestStart,
            RequestGameSettings
        }
    }
}
