/// <summary>
/// Radio Transmission Protocol Itr. 4
/// </summary>
public static class RTP4 {
    static IMyGridProgramRuntimeInfo m_runtime;
    static List<StaticConnection> m_connections;
    static IMyRadioAntenna[] m_antennas;
    static MyTransmitTarget m_transmissionMode;
    static int m_sendLimit;
    static int m_sendTimeoutMs;
    static Func<string, byte, bool> m_connectionAcceptor;
    static Action<IConnection> m_connectionListener;

    static string m_localID;
    static string m_localIDLength;

    static long CurrentTime;// used for packet timing, must be kept internally updated to ensure consistency with the in-game time

    /// <summary>
    /// Returns true if the protocol has be successfully initialized
    /// </summary>
    public static bool IsInitialized { get { return m_connections != null; } }

    /// <summary>
    /// Local id
    /// </summary>
    public static string LocalName { get { return m_localID; } }

    /// <summary>
    /// Attempts to initialize the protocol
    /// </summary>
    /// <param name="runtime">runtime object</param>
    /// <param name="antennas">antennas array</param>
    /// <param name="transmissionMode">transmission mode</param>
    /// <param name="localName">local id, must be between 1 and 99 characters long</param>
    /// <param name="sendLimit">send limit per packet, if a packet is attempted to be sent more than this value, the associated connection is terminated [default=5]</param>
    /// <param name="sendTimeoutMs">time to wait between attempting packet retransmission [default=400]</param>
    /// <param name="connectionAcceptor">connection acceptor delegate, decides whether to accept an incoming connection request, null causes any such connections to be rejected</param>
    /// <param name="connectionListener">connection listener delegate, invoked when a new connection has been accepted, can be null</param>
    public static void Initialize(IMyGridProgramRuntimeInfo runtime, IMyRadioAntenna[] antennas, MyTransmitTarget transmissionMode, string localName, int sendLimit = 5, int sendTimeoutMs = 400, Func<string, byte, bool> connectionAcceptor = null, Action<IConnection> connectionListener = null) {
        if (m_connections == null) {
            if (runtime == null) { throw new Exception("runtime can not be null"); }
            if (antennas == null || antennas.Length == 0) { throw new Exception("antennas can not be null or have length of 0"); }
            if (localName == null || localName.Length <= 0 || localName.Length > 99) { throw new Exception("localName length must be between 1 and 9 inclusive"); }

            m_runtime = runtime;
            m_antennas = (IMyRadioAntenna[])antennas.Clone();
            m_localID = localName;
            m_sendLimit = sendLimit <= 0 ? 1 : sendLimit;
            m_sendTimeoutMs = sendTimeoutMs < 0 ? 0 : sendTimeoutMs;
            m_connectionAcceptor = connectionAcceptor;
            m_connectionListener = connectionListener;

            m_connections = new List<StaticConnection>();
            m_connections.Add(new StaticConnection());
            m_transmissionMode = transmissionMode;
            m_localIDLength = m_localID.Length.ToString("00");
            CurrentTime = sendTimeoutMs * 3;// just an arbitrary value above 0

            for (int i = 0, l = m_antennas.Length; i < l; i++) {
                IMyRadioAntenna a = m_antennas[i];
                if (transmissionMode == MyTransmitTarget.Owned) {
                    a.IgnoreAlliedBroadcast = true;
                    a.IgnoreOtherBroadcast = true;
                } else if (transmissionMode == MyTransmitTarget.Ally) {
                    a.IgnoreAlliedBroadcast = false;
                    a.IgnoreOtherBroadcast = true;
                } else if (transmissionMode == MyTransmitTarget.Enemy || transmissionMode == MyTransmitTarget.Everyone) {
                    a.IgnoreAlliedBroadcast = false;
                    a.IgnoreOtherBroadcast = false;
                }
            }
        }
    }

