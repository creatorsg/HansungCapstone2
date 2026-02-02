using UnityEngine;

namespace inseon.Server.User.PlayerData
{
    [System.Serializable]
    public class UserGameData
    {
        public string playFabId;
        public string nickname;

        public int level;
        public int currency;
        public string rank;

        public int playCount;
        public int winCount;
    }
}
