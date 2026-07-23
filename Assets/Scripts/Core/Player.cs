using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    bool gameover = false;
    public int gold;
    public int maxHp;
    private int _currentHp;
    private SceneDirector sceneDirector;
    public int currentHp
    {
        get { return _currentHp; }
        private set { _currentHp = Mathf.Max(0, value); }
    }

    private int wallCount;
    private int towerCount;

    [SerializeField]
    private EnemySpawner enemySpawner;
    [SerializeField]
    private TowerSpawner towerSpawner;
    [SerializeField]
    private WaveSystem waveSystem;
    [SerializeField]
    private Image hitRedImage;

    private void Awake()
    {
        currentHp = maxHp;

    }
    // Start is called before the first frame update
    void Start()
    {
        sceneDirector = GetComponent<SceneDirector>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;

        StopCoroutine("HitAnimation");
        StartCoroutine("HitAnimation");

        if (currentHp <= 0 && !gameover)
        {
            gameover = true;
            //holy moly wtf game over damn it
            waveSystem.FinishGame();
            Invoke("GameOverScene", 3f);
        }
    }

    public void GameOverScene()
    {
        sceneDirector.OpeningScene();
    }
    private IEnumerator HitAnimation()
    {
        //이 이미지 UI는 화면 전체를 덥기 때문에 RayCastTarget을 꺼주자
        // 전체화면 크기로 배치된 imageScreen의 색상을 color 변수에 저장
        // imageScreen의 투명도를 40%로 설정
        Color color = hitRedImage.color;
        color.a = 0.4f;
        hitRedImage.color = color;

        // 투명도가 0%가 될때까지 감소
        while (color.a >= 0.0f)
        {
            color.a -= Time.deltaTime;
            hitRedImage.color = color;

            yield return null;
        }
    }
}
