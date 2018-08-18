# Radio Transmission Protocol Itr. 4
Data transmission protocol api for Space Engineers programmable blocks.

Steam Workshop: 

## Current Features:
- Reliable packet delivery
- Packet resend timeout
- Object-oriented approach
- High level api

## Removed Features
- Encryption
  - Adds perofrmance overheads for little gain - interference is usually elliminated by using owner-only trusted communication

## Required Blocks
- Antenna(s) _set to trigger the associated Programmable Block_
- Programmable Block

## API
### API.1 Code Setup
Setup programmable block environment if the preferred code editor as usual, and copy [RTP4 source](https://github.com/OmgItsRito/se-rtp4/blob/master/src/RTP4.cs) as an internal static class.

### API.2 Initialization
Before using the transmission protocol it is necessary to initialize it:
```javascript
void RTP4.Initialize(IMyGridProgramRuntimeInfo runtime, IMyRadioAntenna[] antennas, MyTransmitTarget transmissionMode, string localName, int sendLimit, int sendTimeoutMs, Func<string, byte, bool> connectionAcceptor, Action<IConnection> connectionListener)
```
Parameters:
1. `IMyGridProgramRuntimeInfo runtime`
   * Runtime object for this Programmable Block, used for packet timing
2. `IMyRadioAntenna[] antennas`
   * Antennas array, must not contain `nulls` ans should have at least one working antenna
3. `MyTransmitTarget transmissionMode`
   * Transmission mode for this protocol, valid flags: Owned, Ally, Enemy (or Everyone)
4. `string localName`
   * Name of this connection platform
5. `int sendLimit`
   * Maximal number of times packets will be attempted to be transmitted; min=1, default=5
    * After running out of tries the connection will be shutdown
6. `int sendTimeoutMs`
   * Amount of in-game milliseconds to wait before trying to re-send a packet; min=0, default=200
7. `Func<string, byte, bool> connectionAcceptor`
   * Delegate for handling incoming connection requests
     * parameter 0: [string] -> target name
     * patameter 1: [byte] -> connection channel
     * return: [bool] -> `true` to accept the connection, `false` to reject
8. `Action<IConnection> connectionListener`
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
   * Target name identifier - `localName` used in the initialization method
2. `byte channel`
   * Connection channel to use, same concept as ports - connection identifier number between two platforms

Return:
- `RTP4.IConnection`
  * Connection object
### API.5 Accepting Connections
If a platform invokes `OpenConnection` with the target name of the recieving platform, the `connectionAcceptor` delegate will be invoked immediately, and the returning value will decide whether the connection is opened or rejected.
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
   - Invoked at most once, when the connection has been closed for any reason, inclusing `Close()` invocation
- `Action<string> OnData { get; set; }`
   - Invoked every time the connection recieves data, after the target platform has invoked `SendData(string)`
- `bool IsOpen { get; }`
   - Indocates whether the connection is opened

Methods:
- `void SendData(string data)`
   - Queues up data to be sent to the target platform
     - Parameters:
     1. `string data` - data to send
- `void Close()`
   - Shuts down the connection immediately regardless of its state, has no effects if the connection has already been shutdown
