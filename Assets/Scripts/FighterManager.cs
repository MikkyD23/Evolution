using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class FighterManager : MonoBehaviour
{
    const int POOL_SIZE = 100;

    List<Fighter> fightingPool = new();
    [SerializeField] GameObject fighterPrefab;
    [SerializeField] GameObject arenaPrefab;

    private void Awake()
    {
        Time.timeScale = 2f;
        for (int i = 0; i < POOL_SIZE; i++)
        {
            Fighter newFighter = Instantiate(fighterPrefab).GetComponent<Fighter>();
            newFighter.mutateSelf(10f);
            newFighter.gameObject.SetActive(false);
            fightingPool.Add(newFighter);
        }

        StartCoroutine(startFight(new Vector2(0, 0)));
        StartCoroutine(startFight(new Vector2(25, 0)));
        StartCoroutine(startFight(new Vector2(50, 0)));
        StartCoroutine(startFight(new Vector2(75, 0)));

        StartCoroutine(startFight(new Vector2(0, 25)));
        StartCoroutine(startFight(new Vector2(25, 25)));
        StartCoroutine(startFight(new Vector2(50, 25)));
        StartCoroutine(startFight(new Vector2(75, 25)));

        StartCoroutine(startFight(new Vector2(0, 50)));
        StartCoroutine(startFight(new Vector2(25, 50)));
        StartCoroutine(startFight(new Vector2(50, 50)));
        StartCoroutine(startFight(new Vector2(75, 50)));

        StartCoroutine(startFight(new Vector2(0, 75)));
        StartCoroutine(startFight(new Vector2(25, 75)));
        StartCoroutine(startFight(new Vector2(50, 75)));
        StartCoroutine(startFight(new Vector2(75, 75)));

    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.Alpha0))
        {
            Time.timeScale = 0;
        }
        else if (Input.GetKey(KeyCode.Alpha1))
        {
            Time.timeScale = 1;
        }
        else if (Input.GetKey(KeyCode.Alpha2))
        {
            Time.timeScale = 5;
        }
        else if (Input.GetKey(KeyCode.Alpha3))
        {
            Time.timeScale = 10;
        }
    }

    IEnumerator startFight(Vector2 location)
    {
        GameObject arena = Instantiate(arenaPrefab);
        arena.transform.position = location;
        Fighter fighter1 = chooseFighter();
        Fighter fighter2 = chooseFighter();

        fighter1.transform.position = location + new Vector2(-2.5f, 0);
        fighter2.transform.position = location + new Vector2(2.5f, 0);

        float tickTime = 0.25f;
        for (int i = 0; i < 60; i++)
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

        print($"winner score: {winner.rewardScore()}. loser score: {loser.rewardScore()}");

        fightingPool.Remove(loser);
        Destroy(loser.gameObject);
        Fighter newFighter = winner.reproduce();
        newFighter.mutateSelf(0.1f);

        Debug.Log($"Winner XML:");
        winner.debugPrintXml();
        newFighter.gameObject.SetActive(false);
        winner.gameObject.SetActive(false);
        fightingPool.Remove(winner);
        fightingPool.Add(winner); // add to end like a queue
        fightingPool.Add(newFighter);

        yield return new WaitForSeconds(2f);
        Destroy(arena);
        StartCoroutine(startFight(location));
    }

    Fighter chooseFighter()
    {
        Fighter chosen = fightingPool[Random.Range(0, fightingPool.Count - 1)];
        fightingPool.Remove(chosen);
        chosen.gameObject.SetActive(true);
        chosen.resetForBattle();
        return chosen;
    }


}
