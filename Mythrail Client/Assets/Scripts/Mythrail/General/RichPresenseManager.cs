using System;
using Discord;
using UnityEngine;

namespace Mythrail.General
{
    public class RichPresenseManager : MonoBehaviour
    {
        public static RichPresenseManager Singleton;
        
        private Discord.Discord discord;
        public Activity currentActivity;
        
        private void Awake()
        {
            Singleton = this;
        }
    
        void Start()
        {
            discord = new Discord.Discord(1026417033409216522, (UInt64)CreateFlags.Default);
            discord.GetActivityManager().ClearActivity(result =>
            {
                
            });
        }
    
        private void updateStatus(string details, string state, bool keepCurrentTime)
        {
            if(Application.isEditor) return;
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

        public static void UpdateStatus(string details, string state, bool keepCurrentTime)
        {
            if (Singleton)
            {
                Singleton.updateStatus(details, state, keepCurrentTime);
            }
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