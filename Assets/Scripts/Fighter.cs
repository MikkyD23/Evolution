using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fighter : MonoBehaviour
{
    const float BASE_HP = 100f;
    float currentHp = BASE_HP;

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

        List<Node> formattedOutputNodes = new();
        foreach (var n in outputNodes)
        {
            formattedOutputNodes.Add(n.Value);
        }

        new NodeManaging().mutateWholeNetwork(formattedOutputNodes, 0.3f);

        print(new NodeManaging().serializeNetwork(outputNodes));
    }

    class BattleStats
    {
        float damageDealt = 0;
        float damageReceived = 0;
        float distanceMoved = 0;
        bool won = false;

        /// <summary>
        /// Used to determine rank/how well we did to decide if we need to change/ get eliminated
        /// </summary>
        /// <returns></returns>
        float rewardScore()
        {
            float accScore = 0;
            accScore += won ? 100f : 0;
            accScore += damageDealt / BASE_HP;
            accScore -= (damageReceived / BASE_HP) * 0.5f;
            accScore += distanceMoved * 0.05f;
            return accScore;
        }
    }


    void makeNetworkForFighter(float resourcesUsed)
    {
        List<Node> newOutputNodes = NodeManaging.generateNetwork(new List<int> { 5, 5 }, this);
        loadExistingNetwork(newOutputNodes);
    }


    public enum inputType
    {
        // hostile detected direction (relative to move direction)
        hostileDetectedForward, hostileDetectedLeft, hostileDetectedRight, hostileDetectedBack,
        // direction aim helpers
        hostileDetectedSlightlyLeft, hostileDetectedSlightlyRight, hostileDetectedStraightOn,

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
        shootRanged
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

    public bool checkInput(inputType forType)
    {
        return false;
        switch (forType)
        {
            case inputType.hostileDetectedForward:
                break;
            case inputType.hostileDetectedLeft:
                break;
            case inputType.hostileDetectedRight:
                break;
            case inputType.hostileDetectedBack:
                break;
            case inputType.hostileDetectedSlightlyLeft:
                break;
            case inputType.hostileDetectedSlightlyRight:
                break;
            case inputType.hostileDetectedStraightOn:
                break;
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
                return lastSpottedActions[forType] >= 0;
            case inputType.mySpeedStationary:
                return rigidBody.velocity.magnitude <= 0.1f;
            case inputType.mySpeedSlow:
                return rigidBody.velocity.magnitude >= 0.1f;
            case inputType.mySpeedFast:
                return rigidBody.velocity.magnitude >= 1f;
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
