using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Text.RegularExpressions;

public class AIChatManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI dialogueText;

    [Header("网络设置")]
    public string serverIP = "192.168.1.X";  // ✅ 替换为你电脑的实际IP地址
    public float refreshInterval = 2f;

    private string lastDisplayedText = "";

    [Header("可选动画")]
    public AIAnimatorManager animatorManager;

    void Start()
    {
        StartCoroutine(FetchReplyLoop());
    }

    IEnumerator FetchReplyLoop()
    {
        while (true)
        {
            yield return FetchLatestReply();
            yield return new WaitForSeconds(refreshInterval);
        }
    }

    IEnumerator FetchLatestReply()
    {
        string url = $"http://{serverIP}:5006/latest";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;

                string reply = ParseJsonReply(json);
                reply = CleanText(reply);

                Debug.Log("🌐 Raw JSON: " + json);
                Debug.Log("🧠 AI Reply: " + reply);

                if (!string.IsNullOrEmpty(reply) && reply != lastDisplayedText)
                {
                    lastDisplayedText = reply;
                    StopCoroutine("TypeText");
                    StartCoroutine(TypeText(reply, 0.04f));
                }
            }
            else
            {
                Debug.LogWarning("获取回复失败：" + request.error);
            }
        }
    }

    public void SendUserPrompt(string prompt)
    {
        StartCoroutine(SendPromptCoroutine(prompt));
    }

    IEnumerator SendPromptCoroutine(string prompt)
    {
        string url = $"http://{serverIP}:5006/ask";
        var requestBody = new AIRequest { prompt = prompt };
        string json = JsonUtility.ToJson(requestBody);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("发送失败: " + request.error);
            }
        }
    }

    // ✅ 改用 Unity 内建 JSON 解码器
    string ParseJsonReply(string json)
    {
        try
        {
            AIResponse response = JsonUtility.FromJson<AIResponse>(json);
            return response.reply;
        }
        catch
        {
            Debug.LogWarning("⚠️ JSON 解析失败: " + json);
            return "";
        }
    }

    IEnumerator TypeText(string message, float delay = 0.05f)
    {
        dialogueText.text = "";

        if (animatorManager != null)
            animatorManager.PlayTalkingAnimation();

        foreach (char c in message)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(delay);
        }

        if (animatorManager != null)
            animatorManager.PlayIdle();
    }

    string CleanText(string input)
    {
        Regex unicodeEscape = new Regex(@"\\u[0-9a-fA-F]{4}|\\U[0-9a-fA-F]{8}");
        return unicodeEscape.Replace(input, "").Trim();
    }

    [System.Serializable]
    public class AIRequest
    {
        public string prompt;
    }

    [System.Serializable]
    public class AIResponse
    {
        public string reply;
    }
}
