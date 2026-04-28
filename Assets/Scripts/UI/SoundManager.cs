using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Mining Sounds")]
    public AudioClip miningLv1;         // 곡괭이 소리
    public AudioClip miningLv2;         // 양손 곡괭이 소리
    public AudioClip miningLv3;         // 포크레인 소리

    [Header("Stone")]
    public AudioClip stoneDrop;         // 돌 SetterZone에 놓는 소리

    [Header("Handcuff")]
    public AudioClip handcuffPickup;    // 수갑 줍는 소리
    public AudioClip handcuffDrop;      // 수갑 놓는 소리

    [Header("Prisoner")]
    public AudioClip prisonerComplete;  // 수감자 수갑 충족 소리

    [Header("Money")]
    public AudioClip moneyDeposit;      // 돈 넣는 소리
    public AudioClip moneyWithdraw;     // 돈 꺼내는 소리
    public AudioClip upgradeComplete;   // 업그레이드 완료 소리

    [Header("Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;

    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    // ─── 사운드 재생 ──────────────────────────────────────
    public void Play(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, masterVolume * volumeScale);
    }

    // ─── 개별 사운드 메서드 ───────────────────────────────
    public void PlayMining(int toolLevel)
    {
        switch (toolLevel)
        {
            case 1: Play(miningLv1); break;
            case 2: Play(miningLv2); break;
            case 3: Play(miningLv3); break;
        }
    }

    public void PlayStoneDrop() => Play(stoneDrop);
    public void PlayHandcuffPickup() => Play(handcuffPickup);
    public void PlayHandcuffDrop() => Play(handcuffDrop);
    public void PlayPrisonerComplete() => Play(prisonerComplete);
    public void PlayMoneyDeposit() => Play(moneyDeposit);
    public void PlayMoneyWithdraw() => Play(moneyWithdraw);
    public void PlayUpgradeComplete() => Play(upgradeComplete);
}