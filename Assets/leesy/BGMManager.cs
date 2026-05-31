using UnityEngine;

namespace Lsy {
    public class BGMManager : MonoBehaviour
    {
        // 어디서든 접근할 수 있도록 싱글톤 인스턴스 생성
        public static BGMManager instance;

        private AudioSource audioSource;

        private void Awake()
        {
            // 이미 인스턴스가 존재한다면 (씬 이동 후 다시 돌아왔을 때)
            if (instance != null)
            {
                Destroy(gameObject); // 새로 생긴 오브젝트를 파괴하여 중복 방지
                return;
            }

            // 처음 생성될 때 인스턴스 지정 및 씬 전환 시 파괴 방지
            instance = this;
            DontDestroyOnLoad(gameObject);

            // AudioSource 컴포넌트 가져오기
            audioSource = GetComponent<AudioSource>();
        }
    }
}