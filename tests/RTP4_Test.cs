/// Test PB Script for Radio Transmission Protocol 4
/// Command Format: [command]:[arg 0]:[arg 1]:...
///
/// Commands:
///           init:[local name]
///           connect:[target Name]:[channel]// initiates a new connection
///           send:[text data]// sends data over an open connection if there is such
///           disonnect// disconnects an open connection if there is such

const string def_block_antenna = "Antenna";// name of the antenna to use
const string def_block_lcdPanel_messages = "LCD_Messages";// panel to output received data to
const string def_block_lcdPanel_debug = "LCD_Debug";// panel to output debug data to

IMyRadioAntenna antenna;
static IMyTextPanel lcdPanel_messages;
static IMyTextPanel lcdPanel_debug;

bool init = false;

Program() {
    List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.SearchBlocksOfName(def_block_antenna, blocks);
    antenna = (IMyRadioAntenna)blocks[0];
    GridTerminalSystem.SearchBlocksOfName(def_block_lcdPanel_messages, blocks);
    lcdPanel_messages = (IMyTextPanel)blocks[0];
    GridTerminalSystem.SearchBlocksOfName(def_block_lcdPanel_debug, blocks);
    lcdPanel_debug = (IMyTextPanel)blocks[0];

    lcdPanel_messages.WritePublicText("");
    lcdPanel_debug.WritePublicText("");

    antenna.AttachedProgrammableBlock = Me.EntityId;

    Runtime.UpdateFrequency = UpdateFrequency.Update10;
}

RTP4.IConnection connection;

void Main(string arg, UpdateType updateSource) {
    if (updateSource == UpdateType.Antenna) {// process received transmission
        RTP4.OnAntennaMessage(arg);
    } else if (updateSource == UpdateType.Terminal) {// process user command
        string[] args = arg.Split(':');
        string cmd = args[0];
        if (cmd == "connect") {
            if (connection == null) {
                if (arg.Length > 3) {
                    string targetId = args[1];
                    byte channel;
                    if (byte.TryParse(args[2], out channel)) {
                        Print("connection requested");
                        SetConnection(RTP4.OpenConnection(targetId, channel));
                    }
                }
            }
        } else if (cmd == "disconnect") {
            if (connection != null) {
                connection.Close();
            }
        } else if (cmd == "send") {
            if (connection != null) {
                if (arg.Length > 1) {
                    connection.SendData(args[1]);
                }
            }
        } else if (cmd == "init") {
            if (args.Length > 1) {
                RTP4.Initialize(Runtime, new IMyRadioAntenna[] { antenna }, MyTransmitTarget.Owned, args[1], 5, 400, delegate (string hostId, byte channel) { return connection == null; }, SetConnection);
                Print("initialized with id: " + RTP4.LocalName);
                init = true;
            }
        } else if (cmd == "shutdown") {
            if (init) {
                RTP4.Shutdown();
                init = false;
                Print("shutdown");
            }
        }
    }

    if (init) { RTP4.StaticUpdate(); }
}

void SetConnection(RTP4.IConnection connection) {
    this.connection = connection;
    connection.OnOpen = delegate () { Print("connection opened: target=" + connection.Target + ", channel=" + connection.Channel); };
    connection.OnData = delegate (string data) { Print("connection.OnData=[" + data + "]"); };
    connection.OnClose = delegate () { Print("disconnected: target=" + connection.Target + ", channel=" + connection.Channel); this.connection = null; };
}

static void Print(string msg) { lcdPanel_messages.WritePublicText(msg + "\n", true); }

static void PrintDebug(string msg) { lcdPanel_debug.WritePublicText(msg + "\n", true); }


  /////////////////////
  // [ RTP4 Source ] //
  /////////////////////

