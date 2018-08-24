# Radio Transmission Protocol Itr. 4
Data transmission protocol api for Space Engineers programmable blocks.

Steam Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=1484432466

<details><summary><b>Contents</b></summary>
<a>

- [Current Features](#current-features)
- [Removed Features](#removed-features)
- [API](#api)
  - [Code Setup](#api1-code-setup)
  - [Initialization](#api2-initialization)
  - [Maintaining Protocol](#api3-maintaining-protocol)
    - [Packet Listening](#32-packet-listening-antenna-messages)
    - [Packet Dispatching](#32-packet-dispatching-static-updates)
  - [Opening Connections](#api4-opening-connections)
  - [Accepting Connections](#api5-accepting-connections)
  - [IConnection Interface](#api6-rtp4iconnection-interface)
- [Protocol Theory](#protocol-theory)
  - [Protocol Modules](#1-protocol-modules)
    - [Static Connection](#11-static-connection-staticconnection)
    - [Connection Instance](#12-connection-instance-connectionimpl)
      - [Packet Tracking](#packet-tracking)
      - [Packet Types](#packet-types)
      - [Packet Structure](#packet-structure)
  - [Protocol Flow](#2-protocol-flow)
    - [Packet Flow](#21-packet-flow)
      - [Untracked Packets](#untracked-packets)
      - [Tracked Packets](#tracked-packets)
    - [Connection Initialization](#22-connection-initialization)
    - [Tracked Packet Flow](#23-tracked-packet-flow)
    - [Connection Shutdown](#24-connection-shutdown)
</a>
</details>

## Current Features:
- Reliable & Unreliable packet delivery
- Packet ordering
- Packet resend timeout
- Object-oriented approach
- High level api

## Removed Features
- Encryption
  - Adds performance overheads for little gain - interference is usually elliminated by using owner-only trusted communication

## Required Blocks
- Antenna(s) _set to trigger the associated Programmable Block_
- Programmable Block

## API
### API.1 Code Setup
Setup programmable block environment in the preferred code editor as usual, and copy [RTP4 source](/src/RTP4.cs) as an internal static class. When copying code into the programmable block [minifed version](/programmable_block/RTP4.cs) can be used instead of the source to free up code space.

### API.2 Initialization
Before using the transmission protocol it is necessary to initialize it:
```javascript
void RTP4.Initialize(IMyGridProgramRuntimeInfo runtime, IMyRadioAntenna[] antennas, MyTransmitTarget transmissionMode, string localName, int sendLimit = 5, int sendTimeoutMs = 400, Func<string, byte, bool> connectionAcceptor = null, Action<IConnection> connectionListener = null)
```
Parameters:
1. `IMyGridProgramRuntimeInfo runtime`
   * Runtime object for this Programmable Block, used for packet timing
2. `IMyRadioAntenna[] antennas`
   * Antennas array, must not contain `nulls` and should have at least one working antenna
3. `MyTransmitTarget transmissionMode`
   * Transmission mode for this protocol, valid flags: Owned, Ally, Enemy (same as Everyone)
4. `string localName`
   * Name identifier for this connection platform, length between 1 and 99 characters
5. `int sendLimit [ = 5 ]`
   * Maximal number of times packets will be attempted to be (re-)transmitted; min=1, default=5
   * After running out of tries the connection will be shutdown
6. `int sendTimeoutMs [ = 400 ]`
   * Amount of in-game milliseconds to wait before trying to (re-)transmit a packet; min=0, default=400
7. `Func<string, byte, bool> connectionAcceptor [ = null ]`
   * Delegate for handling incoming connection requests, null means rejecting everything
     * parameter 0: [string] -> target name
     * parameter 1: [byte] -> connection channel
     * return: [bool] -> `true` to accept the connection, `false` to reject
8. `Action<IConnection> connectionListener [ = null ]`
   * Delegate for getting notifications about newly accepted connections, can be null
     * parameter 0: [RTP4.IConnection] -> connection object that has just been accepted
### API.3 Maintaining Protocol
After the protocol has been initialized it needs to receive constant updates and antenna messages.
#### 3.2 Packet Listening (Antenna Messages)
```javascript
void RTP4.OnAntennaMessage(string msg)
```
Parameters:
1. `string msg`
   * Raw argument from the antenna message

#### 3.2 Packet Dispatching (Static Updates)
```javascript
void RTP4.StaticUpdate()
```
Attempts to dispatch any queued up packets, and shutdown timed out connections.
### API.4 Opening Connections
Connections can be opened by using the following method:
```javascript
RTP4.IConnection RTP4.OpenConnection(string targetID, byte channel)
```
Parameters:
1. `string targetID`
   * Target name identifier - `localName` used in the initialization method, length between 1 and 99 characters
2. `byte channel`
   * Connection channel to use, same concept as ports - connection identifier number between two platforms

Returns:
- `RTP4.IConnection`
  * Connection object
### API.5 Accepting Connections
If a platform invokes `OpenConnection` with the target name of the recieving platform, the `connectionAcceptor` delegate will be invoked when the message is processed, and the returning value will decide whether the connection is opened or rejected. (See [Initialization](#api2-initialization))
### API.6 RTP4.IConnection Interface
For any connection related operations the protocol api uses `RTP4.IConnection` objects.

Properties:
- `string Target { get; }`
   - Target platform identifier
- `byte Channel { get; }`
   - Connection channel
- `Action OnOpen { get; set; }`
   - Invoked at most once, when the connection has been successfuly opened on both endpoints
- `Action OnClose { get; set; }`
   - Invoked at most once, when the connection has been closed for any reason, including `Close()` invocation
- `Action<string> OnData { get; set; }`
   - Invoked every time the connection recieves data, after the target platform has invoked `SendData(string)`
- `bool IsOpen { get; }`
   - Indicates whether the connection is opened
- `int DataQueueLength { get; }`
   - Gets the amount of queued messages

Methods:
- `void SendData(string data, bool reliable = true)`
   * Queues up data to be sent to the target platform, only works if the connection is opened
     * Parameters:
       * `string data` - data to send
       * `bool reliable` - whether to track & ensure packet delivery
- `void Close()`
   - Shuts down the connection immediately regardless of its state, has no effects if the connection has already been shutdown
- `void GetQueuedData(List<string> dataList)`
   - Gets the currently unsent data, can be called after the connection has been closed to retrieve any data that could not be transmitted
   - Currently untested code, may throw exceptions

## Protocol Theory
This section contains detailed explanation on how the protocol works internally. Knowing this is completely optional and is not required to be able to effectively use the exposed api controls.

### 1. Protocol Modules
The protocol is managed by three main modules which carry out tasks of message management (storing, dispatching) and data tracking (detecting that data is delivered or not).

#### 1.1 Static Connection (StaticConnection)
This object is responsible for dispatching (at the moment) only connection shutdown messages. After `IConnection.Close()` method is invoked the connection is marked as closed, all its internal states are cleared and the static connection enqueues the shutdown message for that connection's target.

SCO is always stored in the `RTP4.m_connection` static list at index 0.

Packet Sending: Dumps maximal amount of enqueued packets

#### 1.2 Connection Instance (ConnectionImpl)
This is the class that implements `IConnection` interface and exposes the related properties. As well as that it stores all related state information: packet tracking numbers (outgoing & incoming) and connection state (pending, open, closed).
The class extends `StaticConnection` to inherit the `m_dataPackets` list, and defines its own `m_callbackPackets` list.

Packet Sending: Sends at most one enqueued packet, and dumps maximal amount of callback packets.

#### 1.3 Packet Instance (Packet)
##### Packet Tracking
Most packets will have a uid number (Unique Identifier, which is not always unique).

This number is used during the packet parsing to ensure that the target connection has not received this packet before. In this case the uid acts as a packet tracking number.

Packets can also be sent without tracking, in which case they will be only dispatched once and then be removed from the queue without waiting (or expecting) any recieve confirmation callback from the target.

In other cases it is used a packet type identifier (most system messages have uid = 1).

Packet numbers are always stored as three digits, and once the number reaches 999, the next uid is looped back to 100.

##### Packet Types
There is only one packet class, yet there are several different types of packets which are treated differently by the connections and by packet management logic.

- System Packet
  - Usually carry system related information such as connection initialization procedure messages
  - Most of system messages have a uid value of 1 except for the shutdown message, which uses a tracked uid
- Data Packet
  - Used for transmitting tracked & untracked packets of custom data
- Callback Packet
  - Contains the uid of a packet which was delivered

##### Packet Structure
Each packet contains the sender & target ids, channel id and the packet data.

Each packet has the same header which contains sender, target and channel ids.

Packet Header: \[packet length]\[target length]\[target]\[sender length]\[sender]\[channel]\[uid]
- `packet length` - \[3 digits] total length of the packet, used as the error detection value during parsing
- `target length` - \[2 digits] length of the target name id
- `target` - \[determined by `target length`] target id, the reciever of this packet
- `sender length` - \[2 digits] length of the sender name id
- `sender` - \[determined by `sender length`] sender id, the sender of this packet
- `channel` - \[3 digits] channel of this connection between the two platforms
- `uid` - \[3 digits] identification number of this packet

After the packet header the packet is encoded differently based on the type.

System Packet Body: { Header }\[msg]
- `msg` - \[1 char] system message

Data Packet Body: { Header }\[msg]\[data]
- `msg` - \[1 char] system message, always `Packet.msg_data` for data packets
- `data` - \[determined by packet length] sent data, substring of the leading index and the ending index of this packet

Callback Packet Body: { Header }\[callback uid]
- `callback uid` - \[3 digits] recieved packet uid
- callback packets always have packet uid = 0

### 2. Protocol Flow
This section contains explanation on the process flow of the protocol. The following explanation is valid when the code is properly setup, that is the `RTP4.OnAntennaMessage` and `RTP4.StaticUpdate` methods are called at their appropriate times.

#### 2.1 Packet Flow
##### Untracked Packets
Untracked packets are enqueued in the connection data queue and then transmitted one by one. No callback is sent by the target upon the arrival. This is a best-case-scenario arrival.

These packets always have uid = 50.

##### Tracked Packets
Tracked packets are also enqueued in the connection data queue of a connection, and transmitted one by one.
However, these packets are retransmitted again and again with the specified re-send delay, and up to the specified amount of times, after which the connection is shutdown.

During this process the connection may recieve a callback packet, which will contain a uid of a recieved packet. If the uid specified by the callback matches the uid of the currently enqueued and re-transmitted packet, the data packet is removed - it has successfully reached its destination (in theory).

#### 2.2 Connection Initialization
In this scenario the connection initializer is A and target is B.
1. After the a new connection has been requested on platform A (`RTP4.OpenConnection`) it is added to the `RTP4.m_connections` list. At the same time it will enqueue a tracked `Packet.msg_sys_RequestOpen` message.
2. Platform B recieves the `Packet.msg_sys_RequestOpen` message:
   - \[If connection doesnt exist] Initialize a new connection object with the sender details (TargetID and Channel)
   - Enqueue a tracked `Packet.msg_sys_ConfirmOpen` message.
3. Platform A recieves `Packet.msg_sys_ConfirmOpen`:
   - If the connection is in pending state:
     - Clears all packets from `m_dataPackets`
     - Appends untracked `Packet.msg_sys_OpenHandshake` message to the `m_dataPackets` list
     - Sets connection state to open
     - Invokes `OnOpen` delegate if not null
   - If the connection is opened:
     - Prepends untracked `Packet.msg_sys_OpenHandshake` message to the beginning of `m_dataPackets` list
4. Platform B recieves `Packet.msg_sys_OpenHandshake` message, if the connection state is pending:
   - Clears all packets from `m_dataPackets`
   - Sets connection state to open
   - Invokes `OnOpen` delegate if not null

#### 2.3 Tracked Packet Flow
In this scenario the sender is A and target is B.
1. A enqueues a data packet with the next packet uid in the count.
2. B recieves the data packet:
   - If the packet uid macthes the next expected uid:
     - Increment next expected uid
     - Invoke `OnData` delegate if not null
   - In any case:
     - Enqueue callback message with the uid of the recieved packet
3. A recieves the callback with the target packet uid and, if that matches the uid of the originally send data packet, it is removed.

#### 2.4 Connection Shutdown
When a connection is shutdown for any reason:
1. Connection is cleaned up (nulled out) and marked as closed.
2. Removed from `RTP4.m_connections` list.
3. Static Connection enqueues an untracked (but with the valid uid) shutdown message with the signature of the shutdown connection.
