using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets
{
    public class TelegramBot
    {
        public static string SendMessage(string msg)
        {
            string apiToken = "803940137:AAGZ7Y0Fk681ujm71KZCUtkyYSNKCP0C1Yk";
            string destID = "371151579" ;

            msg = "*Unity Minimal Surprise*\n"+msg;
            string urlString = $"https://api.telegram.org/bot{apiToken}/sendMessage?chat_id={destID}&text={msg}&parse_mode=markdown";
            WebClient webclient = new WebClient();
            return webclient.DownloadString(urlString);
        }
        
        /// <summary>
        /// Execute with
        /// StartCoroutine(SendMessageCoroutine("your message here"));
        /// </summary>
        /// <param name="msg">message string</param>
        /// <returns></returns>
        public static IEnumerator SendMessageCoroutine(string msg) {
            string apiToken = "803940137:AAGZ7Y0Fk681ujm71KZCUtkyYSNKCP0C1Yk";
            string destID = "371151579" ;

            msg = "*Unity Minimal Surprise*\n" +
                  $"Runtime Platform: `{Application.platform.ToString()}`\n" +
                  msg;
            UnityWebRequest www = UnityWebRequest.Get($"https://api.telegram.org/bot{apiToken}/sendMessage?chat_id={destID}&text={msg}&parse_mode=markdown");
            yield return www.SendWebRequest();
            
            if(www.isNetworkError || www.isHttpError) {
                Debug.Log("Error in Telegram Bot connection: " + www.error);
            }
            else {
                // Show results as text
                //Debug.Log(www.downloadHandler.text);
 
                // Or retrieve results as binary data
                byte[] results = www.downloadHandler.data;
            }
        }
        
        
    }
}