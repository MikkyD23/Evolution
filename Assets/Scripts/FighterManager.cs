using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FighterManager : MonoBehaviour
{
    const int POOL_SIZE = 80;
    const float MUTATION_AMOUNT = 0.05f;
    const float TICK_TIME = 0.25f;
    const int BATTLE_TICKLENGTH = 100;

    // use as a lock to keep track of when we all finish in case timing is inconsistent at high timescales
    int ongoingFights = 0; 

    List<Fighter> allFighters = new();
    [SerializeField] GameObject fighterPrefab;
    [SerializeField] GameObject arenaPrefab;
    [SerializeField] UiManager ui;

    int generationCount = 0;

    private void Awake()
    {
        for (int i = 0; i < POOL_SIZE; i++)
        {
            Fighter newFighter = Instantiate(fighterPrefab).GetComponent<Fighter>();
            newFighter.makeEmptyNetworkForFighter(0);
            newFighter.mutateSelf(MUTATION_AMOUNT * 10);
            newFighter.gameObject.SetActive(false);
            allFighters.Add(newFighter);
        }

        StartCoroutine(fightLoop());
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

    IEnumerator fightLoop()
    {
        List<Fighter> fightingPool = new();
        fightingPool.AddRange(allFighters);
        const float ARENA_DISTANCE = 25f;

        const int COLUMN_COUNT = 10;
        for (int i = 0; fightingPool.Count > 1; i++)
        {
            int onColumn = i % COLUMN_COUNT;
            int onRow = i / COLUMN_COUNT;
            StartCoroutine(
                startFight(new Vector2(onColumn * ARENA_DISTANCE, onRow * ARENA_DISTANCE),
                chooseFighter(fightingPool),
                chooseFighter(fightingPool)
                )
            );
        }

        while(ongoingFights >= 1)
        {
            yield return new WaitForSeconds(2f);
        }

        // all fights fininshed now
        List<Fighter> placings = new();
        placings.AddRange(allFighters);
        placings.Sort();

        const float UPPER_PLACING_THRESHOLD = 0.3f;
        int destroyCount = Mathf.FloorToInt(UPPER_PLACING_THRESHOLD * placings.Count);
        // destroy/reproduce on top/bottom percentiles
        for (int i = 0; i < destroyCount; i++)
        {
            Fighter winner = placings[i];
            int equivalentLoserPlacing = placings.Count - 1 - i;
            Fighter loser = placings[equivalentLoserPlacing];
            print($"rewarding winner at place {i} (score {winner.rewardScore()}). executing loser at place {equivalentLoserPlacing} (score {loser.rewardScore()})");
            reproduceWinner(winner);
            destroyLoser(loser);
        }

        // mutate the middle class
        for (int i = destroyCount - 1; i < placings.Count - destroyCount; i++)
        {
            placings[i].mutateSelf(MUTATION_AMOUNT);
        }

        StartCoroutine(fightLoop());
    }

    IEnumerator startFight(Vector2 location, Fighter fighter1, Fighter fighter2)
    {
        ongoingFights++;
        GameObject arena = Instantiate(arenaPrefab);
        arena.transform.position = location;

        fighter1.transform.position = location + new Vector2(-2.5f, 0);
        fighter2.transform.position = location + new Vector2(2.5f, 0);

        for (int i = 0; i < BATTLE_TICKLENGTH; i++)
        {
            yield return new WaitForSeconds(TICK_TIME * 0.5f);
            fighter1.pollForOutput(TICK_TIME);
            yield return new WaitForSeconds(TICK_TIME * 0.5f);
            fighter2.pollForOutput(TICK_TIME);

        }

        fighter1.gameObject.SetActive(false);
        fighter2.gameObject.SetActive(false);
        Destroy(arena);
        ongoingFights--;
    }

    Fighter chooseFighter(List<Fighter> fromPool)
    {
        Fighter chosen = fromPool[Random.Range(0, fromPool.Count - 1)];
        fromPool.Remove(chosen);
        chosen.gameObject.SetActive(true);
        chosen.resetForBattle();
        return chosen;
    }

    void reproduceWinner(Fighter winner)
    {
        Fighter newFighter = winner.reproduce();
        newFighter.mutateSelf(MUTATION_AMOUNT);
        newFighter.gameObject.SetActive(false);
        allFighters.Add(newFighter);
    }

    void destroyLoser(Fighter loser)
    {
        allFighters.Remove(loser);
        Destroy(loser.gameObject);
    }
}
