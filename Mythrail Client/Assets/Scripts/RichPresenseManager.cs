using System;
using Discord;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MythrailEngine
{
public class RichPresenseManager : MonoBehaviour
{
    private static RichPresenseManager _singleton;
    public static RichPresenseManager Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
                _singleton = value;
            else if (_singleton != value)
            {
                Debug.Log($"{nameof(RichPresenseManager)} instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }
    
    private Discord.Discord discord;
    public Activity currentActivity;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Singleton = this;
        SceneManager.sceneLoaded += (arg0, mode) =>
        {
            Singleton = this;
        };
    }

    void Start()
    {
        discord = new Discord.Discord(1026417033409216522, (UInt64)CreateFlags.Default);
        discord.GetActivityManager().ClearActivity(result =>
        {
            
        });
        UpdateStatus("In Main Menu", "Idling", false);
    }

    public void UpdateStatus(string details, string state, bool keepCurrentTime)
    {
        var activityManager = discord.GetActivityManager();
        var activity = new Activity
        {
            Details = details,
            State = state,
            Assets =
            {
                LargeImage = "screenshot1",
            },
            Timestamps =
            {
                Start = keepCurrentTime ? currentActivity.Timestamps.Start : DateTimeOffset.Now.ToUnixTimeMilliseconds()
            }
        };
        currentActivity = activity;
        activityManager.UpdateActivity(activity, (res) =>
        {
            if (res == Result.Ok)
            {
                Debug.Log("Discord status set");
            }
            else
            {
                Debug.LogError("Discord status failed");
            }
        });
    }

    private void OnApplicationQuit()
    {
        var activityManager = discord.GetActivityManager();
        activityManager.ClearActivity((result) =>
        {
            if (result == Result.Ok)
            {
                Debug.Log("Cleared status");
            }
            else
            {
                Debug.LogError("Could not clear status");
            }
        });
    }

    private void Update()
    {
        discord.RunCallbacks();
    }
}
   
}