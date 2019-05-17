# Playhub
A sample Server/Client game writen with C#. This application uses a custom message protocol on TCPClient & TCPListener with asynchronous

## Features
- Create/Join Lobby
- Async TcpClient/TcpListener
- Windows Form Application
- ObservableCollection & EventHandler
- JsonConvert
- Generate Shapes
## Build Setup
### Prerequisites
* Newtonsoft.Json

### Building in Visual Studio
1. Download and install the latest version of [Visual Studio](https://visualstudio.microsoft.com/tr/).
2. In Project Folder. open "Playhub.sln"
3. Browse to the location of the project, and double-click on the project directory.
5. Click the green start button (or 'F5') to run the project!

### Dependencies
- Newtonsoft.Json
- System.*
- System.Net.Http

## Protocol Formats
*All request and responses convert to JSON before to send.  
`JsonConvert.SerializeObject(*request or response object)
`
### Client Requests
#### *Request join to lobby
```csharp
   new ProtocolModel.Base
                {
                    DateTime = DateTime.Now,
                    Type = ProtocolModel.MessageType.RequestJoin,
                    Data = new ProtocolModel.Player
                    {
                        Name = playerName,
                        Point = 0
                    }
                }
```
#### *Send message to lobby
```csharp
new ProtocolModel.Base
            {
                DateTime = DateTime.Now,
                Type = ProtocolModel.MessageType.LobbyMessage,
                Data = new ProtocolModel.LobbyMessage()
                {
                    Player = "*userName",
                    Message = "*message"
                }
            }
```
#### *Request game settings
```csharp
new ProtocolModel.Base
            {
                DateTime = DateTime.Now,
                Type = ProtocolModel.MessageType.RequestGameSettings,
                Data = ""
            }
```
#### *Start game (can only hosts)
```csharp
new ProtocolModel.Base
            {
                DateTime = DateTime.Now,
                Type = ProtocolModel.MessageType.RequestStart,
                Data = ""
            }
```
#### *Send to clicked coordinants
```csharp
new ProtocolModel.Base
            {
                DateTime = DateTime.Now,
                Type = ProtocolModel.MessageType.RequestSendCoor,
                Data = new ProtocolModel.ClickCoor
                {
                    Coor = new Point(*x, *y)
                }
            }
```
### Server Responses
#### *Broadcast Players in Lobby
```csharp
new ProtocolModel.Base
            {
                Type = ProtocolModel.MessageType.ResponsePlayers,
                DateTime = DateTime.Now,
                Data = new ProtocolModel.PlayerList
                {
                    Players = *ObservableCollection<ProtocolModel.Player>
                }
            }
```
#### *Broadcast Shapes
```csharp
new ProtocolModel.Base
                {
                    Type = ProtocolModel.MessageType.ResponseShapes,
                    DateTime = DateTime.Now,
                    Data = *ObservableCollection<Shapes>
                }
```
#### *Broadcast Message
```csharp
new ProtocolModel.Base
            {
                DateTime = DateTime.Now,
                Type = ProtocolModel.MessageType.LobbyMessage,
                Data = new ProtocolModel.LobbyMessage()
                {
                    Message = "message"
                }
            }
```
#### *Broadcast Settings
```csharp
new ProtocolModel.Base
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

                 }
```

## In-App Images
![Home](https://github.com/omansak/playhup/blob/master/Playhub/Images/Home.PNG)
![Game Panel](https://github.com/omansak/playhup/blob/master/Playhub/Images/Game%20Lobby.PNG)
