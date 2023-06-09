using System.Collections.Generic;
using Redirection;
using UnityEngine;


public class VirtualPathGenerator
{
    public enum AlternationType
    {
        None,
        Random,
        Constant
    }


    public enum DistributionType
    {
        Normal,
        Uniform
    }


    public static int RandomSeed = 3041;


    private static float SampleUniform(float min, float max)
    {
        //return a + Random.value * (b - a);
        return Random.Range(min, max);
    }


    private static float SampleNormal(float mu = 0, float sigma = 1, float min = float.MinValue, float max = float.MaxValue)
    {
        // From: http://stackoverflow.com/questions/218060/random-gaussian-variables
        var r1 = Random.value;
        var r2 = Random.value;
        var randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(r1)) * Mathf.Sin(2.0f * Mathf.PI * r2); // Random Normal(0, 1)
        var randNormal = mu + randStdNormal * sigma;

        return Mathf.Max(Mathf.Min(randNormal, max), min);
    }


    private static float SampleDistribution(SamplingDistribution distribution)
    {
        float retVal = 0;

        if (distribution.DistributionType == DistributionType.Uniform)
        {
            retVal = SampleUniform(distribution.Min, distribution.Max);
        }
        else if (distribution.DistributionType == DistributionType.Normal)
        {
            retVal = SampleNormal(distribution.Mu, distribution.Sigma, distribution.Min, distribution.Max);
        }

        if (distribution.AlternationType == AlternationType.Random && Random.value < 0.5f)
        {
            retVal = -retVal;
        }

        return retVal;
    }


    // The angular sampling distribution must be 
    public static List<Vector2> GeneratePath(PathSeed pathSeed, Vector2 initialPosition, Vector2 initialForward, out float sumOfDistances, out float sumOfRotations)
    {
        // THE GENERATION RULE IS WALK THEN TURN! SO THE LAST TURN IS TECHNICALLY REDUNDANT!
        // I'M DOING THIS TO MAKE SURE WE WALK STRAIGHT ALONG THE INITIAL POSITION FIRST BEFORE WE EVER TURN
        var waypoints = new List<Vector2>(pathSeed.WaypointCount);
        var position = initialPosition;
        var forward = initialForward.normalized;
        Vector2 nextPosition, nextForward;
        float sampledDistance, sampledRotation;
        sumOfDistances = 0;
        sumOfRotations = 0;
        var alternator = 1;

        for (var i = 0; i < pathSeed.WaypointCount; i++)
        {
            sampledDistance = SampleDistribution(pathSeed.DistanceDistribution);
            sampledRotation = SampleDistribution(pathSeed.AngleDistribution);

            if (pathSeed.AngleDistribution.AlternationType == AlternationType.Constant)
            {
                sampledRotation *= alternator;
            }

            nextPosition = position + sampledDistance * forward;
            nextForward = Utilities.RotateVector(forward, sampledRotation).normalized; // Normalizing for extra protection in case error accumulates over time
            waypoints.Add(nextPosition);
            position = nextPosition;
            forward = nextForward;
            sumOfDistances += sampledDistance;
            sumOfRotations += Mathf.Abs(sampledRotation); // The last one might seem redundant to add
            alternator *= -1;
        }

        return waypoints;
    }


    public static Vector2 GetRandomPositionWithinBounds(float minX, float maxX, float minZ, float maxZ)
    {
        return new Vector2(SampleUniform(minX, maxX), SampleUniform(minZ, maxZ));
    }


    public static Vector2 GetRandomForward()
    {
        var angle = SampleUniform(0, 360);

        return Utilities.RotateVector(Vector2.up, angle).normalized; // Over-protective with the normalizing
    }


    public struct SamplingDistribution
    {
        public DistributionType DistributionType;
        public float Min, Max;
        public float Mu, Sigma;
        public AlternationType AlternationType; // Used typicaly for the case of generating angles, where we want the value to be negated at random


        public SamplingDistribution(DistributionType distributionType, float min, float max, AlternationType alternationType = AlternationType.None, float mu = 0, float sigma = 0)
        {
            DistributionType = distributionType;
            Min = min;
            Max = max;
            Mu = mu;
            Sigma = sigma;
            AlternationType = alternationType;
        }
    }


    public struct PathSeed
    {
        public int WaypointCount;
        public SamplingDistribution DistanceDistribution;
        public SamplingDistribution AngleDistribution;


        public PathSeed(SamplingDistribution distanceDistribution, SamplingDistribution angleDistribution, int waypointCount)
        {
            DistanceDistribution = distanceDistribution;
            AngleDistribution = angleDistribution;
            WaypointCount = waypointCount;
        }
    }
}