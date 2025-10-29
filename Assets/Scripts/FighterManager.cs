using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FighterManager : MonoBehaviour
{
    List<Fighter> fightingPool = new();
    [SerializeField] GameObject fighterPrefab;

    private void Awake()
    {
        for (int i = 0; i < 20; i++)
        {
            Fighter newFighter = Instantiate(fighterPrefab).GetComponent<Fighter>();
            newFighter.gameObject.SetActive(false);
            fightingPool.Add(newFighter);
        }

        StartCoroutine(startFight());
    }

    IEnumerator startFight()
    {
        Fighter fighter1 = fightingPool[0];
        Fighter fighter2 = fightingPool[1];

        fighter1.gameObject.SetActive(true);
        fighter2.gameObject.SetActive(true);

        fighter1.resetForBattle();
        fighter2.resetForBattle();

        fighter2.transform.position += new Vector3(0, 5, 0);

        float tickTime = 0.25f;
        for (int i = 0; i < 100; i++)
        {
            yield return new WaitForSeconds(tickTime * 0.5f);
            fighter1.pollForOutput(tickTime);
            yield return new WaitForSeconds(tickTime * 0.5f);
            fighter2.pollForOutput(tickTime);

        }
        Fighter winner = fighter2;
        Fighter loser = fighter1;
        if(fighter1.rewardScore() >= fighter2.rewardScore())
        {
            winner = fighter1;
            loser = fighter2;
        }

        Destroy(loser);
        Fighter newFighter = winner.reproduce();
        newFighter.mutateSelf(0.05f);
        fightingPool.Add(newFighter);

        Debug.Log($"Winner XML:");
        winner.debugPrintXml();

        yield return new WaitForSeconds(1f);
    }
}
