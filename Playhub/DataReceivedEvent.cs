using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Playhub
{
    public class DataReceivedEvent : EventArgs
    {
        public string Data { get; set; }
        public DataReceivedEvent(string data)
        {
            Data = data;
        }


    }
    public class GameSettingsReceivedEvent : EventArgs
    {
        public ProtocolModel.GameSetting GameSettings { get; set; }
        public GameSettingsReceivedEvent(string data)
        {
            GameSettings = JsonConvert.DeserializeObject<ProtocolModel.GameSetting>(data); ;
        }
    }
}
