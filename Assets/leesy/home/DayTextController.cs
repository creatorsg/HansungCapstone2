using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lsy
{
    public class DayTextController : MonoBehaviour
    {
        [SerializeField] private TMP_Text dayText;

        private static int currentDay = 1;
        private static bool wasInBattleScene;
        private static bool sceneHookInstalled;

        private static readonly string[] BattleSceneNames =
        {
            "Cathedral",
            "Harbor",
            "Industrial",
            "Noble",
            "Slum",
            "00cathedral",
            "00harbor",
            "00industrial",
            "00noble",
            "00slum"
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallSceneHook()
        {
            if (sceneHookInstalled) return;

            currentDay = 1;
            wasInBattleScene = false;
            SceneManager.sceneLoaded += OnSceneLoaded;
            sceneHookInstalled = true;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsHomeScene(scene.name))
            {
                if (wasInBattleScene)
                    currentDay++;

                wasInBattleScene = false;
                return;
            }

            if (IsBattleScene(scene.name))
                wasInBattleScene = true;
        }

        private void Awake()
        {
            if (dayText == null)
                dayText = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (dayText != null)
                dayText.text = $"{currentDay}day";
        }

        private static bool IsHomeScene(string sceneName)
        {
            return string.Equals(sceneName, "Home", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsBattleScene(string sceneName)
        {
            for (int i = 0; i < BattleSceneNames.Length; i++)
            {
                if (string.Equals(sceneName, BattleSceneNames[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
