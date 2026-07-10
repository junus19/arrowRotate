using System;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace GameBrain.SDK
{
    public static class NetworkService
    {
        private const string Google_URL = "https://www.google.com";

        public static async Task CheckInternetConnection(Action<bool> callback)
        {
            try
            {
                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    callback(false);
                    return;
                }

                using UnityWebRequest request = UnityWebRequest.Head(Google_URL);
                request.timeout = 5;
                await request.SendWebRequest();
                callback(request.result == UnityWebRequest.Result.Success);
                await Task.CompletedTask;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
    }
}
