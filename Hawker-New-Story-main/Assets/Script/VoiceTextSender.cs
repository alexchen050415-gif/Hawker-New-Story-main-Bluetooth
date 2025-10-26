using UnityEngine;
using System.Collections;
using System.Text;
using UnityEngine.Networking;

public class VoiceTextSender : MonoBehaviour
{
    public AIChatManager chatManager;

    public void SendTextToBackend(string recognizedText)
    {
        Debug.Log("识别到的文本：" + recognizedText);
        string url = $"http://{chatManager.serverIP}:5006/receive";
        StartCoroutine(PostRequest(url, recognizedText));
    }

    IEnumerator PostRequest(string url, string text)
    {
        string json = "{\"text\":\"" + text + "\"}";
        byte[] jsonToSend = new UTF8Encoding().GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Text sent successfully: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Error sending text: " + request.error);
        }
    }
}
