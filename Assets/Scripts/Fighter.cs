using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Fighter : MonoBehaviour, IComparable
{
    public static readonly int FIGHTER_LAYER = 3;

    const float BASE_HP = 100f;
    float currentHp = BASE_HP;

    const float BASE_ENERGY = 100f;
    float currentEnergy = 50f;
    const float ENERGY_REGEN_SECOND = 3f;

    const float LIGHT_ENERGY_COST = 5f;
    const float HEAVY_ENERGY_COST = 10f;
    const float LIGHT_RECHARGE = 0.5f;
    const float HEAVY_RECHARGE = LIGHT_RECHARGE * 3f;
    const float BLOCK_DMG_RESIST = 0.6f;
    float currentRechargeLeft = 0f;

    const float LIGHT_DAMAGE = 10f;
    const float RANGED_DAMAGE = LIGHT_DAMAGE * 0.6f;
    const float HEAVY_DAMAGE = LIGHT_DAMAGE * 2f;

    const float LOS_RANGE = 14f;

    const float TURN_SPEED = 7.5f;
    const float MOVE_SPEED = 40f;

    [SerializeField] GameObject bulletPrefab;

    BattleStats thisBattleStats = new();

    Dictionary<outputType, Node> outputNodes = new();

    List<bool> memories = new List<bool> { false, false, false };
    Rigidbody2D rigidBody;

    float ACTION_RECENCY_COUNTED = 1f;

    bool enemyDetectedThisTick = false;
    Fighter enemyThisFight;

    Dictionary<inputType, float> lastSpottedActions = new()
    {
        {inputType.hostileRecentlyShot, 0},
        {inputType.hostileRecentlyQuickMelee, 0},
        {inputType.hostileRecentlyHeavyTargetedMelee, 0},
        {inputType.hostileRecentlyWideMelee, 0},
        {inputType.hostileRecentlyBlock, 0},
        {inputType.hostileRecentlyGrab, 0},
        {inputType.hostileRecentlyWalk, 0},
        {inputType.hostileRecentlyRun, 0},
        {inputType.hostileRecentlyHurt, 0},
        {inputType.recentlyHurt, 0}
    };


    List<Node> orderedOutputNodes()
    {
        List<Node> ordered = new();
        foreach (outputType t in (outputType[])Enum.GetValues(typeof(outputType)))
        {
            ordered.Add(outputNodes[t]);
        }
        return ordered;
    }

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();

        resetForBattle();
    }

    public void mutateSelf(float magnitude)
    {
        List<Node> formattedOutputNodes = new();
        foreach (var n in outputNodes)
        {
            formattedOutputNodes.Add(n.Value);
        }

        new NodeManaging().mutateWholeNetwork(formattedOutputNodes, magnitude);
    }

    public void debugPrintXml()
    {
        print(new NodeManaging().serializeNetwork(outputNodes.Values.ToList()));

    }

    class BattleStats
    {
        public float damageDealt = 0;
        public float damageReceived = 0;
        public float distanceMoved = 0;

        /// <summary>
        /// Used to determine rank/how well we did to decide if we need to change/ get eliminated
        /// </summary>
        /// <returns></returns>
        public float rewardScore()
        {
            float accScore = 0;
            accScore += (damageDealt / BASE_HP) * 4f;
            accScore -= (damageReceived / BASE_HP);
            accScore += (distanceMoved / MOVE_SPEED) / 2f; // basically tiebreaker
            // TODO add in move variety score
            return accScore;
        }
    }

    public int CompareTo(object other)
    {
        Fighter otherFighter = other as Fighter;
        float myStats = rewardScore();
        float otherStats = otherFighter.rewardScore();
        float difference = otherStats - myStats;
        if(difference > 0)
        {
            return 1;
        }
        else if(difference < 0)
        {
            return -1;
        }
        return 0;
    }

    public float rewardScore()
    {
        return thisBattleStats.rewardScore();
    }


    public void makeEmptyNetworkForFighter(float resourcesUsed)
    {
        List<Node> newOutputNodes = NodeManaging.generateNetwork(new List<int> { 5, 5 }, this);
        loadExistingNetwork(newOutputNodes);
    }


    public enum inputType
    {
        alwaysOn, random,
        // hostile detected direction (relative to move direction)
        hostileDetectedForward, hostileDetectedLeft, hostileDetectedRight, hostileDetectedBack,
        // direction aim helpers
        hostileDetectedSlightlyLeft1, hostileDetectedSlightlyLeft2, hostileDetectedSlightlyLeft3,
        hostileDetectedSlightlyRight1, hostileDetectedSlightlyRight2, hostileDetectedSlightlyRight3,

        // distance away from target
        hostileDetectedClose, hostileDetectedMedium, hostileDetectedFar,

        // Enemy recent action (last 1 second)
        hostileRecentlyShot, hostileRecentlyQuickMelee, hostileRecentlyHeavyTargetedMelee, hostileRecentlyWideMelee,
        hostileRecentlyBlock, hostileRecentlyGrab, hostileRecentlyWalk, hostileRecentlyRun,
        hostileRecentlyHurt,

        // our movement speed/momentum
        mySpeedStationary, mySpeedSlow, mySpeedFast,

        // Our status
        readyToAttack, recentlyHurt, lowHealth,
        has1ThirdEnergy, has2ThirdEnergy, has3ThirdEnergy, // or more

        // callaback from last time
        memory1, memory2, memory3
    }

    public enum outputType
    {
        shootRanged, quickMelee, heavyTargetedMelee, wideMelee, block, grab,
        walk, run,

        // move direction (can combine directions) (relative to look)
        moveForward, moveLeft, moveRight, moveBack,

        // look/attack direction
        rotateLookSoft, rotateLookHard, lookRight,

        // callback output that goes directly back to input (memory)
        memory1, memory2, memory3

    }

    public void pollForOutput(float secondsPassed = 0.25f)
    {
        setEnemyDetection();
        decrementPerceivedActions(secondsPassed);

        // expecting this to be called every quarter second
        currentEnergy = Mathf.Min(currentEnergy + (ENERGY_REGEN_SECOND * secondsPassed), BASE_ENERGY);

        // positive goes left
        float directionLook = 0f;
        directionLook += isOutputting(outputType.rotateLookSoft) ? 1f : 0;
        directionLook += isOutputting(outputType.rotateLookHard) ? 1.5f : 0;
        directionLook *= isOutputting(outputType.lookRight) ? -1f : 1f;

        directionLook *= secondsPassed * TURN_SPEED;
        rigidBody.AddTorque(directionLook);

        Vector2 directionMove = Vector2.zero;
        directionMove += isOutputting(outputType.moveForward) ? transform.up : Vector2.zero;
        directionMove += isOutputting(outputType.moveRight) ? transform.right : Vector2.zero;
        directionMove += isOutputting(outputType.moveLeft) ? -transform.right : Vector2.zero;
        directionMove += isOutputting(outputType.moveBack) ? -transform.up : Vector2.zero;

        bool isRunning = isOutputting(outputType.run);

        directionMove *= isRunning ? 2f : isOutputting(outputType.walk) ? 1f : 0f;
        rigidBody.AddForce(directionMove * secondsPassed * MOVE_SPEED);

        currentRechargeLeft -= secondsPassed;
        if (!isRunning)
        {
            if (canUseAttack(LIGHT_RECHARGE, LIGHT_ENERGY_COST) && isOutputting(outputType.shootRanged))
            {
                rangedAttack();
                usedAttackResources(LIGHT_RECHARGE, LIGHT_ENERGY_COST);
            }
            else if (canUseAttack(LIGHT_RECHARGE, LIGHT_ENERGY_COST) && isOutputting(outputType.quickMelee))
            {
                quickMeleeAttack();
                usedAttackResources(LIGHT_RECHARGE, LIGHT_ENERGY_COST);
            }
        }
        // allow it to go slightly negative this frame so more consistent with long tick lengths
        currentRechargeLeft = Mathf.Max(currentRechargeLeft, 0f); 

        memories[0] = isOutputting(outputType.memory1);
        memories[1] = isOutputting(outputType.memory2);
        memories[2] = isOutputting(outputType.memory3);

        thisBattleStats.distanceMoved += (rigidBody.linearVelocity.magnitude * secondsPassed);
    }

    public bool checkInput(inputType forType)
    {
        switch (forType)
        {
            case inputType.alwaysOn:
                return true;
            case inputType.random:
                return UnityEngine.Random.value > 0.5f;
            case inputType.hostileDetectedForward:
                return checkLos(transform.up);
            case inputType.hostileDetectedLeft:
                return checkLos(-transform.right);
            case inputType.hostileDetectedRight:
                return checkLos(transform.right);
            case inputType.hostileDetectedBack:
                return checkLos(-transform.up);
            case inputType.hostileDetectedSlightlyLeft1:
                return checkLos(Vector2.Lerp(transform.up, -transform.right, 0.2f).normalized);
            case inputType.hostileDetectedSlightlyLeft2:
                return checkLos(Vector2.Lerp(transform.up, -transform.right, 0.4f).normalized);
            case inputType.hostileDetectedSlightlyLeft3:
                return checkLos(Vector2.Lerp(transform.up, -transform.right, 0.6f).normalized);
            case inputType.hostileDetectedSlightlyRight1:
                return checkLos(Vector2.Lerp(transform.up, transform.right, 0.2f).normalized);
            case inputType.hostileDetectedSlightlyRight2:
                return checkLos(Vector2.Lerp(transform.up, transform.right, 0.4f).normalized);
            case inputType.hostileDetectedSlightlyRight3:
                return checkLos(Vector2.Lerp(transform.up, transform.right, 0.6f).normalized);
            case inputType.hostileDetectedClose:
                return enemyDistance() < 1f;
            case inputType.hostileDetectedMedium:
                return enemyDistance() < LOS_RANGE * 0.5f;
            case inputType.hostileDetectedFar:
                return enemyDistance() < LOS_RANGE;
            case inputType.hostileRecentlyShot:
            case inputType.hostileRecentlyQuickMelee:
            case inputType.hostileRecentlyHeavyTargetedMelee:
            case inputType.hostileRecentlyWideMelee:
            case inputType.hostileRecentlyBlock:
            case inputType.hostileRecentlyGrab:
            case inputType.hostileRecentlyWalk:
            case inputType.hostileRecentlyRun:
            case inputType.hostileRecentlyHurt:
            case inputType.recentlyHurt:
                return lastSpottedActions[forType] >= 0;
            case inputType.mySpeedStationary:
                return rigidBody.linearVelocity.magnitude <= 0.1f;
            case inputType.mySpeedSlow:
                return rigidBody.linearVelocity.magnitude >= 0.1f;
            case inputType.mySpeedFast:
                return rigidBody.linearVelocity.magnitude >= 1f;
            case inputType.has1ThirdEnergy:
                return currentEnergy >= BASE_ENERGY / 3f;
            case inputType.has2ThirdEnergy:
                return currentEnergy >= (BASE_ENERGY / 3f) * 2;
            case inputType.has3ThirdEnergy:
                return currentEnergy >= BASE_ENERGY - 1f;
            case inputType.readyToAttack:
                return currentRechargeLeft <= 0f;
            case inputType.lowHealth:
                return currentHp <= (BASE_HP * 0.5f);
            case inputType.memory1:
                return memories[0];
            case inputType.memory2:
                return memories[1];
            case inputType.memory3:
                return memories[2];
            default:
                print($"Missing input: {forType}!!");
                throw new Exception($"Missing input: {forType}!!");
        }
    }

    void rangedAttack()
    {
        GameObject newBullet = Instantiate(bulletPrefab);
        newBullet.GetComponent<Projectile>().initialise(transform.up, this, RANGED_DAMAGE);
        newBullet.transform.position = transform.position;

        // alert to enemy we made this action
        enemyThisFight.perceivedEnemyAction(inputType.hostileRecentlyShot);
    }

    void quickMeleeAttack()
    {

    }

    bool canUseAttack(float rechargeTime, float energyCost)
    {
        return currentRechargeLeft <= 0f && currentEnergy >= energyCost;
    }
    void usedAttackResources(float rechargeTime, float energyCost)
    {
        currentRechargeLeft += rechargeTime;
        currentEnergy -= energyCost;
    }

    public void reportDealtDamage(float damage)
    {
        thisBattleStats.damageDealt += damage;
        perceivedEnemyAction(inputType.hostileRecentlyHurt);
    }

    public void takeDamage(float damage)
    {
        currentHp -= damage;
        thisBattleStats.damageReceived += damage;
        perceivedEnemyAction(inputType.recentlyHurt);
    }

    bool checkLos(Vector2 direction)
    {
        if(!enemyDetectedThisTick)
        {
            return false;
        }
        const float VECTOR_THRESHOLD = 0.3f;
        Vector2 enemyDirection = (enemyThisFight.transform.position - transform.position).normalized;
        bool noticedTarget = Vector2.Distance(direction, enemyDirection) <= VECTOR_THRESHOLD;

        //Debug.DrawRay(transform.position, direction * LOS_RANGE, noticedTarget ? Color.red : Color.gray, 0.1f);

        return noticedTarget;
    }

    bool isOutputting(outputType forOutput)
    {
        bool outputting = outputNodes[forOutput].isOutputting();
        //print($"output type: {forOutput} is outputting: {outputting}");
        return outputting;
    }

    void setEnemyDetection()
    {
        if(enemyThisFight == null)
        {
            findEnemyForThisFight();
        }

        RaycastHit2D hit = Physics2D.Linecast(transform.position, enemyThisFight.transform.position);
        enemyDetectedThisTick = hit && hit.transform.gameObject.layer == FIGHTER_LAYER;
        //print($"detected enemy location: {enemyThisFight.transform.position}. my position = {transform.position}. detected enemy: {enemyDetectedThisTick}");
    }
    void findEnemyForThisFight()
    {
        Fighter[] allFighters = FindObjectsByType<Fighter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Fighter closestEnemy = null;
        Vector2 currentLocation = transform.position;
        foreach (Fighter f in allFighters)
        {
            if(f == this)
            {
                continue;
            }
            if(closestEnemy == null ||
                Vector2.Distance(currentLocation, f.transform.position) <=
                Vector2.Distance(currentLocation, closestEnemy.transform.position))
            {
                closestEnemy = f;
            }
        }
        enemyThisFight = closestEnemy;
    }

    float enemyDistance()
    {
        return Vector2.Distance(transform.position, enemyThisFight.transform.position);
    }

    void perceivedEnemyAction(inputType actionType)
    {
        // some sort of moveset data structure, or keep it simple and static?
        lastSpottedActions[actionType] = ACTION_RECENCY_COUNTED;
    }

    void decrementPerceivedActions(float secondsPassed)
    {
        lastSpottedActions[inputType.hostileRecentlyShot] -= secondsPassed;
        lastSpottedActions[inputType.hostileRecentlyQuickMelee] -= secondsPassed;
        lastSpottedActions[inputType.hostileRecentlyHeavyTargetedMelee] -= secondsPassed;
        lastSpottedActions[inputType.hostileRecentlyWideMelee] -= secondsPassed;
        lastSpottedActions[inputType.hostileRecentlyBlock] -= secondsPassed;
        lastSpottedActions[inputType.hostileRecentlyGrab] -= secondsPassed;
        lastSpottedActions[inputType.hostileRecentlyWalk] -= secondsPassed;
        lastSpottedActions[inputType.hostileRecentlyRun] -= secondsPassed;
        lastSpottedActions[inputType.hostileRecentlyHurt] -= secondsPassed;
        lastSpottedActions[inputType.recentlyHurt] -= secondsPassed;
        // can't do for loop because get error from modifying it
        //foreach (var item in lastSpottedActions)
        //{
        //    lastSpottedActions[item.Key] -= secondsPassed;
        //}

    }

    public void loadExistingNetwork(List<Node> newOutputNodes)
    {
        outputNodes = new();
        outputType[] outputs = (outputType[])Enum.GetValues(typeof(outputType));

        for (int i = 0; i < outputs.Length; i++)
        {
            //print($"loading output layer node with layer weights:");
            //foreach (var item in newOutputNodes[i].previousLayerWeights())
            //{
            //    print(item);
            //}
            outputNodes.Add(outputs[i], newOutputNodes[i]);
        }
    }

    public void resetForBattle()
    {
        thisBattleStats = new();
        currentHp = BASE_HP;
        currentEnergy = BASE_ENERGY;
        currentRechargeLeft = 0f;

        enemyThisFight = null;
        transform.rotation = Quaternion.identity;
    }

    public Fighter reproduce()
    {
        Fighter newFighter = Instantiate(this.gameObject).GetComponent<Fighter>();
        new NodeManaging().deepCloneNetwork(orderedOutputNodes(), newFighter);

        return newFighter;
    }
}
