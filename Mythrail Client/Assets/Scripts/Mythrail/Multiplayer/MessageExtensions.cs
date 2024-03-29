using Mythrail.MainMenu.Tabs.Invites;
using Mythrail.Players;
using Riptide;
using UnityEngine;

namespace Mythrail.Multiplayer
{
    public static class MessageExtensions
    {
        #region Vector2
        public static Message AddVector2(this Message message, Vector2 value) => Add(message, value);

        public static Message Add(this Message message, Vector2 value)
        {
            message.AddFloat(value.x);
            message.AddFloat(value.y);
            return message;
        }

        public static Vector2 GetVector2(this Message message)
        {
            return new Vector2(message.GetFloat(), message.GetFloat());
        }
        #endregion

        #region Vector3
        public static Message AddVector3(this Message message, Vector3 value) => Add(message, value);

        public static Message Add(this Message message, Vector3 value)
        {
            message.AddFloat(value.x);
            message.AddFloat(value.y);
            message.AddFloat(value.z);
            return message;
        }

        public static Vector3 GetVector3(this Message message)
        {
            return new Vector3(message.GetFloat(), message.GetFloat(), message.GetFloat());
        }
        #endregion

        #region Quaternion
        public static Message AddQuaternion(this Message message, Quaternion value) => Add(message, value);

        public static Message Add(this Message message, Quaternion value)
        {
            message.AddFloat(value.x);
            message.AddFloat(value.y);
            message.AddFloat(value.z);
            message.AddFloat(value.w);
            return message;
        }

        public static Quaternion GetQuaternion(this Message message)
        {
            return new Quaternion(message.GetFloat(), message.GetFloat(), message.GetFloat(), message.GetFloat());
        }
        #endregion

        #region MatchInfo

        public static MatchInfo[] GetMatchInfos(this Message message)
        {
            string[] matchNames = message.GetStrings();
            string[] matchCreatorNames = message.GetStrings();
            ushort[] matchPorts = message.GetUShorts();
            string[] codes = message.GetStrings();
            
            MatchInfo[] infos = new MatchInfo[matchNames.Length];

            for (int i = 0; i < matchNames.Length; i++)
            {
                infos[i] = new MatchInfo(matchNames[i], matchCreatorNames[i], matchPorts[i], codes[i]);
            }

            return infos;
        }

        public static ClientInviteInfo[] GetClientInfos(this Message message)
        {
            ushort[] ids = message.GetUShorts();
            string[] usernames = message.GetStrings();
            
            ClientInviteInfo[] infos = new ClientInviteInfo[ids.Length];

            for (int i = 0; i < ids.Length; i++)
            {
                infos[i] = new ClientInviteInfo(ids[i], usernames[i]);
            }

            return infos;
        }

        public static Message AddClientInfos(this Message message, ClientInviteInfo[] value) => Add(message, value);
        
        public static Message Add(this Message message, ClientInviteInfo[] value)
        {
            ushort[] ids = new ushort[value.Length];
            string[] usernames = new string[value.Length];
            
            for (int i = 0; i < value.Length; i++)
            {
                ids[i] = value[i].id;
                usernames[i] = value[i].username;
            }
            
            message.AddUShorts(ids);
            message.AddStrings(usernames);
            
            return message;
        }
        #endregion

        #region PlayerStatesAndInputs

        public static Message AddPlayerInput(this Message message, PlayerInput value) => Add(message, value);

        public static Message Add(this Message message, PlayerInput value)
        {
            message.AddBools(value.inputs);
            message.AddVector3(value.forward);
            message.AddUInt(value.tick);
            return message;
        }

        public static PlayerInput GetPlayerInput(this Message message)
        {
            PlayerInput input = new PlayerInput();
            input.inputs = message.GetBools();
            input.forward = message.GetVector3();
            input.tick = message.GetUInt();
            return input;
        }

        public static Message AddPlayerState(this Message message, PlayerMovementState value) => Add(message, value);

        public static Message Add(this Message message, PlayerMovementState value)
        {
            message.AddVector3(value.position);
            message.AddVector3(value.forward);
            message.AddBool(value.didTeleport);
            message.AddUInt(value.tick);
            return message;
        }

        public static PlayerMovementState GetPlayerState(this Message message)
        {
            PlayerMovementState state = new PlayerMovementState();
            state.position = message.GetVector3();
            state.forward = message.GetVector3();
            state.didTeleport = message.GetBool();
            state.tick = message.GetUInt();
            return state;
        }

        #endregion
    }
    
    public class MatchInfo
    {
        public string name;
        public string creatorName;
        public ushort port;
        public string code;

        public MatchInfo(string name, string creatorName, ushort port, string code)
        {
            this.name = name;
            this.creatorName = creatorName;
            this.port = port;
            this.code = code;
        }
    }

}