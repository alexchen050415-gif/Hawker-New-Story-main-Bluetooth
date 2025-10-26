using UnityEngine;

public class AIAnimatorManager : MonoBehaviour
{
    [Header("角色 Animator 控制")]
    public Animator characterAnimator;
    public string[] talkAnimationNames;
    public string idleAnimationName = "Idle";

    private string lastPlayedAnim = "";

    // 播放一次随机交谈动画
    public void PlayTalkingAnimation()
    {
        if (characterAnimator == null || talkAnimationNames.Length == 0) return;

        int index = Random.Range(0, talkAnimationNames.Length);
        lastPlayedAnim = talkAnimationNames[index];
        characterAnimator.Play(lastPlayedAnim);
    }

    // 打字机完成后切换回 Idle
    public void PlayIdle()
    {
        if (characterAnimator != null && !string.IsNullOrEmpty(idleAnimationName))
        {
            characterAnimator.Play(idleAnimationName);
        }
    }
}