    /// <summary>
    /// Main update function, manages packet transmissions
    /// </summary>
    public static void StaticUpdate() {
        CurrentTime += m_runtime.TimeSinceLastRun.Milliseconds;// update current in-game time
        List<Packet> packets = null;
        int messageLength = 0;
        m_connections[0].GetNextPackets(ref packets, ref messageLength);
        for (int i = m_connections.Count - 1; i > 0; i--) { m_connections[i].GetNextPackets(ref packets, ref messageLength); }// get packets
        if (packets != null) {
            StringBuilder pb = new StringBuilder(6 + messageLength);
            pb.Append("[RTP4]");
            for (int i = 0, l = packets.Count; i < l; i++) { pb.Append(packets[i].packetData); }// build packet transmission data
            string msg = pb.ToString();
            for (int j = 0, m = m_antennas.Length; j < m; j++) {
                if (m_antennas[j].TransmitMessage(msg, m_transmissionMode)) {// attempt to transmit
                    for (int i = 0, l = packets.Count; i < l; i++) { packets[i].OnTrySend(); }// if transmission succeeds, update involved packets' internal states
                    return;
                }
            }
            // abnormal state, none of the antennas could transmit
        }
    }

    /// <summary>
    /// Method to be called when the programming block is invoked by an antenna
    /// </summary>
    /// <param name="msg">recieved message (argument)</param>
    public static void OnAntennaMessage(string msg) {
        if (msg != null) {// null check
            int length = msg.Length;
            if (length > 6 && msg.StartsWith("[RTP4]")) {// length & header check
                int index = 6, packetLength;
                while (index < length) {//iterate while index is smaller than length
                    if (index + 3 < length && int.TryParse(msg.Substring(index, 3), out packetLength)) {// with checks, try to parse the packet length
                        if (packetLength <= length - index - 3) {// check if the packet length is smaller or equals to the remaining length, if not - currupted transmission
                            if (Packet.ProcessPacketData(msg, index + 3, packetLength + index + 3)) {// try to parse & process the packet
                                index += 3 + packetLength;// update index & continue to the next packet if any remain
                                continue;
                            }
                        }
                    }
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Attempts to open a new connection witht the given id and channel. If there is already an existing connection witht the same signature, null is returned.
    /// </summary>
    /// <param name="targetID">endpoint target id, must be between 1 and 99 characters</param>
    /// <param name="channel">connection channel</param>
    /// <returns>New connection object</returns>
    public static IConnection OpenConnection(string targetID, byte channel) {
        if (targetID == null || targetID.Length <= 0 || targetID.Length > 99) { return null; }// validate target id
        if (GetExistingConnection(targetID, channel) == null) {// check if there is existing connection with the provided signature
            ConnectionImpl c = new ConnectionImpl();// init new connection
            c.m_target = targetID;
            c.m_channel = channel;
            m_connections.Add(c);
            c.m_dataPackets.AddLast(Packet.NewSysPacket(targetID, channel, Packet.msg_sys_RequestOpen, 1, c.OnFailedDataPacket));// send out connection request
            return c;
        }
        return null;
    }

    /// <summary>
    /// Gets an existing connection with the specified signature.
    /// </summary>
    /// <param name="targetID">endpoint target id</param>
    /// <param name="channel">connection channel</param>
    /// <returns>Existing connection object</returns>
    public static IConnection GetExistingConnection(string targetID, byte channel) {
        for (int i = 1, l = m_connections.Count; i < l; i++) {// iterate over connections, skipping static (first)
            ConnectionImpl c = m_connections[i] as ConnectionImpl;
            if (c.m_channel == channel && c.m_target == targetID) { return c; }
        }
        return null;
    }

    /// <summary>
    /// Shuts down this protocol if it was initialized
    /// </summary>
    public static void Shutdown() {
        if (m_connections != null) {// guard initialization state
            for (int i = 0, l = m_connections.Count; i < l; i++) { m_connections[i].Close(); }// close all connections
            CurrentTime = long.MaxValue;// set max time to enable any remaining connection_delete packets to be sent
            StaticUpdate();// causes packets to be transmitted, best efforts scenario
            m_runtime = null;// null out the rest members, enabling faster gc
            m_connections.Clear();
            m_connections = null;
            m_antennas = null;
            m_connectionAcceptor = null;
            m_connectionListener = null;
        }
    }

    /// <summary>
    /// RTP4 Connection interface
    /// </summary>
    public interface IConnection {
        /// <summary>
        /// Endpoint target id
        /// </summary>
        string Target { get; }

        /// <summary>
        /// Endpoint channel id
        /// </summary>
        byte Channel { get; }

        /// <summary>
        /// Delegate, invoked when this connection becomes open
        /// </summary>
        Action OnOpen { get; set; }

        /// <summary>
        /// Delegate, invoked when this connection recieves data
        /// </summary>
        Action<string> OnData { get; set; }

        /// <summary>
        /// Delegate, invoked when this connection is closed for any reasons
        /// </summary>
        Action OnClose { get; set; }

        /// <summary>
        /// Returns open status
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// Gets the number of data packets queued for sending on this connection
        /// </summary>
        int DataQueueLength { get; }

        /// <summary>
        /// Attempts to transmit data (length: [0, 850]) to the target endpoint, this connection must be opened
        /// </summary>
        /// <param name="data">data to transmit</param>
        /// <param name="reliable">whether to ensure delivery</param>
        void SendData(string data, bool reliable = true);

        /// <summary>
        /// Closes this connection, note that OnClose delegate will be invoked during the invocation of this method
        /// </summary>
        void Close();

        /// <summary>
        /// Retrieves any unsent or unconfirmed data that is still queued up in this connection in FIFO order
        /// </summary>
        /// <param name="dataList">list into which to add data</param>
        [Obsolete("Untested Code")]
        void GetQueuedData(List<string> dataList);
    }

    /// <summary>
    /// Static connection class, static packet handling
    /// </summary>
    class StaticConnection {
        public LinkedList<Packet> m_dataPackets = new LinkedList<Packet>();

        /// <summary>
        /// Gets any and all packets that can be sent from this connection
        /// </summary>
        /// <param name="packets">packet list to put packets into</param>
        /// <param name="messageLength">total accumulated packet length, must be under 90000</param>
        public virtual void GetNextPackets(ref List<Packet> packets, ref int messageLength) {
            if (m_dataPackets.Count > 0) {// only process if there are any packets
                LinkedListNode<Packet> node = m_dataPackets.First;
                do {// iterate the linked nodes
                    Packet p = node.Value;
                    if (node == node.Next) { node = null; } else { node = node.Next; }// switch to the next node before processing packet as the node may get removed from list during Packet.CanTrySend()
                    if (p.CanTrySend() && messageLength + p.packetData.Length < 90000) {// ensure parameters
                        if (packets == null) { packets = new List<Packet>(); }// init list if its null
                        packets.Add(p);
                        messageLength += p.packetData.Length;
                    } else if (node == null) { node = m_dataPackets.First; }
                } while (node != null && node != m_dataPackets.First);
            }
        }

        public virtual void Close() { m_dataPackets.Clear(); }

        public virtual void RemoveDataPacket(Packet p) { m_dataPackets.Remove(p); }// failed static packets are simply removed 
    }

    /// <summary>
    /// Instanced connection class, manages connection states
    /// </summary>
    class ConnectionImpl : StaticConnection, IConnection {
        public const byte state_pending = 23;// initial state, pending for approval from the endpoint
        public const byte state_open = 25;// open state callback recieved, the connection is opened
        public const byte state_closed = 27;// connection is flagged as closed, no further operations are possible

        public List<Packet> m_callbackPackets = new List<Packet>(4);// callback packets, short lived and single send operation
        public string m_target;// endpoint target id
        public byte m_channel;// endpoint channel

        public string Target { get { return m_target; } }
        public byte Channel { get { return m_channel; } }

        public Action OnOpen { get; set; }
        public Action<string> OnData { get; set; }
        public Action OnClose { get; set; }

        public byte state = state_pending;

        public bool IsOpen { get { return state == state_open; } }

        public int DataQueueLength {
            get {
                LinkedListNode<Packet> node = m_dataPackets.First;
                int l = 0, uid;
                while (node != null && node != m_dataPackets.First) {
                    uid = node.Value.packetUID;
                    if (uid >= 100 || uid == 50) { l++; }
                    node = node.Next;
                }
                return l;
            }
        }

        public int NextPacketUID { get { return ++lastSentPacketUID >= 1000 ? (lastSentPacketUID = 100) : lastSentPacketUID; } }// tracks packet ids
        int lastSentPacketUID = 99;// will get incremented to 100 and returned in the 1st operation

        public int nextExpectedPacketUID = 100;// next packet id to expect, used for tracking user packets and delete_connection system messages

        public override void Close() {
            if (state != state_closed) {// guard state to prevent null pointer exceptions
                                        // internal close operations
                InternalClose();

                // notify endpoint of the connection termination
                Packet p = Packet.NewSysPacket(m_target, m_channel, Packet.msg_sys_ShutdownConnection, NextPacketUID, m_connections[0].RemoveDataPacket);
                p.m_sendsLeft = 1;
                m_connections[0].m_dataPackets.AddLast(p);
            }
        }

        public void InternalClose() {
            m_connections.Remove(this);// remove from the global connections list
            m_callbackPackets.Clear();
            m_callbackPackets = null;

            state = state_closed;
            if (OnClose != null) {
                OnClose.Invoke();
                OnClose = null;
            }
            OnOpen = null;
            OnData = null;
        }

        public void SendData(string data, bool reliable) {
            if (state == state_open) {// guard state
                if (data == null) { data = ""; } else if (data.Length > 850) { return; }// ensure data validity
                if (reliable) {
                    m_dataPackets.AddLast(Packet.NewDataPacket(this, data, NextPacketUID, OnFailedDataPacket));
                } else {
                    m_dataPackets.AddLast(Packet.NewDataPacket(this, data, 50, RemoveDataPacket));
                }
            }
        }

        /// <summary>
        /// Gets unsent data; unrevised, untested & possibly broken code
        /// </summary>
        /// <param name="dataList">list to add data to</param>
        public void GetQueuedData(List<string> dataList) {
            dataList.EnsureCapacity(dataList.Count + m_dataPackets.Count);
            LinkedListNode<Packet> node = m_dataPackets.First;
            do {
                string data = node.Value.packetData;
                int a = int.Parse(data.Substring(3, 2));
                a = 3 + 2 + a + 2 + m_localID.Length + 3 + 3 + 1;
                dataList.Add(data.Substring(a, data.Length - a));
                node = node.Next;
            } while (node != m_dataPackets.First);
        }

        public override void GetNextPackets(ref List<Packet> packets, ref int messageLength) {
            if (m_dataPackets.Count > 0) {// process data packets
                Packet p = m_dataPackets.First.Value;// get the first packet, sequential user data dispatching only
                if (p.CanTrySend()) {// check if the packet can be sent
                    if (messageLength + p.packetData.Length < 90000) {
                        if (packets == null) { packets = new List<Packet>(); }//init packets as needed
                        packets.Add(p);
                        messageLength += p.packetData.Length;
                    }
                } else if (state == state_closed) { return; }// guard state: p.CanTrySend may close the connection due to running out of send tries
            }
            if (m_callbackPackets.Count > 0) {
                if (packets == null) { packets = new List<Packet>(); }
                int i = 0, l = m_callbackPackets.Count, p;
                for (; i < l; i++) {// add as many callback packets as possible
                    p = m_callbackPackets[i].packetData.Length;
                    if (messageLength + p < 90000) {
                        messageLength += p;
                    } else {
                        break;
                    }
                }
                if (i == l) {// if all packets were added, use the bulk add method to add the whole list
                    packets.AddList(m_callbackPackets);
                } else {
                    l = 0;
                    packets.EnsureCapacity(packets.Count + i);
                    for (; l < i; l++) { packets.Add(m_callbackPackets[l]); }// add selected packets 1 by 1
                }
            }
        }

        public void OnFailedDataPacket(Packet p) { Close(); }

        public void OnCallbackPacketSent(Packet p) { m_callbackPackets.Remove(p); }
    }

    /// <summary>
    /// Manages packet structure; limitations: target name length between 1 - 9 inclusive, msg length between 0 - 900
    /// </summary>
    class Packet {
        public const char msg_sys_RequestOpen = 'd';// signals connection request
        public const char msg_sys_ConfirmOpen = 'e';// signals connection request accept
        public const char msg_sys_OpenHandshake = 'f';// signals connection request accept confirmation
        public const char msg_sys_ShutdownConnection = 'c';// signals connection shutdown
        public const char msg_data = 'b';// indicates message data

        // [packet length]000 [target length]0 [target]a- [sender length]0 [sender]a- [channel]000 [packet uuid]000 [msg data]a
        public static Packet NewSysPacket(string targetId, byte channel, char msg, int packetUID, Action<Packet> onTransmissionFailure) {
            Packet p = new Packet();
            p.packetUID = packetUID;
            p.m_sendsLeft = m_sendLimit;
            string data = targetId.Length.ToString("00") + targetId + m_localIDLength + m_localID + channel.ToString("000") + packetUID.ToString("000") + msg;
            p.packetData = string.Concat(data.Length.ToString("000"), data);
            p.OnTransmissionFailure = onTransmissionFailure;
            return p;
        }

        // [packet length]000 [target length]0 [target]a- [sender length]0 [sender]a- [channel]000 [packet uuid]000 msg_data [msg]a-
        public static Packet NewDataPacket(ConnectionImpl target, string msg, int packetUID, Action<Packet> onTransmissionFailure) {
            Packet p = new Packet();
            p.packetUID = packetUID;
            p.m_sendsLeft = m_sendLimit;
            string data = target.m_target.Length.ToString("00") + target.m_target + m_localIDLength + m_localID + target.m_channel.ToString("000") + packetUID.ToString("000") + msg_data + msg;
            p.packetData = string.Concat(data.Length.ToString("000"), data);
            p.OnTransmissionFailure = onTransmissionFailure;
            return p;
        }

        // [packet length]000 [target length]0 [target]a- [sender length]0 [sender]a- [channel]000 000 [callback packet uid]000
        public static Packet NewCallbackPacket(ConnectionImpl target, int callbackPacketUID) {
            Packet p = new Packet();
            p.packetUID = 0;
            string data = target.m_target.Length.ToString("00") + target.m_target + m_localIDLength + m_localID + target.m_channel.ToString("000") + "000" + callbackPacketUID.ToString("000");
            p.packetData = string.Concat(data.Length.ToString("000"), data);
            p.OnTransmissionFailure = target.OnCallbackPacketSent;
            return p;
        }

        /// <summary>
        /// Processes the packet
        /// </summary>
        /// <param name="msg">bulk message</param>
        /// <param name="dataStart">packet start (without packet length)</param>
        /// <param name="length">packet length relative to the msg</param>
        /// <returns>True if packet was parsable, false if corrupted</returns>
        public static bool ProcessPacketData(string msg, int dataStart, int length) {
            int tmpInt;
            string tmpString;

            if (dataStart + 2 < length && int.TryParse(msg.Substring(dataStart, 2).ToString(), out tmpInt)) {// target length
                dataStart += 2;
                if (dataStart + tmpInt < length) {
                    tmpString = msg.Substring(dataStart, tmpInt);
                    if (tmpString == m_localID) {// target
                        dataStart += tmpInt;
                        if (dataStart + 2 < length && int.TryParse(msg.Substring(dataStart, 2).ToString(), out tmpInt)) {// sender length
                            dataStart += 2;
                            if (dataStart + tmpInt < length) {
                                tmpString = msg.Substring(dataStart, tmpInt);// sender
                                dataStart += tmpInt;
                                byte channel;
                                if (dataStart + 3 < length && byte.TryParse(msg.Substring(dataStart, 3), out channel)) {// channel
                                    dataStart += 3;
                                    if (dataStart + 3 < length && int.TryParse(msg.Substring(dataStart, 3), out tmpInt)) {// packet uid
                                        dataStart += 3;
                                        if (tmpInt == 0) {
                                            ConnectionImpl c = GetExistingConnection(tmpString, channel) as ConnectionImpl;
                                            if (c != null) {
                                                if (dataStart + 3 <= length && int.TryParse(msg.Substring(dataStart, 3), out tmpInt)) {// callback uid
                                                    LinkedListNode<Packet> node = c.m_dataPackets.First;
                                                    if (node.Value.packetUID == tmpInt) {
                                                        c.m_dataPackets.RemoveFirst();
                                                    } else {
                                                        while ((node = node.Next) != c.m_dataPackets.First) {
                                                            if (node.Value.packetUID == tmpInt) {
                                                                c.m_dataPackets.Remove(node);
                                                                break;
                                                            }
                                                        }
                                                    }
                                                } else { return false; }
                                            }
                                        } else if (dataStart < length) {
                                            char packetType = msg[dataStart++];
                                            ConnectionImpl c = GetExistingConnection(tmpString, channel) as ConnectionImpl;
                                            if (c == null) {
                                                if (packetType == msg_sys_RequestOpen) {
                                                    if (m_connectionAcceptor != null && m_connectionAcceptor.Invoke(tmpString, channel)) {
                                                        c = new ConnectionImpl();
                                                        c.m_target = tmpString;
                                                        c.m_channel = channel;
                                                        c.m_dataPackets.AddLast(NewSysPacket(tmpString, channel, msg_sys_ConfirmOpen, 1, c.OnFailedDataPacket));
                                                        m_connections.Add(c);
                                                        if (m_connectionListener != null) { m_connectionListener.Invoke(c); }
                                                    } else {
                                                        Packet p = NewSysPacket(tmpString, channel, msg_sys_ShutdownConnection, 1, m_connections[0].RemoveDataPacket);
                                                        p.m_sendsLeft = 1;
                                                        m_connections[0].m_dataPackets.AddLast(p);
                                                    }
                                                }
                                            } else {
                                                if (packetType == msg_data) {
                                                    if (c.state == ConnectionImpl.state_open) {
                                                        if (tmpInt == 50 || c.nextExpectedPacketUID == tmpInt) {
                                                            if (tmpInt != 50) { if (++c.nextExpectedPacketUID > 999) { c.nextExpectedPacketUID = 100; } }
                                                            int l = length - dataStart;
                                                            if (l < 0) { return false; }
                                                            if (l == 0) {
                                                                if (c.OnData != null) { c.OnData.Invoke(""); }
                                                            } else if (dataStart + l <= length) {
                                                                if (c.OnData != null) { c.OnData.Invoke(msg.Substring(dataStart, l)); }
                                                            } else { return false; }
                                                        }
                                                        if (tmpInt >= 100) { c.m_callbackPackets.Add(NewCallbackPacket(c, tmpInt)); }
                                                    }
                                                } else if (packetType == msg_sys_RequestOpen) {
                                                    if (c.state == ConnectionImpl.state_pending) {
                                                        c.m_dataPackets.Clear();
                                                        c.m_dataPackets.AddLast(NewSysPacket(tmpString, channel, msg_sys_ConfirmOpen, 1, c.OnFailedDataPacket));
                                                    }
                                                } else if (packetType == msg_sys_ConfirmOpen) {
                                                    Packet p = NewSysPacket(tmpString, channel, msg_sys_OpenHandshake, 1, c.RemoveDataPacket);
                                                    p.m_sendsLeft = 1;
                                                    if (c.state == ConnectionImpl.state_pending) {
                                                        c.m_dataPackets.Clear();
                                                        c.m_dataPackets.AddLast(p);
                                                        c.state = ConnectionImpl.state_open;
                                                        if (c.OnOpen != null) { c.OnOpen.Invoke(); }
                                                    } else {
                                                        c.m_dataPackets.AddFirst(p);
                                                    }
                                                } else if (packetType == msg_sys_OpenHandshake) {
                                                    if (c.state == ConnectionImpl.state_pending) {
                                                        c.m_dataPackets.Clear();
                                                        c.state = ConnectionImpl.state_open;
                                                        if (c.OnOpen != null) { c.OnOpen.Invoke(); }
                                                    }
                                                } else if (packetType == msg_sys_ShutdownConnection && tmpInt >= 100) {
                                                    if ((c.state == ConnectionImpl.state_pending || c.nextExpectedPacketUID == tmpInt) && c.state != ConnectionImpl.state_closed) { c.InternalClose(); }
                                                }
                                            }
                                        } else { return false; }
                                        return true;// parse success
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;// parse failure, corrupted transmission
        }

        public string packetData;//compiled
        public int packetUID;
        public Action<Packet> OnTransmissionFailure;

        public int m_sendsLeft;// number of send tries left
        public long m_lastSendTimestamp;// last send timestamp

        /// <summary>
        /// Returns true if the packet can be sent
        /// </summary>
        public bool CanTrySend() {
            if (packetUID == 50) { return true; }// unreliable packets can always be sent, but only once
            if (m_sendsLeft <= 0) {
                OnTransmissionFailure.Invoke(this);
                return false;
            }
            return CurrentTime - m_lastSendTimestamp >= m_sendTimeoutMs;
        }

        /// <summary>
        /// Signals that the packet has been attempted to be sent
        /// </summary>
        public void OnTrySend() {
            if (packetUID == 0 || packetUID == 50) {// callbacks and unreliable packets are always deleted on send
                OnTransmissionFailure.Invoke(this);
            } else {
                m_lastSendTimestamp = CurrentTime;
                m_sendsLeft--;
            }
        }
    }
}
