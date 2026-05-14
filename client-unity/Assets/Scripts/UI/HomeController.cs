using UnityEngine;
using UnityEngine.UI;
using Ludo.Audio;
using Ludo.Core;

namespace Ludo.UI
{
    /// <summary>
    /// Home screen: navigation to all game modes. Wire buttons in the Inspector.
    /// </summary>
    public class HomeController : MonoBehaviour
    {
        [SerializeField] private Button playBotButton;
        [SerializeField] private Button playLocalButton;
        [SerializeField] private Button playPrivateButton;
        [SerializeField] private Button playOnlineButton;
        [SerializeField] private Button leaderboardButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button shopButton;

        private void Start()
        {
            if (playBotButton)     playBotButton.onClick.AddListener(() => Go(ScreenManager.Lobby, "bot"));
            if (playLocalButton)   playLocalButton.onClick.AddListener(() => Go(ScreenManager.Lobby, "local"));
            if (playPrivateButton) playPrivateButton.onClick.AddListener(() => Go(ScreenManager.Lobby, "private"));
            if (playOnlineButton)  playOnlineButton.onClick.AddListener(() => Go(ScreenManager.Matchmaking, "online"));

            if (leaderboardButton) leaderboardButton.onClick.AddListener(() => Debug.Log("Leaderboard"));
            if (settingsButton)    settingsButton.onClick.AddListener(() => Debug.Log("Settings"));
            if (shopButton)        shopButton.onClick.AddListener(() => Debug.Log("Shop"));

            if (ServiceLocator.TryGet<AudioManager>(out var a))
            {
                a.PlayMusic(AudioManager.ClipBgm);
            }
        }

        private void Go(string scene, string mode)
        {
            PlayerPrefs.SetString("ludo.requestedMode", mode);
            if (ServiceLocator.TryGet<AudioManager>(out var a))
                a.PlaySfx(AudioManager.ClipClick);
            ScreenManager.Load(scene);
        }
    }
}
