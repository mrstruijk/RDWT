using Redirection;
using UnityEngine;


public abstract class SteerToRedirector : Redirector
{
    private readonly bool _dontUseDampening = false;
    private float _lastRotationApplied = 0f;

    // Auxiliary Parameters
    private float _rotationFromCurvatureGain; //Proposed curvature gain based on user speed
    private float _rotationFromRotationGain; //Proposed rotation gain based on head's yaw

    // Testing Parameters
    private readonly bool _useBearingThresholdBasedRotationDampeningTimofey = true;

    // Reference Parameters
    protected Transform CurrentTarget; //Where the participant  is currently directed?

    // State Parameters
    protected bool NoTmpTarget = true;
    protected GameObject TMPTarget;

    // User Experience Improvement Parameters
    private const float MovementThreshold = 0.2f; // meters per second
    private const float RotationThreshold = 1.5f; // degrees per second
    private const float CurvatureGainCapDegreesPerSecond = 15; // degrees per second
    private const float RotationGainCapDegreesPerSecond = 30; // degrees per second
    private const float DistanceThresholdForDampening = 1.25f; // Distance threshold to apply dampening (meters)
    private const float BearingThresholdForDampening = 45f; // TIMOFEY: 45.0f; // Bearing threshold to apply dampening (degrees) MAHDI: WHERE DID THIS VALUE COME FROM?
    private const float SmoothingFactor = 0.125f; // Smoothing factor for redirection rotations


    public abstract void PickRedirectionTarget();


    public override void ApplyRedirection()
    {
        PickRedirectionTarget();

        // Get Required Data
        var deltaPos = RedirectionManager.deltaPos;
        var deltaDir = RedirectionManager.deltaDir;

        _rotationFromCurvatureGain = 0;

        if (deltaPos.magnitude / RedirectionManager.GetDeltaTime() > MovementThreshold) //User is moving
        {
            _rotationFromCurvatureGain = Mathf.Rad2Deg * (deltaPos.magnitude / RedirectionManager.CurvatureRadius);
            _rotationFromCurvatureGain = Mathf.Min(_rotationFromCurvatureGain, CurvatureGainCapDegreesPerSecond * RedirectionManager.GetDeltaTime());
        }

        //Compute desired facing vector for redirection
        var desiredFacingDirection = Utilities.FlattenedPos3D(CurrentTarget.position) - RedirectionManager.currPos;
        var desiredSteeringDirection = -1 * (int) Mathf.Sign(Utilities.GetSignedAngle(RedirectionManager.currDir, desiredFacingDirection)); // We have to steer to the opposite direction so when the user counters this steering, she steers in right direction

        //Compute proposed rotation gain
        _rotationFromRotationGain = 0;

        if (Mathf.Abs(deltaDir) / RedirectionManager.GetDeltaTime() >= RotationThreshold) //if User is rotating
        {
            //Determine if we need to rotate with or against the user
            if (deltaDir * desiredSteeringDirection < 0)
            {
                //Rotating against the user
                _rotationFromRotationGain = Mathf.Min(Mathf.Abs(deltaDir * RedirectionManager.MinRotGain), RotationGainCapDegreesPerSecond * RedirectionManager.GetDeltaTime());
            }
            else
            {
                //Rotating with the user
                _rotationFromRotationGain = Mathf.Min(Mathf.Abs(deltaDir * RedirectionManager.MaxRotGain), RotationGainCapDegreesPerSecond * RedirectionManager.GetDeltaTime());
            }
        }

        var rotationProposed = desiredSteeringDirection * Mathf.Max(_rotationFromRotationGain, _rotationFromCurvatureGain);
        var curvatureGainUsed = _rotationFromCurvatureGain > _rotationFromRotationGain;


        // Prevent having gains if user is stationary
        if (Mathf.Approximately(rotationProposed, 0))
        {
            return;
        }

        if (!_dontUseDampening)
        {
            //DAMPENING METHODS
            // MAHDI: Sinusiodally scaling the rotation when the bearing is near zero
            var bearingToTarget = Vector3.Angle(RedirectionManager.currDir, desiredFacingDirection);

            if (_useBearingThresholdBasedRotationDampeningTimofey)
            {
                // TIMOFEY
                if (bearingToTarget <= BearingThresholdForDampening)
                {
                    rotationProposed *= Mathf.Sin(Mathf.Deg2Rad * 90 * bearingToTarget / BearingThresholdForDampening);
                }
            }
            else
            {
                // MAHDI
                // The algorithm first is explained to be similar to above but at the end it is explained like this. Also the BEARING_THRESHOLD_FOR_DAMPENING value was never mentioned which make me want to use the following even more.
                rotationProposed *= Mathf.Sin(Mathf.Deg2Rad * bearingToTarget);
            }


            // MAHDI: Linearly scaling the rotation when the distance is near zero
            if (desiredFacingDirection.magnitude <= DistanceThresholdForDampening)
            {
                rotationProposed *= desiredFacingDirection.magnitude / DistanceThresholdForDampening;
            }
        }

        // Implement additional rotation with smoothing
        var finalRotation = (1.0f - SmoothingFactor) * _lastRotationApplied + SmoothingFactor * rotationProposed;
        _lastRotationApplied = finalRotation;

        if (!curvatureGainUsed)
        {
            InjectRotation(finalRotation);
        }
        else
        {
            InjectCurvature(finalRotation);
        }
    }
}