using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Fighter : MonoBehaviour
{
    const float BASE_HP = 100f;
    float currentHp = BASE_HP;

    const float BASE_ENERGY = 100f;
    float currentEnergy = 100f;
    const float ENERGY_REGEN_SECOND = 7f;

    const float LIGHT_ENERGY_COST = 5f;
    const float HEAVY_ENERGY_COST = 10f;
    const float LIGHT_RECHARGE = 0.3f;
    const float HEAVY_RECHARGE = 1f;
    const float BLOCK_DMG_RESIST = 0.6f;
    float currentRechargeLeft = 0f;

    const float LIGHT_DAMAGE = 10f;
    const float RANGED_DAMAGE = LIGHT_DAMAGE * 0.6f;
    const float HEAVY_DAMAGE = LIGHT_DAMAGE * 2f;

    const float LOS_RANGE = 14f;


    [SerializeField] GameObject bulletPrefab;

    BattleStats thisBattleStats = new();

    Dictionary<outputType, Node> outputNodes = new();

    List<bool> memories = new List<bool> { false, false, false };
    Rigidbody2D rigidBody;

    float ACTION_RECENCY_COUNTED = 1f;
    Dictionary<inputType, float> lastSpottedActions = new()
    {
        { inputType.hostileRecentlyShot, 0}
    };

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();

        makeNetworkForFighter(0);


        resetForBattle();
    }

    public void mutateSelf(float magnitude)
    {
        List<Node> formattedOutputNodes = new();
        foreach (var n in outputNodes)
        {
            formattedOutputNodes.Add(n.Value);
        }

        new NodeManaging().mutateWholeNetwork(formattedOutputNodes, 0.3f);
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
        public bool won = false;

        /// <summary>
        /// Used to determine rank/how well we did to decide if we need to change/ get eliminated
        /// </summary>
        /// <returns></returns>
        public float rewardScore()
        {
            float accScore = 0;
            accScore += won ? 100f : 0;
            accScore += damageDealt / BASE_HP;
            accScore -= (damageReceived / BASE_HP) * 0.5f;
            accScore += distanceMoved * 0.05f;
            return accScore;
        }
    }

    public float rewardScore()
    {
        return thisBattleStats.rewardScore();
    }


    void makeNetworkForFighter(float resourcesUsed)
    {
        List<Node> newOutputNodes = NodeManaging.generateNetwork(new List<int> { 5, 5 }, this);
        loadExistingNetwork(newOutputNodes);
    }


    public enum inputType
    {
        random1, random2, random3, random4, random5,
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
        lookLeft, lookHardLeft, lookRight, lookHardRight,

        // callback output that goes directly back to input (memory)
        memory1, memory2, memory3

    }

    public void pollForOutput(float secondsPassed = 0.25f)
    {
        float movePower = secondsPassed * 10f;
        // expecting this to be called every quarter second
        currentEnergy = Mathf.Max(currentEnergy + (ENERGY_REGEN_SECOND * movePower), BASE_ENERGY);

        // positive goes left
        float directionLook = 0f;
        directionLook += isOutputting(outputType.lookLeft) ? 1f : 0;
        directionLook += isOutputting(outputType.lookHardLeft) ? 1.5f : 0;
        directionLook -= isOutputting(outputType.lookRight) ? 1f : 0;
        directionLook -= isOutputting(outputType.lookHardRight) ? 1.5f : 0;

        directionLook *= movePower;
        rigidBody.AddTorque(directionLook);

        Vector2 directionMove = Vector2.zero;
        directionMove += isOutputting(outputType.moveForward) ? transform.up : Vector2.zero;
        directionMove += isOutputting(outputType.moveRight) ? transform.right : Vector2.zero;
        directionMove += isOutputting(outputType.moveLeft) ? -transform.right : Vector2.zero;
        directionMove += isOutputting(outputType.moveBack) ? -transform.up : Vector2.zero;

        directionMove *= isOutputting(outputType.run) ? 2f : isOutputting(outputType.walk) ? 1f : 0f;
        rigidBody.AddForce(directionMove);

        currentRechargeLeft -= secondsPassed;
        if (currentRechargeLeft <= 0 && isOutputting(outputType.shootRanged))
        {
            currentRechargeLeft += LIGHT_RECHARGE;
            rangedAttack();
        }
        // allow it to go slightly negative this frame so more consistent with long tick lengths
        currentRechargeLeft = Mathf.Max(currentRechargeLeft, 0f); 

        memories[0] = isOutputting(outputType.memory1);
        memories[1] = isOutputting(outputType.memory2);
        memories[2] = isOutputting(outputType.memory3);

        thisBattleStats.distanceMoved += (rigidBody.linearVelocity.magnitude * movePower);
    }

    public bool checkInput(inputType forType)
    {
        switch (forType)
        {
            case inputType.random1:
                return UnityEngine.Random.value > 0.5f;
            case inputType.random2:
                return UnityEngine.Random.value > 0.5f;
            case inputType.random3:
                return UnityEngine.Random.value > 0.5f;
            case inputType.random4:
                return UnityEngine.Random.value > 0.5f;
            case inputType.random5:
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
                return checkLos(Vector2.Lerp(transform.up, -transform.right, 0.2f));
            case inputType.hostileDetectedSlightlyLeft2:
                return checkLos(Vector2.Lerp(transform.up, -transform.right, 0.4f));
            case inputType.hostileDetectedSlightlyLeft3:
                return checkLos(Vector2.Lerp(transform.up, -transform.right, 0.6f));
            case inputType.hostileDetectedSlightlyRight1:
                return checkLos(Vector2.Lerp(transform.up, transform.right, 0.2f));
            case inputType.hostileDetectedSlightlyRight2:
                return checkLos(Vector2.Lerp(transform.up, transform.right, 0.4f));
            case inputType.hostileDetectedSlightlyRight3:
                return checkLos(Vector2.Lerp(transform.up, transform.right, 0.4f));
            case inputType.hostileDetectedClose:
                break;
            case inputType.hostileDetectedMedium:
                break;
            case inputType.hostileDetectedFar:
                break;
            case inputType.hostileRecentlyShot:
            case inputType.hostileRecentlyQuickMelee:
            case inputType.hostileRecentlyHeavyTargetedMelee:
            case inputType.hostileRecentlyWideMelee:
            case inputType.hostileRecentlyBlock:
            case inputType.hostileRecentlyGrab:
            case inputType.hostileRecentlyWalk:
            case inputType.hostileRecentlyRun:
            case inputType.hostileRecentlyHurt:
                return false; // TODO
                //return lastSpottedActions[forType] >= 0;
            case inputType.mySpeedStationary:
                return rigidBody.linearVelocity.magnitude <= 0.1f;
            case inputType.mySpeedSlow:
                return rigidBody.linearVelocity.magnitude >= 0.1f;
            case inputType.mySpeedFast:
                return rigidBody.linearVelocity.magnitude >= 1f;
            case inputType.readyToAttack:
                break;
            case inputType.recentlyHurt:
                break;
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

        return false;
    }

    void rangedAttack()
    {
        GameObject newBullet = Instantiate(bulletPrefab);
        newBullet.GetComponent<Projectile>().initialise(transform.up, this, RANGED_DAMAGE);
        newBullet.transform.position = transform.position;
    }

    public void reportDealtDamage(float damage)
    {
        thisBattleStats.damageDealt += damage;
    }

    public void takeDamage(float damage)
    {
        currentHp -= damage;
        thisBattleStats.damageReceived += damage;
    }

    bool checkLos(Vector2 direction)
    {

        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, LOS_RANGE);
        bool noticedTarget = hit && hit.transform.gameObject.GetComponent<Fighter>() != null;
        // make sure not detecting self

        //Debug.DrawRay(transform.position, direction * LOS_RANGE, noticedTarget ? Color.red : Color.gray, 0.1f);

        return noticedTarget;

    }

    //RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, direction, LOS_RANGE);
    //bool noticedTarget = false;
    //foreach (RaycastHit2D hit in hits)
    //{
    //    Fighter foundFighter = hit.transform.gameObject.GetComponent<Fighter>();
    //    // make sure not detecting self
    //    if (foundFighter != null && foundFighter != this)
    //    {
    //        noticedTarget = true;
    //    }
    //}


    bool checkWithinRange()
    {
        return false;
    }

    bool isOutputting(outputType forOutput)
    {
        bool outputting = outputNodes[forOutput].isOutputting();
        //print($"output type: {forOutput} is outputting: {outputting}");
        return outputting;
    }

    Vector2 hostileDetectedLocation()
    {
        return Vector2.zero;
    }

    void perceivedEnemyAction(inputType actionType)
    {
        // some sort of moveset data structure, or keep it simple and static?
        lastSpottedActions[actionType] = ACTION_RECENCY_COUNTED;
    }

    void loadExistingNetwork(List<Node> newOutputNodes)
    {
        outputType[] outputs = (outputType[])Enum.GetValues(typeof(outputType));

        for (int i = 0; i < outputs.Length; i++)
        {
            outputNodes.Add(outputs[i], newOutputNodes[i]);
        }
    }

    public void resetForBattle()
    {
        thisBattleStats = new();
        currentHp = BASE_HP;
        currentEnergy = BASE_ENERGY;
        currentRechargeLeft = 0f;

        transform.rotation = Quaternion.identity;
    }

    public Fighter reproduce()
    {
        Fighter newFighter = Instantiate(this.gameObject).GetComponent<Fighter>();
        new NodeManaging().deepCloneNetwork(outputNodes.Values.ToList(), newFighter);

        return newFighter;
    }

    // inputs
    // hostile detected direction (relative to move direction)
    // forward, left, right, back
    // close, medium, far (distance away from target)
    // enemy recently shot, quickMelee, heavyTargetedMelee, wideMelee, block, grab, walk, run
    // our movement speed/momentum: stationary, slow, fast
    // readyToAttack, recentlyHurt, lowHealth

    // outputs
    // shootRanged
    // quickMelee 
    // heavyTargetedMelee (slow) (best damage)
    // wideMelee (slow) (good if enemy maybe moves a little, or we don't want to use the intelligence to aim/predict)
    // block
    // grab
    // walk (can attack)
    // run (cannot attack)

    // move direction (can combine directions)
    // forward, left, right, back

    // attack direction (relative to move direction)
    // forward, left, right, back

    // callback output that goes directly back to input (memory)

}
