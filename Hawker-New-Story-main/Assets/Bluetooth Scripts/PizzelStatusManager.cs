using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 两行显示 8 个喷嘴（A1~A4 / B1~B4）
/// 仅依据“最近一次发送的 code”判断工作状态（1..4=工作，其它=停止）；
/// 同时展示“发往的板（A/B）”以及“发送当时 BLEManager 是否处于已连接状态”。 
/// </summary>
public class PizzelStatusManager : MonoBehaviour
{
    [Header("把一个 UI Text 拖到这里（必须在 Canvas 下）")]
    public Text statusText;

    [Header("显示风格")]
    public bool useRichColor = true;
    public string workingLabel = "工作中";
    public string stoppedLabel = "停止";
    public string linkOkLabel = "OK";
    public string linkNoLabel = "NO";

    private class Node
    {
        public bool hasTx;
        public byte lastCode;        // 最近一次发送的码
        public bool working;         // 1..4 视为工作
        public string lastBoardKey;  // "A"/"B"
        public bool lastLinkReady;   // 发送当时 BLEManager 是否连接就绪
    }

    private readonly Dictionary<string, Node> _nodes = new Dictionary<string, Node>
    {
        { "A1", new Node() }, { "A2", new Node() }, { "A3", new Node() }, { "A4", new Node() },
        { "B1", new Node() }, { "B2", new Node() }, { "B3", new Node() }, { "B4", new Node() },
    };

    /// <summary>
    /// 由触发脚本调用：报告“刚刚发往 boardKey，码=code，发送当时 linkReady？”
    /// </summary>
    public void ReportSent(string pizzelId, string boardKey, byte code, bool linkReady)
    {
        if (!_nodes.TryGetValue(pizzelId, out var n)) return;

        n.hasTx = true;
        n.lastBoardKey = boardKey;
        n.lastCode = code;
        n.lastLinkReady = linkReady;
        n.working = (code >= 1 && code <= 4);

        RefreshText();
    }

    void Start() => RefreshText();

    void RefreshText()
    {
        if (statusText == null) return;

        var sb = new StringBuilder();

        // A 行
        sb.Append(FormatCell("A1")).Append("   ");
        sb.Append(FormatCell("A2")).Append("   ");
        sb.Append(FormatCell("A3")).Append("   ");
        sb.Append(FormatCell("A4")).Append("\n");

        // B 行
        sb.Append(FormatCell("B1")).Append("   ");
        sb.Append(FormatCell("B2")).Append("   ");
        sb.Append(FormatCell("B3")).Append("   ");
        sb.Append(FormatCell("B4"));

        statusText.supportRichText = useRichColor;
        statusText.alignment = TextAnchor.UpperLeft;
        statusText.text = sb.ToString();
    }

    string FormatCell(string key)
    {
        var n = _nodes[key];

        if (!n.hasTx)
        {
            return useRichColor
                ? $"<b>{key}:</b><color=#999999>未发送</color>"
                : $"{key}:未发送";
        }

        string state = n.working ? workingLabel : stoppedLabel;
        string link  = n.lastLinkReady ? linkOkLabel : linkNoLabel;
        string tail  = $"(板={n.lastBoardKey}, tx={n.lastCode}, link={link})";

        if (!useRichColor) return $"{key}:{state} {tail}";

        string stateColor = n.working ? "#2ECC71" : "#999999"; // 绿/灰
        string linkColor  = n.lastLinkReady ? "#2ECC71" : "#E67E22"; // OK=绿, NO=橙
        return $"<b>{key}:</b><color={stateColor}>{state}</color> <color=#BBBBBB>(板={n.lastBoardKey}, tx={n.lastCode},</color> <color={linkColor}>link={link}</color><color=#BBBBBB>)</color>";
    }
}


