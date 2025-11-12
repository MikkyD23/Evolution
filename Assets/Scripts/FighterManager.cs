using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FighterManager : MonoBehaviour
{
    const int POOL_SIZE = 100;
    const float MUTATION_AMOUNT = 0.05f;
    const float TICK_TIME = 0.25f;
    const int BATTLE_TICKLENGTH = 100;
    const float ARENA_DISTANCE = 25f;
    const int COLUMN_COUNT = 10;


    // number of generations to go for before we check our generation vs generation from x ago
    const int GENERATION_COMPARISON_INTERVAL = 7;

    // use as a lock to keep track of when we all finish in case timing is inconsistent at high timescales
    int ongoingFights = 0; 

    List<Fighter> allFighters = new();
    [SerializeField] GameObject fighterPrefab;
    [SerializeField] GameObject arenaPrefab;
    [SerializeField] UiManager ui;

    int generationCount = 0;

    List<GenerationSnapshot> snapshots = new();

    class GenerationSnapshot
    {
        public List<string> nodeXmls = new();
        public int fromGeneration;

        public GenerationSnapshot(List<string> bestNetworkOutputs, int onGenerationNumber)
        {
            nodeXmls.AddRange(bestNetworkOutputs);
            fromGeneration = onGenerationNumber;
        }
    }

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
        iterateGeneration();
        List<Fighter> fightingPool = new();
        fightingPool.AddRange(allFighters);

        bool testAgainstPreviousGeneration = generationCount % GENERATION_COMPARISON_INTERVAL == 0;
        if (testAgainstPreviousGeneration && snapshots.Count > 0)
        {
            yield return StartCoroutine(ancestorFight(fightingPool));

        }
        else
        {
            yield return StartCoroutine(standardFightType(fightingPool));
        }

        // all fights fininshed now
        List<Fighter> placings = new();
        placings.AddRange(allFighters);
        placings.Sort();

        if(testAgainstPreviousGeneration)
        {
            recordGenerationSnapshot(placings);
        }

        const float UPPER_PLACING_THRESHOLD = 0.2f;
        int destroyCount = Mathf.FloorToInt(UPPER_PLACING_THRESHOLD * placings.Count);
        // destroy/reproduce on top/bottom percentiles
        print($"rewarding winner (score {placings[0].rewardScore()}). executing loser (score {placings[placings.Count - 1].rewardScore()})");
        for (int i = 0; i < destroyCount; i++)
        {
            Fighter winner = placings[i];
            int equivalentLoserPlacing = placings.Count - 1 - i;
            Fighter loser = placings[equivalentLoserPlacing];
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

    IEnumerator standardFightType(List<Fighter> fightingPool)
    {
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

        while (ongoingFights >= 1)
        {
            yield return new WaitForSeconds(2f);
        }
    }

    IEnumerator ancestorFight(List<Fighter> fightingPool)
    {
        print("Ancestor fight started");
        const int fightSize = POOL_SIZE / 2;
        List<Fighter> ancestorPool = unfreezeSnapshot(snapshots[snapshots.Count-1]);
        List<Fighter> originalAncestors = new();
        originalAncestors.AddRange(ancestorPool);
        for (int i = 0; i < fightSize; i++)
        {
            int onColumn = i % COLUMN_COUNT;
            int onRow = i / COLUMN_COUNT;
            StartCoroutine(
                startFight(new Vector2(onColumn * ARENA_DISTANCE, onRow * ARENA_DISTANCE),
                chooseFighter(fightingPool),
                chooseFighter(ancestorPool)
                )
            );
        }

        while (ongoingFights >= 1)
        {
            yield return new WaitForSeconds(2f);
        }

        for (int i = 0; i < fightSize; i++)
        {
            int onColumn = i % COLUMN_COUNT;
            int onRow = i / COLUMN_COUNT;
            StartCoroutine(
                startFight(new Vector2(onColumn * ARENA_DISTANCE, onRow * ARENA_DISTANCE),
                chooseFighter(fightingPool),
                chooseFighter(ancestorPool)
                )
            );
        }

        while (ongoingFights >= 1)
        {
            yield return new WaitForSeconds(2f);
        }

        print($"Team younglings: mean: {averageScore(allFighters)}. median: {medianScore(allFighters)}. Top Percentile: {topPercentileScore(allFighters)}." +
            $"Team boomers: mean: {averageScore(originalAncestors)}. median: {medianScore(originalAncestors)}. Top Percentile: {topPercentileScore(originalAncestors)}" +
            $"Youngling advantage: {averageScore(allFighters) - averageScore(originalAncestors)}. median: {medianScore(allFighters) - medianScore(originalAncestors)}. Top Percentile: {topPercentileScore(allFighters) - medianScore(originalAncestors)}");

        foreach (Fighter a in ancestorPool)
        {
            Destroy(a.gameObject);
        }
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

    void iterateGeneration()
    {
        generationCount++;
        ui.updateGenerationText(generationCount);
    }

    void recordGenerationSnapshot(List<Fighter> sortedPlacings)
    {
        int amountToGrab = sortedPlacings.Count;

        List<string> bestGenerationOutputs = new();
        for (int i = 0; i < amountToGrab; i++)
        {
            bestGenerationOutputs.Add(sortedPlacings[i].serializedNodes());
        }

        GenerationSnapshot newSnapshot = new GenerationSnapshot(bestGenerationOutputs, generationCount);
        snapshots.Add(newSnapshot);
    }

    List<Fighter> unfreezeSnapshot(GenerationSnapshot forSnapshot)
    {
        List<Fighter> acc = new();
        foreach (string x in forSnapshot.nodeXmls)
        {
            Fighter ancestor = Instantiate(fighterPrefab).GetComponent<Fighter>();
            new NodeManaging().applySerializedNetwork(x, ancestor);
            acc.Add(ancestor);
            ancestor.gameObject.SetActive(false);
        }
        return acc;
    }

    List<float> allScores(List<Fighter> fighters)
    {
        List<float> acc = new();
        foreach (Fighter f in fighters)
        {
            acc.Add(f.rewardScore());
        }
        return acc;
    }

    float averageScore(List<Fighter> fighters)
    {
        List<float> scores = allScores(fighters);
        float totalScore = 0;
        foreach (float s in scores)
        {
            totalScore += s;
        }
        return totalScore / (float)fighters.Count;
    }
    float medianScore(List<Fighter> fighters)
    {
        List<Fighter> orderedFighters = new();
        orderedFighters.AddRange(fighters);
        orderedFighters.Sort();

        List<float> scores = allScores(orderedFighters);
        return scores[orderedFighters.Count / 2];

    }
    float topPercentileScore(List<Fighter> fighters)
    {
        List<Fighter> orderedFighters = new();
        orderedFighters.AddRange(fighters);
        orderedFighters.Sort();
        List<Fighter> topFighters = new();
        for (int i = 0; i < fighters.Count / 2; i++)
        {
            topFighters.Add(orderedFighters[i]);
        }
        return medianScore(topFighters);
    }
}
