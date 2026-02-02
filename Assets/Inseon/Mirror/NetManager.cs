using insoen.Server.Mirrror.Network;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace insoen.Server.Playfab.Network
{
    public class NetManager : MonoBehaviour
    {
        public static NetManager Instance { get; private set; }

        [SerializeField] private Button _loginBtn;
        [SerializeField] private Text _statusTxt;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else Destroy(gameObject);
        }

        private void Start()
        {
            _loginBtn.interactable = false;
            _statusTxt.text = "Ready to connect";
        }

        // PlayFab 로그인 성공 후 서버 접속
        public void Connect(string playerName)
        {
            _statusTxt.text = "Connecting to server...";
            _loginBtn.interactable = false;

            if (NetworkManager.singleton is MyNetworkManager myNM)
            {
                myNM.PlayerName = playerName;       // 서버에 전달할 닉네임
                NetworkManager.singleton.StartClient();
            }
            else
            {
                _statusTxt.text = "Server setup error";
            }
        }

        // Mirror 클라이언트 접속 성공 시
        public void OnConnected()
        {
            _statusTxt.text = "Connected!";
            SceneManager.LoadScene("KingdomScene");
        }

        // Mirror 클라이언트 접속 실패 시
        public void OnDisconnected()
        {
            _statusTxt.text = "Disconnected from server";
            _loginBtn.interactable = true;
        }
    }
}
