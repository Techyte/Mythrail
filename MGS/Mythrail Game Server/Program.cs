﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Riptide;
using Riptide.Utils;

namespace Mythrail_Game_Server
{
    #region MessageIds

    public enum GameServerToClientId : ushort
    {
        matches = 100,
        createMatchSuccess,
        joinedMatch,
        matchNotFound,
        invalidName,
        playersResult,
        invite,
    }

    public enum ClientToGameServerId : ushort
    {
        id = 100,
        updateUsername,
        requestMatches,
        createMatch,
        joinMatch,
        getPlayers,
        invites,
    }

    #endregion

    public class Program
    {
         public static Dictionary<int, ClientInfo> currentlyConnectedClients = new Dictionary<int, ClientInfo>();
        
         private static Program _Program;
        
         private static Server Server;
        
         private ushort port = 63231;
         private ushort maxClientCount = 100;
        
         private static List<MatchInfo> matches = new List<MatchInfo>();

         private bool isRunning;

        public static void Main()
        {
            _Program = new Program();
            _Program.Start();
        }

         private void Start()
         {
             RiptideLogger.Initialize(Console.Write, Console.Write, Console.Write, Console.Write, false);
             isRunning = true;
             
             new Thread(new ThreadStart(Loop)).Start();
             
             Console.WriteLine("Press enter to stop the server at any time.");
             Console.ReadLine();

             isRunning = false;

             Console.ReadLine();
         }

         private void Loop()
         {
             Server = new Server();
             Server.Start(port, maxClientCount);
             Server.ClientDisconnected += ClientDisconnected;
             
             int interval = 16;
             while (isRunning)
             {
                 ServerTick();
                 Thread.Sleep(interval);
             }
         }
        
         private void ServerTick()
         {
             Server.Update();
         }
        
         private void ClientDisconnected(object sender, ServerDisconnectedEventArgs e)
         {
             if (currentlyConnectedClients.TryGetValue(e.Client.Id, out ClientInfo clientInfo))
                 currentlyConnectedClients.Remove(e.Client.Id);
         }
        
         private void SendMatches(ushort clientId)
         {
             Message message = AddMatchInfos();
        
             Server.Send(message, clientId);
         }
        
         private static void SendMatchesToAll()
         {
             Message message = _Program.AddMatchInfos();
        
             Server.SendToAll(message);
         }
        
         private void SendMatchCreationConformation(ushort fromClientId, ushort port, bool isPrivate, string code)
         {
             Message message = Message.Create(MessageSendMode.Reliable, GameServerToClientId.createMatchSuccess);
             message.AddBool(isPrivate);
             message.AddString(code);
             message.AddUShort(port);
             Server.Send(message, fromClientId);
         }
        
         private static void MatchEnded(MatchInfo match)
         {
             matches.Remove(match);
             Console.WriteLine("Match Ended");
             SendMatchesToAll();
         }
        
         private void PrivateMatchFound(ushort toClientId, ushort port)
         {
             Message message = Message.Create(MessageSendMode.Reliable, GameServerToClientId.joinedMatch);
             message.AddUShort(port);
             Server.Send(message, toClientId);
         }
        
         private void SendInvite(ushort fromId, ushort toId, ushort port)
         {
             ClientInfo info;
             currentlyConnectedClients.TryGetValue(fromId, out info);
             string username = info.username;
             MatchInfo matchInfo = MatchInfoFromPort(port);
             Message message = Message.Create(MessageSendMode.Reliable, GameServerToClientId.invite);
             message.AddString(username);
             message.AddUShort(port);
             message.AddString(matchInfo.code);
             message.AddString(matchInfo.name);
             Server.Send(message, toId);
         }

         private MatchInfo MatchInfoFromPort(ushort port)
         {
             for (int i = 0; i < matches.Count; i++)
             {
                 if (matches[i].port == port)
                 {
                     return matches[i];
                 }
             }

             return new MatchInfo();
         }

         private MatchInfo MatchInfoFromCode(string code)
         {
             for (int i = 0; i < matches.Count; i++)
             {
                 if (matches[i].code == code)
                 {
                     return matches[i];
                 }
             }

             return new MatchInfo();
         }
        
         private Message AddMatchInfos()
         {
             Message message = Message.Create(MessageSendMode.Reliable, GameServerToClientId.matches);
             List<MatchInfo> publicMatches = new List<MatchInfo>();
             foreach (var match in matches)
             {
                 if (!match.isPrivate)
                 {
                     publicMatches.Add(match);
                 }
             }
        
             message.AddMatchInfos(publicMatches.ToArray());
        
             return message;
         }
        
         static ushort FreeTcpPort()
         {
             TcpListener l = new TcpListener(IPAddress.Loopback, 0);
             l.Start();
             ushort port = (ushort)((IPEndPoint)l.LocalEndpoint).Port;
             l.Stop();
             return port;
         }
        
         private string GenerateGameCode()
         {
             Random random = new Random();
             string code = "";
             for (int i = 0; i < 5; i++)
             {
                 int randValue = random.Next(0, 26);
                 char letter = Convert.ToChar(randValue + 65);
                 code += letter;
             }
        
             return code;
         }
        
