using UnityEngine;

namespace Jun
{
    public class AnimEventReceiver : MonoBehaviour
    {
        public PlayerView playerView;

        public void EndAnim(string name)
        {
            playerView.EndAnim(name);
        }
    }
}