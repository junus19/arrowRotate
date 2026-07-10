using UnityEngine;
using System.Threading.Tasks;

#if FACEBOOK_ENABLED
using Facebook.Unity;
#endif

namespace GameBrain.SDK
{
    public class FacebookModule
    {
        private bool _isInitialized;
        
        public bool IsInitialized => _isInitialized;
        
        public Task Initialize()//async Task Initialize()
        {
#if FACEBOOK_ENABLED
            // if (FB.IsInitialized)
            // {
            //     FB.ActivateApp();
            // }
            // else
            // {
            //     FB.Init(() =>
            //     {
            //         FB.ActivateApp();
            //     });
            // }
            // _isInitialized = true;

            if (!FB.IsInitialized)
            {
                Debug.Log("Facebook SDK initialization.");
                // Initialize the Facebook SDK
                FB.Init(InitCallback, OnHideUnity);
            }
            else
            {
                // Already initialized, signal an app activation App Event
                FB.ActivateApp();
                Debug.Log("Facebook SDK initialized successfully.");
                _isInitialized = true;
            }
#endif
            return Task.CompletedTask;
        }

        private void InitCallback()
        {
#if FACEBOOK_ENABLED
            if (FB.IsInitialized)
            {
                // Signal an app activation App Event
                FB.ActivateApp();
                // Continue with Facebook SDK
                // ...
                Debug.Log("Facebook SDK initialized successfully.");
                _isInitialized = true;
            }
            else
            {
                Debug.Log("Failed to Initialize the Facebook SDK");
            }
#endif
        }

        private void OnHideUnity(bool isGameShown)
        {
            if (!isGameShown)
            {
                // Pause the game - we will need to hide
                Time.timeScale = 0;
            }
            else
            {
                // Resume the game - we're getting focus again
                Time.timeScale = 1;
            }
        }
    }
}

