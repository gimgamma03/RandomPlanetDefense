using UnityEngine;
using UnityEngine.UI;

public class EnemyHpViewer : MonoBehaviour
{
    private EnemyHp enemyHp;
    private Slider hpSlider;

    public void Setup(EnemyHp enemyHp)
    {
        this.enemyHp = enemyHp;
        hpSlider = GetComponent<Slider>();
        hpSliderUpdate();
    }

    public void ClearForPool()
    {
        enemyHp = null;
        SliderPositionAutoSetter positionSetter = GetComponent<SliderPositionAutoSetter>();
        if (positionSetter != null)
        {
            positionSetter.Setup(null);
        }
    }

    public void hpSliderUpdate()
    {
        if (enemyHp == null || hpSlider == null || enemyHp.maxHp <= 0f)
        {
            return;
        }

        hpSlider.value = enemyHp.currentHp / enemyHp.maxHp;
    }

    private void Update()
    {
        if (enemyHp == null)
        {
            return;
        }

        hpSliderUpdate();
    }
}