         private MatchCreationInfo CreateMatch(ushort creatorId, ushort maxPlayers, ushort minPlayers, string name,
             bool isPrivate)
         {
             Process matchProcess = new Process();
             matchProcess.EnableRaisingEvents = true;

             string dir = Directory.GetCurrentDirectory();
             if(IsLinux())
             {
                 dir += "/Match Application/Mythrail Server.x86_64";
             }
             else
             {
                 dir += "/Match Application/Windows/Mythrail Server.exe";
             }
             
             Console.WriteLine(dir);
             matchProcess.StartInfo.FileName = dir;
        
             ushort port = FreeTcpPort();
             string code = GenerateGameCode();
             
             if (IsLinux())
             {
                 matchProcess.StartInfo.Arguments = "-batchmode -nographics";
             }
             else
             {
                 matchProcess.StartInfo.Arguments = "";
             }
             
             matchProcess.StartInfo.Arguments +=
                 $" port:{port.ToString()} maxPlayers:{maxPlayers.ToString()} minPlayers:{minPlayers.ToString()} isPrivate:{isPrivate.ToString()} code:{code}";
             
             matchProcess.Start();
        
             MatchInfo newMatch = new MatchInfo(name, currentlyConnectedClients[creatorId].username, matchProcess, port,
                 isPrivate, code);
             matches.Add(newMatch);
             matchProcess.Exited += (sender, eventArgs) => { MatchEnded(newMatch); };
        
             return new MatchCreationInfo(port, code);
         }
         
         private bool IsLinux()
         {
             int p = (int) Environment.OSVersion.Platform;
             return p == 4 || p == 6 || p == 128;
         }
        
         #region Message Handlers

         [MessageHandler((ushort)ClientToGameServerId.id)]
         private static void ReceiveConnecrId(ushort fromClientId, Message message)
         {
             currentlyConnectedClients.Add(fromClientId, new ClientInfo(fromClientId, message.GetString()));
             Console.WriteLine(currentlyConnectedClients[fromClientId].username);
             Console.WriteLine("Client connected and we are receiving information");
         }
        
         [MessageHandler((ushort)ClientToGameServerId.createMatch)]
         private static void CreateMatchHandler(ushort fromClientId, Message message)
         {
             ushort maxPlayers = message.GetUShort();
             ushort minPlayers = message.GetUShort();
             string name = message.GetString();
             bool isPrivate = message.GetBool();
        
             MatchCreationInfo creationInfo =
                 _Program.CreateMatch(fromClientId, maxPlayers, minPlayers, name, isPrivate);
        
             _Program.SendMatchCreationConformation(fromClientId, creationInfo.port, isPrivate, creationInfo.code);
        
             SendMatchesToAll();
         }
        
         [MessageHandler((ushort)ClientToGameServerId.requestMatches)]
         private static void MatchesRequested(ushort fromClientId, Message message)
         {
             _Program.SendMatches(fromClientId);
             Console.WriteLine("Client requested a matches list");
         }
        
         [MessageHandler((ushort)ClientToGameServerId.updateUsername)]
         private static void UpdateUsername(ushort fromClientId, Message message)
         {
             if (currentlyConnectedClients.TryGetValue(fromClientId, out ClientInfo clientInfo))
             {
                 string newUsername = message.GetString();
                 foreach (ClientInfo client in currentlyConnectedClients.Values)
                 {
                     if (client.username == newUsername)
                     {
                         Message invalidUsernameMessage =
                             Message.Create(MessageSendMode.Reliable, GameServerToClientId.invalidName);
                         Server.Send(invalidUsernameMessage, fromClientId);
                         return;
                     }
                 }
        
                 clientInfo.username = newUsername;
                 Console.WriteLine("Updated username: " + clientInfo.username);
             }
         }
        
         [MessageHandler((ushort)ClientToGameServerId.joinMatch)]
         private static void JoinPrivateMatch(ushort fromClientId, Message message)
         {
             string codeKey = message.GetString();
             foreach (MatchInfo match in matches)
             {
                 if (match.code == codeKey)
                 {
                     _Program.PrivateMatchFound(fromClientId, match.port);
                     return;
                 }
             }
        
             Message failedMessage = Message.Create(MessageSendMode.Reliable, GameServerToClientId.matchNotFound);
             Server.Send(failedMessage, fromClientId);
         }
        
         [MessageHandler((ushort)ClientToGameServerId.getPlayers)]
         private static void GetPlayersForInviting(ushort fromClientId, Message message)
         {
             Message resultMessage = Message.Create(MessageSendMode.Reliable, GameServerToClientId.playersResult);
             List<ClientInfo> infos = new List<ClientInfo>();
             foreach (var info in currentlyConnectedClients.Values)
             {
                 if (info.id != fromClientId)
                 {
                     infos.Add(info);
                 }
             }
             resultMessage.AddClientInfos(infos.ToArray());
             Server.Send(resultMessage, fromClientId);
         }
        
         [MessageHandler((ushort)ClientToGameServerId.invites)]
         private static void InvitePlayers(ushort fromClientId, Message message)
         {
             ClientInfo[] infos = message.GetClientInfos();
             ushort port = message.GetUShort();
             foreach (var client in infos)
             {
                 _Program.SendInvite(fromClientId, client.id, port);
             }
         }
        
         #endregion Message Handlers
    }
    
     #region Custom Data Types
    
     public struct MatchCreationInfo
     {
         public ushort port;
         public string code;
    
         public MatchCreationInfo(ushort port, string code)
         {
             this.port = port;
             this.code = code;
         }
     }
    
     public struct ClientInfo
     {
         public ushort id;
         public string username;
    
         public ClientInfo(ushort id, string username)
         {
             this.id = id;
             this.username = username;
         }
     }
    
     public struct MatchInfo
     {
         public string name;
         public string creatorName;
         public Process process;
         public ushort port;
         public bool isPrivate;
         public string code;
    
         public MatchInfo(string name, string creatorName, Process process, ushort port, bool isPrivate, string code)
         {
             this.name = name;
             this.creatorName = creatorName;
             this.process = process;
             this.port = port;
             this.isPrivate = isPrivate;
             this.code = code;
         }
     }
    
     #endregion
}